# Epic 10 Context: Constrained Mechanics & Elastic Fixtures

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the solid cylinder collider and inertia, pivots, pulleys, tensile ropes, counterweights and compliant elastic fixtures, all expressed as unified TGS Soft constraints with no ad-hoc velocity projections or Baumgarte stabilization. Players can then build contraptions with true wheeled shafts and pivot posts, see-saw impact levers, pulley-routed rope networks, swinging pendulums and bouncing trampolines that transfer loads and store and release elastic energy without artificial energy gain.

## Stories

- Story 10.1: Solid Cylinder Collider & Cylinder Inertia
- Story 10.2: Pulley Wheel & Tensile Rope Dynamics (aggregate parent; closes with 10.2a–10.2c)
- Story 10.2a: Weight
- Story 10.2b: Rope Anchor, tensile rope and cutting
- Story 10.2c: Pulley wheel and routed rope
- Story 10.3: Balanced Impact Lever & Pivot Fulcrum
- Story 10.4: Counterweight & Multi-Body Pendulum Lifting
- Story 10.5: Trampoline Compliant Membrane Dynamics

## Requirements & Constraints

- **Cylinders:** contact follows the true curved side and flat end faces against spheres, boxes and the workbench plane, with no sphere, box or prism-hull stand-in. A dynamic cylinder rolls on its side and rests on an end face without jitter. Inertia compiles from the declaration: axial $m r^2/2$, transverse $m(3r^2 + w^2)/12$. Cylinder is a closed collider-shape enum value end-to-end.
- **Ropes:** tensile-only constraints couple admitted Tie/Load endpoints; slack ropes apply zero force and equal loads stay balanced. A typed cut/release command removes the constraint atomically at the committed boundary; repeated or unknown cuts return a typed refusal and leave other ropes unchanged. Tests cover zero and maximum admitted length, incompatible or missing endpoints, slack/taut crossing, cutting before and after load, and multiple independent ropes.
- **Pulleys:** route a rope between unequal Weights so the heavy load falls and the light load rises; routing obeys length and contact boundaries. Preserve every CAT-053 variant and its source geometry.
- **Impact Lever:** pivots on a solid cylinder fulcrum (not an art-only post), strikes its end stop and launches a payload resting on the opposite arm when a heavy ball drops on one arm. Rope tension routed through two Pulleys from a falling Counterweight to a lever end rope socket lifts that arm. **Open item:** indefinite balance under equal loads is not met because the hinge is frictionless and loads resting on the beam form an unstable equilibrium. A separate engine slice adds a declared pivot holding mechanism (friction or detent) and re-qualifies the lever tests against indefinite balance.
- **Pendulum:** a suspended Counterweight released from an angle oscillates with period $T = 2\pi\sqrt{L/g}$ and damps gradually. Reset restores the initial angle and zero velocity.
- **Trampoline:** the membrane deflects with compliant stiffness and launches the ball upward. Off-centre hits deflect along the surface normal. Kinetic energy after rebound never exceeds the initial potential energy.
- **Validation:** non-positive or non-finite dimensions or mass, incompatible sockets and out-of-range parameters are rejected atomically before any state changes.
- **Lifecycle:** exact Reset and supported Save/Load restore authored poses, endpoints, lengths and uncut construction, and rerunning reproduces behaviour.
- **Verification:** native C# tests run the real production simulation through defined interfaces and cover positive, negative/control, boundary, tick-progression and Reset/Save/Load cases. Bounds are derived from gameplay. Each story's named Chrome suite (`tools/e2e/*.test.ts`) is deferred to the post-catalogue E2E phase, so browser qualification stays pending.

## Technical Decisions

- Each element is declaration data in its own `elements/<Name>/` package: a `.tscn` scene with visual meshes, dedicated collision proxies (`col_box`, `col_sphere`, `col_cylinder`, …), `Loc_Port_<Id>` socket locators, mass and material coefficients, constraint parameters and typed parameters. No element identifier, branch, class or tick loop exists outside `elements/`. The solver, compiler and workers stay generic.
- Revolute hinges with end stops, ropes, pulley routing, springs and compliant membranes compile into generic `ConstraintTable` rows. These run in the TGS Soft solver with compliance, softness and effective mass, at 120 Hz with four 480 Hz substeps on the WASM SIMD physics worker.
- Game-grade envelope:
  - Resting penetration is at most 0.5 mm.
  - Linear velocity is clamped to 64 m/s and angular velocity to 128 rad/s.
  - Non-finite candidate state is discarded and the last committed pose is kept.
  - Residuals clamp or continue; they never fault the tick.
- Game values are f32. Time is integer ticks plus an f32 phase. Closed sets are enums, and strings are converted only at genuine authoring or serialization boundaries.
- Mutations are sequenced typed commands that return receipts, for example rope cut. Queries read committed state. Transient occurrences go on the event ring. Rendering consumes poses one-way from the pose ring.
- Refactoring is forward-only: no fallback, shim, legacy path, version branch or second rule for the same behaviour. Each story deletes the legacy it supersedes, for example the 24-sided prism-hull shaft in 10.1 and superseded rope paths in 10.2b.
- Non-C# runtime and harness code is strict TypeScript, built by the pinned `tsc`.

## UX & Interaction Patterns

Standard Workshop loop: place elements, adjust typed sliders, connect compatible sockets (including ropes) on declared locators, Run, observe, then Reset. Moving meshes track committed physics poses. Keep the approved palette.

## Cross-Story Dependencies

- **10.1** (cylinder) is an engine prerequisite for the 10.2c Pulley wheel and the 10.3 Lever fulcrum. The engine restitution energy-correctness slice followed it.
- **10.2a** (Weight) supplies the dynamic load body for 10.2b, 10.2c, 10.4 and the 10.5 tether controls.
- **10.2b** supplies the generic rope domain and cut command used by 10.2c, the 10.3 rope sockets and 10.4. It does not depend on the Pulley.
- **10.2c** completes the two-pulley counterbalance and the parent 10.2 criteria.
- **10.3** also depends on impact restitution and the Story 6.2 Bumper. Its indefinite equal-load balance waits on the separate pivot-holding engine slice.
- **10.5** depends on the Story 6.4 elastic spring constraint, the Stories 6.6–6.7 pipe (drop-to-pipe lesson), 10.2 ropes and the 10.4 Weight tethers.
