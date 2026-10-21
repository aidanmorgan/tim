---
title: 'Story 9A.16: Bundle Is the Sole Collider Source'
type: 'refactor'
created: '2026-10-21'
status: 'in-review'
route: 'dispatch'
integrated_main: 'aa83c6437731c4a6bf2440ee1454d705e412cc7f'
baseline_commit: 'd563d6eb5d20b2e4ec7df7e3d70ae2ab789fd659'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a16-bundle-is-the-sole-collider-source'
---
<frozen-after-approval reason="coordinator approved finite design under owner delegation, 21 October 2026">

## Intent

Placed parts, worker simulation and saved/reloaded copies must use the same bundle-owned collision geometry. Today scene extraction, canonical tables and compiler literals disagree.

Build-time extraction of package scenes produces f32 bundle geometry. Runtime placement, decoding and compilation consume only that bundle. Data-only geometry remains supported when the package has no scene colliders.

## Boundaries & Constraints

Preserve the existing authoritative worker/load compiled rows as the golden baseline. Correct discrepant placement proxies explicitly; do not claim both old geometries remain unchanged. Fold only the necessary pipe/funnel proxy correction from 9R.23 into this story; its remaining gameplay/art proof stays there.

A package has one geometry source across base and moving bodies. Scene body roots assign each collider exactly once; moving subtrees are excluded from base extraction. Mass, material and slider declarations are metadata, not a second geometry source. Reject mixed sources, overlapping ownership, unsupported shapes and bodies with no collider.

Preserve pipe lengths 1–8 m (default 3.6), tube-mouth snapping, secondary body identities, ordinary Run/Reset and Save/Load. Use existing slider capabilities with typed constant-or-parameter values for the gate. No new solver, generic expression platform, family conversion, calibration prerequisite, compatibility path or format version.

## I/O & Edge-Case Matrix

| Scenario | Expected |
|---|---|
| Scene-only package | Extract geometry at build time and bake f32 rows |
| Data-only package | Bake declared collider values unchanged |
| Both sources, missing/non-3D scene, empty body | Reject build naming package; no partial bundle |
| Missing geometry referenced at runtime | Explicit rejection; no fallback |
| Place then Save/Load | Identical admitted geometry and gameplay |
| Pipe length 1, 3.6, 8 | Same bounded bundle evaluator in placement, decode and worker |
| Powered gate | Declared frame and moving blade; existing motion, material and slider behavior |
| Reset | Restore authored state and owned secondary bodies |

</frozen-after-approval>

## Code Map

- `tools/BundleElements/{BundleGenerator,PackageValidator}.cs`: build extraction and source ownership before final validation.
- `CuriousContraptions.Abstractions*`, `CuriousContraptions.Bundle/{BundleSchema,BundleValidator}.cs`: f32 geometry/body ownership, bounded parameter bindings, tube mouths and existing slider payload.
- `elements/*`: one geometry source, corrected proxies and gate blade declaration.
- `engine/{PartRegistry,PartDefinition,MachineWorld.Gpu,TubeMouthSnap}.cs`: consume baked declarations; remove runtime extraction.
- `engine/gpu/{WorkshopPhysicsCompiler,WorkshopConstruction,WorkshopPipe,WorkshopWireFlatBuffers}.cs`, Simulation `Program.cs`: remove geometry copies/fallbacks including warm-up.
- `engine/gpu/WorkshopCanonicalColliders.cs`: delete whole table; move any still-needed neutral value types.
- Existing package, extraction, wire, pipe, gate and gameplay tests: migrate callers to the sole path.

## Tasks & Acceptance

- [x] Capture pre-change compiled golden rows for all 22 package defaults, including gate blade/material/mass/slider, plus pipe lengths 1/3.6/8. Ramp/wall limits are separate runtime boundary checks, not part of this captured golden.
- [x] Move scene extraction to build time; bake typed body geometry, materials and tube mouths; reject matrix errors atomically.
- [x] Replace canonical/compiler collider copies with bundle readers and bounded parameter evaluation; retain unrelated domain logic.
- [x] Declare gate using existing ISliderElement, including fixed mechanical values and scene-owned blade collider.
- [x] Delete `WorkshopCanonicalColliders`, runtime `GltfPhysicsBodyExtractor` callers, compiler collider copies, silent empty paths and all geometry fallbacks; migrate or remove obsolete callers, content and test fixtures so only the bundle path remains.
- [x] Check the replacement against the sole current f32 shared-engine and element-package-isolation design: build-time package geometry, typed declarations and general simulation capabilities; no legacy bridge, per-element solver or competing runtime path.
- [x] Execute real production C# simulation tests for contact behavior, gate blade/slider operation and pipe/tube interactions, with meaningful negative/control and parameter-boundary cases, tick progression, Reset, supported Save/Load and affected shared integrations. Declaration-only/golden/mocked checks supplement, not replace, this primary behavioral proof.
- [ ] Prove golden parity and explicit placement reconciliation, obtain independent implementation review/publication, and retain browser gap-finding cases as pending until all puzzle elements are implemented.

All original Story 9A.16 acceptance clauses remain authoritative. Golden preservation means worker/load authority, with explicit scene reconciliation as approved above. Gate blade half-extents (0.06,0.72,0.72), restitution0.1/friction0.3/threshold0.1, mass1 and existing slider0–1.55/stiffness14/damping4 remain unchanged. No runtime caller of GltfPhysicsBodyExtractor or production reference to WorkshopCanonicalColliders/LatchColliders/CreateLatchColliders remains.

## Verification

Build the affected closure once with zero warnings; use matching outputs for selected C# simulation tests with --no-build --no-restore. Rebuild only when relevant source/generated inputs/dependencies/configuration change. Run other affected native/Node/contracts only where the change requires them. Focused negatives cover dual sources, invalid roots/shapes, missing geometry, wrong ownership and parameter bounds. Golden tests compare admitted rows and identities, not duplicated implementations.

Latest owner override (21 October): real C# simulation unit/integration tests are the primary proof until all puzzle elements are implemented. No Chrome is required for those tests. Then use E2E to find remaining browser/integration gaps; keep browser qualification pending and eventual release behavior intact. No element counter, new test framework or native-fixture programme is a prerequisite.

Pending post-catalogue E2E gap-finding: story-specific place/load geometry parity plus existing cat-048a, cat-048b, cat-049, cat-050, cat-030, cat-037; affected detector, gate, springboard, tube-snapping/joined-pipe and smoke checks. Use actual UI construction and meaningful controls. Broaden only for demonstrated shared/unknown impact; no automatic Full, timing or calibration campaign. Independent SnapshotApproval precedes publication; remote/artifact identity checks remain required, with routine production E2E deferred under the owner cadence.

## Implementation Notes

The existing Wall authored dimensions and physical ABI still use admitted Half lanes; the named endpoint adapter rejects values outside those existing bounds and restores only rounding residue to the declared f32 boundary. This is not a whole-runtime f32 migration; remaining Wall conversion stays with 9A.27.


Existing slider body declaration already supports secondary bodies; its parameter-only values need a bounded constant alternative. Runtime tube-mouth extraction and pipe length-dependent canonical geometry are required deletion closure, not new features.

## Spec Change Log

- 21 October, latest owner instruction: retain explicit cleanup/latest-design/real-C#simulation-proof tasks inside 9A.16; defer E2E until all puzzle elements are implemented. All original behavior ACs remain; only proof cadence changes.

## Review Triage Log

## Coherent review evidence

No known failure remains. Original raw output is retained in [native verification](evidence-9a-16/native-verification.log); the immutable pre-change [golden](../../CuriousContraptions.tests/Fixtures/compiled-geometry-9a16.json) and [capture output](evidence-9a-16/prechange-golden.log) remain unchanged.

| Criterion / impact | Actual proof and result |
|---|---|
| Compiled geometry/material/body/slider identity | PackageRuntimeParityTests: 22 defaults + pipe1/3.6/8 exact golden; included in197/197 and final77/77 |
| Real contact, gate supply/motion, springboard, joined pipes, ticks, ordinary Reset and Save/Load | WorkshopPhysicsEngineTests, PoweredGateTests, WorkshopSpringboardTests, JoinedPipeTests, WorkshopPipeLengthTests, WorkshopWireFlatBuffersTests:197/197,3s, tool chunk63d62e |
| Other affected package behavior, decoding and registry | Remaining selected native classes:449/458 initially; nine causal failures fixed. PartRegistryTests, TubeMouthSnapTests, BundleElementIdTests, WorkshopWallTests plus golden:77/77,2.7694s (session77125). Other449 unchanged results retained by impact |
| Build extraction / schema controls | BundleElements tests130/130,225ms(chunke27f8c); changed extractor and mouth controls24/24,49ms(chunk1a0386), including negative scale, mixed body sources, missing/invalid geometry, quaternion and parameter rejection |
| Worker consumer | Release Simulation build0warnings/0errors,16.05s(chunk380d6c) |
| Deletion | Entire canonical table/runtime extractor removed; compiler per-element collider copies and scene fallback callers deleted; unused old shape resources removed |
| Browser | Pending post-catalogue gap-finding; no current browser qualification claim |

The nine failures were: three tube-mouth basis-orientation regressions, one Wall endpoint precision rejection, one registry global-install side effect, and four forged-input fixtures reaching the wrong boundary. All valid expected outcomes remain unchanged. Renderer/worker JS and clocks are unchanged; no Node/timing campaign is added.

### Reviewer findings resolved
- R1: reject negative/zero authored scales rather than taking absolute values; focused controls pass.
- R2: preserve typed UnknownElement rejection at wire decode; actual forged decode/router controls pass.
- R3: reject zero/nonunit baked mouth quaternions; positive/negative controls pass.
