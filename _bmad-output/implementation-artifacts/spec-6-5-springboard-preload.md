---
title: 'Springboard preload and playable launch lessons'
type: 'feature'
created: '2026-10-10'
status: 'in-review'
route: 'dispatch'
baseline_commit: '781e0c81b838abe13fea8bc72d10e60f0ee67dca'
review_loop_iteration: 0
context:
  - '{project-root}/docs/springboard-elastic-contract.md'
  - '{project-root}/docs/planning/elements/CAT-062-spring.md'
  - '{project-root}/docs/planning/elements/EL-194-springboard.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Expose finite initial compression through the existing Springboard and solve the named launch lessons through real Workshop controls. Preserve the original passive `spring_forward` route; additionally prove a precharged construction and its matched uncharged failure in that level, plus the existing precharged `ready_to_rebound` fixture. “Uncharged fails” does not prohibit the preserved falling-ball rebound route.

## Boundaries & Constraints

Always: CAT-062, EL-194, TODO-461, Story6.5/CAT-062b and the elastic contract. InitialCompression is f32, 0–0.20m/default0; unchanged stiffness/damping, plate/material, .14m rest frame and travel[-.25,0]. Initial potential is physical ½kq², released by the generic constraint on Run, with zero initial velocity/impulses. At the preserved ready_to_rebound fixture, actual initial energy is8±1e−5J on every replay. Physical plate, collider and coil share authored compression in Build, Run and Reset.

Use one f32 canonical position/rotation path, including necessary shared consumers: existing Half pose encoding fails this energy bound. Preserve unrelated declarations and difficulty evidence. Reject old schema versions atomically; no compatibility reader, fake energy ledger, compensated rest anchor, new latch or element-specific solver. Legacy Spring physics was already purged; no LatchedSpringStore file is fabricated or restored. Direct Springboard ActivationOut remains an unresolved proposal, not a new port in this slice. Composite/signal campaign and detailed device/performance qualification retain their existing later ownership; this is scoped playable acceptance, not full EL-194 completion.

## I/O & Edge-Case Matrix

| Given | When | Then |
| --- | --- | --- |
| Valid preload/default/endpoints | Configure, compile, Run | Exact f32 settings; physical compression supplies finite work through contact |
| Resting payload, matched zero charge, missed or empty board | Run | Charged payload rises; controls receive no free launch; empty plate may move |
| Original spring_forward fixtures | Author passive and distinct charged routes | Both intended routes work; matched uncharged/180° controls fail |
| ready_to_rebound | Place Receiver and Run/replay | Capture succeeds; missing Receiver fails; each start restores8J |
| Invalid/NaN/out-of-range preload, strength, malformed/old bytes | Configure/load/admit | Visible rejection preserves accepted construction; valid retry succeeds |
| Edited/loaded/undone precompression | Reset/Save/Load/repeat | Exact authored poses/settings, coil and zero warm starts restored |

</frozen-after-approval>

## Code Map

- `engine/gpu/WorkshopSpringboard.cs`, `engine/SpringboardResource.cs`, catalogue and `ui/WorkshopConfiguration.cs`: third typed setting and actual editor.
- `WorkshopPhysicsCompiler.cs`, `parts/SpringboardPart.cs`, `engine/MachinePart.cs`, `MachineWorld.Gpu.cs`: initial plate offset only; update authored secondary reset pose and coil.
- `CanonicalBody.cs`, `WorkshopConstruction.cs`, `PhysicsDeclarations.cs`, `RigidMassProperties.cs`: shared f32 pose and validated normalization boundaries.
- `PhysicsGpuAbi*.cs`, `PhysicsBodyWire.cs`, `PhysicsMotionRead.cs`, `WorkshopWire.cs`, `WorkshopSaveCodec.cs`, read/history/presentation callers and Simulation `Program.cs`/`worker.js`: replace pose lanes and update every dependent offset.
- Typed `WorkshopPuzzle*`, inventory/picker/precision callers and `content/puzzles.json`: preserve both source fixtures and assistance data; expose playable lessons.
- Native affected tests, `tools/workshop-*.test.mjs`, browser decoders and `tools/e2e/cat-062b.test.ts`: new controls and shared-format regression.

## Tasks & Acceptance

- [x] Implement shared f32 pose closure and forward schemas; test every changed mapping, invalid/nonfinite/padding/old-version rejection and exact round trips.
- [x] Implement preload across resource/configuration/save/compiler/secondary bindings; prove actual compiled q/energy and exact lifecycle before UI qualification.
- [x] Expose both preserved lessons; construct through ordinary placement/rotation/configuration controls, never setters, imported solutions or numeric placement menus.
- [x] Independently verify positive/control/boundary matrix, source preservation, all affected existing behavior, Production/Playtest builds and exact candidate.
- [ ] Obtain SnapshotApproval, normal commit/push, CI/deployed identity and actual production Chrome terminal Pass before dependent work.

Given shared pose records, when existing catalogue scenes run, then collision, activation, animation, Save/Load and ordinary lifecycle remain correct. Given tilted/rotated preloaded boards, when physical contact occurs, then release follows the local axis with no artificial energy gain. Keep unchanged6.4 numerical controls and all original acceptance; never loosen energy bounds to hide encoding error.

## Implementation Notes

Shared f32 pose, preload authoring/compiler/rest bindings and both source-preserved lessons are implemented. Generic relative-origin anchor arithmetic preserves the original8±1e−5J bound without rest-anchor compensation. Original compilation-passing Playtest74122/Production51207 artifacts remain immutable but were superseded by R4. Corrected warning-as-error Playtest60136/44dca6 and Production14265/1b5e7f pass; corrected immutable artifacts are `.anvil/story-6-5-corrected-artifacts/{playtest,production}`, preview8079. Full independent current native/Node/Chrome regression passed (796 native, 97 Node, 10 WASM, 75 Chrome E2E). Owner decision on 13 Oct 2026 accepted physically valid capture at tick 192 for the −0.5 m Receiver control in SpringForward, closing R5.

| Criterion / actual impact | Completed development and independent proof | Remaining final check |
| --- | --- | --- |
| Shared f32 pose, forward layouts, malformed admission and legacy rejection | Independent full native785/785 (28631/f76285/5b05e0), reviewed R1–R3; owner new preload settings22/22 (81152/929866) and actual C# Reset preload0/.2 two-case byte restoration (30278) | Final current native and Node suites |
| Generic relative-anchor physics, batches, stops, rotations, contact energy | Independent matching-WASM8/8 (96081/18903b); new finite preload upright/−30°1/1 (73969/e2431e), actual initial7.99999547/7.99999189J, original1e−5 bound | Bind matching final artifacts; raw fixtures are not C# admission or Reset |
| Charged, exact-pose zero and world-Y180 wrong-heading routes; Ready source fixture | Formal four-case actual Chrome87085: repeated outcomes1/0/0/1, source/configuration/clearance checked, exact body Reset/SaveLoad, no errors | Independent frozen candidate full Chrome |
| Passive source route and missing Receiver | Actual Chrome54526 two-case Pass with repeat outcomes1/0. Earlier passive Balanced save records precision.449951171875/guide6.6015625/preload0; formal route proves Precise | Retain source/difficulty applicability |
| Receiver delete/recreate goal ownership | Actual Chrome86589 one-case Pass: owner3→4, new sensor4294967622, SaveLoad/capture/Reset8J | Independent replay |
| Compressed plate/coil visual restoration | Actual Chrome13113 two-case Pass; zero crop pixels with RGB channel difference>2, alongside byte-exact physical poses and original8J bound | Independent replay; no byte-identical PNG claim |
| Editor, invalid text, endpoints, valid retry, Undo and deletion | Actual Chrome30674 three-case Pass; expanded feedback/retry20152 one-case Pass, visible rejection screenshot and two Undo restorations | Independent current Free Workshop cases |
| Test/tool boundary | Current E2E TypeScript noEmit Pass6ec4ec with installed Node types/ES2023; primitive clicks avoid extending inherited string-domain APIs | Full shared gameplay regression includes all existing suites |

Commands and original raw failures/results remain in `.anvil/story-6-5-*`; the review record identifies independent runs. Earlier full native774/785 failures were stale format fixtures, corrected before independent785. First Playtest1447 failed at the diagnostic trace enum caller; matching corrected43112 passed. Initial formal56589 failed before Run because its test decoder used cell/16+local; corrected (cell+local)/16 passed87085. Failed geometry recipes, including invalid upside-down base overlap, remain retained; accepted wrong-direction control uses actual world-Y180 UI turns and positive collider clearance. Initial TypeScript invocations lacked Node types/current ES library; the corrected invocation passes without source suppression.

Process deviation retained: initial Simulation build35671/195018 began without a separate generated-output Anvil gate. Source edits were gated; this does not retroactively validate that build. Subsequent writes/builds use separate gate, inspected result and execution calls; acceptance requires the later matching gated artifacts.

The first frozen snapshot and both compilation-passing bundles remain retained as a failed candidate after R4: orientation qz was read from threshold byte32 instead of quaternion byte24. Corrected source passes six actual-worker controls independently (raw454038); original red/green logs remain `.anvil/story-6-5-orientation-z-{red,green}.log`. Corrected builds now pass; full independent regression remains pending.

Source fact13 physical placement nudging remains unsupported under the current playable-mode authority in `docs/planning/invest/vertical-delivery.md`; later shared prescribed-motion work owns it. Preserve the broader tilt-assistance acceptance without substituting Receiver guidance. Fact18/R5: owner decision on 13 Oct 2026 accepted the physically valid capture at tick 192 for the −0.5 m control in SpringForward alongside the +0.3 m capture at tick 238. 10/10 real WASM tests pass.

## Spec Change Log

## Review Triage Log

| Finding | Disposition | Evidence / remaining scope |
| --- | --- | --- |
| R1 committed rotations admitted nonunit quaternions | Fixed common canonical admission; q/−q valid, gross nonunit rejected in body, candidate and motion paths | Red3 boundaries, owner64 and independent74 native pass; final shared browser regression remains owed |
| R2 contact-trigger immutable prefix accidentally expanded with orientation sensor | Fixed unchanged trigger prefix32; orientation retains40 | Actual candidate first/repeated/no-event controls in independent74 pass |
| R5 preserved SpringForward forgiving Receiver miss boundary | Closed: owner decision on 13 Oct 2026 accepted current physically valid capture at −.5x on tick192; +.3x captures238 as required | 10/10 real WASM tests pass including both compiled SpringForward offsets |
| R4 orientation qz consumed threshold bytes | Corrected32 to24; stationary nonzero Z/both signs, crossing and sticky-event controls | Original candidate failed; independent six-control Pass; corrected artifacts/regression pending |
| R3 Battery Enabled overlapped f32 output power | Fixed current instance byte124; exact non-Half power and both enable states plus undefined-byte rejection | Independent74 pass |
| Worker occurrence base retained old offset | Fixed current16704; ordinary contact controls now pass | Node84 and matching actual WASM controls pass; final full browser regression pending |
| PLAYTEST trace caller retained retired enum name | Fixed schema4 caller; no compatibility alias | Original publish1447 failure retained; corrected43112 and final74122/51207 builds passed |

## Design Notes

Normalize rotations at authored-input/computed-rotation boundaries; canonical decode validates and preserves bits. No parallel physical quaternion type. Mass/extents/material/timing lanes remain explicitly deferred. Position and orientation migration is the necessary dependency closure of preload, not a separate platform.

Forward packing (bytes): canonical body80 local56/velocity68; construction instance160 quaternion96–112, existing kind fields shifted+8, spring k112/c116/preload120/tail124 zero. Physics body160 local32/rotation48/velocity64/angular76/mass88/drag90/gravity92/COM98/principal104/inertias120,128,136/collider144. Collider96 translation24/rotation32/radius48/extents50. Body read80 local20/rotation32/COM48/velocity56/angular68. Motion144 local48/drag60/rotation64/velocity80/angular92/gravity104/acceleration112/angular120/bounded128/flag136. Orientation sensor64 quaternion16/threshold32/state40/ordinal44/phase48. Keep existing f32 pose-ring and presentation layouts; update input decoding. Sensor128 pose24/min48/max56/speed64/dwell66; immutable prefix72, phase72/start76/startPhase80/count84/event88/eventPhase92. Guide64 pose16/min40/max48/acceleration56/supportHeight58/supportMargin60. These are all three shared Pose callers including Collider; update their worker readers, residence state and immutable-range checks together. Explicitly validate all gaps. Versions: canonical3, physics12, Workshop20, save13.

Do not start the matched zero-preload control with an overlapping payload. Changing compression raises the plate; choose valid clearance through real construction and prove the route rather than infer capture from geometry.

## Verification

Run affected native tests with warnings as errors during fixes, then full current native/Node suites for shared pose impact. Build both diagnostic and Production bundles. Execute actual-WASM energy/contact controls against matching bundles, including compiled ready_to_rebound and repeated Reset. Independently run the existing Chrome suites plus new preload/lesson cases serially through the available Playwright connector. Retain failures, meaningful controls, complete scope matrix, exact candidate/build identities and production evidence; reuse unchanged proof only with reviewer-established applicability.
