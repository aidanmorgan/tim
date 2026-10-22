---
title: "9.7a — Electrical NAND Gate"
status: review
baseline: 9b324bbd6a161858b9342562cd0a572d67f9e62a
context:
  - TODO.md
  - docs/planning/elements/CAT-024-electrical_nand.md
  - docs/planning/elements/CAT-013-both_gate.md
  - docs/architecture/element-package-isolation.md
  - docs/gpu-f32-physics.md
---

# Outcome and authority

Ship the package-owned "Not both gate" (CAT-024, EL-137).

- Authority: Story 9.7a AC, CAT-024 D1–D11 and §5, current-cat-024 and todo-362, and CAT-013 G1–G15 as they apply to Nand.
- Output = supplied ∧ ¬(first ∧ second). Row 00 is logically true but still needs real PowerIn energy. Conditions never lend energy (9.6 rule, D5).
- Scope: add `Nand` to the closed `LogicGateOperation`, plus its law and package. No Nor/Or/Xor law or package.
- Deletes: no element record. Replaces the unordered monotone sweep in `CompiledSupplyGraph.Resolve` (D2).

# Finite design decisions

- **D1 Feedback: typed refusal (option a).**
  - Rule: any directed electrical cycle that contains a nonmonotone gate is refused atomically. This covers every socket, including PowerIn. Detection runs over the potential topology, so open contacts count (CAT-005 N5).
  - Sources (they agree): CAT-013 G5/G6, CAT-024 D7–D9, CAT-025 R6–R8, CAT-026 O10, CAT-027 X8–X10, requirements:598 ("even through open switched routes"), DESIGN.md "Supplied electrical logic". No owner question.
  - Previous-tick reads (b) are ruled out: "reject" is the stated rule, and those reads would add hidden snapshot state. Bounded or faulting iteration is ruled out.
  - Where the refusal fires (traced): the check sits in `WorkshopConnections.Validate`, which is the single typed path for connection rules.
    - Every build-mode wiring edit runs `next.Validate()` before `PushUndo` (`ui/WorkshopConnections.cs` ~111). `WorkshopUiFailure.Report` then shows the refusal on the existing bottom status line, with construction and Undo unchanged.
    - Save prevalidation runs the same check (`WorkshopSavePrevalidation` ~247), so a save that holds a loop is refused at load with `WorkshopSaveRejection.InvalidConnection`.
  - It must not be a compiler-only throw. The compiler runs inside device `Admit` during `WorkshopSimulation.Construct`, and its generic catch (~206) turns any exception into an `InvalidRead` fault. That is untyped and is not a refusal.
  - "At construction" (G5/D8) and "at Start" (X8) both mean before any solve, in build mode, with the machine unchanged. Refusing at the edit is the earliest point in the current architecture that meets both.
  - *Playability:* a loop through a NAND can only flicker at the tick rate. The State Latch already gives players memory, so refusing the closing wire on the spot, with the parts named, teaches more than a buzzing gate.
- **D2 Evaluation order.**
  - The `CompiledSupplyGraph` constructor builds the strongly-connected-component condensation once, in topological order.
  - `Resolve` visits the components in order. Inside a cyclic component, which can hold only monotone nodes, it iterates to the least fixed point. Each NAND is a singleton evaluated after its inputs settle.
  - Why: the current `if (powered[i]) continue` sweep is wrong under negation even without a cycle. A NAND whose slot comes before its switch-fed input latches on, and row 11 fails.
  - The constructor rejects a nonmonotone node in a cyclic component (`ArgumentException`), as an invariant only.
- **D3 Monotonicity.** An exhaustive switch over `LogicGateOperation` (And → monotone, Nand → nonmonotone); an undefined value throws. No PartId or string classification.
- **D4 Enum value.** Append `Nand = 1`. Legacy scene integer 4 is not carried forward. No wire or schema change: `ElectricalLogicRecord` has no operation field.
- **D5 Inputs and consumer.** Inputs are "power switches": Battery → ImpactSwitch (CAT-063) PowerIn→Supply contacts from 9.5b. This resolves CAT-024 open question 1. The consumer is Powered Gate (CAT-051).
- **D6 Or substitute.** D6/D7 use Or, which is not delivered yet. Substitute Both Gate for the reconvergence and loop controls. Or chaining is pending under 9.8a.

# Tasks

- [x] Enum and law:
  - Add `LogicGateOperation.Nand`.
  - Add the Nand arm to `CompiledSupplyGraph.Evaluate`.
  - Add the D3 predicate.
  - Confirm the undefined pins `(LogicGateOperation)2` (BundleValidatorTests ~90) and `255` (BothGateTests ~169) stay undefined.
- [x] Graph:
  - Write a static cycle check in `SupplyRoute.cs`: owner → declared operation, plus electrical edges, using Tarjan SCC.
  - Call it from `WorkshopConnections.Validate(partOf)` using `DeclaredPhysics.Of(part).Electrical?.Supply?.Operation`. It throws `WorkshopEditRejection.NonmonotoneFeedback` with the short G6 text naming the cycle's ids; it must fit the status line.
  - Reuse the same check for the D2 constructor invariant.
  - Implement the ordered `Resolve`.
- [x] Package `elements/ElectricalNand/`:
  - `ElectricalNandElementDeclaration`: id `electrical_nand`, "Not both gate", Control, colour (.4,.72,.79), the CAT-024 description.
  - Palette ordinal: the next free one after Both Gate's 26. Verify it is unique after the C-Ramp merge.
  - The Both Gate's four ports, `logic` node and nine lamp bindings, with `Operation: Nand`.
  - Scene: Both Gate body, foot, face, marks and lamps; no converging bars; the navy NAND pictogram (`WorkshopIcons.cs@a6c914e:L79`); gold truth dots at x −.21/−.07/.07, y −.4, z .42.
  - icon.svg, csproj, slnx and bundle membership.
  - No record type and no runtime PartId branch.
- [x] Count and membership pins, enumerated exactly (each is the post-C-Ramp value +1):
  - PartRegistry definitions count.
  - `SaveFixture.Elements` (BundleElementIdTests).
  - WorkbenchCapacity kinds.
  - PalettePlacement.
  - PackageRuntimeParity.
  - The generic chooser.
- [x] Explicit cleanup: delete the unordered sweep and its "powered only once" comment. No parallel path, fallback or compatibility reader. Do not enlarge the isolation Known list.
- [x] Latest-design alignment:
  - The package owns identity and art; the shared typed engine owns the law. f32 SmoothStep .1s is unchanged. No unrelated 9A.44 work.
  - Update CAT-024 status and open question 1, and TODO.md.
- [x] Production C# UNIT tests are PRIMARY (`ElectricalNandTests.cs`), through the real compiler, worker device and Powered Gate motion:
  - Truth table: 00/01/10/11 × supply present/absent through real ImpactSwitch contacts, checking consumer displacement or velocity. Row 00 without supply is unpowered (negative control). Battery-enable rows are supplemental only.
  - Order: the NAND instance placed before its switch-fed input (this exposes the D2 defect), the reverse order, and swapped link order all give identical results.
  - Reconvergence: battery → NAND FirstIn, and battery → Both Gate → NAND SecondIn. The output retracts within the same tick and stays retracted.
  - Feedback, each a typed `NonmonotoneFeedback` that names its ids, with no Undo push and no construction change:
    - NAND→NAND.
    - NAND→Both Gate→NAND.
    - A loop closed only through an open ImpactSwitch.
    - A loop entering via PowerIn.
  - Saves: a looped save is refused with `InvalidConnection` and nothing is constructed. The compiled-graph invariant throws.
  - Monotone control: a source-free Both Gate loop is admitted and stays unpowered.
  - Lamps: committed reads per row, and tick-0 neutral admission.
  - Run/Reset is exact against the 9R.1 snapshot.
  - Save/load round-trips typed sockets.
  - Undefined operation and undefined socket are rejected.
- [x] 9A.45a ratchet passes with no new entry.
- [x] Build the affected closure once, then reuse it with `dotnet test --no-build --no-restore` and filtered classes. Projects: native tests, `tools/BundleElements.Tests`, the `CuriousContraptions.Simulation` worker. Run strict TS only if a TS input changes. Rebuild only when relevant inputs change.
- [ ] An independent reviewer gives exact SnapshotApproval → protected commit/push → verification of remote/tree identity, exact-SHA CI native success, the Pages artifact and the deployment → terminal `review-9-7a-electrical-nand.md`.

# Pending browser cases (post-catalogue, `tools/e2e/cat-024.test.ts`)

Real-UI construction: Battery to the bottom socket via the icon choice, switches to the one-mark and two-mark sockets, gate to Powered Gate. Cases:

- Four rows with supply, and 00 without supply.
- The lamp slate/gold pair.
- The pictogram and three dots at projected size.
- Reconvergence retraction.
- A loop-closing wire, including one through an open switch, is refused on the status line.
- Reordered placement and wiring.
- Run/Reset and Save/Load.
- Or chaining (after 9.8a).

# Code Map (line refs approximate)

Change:

| File | Symbols |
| --- | --- |
| `CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs` | `LogicGateOperation` |
| `engine/gpu/SupplyRoute.cs` | cycle check, `CompiledSupplyGraph` ctor, `Resolve`, `Evaluate` |
| `engine/gpu/WorkshopConnections.cs` | `Validate(partOf)` |
| `engine/gpu/WorkshopEditRejectedException.cs` | `WorkshopEditRejection` (C#-only, not on the wire) |
| Membership | new `elements/ElectricalNand/*`, `CuriousContraptions.slnx`, `CuriousContraptions.tests.csproj`, the pins above |
| Tests | `tools/BundleElements.Tests/BundleValidatorTests.cs` (Nand admitted) |

Reuse unchanged: the `elements/BothGate/*` template, `WorkshopMessages` logic admission (~69, which calls `Evaluate`), `WorkshopDeclaredPresentation.CommittedElectricalSocket`, `WorkshopGpuDevice` `NeutralReads`, the `ui/WorkshopConnections.cs` chooser, `WorkshopUiFailure`, `WorkshopSavePrevalidation`, `UpstreamSources`, `BundleValidator` LogicGate rules, and the `BothGateTests`/`SwitchElectricalTests`/`EditRefusal` helpers.

Do not change:

- `schemas/workshop_wire.fbs` or the generated C#/TS.
- `ElectricalLogicRecord`.
- `WorkshopRejection` values.
- `PortSocket`/`WorkshopSocket` values.
- `WorkshopPhysicsCompiler.Compile`: no refusal goes there (C-Ramp conflict).
- Both Gate behavior and art.

# Current state

## Material assumptions (stated before coding)

- **A1 Pictogram from meshes, not a texture quad.** No package scene references a texture, the worktree has no `.godot` import cache, and native tests instantiate package scenes. The navy NAND pictogram is therefore authored as navy `BoxMesh` strokes (input/output stubs, flat back, top/bottom, six-segment arc) and a `TorusMesh` bubble, scaled from the 24-unit icon to 0.65 m at z ≈ 0.40–0.41. The same path ships as the package `icon.svg`. `WorkshopIcons.Pictogram` is not touched (element ids are forbidden there).
- **A2 Undefined pin.** The spec's "`(LogicGateOperation)2` at BundleValidatorTests ~90" is really `255` at line 130. `(LogicGateOperation)2` (first unassigned value) is added as an explicit boundary for `Evaluate`, the D3 predicate and the bundle validator; 255 pins stay.
- **A3 Truth table through one-shot ImpactSwitch contacts.** A switch latches closed after a qualifying ball hit and never reopens. The supply battery is therefore held Disabled while the balls latch the condition switches, then enabled, so every row moves from rest. An open condition is a switch with no ball (miss/threshold controls are already proven by `SwitchElectricalTests`). Supply absent = supply battery Disabled throughout, plus a missing-supply-wire row 00.
- **A4 Refusal surface.** The D1 check lives in `WorkshopConnections.Validate(partOf)`, so it also fires through `WorkshopConstruction.Validate` (`WorkshopSimulation.Construct` returns `InvalidConstruction`, construction unchanged) and save prevalidation (`InvalidConnection`). New `WorkshopEditRejection.NonmonotoneFeedback` is appended after `UnsupportedPart` (no renumbering; C#-only).
- **A5 Tool membership.** `tools/E2eRunner/ImpactRules.ElementPackages` gains `ElectricalNand`, and `tools/Coverage.Tests` swaps its still-planned example from `electrical_nand` to `electrical_nor`, because admitting the package changes both derived memberships. Both suites already fail at base for unrelated reasons (below); these are the minimal current-path updates, not fixes of those failures.
- **A6 Palette ordinal 27.** Base bundle orders are 1–12, 15–26; 27 is free.
- **A7 Feedback text.** "Break the wire loop through parts 4, 6. This gate's output cannot feed its own inputs." G6 content, generic over future nonmonotone gates, no element id/title in engine code. The status label (`Workshop.MakeInterface`: `Paragraph(…, 12, …, new(480, 28))`, `AutowrapMode.WordSmart`) wraps but never truncates, so after review L1 the refusal names the first `LoopPartsNamed` = 4 ids in ascending order and then counts the rest ("… 4, 10, 11, 12 and 37 more."). The tests bound every refusal to 120 characters, which is about two lines of that label.
- **A8 Solver-scan vocabulary.** The catalogue id `electrical_nand` adds the word "electrical" to the 9A.19 solver element-word scan (`WorkshopPhysicsLawTests`), which then flagged six shared electrical-domain tables in `WorkshopPhysicsEngine.cs`. They name no gate, so these six exact tokens join the reviewed `GenericIdentifiers` list (the "Clamp"/"lamp" mechanism), with controls proving `ElectricalSupply` is exempt and `electricalNandRow`/`nandRow` are still found. The derivation, its test and the hash-frozen Known list are unchanged.

## Status

Pre-merge review: Pass-pending-merge ([review](review-9-7a-electrical-nand.md)). The candidate was committed as WIP `1101069c` (normal hooks) and merged with main `59ecb70b` (corrective slice) as `411425ea`. PM1, PM2 and L1 are uncommitted on top of that merge. Next: exact SnapshotApproval of the merged snapshot, then protected publication. Browser qualification pending post-catalogue (cases above).

### Post-merge (PM1/PM2/L1)

Merge conflicts and their resolutions:

- `tools/E2eRunner/ImpactRules.cs`: kept main's BothGate and HoldTimer rows and added ElectricalNand.
- `TODO.md`: kept main's brief and re-added the 9.7a In flight and Owner lines; Next reads "publish NAND after the forward fix …".
- `sprint-status.yaml`: auto-merged; main's done rows and 9.7a `review` are both kept.

The other files touched by both sides (`PalettePlacementTests`, `CurrentCapabilityInputsTests`) auto-merged.

PM1 expectation changes, each justified by main's current paths:

- **Package declaration.** `ElectricalNandElementDeclaration` uses named metadata arguments. The current Coverage reader, `CurrentCatalogue.ValidateResources`, binds each package by the regex `\bId: "…"`, and main converted BothGate and HoldTimer to the same form. Before the change, all four Coverage tests failed with "declares 0 Ids".
- **`CurrentCatalogueTests`: admitted count 24 → 25.** Every bundle package is admitted, and `electrical_nand` maps to its declaration path.
- **`CurrentCapabilityInputsTests`: planned-absent package `elements/ElectricalNand` → `elements/ElectricalNor`.** NAND is admitted now; NOR (CAT-025) still has a catalogue row and no package. The `electrical_nor` FutureDeclaration example now actually executes and passes.
- **Source and fixture counts.** 1472 and 303 are main's and are unchanged: NAND adds no requirement or fixture source.

| Check | Command | Result |
| --- | --- | --- |
| Whole solution after one rebuild (`build-final.log`, 0 W / 0 E) | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2173/2173**, Coverage 77/77, BundleElements 143/143, E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx-final.log`) |
| CI "Test C# tools" step, run exactly as written in `pages.yml` | the step's script | Coverage 77/77, BundleElements 143/143, WorkerAotGuard 17/17, E2eRunner 174/174 filtered; exit 0 (`ci-tool-step.log`) |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release --no-incremental` | 0 W / 0 E (`worker-release.log`) |
| Pins and ordinal after the merge | bundle | 25 elements, 25 distinct palette orders, NAND = 27 (`palette-ordinals.txt`). The native pins (registry 25, SaveFixture 25, ids+undelivered 27) pass inside 2173. |
| L1 boundary | `LongLoopRefusalNamesTheFirstPartsAndCountsTheRest` (4, 5 and 41 loop members) | exact text and ≤ 120 characters; pass |

Iterations, each driven by the inputs:

- First post-merge run: 4 Coverage failures, "declares 0 Ids".
- Second: the admitted-count pin.
- Third: the new long-loop fixture placed parts beyond the bench range (`WorkshopInput.Position` refusal). The fixture now uses a grid; the behaviour oracle is unchanged.

Design delivered: `LogicGateOperation.Nand = 1`; `CompiledSupplyGraph.Evaluate` Nand arm and exhaustive `IsMonotone`; new `SupplyCycles` (iterative Tarjan over predecessor lists → components in evaluation order, `Cyclic`, `NonmonotoneCycle`) shared by `WorkshopConnections.Validate(partOf)` (typed `WorkshopEditRejection.NonmonotoneFeedback`, appended) and the `CompiledSupplyGraph` constructor invariant (`ArgumentException`); `Resolve` visits components in order, evaluates singletons once and iterates only monotone cycles to their least fixed point. Deleted: the unordered slot sweep and its "powered only once" comment. No wire/schema, `WorkshopRejection`, socket, compiler, Both Gate or TS change. Package `elements/ElectricalNand` is declaration, scene art (Both Gate body/foot/face/marks/lamps/ports, 13-stroke navy mesh pictogram, three gold truth dots) and icon only; no record type, no runtime PartId branch, no new isolation Known entry.

## Evidence (criterion → command → result)

Pre-merge raw logs (the rows below): [`evidence-9-7a/pre-merge/`](evidence-9-7a/pre-merge/), moved unchanged from the implementer's session scratchpad (PM2). Post-merge logs: [`evidence-9-7a/post-merge/`](evidence-9-7a/post-merge/). Reviewer evidence: `evidence-9-7a-review/`. Builds: `dotnet build CuriousContraptions.slnx -c Debug -v:q`; tests: `dotnet test <project> -c Debug --no-build --no-restore -v:minimal [--filter …]`.

| Criterion | Command | Result |
| --- | --- | --- |
| Baseline of pre-existing tool failures (base 9b324bbd) | base build, then Coverage.Tests and E2eRunner.Tests | Coverage 73/77: 4 × `KeyNotFoundException` in `CurrentCatalogue.ValidateFixtures` (fixtures JSON). E2eRunner 187/189: `ElementPackagesMatchDirectoriesAndBundle` (BothGate, HoldTimer absent from `ImpactRules.ElementPackages`), `PartPreplacedByADeclaredLevelSelectsThatLevelsSuites` (cat-023b). `base-*.log` |
| Red before the ordered resolve (Nand arm only) | build + `--filter ElectricalNand` | 14/22: order test and reconvergence (NAND placed first) `Faulted` at read admission (Active contradicted the Nand law: the D2 defect); four feedback cases and graph invariant not refused; pictogram count oracle 14 was my miscount of the authored scene (5 straight + 6 arc + bubble + output = 13), corrected, no behaviour oracle changed. `red-test.log` |
| Truth table via real Battery→ImpactSwitch contacts × supply present/absent; actual Powered Gate blade motion; row 00 without supply/wire unpowered | `--filter ElectricalNand` | pass (8 rows + missing-wire control) |
| Row 11 retraction after 00 opening, save/load bytes, typed connections, exact Run/Reset (9R.1) and saved replay | same | pass |
| Placement/wiring order (gate first/last × forward/reversed links) bit-identical logic rows, power and blade | same | pass |
| Reconvergence via Both Gate (D6 substitute) retracts on the first tick, both id/placement orders | same | pass |
| Feedback refused: NAND→NAND, →Both Gate→, open ImpactSwitch, entering PowerIn; ids named, ≤100 chars pre-merge (≤120 budget after L1), wire-order independent; looped save `InvalidConnection` whose inner refusal is `NonmonotoneFeedback`, open save decodes (control); `WorkshopSimulation.Construct` `Rejected/InvalidConstruction`, construction unchanged; open control admitted | same | pass |
| Compiled-graph invariant throws exact `ArgumentException` with the nonmonotone-cycle message; valid graph admitted | same (test-only rebuild, `green2.log` 24/24) | pass |
| Law table, `IsMonotone`, undefined `(LogicGateOperation)2`, undefined socket 255, activation link refused | same | pass |
| Committed reads per row, tick-0 neutral admission, And-shaped row refused by declared-law admission; native lamps slate/gold per row through Reset; pictogram 13 navy strokes, three gold dots at x −.21/−.07/.07 | same | pass |
| Real chooser: three explicit socket icons; loop-closing wire (NAND→NAND, open switch) refused on `_status`, no command, no Undo, construction unchanged | `--filter NotBothGateChooser` | pass (with the class above: 24/24, `green1.log`) |
| Full native project incl. membership pins, isolation ratchet (no new Known), parity, ports golden, capacity, solver scan | full native | first run 2162/2163: solver scan flagged `Electrical*` domain tables (A8). After A8: **2166/2166**, 38 s. `native-full*.log` |
| Bundle validator: Nand admitted, `(LogicGateOperation)2`/255 rejected | BundleElements.Tests | 142/143. Only failure `PortLocatorTests.AnUndefinedSocketIsRefused` (newly found, pre-existing, unassigned): it pins `(PortSocket)5`, which is `FirstIn` since 9.6; neither the test nor `PortSocket` is touched by this diff. Not fixed here. |
| Worker AOT guard | WorkerAotGuard.Tests | 17/17 |
| Worker production build | `dotnet build CuriousContraptions.Simulation -c Release -v:q` | 0 warnings, 0 errors |
| Tool memberships (A5) | Coverage.Tests, E2eRunner.Tests | Same failing sets as baseline (4 identical KeyNotFound; 2 identical E2eRunner). `ls elements` − `ImpactRules` keys = {BothGate, HoldTimer} only. The Coverage `electrical_nor` flip is unverified: all four affected tests stop at `ValidateFixtures` before reaching it, until the corrective slice lands. |
| No new PartId / string classification | `git diff 9b324bbd -- engine/PartId.cs`; `rg 'electrical_nand|ElectricalNand|"Nand"|not.both' engine CuriousContraptions.Simulation ui CuriousContraptions.Bundle` | empty; no hits. `Validate` reads only the declared `Supply.Operation`. |
| TS | none changed | strict tsc not required |

Builds: base (incremental) 10 s; red 8 s (three analyzer errors in the new test file fixed: CA1716 enum name, xUnit2005 on a value-type construction, CA1861); post-change rebuild after A8 3 s; all clean. Two rebuilds after the red build followed changed inputs only. Anvil: the first writes (enum, declaration) were pre-validated with `anvil_validate_write`/`anvil_apply_patch` (`allow`). Later intermediate edits were not individually pre-validated, and some MCP calls carried partial excerpts, so those decisions are not claimed as candidate evidence. The frozen candidate's exact on-disk content, all 29 changed or new files, was then checked with `anvil check --no-tui <files>` (regex + AST anti-pattern and secret tiers): "No warnings found" (`anvil-check.log`). Three small edits used sed/perl/shell rather than the edit tool (the import sidecar derivation, test renames for the analyzer fixes); their content is in the checked candidate.
