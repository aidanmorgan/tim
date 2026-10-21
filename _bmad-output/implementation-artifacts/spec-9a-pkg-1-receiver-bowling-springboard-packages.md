---
title: 'Story PKG-1: Receiver, Bowling Ball and Springboard Element Packages (9A.5 + 9A.6 + 9A.12)'
type: 'feature'
created: '2026-10-21'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-pkg-1-receiver-bowling-ball-and-springboard-element-packages-9a5--9a6--9a12'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9a-4-lossless-bundle-build-gate.md'
---

## Intent

PKG-1 merges Stories 9A.5 (Receiver, `basket`), 9A.6 (Bowling ball, `bowling`) and 9A.12 (Springboard, `spring`). Each element gets its own `elements/<Name>/` package, declared only there, and enters the generated bundle with today's runtime values. The 9A.4 parity guard proves the values equal, so nothing the player sees changes. Audit finding A15 shrinks from nine unpackaged elements to six.

## Implementation Notes

Amelia-17, 21 Oct 2026. Base: main `980e222d` (with 9A.1) merged with 9A.4 `b8910e92` (merge `193cdf37`). Candidate: `ce460ddc`. Evidence: [evidence-9a-pkg-1/](evidence-9a-pkg-1/).

**Deletes:**
- `parts/scenes/basket.tscn`, `bowling.tscn` and `springboard.tscn`, moved (100% `git mv` renames, not copies) into `elements/Receiver/`, `elements/BowlingBall/` and `elements/Springboard/`. The three `.tres` files now point at `res://elements/<Name>/<scene>.tscn`. Story 9A.14 deletes the `.tres` files.
- `basket`, `bowling` and `spring` from the parity guard's unpackaged set, which goes from 11 ids to 8 (`TheCatalogueIdsWithoutAPackageAreExactlyTheEightListed`).

`rg` proof: [deletion-rg-proof.txt](evidence-9a-pkg-1/deletion-rg-proof.txt). There are 0 production, test or tool hits. Two historical records, a frozen coverage capture and a commit-pinned CAT-004 row, are left unchanged.

**What was built:**
- **Packages.**
  - `elements/Receiver`: five wall boxes and six typed parameters, `capture_margin`, `capture_speed_limit`, `capture_dwell`, `capture_participation` (a Choice over `SensorParticipation`), `force_support_margin` and `force_maximum_acceleration`. Each has the `ReceiverCaptureSettings.Free` / `ReceiverForceRegion.Free` default and the range its `Validate` admits.
  - `elements/BowlingBall`: a 0.28 m sphere of 4 kg, with bounce 0.14, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 and drag 0.04.
  - `elements/Springboard`: the base slab, plus `stiffness` (N/m, 120 to 1200, default 400), `damping` (N·s/m, 0 to 8, default 0.2) and `initial_compression` (m, 0 to 0.2, default 0), plus one slider.
  - Each package references only `CuriousContraptions.Abstractions` and `.Mechanical`.
- **The slider contract.** `CuriousContraptions.Abstractions.Mechanical` gains `ISliderElement : IMechanicalElement`, with `PrismaticSliderDescriptor`, `SliderFrame` and `SliderBodyDescriptor`. The descriptor holds:
  - the base and moving frames (local Y is the slide axis);
  - the limits;
  - the parameter keys for stiffness, damping and compression;
  - whether the bodies collide;
  - the moving body (mass, gravity, drag and colliders).
  
  It is opt-in, so the other mechanical packages are untouched. The bundle carries it as `MechanicalPayload.Sliders`, which is empty for a non-slider element. The validator checks finite frames, that the limits bracket rest, a positive finite moving mass and a moving collider. It also rejects a key that names no declared Real parameter, as the new `BUNDLE007 UnresolvedReference`.
- **`ParameterUnit`** gains `MetrePerSecondSquared`, `NewtonPerMetre` and `NewtonSecondPerMetre`.
- **Parity (`BundleCatalogueParityTests`).**
  - New `Placed` and `Parameters` arms.
  - New: `EveryMechanicalPackageDeclaresTheCompiledBodyMotionMassDragAndGravity` (all mechanical packages; no existing package diverged).
  - New: `EveryBallPackageDeclaresItsCatalogueBallMaterial`, against both `BallMaterial.For` and the `.tres` `BallMaterialResource` bits.
  - New: `EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody`. It compiles the part with every parameter at its declared maximum, so each key must bind its own value, including the plate's start position under compression.
- **New tests in `tools/BundleElements.Tests`.**
  - `PackageReferenceTests`: every package references exactly the general Abstractions plus the domain Abstractions its declaration implements, and has no NuGet package.
  - Slider validator and round-trip tests.
- **Pinned lists:**
  - `CuriousContraptions.slnx`;
  - `CSharpStaticGateTests.TrimAnalyzedProjects`;
  - `ImpactRules.ElementPackages` (the E2E runner);
  - `SelectionTests`: the moved springboard scene row is now `parts/scenes/clock.tscn` → clock plus `elements/Springboard/springboard.tscn` → spring.
- **The isolation authority.** The A15 row now lists the six remaining ids, owned by PKG-2 and PKG-3.

**Test-first and mutants:**
- Red: [red-bundle-tests.txt](evidence-9a-pkg-1/red-bundle-tests.txt) (5 failing) and [red-parity-tests.txt](evidence-9a-pkg-1/red-parity-tests.txt) (3 failing).
- Mutants, each killed by the named test ([mutants-parity.txt](evidence-9a-pkg-1/mutants-parity.txt), [mutants-bundle.txt](evidence-9a-pkg-1/mutants-bundle.txt)):

| AC | Mutant | Killed by |
|---|---|---|
| 9A.5 parameters | Receiver `capture_dwell` default 0.35 → 0.36 | `…ParameterDefaultsAndBounds` |
| 9A.5 colliders | Receiver front-lip half-height 0.3 → 0.35 | `…FootprintCollidersAndMaterial` |
| 9A.5 references | Receiver references `elements/Ball` (a sibling) | `PackageReferenceTests(Receiver)` |
| 9A.6 material | Bowling rolling resistance 0.03 → 0.035 | `…CatalogueBallMaterial`, `…CollidersAndMaterial` |
| 9A.6 body | Bowling drag 0.04 → 0.05 | `…BodyMotionMassDragAndGravity`, `…CatalogueBallMaterial` |
| 9A.6 scene | Bowling `ScenePath` back to `parts/scenes/` | `…CatalogueMetadataAndCategory` |
| 9A.12 slider binding | stiffness and damping keys swapped | `…PrismaticJointAndMovingBody` |
| 9A.12 slider limits | lower limit −0.25 → −0.2 | `…PrismaticJointAndMovingBody` |
| 9A.12 bundle | `BundleSchema.Entry` drops the sliders | `ASliderRoundTripsBitForBit`, round trip of `Springboard` |

**Tier 1 on the candidate:**
- `dotnet build CuriousContraptions.slnx`: 0 warnings.
- `dotnet test CuriousContraptions.slnx`: 100%.
- The pre-commit C# gate passed.
- Ownership `--oracles .`: exit 0, 22/22 attacks rejected. The first two attempts failed for environmental reasons (no Release build, then the web host not restored); see [tier1-contracts.txt](evidence-9a-pkg-1/tier1-contracts.txt).
- WireContract: 108 oracles, 0 failures.
- LifecycleContract: 510, 0 failures.
- Node harnesses: 88/88.
- `anvil check` on the changed files: no warnings ([anvil-check.txt](evidence-9a-pkg-1/anvil-check.txt)).

**Chrome (Tier 2), see [E2E](#e2e-chrome).**

## Spec Change Log

Amelia-17, 21 Oct 2026. These decisions apply the owner's delegation to decide small gaps; no AC is weakened.

- **CL1. The slider is declared data through an opt-in contract (9A.12 "its slider declaration").** The Abstractions had no way to declare a second body or a prismatic joint; 9A.4 CL4 hit the same gap for the gate blade.
  - `ISliderElement` adds both, and the bundle carries them.
  - The gate's blade stays with Story 9A.16, as 9A.4 CL4 filed it. The new contract fits it, but moving the gate is outside PKG-1.
  - The stiffness, damping and compression values live in the parameters, and the slider names them by key. They are not duplicated into the slider.
- **CL2. `CosmeticCurves.Receiver` is not declared in the package (9A.5 Given).**
  - It is a presentation binding: `AnimationFeedbackSource.Capture` with `SmoothStep`, 0.5 s.
  - The Abstractions have no presentation-binding contract. Story 9A.25a designs the committed-state bindings and moves every `CosmeticCurves.*` arm out of the engine (isolation authority, row A4/A10).
  - Declaring a one-off binding type here would pre-empt that design, so the curve stays in the engine until 9A.25a.
  - The other Receiver values in the Given are declared and proven equal: the `.tres` metadata, the colliders, `ReceiverCaptureSettings` and `ReceiverForceRegion`.
- **CL3. Stale comparators in the Givens.**
  - `WorkbenchFootprint.Of` was deleted by Story 9R.5b. The footprint is compared with a one-instance compile (9A.4 CL2).
  - `basket`, `bowling` and `spring` have no `WorkshopCanonicalColliders.For` arm. Their colliders are compared with the compiled scene (9A.4 CL3).
  - The ports are compared with `WorkshopPorts.For`, and all three are empty.
- **CL4. The force region's parameters are the two arguments of `ReceiverForceRegion.Create`.** These are the support margin (0 to 1 m) and the maximum acceleration (0 to 12 m/s²). The region's box and support height are fixed geometry in `Create`, and the runtime has no authored value for them.
- **CL5. Buoyancy.** `BallMaterial` carries a buoyancy, and it is 0 for both balls. The Abstractions have no buoyancy slot for a ball. The ball-material parity test asserts that the runtime buoyancy is 0, so a buoyant ball would fail until a slot exists.
- **CL6. Lanes.** Receiver values are compared at the runtime's binary16 precision (9A.4 D2). Springboard values are f32 at runtime and are compared bit for bit.
- **CL7. Events.** The Receiver and the Springboard declare no events. They have no runtime event producer (D1, owned by 9A.22). The Bowling ball declares `Impact`, as the Basketball does.
- **CL8. Verification tier.** The ACs of 9A.5, 9A.6 and 9A.12 each say "every `tools/e2e/*.test.ts` suite in one serial run". The coordinator's 21 Oct tier rules and PKG-1's own AC ("the union of the three stories' named suites passes once") supersede that.
  - `run --diff b8910e92` refuses with `NoPrimarySuite` for `basket`, which has no Primary suite ([raw](evidence-9a-pkg-1/runner-run-diff-b8910e92.txt)).
  - The diff touches no solver, clock, worker, wire or compiler code. Its Full triggers are only shared non-engine files: the Abstractions contract records, `tools/BundleElements`, `tools/E2eRunner`, and the new `.csproj` and `.slnx` entries.
  - The changed Abstractions symbols reach no runtime code. `rg` for `ParameterUnit`, `ISliderElement`, `PrismaticSliderDescriptor`, `SliderFrame` and `SliderBodyDescriptor` over the Simulation and Animation workers, `engine`, `ui` and the web host finds 0 hits ([worker-reach-rg.txt](evidence-9a-pkg-1/worker-reach-rg.txt)).
  - So the affected suites were run by name, through `run --changed` (see E2E).

## E2E (Chrome)

The selection is [tier2-suites.txt](evidence-9a-pkg-1/tier2-suites.txt), built as the union of four sources:
- the AC-named suites: cat-005, cat-014, cat-015a, cat-051, cat-052, cat-062 and cat-062b;
- every suite whose `Places` contains the Receiver, Bowling ball or Springboard (cat-014, anim-1c, cat-030, cat-062, cat-062b and cat-052-*);
- every suite whose declared levels pre-place one of them (41 levels pre-place a Receiver):
  - `first_principles` places a Receiver: anim-1a, anim-1c, engine-core-2a3, engine-core-2b4 and engine-core-2c;
  - the inline `new LevelId(…)` declarations that pre-place a Receiver. The first selection missed these, and they were run separately on the head (see below):
    - cat-015a-sidekick (`bumper_sidekick`);
    - cat-015b (`bumper_depth`, `wall_and_bumper`);
    - cat-048b and tube-snapping (`clear_pipe`);
    - cat-049 (`gentle_bend`);
    - cat-050 (`quarter_bend`);
    - cat-050-joined (`joined_pipe`);
  - `domino_effect` places a bowling ball: clock-master and cat-023b;
  - `spring_forward` and `ready_to_rebound`: cat-062b;
- palette-placement (every palette part) and the smoke pair, engine-core-2a1 and engine-core-2b1.

The runner's own selection for the two parts that have Primary suites is a subset of this set ([input](evidence-9a-pkg-1/select-bowling-spring.txt), [output](evidence-9a-pkg-1/select-bowling-spring-output.txt)).

**Result on `ce460ddc`: verdict Pass.**
- The command was `run --changed tier2-suites.txt` ([summary](evidence-9a-pkg-1/e2e-tier2-ce460ddc/summary.txt), [log](evidence-9a-pkg-1/e2e-tier2-ce460ddc.log)).
- 20 suites passed: 22 files, 129/129 tests, 0 failures.
- The runner reran clock-master and anim-1c because their first runs were Contended by load. Both runs of each passed, and the reruns were Clean.
- palette-placement placed every palette part from the published bundle, 25/25 tests, including the Receiver, Bowling ball and Springboard from their moved scenes.
- Identities:
  - sourceHead `ce460ddc`;
  - bundleIdentity `ceb64963dc8c…f3eb`, the same at start and end;
  - Chrome 155.0.8059.86.
- Of these suites, cat-030, cat-051 and cat-052 use the 9R.1 snapshot helper. cat-014, cat-062 and cat-062b do not yet; their own 9R suite stories own that rewrite.

**The PKG-1 suite (`tools/e2e/pkg-1.test.ts`), added after the run above.** It uses only actual UI controls and the 9R.1 snapshot helper:
1. Place a lifted Bowling ball, a Springboard under it and a Receiver from the palette. The save holds `bowling`, `spring` and `basket`.
2. Run twice. Each Run advances (the paused committed read is past tick 0 and differs from S0), and each Reset matches S0 exactly (`expectResetMatches`).
3. Save, reload and Load. The loaded bench matches S0 (`assertLoadRestores`), then Runs and Resets exactly.

Supporting changes:
- It is registered in `SuiteCatalogue` as `Pkg1`, SerialAlone under the new-suite rule, with no Primary. It places BowlingBall, Receiver and Springboard, and covers the Palette, BenchPlacement and Gizmo areas.
- The README timing table records it.
- Two `SelectionTests` lists gain `pkg-1`: the console-module importers and the palette and bench area.

Results:
- Pass, 3/3, with the smoke pair 6/6 ([summary](evidence-9a-pkg-1/e2e-pkg-1-try1/summary.txt)).
- Mutant: pointing `basket.tres` back at the deleted `parts/scenes/basket.tscn` fails the run ([summary](evidence-9a-pkg-1/e2e-pkg-1-mutant-stale-scene/summary.txt)). The catalogue cannot load the missing scene, so even the smoke pair cancels. This is the player-visible failure the move must avoid.
- Rerun on the committed head `ba4678d9`: Pass, pkg-1 3/3, smoke pair 6/6 ([summary](evidence-9a-pkg-1/e2e-pkg-1-ba4678d9/summary.txt)).
- Tier 1 on `ba4678d9`: build 0 warnings; `dotnet test CuriousContraptions.slnx` 2210/2210 ([log](evidence-9a-pkg-1/tier1-build-ba4678d9.txt)).

**The Receiver level suites, on `2f8b0654`.** The seven inline-level suites above all passed: 10 files, 60/60 tests, including tube-snapping, which was rerun after a Contended first run ([summary](evidence-9a-pkg-1/e2e-receiver-levels-2f8b0654/summary.txt)).

**The merge of main `12658442` (9A.4 round 2, `a8c0cf47`).**
- One conflict, in `BundleValidator`. Main's rule is that any package with a body has a collider, whatever its domains. It replaces the old mechanical-only check, and PKG-1's slider checks sit beside it.
- The moving-body BUNDLE004 message now uses main's wording, "a body declares no collider".
- Main's changes outside the records touch only `tools/BundleElements*` and the parity test, so no runtime input changed.
- After the merge, `dotnet test CuriousContraptions.slnx` passes 2216/2216, with build 0 warnings ([log](evidence-9a-pkg-1/tier1-test-after-main-12658442.txt)). Every body in the three packages declares colliders: 5 Receiver walls, 1 ball sphere, and the springboard's base slab and plate.
- pkg-1 and the smoke pair on merge `9cca93c8`: Pass, 9/9 ([summary](evidence-9a-pkg-1/e2e-pkg-1-9cca93c8/summary.txt)).

**Reuse of the 20-suite run.** The diff from `ce460ddc` to `ba4678d9` touches only test, runner and docs files: `tools/e2e/pkg-1.test.ts`, `tools/e2e/README.md`, `tools/E2eRunner/SuiteCatalogue.cs`, `tools/E2eRunner.Tests/SelectionTests.cs` and the `_bmad-output` records. No runtime or bundle input changed, so the 20-suite run on `ce460ddc` still applies.

The runner's bundleIdentity differed across the three publishes of unchanged runtime sources (`ceb64963…`, `589fffef…`, `3839c6e4…`), so the publish is not byte-reproducible. It was already so before PKG-1: `c948fec6` published twice with two identities in evidence-9a-4. This is filed as Story F.15, at the back of the queue and non-blocking.
