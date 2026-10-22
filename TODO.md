# Current delivery

**Goal:** 150 progressively taught levels, unlimited Free Workshop and complete catalogue through named qualification gates.

**Verified:** C-Ramp+Receiver terminal scoped Pass, source8665e68b at main9b324bbd; CI39697329074 native2138/2138, artifact12392320102, deployment7240389509. [Review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md#terminal-scoped-pass--22-october-2026). Prior Both Gate9.6 terminal scoped Pass, main e036b7f9; CI39690098234 2123/2123, artifact12388827243, deployment7239438508. [Terminal publication verification](_bmad-output/implementation-artifacts/review-9-6-both-gate.md#terminal-publication-verification--22-october-2026).

**In flight:** C-Balls (position 26; worktree `story-c-balls`, base 59ecb70b): Basketball and Bowling ball are zero-parameter generic instances; `WorkshopBall`/`BallMaterial`/`CaptureBall` deleted, payload predicate derived, 50 Known rows removed. Candidate awaiting independent SnapshotApproval (not committed); 9.7a NAND runs in parallel and main must be merged in before review. [Spec and evidence](_bmad-output/implementation-artifacts/spec-c-balls.md#evidence).

**Owner / next check:** C-Balls implementation owner, retained for reviewer findings. `dotnet test CuriousContraptions.slnx --no-build --no-restore` 2638/2638 on a matching 0-warning Debug build; Release worker clean; strict TS and snapshot schema unit pass. Next: merge main, independent review, then normal publication. No known product blocker; browser qualification deferred.

**Verification:** Production C# UNIT tests PRIMARY, integration supplemental. Real behavior/control/boundary/ticks/Reset/save-load; one matching build per changed input/config then --no-build --no-restore. Chrome/E2E deferred until all catalogue elements complete.

**Cleanup/design:** Explicit record/factory/compiler/capture/whitelist/caller deletion, package-owned declarations and typed shared engines. No fallback, compatibility bridge or new platform.

**Next:** NAND after the forward fix is reviewed and published. Preserve unrelated main dirt and historical evidence.
