# Project engineering contract — BMAD customization

This extension turns the project's existing architectural, design and delivery authorities into decisions and evidence inside BMAD. It does not replace those authorities, certify the current implementation, or create a separate checklist or approval system.

## Apply within the invoked workflow

1. Resolve the invoked skill's project customization before using its planning or acceptance gate. Read this extension, then select applicable rule families below from the actual affected behavior and dependency closure. Read only their linked source sections and affected requirement/element rows. A non-applicable family needs a short scope reason, not invented design work.
2. In the existing spec constraints/memlog, architecture ADs, story ACs/readiness record, or build Code Map/verification plan, record each selected rule ID, exact source, affected requirement/capability, concrete design obligation and planned proof. Carry these references into the native review findings/triage and acceptance evidence. A compact applicability table in an existing artifact is sufficient; do not create another register.
3. Planning must settle the applicable contracts and name independently expected acceptance cases. Acceptance checks the actual changes and required-now evidence. Planned tests are not executed proof; source targets are not statements of implemented status. Reviewer inputs follow the loop protocol: specialists receive role-appropriate source sections without implementation claims; the informed engineering auditor receives the applicable contract, candidate scope and matching evidence.
4. A confirmed review finding remains blocking until fixed and independently re-reviewed, even when pre-existing or outside the initial diff; severity does not waive repair. The one exception is the loop protocol's Re-review scope (owner decision, 21 November 2026). A confirmed applicable hard-rule violation is **Fail**; missing required-now decisions or evidence is **Incomplete**. Only a justified affected scope with no unresolved violations or gaps can receive **Scoped Pass**. Preserve the invoked skill's native result schema: use its failure/blocking outcome with an explicit Incomplete reason when no Incomplete value exists. Severity follows the source policy; element-specific and forward-only violations are high, but do not relabel every other finding high automatically.
5. Apply current explicit owner instructions before conflicting older prose, then the sole design authority for architectural conflicts. Preserve requirement identities and intended behavior while honoring the current enforcing stage. State material conflicts and their controlling source in the native decision record. An unresolved required-now conflict blocks readiness; an obsolete sentence does not authorize a waiver.

Engine-affecting work additionally applies [the element-free design contracts D1–D7](element-free-engine-design.md) and [the planning/acceptance review policy](element-free-engine-review.md), including their scope and handoff duties. These remain mandatory, not a substitute for the other applicable families below.

## Native workflow bindings

Use [the loop protocol](bmad-loop-protocol.md) for entry, candidate identity, role-specific reviewer inputs, bounded dispatch, repair and acceptance. It overrides named native branches through supported hooks; installed step files remain unchanged.

| Project override | Native checkpoint and evidence |
| --- | --- |
| [PRD](bmad-prd.toml), [UX](bmad-ux.toml) | Capture constraints in canonical memlogs; derive artifacts and run applicable engineering review before Finalize/Validate, including update/headless routes. |
| [Spec](bmad-spec.toml) | Use the [project template](bmad-spec-template.md); log decisions, derive the five-field kernel/companions, record validation verdict. Story Breakdown preserves affected constraints and ACs in stories.yaml. |
| [Architecture](bmad-architecture.toml) | Use the [spine template](bmad-architecture-spine-template.md); record stable ADs/memlog decisions. Run the auditor before Finalize/Validate/final status, including updates and resumed work. |
| [Epics/stories](bmad-create-epics-and-stories.toml) | Extract D1–D7 and applicable E rules in step 1, assign bounded prerequisite owners in step 2, write design-linked ACs in step 3, validate coverage/dependencies in step 4. Backlog completion does not make unresolved stories ready. |
| [Sprint planning](bmad-sprint-planning.toml) | Gate readiness before tracking. Missing required evidence maps to native FAIL with reason Incomplete. Status/repair cannot manufacture approval. |
| [Auto](bmad-build-auto.toml), [Build](bmad-build.toml) | Guard entry before early exits; apply design gate in native plan/Code Map; hand off directly to owner; perform independent review and affected repair review before done/commit. Oneshot retains design in Implementation Notes. Evidence stays in Review Triage Log/Auto Run Result. |
| [Code review](bmad-code-review.toml), [Review](bmad-review.toml) | Use distinct reviewer inputs and bounded capacity. Engineering audit evaluates applicable design/evidence; coordinator records verdict. Exact partial lens requests remain partial, never acceptance approval. |
| [Retrospective](bmad-retrospective.toml) | Review cross-story seams and carry sourced lessons into existing owned actions. Apply current acceptance and qualification rules; confirmed open findings cannot become accepted-with-open-items. |
| [QA](bmad-qa-generate-e2e-tests.toml) | Check browser deferral before generation, local test authority during execution and proof limits in summary. QA cannot grant story acceptance. |

Build/Auto expose no spec-template or individual-step override: activation hooks carry obligations into native fields and explicitly replace conflicting branch instructions. Confirm the current render contains hooks/handoffs and resolve remaining paths before dispatch. Completion hooks run too late to protect approval. Remediation plans may describe violations they remove; proposed solutions must comply, and planning approval never certifies unimplemented behavior.

## E1 — Package isolation, discovery and element independence

**Trigger:** package declarations, dependencies, discovery, authoring, compiler, shared runtime or presentation support changes.

**Plan:** identify package-owned declarations/assets/bindings, typed abstraction contracts and dependency direction. Each element project depends only on Abstractions and needed domain Abstractions, never an engine or sibling element. Build discovery produces the sole bundle; runtime and palette consume it without another catalogue or identity-selected support. New supported compositions need no custom engine code. Apply D1–D7 to any changed generic capability.

**Accept:** inspect affected project references and discovery/consumer paths, prove admitted declaration behavior and relevant unknown/invalid-input refusals, and verify superseded tables/branches are removed. A structural guard alone cannot prove semantic generality.

**Sources:** [target invariants and dependency graph](../../docs/architecture/element-package-isolation.md#target-invariants), [bundle loading](../../docs/engine-contracts.md#element-bundle-loading-contract).

## E2 — Execution, numbers and typed boundaries

**Trigger:** simulation, animation, rendering, numeric values, wire/save representations, browser/worker glue or tool code changes.

**Plan:** preserve the Tri-Graph split: main-thread logical authoring compiles flat SIMD-aligned SoA physics tables; dedicated C# WASM SIMD128 simulation runs 120 Hz/480 Hz substeps using the shared BVH/DSU/TGS Soft laws; independent animation runs 60 Hz; display-paced rendering runs 30–60 FPS with WebGL 2/WebGPU instanced batches and the zero-copy pose ring. Data flows physics → animation → renderer and physics → renderer, never back. Physics has no external physics-package dependency or alternate CPU fallback. Keep game calculations/authoring/presentation f32, exact integer identities/time where declared, and named validated external adapters. Prefer C#; other code is strict TypeScript without any escapes, with shipped JS generated by pinned tsc in normal build/publish/CI. Closed sets remain enums end-to-end; extensible identities are typed IDs. Strings are converted only at genuine UI/serialization/external boundaries.

**Clock and property contract:** when clock/scheduling/presentation is affected, preserve the Simulation-owned session master, independently scheduled consumers without a main-thread pulse pump, typed integer-origin PhysicalTime with bounded local f32 evaluation, and coherent display at committed world time. World cosmetics follow world Pause/Step/Resume; autonomous UI follows continuous master time. One owner writes each presentation property; cosmetics cannot mutate physics, energy, objectives or authoritative save state. Read only the relevant [shared-clock sections](../../docs/shared-clock-cadence.md) and affected REQ-08 criteria.

**Accept:** compile the affected callers; check changed canonical mappings, finite/range/unknown-value refusals and production paths. Test compiled TypeScript output that ships. Identify the writer and clock for changed state/properties; show cosmetic output cannot alter physical truth. Scoped native evidence does not qualify device performance or browser rendering.

**Sources:** [sole design authority](../../docs/architecture/element-package-isolation.md#target-invariants), [Tri-Graph and numeric authority](../../docs/gpu-f32-physics.md#compilation-model), [AGENTS engineering requirements](../../AGENTS.md#engineering-and-gameplay-requirements). Detailed device/workload performance stays at E8's named gates.

## E3 — Commands, queries, events and shared-memory ownership

**Trigger:** communication, worker scheduling, commands, read models, event consumers, shared rings or their glue changes.

**Plan:** each mutation is a typed sequenced command with a receipt; side-effect-free reads use committed state, the pose ring or typed queries. One-shot occurrences use the single-producer/multiple-consumer event ring with independent cursors; level states remain queries. Name the sole owner/writer, atomic shared-view API, sequence/epoch barriers and retention/lapping behavior. No ad-hoc message flags, global mutable state, main-thread occurrence inference, event pumps or presentation feedback.

**Accept:** production positive/control/boundary checks cover relevant receipt/commit distinction, stale generations, malformed input, causal occurrences, consumer cursor preservation and atomic reads. Inspect the encapsulated atomic API and its bypass guard; stress cannot prove absence of races. Preserve joint world/event commit, same-epoch growth cursors and explicit lapping outside retained history.

**Sources:** [communication contracts](../../docs/architecture/element-package-isolation.md#2-communication-rings-commands-and-queries), [wire/admission](../../docs/engine-contracts.md#wire-and-admission), [concurrency guidance](../../docs/testing-guidance.md#stress-and-randomised-tests-coordinator-24-october-2026).

## E4 — Current schema, lifecycle and atomic installation

**Trigger:** admission, schema, construction/save/load, Run/Reset, animation lifecycle, resource ownership or disposal changes.

**Plan:** one current structurally validated schema per boundary; no version fields/checks, defaults for malformed input or migration readers. Validate widths, lengths, enum values, finite ranges, unique identities and resolving references before atomic installation. Name candidate versus committed state, session/generation/revision, ownership transfer and disposal; preserve authored Reset state and supported save/load, invalidate stale references/effects and retain truthful pending/refusal outcomes. Geometry, query metadata and presentation must share the admitted identity/revision.

**Accept:** execute applicable ordinary Run/Pause/Resume/Reset, save/load and disposal paths through defined production interfaces, including rejected candidates preserving the installed world and stale work unable to mutate a new generation. Compare decoded canonical fields and semantic outcomes, never whole-output bytes. Do not replace ordinary lifecycle evidence with declarations or mocks.

**Sources:** [current schema invariants](../../docs/architecture/element-package-isolation.md#target-invariants), [lifecycle and atomicity](../../docs/engine-contracts.md#lifecycle-and-atomicity), [ownership](../../docs/engine-contracts.md#ownership-and-assemblies), [boundary tests](../../docs/testing-guidance.md#contract-tests-at-boundaries).

## E5 — Actual-demand storage and forward-only replacement

**Trigger:** tables, allocations, interaction state, transport capacity, replacement APIs or refactoring changes.

**Plan:** keep logical counts equal to actual scene/workload demand, sparse interaction state and reusable backing storage with common initial reserves and geometric watermark growth. Prepare complete replacements before atomic publication; no arbitrary ceilings, owner×target reservations or exact-plus-one churn. Memory and checked representation are the bounds unless a current written requirement states a fixed count. Name every superseded symbol/path and its current callers/content/tools/tests to update and delete in the same slice.

**Accept:** relevant empty/normal/growth/refusal controls demonstrate no dropped content, state or events. Inspect ownership and logical/backing separation. Deleted symbols have zero production hits. Fail high for fallback, shim, old alias, migration/version branch, retained deprecated API or conditional duplicate behavior; typed unsupported-input refusal and numerical recovery within the same law retain only the exceptions explicitly allowed by the owner.

**Sources:** [invariants 7–9](../../docs/architecture/element-package-isolation.md#target-invariants), [demand-grown storage](../../docs/gpu-f32-physics.md#8-demand-grown-reusable-scratch-and-scene-buffers), [forward-only owner gate](../../AGENTS.md#engineering-and-gameplay-requirements). Old transport fixed quotas are explicitly void in [reliable storage](../../docs/engine-contracts.md#reliable-storage-and-leases); do not reinstate them. Detailed allocation stress belongs at E8's later gates.

## E6 — Scene assets and visual/physical truth

**Trigger:** scene assets, geometry, sockets, collision extraction, visual design, interaction or animation bindings change.

**Plan:** preserve the approved palette, sculptural form, readable silhouettes, lighting and purposeful motion. The current element asset is its package-owned .tscn scene; dedicated hidden collision proxies and owning-body socket locators are extracted into the bundle without element branches. Render and contact geometry remain separate but share dimensions/frames: visible functional solids, passages and moving contacts must agree with physical behavior. Visibility/LOD/cosmetics cannot change simulation.

**Accept:** check affected declared geometry, proxy/locator admission and committed physical-to-visual bindings, including moving bodies. Native geometry/behavior checks are required now; retain actual UI construction, configuration, motion, desktop/mobile readability and Run/Reset cases for the post-catalogue Chrome gate. Do not claim visual/browser qualification from source checks.

**Sources and conflict resolution:** [scene contract](../../docs/engine-contracts.md#element-scene-contract), [locator contract](../../docs/engine-contracts.md#locator-anchored-sockets-contract), [visual direction](../../DESIGN.md#art-direction--monument-valley-inspired), [independent animation](../../DESIGN.md#independent-animation-ownership). DESIGN's older glTF delivery/conversion wording cannot replace the sole authority's .tscn package contract or require a second loader/new conversion programme. Apply compatible visual requirements; do not infer currently admitted shape/extension support from the art guide alone.

## E7 — Behavioral verification and evidence

**Trigger:** every planning, implementation or review task; choose checks by actual impact.

**Plan:** preserve requirement IDs and define independent positive/control/boundary cases through production interfaces. For runtime work include intended tick progression and applicable lifecycle; for documentation/tooling-policy-only work select source, link, preservation, dependency and consistency checks. Numerical assertions name the gameplay defect or mathematical/interface contract, units and duration, and reuse quantity-specific tolerances. Preserve intentional timers and causal order, not incidental arrival ticks, historical trajectories or every contact. Use generous finite stall guards.

**Accept:** meaningful assertions directly catch the intended defect; no output hashes/digests/byte identity, copied oracles, vacuous assertions or skipped/excluded game tests. Exact IDs, specified counts, authored Reset data and canonical fields remain valid. Before a second numeric-only repair, state the observable defect or correct the obsolete test contract without fitting bounds to output. For runtime slices use the matching incremental build, affected native/compiled-TS checks, one full local solution round with zero failures and Skipped: 0, compiler logs and normal hooks under the linked commands. Scoped hooks do not replace the full round. Reuse valid proof; repeat only for relevant changes, failures or concrete gaps. No mutation runs before post-catalogue E2E. Documentation-only work does not require a runtime rebuild/test round or gain runtime qualification.

**Sources:** [testing guidance](../../docs/testing-guidance.md), [float comparison and effort](../../docs/testing-guidance.md#float-comparison-and-effort-rules), [local test authority](../../docs/delivery-workflow.md#local-test-authority), [game fidelity](../../docs/delivery-workflow.md#game-fidelity), [hard test rules](../../AGENTS.md#engineering-and-gameplay-requirements).

## E8 — Scope, review, delivery and deferred qualification

**Trigger:** scope selection, story planning/readiness, implementation acceptance or publication.

**Plan:** start from [TODO](../../TODO.md), then affected [requirements](../../docs/planning/requirements.md), [named element rows](../../docs/planning/invest/named-elements.md) and [ordered roadmap](../../docs/planning/invest/vertical-delivery.md#rolling-playable-roadmap). Preserve every named element/variant and the 150 progressively taught levels target. Take one bounded outcome with necessary dependency closure, including its cleanup/proof; no broad subsystem prerequisite programme or added element bundling. A single missing engine capability split out by the [capability probe](bmad-loop-protocol.md#capability-probe) is a bounded prerequisite slice, not such a programme. Current high-priority engine prerequisites precede new elements. Keep one BMAD Auto loop and its independent review layers, fresh implementation ownership and fixes returned to that owner; no duplicate review system.

**Accept:** native planning/readiness/triage artifacts identify scope, applicable rules, actual evidence and remaining named gates. Mandatory Anvil pre-write validation and normal hooks remain. After required local checks and independent approval, publish only when publication is authorized for the task; the owner's 29 October no-CI-wait instruction controls. Record remote build/artifact/deployment qualification unverified unless independently established; do not wait for or investigate CI as a prerequisite to continuation. Only an authorized backlog goal permits continuing to the next slice; a scoped customization task does not.

**Deferred, not waived:** actual Chrome/Playwright UI proof and browser/toolchain suites remain deferred until every named puzzle element passes native unit/integration tests; record pending cases/owners in existing slice records. No browser gate or mutation run during catalogue implementation. Detailed profiling, injected faults, retention/device stress and global qualification stay at their named [stage gates](../../docs/delivery-workflow.md#stage-gates) and [release checklist](../../docs/browser-physics-performance.md), including P0-034 and P0-035. Basic correctness, ordinary lifecycle, typed general engines and production builds remain required under the current cadence; CI owns Release/publish, so no local Release worker/web publish. A scoped Pass never implies browser, release or repository-wide qualification.

**Sources:** [focused execution and current owner precedence](../../AGENTS.md), [playable-first](../../docs/delivery-workflow.md#playable-first), [native-first cadence](../../docs/delivery-workflow.md#fast-test-cadence), [standing requirements](../../docs/delivery-workflow.md#standing-requirements).

## E9 — Maintainability gate

**Owner rule, 5 November 2026.** This is a hard gate. It cannot be skipped, waived or deferred, and pre-existing code is not exempt. It applies to every slice that starts after adoption, under the AGENTS.md Stable rules. 9A.46e, in progress at adoption, finishes under its starting rules.

**Owner rule, 6 November 2026:** the gate also enforces balanced coupling, and test projects are excluded from both checks. These changes follow the same Stable rules: 9A.46g, in progress on 6 November, finishes under its starting rules, so coupling findings do not block it.

**Trigger:** every Auto, Build (including oneshot and small-change), Code Review or engineering Review approval, whatever the change touches. The gate scores all production and tool code, not only the diff, so a clean diff does not pass it on its own. Test projects are excluded.

**Tool and scores:** the .NET SDK's code-metrics analyzer checks every analysed C# type and member against two thresholds:
- **Maintainability index (CA1505, 0–100): at least 20.** That is the floor of Microsoft's "good maintainability" band (20–100 good, 10–19 moderate, 0–9 low).
- **Cyclomatic complexity (CA1502): at most 25**, the rule's default. The index blends size and complexity logarithmically, so a short member with heavy branching can still score 20 or more; this limit catches members with too many paths to test or change safely.

Class coupling (CA1506) and depth of inheritance (CA1501) are deliberately not gated. CA1506 counts every framework, Godot and FlatBuffers type a class uses, which is not what the architecture's coupling model measures, so balanced coupling is gated instead (below). The only inheritance-depth hit comes from Godot's own control hierarchy.

**Balanced coupling:** the same command applies the [Balanced Coupling model](../../docs/architecture/element-package-isolation.md#3-balanced-coupling-couplingdev) to every dependency between two production projects. It reads the compiled IL of the same builds.
- **Strength.** Contract: a public type or member of a declared contract project, either the Abstractions family or the FlatBuffers wire contract. Model: a public type or member of any other project. Intrusive: non-public code reached through `InternalsVisibleTo`, an `UnsafeAccessor` method or `BindingFlags.NonPublic` reflection, or one source file compiled into two production assemblies. Functional coupling leaves no trace in metadata and stays with review.
- **Distance.** Every measured dependency crosses a project boundary. Coupling inside one project is left to the metrics above and to review.
- **Volatility.** Declared in the tool. Engine and element projects are volatile; the Abstractions, the wire contract and tools are stable. New `elements/*` and `tools/*` projects are classified by path. Any other new project is refused until the owner declares its kind.
- **Failing dependencies:**
  - `SiblingElement`: an element references another element;
  - `ElementBoundary`: an element references anything outside the Abstractions;
  - `Intrusive`: any intrusive dependency;
  - `UnbalancedModel`: model coupling to a volatile project.

Repair a coupling finding in one of three ways: integrate through a contract (move the type or surface into an Abstractions project), remove the intrusive path, or remove the dependency. Merging projects to shorten the distance changes declared boundaries and needs an owner decision.

`dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root <root>` builds every C# compile the pre-commit static gate builds: the solution, its PLAYTEST variant, the web host and every file-based app. It injects both metric rules and thresholds for that run only. Before scoring anything it runs two canaries. The metric canary must be reported for both rules, and the same code in a test project for neither. The coupling fixture solution must produce exactly one finding per strength and rule, with its clean controls passing. Output lines are `FAIL MI`, `FAIL CC` and `FAIL COUPLING`, with an `EDGE` count for each failing dependency between two projects. The command exits non-zero on any violation or failed build. `--coupling` runs the coupling check alone for diagnosis; it does not replace the gate. The tool lists every exclusion:
- test projects (`IsTestProject=true`), printed as `EXCLUDED test project`;
- the vendored FlatBuffers runtime (static-gate opt-out);
- FlatBuffers-compiler output marked `<auto-generated>`.

File-based apps are scored by the metrics. They have no project references, so coupling does not apply to them. A production project that references a test project is refused.

**Plan:** in the existing Code Map/verification plan, name the gate and its executor. Design new or changed members to stay within both thresholds. Design new dependencies between projects as contract coupling, or as model coupling to a stable project. When the gate already lists violations, include the refactor closure for every one in the slice scope; existing violations are not deferred to another slice.

**Accept:** apply the [loop protocol's Maintainability gate](bmad-loop-protocol.md#maintainability-gate) to the actual final candidate:
- A passing run is required.
- Balanced coupling is mandatory on every approving route. It runs inside the gate command; `--coupling` on its own never replaces the gate. A run whose output lacks the `Coupling dependencies:` summary or either canary `PASS` line is **Incomplete**.
- Every listed type, member or coupling dependency, including pre-existing code outside the diff, is a confirmed high finding. It is repaired by a behaviour-preserving, forward-only refactor with behavioural test coverage, then independently re-reviewed.
- The following are each a **Fail**:
  - suppressions (`#pragma`, `SuppressMessage`, editorconfig severity or exclusions);
  - marking hand-written code as generated;
  - moving code out of the gate's build scope;
  - changing a declared project kind or volatility;
  - marking a production project as a test project (`IsTestProject`), or moving production code into a test project;
  - changing a threshold, dropping a metric rule or coupling check, or changing the tool so the gate is weaker.
- A failing run is **Fail**. A refused, missing or stale run (including a non-compiling candidate) is **Incomplete**.

**Sources:** [maintainability gate tool](../../tools/anvil/VerifyPolicy.cs), [loop protocol](bmad-loop-protocol.md#maintainability-gate), [CA1505](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1505), [CA1502](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502), [code metrics values](https://learn.microsoft.com/visualstudio/code-quality/code-metrics-values), [Balanced Coupling in the architecture](../../docs/architecture/element-package-isolation.md#3-balanced-coupling-couplingdev), [coupling.dev](https://coupling.dev).
