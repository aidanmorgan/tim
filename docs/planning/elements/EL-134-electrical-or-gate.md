# EL-134 · Electrical OR gate — element readiness spec

**Current delivery (24 October 2026):** [EL-134 delivery slice](../invest/named-elements.md#element-134). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Shared gate facts E1–E10 are in [EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137); the catalogue harvest is [CAT-026](CAT-026-electrical_or.md) with shared facts in [CAT-013](CAT-013-both_gate.md) and the supplied network in [CAT-005](CAT-005-battery.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-134 · Electrical OR gate |
| Type | Electrical |
| Anchor | [requirements.md#element-134](../requirements.md#element-134); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-134); owner S406 |
| Related | Refines [CAT-026 electrical_or](CAT-026-electrical_or.md) ("Either gate", [current-cat-026](../requirements.md#current-cat-026)). Siblings [EL-133](EL-133-electrical-and-gate.md), [EL-135](EL-135-electrical-xor-gate.md), [EL-136](EL-136-electrical-nor-gate.md), [EL-137](EL-137-electrical-nand-gate.md). |
| Roadmap story | [EL-134 delivery slice](../invest/named-elements.md#element-134); canonical proof owner and prerequisite decisions are recorded there. |
| Status | open: candidate duplicate of the delivered `electrical_or`; closes in the electrical-gates closure review. Closure review blocked on the engine programme (owner decision 3a), which comes first, in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES`; then the closure contract replanned. |
| Disposition | **Candidate Duplicate closure** of [CAT-026](CAT-026-electrical_or.md), package `elements/ElectricalOr`, id `electrical_or`, in the closure review `_bmad-output/implementation-artifacts/spec-closure-review-electrical-gates.md` (blocked on the engine programme, owner decision 22; its prerequisite is the self-edge row of `ENGINE-SIGNAL-NODES`, whose slice text is in `epics.md` under that story). No delta found; it is a duplicate, not a variant. |
| Planned package | none — closed duplicate. The satisfying package keeps palette order 29 (`elements/ElectricalOr/ElectricalOrElementDeclaration.cs:17`). |

## 2. Declaration

The `electrical_or` declaration, art and bindings are [CAT-026 section 2](CAT-026-electrical_or.md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026). The delivered package differs from the Both gate's only in id, display name, description, palette order, scene path and the operation `LogicGateOperation.Or` (file comparison with `elements/BothGate/BothGateElementDeclaration.cs`, 21 November 2026); `FirstIn`/`SecondIn` are the delivered terminal names that `ENGINE-CAPABILITY-INSTANCES` replaces (finding E2, [EL-133 §2](EL-133-electrical-and-gate.md#2-declaration)).

**Variants.** None beyond the fixed operation (`Logic=Or`, [current-cat-026](../requirements.md#current-cat-026)). Truth table (E6): 00 → 0, 01 → 1, 10 → 1, 11 → 1; output = `PowerIn` ∧ (A ∨ B). Or is monotone: an Or cycle is admitted and settles from the unpowered least fixed point each solve (E8, E9).

**Equivalence check.** `ElectricalGateClosureTests.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(electrical_or)` with the row `electrical_or` / "Either gate" / Or taken from this spec, plus rows G1–G3, G5, G7 and G8 passing through the delivered part.

### Behaviour and numbers

As [EL-133](EL-133-electrical-and-gate.md#behaviour-and-numbers), with operation Or. No number of its own. An Or loop holds only while a source feeds it and forgets a removed source on the next committed tick.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S406): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction). The harvest-baseline "Missing" list (Stories 8.1, 9.6, 9.8) is delivered. The probe is [EL-133 §3](EL-133-electrical-and-gate.md#3-engine-capabilities): 7 present, 0 present-untested, 1 narrower (a self-wire is refused as `InvalidConnection` before the cycle rule, `engine/gpu/SupplyRoute.cs:50`–`:54`; owner: the self-edge rule row of `ENGINE-SIGNAL-NODES`), 0 missing. Rows exercised through this part:

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Logic node, operation Or | present | `elements/ElectricalOr/ElectricalOrElementDeclaration.cs:35` | `ElectricalOrTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` (`ElectricalOrTests.cs:60`) |
| Monotone cycle settles from unpowered | present | `engine/gpu/SupplyRoute.cs:60` | `ElectricalOrTests.OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` (`:155`) |
| Typed monotonicity of the operation | present | `CuriousContraptions.Abstractions/SupplyCycles.cs:13` | `ElectricalOrTests.LawMonotonicityAndUndefinedValuesAreClosed` (`:308`) |

**Dependencies.** Battery, Impact switch, Powered gate and Motor: all delivered.

### Engine conformance (22 November 2026)

Checked against [engine-conformance.md](readiness/engine-conformance.md); the seven points are written out in [EL-133](EL-133-electrical-and-gate.md#engine-conformance-22-november-2026) and hold here with operation Or. This identity has no package and compiles to nothing of its own. The satisfying package is declaration data, a scene and bindings; no engine project names it or its class (searched 22 November 2026); its behaviour is the generic supply gate rule with one declared `LogicGateOperation` value (`engine/gpu/SupplyRoute.cs:389`–`:394`). Three layers: colliders are ten primitives on one static body; visuals are those ten solids plus the flat printed pictogram, truth dots and input marks, none a collider; animation is colour bindings on the three lamps. What moves each art node: body, foot, face, sockets and printed marks, nothing; the three lamps, a binding (colour only). Flat counts, identical to EL-133 §3: bodies 1 (static); colliders 10 (box 3, sphere 7), also its ten proud visible solids; joints 0; drives 0; sensors 0; domain nodes 1 (supply gate node); stores 0; presentation bindings 9 rows. No spatial query and no dynamic body, so it needs nothing from `ENGINE-SPATIAL-QUERIES` or `ENGINE-ISLANDS-SLEEP`. Acceptance rows are scenarios on the production simulation. Verdict: **Conforms**: the self-wire capability is owned by the self-edge rule row of `ENGINE-SIGNAL-NODES`, and finding E2 (the fixed two-terminal node) by `ENGINE-CAPABILITY-INSTANCES`; the package's unread `ElectricalNodes` ratings are CAT-026's recorded forward-only refactor and do not block the closure.

## 4. Sources and legacy

- Requirement row [element-134](../requirements.md#element-134): "Separately supplied electrical output follows at least one input present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [current-cat-026](../requirements.md#current-cat-026): "And/Or strongly connected components restart from unpowered least fixed point each solve. Test source-free loop, missing supply, reorder/reconvergence".
- Named entry [element-134](../invest/named-elements.md#element-134), owner S406; [electrical profile](../invest/profiles.md#electrical).
- Legacy: E1–E10 ([EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137)); Or-specific: scene `Operation = 1` (`parts/scenes/electrical_or.tscn@a6c914e:L1-L5`), face pictogram and truth-row dots for rows 01, 10, 11 at x −0.07, 0.07, 0.21, y −0.4 (`parts/ElectricalLogicPart.cs@a6c914e:L55-L70`; icon `ui/WorkshopIcons.cs@a6c914e:L76-L76`); Or participates in monotone-feedback tests (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L131-L157`) and as the upstream gate in the reconvergence test (`:L63-L90`).
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/electrical_or.tres`, `parts/scenes/electrical_or.tscn`, `engine/LogicGate.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-134](../requirements.md#element-134), [current-cat-026](../requirements.md#current-cat-026). Rows are the closure contract's AC-134 (G1–G3, G5, G7, G8). `Closure` is the planned `ElectricalGateClosureTests`; all other tests exist in `CuriousContraptions.tests/ElectricalOrTests.cs`.

| Row | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| G1, G3 | Given switched conditions and a separate supply, when each row runs, then the consumer is powered for 01, 10, 11 with supply only; Reset, Save and Load are exact | `ElectricalOrTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`; `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` | `ElectricalOrTests.TrueRowWithoutSupplyWireNeverCreatesPower` | row 00 with supply |
| G2 | Given a running gate, when conditions change and parts or wires are reordered, then the output follows on the committed tick and order does not matter | `ElectricalOrTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | source-free loop unpowered | `ElectricalOrTests.PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse`; `OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` (forgets a removed source) |
| G5 | Given `Supply` wired to its own `FirstIn` (carried by a construction or a save; the connect tool refuses a same-part target, `ui/WorkshopConnections.cs:106`), when its other input turns on and off, then the wire is admitted, the gate is on only while the other input is on, and off on the next committed tick after | `Closure.AMonotoneGateWiredToItsOwnInputIsAdmittedAndNeverPowersItself(electrical_or)` (planned; needs the self-edge rule row of `ENGINE-SIGNAL-NODES`) | first 60 ticks unpowered | off again within one committed tick |
| G7, G8 | Given the bundle and the lamps, then the entry is "Either gate" / Control / Or with four electrical ports and no parameter, and lamps and truth dots follow committed rows through Reset | `Closure.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(electrical_or)` (planned); `ElectricalOrPresentationTests.PictogramThreeTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` | `ElectricalOrTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | Reset to slate |

The requirement phrase "A/B inputs carry conditions, not output energy" is proven for this operation by `TrueRowWithoutSupplyWireNeverCreatesPower`; the finite-debit form (the contract's G6) is executed only through the Both gate, because the provenance law does not read the operation (`engine/gpu/SupplyRoute.cs:77`). See the batch summary for the proposed contract wording.

No tolerance is introduced (typed states, refusals, identities); 60-tick windows observe an absence. Generic coverage today: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`; `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`; the loop refusal on the status line is `PalettePlacementTests.EitherGateChooserAdmitsAnOrLoopAndRefusesAMixedOne`. No parameter, so no parameter refusal.

**Chrome recipe (pending, post-catalogue).** Battery → gate `PowerIn`; condition sources → `FirstIn`/`SecondIn` via the socket-choice buttons; gate `Supply` → supplied load. CAT-026's browser cases cover it.

## 6. Open questions

None open; needs the owner: none. The five shared questions are resolved in [EL-133 §6](EL-133-electrical-and-gate.md#6-open-questions) (consumer: Powered gate; condition sources: delivered contacts and batteries; f32 lanes; no power limit of its own; self-wire admitted for a monotone gate).

## 7. Integration

Collaborators (all delivered): Battery, Impact switch, State latch, Hold timer, Powered gate, Motor (`MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`), and the Xor/Nand gates that use it for the reconvergence control. Campaign: logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 21–40 (reuse 41–80, 101–150); first use stays with the level slices. Retained behaviour [todo-362](../requirements.md#todo-362), [todo-364](../requirements.md#todo-364); processes [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07). A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

- **Coverage binding (planned, not yet live).** Catalogue key CAT-026 (`electrical_or`) and the closure record. No named binding: the package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239, binds a package and a closed duplicate has none; the five gates also share `todo-364`, and the reader refuses two identities on one task source (coordinator decision, 22 November 2026, under the owner's standing delegation). Any named binding is blocked on S7, which widens the reader for companion packages and non-`EL` identities (owner decision 15).

## 8. Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES`; the programme comes first. Then the replanned closure contract, which checks this gate in its post-E2 form.

Closed in the one closure review described in [EL-133 §8](EL-133-electrical-and-gate.md#8-delivery): tests and ledger rows only; engine tier while the self-wire capability is narrower. Disqualifiers: a planned row failing through `electrical_or`; a parameter, port or mode added to the package before closure, other than the forward-only edit of `ENGINE-CAPABILITY-INSTANCES` (finding E2).
