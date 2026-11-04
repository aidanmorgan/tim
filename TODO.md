# Current delivery

**Outcome:** implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest publication:** engine slice `engine-tensile-drive-coupling`, `c2637290826d2d3d188f8de06b7276d6de281931`, fast-forward from `557c7de4`.
- A tensile (rope) row is rigid in relax, like contacts: stabilisation softness acts only in the solve pass. A rope can now hold a supplied Motor at rest.
- The two 11.1a rope cases are restored (recorded assertions plus review guards), with a new lift case, a static-hang theory and a rope-held prismatic drive.
- Full local round 5,524/0/0 (`Skipped: 0`) and normal hooks 127/0/0.
- Five Auto layers plus two independent re-reviews, Pass.
- [Receipt](./_bmad-output/implementation-artifacts/spec-engine-tensile-drive-coupling.md#publication-receipt--4-november-2026).
- CI/deployment and browser qualification remain unverified.

**Current slice:** engine slice `engine-revolute-drive-target`: a run-time adjustable drive target speed (the legacy runtime `speed` input, CAT-042 open question 5).

**Blocker / owner / retry:** none. Start from published main in a fresh worktree with a fresh implementer.

**Next acceptance check:** the `engine-revolute-drive-target` spec's matrix through the real compiled Motor package, then the full local round and step-04 review.

**Order:**
1. Engine slice `engine-revolute-drive-target` (current): a run-time adjustable drive target speed (the legacy runtime `speed` input, CAT-042 open question 5).
2. Story 11.1b Conveyor.
3. The remaining Epic 11 children in dependency order: 11.2a Clutch, 11.2b Reverse transmission, 11.3 Linear pusher, 11.4a Wound spring and the remaining 11.4 child. Then the split 12.x and 13.x children, then 14.0, 14.2, 14.1, 14.3 and 14.4.
4. Remaining `named-*` slices under the identity ledger's family and dependency order.
5. Engine follow-ups queued in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md), each scheduled by its destination. These include [compliant-surface friction](./_bmad-output/implementation-artifacts/deferred-work.md#compliant-surface-friction) and the [powered-part demand drain](./_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain) (EL-196 battery drain, owner, 3 November 2026).

**Rules now:** [local authority](./docs/delivery-workflow.md#local-test-authority), [game-grade testing](./docs/testing-guidance.md), zero skips, no output hashes/mutants/fallback/compatibility; one final full local round and normal hooks. Data-driven engine: fix the generic law or its declared data, never add a per-interaction handler. Browser E2E remains in the [single deferral list](./tools/E2eRunner/BrowserDeferral.cs) until complete native catalogue closure. [Deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains later gates. Blocked duration unknown.
