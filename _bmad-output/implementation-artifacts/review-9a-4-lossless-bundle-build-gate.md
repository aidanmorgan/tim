# Review: Story 9A.4, Lossless Bundle Build Gate

Reviewer: Murdoch-17 (independent; no deliverable edited). Round 1 of at most 2. Date: 21 Oct 2026.

## Verdict

**Fail (round 1), with one blocking finding.**

**B-1. AC4: a body without a collider does not fail the build.**
- **What happens:** BUNDLE004 is checked only for `IMechanicalElement` declarations. But five real packages declare a body in their footprint (`Footprint.Bodies` 1) without being mechanical: battery, counter, latch, lamp and clock.
- **Proof (mutant M25):** emptying the lamp's `Colliders` still gives `Build succeeded` and a 13-element bundle. Probe P3b gives the same result with a fixture: a non-mechanical declaration with `Footprint(1, 0, 0)` and no colliders is admitted.
- **Why it blocks:** AC4 says generation fails "for … a body without a collider". The footprint this story added is the package's own declaration of its bodies. Parity would catch the lamp mutant in `dotnet test`, but the AC requires the generation step itself to fail the build.
- **Fix:**
  - raise BUNDLE004 for any declaration whose `Footprint.Bodies` is above 0 with no colliders. "Fewer colliders than bodies" would wrongly refuse the gate: its blade is Story 9A.16's;
  - add a non-mechanical fixture as a negative control, keeping the mechanical row.
- **Round 2 re-review:** Tier 1, BundleElements.Tests, and M25 and P3b again.
  - The fix changes only `tools/BundleElements` and its tests. No served file changes, and the bundle is not in the publish output (`.pck` inclusion is 9A.14's job).
  - So the implementer's full run stays applicable, and Chrome only needs the runner's selection for the fix diff, if any. The runner selects Full for `tools/BundleElements` as SharedDriverInfrastructure, so the coordinator should rule on reusing the full run.

No other finding breaches an AC, and none is a player-visible regression. Seven findings are filed to **Story F.14, "Bundle Gate Follow-ups"** (below). The implementer's runner follow-ups, which she called "F.13", belong in F.14 too, because main's F.13 is now the 9A.1 follow-ups.

## Candidate and identities

| Item | Identity |
| --- | --- |
| Candidate worktree | `.claude/worktrees/agent-a701795d6eefb0fce` |
| Code commit | `c948fec6` |
| Candidate HEAD | `b8910e92`. Not quite "evidence only": it also edits the doc comment of `BundleCatalogueParityTests` (3 comment lines, test-only, no browser-served file). Run reuse is unaffected. |
| Candidate base | `fda80021` (main plus 9A.1 at `844981c9`) |
| Main | `980e222d` (9A.1's final code is `b71d32fd`) |
| Review merge | `ec66da08`, in worktree `.claude/worktrees/murdoch17-9a4` (detached). Command: `git merge --no-ff -X theirs 980e222d`. |
| Evidence | [evidence-9a-4-review/](evidence-9a-4-review/) |

## Merge check

- Five files conflicted:
  - `CuriousContraptions.tests/WorkshopFormatTests.cs`, where 9A.1 round 2 moved a method;
  - `docs/engine-contracts.md`, `docs/architecture/element-package-isolation.md`, `epics.md` and `sprint-status.yaml`, which had 9A.1 round-1 versus final text.
- 9A.4 changed none of the conflicted hunks, so main's side was taken for each. 9A.4 never touched `WorkshopFormatTests.cs`, so that file is byte-identical to main.
- **The resolution matches 9A.1's final code.**
  - `git diff 980e222d ec66da08`, outside `_bmad-output/` and `docs/`, is byte-identical to the candidate's own diff `git diff fda80021 b8910e92` (46 files, +1502/−963).
  - The same holds for `docs/`.
  - So every 9A.1 round-2 change survives, and 9A.4 adds exactly its own change.
- **Planning items for the coordinator's merge:**
  - M-1. The implementer's runner follow-ups ("Story F.13: E2E Runner Selection Follow-ups" in `epics.md`, and `f-13-e2e-runner-selection-follow-ups` in `sprint-status.yaml`) clash with main's F.13. My resolution dropped them. Re-file them under F.14 (below).
  - M-2. The spec's Implementation Notes say "Story F.13 (runner)". They should say F.14.
  - M-3. Main's F.13 item F13-2 ("remove the `"Version": "1.0"` in the bundle JSON, if 9A.4 hasn't already") is satisfied by this story. The tracked bundle is gone, and the generated one has no `Version` (`deletion-greps.txt`).

## Acceptance criteria

| AC | Evidence | Result |
| --- | --- | --- |
| AC1. The slnx contains every `elements/*` project, the four domain Abstractions and `tools/BundleElements` | `SolutionMembershipTests.TheSolutionContains…` globs `elements/*/*.csproj` and requires each one. Mutant M01, which drops `elements/Wall` from the slnx, is killed. | Pass |
| AC2. An MSBuild target runs before the Godot export and before publish, discovering packages by glob and reflection, with no hand list | `GenerateElementBundle BeforeTargets="TwoDogExportGamePack;_WasmGenerateAppBundle"`. My merge-run `publish.log` shows `Element bundle … (13 elements)` before the Godot export (line 56) and again before `CuriousContraptions.web -> …/publish/` (line 246). `RunElementBundleGenerator AfterTargets="Build"` runs in `dotnet build CuriousContraptions.slnx` (`tier1/build.log`) and in the Release build. `PackageDiscovery` globs and reflects. M02 (a hand `ProjectReference` re-added) and M03 (the `_WasmGenerateAppBundle` hook dropped) are killed. | Pass |
| AC3. Typed schema with one schema, no version and no timestamp; vectors `[x,y,z]` and quaternions `[x,y,z,w]` bit-exact; mechanical, electrical and signal payloads; a typed parameter schema; an enum category | The generated `parts/elements.bundle.json` has 13 elements, root key `Elements` only, no `{}`, and no `Version` or `GeneratedAt`. M04 (`GeneratedAt` re-added), M05 (`Version` re-added) and M08 (one vector component dropped) are killed, and so is M20 (enum numbers allowed). The reader leniency in F14-1 is filed: no runtime reader exists until 9A.14, and generation and round trip are unaffected. | Pass |
| AC4. Generation fails the build, naming package and field, for a duplicate id, a non-finite value, an unknown enum and a body without a collider | The validator fixtures pass. Mutants M09 (duplicate check), M10 (finite check), M11 (enum check), M12 (collider check) and M13 (the declaration count) are each killed. M14 plants a NaN in the real Ball package: `dotnet build BundleElements.csproj` fails with `error BUNDLE002: Ball.Colliders[0].HalfExtents.x: not finite`. **But M25 (the lamp's colliders emptied) builds, and so P3b's fixture is admitted.** | **Fail (B-1)** |
| AC5. Round trip of every package, f32 bit for bit | `EveryPackageEqualsItsDeserialisedEntry` runs for all 13 packages and compares with `SingleToInt32Bits`. -0 and the smallest subnormal are covered. M08 is killed. | Pass |
| AC6. Parity with `.tres`, `WorkshopPorts.For`, `WorkshopCanonicalColliders.For`, footprint and `CaptureInstance` defaults; the unpackaged set is exactly 11 ids | Killed: M15 and M21 (category), M24 (scene path), M18 (port position), M19 (footprint), M22 (collider extent), M17 (clock default). The unpackaged-set test lists the exact 11 ids. The footprint comparator is a one-instance compile, because 9R.5b deleted `WorkbenchFootprint.Of` (CL2; accepted). Mass and inertia are not compared (F14-3); the AC does not list them. | Pass |
| AC7. Package categories adopt the catalogue's categories | All 13 equal their `.tres` `Category` (checked by hand against `parts/catalog/*.tres` and by parity). Palette grouping is unchanged. | Pass |
| AC8. 0 warnings, `dotnet test` 100%, and every e2e suite 100% | Tier 1 on the merge is below. Chrome: the implementer's full run was reused after identity checks, and the merge-diff selection was run. | Pass |

## Deletions: grep and re-add mutants

| Deleted item | grep on the merge (`deletion-greps.txt`) | Re-add mutant |
| --- | --- | --- |
| The hand list of 13 declarations and the element `using`s | `rg "ElementDeclaration\|using CuriousContraptions.Elements" tools/BundleElements` finds only `IPuzzleElementDeclaration` | M02 is killed (`EveryPackageIsDiscoveredByGlobWithNoHandList`) |
| The 13 `ProjectReference`s | One `elements/*/*.csproj` glob, plus the 5 Abstractions | M02 is killed |
| The anonymous serialiser and the `AppBundle` copies | `rg AppBundle tools/BundleElements` returns 0 | M28 is killed. It re-adds a third `--out …/AppBundle/elements.bundle.json` in the Exec, and `CSharpStaticGateTests.RepositoryMsBuildLogicIsOnlyThePinnedTargetsAndImports` pins the target. (A copy re-added inside `Program.cs` would only be caught by grep.) |
| `GeneratedAt` | `rg GeneratedAt\|DateTime` returns 0 in the Abstractions, `elements` and `tools/BundleElements` | M04 is killed |
| Bundle `Version` | `rg '"Version"\|SchemaVersion'` returns 0, and none in the generated JSON | M05 is killed (determinism test, root keys) |
| `ElementMetadata.Parameters` bag | `rg "IReadOnlyDictionary<string, object>" ElementMetadata.cs` returns 0 | **M06 survives** (F14-2): an empty bag parameter re-added to `ElementMetadata` passes the bundle tests and parity |
| String `Category` | `rg "string Category"` returns 0 | M26 is killed: the build fails with CS1503 in every package |
| `ColliderDescriptor.Dimensions` | `rg -w Dimensions` returns 0 | M27 is killed: the build fails with CS1739 (`HalfExtents`) in the packages |
| The tracked `parts/elements.bundle.json` | `git ls-files` returns nothing; `git check-ignore` hits `.gitignore:17` | **M07 survives** (F14-2): removing the ignore line passes every test, and nothing checks `git ls-files` |
| The E2eRunner `ElementBundle` rule and kind | `rg "ImpactKind.ElementBundle\|GeneratedAt only"` returns 0 | **M29 survives** (F14-2): re-adding the enum member and its rule line passes E2eRunner.Tests 157/157 |

The AC's own deletion checks are grep checks, and all of them hold. The three survivors (M06, M07 and M29) are missing ratchets, so they are filed.

## Player-visible regression check

- The diff touches no runtime source: nothing under `engine/`, `ui/`, `scenes/`, `parts/catalog`, `parts/scenes`, `CuriousContraptions.Simulation` or any served JS. The game consumes only the Abstractions' event-ring types (`ActivationNetwork.cs`), and those are unchanged.
- The one build-path change is that publish now builds and runs the generator. My merge-run publish succeeded (exit 0, 101.8 s), and the implementer's did too (116.0 s).
- The smoke pair passed on the merged bundle.
- The full run covered all 41 `tools/e2e/*.test.ts` files: the same 41 distinct suites as 9A.1's full run, with 43 records counting repeats.
- **No regression found.**

## Runtime evidence

- **Implementer full run, reused after identity checks** (copied to `implementer-full-run-summary.txt`):
  - run `20261020T205802Z-79199`, verb Full;
  - `sourceHead` `c948fec6…`, and `sourceStatusDigest` `e3b0c442…` (an empty status);
  - `bundleIdentity` `c194034b…`, the same at start and end;
  - 43/43 files, 223/223 tests, 0 failures;
  - verdict Incomplete(Contended), only because both clock-master records were Contended. Both passed.
- **The clock-master rerun, identity-checked** (`evidence-9a-4/clock-master-rerun/summary.txt`):
  - run `20261020T214832Z-67332`, verdict Pass;
  - `sourceHead` `c948fec6…` and an empty `sourceStatusDigest` (`e3b0c442…`);
  - clock-master 9/9 SerialAlone `observation=Clean`, plus the smoke pair; 15/15 in all.

  Together the two runs give all 41 suites passing, with clock-master Clean.
  
  The run is reusable for this candidate: `c948fec6` and `b8910e92` differ outside `_bmad-output/` only by the test comment, and `NoPrimarySuite` blocked the `run --diff` selection, so the Full verb was correct (Abstractions, BundleElements and E2eRunner each force Full).
- **Merge-diff selection** (`select --diff b8910e92` on `ec66da08`, `select-merge-diff.txt`):
  - Mode Selected; Tier 3 not required;
  - only main's test files, docs and one `tools/*.mjs` harness (0 importing suites) changed;
  - selection: the smoke pair.
- **Merge-diff run** (`run --diff b8910e92 --out …`, `merge-run/summary.txt`):
  - run `20261020T233758Z-91155`, verdict **Pass**;
  - `sourceHead` `ec66da08`, empty status, `bundleIdentity` `ad43af6f…` at start and end;
  - engine-core-2a1 5/5 and engine-core-2b1 1/1, both Clean.
- **Tier 1 on the merge `ec66da08`** (`tier1/`):
  - `dotnet build CuriousContraptions.slnx`: 0 warnings, 0 errors, and the bundle was generated (13 elements). The Release build is also 0/0.
  - `dotnet test CuriousContraptions.slnx`: WorkerAotGuard 17/17, Coverage 78/78, BundleElements.Tests 34/34, E2eRunner 157/157, CuriousContraptions.tests 1894/1894.
  - `node --experimental-vm-modules --test tools/*.test.mjs`: 88/88.
  - LifecycleContract: 510 checks, 0 failures. WireContract: 108 oracles, 0 failures.
  - Ownership `--oracles .`: exit 0, Positive true, 0 attacks accepted. This needed the Release build and a web restore first.
  - `anvil check` on the 44 changed code and doc files: 0 warnings. `docs/engine-contracts.md`, checked alone, reports the pre-existing too-long line (already F13-5).

## Mutants

Every mutant ran in a scratch worktree at `ec66da08` (`.claude/worktrees/murdoch17-9a4-mut`). Each was applied with `perl`, checked, and reverted, and the tree ends clean. Logs are in `mutants/M01.log` to `mutants/M24.log`; a summary is in `mutants/results.txt`. Baseline: BundleElements.Tests 34/34 and parity 5/5.

| Id | Mutation | Result |
| --- | --- | --- |
| M01 | drop `elements/Wall` from the slnx | killed: SolutionMembership |
| M02 | re-add a hand `ProjectReference` to Ball | killed: NoHandList |
| M03 | drop `_WasmGenerateAppBundle` from `BeforeTargets` | killed: WebHost target |
| M04 | re-add `GeneratedAt = DateTime.UtcNow` to the document | killed: determinism |
| M05 | re-add `Version = "1.0"` to the document | killed: determinism (root keys) |
| M06 | re-add an (empty) `IReadOnlyDictionary<string, object>? Bag` to `ElementMetadata` | **survives**: F14-2 |
| M07 | remove `/parts/elements.bundle.json` from `.gitignore` | **survives**: F14-2 |
| M08 | write 0 for a vector's Z | killed: round trip |
| M09 | duplicate check needs more than 2 | killed: BUNDLE001 |
| M10 | no finite check on `HalfExtents` | killed: BUNDLE002 |
| M11 | no `Category` enum check | killed: BUNDLE003 |
| M12 | no body-collider check | killed: BUNDLE004 |
| M13 | admit 2 declarations per package | killed: BUNDLE005 |
| M14 | NaN in the real Ball package | killed: the build fails `BUNDLE002 Ball.Colliders[0].HalfExtents.x` |
| M15 | Ball category Goals | killed: parity category |
| M16 | Ball mass 2 kg | **survives**: F14-3 |
| M17 | clock default 2 s | killed: parity parameters |
| M18 | battery port y 0.63 | killed: parity ports |
| M19 | domino footprint has 0 moving bodies | killed: parity footprint |
| M20 | enum integers allowed | killed: malformed-input test |
| M21 | Wall category Motion | killed: parity category |
| M22 | gate housing half-extent changed | killed: parity colliders |
| M23 | Ball inertia changed | **survives**: F14-3 |
| M24 | Ball scene path changed | killed: parity scene |
| M25 | the SignalLamp's `Colliders` emptied (a non-mechanical package with footprint `Bodies` 1) | **survives**: `dotnet build BundleElements.csproj` succeeds (B-1) |
| M26 | `ElementMetadata.Category` retyped to `string` | killed: CS1503 in every package |
| M27 | `HalfExtents` renamed back to `Dimensions` | killed: CS1739 in the packages |
| M28 | a third `--out` to `CuriousContraptions.web/AppBundle/` in the generator Exec | killed: `CSharpStaticGateTests` MSBuild pin |
| M29 | `ImpactKind.ElementBundle` and its rule re-added | **survives**: F14-2 |

M25 to M29 ran in the review merge worktree `murdoch17-9a4`; it ends clean. Their summary is in `mutants/results-round-b.txt`.

## Probes

Probe source: [ProbeTests.cs.txt](evidence-9a-4-review/ProbeTests.cs.txt). It was compiled temporarily into BundleElements.Tests as `Murdoch17Probes.cs`, then removed. Output: `probes.log`.

| Probe | Expected | Actual |
| --- | --- | --- |
| P1. `"Category": "Motion, Goals"` | rejected | **read as `Power`** (1\|2 = 3) |
| P2. `"Category": "structure"` | rejected | **read as `Structure`** |
| P3. A non-mechanical declaration with footprint `Bodies` 0 and no colliders (`SecondFixture`). This only shows that BUNDLE004 is never raised for a non-mechanical declaration. | BUNDLE004 | no error |
| P3b. A non-mechanical (Signal) declaration with `Footprint(1, 0, 0)` and no colliders | BUNDLE004 (AC4) | **no error: admitted (B-1)** |
| P4. `Id = ""` | refused | no error |

## Findings filed to Story F.14 "Bundle Gate Follow-ups" (none blocking)

- **F14-1. The bundle reader accepts names that are not exact enum names.**
  - Problem: `BundleSchema.Read` uses `JsonStringEnumConverter`. That converter accepts comma-joined flag syntax and case-folded names for non-flags enums (P1, P2). This contradicts the spec's "JSON enum names are parsed with unknown names rejected" and `BundleSchema`'s own summary.
  - Why it doesn't block now: generation and the round trip are unaffected, and there is no runtime reader until 9A.14. The E2eRunner parser, by contrast, is exact (`Named<TEnum>`).
  - Fix before 9A.14 reads the bundle: use an exact-name converter, and test P1 and P2.
- **F14-2. Three deletions have no ratchet.**
  - Problem: each of these passes every test:
    - a re-added (empty) property bag on `ElementMetadata` (M06);
    - a re-tracked or un-ignored `parts/elements.bundle.json` (M07);
    - a re-added `ImpactKind.ElementBundle` and its rule (M29).
    
    The AC's grep checks hold today.
  - Fix:
    - a field-list pin on `ElementMetadata` and the bundle records, like `WorkshopFormatTests.TheVersionlessRecordsDeclareExactlyTheirFields`;
    - a test that the bundle is git-ignored and untracked;
    - an `ImpactKind` member pin in E2eRunner.Tests.
- **F14-3. Parity does not compare the mechanical payload.**
  - Problem: mass, inertia, linear drag and gravity are never compared with the runtime (M16 and M23 survive).
  - Current values: the Ball's mass (1 kg = `MassBits` 15360) and drag (≈0.04 = `DragBits` 10527) agree with `ball.tres` by inspection. The inertia (0.04624, a solid sphere) is unchecked.
  - Fix: compare the payload with the compiled body's rows before 9A.14 cuts over.
- **F14-4. The validator admits an empty element id (P4).** The body-without-collider gap is B-1, not part of this item.
- **F14-5. Runner selection (the implementer's "F.13", renumbered).**
  - `select` and `run --diff` let a `NoPrimarySuite` refusal pre-empt the Full triggers in the same diff (`evidence-9a-4/runner-select-diff-844981c9.txt`).
  - `ball` has no Primary suite, or that absence should be a declared and tested decision. Every C-Balls story changes `elements/Ball`.
- **F14-6. The web publish builds the generator in Debug.** `GenerateElementBundle` hard-codes `Configuration=Debug`, so a Release publish also restores and builds the Debug graph of the generator and all 13 packages. The output is identical; the cost is build time only.
- **F14-7. Scene versus canonical colliders.** The detector's scene-extracted ring, which `CaptureInstance` prefers, differs from the canonical table the package matches. The implementer filed this to Story 9A.16 (CL4) along with the gate's blade body. I confirm both are tracked there: they are the two new "**And**" rows in Story 9A.16's ACs in the candidate's `epics.md`, at lines 2799 and 2800 of the merged tree, and they survive my merge. Main gains them when the story merges. They need no F.14 work beyond this cross-reference.

## Criterion and impact scope

- **Consumers:**
  - `tools/E2eRunner`, the one bundle reader: its parser is moved to the new schema and the `ElementBundle` rule is deleted, and E2eRunner.Tests passes 157/157;
  - `CuriousContraptions.tests`, which now references the generator: 1894/1894.
- **Build and tools:**
  - the slnx Debug and Release builds;
  - the web publish, twice (the implementer's run and mine);
  - `CSharpStaticGateTests`, which pins both new targets and passed.
- **Shared state:** the generator writes `parts/elements.bundle.json` in the worktree on every build, which `.gitignore:17` ignores. Concurrent worktrees each write their own root.
- **Runtime, lifecycle and workers:** none changed. The smoke pair passed on the merge.
- **Out of scope:** detailed performance and global qualification stay at their named gates.

## Round 2 (final): terminal verdict Pass

- **Candidate:**
  - `1ba4eb95`; code fix `a8c0cf47`; merge with main `11f815f7`.
  - That merge's code is identical to my round-1 merge `ec66da08` (an empty diff outside `_bmad-output/`).
  - Reviewed on a clean worktree, `.claude/worktrees/murdoch17-9a4-r2`, which ends clean.
  - Evidence: [evidence-9a-4-review/round2/](evidence-9a-4-review/round2/).
- **B-1 is resolved.**
  - BUNDLE004 now fires for `Footprint.Bodies > 0` or any `IMechanicalElement` declaration with no colliders.
  - M25 (the lamp's colliders emptied) is now **killed**: the real build fails with `error BUNDLE004: SignalLamp.Colliders: a body declares no collider`.
  - Probe P3b passes, and the new non-mechanical fixture tests pass.
  - M30, which drops the footprint condition, is killed by `ANonMechanicalBodyWithoutAColliderIsRefused`.
- **F14-1 is resolved, so it is removed from F.14.**
  - An exact-name enum converter is in place. Probes P1 (`"Motion, Goals"`) and P2 (`"structure"`) are now rejected.
  - M31, which makes the converter case-insensitive, is killed by the new `MalformedInputIsRejected` rows.
- **Flakiness attack: the cause is reproduced, and the fix is proven.**
  - **Five runs of the solution tests** (`dotnet test CuriousContraptions.slnx`, the default order) all passed: WorkerAotGuard 17, Coverage 78, BundleElements.Tests 40, E2eRunner 157 and CuriousContraptions.tests 1894, with 0 failures (`test-run-1..5.log`). The build has 0 warnings.
  - **The default order does not exercise the hazard.** With round 1's `using var definition` restored, three default-order runs still passed (`dispose-restored-1..3.log`).
  - **Randomised xUnit seeds do.** With the dispose restored, seed `:3` fails 50 tests: TubeMouthSnap 25, WorkshopPipeLength 11, BasketballResource 6, Springboard 3, WorkshopWallResource 2, and Counter, PressurePlate and StraightPipe 1 each. The cause is `mono_object_disposed_baseref` on the cached `PartDefinition`. Seeds 1, 2, 4, 5 and 6 pass. This confirms the implementer's attribution (`dispose-restored-seed*.log`).
  - **On the fixed candidate, 12 seeds pass**: `:1` to `:12`, including `:3`, each 1918/1918 with 0 failures (`fixed-seed*.log`).
  - The parity test has no other `using`, `Dispose` or `Free` call.
- **Chrome:** none needed. The fix changes only `tools/BundleElements`, its tests and a test comment; no served file changes. The implementer's round-2 smoke pair passed 6/6.
- **Still filed to F.14:**
  - F14-2 (ratchets for M06, M07 and M29);
  - F14-3 (mechanical payload parity);
  - F14-4 (empty id: probe P4 is still admitted);
  - F14-5 (runner `NoPrimarySuite` precedence and the ball's Primary suite);
  - F14-6 (the Debug generator build during publish);
  - F14-7 is tracked in 9A.16.
- **New, filed to F.14 as F14-8:** the parity guard's fixture-sharing hazard is invisible in the default xUnit order. Add a seeded-order run, for example seed `:3`, to Tier 1 or to the parity test's own guidance, so a reintroduced dispose is caught.
