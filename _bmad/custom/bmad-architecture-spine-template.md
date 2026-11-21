---
name: '{name}'
type: architecture-spine
purpose: build-substrate    # build-substrate (default) · discussion · report · deck
altitude: feature           # initiative (keeps features) · feature (keeps epics) · epic (keeps stories)
paradigm: '{named design pattern, e.g. hexagonal, layered, pipes-and-filters, actor}'
scope: '{what this spine governs}'
status: draft               # draft · final
created: '{date}'
updated: '{date}'
binds: []                   # capability / unit IDs governed (from the driving spec; at epic altitude, also the inherited parent AD ids)
sources: []
companions: []
---

# Architecture Spine — {name}

<!-- TEMPLATE GUIDE — act on these comments, then delete them; never emit a comment in the finished spine. This is a shape, not a script: keep only the sections this spine needs and cut the rest (no empty headers). A small intent may be just paradigm + a few ADs + conventions; a platform earns more. An inherited epic spine is usually mostly Inherited Invariants + a thin Deferred. Decisions, not rationale (rationale lives in the memlog). Carry shape in diagrams; prose only where it must. -->

<!-- PROJECT CUSTOMIZATION: Adopt project-engineering-contract.md as a required companion using a path relative to the generated spine. Select applicable E1–E9 families in the native ADs and conventions, preserving source/requirement links and planned evidence; record scope reasons for exclusions in the memlog. Engine-affecting spines must adopt element-free-engine-design.md and element-free-engine-review.md as required companions, with paths relative to the generated spine. Preserve this skill's decision/memlog derivation and existing AD identities. Put each affected reusable contract in the normal AD blocks and map acceptance to capability IDs; no additional governance artifact is required. For non-engine work, omit the conditional fields below. -->

## Design Paradigm

<!-- Name the pattern (a known one loads a whole model for free) and map its layers to namespaces/directories. The smallest, most durable thing here. -->

## Inherited Invariants

<!-- Only when this spine inherits a higher-altitude parent. The parent's ADs/conventions/paradigm that bind here, by their ORIGINAL ids — read-only, never renumbered, not re-derived. A local decision that contradicts one is a conflict to surface, not an override. Cut this section otherwise. -->

| Inherited | From parent | Binds here |
| --- | --- | --- |
| {AD-id / convention} | {parent spine} | {what it constrains in this scope} |

## Invariants & Rules

<!-- The durable heart: calls a future builder can't read off compliant code. One block per decision: stable ascending id (never reused/renumbered), Binds, Prevents (the divergence), Rule (enforceable). Tag [ADOPTED] when the user or existing reality settled it. Include a dependency-direction diagram (who may depend on whom) — it IS a rule; author it as valid mermaid, never an empty graph. -->

### AD-1 — {decision}

- **Binds:** {capability / unit ids / fr/nfr's, areas, or `all`}
- **Prevents:** {the divergence this stops}
- **Rule:** {the constraint downstream must follow; include applicable E1–E9 rule IDs, exact authoritative source sections and concrete design obligations. Bind planned acceptance to the capability IDs; distinguish existing proof from planned checks.}

<!-- ENGINE-AFFECTING AD ONLY: Fold these answers into this Rule or reference exact sections of required companions. Do not treat an element name as a reusable contract. -->
  - **Behavior/reuse:** {Affected outcome and retained requirements; existing or bounded new primitive with kind, inputs/state/outputs, units and validity domain.}
  - **Declaration contract:** {Package data/composition; explicit typed capability/port/body/coordinate identities, their resolution and admission constraints.}
  - **Execution/ownership:** {Declaration → compiled rows → execution → observations/events → animation/binding; sole state/property writers, lifecycle and atomic refusal.}
  - **Acceptance:** {Unfamiliar declaration/composition, variation/reordering, positive/control/boundary/refusal and applicable Reset/save/load cases with independent expected behavior; identify existing evidence separately from planned tests.}
  - **Deletion/prerequisites:** {Superseded paths removed or none; resolved required-now prerequisites/owners, including any code available only in an uncommitted candidate. Unresolved required-now decisions block finalization.}

## Consistency Conventions

<!-- Defaults that bind where independent builders would drift. Cut rows that don't apply; add rows the project needs. -->

| Concern | Convention |
| --- | --- |
| Naming (entities, files, interfaces, events) | |
| Data & formats (ids, dates, error shapes, envelopes) | |
| State & cross-cutting (mutation, errors, logging, config, auth) | |
| Project planning gate | {Applicable E1–E9 rules and source links mapped to ADs/capabilities; Scoped Pass / Fail / Incomplete and required-now evidence. Record verdict in the native validation/memlog; unresolved required-now gaps block finalization.} |
| Engine planning gate (only when affected) | {Scoped Pass / Fail / Incomplete; scope, inspected decisions/contracts and evidence limits. Record the native validation verdict/memlog event before status final. Required-now gaps block finalization; a planning Pass is not runtime proof.} |

## Stack

<!-- SEED — verified current at authoring; the code owns this once it exists. Name + version only; the why lives in the memlog. One row per language, framework, key dependency, platform, or chain that's pinned. -->

| Name | Version |
| --- | --- |
| {language / framework / key dep / platform / chain} | {pinned version} |

## Structural Seed

<!-- The shapes worth fixing at cold-start — not a fixed list. Include only what's non-obvious at this altitude, and use as many diagrams as convey it, each as VALID mermaid (never a placeholder or empty graph). Candidates: system/container/context view; DEPLOYMENT & ENVIRONMENTS and external provider/infra topology (cover the operational envelope here when this altitude owns it — don't let it fall through); core-entity ERD (names + relationships only; an attribute that's itself an invariant is an AD, not a diagram); a minimal source tree. The code owns the detail — this is scaffold, not a mirror to maintain. -->

```text
{root}/
  {dir}/   # {what lives here}
```

## Capability → Architecture Map

<!-- Present when a spec drove this run. Bridges the spec's capabilities to where they live + what governs them; the consistency auditor's checklist. Cut otherwise. -->

| Capability / Area | Lives in | Governed by |
| --- | --- | --- |
| {CAP-id / area} | {component / module} | {AD-id, convention, paradigm} |

## Deferred

<!-- Name the applicable later gate and owner, evidence still pending and why no required-now obligation is deferred. Do not claim browser/release qualification from native proof. Decisions intentionally pushed down, each with the reason it can wait — including whole dimensions this altitude doesn't own yet. The half of the contract that keeps the spine lean. -->
