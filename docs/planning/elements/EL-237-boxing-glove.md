# EL-237 · Boxing glove — named-identity spec

**Current delivery (21 November 2026):** [EL-237 ledger entry](../invest/named-elements.md#element-237), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` (published main `8d1d68aa`); abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-237 · Boxing glove · Mechanical |
| Requirement anchor | [todo-194](../requirements.md#todo-194) (`sequence-task-376`) |
| Ledger entry / owner | [element-237](../invest/named-elements.md#element-237); slice `named-el-237`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a).** Run "supplied sliders" in delivery batches 1b. Not a duplicate of the Linear pusher (CAT-039): one pulse gives one timed punch with a cooldown, the glove springs back, and there are no extend, retract or limit ports. |
| Planned package | id `boxing_glove`, directory `elements/BoxingGlove/` |
| Related | [CAT-039 Linear pusher](CAT-039-linear_pusher.md), [CAT-051 Powered gate](CAT-051-powered_gate.md) (sprung supplied slider), [CAT-033 Hold timer](CAT-033-hold_timer.md) (window), [EL-222 Pinball flipper](EL-222-pinball-flipper.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (the drive's work paid from the Battery within its power limit; section 3) and `ENGINE-ISLANDS-SLEEP` (a homed glove rests) |

## 2. Declaration

Shape: the Powered gate's sprung, fixed-target slider (`elements/PoweredGate/PoweredGateElementDeclaration.cs:76`) commanded by the Hold timer's window (`elements/HoldTimer/HoldTimerElementDeclaration.cs:33`, `:40`).

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), `IClippedExtrusionElement` (`SceneGeometry.cs:19`) for the arm.
- **Housing (body 0, static).** Box 0.8 × 0.6 × 0.6 m. Restitution 0, friction 0.3.
- **Glove (body 1, dynamic).** Sphere radius 0.28 m; mass 0.5 kg; inertia 0.4 m r². Restitution 0.2, friction 0.4: a dull face that shoves rather than bounces.
- **Arm.** Clipped extrusion of radius 0.06 m between housing and glove, as the pusher's rod (`LinearPusherElementDeclaration.cs:40`).
- **Slider.** Along +X from the housing face, limits 0 to `reach`; spring 30 N/m with damping 4 N·s/m pulls the glove home (damping ratio 0.52, home in about 0.5 s). Drive: fixed target `reach`, maximum speed `punch_speed`, stopping acceleration 60 m/s², effort 40 N, power 40 N × `punch_speed`, inactive response `Free`. At the default 3 m/s that is 120 W, a default Battery's limit (`elements/Battery/BatteryElementDeclaration.cs:44`); faster settings need a Battery whose `power_limit` is raised to 40 × `punch_speed` (320 W at 8 m/s; the Battery admits up to 480 W). The Battery's limit binds the drive only once `ENGINE-SUPPLY-DEMAND` pays drive work from its source; drive supply is binary today (`_bmad-output/implementation-artifacts/deferred-work.md:211`).
- **Signal node.** `SignalTimerDescriptor("strike", "cooldown_seconds", TimerCompletionPolicy.Rearm, TimerBoundary.BeforeNetworks, TimerElapsedSignal.None)` (`SignalContractValues.cs:49`). Checked against the signal-node gaps: this is the Hold timer's delivered policy; the validator admits Rearm only with no elapsed signal at the `BeforeNetworks` boundary (`CuriousContraptions.Bundle/SignalBundleValidation.cs:157`), which this timer meets, so no row of `ENGINE-SIGNAL-NODES` is needed.
- **Supply graph.** `PowerIn` → a contact closed while the timer counts (`SignalStateContactControl`, `SignalContactState.TimerCounting`, `SignalContactControl.cs:5`, `:12`) → a consumer with `SupplyDriveTarget` on the slider (`SupplyContractValues.cs:52`).

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `reach` | Real, m | 0.3–1.2 | 0.8 | Reject, build-only | Spans one ball lane; the spring at the longest reach (36 N) stays under the 40 N effort |
| `punch_speed` | Real, m/s | 2–8 | 3 | Reject | The fastest punch a default Battery pays for; a directed shove, well below the Bumper's 8 m/s target |
| `cooldown_seconds` | Real, s | 0.3–5 | 0.6 | Reject | Seconds, not ticks (D10). The default exceeds the 0.3 s default stroke; a window shorter than the stroke gives a short punch, which is physical |

| Port | Domain | Direction, role, kind | Position (m) |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input, PowerIn | (−0.4, −0.3, 0.3) |
| `ActivationIn` | Signal | Input, ActivationIn, Pulse | (−0.4, 0.3, 0.3) |

- **Presentation.** Glove body binding; indicator colour from `CommittedTimerActive` (slate to gold, as the Hold timer); cooldown bar from `CommittedTimerRemaining`.
- **Palette.** Power, "Boxing glove"; order 309 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Housing | Box, cream `#fff8e9` (owner decision 4) | Solid, collider on body 0 |
| Glove | Sphere 0.28 m, coral `#d96b58`, with a navy cuff ring flush to the sphere | Solid, sphere collider on body 1; the cuff is printed flat |
| Arm | Lattice drawn inside a 0.06 m cylinder | Solid: the clipped extrusion is its collider |
| Indicator and cooldown bar | Flush on the housing top | Decorative: printed flat |

Icon: a round glove on a zigzag arm. It differs from the Linear pusher by the round glove and zigzag, and from the Bumper by having a direction.

### Behaviour and numbers

1. A pulse on `ActivationIn` starts the timer. While it counts and `PowerIn` is supplied, the drive throws the glove to `reach` (about 0.3 s at the defaults) and holds it there.
2. When the window ends the drive is unsupplied, the slider is free and the spring returns the glove.
3. A pulse during the window is ignored; expiry rearms. So `cooldown_seconds` is the shortest interval between punches. This is the Hold timer's rule, not a new one.
4. No supply: the timer still counts but nothing moves (a command alone supplies nothing). A blocked glove stalls against the obstacle with at most 40 N and reports Blocked.
5. Work is finite: the ball's kinetic energy cannot exceed the drive work, bounded by 40 N × `reach` (32 J at the defaults). Hand calculation at the defaults: the 0.5 kg glove reaches 3 m/s within 0.1 m; meeting a 1 kg ball 0.4 m out it slows to 1 m/s (dead contact, 0.5 × 3 ÷ 1.5); the drive, 40 N less 12 to 20 N of spring, then gives the pair about 16 m/s² and brings them back to the 3 m/s limit within 0.25 m, inside the 0.4 m left. The ball leaves at about **3 m/s**.
6. Tick order: the timer boundary is `BeforeNetworks`, so the contact closes before the same tick's supply solve, as the Hold timer does.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Sprung slider with a fixed-target supplied drive that returns when unsupplied | present | `PoweredGateElementDeclaration.cs:76` | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` |
| Timer window that ignores busy triggers and rearms | present | `SignalContractValues.cs:9` | `HoldTimerTests.BusyInputsDoNotExtendAndExpiryRearmsWithoutAnyOutput` |
| Contact closed while a timer counts | present | `SignalContactControl.cs:12` | `HoldTimerTests.PhysicalTriggerHoldsExactWindowWithoutGeneratingSupply` |
| Drive power and work bounds | present | `MechanicalContractValues.cs:93` | `PrismaticWorkBudgetTests` (class); `RopeTests.ASuppliedPrismaticDriveHeldByARopeHoldsShortOfItsTargetAndCommitsRest` |
| Blocked phase on obstruction | present | `MechanicalContractValues.cs:87` | `DeclaredPrismaticDriveTests.BlockedNeedsContactInteriorAndRestTogether` |
| Clipped extrusion collider for the arm | present | `SceneGeometry.cs:19` | `LinearPusherTests.PackageInstallationRoundTripsItsDriveExtrusionAndPhaseBinding` |
| A Level source refused on a Pulse input | present | `SignalContracts.cs:29` | `StateLatchTests.Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes` |
| The timer contact supplying a drive inside the same package | present, untested | `SupplyContractValues.cs:40`, `:52` | Nearest: `ComposableSupplyRoutingTests.ComposedPoweredAssemblyChargesStoreAndEnablesDriveWhenRouteIsClosed`; the slice adds `BoxingGloveTests.OnePulseGivesOnePunch` |
| **A supplied drive's committed work debited from its source within the source's power limit** (the Battery sizing in section 2; G5's 320 W Battery) | **missing** | drive supply is binary (`_bmad-output/implementation-artifacts/deferred-work.md:211`) | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |

Counts: 7 present, 1 untested, 0 narrower, 1 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static housing, one sprung slider with a supplied fixed-target drive, one timer node, one contact and two ports: the Powered gate's slider commanded by the Hold timer's window. The punch is the drive law within its declared speed, effort and power; the cooldown is the timer rule; the return is the spring law; the shove is rigid contact. The missing capability is stated for any package: a supplied drive's committed work debited from its source within the source's power limit, owned by `ENGINE-SUPPLY-DEMAND`. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 1 box (housing), 1 sphere (glove) and 1 clipped extrusion (arm) as hidden `col_` proxies; the lattice is drawn inside the extrusion. Visuals: the art nodes, none a collider. Animation: indicator and cooldown bar are bindings from committed timer state. What moves each art node: housing, nothing (static body 0); glove and its printed cuff, body 1's pose; arm, the clipped extrusion spanning the housing and the glove's pose; indicator and cooldown bar, driver bindings only.
- **Flat state.** 2 bodies; 3 colliders (1 box, 1 sphere, 1 clipped extrusion); 1 prismatic joint; 1 drive; 0 sensors; 1 timer node; 1 contact; 1 consumer; 0 stores; 1 body-pose and 2 driver bindings.
- **Performance.** The Linear pusher's primitive count. The housing is a static body. Whether a homed glove may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The glove moves at 3 m/s by default and 8 m/s at most; thin targets rely on the engine's speculative contacts ([numeric authority](../../gpu-f32-physics.md)).
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-194](../requirements.md#todo-194): "Add a directional actuator with explicit contact face and cooldown. Teach timing a lateral impulse rather than continuous force."
- **Campaign.** Chapter 5 (41–50), "Linear pusher/boxing glove"; coverage row "Pusher, boxing glove, flipper, …; each distinct contact/charge/carry contract remains separate".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: speed asserted as a floor or ratio, never a value; position ±0.02 m; the window is a timer contract, so its length is asserted to ±1 tick at the selected cadence; stall guard 10 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| G1 | Basketball resting 0.4 m in front of the glove, a default Battery, one pulse | The ball leaves along +X at 2 m/s or more (estimate 3 m/s) | Control: no pulse, or pulse with no supply: the ball moves less than 0.01 m | `BoxingGloveTests.OnePulseGivesOnePunch` |
| G2 | A second pulse inside the window; a third after it | One extension for the first two pulses, a second for the third | Boundary: window 0.3 s and 5 s admitted and measured to ±1 tick | `BoxingGloveTests.TheCooldownIgnoresEarlyPulsesAndRearms` |
| G3 | Ball offset sideways by more than both radii plus 0.05 m | Untouched | Control: aligned ball is struck | `BoxingGloveTests.ALateralMissLeavesTheBallAlone` |
| G4 | Wall 0.2 m in front | Glove stops at the wall, Blocked, no penetration, then returns | Control: no wall, full reach | `BoxingGloveTests.ABlockedGloveStallsAndReturns` |
| G5 | Any punch | Ball kinetic energy ≤ reported drive work + 0.5 J | Control: `punch_speed` 2 gives a slower ball than 8, the 8 m/s case on a Battery set to 320 W | `BoxingGloveTests.ThePunchNeverExceedsItsDriveWork` |
| G6 | Clock → glove beside a Conveyor lane | Each accepted pulse knocks one ball off the lane | Control: clock unpowered | `BoxingGloveTests.AClockTimesLateralPunches` |
| G7 | Reset mid-punch; Save, Load | Glove home, timer ready, no stale punch on the next Run; parameters and wires restored | — | `BoxingGloveTests.ResetMidPunchLeavesNoStalePunch` |
| G8 | Electrical wire into `ActivationIn`; Pressure plate level into `ActivationIn`; `reach` 0.2 | Refused with a typed reason | Boundary: `reach` 0.3 and 1.2 admitted | `BoxingGloveTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

**Integration.** Battery, Impact switch, Clock, Delay, Conveyor, Ball detector (delivered). A level that places it also needs lesson fixtures bound to a named package (S8).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `boxing_glove`; Named identity EL-237; Acceptance todo-194 and sequence-task-376; owner slice `named-el-237`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` and `ENGINE-ISLANDS-SLEEP`. Other prerequisites: the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: `elements/BoxingGlove/` (project, declaration and `.uid`, `boxing_glove.tscn`, icon and import), `CuriousContraptions.tests/BoxingGloveTests.cs`, and the usual hand-edited lists until S3 (`CuriousContraptions.slnx`, `CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596` and `:653`, one `VisibleSolidColliderTests.cs:110` row). Review tier: engine (one untested capability). **Disqualified if** the probe finds a timer contact cannot feed a drive consumer inside one package; then the contact-to-drive route is an engine slice of its own before this element.

## 6. Open questions

1. *Does the glove snap back at once or dwell?* Resolved under the standing delegation: it dwells at reach until the window ends. One declared timer then gives both the punch and the cooldown; a snap-back would need a second chained timer and no lesson needs it (D19).
2. *Strike on a level input as well?* Resolved by D19: pulse only; a held level is what the Linear pusher is for.
3. *A declared spring rest position?* Coordinator decision 25, 22 November 2026, was checked and does not apply: the spring returns a punch home, and the glove is not a blade, hatch or gate that must close when unpowered.

Needs the owner: nothing.
