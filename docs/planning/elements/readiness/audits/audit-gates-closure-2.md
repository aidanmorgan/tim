# Planning audit 2: closure review, electrical gates (EL-133 to EL-137)

Contract: `_bmad-output/implementation-artifacts/spec-closure-review-electrical-gates.md` (3,530 words), re-read in full on `8d1d68aa` in the spec-drafts checkout, 21 November 2026. Read-only; nothing was built or run. Planned tests are not executed proof.

## Verdict

**Scoped Pass, blocked only on the prerequisite `engine-supply-self-edge-cycle-rule`.**

The verdict is a planning verdict as it would stand once that slice is published. It holds under three conditions, none of which is a defect in the contract text today:

1. The prerequisite is published with rows S1, S2, S3 and S5 as drafted (`spec-engine-supply-self-edge-cycle-rule.md:46`-`:50`). Rows G4 and G5 of this contract restate exactly those rows, including the message text and the save refusal with inner reason `NonmonotoneFeedback`. If the prerequisite's own audit changes any of them, G4 and G5 must be corrected before this intent block becomes read-only.
2. At replanning from the publishing revision, probe row 6 is re-marked *present* with the repaired symbol in `engine/gpu/SupplyRoute.cs` and a named `SupplySelfEdgeTests` method, and the counts line is updated. That one row needs a short re-check; the rest of this audit carries over if the other cited inputs are unchanged.
3. At replanning the line numbers that the prerequisite shifts are refreshed: `CuriousContraptions.tests/CuriousContraptions.tests.csproj:156` (the prerequisite adds a `Compile` line at `:84`) and the `SupplyRoute.cs` lines in the "Why it is blocked" paragraph.

No blocking finding remains.

## Previous blocking findings

| Finding | State | Evidence |
|---|---|---|
| B1 self-wire refused as `InvalidConnection`; probe row 6 wrong | Resolved by blocking | Contract `:43`, `:60`-`:62`, `:91`. Row 6 is *narrower* with `SupplyRoute.cs:50`; the owning slice is named; the contract is "blocked on prerequisite". G4 and G5 (`:50`-`:51`) match prerequisite rows S1-S3 and S5, and the message matches the format string at `engine/gpu/SupplyRoute.cs:69`-`:70` for a one-part loop. |
| B2 Design Note 5 against the repair policy | Resolved | `:35`, `:159`. A failing planned test returns the slice to planning; no identity closes on a candidate with a row absent, failing or removed. |
| B3 lamp test classes and filter | Resolved | `:92`, `:185`, `:188`, `:192`, `:196`, `:200` name the five `...PresentationTests` classes, which exist (`BothGateTests.cs:252`, `ElectricalOrTests.cs:366`, `ElectricalXorTests.cs:362`, `ElectricalNorTests.cs:347`, `ElectricalNandTests.cs:342`). The filter at `:169` selects all five. |
| B4 G2 tests without a mid-Run change | Resolved | EL-135 G2 (`:190`) names `ElectricalXorTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` (`:343`). EL-133 G2 (`:182`) names the new `ABothGateOutputRetractsWhenOneConditionIsRemovedDuringRunAndReturns` (`:137`), which removes one condition while supply stays on and asserts the consumer on every read of each phase. `BothGateTests.Machine()` wires battery 2 to `SecondIn` (`BothGateTests.cs:41`, `:45`), so the expected `FirstOnly` read is right. |

## Previous non-blocking findings

- N1 (dangling "D19"): resolved at `:120`; the description decision is recorded here with its reason.
- N2 (Motor): resolved at `:119`. `MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn` exists at `MotorTests.cs:497`; a Motor through a Both gate is stated as CAT-013's residual.
- N3 (socket names): resolved at `:121`. `PortId.FirstIn`, `SecondIn`, `Supply` and `PowerIn` are at `CuriousContraptions.Abstractions/PortId.cs:25`-`:28`.
- N4 (hash claim): resolved at `:109`. `tools/Coverage/RequirementDiscovery.cs:70` hashes each requirement block and `tools/Coverage/CurrentCatalogue.cs:94` hashes the consumer section with the CAT spec text.
- N5 (Nand order test): resolved at `:198`.

## Remaining findings (non-blocking)

**R1. EL-134 G2 names an order test as the mid-Run test.** Classification: test.
- Contract `:187`. `ElectricalOrTests.PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse` (`ElectricalOrTests.cs:136`) settles the switches with the supply off and then offers the supply; no condition changes while the output is observed. The row is still proven, because its boundary `OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` and the G1/G3 test `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` both change a condition during Run.
- Fix: in the EL-134 G2, G5 row put `ElectricalOrTests.CommittedReadsFollowEveryRowAndAdmissionIsNeutral` first as the G2 test and keep the placement test as the order case, as was done for EL-135.

**R2. Count wording.** Classification: test (text).
- Contract `:78` says "Four acceptance items" and lists three kinds (self-wire, Both gate mid-Run change, identity check). `:101` and `:136` say four tests, which is correct.
- Fix: change `:78` to "Three acceptance items ... are added here as four tests".

**R3. Deferred-work line.** Classification: test (text).
- Contract `:107` keeps "`deferred-work.md` (only if a finding is deferred)". With Design Note 5 this can only mean the third-pass re-review exception, never a failed planned test.
- Fix: append "under the third-pass re-review exception only; never for a failing planned row".

## Checks that carry over unchanged from audit 1

- Duplicate decision: all five are true duplicates by a full read of each EL spec against its declaration, apart from the self-wire acceptance that the prerequisite now owns. The declarations differ only in id, name, description, palette order, paths and operation.
- Every existing test named in the table exists and asserts what the row claims. The planned tests are behavioural: real `Validate`, save decode, simulation reads and the real bundle; no hash, tolerance or fixed arrival tick.
- "Closed" changes the right records: ledger lines 1227, 1236, 1245, 1254 and 1263, sprint status 650-654, line 16 of each EL spec, `delivery-batches.md:107`. 44 packages; nothing added under `elements/`, the solution, palette or catalogue. No tool, hook or test parses the ledger sentence or the `named-el-*` keys.
- Shape (closure review) and tier (engine, re-determined at replanning) are right. One coherent slice within budget. It runs alone and publishes serially after its prerequisite.

## Owner decisions

None open. Decision 1 at `:117` (monotone self-wire admitted; repair as its own engine slice first) was the open item of audit 1 and is recorded as made by the coordinator under the standing delegation; it neither cuts scope nor waives a gate.

## Evidence limits

- The prerequisite contract was read only for rows S1-S8, its scope list and its decisions; its audit belongs to another auditor.
- The self-wire behaviour on `8d1d68aa` is established by reading `SupplyRoute.cs:50`-`:54`, not by execution.
- CAT-024 to CAT-027 specs were not read in full; EL-134, EL-136 and EL-137 were read as differences from EL-135.
- The registration-check filter and the pre-commit path were confirmed present, not exercised.
