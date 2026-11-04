# Current delivery

**Outcome:** implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest publication:** engine slice `engine-run-control-table`, `30fcfb56218342e4b94bea6cb86eff4c671dc6cb`, fast-forward from `78ff003c`.
- One typed run-control path replaces the Battery-only control: a compiled run-control table and one `ConfigureRunControl` command. Battery enable is its first row (`SourceEnable`), with no player-visible change.
- Deleted: `ConfigureElectrical`, `WorkshopElectricalControl`, `ElectricalControl` (fbs), `EditableSources` and `HasRunControlledSource`.
- Full local round 5,538/0/0 (`Skipped: 0`) and normal hooks 127/0/0.
- Five Auto layers plus one independent re-review of the fix delta, Pass.
- [Receipt](./_bmad-output/implementation-artifacts/spec-engine-run-control-table.md#publication-receipt--4-november-2026).
- CI/deployment and browser qualification remain unverified.

**Current slice:** engine slice `engine-revolute-drive-target`: the `RevoluteDriveTarget` run-control lane, the Motor `speed` run control and its numeric UI control (owner, 3 November 2026; CAT-042 open question 5). It must also carry:
- the several-bindings validator refusal test (constructible once a second lane exists);
- a committed-value check of the new lane in `WorkshopSimulation.ValidateAdvancedRead`, so a candidate that drops a speed change faults;
- the first real use of `RunControlValueKind.Scalar`, which replaces the UI's `UnreachableException` arms with the numeric control.

**Blocker / owner / retry:** none. Start from published main `30fcfb56` in a fresh worktree with a fresh implementer.

**Next acceptance check:** the `engine-revolute-drive-target` spec's matrix through the real compiled Motor package, then the full local round and step-04 review.

**Order:**
1. Engine slice `engine-revolute-drive-target` (current): the `RevoluteDriveTarget` lane, the Motor `speed` run control and its numeric UI control (CAT-042 open question 5).
2. Story 11.1b Conveyor.
3. The remaining Epic 11 children in dependency order: 11.2a Clutch, 11.2b Reverse transmission, 11.3 Linear pusher, 11.4a Wound spring and the remaining 11.4 child. Then the split 12.x and 13.x children, then 14.0, 14.2, 14.1, 14.3 and 14.4.
4. Remaining `named-*` slices under the identity ledger's family and dependency order.
5. Engine follow-ups queued in [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md), each scheduled by its destination. These include [compliant-surface friction](./_bmad-output/implementation-artifacts/deferred-work.md#compliant-surface-friction) and the [powered-part demand drain](./_bmad-output/implementation-artifacts/deferred-work.md#powered-part-demand-drain) (EL-196 battery drain, owner, 3 November 2026).

**Rules now:** [local authority](./docs/delivery-workflow.md#local-test-authority), [game-grade testing](./docs/testing-guidance.md), zero skips, no output hashes/mutants/fallback/compatibility; one final full local round and normal hooks. Data-driven engine: fix the generic law or its declared data, never add a per-interaction handler. Browser E2E remains in the [single deferral list](./tools/E2eRunner/BrowserDeferral.cs) until complete native catalogue closure. [Deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains later gates. Blocked duration unknown.
