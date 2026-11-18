# Epic 11 Context: Rotary Drive & Mechanical Actuators

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver motors, conveyors, clutches, reversing transmissions, linear pushers, wound springs and toy cannons as declarative actuators with physically meaningful motion and bounded work. This bounded refresh supports Story 11.3 only, on baseline `c34f63903722295ae0e130aa3d279876d20420bb`, branch `story-11-3-linear-pusher`. It is context, not readiness approval.

## Stories

- Story 11.1a: Electric Motor
- Story 11.1b: Conveyor
- Story 11.2a: Clutch
- Story 11.2b: Reverse Transmission
- Story 11.3: Linear Pusher
- Story 11.4a: Wound Spring
- Story 11.4b: Toy Cannon

Split children retain their grouped acceptance. Only the selected story and genuine prerequisites are context; sibling status does not authorize loading or implementation.

## Requirements & Constraints

[CAT-039](../../docs/planning/requirements.md#current-cat-039), [retained behavior](../../docs/planning/requirements.md#sequence-task-309), [declaration contract](../../docs/planning/elements/CAT-039-linear_pusher.md) and [Story 11.3](../planning-artifacts/epics.md#story-113-linear-pusher-telescopic-actuator-cat-039) require independent electrical supply, extend and retract commands; bounded stroke, speed, acceleration, effort and work; and supplied endpoint outputs visible on the next electrical tick. Conflict or no command holds; power loss engages the ideal self-locking brake at the current pose. Both directions, mid-stroke reversal and interruption are required now.

Package parameter ranges are stroke 0.25–3 m, speed 0.25–4 m/s, acceleration 1–30 m/s² and force 1–100 N, with defaults 2.4 m, 1.5 m/s, 12 m/s² and 40 N. Story 11.3's 1.0 m stroke is a configured case, not a conflicting default. Power is bounded by force × speed; electrical supply enables the ideal servo without inventing battery depletion or a pneumatic model.

Preserve cargo push and miss, no-supply/conflict controls, endpoint holding, low-force gravity support, stall/obstruction and rotated mounting. Head and telescoping rod are physical: bodies and rays cannot pass through a visible functional solid. Stalled motion delivers no new work; observation cannot double-count solver work. Reject invalid parameters and invalid initial overlap atomically. Run captures authored configuration; Reset restores authored construction and initial motion/work; save/load preserves decoded configuration, connections and meaningful replay behavior.

Use real production C# native positive/control/boundary and lifecycle checks. Apply [quantity-specific comparison rules](../../docs/testing-guidance.md#float-comparison-and-effort-rules): independently justify tolerances by units, gameplay clearance and duration, preserve intentional timing and causal order, use generous finite stall guards. Legacy exact trajectories, tick deadlines, nanometre thresholds and bit-exact replay are not new acceptance obligations.

The primary checkout's [AGENTS.md](/Users/aidan/dev/personal/tim/AGENTS.md), [loop protocol](/Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md) and [engineering contract](/Users/aidan/dev/personal/tim/_bmad/custom/project-engineering-contract.md) govern over older candidate policy copies. Required full local solution results have zero failures/skips; normal hooks are additional. E9 maintainability/coupling and independent Auto review remain required; its injected build does not replace normal compiler evidence. Publish approved work after local gates without waiting for CI; remote build/artifact/deployment remain unverified unless established. Browser/E2E and mutation testing remain deferred until all named elements pass native tests; detailed stress, injected faults and release qualification retain their later gates.

## Technical Decisions

Declare the mechanism in a package over typed, reusable capabilities: fixed frame, finite-mass spherical head, bounded prismatic coordinate, independent electrical inputs and supplied endpoint contacts. Geometry, materials, parameter ranges and visual mappings belong to package data. Simulation owns physical motion/work; animation reads committed observations and cannot move collision or affect physics. Preserve canonical f32, established WASM SIMD cadence and one-way Tri-Graph ownership.

Before readiness, settle the bounded generic controller's command precedence, acceleration/deceleration and reversal law, hold capture, endpoint/blocked observations and causal electrical timing. Define effort/power limiting and actual delivered-work accounting together with the constraint solve. Binary routing alone does not prove bounded work.

Also settle the physical telescoping-rod representation and its coherent collision/query/render update ownership; endpoint and blocked thresholds; head rolling resistance; placement-assistance applicability; exact capability references and atomic refusal boundaries. These are Story 11.3 planning decisions, not permission to remove required behavior. DESIGN already resolves the hold as ideal self-locking. Electrical command sources exist; the harvest-era activation-only Switch concern is obsolete.

Apply current [element-free design](/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-design.md) and [review policy](/Users/aidan/dev/personal/tim/_bmad/custom/element-free-engine-review.md): record the eight design answers and discriminating unfamiliar compositions before independent planning approval. No element-selected laws, callbacks, ordinal inference or implicit product recipes. Use one current representation with typed references, explicit writers and atomic validation. Identify/delete superseded paths; no fallback, compatibility alias or migration. Anvil validates every write; confirmed defects require repair and independent re-review.

## UX & Interaction Patterns

Follow [Pusher design](../../DESIGN.md#electric-linear-pusher): cream barrel/navy foot, gold rod/travel marks, cream spherical head/cyan ring, distinct lower inputs and upper endpoint outputs. Slate means held/unpowered, cyan moving, gold loaded/blocked/conflicting. Art follows actual collision extension; never animate through obstructions or spring-return after power loss. Preserve minimal contextual configuration and wiring, original pictogram and typed socket icons; no permanent inspector. Queue actual Chrome construction, parameter/connection verification, motion, Run/Reset and save/load proof.

## Cross-Story Dependencies

Use the existing slider/limits foundation from 6.4, Battery supply from 8.1 and Powered Gate's bounded slider-drive work from 8.2 where their current contracts actually apply. Harvest inventories are not current missing-capability proof. ImpactSwitch and StateLatch electrical outputs resolve command sourcing; sensor/delay integration still needs an explicit electrical command route.

Retain sensor → delay → pusher moving Conveyor cargo into a Pipe, plus tube/conveyor/rope/multiple-actuator integration. Native applicable interaction proof belongs to the consuming slice; campaign and sustained mobile/browser qualification retain their named later gates. No prerequisite expands this slice into a sibling implementation or a general subsystem programme.
