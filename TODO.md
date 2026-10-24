# Current delivery

**Goal:** 150 progressively taught levels, unlimited Free Workshop, and the complete catalogue through named qualification gates.

**Verified (terminal scoped native/publication Pass, browser pending):**
- Electrical logic: 9.6 Both, 9.7a NAND, 9.7b NOR, 9.8a OR, 9.8b XOR.
- Conversions: C-Ramp+Receiver, C-Balls, C-Bumper+Spring, 9.4c, C-Gate, 9A.39 Battery, C-Lamp+Detector, 9A.32 Domino, C-Plate, C-Clock, C-Memory. All element record conversions are done; the remaining Known rows belong to Pipe (9A.45).
- 9A.22 event ring producer.
- 10.1 solid cylinder.
- Engine restitution energy correctness (main 0cba945b).
- Review records are in `_bmad-output/implementation-artifacts/review-*.md`.

**Blocker:** main CI has been red since 7143a874 (CI no-skip slice). Two game node harnesses were timing-dependent on Linux x64.
- Owner: forward-fix slice `story-ci-harness-determinism`.
- Retry condition: CI green on main.
- No other slice publishes until then.

**In flight, waiting for green main:**
- 9A.23 + 9A.25b ring consumers and one-shot bindings. Review is Pass-pending-main-green. The ring's shared memory is encapsulated behind an atomic word API in JS and C#.
- 10.2a Weight. Review is Pass-pending-main-green.

**Priority order after main is green** (coordinator, 24 Oct 2026, from owner directives):
1. Publish 9A.23 + 9A.25b, then 10.2a.
2. **9A.44 f32 everywhere** (owner priority). Part A (solver/wire): every half-precision lane removed, guarded by `HalfPrecisionGuardTests`; implemented, awaiting independent review and publication. Part B (presentation/clock): remove every `double` game value (`double` only at named external adapters), integer-tick game time plus an f32 phase, and a guard; not started.
3. Engine: lying cylinder axial creep (spec ready; orientation remainder approved).
4. Workshop integration harness. A is the shared deterministic native harness plus migration; B is the cross-element coverage matrix, with every lesson at 3 precisions.
5. TypeScript conversion of the wwwroot runtime JS and the node harnesses (pinned `tsc` in build/CI), including the same encapsulation for the pose ring and other shared-memory views.
6. Test hygiene: replace output-hash pins with behavioural assertions, turn hash-pinned fixtures into fixture files, and remove private-member reach-ins (e.g. WorkshopActivationAnimationTests) per `docs/testing-guidance.md`.
7. Deletion lane: 9A.24 typed observation port, then 9A.45 final isolation gate (Pipe arms, unused `TimerObservation`).
8. Element lane: 10.2b Rope anchor (rope law plus the owner-mandated rope cut), 10.2c Pulley, 10.3 Lever, 10.4 pendulum qualification, 10.5 Trampoline, 11.x, 12.x, 13.x, 14.x, in epics delivery order.

**Verification (binding):**
- `docs/testing-guidance.md` applies: behaviour through defined interfaces, browser-free integration through the shared harness, derived oracles, mutation-proven, deterministic.
- No skipped tests and no output-hash assertions (AGENTS.md).
- One matching build, then `--no-build --no-restore`.
- Never `--blame-crash` on the full solution.

**Deferred browser phase:** all browser/E2E suites stay disabled through the one explicit list (`tools/E2eRunner/BrowserDeferral.cs`). They run only when every named puzzle element is implemented and passing its unit and integration tests.

**Deferred engine follow-ups (in `deferred-work.md`):** resting-stack settling, supported-impact restitution, test-host SIGSEGV under concurrent load, and the cylinder narrowphase cost (performance gate).
