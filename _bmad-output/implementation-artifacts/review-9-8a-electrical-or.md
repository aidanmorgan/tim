# Independent review: 9.8a Electrical OR Gate (CAT-026)

## Pre-merge review — 22 October 2026

The reviewer is independent, did not implement this story, and made no edits to source, test, tool or content files.

- **Candidate:** uncommitted on `story-9-8a`. The base is `55ee05b96f949a7d93241b5ed0608b32db6bb8a0` (main with 9.7b NOR merged).
- **Identity manifest:** the tracked diff plus the SHA-256 of each untracked file, excluding this review's evidence. Saved as `evidence-9-8a-review/candidate-identity-manifest.txt`, SHA-256 `110f5742060a85124a6d866f879c10c578cae64d136c0187b0c12aef1a9616dd`, identical before and after this review.
- **Mutation probes** ran in a throwaway `git worktree` built from the base, the candidate's binary patch and the copied untracked files. After every revert, that worktree's diff was byte-equal to the candidate patch. It was then removed.

This is not a SnapshotApproval. The branch must first merge main after C-Bumper+Spring publishes (PM1/PM2).

### Criterion / impact matrix

| Criterion / impact | Expected (derived) | Evidence | Disposition |
| --- | --- | --- | --- |
| Truth table via real power switches (9.8a AC1/AC2, CAT-026 O1/O2/O5) | Battery 7 supplies; the conditions are real ImpactSwitch contacts; the consumer is a real Powered Gate. Rows 01/10/11 open the blade. Row 00 stays still on every tick. No row powers without supply. | `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` 8 rows, with per-tick `AssertBladeStill` on false rows. P1 (`Or => first`) turns the 01 truth, order, reads, lamps, law, contact-loop and Or-loop tests red. Implementer M1 (And) and M2 (Xor) cover rows 01/10 and 11. | Pass, discriminated |
| No supply → nothing (todo-362) | A true row (11) with the supply wire missing never powers, with control evidence that both conditions really hold | `TrueRowWithoutSupplyWireNeverCreatesPower`; implementer M4 (Or without supply) → 14 red | Pass |
| Order independence | Bit-identical rows, power and blade across gate first/last × links forward/reversed, every row | `PlacementAndWiringOrderGiveIdenticalCommittedResults` ×4. Or is monotone, so the old slot sweep cannot change an Or row (G11, accepted). P4 (old sweep) still turns NAND order, NAND reconvergence (gate first, now fed through the real Either) and NOR order 01/10/11 red. That is the same discrimination as the 9.7a/9.7b reviews (2 + 3). | Pass |
| **Monotone cycles (O6/O7):** admitted; least fixed point; source loss retracts; no stale carry; id-order independent | Two Either gates feed each other's FirstIn, supplied by battery 2, with a condition on one SecondIn. The run must show: off from tick 0 with no self-latch; on in the same tick the condition arrives; off on the next tick after the condition goes, and again after supply loss; identical across id orders. | `OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` ×2 (five phases). P2 (single pass in cycles, no fixed-point iteration) → `swapIds:True` red, which shows the id-order variant genuinely exercises intra-component order. P5 (previous-tick carry into cyclic components) → both Or-loop variants red, plus Both Gate/switch cycle tests. Implementer M5 (seeded powered) and M6 agree. | Pass, discriminated |
| Source-free pure Or loop (O7) | Save, `Construct` and validation admit it; it stays unpowered; Reset is exact | `PureOrLoopIsAdmittedBySaveAndConstruct`; chooser `OrToOr` (one Undo push, Undo restores); `CompiledGraphAdmitsAnOrCycleAndRejectsAMixedOne` | Pass |
| Contact loop (O8) | With an open contact closing the loop, Either stays on through its other input; Both Gate in the same place stays off | `ContactLoopKeepsAnEitherGateOnThroughItsOtherInput` ×2 (Either/Both control) | Pass |
| Mixed loops refused (O10): WithNand, WithNor, through an open switch, entering PowerIn | Typed `NonmonotoneFeedback` naming the ids; wire-order independent. Save gives InvalidConnection with inner NonmonotoneFeedback. `Construct` gives Rejected/InvalidConstruction with the construction kept. Chooser: status line, no command, no Undo. | `LoopsMixingANonmonotoneGateAreRefusedAtomicallyBeforeAnySolve` ×4; `EitherGateChooserAdmitsAnOrLoopAndRefusesAMixedOne` ×5. P6 (Or declared nonmonotone) turns the pure-loop admission, compiled-graph admission, contact loop, Or loop ×2 and chooser `OrToOr` red. | Pass, discriminated |
| Closing the 9.7a/9.7b Or substitutes (F5) | NAND D6 and NOR reconvergence route through a real Either fed on FirstIn only and assert `FirstOnly`/active. `ThroughOr` is added to the NAND and NOR feedback and chooser theories. `ThroughBothGate` is kept. | Diff review. P3 (Or as And) turns all four NAND/NOR reconvergence cases red, so the single-input Either proves the Or law. P4 shows the ordering discrimination is unchanged. No "undelivered Or" text remains. | Pass; changed tests still discriminate |
| Committed reads, lamps, neutral admission (O12) | Rows 00/10/01/11 supplied and 11 unsupplied are exact. Forged rows (supplied 01 inactive, supplied 00 active) are refused. Native lamps go slate/gold per row and back to slate through Reset. | `CommittedReadsFollowEveryRowAndAdmissionIsNeutral`; `PictogramThreeTruthDotsAndThreeLampsFollowCommittedRowsThroughReset`. P1 turns both red. | Pass |
| Run/Reset (9R.1), Save/Load typed sockets (AC3) | Reset exact with identical presentation bits; bytes round-trip with connections equal and the gate present; saved replay identical; the 00 row never moved the blade before the condition | `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` | Pass |
| Closed enum / undefined (F2/F3/F9) | `Or = 3` appended; exhaustive arms; the undefined pin moved 3→4 at every prior site (NAND ×2, NOR, BundleValidator); 255 kept; validator admits Or | Diff; law tests. P7 (`_ => false`) turns the NAND, NOR, Or and BothGate undefined-rule tests red. | Pass, discriminated |
| Art (CAT-026 §2/O11, DESIGN "Supplied electrical logic"; F6/G1 deviation) | Icon equals `WorkshopIcons.cs@a6c914e:L76`; navy strokes follow it; no bubble; gold dots 01/10/11 at x −.07/.07/.21 (y −.4, z .42) | `icon.svg` path is byte-equal to the legacy icon. The reviewer independently derived all 12 quadratic chord segments plus the stubs (len .1704 at x −.20313) and the output stub. Every mid-point, cos/sin and length matches the scene to 4–6 decimals. The dots, the absence of TruthDot00 and the 15-stroke count are pinned natively. | Accepted deviation (L2) |
| Package isolation / identity | Declaration, art and icon only; no PartId branch | `static-scope-checks.log`: the only production hits are the two `LogicGateOperation.Or` law arms in `SupplyRoute.cs`, which is the shared typed engine. Schemas, PartId, compiler, `WorkshopConnections`, the BothGate/NAND/NOR packages, ui and Simulation are unchanged. UIDs `00cjt3fjoi6p` and `dgjxmwg3f8fdk` are unique; the ctex hash equals md5(path). | Pass |
| 9A.45a ratchet / solver scan (F8) | The Known list, hash and `GenericIdentifiers` are unchanged; "or" is below the 3-letter floor; Or-specific names are caught through "electrical" | Diff adds only `("electricalOrRow", true)` and `("orRow", false)`; the full native run includes the frozen list and scan | Pass |
| Membership / pins (pre-merge) | Registry 27 (×2), SaveFixture 27, Coverage admitted 27, ids+undelivered 29, palette, capacity, parity, ports golden, csproj/slnx, ImpactRules. Planned example → `electrical_xor` (CAT-027 exists). | Independent derivation: 27 bundle entries, 27 package dirs, 2 undelivered, no `PaletteOrder` duplicates, `electrical_or` = 29; pin lines match | Pass pre-merge; re-derive after merge (PM1) |

### Commands and results (raw under `evidence-9-8a-review/`)

| Command | Result |
| --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug -v:q` | 0 W / 0 E |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2317/2317**, Coverage 77/77, BundleElements 147/147, E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx.log`) |
| CI "Test C# tools", `pages.yml` lines 51–64 verbatim under `bash -e` | 77 / 147 / 17 / 174; exit 0 (`ci-tool-step.log`) |
| `dotnet build CuriousContraptions.Simulation -c Release --no-incremental` | 0 W / 0 E |
| Focused named run (Or, NAND, NOR, BothGate, all choosers, scan) | 141/141, including 32 Or/Either-chooser cases (`native-focused-named.log`) |
| Mutation probes (`mutations/P*`): rebuild, then the electrical, chooser, switch and scan classes (202 tests) | P0 control 202/202. P1 Or=`first`: 8 red. P2 single pass in cycles: 3 red. P3 Or=And: 15 red (all 4 NAND/NOR reconvergence included). P4 old slot sweep: 5 red (NAND/NOR ordering). P5 previous-tick carry in cycles: 6 red. P6 Or nonmonotone plus P7 undefined→false: 10 red. |
| `anvil check --no-tui` on the implementer's 32 changed files; `git diff --check`; AppBundle | No warnings; clean; AppBundle absent |

### Findings

Pre-merge conditions block SnapshotApproval. They are not candidate defects.

- **PM1 — merge with main after C-Bumper+Spring; re-derive the pins.**
  - *Risk:* C-Bumper+Spring touches the shared pin and membership lines, `ImpactRules` and the TODO/sprint-status files. The pins and `PaletteOrder` 29 are unproven for the merged inputs.
  - *Smallest resolving check:*
    - Prove the merge brings only main's changes plus resolutions: `git merge-tree` recomputation, with NOR-style per-file classification.
    - Re-derive every pin from the merged bundle.
    - One rebuild, then full slnx `--no-build --no-restore`.
    - The CI tools step under bash.
    - Release worker build and a named Or/NAND/NOR rerun.
- **PM2 — exact identity.** Stage the merged candidate and record the tree and diff identity. The implementer evidence is already durable in `evidence-9-8a/`.

Non-blocking:

- **L1 — TODO brief.** `TODO.md` appends 9.8a but still carries the base's 9.7b in-flight text. The 9.7b terminal receipt (Pass at `55ee05b9`) sits uncommitted in the `story-9-7b` worktree, awaiting carry. Reconcile at merge.
- **L2 — pictogram deviation (F6/G1).** The pictogram is shaded navy box-segment strokes, not the legacy unshaded textured quad. Geometry is verified exactly against the icon path. Readability at projected size is a queued browser case (`cat-026.test.ts`).
- **I1 — ThroughOr cases.** The `ThroughOr` cases in the NAND and NOR theories are refused by the NAND/NOR gate, whatever Or is declared. They prove the closed CAT-024 D7 and CAT-025 R6 forms, while Or's own admission and refusal behaviour is carried by the Or suite (P6).
- **I2 — Xor forms.** The Xor forms of O9/O10 remain pending under 9.8b, as the spec records.

No unresolved scoped unintended regression found in the candidate.

### Provisional verdict

**Pass-pending-merge.** All required-now 9.8a native criteria pass on the real C# simulation. The monotone-cycle semantics are mutation-discriminated: least fixed point, fresh state every tick, source and condition loss, and id order. So are the law rows, the mixed-loop refusal and undefined closure. The re-routed NAND/NOR tests keep their prior ordering discrimination and now also prove the Or law. SnapshotApproval is withheld until PM1 and PM2 close on the merged snapshot after C-Bumper+Spring. This is not browser or release qualification: `tools/e2e/cat-026.test.ts` remains queued post-catalogue.
