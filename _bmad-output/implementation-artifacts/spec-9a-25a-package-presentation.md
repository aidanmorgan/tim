---
title: '9A.25a — package-owned live presentation bindings'
type: 'feature'
created: '2026-10-21'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'c1682bc385834202e28a79b569bde66bba51f363'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="Owner-authorised dispatch; independent bounded design precheck complete">

## Intent

Clock, Counter, Powered Gate and Pressure Plate must show live committed simulation state through package-owned presentation declarations. Remove the element-specific presentation implementation, preserving existing motion and one-shot behavior. This implements 9A.25a, all applicable 9A.25 acceptance and 9R.20; their original behavior requirements remain authoritative.

## Boundaries & Constraints

**Always:** Use the sole current package-isolation and f32 architecture. Every binding, including one-shots, belongs to its element package. Animation owns committed-state evaluation. Preserve 9A.16 collider/parameter/tube fields and 9A.17 completed dependencies. Typed enum drivers, channels and axes; float authored values, evaluator samples and serialization throughout the affected animation lane. Reuse existing C# animation, simulation, worker connection and native scene infrastructure.

**Never:** Introduce per-element consumer branches, a legacy bridge, parallel Half/float modes, new test framework, event bus or main-thread interpretation of level state. Retain the existing one-shot relay only until explicitly assigned 9A.25b. No Chrome before the complete catalogue under the owner's current verification decision.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Clock | Committed tick advances oscillator | Pendulum and pulse lamp follow declared independent drivers | No invented pulse from wall clock |
| Counter | Counts below, at and beyond each dot threshold | Correct dots follow committed count | Missing binding fails behavioral proof |
| Gate | Direct/switched supply, disabled/depleted source | Indicator follows exact actuator drive used for the committed physics tick | Never infer any-enabled-battery |
| Plate | Loaded/unloaded contact state | Studs show committed activation | Unloaded control remains neutral |
| Lifecycle | Reset, previous generation read, save/load | Exact authored/rest state; stale read cannot relight cue | Reject invalid identities/order |
| Precision | Endpoint representable in float but not Half | Identical float through evaluator and worker codec | Reject nonfinite/out-of-range values |
| Declaration | Unknown channel, missing node, duplicate writer | Unsupported declarations rejected atomically | No silent omission or fallback |

</frozen-after-approval>

## Code Map

- `CuriousContraptions.Abstractions/`, `elements/*/`, `CuriousContraptions.Bundle/BundleSchema.cs`: declaration-only packages, existing bundle authority and new geometry fields.
- `engine/MachinePart.cs`, `engine/CosmeticBindingSet.cs`, `engine/WorkshopVisualBinding.cs`: currently per-id setup and one curve per part; replace with generic binding instances, including radius/axes/geometry and physical spans.
- `engine/gpu/WorkshopCosmetic.cs`, `engine/gpu/Workshop*.cs`: element-named curve table and instance cosmetic properties; replace consumers and delete obsolete paths.
- `engine/gpu/BrowserWorkshopClient.Cosmetic.cs`, `engine/gpu/BrowserWorkshopClient.Activation.cs`: existing one-shot relay retained; committed Memory/Timer/Electrical interpretation moves to animation ownership.
- `engine/gpu/SupplyRoute.cs` and simulation read/wire consumers: publish typed actuator drive keyed by owner/prismatic from exact tick input used by physics.
- `engine/presentation/AnimationValues.cs`, shared evaluator/sample codecs, `CuriousContraptions.Animation.Worker/Program.cs`: single affected f32 lane and ongoing validated committed snapshots over existing worker connection.
- `CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs`: real RunPhases simulation fixture; `PortableAnimationTests.cs`: shared native batch; existing Clock/Counter/Gate/Plate and NativeSceneFixture tests: real property application.

## Tasks & Acceptance

**Execution:**
- [x] Align the production path with the current package-isolation and f32 design; keep one implementation with no legacy bridge.
- [x] Add typed package presentation declarations and existing bundle validation; populate every existing binding, radius, resize rule and physical visual.
- [x] Publish authoritative committed actuator drive; deliver ongoing typed committed snapshots to animation with session/world generation, epoch/revision and monotonic tick/publication checks.
- [x] Implement per-binding shared evaluation and reset neutralization; remove level-state pumps while preserving existing one-shot relay.
- [x] Migrate the affected AnimationValue/evaluator/sample/wire path to float end to end; update current consumers and tests together.
- [x] Replace MachinePart per-id construction/axes/geometry behavior; delete old lamp methods, element-named CosmeticCurves and Memory source plus obsolete callers/fixtures.
- [x] Use unit tests executing production C# simulation as the primary proof: intended behavior, negative/control and boundary cases, tick progression, Reset and supported save/load. Supplement with integration checks where needed. Prove the four live cues through committed reads, animation and native scene properties; then capture fixed-time samples for all existing bindings.
- [x] Complete affected controls, boundaries, stale/reset/saveLoad, exact float endpoint and missing-binding mutation proof; preserve pending browser cases in this record.
- [ ] Independent review and approved publication with applicable CI, without adding a browser gate.

**Acceptance Criteria:**
- Given an existing element package, when its presentation is constructed, then all behavior comes from validated declarations and no element-id branch is needed in MachinePart.
- Given real simulation progression, when committed reads reach animation, then clock/counter/gate/plate cues show the intended values and negative controls remain neutral.
- Given reset or stale input, when evaluation continues, then previous-world state cannot affect current properties.
- Given all existing curves at fixed times after repairing live cues, when compared with intended behavior, then motion is preserved including State Latch's 0.125-second SmoothStep.
- Given production source, when deletion checks run, then MachinePart has zero `PartId\s*==|UpdateClockLamp|UpdateCounterLamps` hits and removed element-named curves/Memory have no production references.

## Implementation Notes

Design prechecked by retained independent reviewer transport_reviewer. Owner explicitly authorised this cohesive cross-layer scope and routine isolated checkout. No intent gaps or irreversible data changes; publication requires exact SnapshotApproval. 9A.16 terminal implementation/publication Pass is recorded on c1682bc3 (CI 1935/1935, deployment 7228663624, artifact 12353116242); its review-only receipt is carried here.

## Spec Change Log

## Review Triage Log

The retained independent reviewer `/root/transport_reviewer` inspected the actual implementation and forward fixes throughout. Per the owner's current retained-pair instruction and supplied AGENTS.md, this candidate does not launch fresh BMAD lenses; none are claimed. Findings resolved include joint-anchor/relative-speed projection, single f32 signed-property transport, nonzero clock origin, atomic query staging, exact staged follow target, authored material ownership/emission, typed timer mapping, direct generic sizing/current callers, radius prevalidation, exact worker envelopes, retained hidden pulses, visibility reinstall and supported declaration admission. Reviewer rechecked each relevant source correction; final scoped verdict remains pending.

## Design Notes

The current single-curve-per-part model cannot express counter dots and independently driven clock cues. The current animation worker receives only an initial read. Both require bounded production changes inside this slice. Gate drive comes from the exact committed tick input, not a cosmetic reconstruction. The single affected animation lane changes forward to f32 without retaining Half serialization.

## Verification

### Original evidence and scope

Final restored candidate build85393 passed8.36s, focused67421 passed52/52 in786ms, worker52635 passed5.27s. Same-world Pause/Resume/Step/Completed now validates a staged rebind and preserves evaluator handles, visibility, pending pulses and curve progress; only matching Commit changes endpoint ownership. Reset/new world remains fresh. Build2744 passed11.67s, lifecycle22247 passed2/2 in117ms, worker18348 passed5.50s. Final native-project61981 ran matching Debug outputs with `--no-build --no-restore`:2006/2007 passed in32s; one missing-ball-material rejection regressed and was fixed through declared dynamic-sphere capability before Configure mutation. Existing duplicate data-ID discovery warnings in unchanged ConstructionDecodeBoundaryTests are explicit; runner reports zero skipped. Reviewer accepts reuse of the2006 unaffected passes with focused correction proof. Rebind was also corrected to the actual installation ACK/Publication0 protocol after reviewer traced Simulation.Program.Schedule; the fixture now round-trips that endpoint through the real codec. Final correction build14316 passed12.14s; focused91072 passed2/2 in140ms; worker7493 passed5.27s. No live process remains.


Commands below ran in this worktree. Session IDs refer to original tool transcripts, including failed attempts; they are not invented file receipts. Game build command: `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-restore --nologo -v quiet`. Game tests use that project/configuration with `--no-build --no-restore --nologo -v minimal` and the named filters.

- Live four-cue production simulation → committed read → shared evaluator → native properties: session89761, 8/8 in204ms. Temporal/read/lifecycle scope subsequently42148,47/47 in651ms; extended hidden/skipped-frame, Maximum overlap, Reset, authored-owner codec and same-construction visibility scope27838,41/41 in569ms.
- Geometry/default dimensions/spring rest/save/wire:1045,67/67 in585ms. Shared animation/affected native preservation:26420,385/386 in867ms; the remaining typed Delay fixture was corrected and26071 passed1/1 in23ms. Later typed phase and temporal coverage includes its changed production mapping.
- Fixed c168 binding/property oracles after live cues:51663,11/11 in197ms (`PresentationBindingGoldenTests`, `CommittedLatchAndLamp`, `BatterySupplyAndEachCharge`). Includes four retained one-shot packages; actual latch .125-second transitions both directions and linked lamp; Delay progress/phase palette; all four battery charge boundaries and disabled supply; unsupported declaration/missing-node controls. Expected values use current f32 mathematical values rather than old Half rounding.
- Four isolated production `MachinePart` mutants skip binding registration for exactly one driver each. Clock pulse:35105,2/2 fail (124ms); Counter:69665,2/2 fail (125ms); gate:84854,2/2 fail (140ms); plate:51912,3/3 fail (135ms). Builds76170/95427/47318/86659 passed before their respective runs. Clock/counter/plate fail unowned-property application; gate fails missing owned indicator material. Exact original source restored before final positive check.
- Worker Debug/browser-wasm build8992 passed6.36s; later34167 passed5.10s. Command: `dotnet build CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj -c Debug --no-restore --nologo -v quiet`.
- Shipped worker envelope shell: `node --experimental-vm-modules --test tools/workshop-animation-clock.test.mjs`,9/9 in239.915ms. Strict TS compiler with existing external-tool type roots passed; generated snapshot schema test `node --import /Users/aidan/.npm/_npx/e6b261a129b06bcc/node_modules/tsx/dist/loader.mjs --test tools/e2e/unit/run-reset-snapshot.test.ts`,24/24 in162.291ms. No Chrome used.
- Failures retained: initial f32 caller compile errors; first material ownership/revision failures;11 obsolete Half/retired-relay expectations; physical-ID validation compile65573; invalid Read sequence fixture56205; dynamic-body ownership/clock nanosecond endpoint78046; golden fixture revision/neutral-reset25296. Each correction retained the intended production contract, without widening numeric tolerances or bypassing validation.

Ordinary hidden→visible/skipped-frame first flash and Reset are covered. Multi-lamp saturation/whole-world resource qualification CAT017C26–29 remains at its named later gate. Existing nine authored counter lamps are preserved; target-dependent layout is not newly claimed. Browser motion/screenshots and anim1a/b/c, cat017/020/051/052 remain post-catalogue qualification, not a native Pass claim.


Build the affected closure once per candidate/configuration; run affected real C# tests using matching `--no-build --no-restore` outputs. Rebuild only after relevant inputs change. Review independently checks behavioral coverage and stale/reset boundaries. Browser cases anim1a/b/c and cat017/020/051/052 remain pending for post-catalogue gap-finding; no browser qualification claim from native proof.

