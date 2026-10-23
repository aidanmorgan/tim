# Current delivery

**Goal:** 150 progressively taught levels, unlimited Free Workshop and complete catalogue through named qualification gates.

**Verified:** C-Ramp+Receiver terminal scoped Pass, source8665e68b at main9b324bbd; CI39697329074 native2138/2138, artifact12392320102, deployment7240389509. [Review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md#terminal-scoped-pass--22-october-2026). Prior Both Gate9.6 terminal scoped Pass, main e036b7f9; CI39690098234 2123/2123, artifact12388827243, deployment7239438508. [Terminal publication verification](_bmad-output/implementation-artifacts/review-9-6-both-gate.md#terminal-publication-verification--22-october-2026).

**Also verified:** 9.7a Electrical NAND (CAT-024, position 25) terminal scoped native/publication Pass at main b35be7f4: CI 39706295965 native 2173/2173, artifact 12395965745, deployment 7241638673; browser pending. [Review](_bmad-output/implementation-artifacts/review-9-7a-electrical-nand.md#terminal-publication-verification--22-october-2026). The C-Ramp+Receiver corrective forward fix's terminal receipt is carried with C-Balls ([review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md)).

**Also verified (receipts carried with C-Bumper+Spring):** C-Balls (position 26) terminal scoped Pass at main 3fce7a03 ([review](_bmad-output/implementation-artifacts/review-c-balls.md)); 9.7b Electrical NOR (position 27) terminal scoped Pass at main 55ee05b9, CI 39714013919, deployment 7242636803 ([review](_bmad-output/implementation-artifacts/review-9-7b-electrical-nor.md)); browser pending for both.

**In flight:** C-Gate (position 32: 9A.19 declared prismatic drive + 9A.21 declaration-free warm-up, plus the 9.4c Blocked handoff; worktree `story-c-gate`, main 8bfddc91 merged in). Pre-merge review Pass-pending-merge ([review](_bmad-output/implementation-artifacts/review-c-gate.md), [spec and current state](_bmad-output/implementation-artifacts/spec-c-gate.md#current-state)). Blocked now needs solver contact (coordinator decision 2), so `ActuatorDriveRecord` gains `moving_contact` (saves unchanged). Reviewer findings recorded in the spec:
- F1: a ball placed 0.3 m inside the closed blade is ejected upstream. This is physically consistent and accepted. The queued cat-051 browser test 3 "downstream" expectation must be corrected (owner: post-catalogue E2E).
- F2: 9A.21 timing is advisory-not-met (native 0.20→1.42 ms; production is AOT). Measure in Chrome at the Epic 16 performance qualification.
- F3: the static-velocity fix changed 10 of 24 lesson/route trajectories (solve outcomes unchanged), so the base browser proofs are stale.
- F4: an any-substep versus last-substep contact test was added (`TheMovingContactLaneRecordsAContactInAnySubstepOfTheTick`); it kills the last-substep-only mutant M9.

Pending browser cases: `cat-051`, `cat-051-own-supply`, `cat-051-interrupt`, `cat-052`, `cat-037`, `cat-005-n19`, `cat-020`, the 9A.19 downstream-ball check and 9A.21 first-Run timing (post-catalogue E2E).

**Merged, receipts carried with C-Gate:** 9.8b Exactly one gate (CAT-027, position 31) is merged to main 8bfddc91 ([spec and evidence](_bmad-output/implementation-artifacts/spec-9-8b-electrical-xor.md#current-state)).

**Merged, receipts pending (9.4c):** 9.4c latch-fixture integrations (position 30, guards C-Gate) is merged to main a598a09e: native `LatchSupplyFixtureTests` (10 cases); pre-merge review Pass-pending-merge ([review](_bmad-output/implementation-artifacts/review-9-4c-latch-fixture-deferred-integrations.md)). The supply lag is one tick: coordinator decision under owner delegation, 22 Oct 2026 (spec D3), which supersedes the "+2" wording in the frozen Set row, AC1 and CAT-037 T14. Pending browser cases `cat-005-n19` and `cat-051-interrupt` (post-catalogue E2E). **Handoff (coordinator decision, 22 Oct 2026):** `ActuatorDriveRead.Blocked` reads false for a blade resting on a ball (−1.083e-5 m/s residual vs the 1e-5 threshold, so the indicator shows unpowered). Owner: the C-Gate implementation owner; native proof required; the threshold is CAT-051 open question 3. [Spec](_bmad-output/implementation-artifacts/spec-9-4c-latch-fixture-deferred-integrations.md#evidence).

**Merged, receipts pending:** 9.8a Either gate (CAT-026, position 29: package `elements/ElectricalOr` and the monotone Or law; Or-only loops admitted at their least fixed point, mixed loops refused) is merged to main a2eddf81 ([review](_bmad-output/implementation-artifacts/review-9-8a-electrical-or.md), [spec and evidence](_bmad-output/implementation-artifacts/spec-9-8a-electrical-or.md#current-state)); its terminal receipt stays with its owner. C-Bumper+Spring (position 28: Pinball Bumper and Springboard as generic instances, [spec](_bmad-output/implementation-artifacts/spec-c-bumper-spring.md#evidence)) is merged to main 711568c4; its terminal receipt is carried with 9.4c ([review](_bmad-output/implementation-artifacts/review-c-bumper-spring.md)).

**Owner / next check:** C-Gate implementation owner, retained for reviewer findings. Full `dotnet test CuriousContraptions.slnx --no-build --no-restore` on one matching 0-warning Debug build of the merged candidate (totals in the spec evidence); CI tools step and Release worker clean. Next: independent review and SnapshotApproval of the merged snapshot, then normal publication. No known product blocker; browser qualification deferred.

**Verification:** Production C# UNIT tests PRIMARY, integration supplemental. Real behavior/control/boundary/ticks/Reset/save-load; one matching build per changed input/config then --no-build --no-restore. Chrome/E2E deferred until all catalogue elements complete.

**Cleanup/design:** Explicit record/factory/compiler/capture/whitelist/caller deletion, package-owned declarations and typed shared engines. No fallback, compatibility bridge or new platform.

**Next:** publish C-Gate (position 32) after its SnapshotApproval, then the next ready roadmap item (coordinator). Preserve unrelated main dirt and historical evidence.
