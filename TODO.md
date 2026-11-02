# Current delivery

**Outcome:** implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest publication:** Story 10.3b lever pivot holding (CAT-034), `3ce30ba3e0ebb6afef3894c20a15b9cd436d4db4`, fast-forward from `140c6f07`.
- Every revolute hinge now declares a Coulomb friction torque. The Impact Lever declares 2 N·m, so equal loads stay balanced for a whole run and the lightest catalogue body still tips it.
- Full local round 5,396/0/0 (`Skipped: 0`) and normal hooks 127/0/0.
- Five Auto layers, plus two independent re-review rounds of the fix delta.
- [Receipt](./_bmad-output/implementation-artifacts/spec-10-3b-lever-pivot-hold.md#publication-receipt--2-november-2026).
- CI/deployment and browser qualification remain unverified.

**Current slice:** Story 11.1a Motor ([CAT-042](./docs/planning/elements/CAT-042-motor.md)), the first individual child of the split Epic 11 aggregate (`11-1a-motor`). See the [current execution plan](./_bmad-output/planning-artifacts/epics.md#current-execution-plan), item 3. Acceptance:
- a supplied shaft delivers bounded torque and work;
- an unpowered shaft delivers none;
- stalled demand never creates work.

**Blocker / owner / retry:** none. Start from published main in a fresh worktree with a fresh implementer.

**Next acceptance check:** the 11.1a spec's red-first native tests (Battery-supplied motor shaft torque, unpowered control, stall work ledger), then the full local round and step-04 review.

**Order:**
1. Epic 11 individual children in dependency order: 11.1a Motor, then 11.1b Conveyor, 11.2a Clutch, 11.2b Reverse transmission, 11.3 Linear pusher, 11.4a Wound spring and the remaining 11.4 child. Then the split 12.x and 13.x children, then 14.0, 14.2, 14.1, 14.3 and 14.4.
2. Remaining `named-*` slices under the identity ledger's family and dependency order.
3. Engine follow-ups queued in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md), including [compliant-surface friction](./_bmad-output/implementation-artifacts/deferred-work.md#compliant-surface-friction), each scheduled by its destination.

**Rules now:** [local authority](./docs/delivery-workflow.md#local-test-authority), [game-grade testing](./docs/testing-guidance.md), zero skips, no output hashes/mutants/fallback/compatibility; one final full local round and normal hooks. Data-driven engine: fix the generic law or its declared data, never add a per-interaction handler. Browser E2E remains in the [single deferral list](./tools/E2eRunner/BrowserDeferral.cs) until complete native catalogue closure. [Deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains later gates. Blocked duration unknown.
