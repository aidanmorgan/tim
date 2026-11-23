# EL-062 · Docking ferry declaration readiness spec

**Current delivery (24 October 2026):** [EL-062 delivery slice](../invest/named-elements.md#element-062). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

Deepened to build-ready detail on 21 November 2026 against checkout `8d1d68aa`; every `path:line` outside section 4 was read in that checkout. Values are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-062 |
| Name | Docking ferry |
| Type | Mechanical |
| Requirement anchor | [element-062](../requirements.md#element-062) (sequence-task-615); scope index [todo-320](../requirements.md#todo-320); integration [sequence-task-371](../requirements.md#sequence-task-371) |
| Named entry | [named-elements.md#element-062](../invest/named-elements.md#element-062); proof owner S372 |
| CAT spec refined or extended | none. Related: the Linear pusher (the declaration shape, delivered), [CAT-051 powered gate](CAT-051-powered_gate.md) (a loader gated by the interlock), [CAT-005 battery](CAT-005-battery.md) |
| Related identities | EL-220 Docking lift ([todo-429](../requirements.md#todo-429); the vertical sibling, same shape, a separate identity), [EL-061 indexed carousel](EL-061-indexed-carousel.md) (the other carrier; same interlock contract), [EL-055 brake](EL-055-mechanical-brake.md), [EL-063 cable winch](EL-063-cable-winch.md) |
| Roadmap story | [EL-062 delivery slice](../invest/named-elements.md#element-062); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (its supplied deck drive) and ENGINE-ISLANDS-SLEEP (a self-locked deck and its cargo at rest); then S7. No package exists. Planning spec at build-ready detail; no engine story of its own blocks it |
| Disposition | To deliver as a single element |
| Planned package | id `docking_ferry`, directory `elements/DockingFerry/`, class `DockingFerryElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | **Not classified** (in none of 1a–2c). The coordinator's guidance is **confirmed**: the ferry is declarable today as a supplied directional slider with endpoint-contact dock outputs. It belongs in the 1b run "Supplied sliders" beside EL-220. **Corrected** on tier: three capabilities it relies on are present but untested (section 3), so the slice is engine tier, not data-only |

## 2. Declaration

Shape followed: `elements/LinearPusher/LinearPusherElementDeclaration.cs`, with a deck instead of a head and scene-bound rail geometry as `elements/Pipe/PipeElementDeclaration.cs:53`–`:54`.

**Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`). Base `KinematicMode.Static`, `PayloadPolicy.None`, domains Mechanical | Electrical, footprint 2 bodies, 1 moving, 1 prismatic, 0 revolute.

**Bodies and colliders.** Local +X is the travel direction from dock A to dock B, Y is up; t = `travel`.

| Body | Collider | Shape (m) | Centre (m) |
| --- | --- | --- | --- |
| 0 base (static) | rails, 2 | box (t + 1.6) × 0.12 × 0.12 | (0.5·t, 0, ±0.56) |
| 0 | dock A post | box 0.3 × 0.7 × 1.4 | (−0.92, 0.20, 0) |
| 0 | dock B apron | box 0.4 × 0.44 × 1.4, top at y 0.22 | (t + 0.95, 0, 0) |
| 0 | sockets, 5 | sphere r 0.075 | tangent to the −X face of the dock A post (centres at x −1.145; positions under Ports) |
| 1 deck (moving) | deck | box 1.4 × 0.12 × 1.4, top at y 0.22 | (0, 0.16, 0) |
| 1 | back lip | box 0.08 × 0.15 × 1.4 | (−0.66, 0.295, 0) |
| 1 | side lips, 2 | box 1.4 × 0.15 × 0.08 | (0, 0.295, ±0.66) |

The deck is open at its +X end. Two narrow rails under the deck's edges replace the earlier single 0.3 m rail: the 1.0 m clear between them is wider than every delivered ball (0.70 m at most), so a ball released where the deck is not falls through instead of resting on a rail at deck level. `ColliderParameterBinding` rows: each rail `HalfExtentX` (0.5, 0.8) and `OffsetX` (0.5, 0); apron `OffsetX` (1, 0.95).

**Mass and inertia.** Deck 2.0 kg, constant mass frame, principal moments of the 1.4 × 0.12 × 1.4 box (0.33, 0.65, 0.33) kg·m², gravity (0, −9.81, 0). The rail constraint carries its weight.

**Material** (restitution, friction, rolling resistance, bounce threshold): base (0, 0.3, 0, 0.1) as the pusher (`LinearPusherElementDeclaration.cs:27`); deck (0.1, 0.5, 0, 0.1), so dropped cargo settles and box cargo is carried.

**Slider.** One `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`): base frame at (0, 0, 0) on body 0, with the pusher's axial frame (`LinearPusherElementDeclaration.cs:10`) so the slide axis is +X; the deck's frame coincides with it at travel 0, and the deck colliders above are in that frame; lower limit 0 (docked at A), upper limit `travel` (docked at B), no spring, no damping, no starting compression, bodies do not collide. Drive (`PrismaticDriveDescriptor`, `:89`): target `travel`, maximum speed `speed`, stopping acceleration fixed 2 m/s², maximum effort fixed 60 N, maximum power fixed 180 W, `PrismaticTargetSelection.Directional`, `PrismaticInactiveResponse.SelfLock`, positive command node `forward`, negative `return`.

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `travel` | Real | Metre | 3 | 1 | 6 | Reject | BuildOnly |
| `speed` | Real | MetrePerSecond | 1.5 | 1 | 3 | Reject | BuildOnly |

**Ports.** All on body 0, on the dock A post. The three non-standard `PortId`s are minted by the package as the pusher mints its own (`LinearPusherElementDeclaration.cs:45`–`:48`).

| `PortId` | Role | Domain | Direction | Glyph | Meaning |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | PowerIn | Electrical | Input | Power | motor and interlock supply |
| `ForwardIn` | FirstIn | Electrical | Input | Extend | command toward dock B while supplied |
| `ReturnIn` | SecondIn | Electrical | Input | Retract | command toward dock A while supplied |
| `DockAOut` | Supply | Electrical | Output | Retracted | supplied while powered and the deck is at its lower limit |
| `DockBOut` | Supply | Electrical | Output | Extended | supplied while powered and the deck is at its upper limit |

Positions, all with normal −X, sockets tangent to the dock A post's −X face: `PowerIn` (−1.145, 0.05, 0), `ForwardIn` (−1.145, 0.05, −0.4), `ReturnIn` (−1.145, 0.05, 0.4), `DockAOut` (−1.145, 0.40, −0.4), `DockBOut` (−1.145, 0.40, 0.4). Commands sit low and dock outputs high, as the pusher separates its inputs from its end-switch outputs (`LinearPusherElementDeclaration.cs:43`–`:47`).

**Supply.** The pusher's graph unchanged (`LinearPusherElementDeclaration.cs:52`–`:65`): a powered bus, a motor consumer targeting Prismatic 0, the two command consumers, and two `SupplyContactDescriptor`s with `PrismaticContactControl` Lower and Upper bound to `DockAOut` and `DockBOut`. Electrical nodes: motor consumer 12 V, 24 Ω, 400 W; distribution 12 V, 0.05 Ω, 400 W. No stores, no signal nodes, no events.

**Presentation.** `PickRadius` 1.6. Body binding `Visual/Deck` → body 1. Parameter bindings: `Visual/Rail_0` and `Visual/Rail_1` each `ScaleX` (1, 1.6) and `LocalX` (0.5, 0); `Visual/DockB` `LocalX` (1, 0.95). `Visual/Indicator` albedo driven by `PresentationDriver.CommittedPrismaticPhase` with the pusher's held, moving and blocked colours (`LinearPusherElementDeclaration.cs:67`–`:76`).

**Palette.** Category `ElementCategory.Power`, palette order 313 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Docking ferry", colour `#66b8c9` (the supplied-actuator tone of the Linear pusher and Motor, `DESIGN.md:239`).

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Visual/Rail_0`, `Visual/Rail_1` | boxes (t + 1.6) × 0.12 × 0.12 at z ±0.56 | solid, rail colliders |
| `Visual/DockA` | box 0.3 × 0.7 × 1.4 | solid |
| `Visual/DockB` | box 0.4 × 0.44 × 1.4 | solid |
| five `Visual/Socket_*` | spheres r 0.075 | solid |
| `Visual/Deck` with its three lips | as body 1 | solid on the moving body |
| `Visual/Indicator` | disc on the dock A post top, flush | decorative: printed flat; colour moved by a binding |
| `Visual/DockMarks` | "A" and "B" chevrons on the post and apron tops, flush | decorative: printed flat |
| wheels | none drawn | the earlier cosmetic wheels are dropped: a proud wheel needs a collider |

Colours (`DESIGN.md:80`, `:239`): deck Cream `#fff8e9` (part-body cream, owner decision 4), lips Ochre `#d99e32`, rail and docks Navy `#1c2836`, chevrons Gold `#f7cb52`, indicator as bound. Icon: a side view of a tray with a back lip on a rail between a tall post and a low apron, a ball aboard, an arrow toward the apron. At a glance it differs from the Linear pusher (a housing with a rod and round head) by its flat three-lipped deck on a long rail with two end blocks.

### Behaviour and numbers

- **Laws.** One prismatic coordinate x ∈ [0, `travel`] m. While `PowerIn` is supplied, exactly one of the two commands moves the deck toward that end on the stopping profile (speed ≤ `speed`, |acceleration| ≤ 2 m/s², force ≤ 60 N, power ≤ 180 W); neither command, both, or lost power lock it where it is. Cargo is carried only by contact with the deck and lips. There is no cargo joint and no attachment.
- **Dock outputs.** Each is a contact that is closed while the deck is within 0.5 mm of that limit (`PhysicalTolerance.ActuatorStopSlopMetres`) and passes the bus supply. It is read from the committed pose at the start of the electrical phase, so a consumer is supplied on the tick after arrival (`CuriousContraptions.tests/LinearPusherTests.cs:52`). The outputs inform a loader. The requirement outcome follows from the law above: an output, a command or a gate never supports anything, so cargo released where the deck is not falls.
- **Why supplied, not rope-pulled.** The harvest-time draft pulled a passive carriage by ropes from both ends. That needs a two-ended rope role that does not exist (`RopeAttachmentRole { None, Anchor, Load, Guide }`, `CuriousContraptions.Abstractions/PortDescriptor.cs:74`) and gives no dock outputs without a second new capability. The supplied slider is delivered and tested, and the campaign places the ferry in the supplied-actuator chapter beside the pusher and the lift (`docs/planning/invest/campaign-reservations.md:118`).
- **One-way deck.** The deck is closed at the back and sides and open toward B. Leaving A, cargo lags against the back lip and is pushed. When the deck stops at B, cargo keeps its speed and rolls off the open end onto the apron, whose top is level with the deck. Transfer therefore happens by docking, with no second actuator. A deck stopped between docks sheds rolling cargo off the open end onto nothing: stopping short is a real failure.
- **Lip height 0.15 m against acceleration.** A ball of radius r climbs a step of height h only above g·tan(acos((r − h)/r)): 14.5 m/s² for the Basketball (r 0.34) and 22 m/s² for the Tennis ball (r 0.25). The fixed 2 m/s² leaves a factor of seven.
- **Speed 1–3 m/s.** The floor of 1 m/s is there because unloading uses the cargo's own momentum against its rolling loss over the 1.4 m deck (Basketball rolling resistance 0.035, `elements/Ball/BallElementDeclaration.cs:38`; Bowling ball 0.03, `elements/BowlingBall/BowlingBallElementDeclaration.cs:40`). Hand estimate at 1 m/s for the Basketball: pushed by the lip it has no spin; while the deck stops at 2 m/s² it gains 0.25 m on the deck, then slides into rolling at 5/7 of its speed (0.71 m/s) and loses 0.245 m/s² to rolling resistance over the remaining 0.73 m, leaving the edge at about 0.39 m/s. That margin is thin, and the audit expects a floor nearer 1.5 m/s. Row F8 proves each delivered ball at the floor.
- **Force and power.** Deck plus a Bowling ball is 6 kg; 2 m/s² needs 12 N, so 60 N leaves margin for a 16 kg load and stalls against a Wall. 180 W is 60 N at the 3 m/s ceiling.
- **Travel 1–6 m.** 1 m is the shortest crossing that separates the docks by more than a ball; 6 m keeps the part under the Wall's 8 m maximum (`elements/Wall/WallElementDeclaration.cs:28`). The pusher's tested stroke ends at 3 m, so 3–6 m is an untested range (section 3).
- **Holding.** `SelfLock` is the carrier's brake on power loss (`LinearPusherTests.cs:104`). The separate "carrier mode" clamp once proposed for EL-055 is not needed here.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test or owner |
| --- | --- | --- | --- |
| Prismatic slider between declared limits | *present* | `PrismaticSliderDescriptor` `MechanicalContractValues.cs:104`; `ISliderElement` `MechanicalContracts.cs:15` | `CuriousContraptions.tests/SliderInstanceTests.cs:61` |
| Supplied directional drive with speed, acceleration, effort and power bounds | *present* | `PrismaticDriveDescriptor` `MechanicalContractValues.cs:89`; `PrismaticTargetSelection.Directional` `:84` | `CuriousContraptions.tests/DirectionalPrismaticTests.cs:19`, `:53`, `:93`; `LinearPusherTests.cs:52` |
| Self-lock when unpowered or uncommanded | *present* | `PrismaticInactiveResponse.SelfLock` `MechanicalContractValues.cs:85` | `LinearPusherTests.cs:104`, `:38` |
| Endpoint contacts as supplied outputs, with next-tick causality | *present* | `PrismaticContactControl` `CuriousContraptions.Abstractions/SignalContactControl.cs:13` | `LinearPusherTests.cs:65`, `:52` |
| Obstruction stops the body and reports Blocked | *present* | `PrismaticDrivePhase.Blocked` `MechanicalContractValues.cs:87` | `LinearPusherTests.cs:125` |
| Limit and target bound to a parameter | *present* up to 3 m | `MechanicalValue.FromParameter` `MechanicalContractValues.cs:171`; travel rule `CuriousContraptions.Bundle/MechanicalValueDomain.cs:62` (no upper cap) | `LinearPusherTests.cs:228` (0.25 and 3 m) |
| The same over 3–6 m of travel | *present, untested* | as above | none; first exercised by row F10 |
| Moving slider body with several box colliders | *present* | `SliderBodyDescriptor` `MechanicalContractValues.cs:75` | `CuriousContraptions.tests/DirectionalCompositionTests.cs:122` (two-box moving bodies) |
| A moving body pushes cargo, and lifts cargo resting on it | *present* | generic contact | `LinearPusherTests.cs:307`, `:322` |
| Cargo resting or rolling on top of a body that moves sideways beneath it (support and friction from a moving deck) | *present, untested* | generic contact | none found (searched test names for carry, ride, deck, platform); first exercised by rows F1 and F8. If it fails, an engine repair slice owns it |
| Parameter-bound colliders on the static base of a slider package | *present, untested* | `ColliderParameterBinding` `SceneGeometry.cs:27`; delivered only on packages without joints (Wall, Pipe, Ramp, Weight) | none; first exercised by row F13 |
| Art position and scale bound to a parameter; moving art follows its body | *present* | `PresentationParameterBinding` `CuriousContraptions.Abstractions/ElementPresentation.cs:119`; `PresentationBodyBinding` `:114` | `CuriousContraptions.tests/WorkshopPipeLengthTests.cs:139`; `LinearPusherTests.cs:17` |
| Phase indicator colour | *present* | `PresentationDriver.CommittedPrismaticPhase` `ElementPresentation.cs:31` | `LinearPusherTests.cs:17` |
| Run/Reset and save/load with commands and cargo | *present* | generic | `LinearPusherTests.cs:148`, `:333`; `CuriousContraptions.tests/CatalogueIntegrationTests.cs:273` |

Counts: 11 present, 3 present-untested, 0 narrower, 0 missing. No engine story of its own is required beyond the engine programme (owner decision 3a). Nothing needs element-specific engine code.

**What carrying cargo on the moving deck relies on** (the coordinator's question): the back lip's contact push (tested shape), the deck's support of a body above it while the deck translates (untested), and friction for box cargo (untested); not on any attachment, residence sensor or guide.

**Element dependencies.** Battery, Powered gate, balls, Wall: all delivered.

### Engine conformance

- **Declaration data only.** One slider with a supplied directional position drive, the pusher's supply graph and two endpoint contacts. No cargo joint, carry rule, callback or engine branch.
- **Generic laws.** Crossing is the prismatic drive's stopping profile with declared bounds; holding is self-lock; dock outputs are contact controls reading the committed coordinate; carrying and unloading are contact, friction and momentum.
- **Layers.** Colliders: 4 static boxes (2 rails, dock A post, dock B apron), 5 socket spheres, and 4 boxes on the deck (deck, back lip, 2 side lips). Visuals: the art table, none used as a collider. Animation: `Visual/Deck` follows a body pose; the indicator colour is a presentation binding reading the committed drive phase; rails and apron follow `travel` through Build-time parameter bindings; posts, sockets and chevrons are moved by nothing during a Run. Visible = solid: rails, both docks, sockets, deck and lips each have colliders; the indicator and chevrons are printed flat, and no wheels are drawn.
- **One-way flow and typing.** Bindings write nothing back. Ports, roles, domains and the drive phase are enums; parameters are typed f32.
- **Flat state.** 2 bodies, 13 colliders (8 boxes, 5 spheres), 1 prismatic joint, 1 drive, 6 supply nodes, 2 electrical nodes, 0 sensors, 0 stores, 1 body binding, 3 albedo bindings and 5 presentation parameter bindings.
- **Performance.** Thirteen primitives against the Linear pusher's comparable set; each box blocks or carries. The base is static. No island sleeping exists today; a resting deck costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows F1 to F14 run the production simulation through placed parts and wires.

## 4. Sources and legacy

- Requirements row: "Undocked loading interlock does not bypass actual support." No variants.
- todo-320 integration: "cargo can transfer only when the carrier is actually docked".
- todo-429 docking lift (separate P2 row): "Cargo stays physically supported through travel … test edge cargo, acceleration, blocked travel, power loss and unloading between docks" ([requirements.md#todo-429](../requirements.md#todo-429)) — the same support rule, recorded for consistency.

No legacy implementation. Shared rope facts (route completeness, slack carries no tension, branch rejection) are harvested in [CAT-058](CAT-058-rope_anchor.md) R4–R17 and apply unchanged:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Loads and anchors accept one rope end; guides two; branching rejected. | `engine/RopeNetwork.cs@a6c914e:L69-L78` | carry forward; extend with a two-end carriage role | A ferry needs a rope at each end. |
| 2 | Rope supplies tension only; inactive while slack. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward (law) | Slack rope cannot push the ferry. |

**Files harvested:** `engine/RopeNetwork.cs`, `engine/physics/PhysicsRopeJoint.cs`, `parts/WeightPart.cs`. Searched with no hit: `parts/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: ferry, dock, carriage, shuttle).

## 5. Acceptance outline

Requirement row: [element-062](../requirements.md#element-062).

- **Chrome UI recipe (pending, post-catalogue).** Place a Docking ferry, three Batteries and a Powered gate in a short Pipe above dock A holding a Basketball; wire one Battery → `PowerIn`, one → `ForwardIn`, one → `ReturnIn` (disabled), and `DockAOut` → gate `PowerIn`. Verify placement, `travel`, `speed` and wires. Run: the gate opens, the ball drops onto the deck, the ferry crosses and the ball rolls onto the dock B apron; a second ball released over dock A while the ferry is away falls past the rail.

Planned tests are in `DockingFerryTests`. The rig is the pusher's (`LinearPusherTests` `Machine(power, positive, negative)`): separate run-controlled Batteries on `PowerIn`, `ForwardIn` and `ReturnIn`. "At B" means coordinate ≥ `travel` − 0.5 mm. Stall guards are `travel`/`speed` plus the stated margin, in game seconds.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| F1 | A Basketball resting on the deck at A; forward commanded. Then the deck reaches B, and the ball crosses the deck's open end still at deck height and comes to be supported on the apron or beyond it | `TheFerryCarriesABallToTheFarDockAndUnloadsIt` | no forward command: deck and ball stay at A | — | ball centre passes x = `travel` + 0.7 no lower than 0.05 m under its resting height (it rolled off, it did not fall through); guard +10 s |
| F2 | Requirement outcome: the ferry at B; a ball released above dock A's deck position, on the centre line. Then it falls between the two rails and is not supported at the dock | `ABallReleasedAtAnEmptyDockFalls` | the same release with the ferry at A: supported | released 0.25 m off the centre line it clips a rail edge and still falls through | centre more than 0.5 m below its on-deck resting height within 3 s, with the ferry mounted at least 1 m above the bench: a ball resting anywhere at the dock fails |
| F3 | Dock outputs follow the deck, not the command: `DockAOut` supplied only at A, `DockBOut` only at B, neither while between, none without `PowerIn` | `DockOutputsFollowTheDeckPosition` (pattern `LinearPusherTests.cs:65`) | each phase is the next one's control | deck 0.03 m short of B: not supplied | exact supplied flags per tick against the previous tick's coordinate |
| F4 | Interlock collaborator: `DockAOut` feeds a Powered gate holding a ball above dock A. With the ferry at A the ball is released onto the deck; with the ferry held at B the gate stays shut and the ball stays above it | `TheLoaderGateOpensOnlyWhileDocked` | wiring the gate straight to a Battery releases the ball with the ferry away, and it falls (F2) | — | exact supplied flag; ball above the gate for 5 s |
| F5 | Power lost mid-crossing. Then the deck locks, does no work, and `DockBOut` is never supplied; restoring power with `ReturnIn` brings it back to A | `LosingPowerLocksTheDeckBetweenDocks` (pattern `LinearPusherTests.cs:104`) | F1 | — | deck within 0.02 m of where it stopped, the pusher's lock tolerance; guard +10 s for the return |
| F6 | A Wall across the deck's path. Then the phase is Blocked and `DockBOut` is never supplied | `AnObstructionBlocksTheCrossing` (pattern `LinearPusherTests.cs:125`) | a Wall beside the path: the crossing completes | — | exact phase; observed 5 s |
| F7 | No command, or both commands, holds the deck without motor work | `InactiveOrConflictingCommandsHoldTheDeck` (pattern `LinearPusherTests.cs:38`) | F1 | — | exact phase; tick work exactly 0, an intentional zero |
| F8 | Each of Basketball, Bowling ball and Tennis ball unloads at `speed` 1 and at 3 | `EveryBallUnloadsAtBothSpeedLimits` | — | both speed limits | as F1 |
| F9 | At `speed` 3 no ball climbs the back lip: it is aboard when the deck reaches B | `TheBackLipKeepsCargoAboardAtTopSpeed` | — | top speed | ball centre ahead of the back lip's inner face at every tick of the crossing |
| F10 | `travel` 1 and 6, `speed` 1 and 3 each complete a crossing; values outside either range or non-finite are refused at every seam | `ParameterEndpointsCompleteACrossing` (pattern `LinearPusherTests.cs:228`); refusal: S4 row R2 once delivered, else `AnOutOfRangeParameterIsRefused` | bounds admitted | one f32 step outside | reaches B within guard +10 s; exact refusal |
| F11 | Wrong-kind connections refused: an activation wire into `ForwardIn`, a mechanical link into `PowerIn`, an activation wire from `DockBOut` | S4 row R5 once delivered, else `ConnectionsAcceptElectricalOnly` (pattern `MotorTests.cs:316`) | the rig's wires are admitted | — | exact refusal |
| F12 | Reset, and save then load, taken mid-crossing with cargo aboard restore deck, cargo, commands, parameters and wires, and a second Run repeats F1 | `ResetAndSaveLoadRestoreTheCrossing` (pattern `LinearPusherTests.cs:148`, `:333`); placed alone: `CatalogueIntegrationTests.cs:273` | — | — | authored state as those tests compare it |
| F13 | Rail, apron and their art follow `travel` at 1, 3 and 6; the deck art follows its body | `DockingFerryArtTests.RailAndApronFollowTheTravel` | — | both limits | positions within 1 mm |
| F14 | A ferry yawed 90° carries along its turned axis | `ARotatedFerryCarriesAlongItsAxis` (pattern `LinearPusherTests.cs:307`) | — | — | as F1 |

## 6. Open questions

**Resolved**

1. Rope attachment role for a two-ended carrier? Not needed. Decided now: the ferry is a supplied slider (section 2), so it has no `Tie` ports and the missing two-end role is not raised. The map's TensionTransmission family is navigation: D14.
2. A shaft-drive alternative? No: D19 (no row or lesson uses one). A player who wants shaft-driven carriage has the Conveyor.
3. A boat on liquid in a later mode? No: not in the row (D19).
4. Dock outputs as activation occurrences or supplied contacts? Decided now: supplied electrical contacts, the delivered endpoint-contact idiom; a held state is what an interlock needs.
5. Four lips or three? Decided now: three, open toward B, so cargo transfers by docking without a second actuator.
6. Extra `acceleration` and `force` parameters as on the pusher? No: D13 (fixed per kind); the fixed values protect the lip margin.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Battery, Powered gate, Pipe, electrical gates and timers for command logic, balls, Wall (all delivered). Port kinds agree: a `Supply` (Electrical, Output) into `PowerIn`, `ForwardIn` or `ReturnIn` (Electrical, Input); `DockAOut` and `DockBOut` (Electrical, Output, Supply) into a `PowerIn`. Wiring `DockBOut` to `ReturnIn` and `DockAOut` to `ForwardIn` through delivered logic makes a shuttle; that is a level design, not an acceptance row.
- **Shared contract with EL-061.** Both carriers expose "cargo may transfer" as supplied electrical outputs fed from their own `PowerIn`.
- **EL-220 Docking lift.** Same declaration shape turned vertical, with dock outputs and `SelfLock` as its holding brake. Planning the two consecutively reuses this probe. They stay separate identities and packages: the lift's deck must hold cargo against gravity through travel and has its own row.
- **Carrier task** [sequence-task-371](../requirements.md#sequence-task-371): "cargo can transfer only when the carrier is actually docked" is rows F1–F4.
- **Campaign.** First use levels 41–50, reuse 61–100, 111–120 and 136–150 (`docs/planning/invest/campaign-reservations.md:129`).
- **S8.** A lesson that places the ferry needs the lesson-fixture story S8 first.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `docking_ferry` |
| Named identity | EL-062, `../invest/named-elements.md#element-062` |
| Acceptance | `todo-320` and `sequence-task-615` |
| Decision | S372; `scope-corrections.md` has no `s372` block |
| Mode records | `Configuration=Adjustable` (it declares parameters; confirmed at slice planning) |

What it lacks today: an `s372` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-320 anchor is a scope index, not immediately followed by its sequence-task-615 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element; declaration data, scene, icon and tests.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 optional. No engine story of its own.
- **Files added.** `elements/DockingFerry/DockingFerry.csproj`, `DockingFerryElementDeclaration.cs`, `docking_ferry.tscn`, `icon.svg`; `CuriousContraptions.tests/DockingFerryTests.cs`, `DockingFerryArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule); until S3 `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` and `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483`, `:641` (generic list `:653`); at delivery the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Engine tier (all six layers) because of the three *present, untested* rows, although no engine change is planned.
- **Disqualifiers the probe must re-check.** A ball on the translating deck jitters, creeps through the deck or is left behind without touching the lip (F1): stop and raise an engine repair slice; never add a cargo joint or a carry rule. A slider package that also implements `IParameterizedGeometryElement` is refused or miscompiled (F13): repair generically or fix `travel` per kind under D13. Travel above 3 m misbehaves (F10): lower the maximum to the proven value rather than widen a tolerance. A ball fails to unload at 1 m/s (F8): raise the speed floor.
