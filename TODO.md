# Current delivery

**Goal:** 150 progressively taught challenge levels, unlimited Free Workshop and the complete catalogue through the named qualification gates.

**Verified outcome:** [Story 9.4b Cumulative Chrome Regression Repair](_bmad-output/planning-artifacts/epics.md#story-94b-cumulative-chrome-regression-repair) has Murdoch-2's round-5 terminal scoped Pass (19 Oct 2026). It passed on candidate 6: source `d7536a1c…`, bundle `102e9f12…`.
- **Tier 1:** build 0 warnings; 1523/1523 native tests plus 78/78 Coverage; node 76/76; Lifecycle 510/0; Wire 109/0.
- **Tier 2:** clock-master 5×8/8, cat-017, cat-023b and the smoke pair.
- **Full serial runs:** candidate 5's full cumulative runs passed 140/140 twice. Candidate 6 changed only the renewal-conclusion predicate.
- **Design:** the clock is now a JS clock master with typed probes; a lapse is renewed and checked, and is recalibrated only on a real move or a stall over about 1.2 s.
- [Review](_bmad-output/implementation-artifacts/review-9-4b-cumulative-regression-repair.md); [spec and evidence](_bmad-output/implementation-artifacts/spec-9-4b-cumulative-regression-repair.md).

**Also verified:** Story 9R.0c (the Balloon crash) has Murdoch-4's scoped Pass on `8de7f259`.
- Tennis and Balloon have left the palette through a typed undelivered set; Story 12.1 returns them as real packages with their own art.
- Bench placement can't crash. No UI handler exception can kill the browser runtime: all 19 are guarded.
- Results: 1539/1539 native tests; one full Chrome run of 34 files, 165/165; palette-placement 25/25.

**Also verified:** Story 9R.0, the real C# static gate. It has Murdoch-3's round-7 verdict on `ea4edab4`: analyzers run with warnings as errors on every C# project, and CI runs on every push and PR.

**Current outcome (21 Oct 2026):**
- Order: the epics.md [Delivery order](_bmad-output/planning-artifacts/epics.md#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination), a critical path to Story 9.5, the first new element.
- Done today: 9A.14+18, 9A.2, 9A.15 and 9A.17. Status is in `sprint-status.yaml`.
- In flight: 9A.3 (typed transport commands), then T.1, 9A.16, T.2, 9A.25a, 9A.27, 9A.45a, 9.5a and 9.5.

**Verification policy:** [verification tiers](_bmad-output/planning-artifacts/autonomous-development-guide.md#verification-tiers), the single current rule set (consolidated 21 Oct 2026).
- Every candidate runs Tier 1 (native, minutes). Every story runs the Tier 2 impact-selected Chrome suites once.
- A Story Full run is needed only for an engine change in a family that Story T.2's native replay does not yet cover.
- A background checkpoint Full run on main follows every 5 merged stories.
- Fast-test lane, in epics.md: T.1 (type-check, selection, calibration) and T.2 (native fixture replay) are on the critical path; T.3 follows 9.5.

**Blocker / owner / retry:** None.

**Next acceptance check:** Story 9A.3's terminal review, then Story T.1's measured Full run before and after.

**Provenance:** Original `a6c914e` is the sole legacy baseline. [History recovery](docs/history-recovery.md), recovery bundle and archives remain inside tim and are not uploaded. Preserve unrelated dirty historical review/question records. Unknown measurements remain unknown.
