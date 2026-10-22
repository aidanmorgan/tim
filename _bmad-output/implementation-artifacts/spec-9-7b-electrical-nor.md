---
title: "9.7b — Electrical NOR Gate"
status: review
baseline: b35be7f444a199c2faaa8958d66c210947aad346
context:
  - TODO.md
  - docs/planning/elements/CAT-025-electrical_nor.md
  - _bmad-output/implementation-artifacts/spec-9-7a-electrical-nand.md
  - docs/architecture/element-package-isolation.md
---

# Outcome and authority

Ship the package-owned "Neither gate" (CAT-025, EL-136).

- Authority: Story 9.7b AC; CAT-025 R1–R10 and §5; current-cat-025 and todo-362; CAT-013 G1–G15. NAND template: 9.7a D1–D6 and A1–A8.
- Output = supplied ∧ ¬(first ∨ second). Row 00 is the only true row, and it still needs real PowerIn energy (no lent energy).
- Deletes: no element record. No legacy path is replaced.

# Finite design decisions

- **E1 Engine reuse.** Use NAND's condensation-ordered `Resolve`, `SupplyCycles`, and its `NonmonotoneFeedback` refusal at `WorkshopConnections.Validate` (edit, save prevalidation, Construct) unchanged. Keep the L1 text and the 4-id cap. Nor is nonmonotone, so it is refused on every cycle, including through an open contact and via PowerIn (R6–R8).
- **E2 Enum.** Append `Nor = 2`. CAT-025 §2/R3 "value 3" is the legacy scene integer and is not carried forward (as NAND D4). No wire, schema or record change.
- **E3 Law.** The `Evaluate` arm is `!(first || second)`, and the `IsMonotone` arm is `false`. The switches stay exhaustive. No PartId or string classification.
- **E4 Inputs and consumer.** As NAND D5/A3: Battery → ImpactSwitch PowerIn→Supply contacts as the conditions, and Powered Gate (CAT-051) as the consumer. The supply battery stays Disabled while the balls latch the switches.
- **E5 Or substitute.** Or is still undelivered (9.8a), so it is replaced by Both Gate, and NAND where a nonmonotone peer is needed. Or chaining stays pending under 9.8a.
- **E6 Art (A1-style deviation).** Shared Both Gate body/foot/face/marks/lamps/ports. No converging bars.
  - Navy pictogram from `WorkshopIcons.cs@a6c914e:L78`: input stubs, the OR body's two quadratic curves and a concave back, an output stub, and a bubble at cx 19 r 2.
  - Curves are built from `BoxMesh` segments and the bubble from a `TorusMesh`, at the NAND z/scale. The same path ships as `icon.svg`.
  - One gold dot only, `TruthDot00`, at x −.21, y −.4, z .42 (R9). Delete the 01/10 dot nodes from the copied scene.
  - The stroke-count oracle is counted from the authored scene. Do not copy 13 (NAND red lesson).
- **E7 Suite name.** Use the story's `tools/e2e/cat-025.test.ts`, and correct CAT-025 §5 (`cat-024-025`).

# Tasks

- [x] **Enum and law.**
  - Add `LogicGateOperation.Nor = 2`, plus the `Evaluate` and `IsMonotone` arms.
  - Make the `SupplyCycles` doc comment's "(Nand)" generic ("a nonmonotone gate").
- [x] **Undefined pins move to 3.** There are exactly three sites: `ElectricalNandTests.cs:281`, `ElectricalNandTests.cs:282` and `BundleValidatorTests.cs:131`. Keep the 255 pins (`BothGateTests:169`, `BundleValidatorTests:132`). Add a Nor-admitted line beside `BundleValidatorTests:130`. Note: 9.8a moves the 3 pin again.
- [x] **Package `elements/ElectricalNor/`.**
  - `ElectricalNorElementDeclaration` uses named `ElementMetadata` arguments (`Id: "electrical_nor"`; PM1: the Coverage regex `\bId: "…"`).
  - Title "Neither gate", Control, colour (.4,.72,.79), and the CAT-025 description.
  - `PaletteOrder: 28` is provisional: verify it is unique in the generated bundle after a build and after C-Balls.
  - NAND's four ports, `logic` node, nine lamp bindings, and `Operation: Nor`.
  - Also: the scene (E6), icon.svg plus import, the csproj and the .uid.
  - No record type and no runtime PartId branch.
- [x] **Membership.**
  - Add to `CuriousContraptions.slnx`.
  - Add `ElectricalNorTests.cs` to `CuriousContraptions.tests.csproj`.
  - Bundle.
  - `tools/E2eRunner/ImpactRules.ElementPackages` gets `["ElectricalNor"]=new("electrical_nor")`.
  - Coverage: `CurrentCatalogueTests:197` admitted count +1, plus an `electrical_nor` → declaration-path assertion.
  - Coverage: `CurrentCapabilityInputsTests:45,87–88` move the planned-absent example to `electrical_or` / `elements/ElectricalOr`. `CapabilityCatalogueRequirements:34` has the entry.
  - The catalogue (72), source and fixture counts are unchanged.
- [x] **Count and ordinal pins.** (Pre-merge values; re-derive after C-Balls.) Each is the post-NAND value +1. Re-derive each one after C-Balls, which may deliver Tennis/Balloon and shift the registry and ids counts.

  | Pin | Change |
  | --- | --- |
  | `PartRegistryTests:22` | add to the list |
  | `PartRegistryTests:39` and `:130` | 25→26 |
  | `PartRegistryTests:261` (ids+undelivered) | 27→28 |
  | `BundleElementIdTests:81` (`SaveFixture.Elements`) | 25→26 |
  | `BasketballResourceTests:35` | add to the id list |
  | `BundlePortsTests:84` | add a golden row |
  | `CSharpStaticGateTests:638` | add the csproj |
  | `PackageRuntimeParityTests:201` and `:408` | exclude from the 9A.16 golden, and add the static row |
  | `WorkbenchCapacityTests:117` and `:260` | add |
  | `PalettePlacementTests` | add the NOR chooser theory |

- [x] **Explicit cleanup.** No parallel path, fallback or alias. No isolation Known entry. Nothing is deleted beyond the stale comment.
- [x] **Latest-design alignment.**
  - The package owns identity and art; the shared typed engine owns the law. f32 SmoothStep is unchanged.
  - Update the CAT-025 status, §2 enum note and §5 suite name, plus TODO.md and sprint-status.
  - Solver-scan word "nor": zero hits in the five shipped solver files (checked at b35be7f4), so no `GenericIdentifiers` change is expected. Rerun the scan to confirm.
- [x] **PRIMARY production C# unit tests (`ElectricalNorTests.cs`),** through the real compiler, worker device and Powered Gate motion:
  - **Truth table.** Run 00/01/10/11 × supply present/absent through real ImpactSwitch contacts.
    - Row 00 with supply moves the blade.
    - For 01/10/11 with supply, and for every row without supply, the consumer shows zero displacement on every tick, not only at the end. Ordering glitches are transient under NOR.
    - Also a missing-supply-wire row 00.
  - **Order.** NOR placed before or after its switch-fed inputs × forward/reversed links gives bit-identical logic rows, power and blade.
  - **Reconvergence.** Battery → NOR FirstIn, and battery → Both Gate → NOR SecondIn. Zero displacement on every tick from the first.
  - **Feedback.** Each case is a typed `NonmonotoneFeedback` naming its ids, with no Undo push and the construction unchanged:
    - NOR→NOR.
    - NOR→Both Gate→NOR.
    - NOR↔NAND mixed.
    - A loop closed only through an open ImpactSwitch.
    - A loop entering via PowerIn.
  - **Saves.** A looped save is refused with `InvalidConnection` (inner `NonmonotoneFeedback`). `Construct` gives `InvalidConstruction`. The compiled-graph invariant throws.
  - **Monotone control.** A source-free Both Gate loop is admitted and stays unpowered.
  - **Lamps.** Committed reads per row; tick-0 neutral admission; slate/gold lamps per row through Reset; the pictogram stroke count and one dot.
  - **Run/Reset** is exact against 9R.1. **Save/load** round-trips typed sockets.
  - **Rejections.** Undefined operation 3/255 and undefined socket 255 are rejected.
- [x] **9A.45a ratchet** passes with no new entry.
- [x] **One matching build, then reuse.** (Pre-merge snapshot; repeat on the merged snapshot.)
  - Build once, then run `dotnet test CuriousContraptions.slnx --no-build --no-restore` (native, Coverage, BundleElements, E2eRunner, WorkerAotGuard).
  - Run the CI "Test C# tools" step exactly as written in `pages.yml`.
  - Release worker build. Strict TS is not required: there are no TS/fbs Nand hits.
- [ ] **Publication.** Independent SnapshotApproval → protected commit/push → remote/tree identity, exact-SHA CI, Pages artifact and deployment → terminal `review-9-7b-electrical-nor.md`.

# Pending browser cases (post-catalogue, `tools/e2e/cat-025.test.ts`)

Real-UI construction: Battery to the bottom socket via the icon choice, switches to the one-mark/two-mark sockets, gate to Powered Gate. Cases:

- Four rows with supply, and 00 without supply.
- The lamp pair.
- The pictogram and the single dot at projected size.
- Reconvergence.
- A loop-closing wire (including through an open switch) refused on the status line.
- Reordered placement and wiring.
- Run/Reset and Save/Load.
- Or chaining (after 9.8a).

# Code Map

| File | Change |
| --- | --- |
| `CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs` | `LogicGateOperation.Nor` |
| `engine/gpu/SupplyRoute.cs` | `Evaluate`, `IsMonotone` arms; `SupplyCycles` comment |
| `elements/ElectricalNor/*` | new package |
| Membership/pins | slnx, tests csproj, `ImpactRules`, Coverage tests, the pin files above |
| Tests | `ElectricalNorTests.cs` (new), `ElectricalNandTests.cs` (pins), `BundleValidatorTests.cs`, `PalettePlacementTests.cs` |
| Docs | CAT-025, TODO.md, sprint-status |

Reuse unchanged: `CompiledSupplyGraph` ctor/`Resolve`, `WorkshopConnections.Validate`, `WorkshopEditRejection`, `WorkshopSavePrevalidation`, `BundleValidator` (`Defined`), the `elements/ElectricalNand/*` template, and the NAND/BothGate/SwitchElectrical test helpers.

Do not change: `workshop_wire.fbs`/generated code, `ElectricalLogicRecord`, `WorkshopRejection`, `PortSocket`/`WorkshopSocket`, `WorkshopPhysicsCompiler`, Both Gate/NAND behavior or art.

# Current state

The pre-merge candidate was committed as WIP `f443dc99`, with normal hooks and including the reviewer's review/evidence files. The pre-merge review is Pass-pending-merge ([review](review-9-7b-electrical-nor.md)). It was then merged with main `3fce7a03` (C-Balls) as `01cdd6cc` (see Post-merge below). Next: an exact SnapshotApproval of the merged snapshot, then protected publication. Browser qualification is pending post-catalogue (cases above).

## Post-merge (PM1, PM2, L1)

Merge `01cdd6cc9e9292ac4355d4206b5247cb9ef5983a`: tree `48d12b36f786934dd467f24d0f6fae647d8e7293`, parents `f443dc99` (WIP) and `3fce7a03` (main). Evidence: [`evidence-9-7b/post-merge/`](evidence-9-7b/post-merge/).

- **Conflict: `TODO.md` only.** Main's Verified and Also-verified lines are kept. In flight and Owner now name 9.7b, and C-Balls is recorded as merged to main `3fce7a03` (L1). Next reads "publish 9.7b after its SnapshotApproval". Every other file auto-merged, and sprint-status keeps main's rows plus 9.7b `review`.
- **C-Balls API.** C-Balls deleted `WorkshopInput.Basketball`. The two `SwitchMachine` callers in `ElectricalNorTests` now use `WorkshopInput.Generic(PartId.Basketball, …)`, exactly as main converted NAND's. No oracle changed.
- **Pins re-derived from the merged bundle (`pin-derivation.txt`).** C-Balls adds no package and no undelivered part.
  - 26 bundle entries and 26 package dirs, with 2 undelivered parts (ids+undelivered 28).
  - 26 distinct `PaletteOrder` values; `electrical_nor` is 28.
  - So registry 26, SaveFixture 26, Coverage admitted 26 and ids 28 all hold unchanged; no pin was edited.
- **Identity (`identity.txt`, `identity-checks.txt`).** Against main, the merge differs only in the slice's own files: 30 non-evidence paths. Against the WIP commit, the slice-owned changes are only the two resolutions above.
- **`git diff --check`.** It is clean on every non-evidence path and on the working tree. It flags trailing whitespace only inside committed raw tool output (`evidence-9-7b*/` logs, mutation diffs and the reviewer's manifest), which is kept unchanged as evidence (`diff-check-evidence-files.txt`).
- `CuriousContraptions.web/AppBundle` is absent from the worktree.

| Check | Command | Result |
| --- | --- | --- |
| One rebuild of the merged tree | `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E (`build-debug.log`) |
| Whole solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2278/2278** (main 2245 + 33 NOR), Coverage 77/77, BundleElements 145/145, E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx.log`) |
| CI "Test C# tools" step, verbatim, under bash | `evidence-9-7b/ci-tool-step.sh` | Coverage 77, BundleElements 145, WorkerAotGuard 17, E2eRunner 174 filtered; exit 0 (`ci-tool-step.log`) |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release` | 0 W / 0 E (`worker-release.log`) |
| Named NOR, chooser and scan tests | same filter as below | 56/56 (`native-nor-named.log`) |
| Merge commit hooks | `git commit` (normal pre-commit) | anvil 0 errors; gate tests 124/124 |

## Assumptions and deviations

- **B1 Pictogram (E6).** The pictogram has 16 navy strokes, counted from the authored scene:
  - two input stubs (NAND's transforms; same `M2 7h4`/`M2 17h4`);
  - upper OR curve (5,4)–(14,4)–(17,12), lower curve (17,12)–(14,20)–(5,20) and concave back (5,20)–(11,12)–(5,4), each as four chord `BoxMesh` segments;
  - a `TorusMesh` bubble at cx 19 (x .18958);
  - an output stub 21→23 (x .27083).

  The mapping is NAND's: s = .65/24, centre (12,12), y flipped, z .4, length = chord·s + .035. Six chord-length meshes are used; the derivation is in `evidence-9-7b/nor_pictogram.py`. There is one `TruthDot00`, and the 01/10 dots are removed. load_steps is 25.
- **B2 Undefined value 3.** The NOR test pins 3 and 255 for both `Evaluate` and `IsMonotone`.
- **B3 Order test** is a theory over three false rows (01, 10, 11) plus the 00 positive control. In a false row, a gate visited before its switch-fed inputs would latch on.
- **B4 Reconvergence.**
  - The condition battery is enabled from tick 0: the output stays off and the blade stays still on every tick against the built pose, with Run/Reset exact.
  - Then a disable phase opens the blade (positive control), and re-enabling retracts it on the first tick.
  - Both id/placement orders are run.
  - FirstIn is fed directly, so this is a reconvergence/same-tick proof, not ordering evidence. B3 carries the ordering evidence: mutation Md, the old whole-graph slot sweep, fails exactly the three false-row order cases and passes the 00 row.
- **B5 Feedback cases** add `MixedWithNand` (NOR↔NAND). The real-chooser theory (`PalettePlacementTests`) runs all five cases: no command, no Undo, construction unchanged, and the refusal on `_status`.
- **B6 Admission.** Two forged rows are refused: a supplied 00 left inactive, and a supplied 10 made active.
- **B7 Scan.** `("norRow", true)` was added to the general-scan theory. The existing `contactNormal` row shows that "Normal" is not "nor". `GenericIdentifiers`, the Known list and its hash are unchanged.
- **B8 CI step shell.** The first run of the step under zsh did not word-split `$toolchain`, so it ran all 189 E2eRunner tests. It was rerun under bash, as Actions does (`ci-tool-step.sh`), and the filter gives 174.
- **B9 Generated identities.** The build's Godot import generated the `.cs.uid` (`uid://bg7psx5f4qufu`) and the `icon.svg.import` (`uid://b8yy2eb8orpeo`; ctex hash = md5 of `res://elements/ElectricalNor/icon.svg`). Both are unique in the tree.
- **Anvil.** Most writes were pre-validated (`allow`). Some later doc/spec/checkbox edits were not individually pre-validated. All 29 changed/new candidate files (`changed-files.txt`) pass `anvil check --no-tui` with "No warnings found".

## Evidence (`evidence-9-7b/`)

| Criterion | Command | Result |
| --- | --- | --- |
| Build | `dotnet build CuriousContraptions.slnx -c Debug` | 0 W / 0 E (`build-debug-final.log`) |
| Whole solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2206/2206** (base 2173), Coverage 77/77, BundleElements 145/145 (base 143), E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx-final.log`) |
| CI "Test C# tools" step, verbatim, under bash | `ci-tool-step.sh` | Coverage 77, BundleElements 145, WorkerAotGuard 17, E2eRunner 174 filtered; exit 0 (`ci-tool-step.log`) |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release` | 0 W / 0 E (`worker-release.log`) |
| NOR, chooser and scan tests by name | `--filter ElectricalNor\|NeitherGateChooser\|TheGeneralScan\|TheShippedSolver\|KnownViolation` | 56/56 (`native-nor-named.log`) |
| Mutation: Nor law as Nand | rebuild + NOR filter | 8 fail: truth rows 01/10, order 01/10, law, committed reads, save/Reset, lamps (`mutations/Ma-*`) |
| Mutation: reverse component order (non-specific: the consumer is visited before the gate) | rebuild + NOR filter | 12 fail: every supplied truth row, all 4 order rows, reconvergence ×2, monotone loop, save/Reset (`mutations/Mb-*`) |
| Mutation: old whole-graph slot sweep (9.7a M1b) | rebuild + NOR filter | exactly 3 fail: order rows 01/10/11 (gate-first variants latch on); order row 00 and all other NOR tests pass (`mutations/Md-*`) |
| Mutation: Nor declared monotone | rebuild + NOR/chooser filter | 8 fail: 4 Nor-only loops, invariant, law, chooser NorToNor/open switch. MixedWithNand is still refused, through NAND (expected) (`mutations/Mc-*`) |
| Ordinals | generated bundle | 26 elements, 26 distinct `PaletteOrder`, NOR = 28 (`palette-ordinals.txt`) |
| No new PartId/string classification; solver word "nor"; Known list | rg / git diff (`static-scope-checks.log`) | No production hits. The five solver files have no "nor" token. The Known list, totals and hash are unchanged. `git diff --check` is clean. |
| Anvil | `anvil check --no-tui <29 files>` | No warnings found (`anvil-check.log`) |

The mutation runs (Ma, Mb, Mc, Md) used the pre-chooser-extension test set. After each mutation, the source was restored exactly before the final build; `git diff engine/gpu/SupplyRoute.cs` shows only the two Nor arms and the comment.

Retained failures: none.
