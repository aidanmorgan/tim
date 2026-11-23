# EL-068 · Tin snips declaration readiness spec

**Current delivery (24 October 2026):** [EL-068 delivery slice](../invest/named-elements.md#element-068). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against published main `8d1d68aa`; their `path:line` citations are to that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-068 |
| Name | Tin snips |
| Type | Mechanical |
| Requirement anchor | [element-068](../requirements.md#element-068); scope index [todo-210](../requirements.md#todo-210) |
| Named entry | [named-elements.md#element-068](../invest/named-elements.md#element-068); proof owner S357 |
| CAT spec refined or extended | none. Related: [CAT-005 battery](CAT-005-battery.md) (supply), [CAT-063 switch](CAT-063-switch.md) (actuation by switching the supply), [CAT-042 motor](CAT-042-motor.md) (supplied hinge drive pattern), [CAT-058 rope anchor](CAT-058-rope_anchor.md) (route topology) |
| Related identities | [EL-066 scissors](EL-066-scissors.md) (same cut criterion, unpowered), [EL-067 steel cable](EL-067-steel-cable.md) (resistant target), EL-205 rope |
| Roadmap story | [EL-068 delivery slice](../invest/named-elements.md#element-068); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the stroke's work debits its Battery and is limited to the Battery's per-tick allowance) and ENGINE-SPATIAL-QUERIES (candidate spans); then ENGINE-CONNECTION-MATERIALS (declared materials and the solid strand) and ENGINE-TENSILE-EXTENSIONS (cutting clause, with a parameter-bound capacity); then S7. Its jaw, drive and supply are declarable today |
| Disposition | To deliver as a single element. Not a family with the Scissors: it adds an electrical port, a supplied drive and a parameter |
| Planned package | `tin_snips`, directory `elements/TinSnips/`, class `TinSnipsElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on the coverage follow-on story S7. No CAT number |
| delivery-batches class | Not listed in 1a, 1b or 1c of [delivery-batches](../invest/delivery-batches.md). Its map row names StructuralFracture and TopologyTransaction (`docs/planning/general-engine-element-map.md:173`), which `ENGINE-FRACTURE` unblocks (`_bmad-output/planning-artifacts/epics.md@25df3cd5:4472`). It belongs in 1c under "Joints and fracture", run "Cutters", after EL-066; the cut itself is the cutting clause of ENGINE-TENSILE-EXTENSIONS, not ENGINE-FRACTURE (section 3) |

## 2. Declaration

Follows the delivered **Motor** shape: a static housing carrying one body on a hinge with a supplied drive, consumed by its own `PowerIn`, with a supply lamp (`elements/Motor/MotorElementDeclaration.cs:16`, `:25`–`:28`, `:88`–`:127`).

**Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the cutter declaration the engine story adds. `KinematicMode.Static` root; domains Mechanical and Electrical.

**Bodies and colliders** (pivot at the origin, hinge axis +Z, jaws in the XY plane pointing +X).

| Body | Collider | Box full size (m) | Centre (m) |
| --- | --- | --- | --- |
| 0 static | housing | 0.40 × 0.40 × 0.30 | (−0.20, −0.10, 0) |
| 0 static | fixed lower jaw | 0.29 × 0.06 × 0.10 | (0.205, −0.03, 0), from x 0.06 to 0.35; cutting edge its top, y = 0 |
| 1 jaw (dynamic, hinged) | upper jaw | 0.29 × 0.08 × 0.10 | (0.205, 0.04, 0) in the jaw's frame, from x 0.06 to 0.35; cutting edge its underside, y = 0. Its back-top corner (0.06, 0.08) turned by the 0.6 rad open pose sits at x = 0.06·cos 0.6 − 0.08·sin 0.6 = +0.004, clear of the housing face at x 0 (a jaw starting at x 0 would reach −0.045 into the housing) |

**Mass and inertia.** Housing static. Jaw 0.2 kg, centre of mass at its box centre, principal moments of a uniform box (m·(b² + c²)/12 and cyclic) with 0.29, 0.08, 0.10 m, gravity (0, −9.81, 0).

**Material** (restitution, friction, rolling resistance, bounce threshold): all three colliders `(0, 0.3, 0, 0.1)`, the Motor's rigid-part values (`MotorElementDeclaration.cs:63`–`:64`).

**Hinge and drive.** One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0; base frame at the pivot turned +0.6 rad about Z, so angle 0 is the open pose (jaw raised 34°); `RevoluteTravel.Limited`, lower limit −0.6 rad (closed), upper limit 0; stiffness fixed 6 N·m/rad (reopens the jaw); damping fixed 0.1 N·m·s/rad; friction torque fixed 0.02 N·m; initial angle 0; `BodiesCollide: false`. `Drive`: target speed fixed −3 rad/s (closing), maximum torque fixed 100 N·m (the Motor's tested maximum, `MotorElementDeclaration.cs:44`).

**Supply.** As the Motor (`MotorElementDeclaration.cs:120`–`:127`): one `SupplyConsumerDescriptor` whose target is `SupplyDriveTarget(Revolute 0)` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:52`), bound to `PowerIn`. Electrical node: consumer, 12 V, maximum power 100 N·m × 3 rad/s = 300 W, resistance 12² / 300 = 0.48 Ω. The Battery's own electrical node declares `MaxPower` 100 W (`elements/Battery/BatteryElementDeclaration.cs:76`), but that value is only checked to be finite (`CuriousContraptions.Bundle/BundleValidator.cs:876`) and limits nothing, so it neither caps nor refuses this 300 W consumer. Today supply is on or off and the drive's work is not debited; ENGINE-SUPPLY-DEMAND (owner decision 3a, sprint key `engine-supply-drive-demand`) comes first. It debits the drive's committed positive work from the Battery (row T13), and when the drive asks for more than the Battery's per-tick allowance (`power_limit` × dt: 1 J per tick at the default 120 W) it limits the drive to that allowance in that tick, scaling its torque so that the work done that tick equals the energy granted (coordinator decision 27, 23 November 2026). At jaw speed ω the drive can therefore apply min(100 N·m, `power_limit` ÷ ω).

**Stroke power (hand calculation).** An empty stroke needs at most about 4 N·m (spring at most 3.6 N·m, damping 0.3 N·m at 3 rad/s, friction 0.02 N·m, less the jaw's weight), about 12 W at 3 rad/s, well inside 120 W. Cutting a steel cable at the acceptance crossing (0.10 m) needs about 63–66 N·m (62.5 N·m from the cable plus 1.1 to 3.6 N·m of spring across the band), 66 × 3 ≈ 200 W at full speed, above the default allowance. At the default `power_limit` 120 W the drive can hold 66 N·m only while ω ≤ 120/66 = 1.82 rad/s, so the jaw slows to about 1.8–1.9 rad/s through the 0.412 rad engagement band and the cut still completes: the band's work is about 26 J (25 J of cutting, 0.97 J of spring work ½·6·(0.6² − 0.188²), less about 0.15 J returned by the jaw's weight), which takes 26/120 ≈ 0.22 s. The standard rig therefore uses a Battery at its default 120 W; no raised `power_limit` is needed. At the 480 W maximum the drive is not limited (480/66 = 7.3 rad/s, above its 3 rad/s target) and crosses the band at 3 rad/s; at the 10 W minimum the same cut takes about 26/10 = 2.6 s. The reach below is a torque bound and is the same at every `power_limit`: the full 100 N·m is available once the jaw has slowed to `power_limit` ÷ 100 rad/s.

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `cut_work` | Real | `ParameterUnit.Joule` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:25`) | 30 | 5 | 40 | Reject | BuildOnly |

`cut_work` is the cutter's capacity C: the "declared cutting-work range" of the requirement row. Player-editable unless the level locks it (D11).

**Ports.**

| `PortId` | Role | Domain | Direction | Signal | Body | Position (m) |
| --- | --- | --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | Electrical | Input | none | 0 | (−0.40, −0.10, 0), normal −X |

There is no activation port and no Tie port. The strand to be cut only passes between the jaws.

**Cutter (declared to the engine capability of section 3).** Fixed edge on body 0 from (0.06, 0, 0) to (0.35, 0, 0); moving edge on body 1 over the same span, closing on revolute coordinate 0 toward its lower limit; blade thickness ±0.05 m in Z; capacity C bound to `cut_work`.

**Stores.** None. **Presentation.** `Bodies: [("Visual/Jaw", PackageBodyIndex(1))]`; three albedo bindings on `Visual/Lamp` driven by `PresentationDriver.CommittedActuatorDrive`, slate to gold over 0.1 s, copied from the Motor (`MotorElementDeclaration.cs:25`–`:27`). Pick radius 0.5 m. Footprint: 2 bodies, 1 moving, 0 prismatic, 1 revolute.

**Palette.** `ElementCategory.Power`, palette order 302 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), id `tin_snips`, display name "Tin snips", catalogue colour the Motor's cyan `(0.40, 0.72, 0.79)` (`DESIGN.md:224`–`:246` table).

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Visual_Housing` | cyan box 0.40 × 0.40 × 0.30 on a navy band printed on its lower face | solid, housing collider; the band is decorative: printed flat |
| `Visual_FixedJaw` | pale grey box 0.29 × 0.06 × 0.10 | solid, fixed-jaw collider |
| `Visual/Jaw` | pale grey box 0.29 × 0.08 × 0.10 | solid, jaw collider |
| `Visual/Lamp` | disc r 0.05 flush on the housing's front face | decorative: glow, printed flat |

Icon: a box with a short open jaw and a lightning mark. At a glance it differs from the Scissors by the housing and lamp, the short thick jaws and the absence of a handle.

### Behaviour and numbers

**Cutting contract.** Stated once in [EL-066](EL-066-scissors.md), section 2, and cited here unchanged: a span between the open edges engages when the gap at its crossing point falls to the strand diameter d, resists closing with force R/d there, is severed at gap 0 if R is at most the capacity C, and holds the gap at d if R exceeds C. The absorbed work leaves the mechanism.

| | Rope, d 0.04 m, R = 1 J (25 N) | Steel cable, d 0.04 m, R = 25 J (625 N) |
| --- | --- | --- |
| Scissors, C = 4 J | cut when the delivered work reaches 1 J | never cut |
| Tin snips, C = `cut_work` 5–40 J, default 30 | always cut | cut when `cut_work` ≥ 25 J and the crossing is within 0.154 m of the pivot |

- **What the drive can cut.** Acceptance routes cross the jaws 0.10 m from the pivot. There a strand engages asin(0.04/0.10) = 0.412 rad short of closed, and a steel cable resists with 625 N × 0.10 m = 62.5 N·m; with the return spring (at most 3.6 N·m) the 100 N·m drive keeps a 34 % margin. In general a strand of resistance R at distance x is cut when R ≤ `cut_work` and R·x/0.04 + 3.6 ≤ 100 N·m: a cable out to 0.154 m, a 40 J strand out to 0.096 m. Farther out the jaw stalls on the strand, so placement matters, as with real snips. The earlier claim that 70 N·m over the whole stroke (42 J) could always pay the capacity was wrong: the work must be delivered inside the engagement band.
- **Powered.** While `PowerIn` is supplied the jaw closes in about 0.2 s (0.6 rad at 3 rad/s) and then holds closed, stalled against its stop, doing no further work. One closing is one stroke.
- **Actuated.** The stroke is triggered by switching the supply: a delivered Impact switch, Powered gate or logic gate in the line between Battery and `PowerIn`. There is no separate trigger port.
- **Reopening.** Unsupplied, the spring reopens the jaw. The jaw's weight (0.2 kg × 9.81 m/s² × 0.147 m, the horizontal lever of its centre (0.205, 0.04) at the 0.6 rad open pose: 0.29 N·m) and 0.02 N·m of friction leave it about (0.29 + 0.02)/6 = 0.05 rad short of fully open, which is the open rest pose.
- **Jaw gap.** The open tips are about 0.18 m apart at rest (0.35 m × sin 0.55 rad). No ball fits (the smallest, the Tennis ball, is 0.5 m across), so the 286 N tip force (100 N·m at 0.35 m) meets only strands, the hook's bar or a domino edge.
- **Tick order.** Electrical supply is solved in Phase 1 of the tick and enables the drive for that tick's physics; the cut commits with the physics step in which the contract is met.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Static housing with one body on a limited hinge; stops arrest travel | *present* | `RevoluteHingeDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `CuriousContraptions.tests/ImpactLeverTests.cs:380` |
| Supplied velocity drive on a limited hinge: runs to its stop, then stalls without work; unsupplied it applies nothing | *present* | `RevoluteDriveDescriptor` `MechanicalContractValues.cs:138` | `CuriousContraptions.tests/RevoluteDriveTests.cs:307`, `:259` |
| The drive is enabled by the package's own `PowerIn` and follows the supply during Run | *present* | `SupplyDriveTarget` `CuriousContraptions.Abstractions/SupplyContractValues.cs:52` | `CuriousContraptions.tests/MotorTests.cs:361`, `:330` |
| Supply lamp from the committed drive state | *present* | `PresentationDriver.CommittedActuatorDrive` `CuriousContraptions.Abstractions/ElementPresentation.cs:23` | `CuriousContraptions.tests/MotorTests.cs:265` |
| An activation wire into `PowerIn` is refused | *present* | `engine/gpu/WorkshopConnections.cs:154`–`:159` | `CuriousContraptions.tests/MotorTests.cs:316` |
| A package-declared drive with a fixed target speed and fixed torque (no parameter, no run control) | *present, untested* | admitted by `CuriousContraptions.Bundle/BundleValidator.cs:825`–`:826` | none: the only delivered driven hinge binds both to parameters (`MotorElementDeclaration.cs:115`). Row T7 is the first test |
| Constant return spring on a limited hinge, declared by a package | *present, untested* | `RevoluteHingeDescriptor.Stiffness` `MechanicalContractValues.cs:156`; `engine/gpu/WorkshopPhysicsCompiler.cs:221` | Engine fixture only: `CuriousContraptions.tests/AirflowRuntimeTests.cs:14`. First package test is [EL-066](EL-066-scissors.md) X7 or this spec's T7, whichever lands first |
| Removing a route's tensile rows at a committed step; Reset restores | *present* (player command only) | `engine/gpu/WorkshopSimulation.cs:11` | `CuriousContraptions.tests/RopeTests.cs:151`, `:882` |
| A declared material on a tensile connection, with its solid strand (owner decision 1) | *missing* | none | ENGINE-CONNECTION-MATERIALS (solid strand) ([EL-067](EL-067-steel-cable.md) section 3) |
| **The blade-pair cut under the cutting contract, with the capacity bound to a package parameter** | *missing* | none | ENGINE-TENSILE-EXTENSIONS, cutting clause ([EL-066](EL-066-scissors.md) sections 2 and 3; the one cutting law, coordinator decision 16). The clause must admit a parameter-bound capacity, not only a constant |
| A drive's committed positive work debited from its route's finite source | *missing* | none: supply is on or off (`deferred-work.md:209`) | ENGINE-SUPPLY-DEMAND (owner decision 3a) |

Map note "Source D: distinct supported material/cutting capacity; no target-name snip" is met: the rule compares R with C and row T4 proves it on a third material.

**Element dependencies.** Delivered: Battery, Impact switch, Powered gate, logic gates, Rope anchor, Weight. Not delivered: EL-067 steel cable (required: the row's "resistant material"), EL-205 rope material, EL-066 (shares the criterion, lands first).

### Engine conformance

- **Declaration data only.** One limited hinge with a constant spring and a supplied drive, one electrical consumer, one declared blade pair whose capacity is bound to a parameter. The cut is the general cutter capability, not snips logic. No callback, script or engine branch on this part.
- **Generic laws.** The stroke is the supplied revolute drive law; reopening is the hinge spring; severing is the cutting contract reading declared d, R and C; "no cable-name special case" is that contract reading numbers only (row T4).
- **Layers.** Colliders: 2 static boxes (housing, fixed jaw) and 1 box on the hinged jaw; a strand between the jaws is a solid connection-material collider that the blade edges do not meet ([EL-066](EL-066-scissors.md) section 2). Visuals: the art table, none used as a collider; every proud visible solid has its collider. Animation: `Visual/Jaw` follows a body pose; `Visual/Lamp` colour is a presentation binding reading the committed drive state; the printed band is moved by nothing.
- **One-way flow and typing.** The lamp binding reads committed state and writes nothing back. Port role and domain are enums; `cut_work` is a typed f32 parameter in joules.
- **Flat state.** 2 bodies, 3 box colliders, 1 revolute joint, 1 drive, 1 cutter row, 1 supply node, 1 electrical node, 0 sensors, 0 stores, 1 body binding and 3 albedo bindings.
- **Performance.** As EL-066: the blade pair is a bounded region tested against spans only, its candidates from ENGINE-SPATIAL-QUERIES. Three primitive colliders; the housing is static. No island sleeping exists today; resting snips cost nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows T1 to T12 run the production simulation through placed parts and wires.

## 4. Sources and legacy

- Requirements row: "Insufficient work fails on resistant material; no cable-name special case." No variants.
- Design note: EL-066/068 use the shared fracture criterion, not counterpart-type checks (`docs/general-engine-design.md@a6c914e:L149-L149`).

No legacy implementation (rope cutting recorded as future work, `reference/P0-022-before/source-README.md@a6c914e:L99-L99`). Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A bounded drive declares maximum speed, acceleration, force and power = force × speed. | `parts/LinearPusherPart.cs@a6c914e:L28-L32` (via [CAT-039](CAT-039-linear_pusher.md)) | carry forward (pattern) | Jaw drive budget. |
| 2 | A supplied actuator with no supply cannot start motion. | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L136-L141` | carry forward | Powered-mode negative control. |

**Files harvested:** `parts/LinearPusherPart.cs` (via CAT-039), `CuriousContraptions.tests/MotorPredictionTests.cs`, `reference/P0-022-before/source-README.md`. Searched for snip, shear, cutter: no hit.

## 5. Acceptance outline

Requirement row: [element-068](../requirements.md#element-068).

- **Chrome UI recipe (pending, post-catalogue).** Place a Rope anchor, a Weight hanging by steel cable that passes between the snips' jaws, a Battery, and an Impact switch wired Battery → switch → `PowerIn` that a rolling ball strikes. Verify `cut_work`, wires and route; Run; Reset.

All tests are planned, in `CuriousContraptions.tests/TinSnipsTests.cs`, on the real simulation. In each, route 1 runs from a Rope anchor to a 4 kg Weight A and crosses the jaws 0.10 m from the pivot. Stall guard: 5 s.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and its gameplay reason |
| --- | --- | --- | --- | --- | --- |
| T1 cut a resistant material | Route 1 in steel cable, `cut_work` 30, a Battery at its default `power_limit` 120 W wired. Run: route 1 reads Cut and Open and A falls; while the jaw is between 0.30 and 0.05 rad short of closed (inside the 0.412 rad engagement band, past the arrival transient) its committed speed stays at or below 2.2 rad/s, because the drive holds the cable's 63–66 N·m only up to 120/66 = 1.82 to 120/63 = 1.9 rad/s (section 2, stroke power) | `TinSnipsTests.SuppliedSnipsCutASteelCable` | Battery disabled: jaw stays open, route Intact, A hangs. Battery at `power_limit` 480 W: not limited (480/66 = 7.3 rad/s, above the 3 rad/s target), so the jaw crosses the same window at 2.5 rad/s or more; an engine that ignored the allowance would show that speed at 120 W too | Battery at `power_limit` 10 W (the minimum): still cut, about 26 J ÷ 10 W = 2.6 s after engagement, inside the 5 s guard; an engine that stopped an over-allowance drive instead of limiting it leaves the route Intact | A more than 1 m below its start within the guard; before the cut within 0.1 m. The 2.2 rad/s bound sits about 15 % above the derived 1.9 rad/s and 27 % below the unlimited 3 rad/s, so either wrong law fails it |
| T2 rope | Route 1 in rope, `cut_work` 5 (the minimum): cut | `TinSnipsTests.SnipsAtTheirLowestWorkCutARope` | T1's control | `cut_work` 40 | as T1 |
| T3 exact source outcome: insufficient work fails on resistant material | Steel cable, `cut_work` 20: the jaw stops on the cable, route Intact, A hangs for 5 s | `TinSnipsTests.TooLittleCutWorkLeavesASteelCableIntact` | T1 | `cut_work` 24 (intact) and 25 (cut): R and C are declared values, so the boundary is exact | Jaw more than 0.05 rad short of closed (gap held at d: 0.41 rad); A within 0.1 m |
| T4 exact source outcome: no cable-name special case | A third material declared in the test bundle with R = 10 J is cut at `cut_work` 10 and not at 9; a test material that copies the cable's name-free values (R = 25 J) behaves as the cable does | `TinSnipsTests.AMaterialIsCutByItsDeclaredResistanceAlone` | T1 and T3 | 9 and 10 J | as T3 |
| T5 wrong placement | Cable routed 0.10 m beyond the jaw tips: the jaw closes fully, route Intact | `TinSnipsTests.ACableBesideTheJawsIsNotCut` | T1 | Cable crossing 0.30 m from the pivot, inside the jaws: it needs 187.5 N·m, the jaw stalls on it and the route stays Intact | Beyond the tips: jaw within 0.03 rad of closed. At 0.30 m: jaw more than 0.05 rad short of closed |
| T6 actuated | Battery → Impact switch → `PowerIn`; a ball strikes the switch: cut. | `TinSnipsTests.SwitchingTheSupplyTriggersTheStroke` | No ball: no stroke, Intact for 5 s | none | Cause before effect: the switch's committed closure tick is not after the cut's |
| T7 stroke, hold and reopen | Supplied with nothing in the jaws: closed within 0.5 s, then the drive's work grows by under 1 % of a free second's work; unsupplied: back within 0.1 rad of open in 1 s | `TinSnipsTests.TheJawClosesHoldsWithoutWorkAndReopens` | Unsupplied from the start: open | none | 0.1 rad covers the 0.05 rad sag; work bound as `RevoluteDriveTests.cs:307` |
| T8 the cut costs its work | In T1 the cutter's committed cut-work read is 25 J, and the drive's work at the end of the stroke exceeds that of the empty stroke T7 | `TinSnipsTests.TheDriveIsChargedTheCuttingWork` | T7 | none | Read within 2 % of the declared 25 J; drive work higher by 20 to 30 J: a free cut or a double charge fails |
| T9 parameter refusal | `cut_work` below 5, above 40, NaN and infinite are refused at every seam and change nothing | S4 row R2 once delivered, else `TinSnipsTests.AnOutOfRangeCutWorkIsRefused` | 5 and 40 admitted | one f32 step outside each bound | Typed refusal |
| T10 Reset | After T1, Reset: route Intact, jaw open, lamp slate, drive work zero; a second Run cuts again | `TinSnipsTests.ResetRestoresTheCableTheJawAndTheLamp`; S4 row R3 once delivered | none | none | Authored values exactly |
| T11 save and load | `cut_work`, the wire and the route round-trip; a Build save holds no cut state | S4 rows R1a and R1b once delivered, else `TinSnipsTests.ASavedSnipsLoadWithTheirCutWorkAndWire` | none | none | Field equality |
| T12 wrong-kind link | An activation, rope or mechanical link to `PowerIn` is refused | S4 row R5 once delivered, else `TinSnipsTests.AWrongKindLinkIsRefused` | The battery wire is admitted | none | Typed refusal |
| T13 the stroke pays its Battery | In T1 the Battery's stored energy falls by the drive's committed positive work; an empty Battery (`initial_charge` 0) gives no stroke and leaves route 1 Intact | `TinSnipsTests.TheStrokeDebitsItsBattery` | T7's empty stroke debits less | `initial_charge` just enough for the empty stroke but not the cut: the drive stops when the Battery empties, the route stays Intact and the unsupplied jaw springs back open (as T7) | Debit never below the committed work and above it by no more than the ledger residual (the ENGINE-SUPPLY-DEMAND acceptance); a stroke that costs nothing would make every powered lesson free |

## 6. Open questions

**Resolved**

1. "Powered or actuated" is one supplied jaw: powered by its `PowerIn`, actuated by switching that supply. There is no stored-charge mode, no `mode` parameter and no `ActivationIn`. Decided now under D19: the row names no variants, no campaign lesson needs a one-shot charge, and the same idiom controls the Clutch and the brake (EL-055). The row's two outcomes keep their own rows (T3, T4).
2. The map row omits ElectricalPower and FiniteWorkActuation. D14: the probe is the authority.
3. The stroke follows continuous supply (cut while powered), one stroke per closing. Decided now with item 1.
4. No `power` parameter. Decided now: the revolute drive has no power bound (`MechanicalContractValues.cs:138`) and no lesson times the 0.2 s stroke.
5. `cut_work` is 5 to 40 J, default 30, replacing 5 to 60. Decided now: the range still straddles the cable's 25 J, and the fixed drive torque is 100 N·m, the Motor's tested maximum.
6. Art, colour and icon. D9.

**Needs the owner**

None. The player's Run-time "Cut rope" control is kept ([EL-066](EL-066-scissors.md), resolved item 7), and a level may withhold it as authored level data so the snips are the only way to cut (coordinator decision 24l, 23 November 2026); strands are solid (owner decision 1; [EL-067](EL-067-steel-cable.md) resolved item 8).

## 7. Integration

- **Collaborators.** Battery, Impact switch, Powered gate, logic gates, Rope anchor, Weight (delivered). EL-067 steel cable, EL-205 rope material, EL-066 scissors (not delivered).
- **Campaign.** Scissors and tin snips first use in levels 11–20 (`docs/planning/invest/campaign-reservations.md:44`). Shared interactions [IX-37 structural fracture](../requirements.md#interaction-37), [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-06 electrical power transfer](../requirements.md#interaction-06).
- **Lesson fixture (S8).** Needed by any level that places snips before this slice.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (`origin/main`, `25df3cd5`; pattern EL-239 Baseball). That section is not written here: the reader parses every spec that carries the heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `tin_snips` |
| Named identity | EL-068, `../invest/named-elements.md#element-068` |
| Acceptance | `todo-210`, `sequence-task-621` |
| Decision | S357, `../invest/scope-corrections.md#s357` |
| Mode records | `Configuration=Adjustable` (`cut_work`; confirmed at slice planning) |

What it lacks today: `scope-corrections.md` has no `s357` block, and `todo-210` is a scope index not immediately followed by `sequence-task-621` in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element slice, after EL-066 and EL-067.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND and ENGINE-SPATIAL-QUERIES; then (1) ENGINE-CONNECTION-MATERIALS (solid strand); (2) ENGINE-TENSILE-EXTENSIONS (cutting clause), with a parameter-bound capacity; (3) EL-067 steel cable and the rope material; (4) EL-066, so the contract's first consumer and the hinge-spring test already exist; (5) S7 (the coverage follow-on, which widens the reader, owner decision 15).
- **Files.** Adds `elements/TinSnips/TinSnips.csproj`, `TinSnipsElementDeclaration.cs`, `tin_snips.tscn`, `icon.svg`, `CuriousContraptions.tests/TinSnipsTests.cs`. No test-project edit (published main compiles every top-level `*.cs` by one rule). Edits, until S3 lands: `CuriousContraptions.slnx`, the trim list in `CuriousContraptions.tests/CSharpStaticGateTests.cs` (ends at `:703`) and the placement and parameter switches in `CuriousContraptions.tests/PackageRuntimeParityTests.cs` (`:483`, `:641`; generic list `:653`). At delivery: the binding section of section 7. This spec and TODO.
- **Review tier.** Engine: the fixed-value drive is *present, untested* (and the hinge spring too, unless EL-066 has tested it).
- **Disqualifiers the probe must re-check.** A fixed negative drive target is refused or a package with a driven hinge must declare a run-control parameter (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:48`); a return spring and a drive cannot share one limited hinge; the engine's cutter capacity cannot be bound to a parameter; the jaw's 100 N·m launches cargo it can reach (then lower the torque and re-derive the cutting reach, not the tolerances); ENGINE-SUPPLY-DEMAND does not limit an over-allowance drive as coordinator decision 27 states (then T1 fails and the story is reopened, not this part).
