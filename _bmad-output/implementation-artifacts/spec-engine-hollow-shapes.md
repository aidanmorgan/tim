---
title: 'Engine: true hollow collider shapes'
type: 'feature'
created: '2026-10-26'
status: 'done'
baseline_revision: 4a272e4bb5dcd36b446769120c6be260f5cc9330
planning_gate: '2026-10-26 coordinator verified actionable tasks, complete source acceptance, explicit dependency ownership and native proof requirements'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/AGENTS.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/docs/testing-guidance.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/docs/gpu-f32-physics.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/docs/hollow-geometry-design.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/docs/architecture/element-package-isolation.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/docs/delivery-workflow.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/DESIGN.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/docs/engine-contracts.md'
  - '/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes/docs/general-engine-design.md'
warnings: ['oversized']
deferred:
  - summary: >-
      Sampled hollow minima may miss narrow intersections
    evidence: |-
      No admitted geometry defeating all candidate generators was established. Eight advancing minimum-wall Tube/Frustum and short-sweep Torus contact/clear fixtures now pass; the Blind Hunter accepted this required-now coverage. Universal completeness of bounded searches remains unverified, not a demonstrated regression; a concrete admitted counterexample would reopen this concern.
    location: >-
      engine/gpu/HollowContactQuery.cs:193
    severity: high (unverified)
  - summary: >-
      Spatial merging may lose distinct minimum-wall constraints
    evidence: |-
      Minimum wall thickness is less than the merge distance, but no necessary generated constraint was shown lost. Minimum-wall inner/outer normal and advancing support/clear controls pass; the Blind Hunter accepted this required-now coverage. A concrete lost necessary constraint would settle the broader unverified concern.
    location: >-
      engine/gpu/HollowContactQuery.cs:211
    severity: high (unverified)
  - summary: >-
      Finite cylinder-axis search may overstate radial support
    evidence: |-
      The exact interior minimum is perpendicular; a reachable erroneous nonperpendicular selected candidate remains unproved. Independently derived tilted cylinder side cases at x=.801 and .799 pass clear/contact motion checks; the Blind Hunter accepted this required-now coverage. A concrete erroneous selected interior candidate would settle the broader concern.
    location: >-
      engine/gpu/HollowContactQuery.cs:424
    severity: high (unverified)
  - summary: >-
      Submillimetre separated mouths may lose a necessary free-rim constraint
    evidence: |-
      The predicate includes a 0.5mm gap, but no observable envelope violation is established. Real rolling cases at .5, 1 and 1.5mm gaps pass unchanged bounds alongside zero-gap/free-lip controls; the Blind Hunter accepted this required-now coverage. An admitted case violating the envelope would reopen the concern.
    location: >-
      CuriousContraptions.Simulation/WorkshopHollowKernels.cs:129
    severity: high (unverified)
  - summary: >-
      Internal-edge removal may depend on collider enumeration
    evidence: |-
      An abstract dependency chain is order-sensitive; no admitted geometry meeting all predicates was supplied. Reordered tubes around a third full-torus collar pass the same crossing/height bounds; the Blind Hunter accepted this required-now coverage. A concrete admitted ordering-dependent failure would settle the broader concern.
    location: >-
      CuriousContraptions.Simulation/WorkshopHollowKernels.cs:119
    severity: high (unverified)
---

<intent-contract>

## Intent

**Problem:** the round hollow parts are approximated with boxes, and balls catch on that approximation.
- **Pipe shell:** 16 boxes.
- **Bends:** octagons of boxes, with 4–8 cm slots between slices on the outer wall, so a ball catches an edge at every slice.
- **Funnel:** rings of tilted boxes, with an inlet collar that reaches 1.6–2.16 m against 1.43 m drawn.
- **Collars:** square frames reaching 0.95–1.24 m against 0.78 m drawn.
- **Snapped pipe joints:** a ball dips about 1.2 cm crossing one.
- **Ball detector ring:** its box corners reach 0.836 m against 0.82 m drawn.
- **Missing parts:** the gate collars have no collider. The torus parts listed by the sweep (basketball seam, bumper upper ring, weight bands and lifting eye, pressure-plate ring) need a torus kind, so they cannot collide yet.

**Owner decision (25 Oct 2026), "true hollow shapes":** the engine gets true hollow collider kinds per `docs/hollow-geometry-design.md`, with a stated surface-error bound and continuous joints.

**Approach:**
- Add shape kinds to the shared contact law. Use one narrowphase per pair, with no element-specific code:
  - **Annular cylinder (tube):** inner and outer radius, half-length.
  - **Torus segment:** centreline radius, tube inner and outer radius, sweep angle. A full torus has a 360° sweep.
  - **Frustum shell:** two radii, wall thickness, length.
- Each contact gives a correct normal and depth, against spheres first (every ball is a sphere), then against box, cylinder and the new kinds, as required by the parts that use them.
- Replace the box approximations in the Pipe, PipeBend45, PipeBend90, Funnel, BallDetector and PoweredGate collars.
- Make the sweep's static Bumper upper torus collide here. Preserve the Basketball seam, Weight bands/lifting eye and pressure-plate ring as required outcomes of the separately scheduled `engine-multi-collider-dynamic` prerequisite and consumer work; the scope disposition below records why they cannot be claimed complete here.
- Set pipe-family restitution to the documented 0.15 and re-author the authored solutions of `clear_pipe` and `joined_pipe` so they still solve.

## Boundaries & Constraints

**Always:**
- Use the real production C# simulation with behavioural assertions, and keep Skipped at 0. No mutation testing.
- Keep f32 throughout, with one current path. Delete the box-ring approximations; do not keep them.
- Derive and document a surface-error bound for every new kind in `gpu-f32-physics.md`, and test it: the contact depth of a ball pressed on each surface stays within the bound.
- Make joints continuous: a ball rolling through two snapped pipes keeps its height within the bound across the joint.
- Keep the energy gates at their current bounds, and the `F32BehaviourIdentityTests` tolerances unchanged. Re-authored lessons solve.
- Run the collider-versus-art test (all 29 packages). Move the Pending and Uncollided entries this slice resolves to Solid or Ring, and delete the `Pending` category if it becomes empty.
- Update the docs: `hollow-geometry-design.md` and the element docs.
- Edit only inside the slice worktree, with Anvil validation before each write. Implementation does not commit until the coordinator reaches Auto finalization; publication follows independent review and terminal checks.

**Never:**
- A box-ring approximation or a convex-hull fill of a hollow part.
- A second narrowphase rule for the same pair.
- Loosening any bound.
- Changing the ABI or save format beyond what the new kinds need. If the shape table needs new lanes, they are named offsets, and older layouts are refused with a typed reason.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Ball rolling through a straight pipe | 1, 3.6 and 8 m pipes | Height within the bound along the whole bore, and no catch on slices | — |
| Ball rolling through a bend | 45° and 90° bends | Stays on the outer wall, no slot catch, exits at the derived speed within tolerance | — |
| Ball through the funnel | A ball dropped into the inlet | Spirals down and out of the throat, and never escapes the wall | — |
| Snapped joint | Two pipes snapped together | No dip beyond the bound at the joint | — |
| Collar strike | A ball onto a pipe, gate or detector collar | Stops at the drawn 0.78 m or 0.82 m surface | — |
| Static torus | A ball onto the bumper upper ring | Stops at the drawn tube surface | — |
| Pipe-family restitution | A ball dropped onto a pipe shell | Rebound ratio within a derived interval of 0.55 × 0.15 | — |
| Lessons | clear_pipe, joined_pipe | Their re-authored solutions solve at every precision | — |

</intent-contract>

## Code Map

Paths are relative to the existing slice worktree `/Users/aidan/dev/personal/tim/.claude/worktrees/story-engine-hollow-shapes`. Resume its uncommitted implementation; do not replace it with the stale shared main. Baseline is the published SHA above. The root's separate unpublished planning commit is preserved, not merged indiscriminately.

- `engine/gpu/HollowGeometry.cs` — new shared `HollowSolid`, `Distance`, `MeridianDistance`, `SupportNear`, `Rims`; geometric data, f32 only.
- `engine/gpu/HollowContactQuery.cs` — shared typed sphere/box/cylinder measurements, bounded geometric scratch and overlap queries for simulation, placement and compile.
- `CuriousContraptions.Simulation/WorkshopHollowKernels.cs` — `CollideHollow` contact adapter, hollow bounds and `RemoveInternalHollowEdges`; paired order, free lips and seams.
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs` — `ShapeKind`, `ColliderExtent`, `GenerateManifold`; `WorkshopPhysicsEngine.cs` — hollow AABBs and shared manifold processing. No element-selected laws.
- `CuriousContraptions.Abstractions/ColliderDescriptor.cs`, `CuriousContraptions.Bundle/BundleGeometry.cs`, `BundleValidator.cs`, `engine/gpu/PhysicsDeclarations.cs`, `DeclaredPhysics.cs`, `PassageSensorDeclaration.cs` — typed descriptor, bounds, admission and consumers; existing lanes hold the new geometric data.
- `tools/BundleElements/SceneGeometryExtractor.cs` — hidden collision proxies, open-cylinder and torus extraction; `tools/BundleElements.Tests/SceneGeometryExtractorTests.cs`, `BundleValidatorTests.cs`, `BundleRoundTripTests.cs`, `Fixtures.cs` — real build-boundary checks.
- `engine/PlacementPose.cs` — `OverlapsHollow` uses the shared feature query; sparse sampled placement logic is deleted. `WorkshopPhysicsCompiler` uses the same query and `WorkshopSimulation` preserves prior state on typed overlap refusal.
- `elements/Pipe/pipe.tscn`, `PipeElementDeclaration.cs`; `elements/PipeBend45/pipe_bend_45.tscn`, `PipeBend45ElementDeclaration.cs`; corresponding `PipeBend90` files; `elements/Funnel/funnel.tscn`, `FunnelElementDeclaration.cs`; `elements/BallDetector/ball_detector.tscn`, `elements/PoweredGate/powered_gate.tscn`, `elements/Bumper/bumper.tscn` — existing art, separate proxies, mouths, materials and parameter bindings.
- `CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs` — new real-solver fixtures, admission, surface and free-end checks; `HollowElementTests.cs` — production package/command/committed-read integration, mixed joins, lifecycle and no-route controls.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` — all 29 packages, art classification and geometric fit; retain named unresolved solid consumers.
- `CuriousContraptions.tests/{StraightPipeTests,PipeBend45Tests,PipeBend90Tests,FunnelTests,BallDetectorTests,PoweredGateTests,PlacementPoseTests,WorkshopPipeLengthTests,PackageRuntimeParityTests,BundlePortsTests,WorkbenchCapacityTests,WorkshopBumperTests,WorkshopPhysicsLawTests,WorkshopPhysicsLawCylinderTests,ClearPipeLevelTests}.cs` — already changed callers; correct affected assertions without widening bounds.
- `CuriousContraptions.tests/EventRingProducerTests.cs` — existing `Ring` harness; `WorkshopPhysicsLawTests.cs` — existing `LawScene`. Reuse defined interfaces; no new harness platform.
- `content/puzzles.json` — existing clear_pipe/joined_pipe re-authoring; `CuriousContraptions.tests/F32BehaviourIdentityTests.cs` — read-only regression tolerances and retained event identity; existing lesson tests exercise production content.
- `docs/hollow-geometry-design.md`, `docs/gpu-f32-physics.md`; `docs/planning/elements/CAT-002-ball_detector.md`, `CAT-030-funnel.md`, `CAT-048-pipe.md`, `CAT-049-pipe_bend_45.md`, `CAT-050-pipe_bend_90.md`, `CAT-051-powered_gate.md`, `CAT-015-bumper.md`, `CAT-001-ball.md`, `CAT-052-pressure_plate.md`, `CAT-067-weight.md` — current behavior and explicit retained consumer ownership.
- `_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes` — exact source acceptance; `TODO.md` and `_bmad-output/implementation-artifacts/deferred-work.md` — concise current status/dependencies, not completion by assertion.

## Tasks & Acceptance

**Execution (dependency order):**

1. [x] `docs/hollow-geometry-design.md`, `docs/gpu-f32-physics.md` — distinguish the three error measures below and preserve original bounds. Record generic shape domains, static-only admission and explicit remaining dynamic/identity consumers. Read exact affected CAT acceptance and requirements anchors; retain every source ID.
2. [x] `ColliderDescriptor.cs`, `BundleGeometry.cs`, `BundleValidator.cs`, `SceneGeometryExtractor.cs`, `PhysicsDeclarations.cs`, `DeclaredPhysics.cs` and their mapped tests — finish typed finite/range/undefined-kind, cap/scale/wall/sweep refusal and valid boundary mapping through bundle, compile and command boundaries. Demonstrate atomic rejection and no prior-state mutation.
3. [x] `HollowGeometry.cs`, `WorkshopHollowKernels.cs`, `WorkshopCollisionKernels.cs`, `WorkshopPhysicsEngine.cs`, `PlacementPose.cs` — finish one shared geometry/contact path, with complete broadphase bounds and supported partner interactions. Fix only demonstrated defects. Inspect seam removal against free rim, nearby unjoined solids and reversed pair/order controls; inspect sampling and support-point validity.
4. [x] All mapped element scene/declaration files, `BundlePortsTests.cs`, `WorkshopPipeLengthTests.cs`, `PackageRuntimeParityTests.cs` — preserve approved art/palette, separate hidden proxies, bore/mouth transforms and resizing; remove superseded box shells/collars and update all actual callers. Material restitution is 0.15 for pipe, both bends and funnel.
5. [x] `WorkshopPhysicsLawHollowTests.cs`, `HollowElementTests.cs`, `VisibleSolidColliderTests.cs`, `PlacementPoseTests.cs` — close the acceptance matrix below with independently derived positive/control/boundary tests; add real mixed-joint, collar/ring, restitution, jam/miss, atomic overlap and lifecycle proof. Keep tests deterministic and bounded.
6. [x] Mapped changed package tests — replace save-output byte equality by decoded semantic fields; replace tautological radius comparisons by real physical controls. Preserve equivalent coverage and record any superseded test. Do not undertake unrelated repository-wide hygiene.
7. [x] `content/puzzles.json` and existing lesson tests — finish physical re-authoring, prove both lessons at every supported precision with unchanged event-identity tolerance; retain affected existing package/energy regression coverage.
8. [x] `CuriousContraptions.tests/CuriousContraptions.tests.csproj` — include new tests; mapped current docs, epic, TODO and this spec — record truthful scope/status, explicit deferred consumer owners and pending browser cases. Run affected checks then one full solution run at round end; send complete candidate to Auto's independent review layers.

**Acceptance Criteria / required-now matrix:**

| ID | Given / When / Then | Proof owner |
| --- | --- | --- |
| H1 | Given valid Tube/Frustum/Torus declarations at literal admitted limits, when extracted, bundled and compiled through real production boundaries, then their dimensions, poses and typed kinds are retained; given malformed/non-finite/undefined/out-of-range data, when admitted, then typed refusal leaves construction, revision and existing world unchanged. | Bundle tests; hollow admission and command tests |
| H2 | Given each supported hollow surface and sphere, box or cylinder partner, when the production simulation advances fixed ticks, then contact/non-contact, resting placement and motion obey derived geometry and existing energy/penetration limits; reversed orientation/pair and adjacent separation controls do not add phantom contacts. | Hollow law tests |
| H3 | Given straight pipes of length 1, 3.6 and 8 m, both bend angles and a funnel, when a released ball rolls/falls through them, then it reaches the intended exit without wall escape or artificial catch/energy; the funnel off-axis release is guided physically through its throat. | Hollow element tests |
| H4 | Given production-snapped straight–straight, straight–bend and bend–funnel constructions, when a ball traverses each joint, then no dip or step exceeds the 5 mm declared surface-to-art bound and no artificial catch occurs; existing tighter straight-joint checks stay unchanged. Given a free end or genuinely separated mouths, when reached, then the ball keeps the free lip/unsupported motion rather than receiving invented support. | Mixed-joint integrations and law controls |
| H5 | Given all affected declared art/proxies and all 29 catalogue packages, when collider-versus-art checks run, then every required static solid has matching geometry within the derived bound; gate/detector collars and Bumper upper torus physically stop a striking ball at their drawn surfaces. The listed dynamic/identity consumer obligations remain explicitly incomplete with their owner. | Art and physical strike tests |
| H6 | Given each pipe-family part and a Basketball dropped onto a known outward surface, when impact occurs, then the measured normal rebound-speed ratio matches 0.55 × 0.15 = 0.0825 within a tolerance derived from tick/gravity and geometry, with a zero-restitution control; declarations alone cannot satisfy this criterion. | Package drop tests |
| H7 | Given fitting and oversized payloads, a beside-mouth launch and initial overlap, when run or placed, then fitting passage, oversized jam, miss and atomic overlap refusal occur through real interfaces; literal admitted size/scale boundaries are covered. | Law and placement/command tests |
| H8 | Given affected constructions with real connections, when Run, Pause/Step, Reset and save/load into a fresh production harness occur, then construction, parameters, connections and relevant event/state fields restore and the intended physical behavior repeats; malformed input leaves prior state intact. | Existing harness lifecycle integration |
| H9 | Given clear_pipe and joined_pipe authored solutions at all supported precisions, when played by the native production path, then both solve and preserve event identity/order and the existing timing bound; unchanged energy and F32BehaviourIdentity tolerances pass. | Existing lesson/F32/energy suites |
| H10 | Given the final candidate and its necessary callers, when schema/deletion/build/tests and Auto review run, then no box-built hollow alternative, compatibility path, element-selected solver, skipped game test or changed-output hash assertion remains in the slice; required native checks pass with Skipped 0. | Implementer checks and Auto review |
| H11 | Given the independently reviewed committed candidate, when published, then exact-SHA CI, Pages artifact and deployment succeed before the next slice starts; browser construction/Run/Reset/save qualification remains queued for post-catalogue E2E. | Coordinator and small terminal verifier |

Affected standing requirements: REQ-06 (one current path/deletions), REQ-07 (typed boundaries), REQ-08 (physical truth/energy), REQ-09 (ordinary lifecycle), REQ-11 (honest scoped evidence), REQ-12 (dependency order), REQ-13 (C#/palette), REQ-14 (independent Auto review). Existing worker ownership remains under REQ-01; no worker/clock redesign. Detailed REQ-02–05/10 performance/fault/resource qualification stays with its named later gates, without waiving ordinary correctness.

## Spec Change Log

### 2026-10-26 — Resume planning and coordinator dependency disposition
The user confirmed no other agent owns the partial work and explicitly requested resumption. The existing spec lacked executable tasks, complete mixed-join/lifecycle/control acceptance and evidence for its ready status; restored to draft for the planning gate. All original physical intentions are retained.

Coordinator decision: the published engine-hollow-shapes story owns the complete static pipe/bend/funnel/detector/gate outcome and all three named joins; the already-implemented static Bumper upper torus is included. Published TODO separately schedules engine-multi-collider-dynamic. Basketball seam and Weight bands/lifting eye need dynamic compound bodies/inertia and hollow partner support; the pressure-plate ring needs collider identity expansion because its sixth collider reaches its contact-load row. Those exact contacts and native/control/lifecycle/art proof remain mandatory under that owner, not cancelled or complete. Update current source pointers with this disposition; do not claim whole visible-solid closure. The original no-commit implementer restriction applies until Auto finalization, not as a ban on the user's authorized publication.

KEEP the existing useful static geometry, package replacements, approved palette, strict f32 runtime, unchanged energy gates and F32 regression tolerances. Repair causal defects and missing proof without rewriting working code or introducing a second supported path.

## Review Triage Log

### 2026-10-26 — Review pass 0
- verdicts: 18 findings — high 3, medium 6, low 0, false 4, maybe-false 5
- findings:
  - `[medium]` `[patch]` V1: full-torus flat-face support lacks a discriminating simulation check — the verification layer traced sphere, segment-end and tube tests; none exercises a broad box/cylinder cap over the horizontal full ring. Add real settled-height, orientation, speed and separated controls.
  - `[medium]` `[patch]` B1: explicitly closed CylinderMesh caps are rejected — extractor treats property presence as openness, then requires both values false. Preserve the one solid-cylinder path for both true caps and cover that input.
  - `[maybe-false]` `[defer]` B2: sampled minimum can miss an intersection — reviewer withdrew the claim of demonstrated failure: no admitted geometry defeating all candidate generators was established. If true, high; independently derived narrow intersections between sample positions would settle the specific concern. Required-now boundary coverage is being strengthened; no universal completeness claim follows from selected fixtures.
  - `[maybe-false]` `[defer]` B3: 1 mm merging can discard distinct thin-wall constraints — wall minimum is 0.9765625 mm, but no conflicting generated contacts or lost support was established. If true, high; real minimum-wall rim/normal cases would settle it.
  - `[maybe-false]` `[defer]` B4: cylinder axis search can overstate radial support — r versus r*sqrt(1-c*c) differs when c is nonzero, but an exact smooth interior minimum has c=0 and no reachable erroneous candidate was established. If true, high; a tilted just-clear side boundary would settle it.
  - `[maybe-false]` `[defer]` B5: submillimetre separated rims qualify for seam removal — the 0.5 mm gap satisfies the 1 mm geometric predicate, but the reviewer established no observable violation beyond the accepted envelope. If true, high; controls below/at/above the seam tolerance would settle it.
  - `[maybe-false]` `[defer]` B6: seam removal can depend on manifold enumeration — the abstract A-to-B-to-C removal chain is order-sensitive, but no admitted geometry satisfying the actual predicates was supplied. If true, high; a three-collider junction under reordered identities would settle it.
  - `[high]` `[patch]` B7: a centred sphere striking a bare partial-torus opening receives an arbitrary transverse impulse — for R=2.4, inner=.65, outer=.7, sweep=pi/2, sphere radius=.66, first start-face tangency at z=-sqrt(.66^2-.65^2) selects only the +Y rim point. The early return retains an asymmetric normal although reflection about Y preserves the launch. Supply the missing symmetric end-rim support in the same query and prove the zero-gravity centred strike/control.
  - `[medium]` `[patch]` B8: thin-wall tilted partner checks stop at Overlaps — the asserted query result bypasses manifold reduction and ticks. Add corresponding advancing real box/cylinder boundary and clear controls.
  - `[false]` `[reject]` B9: partial-torus frame errors evade all checks — the generic helper checks the axis, but BendColliders.Assert also checks directed local +X; VisibleSolidColliderTests.AssertArc checks every chord angle against the directed local +Z sweep and physical bend exit tests traverse it. The helper is not the sole rotation assertion.
  - `[medium]` `[patch]` B10: affected bend/funnel/gate physical replay is missing — their codec round trips and F32 RunAndReset checks do not re-run the changed interaction after fresh load. Extend the existing native lifecycle scenario with those affected constructions.
  - `[high]` `[patch]` E1: tilted torus-crest candidates lie inside empty bore — at R=2, inner=.699, outer=.7, a=pi/3 and n=.19*tangent+sqrt(1-.19^2)*Y, R*radial-.7*n has tube distance about .68727, below inner radius. A tiny admitted box around it is clear yet receives a negative gap. Project the normal into the sampled meridian before constructing its surface point and prove clear/contact controls.
  - `[high]` `[patch]` E2: symmetric placement bounds invent the missing half of a torus segment — Within subtracts the maximum absolute support from the revolution origin; occupied arcs are asymmetric, so valid grid/snapped bends fail near boundaries. Use signed directional support bounds in the existing one placement-volume rule, with boundary controls.
  - `[false]` `[reject]` I1: intent necessarily promises universal 10 micrometre qualification — 10 micrometres is the explicitly retained near-origin fixture target; the required 5 mm shipped surface-to-art bound is derived separately. Current numeric authority rejects proof-grade certificates as acceptance and retains later qualification; universal 10 micrometre certification is not claimed.
  - `[false]` `[reject]` I2: distinct error observables violate one bound — current authority distinguishes surface fit, resting penetration and temporary impact penetration. The candidate preserves each existing limit rather than substituting the impact envelope for art or rest.
  - `[medium]` `[patch]` I3: funnel spiral and complete payload containment lack direct proof — the current release has no tangential motion and checks only its centre. Add a tangential physical release with angular travel and complete sphere/wall clearance assertions using independently derived geometry and the unchanged impact envelope.
  - `[medium]` `[patch]` I4: sphere coverage omits frustum ends/rims — existing frustum end tests use boxes/cylinders. Add real sphere end-face/rim height or clearance cases and controls under the unchanged bounds.
  - `[false]` `[reject]` I5: named lesson positive evidence is missing or joined_pipe was not re-authored — F32BehaviourIdentityTests runs every authored lesson at all three precisions with goal/event timing assertions (66 passing cases); the diff moves joined_pipe's receiver. The new no-route tests are controls, not the positive oracle. CAT-048's “unchanged” sentence is stale and needs direct correction.
- Forward-Only Auditor: no confirmed violation; singular radial-direction choices remain within one numerical algorithm, declaration defaults describe current solid/full tori, and the required Radius field has no compatibility reader.
- Coordinator direct corrections: update CAT-048's stale joined_pipe/receiver prose and split the two adjacent Compile entries onto separate lines. No acceptance bounds change.
- Repairs were returned to the original implementation owner and independently re-reviewed as recorded below. Five review layers ran in two waves because the host has three child slots; no layer was skipped. The ignored worktree diff file is used because Anvil admits writes inside this worktree; it is a single review input, not a new manifest.

## Design Notes

**Guidance precedence.** AGENTS delivery speed/native-first rules govern cadence; one Auto review system including Forward-Only Auditor. The sole isolation authority requires package .tscn scenes and declaration-only generic capabilities; stale glTF-only prose in published DESIGN is not a reason to add another loader. Preserve its palette/form/motion. No physics on the main-thread presentation path, no per-element solver or second runtime mode, no ABI versions or migration, no new JS source.

**Independent error model, before verification.** Keep three quantities separate:
- Surface-to-art requirement: at most 5 mm for the shipped hollow parts. For an n-segment circular mesh, radial chord sagitta is r(1 − cos(π/n)); a bent straight chord of half-length a on centreline radius R has sagitta R − sqrt(R² − a²). Evaluate these from actual mesh declarations, including both torus directions and endpoint/stub coverage; account for simultaneous error terms rather than assuming their maximum suffices. If art exceeds 5 mm, refine its mesh without changing palette/physical dimensions or widening the bound.
- Existing near-origin analytic fixture target: retain 10 µm distance error plus at most the existing 0.5 mm resting penetration, and the current tighter straight-joint assertions. The current prose's “twenty roundings within 4 m” does not prove a universal bound at admitted 16 m dimensions or translated world coordinates. Audit operation/transform magnitudes and independently test literal geometric extrema/translations. Do not advertise the 10 µm target as universally qualified until the derivation covers its domain.
- Physical dynamic loading: retain the authority's temporary penetration bound (5% of smaller extent), existing energy limits and fixed f32 regression tolerances. Finite sample/minimum searches for box/cylinder candidates need their own completeness/boundary controls; an exact SDF alone does not prove an exact manifold. No tolerance may be selected from observed output.

No unresolved product/model choice is delegated to invented behavior. Implementation must demonstrate these existing bounds or report the concrete failing geometry and retain the same slice; it cannot shrink admitted behavior to obtain a pass.

## Verification

From the slice worktree, after source/configuration changes:
- `dotnet build CuriousContraptions.slnx -c Debug` — incremental matching build; zero warnings/errors. Do not run local Release worker builds or web publish.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter "FullyQualifiedName~Hollow|FullyQualifiedName~VisibleSolidCollider|FullyQualifiedName~PlacementPose|FullyQualifiedName~StraightPipe|FullyQualifiedName~PipeBend|FullyQualifiedName~Funnel|FullyQualifiedName~PoweredGate|FullyQualifiedName~BallDetector|FullyQualifiedName~F32BehaviourIdentity|FullyQualifiedName~JoinedPipe|FullyQualifiedName~ClearPipe"` — affected diagnosis; all selected tests execute and pass. This selection never replaces the full required run.
- `dotnet test tools/BundleElements.Tests/BundleElements.Tests.csproj -c Debug -f net10.0 --no-build --no-restore` — affected extraction/validation/round-trip tests; zero skipped.
- `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` — once at round end, all required game tests including shipped-output node harnesses, zero failed/skipped; only the existing explicit browser deferral applies.
- `git diff --check` — zero whitespace defects. Inspect actual changed/untracked files, including new tests and geometry.
- Anvil before each write; affected non-C# Anvil check and normal C# build/static protections. Inspect deletion of superseded box shells/collars and caller agreement; no output hashes or compatibility remnants. Reviewers assess plausible-defect sensitivity by reading assertions; no mutants.
- Preserve original command results and any failed attempts in the slice record; identities are full Git SHAs, CI runs and test totals, not duplicate manifests.
- Pending browser proof: actual UI construction, placed configuration, mouth joins, physical controls, Run/Reset/save/load and affected collar/art interactions, owned by post-catalogue E2E. No browser tests are written/run/provisioned as this slice's gate.

## Implementation checkpoint — 26 October 2026

This section records the original r0 checkpoint. The later repair, full-run and final review sections supersede its approval status and test totals. Browser qualification and publication remain separately tracked.

- Shared `HollowContactQuery` now owns sphere/box/cylinder feature measurement for simulation, placement and compile. Sparse placement sampling is deleted. Compile rejects independent dynamic payload/wall intersections atomically as `InvalidConstruction`; declared mechanism children retain their own-parent contacts. The 0.1 mm placement slop admits a 50 µm rested overlap and refuses 200 µm.
- Fixed a demonstrated torus-segment support defect: the endpoint support now lies in its flat meridian disc, not an invented rounded cap. Cylinder cap-area candidates prevent the demonstrated tipping/sinking on that disc. Scratch capacity 152 comes from bounded geometric features, not scene counts; no candidate truncation branch exists.
- Refined radial/collar tessellation to 64/16 where necessary. Art tests add simultaneous chord errors, including bend chord plus radial error, within the unchanged 5 mm bound. The 10 µm near-origin analytic target and 0.5 mm resting envelope remain unchanged; literal 16 m dimensions and translated 32 m fixtures are checked, without claiming universal f32 error qualification.
- Removed mapped save-output byte comparisons in favour of semantic decoded values. Removed tautological radius-fit assertions; real package fitting/oversize simulations replace that claimed coverage.

### Authored physical corrections

Receiver walls have inner x faces at ±0.69 m and top at local y+0.5 m. A radius-0.34 ball must therefore be within receiverX±0.35 at centre height receiverY+0.84 to enter without hitting the near rim. Floor-centre ballistic predictions alone are insufficient; trials using them hit rims and were discarded.

- `clear_pipe`: the retained pipe pose (0.2,3.1), −37.5° keeps its outgoing trajectory above the near rim; the original −45° pose at (0,3) hit the rim and rode outside. Receiver x moves 2→1.7, within the derived feasible mouth interval approximately [1.49,2.19]. Native authored captures are within the unchanged baseline timing bound at all precisions; bounded approach diagnostics gave ticks [209,256,269] for this geometry and [210,258,270] with a +0.5 m release-height control.
- `joined_pipe`: receiver x moves −0.2→0.2, into the mouth-entry interval approximately [0.05,0.75] from the smooth bend's outgoing path. The production-snapped 2 m pipe and fixed bend poses are unchanged. All three authored precision timing gates pass.
- `quarter_bend`: lower the locked source by 0.5 m (7.3870564→6.8870564 m), reducing gravitational energy before the bend while preserving the vertical-entry/sideways-exit lesson. The bend, receiver, inventory, goal and assistance are unchanged. Bounded native runs give [254,264,318] versus baseline [259,268,322]; actual authored F32 checks pass.
- No capture margin, guide acceleration, speed limit, dwell, event identity or F32 tolerance was changed. No-route controls retain the need for the player’s hollow part; the joined lesson’s existing unsnapped/default-length controls remain required.
- Temporary trajectory/authoring diagnostics were removed. They changed authored inputs only, never solver source; no mutants, browser run or output-hash assertion was introduced.

### Acceptance coverage and verification

| Criteria | Executed proof |
|---|---|
| H1 | Bundle extraction/validation/round-trip suite; literal hollow declaration boundaries; invalid length and initial-overlap atomic command refusal; unchanged prior revision/world |
| H2 | Sphere surface laws; box/cylinder Tube/Frustum end faces and Torus flat ends; reversed collider order; separated controls; rotated near-64 m/s bore/air/oversize launch; minimum-wall tilted box/cylinder overlap and just-clear query controls |
| H3 | Production straight pipes at 1/3.6/8 m; both bend exits with derived speed; off-axis funnel throat traversal |
| H4 | Actual production snaps for straight–straight, straight–bend and funnel–bend; free lip and separated-mouth controls |
| H5 | All 29 catalogue art classifications and physical collar/ring strikes; dynamic seam/bands/eye and pressure-plate identity work remains explicitly owned elsewhere |
| H6 | Physical 0.55×0.15 drops on pipe, both bends and funnel; four zero-restitution package controls |
| H7 | Eight real fitting/oversized package passages; beside-mouth/free-air controls; all three partner kinds in placement/compile, tilted controls and rested-slop boundary |
| H8 | Connected pipe/detector/lamp construction, Pause/Step/Resume, exact Reset restoration, semantic save/load into fresh harness and repeated passage source/tick |
| H9 | Actual authored `F32BehaviourIdentityTests`: 66 passed, 0 failed, 0 skipped; route-required controls: 6 passed |
| H10 | Matching Debug solution build: 0 warnings/errors; post-fix full solution: 4,182 passed / 0 failed / 0 skipped; affected Anvil non-C# check: no warnings; whitespace check clean |
| H11 | Coordinator-owned Auto review, commit, exact-SHA CI and deployment pending |

Verification commands use the spec’s Debug/`--no-build --no-restore` cadence. BundleElements: 206 passed, 0 failed/skipped. High-speed positive/control/boundary checks: 6 passed. Initial high-speed free-flight assertion incorrectly reused the analytic distance target; its independent integration bound now accounts for 60 additions plus eight transform/projection roundings, coordinates below 8 m, half-ulp 2^-21 m/component and sqrt(3) for vector norm. Existing analytic/resting/energy limits were not widened. First full run: 4,182 tests (main 3,672 passed / 2 failed in 8m33s; tool projects 508 passed), Skipped 0. It found only generic local names misclassified as Wall package knowledge by the isolation guard, and a pressure-plate negative control still resting a ball on the deleted bend box slat. Locals are renamed without altering the guard; the obsolete slat fixture now uses a declared solid panel with the plate footprint/top height, retaining the supported-ball/closed-gate control and strengthening the rest-height assertion. Admission prefilters the hollow collider array rather than repeatedly scanning ordinary colliders. Post-fix focused checks: 21 passed, 0 failed/skipped. The required post-fix full run passed all 4,182 tests (main 3,674 in 8m36s; WorkerAotGuard 17, BundleElements 206, Coverage 77, E2eRunner 208), with 0 failed/skipped. Failed diagnostic/build attempts and corrective runs remain in the implementation tool transcript.

### Final verification applicability

- Baseline HEAD: `4a272e4bb5dcd36b446769120c6be260f5cc9330`; working-tree candidate remains uncommitted.
- Matching incremental build: `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet` — 0 warnings/errors.
- Full required run: `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal` — 4,182 passed, 0 failed, 0 skipped (tool session 62914, completed exit 0).
- After that full run, only `ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` assertions were strengthened: oversized payloads must retain the centred release line above/before the exit and stay within the existing resting envelope over the last quarter-second, with speed bounded by that displacement/window. This rules out a wall escape or bench settling masquerading as a jam.
- Matching affected test-project build after strengthening: 0 warnings/errors (8.73 s). `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload|FullyQualifiedName~FastAxialLaunch|FullyQualifiedName~ReauthoredLessons' --nologo -v minimal` — 20 passed, 0 failed/skipped (session 92195).
- The full result applies to unchanged production, content, dependencies and other tests. It does **not** claim to have executed the subsequently strengthened assertions; the matching 20-test run supplies that evidence. The coordinator explicitly accepted this relevant-input reuse; exact-SHA CI will run the full suite again.
- Final non-C# Anvil and whitespace checks are clean. No local Release/publish, browser or mutation run was performed. Native scope does not qualify the separately owned dynamic contacts, universal floating-point error domain, device/performance gates or post-catalogue browser cases.

## Review-fix handoff — 26 October 2026

The production/test candidate is frozen for coordinator full verification and independent re-review. The prior 4,182-test result above belongs to r0, not this changed solver candidate. No commit, full solution run, browser run, Release build or mutant was made in this fix turn.

Confirmed fixes:
- Cylinder proxy classification reads actual cap booleans (omitted caps default closed). Explicit true/true follows the existing solid-cylinder path; malformed and partially open declarations remain refused. Extractor controls pass.
- Torus crest candidates project each face normal into that sample's meridian. The reported clear 1 mm half-extent box failed before correction and now passes beside the real-contact control.
- Placement uses signed directional supports; the superseded symmetric hollow extent helper is deleted. Grid and snapped bend placements at y=1.4 have their actual bottom at -0.387056, less than one grid step above the -0.46 bench. Genuine outside controls at y=1.2 refuse on both paths.
- Bare torus-segment mouths supply their full sphere rim contact set (bounded maximum 17 candidates). The reported centred jam failed before correction. Completing that set exposed a shared solver defect: translational coupling omitted the normals' dot product. Correcting it fixes both radius-.66 and radius-.75 real centred jams without transverse drift, with fitting/offset controls. This shared normal-block change requires the coordinator's full suite and independent review; it changes no energy limit.
- That correction exposed two existing cylinder flat-end rest-speed failures. A retained micrometre plane-normal test passed, so no speculative distance-function change was made. Omitting the redundant moving rim minimum when all fixed cap-rim supports are present resolves both failures; partial rims retain their minimum. Tilted partial-cap and separated real controls pass.
- Added broad box/cylinder full-torus crest rest, sphere frustum face/inner-rim/outer-rim/clear controls, tangential funnel angular travel plus full-sphere finite-wall clearance, bend/funnel/gate Reset and fresh semantic-load physical replay. The ordinary off-axis funnel proof remains.
- Corrected CAT-048's receiver re-authoring account and split the two project Compile entries. No authored puzzle, assistance, impact/resting or energy tolerance changed.

The five uncertain numerical concerns now have these selected behavioural outcomes. They establish these cases, not universal completeness of bounded searches:

| Concern | Targeted result |
|---|---|
| Sample minima | 8 advancing tilted box/cylinder minimum-wall Tube/Frustum contact/clear cases pass. Short-sweep Torus intersection lies between box edge sample locations: 2 shared-query clear/contact cases and 2 advancing real box contact/clear cases pass. |
| Thin-wall merging | Minimum-thickness rim query retains both inner and outer normals with admitted box extents; the final corrected fixture passes. The 8 real minimum-wall support/clear cases also pass. No lost required constraint was demonstrated in these cases; no merge-rule change was made. |
| Cylinder side projection | Tilt about X leaves exact radial X extent .1: x=.801 clears the .7 Tube by 1 mm throughout Z travel; x=.799 acquires separation. Both advancing cases pass. |
| Sub-mm seams | Real rolling passage at nominal mouth gaps .5, 1 and 1.5 mm passes unchanged resting/analytic bounds. Zero-gap baseline and existing free-lip/half-metre gap controls pass. |
| Junction ordering | Both tube identity orders around a third full-torus collar pass the same independently derived bore-height/crossing bounds. |

Commands/results (all Skipped: 0):
- Matching extractor build, then `dotnet test tools/BundleElements.Tests/BundleElements.Tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~SceneGeometryExtractorTests' --nologo -v minimal`: 49 passed, 0 failed (session 30709).
- Matching main test-project Debug build: 0 warnings/errors. `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~WorkshopPhysicsLaw|FullyQualifiedName~PlacementPoseTests|FullyQualifiedName~HollowElementTests|FullyQualifiedName~DeclaredPrismaticDriveTests' --nologo -v minimal`: 483 passed, 0 failed, 7m20s (session 73129). This covers the shared solver's physics-law consumers.
- After that build/run, only test inputs/assertions were refined: admitted box extents for the rim-normal case, the bend boundary inside one grid step, and committed Reset pose/speed/tick assertions. Matching build: 0 warnings/errors, 9.30s. `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~MinimumThicknessRimKeeps|FullyQualifiedName~BendGridPlacement|FullyQualifiedName~SnappedBendUses|FullyQualifiedName~ABallDroppedIntoABend|FullyQualifiedName~ABallDroppedIntoTheFunnels' --nologo -v minimal`: 8 passed, 0 failed (session 52377). Production was unchanged; the 483-test output does not claim these later assertions.
- The frustum free-flight control uses an independent accumulated rounding bound for 480 updates below 8 m, rather than the analytic surface-distance target. The centred jam allows at most one tick of speculative early stopping; its penetration and transverse drift retain the existing resting envelope. Diagnostic trace collection was removed.
- Changed-file Anvil and whitespace checks are clean. Raw failed build/test attempts and repairs remain in the tool transcript. Deferred-list resolution and full required verification remain coordinator/reviewer-owned.

Files changed from the preserved r0 candidate: `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`; `CuriousContraptions.tests/{CuriousContraptions.tests.csproj,DeclaredPrismaticDriveTests.cs,HollowElementTests.cs,PlacementPoseTests.cs,WorkshopPhysicsLawHollowTests.cs}`; `engine/PlacementPose.cs`; `engine/gpu/{HollowContactQuery.cs,HollowGeometry.cs}`; `tools/BundleElements/SceneGeometryExtractor.cs`; `tools/BundleElements.Tests/SceneGeometryExtractorTests.cs`; `docs/hollow-geometry-design.md`; `docs/planning/elements/CAT-048-pipe.md`; `TODO.md`; this spec (the coordinator's existing review log is preserved).

## Full-round regression repair — 26 October 2026

The five independent layers completed r1 source/assertion re-review; the intent layer retained a literal resting-frustum proof distinction, closed below. Required runtime verification then failed. Coordinator command `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal`, after a matching solution build (0 warnings/errors, 8.99s), returned main 3,715 passed / 1 failed / 0 skipped in 8m36s, plus 508 tool tests passed (session 20455). Total: 4,223 passed, 1 failed, 0 skipped. The only failure was Weight's existing 0.05 mm post-stop no-creep bound: observed 0.064611435 mm. All hollow laws passed. This full-round failure is distinct from the successful independent source review and prior candidate results.

Causal trace from the unchanged Weight fixture:
- At stopping tick 1138, centre (8.369494, -0.10860491, 0.000010699034); final centre (8.369494, -0.10860491, 0.00007531047). The movement is axial, not vertical settling. Residual axial speed remains approximately 3.7 to 3.6 micrometres/second.
- Reconstructing the production narrowphase from committed rolling poses at ticks 60 and 90 gives two identical normals (0.25881904, 0.9659259, 0), each with f32 self-dot 1.0000001. The new direct-dot block diagonal consequently differed from the single-row effective mass's unit-direction diagonal. This perturbed the ramp trajectory before the final bench contact.
- The sole production repair evaluates the same unit-normal coupling with the branch-free chord identity `ni·nj = 1 − |ni−nj|²/2`. Identical normals have exactly unit coupling in f32; nonparallel contacts retain their correct coupling. No threshold, solver mode, fallback, force clamp or tolerance changed.
- Temporary normal reconstruction and trace code were removed; `WeightTests.cs` has no delivered change. The failing test, all its bounds, energy checks and F32 assertions remain intact.

Verification: matching test-project Debug build, 0 warnings/errors in 6.16s (session 65859), then
`dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~WeightTests|FullyQualifiedName~WorkshopPhysicsLawHollowTests|FullyQualifiedName~ACylinderRollsDownARampAtTwoThirds|FullyQualifiedName~BelowTheNoSlipFriction|FullyQualifiedName~RollingResistanceOnTheCurvedSide|FullyQualifiedName~DroppedEndDownACylinder|FullyQualifiedName~DroppedOnItsSideACylinder|FullyQualifiedName~AWeightRollingOffAFlushRamp|FullyQualifiedName~ALyingCylinderSetDownWithAMicroTilt|FullyQualifiedName~ACylinderSetDownExactlyLevel|FullyQualifiedName~ARockingLyingCylinder' --nologo -v minimal`
passed 152 tests, 0 failed, 0 skipped in 4s (session 46026). These cover the Weight regression, hollow nonparallel jams, parallel ramp rolling/slip controls, rolling resistance and cap/side/microtilt/level rest. Anvil and whitespace checks are clean.

Files changed for this repair: `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`, `TODO.md`, and this spec. No full run or commit was made by the implementer. The coordinator subsequently completed the passing full run and independent scoped numerical re-review recorded below.

## Final test-only frustum acceptance closure — 26 October 2026

Coordinator full verification after the normal-coupling rounding repair passed: `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore --nologo -v minimal`, session 55049 exit 0, main 3,716 passed / 0 failed / 0 skipped in 8m35s plus 508 tool tests. Total 4,224 passed, 0 failed, 0 skipped. This supersedes the preceding runtime failure.

After that full run completed and the coordinator explicitly released the write/build hold, only four cases in `WorkshopPhysicsLawHollowTests.GravityPressesSpheresOntoFrustumFacesAndRimsWithinTheRestingEnvelope` were added:
- Frustum top annulus [.8,1] at y=4: radius-.1 sphere over the face at x=.9 rests at y=4.1, vertical normal projection 1.
- Radius-1 sphere centred over the inner rim rests at y=4+sqrt(1−.8²)=4.6, vertical normal projection .6.
- Four equivalent frusta centred ±1.06 along X/Z hold a radius-.1 sphere on their inward-facing outer rims, horizontal reach .06. Its centre rests at y=4+sqrt(.1²−.06²)=4.08, vertical normal projection .8. The symmetric statics provide physical support in both horizontal directions; a single convex outer rim would not hold a laterally free sphere.
- A sphere released clear at x=1.2 falls under gravity, retains its lateral position and reaches the independently bounded free-fall velocity.

Each resting case advances one continuous 1.25-second real simulation and checks every tick in its final quarter-second: the independently derived normal-projected resting height, exact distance to the circular rim(s), bounded lateral displacement, and settled speed. The 10 µm analytic target and 0.5 mm resting envelope are unchanged. No production, geometry input, dependency, impact/energy or existing assertion changed.

First targeted attempt (session 43944): 17 passed / 3 failed / 0 skipped. The test mistakenly called `LawWorld.Step` twice as though it continued; that harness explicitly resets from admitted bytes on every call. The three assertions therefore observed the first falling tick, still approximately .05 m above rest. Corrected to one continuous call with observations after one second. No physics change or output-fitted geometry/tolerance adjustment was made.

Final matching build: `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo -v quiet`, 0 warnings/errors, 6.73s (session 11504). Then `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~GravityPressesSpheresOntoFrustum|FullyQualifiedName~SpheresMeetTheFrustumEnd|FullyQualifiedName~BareSegmentMouth|FullyQualifiedName~BoxAndCylinderMeetTubeAndFrustumEndFaces' --nologo -v minimal`: 20 passed / 0 failed / 0 skipped, 324ms (session 78583).

Applicability: the 4,224-test full result covers unchanged production and pre-existing tests; it does not claim to include the four subsequently added cases. The matching 20-test result covers that test-only addition and relevant hollow positive/control cases. Final Anvil and whitespace checks are clean. Changed files for this last closure are the hollow law test file, this spec and TODO only. Independent focused intent/verification re-review and publication remain coordinator-owned; implementer edits are frozen.


## Final independent review and Auto Run Result — 26 October 2026

**Local status: done. Publication status: pending.** H1–H10 pass within the required-now scope. H11 still requires approval of the committed SHA, exact-SHA CI, the Pages artifact and deployment verification. This local Auto result is not a terminal publication or browser-qualification claim.

**Implementation:** one shared typed f32 Tube/Frustum/Torus geometry and contact path now serves simulation, placement and compile admission. Named static packages use their true hollow surfaces; superseded box shells/collars and sparse placement sampling are deleted. Signed supports preserve partial-arc placement, and one shared unit-normal coupling law handles parallel and nonparallel contacts. Art fit, real package interactions, controls, lifecycle and authored lessons are verified without widening bounds.

**Changed files:** the Code Map describes geometry, declaration/bundle boundaries, element scenes, content, tests and current contracts. Additional affected files are `WorkshopConstraintSolver.cs` (same-law normal coupling), `DeclaredPrismaticDriveTests.cs` (gate Reset/save-load physical replay), `PressurePlateTests.cs` (solid-panel control replaces deleted bend-slat fixture), the main test project (includes hollow tests), and `sprint-status.yaml` (review/publication state). Scene UID files identify the two new geometry sources. No unrelated shared-checkout planning changes are included.

**Independent identities and applicability:**
- Implementation and all forward fixes: `/root/hollow_dev`; coordinator: `/root`.
- `/root/hollow_blind_r0`: original B1–B10 investigation and r1 fix re-review. Confirmed repairs pass; B2–B6 gained sufficient scoped behavioural controls with no demonstrated required-now defect. Their broader unverified claims remain in the single deferred list.
- `/root/hollow_edge_r0`: crest and asymmetric-placement findings repaired and passed r1 re-review; r2 normal-chord evaluation inspected against actual unit-normal callers, Pass.
- `/root/hollow_forward_r0`: original/r1/r2 compliance Pass. The chord identity replaces direct dot evaluation unconditionally; it adds no alternate path. Existing singular/inadmissible block recovery applies the same contact law and is numerical recovery, not compatibility.
- `/root/hollow_verification_r0`: full-torus support gap repaired; r1 checks Pass. r2 unchanged Weight displacement assertion and nonparallel jam controls discriminate the corrected law, Pass. Final four resting-frustum cases independently assessed for geometry, real interfaces, deterministic assertions and defect sensitivity, Pass.
- `/root/hollow_intent_r0`: final I4 re-review confirms pressed/resting face and both rims now meet the literal acceptance surface. Prior distinctions remain: the 10 µm fixture target is not universal qualification; tangential launch proves angular guided descent and full-sphere finite-wall clearance under the existing impact envelope, not spontaneous angular motion from a perfectly radial release. Neither is advertised as broader qualification.

### Follow-up triage
- verdicts: 3 intent distinctions — medium 1, false 2; patches: medium 1.
- `[false]` `[reject]` Universal 10 µm certification remains absent — carried I1 disposition: intent/current authority distinguishes the near-origin fixture target, derived art fit and later qualification; no universal claim is made.
- `[false]` `[reject]` Tangential funnel launch does not prove spontaneous gravity-only spiralling — the guided angular traversal scenario directly observes angular travel and finite-wall sphere clearance; ordinary off-axis gravity release also passes. No requirement selects spontaneous angular motion from symmetric initial conditions.
- `[medium]` `[patch]` Frustum impact-only cases do not establish pressed/resting bounds — original owner added face, inner-rim and symmetric outer-rim gravity-rest cases plus free-fall control. Both intent and verification reviewers independently accepted the final assertions. No production change or tolerance relaxation.

Original triage retains every finding: 3 high and 6 medium repaired; 4 false rejected for their recorded reasons; 5 maybe-false numerical observations retained with strengthened scoped evidence. The follow-up closes the remaining medium proof gap. All confirmed required-now findings are resolved. Follow-up review recommendation: **false**; prior high repairs and the shared numerical change have already received independent re-review, with no remaining specific unverified required-now risk.

**Final verification identity and reuse:** baseline `4a272e4bb5dcd36b446769120c6be260f5cc9330`. Matching solution build `dotnet build CuriousContraptions.slnx -c Debug --nologo -v quiet`: 0 warnings/errors, 7.30s, session 48579. Full solution command recorded above: **4,224 passed, 0 failed, 0 skipped**, main 8m35s, session 55049. Only four tests and review/status prose changed afterward; matching build and the 20-test run (session 78583) cover those additions. Reviewers accepted relevant-input reuse; the full result does not claim it executed the four new cases. Exact-SHA CI will execute all 4,228 tests, subject to its actual reported result. Earlier failed attempts remain recorded, including the unchanged Weight bound and mistaken restarted test-helper run.

**Residual scope:** browser qualification, detailed performance/device/global numerical qualification and the named dynamic multi-collider consumers remain pending at their established gates. No local Release worker build, web publish, browser test or mutation run was performed. Unknown blocked time remains unknown; recorded full-test durations are retained without extrapolated metrics.

### Publication-dependent terminal checks
- SnapshotApproval: `/root/hollow_verification_r0` passed committed SHA `7cc57c2e2031ee4f36c2695560894d8a85cae688`, verifying a clean tree and all 71 per-file diffs against the r3 snapshot. The subsequent CI-input repair requires fresh committed-SHA approval before its publication.
- Exact-SHA CI [40236967750](https://github.com/aidanmorgan/tim/actions/runs/40236967750) on `7cc57c2e2031ee4f36c2695560894d8a85cae688`: **Fail**, main 3,719 passed / 1 failed / 0 skipped (3,720 total), 16m49s. `EveryLoggedCompileReceivedThePinnedCompilerInputs` correctly rejected 30 reference-pack analyzer inputs from 10.0.14. Tools, publish and deployment never ran; no Pages artifact or exact-SHA deployment exists.

### Same-owner CI provenance repair
- Cause: CI's unversioned `dotnet workload install wasm-tools` checked for an updated workload set and selected 10.0.403 / WebAssembly and Mono packs 10.0.14. Prior green [CI 40211261106](https://github.com/aidanmorgan/tim/actions/runs/40211261106) used the same setup SDK 10.0.401 but explicitly logged workload set **10.0.402.1** / packs **10.0.13**. This is dependency-resolution drift, not a hollow-physics failure.
- Correction: CI now installs `wasm-tools --version 10.0.402.1`. The global.json comment honestly describes its existing SDK feature-band roll-forward; its actual SDK configuration is unchanged. Analyzer pins, all compiler assertions, game code and physics bounds are unchanged. Official [workload-set documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-sets) confirms `install --version` selects and pins that workload set; local CLI help exposes the option. No local workload installation, Release worker build or web publish was performed.
- Affected check: `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~CiKeepsTheSameRestoreAndBuilds|FullyQualifiedName~OnlyMainDeploys' --nologo -v minimal`: **12 passed, 0 failed, 0 skipped**, 98ms, session 62008. Reused the unchanged compiled tests; they read current workflow protections. This does not claim Linux workload installation or compiler-provenance runtime verification.
- Owner: original hollow implementation owner; candidate frozen for scoped independent build-input review. Retry condition: normal-hook commit, committed-SHA approval, then fresh exact-SHA CI resolving the approved compiler inputs with every required test passing, followed by Pages artifact and deployment. Prior unchanged physics proof remains applicable; failed publication is not accepted as terminal delivery.
- Scoped repair re-review: `/root/hollow_verification_r0` independently confirmed both CI workload histories and official `--version` semantics; Pass with unchanged physics proof applicable. `/root/hollow_forward_r0` confirmed one unconditional pinned installation, no alternate path; Pass. Twelve workflow guards passed but do not establish Linux workload provenance. The coordinator corrected the prior SnapshotApproval record; next is a normal-hook commit and new committed-SHA approval. No new unresolved finding or follow-up review recommendation.
