# Current delivery

**Goal:** 150 progressively taught levels, unlimited Free Workshop and complete catalogue through named qualification gates.

**Verified:** C-Ramp+Receiver terminal scoped Pass, source8665e68b at main9b324bbd; CI39697329074 native2138/2138, artifact12392320102, deployment7240389509. [Review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md#terminal-scoped-pass--22-october-2026). Prior Both Gate9.6 terminal scoped Pass, main e036b7f9; CI39690098234 2123/2123, artifact12388827243, deployment7239438508. [Terminal publication verification](_bmad-output/implementation-artifacts/review-9-6-both-gate.md#terminal-publication-verification--22-october-2026).

**Also verified:** 9.7a Electrical NAND (CAT-024, position 25) terminal scoped native/publication Pass at main b35be7f4: CI 39706295965 native 2173/2173, artifact 12395965745, deployment 7241638673; browser pending. [Review](_bmad-output/implementation-artifacts/review-9-7a-electrical-nand.md#terminal-publication-verification--22-october-2026). The C-Ramp+Receiver corrective forward fix's terminal receipt is carried with C-Balls ([review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md)).

**Also verified (receipts carried with C-Bumper+Spring):** C-Balls (position 26) terminal scoped Pass at main 3fce7a03 ([review](_bmad-output/implementation-artifacts/review-c-balls.md)); 9.7b Electrical NOR (position 27) terminal scoped Pass at main 55ee05b9, CI 39714013919, deployment 7242636803 ([review](_bmad-output/implementation-artifacts/review-9-7b-electrical-nor.md)); browser pending for both.

**In flight:** 9.8a Either gate (CAT-026, position 29; worktree `story-9-8a`, main 711568c4 merged in): package `elements/ElectricalOr` and the monotone Or law; Or-only loops admitted at their least fixed point, mixed loops refused; pre-merge review Pass-pending-merge ([review](_bmad-output/implementation-artifacts/review-9-8a-electrical-or.md)). [Spec and evidence](_bmad-output/implementation-artifacts/spec-9-8a-electrical-or.md#current-state). C-Bumper+Spring (position 28: Pinball Bumper and Springboard as generic instances, [spec](_bmad-output/implementation-artifacts/spec-c-bumper-spring.md#evidence)) is merged to main 711568c4; its terminal receipt stays with its owner.

**Owner / next check:** 9.8a implementation owner, retained for reviewer findings. Full `dotnet test CuriousContraptions.slnx --no-build --no-restore` on one matching 0-warning Debug build of the merged candidate (totals in the spec evidence); CI tools step and Release worker clean. Next: independent SnapshotApproval of the merged snapshot, then normal publication. No known product blocker; browser qualification deferred.

**Verification:** Production C# UNIT tests PRIMARY, integration supplemental. Real behavior/control/boundary/ticks/Reset/save-load; one matching build per changed input/config then --no-build --no-restore. Chrome/E2E deferred until all catalogue elements complete.

**Cleanup/design:** Explicit record/factory/compiler/capture/whitelist/caller deletion, package-owned declarations and typed shared engines. No fallback, compatibility bridge or new platform.

**Next:** publish 9.8a OR (position 29) after its SnapshotApproval, then the next roadmap position. Preserve unrelated main dirt and historical evidence.
