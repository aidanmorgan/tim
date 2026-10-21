# Review: Story PKG-2, Funnel, Clear Pipe and Pipe Bend Element Packages (9A.8 + 9A.9 + 9A.10 + 9A.11)

Reviewer: Murdoch-19 (independent; no deliverable edited). Round 1 of at most 2. Date: 21 Oct 2026.

## Verdict

**Pass (round 1). No blocking finding.**
- Every AC of 9A.8–9A.11 holds as PKG-2 scopes them, including bit-for-bit collider parity at the runtime's own lanes.
  - The stories' "every e2e suite in one serial run" clause is superseded and was not run (owner tiers, this brief, PKG-2 AC3; as PKG-1 ruled under its CL8).
- Every deletion is made, and each has a re-add mutant (eight killed; the stray-copy mutant survives as the already-filed F15-3).
- No player-visible regression was found. The scene-versus-canonical collider mismatch (her CL1, owned by 9A.16) does not reach play today (section "The 9A.16 collider mismatch").
- The dated addenda for Stories 6.6, 6.7, 6.9, 6.10 and 6.11 are appended (PKG-2 AC4). All five fresh verdicts are **Incomplete**: each original story still has clauses that no repair story touched and no test proves. Those are 6.x proof gaps, not PKG-2 defects, and they are filed (F15-8).

Findings: none blocking. Four are filed to **Story F.15, "Package Follow-ups"** (below).

## Candidate and identities

| Item | Identity |
| --- | --- |
| Candidate worktree | `.claude/worktrees/agent-afac8f6ac2e30208b` |
| Review head | `d4195fb0` (docs only over the merge) |
| Code candidate | `66597b36` (packages, scene moves, tests, pinned lists) |
| Merge | `08e72760` = `66597b36` + main `dc1f59a1`. `git diff --stat 66597b36 08e72760 -- . ':(exclude)_bmad-output'` is empty |
| Main | `dc1f59a1`, unchanged (`git rev-parse main`), so no further merge is needed |
| Clean review copy | `.claude/worktrees/murdoch19-pkg2`, detached at `d4195fb0`, fresh (no bin/obj), `git status` empty before every check |
| Mutant copy | `.claude/worktrees/murdoch19-mut`, detached at `d4195fb0` |
| Identity copy | `.claude/worktrees/murdoch19-id`, detached at `66597b36` |
| Evidence | [evidence-9a-pkg-2-review/](evidence-9a-pkg-2-review/) |

## Scope

`git diff -M 8333682f 66597b36`, outside `_bmad-output`, is 31 files and exactly the PKG-2 set:
- four new packages (`elements/Funnel`, `Pipe`, `PipeBend45`, `PipeBend90`: csproj, declaration, `.cs.uid`) and the four scenes as 100% renames;
- the four `.tres` scene pointers;
- the slnx, `CSharpStaticGateTests.TrimAnalyzedProjects`, `ImpactRules.ElementPackages`;
- `BundleCatalogueParityTests` (unpackaged list, `Canonical`, `Placed`, the pipe `Parameters` arm), the four scene tests' load paths, `PackageReferenceTests`, four `SelectionTests` rows;
- the isolation doc's A15 row.

No engine, ui, worker, wire, solver or web-host file changes. No runtime project reads the element bundle: `rg 'elements\.bundle'` over engine, ui, scenes, the web host, Simulation and Animation returns nothing (`identity/runtime-reach.txt`). So the scene moves, through the `.tres` paths, are the only runtime-visible change.

## Acceptance criteria

| AC | Evidence | Result |
| --- | --- | --- |
| PKG-2 AC1 / 9A.8. `Funnel.csproj` added; the bundle contains `funnel`; its colliders equal the current ones bit for bit | References exactly `.Abstractions` and `.Abstractions.Mechanical`. `…FootprintCollidersAndMaterial` compares the 72 declared boxes with the compiled scene and with `WorkshopCanonicalColliders.For("funnel")`: rotations at f32 bits, offsets and extents at the runtime's binary16 lane (`Same`). Mutants: her M1 killed; my **A3** (rotation sign flipped to the scene convention) and **A8** (collar offset 1.45→1.46) killed. The AC's Given names "the registry or `WorkshopCanonicalColliders.For`"; she chose the canonical set, which is what the worker simulates (ruling on CL1 below). | Pass |
| 9A.8. `cat-030` passes | My run: cat-030 7/7, Clean (`e2e-tier2-d4195fb0/cat-030.tap`) | Pass |
| 9A.9. `Pipe.csproj` added; `pipe` in the bundle; parity bit for bit | 24 boxes at the default 3.6 m equal `WorkshopInput.Pipe` compiled (`WorkshopCanonicalColliders.Pipe(PipeLength.Default)`). The typed `length` (Metre, Real, 1–8, default 3.6) is checked against `PipeLength.Validate` at both bounds and one f32 step outside. Mutants: her M2/M2b killed; my **A1** (min 1→0.5) and **A2b** (unit Metre→None) killed by the parameter test; **A2** (unit and kind together) is rejected earlier, by the generator's BUNDLE006. | Pass |
| 9A.9. `cat-048a`, `cat-048b`, `cat-030` pass | My run: cat-048a 4/4, cat-048b 5/5, cat-030 7/7, all Clean | Pass |
| 9A.10. `PipeBend45.csproj` added; `pipe_bend_45` in the bundle; parity bit for bit | 40 literal rows equal the canonical table. Mutants: her M3 killed; my **A5** (friction 0.3→0.31) and **A6** (one rim dropped) killed; **A7** (`PipeBend45.csproj` references the engine project) killed by `PackageReferenceTests`. | Pass |
| 9A.10. `cat-049` passes | My run: cat-049 5/5, Clean | Pass |
| 9A.11. `PipeBend90.csproj` added; `pipe_bend_90` in the bundle; parity bit for bit | 72 literal rows equal the canonical table. Mutants: her M4 and M6 killed; my **A4** (one f32 ULP on row 40's `w`, 0.8577286→0.85772866, bits `3f5b941a`→`3f5b941b`) killed: it survives `Quaternion.Normalize` and fails at f32 bits ("PipeBend90.Colliders[40]: rotation … declared"). | Pass |
| 9A.11. `cat-050` passes | My run: cat-050 5/5, Clean | Pass |
| PKG-2 AC2. `git ls-files` empty for the four moved scenes; the four ids gone from the unpackaged set | `identity/deletion-check.txt` §1 is empty; the list is `["balloon", "delay", "switch", "tennis"]`. Re-add mutants: **D1** ×4 (each id back in the list) killed; **D2** ×4 (each `.tres` back at a re-added `parts/scenes/<x>.tscn`) killed by `…CatalogueMetadataAndCategory`; **D3** (all four stray copies re-added) survives the whole `dotnet test`, which is F15-3, already filed and not re-filed. | Pass |
| PKG-2 AC3. `cat-030`, `cat-048a`, `cat-048b`, `cat-049`, `cat-050`, `tube-snapping`, `cat-050-joined` pass on the merged candidate | My own run on the clean copy of the review head `d4195fb0` (code equal to the merge `08e72760`): cat-030 7/7, cat-048a 4/4, cat-048b 5/5, cat-049 5/5, cat-050 5/5, tube-snapping 13/13, cat-050-joined 6/6, plus capacity-9r 3/3, palette-placement 25/25 and the smoke pair 6/6. Verdict Pass, 92/92 across 12 files (`e2e-tier2-d4195fb0/summary.txt`). Her run was not reused (F15-6). | Pass |
| PKG-2 AC4. The review appends the dated addenda re-verifying 6.6, 6.7, 6.9, 6.10, 6.11 | Appended to the five records (section "Reopened-record addenda"); `git diff --numstat` shows 0 deletions for each. | Pass |
| 9A.8–9A.11. `dotnet build` 0 warnings, `dotnet test` 100% | Tier 1 below. | Pass |
| 9A.8–9A.11. Every `tools/e2e/*.test.ts` suite in one serial run | **Superseded, not run.** The owner's tiers and this brief supersede it; PKG-2 AC3 replaces it (ruling on CL4). | Superseded (not run) |
| Owner amendment: every "Deletes:" symbol returns 0 production hits; 9A.45a entries gone | §2–§4 of `identity/deletion-check.txt`: no code, scene, catalogue, content, tool or e2e file names an old path, and no runtime source composes `parts/scenes`. The remaining hits are the hashed `docs/*-sources.json` manifests, the frozen `catalogue-01.json` capture and the CAT-030/048/049/050 readiness specs pinned to `a6c914e`, all historical (PKG-1 treated the CAT-004 row the same way). The 9A.45a ratchet does not exist yet (the only known-violations list is the physics-law one in `WorkshopPhysicsLawTests`), so that clause is N/A. | Pass |

## Deletions

- **Scenes:** `git ls-files` is empty for the four old paths; all four are 100% renames (`deletion-check.txt` §8). Each moved scene's only `ext_resource` is `res://engine/MachinePart.cs`, and the scenes carry no `uid://`, so nothing references them except by path, and every path reference is now `res://elements/<Name>/…`.
- **Palette proof:** palette-placement places all four from the published bundle: 25/25 Clean in my run. The run's `.tap` and `.log` files contain no load failure (`Failed loading`, `Cannot open`, `not found`; `identity/runtime-reach.txt`).
- **Unpackaged set:** 8 → 4, each id guarded by a D1 mutant.
- **No format version, fixed capacity, shim or alias** is added: the declarations are data, and no engine file changes.

## The 9A.16 collider mismatch: not player-visible today

Her probe (`evidence-9a-pkg-2/scene-vs-canonical-probe.txt` on her branch) shows the scene-extracted sets differ from the canonical tables: the funnel and pipe rings by sign, the bends only by f32 rounding. I traced every consumer of the main-thread (scene-preferring) set:
- **The executing solver never sees it.** The Simulation worker excludes `GltfPhysicsBodyExtractor.cs` (`CuriousContraptions.Simulation.csproj` line 35). The wire and save encoders write no collider for `IWorkshopDeclaredColliders` instances (`WorkshopWire.cs` line 424, `WorkshopWireFlatBuffers.cs` line 772), and both decoders rebuild the funnel and bends with `WorkshopCanonicalColliders.For(partId)` (`WorkshopWire.cs` 492–493, `WorkshopWireFlatBuffers.cs` 938–939). `WorkshopSimulation` is constructed only by the worker (`Program.cs` line 87). There is no in-process or native solver path.
- **The pipe is canonical everywhere:** `WorkshopPipe.DeclaredColliders` is `WorkshopCanonicalColliders.Pipe(Length)`.
- **Main-thread compiles** (`MachineWorld.ValidateWorkshopRead`, `BrowserWorkshopClient.EnsureInstalledScene`) use colliders only for dynamic bodies (`PhysicsBodyReadSet.ValidateScene`) and collider ids and counts (`PhysicsContactWorkRead`, `ActivationNetwork`). The funnel and bends are static, and the counts match (72/40/72).
- **Selection** (`Workshop.Pick`) is by projected part origin, with no collider. **Snapping** (`TubeMouthSnap.Find`) reads only `Loc_Port_mouth_*` locators and bores.
- **PKG-2 changes nothing on this path:** byte-identical renames and `.tres` repoints.

**Which side is wrong (new, for 9A.16).** The canonical set is geometrically right, and the scene proxies are mis-authored. Pipe shell box 2 sits at (0, 0.4773, 0.4773), at θ = 45°, with its thin axis on local Y. The canonical rotation turns local Y by +45° about X to (0, 0.707, 0.707), which is radial. The scene's, as Godot's own `Basis.GetRotationQuaternion` reads it, turns it by −45° to (0, 0.707, −0.707), which is tangential, so the box stands as a fin across the bore instead of a wall. A transposed basis is the inverse rotation, which is exactly the conjugate pattern her probe shows for the funnel. If 9A.16 made scene extraction authoritative without re-authoring those `col_*` transforms, the pipe and funnel bores would break. The parity guard's `Placed` also says it mirrors `CaptureInstance`, but for the funnel and bends it skips `CaptureInstance`'s registry branch. Filed as F15-7.

## Behaviour in Chrome (my run on the clean copy)

- **Run** `20261021T020207Z-92416`, `run --changed` over her nine-suite list, which my independent derivation reproduces (below), plus the smoke pair.
  - Clean copy at `d4195fb0`; `sourceStatusDigest e3b0c442…` (the empty status); `suiteSourceDigest fe1c75cd…` and `runnerSourceDigest a946b588…`, equal to her run's.
  - Bundle identity `c389a3b7…3e14`, the same at start and end, with the served check passing for 285 files.
  - Chrome 155.0.8059.86; from 2026-10-21T02:02:07Z to 02:18:35Z. Start record: `e2e-run-start.txt`; stdout: `e2e-run.log`.
- **Result: verdict Pass, 92/92 across 12 files, closeFindings 0, debtFaults 0, wideOffset 0.**
- **tube-snapping ran twice.** The first attempt passed 13/13, but the runner marked it Contended and re-ran it, Clean, 13/13. The contention was my own: I ran the status-digest brute force (`identity/source-digests.txt`, written at 02:07:45Z) inside its window (02:06:24–02:09:03). Both attempts pass, so this is not a flake.
- **What the player sees:**
  - straight-pipe traversal with lateral containment (cat-048a);
  - the three tube levels solved through the palette (cat-048b, cat-049, cat-050);
  - the joined route with its Precise negatives (cat-050-joined);
  - funnel gathering, the outer-shell negative and the snapped outlet (cat-030);
  - snapped joints crossed continuously, straight→45°, 45°→45°, straight→90° and funnel→pipe (tube-snapping 10–13);
  - every palette part, including the four moved scenes, placed from the published bundle (palette-placement).
  
  All pass with exact Reset and Save/Load.
- **An unused attempt is kept:** `e2e-run-usage-error.log`, where `run` was started without `--out` and exited 2 before publishing.

## Tier 1, on the clean copy at `d4195fb0`

| Check | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx` (Debug, then Release) | 0 warnings, 0 errors, both | `tier1/build.log`, `tier1/release-build.log` |
| `dotnet test CuriousContraptions.slnx` | 2229/2229 (1897 + 162 + 78 + 75 + 17), equal to her count | `tier1/test.log` |
| C# static gate (the pre-commit hook on the tree) | exit 0, 123/123 | `tier1/pre-commit-hook-d4195fb0.log` |
| Node harnesses `node --experimental-vm-modules --test tools/*.test.mjs` | 88/88 | `tier1/node.log` |
| LifecycleContract (Release) | 510, 0 failures | `tier1/lifecycle.log` |
| WireContract (Release) | 108, 0 failures | `tier1/wire.log` |
| Ownership `--oracles .` (after the web restore) | exit 0, Positive, 22/22 attacks rejected; line 1 byte-identical to hers (SHA-256 `7b72a39c…`) | `tier1/ownership-oracles.log`, `tier1/web-restore.log` |
| `anvil check --since dc1f59a1` | no files to analyse (anvil scans no C#; the static gate covers it) | `tier1/anvil-check.log` |

Two first attempts failed for environment reasons and are kept: `tier1/node-without-vm-modules-flag.log` (6 failures, `vm.SourceTextModule is not a constructor`, without the flag) and `tier1/ownership-oracles-before-web-restore.log` (the web project's `nuget.g.props` was missing on a fresh copy). Neither is a code finding.

Anvil write validation:
- The first drafts of this record and of the five addenda were written before `anvil_apply_patch` was called. That is a process slip on my side.
- Every later edit was validated first, and each returned `allow`.
- Afterwards, each record's full addendum diff went through `anvil_apply_patch`, and this record's full content through `anvil_validate_write`. All returned `allow`.
- The commit and timestamps of the first Tier 1 pass are in `tier1/identity.txt`.

## Mutants (reviewer's own; raw output in `mutants/<name>.txt`)

Each mutant was applied as a patch to the mutant copy, the named tests were run, and the tree was restored with `git checkout` (empty `git status` after each). Driver: `mutants/run.log`.

| Mutant | Target | Result |
| --- | --- | --- |
| D1 ×4 | `funnel`, `pipe`, `pipe_bend_45`, `pipe_bend_90` each put back in the unpackaged list | Killed ×4 (`TheCatalogueIdsWithoutAPackageAreExactlyTheFourListed`) |
| D2 ×4 | each `.tres` pointed back at a re-added `parts/scenes/<x>.tscn` | Killed ×4 (`…CatalogueMetadataAndCategory`) |
| D3 | all four stray copies re-added under `parts/scenes/` | **Survives** the whole `dotnet test` (2229 pass; `D3-stray-scenes-full-test.txt`). This is F15-3, already filed. AC2's `git ls-files` check holds. |
| A1 | pipe `length` Minimum 1→0.5 | Killed (`…ParameterDefaultsAndBounds`) |
| A2 | pipe `length` unit None and kind WholeNumber | Killed at build by the generator (`BUNDLE006: … Default: outside its declared range`), not by the parity test |
| A2b | pipe `length` unit Metre→None only | Killed (`…ParameterDefaultsAndBounds`, `Assert.Equal` on the unit) |
| A3 | funnel ring rotation in the scene's (conjugate) convention | Killed (`…CollidersAndMaterial`, "Funnel.Colliders[0]: rotation") |
| A4 | bend-90 row 40 `w` one f32 ULP up | Killed (same test, at f32 bits) |
| A5 | bend-45 friction 0.3→0.31 | Killed (same test, the material assert) |
| A6 | bend-45 last rim dropped | Killed ("PipeBend45: 39 colliders declared, 40 at runtime.") |
| A7 | `PipeBend45.csproj` references `CuriousContraptions.csproj` | Killed (`PackageReferenceTests(PipeBend45)`) |
| A8 | funnel inlet collar y 1.45→1.46 | Killed ("Funnel.Colliders[64]: offset") |

A sub-binary16 change to an offset or extent would survive by design, because the runtime stores those lanes as `Half` (`Same`). So "bit for bit" means exact at the runtime's own lane, which is PKG-1's ruling.

Her seven mutants (`evidence-9a-pkg-2/mutants.txt`) were read: each names the failing assertion, and each targets a distinct AC.

## Suite selection, derived independently

- `grep -l` over `tools/e2e/*.test.ts` for the four ids and the four tube levels returns exactly nine suites: capacity-9r, cat-030, cat-048a, cat-048b, cat-049, cat-050, cat-050-joined, palette-placement, tube-snapping.
- The runner's element selection over the twelve declaration, scene and `.tres` paths returns the same nine plus the smoke pair (`identity/select-elements-reviewer.txt`), with a reason per suite.
- So her 11-file set is complete, and my run used it.

## Rulings on her Spec Change Log

- **CL1 (canonical tables as "current").** Accepted. The AC's Given allows either; the canonical set is the one the worker simulates and every load decodes, and it is the geometrically correct one (above). Filed with the direction to 9A.16 (F15-7).
- **CL2 (pipe colliders at the default length).** Accepted, as for Ramp and Wall. The length-generated colliders stay engine-side until 9A.16/9A.17.
- **CL3 (no PKG-2 suite).** Accepted. The own suites assert exact Run/Reset (`expectResetMatches`) and Save/reload/Load (`assertLoadRestores`) through actual UI controls.
- **CL4 (tier).** Accepted. `select --diff` picks Full only for build, runner and moved-scene paths; no solver, worker, wire, compiler or shared driver file changes. The runner gaps are hers to F.14.
- **CL5, CL6.** Accepted; F15-3 covers the stray-copy survivor (my D3).

## Filed to Story F.15, "Package Follow-ups" (non-blocking)

- **F15-6. Her Chrome run's source identity does not reproduce.** `sourceHead 66597b36`, `suiteSourceDigest fe1c75cd…` and `runnerSourceDigest a946b588…` reproduce exactly from a clean `66597b36` (`identity/source-digests.txt`). But `sourceStatusDigest 93fe5a7e…` matches none of the 1,024 combinations of ten plausible dirty files (the suite list, the run log, the then-untracked spec, `epics.md`, `sprint-status.yaml`, the two selection files, publish and preview logs, a summary; `identity/source-digests.txt`). So the run's dirty files are unknown, and I re-ran the suites on a clean copy instead of reusing it. Have the runner write the porcelain status listing beside its digest, so a reviewer can see what was dirty.
- **F15-7. The scene `col_*` proxies of the pipe and funnel are transposed** (above). The physics is right today. Story 9A.16 must re-author them, or keep the canonical set, before scene extraction becomes authoritative. The parity guard's `Placed` comment should say it bypasses the registry branch.
- **F15-8. The reopened 6.x stories have clauses that no repair story owns.** They are listed per record in the addenda. 6.6: the oversized-ball stop. 6.7: high entry speed and edge angles (only one pipe pose; no speed assertion), plus the story spec's 1200-tick bound. 6.9: no added speed; the rim, oversize and sidewall controls; the Precise alternative route; the icon, rails and collars. 6.10: the rotated, oversize and rim controls; the bore, collars, rails and icon. 6.11: fast and rotated entries; 0.75 m off-centre; rim, oversize and mesh-match qualification; the bend join. No catalogue ball is larger than the 0.65 m bore, so the oversize controls need a fixture or a part first. The coordinator should give each an owning story, or have the owner waive it. Until then, those five records stay `in-progress`.
- **F15-9. Pipe parity at one length only.** The guard compiles the pipe at 3.6 m. A declaration that computed its colliders wrongly at other lengths could not be caught, because the declaration has only the default set (CL2). This closes with 9A.16/9A.17's per-parameter collider generation.

Not filed: the readiness specs `CAT-030/048/049/050` still list `parts/scenes/<x>.tscn` in their `a6c914e` legacy inventories. These are historical pins, treated like PKG-1's CAT-004 row.

## Reopened-record addenda (PKG-2 AC4)

Each record received "Addendum 21 Oct 2026, retro-verification (Murdoch-19, PKG-2 closing review)" at its end. Integrity (`integrity.txt`):
- `git diff --numstat` shows 30, 30, 33, 32 and 31 added lines and 0 deletions, so the original text is unchanged.
- Each of the 28 quoted TAP lines occurs verbatim in this run's `.tap` files.
- Each of the 18 distinct evidence paths cited in the addenda exists, and so does each path cited in this record (bar the `mutants/<name>.txt` pattern).

| Original | Record | Fresh verdict | Open clauses and owners |
| --- | --- | --- | --- |
| 6.6 | `review-6-6-straight-pipe.md` | Incomplete | oversized-ball stop → unowned (F15-8) |
| 6.7 | `review-6-7-clear-pipe.md` | Incomplete | entry speed and edge angles unasserted → unowned (F15-8) |
| 6.9 | `review-6-9-pipe-bend-45.md` | Incomplete | nudging → Story 15.0; controls, speed, Precise route, art → unowned (F15-8) |
| 6.10 | `review-6-10-pipe-bend-90.md` | Incomplete | depth-error nudging → Story 15.0; controls, art → unowned (F15-8) |
| 6.11 | `review-6-11-funnel.md` | Incomplete | air → Story 12.2; light → Story 13.1; entry variants, qualification → unowned (F15-8) |

`sprint-status.yaml` is left to the coordinator. By the closure rule, none may be marked `done` on these addenda. 6.6, 6.7 and 6.11 (weak, currently `done`) reopen to `in-progress`, and 6.9 and 6.10 stay `in-progress`.
