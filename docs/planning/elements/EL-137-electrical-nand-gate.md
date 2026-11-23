# EL-137 · Electrical NAND gate — element readiness spec

**Current delivery (24 October 2026):** [EL-137 delivery slice](../invest/named-elements.md#element-137). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Shared gate facts E1–E10 are in [EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137); the catalogue harvest is [CAT-024](CAT-024-electrical_nand.md) with shared facts in [CAT-013](CAT-013-both_gate.md) and the supplied network in [CAT-005](CAT-005-battery.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-137 · Electrical NAND gate |
| Type | Electrical |
| Anchor | [requirements.md#element-137](../requirements.md#element-137); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-137); owner S409 |
| Related | Refines [CAT-024 electrical_nand](CAT-024-electrical_nand.md) ("Not both gate", [current-cat-024](../requirements.md#current-cat-024)). Siblings [EL-133](EL-133-electrical-and-gate.md), [EL-134](EL-134-electrical-or-gate.md), [EL-135](EL-135-electrical-xor-gate.md), [EL-136](EL-136-electrical-nor-gate.md). |
| Roadmap story | [EL-137 delivery slice](../invest/named-elements.md#element-137); canonical proof owner and prerequisite decisions are recorded there. |
| Status | open: candidate duplicate of the delivered `electrical_nand`; closes in the electrical-gates closure review. Closure review blocked on the engine programme (owner decision 3a), which comes first, in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES`; then the closure contract replanned. |
| Disposition | **Candidate Duplicate closure** of [CAT-024](CAT-024-electrical_nand.md), package `elements/ElectricalNand`, id `electrical_nand`, in the closure review `_bmad-output/implementation-artifacts/spec-closure-review-electrical-gates.md` (blocked on the engine programme, owner decision 22; its prerequisite is the self-edge row of `ENGINE-SIGNAL-NODES`, whose slice text is in `epics.md` under that story). No delta found; it is a duplicate, not a variant. |
| Planned package | none — closed duplicate. The satisfying package keeps palette order 27 (`elements/ElectricalNand/ElectricalNandElementDeclaration.cs:17`). |

## 2. Declaration

The `electrical_nand` declaration, art and bindings are [CAT-024 section 2](CAT-024-electrical_nand.md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026). The delivered package differs from the Both gate's only in id, display name, description, palette order, scene path and the operation `LogicGateOperation.Nand` (file comparison, 21 November 2026); `FirstIn`/`SecondIn` are the delivered terminal names that `ENGINE-CAPABILITY-INSTANCES` replaces (finding E2, [EL-133 §2](EL-133-electrical-and-gate.md#2-declaration)). Output energy comes only from `PowerIn` (E4): the true no-input rows still need the independent supply.

**Variants.** None beyond the fixed operation (`Logic=Nand`, [current-cat-024](../requirements.md#current-cat-024)). Truth table (E6): 00 → 1, 01 → 1, 10 → 1, 11 → 0; output = `PowerIn` ∧ ¬(A ∧ B). Nonmonotone: the second input arriving (10 → 11) retracts the output in the same solve (E10); any zero-delay cycle through a Nand is refused, even through an open switched route (E8).

**Equivalence check.** `ElectricalGateClosureTests.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(electrical_nand)` with the row `electrical_nand` / "Not both gate" / Nand taken from this spec, plus rows G1–G4, G7 and G8 passing through the delivered part.

### Behaviour and numbers

As [EL-133](EL-133-electrical-and-gate.md#behaviour-and-numbers), with operation Nand. No number of its own. Refusal text as [EL-135](EL-135-electrical-xor-gate.md#behaviour-and-numbers) (`engine/gpu/SupplyRoute.cs:70`); a long loop names the first parts and counts the rest.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S409): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction). The harvest-baseline "Missing" list is delivered. Probe as [EL-133 §3](EL-133-electrical-and-gate.md#3-engine-capabilities): 7 present, 0 present-untested, 1 narrower, 0 missing. Rows exercised through this part:

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Logic node, operation Nand | present | `elements/ElectricalNand/ElectricalNandElementDeclaration.cs:35` | `ElectricalNandTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` (`ElectricalNandTests.cs:55`) |
| Nonmonotone cycle refused atomically; long loop message | present | `engine/gpu/SupplyRoute.cs:69`–`:70` | `ElectricalNandTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` (`:220`); `LongLoopRefusalNamesTheFirstPartsAndCountsTheRest` (`:250`) |
| The same refusal for a wire from the gate to its own input | **narrower** (`engine/gpu/SupplyRoute.cs:50`–`:54` refuses it first as `InvalidConnection`) | `CuriousContraptions.Abstractions/SupplyCycles.cs:67` unreachable | none; owner: the self-edge rule row of `ENGINE-SIGNAL-NODES` |
| Same-tick retraction on reconvergence | present | `engine/gpu/SupplyRoute.cs:60`–`:74` | `ElectricalNandTests.ReconvergentConditionRetractsOutputOnTheSameTick` (`:142`) |

**Dependencies.** Battery, Impact switch, Powered gate, the Either gate (reconvergence control): all delivered.

### Engine conformance (22 November 2026)

Checked against [engine-conformance.md](readiness/engine-conformance.md); the seven points are written out in [EL-133](EL-133-electrical-and-gate.md#engine-conformance-22-november-2026) and hold here with operation Nand. This identity has no package and compiles to nothing of its own. The satisfying package is declaration data, a scene and bindings; no engine project names it or its class (searched 22 November 2026); its behaviour is the generic supply gate rule with one declared `LogicGateOperation` value (`engine/gpu/SupplyRoute.cs:389`–`:394`). Three layers: colliders are ten primitives on one static body; visuals are those ten solids plus the flat printed pictogram, truth dots and input marks, none a collider; animation is colour bindings on the three lamps. What moves each art node: body, foot, face, sockets and printed marks, nothing; the three lamps, a binding (colour only). Flat counts, identical to EL-133 §3: bodies 1 (static); colliders 10 (box 3, sphere 7), also its ten proud visible solids; joints 0; drives 0; sensors 0; domain nodes 1 (supply gate node); stores 0; presentation bindings 9 rows. No spatial query and no dynamic body, so it needs nothing from `ENGINE-SPATIAL-QUERIES` or `ENGINE-ISLANDS-SLEEP`. Acceptance rows are scenarios on the production simulation. Verdict: **Conforms**: the self-wire refusal is owned by the self-edge rule row of `ENGINE-SIGNAL-NODES`, and finding E2 (the fixed two-terminal node) by `ENGINE-CAPABILITY-INSTANCES`; the package's unread `ElectricalNodes` ratings are CAT-024's recorded forward-only refactor and do not block the closure.

## 4. Sources and legacy

- Requirement row [element-137](../requirements.md#element-137): "Separately supplied electrical output follows not both inputs present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [current-cat-024](../requirements.md#current-cat-024): "A true no-input condition cannot create power. Reject zero-delay nonmonotone feedback atomically even through open switched routes; test reordering/reconvergence".
- Named entry [element-137](../invest/named-elements.md#element-137), owner S409; [electrical profile](../invest/profiles.md#electrical); recipe "Overflow Interlock" warns against deriving a command from the gate's own output ([component research](../../component-research.md#logic)).
- Legacy: E1–E10; Nand-specific: scene `Operation = 4` (`parts/scenes/electrical_nand.tscn@a6c914e:L1-L5`); pictogram and dots (`parts/ElectricalLogicPart.cs@a6c914e:L55-L70`; icon `ui/WorkshopIcons.cs@a6c914e:L79-L79`); reconvergent retraction (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90`), feedback rejection (`:L91-L109`), message (`engine/ElectricalNetwork.cs@a6c914e:L116-L122`).
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/electrical_nand.tres`, `parts/scenes/electrical_nand.tscn`, `engine/LogicGate.cs`, `engine/ElectricalNetwork.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-137](../requirements.md#element-137), [current-cat-024](../requirements.md#current-cat-024). Rows are the closure contract's AC-137 (G1–G4, G7, G8). `Closure` is the planned `ElectricalGateClosureTests`; all other tests exist in `CuriousContraptions.tests/ElectricalNandTests.cs`.

| Row | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| G1, G3 | Given switched conditions and a separate supply, when each row runs, then the consumer is powered for 00, 01, 10 with supply only; Reset, Save and Load are exact | `ElectricalNandTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`; `ClosingBothSwitchesRetractsTheOutputAndSaveLoadAndResetAreExact` | `ElectricalNandTests.TrueRowWithoutSupplyWireNeverCreatesPower` | row 11 with supply |
| G2 | Given a running gate, when the second input arrives through an upstream Either gate (10 → 11), then the output retracts on the same tick; order does not matter | `ElectricalNandTests.ReconvergentConditionRetractsOutputOnTheSameTick` | `ElectricalNandTests.MonotoneLoopIsAdmittedStaysUnpoweredAndSettlesBeforeTheGateItFeeds` | `PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse` covers row 11 in four orders; the other rows' order independence rests on `CommittedReadsFollowEveryRowAndAdmissionIsNeutral` |
| G4 | Given the gate's `Supply` wired to its own `FirstIn` or `SecondIn` (by construction or save; the connect tool refuses a same-part target, `ui/WorkshopConnections.cs:106`), when validated or loaded, then refused before any solve with `NonmonotoneFeedback` and the loop message; construction unchanged; the save is refused `InvalidConnection` with that inner reason | `Closure.ANonmonotoneGateWiredToItsOwnInputIsRefusedAtomically(electrical_nand, FirstIn/SecondIn)` (planned; needs the self-edge rule row of `ENGINE-SIGNAL-NODES`; input names as replanned after `ENGINE-CAPABILITY-INSTANCES`) | the same parts without the wire validate | `ElectricalNandTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve`; `LongLoopRefusalNamesTheFirstPartsAndCountsTheRest` |
| G7, G8 | Given the bundle and the lamps, then the entry is "Not both gate" / Control / Nand with four electrical ports and no parameter, and lamps and three truth dots follow committed rows through Reset | `Closure.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(electrical_nand)` (planned); `ElectricalNandPresentationTests.PictogramTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` | `ElectricalNandTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | Reset to slate |

The earlier outline's "refused in build mode" for a Nand loop holds for a two-part loop (`PalettePlacementTests.NotBothGateChooserRefusesALoopClosingWireOnTheStatusLine`); the one-part case is native only, as in [EL-135 §5](EL-135-electrical-xor-gate.md#5-acceptance-outline).

No tolerance is introduced. Generic coverage today: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`; `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`. No parameter, so no parameter refusal.

The requirement phrase "A/B inputs carry conditions, not output energy" is proven for this operation by its true-row-without-supply test; the finite-debit form (closure contract row G6) is executed once, through the Both gate. The provenance walk follows `PowerIn` edges only and does not read the operation (`engine/gpu/SupplyRoute.cs:77`–`:82`).

**Chrome recipe (pending, post-catalogue).** Battery → gate `PowerIn`; conditions → `FirstIn`/`SecondIn`; gate `Supply` → supplied load. CAT-024's browser cases cover it.

## 6. Open questions

None open; needs the owner: none. Shared questions are resolved in [EL-133 §6](EL-133-electrical-and-gate.md#6-open-questions); the self-wire is refused with the loop message.

## 7. Integration

Collaborators (all delivered): Battery, Impact switch, Powered gate, Either gate ([EL-134](EL-134-electrical-or-gate.md)) for the reconvergence control. Campaign: logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 21–40 (reuse 41–80, 101–150). Retained behaviour [todo-362](../requirements.md#todo-362), [todo-364](../requirements.md#todo-364); processes [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07). A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

- **Coverage binding (planned, not yet live).** Catalogue key CAT-024 (`electrical_nand`) and the closure record. No named binding: the package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239, binds a package and a closed duplicate has none; the five gates also share `todo-364`, and the reader refuses two identities on one task source (coordinator decision, 22 November 2026, under the owner's standing delegation). Any named binding is blocked on S7, which widens the reader for companion packages and non-`EL` identities (owner decision 15).

## 8. Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES`; the programme comes first. Then the replanned closure contract, which checks this gate in its post-E2 form.

Closed in the one closure review described in [EL-133 §8](EL-133-electrical-and-gate.md#8-delivery): tests and ledger rows only; engine tier while the self-wire capability is narrower. Disqualifiers: row G4 failing after the prerequisite publishes; a parameter, port or mode added to `electrical_nand` before closure, other than the forward-only edit of `ENGINE-CAPABILITY-INSTANCES` (finding E2).
