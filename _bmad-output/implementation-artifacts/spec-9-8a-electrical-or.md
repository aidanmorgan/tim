---
title: "9.8a — Electrical OR Gate"
status: review
baseline: 55ee05b96f949a7d93241b5ed0608b32db6bb8a0
context:
  - TODO.md
  - docs/planning/elements/CAT-026-electrical_or.md
  - _bmad-output/implementation-artifacts/spec-9-7a-electrical-nand.md
  - _bmad-output/implementation-artifacts/spec-9-7b-electrical-nor.md
---

# Outcome and authority

Ship the package-owned "Either gate" (CAT-026, EL-134).

- **Authority:** Story 9.8a AC; CAT-026 O1–O12 and §5; current-cat-026 and todo-362; CAT-013 G1–G15 and A4. Templates: 9.7a D1–D6 and 9.7b E1–E7.
- **Law:** output = supplied ∧ (first ∨ second). Rows 01, 10 and 11 are true; a true row still needs real `PowerIn` energy.
- **Cycles:** Or is monotone, so it may sit on a cycle. The cycle settles at the least fixed point from unpowered on every tick (G5, O6).
- **Deletes:** no element record. Also deletes the Both Gate stand-ins for Or in the 9.7a and 9.7b reconvergence tests.

# Finite design decisions

- **F1 Engine reuse.** These stay unchanged: the condensation-ordered `Resolve`, `SupplyCycles`, the `WorkshopConnections.Validate` refusal and the G6 text with its 4-id cap.
  - `Resolve` starts each cyclic component from unpowered, using a fresh `powered[]` every tick, and iterates upward.
  - Because `IsMonotone(Or)` is true, a loop made only of Or and And gates is admitted.
  - Any cycle that also holds a Nand or Nor is refused with `NonmonotoneFeedback` (O10).
- **F2 Enum.** Append `Or = 3`. CAT-026 §2/O3 "value 1" is the legacy scene integer and is not carried forward. No change to the wire, schema or record.
- **F3 Law.** The `Evaluate` arm is `first || second` and the `IsMonotone` arm is `true`. Both switches stay exhaustive.
- **F4 Inputs and consumer.** The conditions are Battery → ImpactSwitch PowerIn→Supply contacts. The consumer is Powered Gate (CAT-051). Both follow NAND D5.
- **F5 Close the Or substitutes natively.**
  - Items closed: 9.7a D6 and review I2 (CAT-024 D6 reconvergence; D7 Nand→Or→Nand), and 9.7b E5 and review I3 (CAT-025 R6 Nor→Or→Nor).
  - The Xor forms (O9/O10 into Xor, X9) were closed natively in Story 9.8b (H8).
  - Still queued post-catalogue: the "Or chaining" browser cases in cat-024 and cat-025.
- **F6 Art (A1-style deviation, as NOR B1).**
  - Copy NAND's scene, which already has dots 01 and 10 at x −.07 and .07. Delete `TruthDot00` and add `TruthDot11` at x .21 (y −.4, z .42; O11).
  - Draw the navy pictogram from `WorkshopIcons.cs@a6c914e:L76`:
    - stubs `M2 7h5` and `M2 17h5`;
    - the body `M6 4q11 0 15 8-4 8-15 8 6-8 0-16z`, as three quadratics of four chord `BoxMesh` segments each;
    - output stub `M21 12h2`;
    - no bubble.
  - Use NAND's mapping: s = .65/24, centre (12,12), y flipped, z .4.
  - Expect 15 strokes, but count them from the authored scene.
  - Ship the same path as `icon.svg`.
- **F7 Suite.** The suite is `tools/e2e/cat-026.test.ts`. Correct CAT-026 §5, which still names `cat-026-027`.
- **F8 Solver scan.** "or" falls under the three-letter `ElementNames` floor, so it is never an element word. That keeps the `or` keyword and words like color and order from matching.
  - Or-specific identifiers are still caught through "electrical", which is an element word, while the `Electrical*` domain tables stay in `GenericIdentifiers`.
  - Add the row `("electricalOrRow", true)`.
  - Expect no change to the Known list or its hash. Rerun the scan to confirm.
- **F9 Undefined pin.** It moves 3 → 4. Story 9.8b moves it again, to 5.

# Tasks

- [x] **Enum and law.**
  - Add `LogicGateOperation.Or = 3` with both switch arms.
  - Change the enum summary to "And and Or are monotone; Nand and Nor are not".
- [x] **Undefined pins 3 → 4.** Re-derive the line numbers after NOR. The four sites are:
  - `ElectricalNandTests:281`
  - `ElectricalNandTests:282`
  - `ElectricalNorTests:283` (`foreach` over 3 and 255)
  - `BundleValidatorTests:132`

  Keep all the 255 pins. Add an Or-admitted line in `BundleValidatorTests`. No test enumerates `LogicGateOperation` (checked).
- [x] **Package `elements/ElectricalOr/`.**
  - `ElectricalOrElementDeclaration`, with named `ElementMetadata` arguments:
    - `Id: "electrical_or"`;
    - title "Either gate";
    - Control;
    - colour (.4,.72,.79,1);
    - the CAT-026 description;
    - `PaletteOrder: 29` (provisional; check it is unique in the merged bundle).
  - NOR's four ports, `logic` node and nine lamp bindings, with `Operation: Or`.
  - The scene (F6), `icon.svg` with its import, the csproj and the `.uid`.
  - No record type and no PartId branch.
- [x] **Membership.**
  - Add the package to `CuriousContraptions.slnx` and `ElectricalOrTests.cs` to the tests csproj.
  - Add the package to the bundle, and `ImpactRules.ElementPackages["ElectricalOr"]=new("electrical_or")`.
  - Coverage, `CurrentCatalogueTests`: admitted count +1, plus an `electrical_or` path assertion.
  - Coverage, `CurrentCapabilityInputsTests`: move the planned-absent example to `electrical_xor` / `elements/ElectricalXor` (the requirement is at `CapabilityCatalogueRequirements:35`).
- [x] **Pins.** (Pre-merge values; re-derive after C-Bumper+Spring.) Re-derive every pin from the merged post-NOR bundle. The expected changes, if nothing else lands first:

  | Pin | Change |
  | --- | --- |
  | `PartRegistryTests` | add to the list; counts 26→27; ids plus undelivered 28→29 |
  | SaveFixture | 26→27 |
  | `BasketballResourceTests` | add to the id list |
  | `BundlePortsTests` | add a golden row |
  | `CSharpStaticGateTests` | add the csproj |
  | `PackageRuntimeParityTests` | add the exclusion and the static row |
  | `WorkbenchCapacityTests` | add the rows |
  | `PalettePlacementTests` | add the Either-chooser theory |

- [x] **Cleanup.**
  - No parallel path, fallback, alias or isolation Known entry.
  - In the NAND and NOR reconvergence tests, replace the Both Gate stand-in with Or and delete the "undelivered Or" comments.
  - Keep `ThroughBothGate`: an And loop is its own valid case.
- [x] **Latest-design alignment.**
  - The package owns identity and art; the shared typed engine owns the law. f32 SmoothStep is unchanged.
  - Docs: CAT-026 (status, §2 enum note, §5 suite); CAT-024 §5 and CAT-025 §5 (native Or chaining closed); TODO.md; sprint-status.
- [x] **PRIMARY C# tests.** These go in `ElectricalOrTests.cs` unless noted, and run through the real compiler, worker device and Powered Gate motion.
  - **Truth table:** 00/01/10/11, each with and without supply, through real ImpactSwitch contacts.
    - With supply, rows 01, 10 and 11 open the blade.
    - Row 00 with supply, and every row without supply, show zero displacement on every tick.
    - A true row with the supply wire missing never powers.
  - **Order:** gate before or after its inputs, and links forward or reversed. Every row gives bit-identical reads, power and blade.
  - **Monotone loop admitted (O6/O7, A4):** two Or gates feed each other's `FirstIn`, with both `PowerIn` supplied.
    - With no condition, the loop stays unpowered on every tick from tick 0. It must not latch itself on.
    - A condition on one `SecondIn` powers both gates.
    - Disabling the condition battery retracts both gates on the next tick.
    - Disabling the supply battery instead also retracts both.
    - Run both id orders.
  - **Contact loop (O8):** the loop is closed through an open ImpactSwitch, and the other input is fed.
    - The Or reads `SecondOnly` with its output on.
    - A Both Gate in the same position reads `SecondOnly` with its output off.
  - **Mixed loops refused (O10):** Or↔Nand and Or↔Nor, including one through an open switch and one via `PowerIn`.
    - Each is refused with a typed `NonmonotoneFeedback` that names the ids, with no Undo push and the construction unchanged.
    - Saving gives `InvalidConnection`, and `Construct` gives `InvalidConstruction`.
    - The chooser refuses the wire on `_status`.
    - The Either chooser admits a pure Or loop and pushes Undo.
  - **F5 closures:**
    - Or → NAND and Or → NOR `SecondIn` reconvergence retracts on the first tick, in both orders. These go in the NAND and NOR tests.
    - Add `ThroughOr` to both `FeedbackLoop` enums, and to their native and chooser theories.
  - **Lamps and reads:**
    - Committed reads per row; tick 0 admits neutral.
    - Forged rows are refused: a supplied 01 marked inactive, and a supplied 00 marked active.
    - Lamps go slate/gold and return through Reset; the stroke count and the three dots match.
  - **Run/Reset and Save/Load:** Reset is exact (9R.1), and Save/Load round-trips the typed sockets.
  - **Rejections:** undefined operation 4 and 255; undefined socket 255.
  - **Review mutations (each must turn tests red):**
    - cyclic components seeded powered;
    - the previous tick carried into the solve;
    - Or evaluated as And;
    - Or evaluated as Xor;
    - Or declared nonmonotone;
    - Or active without supply;
    - the old slot sweep.
- [x] **9A.45a ratchet** passes with no new entry (F8).
- [x] **One matching build, then reuse it.** (Pre-merge snapshot; repeat on the merged snapshot.)
  - Build: `dotnet build CuriousContraptions.slnx -c Debug`.
  - Test: `dotnet test CuriousContraptions.slnx --no-build --no-restore`.
  - The CI "Test C# tools" step, verbatim under bash.
  - The Release worker build.
  - `PaletteOrder` uniqueness.
  - `anvil check --no-tui` on the changed files.
- [ ] **Publication:** independent SnapshotApproval, then protected commit/push. Then check remote/tree identity, exact-SHA CI, the Pages artifact and deployment, and write the terminal `review-9-8a-electrical-or.md`.

# Pending browser cases (post-catalogue, `cat-026.test.ts`)

Construction uses real UI as in CAT-013. Cases:

- The four rows with supply, and a true row without supply.
- The lamps, the pictogram and the three dots.
- An Or loop that forgets its condition.
- A mixed-loop wire refused on the status line.
- Reconvergence into NAND and into NOR.
- Reordered placement and wiring.
- Run/Reset and Save/Load.

The cat-024 and cat-025 "Or chaining" cases are now unblocked but stay queued.

# Code Map

| File | Change |
| --- | --- |
| `CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs` | `Or`, summary |
| `engine/gpu/SupplyRoute.cs` | two switch arms only |
| `elements/ElectricalOr/*` | new package |
| Membership/pins | slnx, tests csproj, `ImpactRules`, Coverage tests, the pin files above |
| Tests | `ElectricalOrTests.cs` (new); `ElectricalNandTests` and `ElectricalNorTests` (`ThroughOr`, Or reconvergence, pin 4); `BundleValidatorTests`; `PalettePlacementTests`; `WorkshopPhysicsLawTests` (scan row) |
| Docs | CAT-026, CAT-024, CAT-025, TODO.md, sprint-status |

Do not change: `Resolve`, `SupplyCycles`, `WorkshopConnections.Validate`, `workshop_wire.fbs`, `ElectricalLogicRecord`, `WorkshopPhysicsCompiler`, or the Both Gate, NAND and NOR behaviour and art.

# Current state

The pre-merge candidate was committed as WIP `8527aa3f` with normal hooks, together with the reviewer's review and evidence files. Its pre-merge review verdict was Pass-pending-merge ([review](review-9-8a-electrical-or.md)). It was then merged with main `711568c4` (C-Bumper+Spring) as `71f7ca12`. Next: an exact SnapshotApproval of the merged snapshot, then protected publication. Browser qualification is pending post-catalogue (cases above).

## Post-merge (PM1, PM2, L1)

Merge `71f7ca1295e592db219bbc8bf1d2fcd76110afef`: tree `eb87ad00b876cfa4bc26028acc2186ef734981d1`, parents `8527aa3f` (WIP) and `711568c4` (main). Evidence is in [`evidence-9-8a/post-merge/`](evidence-9-8a/post-merge/).

- **Conflict 1: `WorkbenchCapacityTests` gate/bumper rows.**
  - Main's generic Bumper row is kept: `WorkshopInput.Generic(PartId.PinballBumper, …)`, because C-Bumper+Spring deleted `WorkshopInput.Bumper`.
  - `electrical_or` is added to the generic gate row.
  - The OR/NAND/NOR tests use no other deleted API; an `rg` for `WorkshopInput.(Bumper|Springboard|Basketball|BowlingBall)(` across the tests finds nothing.
- **Conflict 2: `TODO.md`.**
  - Main's Verified and Also-verified lines are kept, including the C-Balls and 9.7b receipts.
  - In flight and Owner name 9.8a. C-Bumper+Spring is recorded as merged to main `711568c4`. Next reads "publish 9.8a after its SnapshotApproval" (L1).
- **Auto-merged.** All other files merged automatically. Sprint-status keeps main's rows plus 9.8a `review`.
- **Pins re-derived from the merged bundle (`pin-derivation.txt`).** C-Bumper+Spring adds no package and no undelivered part:
  - 27 bundle entries and 27 package dirs; 2 undelivered parts (ids 29);
  - 27 distinct `PaletteOrder` values, with `electrical_or` = 29;
  - so registry 27, SaveFixture 27, Coverage admitted 27 and ids 29 all hold, and no pin was edited.
- **Identity (`identity.txt`).**
  - Against main, the merge differs only in the slice's own 33 non-evidence paths.
  - Against the WIP commit, the slice-owned changes are only the two resolutions.
- **`git diff --check`.** It is clean on every non-evidence path and on the working tree. It flags trailing whitespace only in committed raw tool output (`diff-check-evidence-files.txt`), which is kept unchanged.
- **AppBundle.** `CuriousContraptions.web/AppBundle` is absent.

| Check | Command | Result |
| --- | --- | --- |
| One rebuild of the merged tree | `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E (`build-debug.log`) |
| Whole solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2352/2352** (main 2313 + 39), Coverage 77/77, BundleElements 148/148 (main 146 + 2), E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx.log`) |
| CI "Test C# tools" step, verbatim, under bash | `evidence-9-8a/ci-tool-step.sh` | Coverage 77, BundleElements 148, WorkerAotGuard 17, E2eRunner 174 filtered; exit 0 (`ci-tool-step.log`) |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release` | 0 W / 0 E (`worker-release.log`) |
| Named electrical, chooser and scan tests | same filter as the pre-merge row | 122/122 (`native-electrical-named.log`) |
| Merge commit hooks | `git commit` (normal pre-commit) | anvil clean; gate tests 124/124 |

## Assumptions and deviations

- **G1 Pictogram (F6).** The pictogram has 15 navy strokes, counted from the authored scene:
  - two input stubs, 2→7, at x −.20313;
  - upper (6,4)–(17,4)–(21,12), lower (21,12)–(17,20)–(6,20) and back (6,20)–(12,12)–(6,4), each as four chord `BoxMesh` segments;
  - an output stub 21→23;
  - no bubble.

  The mapping is NAND's: s = .65/24, centre (12,12), y flipped, z .4, length = chord·s + .035. The derivation is in `evidence-9-8a/or_pictogram.py`. The truth dots are 01, 10 and 11 at x −.07, .07 and .21, and load_steps is 24.
- **G2 F5 closures.**
  - The NAND and NOR reconvergence tests now route the second input through an Either gate fed on its `FirstIn` only. Its single true input proves the Or law and not the Both Gate's, and it asserts `FirstOnly`/active.
  - `ThroughOr` is added to both `FeedbackLoop` enums, to the native theories and to the chooser theories.
  - `ThroughBothGate` is kept.
  - The "undelivered Or" comments are deleted.
- **G3 Or loop (O6/O7).**
  - Two Either gates feed each other's `FirstIn`. Battery 2 supplies both `PowerIn`s, and condition battery 1 feeds one `SecondIn`.
  - The run has five phases, executed for both id/placement orders:
    1. off from tick 0;
    2. condition on: both gates on in the same tick;
    3. condition off: both gates off on the next tick;
    4. condition on again;
    5. supply off: both gates off, while the condition is still held.
- **G4 Contact loop (O8).** This is a theory over Either vs Both Gate in the same place. The Either gate reads `SecondOnly` with its output on and the blade open; the Both Gate reads `SecondOnly` with its output off and the blade still.
- **G5 Mixed loops (O10).** The cases are `WithNand`, `WithNor`, `ThroughOpenSwitch` (Or→switch→Nor→Or, ids 4, 6, 7) and `EnteringPowerIn` (Nand→Or→Nand `PowerIn`). Each is refused natively and in the chooser. `OrToOr` is the admitted control: the chooser makes one Undo push and Undo restores; save and `Construct` admit it, and it stays unpowered without a condition.
- **G6 Truth-without-supply wire.** The case uses row 11, since every true row needs the supply.
- **G7 Lamp rows.** The rows are 00, 10, 01 and 11 supplied, then 11 unsupplied. The forged rows are a supplied 01 marked inactive and a supplied 00 marked active.
- **G8 Scan.** Two rows were added: `("electricalOrRow", true)` and the control `("orRow", false)` (F8 floor). `GenericIdentifiers`, the Known list and its hash are unchanged.
- **G9 Analyzer fix.** CA1861 on a constant string array in the presentation test was fixed with a `static readonly` field (one rebuild).
- **G10 Generated identities.** The Godot import generated `uid://00cjt3fjoi6p` (.cs.uid) and `uid://dgjxmwg3f8fdk` (icon; ctex hash = md5 of the resource path). Both are unique.
- **G11 Mutation scope.** The old slot sweep (M7) cannot change any Or result, because Or is monotone. It is caught by the NAND/NOR order tests and by the Or-fed NAND reconvergence (gate-first).
- **TODO.** Only the 9.8a sentences were appended. The stale 9.7b in-flight text is left for the merge with main.

## Evidence (`evidence-9-8a/`)

| Criterion | Command | Result |
| --- | --- | --- |
| Build | `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E (`build-debug-final.log`) |
| Whole solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2317/2317** (base 2278), Coverage 77/77, BundleElements 147/147 (base 145), E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx-final.log`) |
| CI "Test C# tools" step, verbatim, under bash | `ci-tool-step.sh` | Coverage 77, BundleElements 147, WorkerAotGuard 17, E2eRunner 174 filtered; exit 0 (`ci-tool-step.log`) |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release` | 0 W / 0 E (`worker-release.log`) |
| Electrical, chooser and scan tests by name | `--filter ElectricalOr\|ElectricalNand\|ElectricalNor\|GateChooser\|TheGeneralScan\|TheShippedSolver\|KnownViolation` | 122/122 (`native-electrical-named.log`) |
| Pins and ordinals | generated bundle (`pin-derivation.txt`, `palette-ordinals.txt`) | 27 entries and 27 package dirs, 2 undelivered (ids 29); 27 distinct `PaletteOrder` values, `electrical_or` = 29 |
| Static scope | `static-scope-checks.txt` | No production PartId/string classification; Known list/hash/`GenericIdentifiers` unchanged; no "undelivered Or" text left; uids unique |
| Hygiene | `git diff --check`; `anvil check --no-tui <32 files>` | clean; No warnings found (`anvil-check.log`, `changed-files.txt`) |
| `CuriousContraptions.web/AppBundle` | `ls` | absent |

Mutations: each one rebuilds, then runs the filter `ElectricalOr|ElectricalNand|ElectricalNor|GateChooser|BothGate` (114 tests); logs are in `mutations/`. The source was restored after each, and the final `git diff engine/gpu/SupplyRoute.cs` shows only the two Or arms.

| Mutation | Red tests |
| --- | --- |
| M1 Or as And | 15: Or truth 01/10, order 01/10, law, reads, save/Reset, contact loop, Or loop ×2, lamps; NAND and NOR reconvergence ×2 each |
| M2 Or as Xor | 7: truth 11, order 11, law, reads, Or loop ×2, lamps |
| M3 Or declared nonmonotone | 7: Or loop ×2, contact loop, pure-loop admission, compiled graph, law, chooser `OrToOr` |
| M4 Or active without supply | 14: truth rows 01/10/11 with and without supply (6; the supplied rows fail in their supply-off latch phase), missing-wire row, order 01/10/11, Or loop ×2, reads, lamps |
| M5 Cyclic components seeded powered | 10: Or loop ×2, contact loop ×2, pure loop, NAND and NOR monotone loops, Both Gate cycles ×3 |
| M6 Previous tick carried into cycles | 5: Or loop ×2, Both Gate cycles ×3 |
| M7 Old whole-graph slot sweep | 5: NAND order, NAND reconvergence via Or (gate first), NOR order 01/10/11 (G11) |

Retained failures: none.
