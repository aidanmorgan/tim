# Independent review: 9.7b Electrical NOR Gate (CAT-025)

## Pre-merge review — 22 October 2026

The reviewer is independent, did not implement this story, and made no edits to source, test, tool or content files.

- **Candidate:** uncommitted on `story-9-7b`. The base is `b35be7f444a199c2faaa8958d66c210947aad346` (main with 9.7a merged).
- **Identity manifest:** the tracked diff plus the SHA-256 of each untracked file, excluding this review's evidence. Saved as `evidence-9-7b-review/candidate-identity-manifest.txt`, SHA-256 `86f393f1c700443f6b44857936a0724b71ecb2eadabcc2709c8e7e37dc942ac0`, identical before and after this review.
- **Mutation probes** ran in a throwaway `git worktree` built from the base, the candidate's binary patch and the copied untracked files. After every revert, that worktree's diff was byte-equal to the candidate patch. It was then removed.

This is not a SnapshotApproval. The branch must first merge main after C-Balls publishes (PM1/PM2).

### Criterion / impact matrix

| Criterion / impact | Expected (derived) | Evidence | Disposition |
| --- | --- | --- | --- |
| Truth table via real power switches (9.7b AC1, CAT-025 R1/R2/R5, §5 Positive/Negative) | Supply comes from battery 7; the conditions are Battery → ImpactSwitch contacts; the consumer is the real Powered Gate. Row 00 opens the blade. For rows 01/10/11 the blade stays still on every tick. | `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` covers 8 rows. Per-tick `AssertBladeStill` on false rows; displacement > .2 m on 00. | Pass |
| No supply → nothing (AC2, todo-362) | Every row without supply is unpowered and the blade never moves. A true 00 row with the supply wire missing never powers. | Supply=false rows; `TrueRowWithoutSupplyWireNeverCreatesPower`. N3 (Nor active without PowerIn) → 15 red. | Pass, discriminated |
| Law, row by row | Each row is pinned independently | N1 (`!first`): row 01 red in truth, order, reads, lamps and law. N2 (XNOR): row 11 red in truth, order, reconvergence ×2, reads, lamps and law. Implementer Ma (Nand law) is consistent. | Pass, discriminated |
| Order independence (shared 9.7a resolve) | Bit-identical rows, power and blade for gate first/last × links forward/reversed, on every false row plus the 00 control | `PlacementAndWiringOrderGiveIdenticalCommittedResults` ×4. N5 (old slot sweep) → NOR order rows 01/10/11 red (00 passes), plus the two NAND ordering tests. Matches implementer Md. | Pass, discriminated |
| Reconvergence / same tick | Condition battery enabled from tick 0: output off and blade still on every tick, Reset exact. Disable gives a positive control; re-enable retracts. Both id orders. | `ReconvergentConditionKeepsTheOutputOffFromTheFirstTick` ×2; N2 turns both red | Pass |
| NonmonotoneFeedback (R6–R8): NOR→NOR, →Both Gate→, NOR↔NAND, open ImpactSwitch, PowerIn | Typed refusal naming the ids; wire-order independent. Save gives `InvalidConnection` with inner `NonmonotoneFeedback`. `Construct` gives Rejected/InvalidConstruction with the construction kept. Open control is admitted. | `ZeroDelayFeedback…` ×5; `NeitherGateChooserRefusesALoopClosingWireOnTheStatusLine` ×5 (no command, no Undo, status line). N4 (Nor monotone) → the 4 NOR-only loops, the invariant, the law and 4 chooser cases go red. MixedWithNand is correctly still refused, via NAND. | Pass, discriminated |
| Compiler invariant only as invariant | `CompiledSupplyGraph` throws `ArgumentException`; no player path reaches it | `CompiledGraphRejectsNonmonotoneCycleAsAnInvariant`. The engine refusal path is unchanged from 9.7a (call-site trace in the 9.7a review). | Pass |
| Monotone control | A source-free Both Gate loop is admitted and unpowered. The NOR it feeds settles after it and its 00 row opens the blade. | `MonotoneLoopIsAdmitted…` | Pass |
| Committed reads, lamps, neutral admission (R10) | Rows 00/10/01/11 supplied plus 00 unsupplied are committed exactly; tick 0 is neutral. Two forged rows (supplied 00 inactive, supplied 10 active) are refused. Native lamps go slate/gold per row and back to slate through Reset. | `CommittedReadsFollowEveryRowAndAdmissionIsNeutral`; `PictogramSingleTruthDotAndThreeLampsFollowCommittedRowsThroughReset`. N1, N2 and N3 turn both red. | Pass |
| Run/Reset (9R.1), Save/Load typed sockets (AC3) | Reset is exact with identical presentation bits. Bytes round-trip with connections equal and the gate instance present. Saved replay is identical. | `ClosingOneSwitchRetractsTheOutputAndSaveLoadAndResetAreExact` | Pass |
| Closed enum and undefined values (E2/E3, AGENTS) | `Nor = 2` is appended. Arms are exhaustive. 3 and 255 throw. The undefined pins moved 2→3 at exactly three sites (two in NandTests, one in BundleValidator); the 255 pins are kept. The bundle validator admits Nor. | Diff; `LawMonotonicityAndUndefinedValuesAreClosed` (NOR and NAND); BundleValidatorTests. N6 (`_ => false`) → NOR, NAND and BothGate undefined-rule tests red. | Pass, discriminated |
| Art (CAT-025 §2/R9, DESIGN "Supplied electrical logic"; A1-style deviation B1) | Icon equals `WorkshopIcons.cs@a6c914e:L78`; navy strokes follow that path; one gold dot `TruthDot00` at (−.21, −.4, .42) | `icon.svg` path is byte-equal to the legacy icon. The reviewer independently derived the 12 quadratic chord segments (s = .65/24, centre 12,12, y flipped): every mid-point, cos/sin and length (+.035 caps) matches the scene to 5–6 decimals. Bubble x .18958 (cx 19) and output stub x .27083 also match. Native test pins 16 strokes, the bubble x, z .4, navy colour and the single dot. | Accepted deviation (L2) |
| Package isolation / identity | Declaration, scene, icon and import only. No PartId branch, no cross-package reference, no record type. | `static-scope-checks.log`: no `electrical_nor`/`ElectricalNor`/`"Nor"`/"Neither gate" in production dirs. Schemas, PartId, compiler, `WorkshopConnections`, BothGate, NAND, ui and Simulation are unchanged. No NAND or BothGate reference in the package. UIDs `bg7psx5f4qufu` and `b8yy2eb8orpeo` are unique in the tree. The ctex hash equals md5(`res://elements/ElectricalNor/icon.svg`). | Pass |
| 9A.45a ratchet / solver scan | No Known entry, hash or total change. Element word "nor" is scanned. | The `WorkshopPhysicsLawTests` diff only adds `("norRow", true)`. N7 inserted `norRow`, `electricalNorTable` and `neitherGateRow` into `WorkshopPhysicsEngine.cs` → all flagged. Full native pass, including the frozen list. | Pass |
| Membership / pins (pre-merge values) | Each pin is +1 from post-NAND: registry 26, SaveFixture 26, ids+undelivered 28, palette rows, capacity, parity exclusion and Generic row, ports golden, static-gate csproj, ImpactRules, Coverage admitted 26 plus a path assertion. Planned example → `electrical_or` (CAT-026 exists). | Diff inspection. Bundle has 26 `PaletteOrder` entries with no duplicates; `electrical_nor` = 28. Full slnx and CI step pass. | Pass pre-merge; re-derive after C-Balls (PM1) |
| Docs | CAT-025 status, §2 enum note and §5 suite name; TODO; sprint-status `review` | Diff | Pass (L1) |

### Commands and results (raw under `evidence-9-7b-review/`)

| Command | Result |
| --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug -v:q` | 0 W / 0 E (`build-debug.log`) |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore -v:minimal` | native **2206/2206**, Coverage 77/77, BundleElements 145/145, E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx.log`) |
| CI "Test C# tools", `pages.yml` lines 51–64 verbatim under `bash -e` | Coverage 77, BundleElements 145, WorkerAotGuard 17, E2eRunner 174 (filtered); exit 0 (`ci-tool-step.log`) |
| `dotnet build CuriousContraptions.Simulation -c Release --no-incremental -v:q` | 0 W / 0 E (`worker-release.log`) |
| Focused named run (NOR, NAND, BothGate, both choosers, solver scan) | 103/103, including 31 NOR and chooser cases (`native-focused-named.log`) |
| Mutation probes: rebuild, then the filtered electrical, chooser and scan classes (`mutations/N*.{mutation-vs-candidate.diff,build.log,test.log}`) | N0 control 164/164. N1 `!first`: 5 red. N2 XNOR: 7 red. N3 no-supply NOR: 15 red. N4 Nor monotone: 10 red. N5 old slot sweep: 5 red. N6 undefined op → `false` plus N7 nor names in the solver: 4 red, with the scan reporting all 3 identifiers. |
| `anvil check --no-tui` on the implementer's 29 candidate files | No warnings (`anvil-check-candidate.log`). Over all untracked evidence, the only notice is 4 over-length lines in the implementer's raw `Mc` test log (`anvil-check-all.log`), which is raw output, not authored content. |
| `git diff --check`; identity hash before and after | clean; identical |

### Findings

Pre-merge conditions block SnapshotApproval. They are not candidate defects.

- **PM1 — merge with main after C-Balls; re-derive the pins.**
  - *Criterion:* the counts, ordinal and memberships hold on the exact snapshot to be published.
  - *Risk:* C-Balls touches most of the same pin lines and may deliver Tennis/Balloon. That would shift the registry, SaveFixture, ids+undelivered, palette-row, capacity and Coverage counts, and possibly `PaletteOrder` 28 uniqueness and the `ImpactRules`/`TODO.md`/sprint-status lines.
  - *Why blocking:* pre-merge pins are unproven for the merged inputs.
  - *Smallest resolving check:*
    - Merge main. Show `git diff <WIP> <merge>` touches only main's content plus conflict resolutions.
    - Re-derive each pin from the merged bundle, not +1 arithmetic.
    - One rebuild, then full slnx `--no-build --no-restore`.
    - The exact CI tools step under bash.
    - Release worker build, `PaletteOrder` uniqueness, and a named NOR rerun.
- **PM2 — exact identity.** Stage the merged candidate and record the tree and diff identity. Keep the implementer's `evidence-9-7b/` durable; it already is.

Non-blocking:

- **L1 — TODO brief.** `TODO.md` adds 9.7b but still carries the base's 9.7a "in flight / next SnapshotApproval" text. The 9.7a terminal receipt (Pass at `b35be7f4`) sits uncommitted in the `story-9-7a` worktree, awaiting carry. Reconcile at merge so the brief records 9.7a as verified and 9.7b as in flight.
- **L2 — pictogram deviation (B1).** The pictogram is shaded navy box-segment strokes, not the legacy unshaded textured quad. Geometry is verified exactly against the icon path. Readability at projected size is a queued browser case (`cat-025.test.ts`).
- **I1 — refusal text length.** The NOR feedback theory does not re-assert the 120-character budget. The text and cap are shared engine behaviour, proved by 9.7a's `LongLoopRefusal…`, and each NOR case names only two or three ids.
- **I2 — MixedWithNand.** The NOR↔NAND case is refused even when NOR is mis-declared monotone, because NAND alone forbids the cycle (N4). The NOR-only loops carry NOR's own nonmonotone proof.
- **I3 — Or chaining.** Or chaining (R6's Nor → Or → Nor) is substituted with Both Gate per E5. It stays pending under 9.8a.

No unresolved scoped unintended regression found in the candidate.

### Provisional verdict

**Pass-pending-merge.** All required-now 9.7b native criteria pass on the real C# simulation. Each law row, the supply rule, ordering, nonmonotone refusal, undefined-value closure and solver scan is mutation-discriminated. SnapshotApproval is withheld until PM1 and PM2 close on the merged snapshot after C-Balls. This is not browser or release qualification: `tools/e2e/cat-025.test.ts` remains queued post-catalogue.
