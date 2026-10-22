# Current delivery

**Goal:** 150 progressively taught levels, unlimited Free Workshop and complete catalogue through named qualification gates.

**Verified:** C-Ramp+Receiver terminal scoped Pass, source8665e68b at main9b324bbd; CI39697329074 native2138/2138, artifact12392320102, deployment7240389509. [Review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md#terminal-scoped-pass--22-october-2026). Prior Both Gate9.6 terminal scoped Pass, main e036b7f9; CI39690098234 2123/2123, artifact12388827243, deployment7239438508. [Terminal publication verification](_bmad-output/implementation-artifacts/review-9-6-both-gate.md#terminal-publication-verification--22-october-2026).

**Also verified:** 9.7a Electrical NAND (CAT-024, position 25) terminal scoped native/publication Pass at main b35be7f4: CI 39706295965 native 2173/2173, artifact 12395965745, deployment 7241638673; browser pending. [Review](_bmad-output/implementation-artifacts/review-9-7a-electrical-nand.md#terminal-publication-verification--22-october-2026). The C-Ramp+Receiver corrective forward fix's terminal receipt is carried with C-Balls ([review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md)).

**In flight:** C-Balls (position 26; worktree `story-c-balls`, main b35be7f4 merged in): Basketball and Bowling ball are zero-parameter generic instances; `WorkshopBall`/`BallMaterial`/`CaptureBall` deleted, payload predicate derived, 50 Known rows removed. Merged candidate awaiting independent SnapshotApproval. [Spec and evidence](_bmad-output/implementation-artifacts/spec-c-balls.md#evidence).

**Owner / next check:** C-Balls implementation owner, retained for reviewer findings. Full `dotnet test CuriousContraptions.slnx --no-build --no-restore` on one matching 0-warning Debug build of the merged candidate (totals in the spec evidence); Release worker clean; strict TS and snapshot schema unit pass. Next: independent review, then normal publication. No known product blocker; browser qualification deferred.

**Verification:** Production C# UNIT tests PRIMARY, integration supplemental. Real behavior/control/boundary/ticks/Reset/save-load; one matching build per changed input/config then --no-build --no-restore. Chrome/E2E deferred until all catalogue elements complete.

**Cleanup/design:** Explicit record/factory/compiler/capture/whitelist/caller deletion, package-owned declarations and typed shared engines. No fallback, compatibility bridge or new platform.

**Also in flight:** C-Bumper+Spring (position 28; worktree `story-c-bumper-spring`, base 3fce7a03 = C-Balls merged). Pinball Bumper and Springboard are generic instances: the Bumper declares a `ContactWorkDescriptor` and the Springboard is the shared slider compile with generic slider presentation. Records, panels and six wire fields are deleted; 91 Known rows removed. Uncommitted candidate: full slnx 2703/2703 on one 0-warning Debug build, lesson hashes byte-identical to the record path. [Spec and evidence](_bmad-output/implementation-artifacts/spec-c-bumper-spring.md#evidence). Owner: C-Bumper+Spring implementation owner, retained for reviewer findings. Next check: independent review. Publication waits for the 9.7b NOR terminal Pass and a main merge (checklist in the spec). Browser 9R.8 pending.

**Next:** 9.7b NOR (position 27) after C-Balls is reviewed and published. Preserve unrelated main dirt and historical evidence.
