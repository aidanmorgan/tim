# Review: Story PKG-3, Delay Box and Impact Switch Element Packages (9A.7 + 9A.13)

Reviewer: Murdoch-20 (independent; no deliverable edited). Round 1 of at most 2. Date: 21 Oct 2026.

## Verdict

**Pass (round 1). No blocking finding.**
- Every AC of 9A.7 and 9A.13 holds as PKG-3 scopes them. The stories' "every e2e suite in one serial run" clause is superseded by the owner tiers and PKG-3 AC3 (her CL5, as PKG-1 CL8 and PKG-2 CL4 ruled), and was not run.
- Both deletions are made, and each has a re-add mutant: four killed. The stray-copy mutant survives as the already-filed F15-3.
- No player-visible regression: no runtime file changed, the two scenes are byte-identical renames, and no runtime project reads the bundle. Her identity-verified Chrome run of the latch, animation and palette consumers passes.
- No reopened-record closure row names PKG-3, so no addendum is due.

Findings: none blocking. Three are filed to **Story F.15, "Package Follow-ups"** (F15-10 to F15-12, below).

## Candidate and identities

| Item | Identity |
| --- | --- |
| Candidate worktree | `.claude/worktrees/agent-a26d85b964fcb4310` |
| Review head | `8b646ffe` (docs only over the merge) |
| Code candidate | `d925637c` (parent `d4195fb0`) |
| Merge | `6f2b3b74` = `d925637c` + main `a8a3eab1`. `git diff --stat d925637c 8b646ffe -- . ':(exclude)_bmad-output'` is empty |
| Clean review copy | `.claude/worktrees/murdoch20-pkg3`, detached at `8b646ffe`, fresh (no bin/obj), `git status` empty before and after every check (`tier1/identity.txt`) |
| Mutant copy | `.claude/worktrees/murdoch20-mut`, detached at `8b646ffe`, restored by `git reset --hard` after each mutant |
| Identity copy | `.claude/worktrees/murdoch20-id`, detached at `d925637c` |
| Evidence | [evidence-9a-pkg-3-review/](evidence-9a-pkg-3-review/) |

## Scope

`git diff -M d4195fb0 d925637c` is 20 files, exactly the PKG-3 set:
- two new packages (`elements/DelayBox`, `elements/ImpactSwitch`: csproj, declaration, `.cs.uid`) and the two scenes as 100% renames;
- the two `.tres` scene pointers;
- the slnx, `CSharpStaticGateTests.TrimAnalyzedProjects`, `ImpactRules.ElementPackages`, `SuiteCatalogue` (`Pkg3`);
- `BundleCatalogueParityTests` (unpackaged list, `Placed` and `Parameters` arms), `PackageReferenceTests`, `SelectionTests` rows;
- the new suite `tools/e2e/pkg-3.test.ts` with its README row, and the isolation doc's A15 row.

No engine, ui, worker, wire, solver or web-host source changes. The web csproj only generates and validates the bundle at build time; no runtime project reads it (`identity/runtime-reach.txt`). So the scene moves, reached through the `.tres` paths, are the only runtime-visible change.

## Acceptance criteria

| AC | Evidence | Result |
| --- | --- | --- |
| 9A.7. `DelayBox.csproj` added with Abstractions plus `.Signal` | References exactly those two. My **A9** (adds `.Mechanical`) is killed by `PackageReferenceTests(DelayBox)`. | Pass |
| 9A.7. Bundle has `delay` with a typed duration parameter; default and range equal `DelayDuration` | `duration_seconds`, Second, Real, 0.1 to 12, default 1, checked against `WorkshopInput.Delay` at both bounds and one Half step outside. Her M1/M2 killed; my **A6** (unit None) killed. | Pass |
| 9A.7. Activation in/out ports equal the `delay` port arms | `ActivationIn` (−0.72, 0, 0) and `ActivationOut` (0.72, 0, 0), Pulse, compared with `WorkshopPorts.For`/`LocalPosition` bit for bit. Her M4 killed; my **A3** (out direction Input) killed. | Pass |
| 9A.7. Footprint and collider arms equal | One static body; foot box equal to the compiled record at the binary16 lane, with material. Her M3 killed; my **A4** (friction 0.31) and **A8** (`MovingBodies` 1) killed. | Pass |
| 9A.7. `delay.tres` category "Control" | `…CatalogueMetadataAndCategory`; my **A7** (Power) killed. The title is not compared: F15-10. | Pass |
| 9A.13. `ImpactSwitch.csproj` added; bundle has `switch` | Abstractions, `.Mechanical`, `.Signal`, no Electrical (her CL3; 9.5b adds it). Her M9 killed. | Pass |
| 9A.13. `ContactTriggerSettings` threshold default and range | `trigger_threshold`, MetrePerSecond, Real, 0 to 64, default 0.8. Her M6 killed; my **A5** (max 65) killed. | Pass |
| 9A.13. Port, footprint and collider arms; category "Power"; the static body | `ActivationOut` (0.45, 0, 0.4) Pulse; base and button boxes; Static, mass 0. Her M7, M8, M10 killed; my **A1** (port z 0.41), **A2** (Level), **A10** (mass 0.5), **A11** (button dropped) killed. | Pass |
| 9A.7 and 9A.13. `CosmeticCurves.Delay` and `.ImpactSwitch` | **Ruling on her CL1: accepted.** The Abstractions have no presentation-binding contract, and Story 9A.25a's Deletes list names both arms, so the deferral follows the plan (as PKG-1 CL2 did for the Receiver). | Pass (deferred to 9A.25a) |
| PKG-3 AC2. `git ls-files` returns nothing for both moved scenes; unpackaged set is exactly `tennis` and `balloon` | `identity/deletion-check.txt`: empty `ls-files`; `parts/scenes` holds the 13 scenes 9A.14 moves. `TheCatalogueIdsWithoutAPackageAreExactlyTennisAndBalloon` passes. | Pass |
| PKG-3 AC3 / 9A.7 / 9A.13 named suites pass once on the merged candidate | Her run `20261021T022133Z-53936` reused after its identity reproduced (below): anim-1b 3/3, anim-1c 4/4, cat-015a-sidekick 3/3, cat-015b 4/4, cat-037 6/6, cat-062 7/7, cat-062b 7/7, engine-core-2c 2/2, all Clean. | Pass |
| 9A.7 and 9A.13. `dotnet build` 0 warnings, `dotnet test` 100% | Tier 1 below. | Pass |

Ruling on the Givens' title clause. 9A.7 and 9A.13 name the `.tres` title ("Delay", "Impact switch"). The Abstractions carry only `DisplayNameKey` (`part_delay`, `part_switch`), which resolves to nothing in the tree, and the parity guard compares no title. Her spec has no CL for this. PKG-1's Givens had the same clause ("Receiver", "Bowling ball"), and Murdoch-18 passed it without ruling. The title is player-visible only through the `.tres` today, which is unchanged. Story 9A.14 owns it: its palette AC requires "the same titles, categories and order the player saw before". So this does not block, and it is filed (F15-10).

## Deletions: re-add mutants (raw output in `mutants/<name>.txt`)

| Mutant | Target | Result |
| --- | --- | --- |
| D1 | `delay` put back in the guard's unpackaged list | Killed (`TheCatalogueIdsWithoutAPackageAreExactlyTennisAndBalloon`) |
| D2 | `switch` put back in the guard's unpackaged list | Killed (same test) |
| D3 | Delay move reverted: scene `git mv`'d back to `parts/scenes/`, `delay.tres` re-pointed, package unchanged | Killed (`…CatalogueMetadataAndCategory`, `ScenePath`) |
| D4 | the same for the switch | Killed (same test) |
| D5 | stray copies of both scenes re-added under `parts/scenes/`, `.tres` untouched | **Survives** the whole `dotnet test` (2236 pass). This is F15-3, already filed. AC2's `git ls-files` check holds. |

Her M5 (package `ScenePath` back to `parts/scenes`) is the complementary direction to D3 and is killed in her raw `mutants.txt`.

## Value mutants (reviewer's own)

A1 to A11, as in the AC table: all eleven are killed by the named tests. Baseline `M0-baseline.txt`: 8/8 parity facts and 25/25 package tests green before any mutant. Every mutant build reports `0 Error(s)`, and each killing failure names that mutant's own value (for example A1 "position <0.45, 0, 0.41> declared", A10 "mass 0.5 declared", D2 `Expected: "switch"`), so no kill comes from stale binaries.

Probe **P1**, switch `Triggers[0].Threshold` 0.8 → 0.5: it **survives** the whole `dotnet test` (2236 pass). No parity test compares a package's `Triggers`, `Events`, `LatchMode`, `Counter` or port `Normal` with the runtime. The runtime has no port normal (`identity/runtime-reach.txt`), so a Normal probe would survive by construction and was not run. Nothing reads the bundle at runtime, so this is not player-visible today. Filed as F15-11.

## Chrome: her run reused, identity reproduced

- Her run `20261021T022133Z-53936`, `run --changed` over her suite list on `d925637c`, from 02:21:33Z to 02:36:19Z. Verdict Pass: 14 files, 79/79, every observation Clean, closeFindings 0, debtFaults 0. Bundle `1253382d…2936`, the same at start and end. Chrome 155.0.8059.86.
- **Identity.** From a clean `d925637c` (`identity/source-digests-d925637c.txt`):
  - `suiteSourceDigest 2a57688b…` and `runnerSourceDigest 9a93b7ca…` reproduce exactly.
  - `sourceStatusDigest 6a944b04…` reproduces with exactly one dirty set out of 2,048 searched (`identity/status-digest-match.txt`; script `identity/status-brute.sh.txt`). That set is her 14 evidence files from before the run, plus the run-diff publish log and refusal, the run's own stdout log and publish log, the untracked spec, and modified `epics.md` and `sprint-status.yaml`. Every dirty path is under `_bmad-output`, none is code, so the run applies to the code candidate.
- So I reused it and re-ran no suite (End-to-end scope rule).
- **Selection, derived independently** (`identity/selection-derivation.txt`).
  - Every `SuiteCatalogue` entry whose `Places` holds `PartId.Delay` or `PartId.ImpactSwitch`: cat-037 (latch), anim-1b, anim-1c, cat-052-lamp, capacity-9r5b and pkg-3.
  - Levels: of the 28 `content/puzzles.json` levels holding a switch or delay, only `delayed_signal` is opened by any suite (anim-1b, anim-1c).
  - cat-005, cat-017, cat-020 and clock-master place neither part. No clock or counter suite places them; cat-052-lamp uses the Delay and Counter only as refused plate targets.
  - Adding the AC-named suites, palette-placement (bench placement from the published bundle) and the smoke pair gives exactly her 14.

## Player-visible regression check: the Delay timing and the Switch contact

- The delay timer, `DelayDuration`, `ContactTriggerSettings`, the activation network and the solver are untouched, and the scene bytes are identical. The only runtime change is two `res://` paths.
- In her run:
  - **cat-037**: switch → Set; switch → Delay (1 s) → Reset; persistence;
  - **anim-1b**: switch depression curve; the Delay fill rises monotonically to 1 and the lamp lights after it;
  - **anim-1c**: Delay fill during hints;
  - **pkg-3**: switch → Delay → lamp twice, with exact Reset, the unwired control and Save/Load;
  - **palette-placement**: both parts placed from their moved scenes.
  
  All pass.
- No suite asserts the Delay's duration to the tick. That proof is cat-022, owned by Story 9R.17 (with 9.5a); for the switch it is cat-063, owned by 9R.15 (with C-Switch), as her CL4 records. Not a PKG-3 gap.

## Tier 1, on the clean copy at `8b646ffe`

| Check | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx` (Debug, then Release) | 0 warnings, 0 errors, both | `tier1/build.log`, `tier1/release-build.log` |
| `dotnet test CuriousContraptions.slnx` | 2236/2236 (1897 + 164 + 80 + 78 + 17), equal to her count | `tier1/test.log` |
| C# static gate (`CSHARP_GATE_STAGED_HOOK=1 sh .githooks/pre-commit`) | exit 0; three builds 0 warnings; 123/123 | `tier1/pre-commit-hook-8b646ffe.log` |
| `node --experimental-vm-modules --test tools/*.test.mjs` | 88/88 | `tier1/node.log` |
| LifecycleContract (Release) | 510, 0 failures | `tier1/lifecycle.log` |
| WireContract (Release) | 108, 0 failures | `tier1/wire.log` |
| Ownership `--oracles .` (after the web restore) | exit 0, Positive, 22/22 attacks rejected; line 1 byte-identical to hers (SHA-256 `7b72a39c…`) | `tier1/ownership-oracles.log`, `tier1/web-restore.log` |
| `anvil check --since d4195fb0` | 1 changed non-C# file checked, no warnings | `tier1/anvil-check.log` |

Timestamps and status counts: `tier1/identity.txt` (02:42:10Z to 03:01:00Z).

## Findings (all filed to Story F.15; none blocking)

- **F15-10. Package titles are not carried or compared.** The Givens of 9A.5, 9A.6, 9A.7 and 9A.13 name the `.tres` title, but `ElementMetadata` has only `DisplayNameKey`, and `part_<id>` resolves to nothing in the tree. When Story 9A.14 deletes the `.tres` files, the palette needs a title source, along with the `.tres` `Description` and `Color`, which no package declares either. Owner: Story 9A.14, whose palette-title screenshot AC must cover it. Her spec has no CL recording this, and PKG-1's review did not rule on it.
- **F15-11. Signal declarations are not parity-guarded.** Probe P1 (the switch trigger threshold 0.8 → 0.5) survives all 2236 tests. No test compares `Triggers`, `Events`, `LatchMode`, `Counter` or port `Normal` with a runtime value. The switch's trigger threshold duplicates the `trigger_threshold` default with no check that they agree, and the Delay's `activation` trigger (Threshold 1, 0.1 s) has no runtime counterpart. Not player-visible while nothing reads the bundle. It must be guarded before any cutover reads these fields (9A.14 / 9A.17 / 9A.22).
- **F15-12. One selection reason is mis-described.** Her spec lists cat-052-lamp as "the counter, latch and clock chain with a Delay". The suite places the Delay only as a refused plate target. The selection is still right, because its `Places` holds `PartId.Delay`.

Already filed, re-observed: F15-3 (stray scene copy survives; D5).

## Process

- Every mutant was made with the Edit tool (or `git mv`/`cp` for file moves) in the disposable mutant copy, never in a deliverable. Each was recorded with its diff and restored to an empty status.
- This record's content was validated with `anvil_validate_write` before it was written.
- I edited no deliverable, and the closure table assigns PKG-3 no addendum.
