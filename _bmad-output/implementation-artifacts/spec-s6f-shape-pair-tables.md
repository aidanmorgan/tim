---
title: 'Table-driven collider shape routes and the narrowphase pair table (S6f)'
type: 'refactor'
created: '2026-11-23'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/gpu-f32-physics.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s6b-response-codec-read-family-table.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** A collider shape is routed by hand through 18 switches in 12 production files (the 24 November re-run at `25df3cd5`; the fifth audit counted 19 with `tools/BundleElements`, which refuses by string and stays): bundle geometry, placement rest, material surface and support, passage sensing, cylindrical containment, the hollow contact query, declared physics, and in the simulation the narrowphase pair dispatch (`WorkshopCollisionKernels.cs:2009`) and the hollow kernels. Several of those members sit at or near the gate's limit (`HollowContactQuery.SurfaceTangent` 25 and `SurfaceWitnesses` 25, `MaterialSurface.Point` 23, `PassageSensorDeclaration.Create` 23, `HollowGeometry.FeatureProjections` 22, `GenerateManifold` 21), and each new shape adds about eight arms to the pair dispatch. The private placement enum `PlacementPose.Shape` is routed the same way in five members. `ENGINE-PROFILE-COLLIDER` and the fluid engines add values to these enums.

**Approach:** Per enum, one static table keyed by it, at each layer on the enum that layer already uses: a shape table of geometry functions (bounds, support, material point, containment, passage, surface tangent and witnesses, feature projections) keyed by `ColliderShapeKind`, a table keyed by the placement enum `Shape`, and a narrowphase pair table of contact functions keyed by the simulation's `ShapeKind` pair, the dispatch Box2D v3 uses. The `IsHollow` pre-branch of the pair dispatch stays ahead of the table, so the table holds the 16 solid cells; each cell is `{ function, swapped }` and the call passes `(colB, colA)` when swapped, so contact orientation (`ColA`, `ColB`) is as today. Contact results, motion and refusals are unchanged.

## Boundaries & Constraints

**Always:**
- Every value of `ColliderShapeKind` (9 in the declaration enum `CuriousContraptions.Abstractions/ColliderDescriptor.cs:8-19`; 7 in the physics enum `PhysicsDeclarationValues.cs:12`), `ShapeKind` (7, `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:9-18`) and `PlacementPose.Shape` (4) has exactly one row in its table; every ordered pair of the 7 `ShapeKind` values has exactly one cell: one of the 16 solid cells (15 contact functions, six of them swapped: `(Plane, Sphere)`, `(Sphere, Box)`, `(Plane, Box)`, `(Plane, Cylinder)`, `(Sphere, Cylinder)`, `(Box, Cylinder)`, and the no-contact cell `(Plane, Plane)`), or the hollow route (the 33 pairs with a hollow side, `CollideHollow(partner, hollow)`, the hollow side being `colA` when both are hollow and otherwise the hollow one). An undefined `ShapeKind` is refused at ingest (`AnUndefinedShapeAtIngestIsATypedInvalidDeclarationThatChangesNothingElse`, `WorkshopPhysicsLawCylinderTests.cs:129`) before the table and never reaches it; the lookup adds no default cell, and an index out of range is unreachable by that refusal.
- The declaration enum's `Plane`, `Capsule` and `Mesh` keep `DeclaredPhysics.Admit`'s refusal (`ArgumentException("Unsupported baked collider shape.")`, `engine/gpu/DeclaredPhysics.cs:81`, existing check `WorkshopPhysicsLawCylinderTests.cs:121`); the physics enum's undefined value keeps `PhysicsDeclarationValues.ValidateShape`'s `ArgumentException("Unsupported shape.")`; the extractor's string refusals (`tools/BundleElements/SceneGeometryExtractor.cs:108-111`) are unchanged.
- Contacts, rest, motion, sensors and goals are unchanged for every delivered package and lesson within the game-grade envelope; the rolling and support formulas (`RollingGeometry`, `SupportPoint`, `ColliderExtent`) are untouched and only their selection by shape is by row.
- Contact orientation follows collider index as today (`WorkshopPhysicsEngine.cs:870-877`, `PrepareContacts :1103`: `colA.Index < colB.Index`), so the same two bodies reach different cells depending on declaration order; both orders are tested.
- The narrowphase table is a 4 x 4 array of static method-group delegates over `(SimCollider, SimCollider, float, List<ContactPoint>)`, built once. Delegates, not function pointers: no unsafe code (the Simulation project allows it, `CuriousContraptions.Simulation.csproj:6`, but the boring choice is verifiable under the trimmed worker); the delegate invocation on the per-pair path is inside AC4's measurement, and a regression there goes back to the coordinator, who may then choose function pointers. The per-pair path allocates nothing new, has no virtual dispatch beyond that invocation and no reflection (trimmed, ahead-of-time simulation worker).
- Tables are built once from static data and functions; nothing uses runtime enum enumeration in production.

**Never:**
- Merging the two `ColliderShapeKind` declarations (`CuriousContraptions.Abstractions/ColliderDescriptor.cs:8`, `PhysicsDeclarationValues.cs:12`; audit duplicate-enum finding) or adding a shape: owned by `ENGINE-CAPABILITY-INSTANCES` and `ENGINE-PROFILE-COLLIDER`.
- A change to any contact, support or geometry function body, to solver iteration, to the wire schema, or to element packages; to `PlacementPose.Rest` (`:118-179`, complexity 25 from loops and nested ifs, no shape chain), `SceneGeometryExtractor.Bake` and `HollowContactQuery.CollideBoxHollow` (which compares the private `HollowFeature` enum and one `Kind != Tube`), none of which this slice changes.
- A second dispatch path; a default row; a test-only accessor; suppressions; hashes or trajectory pins; mutants; browser runs.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| F1 | For each of the 16 ordered solid cells `(A, B)` of `Sphere`, `Box`, `Plane` and `Cylinder`, two `SimCollider`s built as `WorkshopPhysicsLawCylinderTests.Collider` builds them (`:945`, no production code copied; its `index` parameter is 0 for `colA` and 1 for `colB`, and the index decides which collider is `colA` in the engine), with the lower collider's top surface point on a vertical line through the upper collider's centre and the upper collider's lowest point `d = 0.002 m` above it (inside the speculative margin), in both vertical arrangements where either body may be the upper (a plane is only ever the lower, so the six cells with a plane have one arrangement and `(Plane, Plane)` none); `WorkshopCollisionKernels.GenerateManifold` (as `WorkshopPhysicsLawCylinderTests.Contacts` at `:966` already calls it) | At least one contact, and every contact's `Gap` equals `d` within `1e-4` (the existing `Gaps` bound, `:973-980`); `(Plane, Plane)` returns none; the expected `d` is the vertical distance between support points, computed in the test from the declared dimensions, independent of the production geometry | No error expected |
| F1e | Through `LawScene` (`WorkshopPhysicsLawTests.cs`: `Sphere`, `Box`, `Cylinder`, `StaticBox`, `StaticSphere`, and the plane in its constructor; `Admit()` through `PhysicsSceneDeclaration` and `PhysicsGpuAbi.Admission`): a dynamic sphere, box and cylinder over each of the floor plane, a `StaticBox` and a `StaticSphere` (9 cases, released 0.05 m above the support's top), then `world.Step(world.TicksPerSecond)`; the hollow route's evidence is the existing `WorkshopPhysicsLawHollowTests` (hollow against sphere, box and cylinder, in both orders) | The dynamic body's lowest point never goes below the static top by more than 0.01 m while over its footprint (the existing resting bounds, `WorkshopPhysicsLawHollowTests.cs:25-29`) | No error expected |
| F2 | For each of the 7 physics `ColliderShapeKind` values, a collider of known size (at least 0.05 m in every dimension, and the smallest admitted sphere radius 1/32 m): its bounds (`ColliderExtent`, `HollowGeometry.Bounds`) and, for the six kinds a delivered package declares (the bundle declares Box, Cylinder, Frustum, Sphere, Torus and Tube), its material surface point (`MaterialSurface.Point`) and its placement rest height, through the first registered part whose bundle entry declares a collider of the kind (the case fails when none does); `Plane` is the scene floor, `DeclaredPhysics.Admit` refuses it and no delivered package declares one, so only its bounds apply | Each equals its analytic value within `PhysicalTolerance.ContactSeparationMetres` (0.2 mm), the contact allowance of the engine: a wrong row misses by at least a collider dimension, 250 times the tolerance at 0.05 m and 156 times at the sphere minimum | No error expected |
| F3 | The declaration enum's `Plane`, `Capsule` and `Mesh` through `DeclaredPhysics.Admit`; an undefined physics shape through `PhysicsDeclarationValues.ValidateShape` | Refused | `ArgumentException` "Unsupported baked collider shape." and "Unsupported shape."; control: a cylinder of the same size admitted |
| F4 | `GenerateManifold` for every solid pair (the 16 cells) with a pre-sized list, 1,000 calls | `GC.GetAllocatedBytesForCurrentThread()` is unchanged across the calls (the table lookup allocates nothing) | No error expected |
| F5 | For each `PlacementPose.Shape` value (sphere, box, cylinder, hollow), a part of that placement shape placed on a surface (`PlacementPoseTests`; the case fails when no delivered part has the shape) | Its rest height, support distance and overlap are as today | `PlacementRefusal` as today |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work and the slices before this one; scope guards compare against it. Drafted on `25df3cd5`, the drafting revision only, not a baseline; every line cited here was read there on 24 November 2026. **Status: draft; runs before the engine programme** (owner decision 24h, 23 November 2026). Coordinator decision 42 (24 November 2026) splits the former S6f at the shape and mechanical boundary: this slice keeps the shapes and the shape-pair table; the mechanical kinds are S6g (`spec-s6g-mechanical-kind-tables.md`), which closes S6. Prerequisites: S6e published (S6c and S6e both edit `BundleValidator.cs`, and S6e edits `WorkshopWireFlatBuffers.Compiled.cs`). Review tier: **engine**. Delivery shape: one structural slice, the shape rows of the S6 follow-on inventory (`spec-s6b-response-codec-read-family-table.md`).

Planning verdict: replanned at dispatch from the main that contains the previous S6 slice (coordinator decision 43); sixth-audit findings in `docs/planning/elements/readiness/audits/audit-s6f-6.md` carried into that replanning.

Owned scope (complexity at threshold 12, 23 November 2026, in brackets):
- Shapes, declaration side (keyed by `ColliderShapeKind`): `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:218` `ValidateShape` [20]; `CuriousContraptions.Abstractions/ColliderDescriptor.cs:52` (`Hollow`); `CuriousContraptions.Bundle/BundleGeometry.cs:57`; `engine/PlacementPose.cs:187` (`Pose`, `:181-203`); `engine/gpu/CylindricalContainmentQuery.cs:36`; `engine/gpu/MaterialSupport.cs:19`; `engine/gpu/MaterialSurface.cs:9` (`DerivativeBounds`, 5 members) and `:44` (`Point` [23]); `engine/gpu/PassageSensorDeclaration.cs:61` (`Create` `:31` [23]); `engine/gpu/HollowGeometry.cs:33` (`IsHollow`), `:37`, `:45` (`Bounds`), `:56` (`FeatureProjections` [22]), `:176` (`SmoothClosestWall` [22]), `:376` (`SupportNear`), `:449` (`Rims`); `engine/gpu/HollowContactQuery.cs:44`, `:539`, `:574` (`SurfacePoint`), `:606` (`SurfaceTangent` [25]), `:772` (`SurfaceWitnesses` [25]); `engine/gpu/DeclaredPhysics.cs:81`, `:101`. The Torus-or-not split of formulas and the face count in `SurfaceTangent` and `SurfaceWitnesses` become row functions, which is how they reach AC1. `HollowContactQuery.CollideBoxHollow :386` [23] is not changed.
- The private placement enum `PlacementPose.Shape` (`engine/PlacementPose.cs:62`): `:73` `Reach`, `:80` `Partner`, `:87` `SupportDistance` (a 4-arm switch), `:227` `Vertical`, `:251` `Overlaps` -- a placement-shape table in that file.
- Shapes, simulation side (keyed by `ShapeKind`): `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:277` (`ColliderExtent`), `:862` (`SupportPoint`), `:1999-2028` (the pair dispatch: `GenerateManifold` [21], with the `IsHollow` pre-branch kept ahead of the table); `CuriousContraptions.Simulation/WorkshopHollowKernels.cs:13` (`IsHollow`), `:16`, `:29`, `:159` (the three conversions between `ShapeKind` and the physics `ColliderShapeKind`, whose values coincide, `Sphere` 0 to `Frustum` 6: they become small tables between the two enums, which stay two enums); `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:304` (`RollingGeometry`, 3 members, selection only); `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:806-868` (`PrepareBroadphase`, the plane-bounds `==` ladder over four shapes, per collider per tick; its +Y assumption, audit section 1, is `ENGINE-SPATIAL-QUERIES`'s and is kept).
- Kept as single-member comparisons, recorded in the run log with file:line: `WorkshopPhysicsEngine.cs:825`.
- `CuriousContraptions.tests/ShapeRouteTests.cs` -- new: F1 to F5.
- `CuriousContraptions.Simulation/Program.cs` is untouched; a new file under `engine/gpu/` or `CuriousContraptions.Simulation/` is added to `CuriousContraptions.Core/RuntimeSources.props` (its compile list is explicit; `WorkshopCollisionKernels.cs` is listed at `:158`). The 9-value declaration enum serves Bundle and tools, the 7-value physics enum `engine/gpu`, `ShapeKind` the worker: a Bundle member cannot read an engine table, so each layer keys its own enum.
- `PhysicsDeclarationValues.cs:10-11` -- the comment "static only" on the hollow kinds is stale (dynamic hollow bodies are admitted and tested: `LawScene.DynamicHollow`, `WorkshopPhysicsLawTests.cs:206`, used at `WorkshopPhysicsLawHollowTests.cs:897`, `:1264`, `:1342`, `:1380`, `:1486`, `CompoundMassAndRollingTests.cs:185`, `:210`, `WeightTests.cs:411`); the comment is corrected in this candidate.
- `docs/planning/invest/delivery-batches.md` (story S6), `deferred-work.md`, `TODO.md`, `sprint-status.yaml` (key `s6f-shape-pair-tables`), this contract and its run log.

Direct consumers: every member keeps its signature. `docs/planning/elements/readiness/engine-design-gap.md` sections 1 and 4 and 32 other documents (for example `EL-071`, `CAT-054`) cite `WorkshopCollisionKernels.cs:` lines; they are re-read after publication by the coordinator. The audit's `ENGINE-SPATIAL-QUERIES`, `ENGINE-SOA-SIMD` and `ENGINE-PROFILE-COLLIDER` stories are after this slice; the table is their extension point, but its cells are data that `ENGINE-SOA-SIMD` may replace if it restructures the dispatch: S6f's cells are not a constraint on it. `spec-engine-art-solidity.md` cites the extractor (`SceneGeometryExtractor.cs:108`) and re-reads it.

### Capability probe

All routes exist; nothing in a solver law, the worker ABI, the wire schema or a shared presentation contract changes (dispatch is restructured; every law function body is unchanged).

- **Present:** shape contacts -- `WorkshopPhysicsLawCylinderTests` (the `Contacts` and `Gaps` helpers), `WorkshopPhysicsLawHollowTests` (hollow against sphere, box and cylinder in both orders through `reverseOrder`, `:30-69`), `HollowElementTests`, `HollowBoundsTests`, `MaterialSweepTests`; placement rest -- `PlacementPoseTests`; extraction -- `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`:186`), `BundleValidatorTests` (in `tools/BundleElements.Tests`, which the filter reaches), `SceneGeometryExtractorTests`.
- **Present, untested:** that every ordered solid cell has its contact function in both index orders, and that every value of the shape enums has one row. This slice adds F1 to F5; the existing hollow theories are the *present* evidence for the hollow route, F1 covers the 16 solid cells at the kernel and F1e their end-to-end behaviour.

### Decisions

- **Shapes stay in S6 (decided).** Owner decision 24h names every per-capability chain; a shape-pair function table is the dispatch the stated solver design uses (Box2D v3), so it does not conflict with `ENGINE-SOA-SIMD`.
- **Each layer keys its own enum (decided).** Merging the two `ColliderShapeKind` declarations is a separate forward-only change owned by `ENGINE-CAPABILITY-INSTANCES`.
- **The hollow pre-branch stays ahead of the table (decided).** The table is 4 x 4 solid cells; the hollow route keeps its hollow-side rule. The cell form is `{ function, swapped }` because Box2D v3 registers one function per unordered pair and flips, while here contact orientation must stay as today.
- **Performance (owner decision 3a and coordinator decision 42).** The pair table replaces a dispatch that runs once per candidate pair per tick plus up to four sphere probe calls (`AdvanceSpeculativeSphereContact :1973`). It is not a performance story, and the hot-path rule of the inventory (S6b) applies: before and after, on the F1e scene scaled to 64 dynamic spheres, 64 boxes and 64 cylinders over one plane and 16 static boxes for 1,000 ticks at 120 Hz, the run log records the median `WorkshopPhysicsEngine.Advance` wall time of five runs each; the candidate's median must not exceed the slowest baseline run, or the slice returns to the coordinator before review (no fixed percentage). Allocation is asserted by F4. Trimmed and ahead-of-time safety of the tables (no reflection, no runtime enum enumeration) is by design and review: the local capture builds are Debug without trimming or AOT (`docs/delivery-workflow.md:46-51`) and AGENTS forbids the local Release worker build, so CI publish is the first compile of the trimmed worker and the run log records it unverified locally.

### Concurrency

After S6e; before S6g. No other pre-programme slice touches these files except repair parts A to C, which edit package scenes and `VisibleSolidColliderTests.cs` (read here only as an existing test); S6c edits `BundleValidator.cs` and S6e edits `WorkshopWireFlatBuffers.Compiled.cs` and `BundleValidator.cs`, both published before this slice. The new test file is staged with `git add` before the candidate commit.

## Tasks & Acceptance

**Execution:**
1. `.tools/s6f-evidence/` -- the S6 measurement on the baseline (configuration `CA1502: 12`, judged against 14) -- before; the hot-path measurement of the pair table's reference scene on the baseline.
2. Shape declaration-side files and `PlacementPose.cs` -- the shape geometry tables -- F2, F3, F5.
3. Shape simulation-side files -- the pair table and the conversions -- F1, F1e, F4.
4. `CuriousContraptions.tests/ShapeRouteTests.cs` -- the tests in the table.
5. `.tools/s6f-evidence/`, planning records -- after-measurements; result.

**Acceptance Criteria:**
- AC1: Given the measurement, when it runs on the candidate, then no changed or added member reports CA1502 above its baseline value, and `GenerateManifold`, `MaterialSurface.Point`, `PassageSensorDeclaration.Create`, `HollowContactQuery.SurfaceTangent`, `HollowContactQuery.SurfaceWitnesses` and `HollowGeometry.FeatureProjections` each report at least 3 below their baseline value, or no line; `PlacementPose.Rest`, `SceneGeometryExtractor.Bake` and `HollowContactQuery.CollideBoxHollow` are not changed by this slice.
- AC2: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.
- AC3: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, and the total is the baseline round plus 62 (F1 25: the 9 cells without a plane in two vertical arrangements, the 6 cells with a plane in one, and `(Plane, Plane)`; F1e 9; F2 19: the six declared kinds times three outputs and the plane's bounds; F3 4; F4 1; F5 4), each case named in the run log.
- AC4: Given the hot-path rule, when the pair-table reference scene is measured on the baseline and the candidate, then the candidate's median `Advance` time is not above the slowest baseline run, and the number of contacts at the end of the run is equal (a recorded comparison).

## Spec Change Log

## Review Triage Log

## Design Notes

1. Behaviour and scope: no behaviour change; shape routing becomes data (owner decision 24h).
2. Reusable primitive: one row per kind and one cell per solid shape pair; a new shape adds a row and its pair functions.
3. Declared composition: packages declare shapes; tables key generic enums only.
4. Execution and ownership: same narrowphase order and solver; immutable static tables; the per-pair path allocates nothing (F4).
5. Generality: F1 is a theory over every ordered pair in both orders; an added value without a row or cell fails.
6. Forward-only, deleted: every listed switch and ladder; nothing kept beside the tables.
7. Prerequisites: S6e. E1, E2, E5, E6, E7, E8, E9 and D4, D5, D6 apply; E3, E4 and D7 unaffected.

## Verification

Executor: implementation owner. Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. The measurement runs before the compiler-log captures.

**Commands:**
- The S6 measurement with `CA1502: 12` in its configuration, judged against 14 -- expected before: the bracketed values above; after: AC1.
- The five compiler-log capture commands -- expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` -- expected: AC2.
- The registration check filter (a test file is added) -- expected: 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~ShapeRouteTests|FullyQualifiedName~WorkshopPhysicsLaw|FullyQualifiedName~Hollow|FullyQualifiedName~MaterialSweepTests|FullyQualifiedName~PlacementPoseTests|FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~BundleValidatorTests|FullyQualifiedName~SceneGeometryExtractorTests|FullyQualifiedName~CompoundMassAndRollingTests|FullyQualifiedName~WeightTests"` -- expected: 0 failed, 0 skipped.
- `git add -N` on new files, then `git diff --check <baseline_revision>` -- expected: clean.
- One full local round -- expected: AC3.

**Manual checks (if no CLI):**
- Read the owned diff: every contact and geometry function body is unchanged (moves only); the `IsHollow` pre-branch is ahead of the table; the six swapped cells are exactly those listed.

### Acceptance-to-test table

Tolerances: F1 compares each contact's `Gap` to `d` within `1e-4` (the existing `Gaps` bound; a missing or wrong cell returns no contact or a gap off by at least the dimension of the body, 0.05 m or more); F1e allows the dynamic body's lowest point 0.01 m below the static top while over its footprint (the contact-slop scale), where a missing pair entry lets the body pass through by more than its own size (at least 0.1 m), so the bound discriminates; rest is not asserted because a sphere over a curved support is in unstable equilibrium; F2 uses `PhysicalTolerance.ContactSeparationMetres` (0.2 mm), a wrong row missing by 156 times or more.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| F1 | `ShapeRouteTests.EveryOrderedSolidCellHasItsContactFunction` (theory over the 16 ordered cells and their vertical arrangements) | Control: the same colliders 0.05 m beyond the margin return no contact. Boundary: the two index orders of one unordered pair (for example `(Box, Sphere)` and `(Sphere, Box)`, one of them swapped) return the same contact count and gaps within `1e-4` (symmetry) | planned |
| F1e | `ShapeRouteTests.EverySolidPairArrestsAFallingBody` | Control: the same release clear of the static body passes more than 0.1 m below its top. Boundary: a plane as the static side; a box on a box | planned |
| F2 | `ShapeRouteTests.EveryShapeKindHasItsBoundsRestAndSurfacePoint` | Boundary: the smallest admitted sphere radius 1/32 m | planned |
| F3 | `ShapeRouteTests.ShapesTheEngineDoesNotAdmitAreRefused` (`Plane`, `Capsule`, `Mesh`, an undefined physics shape) | Control: a cylinder proxy of the same size admitted | planned |
| F4 | `ShapeRouteTests.TheSolidPairTableAllocatesNothing` | Control: the same counter around a call that allocates a list reports more than zero bytes | planned |
| F5 | `ShapeRouteTests.EveryPlacementShapeRestsAsBefore` | Control: `PlacementPoseTests` | planned |
| AC1 to AC4 | Measurement, gate, full round, hot-path comparison | Baseline values and the five runs in the run log | planned |
