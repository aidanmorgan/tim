# Current delivery

**Outcome:** implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest publication:** engine slice `engine-revolute-drive-target`, `66da1ae76afceb74089b36f054c175dafe7fcaea`, fast-forward from `015e902d`.
- The Motor's `speed` is adjustable during Run (owner, 3 November 2026; CAT-042 open question 5) through the generic run-control lane `RevoluteDriveTarget`. The hinge record's target is one committed field written each tick; rows carry the declared bounds and refuse `RunControlOutOfRange`; the run panel has a numeric control.
- Full local round 5,578/0/0 (`Skipped: 0`) and normal hooks 127/0/0.
- Five Auto layers plus one independent re-review of the fix delta, Pass.
- [Receipt](./_bmad-output/implementation-artifacts/spec-engine-revolute-drive-target.md#publication-receipt--4-november-2026). The run-control table it builds on is `engine-run-control-table` at `30fcfb56` ([receipt](./_bmad-output/implementation-artifacts/spec-engine-run-control-table.md#publication-receipt--4-november-2026)).
- CI/deployment and browser qualification remain unverified.

**Current slice:** Story 11.1b Conveyor. It must also carry, from the `engine-revolute-drive-target` review: before declaring any run-controlled speed, confirm the parameter feeds only its lane binding (a run control changes only its lane, so any other compiled use of the same parameter would keep its authored value), or add that validator rule.

**Blocker / owner / retry:** none. Start from published main `66da1ae7` in a fresh worktree with a fresh implementer.

**Next acceptance check:** the Story 11.1b spec's matrix through the real compiled Conveyor package, then the full local round and step-04 review.

**Order:**
1. Story 11.1b Conveyor (current).
2. The remaining Epic 11 children in dependency order: 11.2a Clutch, 11.2b Reverse transmission, 11.3 Linear pusher, 11.4a Wound spring and the remaining 11.4 child. Then the split 12.x and 13.x children, then 14.0, 14.2, 14.1, 14.3 and 14.4.
3. Remaining `named-*` slices under the identity ledger's family and dependency order.
4. Engine follow-ups queued in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md), each scheduled by its destination. These include [compliant-surface friction](./_bmad-output/implementation-artifacts/deferred-work.md#compliant-surface-friction), the [powered-part demand drain](./_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain) (EL-196 battery drain, owner, 3 November 2026) and the [run-control observation](./_bmad-output/implementation-artifacts/deferred-work.md#run-control-observation) for the browser phase.

**Rules now:** [local authority](./docs/delivery-workflow.md#local-test-authority), [game-grade testing](./docs/testing-guidance.md), zero skips, no output hashes/mutants/fallback/compatibility; one final full local round and normal hooks. Data-driven engine: fix the generic law or its declared data, never add a per-interaction handler. Browser E2E remains in the [single deferral list](./tools/E2eRunner/BrowserDeferral.cs) until complete native catalogue closure. [Deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains later gates. Blocked duration unknown.
