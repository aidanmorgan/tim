---
title: '10.3a — Lever body-owned port locators and revolute wire transport (Story 10.3 forward fix)'
type: 'bugfix'
created: '2026-10-31'
baseline_revision: 2ee79e8b0a1dce23fed3f2472d7edc40dcd067c6
status: 'done'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/schemas/README.md'
warnings: ['multiple-goals', 'oversized']
deferred:
  - summary: >-
      Contract footprint wording omits revolute constraints.
    evidence: |-
      docs/engine-contracts.md:61 lists bodies, moving bodies and prismatic constraints; ElementFootprint carries RevoluteConstraints since Story 10.3.
    location: >-
      docs/engine-contracts.md:61
    severity: low
  - summary: >-
      SocketWorld places body-owned port endpoints in the part frame, ignoring PortDescriptor.Body.
    evidence: |-
      ui/WorkshopConnections.cs:166-171 passes LocalPosition through part.ToGlobal; latent because today's only body-owned ports are rope ties drawn from committed spans.
    location: >-
      ui/WorkshopConnections.cs:166
    severity: medium
  - summary: >-
      TypeScript wire mirror and SNAPSHOT_SCHEMA revolute entries are verified only by a browser-deferred harness.
    evidence: |-
      tools/e2e/unit/run-reset-snapshot.test.ts:138 checks schema agreement but is listed in tools/E2eRunner/BrowserDeferral.cs:42 until the post-catalogue browser phase.
    location: >-
      tools/e2e/run-reset-snapshot.ts
    severity: low
---

<intent-contract>

## Intent

**Problem:** Published main `2ee79e8b` is red, and the Impact Lever (Story 10.3, CAT-034) does not hinge at runtime. `BundlePortLocatorTests.EveryDeclaredPortAgreesWithItsSceneLocator` reads only root-level `Loc_Port_*` nodes, but the lever's body-owned `TieA`/`TieB` locators sit under `Visual/Beam`. Revolute constraints are compiled but never cross the compiled-scene wire, so every harness and production world runs the lever as a loose plank. The existing lever tests check only the beam's Z angle and payload outcomes, so they cannot see this.

**Approach:** Keep the production contract: a body-owned port's locator is a direct child of `SceneBodies[port.Body].Root`, with `LocalPosition` in that body's frame. Make the port test enforce that contract and replace the package validator's silent out-of-range-body fallback with a refusal. Carry revolutes end to end through `schemas/workshop_wire.fbs`, the regenerated C#, encode/decode and the TypeScript wire mirrors. Prove the hinge with a test that goes red first on a loose beam and runs through the production wire path.

## Boundaries & Constraints

**Always:**
- One current path, with every revolute field carried. Each one decodes into the `RevoluteConstraintDeclaration` the compiler produced: Id, bodies, both frames, limits, stiffness, damping and Collision.
- Invalid decoded revolute rows fail atomically as `WireFaultException(OutOfRange)`, which is the existing constructor-fault path.
- Regenerate `engine/gpu/WorkshopWireGenerated.cs` wholesale with pinned flatc 25.2.10, following `schemas/README.md`. First confirm that the pipeline reproduces the committed file from the unchanged schema.
- Tests use the real production compiler, wire and engine through defined interfaces. Tolerances follow `docs/testing-guidance.md#float-comparison-and-effort-rules`, and each strict assertion names the gameplay failure it excludes.
- The full local round is green with `Skipped: 0` in every project.
- **Owner decision (1 November 2026, resolving the review's intent gap):** land this fix with the frictionless hinge. Reopen Story 10.3's "an equal load placed on both arms remains balanced indefinitely" as a tracked open item. A separate engine slice meets it later with a declared pivot holding mechanism (friction or detent) and re-qualifies the lever tests. In this slice, the heavy equal-Weight pair is asserted over its 3 s play window, and CAT-034 fact 17's 10 s Basketball balance remains asserted. Record the open item in `_bmad-output/implementation-artifacts/deferred-work.md` and as an open-item note under Story 10.3 in `_bmad-output/planning-artifacts/epics.md`, without editing its acceptance text.

**Never:**
- Don't widen `TestBundle.SceneGeometry` to a recursive walk. Eight element tests depend on its root-only dictionary.
- Don't use `GlobalPosition` on an instantiated scene outside a tree.
- No fallback, default body or compatibility path. Don't reuse the prismatic record type for revolutes.
- No hash or byte-identity assertions, no skips, no mutants.
- No browser runs.
- Don't touch `ui/WorkshopConnections.cs`. Its `SocketWorld` ignoring `Body` is recorded as deferred work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Lever hinge holds through the wire | ImpactLever run through `WorkshopIntegrationHarness.Running` under an asymmetric load: a Weight on one beam end, or a rope pull | The beam origin stays at the authored pivot, within a clearance-derived tolerance well below the 0.12 m beam half-thickness. Off-axis (X/Y) tilt stays bounded, and the beam still reaches its ±π/6 stop. The test fails on `2ee79e8b`. | No error expected |
| Revolute round trip | Compiled lever scene, then `EncodeCommand` and `DecodeCommand` | `decoded.Physics.Revolutes` matches the compiled rows field for field | No error expected |
| No-hinge control | Construction without any hinge | Round trip gives empty revolutes, and the other tables are unchanged | No error expected |
| Malformed revolute row | Wire revolute with an invalid limit order or an unadmitted body | Decode refuses, and no scene is produced | `WireFaultException(OutOfRange)` |
| Body-owned locator | ImpactLever `TieA`/`TieB` (`Body` 1) under `Visual/Beam` | Port test passes. Position is compared in the owning body's frame, and the reverse check finds `Loc_Port_*` at any depth. | No error expected |
| Misplaced body-owned locator | Body-owned port whose locator is parented at root, or under the wrong body | Package validator refuses the package with a named reason | Build or validation failure |
| Out-of-range port body | `port.Body` ≥ `SceneBodies.Count` | Package validator refuses it (fallback to "." removed) | Build or validation failure |

</intent-contract>

## Code Map

- `CuriousContraptions.tests/BundlePortsTests.cs:363-392`: the failing test. It compares `port.LocalPosition` with root `child.Position` (line 380, 1e-5). Resolve body-owned ports via `root.GetNode<Node3D>($"{SceneBodies[Body].Root}/Loc_Port_{name}")`, and collect locators at any depth for the reverse check. `TestBundle.SceneGeometry` (lines 28-44) must stay root-only.
- `tools/BundleElements/PackageValidator.cs:45-47,72-86`: the authoritative locator check (parent = body root, local origin). Lines 72-75 silently fall back to "." for an out-of-range Body; change that to a refusal. The runtime `CuriousContraptions.Bundle/BundleValidator.cs:353` already refuses. Update the stale summary.
- `tools/BundleElements.Tests/PortLocatorTests.cs`: add fixtures for a body-owned locator (admitted), a misparented one (refused) and an out-of-range body (refused). Fix the stale summary at lines 6-8.
- Stale contract text to correct:
  - `CuriousContraptions.Abstractions/PortDescriptor.cs:37-39`
  - `docs/general-engine-design.md:77` ("child of the scene root")
  - `docs/engine-contracts.md:62,75,87`
- `elements/ImpactLever/ImpactLeverElementDeclaration.cs:37-69,89-93`: ports at ±1.8 on `PackageBodyIndex(1)`, `SceneBodies [".", "Visual/Beam"]`, hinge frames at origin. `impact_lever.tscn:96-102` holds the locators. Read-only.
- `schemas/workshop_wire.fbs:469-485,633-676`:
  - Add a `CapabilityRevoluteConstraintDeclarationRecord` (same shape as the prismatic record: id, body_a, body_b, frame_a/frame_b as required `CapabilityConstraintFrameRecord`, lower, upper, stiffness, damping, collision).
  - Add `revolutes: [...]` to `CompiledSceneRecord`.
  - There is no format version (`schemas/README.md`). Mid-table insertion has precedent (d2862ebe), so regenerate everything.
- `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs`:
  - Encode is at 712-783; vectors at 726-727 and `Add*` at 768-769.
  - The prismatic record codec pattern is at 405-433.
  - Decode is at 785-827. Pass `revolutes:` by name to the `PhysicsSceneDeclaration` call at 808-810; argument order differs from the schema.
- `engine/gpu/RevoluteConstraint.cs:8-27`: `RevoluteConstraintDeclaration` and `Validate()`. `engine/gpu/PhysicsDeclarations.cs:260-272,385-391`: constructor and revolute validation. Read-only.
- `engine/gpu/WorkshopWireGenerated.cs`: generated. Use the pinned flatc binary at `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/flatc-25.2.10/flatc` (verified `flatc version 25.2.10`). `/opt/homebrew/bin/flatc` is 25.12.19, which is the wrong version.
- `tools/e2e/workshop-wire.ts` (flatc `--ts` output above the marker at line 8285) and `tools/e2e/run-reset-snapshot.ts:92` (`SNAPSHOT_SCHEMA`): regenerate and extend per README. Both are browser-deferred and not run.
- Runtime path, all via the wire:
  - `engine/gpu/BrowserWorkshopClient.cs:186` → `WorkshopCommandRouter.cs:21` → `WorkshopWireFlatBuffers.cs:175`/`:549` → `DecodeCompiledScene`
  - Harness: `CuriousContraptions.tests/WorkshopIntegrationHarness.cs:147,161`
  - Without revolutes, the base/beam no-collide filter (`WorkshopPhysicsEngine.cs:647-650`) is absent. The fulcrum (top y −0.06) and beam (bottom −0.12) overlap by about 6 cm.
- `CuriousContraptions.tests/ImpactLeverTests.cs`: the existing tests (66-390) read only the Z angle (`BeamAngle`), payload and energy. Put the red-first hinge test here, using `WeightTests.Placed` or the rope pattern at 245-288 and `BodyPos(read, beamId)`.
- Round-trip and malformed patterns:
  - `PressurePlateTests.cs:317-330`
  - `DeclaredContactLoadTests.cs:86-92`
  - `CompiledSceneBoundaryTests.cs:89-118`
- `CuriousContraptions.tests/PackageRuntimeParityTests.cs:217`: the hinge set is exactly `["ImpactLever"]`. Read-only.
- `_bmad-output/implementation-artifacts/deferred-work.md`: record that `ui/WorkshopConnections.cs:166-171` `SocketWorld` treats body-owned port positions as part-frame, so lever tie artwork is off the beam when `initial_angle ≠ 0`.

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.tests/ImpactLeverTests.cs`: add the hinge-holds test first, and confirm it fails on the unchanged tree.
- `schemas/workshop_wire.fbs`, `engine/gpu/WorkshopWireGenerated.cs`, `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs`: add the revolute record, regenerate, then encode and decode it.
- `CuriousContraptions.tests/CompiledSceneBoundaryTests.cs`: add the round-trip, no-hinge control and malformed-row tests.
- `tools/e2e/workshop-wire.ts`, `tools/e2e/run-reset-snapshot.ts`: regenerate and extend the mirrors.
- `CuriousContraptions.tests/BundlePortsTests.cs`: enforce the body-owned locator contract.
- `tools/BundleElements/PackageValidator.cs` and `tools/BundleElements.Tests/PortLocatorTests.cs`: refuse an out-of-range body, add the fixtures, and fix the summaries.
- `CuriousContraptions.Abstractions/PortDescriptor.cs`, `docs/general-engine-design.md`, `docs/engine-contracts.md`: correct the contract text.
- `_bmad-output/implementation-artifacts/deferred-work.md`: add the `SocketWorld` entry.

**Acceptance Criteria:**
- Given published main plus this change, when the full local-authority round runs, then every project reports zero failures and `Skipped: 0`.
- Given any package declaring revolutes, when its scene is compiled and run through the production wire, then the running world holds the hinge: base/beam no-collide plus the joint rows. No package declares revolutes that are dropped.

## Design Notes

The Story 10.3 and 10.4 publication receipts recorded normal-hook passes (127/0/0) while the full local round was red. The review record for this slice notes that; it is not a new work item. Story 10.5 Trampoline is blocked until this slice publishes. It also edits `workshop_wire.fbs`, and it resolves that conflict by hand and then regenerates.

## Implementation notes (31 October 2026)

Decisions and scope additions made during implementation, for the review's Intent Alignment layer:

- **Red first.** `ImpactLeverTests.AsymmetricLoad_TurnsTheBeamToItsStopAboutTheAuthoredPivot`, copied unchanged into a throwaway worktree at `2ee79e8b`, fails with "The beam origin left its pivot by 0.6564 m (allowed 0.03 m)". The `BundlePortLocatorTests` failure on `2ee79e8b` is "ImpactLever: no Loc_Port_TieA locator for its declared TieA port."
- **Field position.** `revolutes` is appended as the last `CompiledSceneRecord` field (slot 30) rather than inserted mid-table. The existing malformed-capability tests address slots 22, 25 and 29 by number, and a mid-table insertion would silently retarget them. The new record table sits next to the prismatic record. Both generated outputs were first reproduced byte for byte from the unchanged schema with pinned flatc 25.2.10.
- **README recipe fix.** The TypeScript recipe in `schemas/README.md` took `ObservationPayload:` (from `enum ObservationPayload: uint8`) as a type name and failed. It now strips the `:<type>` suffix. With that fix the recipe reproduces the committed generated part exactly.
- **Revolute stop law (outside the code map, required by "the beam still reaches its ±π/6 stop").** Once the hinge crossed the wire, the loaded beam reached only 0.049 rad. `RevoluteConstraintSolver` had the prismatic stop law's sign inverted: an open stop (error ≥ 0) got the 0.2 / h rate and no relax-pass bias, so in every relax pass both stops arrested all motion towards them from any distance. It now follows `PrismaticConstraintSolver` exactly: an open stop is speculative (1 / h, applied in both passes), and a penetrated stop is pushed out at 0.2 / h only in the biased pass.
- **Fixture placement.** The Weight's origin is its base, and the beam top is at the pivot plus its 0.12 m half-thickness. The 10.3 symmetric-load fixture placed both weights at y = 3.06, 6 cm inside the beam. Uneven soft push-out then seeded a tilt. The 10.3 test and the new hinge test now place the loads at `BeamTop` = 3.12.
- **Balance window (owner game-fidelity policy, 28 October).** Defect statement: none. Equal loads resting on top of a frictionless pivot form an unstable equilibrium; with I ≈ 15 kg·m² and Mgh ≈ 31 N·m, a seed grows e-fold about every 0.7 s. The 10 s hold therefore measures the size of the solver-noise seed, not whether equal loads balance. `SymmetricalLoadBalance_RemainsBalanced` keeps 4 kg loads and its 0.01 rad / 0.02 rad/s bounds. It now asserts them at every tick of the 3 s window the test already declared. That window is about seven times the ~0.4 s an unequal load needs to reach its stop, and the asymmetric hinge test is the control. The requirement row's equal-load ten-second balance (CAT-034 fact 17: Basketballs at x = ±1 m, ≤ 0.01 rad tilt, heights within 0.01 m, final speed ≤ 0.02 rad/s over 1,200 ticks) is kept. The new `EqualBallLoads_StayBalancedForTenSeconds` asserts it at every tick and passes. Only the Story 10.3 review-added 4 kg Weight pair uses the 3 s window. A heavily and equally loaded lever tips after several seconds; the 4 kg probe crossed 0.01 rad at about 8 s. Making it rest would need hinge damping, which is an owner design decision and not part of this slice. Epic Story 10.3's "remains balanced indefinitely" is not literally achievable for heavy loads on an undamped pivot.- **Deferred `SocketWorld` entry, refined.** `SocketWorld` draws only non-rope links. The lever's body-owned ties are rope ports, and their artwork comes from committed rope spans. So no current link shows the defect; the entry records the latent gap.
- **Validator refusal code.** A port naming no declared scene body (`Body` < 0, ≥ `SceneBodies.Count`, or ≠ 0 without `ISceneGeometryElement`) is `BUNDLE006 OutOfRange` on `Ports[i].Body`. This mirrors the runtime `BundleValidator`, and the locator check stops there, so there is no second error.

## Verification

**Commands:**
- Prepend `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/node-26.12.1-dl/node-v26.12.1-darwin-arm64/bin` to `PATH` (the pinned Node v26.12.1, checksum-verified). Then run the full block in `docs/delivery-workflow.md#local-test-authority` (lines 30-56) from the repo root. Expected: every build has 0 warnings and 0 errors; every test project has 0 failed and `Skipped: 0`.
- Run `.githooks/pre-commit` unmodified against the staged candidate. Expected: exit 0, Anvil clean.

## Spec Change Log

## Review Triage Log

### 2026-10-31 — Review pass
- verdicts: 17 findings — high 5, medium 4, low 3, false 5, maybe-false 0
- findings:
  - `[high]` `[intent_gap]` (Blind 1) The ten-second ball balance is the same unstable inverted-pendulum equilibrium the Weight-pair comment cites (λ ≈ 1.4 /s), so its pass depends on mirror symmetry keeping the noise seed near f32 rounding. Balance group: with the hinge actually working, a frictionless pivot cannot meet "balanced indefinitely", and the intent does not say which yields.
  - `[false]` `[reject]` (Blind 2) Ball height-split bound 0.01 m is unjustified precision. Refuted: CAT-034 fact 17 itself states "ball heights stay within 0.01 of each other (3.45–3.47)".
  - `[high]` `[intent_gap]` (Blind 3) Story 10.3 acceptance "an equal load placed on both arms remains balanced indefinitely" (`epics.md:3960`) is narrowed to a 3 s heavy-pair window with no tracked owner decision. Verified: the 4 kg pair tips at about 8 s once the hinge exists; on main it only "balanced" because the missing hinge left a loose plank resting on the fulcrum with friction. Balance group.
  - `[medium]` `[patch]` (Blind 4) `docs/general-engine-design.md:75` still says anchors are "in the element's local frame", contradicting the edited lines 77–78. Moot under the intent_gap cascade; apply on resume (see Auto Run Result).
  - `[medium]` `[patch]` (Blind 5) `docs/engine-contracts.md:77` item 3 promises runtime endpoint tracking for body-owned locators, contradicting the new item 2 and the deferred `SocketWorld` gap. Moot under the cascade; apply on resume.
  - `[low]` `[defer]` (Blind 6) `docs/engine-contracts.md:61` footprint list omits revolute constraints. Pre-existing since Story 10.3.
  - `[high]` `[intent_gap]` (Edge 1) `EqualBallLoads_StayBalancedForTenSeconds` relies on an unstable equilibrium, so harmless solver-ordering changes could turn it red. Same root cause as the balance group.
  - `[medium]` `[defer]` (Verification gap 1) `ui/WorkshopConnections.cs:166-171` `SocketWorld` ignores `PortDescriptor.Body`. Latent: the only body-owned ports are rope ties drawn from committed spans. Pre-existing; already recorded in `deferred-work.md`.
  - `[low]` `[defer]` (Verification gap 2) The TypeScript wire mirror and `SNAPSHOT_SCHEMA` are checked only by the browser-deferred `run-reset-snapshot.test.ts` (`BrowserDeferral.cs:42`). Covered by the owner's browser deferral until the post-catalogue phase.
  - `[high]` `[intent_gap]` (Verification gap, other) `SymmetricalLoadBalance_RemainsBalanced` no longer holds the 4 kg pair for 10 s, which conflicts with the epic wording. Balance group.
  - `[false]` `[reject]` (Intent 1) The red-first evidence is not in the diff, and the stop-reach assertion couples the test to the solver fix. Refuted: `red-first-baseline.log` shows "The beam origin left its pivot by 0.6564 m" on `2ee79e8b`. Reaching the stop is the lever's hinge behaviour, which the intent requires.
  - `[false]` `[reject]` (Intent 2) The solver change falls outside the named wire surface. Refuted: without it a transported hinge holds the beam at 0.049 rad. It matches `PrismaticConstraintSolver.cs:66,114,176` exactly and is exercised by three lever tests (Forward-Only and Verification gap concur).
  - `[high]` `[intent_gap]` (Intent 3) The existing lever test was weakened beyond the stated defects. Balance group.
  - `[medium]` `[patch]` (Intent 4) The locator contract is enforced at build and test, but contract item 3 still promises runtime endpoint tracking. Same root cause as Blind 5; moot under the cascade.
  - `[low]` `[defer]` (Intent 5) The TypeScript module and `SNAPSHOT_SCHEMA` are regenerated but untested. Same root cause as Verification gap 2.
  - `[false]` `[reject]` (Intent 6) "Rather than widening the test" may be breached. Refuted: each port is pinned to the exact `SceneBodies[Body].Root/Loc_Port_X` path, and `TestBundle.SceneGeometry` stays root-only.
  - `[false]` `[reject]` (Intent 7) There is no evidence of a green full round. Refuted: the coordinator's full local-authority round on this candidate passed 5,191 tests with 0 failed and `Skipped: 0` across five projects, with 0 warnings and 0 errors.
- Forward-Only Auditor: no violations. The diff removes `PackageValidator`'s silent out-of-range-body fallback.
- intent_gap action: the attempted change was saved as a patch (19 files, reverse-applies cleanly), and the code paths were restored to `2ee79e8b`. The patch was later re-applied unchanged (see resolution below), so it is not retained as a separate artifact.

### 2026-11-01 — Intent gap resolution and patches
- **Owner decision (1 November 2026, via the coordinator's question):** land the fix and reopen "balanced indefinitely" as a tracked open item, to be met by a separate engine slice. The balance-group rows (Blind 1, Blind 3, Edge 1, Verification gap other, Intent 3) are resolved by this intent update. The heavy-pair 3 s window and the fact-17 10 s ball balance stand. The open item is recorded in [deferred-work.md](deferred-work.md#lever-equal-load-indefinite-balance) and noted under Story 10.3 in `epics.md`, with its acceptance text unchanged.
- **Patched (same implementer):**
  - Blind 4: `docs/general-engine-design.md` "Declared ports" now says the owning body's frame.
  - Blind 5 and Intent 4: `docs/engine-contracts.md` item 3 now says rope spans track the owning body through body-frame anchors, while drawn non-rope links do not yet. It links the deferred `SocketWorld` entry.
- **Patch verification:**
  - Every non-Markdown file is identical to the verified candidate. The code portion of the saved patch reverse-applies cleanly to the index, and the 14 code files are unchanged.
  - The full local round on that code still applies: 5,191 passed, 0 failed, `Skipped: 0`.
  - `ArchitectureDocsConsistencyTests` passed 4/4 with 0 skipped after the doc edits.
  - The normal pre-commit hook runs on the final commit.

## Auto Run Result

Status: done (local commit; publication follows under the coordinator).

**Summary:**
- Revolute constraints now cross the compiled-scene wire (`CompiledSceneRecord.revolutes`, slot 30), so the Impact Lever hinges in every runtime world.
- The revolute end-stop law is aligned with the prismatic solver, so a hinged beam reaches its ±π/6 stops.
- Body-owned port locators are enforced as direct children of `SceneBodies[Body].Root`, with their anchor in that body's frame.
- `PackageValidator`'s silent out-of-range-body fallback is replaced by a `BUNDLE006` refusal.
- Published main's red `BundlePortLocatorTests` is fixed.

**Files changed:**
- **Wire schema and codec:**
  - `schemas/workshop_wire.fbs`: adds the revolute record and the `revolutes` field.
  - `schemas/README.md`: fixes the TypeScript regeneration recipe.
  - `engine/gpu/WorkshopWireGenerated.cs`: regenerated with flatc 25.2.10.
  - `engine/gpu/WorkshopWireFlatBuffers.Compiled.cs`: encodes and decodes revolutes.
  - `tools/e2e/workshop-wire.ts` and `tools/e2e/run-reset-snapshot.ts`: TypeScript mirrors (browser-deferred).
- **Solver:** `CuriousContraptions.Simulation/RevoluteConstraintSolver.cs` uses the prismatic stop law.
- **Locator contract and validator:**
  - `tools/BundleElements/PackageValidator.cs`: refuses an out-of-range port body.
  - `CuriousContraptions.Abstractions/PortDescriptor.cs` and `CuriousContraptions.Bundle/BundleValidator.cs`: summaries updated for the body-frame contract.
- **Tests:**
  - `CuriousContraptions.tests/ImpactLeverTests.cs`: hinge test, ten-second ball balance and fixture heights.
  - `CuriousContraptions.tests/CompiledSceneBoundaryTests.cs`: round-trip, control and malformed-row tests.
  - `CuriousContraptions.tests/BundlePortsTests.cs`: body-owned locator contract.
  - `tools/BundleElements.Tests/PortLocatorTests.cs` and `tools/BundleElements.Tests/Fixtures.cs`: locator fixtures.
- **Docs and tracking:**
  - `docs/engine-contracts.md`, `docs/general-engine-design.md` and `docs/planning/elements/CAT-034-impact_lever.md`: contract text.
  - `_bmad-output/implementation-artifacts/deferred-work.md`: `SocketWorld` entry and the lever balance open item.
  - `_bmad-output/planning-artifacts/epics.md`: open-item note under Story 10.3.

**Review findings:**
- 17 findings: high 5, medium 4, low 3, false 5.
- **Patched:** two medium entries (Blind 4; Blind 5 with Intent 4).
- **Resolved by the owner's intent decision:** the high balance group.
- **Deferred:** three items (footprint wording, `SocketWorld`, TypeScript mirror verification).
- **Rejected as false:** five (Blind 2; Intent 1, 2, 6 and 7), each with its refutation in the triage log.
- Forward-Only found no violations.

**Follow-up review recommended:** true, with patched counts high 0 and medium 2. The specific unverified risk is that the two rewritten contract passages were patched after the review pass and were not re-read by an independent layer. The code is unchanged since that review.

**Verification:**
- **Red first:** on `2ee79e8b` the hinge test fails at 0.6564 m pivot drift.
- **Full round (coordinator, pinned Node 26.12.1):**
  - CuriousContraptions.tests 4,686
  - BundleElements 219
  - E2eRunner 192
  - Coverage 77
  - WorkerAotGuard 17
  - All 0 failed and `Skipped: 0`; builds had 0 warnings and 0 errors.
- **Matrix audit:** all seven rows' tests passed by name.
- **Implementer's pre-commit:** 127/0/0.

**Residual risks:**
- Heavy equal loads tip after several seconds until the pivot holding slice lands (open item).
- The TypeScript mirrors are unverified until the browser phase.
- `SocketWorld` doesn't yet handle body-owned non-rope ports.
- Story 10.5 must merge `workshop_wire.fbs` by hand and regenerate.
- Browser qualification is pending.

## Publication receipt — 1 November 2026

- Commit `cca0061a19cf83652489becf4d1dd7b69236dc94`, pushed as a fast-forward of `origin/main` from `2ee79e8b0a1dce23fed3f2472d7edc40dcd067c6`. The coordinator confirmed afterwards that remote `main` equals that SHA.
- Approval was the Auto review pass with the owner decision. The full local round and the normal hook (127/0/0, `Skipped: 0`) ran on the committed content.
- CI build, Pages artifact and deployment were not monitored, per the owner instruction of 29 October 2026, so they remain unverified. Browser qualification is pending until the post-catalogue phase.
