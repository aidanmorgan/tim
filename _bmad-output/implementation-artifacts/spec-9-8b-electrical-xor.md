---
title: "9.8b — Electrical XOR Gate"
status: review
baseline: a2eddf811323d95819fef5eeeb50a76a63bf428f
context:
  - TODO.md
  - docs/planning/elements/CAT-027-electrical_xor.md
  - _bmad-output/implementation-artifacts/spec-9-7b-electrical-nor.md
  - _bmad-output/implementation-artifacts/spec-9-8a-electrical-or.md
---

# Outcome and authority

Ship the package-owned "Exactly one gate" (CAT-027, EL-135), at delivery position 31.

- **Authority:** the Story 9.8b AC; CAT-027 X1–X12 and §5; current-cat-027 and todo-362; CAT-013 G1–G15 (G5 and G6). Templates: NOR (9.7b, the nonmonotone case) and OR (9.8a).
- **Law:** output = supplied ∧ (first ⊕ second). Rows 01 and 10 are true; rows 00 and 11 are false.
- **Cycles:** Xor is not monotone (`IsMonotone=false`). Any directed cycle that contains a Xor is refused through the existing `NonmonotoneFeedback` path (G5, X8–X10).
- **Dependency:** 9.8a OR must be merged and 9.4c published. Every pin below is re-derived on that base.
- **Deletes:** no element record. It does delete the 9.8a F5 "Xor forms pending" deferral.

# Finite design decisions

- **H1 Engine reuse.** These stay unchanged: `Resolve`, `SupplyCycles`, `WorkshopConnections.Validate` and the generic G6 text ("This gate's output cannot feed its own inputs."). The legacy wording "XOR, NOR and NAND" is not restored.
- **H2 Enum.** Append `Xor = 4`.
  - CAT-027 §2 "enum value 2" (X3) is the legacy scene integer and is not carried forward; correct §2.
  - No change to the wire, schema or record.
- **H3 Law.** `Evaluate` uses `first != second`, and the `IsMonotone` arm is `false`. Both switches stay exhaustive.
- **H4 Inputs and consumer.** Conditions come through Battery → ImpactSwitch PowerIn→Supply contacts. The consumer is Powered Gate (CAT-051).
- **H5 Art (as in 9.8a F6).**
  - Copy OR's scene. Delete its two input stubs and `TruthDot11`; keep the dots 01 and 10 at x −.07 and .07 (y −.4, z .42; X11).
  - Pictogram, from `WorkshopIcons.cs@a6c914e:L77`:
    - the extra back curve `M2 4q6 8 0 16` (4 chords);
    - OR's body (12 chords);
    - the output stub `M21 12h2`.
  - Expect about 17 navy strokes, but count them from the authored scene.
  - Use NAND's mapping. Ship the same path as `icon.svg`.
- **H6 Solver scan.** "xor" clears the three-letter floor, so it becomes an element word.
  - Add the rows `("electricalXorRow", true)` and `("xorRow", true)`.
  - Check that the five shipped solver files have no camel-split "Xor" token (none at 55ee05b9). The Known list, its hash and `GenericIdentifiers` stay unchanged.
- **H7 Planned-absent example.** After Xor there are no undelivered logic gates. Cylinder (10.1) cannot be used: ENGINE-CYLINDER is roadmap-only and has no catalogue capability row.
  - The next catalogue package is `pulley` (CAT-053, 10.2, position 35). Use `pulley` / `elements/Pulley`.
  - Tennis and balloon are `UndeliveredParts`, so they are not used.
  - Check that `pulley` is a Catalogue source key with a `CapabilityCatalogueRequirements` row, that it classifies `FutureDeclaration`, and that `elements/Pulley` is absent.
- **H8 Close the Xor forms natively** (9.8a F5; CAT-026 O9/O10; CAT-027 X6/X9).
  - Add `WithXor` to `ElectricalOrTests.FeedbackLoop` and to the Either chooser theory.
  - Add the Or → Xor `SecondIn` reconvergence through a real Either gate.
  - Update the CAT-026 §5 boundary and integration notes. The browser cases stay queued.
- **H9 Undefined pin.** It moves 4 → 5.
- **H10 Suite.** The suite is `tools/e2e/cat-027.test.ts`. Correct CAT-027 §5 (`cat-026-027`).

# Tasks

- [x] **Enum, law and IsMonotone.**
  - Add `LogicGateOperation.Xor = 4` with both arms.
  - Change the summary to "And and Or are monotone; Nand, Nor and Xor are not".
- [x] **Package `elements/ElectricalXor/`.**
  - `ElectricalXorElementDeclaration`, with named `ElementMetadata` arguments:
    - `Id: "electrical_xor"`;
    - title "Exactly one gate";
    - Control;
    - colour (.4,.72,.79,1);
    - the CAT-027 description;
    - `PaletteOrder: 30` (provisional; unique).
  - The four ports, `logic` node and nine lamp bindings, with `Operation: Xor`.
  - The scene (H5), `icon.svg` with its import, the csproj and the `.uid`.
  - No record and no PartId branch.
- [x] **Membership.**
  - `CuriousContraptions.slnx`, plus `ElectricalXorTests.cs` in the tests csproj.
  - The bundle, and `ImpactRules.ElementPackages["ElectricalXor"]=new("electrical_xor")`.
  - Coverage: `CurrentCatalogueTests` admitted count +1 plus an `electrical_xor` path; `CurrentCapabilityInputsTests` planned example → `pulley` (H7).
- [x] **Undefined pins 4 → 5.** At OR-candidate lines, re-derive them after the merge:
  - `ElectricalNandTests:285`
  - `ElectricalNandTests:286`
  - `ElectricalNorTests:288` (foreach)
  - `ElectricalOrTests:302` (foreach)
  - `BundleValidatorTests:133`

  Add a Xor-admitted line in `BundleValidatorTests`. Keep every 255 pin, including `BothGateTests:169`.
- [x] **Pins.** (Pre-merge values; re-derive after any main merge.) Expected +1 each (provisional; re-derive from the merged bundle):

  | Pin | Change |
  | --- | --- |
  | `PartRegistryTests` | list; counts 27→28; ids plus undelivered 29→30 |
  | SaveFixture | 27→28 |
  | `BasketballResourceTests` | id list |
  | `BundlePortsTests` | golden row |
  | `CSharpStaticGateTests` | csproj |
  | `PackageRuntimeParityTests` | exclusion and static row |
  | `WorkbenchCapacityTests` | rows |
  | `PalettePlacementTests` | Exactly-one chooser theory |

- [x] **Solver-scan rows** (H6).
- [x] **Cleanup.**
  - No fallback, alias or Known entry.
  - Delete the 9.8a F5 "Xor forms pending" text and any "undelivered Xor" comments.
  - Keep `cat-024`/`cat-025` "Or chaining" queued.
- [x] **Latest-design alignment.**
  - The package owns identity and art; the shared typed engine owns the law. f32 SmoothStep is unchanged.
  - Docs: CAT-027 (status, §2 enum note, §5 suite); CAT-026 §5 (Xor forms closed natively); CAT-013 G5 (Xor now delivered); TODO.md; sprint-status.
- [x] **PRIMARY C# tests.** These go in `ElectricalXorTests.cs` unless noted, and run through the real compiler, worker device and Powered Gate motion.
  - **Truth table:** 00/01/10/11, each with and without supply.
    - With supply, only rows 01 and 10 open the blade.
    - Row 00 and row 11 with supply give zero displacement on every tick. Row 11 is the "low when identical" AC.
    - Every row without supply also gives zero displacement on every tick, and a true row with the supply wire missing never powers (X5).
    - Removing the battery links unpowers the gate.
  - **Order:** gate before or after its inputs, and links forward or reversed. Every row gives bit-identical reads, power and blade (X5).
  - **Reconvergence (X6/X7):**
    - The same battery reaches both Xor inputs, directly and through an Either gate. The output is off on the first tick.
    - Wiring the battery into the Or retracts a powered Xor in the same solve, and it stays off.
    - Run both id orders and both wire orders.
  - **Loops refused (X8–X10, O10):** Xor→Xor, Xor↔Or (X9), Xor↔Nand, and Xor through an open ImpactSwitch (X8/X10).
    - Each is refused with a typed `NonmonotoneFeedback` that names the ids, with no Undo push and the construction unchanged.
    - Saving gives `InvalidConnection`, and `Construct` gives `InvalidConstruction`. No part powers.
    - The chooser refuses the wire on `_status`.
    - Add the Xor-into-`PowerIn` case.
  - **Monotone control:** an Or↔Or loop that feeds a Xor off the cycle is admitted by the chooser, save and `Construct`, and powers correctly. This shows the refusal is limited to cycles.
  - **H8 closures:** in `ElectricalOrTests`, `WithXor` is refused natively and in the chooser.
  - **Lamps and reads:**
    - Committed reads per row; tick 0 admits neutral.
    - Forged rows are refused: a supplied 11 marked active, and a supplied 01 marked inactive.
    - Lamps go slate/gold and return through Reset; the stroke count and the two dots match.
  - **Run/Reset and Save/Load:** Reset is exact (9R.1); Save/Load round-trips the typed sockets; a refused Run leaves the save unchanged (X8).
  - **Rejections:** undefined operation 5 and 255; undefined socket 255.
- [x] **Mutations (each must turn tests red):**
  - Xor as Or (row 11);
  - Xor as Xnor;
  - Xor as And;
  - Xor declared monotone (the loop tests);
  - Xor active without supply;
  - cycle refusal applied to the whole graph (the monotone control);
  - cyclic components seeded powered;
  - the old slot sweep.

  Restore the source after each.
- [x] **9A.45a ratchet** passes with no new entry.
- [x] **One matching build, then reuse it.** (Pre-merge snapshot; repeat on any merged snapshot.)
  - Build: `dotnet build CuriousContraptions.slnx -c Debug` (0 W).
  - Test: `dotnet test CuriousContraptions.slnx --no-build --no-restore`.
  - The CI "Test C# tools" step, verbatim under bash.
  - The Release worker build.
  - `PaletteOrder` uniqueness.
  - `git diff --check`.
  - `anvil check --no-tui` on the changed files.
- [ ] **Publication:** independent review, then SnapshotApproval, then protected commit/push. Then check remote/tree identity, exact-SHA CI, the Pages artifact and deployment, and write the terminal `review-9-8b-electrical-xor.md`.

# Pending browser cases (post-catalogue, `cat-027.test.ts`)

Construction uses real UI as in CAT-013. Cases:

- The four rows with supply, and every row without supply.
- The lamps, the pictogram and the two dots.
- Or feeding Xor, and the same-source reconvergence.
- Xor loops, including one through an open switch, refused on the status line with the save unchanged.
- Reordered wiring.
- Run/Reset and Save/Load.

The cat-026 "reconvergence into Xor" case is unblocked but stays queued.

# Code Map

| File | Change |
| --- | --- |
| `CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs` | `Xor`, summary |
| `engine/gpu/SupplyRoute.cs` | two arms only |
| `elements/ElectricalXor/*` | new package |
| Membership/pins | slnx, tests csproj, `ImpactRules`, Coverage tests, the pin files above |
| Tests | `ElectricalXorTests.cs` (new); `ElectricalOrTests` (`WithXor`, pin 5); `ElectricalNandTests` and `ElectricalNorTests` (pin 5); `BundleValidatorTests`; `PalettePlacementTests`; `WorkshopPhysicsLawTests` (scan rows) |
| Docs | CAT-027, CAT-026, CAT-013, TODO.md, sprint-status |

Do not change: `Resolve`, `SupplyCycles`, `WorkshopConnections.Validate`, `workshop_wire.fbs`, `ElectricalLogicRecord`, `WorkshopPhysicsCompiler`, or the behaviour and art of the And, Nand, Nor and Or gates.

# Current state

Pre-merge candidate in worktree `story-9-8b`, uncommitted, on base `a2eddf81`. 9.4c had not published at the time of writing. Next: if main has moved, merge it and re-derive the pins and `PaletteOrder` 30; then independent review and SnapshotApproval. Browser qualification is pending post-catalogue (cases above).

## Assumptions and deviations

- **J1 Pictogram (H5).** 17 navy strokes, counted from the authored scene:
  - the extra back curve (2,4)–(8,12)–(2,20), as 4 chords that reuse OR's back-curve meshes, since their lengths are identical;
  - OR's upper, lower and back curves, 12 chords;
  - the output stub.

  There are no input stubs and no bubble. Dots are 01 and 10. `load_steps` is 23. The derivation is in `evidence-9-8b/xor_pictogram.py`.
- **J2 Reconvergence (X6/X7).** A theory over gate-first × reversed links (4 cases). Battery 1 feeds Xor `FirstIn` directly and the Either gate's `FirstIn`; the Either gate feeds Xor `SecondIn`.
  - Control: without the battery → Or wire, row 10 opens the blade.
  - With the wire: every tick, the Either gate is active and the Xor reads `Both` and stays off. The blade stays still and Reset is exact.
  - This is two runs, not a mid-run rewire. The Workshop has no live wiring edit during Run, so "wiring into the Or retracts in the same solve" becomes "off from tick 0".
- **J3 Monotone control.** `OrLoopFeedingXor`: an Or↔Or loop with a condition on one `SecondIn` feeds Xor `FirstIn`, off the cycle.
  - It is admitted by Validate, save and Construct, and by the chooser (one Undo push, Undo restores).
  - It powers the Xor on the condition and retracts when the condition goes.
- **J4 Loops.** `XorToXor`, `WithOr` (X9), `WithNand`, `ThroughOpenSwitch` (X8/X10) and `EnteringPowerIn` (Xor → Or → Xor `PowerIn`).
  - Each is refused natively, by save and by Construct, and in the chooser.
  - For X8, the saved bytes of the open machine are re-encoded identically after the refused Construct.
- **J5 X5 links.** A theory removes either the supply wire or both condition battery links from a true row (10). Neither powers.
- **J6 Order.** The row-11 order cases cannot glitch under the old slot sweep in this layout: a gate visited first sees 00, which is false. The same-source reconvergence with the gate first is the discriminating case (M8).
- **J7 Planned example (H7).** Verified:
  - `pulley` has `CapabilityCatalogueRequirements:61` and Catalogue source keys;
  - `elements/Pulley` is absent;
  - Coverage passes with it as the `FutureDeclaration` example.
- **J8 9.8a deferral.** `spec-9-8a-electrical-or.md` F5 "Still pending: the Xor forms" is replaced with a closed-in-9.8b note.
- **TODO.** Only the 9.8b sentences were appended.

## Evidence (`evidence-9-8b/`)

| Criterion | Command | Result |
| --- | --- | --- |
| Build | `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E (`build-debug-final.log`) |
| Whole solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2392/2392** (base 2352), Coverage 77/77, BundleElements 150/150 (base 148), E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx-final.log`) |
| CI "Test C# tools" step, verbatim, under bash | `ci-tool-step.sh` | Coverage 77, BundleElements 150, WorkerAotGuard 17, E2eRunner 174 filtered; exit 0 (`ci-tool-step.log`) |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release` | 0 W / 0 E (`worker-release.log`) |
| Electrical, chooser and scan tests by name | `--filter ElectricalXor\|ElectricalOr\|ElectricalNand\|ElectricalNor\|GateChooser\|TheGeneralScan\|TheShippedSolver\|KnownViolation` | 161/161 (`native-electrical-named.log`) |
| Pins and ordinals | bundle (`pin-derivation.txt`, `palette-ordinals.txt`) | 28 entries/dirs, 2 undelivered (ids 30); 28 distinct `PaletteOrder`, `electrical_xor` = 30 |
| Static scope | `static-scope-checks.txt` | No production PartId/string classification; no `Xor` token in the five solver files; Known list/hash/`GenericIdentifiers` unchanged; uids `b2yf83y2a7m8a` and `d1qgg1pd1thmb` unique |
| Hygiene | `git diff --check`; `anvil check --no-tui` on the 34 changed files (`changed-files.txt`) | clean; No warnings found (`anvil-check.log`) |
| AppBundle | `ls` | absent |

Mutations: each one rebuilds, then runs the filter `ElectricalXor|ElectricalOr|ElectricalNand|ElectricalNor|GateChooser|BothGate` (151 tests). Logs are in `mutations/`. The source was restored after each; the final `SupplyRoute.cs` diff is the two Xor arms only.

| Mutation | Red |
| --- | --- |
| M1 Xor as Or | 9: truth 11, order 11, reconvergence ×4, law, reads, lamps |
| M2 Xor as Xnor | 18 |
| M3 Xor as And | 15: truth 01/10/11, order 01/10/11, reconvergence controls ×4, save/Reset, Or-loop control, law, reads, lamps |
| M4 Xor declared monotone | 12: Xor loops ×4 natively and in the chooser, Or `WithXor` natively and in the chooser, graph invariant, law. `WithNand` is still refused via NAND, as expected |
| M5 Xor active without supply | 9: truth 01/10 without supply and in their supply-off latch phase, X5 supply-wire row, order 01/10, reads, lamps |
| M6 Refuse every cycle | 13: Xor Or-loop control natively and in the chooser, Or loops, contact loops, the NAND/NOR/Both Gate monotone loops |
| M7 Cyclic components seeded powered | 11: Xor Or-loop control, Or loops, contact loops, the NAND/NOR/Both Gate monotone loops |
| M8 Old whole-graph slot sweep | 7: Xor reconvergence gate-first ×2, NAND order and reconvergence, NOR order ×3 (J6) |

Retained failures: none.
