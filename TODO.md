# Current delivery

**Outcome:** stabilize the existing game, then implement every required named puzzle element and variant, then browser qualification; preserve unlimited Workshop and 150 progressively taught levels.

**Current check:** 9A.23+9A.25b and 10.2a are merged with green main CI; reconcile their remaining terminal publication criteria against the exact merged candidates before closing review status. Do not republish identical work.
- Ring: [review](./_bmad-output/implementation-artifacts/review-9a-23-25b-ring-consumers.md), merge `f1ad3bd19e27c32cb05ba34f83ccc1d6cf59fc76`, CI 40055713865.
- Weight: [review](./_bmad-output/implementation-artifacts/review-10-2a-weight.md), merge `b88ca4251d8ff37272981bdd4654bd950987d04d`, CI 40057791030.
- The prior red-CI blocker is resolved by [terminal harness review](./_bmad-output/implementation-artifacts/review-ci-harness-determinism.md#terminal-verdict), merge `9536e2ff70bfc0fe157a27bf1f7436727ea05dd0`, CI 40052056113. Recorded fresh coordinator query, 24 October 2026; future publication still checks its exact SHA.

**Next implementation:** [9A.44 f32 everywhere](./_bmad-output/planning-artifacts/epics.md#story-9a44-f32-lane-completion--remove-every-binary16-lane), including double game values, integer ticks/f32 phase, named external adapters and the bounded strict-TypeScript prerequisite for affected modules.

**Programme:** [current execution order and stabilization contracts](./_bmad-output/planning-artifacts/epics.md#current-execution-plan) govern selection, not raw sprint-file order.
1. f32 → cylinder axial creep → shared native integration harness A/B → remaining TypeScript conversion → behavioral test hygiene → 9A.24 → 9A.45.
2. One element/variant per reviewed and published slice: Rope/cut → Pulley → supported-impact prerequisite → Lever → remaining individually split CAT stories, then every remaining [named identity](./docs/planning/invest/named-elements.md#execution-contract--complete-named-catalogue-24-october-2026).
3. After all named intrinsic implementations, complete [Story 15.0 native assistance](./_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance) and remaining required native mode/collaborator integrations, then verify catalogue-native-closure.
4. Only after complete native catalogue closure: campaign expansion, retained product features, browser E2E and named release/device/performance gates.

**Coverage:** 72 CAT specs overlap 276 actual named-element specs. All 293 source identities remain accounted for: 276 elements, 10 child-forwarding umbrellas, 7 product features. Specifications are not runtime completion; every required mode, variant and later cross-element criterion needs passing unit/integration proof. No representative-family shortcut or potential-element exclusion.

**Verification now:** [testing guidance](./docs/testing-guidance.md), real production C# unit/integration and shipped-output node harnesses, controls/boundaries/mutations, Reset/save/load, zero skipped game tests and no output-hash assertions. One matching build then --no-build --no-restore. Browser suites/toolchain remain disabled through [the single deferral list](./tools/E2eRunner/BrowserDeferral.cs); no browser gate before full catalogue completion.

**Follow-ups:** [deferred work](./_bmad-output/implementation-artifacts/deferred-work.md) retains engine evidence. Ordinary correctness follows its first affected consumer; detailed performance stays at its named release gate. Blocked time/test duration remain unknown unless recorded.
