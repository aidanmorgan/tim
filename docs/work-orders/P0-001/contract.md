# P0-001 — untouched engine baseline

Baseline reconciliation only. Coordinator-reviewed scoped verdict: Pass; publication pending. Runtime correctness, worker architecture and browser qualification remain incomplete.

Coordinator review independently verified all 916 selected runtime hashes, served/local production bundle equality, retained failure accounting and the unchanged-evidence reuse boundary. This accepts baseline capture only and asserts no runtime qualification.

## Boundary and provenance

Allowed changes are this contract and `docs/verification/P0-001/*`. No implementation, caller, authored content, tool, test, palette or browser state changes are required. The coordinator owns Chrome input; this row performs no browser actions or build/export. Immutable evidence is reused, not replayed without a stale dependency.

Current HEAD is `605ca26996326208e8ffb0896e2aa64be35bd7a5`. S001 evidence was published in `a470f84d6c77a874858dbdb616f647271b5a61a7`; its tested HEAD was `0c3cf53428d2b0eb7b4b9b5e8764bab512be3d7b` plus the uncommitted migration. [Identity](../../verification/P0-001/identity.json) records current tracked dirty-diff SHA and artifact hashes. All 916 selected runtime/test/content/resource entries and 20 S001 artifacts match exactly. This selected inventory excludes coverage tooling and many documents; it does not establish a clean or reproducible published runtime. The four interrupted coverage-tool changes belong to P0-002 and are preserved.

Relevant unchanged symbols are `PhysicsWorld.CopyCompliantContacts`, `CommittedPoseBuffer.RegisterCompliantContacts/StageCompliantContacts/Publish/Discard`, `SceneCompliantSurface`, `MachineWorld` Run binding and `Workshop` hint/Run/Reset handlers. Existing boundary types include `CompliantContactPhase`, `PoseSample`, `PhysicsMotionType`, `PhysicsBodyId` and `CompliantContactKey`. This documentation adds no domain selector or runtime API. Repository-wide enum and ownership compliance remains unverified.

## Required-now criteria

Every row below inherits the exact source/artifact hashes in identity.json and the production/diagnostic distinction in [S001](../../s001-baseline.md). Contract identity is recorded separately in the verification hash manifest. “Pass” here means the baseline criterion passed, never that a measured failing runtime passed.

| ID | Expected / tolerance | Observed and independent control | Artifact / verdict |
| --- | --- | --- | --- |
| P0-001/A01 | Untouched selected runtime and retained evidence; zero SHA-256 mismatches | Independently rehashed 916 source entries and 20 original artifacts: zero mismatches. Current HEAD/dirty diff recorded; untracked source is identified by the original manifest, not included by git diff. | identity.json; Pass |
| P0-001/A02 | Current served production matches retained local production; exact byte/hash identity | Coordinator read-only Chrome/Playwright fetch returned HTTP 200 for both assets; PCK 1,095,052 bytes and managed WASM 2,310,425 bytes match S001. Visible localhost page, 216 input events, latest requested Reset pointer-up unchanged. This is continuity, not new behavior proof. | chrome-readback.json; Pass |
| P0-001/A03 | Compile affected callers and preserve numerical failures with no relaxed tolerance | Retained Release build: exit 0, zero warnings/errors. Native XML independently reports 76 tests, 66 pass, 10 fail. All five WoundSpring and five Bellows failures retain messages/stacks. | s001-native-build.log, s001-native-baseline.xml/log, s001-native-failures.json; Pass as baseline, runtime fails |
| P0-001/A04 | Typed invalid input rejects atomically; exact prior snapshot after discard | All 13 committed-contact cases pass: undefined phase/sample, foreign/missing/static body, invalid footprint/speed, missing/reordered/duplicate topology, regressing episode, pending-body coherence and lease/discard boundaries. Seven InvalidContact enum cases independently inspect unchanged snapshot after rejection. This is scoped current boundary proof, not future worker codec proof. | CommittedCompliantContactTests.cs and named cases in s001-native-baseline.xml; Pass |
| P0-001/A05 | Actual UI physical positive/control/integration with exact construction Reset/save (zero structural differences) | UI-placed tilted trampoline/ball/basket positive reaches basket; depth-only control misses. Recomputed run==reset is true for receiver, control and save artifacts. No explicit wire connections: physical contact integration only. Saved construction is UI-authored. Interrupted control and initial observer timeout remain failures. | committed-compliant-recipes.json, receiver.json, control.json, save.json; Pass within existing fixture |
| P0-001/A06 | Physical, animation-only and combined browser observations, reproducible actions and retained motion | First principles default ball/basket; Run/Reset (688,836). Stopped physics: 60 hint toggles at (1186,258), 500 ms waits (30 reveals); combined: 30 toggles, 1000 ms waits (15 reveals). Independent adjacent-RAF nearest-rank recomputation matches physical 796 intervals p95/p99 83.6/84.3 ms, combined 838 intervals 83.7/84.1 ms within 0.000001 ms arithmetic rounding. Separate hint and combined WEBM motion exists; native overlap excludes it from timing. | s001-physical-pacing.json, animation-pacing.json, combined-pacing.json, hint-motion.webm, combined-motion.webm; Pass for capture |
| P0-001/A07 | Compare measured costs to unchanged declared budgets; no inference from native/RAF to qualification | Tick p95 positive/control 17.1/10.5 ms exceeds 2.5 ms. Production compliant RAF p95/p99 150.1/159.0 ms exceeds 60 Hz 18/25 ms and 90 Hz 12/16.7 ms. Publication/animation/submission individual p95 0.2/0.1/~0.1 ms cannot be summed or called total CPU/GPU. Smallest physical/combined windows fail pacing and cover less than 30 simulated seconds. | committed-compliant-browser-stages.json, control-stages.json, production-pacing.json and S001; Pass for failure accounting, qualification fails |
| P0-001/A08 | Retain contamination and unknown attribution; no silent valid-run substitution | Unexpected Reset/depth edit/Run invalidated first control. Two later unrequested click pairs at 680288.5/680363.3 and 682790.5/682873.2 ms are outside selected S001 windows. User denied tab input; source unresolved. Fresh control excludes 20 prior-run recorder samples by run identity; stale samples remain retained. | committed-compliant-input-incident.md, s001-inputs.json, control-stages.json; Pass for preservation, defect unresolved |
| P0-001/A09 | Exact current commands, retained successful Release export, honest provenance | Commands below resolve current project/manifest paths. Existing successful production export and served hash retained. No new export required by unchanged selected source. Historical export log lacks invocation text, so current command is not asserted as verbatim historical command. | committed-compliant-browser-production-export.log; Pass with explicit provenance limitation |
| P0-001/A10 | Independent review and individual publication | Row-agent independently checked hashes, XML, timing and restoration; coordinator review passed as recorded above. Commit/push remain pending. | Review Pass; publication Incomplete until coordinator records verified remote commit |

Native failures retained without reinterpretation: release energy expected 51.200000725878958 J versus 51.500738503661267/51.5007385030748 J; power-loss charge drift approximately 7.75e-9 J; constraint RHS dynamic-range rejection; Bellows stroke 0.00041936039924622692 versus maximum 0.0001; two sweep interval-budget and two support-iteration failures. Full names, original tolerances and stacks remain in the hashed failure artifacts. No solver changes or new thresholds are introduced.

## Commands and recipes

Existing native commands (historical logs, unchanged source):

```sh
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Release --no-restore
dotnet CuriousContraptions.tests/bin/Release/net10.0/CuriousContraptions.tests.dll -noColor -class '*BellowsTests' -class '*WoundSpringRuntimeTests' -class '*CommittedCompliantContactTests' -class '*WorkshopAnimationTests' -xml docs/s001-native-baseline.xml
```

The original executions redirected output to the matching S001 logs. Do not overwrite those historical files on a later run. Current resolved exports, not executed by this row:

```sh
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=true --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

Exact UI actions and placed values are in the hashed recipes/receiver/control/save records. Nominal recipe positions differ slightly from placed values; use the actual recorded construction. Physics precision 0.45, ball mass 1, radius 0.34, trampoline tension 180 and damping ratio 0.12; control changes ball depth from 0 to 1.996268 only. S002 documents First principles 3 bodies/8 convex children and compliant fixture 4/13. These are current finite baseline cases; exhaustive modes and future workload parameters belong to P0-002/P0-003.

## Standing requirements and later ownership

No standing requirement is waived. Required-now for all rows is truthful preservation and no runtime modification; wider runtime establishment remains open.

| Requirement | Baseline assertion / current evidence | Enforcing successor |
| --- | --- | --- |
| REQ-01 | No actual independent workers qualified; no claim from hint/physics separation | P0-004–006 contracts, P0-032 concurrency, P0-035 |
| REQ-02 | 18/25 ms and 12/16.7 ms frame targets unchanged; observed failure A07; RAF is not distinct presentation | P0-003 manifest, P0-034 |
| REQ-03 | Fixed 120 Hz/four substeps and independent animation are targets; no full cadence/replay or 0.99–1.01 real-time proof | P0-005–006, P0-032 |
| REQ-04 | 2.5 ms tick fails; 2 ms animation, 1 ms bridge, aggregate CPU 10/7 ms and GPU 8/6 ms unqualified | P0-003, P0-033–034 |
| REQ-05 | Input attribution and stale samples unresolved; freshness 33.3/50 ms and feedback 100 ms unqualified | S007-D/S007 proof-blocking controls, P0-005–006, P0-032 |
| REQ-06 | Zero paths introduced/replaced; historical artifacts retained. Full retired-path audit remains open | CLEAN-CORE/ANIMATION/PROTOCOL/BUILD/CONTEXT/ENFORCE |
| REQ-07 | No executable values added; current scoped invalid enums reject A04, no repository-wide compliance claim | P0-002 inventory, P0-016 codecs and each affected implementation |
| REQ-08 | Scoped publication discard and physical control A04/A05; current solver correctness fails A03 | CAT-010-D/CAT-071-D consumer contracts and their extraction/proof rows; P0-009, P0-031–032 |
| REQ-09 | Exact one-fixture Reset/save A05; repeated lifecycle, generation and invalid-load proof incomplete | P0-020–021, P0-030–032 |
| REQ-10 | Native zero-allocation subcases supplement only; browser memory/copy/startup/20-cycle observations missing | P0-003, P0-034 |
| REQ-11 | Chrome UI fixture evidence retained with hashes and contaminated evidence excluded; all catalogue/modes not qualified | P0-030–031 and named consumer/fixture rows |
| REQ-12 | No product delivery; baseline Pass cannot unlock it | P0-002, CHECK-AGGREGATE, P0-035 |
| REQ-13 | No code/art/palette/property edits; architecture and reduced-motion review remain open | P0-004, P0-022–028 and visual qualification |

MacBook Pro M4 Max 64 GB/macOS 26.5.2/Chrome 154 at 1440×900 DPR1 is the observed surface. Actual reference integrated-GPU hardware, Pixel 8 Pro and other required device tiers, controlled power/thermal sessions, distinct presentation and valid GPU service measurements remain prerequisites for their named device rows and P0-034. Access to those physical devices and the required observability permits retry; neither emulation nor elapsed time passes those gates.

This contract consumes S001/S002 evidence without duplicating their implementation credit. Next eligible row after coordinator acceptance/publication is P0-002.
