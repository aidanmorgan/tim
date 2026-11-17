# Epic 11 Context: Rotary Drive & Mechanical Actuators

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver electric motors, conveyors, clutches, reversing transmissions, linear pushers, wound springs and toy cannons as declarative mechanisms that exchange real, bounded work. Players should be able to construct readable drivetrains whose direction, loading, disconnection and restoration follow physical state.

## Stories

- Story 11.1a: Electric Motor
- Story 11.1b: Conveyor
- Story 11.2a: Clutch
- Story 11.2b: Reverse Transmission
- Story 11.3: Linear Pusher
- Story 11.4a: Wound Spring
- Story 11.4b: Toy Cannon

The split children are implementation units; grouped 11.1, 11.2 and 11.4 retain aggregate acceptance. Orientation and ownership: [epics](../planning-artifacts/epics.md#catalogue-slice-ownership). This refresh supports 11.2b at published baseline `e9b7f79472598c3a46622878e61570c5f6207143`; it does not authorize sibling implementation.

## Requirements & Constraints

Finite-inertia shafts must share signed motion and load reaction through the same work calculation. Reversal is a fixed −1 phase-free ratio between two shafts, without electrical/activation input or a new energy store. No source work is duplicated downstream. An unpowered source contributes no new work; existing momentum may coast. Source loss is not a command to erase physical momentum.

CAT-057 and EL-203 require both input directions, two reversers restoring direction, load/backdrive, disconnected controls, connection-order and whole-part-rotation invariance, supply loss and exact authored Run/Reset restoration. Save/load must preserve supported poses, configuration and typed connections. Prove conveyor transport direction against a bypass/unpowered control and the later-child integration with the engaged/disengaged Clutch. These obligations come from [CAT-057 requirements](../../docs/planning/requirements.md#current-cat-057), [EL-203](../../docs/planning/requirements.md#element-203), [retained belt behavior](../../docs/planning/requirements.md#todo-151), and the [rotary contract](../../docs/rotary-transmission-parts.md). Their concrete declarations are in [CAT-057](../../docs/planning/elements/CAT-057-reverse_transmission.md) and [EL-203](../../docs/planning/elements/EL-203-reverse-transmission.md).

Use real production C# native behavior tests with positive, control and boundary cases. Assert decoded outcomes and independently derived expectations; choose quantity-specific tolerances by gameplay clearance and duration. Historical exact ticks, copied-speed values, floating-point residuals and buffer/hash equality are not acceptance. Preserve intended timers without pinning incidental arrival timing. Apply [testing guidance](../../docs/testing-guidance.md).

The primary checkout's [AGENTS.md](/Users/aidan/dev/personal/tim/AGENTS.md) and [loop protocol](/Users/aidan/dev/personal/tim/_bmad/custom/bmad-loop-protocol.md) govern current workflow. Required local solution tests have zero failures/skips; normal hooks are additional. Run the authoritative full round once per round with matching normal compiler evidence; the maintainability gate's injected build does not replace it. Independent Auto review and repair re-review remain mandatory. Publish the approved candidate after local gates; do not wait for CI. Remote build/artifact/deployment qualification stays unverified unless established independently. Browser/E2E and mutation testing remain deferred until catalogue closure; inherited Chrome clauses are future qualification obligations, not current gates.

## Technical Decisions

Use declaration packages over shared, element-free capabilities, with typed identities and closed enums at domain boundaries. A shaft has finite inertia, a physical cylinder/hull, a local frame and anchored hinge. Mechanical sockets bind owned guides; ratios operate in the common constraint solve. Art and observations read committed travel, never independently integrated angles or copied endpoint speed.

The architecture is canonical f32, fixed-rate WASM SIMD physics, and one-way physics-to-animation/render data flow. Presentation cannot alter simulation. Follow [package isolation](../../docs/architecture/element-package-isolation.md), [numeric/execution authority](../../docs/gpu-f32-physics.md) and [planning architecture](../planning-artifacts/architecture.md).

Use one current implementation path: no compatibility formats, aliases, migrations, fallback solver or element-specific engine branch. Refuse unsupported input atomically with typed reasons. Resolve genuine missing reusable capability decisions before implementation through the native planning gate; apply both current engine design/review guides referenced by the primary rules. Anvil validation precedes every write. Confirmed defects, including pre-existing ones, require bounded repair and independent re-review.

## UX & Interaction Patterns

Maintain the approved [DESIGN.md](../../DESIGN.md) palette and physical readability: cream housing, navy base, ochre reverse wheels and navy/gold direction witnesses. Wheels follow their own committed hinges; clockwise-positive readings use the marked local face and remain meaningful after whole-part rotation. Typed mechanical links must remain distinct from power and activation. Preserve toolbox construction, configured placement, Run/Reset and save/load flows; queue their actual Chrome qualification.

## Cross-Story Dependencies

11.2b explicitly depends on published Motor 11.1a and Clutch 11.2a; Conveyor 11.1b provides the actual transport load. Battery and existing cylinder/hinge capabilities support these fixtures. The dispatch establishes these prerequisites as delivered; old readiness-spec “missing” inventories are harvest-era descriptions, not instructions to recreate infrastructure. Consult current declarations and capabilities for applicability, and inspect historical evidence only for a named reuse question.

11.2b owns the retained Story 11.2 motor→reverse→clutch integration because it is the later child. Later windmill and campaign teaching remain their scheduled consumers. The EL identity refinement preserves CAT acceptance and does not silently add a crossed-belt variant. Legacy open questions about topology, contact material and wheel contact support must be reconciled with current generic contracts and actual supported behavior in the slice plan; do not infer decisions from obsolete missing-capability lists.
