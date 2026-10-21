---
title: 'Story PKG-3: Delay Box and Impact Switch Element Packages (9A.7 + 9A.13)'
type: 'feature'
created: '2026-10-21'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-pkg-3-delay-box-and-impact-switch-element-packages-9a7--9a13'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9a-pkg-2-funnel-pipe-bend-packages.md'
---

## Intent

PKG-3 merges Stories 9A.7 (Delay Box, `delay`) and 9A.13 (Impact Switch, `switch`). Each element gets its own `elements/<Name>/` package, declared only there, and enters the generated bundle with today's runtime values. The parity guard proves the values equal, so nothing the player sees changes. Audit finding A15 has no delivered element left without a package; the unpackaged catalogue ids are exactly `tennis` and `balloon`, which Story 9A.14 removes from the palette and Story 12.1 delivers.

## Implementation Notes

Amelia-19, 21 Oct 2026. Base: main `dc1f59a1` fast-forwarded, merged with PKG-2 `d4195fb0` (in review). Code candidate: `d925637c`. Evidence: [evidence-9a-pkg-3/](evidence-9a-pkg-3/).

**Deletes:**
- `parts/scenes/delay.tscn` and `parts/scenes/switch.tscn`, moved (100% `git mv` renames, scene bytes unchanged) into `elements/DelayBox/` and `elements/ImpactSwitch/`. The two `.tres` files now point at `res://elements/<Name>/<scene>.tscn`; Story 9A.14 deletes the `.tres` files. `parts/scenes` now holds the 13 scenes 9A.14 moves.
- `delay` and `switch` from the parity guard's unpackaged set, which goes from 4 ids to exactly `balloon` and `tennis` (`TheCatalogueIdsWithoutAPackageAreExactlyTennisAndBalloon`).

`rg` proof: [deletion-rg-proof.txt](evidence-9a-pkg-3/deletion-rg-proof.txt). 0 production, test, tool or current-catalogue hits. The remaining hits are the CAT-022 and CAT-063 readiness specs pinned to baseline `a6c914e` and the frozen `catalogue-01.json` capture, left unchanged as PKG-1 and PKG-2 left theirs.

**What was built:**
- **`elements/DelayBox`** (`ISignalElement`; references only `CuriousContraptions.Abstractions` and `.Signal`, as 9A.7 names): Static footprint (1, 0, 0), category Control, the typed `duration_seconds` parameter (Second, Real, 0.1 to 12, default 1, the `DelayDuration` range), the pulse `ActivationIn` (−0.72, 0, 0) and `ActivationOut` (0.72, 0, 0) ports, the housing foot box (offset (0, −0.7, 0), half-extents (0.675, 0.075, 0.4)) with the compiler's static material (restitution 1, friction 0.3, rolling 0, bounce threshold 0.1), and one `TriggerActivated` event.
- **`elements/ImpactSwitch`** (`IMechanicalElement`, `ISignalElement`; Abstractions, `.Mechanical` and `.Signal`): Static, mass 0, category Power, the typed `trigger_threshold` parameter (MetrePerSecond, Real, 0 to 64, default 0.8, the `ContactTriggerSettings` range), the pulse `ActivationOut` (0.45, 0, 0.4), the base (offset (0, −0.15, 0), half-extents (0.55, 0.125, 0.5)) and button (offset (0, 0.05, 0), half-extents (0.4, 0.09, 0.375)) boxes with the same material, and one `TriggerActivated` event. No electrical reference: Story 9.5b adds the electrical mode.
- Every body declares colliders (BUNDLE004). No Abstractions, schema or bundle format change; no new event kind.
- **Parity (`BundleCatalogueParityTests`).** New `Placed` arms (`WorkshopInput.Switch` / `.Delay` with the record defaults) and `Parameters` arms (Half lane, admitted through the record's own `Validate`). Neither id joins `Canonical`: neither has a `WorkshopCanonicalColliders` arm, so the compiled colliders are the reference. The data-driven metadata, port, footprint, collider, material, parameter and body tests now cover both packages.
- **Tests.** `PackageReferenceTests.TheDelayBoxAndImpactSwitchPackagesExist`; two `SelectionTests.ElementWithoutAPrimarySuiteIsATypedError` rows for the moved scenes (see CL3); the two `SelectionTests` area lists gain `pkg-3`.
- **Pinned lists:** `CuriousContraptions.slnx`, `CSharpStaticGateTests.TrimAnalyzedProjects`, `ImpactRules.ElementPackages`.
- **Chrome suite `tools/e2e/pkg-3.test.ts`** (see [E2E](#e2e-chrome)), registered in `SuiteCatalogue` as `Pkg3` (SerialAlone under the new-suite rule, no Primary) with a README timing row.
- **The isolation authority.** The A15 row says every delivered element has a package once PKG-3 passes; it closes at the terminal Pass.

**Test-first and mutants:**
- Red: [red-parity-tests.txt](evidence-9a-pkg-3/red-parity-tests.txt), [red-bundle-tests.txt](evidence-9a-pkg-3/red-bundle-tests.txt), [red-selection-tests.txt](evidence-9a-pkg-3/red-selection-tests.txt) (4 failing before the packages existed).
- Mutants, each killed by the named test ([mutants.txt](evidence-9a-pkg-3/mutants.txt)):

| AC | Mutant | Killed by |
|---|---|---|
| 9A.7 parameter | Delay default 1 → 1.01 | `…ParameterDefaultsAndBounds` |
| 9A.7 range | Delay maximum 12 → 12.5 | `…ParameterDefaultsAndBounds` |
| 9A.7 colliders | Delay foot y −0.7 → −0.75 | `…FootprintCollidersAndMaterial` |
| 9A.7 ports | Delay `ActivationIn` x −0.72 → −0.71 | `…RuntimePortsInOrder` |
| scene move | Delay `ScenePath` back to `parts/scenes/` | `…CatalogueMetadataAndCategory` |
| 9A.13 parameter | Switch default 0.8 → 0.85 | `…ParameterDefaultsAndBounds` |
| 9A.13 colliders | Switch button half-height 0.09 → 0.1 | `…FootprintCollidersAndMaterial` |
| 9A.13 metadata | Switch category Power → Control | `…CatalogueMetadataAndCategory` |
| 9A.13 body | Switch Static → Dynamic | `…BodyMotionMassDragAndGravity` |
| isolation | `ImpactSwitch.csproj` gains `Abstractions.Electrical` | `PackageReferenceTests(ImpactSwitch)` |

**Tier 1 on `d925637c`:**
- `dotnet build CuriousContraptions.slnx`: 0 warnings (Debug and Release); the pre-commit C# gate passed (123/123).
- `dotnet test CuriousContraptions.slnx`: 2236/2236 ([tier1-test-d925637c.txt](evidence-9a-pkg-3/tier1-test-d925637c.txt)).
- Ownership `--oracles .`: exit 0, 22/22 attacks rejected ([log](evidence-9a-pkg-3/ownership-oracles.log)).
- WireContract: 108 oracles, 0 failures ([json](evidence-9a-pkg-3/wire-contract.json)). LifecycleContract: 510, 0 failures ([log](evidence-9a-pkg-3/lifecycle-contract.log)).
- Node harnesses: 88/88 ([tier1-node.txt](evidence-9a-pkg-3/tier1-node.txt)).
- `anvil check` on the changed non-C# files: no warnings ([anvil-check.txt](evidence-9a-pkg-3/anvil-check.txt)).

## Spec Change Log

Amelia-19, 21 Oct 2026, under the owner's delegation to decide small gaps; no AC is weakened.

- **CL1. `CosmeticCurves.Delay` and `CosmeticCurves.ImpactSwitch` stay in the engine (9A.7 and 9A.13 Givens).** They are presentation bindings, and the Abstractions have no presentation-binding contract. Story 9A.25a designs the committed-state bindings and moves every `CosmeticCurves.*` arm out of the engine (isolation authority, row A4/A10), as PKG-1 CL2 decided for the Receiver. Every other value in the Givens is declared and proven equal.
- **CL2. Stale comparators in the Givens.** `WorkbenchFootprint.Of` was deleted by Story 9R.5b; the footprint is compared with a one-instance compile (9A.4 CL2). Neither id has a `WorkshopCanonicalColliders.For` arm, so the colliders are compared with the compiled scene (9A.4 CL3). Ports are compared with `WorkshopPorts.For` and `WorkshopPorts.LocalPosition` bit for bit.
- **CL3. Domains and keys.** The Delay is Signal only, as 9A.7 fixes its references. The switch is Mechanical plus Signal, following the static contact parts (bumper, pressure plate); no Electrical until Story 9.5b. The parameter keys follow the clock's `interval_seconds`: `duration_seconds` and `trigger_threshold`. Both events use the capability kind `TriggerActivated`; no element-named kind is added (invariant 5). Neither occurrence has a runtime ring producer yet (D1, Story 9A.22).
- **CL4. No Primary suite for `delay` or `switch`.** No suite declares either as Primary, so the runner refuses `select`/`run --diff d4195fb0` with `NoPrimarySuite` ([select](evidence-9a-pkg-3/runner-select-diff-d4195fb0.txt)), as it did for PKG-1's `basket`. Their primaries are Story 9R.17's `cat-022` (merged into 9.5a) and Story 9R.15's `cat-063` (merged into C-Switch); neither file exists, and adding them is those stories' work. The two moved scenes are therefore `ElementWithoutAPrimarySuiteIsATypedError` rows, which still prove that the scenes map to their parts by content. `pkg-3` declares no Primary, like `pkg-1`.
- **CL5. Verification tier.** The diff touches no solver, clock, worker, wire, compiler, driver or shared test module; the Full triggers would be the `.slnx`, the two `.csproj`, `tools/E2eRunner*` and `tools/BundleElements.Tests`, which are non-engine, and the scenes are byte-identical renames. So the affected suites were run by name through `run --changed` (see E2E). The ACs of 9A.7 and 9A.13 each say "every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run"; the coordinator's 21 Oct tier rules and PKG-3's own AC ("the union of both stories' named suites passes once") supersede that, as PKG-1 CL8 and PKG-2 CL4 recorded.
- **CL6. PKG-1 and PKG-2 findings that apply here.** F15-3 (a stray re-added `parts/scenes/<scene>.tscn` copy passes every test) applies to the two scenes and is already filed; AC2's `git ls-files` check holds on this candidate. A `ScenePath` mutant is killed (table above). The slider and inertia findings do not apply: both packages are static with no slider.

## E2E (Chrome)

Selection: [tier2-suites.txt](evidence-9a-pkg-3/tier2-suites.txt), the union of:
- the AC-named suites: 9A.7's anim-1b, anim-1c, cat-015a-sidekick, cat-015b, cat-037, cat-062 and cat-062b, and 9A.13's cat-037, anim-1b, anim-1c and engine-core-2c;
- every suite whose `Places` holds the Delay or the Impact switch: cat-037 (the latch suite), anim-1b, anim-1c, cat-052-lamp (the counter, latch and clock chain with a Delay) and capacity-9r5b;
- every suite whose declared level pre-places one: only `delayed_signal` (anim-1b, anim-1c) among the levels the catalogue declares;
- the own suite pkg-3, palette-placement (every palette part from the published bundle) and the runner's smoke pair engine-core-2a1 and engine-core-2b1;
- cat-022 and cat-063 do not exist (CL4).

**The PKG-3 suite (`tools/e2e/pkg-3.test.ts`).** Actual UI controls only, with the 9R.1 snapshot helper:
1. Place a lifted Basketball above an Impact switch, a Delay and a Signal lamp from the palette; wire Delay `ActivationOut` → lamp and switch `ActivationOut` → Delay `ActivationIn` through the contextual Connect rows. The save holds `ball`, `switch`, `delay` and `lamp` with ids 1–4 and exactly the two typed activation links. Two Runs: each fires the switch and lights the lamp through the Delay, and each Reset matches S0 exactly (`expectResetMatches`).
2. Control: without the switch → Delay wire the switch fires but the lamp stays dark; Reset is exact.
3. Save, reload and Load: the loaded bench matches S0 (`assertLoadRestores`), saves the same parts and links, replays the lit lamp and resets exactly.

**Runner.** `run --diff d4195fb0` refuses with `NoPrimarySuite` for `delay` ([raw](evidence-9a-pkg-3/runner-run-diff-d4195fb0.txt); `select` gives the same, [raw](evidence-9a-pkg-3/runner-select-diff-d4195fb0.txt)), so the selection above ran by name (CL4, CL5). The first `run --diff` attempt hit `RunnerBusy` while the PKG-2 reviewer held the shared runner lock; it was repeated after the lock was released.

**Result on `d925637c`: verdict Pass** ([summary](evidence-9a-pkg-3/e2e-tier2-d925637c/summary.txt), [log](evidence-9a-pkg-3/e2e-tier2-d925637c.log)).
- `run --changed tier2-suites.txt`: 14 files passed, 79/79 tests, 0 failures, every observation Clean, no rerun.
- pkg-3 3/3, cat-037 6/6, anim-1b 3/3, anim-1c 4/4, cat-052-lamp 6/6, capacity-9r5b 3/3, cat-062 7/7, cat-062b 7/7, cat-015a-sidekick 3/3, cat-015b 4/4, engine-core-2c 2/2, palette-placement 25/25 (every palette part from the published bundle, including the Delay and Impact switch from their moved scenes), smoke pair 6/6.
- Identities: sourceHead `d925637c`; bundleIdentity `1253382d…2936`, the same at start and end; Chrome 155.0.8059.86.

**Merge of main `a8a3eab1` (PKG-2 done), merge commit `6f2b3b74`.** Main's diff from `d4195fb0` touches only `_bmad-output` (PKG-2's review record, evidence, statuses and the new Story 9R.23). The merge had no conflicts; main's statuses are kept, and PKG-3's `review` statuses sit beside them. `git diff --stat d925637c 6f2b3b74 -- . ':(exclude)_bmad-output'` is empty ([raw](evidence-9a-pkg-3/merge-a8a3eab1-code-diff.txt)), so the Tier 1 and Chrome results on the code candidate `d925637c` apply to the review head unchanged.

**Identities.** Code candidate: `d925637c`. Review head: the docs commit that adds this spec, on top of merge `6f2b3b74`.

**Filed.** Nothing new. The two primary-suite gaps already belong to Stories 9R.17 (`cat-022`, with 9.5a) and 9R.15 (`cat-063`, with C-Switch); F15-3 already covers the stray-scene gap.
