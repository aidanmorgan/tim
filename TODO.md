# Current delivery

**Goal:** 150 progressively taught levels, unlimited Free Workshop and complete catalogue through named qualification gates.

**Verified:** C-Ramp+Receiver terminal scoped Pass, source8665e68b at main9b324bbd; CI39697329074 native2138/2138, artifact12392320102, deployment7240389509. [Review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md#terminal-scoped-pass--22-october-2026). Prior Both Gate9.6 terminal scoped Pass, main e036b7f9; CI39690098234 2123/2123, artifact12388827243, deployment7239438508. [Terminal publication verification](_bmad-output/implementation-artifacts/review-9-6-both-gate.md#terminal-publication-verification--22-october-2026).

**Also verified:** 9.7a Electrical NAND (CAT-024, position 25) terminal scoped native/publication Pass at main b35be7f4: CI 39706295965 native 2173/2173, artifact 12395965745, deployment 7241638673; browser pending. [Review](_bmad-output/implementation-artifacts/review-9-7a-electrical-nand.md#terminal-publication-verification--22-october-2026). The C-Ramp+Receiver corrective forward fix's terminal receipt is carried with C-Balls ([review](_bmad-output/implementation-artifacts/review-c-ramp-receiver.md)).

**Also verified (receipts carried with C-Bumper+Spring):** C-Balls (position 26) terminal scoped Pass at main 3fce7a03 ([review](_bmad-output/implementation-artifacts/review-c-balls.md)); 9.7b Electrical NOR (position 27) terminal scoped Pass at main 55ee05b9, CI 39714013919, deployment 7242636803 ([review](_bmad-output/implementation-artifacts/review-9-7b-electrical-nor.md)); browser pending for both.

**In flight:** 9.4c latch-fixture integrations (position 30, guards C-Gate; worktree `story-9-4c`, main a2eddf81 merged in): native `LatchSupplyFixtureTests` (10 cases); pre-merge review Pass-pending-merge ([review](_bmad-output/implementation-artifacts/review-9-4c-latch-fixture-deferred-integrations.md)). The supply lag is one tick: coordinator decision under owner delegation, 22 Oct 2026 (spec D3), which supersedes the "+2" wording in the frozen Set row, AC1 and CAT-037 T14. Pending browser cases `cat-005-n19` and `cat-051-interrupt` (post-catalogue E2E). **Handoff (coordinator decision, 22 Oct 2026):** `ActuatorDriveRead.Blocked` reads false for a blade resting on a ball (−1.083e-5 m/s residual vs the 1e-5 threshold, so the indicator shows unpowered). Owner: the C-Gate implementation owner; native proof required; the threshold is CAT-051 open question 3. [Spec](_bmad-output/implementation-artifacts/spec-9-4c-latch-fixture-deferred-integrations.md#evidence).

**In re-review:** 10.1 Solid Cylinder Collider & Inertia (position 33, engine-only, no package; worktree `story-10-1`, base a598a09e, uncommitted).
- **Review:** F1–F3 are resolved. Re-review 1 found F4 (dynamic contact energy), N5 (overlap depth) and N6 (stale comments).
- **Fix round 2 (done):**
  - Side-on-side stretches now need axes parallel within 2°, each end measured to the partner's side.
  - A shallow overlap is measured on the shrunk core, with a depth clamp.
  - New native tests use the reviewer's certified poses.
  - 29 of 32 mutation probes are killed; f4 and m6 survive and are disclosed.
  - Evidence is under `evidence-10-1/round3/`.
- **Re-review 2:** Fail on residual F4 (fresh seeds). This is the third failed round, so pose-patching has stopped.
  - Steps 1–3 are done: the attempts record, the cylinder–cylinder core deleted with a pinning walk test, and a fresh-seed gate (≥ 1000 cases per class) that is red.
  - **Round 4 (done):** the diagnostic's single side-emission rule (E2 + E4 + E16), with a reduction to four, the closest point kept and the core gap clamped. The 1000-case gate is 0 over the bound in all 12 Dead/Rolling classes. 37 of 41 mutation probes are killed; the 4 survivors are explained.
  - **Unmet (b), restitution:** the Lively gate (200 per class, not 1000) has 1–4 per class over the bound, worst 515 mJ against 285 mJ.
    - Restitution on multi-point manifolds gains energy engine-wide: the box control alone gains 205 mJ per tick at e = 0.3.
    - It is not established whether the cylinders' excess is their own.
    - **Coordinator decision needed.**
  - **Next:** independent re-review of round 4, then the main merge after C-Gate. [Round 4](_bmad-output/implementation-artifacts/spec-10-1-solid-cylinder.md#round-4-the-single-emission-fix-2324-oct-2026-evidence-evidence-10-1round5-mutations-round4)
- **Pending and deferred:** browser `engine-cylinder` is pending. The UI dynamic cylinder moves to 10.2 and thin tubes to 14.4.
- [Spec: fix round 2](_bmad-output/implementation-artifacts/spec-10-1-solid-cylinder.md#fix-round-2-23-october-2026-implementation-owner-review-f4-n5-n6)

**Merged, receipts pending:** 9.8a Either gate (CAT-026, position 29: package `elements/ElectricalOr` and the monotone Or law; Or-only loops admitted at their least fixed point, mixed loops refused) is merged to main a2eddf81 ([review](_bmad-output/implementation-artifacts/review-9-8a-electrical-or.md), [spec and evidence](_bmad-output/implementation-artifacts/spec-9-8a-electrical-or.md#current-state)); its terminal receipt stays with its owner. C-Bumper+Spring (position 28: Pinball Bumper and Springboard as generic instances, [spec](_bmad-output/implementation-artifacts/spec-c-bumper-spring.md#evidence)) is merged to main 711568c4; its terminal receipt is carried with 9.4c ([review](_bmad-output/implementation-artifacts/review-c-bumper-spring.md)).

**Owner / next check:** 9.4c implementation owner, retained for reviewer findings. Full `dotnet test CuriousContraptions.slnx --no-build --no-restore` on one matching 0-warning Debug build of the merged candidate (totals in the spec evidence); CI tools step and Release worker clean. Next: independent SnapshotApproval of the merged snapshot, then normal publication. No known product blocker; browser qualification deferred.

**Verification:** Production C# UNIT tests PRIMARY, integration supplemental. Real behavior/control/boundary/ticks/Reset/save-load; one matching build per changed input/config then --no-build --no-restore. Chrome/E2E deferred until all catalogue elements complete.

**Cleanup/design:** Explicit record/factory/compiler/capture/whitelist/caller deletion, package-owned declarations and typed shared engines. No fallback, compatibility bridge or new platform.

**Next:** publish 9.4c (position 30) after its SnapshotApproval, then C-Gate (position 32, which takes the Blocked-residual handoff). Preserve unrelated main dirt and historical evidence.
