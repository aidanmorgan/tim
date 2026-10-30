---
title: 'Engine: supported-impact restitution'
type: 'feature'
created: '2026-10-30'
status: 'done'
baseline_revision: '04d9b4bb75293316c30205c85316f332e3cdf7e1'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** In `WorkshopConstraintSolver.RestitutionPass`, restitution is solved only across bouncing manifolds ($|v_{rel}| > \text{Threshold}$); resting supporting contacts ($|v_{rel}| \approx 0$) are omitted. When a body strikes a resting dynamic body (e.g. a ball dropped onto a box resting on the bench), the resting body yields into its support without reacting, losing up to 93% of the rebound apex energy.

**Approach:** During the substep restitution pass, group bouncing contacts with the active supporting contacts ($c.MaxImpulse > 0$) of touched dynamic bodies. Couple the constraints using generalized multi-body `CouplingMass` across manifolds, targeting $-e \cdot v_{rel}$ on bouncing contacts and unilateral non-penetration ($v_n \ge 0, \Delta \lambda \ge 0$) on supporting contacts via `SolveActiveSet`, bounded by the thermodynamic substep energy limiter.

## Boundaries & Constraints

**Always:**
- Forward-only refactoring: zero compatibility shims, zero fallback branches, and no element-specific branches in the core physics solver.
- All simulation arithmetic remains IEEE-754 single-precision float (`f32`).
- Thermodynamic energy invariant: no free energy. Kinetic energy of any restituted group after the pass may not exceed $\max(E_{\text{relaxed}}, E_{\text{pre-solve}})$.
- Native C# unit tests execute real production simulation (`WorkshopPhysicsEngine`, `LawScene`, `LawWorld`) with `Skipped: 0`.
- Observable behavioral assertions only; zero output-hash or byte-identity pins.

**Never:**
- Never create element-specific or shape-specific solver branches for supported impacts.
- Never introduce backward compatibility wrappers, legacy modes, or fallback paths.
- Never assert golden hashes or arbitrary private fields.
- No mutation testing until post-catalogue E2E phase.
- Browser tests remain explicitly deferred in `tools/E2eRunner/BrowserDeferral.cs`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Ball on resting dynamic box ($e=0.55$) | Ball (0.6 kg, $r=0.12$ m) dropped 0.6 m onto resting box (2 kg, half $0.3\times 0.1\times 0.3$ m) on bench, material $e=0.55$ | Rebound apex ratio matches static support limit within discrete gravity/contact tolerances ($\ge 0.08$ vs static 0.0918, whereas baseline was 0.0201) | Substep energy clamp prevents gain |
| Ball on light resting dynamic box ($e=0.55$) | Ball (0.6 kg) dropped 0.6 m onto light resting box (0.5 kg) on bench | Rebound apex ratio matches supported limit ($\ge 0.07$, whereas baseline collapsed to 0.0063) | Substep energy clamp prevents gain |
| Fully elastic supported impact ($e=1.0$) | Ball (0.6 kg) dropped 0.6 m onto resting box (0.3 kg and 2 kg) on bench, $e=1.0$ | Rebound apex ratio matches static limit within 5% ($\ge 0.90$ vs static 0.9764, whereas baseline was 0.43–0.45) | Substep energy clamp prevents gain |
| Inelastic control ($e=0$) | Ball dropped 0.6 m onto resting box on bench with $e=0$ | Zero rebound: ball never leaves the box top ($h_{\text{rebound}} \le 1$ mm) | Dissipated without oscillation |
| Free-fall mid-air collision (unsupported control) | Ball strikes free-falling box in zero gravity / mid-air with no support | Pair conserves linear momentum $m_A v_A + m_B v_B$ and relative separation speed equals $e |v_{rel}|$ | SOTS active set solve |
| Zero energy gain under random poses | 1 kg box dropped in 200 random poses onto 2 kg box resting on bench at $e \in \{0.3, 0.6, 0.9, 0.95, 1.0\}$ | Every committed tick gains at most $\sum m \cdot g \cdot s_{\text{slop}}$ (3 Slop) | Group energy scale clamps $\le 1.0$ |

</intent-contract>

## Code Map

- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`:
  - `CouplingMass(ContactPoint ci, ContactPoint cj)`: Generalize from same-pair-only assumption to multi-body Jacobian coupling across all shared bodies, maintaining bit-compatibility for same-manifold pairs.
  - `RestitutionPass(List<ContactManifold> manifolds, List<SimBody> touched, int stamp)`: Identify touched bodies from bouncing manifolds and union active supporting contacts ($c.MaxImpulse > 0$) into the restitution island.
  - `ApplyRestitution(...)`: Generalized to solve coupled active set with target $-e \cdot v_{rel}$ for bouncing contacts and $0$ for supporting contacts.
  - Group energy limiter: retains existing group kinetic energy budget $\max(E_{\text{relaxed}}, E_{\text{pre-solve}})$.
- `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs`:
  - Add formal behavioral tests for supported impacts covering light ($0.5$ kg), medium ($2$ kg), and heavy supports at $e=0.55$ and $e=1.0$, with $e=0$ inelastic control and energy boundedness.

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs` -- update `CouplingMass` and `RestitutionPass` to couple bouncing points with active supporting contacts of touched dynamic bodies -- prevents resting support bodies from yielding downward and destroying impact restitution.
- `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs` -- add supported-impact behavioral tests (rebound apex bounds, inelastic control, energy preservation) -- verifies supported impact fidelity and ensures zero regressions across existing restitution tests.

**Acceptance Criteria:**
- Given a ball dropped 0.6 m onto a dynamic box resting on the bench with material $e=0.55$, when the simulation steps through the impact, then the ball rebounds with apex height ratio $\ge 0.07$ for both light (0.5 kg) and medium (2 kg) boxes (recovering the lost bounce), with no energy gain.
- Given the same configuration with $e=1.0$, when the simulation steps through the impact, then the rebound apex ratio is $\ge 0.90$.
- Given the same configuration with $e=0$, when the simulation steps through the impact, then the ball does not rebound ($h_{\text{rebound}} \le 1$ mm above resting).
- Given 200 random pose drops of a box onto a resting dynamic box at $e \in \{0.3, 0.6, 0.9, 0.95, 1.0\}$, no committed tick gains more than $\sum m \cdot g \cdot s_{\text{slop}}$.
- Given all existing tests in `CuriousContraptions.tests`, 100% pass with `Skipped: 0`.

## Spec Change Log

## Review Triage Log

### 2026-10-30 — Review pass
- verdicts: 13 findings — high 3, medium 5, low 5, false 0, maybe-false 0
- findings:
  - `[high]` `[patch]` Regression in `WorkshopPhysicsEngineTests.TheTwoLaneSetupSeparatesTheBallKindsAndBothStruckBallsComeToRest`: `ApplyRestitution` omitted `matrix * points[j].LambdaN` from RHS and applied `solution[i]` as delta rather than total accumulated impulse $\lambda_N$ — restored accumulated LCP formulation; `TheTwoLaneSetupSeparatesTheBallKindsAndBothStruckBallsComeToRest` now passes.
  - `[high]` `[patch]` In `ApplyRestitution`, `solution[i]` is total impulse $\lambda_N$: delta is $\Delta \lambda = \text{solution}[i] - c.\Lambda_N$ — resolved by restoring accumulated LCP formulation with `ApplyNormalImpulse`.
  - `[high]` `[patch]` `Supports(ContactPoint c)` restricted supports to static-dynamic pairs `(c.A.Motion != Dynamic || c.B.Motion != Dynamic)`, excluding dynamic-on-dynamic stacked supports — removed restriction; `Supports` now identifies all resting contacts with $c.\text{MaxImpulse} > 0$ and $c.\text{RelVel} \ge -c.\text{Threshold}$.
  - `[medium]` `[patch]` Transitive island expansion loop in `RestitutionPass` was ineffective when dynamic-dynamic supports were excluded — resolved by expanding dynamic-dynamic supports; multi-box stacks now couple transitively to ground.
  - `[medium]` `[patch]` Supporting manifolds excluded from post-restitution friction/rolling updates — updated `RestitutionPass` to run friction and rolling sweeps across all manifolds in `s_islandManifolds`.
  - `[medium]` `[patch]` Active-set combinatorial explosion risk if island point count grows large — capped `s_islandPoints.Count <= 8` prioritizing bouncing points, bounding worst-case active set search.
  - `[medium]` `[patch]` Unverified off-center angular cross-coupling across multi-body contact manifolds in `CouplingMass` — added `AnOffCenterDropOnARestingBoxReboundsWithCoupledSpin` asserting positive rebound and spin.
  - `[medium]` `[patch]` Unverified dynamic multi-box stack support propagation — added `ABallDroppedOntoATwoBoxDynamicStackReboundsWithoutCollapsing` verifying rebound without stack collapse.
  - `[low]` `[reject]` Absence of Tikhonov diagonal regularization in `ApplyRestitution` — rejected: `docs/gpu-f32-physics.md#exact-f32-consequences` forbids arbitrary matrix regularization, enforcing normalized pivot rejection at $64 \times 2^{-24}$.
  - `[low]` `[patch]` ThreadStatic collections `s_islandPoints` and `s_islandManifolds` lifetime cleanup — wrapped island restitution loop in `try ... finally` block ensuring collections are always cleared.
  - `[low]` `[patch]` Spec Scenario 6 (200 random pose drops) explicitly present in test file — verified covered by pre-existing and expanded `BoxesDroppedOnARestingDynamicBoxNeverGainMoreThanTheSlopInATick` (all 5 member theories passing).
  - `[low]` `[patch]` Intra-manifold mixed bouncing and resting contacts discarded supporting points when `hasBounce` was true — updated manifold collection to include active supporting contacts from resting support manifolds of touched dynamic bodies.
  - `[low]` `[patch]` Missing transverse and angular velocity assertions in `AFreeFallingMidAirCollisionConservesMomentumAndSeparatesAtRestitution` — added assertions verifying transverse and angular velocities remain zero ($< 10^{-4}$).

## Design Notes

### Multi-Body Coupling Mass
For any two contacts $c_i$ and $c_j$ with normals $n_i$ pointing from $B_i$ to $A_i$ and $n_j$ pointing from $B_j$ to $A_j$:
$$K_{ij} = \sum_{\text{body } b} J_{i,b} M_b^{-1} J_{j,b}^T$$
where each shared dynamic body contributes:
- linear: $(s_i \cdot s_j) (n_i \cdot n_j) / m_b$
- angular: $(s_i \cdot s_j) (r_{b,i} \times n_i)^T I_b^{-1} (r_{b,j} \times n_j)$
where $s_i = +1$ if $b = A_i$ and $-1$ if $b = B_i$.

### Restitution Active Set with Supports
For bouncing points: target is $-e \cdot v_{rel}$, $\Delta \lambda \ge 0$.
For supporting points: target is $0$ (preventing downward yield into support), $\Delta \lambda \ge 0$.
The unified LCP system $K \lambda \ge b$ is solved by `SolveActiveSet`, exactly preserving the existing active-set algorithm without separate code paths.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx -c Debug` -- expected: 0 warnings, 0 errors
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter "FullyQualifiedName~WorkshopPhysicsLawRestitutionTests"` -- expected: all pass, 0 failed, 0 skipped
- `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` -- expected: all pass, 0 failed, 0 skipped
