# Current delivery

**Goal:** 150 progressively taught challenge levels, unlimited Free Workshop and the complete catalogue through the named qualification gates.

**Verified outcome:** [Story 9.4b Cumulative Chrome Regression Repair](_bmad-output/planning-artifacts/epics.md#story-94b-cumulative-chrome-regression-repair) has Murdoch-2's round-5 terminal scoped Pass (19 Oct 2026). It passed on candidate 6: source `d7536a1c…`, bundle `102e9f12…`.
- **Tier 1:** build 0 warnings; 1523/1523 native tests plus 78/78 Coverage; node 76/76; Lifecycle 510/0; Wire 109/0.
- **Tier 2:** clock-master 5×8/8, cat-017, cat-023b and the smoke pair.
- **Full serial runs:** candidate 5's full cumulative runs passed 140/140 twice. Candidate 6 changed only the renewal-conclusion predicate.
- **Design:** the clock is now a JS clock master with typed probes; a lapse is renewed and checked, and is recalibrated only on a real move or a stall over about 1.2 s.
- [Review](_bmad-output/implementation-artifacts/review-9-4b-cumulative-regression-repair.md); [spec and evidence](_bmad-output/implementation-artifacts/spec-9-4b-cumulative-regression-repair.md).

**Current outcome:** Story 9R.0c (Undelivered Parts Leave the Palette and Bench Placement Never Throws) is first in Epic 9R. It fixes the owner-reported Balloon crash. Next come Stories 9R.0 (C# static gate), 9R.0a (tiered and parallel e2e runner) and 9R.0b, then 9R.1–9R.18, 9.4c and Epic 9A, all before any new element.

**Verification policy:** [verification tiers](_bmad-output/planning-artifacts/autonomous-development-guide.md#verification-tiers), owner decision 19 Oct 2026.
- A full Chrome run happens only for a material engine change, and as a checkpoint after every 5 element stories.
- Every other story runs impact-selected suites.

**Blocker / owner / retry:** None.

**Next acceptance check:** Story 9R.0c. Rebase Amelia-3's worktree onto 9.4b, then run Tier 1 and the Tier 2 Chrome check (`palette-placement` plus the smoke pair), then the independent review.

**Provenance:** Original `a6c914e` is the sole legacy baseline. [History recovery](docs/history-recovery.md), recovery bundle and archives remain inside tim and are not uploaded. Preserve unrelated dirty historical review/question records. Unknown measurements remain unknown.
