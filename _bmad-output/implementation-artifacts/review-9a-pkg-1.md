# Review: Story PKG-1, Receiver, Bowling Ball and Springboard Element Packages (9A.5 + 9A.6 + 9A.12)

Reviewer: Murdoch-18 (independent; no deliverable edited). Round 1 of at most 2. Date: 21 Oct 2026.

## Verdict

**Pass (round 1). No blocking finding.**
- Every AC of 9A.5, 9A.6 and 9A.12 holds, as PKG-1 scopes them.
  - The one exception is the stories' "every e2e suite in one serial run" clause. It is superseded and was not run (owner tiers, this brief and PKG-1 AC3).
- Every deletion is made.
- No player-visible regression was found.

Five findings are filed to **Story F.15, "Package Follow-ups"** (below). None breaches an AC or changes what the player sees.

## Candidate and identities

| Item | Identity |
| --- | --- |
| Candidate worktree | `.claude/worktrees/agent-a6028085bf7300f38` |
| Candidate HEAD | `8333682f` (docs only over the merge `9cca93c8`) |
| Code commits | `ce460ddc` (packages), `ba4678d9` (pkg-1 suite), `9cca93c8` (merge of main `12658442`) |
| Main | `12658442`, unchanged since the merge (`git rev-parse main`), so no further merge is needed |
| Clean review copy | `.claude/worktrees/murdoch18-pkg1`, detached at `8333682f`, fresh (no bin/obj) |
| Hook replay copy | `.claude/worktrees/murdoch18-hooks` |
| Evidence | [evidence-9a-pkg-1-review/](evidence-9a-pkg-1-review/) |

## Hooks: no commit on the branch skipped them

- `git reflog` shows a single `wip` commit, `b930f94f` (HEAD@{7}), that was reset away with `reset: moving to HEAD~1`.
  - `git merge-base --is-ancestor b930f94f HEAD` exits 1, so it is not on the branch.
  - Its code is identical to `ce460ddc`: `git diff --stat b930f94f ce460ddc` touches only `_bmad-output` evidence files and one line of `docs/architecture/element-package-isolation.md`.
- Git keeps no record of whether a hook ran, so I replayed the hook on all seven branch commits (`git log 12658442..8333682f`).
  - Method: `git checkout --detach <parent>`, then `git read-tree <commit>` so the index holds exactly that commit, then `sh .githooks/pre-commit`. This is the hook's own gate (index snapshot, from-scratch builds, `CheckStagedTests` on the staged diff, the static-gate tests and `anvil hook pre-commit`).
  - `193cdf37` (her 9A.4 merge, parent `980e222d`, 71 staged): exit 0.
  - `ce460ddc` (parent `193cdf37`, 44 staged): exit 0.
  - `ba4678d9` (parent `ce460ddc`, 101 staged): exit 0.
  - `2f8b0654` (parent `ba4678d9`, 16 staged): exit 0.
  - `18429f75` (parent `2f8b0654`, 38 staged): exit 0.
  - `9cca93c8` (parent `18429f75`, 112 staged): exit 0.
  - `8333682f` (parent `9cca93c8`, 15 staged): exit 0.
  - Logs: `hooks/hook-<commit>.log`.
- The hook on the clean HEAD tree also passes (`tier1/pre-commit-hook-8333682f.log`; 123/123 gate tests).
- **Result:** every commit on the branch passes its hook as replayed. The `--no-verify` commit is off the branch, and its content was recommitted with hooks.

## Scope

`git diff 12658442 8333682f`, outside `_bmad-output`, is exactly the PKG-1 file set:
- the three packages, the moved scenes and the three `.tres` pointers;
- the two Abstractions files and the slnx;
- the parity, static-gate, BundleElements and E2eRunner tests;
- `tools/BundleElements`, `tools/E2eRunner`, the pkg-1 suite and its README row;
- the isolation doc's A15 row.

Her `193cdf37` resolution left no stray file.

## Merge check

- The merge delta `18429f75..9cca93c8`, outside `_bmad-output`, touches six files:
  - `tools/BundleElements/BundleSchema.cs` and `BundleValidator.cs`;
  - `tools/BundleElements.Tests/` (three files);
  - `CuriousContraptions.tests/BundleCatalogueParityTests.cs`.
- In `BundleValidator`, main's any-body collider rule sits beside the slider checks. A moving body without a collider raises BUNDLE004 with main's wording, "a body declares no collider".
- No runtime project reads the element bundle: `rg 'elements\.bundle'` over `engine`, `ui`, `scenes`, the web host, Simulation and Animation returns 0 hits. So the generator change cannot alter what the player sees.

## Acceptance criteria

| AC | Evidence | Result |
| --- | --- | --- |
| PKG-1 AC1 / 9A.5. `elements/Receiver/Receiver.csproj` references only `.Abstractions` and `.Abstractions.Mechanical`; the bundle contains `basket`; parity is bit for bit | The csproj has exactly those two references. Parity covers the `.tres` metadata and category, the compiled footprint, the five wall colliders and their material (binary16 lane, CL6), and the empty ports. Mutants: her dwell default and front-lip mutants are killed. | Pass |
| 9A.5. Capture settings and force region are declared parameters with the current defaults and ranges | Six parameters. Their defaults equal `ReceiverCaptureSettings.Free` (0.02 m, 1.5 m/s, 0.35 s, `Enabled`) and `ReceiverForceRegion.Free` (0, 0). Their ranges equal `Validate`. My mutants: **R1** `force_maximum_acceleration` max 12→11 killed; **R2** 12→13 killed; **R3** Choice options reordered killed (all by `…ParameterDefaultsAndBounds`). Her dwell mutant is killed. | Pass |
| 9A.5. A project-reference test proves the package references neither the engine nor a sibling | `PackageReferenceTests` requires the exact Abstractions set and no NuGet package. Her sibling mutant (`elements/Ball`) is killed. **P1:** my engine mutant (`../../CuriousContraptions.csproj` added to `Springboard.csproj`) is killed. | Pass |
| 9A.5. `cat-062b` and every suite that places a Receiver pass | I derived the set myself (see "Receiver suite derivation"): 17 suites, all in her three identity-checked runs. On `ce460ddc`: cat-062b 7/7, including "Ready Receiver deletion and replacement rebind the saved capture goal". On `2f8b0654`: the seven inline-level Receiver suites, 60/60. On `9cca93c8`: pkg-1. | Pass |
| 9A.6. The bundle contains `bowling` with its ball material as declared data; parity is bit for bit | Radius 0.28 m and mass 4 kg (owner, 9 Oct). `EveryBallPackageDeclaresItsCatalogueBallMaterial` compares against both `BallMaterial.For` and the `.tres` resource. Body motion, mass, drag and gravity are compared too. Her rolling, drag and scene-path mutants are killed. My **B2** (radius 0.28→0.27) is killed by two tests. My **B1** (inertia) survives, which is the known F14-3; inertia is not part of the material the AC names. | Pass |
| 9A.6. The package references only the Abstractions it needs | Same test as above. | Pass |
| 9A.6. `cat-014`, `cat-015a`, `cat-052` and `cat-005` pass | On `ce460ddc`: cat-014 4/4, cat-015a, cat-052 (and -lamp, -two-plates) and cat-005 7/7. `domino_effect` (clock-master, cat-023b) also passes. | Pass |
| 9A.12. The bundle contains `spring` with typed parameters and its slider declaration; parity is proven | `ISliderElement` / `PrismaticSliderDescriptor` are carried as `MechanicalPayload.Sliders`. `EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody` compiles the part at every parameter maximum, so each key binds its own value. My mutants: **S1** stiffness min 120→100 killed; **S2** `BodiesCollide` true killed; **S3** compression key renamed to `damping` (a valid Real key) killed by the start-position check. Her key-swap, limit and round-trip mutants are killed. | Pass |
| 9A.12. `cat-062`, `cat-062b` and `cat-051` pass | On `ce460ddc`: cat-062 7/7 (drop Centred/Miss, Step compresses plate and coil, SoftUndamped, StiffDamped, Tilted, delete/recreate retires the plate), cat-062b 7/7, cat-051. | Pass |
| PKG-1 AC2. `git ls-files` of the three old scenes returns nothing; the unpackaged set loses the three ids | `git ls-files parts/scenes/{basket,bowling,springboard}.tscn` is empty. All three are 100% renames. The set is the eight ids listed. **D2** (put `basket` back in the list) is killed. **D3** (point `springboard.tres` back at a re-added `parts/scenes/springboard.tscn`) is killed by `…CatalogueMetadataAndCategory`. **D1** (re-add a stray `parts/scenes/basket.tscn` copy) survives the whole `dotnet test`, see F15-3. | Pass |
| PKG-1 AC3. The union of the named suites passes once on the merged candidate | Her 20-suite run (129/129) and the seven Receiver-level suites (60/60), reused after the identity checks below. Then pkg-1 with the smoke pair, 9/9, on the merged code `9cca93c8`. | Pass |
| 9A.5/6/12 build and test. 0 warnings and `dotnet test` 100% | Tier 1 on the clean copy is below. | Pass |
| 9A.5/6/12. Every `tools/e2e/*.test.ts` suite in one serial run | **Superseded, not run.** The owner's tiers and this brief supersede it, and PKG-1 AC3 replaces it (ruling under CL8). No full run exists for this candidate. | Superseded (not run) |

## Deletions

- **Scenes:**
  - `git ls-files` is empty for the three old paths.
  - Under the implementer's exclusions, `rg 'parts/scenes/(basket|bowling|springboard)\.tscn'` returns only her two historical hits: the frozen `docs/coverage/engine/catalogue-01.json` and the pinned `CAT-004-basket.md` row.
  - The excluded hashed `docs/*-sources.json` manifests are historical evidence (AGENTS.md). Tier 1 still passes, so no current tool reads them.
  - `rg parts/scenes` over the runtime sources returns 0 hits, so no scene path is composed from the id.
- **Moved scenes:** each scene's only `ext_resource` is `res://engine/MachinePart.cs`, which exists. Her Chrome tap and log files contain no `Failed loading`, `Cannot open` or `not found`. Palette-placement placed all three parts from the moved scenes, 25/25.
- **Unpackaged set:** 11 → 8, with D2 killed.
- **No format versions and no fixed capacities:** the added code has no `Version` field, fixed-size array or capacity constant.

## Behavioural parity and player-visible regressions

- **Runtime inputs are unchanged after the 20-suite run.**
  - `git diff ce460ddc 8333682f`, outside `_bmad-output`, touches only `tools/BundleElements*`, the parity test, `tools/E2eRunner*`, `tools/e2e/README.md` and `tools/e2e/pkg-1.test.ts`.
  - No engine, ui, scene, catalogue, element, worker or web-host file changes.
- **Run identities, reproduced independently from each run's `sourceStatusDigest`** (SHA-256 of `git status --porcelain=v1 -z --untracked-files=all`, `tools/E2eRunner/SourceIdentity.cs`):
  - **20-suite run** (`e2e-tier2-ce460ddc`). It was `ce460ddc` plus three untracked records: `deletion-rg-proof.txt`, the run's own `.log` and `publish.log`. The digest `b65c4e56…` reproduces exactly. The bundle identity is equal at start and end (`ceb64963…`), and the served check passes for 285 files.
  - **Receiver-level run** (`e2e-receiver-levels-2f8b0654`). It was `2f8b0654` (code equal to `ba4678d9`) plus three modified records (spec, epics, sprint-status) and five untracked evidence files. The digest `c28eab08…` reproduces. Start and end identities are equal.
  - **pkg-1 plus smoke run** (`e2e-pkg-1-9cca93c8`). It was `9cca93c8` plus only its own `.log` and `publish.log`. The digest `9ba24b41…` reproduces. Start and end identities are equal (`4b8e8d05…`).
  - **So every reused run executed exactly the committed code.** The bundle identity differs across publishes of the same source (her F.15 item), so these source identities are the binding.
- **Behaviour in Chrome:**
  - Receiver capture: cat-062b's capture-goal routes and the level suites (`bumper_*`, `clear_pipe`, `gentle_bend`, `quarter_bend`, `joined_pipe`).
  - Bowling physics: cat-014, cat-023b and clock-master on `domino_effect`.
  - Springboard plate and prismatic: cat-062's compression, the damped and undamped configurations and the tilted case, plus cat-062b's charged and passive routes.
  - All pass on the identity-checked runs.
- **clock-master** shows `unresponsive=1 debtFaults=1 closeFindings=1` in both attempts. These counters are identical in the 9A.1, 9R.4 and 9R.5 full runs, so they are the suite's injected stall and not a regression.
- **The merge-diff selection.**
  - `select --diff 18429f75` returns `Mode: Full` (`select-merge-diff-18429f75.txt`), because `tools/BundleElements` is SharedDriverInfrastructure and its tests are unmatched paths.
  - Per the owner's brief I did not run a full run.
  - The delta cannot reach the player: no runtime reader of the bundle exists. Her pkg-1 and smoke run on `9cca93c8` covers the merged code.

## Tier 1, on the clean copy at `8333682f`

| Check | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx` | 0 warnings, 0 errors | `tier1/build.log` |
| `dotnet test CuriousContraptions.slnx` | 2216/2216 (1897 + 158 + 78 + 66 + 17), equal to her post-merge count | `tier1/test.log` |
| C# static gate (the pre-commit hook on the tree) | exit 0, 123/123 | `tier1/pre-commit-hook-8333682f.log` |
| Node harnesses `tools/*.test.mjs` | 88/88 | `tier1/node.log` |
| LifecycleContract (Release) | 510, 0 failures | `tier1/lifecycle.log` |
| WireContract (Release) | 108, 0 failures | `tier1/wire.log` |
| Ownership `--oracles .` | exit 0, Positive true, 22/22 attacks rejected; byte-identical to her log | `tier1/ownership-oracles.log` |
| `anvil check --since 12658442` | no warnings (anvil scans the one `.ts` file; C# is the static gate's) | `tier1/anvil-check.log` |

## Mutants (reviewer's own; raw output in `mutants/<name>.txt`)

| Mutant | Target | Result |
| --- | --- | --- |
| R1 | Receiver `force_maximum_acceleration` Maximum 12→11 | Killed (`…ParameterDefaultsAndBounds`) |
| R2 | Same, 12→13 | Killed (same) |
| R3 | Receiver `capture_participation` options reordered | Killed (same) |
| S1 | Springboard `stiffness` Minimum 120→100 | Killed (same) |
| S2 | Springboard compression key → `damping` and `BodiesCollide` true | Killed (`…PrismaticJointAndMovingBody`, collision assert) |
| S3 | Springboard compression key → `damping` only | Killed by the start-position assert. The plate should start at the base frame minus the compression maximum (0.2 m), but the mutant bound the damping maximum (8) instead. |
| B1 | Bowling `InertiaTensor` 0.12544→0.5 | **Survives**: inertia is not compared (F14-3, already filed) |
| B2 | Bowling radius 0.28→0.27 | Killed (`…CollidersAndMaterial`, `…CatalogueBallMaterial`) |
| P1 | `Springboard.csproj` references the engine project | Killed (`PackageReferenceTests(Springboard)`) |
| V1 | BUNDLE007 drops its `Kind == Real` clause | **Survives** (F15-1) |
| V2 | The slider `UpperLimit` branch never fires | **Survives** (F15-2) |
| D1 | A stray copy of the old `parts/scenes/basket.tscn` is re-added | **Survives** the whole `dotnet test` (`D1-full-test.txt`; F15-3) |
| D2 | `basket` is put back in the unpackaged list | Killed (`TheCatalogueIdsWithoutAPackageAreExactlyTheEightListed`) |
| D3 | `springboard.tres` points back at a re-added `parts/scenes/springboard.tscn` | Killed (`…CatalogueMetadataAndCategory`) |

**Her red-first claim is only partly evidenced.**
- `red-bundle-tests.txt` shows 5 failing: the four slider-validator tests and "packages exist".
- `red-parity-tests.txt` shows 3 failing (the unpackaged set, the ball material and the slider parity), and ends with `193cdf37`.
- The parameter defaults and bounds, and the per-package references, have no red record. They rest on mutant kills only: hers, and my R1–R3, S1 and P1.
- Each AC is therefore proven by a killed mutant, if not by a recorded red. This is a process note and does not block.

Her nine mutants were also checked (`mutants-parity.txt`, `mutants-bundle.txt`).

## Receiver suite derivation

I derived the set independently of her enumeration, from two sources.
- **Suites that place or name a Receiver**, by grepping `tools/e2e/*.test.ts` for `receiver`/`basket` and `SuiteCatalogue` for `PartId.Receiver`: anim-1a, anim-1c, cat-014, cat-015b, cat-030, cat-050-joined, cat-062b, engine-core-2a3, engine-core-2b4, engine-core-2c, palette-placement and pkg-1.
- **Suites whose levels build a Receiver.** The builders that construct one are:
  - `WorkshopPuzzle` (first_principles);
  - `BumperSidekick`, `BumperAdvanced` (bumper_depth, wall_and_bumper);
  - `ClearPipe`, `GentleBend`, `QuarterBend`, `JoinedPipe`;
  - `SpringboardLessons`.

  `DelayedSignal` and `DominoEffect` build none. The suites that select those levels add cat-015a-sidekick, cat-048b, cat-049, cat-050 and tube-snapping.
- **Union:** 17 suites. Every one is in her runs on `ce460ddc`, `2f8b0654` or `9cca93c8` (28 distinct suites), so none is missing and no extra run was needed.

## Rulings on her Spec Change Log

- **CL1 (the opt-in `ISliderElement`).** Accepted. It is additive, consumed by no runtime code (`worker-reach-rg.txt`), and the gate stays with 9A.16.
- **CL2 (`CosmeticCurves.Receiver` not declared).** Accepted.
  - The isolation authority (row A4/A10) assigns every `CosmeticCurves.*` arm to 9A.25a.
  - 9A.25a's Deletes list names `CosmeticCurves.Receiver`, and its AC moves every binding into its package.
  - Inventing a one-off binding contract here would pre-empt that design.
- **CL3 (stale comparators).** Accepted, as in the 9A.4 review.
- **CL4 (the force region's box and support height stay in engine code).** Accepted. AC2 asks for the authored values, and both arguments of `ReceiverForceRegion.Create` are declared. `ReceiverForceRegion` has an owner that deletes it (the Ramp/Receiver story at `epics.md` line 3130), so this is not filed.
- **CL5–CL7.** Accepted.
- **CL8 (no full Chrome run).** Accepted under the owner's verification tiers (19 Oct) and this review's brief ("never repeat a full run"), which supersede the stories' "every suite" clause.
  - Note for the coordinator: the whole PKG-1 diff touches the Abstractions and `tools/E2eRunner`, which the tiers class as shared driver/test infrastructure.
  - The runner change is additive (one new suite), and E2eRunner.Tests passes 158/158.
  - The next five-element Tier 3 checkpoint should count PKG-1's three elements.

## Filed to Story F.15, "Package Follow-ups" (non-blocking)

- **F15-1. BUNDLE007's Real-kind clause is untested.** Mutant V1 survives: a slider key that names a Choice parameter would be admitted, and no test fails. Add a fixture whose stiffness key names a Choice parameter.
- **F15-2. The slider upper-limit check is untested.** Mutant V2 survives. Add fixtures for `UpperLimit < 0` and for `UpperLimit <= LowerLimit`.
- **F15-3. Nothing stops a moved scene coming back.** Mutant D1 survives: a stray `parts/scenes/basket.tscn` passes every test. AC2 is a one-time `git ls-files` check, so it holds. Add a ratchet that each packaged id's scene exists only under its package (this may fold into F14-2's deletion ratchets).
- **F15-4. `PackageReferenceTests` reads only `ProjectReference` and `PackageReference`.** A package could still reach the engine through `<Reference HintPath>` or `<Compile Include="../../engine/…">`. Assert that a package has no other reference or compile items outside its own directory.
- **F15-5. The slider names its parameters by raw string key.** This matches `ParameterDescriptor.Key`, and BUNDLE007 validates it at the generation boundary. A typed parameter-key ID across the Abstractions would bring it in line with AGENTS.md's typed-identity rule.
- Already filed elsewhere and not re-filed: bowling inertia (F14-3); the bundle identity reproducibility (her F.15 item).
- **Planning note for the coordinator.** Her `epics.md` titles F.15 "Reproducible Publish Bundle Identity", but the brief names it "Package Follow-ups". Rename the heading, and keep her bundle-identity AC as one entry beside F15-1 to F15-5.

Also noted, not filed: `pkg-1.test.ts` test 2 is named "Run moves the bowling ball onto the springboard", but it asserts only that the committed state advanced and that Reset is exact. cat-014, cat-062 and cat-062b carry the behavioural assertions.
