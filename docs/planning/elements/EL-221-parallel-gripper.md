# EL-221 · Parallel gripper — named-identity spec

**Current delivery (21 November 2026):** [EL-221 ledger entry](../invest/named-elements.md#element-221), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-221 · Parallel gripper · Mechanical |
| Requirement anchor | [todo-430](../requirements.md#todo-430) (`sequence-task-373`) |
| Ledger entry / owner | [element-221](../invest/named-elements.md#element-221); slice `named-el-221`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a), `ENGINE-ASSEMBLY-JOINTS`, `ENGINE-SIGNAL-NODES` and `ENGINE-JOINT-KINDS`.** Delivery batches lists it in run "supplied sliders" (1b); this probe moves it to 1c because capabilities are missing (section 3). This is the variant that releases on power loss. |
| Planned package | id `parallel_gripper`, directory `elements/ParallelGripper/` |
| Related | [CAT-051 Powered gate](CAT-051-powered_gate.md) (sprung supplied slider), [EL-219 Rack-and-pinion slide](EL-219-rack-and-pinion-slide.md) (its carrier), [EL-220 Docking lift](EL-220-docking-lift.md), [EL-060 Electromagnet](EL-060-electromagnet.md) (the ferrous pickup) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (the drives' work paid from the Battery), `ENGINE-CAPABILITY-INSTANCES` (the `FirstIn` terminal, finding E2) and `ENGINE-ISLANDS-SLEEP` (a held ball rests with its carrier); then `ENGINE-ASSEMBLY-JOINTS` (the mounting socket), `ENGINE-SIGNAL-NODES` (the `HeldOut` contact; index C3) reading the Blocked observation that `ENGINE-JOINT-KINDS` publishes, and EL-219 and EL-220 |

## 2. Declaration

Shape: two of the Powered gate's sprung fixed-target sliders (`elements/PoweredGate/PoweredGateElementDeclaration.cs:76`) on one supply route.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), plus the mounting socket contract from `ENGINE-ASSEMBLY-JOINTS` (a player-made connection at a typed socket).
- **Palm (body 0).** Bar 1.2 × 0.15 × 0.4 m carrying the mounting socket on top.
- **Jaws (bodies 1 and 2).** Each a finger 0.08 × 0.6 × 0.4 m hanging from the palm with an inward lip 0.15 × 0.06 × 0.4 m at its foot; mass 0.3 kg each; friction 0.6.
- **Sliders.** Opposed along the palm, each with 0.3 m of travel: aperture 0.9 m open, 0.3 m closed. Spring 80 N/m and damping 6 N·s/m toward open. Drive: fixed target closed, maximum speed 1 m/s, stopping acceleration 10 m/s², effort `grip_force`, inactive response `Free`. Drive power is 2 × `grip_force` × 1 m/s: 80 W at the default, inside a default Battery's 120 W (`elements/Battery/BatteryElementDeclaration.cs:44`); 160 W at the maximum, which needs a Battery with `power_limit` set to 160 W or more. The Battery's limit binds the drives only once `ENGINE-SUPPLY-DEMAND` pays drive work from its source; drive supply is binary today (`_bmad-output/implementation-artifacts/deferred-work.md:211`).
- **Supply graph.** An Or node powered from `PowerIn` with `CloseIn` on its first input feeds one consumer that targets both sliders. `HeldOut` closes while the close command is on and both jaws are stopped short of their closed endpoints. The first input is today's fixed `FirstIn` terminal; `ENGINE-CAPABILITY-INSTANCES` replaces the two-input node with an N-input boolean node (finding E2), and `CloseIn` then takes that node's declared first input.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `grip_force` | Real, N | 30–80 | 40 | Reject, build-only | Above the spring's 24 N at full closure; the lips, not friction, carry the weight |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input, PowerIn | (−0.4, 0.1, 0.2) |
| `CloseIn` | Electrical | Input, FirstIn | (0, 0.1, 0.2) |
| `HeldOut` | Electrical | Output, Supply | (0.4, 0.1, 0.2) |

- **Presentation.** Jaw body bindings; indicator from `CommittedPrismaticPhase` (open slate, closing cyan, holding gold).
- **Palette.** Motion, "Parallel gripper"; order 139 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Palm | Box, cream `#fff8e9` (owner decision 4) | Solid, body 0 |
| Two fingers and lips | Boxes, sky blue `#7ebcd4`, gold lips | Solid, bodies 1 and 2 |
| Indicator | Flush disc | Decorative: printed flat |

Icon: two facing L-shaped jaws around a circle. It differs from the Electromagnet (one flat face) by the two jaws.

### Behaviour and numbers

- **Supported shapes:** spheres 0.4–0.8 m across, which covers every ball preset. The jaws close under the ball's lower half so the lips support it: holding is form closure by two-sided contact, not friction alone. One jaw alone cannot lift a ball.
- Empty closure: both jaws reach the closed endpoint and `HeldOut` stays off. Oversize (over 0.9 m): the jaws stall on top of it and cannot lift it. Off-centre: the first jaw pushes the ball until the second meets it. Two small objects: both may be enclosed; `HeldOut` reports held, and nothing is merged or deleted.
- Power loss, or `CloseIn` off: the drives are unsupplied, the springs open the jaws and the object falls.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Sprung fixed-target supplied slider that returns unsupplied | present | `PoweredGateElementDeclaration.cs:76` | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` |
| One supply route driving two sliders | present | `SupplyContractValues.cs:52` | `DeclaredPrismaticDriveTests.OneOwnerRouteDrivesBothChildrenWithinTheirOwnPhysicalBounds` |
| A drive stalling on an obstacle, Blocked | present | `MechanicalContractValues.cs:87` | `DeclaredPrismaticDriveTests.BlockedNeedsContactInteriorAndRestTogether` |
| Endpoint contacts | present | `SignalContactControl.cs:13` | `LinearPusherTests.BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval` |
| A ball held between two moving jaws while the assembly moves | present, untested | contact solver | none |
| **An output closed while a drive is Blocked** (`HeldOut`) | **missing** | `SupplyContactControl` has only signal-state and endpoint kinds (`SignalContactControl.cs:11`–`:13`); a contact-load sensor must sit on a static frame (`CuriousContraptions.Bundle/SignalBundleValidation.cs:190`) | Owner: `ENGINE-SIGNAL-NODES` (the contact; engine-stories-index C3), reading the window / Blocked observation that `ENGINE-JOINT-KINDS` publishes |
| **The gripper mounted on a moving carrier** | **missing** | a package's body 0 is static; no attachment socket contract | Owner: `ENGINE-ASSEMBLY-JOINTS` (player-made connections between separately placed parts at typed sockets) |
| **A supplied drive's committed work debited from its source within the source's power limit** (the Battery sizing in section 2) | **missing** | drive supply is binary (`_bmad-output/implementation-artifacts/deferred-work.md:211`) | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |

Counts: 4 present, 1 untested, 0 narrower, 3 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A palm, two opposed sprung sliders on one supplied consumer, one logic node, one output contact and a mounting socket. Closing is the fixed-target drive law at the declared `grip_force`; opening on power loss is the spring law with the drive's `Free` response; holding is form closure by rigid contact with the lips. The three missing capabilities are stated for any package: a mounting socket for a player-made connection, owned by `ENGINE-ASSEMBLY-JOINTS`; a supply contact controlled by a drive's committed Blocked phase, owned by `ENGINE-SIGNAL-NODES` (engine-stories-index C3), reading the Blocked observation that `ENGINE-JOINT-KINDS` publishes; and a drive's work paid from its source within the source's power limit, owned by `ENGINE-SUPPLY-DEMAND`. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 5 boxes (palm; 2 fingers, 2 lips) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: one indicator colour binding from the committed prismatic phase. What moves each art node: palm, body 0's pose (static until it is mounted on a carrier through the socket); fingers and lips, the poses of bodies 1 and 2; indicator, the colour binding only.
- **Flat state.** 3 bodies; 5 box colliders; 2 prismatic joints; 2 drives on 1 consumer; 0 sensors; 1 logic node; 1 contact; 1 socket; 0 stores; 2 body-pose and 1 driver binding.
- **Performance.** Fewer primitives than two Powered gates. Jaws move at 1 m/s at most. Whether a closed grip resting with its cargo and carrier may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-430](../requirements.md#todo-430): "Supplied opposing jaws, bounded aperture, close command and object-held output. Capture requires actual two-sided contact; define supported shapes. This variant releases on power loss."
- **Campaign.** Chapter 5 (41–50); coverage row "… electromagnet, gripper, lift, ferry …".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: jaw position ±0.02 m; "held" means the ball stays within 0.05 m of the palm frame for 5 s of carrier motion; object identity exact; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| R1 | Basketball centred under the open jaws; supply and `CloseIn` on; carrier raised 1 m | The ball rises with the gripper and `HeldOut` is on | Control: one jaw removed from contact by a spacer, the ball is not lifted | `ParallelGripperTests.TwoSidedContactHoldsAndLifts` |
| R2 | Closing on nothing | Both jaws reach closed; `HeldOut` off | — | `ParallelGripperTests.EmptyClosureReportsNotHeld` |
| R3 | A 1.0 m ball; a ball 0.25 m off centre | The oversize ball is not lifted; the off-centre ball is centred and held | Boundary: 0.4 m and 0.8 m balls are held | `ParallelGripperTests.OversizeIsRefusedAndOffCentreIsCentred` |
| R4 | Two Tennis balls side by side | Both stay physical bodies with their own identities; none is deleted | — | `ParallelGripperTests.TwoObjectsStayDistinct` |
| R5 | A Wall between the jaws | The jaws stall on it, no penetration | — | `ParallelGripperTests.AnObstructionStallsTheJaws` |
| R6 | Held ball carried into a Wall | The ball is stopped or knocked out physically; it is never dragged through | — | `ParallelGripperTests.CarryingCollisionsArePhysical` |
| R7 | Supply cut while holding; `CloseIn` off while holding | The jaws open and the ball falls into a Funnel below | Control: supply kept, the ball stays | `ParallelGripperTests.PowerLossReleases` |
| R8 | Lift docks → grip → rack carries → Hold timer → release over a Funnel | The ball ends in the Funnel | Control: Hold timer unwired, it is not released | `ParallelGripperTests.ALiftRackAndTimerDeliverABall` |
| R9 | Run, Reset; Save, Load | Jaws open, mount and wires restored | — | `ParallelGripperTests.ResetAndReloadRestoreTheAuthoredGripper` |
| R10 | Activation wire into `CloseIn`; `grip_force` 29, 81 | Refused with a typed reason | — | `ParallelGripperTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

**Integration.** Battery, Hold timer, Funnel (delivered); EL-220 Docking lift and EL-219 Rack slide (earlier in this batch's order); EL-060 Electromagnet for the ferrous contrast (later). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `parallel_gripper`; Named identity EL-221; Acceptance todo-430 and sequence-task-373; owner slice `named-el-221`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND`, `ENGINE-CAPABILITY-INSTANCES` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-ASSEMBLY-JOINTS` (the mounting socket), `ENGINE-SIGNAL-NODES` (the `HeldOut` contact) with the Blocked observation of `ENGINE-JOINT-KINDS`, EL-219 and EL-220. Other prerequisites: the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/ParallelGripper/` package, `CuriousContraptions.tests/ParallelGripperTests.cs`, and the hand-edited lists until S3. Review tier: engine. **Disqualified if** a ball cannot be held stably between lipped jaws on a moving carrier; then the grip needs an engine capability of its own and this spec is re-planned.

## 6. Open questions

1. *Friction grip or form closure?* Resolved under the standing delegation: form closure with lips. A friction-only grip of a sphere is unreliable in a game solver, and the requirement asks for real two-sided contact, which lips need as well.
2. *Which shapes?* Resolved: spheres 0.4–0.8 m. Boxes wait for EL-080 Programmable box.
3. *A declared spring rest position for the jaws?* Coordinator decision 25, 22 November 2026, was checked and does not apply: it covers spring-returned blades, hatches and gates that must close when unpowered, and these jaws spring open when unpowered.

Needs the owner: nothing.
