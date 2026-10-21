---
title: 'Story PKG-2: Funnel, Clear Pipe and Pipe Bend Element Packages (9A.8 + 9A.9 + 9A.10 + 9A.11)'
type: 'feature'
created: '2026-10-21'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-pkg-2-funnel-clear-pipe-and-pipe-bend-element-packages-9a8--9a9--9a10--9a11'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9a-pkg-1-receiver-bowling-springboard-packages.md'
---

## Intent

PKG-2 merges Stories 9A.8 (Funnel, `funnel`), 9A.9 (Clear pipe, `pipe`), 9A.10 (45° pipe bend, `pipe_bend_45`) and 9A.11 (90° pipe bend, `pipe_bend_90`). Each element gets its own `elements/<Name>/` package, declared only there, and enters the generated bundle with today's runtime values. The parity guard proves the colliders equal bit for bit, so nothing the player sees changes. Audit finding A15 shrinks from six unpackaged elements to two (`delay`, `switch`, owned by PKG-3).

## Implementation Notes

Amelia-18, 21 Oct 2026. Base: main `12658442` fast-forwarded, merged with PKG-1 `8333682f` (in review). Candidate: `66597b36`. Evidence: [evidence-9a-pkg-2/](evidence-9a-pkg-2/).

**Deletes:**
- `parts/scenes/funnel.tscn`, `pipe.tscn`, `pipe_bend_45.tscn` and `pipe_bend_90.tscn`, moved (100% `git mv` renames, scene bytes unchanged) into `elements/Funnel/`, `elements/Pipe/`, `elements/PipeBend45/` and `elements/PipeBend90/`. The four `.tres` files now point at `res://elements/<Name>/<scene>.tscn`; Story 9A.14 deletes the `.tres` files.
- `funnel`, `pipe`, `pipe_bend_45` and `pipe_bend_90` from the parity guard's unpackaged set, which goes from 8 ids to 4: `balloon`, `delay`, `switch`, `tennis` (`TheCatalogueIdsWithoutAPackageAreExactlyTheFourListed`).

`rg` proof: [deletion-rg-proof.txt](evidence-9a-pkg-2/deletion-rg-proof.txt). 0 production, test, tool or current-catalogue hits. The remaining hits are records pinned to baseline `a6c914e` (the CAT-030/048/049/050 readiness specs and the Epic 7 legacy ledger) and the frozen `catalogue-01.json` capture, left unchanged as PKG-1 left the CAT-004 row.

**What was built:**
- **Packages**, each Static, category Motion, footprint (1, 0, 0), no ports, no events, material restitution 1, friction 0.3, rolling 0, bounce threshold 0.1, and referencing only `CuriousContraptions.Abstractions` and `.Mechanical`:
  - `elements/Funnel`: the 72 boxes (4 rings of 16 tapered wall boxes, 8 collar rims), computed with the canonical table's own `MathF` operation order.
  - `elements/Pipe`: the typed `length` parameter (Metre, Real, 1 to 8, default 3.6, f32 lane, admitted by `PipeLength.Validate`) and the 24 boxes at the default length (16 shell boxes of half-length 1.8, 8 rims).
  - `elements/PipeBend45` (40 boxes) and `elements/PipeBend90` (72 boxes): the canonical rows as literals, each rotation normalised exactly as `CanonicalRotation.Normalize` normalises it (`Quaternion.Normalize`). The rows were extracted mechanically from `WorkshopCanonicalColliders.cs`, and the parity guard proves every bit.
- **Parity (`BundleCatalogueParityTests`).** New `Placed` arms (the pipe as `WorkshopInput.Pipe`, the funnel and bends as generic instances with their canonical colliders), the `pipe` `Parameters` arm, and the funnel and bends joined the `Canonical` comparison. The existing metadata, footprint, collider, material, parameter and body tests now cover the four packages; no test was added for them because the guard is data-driven.
- **Tests.** `PackageReferenceTests.TheFunnelPipeAndPipeBendPackagesExist`; four `SelectionTests.CatalogueAndScenesMapByContent` rows for the moved scenes; the four scene tests (`FunnelTests`, `StraightPipeTests`, `PipeBend45Tests`, `PipeBend90Tests`) load the moved scenes.
- **Pinned lists:** `CuriousContraptions.slnx`, `CSharpStaticGateTests.TrimAnalyzedProjects`, `ImpactRules.ElementPackages`.
- **The isolation authority.** The A15 row lists `delay` and `switch`, owned by PKG-3.

**Test-first and mutants:**
- Red: [red-parity-tests.txt](evidence-9a-pkg-2/red-parity-tests.txt), [red-bundle-tests.txt](evidence-9a-pkg-2/red-bundle-tests.txt) and [red-selection-tests.txt](evidence-9a-pkg-2/red-selection-tests.txt) (6 failing before the packages existed).
- Mutants, each killed by the named test ([mutants.txt](evidence-9a-pkg-2/mutants.txt)):

| AC | Mutant | Killed by |
|---|---|---|
| 9A.8 colliders | Funnel ring-2 wall width 0.46 → 0.47 | `…FootprintCollidersAndMaterial` (`Funnel.Colliders[16]`) |
| 9A.9 parameter, colliders | Pipe default length 3.6 → 3.7 | `…ParameterDefaultsAndBounds`, `…FootprintCollidersAndMaterial` |
| 9A.9 range | Pipe maximum length 8 → 9 | `…ParameterDefaultsAndBounds` |
| 9A.10 colliders | Bend-45 row 1 rotation x 0.3822225 → 0.3822226 (one f32 step) | `…FootprintCollidersAndMaterial` (`PipeBend45.Colliders[1]`) |
| 9A.11 colliders | Bend-90 last rim z 0.8 → 0.81 | `…FootprintCollidersAndMaterial` (`PipeBend90.Colliders[71]`) |
| scene move | Funnel `ScenePath` back to `parts/scenes/` | `…CatalogueMetadataAndCategory` |
| isolation | `PipeBend90.csproj` references the sibling `elements/Pipe` | `PackageReferenceTests(PipeBend90)` |

**Tier 1 on the candidate:**
- `dotnet build CuriousContraptions.slnx`: 0 warnings (Debug and Release); the pre-commit C# gate passed (123/123).
- `dotnet test CuriousContraptions.slnx`: 2229/2229 ([tier1-test.txt](evidence-9a-pkg-2/tier1-test.txt)).
- Ownership `--oracles .`: exit 0, 22/22 attacks rejected ([log](evidence-9a-pkg-2/ownership-oracles.log)).
- WireContract: 108 oracles, 0 failures. LifecycleContract: 510, 0 failures.
- Node harnesses: 88/88 ([tier1-node.txt](evidence-9a-pkg-2/tier1-node.txt)).
- `anvil check` on the changed files: no warnings.

## Spec Change Log

Amelia-18, 21 Oct 2026, under the owner's delegation to decide small gaps; no AC is weakened.

- **CL1. Which colliders are "current" (9A.8 Given names the registry and `WorkshopCanonicalColliders.For`).** A probe ([source](evidence-9a-pkg-2/scene-vs-canonical-probe.cs.txt), [result](evidence-9a-pkg-2/scene-vs-canonical-probe.txt)) compared the scene-extracted sets with the canonical tables at bits: 64 of 72 funnel, 15 of 24 pipe, 36 of 40 bend-45 and 64 of 72 bend-90 rows differ in rotation bits, and the funnel and pipe ring rotations have the opposite sign about X. The packages declare the canonical tables, because they are what the physics runs: the Simulation worker excludes `GltfPhysicsBodyExtractor` and compiles the construction its wire decode rebuilds with `WorkshopCanonicalColliders.For` (`WorkshopWireFlatBuffers.cs`, `WorkshopWire.cs`), and every load decodes the same way. Main-thread placement of the funnel and bends prefers the scene set; that split is pre-existing and is filed to Story 9A.16, which owns both tables and the scene reconciliation (as 9A.4 filed the ball detector's). Neither table is edited here.
- **CL2. The pipe's colliders depend on its length.** As the Ramp and Wall packages do, the pipe declares its colliders at the default dimensions (3.6 m) and its length as a typed parameter. The generated-per-length rule stays in the engine until Story 9A.16/9A.17 move collider generation to the bundle.
- **CL3. No PKG-2 Chrome suite.** Unlike PKG-1, every one of the four parts already has its own suite with exact Run/Reset (`expectResetMatches`) and Save/reload/Load (`assertLoadRestores`) through actual UI controls: cat-030 (funnel), cat-048a (pipe), cat-049 (bend-45) and cat-050 (bend-90); palette-placement places all four from the free-workshop palette. A new suite would duplicate them.
- **CL4. Verification tier.** `select --diff 8333682f` picks Full ([raw](evidence-9a-pkg-2/runner-select-diff-8333682f.txt)) only because of non-engine files: the `.slnx` and the four new `.csproj` (Build), `tools/E2eRunner*` (RunnerItself), `tools/BundleElements.Tests` (unmatched) and the four deleted `parts/scenes/*.tscn` paths (Unknown: once moved, no head catalogue names them). The diff touches no solver, clock, worker, wire, compiler, driver or shared test module, and the scenes are byte-identical renames. So the AC-named suites, every suite that places one of the four parts (capacity-9r places the 90° bend) and palette-placement were run by name through `run --changed`, with the smoke pair (see E2E). The two runner gaps are filed to Story F.14.
  - `select` was used to read the selection because `run --diff` in Full mode would have launched the Tier 3 full run.
  - The runner's own element selection, `select --changed` over only the four declarations, the four moved scenes and the four `.tres` files ([input](evidence-9a-pkg-2/select-elements.txt), [output](evidence-9a-pkg-2/select-elements-output.txt)), is exactly the 11 files run: the own suites, every placer (including levels that pre-place a tube part: `clear_pipe`, `gentle_bend`, `quarter_bend`, `joined_pipe`), palette-placement and the smoke pair.
  - The ACs of 9A.8–9A.11 each say "every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run". The coordinator's 21 Oct tier rules (Tier 3 only for a material engine change or a checkpoint) and PKG-2's own suite AC supersede that, as PKG-1 CL8 recorded.
- **CL6. PKG-1 review findings that apply here.** F15-3 (nothing stops a moved scene coming back: a stray re-added `parts/scenes/<scene>.tscn` copy passes every test) applies equally to the four PKG-2 scenes and is already filed; AC2's `git ls-files` check holds on this candidate. A `.tres` pointed back at the old path (PKG-1 D3) or a `ScenePath` mutant is killed by `…CatalogueMetadataAndCategory` (M5 here). F15-1, F15-2 (slider validator) and F14-3 (inertia) do not apply: the four packages are static, with no slider and zero inertia.
- **CL5. Reopened-record addenda.** PKG-2's last AC (dated addenda re-verifying Stories 6.6, 6.7, 6.9, 6.10 and 6.11) belongs to the independent review and is left for it.

## E2E (Chrome)

Selection: [tier2-suites.txt](evidence-9a-pkg-2/tier2-suites.txt): cat-030, cat-048a, cat-048b, cat-049, cat-050, cat-050-joined, tube-snapping, capacity-9r and palette-placement, plus the runner's smoke pair engine-core-2a1 and engine-core-2b1.

**Result on `66597b36`: verdict Pass** ([summary](evidence-9a-pkg-2/e2e-tier2-66597b36/summary.txt), [log](evidence-9a-pkg-2/e2e-tier2-66597b36.log)).
- `run --changed tier2-suites.txt`: 11 files passed, 79/79 tests, 0 failures, every observation Clean, no rerun.
- cat-030 7/7, cat-048a 4/4, cat-048b 5/5, cat-049 5/5, cat-050 5/5, cat-050-joined 6/6, tube-snapping 13/13, capacity-9r 3/3, palette-placement 25/25 (every palette part from the published bundle, including the four moved scenes), smoke pair 6/6.
- Identities: sourceHead `66597b36`; bundleIdentity `7895f6bc…7434`, the same at start and end; Chrome 155.0.8059.86.

**Merge of main `dc1f59a1` (PKG-1 done), merge commit `08e72760`.** Main's diff from `8333682f` touches only `_bmad-output` (PKG-1's review record, evidence and statuses). The merge had no conflicts; main's PKG-1 `done` statuses sit beside PKG-2's `review` statuses. `git diff --stat 66597b36 08e72760 -- . ':(exclude)_bmad-output'` is empty ([raw](evidence-9a-pkg-2/merge-dc1f59a1-code-diff.txt)), so the Tier 1 and Chrome results on the code candidate `66597b36` apply to the review head unchanged.

**Identities.** Code candidate: `66597b36`. Review head: the docs commit that adds this paragraph, on top of merge `08e72760`.
