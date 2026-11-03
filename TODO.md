# Current delivery

**Outcome:** implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest publication:** engine slice `engine-revolute-drive`, `2c008ee9869e4fc7ba010313cadf82545d110e03`, fast-forward from `b54d1857`. It is the engine prerequisite of Story 11.1a Motor (CAT-042).
- Every revolute hinge can declare `Continuous` travel and a supplied velocity drive.
- Electrical supply consumers are typed (prismatic or revolute drive), and each drive is consumed exactly once.
- Each hinge commits its supplied flag, travel and positive drive work.
- Full local round 5,469/0/0 (`Skipped: 0`) and normal hooks 127/0/0.
- Five Auto layers (one Forward-Only finding fixed), plus two independent re-review rounds.
- [Receipt](./_bmad-output/implementation-artifacts/spec-engine-revolute-drive.md#publication-receipt--3-november-2026).
- CI/deployment and browser qualification remain unverified.

**Current slice:** Story 11.1a Motor ([CAT-042](./docs/planning/elements/CAT-042-motor.md)). The package declares the generic continuous hinge, the supplied drive, the bearing friction and the shaft read. Owner decisions of 3 November apply:
- the Motor spins down when unpowered;
- supply is on/off, with drain deferred;
- speed adjustable during Run is its own later slice.

The 11.1a slice must also cover the deferred compiler hookup test (see the engine spec's frontmatter).

**Blocker / owner / retry:** none. Start from published main in a fresh worktree with a fresh implementer.

**Next acceptance check:** the 11.1a spec's native tests, then the full local round and step-04 review:
- a supplied Motor shaft delivers bounded torque and work;
- an unpowered Motor delivers none and spins down;
- a stall creates no work.

**Order:**
1. Story 11.1a Motor (current). Its package declares the generic continuous hinge and supplied drive, with the speed and torque as placement parameters.
2. Engine slice `engine-revolute-drive-target`: a run-time adjustable drive target speed (the legacy runtime `speed` input, CAT-042 open question 5). It is its own slice, after 11.1a, and is not a prerequisite of it.
3. The remaining Epic 11 children in dependency order: 11.1b Conveyor, 11.2a Clutch, 11.2b Reverse transmission, 11.3 Linear pusher, 11.4a Wound spring and the remaining 11.4 child. Then the split 12.x and 13.x children, then 14.0, 14.2, 14.1, 14.3 and 14.4.
4. Remaining `named-*` slices under the identity ledger's family and dependency order.
5. Engine follow-ups queued in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md), each scheduled by its destination. These include [compliant-surface friction](./_bmad-output/implementation-artifacts/deferred-work.md#compliant-surface-friction) and the [powered-part demand drain](./_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain) (EL-196 battery drain, owner, 3 November 2026).

**Rules now:** [local authority](./docs/delivery-workflow.md#local-test-authority), [game-grade testing](./docs/testing-guidance.md), zero skips, no output hashes/mutants/fallback/compatibility; one final full local round and normal hooks. Data-driven engine: fix the generic law or its declared data, never add a per-interaction handler. Browser E2E remains in the [single deferral list](./tools/E2eRunner/BrowserDeferral.cs) until complete native catalogue closure. [Deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains later gates. Blocked duration unknown.
