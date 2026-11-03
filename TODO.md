# Current delivery

**Outcome:** implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest publication:** Story 11.1a Motor (CAT-042), `5603a834f754c13106b2a64db5450715163bffe9`, fast-forward from `61c32d99`.
- The Motor package is declaration data on the generic hinge drive. Its supply lamp reads the generalised actuator read (one row per drive slot).
- Drive values now require units.
- Full local round 5,516/0/0 (`Skipped: 0`) and normal hooks 127/0/0.
- Five Auto layers (one Forward-Only doc finding fixed), plus two independent re-review rounds.
- [Receipt](./_bmad-output/implementation-artifacts/spec-11-1a-motor.md#publication-receipt--4-november-2026).
- CI/deployment and browser qualification remain unverified.

**Current slice:** engine slice `engine-tensile-drive-coupling` (owner, 4 November 2026). The problem: a rope cannot hold a supplied hinge drive at rest.
- A rim-tied rotor creeps at 0.0082 rad/s per N·m.
- A speed-0 drive lifts a rope-hung load at +0.0075 rad/s.

Fix it generically in how tensile spans and hinge drives are solved together, before any part can tie a rope to a driven shaft.

**Blocker / owner / retry:** none. Start from published main in a fresh worktree with a fresh implementer.

**Next acceptance check:** the two rope cases recorded in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md#tensile-revolute-drive-creep), restored with their assertions unchanged, plus the Motor overload case re-run. Then the full local round and step-04 review.

**Order:**
1. Engine slice `engine-tensile-drive-coupling` (current): a rope cannot hold a supplied hinge drive at rest, so a rim-tied rotor creeps (0.0082 rad/s per N·m) and a speed-0 drive lifts a rope-hung load (+0.0075 rad/s). Fix it generically in how tensile spans and hinge drives are solved together, before any part can tie a rope to a driven shaft. Its acceptance is the two rope cases in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md#tensile-revolute-drive-creep).
2. Engine slice `engine-revolute-drive-target`: a run-time adjustable drive target speed (the legacy runtime `speed` input, CAT-042 open question 5). It is its own slice, after 11.1a, and is not a prerequisite of it.
3. The remaining Epic 11 children in dependency order: 11.1b Conveyor, 11.2a Clutch, 11.2b Reverse transmission, 11.3 Linear pusher, 11.4a Wound spring and the remaining 11.4 child. Then the split 12.x and 13.x children, then 14.0, 14.2, 14.1, 14.3 and 14.4.
4. Remaining `named-*` slices under the identity ledger's family and dependency order.
5. Engine follow-ups queued in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md), each scheduled by its destination. These include [compliant-surface friction](./_bmad-output/implementation-artifacts/deferred-work.md#compliant-surface-friction) and the [powered-part demand drain](./_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain) (EL-196 battery drain, owner, 3 November 2026).

**Rules now:** [local authority](./docs/delivery-workflow.md#local-test-authority), [game-grade testing](./docs/testing-guidance.md), zero skips, no output hashes/mutants/fallback/compatibility; one final full local round and normal hooks. Data-driven engine: fix the generic law or its declared data, never add a per-interaction handler. Browser E2E remains in the [single deferral list](./tools/E2eRunner/BrowserDeferral.cs) until complete native catalogue closure. [Deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains later gates. Blocked duration unknown.
