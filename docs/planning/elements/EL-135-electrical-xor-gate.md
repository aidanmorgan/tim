# EL-135 · Electrical XOR gate — element readiness spec

**Current delivery (24 October 2026):** [EL-135 delivery slice](../invest/named-elements.md#element-135). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Shared gate facts E1–E10 are in [EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137); the catalogue harvest is [CAT-027](CAT-027-electrical_xor.md) with shared facts in [CAT-013](CAT-013-both_gate.md) and the supplied network in [CAT-005](CAT-005-battery.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-135 · Electrical XOR gate |
| Type | Electrical |
| Anchor | [requirements.md#element-135](../requirements.md#element-135); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-135); owner S407 |
| Related | Refines [CAT-027 electrical_xor](CAT-027-electrical_xor.md) ("Exactly one gate", [current-cat-027](../requirements.md#current-cat-027)). Siblings [EL-133](EL-133-electrical-and-gate.md), [EL-134](EL-134-electrical-or-gate.md), [EL-136](EL-136-electrical-nor-gate.md), [EL-137](EL-137-electrical-nand-gate.md). |
| Roadmap story | [EL-135 delivery slice](../invest/named-elements.md#element-135); canonical proof owner and prerequisite decisions are recorded there. |
| Status | open: candidate duplicate of the delivered `electrical_xor`; closes in the electrical-gates closure review. Closure review blocked on the engine programme (owner decision 3a), which comes first, in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES`; then the closure contract replanned. |
| Disposition | **Candidate Duplicate closure** of [CAT-027](CAT-027-electrical_xor.md), package `elements/ElectricalXor`, id `electrical_xor`, in the closure review `_bmad-output/implementation-artifacts/spec-closure-review-electrical-gates.md` (blocked on the engine programme, owner decision 22; its prerequisite is the self-edge row of `ENGINE-SIGNAL-NODES`, whose slice text is in `epics.md` under that story). No delta found; it is a duplicate, not a variant. This identity is the reason the self-edge rule row is a prerequisite: its own acceptance requires an own-input wire to be refused with the loop message. |
| Planned package | none — closed duplicate. The satisfying package keeps palette order 30 (`elements/ElectricalXor/ElectricalXorElementDeclaration.cs:17`). |

## 2. Declaration

The `electrical_xor` declaration, art and bindings are [CAT-027 section 2](CAT-027-electrical_xor.md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026). The delivered package differs from the Both gate's only in id, display name, description, palette order, scene path and the operation `LogicGateOperation.Xor` (file comparison, 21 November 2026); `FirstIn`/`SecondIn` are the delivered terminal names that `ENGINE-CAPABILITY-INSTANCES` replaces (finding E2, [EL-133 §2](EL-133-electrical-and-gate.md#2-declaration)), and the gate keeps exactly two declared inputs: multi-input parity is not modelled ([component research](../../component-research.md#logic): "multi-input XOR parity would make 'Exactly one' misleading").

**Variants.** None beyond the fixed operation (`Logic=Xor`, [current-cat-027](../requirements.md#current-cat-027)). Truth table (E6): 00 → 0, 01 → 1, 10 → 1, 11 → 0; output = `PowerIn` ∧ (A ≠ B). Xor is nonmonotone: a second input arriving (10 → 11) retracts the output in the same solve (E10); any zero-delay cycle through an Xor is refused at construction or Run, even through an open switch (E8).

**Equivalence check.** `ElectricalGateClosureTests.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(electrical_xor)` with the row `electrical_xor` / "Exactly one gate" / Xor taken from this spec, plus rows G1–G4, G7 and G8 passing through the delivered part.

### Behaviour and numbers

As [EL-133](EL-133-electrical-and-gate.md#behaviour-and-numbers), with operation Xor. No number of its own. The refusal text is "Break the wire loop through parts N. This gate's output cannot feed its own inputs." (`engine/gpu/SupplyRoute.cs:70`); the legacy wording ("XOR, NOR and NAND outputs cannot feed their own inputs.", `engine/ElectricalNetwork.cs@a6c914e:L116-L122`) is not current.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S407): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction). The harvest-baseline "Missing" list is delivered. Probe as [EL-133 §3](EL-133-electrical-and-gate.md#3-engine-capabilities): 7 present, 0 present-untested, 1 narrower, 0 missing. Rows exercised through this part:

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Logic node, operation Xor | present | `elements/ElectricalXor/ElectricalXorElementDeclaration.cs:35` | `ElectricalXorTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` (`ElectricalXorTests.cs:59`) |
| Nonmonotone cycle refused atomically (two-part loops, open switch, `PowerIn` entry) | present | `engine/gpu/SupplyRoute.cs:69` | `ElectricalXorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` (`:263`) |
| The same refusal for a wire from the gate to its own input | **narrower**: refused first as `InvalidConnection` with a message that does not name the loop (`engine/gpu/SupplyRoute.cs:50`–`:54`) | `CuriousContraptions.Abstractions/SupplyCycles.cs:67` unreachable | none; owner: the self-edge rule row of `ENGINE-SIGNAL-NODES` (drafted contract rows S1 and S5) |
| Same-solve retraction on reconvergence | present | `engine/gpu/SupplyRoute.cs:60`–`:74` | `ElectricalXorTests.ReconvergenceThroughAnEitherGateKeepsTheOutputOffFromTheFirstTick` (`:166`) |

**Dependencies.** Battery, Impact switch, Powered gate, the Either gate (reconvergence control): all delivered.

### Engine conformance (22 November 2026)

Checked against [engine-conformance.md](readiness/engine-conformance.md); the seven points are written out in [EL-133](EL-133-electrical-and-gate.md#engine-conformance-22-november-2026) and hold here with operation Xor. This identity has no package and compiles to nothing of its own. The satisfying package is declaration data, a scene and bindings; no engine project names it or its class (searched 22 November 2026); its behaviour is the generic supply gate rule with one declared `LogicGateOperation` value (`engine/gpu/SupplyRoute.cs:389`–`:394`). Three layers: colliders are ten primitives on one static body; visuals are those ten solids plus the flat printed pictogram, truth dots and input marks, none a collider; animation is colour bindings on the three lamps. What moves each art node: body, foot, face, sockets and printed marks, nothing; the three lamps, a binding (colour only). Flat counts, identical to EL-133 §3: bodies 1 (static); colliders 10 (box 3, sphere 7), also its ten proud visible solids; joints 0; drives 0; sensors 0; domain nodes 1 (supply gate node); stores 0; presentation bindings 9 rows. No spatial query and no dynamic body, so it needs nothing from `ENGINE-SPATIAL-QUERIES` or `ENGINE-ISLANDS-SLEEP`. Acceptance rows are scenarios on the production simulation. Verdict: **Conforms**: the self-wire refusal is owned by the self-edge rule row of `ENGINE-SIGNAL-NODES`, and finding E2 (the fixed two-terminal node) by `ENGINE-CAPABILITY-INSTANCES`; the package's unread `ElectricalNodes` ratings are CAT-027's recorded forward-only refactor and do not block the closure.

## 4. Sources and legacy

- Requirement row [element-135](../requirements.md#element-135): "Separately supplied electrical output follows exactly one input present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [current-cat-027](../requirements.md#current-cat-027): "enumerate all four rows with/without supply and output retraction. Reject zero-delay nonmonotone feedback atomically".
- Named entry [element-135](../invest/named-elements.md#element-135), owner S407; [electrical profile](../invest/profiles.md#electrical).
- Legacy: E1–E10; Xor-specific: scene `Operation = 2` (`parts/scenes/electrical_xor.tscn@a6c914e:L1-L5`); face pictogram and dots (`parts/ElectricalLogicPart.cs@a6c914e:L55-L70`; icon `ui/WorkshopIcons.cs@a6c914e:L77-L77`); reconvergent retraction (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90`), feedback rejection and open-switch case (`:L91-L129`), rejection message (`engine/ElectricalNetwork.cs@a6c914e:L116-L122`).
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/electrical_xor.tres`, `parts/scenes/electrical_xor.tscn`, `engine/LogicGate.cs`, `engine/BinaryCircuit.cs`, `engine/ElectricalNetwork.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-135](../requirements.md#element-135), [current-cat-027](../requirements.md#current-cat-027). Rows are the closure contract's AC-135 (G1–G4, G7, G8). `Closure` is the planned `ElectricalGateClosureTests`; all other tests exist in `CuriousContraptions.tests/ElectricalXorTests.cs`.

| Row | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| G1, G3 | Given switched conditions and a separate supply, when each row runs, then the consumer is powered for 01 and 10 with supply only; Reset, Save and Load are exact | `ElectricalXorTests.SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`; `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` | `ElectricalXorTests.RemovingTheSupplyOrConditionLinksUnpowersATrueRow` | rows 00 and 11 with supply |
| G2 | Given a running gate, when conditions change through five phases, then the output follows on the committed tick; 10 → 11 through an upstream Either gate keeps the output off from the first tick | `ElectricalXorTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` (`:343`) | its 00 and 11 phases are off | `ReconvergenceThroughAnEitherGateKeepsTheOutputOffFromTheFirstTick`; `PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse` |
| G4 | Given the gate's `Supply` wired to its own `FirstIn` or `SecondIn` (carried by a construction or a save; the connect tool refuses a same-part target, `ui/WorkshopConnections.cs:106`), when it is validated or loaded, then it is refused before any solve with `NonmonotoneFeedback` and the loop message, and the construction is unchanged; a save holding the wire is refused `InvalidConnection` with that inner reason | `Closure.ANonmonotoneGateWiredToItsOwnInputIsRefusedAtomically(electrical_xor, FirstIn/SecondIn)` (planned; needs the self-edge rule row of `ENGINE-SIGNAL-NODES`; input names as replanned after `ENGINE-CAPABILITY-INSTANCES`) | the same parts without the wire validate | `ElectricalXorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` (two-part loops, open switch, `PowerIn` entry) |
| G7, G8 | Given the bundle and the lamps, then the entry is "Exactly one gate" / Control / Xor with four electrical ports and no parameter, and lamps and two truth dots follow committed rows through Reset | `Closure.EachGateIdentityIsItsOwnCatalogueEntryWithItsDeclaredOperation(electrical_xor)` (planned); `ElectricalXorPresentationTests.PictogramTwoTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` | `CommittedReadsFollowEveryRowAndAdmissionIsNeutral` | Reset to slate |

**Correction to the earlier outline.** It said an own-input wire "is refused in build mode with the short loop message on the bottom status line". The Workbench connect tool cannot create a self-wire, so the status-line case exists only for a two-part loop (`PalettePlacementTests.ExactlyOneGateChooserRefusesALoopThroughTheGateAndAdmitsAnOrLoopBeside`). The one-part refusal and its message are asserted natively through the construction and save paths (G4).

No tolerance is introduced. Generic coverage today: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`; `PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`. No parameter, so no parameter refusal.

The requirement phrase "A/B inputs carry conditions, not output energy" is proven for this operation by its true-row-without-supply test; the finite-debit form (closure contract row G6) is executed once, through the Both gate. The provenance walk follows `PowerIn` edges only and does not read the operation (`engine/gpu/SupplyRoute.cs:77`–`:82`).

**Chrome recipe (pending, post-catalogue).** Battery → gate `PowerIn`; conditions → `FirstIn`/`SecondIn`; gate `Supply` → supplied load. CAT-027's browser cases cover it; no browser case is queued for G4.

## 6. Open questions

None open; needs the owner: none. Shared questions are resolved in [EL-133 §6](EL-133-electrical-and-gate.md#6-open-questions). For this gate the self-wire decision reads: refused with the loop message, by the one cycle rule (`requirements.md:6689`).

## 7. Integration

Collaborators (all delivered): Battery, Impact switch, Powered gate, Either gate ([EL-134](EL-134-electrical-or-gate.md)) for the reconvergence control. Campaign: logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 21–40 (reuse 41–80, 101–150); first use stays with the level slices. Retained behaviour [todo-362](../requirements.md#todo-362), [todo-364](../requirements.md#todo-364); processes [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07). A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

- **Coverage binding (planned, not yet live).** Catalogue key CAT-027 (`electrical_xor`) and the closure record. No named binding: the package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239, binds a package and a closed duplicate has none; the five gates also share `todo-364`, and the reader refuses two identities on one task source (coordinator decision, 22 November 2026, under the owner's standing delegation). Any named binding is blocked on S7, which widens the reader for companion packages and non-`EL` identities (owner decision 15).

## 8. Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` and the self-edge rule row of `ENGINE-SIGNAL-NODES` (drafted contract rows S1 and S5); the programme comes first. Then the replanned closure contract, which checks this gate in its post-E2 form.

Closed in the one closure review described in [EL-133 §8](EL-133-electrical-and-gate.md#8-delivery): tests and ledger rows only; engine tier while the self-wire capability is narrower. Disqualifiers: row G4 failing after the prerequisite publishes (the slice returns to planning); a parameter, port or mode added to `electrical_xor` before closure, other than the forward-only edit of `ENGINE-CAPABILITY-INSTANCES` (finding E2).
