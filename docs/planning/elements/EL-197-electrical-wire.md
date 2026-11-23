# EL-197 · Electrical wire — element readiness spec

**Current delivery (24 October 2026):** [EL-197 delivery slice](../invest/named-elements.md#element-197). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and is a starting value under decision D1. Supplied-network facts N1–N21 are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-197 · Electrical wire |
| Type | Electrical |
| Anchor | [requirements.md#element-197](../requirements.md#element-197); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Battery, wire, switch …", first use 11–20); [named-elements entry](../invest/named-elements.md#element-197); owner S303 |
| Related | No CAT kind of its own: it is the Electrical connection every supplied part uses ([CAT-005](CAT-005-battery.md) names it "related, not owned"). Connection-kind siblings: activation links (current), [EL-038 hose](EL-038-pneumatic-hose.md), [EL-048 duct](EL-048-acoustic-duct.md). |
| Roadmap story | [EL-197 delivery slice](../invest/named-elements.md#element-197); canonical proof owner and prerequisite decisions are recorded there. |
| Status | behaviour delivered as the Electrical connection domain; the identity is open until a recorded check cites the executed tests and adds the one that is missing. Closure review blocked on the engine programme (owner decision 3a), which comes first; no specific engine story is needed; then the closure-review slice with new row W3. |
| Disposition | **Candidate closure by recorded check, no package** ([delivery-batches §2a, "Wires"](../invest/delivery-batches.md#2-duplicate-closures)). A wire is a typed connection record, not a part, and every behaviour in its requirement row is delivered by the connection and supply layers that the Battery, Motor, gates and contacts already use. It has no behaviour, parameter or mode of its own to deliver. It closes in one small closure-review slice (tests and the ledger row), because one acceptance item has no executed test today (§5 row W3). |
| Planned package | none. A wire is not a drawer part: no package, catalogue entry or palette order, and so no package binding (§7). |

## 2. Declaration

The wire is an authored typed connection, not a body. Its "declaration" is the connection record and the rules that admit it.

| Item | Current value · source |
| --- | --- |
| Record | `WorkshopConnection(Source, Output, Target, Input, Domain, RopeLength?)` with `Domain = WorkshopConnectionDomain.Electrical` (`engine/gpu/WorkshopConnections.cs:11`, `:32`). Ports are `PortId` values on both ends. |
| Bodies, colliders, mass, material | none. A wire has no collider and no spatial query, so two wires that cross in space never connect and never touch a ball (requirement outcome). |
| Admission rules | Both ends are placed parts that own the named ports in the Electrical domain, source port Output and target port Input (`:109`–`:114`); no duplicate link, no undefined domain (`:132`–`:138`); the source resolves to a supply `Output` terminal and the target to an input terminal (`:153`–`:159`). Then the supply topology: a consumer input takes one supplier (`engine/gpu/SupplyRoute.cs:56`, refusal `PowerInputSupplied`); a contact's `PowerIn` admits fan-in for sourced cycles (CAT-063 decision); an output may fan out; cycles follow the monotone cycle rule (`:60`–`:74`). |
| Interfaces involved | The two parts' `ISupplyElement.SupplyGraph` port bindings (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`; `SupplyPortBinding`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:29`). A wire adds no node: it is an edge `SupplyEdge(Source, Target, Input)` (`CuriousContraptions.Abstractions/SupplyValues.cs:46`). |
| Parameters, stores, sensors | none. It carries availability and, for work-taking targets, the consumer's share of the source's finite power, without loss. |
| Presentation | A straight cylinder between the two sockets' world positions, drawn by the Workbench: radius 0.035 m and navy `#293954` for an electrical link, radius 0.025 m and `#ffd899` for other links (`ui/WorkshopConnections.cs:188`–`:189`; the value is assigned to the cylinder mesh's top and bottom radius at `:282`), so wires read thicker than activation links ([DESIGN.md](../../../DESIGN.md#motion-and-state-feedback): "Electrical cables are navy and thicker than gold activation links"; that navy electrical link is the thin wire of owner decision 1, not a solid strand). The earlier *proposed* 0.05 m diameter is superseded by the delivered 0.07 m. |
| Catalogue / inventory | none. Created by the Connect control from an Electrical output socket ("Click a part with a PowerIn socket.", `ui/WorkshopConnections.cs:239`); removed with "Disconnect supply" (`:247`). Wires are free; a level allows or forbids wiring as a whole through `connections_allowed` (`engine/gpu/PuzzleDefinition.cs:146`). |

**Variants** (row: "carry power or control according to their distinct typed contracts"). The variant is the target terminal, not a kind of wire:

| Variant | Target | Network meaning |
| --- | --- | --- |
| Power wire | `SupplyTerminal.PowerIn` of a consumer, contact or gate | Feeds supply; a work-taking consumer debits the source ([EL-196](EL-196-battery.md)). |
| Condition wire | `SupplyTerminal.FirstIn` / `SecondIn` of a gate ([EL-133](EL-133-electrical-and-gate.md)–[EL-137](EL-137-electrical-nand-gate.md)); the `ConditionIn` consumer terminal of an edge detector ([EL-181](EL-181-rising-edge-detector.md), planned) | Carries a condition only; the target never passes this energy onward (`engine/gpu/SupplyRoute.cs:77`). |

### Art and solidity

The wire is drawn by the Workbench between two sockets and is not package art. It is a thin wire drawn without a collider (owner decision 1, 22 November 2026): thin wires stay non-solid drawings as delivered (`ui/WorkshopConnections.cs:272`–`:283`), while ropes, cables, hoses and ducts are solid strands under `ENGINE-CONNECTION-MATERIALS`. It is not a static proud solid of any part and moves with its endpoints. It is told from an activation link by thickness (radius 0.035 against 0.025 m) as well as colour.

### Behaviour and numbers

- **Law.** A target input is available when any admitted wire into it comes from an available output; a source-free loop of wires stays unpowered; removing the wire removes the availability on the next solve.
- **Tick order (D7).** Wires are construction data; the electrical solve of Phase 1 reads them. Reset never changes them.
- **Numbers.** None that affect behaviour. The wire radius 0.035 m is presentation.
- **Consistency across the batch.** Every electrical output in this batch is role `Supply`, direction Output (Battery, contacts, gates, Solar panel); every electrical input is direction Input with role `PowerIn`, `FirstIn` or `SecondIn`. No spec in the batch declares an electrical port that breaks the output → input rule.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S303): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction). The harvest-baseline "Missing" list (admitting the Electrical domain, condition sockets, cable rendering) is delivered. Probe on `8d1d68aa`: 7 present, 1 present-untested, 0 narrower, 0 missing.

| # | Capability | Mark | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Electrical connection admitted between declared ports | present | `engine/gpu/WorkshopConnections.cs:121`–`:122`, `:153`–`:159` | `PoweredGateTests.Connections_AcceptsElectricalAndRejectsActivation` (`PoweredGateTests.cs:131`) |
| 2 | Wrong domain, wrong direction, missing endpoint refused atomically | present | `engine/gpu/WorkshopConnections.cs:109`–`:114` | `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically` (`PackageLocalPortsTests.cs:96`); `MissingPortEndpointInConnection_RefusesAtomically` (`:76`); `MotorTests.AnActivationWireIntoThePowerInputIsRefused` (`MotorTests.cs:316`) |
| 3 | One supplier per consumer input; duplicate feed refused | present | `engine/gpu/SupplyRoute.cs:56` | `PackageLocalPortsTests.SignalAliasesDeduplicateAndDuplicateElectricalFeedsRefuse` (`:124`); `ElectricalSupplyTests.PlanAdmitsFanoutAndRejectsForeignDuplicateOrAmbiguousSupplier` (`ElectricalSupplyTests.cs:307`) |
| 4 | Power wire carries supply; removing it removes power | present | `engine/gpu/SupplyRoute.cs:60`–`:77` | `BothGateTests.MissingBottomWireNeverConvertsConditionsIntoEnergy` (`BothGateTests.cs:85`); `ElectricalXorTests.RemovingTheSupplyOrConditionLinksUnpowersATrueRow` (`ElectricalXorTests.cs:79`) |
| 5 | Condition wire carries no output energy | present | `engine/gpu/SupplyRoute.cs:77` | `ComposableSupplyRoutingTests.LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork` (`ComposableSupplyRoutingTests.cs:278`) |
| 6 | Independent circuits stay independent | present | supply topology per source, `engine/gpu/SupplyRoute.cs:60`–`:77` | `PoweredGateTests.TwoDirectCircuitsFollowTheirOwnSources` (`PoweredGateTests.cs:225`) |
| 7 | Save and Load of wires with typed ports; unknown port refused | present | `engine/gpu/WorkshopSaveCodec.cs:19`, `:48` (cited from the gates closure contract) | `ElectricalSupplyTests.F32AuthoringWiringAndSaveRoundTripWithAtomicAmbiguousSourceRejection` (`:39`); `ElectricalNorTests.ClosingOneSwitchRetractsTheOutputAndSaveLoadAndResetAreExact` |
| 8 | A wire has no geometry: two wires whose drawn segments cross do not connect, and a ball passes through a drawn wire | **present, untested** | the connection record has no collider or pose (`engine/gpu/WorkshopConnections.cs:32`) | none names the crossing; row 6 proves independence but does not place the circuits so their wires cross. Added by the closure check (W3) |

Nothing needs a specific engine story; the closure review still waits for the engine programme (owner decision 3a), which comes first.

**Dependencies.** Battery and at least one supplied part (delivered).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance standard of 22 November 2026 ([engine-conformance.md](readiness/engine-conformance.md); [element-free engine design](../../../_bmad/custom/element-free-engine-design.md); AGENTS.md, Tri-Graph architecture). This identity has no package. Verdict: **Conforms**.

1. **Declaration data only.** A wire is one authored `WorkshopConnection` record in the Electrical domain; no code path names a wire kind or an element.
2. **Generic laws.** The supply network rule: an input is available when an admitted edge into it comes from an available output; admission is the typed port rule (domain, direction, one supplier per consumer input) and the monotone cycle rule.
3. **Three layers.** Colliders: none; a wire is not in the physics graph, being a thin wire drawn without a collider (owner decision 1). Visuals: one cylinder per link, drawn by the Workbench from the two socket positions; it is not package art and is never a collider. Animation: no binding; what moves the drawn segment is its endpoints' committed poses, read each frame, and nothing is written back.
4. **One-way flow and typing.** Construction data → supply topology → solve; the domain is the closed enum `WorkshopConnectionDomain`, the ends are `PortId` values.
5. **Flat state.** At Run each wire compiles to one `SupplyEdge(Source, Target, Input)` row (`CuriousContraptions.Abstractions/SupplyValues.cs:46`). Bodies 0; colliders 0; joints 0; drives 0; sensors 0; domain nodes 0 (it joins two nodes of its endpoint packages); stores 0; bindings 0. The endpoint counts are those of the satisfying packages, for example `battery` ([EL-196](EL-196-battery.md)) and `both_gate` ([EL-133](EL-133-electrical-and-gate.md)).
6. **Performance fitness.** One edge row per wire; no spatial query (nothing from `ENGINE-SPATIAL-QUERIES`), so crossing wires cost nothing; no body (nothing waits for `ENGINE-ISLANDS-SLEEP`). Whether the connection list or the supply edge table has a stated capacity was not established here; none is assumed.
7. **Tests are scenarios.** W1–W6 run the production construction, supply solve and save paths.

## 4. Sources and legacy

- Requirement row [element-197](../requirements.md#element-197): "Compatible terminals carry power or control according to their distinct typed contracts"; outcome "Geometric crossing alone cannot connect wires".
- Named entry [element-197](../invest/named-elements.md#element-197), owner S303; [S257](../invest/decisions.md#s257) electrical-port: "a reversed or incompatible wire is refused at connection and the construction is unchanged; a full-capacity attempt refuses the extra wire".

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| W1 | A link resolves only with exactly one matching port per end, same domain, output (or bidirectional) → input; unknown domains, missing or undefined socket ids and self-links reject. | `engine/ConnectionPort.cs@a6c914e:L40-L67` | carried forward, except that a same-part electrical link is now admitted by the connection layer (`engine/gpu/WorkshopConnections.cs:134`) and judged by the supply cycle rule |
| W2 | Domain names serialize snake_case (`electrical`); socket ids serialize as exact snake_case names with no aliases. | `engine/MachineData.cs@a6c914e:L67-L70`, `engine/MachineData.cs@a6c914e:L103-L122` | not current: a saved wire carries typed `PortId` values |
| W3 | Typed sockets need matching domain, direction and identity; duplicate source ports or a missing socket reject. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L18-L38` | carry forward |
| W4 | A wire is (input node → output node); a node is powered if any wire into it is powered; a source-free wire loop stays unpowered; supply loss clears every downstream input. | `engine/BinaryCircuit.cs@a6c914e:L16-L16`, `engine/BinaryCircuit.cs@a6c914e:L73-L78`, `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L137-L181` | carry forward |
| W5 | Connecting battery → motor with no explicit sockets picks the sole compatible pair `Supply → PowerIn`; motor → battery is rejected. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L110-L118` | carry forward |
| W6 | Every electrical socket pair has a distinct label and an actual pictogram (15 pairs); non-electrical pairs reject. | `CuriousContraptions.tests/ConnectionChoiceTests.cs@a6c914e:L9-L34` | carry forward (socket glyphs are now package-declared, `SocketGlyph`) |
| W7 | Removing any one solution wire leaves the motor unpowered and the level unsolved. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L82-L91` | carry forward (control) |
| W8 | `switched_motor` wiring: battery `supply` → switch `power_in`, switch `supply` → motor `power_in`. | `content/puzzles.json@a6c914e:L3857-L3872` | carry forward (level input) |

- **Files harvested:** `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `engine/BinaryCircuit.cs`, `CuriousContraptions.tests/ConnectionPortTests.cs`, `CuriousContraptions.tests/ConnectionChoiceTests.cs`, `CuriousContraptions.tests/ElectricalTests.cs`, `content/puzzles.json` (switched_motor wiring).

## 5. Acceptance outline

Acceptance authority: [element-197](../requirements.md#element-197), [S257](../invest/decisions.md#s257) electrical-port construction. Rows W1, W2 and W4–W6 cite tests that exist; W3 is planned in class `ElectricalWireClosureTests`.

| Row | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| W1 | Given a Battery wired `Supply → PowerIn` to a Motor, when Run, then the motor is supplied; given a condition wire into a supplied gate, then the output follows the truth table | `MotorTests.ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss`; `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer` | `MotorTests.AnUnpoweredMotorStaysStillDoesNoWorkAndKeepsItsLampSlate` (no wire) | — |
| W2 | Given an incompatible wire (activation → `PowerIn`, wrong direction, missing port, second supplier on one consumer input), then it is refused with its typed reason and the construction is unchanged | `MotorTests.AnActivationWireIntoThePowerInputIsRefused`; `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`; `MissingPortEndpointInConnection_RefusesAtomically`; `SignalAliasesDeduplicateAndDuplicateElectricalFeedsRefuse` | the compatible wire is admitted | the second wire on one consumer input is the capacity boundary |
| W3 | Given two unrelated circuits (battery A → gate A, battery B → gate B) placed in the plane z = 0 so that circuit A's wire runs between socket world positions (−2, 1, 0) and (2, 3, 0) and circuit B's between (−2, 3, 0) and (2, 1, 0), crossing at (0, 2, 0), with battery B disabled, when Run, then gate A is powered and gate B never is; a Ball released at rest at (0, 4, 0) falls through the crossing point and is below y = 1 with no contact record | `ElectricalWireClosureTests.TwoCircuitsWhoseWiresCrossStayIndependentAndABallPassesThroughThem` (planned) | enabling battery B powers gate B (the wires do work) | the two drawn segments intersect at one point; every part's colliders are at least 1.5 m from the ball's path |
| W4 | Given a source-free loop of wires, then it stays unpowered; given a supplied loop, when the source is removed, then every downstream input clears | `ElectricalOrTests.OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick`; `SwitchElectricalTests.SeededCycleLosesAndRestoresPowerWithoutChangingEitherLatch` | — | — |
| W5 | Given a wired construction, when Run and Reset, then the wires are unchanged; when saved and loaded, then both endpoints and typed ports are equal; a save naming an unknown port is refused whole | `ElectricalSupplyTests.F32AuthoringWiringAndSaveRoundTripWithAtomicAmbiguousSourceRejection`; `ComposableSupplyRoutingTests.SaveLoadAndResetPreservesAuthoredStateAndComposableConnections` | — | — |
| W6 | Given the `switched_motor` wiring (battery → switch → motor), then the motor runs only after the switch closes, and removing either wire leaves it unpowered | `SwitchElectricalTests.PhysicalImpactDrivesBothDomainsAndExactSaveReset`; `UnqualifiedOrUnsuppliedContactCannotMoveConsumer` | — | — |

**Tolerances and stall guards.** W3 asserts typed supply states over a 240-tick window and the absence of any contact record for the ball (there is no wire body to contact); the ball's fall is checked only as "below y = 1 within a 5 s stall guard" (free fall over 3 m takes about 0.8 s), no trajectory. The socket positions are authored inputs, not simulated floats. No float tolerance is needed.

**Generic coverage today.** Wrong-kind refusal and Save/Load are the generic tests cited in W2 and W5. Parameter refusal does not apply.

**Chrome recipe (pending, post-catalogue).** Battery and Motor: select the battery, Connect `Supply`, click the motor — the single pair is offered and a navy wire appears between the actual sockets. For a gate, the socket-choice buttons offer `Supply → PowerIn`, `Supply → FirstIn`, `Supply → SecondIn` with distinct icons. Draw two unrelated circuits whose wires cross on screen: neither gains power from the other.

## 6. Open questions

All resolved; needs the owner: none.

1. **Per-socket capacity — resolved by the delivered rule.** A consumer input accepts exactly one supplier and refuses a second (`PowerInputSupplied`); a contact's `PowerIn` admits fan-in so sourced cycles can exist (CAT-063 decisions); an output fans out freely. This is the S257 "full-capacity attempt refuses the extra wire".
2. **Wire budget — decided: wires are free** (D19; the spec's proposal). No level lists a wire inventory, and limiting wires would add an untaught rule. A level may forbid wiring altogether with `connections_allowed`.
3. **Closure or slice — decided: closure by recorded check** (standing delegation). Reason: the identity has no behaviour of its own; a package or catalogue entry for a wire would be a second representation of the connection.
4. **Coordinator decisions, 22 November 2026 (owner's standing delegation).** (a) Coverage: a wire has no package, and the binding form published on main binds a package only, so this identity has no binding today; S7 must define how a connection kind is covered. Reason: owner decision of 21 November 2026 on the binding form. (b) Wire art: a thin wire drawn without a collider, as owner decision 1 (22 November 2026) decides for thin wires (Art and solidity).

## 7. Integration

- **Collaborators.** Every part with an electrical port: Battery, Impact switch, Counter, State latch, Hold timer, Pressure plate, the five gates, Powered gate, Motor, Fan, System clock, Bumper, Linear pusher, Cannon (delivered); Solar panel and the edge detectors (planned).
- **Campaign.** "wire" is named in the power row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 11–20 (reuse 31–50, 61–80, 136–150). Levels 14 `battery_motor` and `switched_motor` use it; both wait for the goal-evaluation slices.
- **Processes.** Battery retained behaviour [todo-146](../requirements.md#todo-146), switch [todo-147](../requirements.md#todo-147), gates [todo-362](../requirements.md#todo-362); [IX-06](../requirements.md#interaction-06) (power wires) and [IX-07](../requirements.md#interaction-07) (condition wires carry no output energy).
- **Coverage binding (planned, not yet live).** None today. The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239, binds a package, and a connection kind has none. **Blocked on the coverage follow-on story S7** for the mechanism: S7 must define how a connection kind (this wire, the hose EL-038, the duct EL-048) is covered. Lacking in any case: the EL-197 ledger entry has no `todo-NNN` link and owner S303 has no anchor in `scope-corrections.md`. Until S7 decides, the ledger row cites the connection capability and the tests of §5 when the check passes, and `tools/Coverage` keeps `element-197` as a source obligation. The closure check itself does not wait for S7.

## 8. Delivery

- **Slice shape.** One closure-review slice ("closure review: electrical wire"): one record, one candidate commit, one approval; owned diff is `CuriousContraptions.tests/ElectricalWireClosureTests.cs` (one test; compiled by the test project's one top-level rule, no project-file line), the EL-197 ledger entry, sprint status, this spec's Status row and `delivery-batches.md`. It can share a record with EL-196's closure check if both are ready together, but each identity keeps its own rows.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first; no specific engine story is needed. Then the closure-review slice; it does not depend on the gates closure or on S7.
- **Review tier.** Data-only (tests and ledger rows; no production change).
- **Disqualifiers (re-check at planning).** W3 failing (then a wire has geometry somewhere and it is a defect for an engine slice, not a closure); any cited test having been renamed or removed since `8d1d68aa`; a decision to give wires a budget or capacity parameter, which would be behaviour of its own and make this a slice.
