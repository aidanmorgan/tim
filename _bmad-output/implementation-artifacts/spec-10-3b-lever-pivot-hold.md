---
title: '10.3b — Lever pivot holding: declared hinge friction torque (CAT-034)'
type: 'bugfix'
created: '2026-11-02'
status: 'done'
baseline_revision: 140c6f073d094294366cc46f73caaeffe23154ef
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/CAT-034-impact_lever.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** CAT-034's acceptance "an equal load placed on both arms remains balanced indefinitely" is not met. This is the open item reopened by owner decision on 1 November 2026 (`deferred-work.md#lever-equal-load-indefinite-balance`).
- The Impact Lever's revolute hinge is frictionless. Loads resting on the beam top form an inverted-pendulum equilibrium: two 4 kg Weights at x = ±1.2 m tip past 0.01 rad at about 8 s, with growth of about 1.4/s.
- The requirement lists friction as a lever closure criterion (`requirements.md#current-cat-034`).

**Approach:** Give the generic revolute hinge a declared Coulomb friction torque τ (N·m), carried as package data.
- **Law:** a velocity-level friction row on the hinge axis. Its per-substep accumulated impulse is clamped to ±τ·h; it is warm-started and solved with the other joint rows.
- **Effect:** it holds the beam while the net torque imbalance is below τ, and lets it turn otherwise.
- **Lever value:** the Impact Lever declares τ = 2 N·m.

## Boundaries & Constraints

**Always:**
- One generic law for every revolute joint; τ is declared data. τ = 0 goes through the same clamp and behaves frictionless. There is no separate branch for it.
- The friction row only removes energy.
- **Validation:** τ must be finite and ≥ 0. It is refused otherwise at the bundle validator, the declaration, the ABI read and the wire decode.
- **One τ·h:** the solver's clamp and admission's refusal of a committed friction impulse use one shared f32 τ·h expression with the same h.
- **Process:** red-first evidence; game-grade derived tolerances; f32; forward-only; full local round `Skipped: 0`.

**Never:**
- an element, shape or scenario branch in the solver;
- a position anchor, a spring back to the stuck angle, or a detent. If the velocity-level row creeps, stop and report.
- load-proportional pivot friction;
- a `ParameterUnit`/UI control for τ (the lever declares a fixed value);
- changing stiffness or damping semantics;
- browser E2E (deferred).

## I/O & Edge-Case Matrix

All loads start resting on the beam. A dropped load's contact impulse exceeds τ for a substep, so drops are used only where a case says so.

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Equal heavy pair | Lever τ = 2 N·m, beam level, 4 kg Weights resting at x = ±1.2 m | Beam within 0.01 rad and Weights do not walk, for 60 s (7,200 ticks): 7× the observed 8 s tip, so an unheld seed would grow e^84 | No error expected |
| Held by friction, not symmetry | Basketballs at x = −1.0 and +1.01 (≈0.1 N·m imbalance), 10 s | Beam within 0.01 rad (tips on the current base) | No error expected |
| Below τ | Equal pair plus a resting 0.25 kg Weight giving ≈0.8·τ | Held within 0.01 rad over the window | No error expected |
| Above τ | Same, giving ≈1.25·τ | Beam turns to the stop on that side | No error expected |
| Heavy asymmetry | 4 kg Weight at x = −1.2 (≈47 N·m) | Reaches the +π/6 stop within 2 s; pivot drift ≤ 0.03 m | No error expected |
| Launch | Existing bowling-ball impact | Payload above 4 m and faster than 2 m/s; E(t) ≤ 1.01·E0; friction loss ≤ τ·π/3 ≈ 2.1 J | No error expected |
| Rope lift | Existing TieA/TieB rope tests | Arm lifted more than 0.02 rad within 50 ticks | No error expected |
| Two levers (SIMD lanes) | One lever with a balanced pair, one with a tipping load, in one scene | First held, second tips | No error expected |
| Generic law | Unframed hinged bar with offset weight, torque m·g·d below and above τ | Held below τ; above it, angular acceleration ≈ (m·g·d − τ)/I | No error expected |
| Invalid τ | τ < 0, NaN or ±∞ | Refused atomically | Typed refusal at each boundary |
| Committed impulse | Friction impulse beyond ±τ·h, versus exactly ±τ·h | Beyond: refused. Exactly: admitted | Candidate refusal |

</intent-contract>

## Code Map

- **Descriptor:** `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:124-133`, `RevoluteHingeDescriptor`.
  - Add `MechanicalValue FrictionTorque`, modelled on `Stiffness` and `Damping`.
  - `MechanicalValue` is at :144-156.
- **Bundle:**
  - `CuriousContraptions.Bundle/BundleSchema.cs:35-38`: the new field becomes a required JSON member.
  - `BundleValidator.Hinge` (`BundleValidator.cs:618-644`): add `Value(path+".FrictionTorque", …)`, using the :631-632 pattern.
  - `tools/BundleElements/SceneGeometryExtractor.cs:103` passes the field through.
- **Compiler:** `engine/gpu/WorkshopPhysicsCompiler.cs:137-140`, evaluating `hinge.FrictionTorque.Evaluate(parameters)`.
- **Declaration:** `engine/gpu/RevoluteConstraint.cs:8-26`, `RevoluteConstraintDeclaration`. Add the field plus the finite, ≥ 0 check. It is built positionally only at `PhysicsGpuAbi.Constraints.cs:213` and `WorkshopWireFlatBuffers.Compiled.cs:461`.
- **ABI row:** `engine/gpu/PhysicsGpuAbi.Constraints.cs:41-53` and :182-239.
  - Rename the padding word at 92 to `RevoluteFrictionTorqueOffset`, and drop its zero check at :211.
  - Grow `RevoluteRowCount`/impulses from 8 to 9.
  - Pad the row to **144 bytes**. All rows stay multiples of 16 and each starts with a U64 identity. Trailing padding must be zero and is refused otherwise.
  - Candidate check (:232-237): the friction impulse must be finite with |λ| ≤ τ·h, using the shared expression.
  - `WorkshopAbiLiteralScanTests.cs` requires named offsets.
- **Engine:** `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`:
  - read τ at :541-575;
  - warm start loop at :861;
  - persistence at :989-993.
- **Rows:** `engine/gpu/RevoluteRows.cs`. Add `RevoluteRowKind.Friction` (appended after the stops) and `RevoluteRows.Count` = 9. Its Jacobian is the Axial one (:63), with error 0.
- **Solver:** `CuriousContraptions.Simulation/RevoluteConstraintSolver.cs`.
  - In `PrepareJointRows` :33-91, the friction row has Gamma 0, Bias 0, Minimum −τ·h and Maximum +τ·h.
  - In the sweep it never takes bias, including under `useBias`: see :129 and :204.
  - Scalar path :118-144 and SIMD path :146-245.
  - Arrays at :25-26.
- **`ConstraintRowData`** (`PrismaticConstraintSolver.cs:10-20`): add `Maximum`. Set it in **every** initializer:
  - revolute :79;
  - prismatic :69 and :232;
  - tensile `TensileConstraintSolver.cs:44`, which passes `float.MaxValue` at :74.
  Then replace the hard-coded maxima at revolute :138 and :207 and prismatic :179. Leave no second hard-coded maximum.
- **Reuse pattern:** rolling-resistance clamp, `WorkshopConstraintSolver.SolveRollingPatch` :362-390.
- **Wire:**
  - `schemas/workshop_wire.fbs:484-495`: append `friction_torque: float` after `collision` (slot 10).
  - Regenerate `engine/gpu/WorkshopWireGenerated.cs` and `tools/e2e/workshop-wire.ts` per `schemas/README.md` with pinned flatc **25.2.10** at `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/flatc-25.2.10/flatc`. `/opt/homebrew/bin/flatc` is 25.12.19 and must not be used.
  - Codec: `WorkshopWireFlatBuffers.Compiled.cs:443-471`.
  - `tools/e2e/run-reset-snapshot.ts:80`: `SNAPSHOT_SCHEMA`.
- **Package:** `elements/ImpactLever/ImpactLeverElementDeclaration.cs:89-112`. Add `FrictionTorque: MechanicalValue.Fixed(2f)`.
- **Tests:**
  - `CuriousContraptions.tests/ImpactLeverTests.cs`: `SymmetricalLoadBalance_RemainsBalanced` :201 (3 s window today; comment :193-199), `EqualBallLoads_StayBalancedForTenSeconds` :229, `AsymmetricLoad…` :162, launch :70, rope :334/:436, `MultiJointBatching…` :482 (the only SIMD coverage).
  - `CompiledSceneBoundaryTests.cs:222-247`: `EveryRevoluteFieldCrossesTheCommandWire`.
  - `PackageRuntimeParityTests.cs:213-245`: add the field.
  - `tools/BundleElements.Tests/BundleValidatorTests.cs:1009`: refusals.
  - `BundleRoundTripTests.cs:161`.
  - **Generic-law case:** build the unframed hinged bar from the engine-direct scene pattern in `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs` (`LawScene`), declaring one revolute with τ.
- **Docs:**
  - `docs/planning/elements/CAT-034-impact_lever.md`: resolve open question 5 (:208); re-qualify fact 17 (:133) and positive acceptance (:193).
  - `_bmad-output/implementation-artifacts/deferred-work.md:186-190`: mark the item resolved.

## Tasks & Acceptance

**Execution:**
1. **Predict and red-first.** Before any change, add the I/O matrix's hold cases and record their failures on the base. Write the expected outcome of every rule-dependent test in the implementer record. Mismatches are investigated, never tuned.
2. **Data path**, green, with behaviour unchanged at τ = 0: descriptor, bundle, compiler, declaration, ABI row (144 bytes, refusals), wire with regeneration, and package value.
3. **Law**, green:
   - the friction row and `Maximum`;
   - both solver paths;
   - warm start and persistence;
   - the candidate bound.
4. **Tests and docs** as below.
5. **One full local round, then the unmodified pre-commit hook.**

Process rules for the implementer:
- Write only inside the worktree and `$TMPDIR`. Writes elsewhere have stalled background agents on prompts they cannot show.
- Validate each write with Anvil first; edit files with the Edit/Write tools only.
- Put a timeout on every build and test command.
- Stop and report rather than tune a constant or bound.
- Stage the result; do not commit.

**Acceptance Criteria:**
- Given the shipped Impact Lever and any I/O-matrix load, when Run proceeds, then the stated hold, tip, launch and rope outcomes occur with E(t) ≤ 1.01·E0.
- Given the equal 4 kg pair over 60 s, when the angle and both Weights' x are sampled every tick on the new law, then:
  - the angle does not drift steadily toward 0.01 rad;
  - the Weights do not walk along the beam.
  If either fails, stop and report. Do not add a position anchor.
- Given a revolute with τ = 0 (any existing test scene), then its behaviour is unchanged.
- Given CAT-034, then open question 5 records the resolution and its derivation, fact 17 and the heavy-pair acceptance hold indefinitely, and the deferred item is marked resolved.

## Spec Change Log

- **Heavy-pair window: 60 s became one whole run (30 s, 3,600 ticks).** The harness refused tick 3,601: a run ends at `WorkshopCadenceSettings.RunTickLimit` (30 s). The whole run is every tick a player can observe, so it is the indefinite-balance window. It is still almost 4× the observed 8 s tip, so an unheld seed would grow about e^42. Creep is measured from 10 s to the end of the run.
- **Below/above τ load: a 0.5 kg Weight at x = 0.326 m (0.8·τ) and x = 0.510 m (1.25·τ), not a 0.25 kg Weight.** A 4 kg Weight has radius 0.352 m (inner edge at x = 0.848 m). A 0.25 kg Weight needs x = 1.019 m for 1.25·τ, which is inside the 4 kg Weight; inboard of it, a 0.25 kg Weight (radius 0.171 m) can give at most 0.83·τ. A 0.5 kg Weight (radius 0.183 m) fits at both arms, so the two cases differ only in x.
- **Launch "friction loss ≤ τ·π/3 ≈ 2.1 J" is a budget, not an assertion.** Friction work is not observable through committed reads. Against about 90 J of input energy it cannot change the launch outcome. The launch keeps its payload and E(t) ≤ 1.01·E0 assertions, and the dissipation law is proved directly by the generic-law test.
- **Two levers became three, then four in two arrangements (review loop 1).** The four levers fill one SIMD group. Joints 0, 3, 2, 1 take lanes 0, 1, 2, 3.
  - Held levers carry the 0.1 N·m Basketball imbalance. A mirror-symmetric pair stays level unheld for about 8 s, so it would hide a lane that drops friction.
  - Tipping levers carry one 4 kg Weight on opposite arms.
  - The first arrangement holds in lanes 0 and 1 and tips in lanes 2 and 3. The second tips in lanes 0 and 1 and holds in lanes 2 and 3. So every lane both holds a lever and lets one tip.
- **τ = 0 unchanged coverage.** The lever is the only hinge package, so the "τ = 0 behaves frictionless" criterion is covered by the generic-law test's τ = 0 control (α = m·g·d/I) on the same clamp.

## Implementer record

**Acceptance quantities, chosen before observation.**
- **Walk:** 0.025 m = τ/(2·m·g) for a 4 kg Weight. At that distance, both Weights walking the same way would on their own load the hinge with τ.
- **Creep:** no separate rate bound (review loop 2 removed it). Both held lever cases, the heavy pair and the pair plus 0.8·τ, assert the 0.01 rad tilt at every tick of a whole run (the 30 s `RunTickLimit`, every tick a player can observe), so any creep that becomes visible within a run fails them.
- **Below-τ margin:** with a destabilising stiffness of about 31–33 N·m/rad, the 0.8·τ hold survives only about 0.012 rad of tilt (0.4 N·m / 32 N·m/rad). A settling seed beyond that would be a finding, not something to tune.

**Predictions on the base (frictionless hinge), and observed red-first at `140c6f07`** (raw log: `$TMPDIR/red-first-10-3b.keep.log`):

| Test | Predicted on base | Observed on base | Predicted with τ = 2 N·m |
|---|---|---|---|
| `SymmetricalLoadBalance_RemainsBalancedForAWholeRun` | Fail (tips about 8 s) | Fail: 0.5237 rad | Pass, no creep or walk |
| `UnequalBallLoads_AreHeldByThePivotFriction` | Fail | Fail: 0.5236 rad | Pass |
| `ASmallLoad…(0.8)` | Fail | Fail: 0.5237 rad | Pass |
| `ASmallLoad…(1.25)` | Pass (tips) | Pass | Pass: reaches the −π/6 stop |
| `PivotFrictionHoldsOrYieldsPerLeverAcrossSimdLanes` (three levers; in review loop 1 it became `…InEverySimdLane`, four levers in two arrangements) | Fail (held lever tips) | Fail: 0.5236 rad | Pass |
| Asymmetric 47 N·m, launch, rope, limits, SIMD launch, `EqualBallLoads…` | Pass | Pass | Pass (47 N·m reaches the stop about 4% slower; launch loses at most 2.1 J; rope torque about 70 N·m ≫ τ) |
| Generic law (new; needs τ) | n/a | n/a | τ = 0: α = mgd/I; τ = 0.5·mgd: α = 0.5·mgd/I; τ = 2·mgd: held |

**Observed with τ = 2 N·m.** Every prediction held; no constant or bound was tuned.
- A one-off probe (not committed; `$TMPDIR/probe-10-3b.trx`) sampled the angle every tick over a whole run:
  - **4 kg pair:** maximum tilt 8.7e-6 rad, Weight walk 0.16 mm. Drift from 10 s to 30 s was 6.5e-6 rad, about 3e-7 rad/s. The drift plateaued rather than growing steadily.
  - **Pair plus 0.8·τ:** maximum tilt 1.5e-5 rad, well inside the 0.012 rad margin, with no steady drift.
  - **0.1 N·m Basketballs:** maximum tilt 6.4e-7 rad.
  - The velocity-level row does not creep, so no position anchor was considered.
- **Generic law:** the τ = 0 and τ = 0.5·m·g·d spins are within 10% of (m·g·d − τ)·t/I, and τ = 2·m·g·d holds. Review loop 1 added a check that the bar's mechanical energy never exceeds its initial value plus 0.01 J at any tick.
- **Review loop 1, other changes:**
  - `WorkshopConstraintSolver.SubstepSeconds` now references `RevoluteRows.SubstepSeconds` (1 / `PhysicalFrequency`), so h has one source.
  - The wire refusal theory gained a −∞ friction-torque row.
  - The cadence-pinning `τ/480` assertion was deleted.

**Verification.**
- **First full round:** 1 failure. `HalfPrecisionGuardTests` refused the test identifier `Half` in `RevoluteHingeFrictionTests.cs`; it was renamed `BarExtents`.
- **Final full local round** (`$TMPDIR/full-round-10-3b-2.log`, base `140c6f07` plus the staged candidate): 0 warnings and 0 errors across the five compiler-log builds. Results per test project, all with 0 failed and Skipped 0:

  | Test project | Passed |
  |---|---|
  | CuriousContraptions.tests | 4,883 |
  | BundleElements.Tests | 225 |
  | E2eRunner.Tests | 192 |
  | Coverage.Tests | 77 |
  | WorkerAotGuard.Tests | 17 |
- **Unmodified `.githooks/pre-commit`** on the staged candidate: exit 0, Anvil clean, scoped tests 127 passed with Skipped 0.
- **Wire regeneration:** pinned flatc 25.2.10 first reproduced both committed generated files from the unchanged schema, then regenerated them after the schema change. Both generated files now match the pipeline output byte for byte.

## Review Triage Log

### 2026-11-02 — Review pass
- verdicts: 20 findings — high 0, medium 1, low 12, false 7, maybe-false 0. Forward-Only reported no violations; its borderline items, all on the allowed side, are not findings. The duplicated substep source it noted is folded into Intent 9.
- findings:
  - `[false]` `[reject]` (Intent 1) Equal pair held 30 s, not 60 s. `RunTickLimit` refuses tick 3,601, and the whole run is every tick a player can observe. The departure is recorded in the Spec Change Log.
  - `[false]` `[reject]` (Intent 2) The threshold load is 0.5 kg, not 0.25 kg. At 1.25·τ a 0.25 kg Weight would overlap the 4 kg Weight; the torque fractions 0.8·τ and 1.25·τ are preserved.
  - `[low]` `[patch]` (Intent 3) "The friction row only removes energy" has no direct check, and the launch's 2.1 J budget is a comment. → patch: assert E(t) ≤ E0 in the generic-law hinged-bar cases with τ > 0.
  - `[false]` `[reject]` (Intent 4) The heavy-asymmetry and rope rows are covered by unchanged tests. Those tests run under the new law, so they are behavioural coverage of the rows, not merely regression.
  - `[false]` `[reject]` (Intent 5) Three levers rather than two is a superset of the matrix row on the same surface.
  - `[false]` `[reject]` (Intent 6) A box whose centre of mass sits d from the hinge is the same law case as a bar with an offset weight.
  - `[low]` `[patch]` (Intent 7) −∞ is not exercised at the wire decode. → patch: add the row.
  - `[low]` `[reject]` (Intent 8) Atomic refusal is shown only at the wire. The other boundaries throw before any state is built: validator, declaration and ABI read are pure checks.
  - `[low]` `[patch]` (Intent 9) The substep h is defined twice, as `RevoluteRows.SubstepSeconds` and `WorkshopConstraintSolver.SubstepSeconds`. The values are equal today but could drift. → patch: derive the solver's from the same `1f / WorkshopCadenceSettings.PhysicalFrequency` source, or reference one from the other.
  - `[false]` `[reject]` (Intent 10) Scope additions (`Maximum`, the 144-byte row, wire and parity) are the necessary data path the spec names.
  - `[false]` `[reject]` (Intent 11) Red-first evidence is not visible in the diff. It is recorded in the implementer record with its log (four hold tests tipped to the 0.52 rad stop on the base).
  - `[low]` `[patch]` (Verification other) `CreepRateAllowance` is derived from a 10-minute session that a 30 s run rules out, so it is far tighter than anything observable. → patch: re-derive it from the run, so drift extrapolated over a whole `RunTickLimit` stays below the 0.01 rad visible bound.
  - `[low]` `[reject]` (Edge 1) A friction torque sourced from a Real parameter with a negative declared minimum passes the validator. No package declares a parameter friction (the lever uses `Fixed`). Stiffness and Damping already share the same `Value` helper, and the fix adds a guard.
  - `[low]` `[patch]` (Edge 2) Same root as Verification other (the heavy pair's creep bound). → same patch.
  - `[low]` `[patch]` (Edge 3) Same root (the 0.8·τ case's creep bound). → same patch.
  - `[low]` `[reject]` (Edge 4) The spec's acceptance still reads 60 s. The fix edits this build's spec, and the departure is already logged in the Spec Change Log.
  - `[medium]` `[patch]` (Blind 1) The SIMD test can catch missing friction only in lane 0. Levers 2 and 3 carry about 47 N·m and reach their stops with or without friction, so a lane-index defect would pass. → patch: two held levers, or a held lever in a non-zero lane, and say which lanes hold.
  - `[low]` `[patch]` (Blind 2) Same root as Verification other. → same patch.
  - `[low]` `[patch]` (Blind 3) `Assert.Equal(frictionTorque / 480f, limit)` pins the cadence constant and passes only for power-of-two τ. → patch: delete it; the ±limit admit/refuse assertions carry the contract.
  - `[low]` `[patch]` (Blind 4) The `UnequalBallLoads_AreHeldByThePivotFriction` comment claims a creep check the test does not make. → patch: correct the comment.

## Design Notes

**Derivation of τ = 2 N·m.** The owner delegated the choice; the requirement names friction without a value.
- **Tipping bound:** the lightest catalogue body, a 0.25 kg Weight, at the 1.8 m tie port gives 4.4 N·m. It must tip the lever, so τ is at most half of that.
- **Holding bound:** the 4 kg pair's inverted-pendulum stiffness is M·g·h ≈ 31 N·m/rad, so τ = 2 holds any seed up to 0.065 rad. The beam is authored level, so the imbalance it must hold is near zero.
- **Rope lifts:** about 70 N·m. Unaffected.
- **47 N·m asymmetry:** about 4% slower to reach the stop.

**Why a constant torque.** It is one declared number. Load-proportional friction would need the hinge's reaction impulse read back inside the law. That is a second mechanism with its own edge cases, left for a future refinement.

**Row order.** Friction is appended after the stops, so in Gauss-Seidel order the stops resolve first and friction last within each iteration.

**Creep risk (advisor).** A row with no position term drives the relative spin to zero but no further. Under a steady sub-τ torque, residual spin after 4 iterations can creep. The predict step must measure this.

## Verification

**Commands:**
- Prepend pinned Node v26.12.1 (`/private/tmp/claude-501/-Users-aidan-dev-personal-tim/5b49c1ca-9721-4a5d-8c03-bdf3a5db8b62/scratchpad/node-26.12.1-dl/node-v26.12.1-darwin-arm64/bin`) to `PATH`. Run the full block in `docs/delivery-workflow.md#local-test-authority` from the worktree root. Expected: 0 warnings, 0 errors, every test project `Skipped: 0`, 0 failed.
- Run `.githooks/pre-commit` unmodified on the staged candidate. Expected: exit 0, Anvil clean.

## Auto Run Result

Status: done (2 November 2026).

**Implemented:** a declared Coulomb friction torque τ on the generic revolute hinge.
- **Law:** a velocity-level friction row on the hinge axis, `RevoluteRowKind.Friction`, appended after the stops. It has zero error, bias and softness.
  - Its accumulated impulse is clamped to ±τ·h through `RevoluteRows.FrictionImpulseLimit`, the one expression shared by the solver clamp and candidate admission.
  - It is warm-started and persisted, in both the scalar and SIMD paths.
  - τ = 0 goes through the same clamp.
- **Data path:** descriptor → bundle (required field) → compiler → declaration → ABI row (144 bytes: τ at offset 92, 9 impulses, zero padding refused) → wire (`friction_torque`, slot 10, regenerated with flatc 25.2.10).
- **Supporting change:** `ConstraintRowData.Maximum` is set at every row construction site.
- **Package:** the Impact Lever declares 2 N·m.
- **Docs:** CAT-034 open question 5 is resolved with the τ derivation, fact 17 and the positive acceptance now hold indefinitely, and the deferred item is marked resolved.

**Files changed (27 staged, against `140c6f07`):**
- the revolute declaration, rows, ABI and solver;
- the prismatic and tensile solvers (`Maximum`);
- `WorkshopConstraintSolver` (single substep source);
- engine, compiler, wire codec, schema and generated C#/TS;
- the bundle validator;
- the Impact Lever declaration;
- tests: `ImpactLeverTests`, new `RevoluteHingeFrictionTests`, `CompiledSceneBoundaryTests`, `PackageRuntimeParityTests`, `WorkshopPhysicsLawTests` and `BundleValidatorTests`;
- docs: CAT-034, `deferred-work.md` and the refreshed `epic-10-context.md`.

**Review:** one pass of five layers, 20 findings, no high, Forward-Only clean.
- **Patched:** 12, at verdicts medium 1 (the SIMD lane coverage) and low 11.
- **Rejected:** 8, with reasons in the triage log.
- **Deferred:** none.
- **Independent re-review of the fix delta:** round 1 confirmed 6 of 7 fixes and found the creep rule redundant or insufficient, plus two doc errors. These were fixed by removing the creep rule and checking the held cases on every tick of a whole run. Round 2 found no defect and passed.
- **Follow-up review recommendation:** `false`. This is a first pass with one medium patch, and the re-review already passed.

**Verification on the final staged candidate:**
- Coordinator full round per `docs/delivery-workflow.md#local-test-authority` with pinned Node 26.12.1. Exit 0; every project `Skipped: 0`.

  | Test project | Passed |
  |---|---|
  | CuriousContraptions.tests | 4,885 |
  | BundleElements.Tests | 225 |
  | Coverage.Tests | 77 |
  | E2eRunner.Tests | 192 |
  | WorkerAotGuard.Tests | 17 |

- An earlier coordinator round on the pre-patch candidate gave 5,394/0/0.
- Implementer: unmodified hook exit 0 (127/0/0, Anvil clean). Red-first: four hold tests tipped to the 0.52 rad stop on the base.

**Matrix audit:** every I/O row is covered by a test that ran and passed.
- equal pair (whole run);
- held by friction (unequal Basketballs);
- below τ and above τ (0.5 kg at 0.8·τ and 1.25·τ);
- heavy asymmetry, launch and rope (unchanged tests under the new law);
- SIMD (four levers, two arrangements, every lane holds and tips);
- generic law (hinged bar, τ = 0 and spin/held cases, with an energy check);
- invalid τ (validator, declaration, ABI and wire);
- committed impulse (±τ·h admitted, one ULP beyond refused).

The departures (30 s run, 0.5 kg load, launch budget as a comment) are recorded in the Spec Change Log.

**Residual risks:**
- the friction is a constant torque, not load-proportional (by design);
- creep was measured only within single 30 s runs;
- each SIMD lane tests one clamp side;
- a friction torque sourced from a parameter with a negative declared minimum is not refused by the validator (shared with Stiffness and Damping; no package uses it);
- browser qualification is pending the post-catalogue E2E phase.

**Process disclosures:**
- The implementer reports some Anvil checks run after the edit or on previews.
- Coordinator spec edits were not individually Anvil-validated; the hook's Anvil scan covers the staged spec.

## Publication receipt — 2 November 2026

- **Commit:** `3ce30ba3e0ebb6afef3894c20a15b9cd436d4db4`, pushed as a fast-forward of `origin/main` from `140c6f073d094294366cc46f73caaeffe23154ef`. The coordinator confirmed afterwards that remote `main` equals that SHA.
- **Approval:** the five-layer Auto review, plus two independent re-review rounds of the fix delta ending in Pass.
- **Tests:** full local round 5,396/0/0, `Skipped: 0`, on the committed content. Normal hook 127/0/0.
- **Unverified:** CI build, Pages artifact and deployment were not monitored (owner instruction, 29 October 2026). Browser qualification is pending until the post-catalogue phase.
