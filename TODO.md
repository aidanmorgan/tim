# Current delivery

**Outcome:** stabilize the existing game, then implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Latest result:** `engine-hollow-shapes` terminal publication Pass at main `eacf561d6833438c9aeaf71c85e52c2626ea33e5`: true Tube/Frustum/Torus contacts, signed placement, physical controls/lifecycle and re-authored lessons. [CI 40239119353](https://github.com/aidanmorgan/tim/actions/runs/40239119353): 4,212 passed, 0 failed, 0 skipped; deployment 7320169548. The compiler-input drift was repaired with a verified workload pin. [Review and terminal receipt](./_bmad-output/implementation-artifacts/spec-engine-hollow-shapes.md#terminal-publication-receipt--26-october-2026).

**Blocker:** `engine-cap-block` HALTed as **blocked** after three bounded design attempts (25 Oct 2026). Spec: [`spec-engine-cap-block.md`](./_bmad-output/implementation-artifacts/spec-engine-cap-block.md). Attempt patch: [`engine-cap-block-attempt.patch`](./_bmad-output/implementation-artifacts/engine-cap-block-attempt.patch).
- **What worked:** the minimum-norm least-squares block solved by Jacobi eigen-decomposition is exact for full rank, and the rod keeps its energy within the inelastic model with no margin.
- **What blocks it:** resting box faces still drift (4 of 60 boxes over 1e-9 m/s), because load distribution on a face moves the per-point friction limits. Box-on-box contacts also gain energy, which inflates the energy-gate bound.
- **Retry condition:** after `engine-two-point-friction` lands a central friction anchor for multi-point manifolds.
- **Separate pre-existing defect:** the soft push-out of a deep single-point rim-on-edge penetration (7 mm) gains energy (Dead case 179, 2.447 mJ). See deferred-work.

**Next implementation:** `engine-two-point-friction`: restore the unchanged drop-ramp straightness bound and central face-friction anchoring. Start from published main with a fresh Auto implementer after a bounded ready-for-development spec. Read-only drafting identified exact-normal friction patches as a candidate single-law design; its geometry, cache and controls must be explicit before implementation. Basketball seam, Weight bands/eye and pressure-plate ring identity remain owned by `engine-multi-collider-dynamic`.

**Publication:** hollow-shapes is verified. The failed CI run and repair remain in its review record; no active hollow publication blocker remains.

**Next check:** Auto planning for the friction slice, including nonparallel/opposing-contact boundaries and explicit ownership of the separate deep-penetration prerequisite before cap-block retry. Browser qualification remains pending for post-catalogue E2E.

**Order** ([current execution plan](./_bmad-output/planning-artifacts/epics.md#current-execution-plan) governs, not sprint-file order):
1. `elements-visible-solid` (done) → `elements-visible-solid-sweep` (done) → `engine-hollow-shapes` (done) → `engine-two-point-friction` (the drop-ramp landing yaw kick, plus a central friction anchor for faces) → `engine-cap-block` (retry on top of the friction anchor) → `engine-orientation-levelling` (sub-f32-step levelling that keeps the energy gates) → `engine-multi-collider-dynamic` → `bumper-cap-solid` → `face-markings-flush` → [9A.44B](./_bmad-output/planning-artifacts/epics.md#story-9a-44b) (`double` game values to f32, integer-tick clock, `double` guard).
2. Shared native integration harness A then B → remaining TypeScript conversion → behavioral test hygiene → 9A.24 → 9A.45.
3. One element/variant per reviewed and published slice in epics order: Rope/cut (10.2b) → Pulley → supported-impact prerequisite → Lever → the remaining individually split CAT stories, then every remaining [named identity](./docs/planning/invest/named-elements.md#execution-contract--complete-named-catalogue-24-october-2026); then [Story 15.0 native assistance](./_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance) and catalogue-native-closure.
4. Only after complete native catalogue closure: campaign expansion, retained product features, browser E2E and named release/device/performance gates.

**Rules now:** [testing guidance](./docs/testing-guidance.md); real production C# unit/integration and shipped-output node harnesses with positive, control and boundary cases; zero skipped game tests; no output-hash assertions; no mutation testing until the post-catalogue E2E phase; the Forward-Only Auditor fails any fallback, backwards compatibility, deprecated slot or second rule. One matching build, then `--no-build --no-restore`. Browser suites stay disabled through [the single deferral list](./tools/E2eRunner/BrowserDeferral.cs).

**Follow-ups:** [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md). Blocked time and test duration remain unknown unless recorded.
