---
title: 'Complete named-element delivery plan and native-first gates'
type: 'chore'
created: '2026-10-24'
status: 'done'
baseline_commit: 'b88ca4251d8ff37272981bdd4654bd950987d04d'
route: 'dispatch'
review_loop_iteration: 1
context:
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/docs/testing-guidance.md'
---

<frozen-after-approval reason="owner requested all review corrections on 24 October 2026">

## Intent

**Problem:** The current queue does not schedule the complete named-element inventory, and several stabilization and future-element stories lack current, independently executable acceptance. Stale status and architecture instructions obscure the next action.

**Approach:** Repair the existing BMAD planning documents so stabilization has a finite exit, every actual named element/variant has an individual delivery owner, and browser qualification starts only after the complete inventory passes native unit and integration proof.

## Boundaries & Constraints

**Always:** Preserve requirement IDs, source acceptance, element identities, variant obligations and historical evidence. Reuse existing epics, sprint status, element specifications and identity ledger. Treat the 72 CAT entries and 276 named-element specs as overlapping inventories, not additive delivered parts; retain all 293 source identities including 17 index-only entries. The owner's instruction requires every named element: potential/adoption labels cannot silently exclude one. Separate unresolved design decisions from runtime implementation and name their next owner. Preserve individual review/publication and explicit browser-pending status. Current architecture is package-owned .tscn content, bundle discovery, typed command/query and event rings, generic C# WASM SIMD f32 engines, strict TypeScript boundaries, no legacy routes. Require deterministic behavioral unit/integration tests with controls, boundaries, mutation evidence and zero skips.

**Never:** Implement runtime features in this documentation slice, claim catalogue implementation from specification coverage, run/write browser tests as a gate, invent settled physics decisions, erase historical evidence, add a new generator/tracking platform, or touch unrelated dirty work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Full identity coverage | CAT specs, named specs and 293 ledger identities | Every actual identity has one implementation owner or precise existing-owner correspondence; index-only entries retain named child/product owners | Missing, duplicate or excluded identity fails planning review |
| Missing design choice | A declaration has unresolved model parameters | Explicit bounded decision before dependent implementation; source behavior preserved | Story stays backlog, never falsely ready |
| Scope overlap | Named identity refines an existing CAT | Shared implementation allowed only with explicit criterion ownership; distinct variants remain separate | Similar names never prove equivalence |
| Browser gate | Some required element lacks native proof | Browser remains disabled; missing element is selectable backlog | No representative-family or catalogue-count shortcut |
| Stale status | TODO contradicts terminal publication receipt | Link recorded evidence and identify actual next check | No unsupported claim about fresh remote CI |

</frozen-after-approval>

## Code Map

- `TODO.md`: current outcome, blocker and next acceptance check.
- `_bmad-output/planning-artifacts/epics.md`: detailed story acceptance and delivery order; amend current contracts rather than historical reviews.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: existing dispatch/status keys.
- `docs/planning/invest/vertical-delivery.md`, `invest-index.md`: programme order and slice selection.
- `docs/planning/invest/named-elements.md`: 293 identity/requirement links.
- `docs/planning/elements/README.md`, `CAT-*.md`, `EL-*.md`, `TH-*.md`, `RAD-*.md`, `GAP-*.md`: readiness contracts and source mapping.
- `docs/planning/requirements.md`: read affected bodies by anchor; preserve IDs and acceptance.
- `review-work-plan-2026-10-24.json`: findings to close; overlaps between lenses are retained.
- `review-ci-harness-determinism.md`, `deferred-work.md`: terminal status and prerequisites, not instructions to rerun history.

## Tasks & Acceptance

**Execution:**
- [x] `epics.md`, `sprint-status.yaml`: specify finite stabilization slices, f32 double/tick/phase scope and bounded TypeScript prerequisite; define their native exit.
- [x] `epics.md`, element specs and identity ledger: map every identity, split grouped future stories, preserve variants and integration ownership, schedule missing families and bounded design decisions.
- [x] `CAT-058-rope_anchor.md`, Epic 10: separate Rope/cut and Pulley acceptance; link supported-impact restitution before the Lever consumer.
- [x] Roadmap, index and active declaration instructions: replace retired catalogue/asset instructions; place current phase/order ahead of historical reference material.
- [x] `TODO.md`: reconcile recorded CI resolution, link concrete current contracts/evidence and state the next acceptance check.
- [x] Scoped evidence: retain raw membership, links/anchors, preservation and dependency checks plus an independent review record.

**Acceptance Criteria:**
- Given all source identity/spec sets, when compared with implementation owners and sprint entries, then none is missing, duplicated as a second implementation, or silently waived.
- Given any outstanding slice, when selected, then its contract identifies source behavior, dependencies/decisions, changed/deleted paths, native positive/control/boundary/lifecycle proof and individual review/publication.
- Given all stabilization checks pass, when selecting work, then the next ready element is selected without unrelated audits; genuine engine prerequisites precede their first consumer.
- Given any required element lacks implementation and passing unit/integration proof, when evaluating browser eligibility, then eligibility is false; after all pass, the explicit post-catalogue browser/release phase owns retained UI acceptance.
- Given the frozen documentation candidate, when independently reviewed, then every initial finding has a verified disposition and no unresolved preservation, dependency or current-authority contradiction remains.

## Implementation Notes

2026-10-24: Implemented the complete existing-ledger/sprint mapping, individual CAT children, finite native stabilization, direct source adoption and active instruction repair. Retained 276 named identities, 72 overlapping CAT contracts, 10 umbrellas and 7 product features. All requirements/source acceptance and historical harvest remain; source model questions remain bounded entry decisions, not false readiness. Fresh coordinator CI query resolves the red-main blocker; merged Ring/Weight still require terminal publication reconciliation. Documentation-only: no runtime build or browser run.

Owner authorization covers this complete planning repair. Remaining product-model choices are represented as explicit future decision work; this slice does not claim their implementation readiness.

## Spec Change Log

2026-10-24, round 1 V1: removed the catalogue-closure/15.0 dependency cycle. All named intrinsic implementations precede bounded native assistance/mode integration; native 15.0 closes CAT-049/CAT-050 before catalogue-native-closure, while exhaustive difficulty/campaign/browser qualification remains afterward. KEEP all numerical/source acceptance and historical evidence.

## Review Triage Log

| Round / lens | Finding / verdict | Disposition and evidence |
|---|---|---|
|1 Blind Hunter|No findings|Independent lens reported clean; retained first-review evidence is unchanged.|
|1 Edge Case Hunter|[]|Independent lens reported no edge findings; retained first-review evidence is unchanged.|
|1 Verification Gap / retained reviewer|No test gap; V1 high, patch|Confirmed closure → native 15.0 → closure dependency cycle. Phase order, Story 15.0 stage ownership, selectors and CAT-049/CAT-050 scheduling now fixed; independent affected re-review still required. [Round 1 raw evidence](evidence-work-plan-coverage/reviewer-round-1.json).|
|Early verification|Multi-CAT mapping, patch|All eight multi-CAT correspondence rows retain every CAT reference; exact 276 named mapping and 293 source outcomes preserved. [Frozen first-candidate evidence](evidence-work-plan-coverage/candidate.json).|
|Early verification|Collaborator cycle staging, patch|Completed generic qualification fixtures provide intrinsic proof; later consuming owners retain explicitly assigned integration criteria. Each named required-now slice still completes review/publication before the next.|
|Early verification|YAML status placement, patch|New keys moved inside development_status; unique-key YAML parsing and exact named membership passed in the frozen evidence.|

Independent affected re-review returned [terminal planning Pass](review-work-plan-coverage.md) on 24 October 2026. The spec is done; this completion-status update does not change the approved planning contract. Original candidate and first-review evidence remain unchanged.

## Verification

Compare exact CAT/named/index-only identity sets, unique owner keys, sprint membership and all changed local links/anchors. Inspect dependency order/cycles and acceptance preservation against source sections. Run affected existing documentation/coverage checks where applicable. Save raw outputs and use a distinct reviewer for terminal scoped planning Pass. No runtime build or browser run is justified by this documentation-only change.
