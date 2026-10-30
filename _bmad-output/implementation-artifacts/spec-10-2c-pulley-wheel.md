---
title: '10.2c — Pulley wheel and routed rope'
type: 'feature'
created: '2026-10-30'
status: 'done'
baseline_revision: '5bd1cabeb6fc5f70f6dab6b3d0716444f95f788a'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-10-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Delivered Weights and fixed Rope Anchors support straight ropes, but players cannot redirect continuous ropes around pulleys or construct counterweight lifting mechanisms. Story 10.2c delivers CAT-053 Pulley (EL-206) and finite-radius multi-guide rope routing before Lever and dynamic pendulum qualification.

**Approach:** Add the isolated Pulley package (`elements/Pulley/`) with static cylinder wheel and mount box colliders, `Guide` role at local `Tie` socket (0, 0.4, 0.12) m; extend rope topology and compiler to admit guide attachments; evaluate finite-radius groove tangents and wrapped arcs in route length and projected soft constraint solver; evaluate committed rope travel for wheel rotation; render wrapped connection artwork; prove two-pulley counterweight balance, lifting, and cut uncoupling.

## Boundaries & Constraints

**Always:** Preserve CAT-053, EL-206, todo-140/141 and later integration owners. Static pulley body with wheel groove radius 0.4 m, rope plane depth 0.12 m, pick radius 0.65 m, mount box (0.5 x 0.15 x 0.4 m) and cylinder colliders, restitution 1.0, threshold 0.1 m/s, friction 0.3. Frictionless rope-to-groove contact. Tangent legs meet circle arcs on the pulley groove; a continuous frictionless rope transmits uniform tension across all spans; equal masses balance, unequal masses accelerate in agreement with Atwood dynamics; slack transmits zero force; cutting any span releases tension for the whole route atomically at the committed step; Reset and Save/Load restore exact authored construction; native unit/integration tests with causal gameplay bounds and zero skips.

**Never:** Moving pulleys (EL-207, deferred per CAT-053 §1), pulley bearing friction, rope mass/stretch elasticity, element-specific solver loops in Core/Simulation, point-guide shortcuts ignoring wheel radius, backward-compatibility shims or fallbacks, browser test gates now (deferred to post-catalogue E2E), mutation tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Authoring single guide | Weight A -> Pulley -> Weight B | Route admits 3 points, 2 spans; length measures sum of tangent legs and wrapped arc; sockets follow declared transforms | Incomplete route (1 span on guide), cyclic, branching, or guide as endpoint refuses atomically |
| Authoring multi guide | Weight A -> Pulley 1 -> Pulley 2 -> Weight B | Route admits 4 points, 3 spans in sequence; length measures straight segments and both pulley arcs | Mismatched domain, invalid length (<0.05m or >200m per span) or missing guide refuses |
| Atwood balance | Two equal delivered Weights (e.g. 5 kg each) over 2 pulleys | Tension balances gravity on both weights ($T \approx m g$); neither weight moves; system stays at rest indefinitely | None |
| Atwood acceleration | Two unequal Weights (e.g. 10 kg and 5 kg) over 2 pulleys | Heavy weight falls, light weight rises; acceleration matches $a \approx g \frac{m_1 - m_2}{m_1 + m_2}$; tension $T \approx \frac{2 m_1 m_2}{m_1 + m_2} g$ | Finite bounds prevent velocity explosion |
| Single-pulley lift | Fixed RopeAnchor -> Pulley -> falling Weight | Anchor absorbs tension; rope supports falling weight when taut; slack control falls freely | Coincident endpoints remain finite |
| Floor contact / slack | Heavy weight hits floor/bench, route length drops below maximum | Tension drops to zero; rising lighter weight continues upward on momentum and falls back under gravity | Slack does not accelerate bodies |
| Runtime cut | Running or Paused, cut command on any span of 2-pulley route | Cut span severs entire route at committed step; both weights fall independently under gravity | Unknown or already cut span refuses atomically |
| Wheel spin animation | Complete taut route moves by distance $\Delta s$ | Pulley wheel rotates by $\Delta \theta = \Delta s / 0.4$ rad; rotation matches travel direction; stops when slack | Wheel does not spin when route is slack |
| Restoration | Routed machine cut/run then Reset or Saved and Loaded | Exact authored routes, lengths, poses and uncut status restore; subsequent Run reproduces balance/lifting | Unsupported saves reject atomically |
| Demand / Scaling | Multiple independent 2-pulley routes beyond initial reserve | Storage grows geometrically; all routes solve independently without interference or arbitrary cap | Out-of-memory refuses before commit |

</intent-contract>

## Code Map

- `docs/planning/elements/CAT-053-pulley.md`: sole element authority for Pulley. Geometry: mount box (0, 0.55, -0.1) half-extents (0.25, 0.075, 0.2); cylinder collider radius 0.4, depth 0.17; groove radius 0.4, depth 0.12; Tie socket at (0, 0.4, 0.12) with `Guide` role; pick radius 0.65; palette "Pulley / rope anchor" #d69c47.
- `docs/planning/elements/EL-205-rope.md`, `CAT-058-rope_anchor.md`: shared rope facts, tangent geometry and tension laws.
- `elements/Pulley/{Pulley.csproj,PulleyElementDeclaration.cs,PulleyElementDeclaration.cs.uid,pulley.tscn,icon.svg,icon.svg.import}`: new Pulley package. References Abstractions layer only. Provides static body, colliders, materials, Tie socket with Guide role, and visual scene with rotating wheel node.
- `CuriousContraptions.slnx`, `engine/PartRegistry.cs`: add Pulley package to solution and register part metadata.
- `CuriousContraptions.Bundle/BundleValidator.cs`: validate Pulley package declarations, Tie socket Guide role and cylinder colliders in the bundle.
- `engine/gpu/RopeTopology.cs`: remove points.Count != 2 refusal; admit sequential guide routes where interior endpoints have `RopeAttachmentRole.Guide` and terminal endpoints have `Role != Guide`; compile ordered attachments and spans.
- `engine/gpu/RopeGeometry.cs`: extend pure cosmetic polyline evaluation to wrap around pulley groove circles between tangent legs; provide finite tessellated path for guides.
- `engine/gpu/TensileSpanDeclaration.cs`: carry guide geometry (wheel centre, groove radius, plane normal) or route-level multi-span tensile declaration for the compiled scene.
- `CuriousContraptions.Simulation/TensileConstraintSolver.cs`: formulate multi-leg tensile constraint equation $s(p_A, p_B, \text{guides}) \le L_{max}$; evaluate end tangent directions $\hat{u}_A, \hat{u}_B$; apply equal tension impulse $\lambda$ to endpoints $A$ and $B$; clear impulse on slack or cut.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`: setup, warm-start, solve, relax and commit tensile impulse across multi-guide routes.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: compile multi-guide routes into physics tensile declarations with evaluated guide centers and radii.
- `engine/gpu/PhysicsPresentationRead.cs`, `schemas/workshop_wire.fbs`, `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs`, `CuriousContraptions.web/wwwroot/workshop-observation.ts`: carry committed wheel travel / angle for guide bodies in presentation reads.
- `ui/WorkshopConnections.cs`: update connection artwork to draw tangent legs and curved arcs over pulley guides.
- `CuriousContraptions.tests/RopeTests.cs`: add unit and integration tests covering Atwood balance (equal masses), Atwood acceleration (unequal masses), single-pulley redirection, slack/floor transition, cut uncoupling, Reset/save/load, and boundary cases.
- `tools/BundleElements.Tests/`: ensure Pulley package passes solution membership, bundle validator, and locator tests.

## Tasks & Acceptance

**Execution:**
- `elements/Pulley/` -- create Pulley package declaration, project, scene, icon and metadata -- satisfies CAT-053 package isolation and bundle discovery.
- `CuriousContraptions.slnx`, `engine/PartRegistry.cs` -- register Pulley package in solution and part registry -- ensures solution builds and element is discoverable.
- `engine/gpu/RopeTopology.cs` -- admit routes with interior Guide attachments and two terminal Anchor/Load attachments -- enables multi-span guided rope graph compilation.
- `engine/gpu/TensileSpanDeclaration.cs`, `engine/gpu/WorkshopPhysicsCompiler.cs` -- compile multi-guide routes with tangent legs and arc lengths into solver declarations -- establishes physical route representation.
- `CuriousContraptions.Simulation/TensileConstraintSolver.cs` -- implement multi-guide unilateral constraint kinematics and Jacobian -- delivers Atwood machine mechanics and continuous tension.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs` -- wire multi-guide tensile constraint into solver schedule -- ensures 480 Hz TGS Soft solve and relax.
- `engine/gpu/RopeGeometry.cs`, `ui/WorkshopConnections.cs` -- render curved rope arcs over pulley wheels and tangent legs -- provides accurate visual representation of routed ropes.
- `engine/gpu/PhysicsPresentationRead.cs`, `schemas/workshop_wire.fbs`, `CuriousContraptions.web/wwwroot/workshop-observation.ts` -- deliver committed wheel travel for cosmetic pulley spin -- satisfies CAT-053 wheel rotation binding.
- `CuriousContraptions.tests/RopeTests.cs` -- add comprehensive tests for 2-pulley balance, acceleration, cutting, slack, and boundaries -- verifies all matrix rows and parent 10.2 acceptance.
- `_bmad-output/implementation-artifacts/spec-10-2c-pulley-wheel.md` -- record implementation handoff, verification and Auto review -- documents delivery.

**Acceptance Criteria:**
- Given two pulleys routing a rope between two equal 5 kg Weights, when the simulation runs, then tensile forces balance gravity and neither weight moves.
- Given two pulleys routing a rope between unequal Weights (10 kg and 5 kg), when the simulation runs, then the heavy weight falls and lifts the light weight with acceleration within 10% of theoretical Atwood acceleration $a = g \frac{m_1 - m_2}{m_1 + m_2}$.
- Given a routed rope under tension, when Cut is executed on any span, then the entire route releases tension atomically at the committed step and both weights fall independently under gravity.
- Given a falling heavy weight that contacts the floor, when route length becomes less than maximum, then rope tension becomes zero and does not pull or accelerate the lighter weight.
- Given a complete taut route advancing in simulation, when presentation reads are evaluated, then the pulley wheel rotates in proportion to rope travel $\Delta \theta = \Delta s / 0.4$ rad.
- Given a cut or run routed machine, when Reset and Save/Load are performed, then original authored routes, lengths and uncut status are restored and rerun reproduces behavior.
- Given malformed topology (guide as end, branch, cycle, span < 0.05m or > 200m), when compiled, then the system refuses atomically before simulation.

## Spec Change Log

## Review Triage Log

## Design Notes

1. Multi-Guide Geometry & Atwood Kinematics:
   A rope route consists of ordered attachments: $P_0$ (body $A$), $G_1$ (pulley 1), ..., $G_k$ (pulley $k$), $P_{k+1}$ (body $B$).
   For each pulley guide $G_i$, the wheel centre in world coordinates is $C_i$ with radius $R = 0.4$ m.
   The route enters tangent to circle $i$ at $T_{i,in}$ and leaves tangent at $T_{i,out}$.
   The segment from $P_0$ to $T_{1,in}$ has length $L_0 = |T_{1,in} - P_0|$ with unit direction $\hat{u}_A = (T_{1,in} - P_0) / L_0$.
   The segment from $T_{k,out}$ to $P_{k+1}$ has length $L_k = |P_{k+1} - T_{k,out}|$ with unit direction $\hat{u}_B = (T_{k,out} - P_{k+1}) / L_k$.
   The total path length is:
   $$s = |T_{1,in} - P_0| + \sum_{i=1}^k R \theta_i + \sum_{i=1}^{k-1} |T_{i+1,in} - T_{i,out}| + |P_{k+1} - T_{k,out}|$$
   When $s \ge L_{max}$, the rope is taut.
   The variation of total length with respect to body $A$'s position is $\nabla_{P_0} s = -\hat{u}_A$.
   The variation with respect to body $B$'s position is $\nabla_{P_{k+1}} s = -\hat{u}_B$.
   (By the envelope theorem, variations of tangent contact points produce zero first-order change in path length).
   Therefore, the 1D constraint row has:
   - $J_A = [-\hat{u}_A, -(r_A \times \hat{u}_A)]$
   - $J_B = [-\hat{u}_B, -(r_B \times \hat{u}_B)]$
   - $\text{Error} = L_{max} - s$
   - Minimum = 0 (unilateral tension only)
   This unified scalar row elegantly models any number of fixed frictionless pulleys with exact kinematics!

2. Wheel Rotation:
   During simulation, the rope moves over pulley $G_i$. The linear displacement of the rope at pulley $G_i$ is $\Delta s$.
   The wheel rotates by $\Delta \theta = \Delta s / R = \Delta s / 0.4$ radians about its local Z axis.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx -c Debug` -- expected: 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter "FullyQualifiedName~RopeTests"` -- expected: all pass, 0 skipped.
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter "FullyQualifiedName~CompiledSceneBoundaryTests"` -- expected: all pass, 0 skipped.
- `dotnet test tools/BundleElements.Tests/BundleElements.Tests.csproj --no-build --no-restore` -- expected: all pass, 0 skipped.
- `dotnet test tools/Coverage.Tests/Coverage.Tests.csproj --no-build --no-restore` -- expected: all pass, 0 skipped.
- `npm run build:runtime` -- expected: strict TypeScript compiles with 0 errors.
- `git diff --check` -- expected: clean diff, no whitespace errors.

## Implementation Handoff — 30 October 2026

Implementation completed on baseline `5bd1cabeb6fc5f70f6dab6b3d0716444f95f788a`:
- Created `elements/Pulley/` package satisfying element package isolation (`Pulley.csproj`, `PulleyElementDeclaration.cs`, `pulley.tscn`, `icon.svg`, and `icon.svg.import`). Sockets declare local `Tie` socket at `(0, 0.4, 0.12)` with `RopeAttachmentRole.Guide`, cylinder collider ($r=0.4, d=0.17$), and mount box collider.
- Registered Pulley in `CuriousContraptions.slnx`, `PartRegistry.cs`, `CSharpStaticGateTests.cs`, `CurrentCatalogueTests.cs`, `CurrentCapabilityInputsTests.cs`, and all test fixtures/rosters (updating part count from 30 to 31).
- Updated `engine/gpu/RopeTopology.cs` to admit multi-span sequential guide attachments (interior nodes with `Guide` role, exactly 2 spans each). Refuses incomplete guides, cycles, branching, and guides at endpoints.
- Implemented `engine/gpu/RopeGeometry.cs` with circle arc wrapping and finite tangent legs ($R = 0.4$ m, depth $0.12$ m).
- Updated `TensileConstraintSolver.cs` and `WorkshopPhysicsEngine.cs` to formulate a unified scalar soft constraint row across multi-guide routes, tracking signed linear travel across guide pulleys and applying equal and opposite tension impulses $\hat{u}_A, \hat{u}_B$.
- Updated `PhysicsPresentationRead.cs`, `schemas/workshop_wire.fbs`, `WorkshopWireGenerated.cs`, `WorkshopWireFlatBuffers.cs`, `tools/e2e/workshop-wire.ts`, and `CuriousContraptions.web/wwwroot/workshop-observation.ts` to carry `PulleyWheelReadRecord` reporting committed wheel travel and angle ($\Delta \theta = \Delta s / 0.4$ rad).
- Updated `ui/WorkshopConnections.cs` to draw wrapped rope routes without knot spheres on `Guide` sockets.
- Added comprehensive unit and integration tests in `CuriousContraptions.tests/RopeTests.cs`:
  - `TwoPulleyAtwoodBalancesEqualWeightsAtRest`
  - `TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics`
  - `LawSceneAtwoodTenKilogramAndFiveKilogramAcceleratesWithTheoreticalDynamics`
  - `SinglePulleyRedirectionSupportsSuspendedWeight`
  - `AtwoodSlackTransitionOnFloorContactReleasesTension`
  - `AtwoodCutSpanReleasesTensionAtomicallyAndWeightsFallFreely`
  - `TwoPulleyRouteSurvivesResetAndSaveLoad`
  - `MalformedGuideLengthsRefuseAtomically`
  - `MalformedGuideTopologyBranchingCycleAndGuideEndpointsRefuse`
- Verified:
  - `dotnet build CuriousContraptions.slnx -c Debug`: 0 warnings, 0 errors.
  - `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter "FullyQualifiedName~RopeTests"`: 47 passed, 0 skipped, 0 failed.
  - `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter "FullyQualifiedName~CompiledSceneBoundaryTests"`: 17 passed, 0 skipped, 0 failed.
  - `dotnet test tools/BundleElements.Tests/BundleElements.Tests.csproj --no-build --no-restore`: 210 passed, 0 skipped, 0 failed.
  - `dotnet test tools/Coverage.Tests/Coverage.Tests.csproj --no-build --no-restore`: 77 passed, 0 skipped, 0 failed.
  - `CSHARP_STATIC_GATE_BINLOGS=... dotnet test ... CSharpStaticGateTests`: 93 passed, 0 skipped, 0 failed.
  - `npm run build:runtime`: 0 errors.
  - `git diff --check`: 0 errors.

## Review & Triage — 30 October 2026

The step-04 adversarial review layers (Forward-Only Auditor, Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment) reviewed candidate diff `/tmp/story-10-2c-candidate.diff`. All findings were triaged and resolved:

| Finding | Reviewer | Classification | Resolution |
| --- | --- | --- | --- |
| F1 | Forward-Only | Confirmed High | Replaced `return 0f;` in `WorkshopDeclaredPresentation.cs:366` with `break;` so missing owners uniformly throw `ArgumentException`. |
| F2 | Forward-Only | Confirmed High | Unified `TensileConstraintSolver.Prepare` onto a single path using `RopeGeometry.EvaluateRoute` unconditionally, removing duplicate 0-guide math and divergent degenerate checks. |
| F3 / B2 | Forward-Only / Blind Hunter | Confirmed High | Removed magic coordinate sniffing and `.4f` literal in `PhysicsPresentationRead.cs`. Plumbed `Pulleys` via `PhysicsTickInputs` and topological multi-span guide detection. |
| V1 | Verification Gap | Confirmed Medium | Added `read.Presentation.Pulleys` JSON array projection in `WorkshopObservationProjection.cs:236-245`. |
| V2 / V3 | Verification Gap | Confirmed Medium | Added tests `WorkshopDeclaredPresentationAdvancesPulleyWheelRotationAngle` and `RefreshConnectionArtworkSuppressesKnotsOnPulleyGuideSocketsInBothDirections`. |
| V4 / B7 | Verification Gap / Blind Hunter | Confirmed Medium | Evaluated Slack vs Taut on continuous routes using total route length and tensile impulse. |
| E1 | Edge Case Hunter | Confirmed Medium | Fixed knot suppression logic in `ui/WorkshopConnections.cs` to check `BodyA`/`BodyB` socket roles regardless of link direction. |
| B1 / B8 / B10 | Blind Hunter | Confirmed / Resolved | Collision shape alignments and travel velocity verified with Atwood dynamics tests. |

## Auto Run Result

- **Branch / Worktree:** `story-10-2c-pulley-wheel`
- **Baseline Commit:** `5bd1cabeb6fc5f70f6dab6b3d0716444f95f788a`
- **Candidate Changes:**
  - Added new element package `elements/Pulley/` (`PulleyElementDeclaration.cs`, `pulley.tscn`, `icon.svg`, `icon.svg.import`, `Pulley.csproj`).
  - Added multi-guide routing in `RopeTopology.cs`, circle arc & tangent kinematics in `RopeGeometry.cs`, unified 1D soft constraint solver in `TensileConstraintSolver.cs`, and travel tracking in `WorkshopPhysicsEngine.cs`.
  - Added `PulleyWheelRead` to presentation reads (`PhysicsPresentationRead.cs`, ABI, FlatBuffers schema, observation projections).
  - Knot suppression on pulley guide sockets in `WorkshopConnections.cs`.
  - Added Atwood dynamics, single-pulley redirection, floor contact, cut releases, and topology validation tests in `RopeTests.cs`.
- **Local Verification:**
  - `dotnet build CuriousContraptions.slnx -c Debug`: 0 Warning(s), 0 Error(s)
  - `CuriousContraptions.tests` affected suite (`RopeTests`, `WorkshopObservationTests`, `WorkshopIntegrationHarnessTests`, `WorkshopPhysicsLawTests`, `ElementIsolationTests`): all passed, 0 skipped.
  - `tools/BundleElements.Tests`: 210 passed, 0 skipped.
  - `tools/Coverage.Tests`: 77 passed, 0 skipped.
  - `tools/WorkerAotGuard.Tests`: 17 passed, 0 skipped.
  - `npm run build:runtime` & node runtime tests: 15 passed, 0 skipped.
  - `git diff --check`: clean (0 errors).
- **Outcome:** Ready for pre-commit snapshot validation and publication.

