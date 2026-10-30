# Epic 10 Context: Constrained Mechanics & Elastic Fixtures

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver solid cylinder geometry and inertia, elastic fixtures, pivots, pulleys, ropes and counterweights through unified TGS Soft constraints. Players should build plausible mechanisms that transfer loads and stored energy, observe meaningful causal motion, and reliably restore their authored construction.

## Stories

- Story 10.1: Solid Cylinder Collider & Cylinder Inertia
- Story 10.2: Pulley Wheel & Tensile Rope Dynamics
- Story 10.2a: Weight
- Story 10.2b: Rope Anchor, tensile rope and cutting
- Story 10.2c: Pulley wheel and routed rope
- Story 10.3: Balanced Impact Lever & Pivot Fulcrum
- Story 10.4: Counterweight & Multi-Body Pendulum Lifting
- Story 10.5: Trampoline Compliant Membrane Dynamics

## Requirements & Constraints

Preserve CAT-034, CAT-053, CAT-058, CAT-065 and CAT-067 outcomes and distinct named-element obligations. CAT-058 and retained requirements todo-140/todo-141 require explicit typed endpoints, measured lengths, unilateral tension, mass-weighted reaction, meaningful slack and equal-load controls, graph admission and restored authored construction. EL-205 requires slack rope to exert neither push nor pull. EL-064 retains the fixed anchor outcome; EL-067 cutting resistance and other named variants remain separate obligations.

A complete rope couples actual attachment bodies through a finite-length constraint. Open routes carry no tension; slack connections preserve ordinary body contact. Reject incompatible or missing endpoints, duplicate/branching/cyclic routes, invalid lengths and unsupported fixed geometry atomically. Direction and declaration order must not change the intended outcome. Never invent a missing participant as a fixed point.

Cutting is required. An accepted typed cut/release removes the constraint atomically at the committed boundary; unknown or repeated cuts refuse without altering other ropes. Reset restores original endpoints, lengths and uncut construction. Save/load preserves authored construction rather than silently baking runtime cuts into it.

Before rope implementation, resolve the source-owned decisions: connect-time versus editable length and admitted bounds; compliance frequency/damping; massless rope and cosmetic sag; anchor contact material and orientation modes; campaign rope allowance; gameplay-derived slack/taut threshold; and cut/release phase with exact command/read contract. Historical values are evidence, not permission to guess these decisions.

Verify real production C# behavior through defined interfaces with positive, negative/control, boundary, tick-progression and lifecycle cases. Derive numerical tolerances from the gameplay failure or interface invariant excluded, quantity, clearance and observation duration. Test causal order and deliberate timing, not incidental trajectories or arrival ticks. Assert decoded semantic results, not output hashes or whole-buffer identity.

Use affected native and compiled-TypeScript checks, one full local solution round at the end, normal hooks and independent Auto review. Required game tests have zero failures and zero skips. Do not create or run mutants. Browser suites remain explicitly deferred until every named element passes native unit/integration proof. Publish after approved local proof and continue without waiting for CI; remote build/artifact/deployment and browser qualification remain unverified unless separately established. Do not run local Release worker builds or web publish.

## Technical Decisions

Each element owns its declarations, typed parameters/ports, material, colliders, presentation bindings and assets in an isolated package referencing only appropriate Abstractions. Bundle discovery is the single catalogue source. Main-thread authoring compiles generic capability tables; the element-free Core and workers never dispatch on element identities or load authoring records. Closed sets remain enums and extensible identities remain strongly typed.

Use canonical f32 values and the common C# WASM SIMD physics worker: 120 Hz ticks with 480 Hz substeps. Constraints use the generic TGS Soft path, without bespoke element solvers, velocity projections or Baumgarte stabilization. Numerical residuals must not fault a tick. Actual scene demand governs logical counts and geometrically growing storage.

All mutations use sequenced typed commands with receipts; queries expose committed state. Discrete occurrences use the shared event ring; persistent rope state belongs in committed reads. Rendering and cosmetic animation consume physics output without feeding back. Glue and tools that cannot be C# use strict TypeScript compiled by the normal build.

Maintain one current representation and execution path. Delete superseded rope paths and update their callers together; no compatibility aliases, migrations, version branches, fallback solvers or duplicated behavior. Unsupported data receives a typed atomic refusal.

True cylinders provide curved-side and end-face contact plus declared axial/transverse inertia. The delivered Weight uses a base-centre origin and mass-dependent geometry and centre of mass; its new Tie position must follow the declared size binding rather than assume a fixed anchor or the old spherical geometry.

## UX & Interaction Patterns

Preserve the Workshop loop: place/configure, connect compatible typed sockets, Run, observe, Reset and adjust. Use existing contextual connection and transform controls. Links attach to declared socket locators and follow committed physical poses; ambiguous compatible endpoints must be selected explicitly.

Keep the approved cream, navy, ochre/gold and warm-wood palette. Anchors have gold eyes on cream mounts. Rope endpoints have gold knots; pulley contacts do not. Taut free spans are straight, slack spans curve as a length-matched visual approximation, and incomplete threading is dashed. Cosmetic geometry must not imply an implemented collision or pulley-routing capability. Reset cancels stale visual state.

## Cross-Story Dependencies

Cylinder contact/inertia depends on generic collision and dynamic-body foundations and precedes its Pulley and Lever consumers. Weight body delivery is already published; its standalone body, mass and lifecycle work is not reopened as rope implementation. Published compile-time isolation remains mandatory.

The 10.2 parent is aggregate-only. Rope Anchor, Weight Tie/Load and cutting depend on delivered Weight and the stabilized generic constraint engine, not Pulley. Pulley follows cylinder and rope delivery and owns finite-radius routed-rope integration and the two-pulley counterbalance acceptance. Lever then proves its rope-load integration and retains supported-impact prerequisites. Weight pendulum qualification remains its own story. Trampoline retains spring, pipe, rope and Weight prerequisites and owns tether/rebound integration. These later gates stay explicit; native rope acceptance alone closes none of them.

