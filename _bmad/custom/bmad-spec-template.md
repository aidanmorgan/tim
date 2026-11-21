---
id: SPEC-{slug}
companions: []     # files downstream MUST read alongside SPEC.md. Paths may point inside the spec folder (spec-authored) or outside it (adopted from an upstream skill).
sources: []        # files fully absorbed into the SPEC (audit only; downstream does NOT read these). Never the memlog.
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and validate. Source documents listed in frontmatter are for traceability — consult them only if you need narrative rationale or prose color this contract intentionally omits.

<!-- PROJECT CUSTOMIZATION: Keep the native five-field kernel and memlog workflow. Adopt project-engineering-contract.md as a required companion using a path relative to the generated spec. Select its applicable rule families in the native constraints/memlog and CAP acceptance cases; record a short scope reason for exclusions, without copying the whole policy. For engine-affecting work, log the decisions/constraints below in .memlog.md before deriving SPEC.md; never hand-patch a derived spec to satisfy this template. Adopt the project's element-free-engine-design.md and element-free-engine-review.md as required companions using paths relative to the generated spec. If detailed design exceeds the kernel, derive a named companion and link its exact sections below. For work that does not affect engine behavior, omit the engine-specific placeholders rather than fabricate design or runtime evidence. Delete template comments after acting on them. -->

# {Spec Title}

## Why

{One paragraph naming the force behind this work. A spec can exist for any of:
  - **a pain to solve** — a user or operator is stuck on a specific gap;
  - **an opportunity to capture** — something newly possible we want to claim;
  - **a vision to realize** — a thing we want to make exist because we want it to exist;
  - **a mandate to meet** — a regulation, deprecation, deadline, or contractual obligation.

Name which (or which combination) applies, who is affected, and the backdrop that makes it matter now. This is the anchor every downstream trade-off resolves against.}

## Capabilities

- **CAP-1**
  - **intent:** {One sentence. "User or system can do X to achieve Y." WHAT, not HOW.}
  - **success:** {Testable or demonstrable criterion. For engine-affecting work: name the unfamiliar declaration/composition and independently expected observable; include relevant variation/reordering, negative/control, boundary/refusal and lifecycle cases. These are planned acceptance cases, not claimed test results.}

## Constraints

- {A non-negotiable that bends design. If it doesn't rule anything out, it doesn't belong.}
- **Applicable project rules:** {Selected E1–E9 IDs and exact source sections; affected requirement/CAP IDs; concrete design obligations and planned positive/control/boundary evidence, or scoped documentation checks. Record scope reasons for excluded families in the memlog.}
- **Required-now and deferred proof:** {Native planning verdict/evidence limits; unresolved required-now gaps block readiness. Name pending later gates/owners, including browser qualification where applicable. A planned test or planning Pass is not implementation proof.}

<!-- ENGINE-AFFECTING WORK ONLY: Fill these compact contracts or reference their exact locations in required companions. Together with Why/Capabilities they answer all eight design questions. Preserve requirement IDs and retained behavior; do not copy the policy prose. -->
- **Behavior and scope:** {Affected requirement IDs, observable behavior, retained behavior and actual dependent paths; link the relevant CAP success cases.}
- **Reusable primitive:** {Existing or bounded new law/operation; typed kind, inputs/state/outputs, units and validity domain. No element-selected execution.}
- **Declared composition:** {Package-owned values and explicit typed body/collider/coordinate/node/port/store/observation references; roles are not identities.}
- **Execution and ownership:** {Declaration → admission → compiled rows → execution → committed state/events → animation/property binding; owners/writers, Run/Reset/save/load and atomic refusal boundaries.}
- **Generality:** {Reference CAP acceptance cases and any applicable existing proof; distinguish planned tests from executed evidence. Another supported-process element needs declarations/assets/bindings/tests only.}
- **Forward-only change:** {Old symbols, schema paths, callers/content to delete, or explicitly none; current replacements without aliases/fallback/migration.}
- **Prerequisites and uncertainty:** {Required-now decisions/dependencies and owners; explicitly identify candidate-only prerequisite incorporation. Open required-now decisions prevent readiness.}
- **Capability probe:** {Each engine/abstraction/bundle/wire/presentation capability the behaviour needs: present (cited symbol and exercising test in this checkout), narrower (how), or missing (the engine slice that delivers it first). Direct consumers that must change: tables, guards, schema, generated files, TypeScript callers, fixtures. See bmad-loop-protocol.md#capability-probe.}
- **Delivery shape and review tier:** {Single element, Family slice (list the identities and what differs) or closure review of delivered duplicates; any duplicate of this part closed inside this work (the satisfying test); data-only or engine review tier with the reason. Keep each answer in this engine-affecting block to three lines and put derivations in the run log. See bmad-loop-protocol.md#slice-shapes and #review-tiers.}
- **Planning gate:** {Scoped Pass / Fail / Incomplete, inspected scope and design/contract evidence. Record the verdict as a memlog event; map it to native workflow outcomes under the review policy. A planning Pass is not implementation proof.}

## Non-goals

- {Explicit out-of-scope item. At least one. Stops downstream from filling the vacuum.}

## Success signal

- {One or two sentences. World-change moment, not dashboard. Concrete enough to write a test or run a demonstration against.}

## Assumptions

<!-- Optional. Omit this section entirely if empty. Inferred calls made without direct confirmation from the input. -->

- {Statement of fact the Spec proceeded under, e.g. "Assumed mobile-first since input mentioned GPS but no platform."}

## Open Questions

<!-- Optional. Omit this section entirely if empty. Gaps the input did not resolve that need a human decision before downstream skills consume the Spec. -->

- {Question phrased so a human can answer it, e.g. "Is offline playback in scope for CAP-2?"}
