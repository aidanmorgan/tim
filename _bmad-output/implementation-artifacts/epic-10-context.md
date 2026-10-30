# Epic 10 Context: Constrained Mechanics & Elastic Fixtures

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver solid cylinder geometry and inertia, compliant elastic fixtures, pivots, pulleys, ropes, and counterweights using unified TGS Soft constraints without ad-hoc velocity projections or Baumgarte stabilization. This enables players to author physical contraptions featuring realistic wheeled shafts, seesaw impact levers, pulley-routed rope networks, swinging pendulums, and compliant trampolines that reliably transfer loads, store and release elastic energy, and strictly conserve mechanical energy.

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

Solid cylinder colliders must provide analytic curved-side and flat end-face contacts against planes, spheres, boxes, and cylinders without polyhedral hull stand-ins. Dynamic cylinders must roll smoothly and rest stable on end faces; rotational inertia must compile from declared radius, width, and mass using standard cylinder laws (axial $m r^2 / 2$, transverse $m (3 r^2 + w^2) / 12$).

Tensile ropes couple declared, compatible body endpoints through unilateral distance constraints that transmit tensile work only; slack ropes exert zero force. Cutting or releasing a rope removes its constraint atomically, immediately decoupling connected bodies; unknown or repeated cut requests refuse cleanly without modifying other constraints. Machine reset and save/load restore original endpoints, lengths, and uncut authored construction.

Impact levers pivot around a solid cylinder fulcrum via a revolute hinge constrained to defined angular limits (±30° / ±π/6 rad) with inelastic end stops. Dropped payloads striking one arm must pivot the beam and fling resting objects upward on the opposite arm without energy gain, while equal opposing loads balance indefinitely. Levers provide rope connection sockets at beam ends that transfer tension to pivot the arm.

Pulleys redirect tensile ropes across rotary rims with low friction, transmitting mechanical work between connected loads such as counterweights. Counterweights function as balanced rope loads and as 3D pendulums oscillating at their natural period ($T = 2\pi\sqrt{L/g}$) with gradual damping.

Compliant fixtures such as trampolines yield elastically under payload impact and rebound upwards according to membrane surface normals, with total kinetic energy after rebound strictly bounded by initial potential energy (zero artificial energy gain).

Pre-run validation must atomically reject overlapping parts, incompatible socket pairings, non-positive or non-finite dimensions/mass, out-of-range parameters, or unsupported geometries before physical state changes.

Verification must execute real production C# simulation logic through defined interfaces across positive, negative/control, boundary, tick-progression, and reset/save/load scenarios. Assertions must verify observable physical behaviors, causal timing, and derived tolerances rather than output hashes or binary buffers. Browser/E2E test suites remain deferred until all named catalogue elements pass unit and integration verification.

## Technical Decisions

Each element resides in an isolated package under `elements/<Name>/` defining its Godot visual scene (`.tscn`), dedicated physics collision proxies (`col_box`, `col_sphere`, `col_cylinder`), 3D socket locators (`Loc_Port_*`), material coefficients, and typed parameter schemas. The bundle is the sole catalogue source; the core simulation engine and workers contain no element-specific identities, branches, or classes.

All constraints compile into generic Box2D v3 Temporal Gauss-Seidel (TGS) Soft solver rows executed on the dedicated WASM SIMD physics worker at 120 Hz with 480 Hz substeps (4 substeps per tick). Compliance, softness, and effective mass govern joint and contact dynamics; Baumgarte stabilization and ad-hoc velocity projections are strictly prohibited. Numerical residuals clamp or continue without faulting the tick.

Simulation state conforms end-to-end to canonical IEEE-754 32-bit floating-point (`f32`) values and integer tick counters plus f32 phase. Closed parameter sets are enums; string conversions occur only at external authoring and serialization boundaries.

Mutations execute via sequenced typed commands returning receipts; queries read committed state. Transient physical occurrences emit via the shared event ring, while persistent states reside in committed query tables. Rendering and animation consume physics poses from the zero-copy pose ring one-way without backward feedback.

Refactor forward with zero backwards-compatibility shims, legacy fallback solvers, or migration adapters. When replacing legacy constraint or collision code, superseded implementations must be purged and callers updated atomically.

Non-C# runtime and harness code must be strict TypeScript (`strict: true`, no `any`) compiled by the pinned build toolchain.

## UX & Interaction Patterns

Follow the standard Workshop interaction loop: place elements on the workbench, adjust authored sliders (such as beam mass or initial angle), connect compatible sockets (power cables, signal wires, ropes), Run to execute physical simulation, observe results, and Reset to adjust.

Connection links attach to declared 3D socket locators. Ambiguous socket attachments must be explicitly confirmed. Taut ropes render as straight spans; slack ropes curve with length-matched visual approximation; cut ropes visibly uncouple.

Preserve the approved palette (cream structures, cyan insets, ochre/gold pins, marks, and stops, navy foot and base, warm wood). Moving visual meshes track committed physics poses directly from the pose ring.

## Cross-Story Dependencies

Story 10.1 (Solid Cylinder Collider & Inertia) is an engine prerequisite for the Pulley wheel (Story 10.2c) and Impact Lever fulcrum (Story 10.3).

Story 10.2a (Weight body & mass parameter) provides the dynamic counterweight body required for rope tension and pulley balance tests.

Story 10.2b (Rope Anchor, tensile rope, and cutting) implements the generic rope constraint domain, tension solver, and cut command required by the routed Pulley (Story 10.2c), Impact Lever rope sockets (Story 10.3), and Counterweight pendulum (Story 10.4).

Story 10.2c (Pulley wheel and routed rope) provides rope routing around wheels, completing the two-pulley counterbalance integration.

Story 10.3 (Impact Lever) integrates the solid cylinder fulcrum (10.1), revolute hinge constraint with end stops, and rope-load lifting via pulley-routed counterweights (10.2b/10.2c).

Story 10.4 (Counterweight Pendulum) qualifies 3D pendulum oscillation and damping using the delivered Weight (10.2a) and tension rope (10.2b).

Story 10.5 (Trampoline) builds upon generic elastic spring constraints, relying on ball payloads and counterweight tethers for compliance and energy conservation verification.
