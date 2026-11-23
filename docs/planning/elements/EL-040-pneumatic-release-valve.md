# EL-040 · Pneumatic release valve — element readiness spec

**Current delivery (24 October 2026):** [EL-040 delivery slice](../invest/named-elements.md#element-040). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against `8d1d68aa`. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations of the form `path:line` are this checkout. Values marked *proposed* are starting values (decision D1). Box sizes are full extents. Shared sealed-gas facts P1–P20, the family contract F1–F11 and the capability rows G1–G14 are in [EL-039](EL-039-air-reservoir.md). Repaired on 22 November 2026 against the Batch H audit.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-040 · Pneumatic release valve |
| Type | Pneumatic |
| Anchor | [requirements.md#element-040](../requirements.md#element-040); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-040); owner S475 |
| Related | Refines no CAT spec. Fed by [EL-039](EL-039-air-reservoir.md) through [EL-038](EL-038-pneumatic-hose.md); feeds the spring-return piston EL-230 ([todo-426](../requirements.md#todo-426)), [EL-042 Air nozzle](EL-042-air-nozzle.md) or [EL-045 Air whistle](EL-045-air-whistle.md); commanded by any activation source ([CAT-063 Switch](CAT-063-switch.md), [CAT-022 Delay](CAT-022-delay.md), [CAT-017 Clock](CAT-017-clock.md)). |
| Roadmap story | [EL-040 delivery slice](../invest/named-elements.md#element-040); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` (the memory node's capability name, finding E1, and gas paths as declared lists with local ids); then `ENGINE-GAS-NETWORK` (owner decision 18). |
| Disposition | To deliver as a single element. Its two outlets (Output, Exhaust) are one `outlet` choice on one catalogue entry (D8), not two entries. |
| Planned package | id `release_valve`, directory `elements/ReleaseValve/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |

## 2. Declaration

Closest delivered shape: the Fan, for a choice parameter that switches port availability and an `ActivationIn` pulse that sets a memory node (`elements/Fan/FanElementDeclaration.cs:14`, `:57`–`:71`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), and the fluid port and valve-path contract that `ENGINE-GAS-NETWORK` delivers (G5, G8).
- **Bodies and colliders.** One static scene body `"."`, mass 0. Valve body box 0.9 × 0.6 × 0.5 m at the origin *proposed* (smaller than the 1.1 m Switch footprint so it fits between tank and piston); foot box 1.0 × 0.12 × 0.6 m at (0, −0.36, 0) *proposed*. Both colliders are baked from the scene.
- **Material.** Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, as `elements/Battery/BatteryElementDeclaration.cs:63`).
- **Constraints.** None; the flap is art.
- **Signal.** One memory node `open` (`SignalMemoryDescriptor`, `CuriousContraptions.Abstractions/SignalContractValues.cs:64`); `ActivationIn` binds its Set terminal. Nothing binds Reset, so the valve stays open until Reset of the Run. Event: `MemoryChanged` on `open`.
- **Gas path.** `GasIn` to the outlet through a throat of area `opening` while `open` is On; zero area while Off (P6 NoFlow). In Exhaust mode the outlet is the ambient boundary (F2). The valve has no store: it never makes gas.

| Parameter | Type | Unit | Default | Minimum | Maximum | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `opening` | Real f32 | m² | 1 × 10⁻⁴ | 2 × 10⁻⁵ | 2 × 10⁻⁴ | Reject; build only; player-editable unless the level locks it (D11) |
| `outlet` | Choice | none | 0 (Output) | 0 | 1 (Exhaust) | Reject; build only; options "To outlet", "To exhaust" |

| `PortId` | Role | Domain | Direction | Local position, normal | Available |
| --- | --- | --- | --- | --- | --- |
| `GasIn` | Bidirectional (fluid port, L5) | Fluid | Input (link-joined) | (−0.5, 0, 0), −X | always |
| `GasOut` | Bidirectional (fluid port, L5) | Fluid | Output (link-joined) | (0.5, 0, 0), +X | `outlet` = Output |
| `ActivationIn` | `PortRole.ActivationIn` | Signal, Pulse | Input | (0, 0.36, 0), +Y | always |

`GasIn` and `GasOut` are fluid ports of the one record ([EL-039](EL-039-air-reservoir.md) F3; liquid L5): role Bidirectional, so flow follows pressure, and Input or Output is only each link-joined socket's authoring end. They are new `PortId` members (`ENGINE-GAS-NETWORK`); no `PortRole` member is added for gas. A port declares no aperture area: a hose edge's throat is the connection kind's (EL-039 F10), and the valve's `opening` is the throat of its own declared internal path.

- **Presentation bindings.** `Visual/Flap`, `LocalRotationZ`, driver `CommittedActivation` on `open`, 0° to 75° over 0.08 s *proposed*. `Visual/Puff`, `Visible`, driven by committed outflow above zero in Exhaust mode (driver missing, G9). Bindings never alter flow.
- **Palette.** `ElementCategory.Control`, palette order 420 (F8; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cyan `#66b8c9`. Title "Release valve". Description: "A trigger opens the valve and lets stored air through. It never makes air: closed, it holds; open, it passes only what the tank has."

### Art and solidity

| Node | Mesh and size | Class | Reason |
| --- | --- | --- | --- |
| `Visual/Body` | Box 0.9 × 0.6 × 0.5 m, cyan `#66b8c9` | solid | its own box collider |
| `Visual/Foot` | Box 1.0 × 0.12 × 0.6 m, navy `#293954` | solid | its own box collider |
| `Visual/Flap` | Cream `#fff8e9` disc r 0.2 m on the front face | decorative | turned by a presentation binding |
| `Visual/ExhaustMouth` | Navy ring on the +X face, drawn flush, shown in Exhaust mode | decorative | printed flat |
| `Visual/SocketRing` | Gold `#e8b764` ring on the top face, drawn flush | decorative | printed flat |
| `Visual/Puff` | Soft cream sprite at the mouth | decorative | glow, shown by a binding |
| `Loc_Port_GasIn`, `Loc_Port_GasOut`, `Loc_Port_ActivationIn` | Locators | none | not rendered (`elements/Battery/battery.tscn:96` convention; the earlier port spheres are dropped) |

Icon: a small box with one round flap and one arrow through it. It differs from the directional valve (a wider box whose face carries a sliding spool and one-mark and two-mark ports) by its single round flap, not by colour.

### Behaviour and numbers

- **Law.** Converging-nozzle flow (P6) from the higher to the lower pressure side through `opening`. A command carries no gas (F7): with an empty upstream store, opening the valve moves nothing.
- **Why these numbers.** At the default `opening` a 400 000 Pa supply at the ambient temperature of F2 (288 K) is choked at about 0.095 kg/s (twice the 5 × 10⁻⁵ m² row in [EL-042](EL-042-air-nozzle.md)), so the default 0.0484 kg charge is gone in about a second: a visible burst. The minimum, 2 × 10⁻⁵ m², gives about 0.019 kg/s, a slow stroke over several seconds.
- **Tick order (F5).** A pulse emitted in tick N is routed in tick N and `open` is committed at that tick's boundary, the rule the supplied latch shows (`LatchSupplyFixtureTests.SetClosesRouteOneTickAfterStrikeWithoutTopologyChange`, `CuriousContraptions.tests/LatchSupplyFixtureTests.cs:145`). Flow starts in tick N + 1.
- **Consistency.** The standard rig settles at 250 700 Pa: (4160 + 1053.78) J over 0.0208 m³. The hose before the valve starts at 400 000 Pa and the hose after it at ambient (F9), so nothing moves until the trigger. The port ids and directions match F3 at both hose ends.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S475): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction). Under D14 the row is navigation; this probe (21 November 2026, `8d1d68aa`) is the authority.

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with baked colliders (G1) | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`); `FanIntegrationTests.PassiveHubReceivesExternalContactWithoutEmittingUnpaidFlow` (`CuriousContraptions.tests/FanIntegrationTests.cs:417`) |
| Memory node set by a pulse port, committed next tick | present | `SignalMemoryDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:64`); `LatchSupplyFixtureTests.SetClosesRouteOneTickAfterStrikeWithoutTopologyChange` (`CuriousContraptions.tests/LatchSupplyFixtureTests.cs:145`) |
| Port available only for one choice value | present | `PortDescriptor.Availability` as used at `elements/Fan/FanElementDeclaration.cs:57`; `FanAdmissionTests.InactiveConnectionAndRetainedWireModeEditPreserveInstallation` (`CuriousContraptions.tests/FanAdmissionTests.cs:191`) |
| Presentation from a committed memory state | present | `PresentationDriver.CommittedActivation` (`CuriousContraptions.Abstractions/ElementPresentation.cs:22`); `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:415`) |
| Wrong-kind refusal; authored-state Reset and save/load (G12) | present | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`); `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| Fluid port contract (G5) | present, untested | `IFluidElement` (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`); no test, no solver |
| Gas connection kind and the gas `PortId` members (G6; role Bidirectional, no new `PortRole` member, EL-039 F3) | missing | `ENGINE-GAS-NETWORK` |
| Lines with conserved transfer (G7) | missing | `ENGINE-GAS-NETWORK` |
| A path whose throat area is switched by a memory node, to an outlet or to the ambient boundary (G8) | missing | `ENGINE-GAS-NETWORK` ("valves and vents commanded by a signal") |
| Presentation from committed outflow (G9) | missing | `ENGINE-GAS-NETWORK` |
| Unit for m² (G11) | missing | `ENGINE-GAS-NETWORK` |

Counts: 5 present, 1 present but untested, 0 narrower, 5 missing. Nothing here needs element-specific engine code. Bellows: no `elements/Bellows` package and no `spec-12-4-pneumatic-bellows.md` exist in this checkout.

**Dependencies.** A charged EL-039, the EL-038 hose, an activation source, and a consumer to show the release.

### Engine conformance (22 November 2026)

Checked against the owner's conformance instruction of 22 November 2026 and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One static body, two colliders, one memory node, one declared gas path, three ports, two parameters and two presentation bindings. No script, callback or loop; the engine may not branch on `release_valve`. The flap is art.
2. **Generic laws.** The path is the one nozzle law through a declared throat, open or closed by the committed state of a declared memory node. "A command makes no pressure" follows from the law (no pressure difference, no flow), not from a valve rule. Exhaust mode is the same path ending at the declared ambient boundary.
3. **Three layers.** *Colliders:* `col_` body box and foot box on body 0. *Visuals:* the art table above; body, foot, mouth, socket ring and locators are moved by nothing. *Animation:* `Flap` rotation from the committed memory state and `Puff` visibility from committed outflow; read-only, off the physics thread. The flap pushes nothing, so it is not a body.
4. **One-way flow and typing.** Signal → memory node → path state → gas transfer inside the tick phases; committed state → animation → renderer. Port ids, roles and the `outlet` choice are enums; areas and pressures are f32.
5. **Flat-state counts per instance.** Bodies 1 (static); colliders 2 (boxes); joints 0; drives 0; sensors 0; domain nodes 1 (signal memory) plus 1 gas path row; ports 3; stores 0; bindings 2; parameters 2.
6. **Performance fitness.** Two primitive colliders, fewer than the Battery's three. Static body, so it joins no island (`ENGINE-ISLANDS-SLEEP` is not needed). One path evaluation per gas step, so per 1/480 s substep (EL-039 F5); no spatial query. Nothing scales with the scene; path tables are sized at Run from the compiled declarations (design contract D5).
7. **Tests are scenarios.** V1 to V12 author constructions and assert committed pressures, masses, states and refusals on the production simulation.

## 4. Sources and legacy

- Requirement row [element-040](../requirements.md#element-040): "Opens a controlled gas path to a declared output or exhaust"; outcome "Closed valve cannot send a command-shaped free pressure pulse". Integration task [todo-376](../requirements.md#todo-376): "chooses a valve/air-hose route and drives the appropriate piston"; [todo-426](../requirements.md#todo-426): "exhaust → whistle", venting and inadequate pressure.
- Named entry [element-040](../invest/named-elements.md#element-040), owner S475. Gas design: "Gas network node + controller input | opening | Releases a burst after the source stops | Flap angle ← committed opening".
- Legacy: no valve part, level or lesson. Applicable facts: nozzle NoFlow at zero area and forward-only flow (EL-039 P6); a deferred activation command applies on the next tick (`parts/SpeakerPart.cs@a6c914e:L66-L71`) — carry forward the next-tick rule, not the per-part request queue. The harvest also recorded sticky latch targets in the old activation network (`engine/gpu/ActivationNetwork.cs@a6c914e:L199-L229`); the current memory node replaces it.
- **Files harvested:** `parts/SpeakerPart.cs` (activation-socket and next-tick facts only); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-040](../requirements.md#element-040), [pneumatics profile](../invest/profiles.md#pneumatics). Tolerances and stall guards are EL-039 F6. Planned test class `ReleaseValveTests`. The standard rig is a default tank at 400 000 Pa, a hose, the valve, a hose and a default tank at ambient.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| V1 | Given the rig in Output mode, when one pulse reaches `ActivationIn`, then within 20 s both tanks read 250 700 Pa and total gas mass is unchanged | `ReleaseValveTests.ATriggerOpensThePathAndTheTanksSettle` | V2 | — |
| V2 | Requirement outcome: given the rig with no pulse, when Run advances 20 s, then the upstream tank holds 400 000 Pa and the downstream tank stays at ambient | `ReleaseValveTests.AClosedValvePassesNothing` | V1 is the same rig with a pulse | — |
| V3 | Requirement outcome: given the rig with the upstream tank at ambient, when the valve is triggered, then the flap opens and the downstream pressure and mass do not change (a command makes no pressure) | `ReleaseValveTests.ATriggerOnAnEmptyTankMovesNoGas` | V1 | upstream exactly 101 325 Pa |
| V4 | Given Exhaust mode with a 400 000 Pa tank on `GasIn`, when triggered, then the tank falls to ambient within 20 s, never below it, and network gas mass falls | `ReleaseValveTests.ExhaustModeVentsTheTankToAmbient` | untriggered, the tank holds 400 000 Pa for 20 s | an ambient tank: the trigger changes nothing |
| V5 | Given `opening` at its minimum and at its maximum, when triggered, then the minimum takes at least twice as long to settle (the areas differ tenfold) | `ReleaseValveTests.ASmallerOpeningReleasesMoreSlowly` | — | both limits |
| V6 | Given an open valve, when a second pulse arrives, then nothing changes | `ReleaseValveTests.ASecondTriggerHasNoFurtherEffect` | — | two pulses in one tick |
| V7 | Given a pulse emitted in tick N, then no gas has moved at the end of tick N and gas has moved within 0.1 s | `ReleaseValveTests.TheValveOpensTheTickAfterItsTrigger` | — | the declared next-tick rule, not an arrival time |
| V8 | Given a run that opened the valve, when Reset is pressed, then the valve is closed and both tanks hold their authored charge | `ReleaseValveTests.ResetClosesTheValveAndRestoresBothTanks` | — | — |
| V9 | Given non-default `opening`, `outlet` and all three links, when saved and loaded, then they are equal | `ReleaseValveTests.SaveLoadKeepsOpeningOutletAndLinks` | — | both `outlet` values |
| V10 | Given `opening` outside its limits or non-finite, or an undefined `outlet`, when submitted, then it is refused with a typed reason and nothing changes | `ReleaseValveTests.InvalidParametersAreRefusedAtomically` | the limits are admitted | next f32 beyond each limit |
| V11 | Given an electrical output offered to `ActivationIn`, a signal output offered to `GasIn`, or a hose offered to `GasOut` in Exhaust mode, then each is refused | `ReleaseValveTests.WrongKindAndUnavailablePortLinksAreRefused` | the correct links are admitted | — |
| V12 | Later partner (EL-230, [todo-426](../requirements.md#todo-426)): tank, release valve and spring-return piston; one trigger extends the rod and diverts a ball. The same requirement row's spring return on exhaust, repeated strokes and exhaust to a whistle are EL-041 rows D-A14 and D-A15, because this valve latches open and has no exhaust socket | `SpringReturnPistonTests.ATriggeredValveExtendsThePistonAndDivertsABall`, in the EL-230 slice | no activation link: the piston never moves | inadequate pressure: the rod does not reach the ball |

Generic cover today: G12 for authored-state Reset and save/load; V8–V11 stay package tests until the S4 conformance suite is delivered.

**Chrome recipe (pending the post-catalogue browser phase).** Charged Air tank → hose → Release valve (`GasOut → GasIn`) → hose → spring-return piston; Switch under a falling ball; select the switch, Connect `ActivationOut`, click the valve, choose `ActivationOut → ActivationIn`. Positive, negative, boundary, Reset and save/load as V1–V12.

**Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393); [todo-426](../requirements.md#todo-426); combination [todo-378](../requirements.md#todo-378) "One Breath"; interaction processes [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09) and [IX-07 signal propagation](../requirements.md#interaction-07) (the trigger carries no gas).

## 6. Open questions

1. **Control kind.** Resolved (D19): an activation-triggered latch, as proposed. A supplied solenoid is not added because no requirement row or lesson names one.
2. **Re-closing.** Resolved (D19) and confirmed by coordinator decision under the owner's standing delegation (22 November 2026; EL-039 decision 7): no `ResetIn`. The valve is a one-shot release. The exhaust-return, repeated-stroke and exhaust-to-whistle criteria of [todo-426](../requirements.md#todo-426) are delivered in full by [EL-041](EL-041-pneumatic-directional-valve.md), so nothing is weakened and this valve needs no second command input.
3. **Opening rate.** Resolved (D19): instant at the tick boundary.
4. **One entry or two.** Resolved (D8): Output and Exhaust are one `outlet` choice on one entry; the earlier `exhaust_valve` entry is withdrawn.

Needs the owner: none.

## 7. Integration

- **Works with.** EL-039, EL-038, EL-042, EL-045, EL-230 and every activation source. The activation sources are delivered (for example `elements/ImpactSwitch`, `elements/SystemClock`, `elements/DelayBox`); the pneumatic partners are not.
- **Port agreement.** `GasIn` Input and `GasOut` Output in the Fluid domain (F3); `ActivationIn` is a Signal pulse input, as on the Fan.
- **Note for EL-230.** A spring-return piston returns only when its chamber is vented. This valve latches open, so it gives one extension and cannot vent the chamber. The returning build uses [EL-041](EL-041-pneumatic-directional-valve.md): `PortA` to the chamber, `PortB` unconnected, `Exhaust` venting or hosed to a whistle.
- **Campaign.** Named in the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage): first use 71–80, reuse 91–100 and 136–150. No entry in `content/puzzles.json` places it today; a level that does needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7**, which also widens the reader for companion packages and non-`EL` identities (owner decision 15): the published form's preconditions are not met. This identity lacks its own `todo-NNN` and `sequence-task-NNN` pair co-anchored in `requirements.md` (the ledger entry links only the umbrella `todo-376`, which has no co-anchored sequence task and is shared by eight identities; the reader refuses two identities on one task source) and an `<a id="s475">` anchor for its ledger owner S475 in `docs/planning/invest/scope-corrections.md`.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK`; EL-039 Air reservoir; EL-038 hose acceptance; S7. EL-230 is not a prerequisite (V12 is a later-partner row).
- **Files.** Adds `elements/ReleaseValve/ReleaseValve.csproj`, `ReleaseValveElementDeclaration.cs`, `release_valve.tscn`, `icon.svg` and `CuriousContraptions.tests/ReleaseValveTests.cs` (compiled by the test project's one rule; no compile line). Edits one line in `CuriousContraptions.slnx` and this spec's status; the per-package lists S3 owns until S3 lands. Registration is derived.
- **Review tier.** Data-only, if the probe finds every missing row above delivered.
- **Disqualifiers.** No signal-switched gas path; no vent-to-ambient boundary; a memory node that cannot drive a gas path without element code. Each returns the work to `ENGINE-GAS-NETWORK`.
