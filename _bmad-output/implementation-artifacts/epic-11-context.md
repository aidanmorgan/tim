# Epic 11 Context: Rotary Drive & Mechanical Actuators

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver mechanical actuators as declarative compositions with meaningful physical motion and bounded work. This refresh supports only Story 11.4a, CAT-071 Wound spring, on original baseline `f476e279cd168b77fac76d1e0c8688c77bbfb778` in `/Users/aidan/.codex/worktrees/tim-11-4a/tim`. It replaces the 11.3 context, not product behavior or readiness approval.

## Stories

- Story 11.4a: Wound Spring

The [split ownership](../planning-artifacts/epics.md#catalogue-slice-ownership) selects this child. Its source contract is a guided plunger launcher; the table's “finite shaft work” shorthand cannot replace that behavior. Cannon remains a separate child. The native story contract is [spec-11-4a-wound-spring.md](spec-11-4a-wound-spring.md).

## Requirements & Constraints

[CAT-071](../../docs/planning/requirements.md#current-cat-071), [physical wound spring](../../docs/planning/requirements.md#sequence-task-219), [retained behavior](../../docs/planning/requirements.md#todo-394) and the [declaration inventory](../../docs/planning/elements/CAT-071-wound_spring.md) require a finite-mass guided plunger, elastic store, continuous ratchet, shared stops and separate release trigger. Positive shaft travel winds within available torque/work, stroke and clearance; reverse freewheels, reduced/stopped drive retains charge. Release adds no energy and disconnects winding until physical end/rearm. Empty requests expire; repeated requests during release add nothing. Use continuous forces and solved contact motion, never direct head-velocity assignments or kinematic launch APIs.

Parameters are stiffness 40–240 N/m (default 160), stroke 0.2–0.8 m (default 0.8), and winding lead 0.025–0.2 m/rad (default 0.1). Potential is ½kq²; default full capacity is 51.2 J. Keep states, parameters, diagnostics and results typed. [Diagnostic boundaries](../../docs/planning/requirements.md#todo-085) reject noncanonical/unknown enum inputs and round-trip canonical values.

Distinguish paid winding work from legitimate contact/gravitational recharge. Missing supply/belt means no motor work, not an absolute ban on passive compression or a physically funded release. Qualify 0.5 kg and 2 kg payloads, no-trigger/empty/repeated controls, partial charge, winding obstruction, blocked release/resumption, rotated mounting and ordinary pending-request lifecycle. Charged failed-tick injected rollback remains owed at P0-032 injected-fault qualification; it is not a required-now fault-injection check (CAT-071 harvest row 12). [Loading controls](../../docs/planning/requirements.md#todo-087) include the authored 0.061-unit offset and approximately 0.508-radius load outside the 0.41 guide; valid rim contact is allowed. Include stacked loads. Ordinary Reset/replacement and pending-request lifecycle remain required now. Reset restores authored uncharged rest/latch state; save/load preserves decoded pose, parameters and typed connections.

Preserve actual teaching records “Wind, then release” and “Saved for later,” with all difficulties, controls and Reset. The former places/rotates the launcher below a payload with basket, motor supply, mechanical drive and delayed release. The latter uses a two-second Hold Timer supply followed by a three-second delayed release after winding stops. Retain authored construction and causal outcomes, not historical incidental ticks or trajectories.

Use production native positive/control/boundary and lifecycle checks with [gameplay-justified comparisons](../../docs/testing-guidance.md#float-comparison-and-effort-rules). No output hashes, skipped game tests or numerical bounds fitted to results. The [primary owner policy](/Users/aidan/dev/personal/tim/AGENTS.md), [loop protocol](/Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md) and [engineering extension](/Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md) govern over older checkout defaults. Full local solution results must have zero failures/skips; normal hooks and E9 maintainability/balanced coupling remain mandatory. Independent Auto review precedes acceptance/publication. After approved local gates, publish without waiting for CI; remote build/artifact/deployment qualification stays unverified unless established. Browser/mutation and detailed performance/stress qualification remain at their named later gates.

## Technical Decisions

Package data owns geometry, ports, ranges, composition and visual bindings. Shared typed capabilities own guide, ratchet/latch, elastic potential and finite-work coupling; simulation owns charge, physical motion, events and work. Declare exact body/joint/store/port/property references and atomic validation. Preserve f32 C# WASM SIMD, existing clocks and one-way physics-to-animation/render flow. A package-owned .tscn supplies assets/proxies/locators through the current bundle contract.

Apply [element-free design](/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md) and [review policy](/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md). Before readiness, record eight design answers, unfamiliar-composition controls and settled required-now decisions: motor/passive work attribution, charge/rearm/blocked thresholds, drive sign, materials and atomic rollback. Reuse applicable contracts; harvest-era missing-capability notes are not evidence of current absence. No identity-selected equations, callbacks, guessed row targets or per-element loops. Name/delete superseded paths without fallback, migration or aliases; Anvil validates every write.

## UX & Interaction Patterns

Follow [latched wound-spring design](../../DESIGN.md#latched-wound-spring-launcher): cream rails/plunger, navy base, transparent cyan guide, continuous silver helix and small gold latch; preserve palette and toolbox pictogram without permanent panels. Five navy marks and gold index indicate compression, not linear energy percentage. Helix/index follow committed plunger motion through obstruction. Cosmetic latch targets −0.65 radians at bounded 8 radians/second and returns after rearm; it cannot change collision or energy. Queue actual Chrome construction, typed wiring/configuration, continuous motion/readability and Run/Reset/save/load proof.

## Cross-Story Dependencies

The selected ownership names 11.1a shaft contracts. Actual source prerequisites include slider/stops/preload and hollow-guide geometry from 6.4–6.7, Battery supply from 8.1, shaft cylinder from 10.1 and shaft/ratio coupling from 11.1–11.2. Consult only applicable contracts, not sibling history. Hold Timer/delay and impact-switch contracts serve retained lessons; Powered Gate serves obstruction/resumption integration. Published Linear Pusher is the checkout baseline, not proof that its controller is the spring law. Resolve actual missing capabilities through bounded planning; do not expand into cannon feeder work or another element.
