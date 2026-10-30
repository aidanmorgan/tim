---
title: '10.4 — Counterweight & Multi-Body Pendulum Lifting (CAT-067)'
type: 'feature'
created: '2026-10-31'
baseline_revision: 1f0454bb1d5a8616fa1ff8499bf3b4685ffcb6fb
status: 'done'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** While the Weight body (`elements/Weight/`), its mass parameter (0.25–8 kg), and resting/stacking/rolling were established in Story 10.2a, its qualification as a suspended 3D pendulum and counterweight for multi-body load lifting (CAT-067) has not been verified. Specifically, the simulation must prove natural oscillation period $T = 2\pi\sqrt{L/g}$, length scaling ($T \propto \sqrt{L}$), mass independence of period (0.25 kg to 8.0 kg), gradual oscillation damping without unphysical energy growth, 3D non-planar precessing motion, slack-release boundary transitions, multi-body pulley lifting, floor contact tension relaxation, open-route free fall, wiring direction invariance, exact Run/Reset restoration ($\le 1\,\mu\text{m}, \le 1\,\mu\text{rad}$), and Save/Load persistence roundtrips.

**Approach:** Implement comprehensive pendulum qualification and multi-body lifting test coverage in `CuriousContraptions.tests/WeightTests.cs`. Leverage the real C# physics simulation engine (`WorkshopPhysicsCompiler`, `WorkshopPhysicsEngine`, `TensileConstraintSolver`, `WorkshopIntegrationHarness`) operating at 120 Hz / 480 Hz substeps with single-precision `f32` arithmetic. Prove all physical invariants without output-hash assertions or skipped tests. Explicitly verify that browser suite `tools/e2e/cat-067.test.ts` remains deferred to the post-catalogue browser phase per `BrowserDeferral.cs` and `AGENTS.md`.

## Boundaries & Constraints

**Always:**
- Keep all simulation arithmetic and game values in canonical IEEE-754 single precision (`f32`) with zero double precision in runtime physics loops.
- Evaluate rope tensile constraints via the Box2D v3 TGS Soft constraint solver at 120 Hz / 480 Hz substeps without Baumgarte stabilization or ad-hoc velocity hacking.
- Assert observable physical behavior: natural period $T$, damping progression, energy bounds, and exact kinematic state.
- Preserve exact state restoration on Reset ($\le 1\,\mu\text{m}, \le 1\,\mu\text{rad}$, zero velocity) and deterministic replay.
- Maintain `Skipped: 0` across all solution test projects.

**Never:**
- Never create backwards-compatibility shims, legacy flags, fallback solvers, or migration adapters.
- Never add element-specific branches, custom update loops, or hardcoded identifiers into generic simulation engines (`WorkshopPhysicsEngine.cs`, `WorkshopConstraintSolver.cs`).
- Never perform mutation testing during catalogue implementation (deferred to post-catalogue E2E phase).
- Never allow kinetic + potential energy after oscillation release to exceed initial mechanical energy (strict conservation / monotonic dissipation).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Small-Angle Pendulum Oscillation | Weight (4 kg, $L = 2.0$ m) released from $15^\circ$ angle | Oscillates with natural period $T \approx 2\pi\sqrt{L_{\text{eff}}/g} \approx 3.0\text{--}3.1$ s within derived $5\%$ tolerance; zero crossings occur at consistent cadence | No error expected |
| Rope Length Scaling ($T \propto \sqrt{L}$) | Pendulums of length $L_1 = 1.0$ m and $L_2 = 4.0$ m | Measured period ratio $T_2 / T_1$ matches theoretical ratio $\sqrt{L_{\text{eff},2} / L_{\text{eff},1}}$ within $5\%$ tolerance | No error expected |
| Mass Invariance of Period | Light Weight (0.25 kg) vs Heavy Weight (8.0 kg) with matched $L_{\text{eff}} = 2.5$ m | Both oscillate with matching natural periods within $5\%$ of theory and within $3\%$ of each other | No error expected |
| Gradual Damping & Energy Conservation | Weight (4 kg, $L = 2.0$ m, $15^\circ$) swinging over 1,200 ticks (10 s) | Peak amplitude monotonically decreases cycle over cycle; total mechanical energy in local principal frame $E(t) \le 1.005 \times E_0$ | No error expected |
| 3D Non-Planar Precession | Weight displaced in both X ($15^\circ$) and Z ($10^\circ$) planes | Traces stable 3D orbital trajectory through release plane; rope tether length remains within $[L - 0.05, L + 0.05]$ m; tilt remains $< 45^\circ$ | No error expected |
| Steep-Angle Slack Release | Weight placed with $0.8$ m initial slack ($d = 1.2$ m, $L = 2.0$ m) at $75^\circ$ release angle | Weight undergoes gravitational free-fall until rope catches tautly, transferring impulse smoothly into swinging motion without NaN / solver explosion | No error expected |
| Multi-Body Pulley Lifting | Counterweight (4 kg) routed over Pulley to lift a 1 kg Weight | Counterweight descends under gravity, transferring tension to lift the 1 kg load upward past clearance height | No error expected |
| Multi-Body Balance at Rest | Equal masses (4 kg and 4 kg) connected over Pulley at rest | System remains at rest; vertical displacement $\le 0.005$ m and speed $\le 0.005$ m/s over 600 ticks | No error expected |
| Floor Contact Tension Relaxation | Counterweight (4 kg) falls and contacts landing surface over pulleys | Counterweight lands and rests stably at $y \approx 2.25$ m; rope presentation relaxes into `Slack` state | No error expected |
| Open Route Free Fall | Weight connected to pulley without opposing anchor or load | Weight falls freely under gravity to shelf surface without solver fault | No error expected |
| Wiring Direction Invariance | Reversing connection order (B $\to$ P2 $\to$ P1 $\to$ A vs A $\to$ P1 $\to$ P2 $\to$ B) | Displacements and lifting acceleration match identically regardless of wiring order | No error expected |
| Run/Reset Exact Restoration | Swinging pendulum simulated for 600 ticks, then Reset invoked | Restores original authored transform, angle, and zero velocity ($\le 1\,\mu\text{m}, \le 1\,\mu\text{rad}$, zero speed) | No error expected |
| Save/Load Persistence Roundtrip | Authored construction containing suspended pendulum encoded and decoded | Restored construction matches saved state and resumes identical oscillation trajectory | No error expected |

</intent-contract>

## Code Map

- `CuriousContraptions.tests/WeightTests.cs` -- comprehensive pendulum qualification test suite:
  - `SuspendedPendulum(mass, ropeLength, angleRad, anchorPos, angleZRad, initialRadialDistance)`
  - `MeasurePeriod(List<WorkshopRead> reads, GpuBodyId id)`
  - `TwoPulleyCounterweight(massA, massB, heightA, heightB, reverseWiring)`
  - `SuspendedPendulum_OscillatesWithTheoreticalNaturalPeriod`
  - `SuspendedPendulum_PeriodScalesWithSquareRootOfLength`
  - `SuspendedPendulum_PeriodIsInvariantWithMass`
  - `SuspendedPendulum_DampsGraduallyWithoutEnergyGrowth`
  - `SuspendedPendulum_ThreeDimensionalNonPlanarPrecession`
  - `SuspendedPendulum_SteepAngleSlackReleaseTransitionsSmoothly`
  - `Counterweight_LiftsLighterLoadOverPulley`
  - `Counterweight_EqualMassesOverPulleyRemainBalanced`
  - `Counterweight_FloorContactRelaxesTensionAndRestsOnBench`
  - `Counterweight_OpenRouteFallsFreelyToBench`
  - `Counterweight_PulleyWiringDirectionInvariance`
  - `SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity`
  - `SuspendedPendulum_SaveLoadRoundtripPreservesSwingingState`
- `_bmad-output/implementation-artifacts/spec-10-4-counterweight.md` -- this delivery spec
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- update Story 10.4 status to `done`
- `TODO.md` -- advance outcome to Story 10.5 Trampoline (CAT-065)

## Tasks & Acceptance

**Execution:**
- In `CuriousContraptions.tests/WeightTests.cs`:
  - Implement helper `SuspendedPendulum(mass, ropeLength, angleRad, anchorPos, angleZRad, initialRadialDistance)`
  - Implement `MeasurePeriod`: zero-crossings with linear interpolation on canonical $\ge 3$ crossings path
  - Implement `TwoPulleyCounterweight`: proportional geometric span lengths ensuring exact route length agreement
  - Implement `SuspendedPendulum_OscillatesWithTheoreticalNaturalPeriod`: verify $T = 2\pi\sqrt{L_{\text{eff}}/g}$ within 5%
  - Implement `SuspendedPendulum_PeriodScalesWithSquareRootOfLength`: verify $T_2 / T_1 \approx \sqrt{L_2 / L_1}$ within 5%
  - Implement `SuspendedPendulum_PeriodIsInvariantWithMass`: compare 0.25 kg and 8.0 kg on matched $L_{\text{eff}} = 2.5$ m within 3%
  - Implement `SuspendedPendulum_DampsGraduallyWithoutEnergyGrowth`: verify monotonic peak decay and principal-frame energy bound $E(t) \le 1.005 E_0$
  - Implement `SuspendedPendulum_ThreeDimensionalNonPlanarPrecession`: verify 3D swinging across release plane with bounded tether and tilt $< 45^\circ$
  - Implement `SuspendedPendulum_SteepAngleSlackReleaseTransitionsSmoothly`: verify free-fall to taut shock transition
  - Implement `Counterweight_LiftsLighterLoadOverPulley`: verify 4 kg lifts 1 kg load upward
  - Implement `Counterweight_EqualMassesOverPulleyRemainBalanced`: verify static equilibrium over 600 ticks
  - Implement `Counterweight_FloorContactRelaxesTensionAndRestsOnBench`: verify landing and rope transition to `Slack`
  - Implement `Counterweight_OpenRouteFallsFreelyToBench`: verify open route falls under gravity to shelf surface
  - Implement `Counterweight_PulleyWiringDirectionInvariance`: verify forward and reverse wiring yield identical dynamics
  - Implement `SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity`: verify $\le 1\,\mu\text{m}, \le 1\,\mu\text{rad}$, zero speed
  - Implement `SuspendedPendulum_SaveLoadRoundtripPreservesSwingingState`: verify roundtrip serialization and replay
- Run targeted tests: all 74 `WeightTests` pass with `Skipped: 0`
- Run full pre-commit verification hook: all 127 checks clean
- Conduct 5-layer adversarial review (Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment, Forward-Only Auditor)
- Apply all review fixes
- Commit and publish to `origin/main`
- Verify remote main HEAD

---

## Review Triage Log

| Layer | Finding | Disposition | Resolution |
|-------|---------|-------------|------------|
| Forward-Only Auditor | `MeasurePeriod`: 2-crossing fallback branch (`zeroCrossings.Count == 2`) violates single-canonical-path rule | patch (high) | Removed fallback branch; required $\ge 3$ zero-crossings for canonical full-period measurement. |
| Verification Gap / Blind Hunter | `SuspendedPendulum_SteepAngleSlackReleaseTransitionsSmoothly`: placed weight at `ropeLength`, starting taut rather than testing slack release | patch | Configured initial radial distance $1.2$ m with $L = 2.0$ m ($0.8$ m slack), verifying free fall before taut shock. |
| Blind Hunter | Intermediate tick-by-tick micro-pinning across 600 ticks in Reset and SaveLoad tests violates game-fidelity policy | patch | Replaced consecutive micro-pinning with milestone and terminal checks plus `run.Count == replay.Count` assertion. |
| Blind Hunter | Mass invariance test widened tolerance to 8% to mask incommensurate pendulum lengths ($L_{\text{eff}} = 2.145$ vs $2.461$ m) | patch | Matched $L_{\text{eff}} = 2.5$ m for both 0.25 kg and 8.0 kg by adjusting rope lengths for cylinder dimensions; periods match within 3%. |
| Blind Hunter | Missing floor contact test under pulley counterweight | patch | Added `Counterweight_FloorContactRelaxesTensionAndRestsOnBench`, verifying landing on shelf, stable resting, and rope transition to `Slack`. |
| Blind Hunter | Missing open/incomplete route test | patch | Added `Counterweight_OpenRouteFallsFreelyToBench`, verifying an open route weight falls freely to the shelf. |
| Blind Hunter | Missing pulley wiring direction invariance test | patch | Added `Counterweight_PulleyWiringDirectionInvariance`, verifying reverse wiring order produces identical dynamics. |
| Blind Hunter | `TwoPulleyCounterweight`: arbitrary $50/50$ remaining split caused span length distortion | patch | Computed proportional geometric span lengths matching route evaluation, eliminating artificial stretch. |
| Blind Hunter | Rotational kinetic energy used world angular velocity with local principal moments | patch | Transformed angular velocity to local frame before evaluating principal moments of inertia. |
| Edge Case Hunter | Guard against empty reads list in `MeasurePeriod` | patch | Added `if (reads.Count == 0) throw new ArgumentException(...)`. |
| Edge Case Hunter | Guard `Assert.Equal(run.Count, replay.Count)` before indexing `replay[i]` | patch | Added explicit count assertions in Reset and SaveLoad tests. |
| Edge Case Hunter | Equal masses balance duration | patch | Ran for 600 ticks and asserted vertical velocity $\le 0.005$ m/s. |
| Intent Alignment Auditor | Tests exercise native C# simulation harness; browser suite `cat-067.test.ts` remains deferred | accept | Correct per owner fast-test cadence policy and `BrowserDeferral.cs`. |

---

## Auto Run Result

- **Candidate SHA:** HEAD of `story-10-4-counterweight`
- **Solution Build:** `dotnet build CuriousContraptions.slnx -c Debug` — 0 Warning(s), 0 Error(s)
- **Targeted Test Execution:** `WeightTests` — Passed: 74, Failed: 0, Skipped: 0
- **Format & Syntax Checks:** `git diff --check` — 0 errors
- **Pre-Commit Verification:** `.githooks/pre-commit` — 127 checks clean, Anvil 80 files clean
- **Verdict:** PASS

---

## Publication Receipt — 31 October 2026

- **Committed SHA:** `9fe8676fc21604a0f14502d63baf49a6b58d4811`
- **Published Ref:** `origin/main`
- **Remote HEAD Verified:** `9fe8676fc21604a0f14502d63baf49a6b58d4811`
- **Pre-commit Gate:** Static gate clean (127 passed, 0 skipped), Anvil passed (80 snapshots, 0 warnings/errors), build 0 warnings/0 errors.
- **Slice Status:** `done` (Story 10.4 / CAT-067 qualified; Counterweight & Multi-Body Pendulum Lifting verified for small-angle harmonic period $T = 2\pi\sqrt{L_{\text{eff}}/g}$, square-root length scaling, mass invariance across boundary masses [0.25 kg, 8.0 kg], monotonic damping without energy growth, 3D non-planar precession, steep-angle slack-release shock transitions, multi-body pulley lifting and static equilibrium, floor contact tension relaxation, open route free fall, wiring direction invariance, exact Run/Reset restoration $\le 1\,\mu\text{m}, \le 1\,\mu\text{rad}$, and Save/Load persistence roundtrips).

