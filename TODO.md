# Current delivery

**Outcome:** implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest publication:** Story 10.3b lever pivot holding (CAT-034), `3ce30ba3e0ebb6afef3894c20a15b9cd436d4db4`, fast-forward from `140c6f07`.
- Every revolute hinge now declares a Coulomb friction torque. The Impact Lever declares 2 N·m, so equal loads stay balanced for a whole run and the lightest catalogue body still tips it.
- Full local round 5,396/0/0 (`Skipped: 0`) and normal hooks 127/0/0.
- Five Auto layers, plus two independent re-review rounds of the fix delta.
- [Receipt](./_bmad-output/implementation-artifacts/spec-10-3b-lever-pivot-hold.md#publication-receipt--2-november-2026).
- CI/deployment and browser qualification remain unverified.

**Current slice:** engine slice `engine-revolute-drive` ([spec](./_bmad-output/implementation-artifacts/spec-engine-revolute-drive.md)). It is the engine prerequisite of Story 11.1a Motor ([CAT-042](./docs/planning/elements/CAT-042-motor.md)). It adds generic hinge capabilities, all as declared data:
- `Continuous` travel;
- a supplied velocity drive;
- typed supply consumers (prismatic or revolute drives).

Each hinge commits its supplied flag, travel and positive drive work. Acceptance:
- a supplied shaft delivers bounded torque and work;
- an unsupplied shaft delivers none;
- a stalled or overloaded shaft creates no work.

**Blocker / owner / retry:** none. The candidate is staged in its worktree, awaiting the step-04 review.

**Next acceptance check:** the step-04 review of the staged candidate, then publication. After that, 11.1a starts from published main.

**Order:**
1. Engine slice `engine-revolute-drive` (current).
2. Story 11.1a Motor. Its package declares the generic continuous hinge and supplied drive, with the speed and torque as placement parameters.
3. Engine slice `engine-revolute-drive-target`: a run-time adjustable drive target speed (the legacy runtime `speed` input, CAT-042 open question 5). It is its own slice, after 11.1a, and is not a prerequisite of it.
4. The remaining Epic 11 children in dependency order: 11.1b Conveyor, 11.2a Clutch, 11.2b Reverse transmission, 11.3 Linear pusher, 11.4a Wound spring and the remaining 11.4 child. Then the split 12.x and 13.x children, then 14.0, 14.2, 14.1, 14.3 and 14.4.
5. Remaining `named-*` slices under the identity ledger's family and dependency order.
6. Engine follow-ups queued in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md), each scheduled by its destination. These include [compliant-surface friction](./_bmad-output/implementation-artifacts/deferred-work.md#compliant-surface-friction) and the [powered-part demand drain](./_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain) (EL-196 battery drain, owner, 3 November 2026).

**Rules now:** [local authority](./docs/delivery-workflow.md#local-test-authority), [game-grade testing](./docs/testing-guidance.md), zero skips, no output hashes/mutants/fallback/compatibility; one final full local round and normal hooks. Data-driven engine: fix the generic law or its declared data, never add a per-interaction handler. Browser E2E remains in the [single deferral list](./tools/E2eRunner/BrowserDeferral.cs) until complete native catalogue closure. [Deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains later gates. Blocked duration unknown.
