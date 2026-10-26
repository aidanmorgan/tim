# Current delivery

**Outcome:** stabilize the existing game, then implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest result:** local-test-authority and unchanged float policy published at `b39b893d827541ce6421b744078dd76460b73214`: [build 40318527487](https://github.com/aidanmorgan/tim/actions/runs/40318527487), Pages artifact `12647774040` and deployment `7331335202` verified. Full local **4,411/0/0**, hooks **126/0/0**. [Terminal receipt](./_bmad-output/implementation-artifacts/spec-local-test-authority.md#terminal-publication-receipt--26-october-2026).

**Current implementation:** [remove synthetic startup warmup](./_bmad-output/implementation-artifacts/spec-engine-remove-core-warmup.md). Core dummy work, synthetic scene and command lifecycle are deleted; real preparation, clocks and solver warm-starting remain. Cold-start, Reset/replay, empty-bench and ordinary engine reuse tests replace the obsolete preservation tests.

**Blocker / owner / retry:** no known blocker. Local implementation and independent Auto review are done; coordinator owns normal hooks, committed-SHA approval and publication. `engine-cap-block` remains queued for a fresh retry after its historical HALT; its [original observations](./_bmad-output/implementation-artifacts/spec-engine-cap-block.md) remain evidence, not current float-policy acceptance.

**Next acceptance check:** full local session39307 passed **4,411/0/0**, after five-layer review and independently confirmed B3/B7/B10 repairs (affected16/0/0). Matching compiler captures passed with zero warnings/errors. Capture the frozen candidate, run normal hooks, commit, obtain exact-SHA approval and publish; GitHub builds/deploys without repeating tests. Browser and first-use/performance qualification remain pending at their named later gates.

**Order** ([current execution plan](./_bmad-output/planning-artifacts/epics.md#current-execution-plan) governs, not sprint-file order):
1. `elements-visible-solid` (done) → `elements-visible-solid-sweep` (done) → `engine-hollow-shapes` (done) → `engine-single-point-penetration` (generic recovery prerequisite) → `engine-two-point-friction` (the drop-ramp landing yaw kick, plus a central friction anchor for faces) → `engine-cap-block` (retry on top of the friction anchor) → `engine-orientation-levelling` (sub-f32-step levelling that keeps the energy gates) → `engine-multi-collider-dynamic` → `bumper-cap-solid` → `face-markings-flush` → [9A.44B](./_bmad-output/planning-artifacts/epics.md#story-9a-44b) (`double` game values to f32, integer-tick clock, `double` guard).

2. Shared native integration harness A then B → remaining TypeScript conversion → behavioral test hygiene → 9A.24 → 9A.45.
3. One element/variant per reviewed and published slice in epics order: Rope/cut (10.2b) → Pulley → supported-impact prerequisite → Lever → the remaining individually split CAT stories, then every remaining [named identity](./docs/planning/invest/named-elements.md#execution-contract--complete-named-catalogue-24-october-2026); then [Story 15.0 native assistance](./_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance) and catalogue-native-closure.
4. Only after complete native catalogue closure: campaign expansion, retained product features, browser E2E and named release/device/performance gates.

**Rules now:** [local tests are authoritative; GitHub builds and deploys](./docs/delivery-workflow.md#local-test-authority); [testing guidance](./docs/testing-guidance.md); real production C# unit/integration and shipped-output node harnesses with positive, control and boundary cases; zero skipped game tests; no output-hash assertions; no mutation testing until the post-catalogue E2E phase; the Forward-Only Auditor fails any fallback, backwards compatibility, deprecated slot or second rule. One matching build, then `--no-build --no-restore`. Browser suites stay disabled through [the single deferral list](./tools/E2eRunner/BrowserDeferral.cs).

**Owner-requested next work (26 October):** remove synthetic warm-up after float-policy publication, preserving solver warm-starting. Remaining comparison adoption follows the [testing guidance inventory](./docs/testing-guidance.md#physical-comparisons-owner-26-october-2026); raw game-output comparisons and source guards remain bounded hygiene work under the [behavioural inventory](./_bmad-output/implementation-artifacts/test-behavioral-hygiene-inventory.md).

**Follow-ups:** [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md). Blocked time and test duration remain unknown unless recorded.
