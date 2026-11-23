# EL-041 · Pneumatic directional valve — element readiness spec

**Current delivery (24 October 2026):** [EL-041 delivery slice](../invest/named-elements.md#element-041). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against `8d1d68aa`. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations of the form `path:line` are this checkout. Values marked *proposed* are starting values (decision D1). Box sizes are full extents. Shared sealed-gas facts P1–P20, the family contract F1–F11 and the capability rows G1–G14 are in [EL-039](EL-039-air-reservoir.md). Repaired on 22 November 2026 against the Batch H audit.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-041 · Pneumatic directional valve |
| Type | Pneumatic |
| Anchor | [requirements.md#element-041](../requirements.md#element-041); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-041); owner S476 |
| Related | Refines no CAT spec. Drives the double-acting piston EL-231 ([todo-427](../requirements.md#todo-427), two typed chamber ports); fed by [EL-039](EL-039-air-reservoir.md) through [EL-038](EL-038-pneumatic-hose.md); commanded like the [CAT-037 Latch](../requirements.md#current-cat-037) Set/Reset pair. |
| Roadmap story | [EL-041 delivery slice](../invest/named-elements.md#element-041); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` (the memory node's capability name, finding E1, and gas paths as declared lists with local ids); then `ENGINE-GAS-NETWORK` with routed paths (owner decision 18). |
| Disposition | To deliver as a single element. Not a variant of [EL-040](EL-040-pneumatic-release-valve.md): it has two outlets, two command inputs and an exhaust socket that can be vented, piped or blocked. It is the valve that delivers the exhaust-return, repeated-stroke and exhaust-to-whistle criteria of [todo-426](../requirements.md#todo-426). |
| Planned package | id `directional_valve`, directory `elements/DirectionalValve/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |

## 2. Declaration

Closest delivered shape: the State latch, for `SetIn` and `ResetIn` pulse ports on one memory node (`elements/StateLatch/StateLatchElementDeclaration.cs:35`–`:36`, `:57`–`:62`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), and the fluid port and switched-path contract of `ENGINE-GAS-NETWORK` (G5, G8).
- **Bodies and colliders.** One static scene body, mass 0. Valve body box 1.2 × 0.6 × 0.5 m at the origin *proposed* (wide enough for three gas sockets 0.4 m apart); foot box 1.3 × 0.12 × 0.6 m at (0, −0.36, 0) *proposed*.
- **Material.** Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.
- **Signal.** One memory node `route`: On is RouteA, Off is RouteB. `SetIn` binds Set and `ResetIn` binds Reset. Set and Reset in one tick resolve to Off, RouteB (the delivered rule, harvest D2). The Run starts at RouteB, so a double-acting piston starts retracted. Event: `MemoryChanged` on `route`.
- **Gas paths.** RouteA joins `GasIn` → `PortA` and `PortB` → `Exhaust`. RouteB joins `GasIn` → `PortB` and `PortA` → `Exhaust`. Each open path has throat area `opening`. `Exhaust` is a Fluid Output socket. With `exhaust` Vent and no hose joined it is the ambient boundary (F4); with a hose joined it feeds that line, for example a whistle's `GasIn`; with `exhaust` Blocked both exhaust paths are closed and the socket is unavailable. One supply path is open at a time; there is no position in which both ports are fed.
- **Stores.** None.

| Parameter | Type | Unit | Default | Minimum | Maximum | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `opening` | Real f32 | m² | 1 × 10⁻⁴ | 2 × 10⁻⁵ | 2 × 10⁻⁴ | Reject; build only; player-editable unless the level locks it (D11) |
| `exhaust` | Choice | none | 0 (Vent) | 0 | 1 (Blocked) | Reject; build only; options "Open exhaust", "Blocked exhaust" |

| `PortId` | Role | Domain | Direction | Local position, normal |
| --- | --- | --- | --- | --- |
| `GasIn` | Bidirectional (fluid port, L5) | Fluid | Input (link-joined) | (0, −0.15, 0.3), +Z |
| `PortA` | Bidirectional (fluid port, L5) | Fluid | Output (link-joined) | (−0.4, 0.15, 0.3), +Z |
| `PortB` | Bidirectional (fluid port, L5) | Fluid | Output (link-joined) | (0.4, 0.15, 0.3), +Z |
| `Exhaust` | Bidirectional (fluid port, L5) | Fluid | Output (link-joined) | (0, 0.15, −0.25), −Z; available when `exhaust` is Vent |
| `SetIn` | `PortRole.SetIn`, glyph Set | Signal, Pulse | Input | (−0.45, 0.36, 0), +Y |
| `ResetIn` | `PortRole.ResetIn`, glyph Reset | Signal, Pulse | Input | (0.45, 0.36, 0), +Y |

The four gas ports are fluid ports of the one record ([EL-039](EL-039-air-reservoir.md) F3; liquid L5): role Bidirectional, so flow follows pressure, and Input or Output is only each link-joined socket's authoring end. `GasIn`, `PortA`, `PortB` and `Exhaust` are new `PortId` members (`ENGINE-GAS-NETWORK`); no `PortRole` member is added for gas. A port declares no aperture area: a hose edge's throat is the connection kind's (EL-039 F10), and `opening` is the throat of the valve's own declared internal paths.

- **Presentation bindings.** `Visual/Spool`, `LocalX`, driver `CommittedActivation` on `route`, −0.3 m (RouteB) to +0.3 m (RouteA) over 0.1 s *proposed*. `Visual/ArrowA` and `Visual/ArrowB`, emission, the same driver: the arrow of the fed port is lit.
- **Palette.** `ElementCategory.Control`, palette order 422 (F8; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cyan `#66b8c9`. Title "Two-way valve". Description: "Set sends air to the one-mark port, Reset to the two-mark port; the other port exhausts through the rear socket. Only one side is ever fed."

**Modes** (row outcome: "Blocked exhaust changes motion"):

| `exhaust` | Behaviour |
| --- | --- |
| Vent | The unfed port discharges through `Exhaust`: to ambient when nothing is joined, or into the hose joined to it. A double-acting piston moves freely toward the fed side; a spring-return piston on `PortA` extends on Set and returns on Reset. |
| Blocked | The unfed port's gas is trapped and compresses as the piston moves (P3, P8): the stroke slows and can stall; with equal pressures on both chambers the piston holds still (P10). |

### Art and solidity

| Node | Mesh and size | Class | Reason |
| --- | --- | --- | --- |
| `Visual/Body` | Box 1.2 × 0.6 × 0.5 m, cyan `#66b8c9` | solid | its own box collider |
| `Visual/Foot` | Box 1.3 × 0.12 × 0.6 m, navy `#293954` | solid | its own box collider |
| `Visual/Face` | Cream `#fff8e9` panel, flush on the front | decorative | printed flat |
| `Visual/Spool` | Navy bar in a recessed window | decorative | moved by a presentation binding |
| `Visual/MarkA`, `Visual/MarkB` | One navy mark and two navy marks, drawn flush (the earlier raised marks are redrawn flush) | decorative | printed flat |
| `Visual/ArrowA`, `Visual/ArrowB` | Flush arrows | decorative | printed flat, glow |
| `Visual/SetBar`, `Visual/ResetRing` | Flush bar and hollow ring beside their sockets (harvest D3) | decorative | printed flat |
| `Visual/ExhaustMouth` | Navy ring on the rear face around `Loc_Port_Exhaust`, flush; drawn solid navy when `exhaust` is Blocked | decorative | printed flat |
| Six `Loc_Port_*` nodes | Locators | none | not rendered |

Icon: a wide box with a spool bar and one-mark and two-mark arrows. It differs from the release valve (a small box with one round flap) by its width, its spool and its two marked outlets.

### Behaviour and numbers

- **Law.** Nozzle-law flow (P6) through each open path. Commands carry no gas (F7).
- **Why these numbers.** `opening` matches [EL-040](EL-040-pneumatic-release-valve.md), so one tank drives several strokes at the same pace.
- **Check values** (line start state F9; default 400 000 Pa tank on `GasIn`; a 0.002 m³ ambient tank on each of `PortA` and `PortB`; hoses as [EL-038](EL-038-pneumatic-hose.md)). The supply hose starts at 400 000 Pa and the two outlet hoses at ambient. At Run start, RouteB: the supply and tank B settle at 344 000 Pa, (4160 + 243.18) J over 0.0128 m³, and tank A stays at ambient. After Set, RouteA: the supply and tank A settle at 298 500 Pa, (3577.6 + 243.18) J over 0.0128 m³, and tank B falls to ambient (Vent, nothing joined) or keeps 344 000 Pa (Blocked). With a hose from `Exhaust` to a third 0.002 m³ ambient tank, tank B and that tank settle together at 222 700 Pa and no gas leaves the network. Each switch costs supply pressure.
- **Spring-return use ([todo-426](../requirements.md#todo-426)).** Tank → `GasIn`; `PortA` → the piston chamber; `PortB` unconnected and so Sealed (F4). Set feeds the chamber and extends the rod. Reset joins the chamber to `Exhaust`: the chamber vents and the visible spring returns the rod. A hose from `Exhaust` to an [EL-045](EL-045-air-whistle.md) `GasIn` makes each return stroke sound the whistle. Alternating Set and Reset gives repeated strokes, each spending supply pressure.
- **Tick order (F5).** A pulse emitted in tick N is routed in tick N and `route` is committed at that tick's boundary; the paths of tick N + 1 use the new route.
- **Consistency.** EL-231's two chamber ports are Fluid inputs; the delivered glyph set already has Extend and Retract (`CuriousContraptions.Abstractions/PortDescriptor.cs:38`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S476): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction). Under D14 the row is navigation; this probe (21 November 2026, `8d1d68aa`) is the authority. Evidence for G rows is in EL-039 section 3.

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with baked colliders (G1) | present | EL-039 G1 |
| Memory node with Set and Reset pulse ports, repeatable within a Run, Reset wins a same-tick pair | present | `SignalMemoryDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:64`); `StateLatchTests.Network_SetHoldsResetClearsAndSimultaneousRequestsResolveOff` (`CuriousContraptions.tests/StateLatchTests.cs:238`) and `Network_SameTickConflictResolvesOffWhicheverSourceIsOrderedFirst` (`:320`) |
| Distinct Set and Reset sockets with their glyphs | present | `elements/StateLatch/StateLatchElementDeclaration.cs:35`; `StateLatchTests.Ports_DeclareDistinctSetAndResetInputsAndSwitchedSupply` (`CuriousContraptions.tests/StateLatchTests.cs:131`) |
| Presentation from a committed memory state | present | `PresentationDriver.CommittedActivation` (`CuriousContraptions.Abstractions/ElementPresentation.cs:22`); `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:415`) |
| Wrong-kind refusal; authored-state Reset and save/load (G12) | present | EL-039 G12 |
| Fluid port contract (G5) | present, untested | EL-039 G5 |
| Gas connection kind and the gas `PortId` members, including `PortA`, `PortB` and `Exhaust` (G6; role Bidirectional, no new `PortRole` member, EL-039 F3); lines (G7) | missing | `ENGINE-GAS-NETWORK` |
| Several paths selected by one memory node, each open in one state and closed in the other, ending at an outlet or at an exhaust port that vents when unconnected and that a choice can close (G8) | missing | `ENGINE-GAS-NETWORK`; its text covers one valve or vent commanded by a signal, not routing between outlets. Proposed text in `readiness/batch-h.md` section 4 |
| Unit for m² (G11) | missing | `ENGINE-GAS-NETWORK` |

Counts: 5 present, 1 present but untested, 0 narrower, 3 missing. Nothing here needs element-specific engine code. The harvest-era gaps (a repeatable two-state node, Set and Reset sockets) are closed by the delivered State latch. Bellows: no `elements/Bellows` package and no `spec-12-4-pneumatic-bellows.md` exist in this checkout.

**Dependencies.** EL-039, EL-038, two activation sources (delivered: Impact switch, System clock, State latch), and EL-231 for the motion criteria.

### Engine conformance (22 November 2026)

Checked against the owner's conformance instruction of 22 November 2026 and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One static body, two colliders, one memory node, four declared gas paths, six ports, two parameters and three presentation bindings. No script, callback or loop; the engine may not branch on `directional_valve`. The spool is art.
2. **Generic laws.** Each path is the one nozzle law through a declared throat, open or closed by the committed state of a declared memory node (the delivered latch rule: Reset wins a same-tick pair). "Only one side is fed" follows from two paths declared open in opposite states of one node, not from a valve rule. Blocked exhaust is a closed path; trapped gas then compresses by the general store law.
3. **Three layers.** *Colliders:* `col_` body box and foot box on body 0. *Visuals:* the art table above; body, foot, face, marks, bar, ring, mouth and locators are moved by nothing. *Animation:* `Spool` position and the two arrow glows from the committed memory state; read-only, off the physics thread. The spool pushes nothing, so it is not a body.
4. **One-way flow and typing.** Signal → memory node → path state → gas transfer inside the tick phases; committed state → animation → renderer. Port ids, roles, the route and the `exhaust` choice are enums; areas and pressures are f32.
5. **Flat-state counts per instance.** Bodies 1 (static); colliders 2 (boxes); joints 0; drives 0; sensors 0; domain nodes 1 (signal memory) plus 4 gas path rows; ports 6; stores 0; bindings 3; parameters 2.
6. **Performance fitness.** Two primitive colliders, fewer than the Battery's three. Static body, so it joins no island. Four path evaluations per gas step, so per 1/480 s substep (EL-039 F5), constant per instance; no spatial query. Nothing scales with the scene; path tables are sized at Run from the compiled declarations (design contract D5).
7. **Tests are scenarios.** D-A1 to D-A15 author constructions and assert committed pressures, masses, routes and refusals on the production simulation; the stand-in tanks prove the gas side without a piston.

## 4. Sources and legacy

- Requirement row [element-041](../requirements.md#element-041): "Routes supply and exhaust to selected actuator ports"; outcome "Blocked exhaust changes motion; opposite routes cannot both receive full independent supply". Integration task [todo-427](../requirements.md#todo-427): valve transitions, both chambers pressurised, leaks/depletion, blocked load, mid-stroke reversal.
- Named entry [element-041](../invest/named-elements.md#element-041), owner S476. Gas design: "Gas network node + controller input | selected branch | Routes one supply to one branch | Blade position ← committed state".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| D1 | Set/Reset sockets: `SetIn` at (−0.45, 0.72, 0) with command Set, `ResetIn` at (0.45, 0.72, 0) with command Reset; any other command rejects. | `parts/LatchPart.cs@a6c914e:L30-L31`, `parts/LatchPart.cs@a6c914e:L37-L44` | carry forward the typed command sockets (heights rescaled to this body) |
| D2 | The latch is reset-dominant and its commands settle at the next tick boundary, so every delivery in a tick participates. | `parts/LatchPart.cs@a6c914e:L8-L9` | carry forward |
| D3 | Raised bar and hollow ring distinguish Set and Reset without text or colour. | `parts/LatchPart.cs@a6c914e:L62-L62` | carry forward the shapes, drawn flush |
| D4 | Equal opposed chambers hold without spending either inventory; blocked gas compresses with work balanced. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L116-L133`, `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L67-L81` | carry forward (EL-039 P9, P10) |

- **Files harvested:** `parts/LatchPart.cs` (socket and dominance facts), `parts/ElectricalLogicPart.cs` (input-mark convention only); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-041](../requirements.md#element-041), [pneumatics profile](../invest/profiles.md#pneumatics), [todo-427](../requirements.md#todo-427). Tolerances and stall guards are EL-039 F6. Planned test class `DirectionalValveTests`. The rig is the check-value build in section 2: a supply tank and two small tanks standing in for the two chambers.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| D-A1 | Given the rig, when Run settles with no command, then the supply and tank B read 344 000 Pa and tank A reads ambient | `DirectionalValveTests.TheRunStartsOnRouteB` | — | — |
| D-A2 | Given D-A1, when a pulse reaches `SetIn`, then within 20 s the supply and tank A read 298 500 Pa and tank B reads ambient | `DirectionalValveTests.SetFeedsPortAAndExhaustsPortB` | no pulse: D-A1 holds | — |
| D-A3 | Requirement outcome: given D-A2, then from the tick after the pulse tank B's pressure never rises, and network gas mass never exceeds its start (only one side is fed) | `DirectionalValveTests.OppositeRoutesAreNeverBothFed` | — | Output → Output between `PortA` and `PortB` is refused |
| D-A4 | Requirement outcome, gas side: given `exhaust` Blocked, when Set arrives, then tank B keeps 344 000 Pa instead of falling to ambient | `DirectionalValveTests.ABlockedExhaustTrapsTheUnfedSide` | Vent: D-A2 | — |
| D-A5 | Requirement outcome, motion side (later partner EL-231): with Blocked exhaust the same command gives a slower or stalled stroke; with Vent the piston completes it | `DoubleActingPistonTests.ABlockedExhaustSlowsOrStallsTheStroke`, in the EL-231 slice | Vent | equal chamber pressures hold still (P10) |
| D-A6 | Given Set and Reset pulses in one tick, then the route is RouteB | `DirectionalValveTests.ASameTickSetAndResetSelectsRouteB` | Set alone selects RouteA | — |
| D-A7 | Given Set, Reset, Set on separate ticks, then the fed side alternates and the supply pressure is lower after each switch | `DirectionalValveTests.RepeatedSwitchingAlternatesAndSpendsTheSupply` | — | a supply at ambient moves nothing |
| D-A8 | Given a pulse in tick N, then no gas has been re-routed at the end of tick N and it has within 0.1 s | `DirectionalValveTests.TheRouteChangesTheTickAfterItsCommand` | — | the declared next-tick rule |
| D-A9 | Given a run that switched the valve, when Reset is pressed, then the route is RouteB and all three tanks hold their authored charge | `DirectionalValveTests.ResetRestoresRouteBAndAllTanks` | — | — |
| D-A10 | Given non-default `opening`, `exhaust` and all six links, when saved and loaded, then they are equal | `DirectionalValveTests.SaveLoadKeepsParametersAndSixLinks` | — | both `exhaust` values |
| D-A11 | Given `opening` outside its limits or non-finite, or an undefined `exhaust`, then it is refused with a typed reason | `DirectionalValveTests.InvalidParametersAreRefusedAtomically` | the limits are admitted | next f32 beyond each limit |
| D-A12 | Given a wire offered to `SetIn`, a signal link offered to a gas port, a hose offered to `ResetIn`, or a hose offered to `Exhaust` while `exhaust` is Blocked, then each is refused | `DirectionalValveTests.WrongKindAndUnavailablePortLinksAreRefused` | the correct links are admitted | — |
| D-A13 | Later partner ([todo-427](../requirements.md#todo-427)): a Clock alternates a sorting pusher between two chutes; mid-stroke reversal reverses without a jump | `DoubleActingPistonTests.AClockAlternatesThePusherBetweenTwoChutes`, in the EL-231 slice | no clock link | depletion, blocked load |
| D-A14 | Exhaust is a socket. Given the rig with a hose from `Exhaust` to a third 0.002 m³ ambient tank, when Set arrives after D-A1, then tank B and the third tank settle at 222 700 Pa and network gas mass is unchanged | `DirectionalValveTests.AnExhaustHoseCarriesTheUnfedSideToItsTarget` | with nothing joined to `Exhaust`, tank B falls to ambient and network mass falls (D-A2) | — |
| D-A15 | Later partner (EL-230 and EL-045, [todo-426](../requirements.md#todo-426)): with the spring-return build above, Reset vents the chamber and the spring returns the rod; alternating Set and Reset gives repeated strokes until the tank can no longer reach the load; with `Exhaust` hosed to a whistle, each return stroke sounds it | `SpringReturnPistonTests.ResetVentsTheChamberAndTheSpringReturnsTheRod`, `SpringReturnPistonTests.RepeatedStrokesDepleteTheTankUntilTheLoadIsNotReached`, in the EL-230 slice; the whistle clause, `SpringReturnPistonTests.TheExhaustSoundsAWhistleOnEachReturn`, is a cross-part row owned by the EL-045 slice (the whistle is taught at 77a, after this valve and the pistons at 74b) | no Reset: the rod stays out; no exhaust hose: no whistle | `exhaust` Blocked: the rod does not return |

Generic cover today: G12 for authored-state Reset and save/load; D-A9 to D-A12 stay package tests until the S4 conformance suite is delivered. D-A5, D-A13 and D-A15 keep the ledger row open until they run.

**Chrome recipe (pending the post-catalogue browser phase).** Charged Air tank → hose → valve `GasIn`; hoses `PortA → ExtendIn` and `PortB → RetractIn` of a double-acting piston; Switch A → `SetIn`, Switch B → `ResetIn` through Connect and the socket-choice buttons.

**Integrations.** [todo-427](../requirements.md#todo-427) under the pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393); interaction processes [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09) and [IX-07 signal propagation](../requirements.md#interaction-07).

## 6. Open questions

1. **Control kind.** Resolved (D19): activation Set and Reset, as proposed. A supplied solenoid with spring return is not added because no row or lesson names one.
2. **Blocked exhaust form.** Resolved (D19): the `exhaust` choice stays, and it closes both exhaust paths. The exhaust is now a socket (question 5), so a Sealed-end hose on it also traps gas, in the hose's own line volume; that is ordinary hose behaviour and not a second way to configure the valve.
3. **Spool transition.** Resolved (D19): instant at the tick boundary.
4. **Initial position.** Resolved (D19): the earlier `initial_position` parameter is withdrawn; a Run starts at RouteB. No row or lesson needs a RouteA start, and a Set pulse at Run start gives it.
5. **Exhaust socket.** Resolved by coordinator decision under the owner's standing delegation (22 November 2026; EL-039 decision 7): the valve gains an `Exhaust` Fluid Output that vents when unconnected. Reason: [todo-426](../requirements.md#todo-426) requires spring return on exhaust, repeated strokes and "exhaust → whistle"; the release valve latches open and has no exhaust socket, so without this port no part in the batch could deliver them and a requirement criterion would be weakened.

Needs the owner: none.

## 7. Integration

- **Works with.** EL-039, EL-038, EL-230 and EL-231 (not delivered); Impact switch, System clock, State latch and the logic gates as command sources (delivered); [EL-045 Air whistle](EL-045-air-whistle.md) may take `PortA`, `PortB` or `Exhaust`.
- **Port agreement.** `PortA`, `PortB` and `Exhaust` are Fluid Output sockets; the chamber ports of EL-230 (`GasIn`) and EL-231 (`ExtendIn`, `RetractIn`) are Fluid Input sockets, so a hose joins each (EL-039 F3). Every one of these ports has role Bidirectional; the authoring ends only fix which way a hose is drawn. EL-230's "the valve's exhaust" is this part's `Exhaust` socket. `SetIn` and `ResetIn` are Signal pulse inputs with the State latch's roles.
- **Campaign.** Named in the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage): first use 71–80, reuse 91–100 and 136–150. A level that places it needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7**, which also widens the reader for companion packages and non-`EL` identities (owner decision 15): the published form's preconditions are not met. This identity lacks its own `todo-NNN` and `sequence-task-NNN` pair co-anchored in `requirements.md` (the ledger entry links only the umbrella `todo-376`, which has no co-anchored sequence task and is shared by eight identities; the reader refuses two identities on one task source) and an `<a id="s476">` anchor for its ledger owner S476 in `docs/planning/invest/scope-corrections.md`.

## 8. Delivery

- **Slice shape.** Single element, delivered before EL-230 and EL-231; their slices then run D-A5, D-A13 and D-A15.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK` including routed paths; EL-039; EL-038 acceptance; S7.
- **Files.** Adds `elements/DirectionalValve/DirectionalValve.csproj`, `DirectionalValveElementDeclaration.cs`, `directional_valve.tscn`, `icon.svg` and `CuriousContraptions.tests/DirectionalValveTests.cs` (compiled by the test project's one rule; no compile line). Edits one line in `CuriousContraptions.slnx` and this spec; the per-package lists S3 owns until S3 lands.
- **Review tier.** Data-only, if the probe finds every missing row delivered.
- **Disqualifiers.** A memory node that can switch only one path; no exhaust port that vents when unconnected; no closable exhaust; a route change that needs a topology rebuild during Run. Each returns the work to `ENGINE-GAS-NETWORK`.
