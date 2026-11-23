# EL-133 · Electrical AND gate — element readiness spec

**Current delivery (24 October 2026):** [EL-133 delivery slice](../invest/named-elements.md#element-133). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). This spec holds the **gate facts shared by EL-133–EL-137 (E1–E10)**; [EL-134](EL-134-electrical-or-gate.md), [EL-135](EL-135-electrical-xor-gate.md), [EL-136](EL-136-electrical-nor-gate.md) and [EL-137](EL-137-electrical-nand-gate.md) add only their operation. The catalogue-level harvest is in [CAT-013](CAT-013-both_gate.md) (G1–G8) and the supplied-network facts in [CAT-005](CAT-005-battery.md) (N1–N20).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-133 · Electrical AND gate |
| Type | Electrical |
| Anchor | [requirements.md#element-133](../requirements.md#element-133); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-133); owner S405 |
| Related | Refines [CAT-013 Both gate](CAT-013-both_gate.md) (`both_gate`, operation And; requirement [current-cat-013](../requirements.md#current-cat-013)). Supply from [CAT-005 Battery](CAT-005-battery.md) / [EL-196](EL-196-battery.md) through [EL-197 wires](EL-197-electrical-wire.md); conditions typically from [EL-198 Switch](EL-198-switch.md) contacts. |
| Roadmap story | [EL-133 delivery slice](../invest/named-elements.md#element-133); canonical proof owner and prerequisite decisions are recorded there. |
| Status | open: candidate duplicate of the delivered `both_gate`; closes in the electrical-gates closure review. Closure review blocked on the engine programme (owner decision 3a), which comes first, in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES`; then the closure contract replanned. |
| Disposition | **Candidate Duplicate closure** of [CAT-013](CAT-013-both_gate.md), package `elements/BothGate`, id `both_gate` ([delivery-batches §2a](../invest/delivery-batches.md#2-duplicate-closures)). Closed by the drafted contract `_bmad-output/implementation-artifacts/spec-closure-review-electrical-gates.md` (status blocked on the engine programme, owner decision 22; its prerequisite is the self-edge row of `ENGINE-SIGNAL-NODES`, whose slice text is in `epics.md` under that story), together with EL-134 to EL-137. No delta was found (see §2 "Equivalence check"), so it is a duplicate, not a variant. |
| Planned package | none — closed duplicate. No package, catalogue entry, palette entry, icon or id of its own. The satisfying package keeps its palette order 26 (`elements/BothGate/BothGateElementDeclaration.cs:17`). |

## 2. Declaration

The `both_gate` declaration, art and bindings are [CAT-013 section 2](CAT-013-both_gate.md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026). What this spec keeps is the closure record: the terminal-name note, the truth table, the equivalence check, the behaviour statement and the shared gate facts E1–E10 that EL-134 to EL-137 cite. The delivered catalogue text stands over the longer legacy text (`parts/catalog/both_gate.tres@a6c914e:L6-L11`, wording only; closure contract, decision 4).

**Delivered terminal names (engine design audit finding E2).** `FirstIn`, `SecondIn` and the two-input state enum (Neither/FirstOnly/SecondOnly/Both) are the delivered names of a boolean supply node fixed at two inputs and five named operations (`CuriousContraptions.Abstractions/SupplyContractValues.cs:26`; `CuriousContraptions.Abstractions/ElectricalContractValues.cs:25`; evaluation `engine/gpu/SupplyRoute.cs:375`–`:395`). `ENGINE-CAPABILITY-INSTANCES` replaces them, forward-only, with one N-input boolean node whose input list and truth function are declared, and edits the five delivered gate packages in that story. The closure review checks each gate in that post-refactor form: two declared inputs, the same truth table, the same four sockets at the same positions. Wherever this spec or its siblings name `FirstIn`/`SecondIn`, they name the delivered terminals that story replaces; the replanned contract uses the story's names.

**Variants.** The requirements row names none beyond the fixed operation (`Logic=And` in [current-cat-013](../requirements.md#current-cat-013)). Truth table (E6): A B = 00 → 0, 01 → 0, 10 → 0, 11 → 1; output = `PowerIn` ∧ (A ∧ B).

### Equivalence check (why this is a duplicate)

Read against the package and the closure contract's "Duplicate decision" table. Bodies, material, the four ports and their positions, the absent parameter list, the three lamps, the art and the acceptance rows are the same as the delivered part. Differences found: the description wording (not behaviour) and the enum's name and order (`LogicGateKind` in the legacy, `LogicGateOperation` now; no saved value depends on the legacy order). The five gate packages differ from each other only in id, name, description, palette order, scene path and the one `LogicGateOperation` value (file comparison, 21 November 2026). **The exact check that proves equivalence** is the closure contract's test `ElectricalGateClosureTests.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(both_gate)`, whose expected rows come from this spec, plus rows G1–G3 and G5–G8 below passing through the delivered part.

### Behaviour and numbers

- **Law.** Output available = `PowerIn` available ∧ operation(`FirstIn`, `SecondIn`). Conditions carry no energy; a consumer's work is debited only from the source on the `PowerIn` route (`engine/gpu/SupplyRoute.cs:77`).
- **Tick order (D7).** The electrical solve is zero-delay within a tick: a changed condition changes the output on the committed tick it is read. A cycle of monotone nodes settles from unpowered each tick (`engine/gpu/SupplyRoute.cs:60`); a cycle through a nonmonotone node is refused before any solve (`:69`).
- **Numbers.** None of its own. Lamp blend 0.1 s is presentation only. Electrical ratings are not simulated.
- **Consistency with delivered parts.** Port roles agree with the Battery (`Supply`, Output), Impact switch, Counter and State latch contacts (`PowerIn` → `Supply`), Powered gate and Motor (`PowerIn`, Input). A consumer input admits one supplier (`engine/gpu/SupplyRoute.cs:56`).

### Shared gate facts (EL-133–EL-137)

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| E1 | All five gates share one body, foot and port layout; only the operation and face art differ. | `parts/ElectricalLogicPart.cs@a6c914e:L11-L46` | carried forward as five declarations differing in one enum value |
| E2 | Ports FirstIn/SecondIn/PowerIn inputs and Supply output; gate rule (operation, FirstIn, SecondIn, PowerIn, Supply). | `parts/ElectricalLogicPart.cs@a6c914e:L28-L36` | carry forward the behaviour; the two fixed terminals become declared inputs of one N-input node (`ENGINE-CAPABILITY-INSTANCES`, audit finding E2) |
| E3 | Truth reads committed availability of FirstIn and SecondIn; the state enum is Neither/FirstOnly/SecondOnly/Both. | `parts/ElectricalLogicPart.cs@a6c914e:L8-L8`, `parts/ElectricalLogicPart.cs@a6c914e:L18-L21` | carry forward the truth rule; the two-input state enum is deleted by `ENGINE-CAPABILITY-INSTANCES` (E2) |
| E4 | A gate output node is powered only if its supply node is powered and the operation is true; conditions and supply must be distinct nodes. | `engine/BinaryCircuit.cs@a6c914e:L86-L96`, `engine/BinaryCircuit.cs@a6c914e:L136-L138` | carry forward |
| E5 | Owner active = truth ∧ PowerIn supplied; that drives the output lamp. | `parts/ElectricalLogicPart.cs@a6c914e:L37-L41` | carry forward; do not carry forward the per-part `PreparePhysics` loop |
| E6 | Explicit truth table, rows 00, 01, 10, 11: And F F F T; Or F T T T; Xor F T T F; Nor T F F F; Nand T T T F. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L5-L19`, `engine/LogicGate.cs@a6c914e:L11-L19` | carry forward (acceptance table) |
| E7 | Every operation × truth row × supply on/off × reversed ids: load powered iff supply ∧ truth; reconnecting in reverse order gives the same result; removing every battery link leaves the load unpowered. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62` | carry forward |
| E8 | Any cycle containing Xor, Nor or Nand rejects at construction (and at Run even through an open switch), naming the parts; nothing is partially powered and the snapshot is unchanged. And/Or cycles settle from the unpowered least fixed point. | `engine/BinaryCircuit.cs@a6c914e:L104-L112`, `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L129` | carry forward |
| E9 | Monotone (And/Or) feedback cannot remember a condition after its source is removed. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L131-L157` | carry forward |
| E10 | A reconvergent second input retracts an Xor/Nand output within the same solve (10 → 11). | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90` | carry forward (EL-135, EL-137) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S405): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction). The "Missing" list of the harvest baseline (Stories 8.1 and 9.6) is delivered. The probe below is the closure contract's, re-read here: 7 present, 0 present-untested, 1 narrower, 0 missing.

| # | Capability | Mark | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Supplied logic node with a declared operation | present | `elements/BothGate/BothGateElementDeclaration.cs:34` | `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer` (`BothGateTests.cs:61`) |
| 2 | Energy provenance from `PowerIn` only | present | `engine/gpu/SupplyRoute.cs:77` | `BothGateTests.EnergyProvenanceIgnoresBothConditionSourcesAndUndefinedRulesReject` (`:188`) |
| 3 | Finite source debit through a gate | present | `elements/Battery/BatteryElementDeclaration.cs:71` | `ComposableSupplyRoutingTests.LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork` (`ComposableSupplyRoutingTests.cs:278`) |
| 4 | Monotone cycle settles from unpowered each tick | present | `engine/gpu/SupplyRoute.cs:60` | `ElectricalOrTests.OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` (`ElectricalOrTests.cs:155`) |
| 5 | Nonmonotone cycle refused atomically, in edit and save | present | `engine/gpu/SupplyRoute.cs:69` | `ElectricalXorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` (`ElectricalXorTests.cs:263`) |
| 6 | A gate wired to its own input is judged by the cycle rule | **narrower**: refused first as `InvalidConnection` (`engine/gpu/SupplyRoute.cs:50`–`:54`) | one-node branch `CuriousContraptions.Abstractions/SupplyCycles.cs:67` is unreachable | none; owned by the self-edge rule row of `ENGINE-SIGNAL-NODES` |
| 7 | Committed socket lamps | present | `elements/BothGate/BothGateElementDeclaration.cs:44` | `BothGatePresentationTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`BothGateTests.cs:259`) |
| 8 | Save and Load of electrical wiring; Reset | present | `engine/gpu/WorkshopSaveCodec.cs:19`, `:48` (cited from the closure contract; not re-opened here) | `ElectricalNorTests.ClosingOneSwitchRetractsTheOutputAndSaveLoadAndResetAreExact` (`ElectricalNorTests.cs:86`) |

Nothing here needs element-specific engine code: the gate is one `SupplyLogicDescriptor` value. The self-edge rule is a row of `ENGINE-SIGNAL-NODES` (owner decision 3a; one story for the rows of the compiled signal and supply network). The fixed two-input form of the boolean node is a naming and fixed-slot refactor owned by `ENGINE-CAPABILITY-INSTANCES` (finding E2); it changes no truth-table row.

**Dependencies.** Battery (delivered), a supplied load (Powered gate, delivered; Motor, delivered), condition sources (Battery, Impact switch, State latch, Hold timer contacts; all delivered).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 ([element-free engine design](../../../_bmad/custom/element-free-engine-design.md); AGENTS.md, Tri-Graph architecture). Standard: [engine-conformance.md](readiness/engine-conformance.md). This identity has no package; it compiles to nothing of its own, and the counts below are the satisfying package `both_gate` (`elements/BothGate`). Verdict for this identity: **Conforms**: each narrower capability has one owning story (the self-edge rule row of `ENGINE-SIGNAL-NODES`; finding E2 under `ENGINE-CAPABILITY-INSTANCES`). The satisfying package also carries CAT-013's recorded forward-only refactor of its unread `ElectricalNodes` ratings ([CAT-013](CAT-013-both_gate.md) section 7), which changes no behaviour and does not block the closure.

1. **Declaration data only.** `both_gate` is a declaration, a scene and bindings. `engine/`, `CuriousContraptions.Simulation`, `.Core`, `.Bundle` and the abstractions projects contain no reference to `both_gate`, `BothGate` or any of the five gate ids or classes (searched 22 November 2026). The truth table is the declared closed enum `LogicGateOperation`, evaluated by one generic function for any package that declares a gate node (`engine/gpu/SupplyRoute.cs:389`–`:394`). **Finding E2 (owned):** the shared contract fixes that node at two terminals and five named operations; `ENGINE-CAPABILITY-INSTANCES` replaces it with one N-input node whose inputs and truth function are declared and edits this package forward-only (§2, "Delivered terminal names").
2. **Generic laws.** The supply network rule: output available = `PowerIn` available ∧ operation(first, second); conditions carry no energy (the provenance walk follows `PowerIn` edges only, `engine/gpu/SupplyRoute.cs:77`–`:82`); a non-monotone cycle is refused by the topology rule. The five gates differ in one declared enum value.
3. **Three layers.** Colliders: ten on the one static body (three boxes: body, face, foot; seven spheres: three lamps, four ports); nothing reads art. Visuals: the same ten solids plus the flat printed marks; no art node is a collider. Animation: the three lamps are recoloured by presentation bindings from committed socket and supply state. What moves each art node: body, foot, face plate, sockets and printed marks, nothing (one static body, no pose change); the three lamps, a binding (colour only).
4. **One-way flow and typing.** Supply solve → committed socket state → presentation; nothing flows back. The operation is a closed enum; ports are `PortId`; values are f32.
5. **Flat state.** Bodies 1 (static); colliders 10 (box 3, sphere 7); joints 0; drives 0; sensors 0; domain nodes 1 (supply gate node); stores 0; presentation bindings 9 rows on three lamps.
6. **Performance fitness.** Static body; ten primitives, each a proud visible solid (visible = solid), none added by this closure. Per-tick work is the supply solve's per-node cost. The part makes no spatial query (so it needs nothing from `ENGINE-SPATIAL-QUERIES`) and has no dynamic body (so nothing waits for `ENGINE-ISLANDS-SLEEP`). No per-part collider or binding limit is stated in the documents or code read here, and none is assumed.
7. **Tests are scenarios.** Rows G1–G6 run the production simulation through the delivered package; none depends on a gate-specific code path.

## 4. Sources and legacy

- Requirement row [element-133](../requirements.md#element-133): "Separately supplied electrical output follows both inputs present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [todo-364](../requirements.md#todo-364): each gate variant has its own playable entry, icon and separately supplied output.
- Named entry [element-133](../invest/named-elements.md#element-133), owner S405; [electrical profile](../invest/profiles.md#electrical); component research logic family ([component research](../../component-research.md#logic)).
- Legacy harvest: E1–E10 above; CAT-013 G1–G8 and CAT-005 N1–N20 cover the same files in more depth. Legacy geometry, port and art sources: `parts/ElectricalLogicPart.cs@a6c914e:L22-L54`, `:L72-L94`; catalogue `parts/catalog/both_gate.tres@a6c914e:L6-L11`; scene `parts/scenes/both_gate.tscn@a6c914e:L1-L4`; toolbox icon `ui/WorkshopIcons.cs@a6c914e:L61-L61`.
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/both_gate.tres`, `parts/scenes/both_gate.tscn`, `engine/LogicGate.cs`, `engine/BinaryCircuit.cs`, `CuriousContraptions.tests/LogicGateTests.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`, `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-133](../requirements.md#element-133), [current-cat-013](../requirements.md#current-cat-013). Rows and tests are those of the closure contract (AC-133: G1–G3, G5–G8). `Closure` is the planned class `ElectricalGateClosureTests`; every other test exists today and was found in this checkout. Each row is observed on a Powered gate wired to the gate's `Supply` and on the committed logic read.

| Row | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| G1 | Given the Both gate with two condition batteries and a supply battery, when each of the four input rows runs with and without the supply wire, then the consumer is powered only for row 11 with supply | `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer` (8 rows) | `BothGateTests.MissingBottomWireNeverConvertsConditionsIntoEnergy` | rows 00, 01, 10 with supply; 11 without |
| G2 | Given row 11 running, when one condition is removed and restored during Run, then the output retracts on the committed tick it is read and returns | `Closure.ABothGateOutputRetractsWhenOneConditionIsRemovedDuringRunAndReturns` (planned; three 30-tick phases) | first phase powered | `BothGateTests.ChainedReconvergenceAndMutualCyclesRetractWithoutOrderDelay`; `BothGateTests.OpeningPoweredContactRetractsMutualLoopWhileSourcesRemainEnabled` |
| G3 | Given a running construction, when it is Reset, saved and loaded, then the unpowered authored state and wiring are restored and the loaded construction equals the saved one port by port | inside `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer` | — | typed `PortId` round-trip; the snake-case names of the harvest are not current input |
| G5 | Given the gate's `Supply` wired to its own `FirstIn` (carried by a construction or a save; the connect tool refuses a same-part target, `ui/WorkshopConnections.cs:106`), when it runs with its second input off then on then off, then the wire is admitted and the gate never powers itself | `Closure.AMonotoneGateWiredToItsOwnInputIsAdmittedAndNeverPowersItself(both_gate)` (planned; needs the self-edge rule row of `ENGINE-SIGNAL-NODES`) | first 60 ticks unpowered | still off with its second input powered |
| G6 | Given one battery on the conditions and another on `PowerIn`, when the consumer draws work, then only the `PowerIn` battery is debited | `ComposableSupplyRoutingTests.LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork` | condition battery balance unchanged | `BothGateTests.EnergyProvenanceIgnoresBothConditionSourcesAndUndefinedRulesReject` |
| G7 | Given the bundle, when the `both_gate` entry is read, then it is its own entry: "Both gate", Control, operation And, exactly four electrical ports, no parameter, no store, no activation port, a unique icon | `Closure.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(both_gate)` (planned) | — | — |
| G8 | Given the three lamps, when inputs and supply change and on Reset, then input lamps follow committed inputs, the output lamp follows truth ∧ supply, and Reset returns them to slate | `BothGatePresentationTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` | an unsupplied control gate keeps a slate lamp | `BothGateTests.CommittedLatchSetResetAndSupplyRestorationDriveBothGate` (three cadences) |

**Tolerances and stall guards.** None is introduced: every assertion is a typed state, refusal, count or identity. The 30- and 60-tick windows observe an absence (half a second at the default cadence). Same-tick assertions are the feature (a zero-delay solve), not incidental timing.

**Generic coverage today.** Save/Load, Reset and admission of every delivered package: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CatalogueIntegrationTests.cs:273`). Wrong-domain and wrong-direction wires: `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`. Parameter refusal does not apply (no parameter). The S4 conformance suite is drafted, not delivered.

**Chrome recipe (pending, post-catalogue).** Place Battery, Both gate, two condition sources and a supplied load; Connect battery `Supply` → gate, choose `Supply → PowerIn`; condition sources → `FirstIn` and `SecondIn` through the socket-choice buttons; gate `Supply` → load `PowerIn`. The browser cases are CAT-013's; this identity adds none.

## 6. Open questions

All resolved; these apply to all five gates (EL-133–EL-137). Needs the owner: none.

1. **Observable consumer — resolved.** The Powered gate is the consumer (CAT-013 delivery decisions, 22 October 2026; closure contract decision 3). A Motor through an Either gate is already executed (`MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`); a Motor through a Both gate stays CAT-013's own residual and is not required to close EL-133.
2. **Condition sources — resolved.** Batteries, Impact switch, State latch and Hold timer contacts, all delivered (closure contract decision 3).
3. **f32 activation and cosmetic lanes — resolved** by Story 9A.44.
4. **Finite supply — resolved.** A gate carries no power limit of its own; the source's limit and balance govern (closure contract decision 2; probe row 3).
5. **A gate wired to its own input — resolved** (coordinator, standing delegation, 21 November 2026): monotone gates are admitted and settle unpowered; nonmonotone gates are refused with the loop message. Reason: `requirements.md:6689` states one cycle rule with no one-part exception.

**Coordinator decision, 22 November 2026 (owner's standing delegation).** Coverage for a closed duplicate is the satisfying part's catalogue key plus the closure record. Reason: the binding published on main binds a package, and a closed duplicate has none.

## 7. Integration

- **Collaborators.** Battery, Impact switch, State latch, Hold timer, Counter (condition and supply sources); Powered gate and Motor (consumers); the four sibling gates. All delivered.
- **Campaign.** The logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "separately supplied electrical AND/OR/XOR/NOR/NAND", first use 21–40 (reuse 41–80, 101–150). First use stays with the level slices; the closure does not claim it. Retained gate behaviour [todo-362](../requirements.md#todo-362), per-variant rule [todo-364](../requirements.md#todo-364); processes [IX-06](../requirements.md#interaction-06) and [IX-07](../requirements.md#interaction-07).
- **Coverage binding (planned, not yet live).** The catalogue key CAT-013 (`both_gate`) and the closure record. No named binding: the package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239, binds a package, and a closed duplicate has none; the five gate identities also share the umbrella `todo-364`, and that reader refuses two identities on one task source. Any named binding is blocked on S7, which widens the reader for companion packages and non-`EL` identities (owner decision 15). The ledger row cites the part and the tests (closure contract task 3). `tools/Coverage` keeps `element-133` as a source obligation. Contract row G6 (only the `PowerIn` battery is debited) is executed once, through this gate; the provenance walk follows `PowerIn` edges only and does not read the operation (`engine/gpu/SupplyRoute.cs:77`–`:82`), so it holds for the four siblings without a second run.
- **Lesson fixture.** A level that places it uses the delivered `both_gate`. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

## 8. Delivery

- **Slice shape.** One closure review for EL-133 to EL-137: one record, one candidate commit, one approval; tests and ledger rows only.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES`; the programme comes first. Then the closure contract replanned from that revision (its "At replanning" list), checking each gate in its post-E2 form. S7 is not needed for the closure itself.
- **Files.** New `CuriousContraptions.tests/ElectricalGateClosureTests.cs` (the test project compiles every top-level `*.cs` by one rule on published main, so no project-file line is added); the five ledger entries; `sprint-status.yaml`; the five EL specs' Status rows; `delivery-batches.md`; `TODO.md`. Nothing under `elements/`, `engine/`, `tools/` or `schemas/`.
- **Review tier.** Engine tier (six layers) while probe row 6 is narrower; re-determined at replanning.
- **Disqualifiers (re-check in the slice's probe).** Any planned row failing through the delivered part (the slice stops and returns to planning; no identity closes on a missing or failing row); the prerequisite publishing with different rows S1, S2, S3 or S5; a delta appearing in the package before closure (a new parameter, port or mode on `both_gate`), other than the forward-only edit `ENGINE-CAPABILITY-INSTANCES` makes to every gate (finding E2), which the replanned contract adopts.
