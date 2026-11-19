# Epic 12 Context: Conserved Airflow, Pneumatics & Buoyancy

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver conserved airflow, pneumatic pressure and buoyancy through reusable physical laws. This bounded context covers only Story 12.1a: restore the distinct Tennis ball as a constructible, configurable package with credible gravity, bounce, drag, contact and lifecycle behavior. It does not approve planning or broaden the slice to Balloon, Fan or the remainder of Epic 12.

## Stories

- Story 12.1a: Tennis ball.

## Requirements & Constraints

CAT-064 and EL-189 require a lightweight high-bounce spherical payload whose response differs from Bowling because of declared material, never identity-selected physics. Retain mass, bounce, radius, buoyancy and drag as the required configuration field set; the harvest's zero-parameter proposal and consumer row's “Fixed” label do not erase that obligation. Preserve each admitted mode, canonical serialized fields and committed appearance.

Required-now native acceptance must discriminate gravity, rebound, stable supported rest and drag; compare matched Basketball/Bowling controls and verify Receiver capture, Switch impact and Pressure plate loading. The harvested one-ball/two-ball threshold control assumes 0.35 kg balls and a 0.5 kg plate: resolve default scale before transferring this expectation. Define positive, negative and boundary cases for configured values, malformed/nonfinite/unknown inputs and atomic refusal. Run/Reset restores exact authored state; save/load retains decoded configuration and reproduces semantic behavior.

Use the production declaration/compiler/simulation interfaces and shared integration harness. Assertions protect gameplay outcomes, contact clearance, causal events and stable lifecycle, with quantity-specific justified tolerances and finite generous stall guards. Historical arrival ticks, full trajectories, byte identity, kind-bit equality and overprecise resting goldens are not acceptance. No skipped required game tests or mutation runs.

Planning must answer the eight engine-design questions and obtain the native bounded planning review for any changed reusable schema or ownership boundary. Later implementation requires matching affected builds/checks, the complete local solution round with zero failures/skips, normal hooks, independent Auto review and E9 maintainability/coupling gate. Context compilation runs none of those gates and grants no approval. Chrome construction/configuration/lifecycle cases remain pending at post-catalogue qualification; remote build/artifact/deployment remain unverified unless independently established. Publication does not wait for CI.

## Technical Decisions

Retain the package-owned Godot native-art scene and explicit C# ownership approach. Tennis owns declarations, typed configuration, art and bindings; shared engines own equations. Use one current schema, validated atomic installation and explicit typed references. Maintain f32 C# WASM SIMD simulation and one-way committed physics → animation/presentation flow. No product callbacks, element-ID branches, compatibility reader, fallback or second rule path. Name actual superseded paths during planning; historical deletion targets are not proof that a file still exists.

Planning decisions approved for Story12.1a (19November2026): see the native spec for exact typed contracts and domains. Canonical defaults are0.35kg,0.25m, restitution0.85, friction0.3, threshold0.1m/s, rolling resistance0, drag0.04s⁻¹ and buoyancy0. Five BuildOnly fields remain configured. Uniform normalized atmosphere p=1 is the admitted model; variable pressure/profiles/finite atmosphere remain12.1b/S470 before their first consuming interaction, including12.2.

Source questions that led to those decisions (resolved by the native spec):

- **Default scale and restitution:** the CAT harvest records mass 0.35 kg, radius 0.25 m and bounce 0.78. The PRD Tennis row (`_bmad-output/planning-artifacts/prd.md:90`) gives 0.057 kg, 0.033 m and 0.85; parent Story 12.1 expressly requires 0.85. The CAT harvest explicitly retains its canonical values, with bounce already marked unresolved. Resolve current-contract authority and record canonical values before implementation; do not silently combine incompatible presets. This conflict needs a recorded planning resolution, not an automatic user-approval stop.
- **Configuration and inertia:** retain the five-field contract and define typed ranges, defaults, units, UI bindings and current save/load semantics. Coordinator preparation reports existing sphere geometry binding, but independent mass/radius variation needs correct solid-sphere inertia proportional to mass × radius². A one-parameter polynomial does not establish that contract; the existing force × speed power product is not an inertia operation. Verify the bounded reusable capability needed and prove it with unfamiliar parameter combinations.
- **Contact material:** friction 0.3 and bounce threshold 0.1 m/s are inherited defaults requiring explicit interpretation. Rolling resistance has no specified Tennis value. Choose a justified package declaration and distinguish its effect from drag without tuning assertions to observed output.
- **Drag and buoyancy:** harvest drag is linear 0.04 1/s with pressure scaling, and buoyancy is zero by default. Resolve pressure/environment interpretation and the required configurable buoyancy modes. Zero-buoyancy dry operation has no fluid prerequisite; a nonzero mode requires its actual declared law and admission contract. Do not permanently reject a required mode merely to claim completeness.
- **Visual/physical consistency:** configured radius, collider, mass properties and committed artwork must agree. Keep geometry and presentation writers explicit; cosmetics never change physical truth.

Engineering authority for planning is the primary-workspace project engineering contract E1–E9 and element-free design/review guides D1–D7, together with the loop protocol. Apply affected communication/clock rules if those paths change; do not invent worker or ring work for this package.

## UX & Interaction Patterns

Return Tennis to bundle-driven palette discovery with its own felt-yellow ball and seam, matching icon and model; do not reuse the Basketball scene. Preserve approved palette and readable silhouette. Players place/configure through generic authoring, Run, observe, Reset and adjust. Typed refusals remain visible and preserve installed construction. Native geometry and lifecycle proof is required now; actual Chrome visual/interaction qualification remains pending.

## Cross-Story Dependencies

Existing generic sphere/contact/drag behavior is the starting prerequisite, subject to current-input applicability. Do not reopen completed Cannon or other prior slices. Fan owns conserved jet transfer and Tennis airflow integrations in Story 12.2: retain heavier-body, no-fan, missed/blocked-jet controls and source fixtures there. Future liquid/gas modes activate S416/S470 only when their admitted behavior needs those laws. Balloon is a separate slice; Windmill and Bellows are not dry-Tennis prerequisites. Preserve campaign fixture obligations and the 150-level goal without adding the later campaign sweep to this slice.

Scoped inputs: `planning-artifacts/epics.md` catalogue ownership and parent 12.1; `planning-artifacts/prd.md` Tennis row/core loop; architecture spine authority note; `docs/planning/elements/CAT-064-tennis.md`; requirements CAT-064, EL-189 and SIM/ELEMENT profiles; named-elements EL-189; current-consumers CAT-064-I; roadmap Epic 12/contact-airflow applicability; general-engine-element-map CAT-064 and mode-specific applicability. Apply `docs/testing-guidance.md`, especially float comparison and effort rules. Paths are repository-relative except planning artifacts, which are beneath `_bmad-output/`.

Candidate baseline is `90fe6fcacf80460cbefc1b310138f79f4bb17edd` in `/Users/aidan/.codex/worktrees/tim-12-1a/tim`, branch `story-12-1a-tennis`. The checkout's TODO still describes Wound spring; the coordinator reports the primary-workspace TODO now records published Cannon and current Tennis entry. The explicit published-baseline/selected-story handoff governs this context. No sibling specs or historical results were loaded. This file records planning context and decisions; implementation acceptance remains in the native spec/review.

## Implementation Result

Native Story 12.1a implementation is complete: five configured controls, matching package artwork, shared typed physics declarations and production interaction/lifecycle evidence passed independent review. Full local round: 6844 passed, 0 failed, 0 skipped; normal candidate hooks: 134 passed, 0 failed, 0 skipped; E9 PASS. Exact candidate 2ceff309d070be4cfea4954665ada74e1a0c8dc3 received independent SnapshotApproval; completion amendment and publication are coordinated separately. The native spec retains review and raw-evidence references. Browser qualification remains pending; variable pressure/finite atmosphere and Fan retain their existing 12.1b/S470/12.2 ownership. This result changes no normative scope or defaults.
