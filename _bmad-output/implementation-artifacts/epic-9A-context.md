# Epic 9A Context: Sole Architecture Conformance (Project-per-Element, Command/Query, Event Disruptor)

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Make isolated element packages and their compiled bundle the sole declaration authority, with typed commands/queries and committed event occurrences. Extend supported reusable capabilities so new elements need declarations, bindings and assets rather than product-specific engine changes; preserve all existing gameplay.

## Stories

- Story PKG-1: Receiver, Bowling Ball and Springboard Element Packages
- Story PKG-2: Funnel, Clear Pipe and Pipe Bend Element Packages
- Story PKG-3: Delay Box and Impact Switch Element Packages
- Story 9A.25a: Presentation Bindings Ship in Packages, with Live State Cues
- Story 9A.23 + 9A.25b: Event Ring Consumers and One-Shot Presentation Bindings
- Story 9A.45a: Early Element Isolation Ratchet
- Story C-Switch: Impact Switch Generic Instance with Threshold Editor
- Story C-Ramp+Receiver: Ramp and Receiver Generic Instances
- Story C-Balls: Basketball and Bowling Ball Generic Instances
- Story C-Bumper+Spring: Pinball Bumper and Springboard Generic Instances
- Story C-Gate: Declared Prismatic Drive and Declaration-Free Warm-Up
- Story C-Lamp+Detector: Signal Lamp and Ball Detector Generic Instances
- Story C-Plate: Declared Contact-Load Sensor and Pressure Plate Generic Instance
- Story C-Clock: System Clock Generic Instance with Proven Pulses
- Story C-Memory: Pulse Counter and State Latch Generic Instances
- Story 9A.0: Architecture Authority Records the Sole Design
- Story 9A.1: Dead Legacy Purge
- Story 9A.2: Single FlatBuffers Codec
- Story 9A.3: Transport Controls as Typed Commands and Queries
- Story 9A.4: Lossless Bundle Build Gate
- Story 9A.5: Receiver Element Package
- Story 9A.6: Bowling Ball Element Package
- Story 9A.7: Delay Box Element Package
- Story 9A.8: Funnel Element Package
- Story 9A.9: Clear Pipe Element Package
- Story 9A.10: 45° Pipe Bend Element Package
- Story 9A.11: 90° Pipe Bend Element Package
- Story 9A.12: Springboard Element Package
- Story 9A.13: Impact Switch Element Package
- Story 9A.14: Bundle Is the Sole Catalogue Source
- Story 9A.15: Saves and Messages Carry the Bundle Element Id
- Story 9A.16: Bundle Is the Sole Collider Source
- Story 9A.17: Bundle Ports and Footprints
- Story 9A.18: Element Icons Ship in Their Packages
- Story 9A.19: Declared Prismatic Drive for the Powered Gate
- Story 9A.20: Declared Contact-Load Sensor for the Pressure Plate
- Story 9A.21: Remove Synthetic Worker Startup Work
- Story 9A.22: Event Ring Producer
- Story 9A.23: Event Ring Consumers
- Story 9A.24: Typed Read-Only Observation Port Replaces Console Channels
- Story 9A.25: Presentation Bindings Ship in Packages
- Story 9A.26: Lessons and Puzzles as Content Data
- Story 9A.27: Generic Instance and Parameter Editor — Wall
- Story 9A.28: Generic Instance — Ramp
- Story 9A.29: Generic Instance — Receiver
- Story 9A.30: Generic Instance — Basketball
- Story 9A.31: Generic Instance — Bowling Ball
- Story 9A.32: Generic Instance — Domino
- Story 9A.33: Generic Instance — Pinball Bumper
- Story 9A.34: Generic Instance — Springboard
- Story 9A.35: Generic Instance — Impact Switch
- Story 9A.36: Generic Instance — Signal Lamp
- Story 9A.37: Generic Instance — Ball Detector
- Story 9A.38: Generic Instance — Delay Box
- Story 9A.39: Generic Instance — Battery
- Story 9A.40: Generic Instance — Pressure Plate
- Story 9A.41: Generic Instance — Pulse Counter
- Story 9A.42: Generic Instance — System Clock
- Story 9A.43: Generic Instance — State Latch
- Story 9A.44: f32 Lane Completion — Remove Every binary16 Lane
- Story 9A.45: Compile-Time Element Isolation Gate
- Story 9A.46a: Route ropes around any declared guide
- Story 9A.46b: Use the same declared interactions in puzzles and Workshop
- Story 9A.46c: Start articulated parts in their declared joint frames
- Story 9A.46d: Add repeated typed port roles through declarations
- Story 9A.46e: Connect mechanical ports to declared coordinates
- Story 9A.46f: Drive each declared contact surface from its own coordinate
- Story 9A.46g: Compose existing signal primitives within one element
- Story 9A.46h: Reuse count and memory laws independently of product form
- Story 9A.46i: Route supplied work through declared capability nodes
- Story 9A.46j: Declare sensing and guiding independently
- Story 9A.46k: Choose visual responses through declaration data
- Story 9A.46l: Address each actuator's controls and observations
- Story 9A.46m: Enforce element-free core boundaries

## Requirements & Constraints

Preserve every named element/variant, unlimited Workshop and the 150-level goal. PERF-24/25/26 require ownership isolation, typed compilation and unfamiliar declarations that run without core edits. FR-03/06/07/09 apply to the current signal-policy work. Product ranges, physical form and appearance belong to packages; supported count, memory and oscillator laws must not inherit product recipes. Preserve current Counter, Memory and Clock behavior.

Each slice deletes its superseded path and updates affected declarations, codecs, content and callers together. No fallback, compatibility bridge, alias, version branch, automatic migration or arbitrary expression callback. Unsupported inputs receive typed atomic refusals.

Prove behavior through real production compilation/runtime: positive, no-input/control and boundary cases; causal event order; committed state; relevant Run/Reset/save/load; and an unfamiliar declaration using existing primitives. Assert decoded semantics, exact authored restoration and quantity-specific gameplay/interface tolerances. No hashes, whole-buffer identity, incidental tick goldens, skipped game tests, mutation runs or numerical reruns without an uncovered failure.

Use one final full local solution round with zero failures/skips, matching normal warnings-as-errors builds and normal hooks. Current maintainability acceptance includes metrics and balanced coupling across production/tool code; test projects are excluded. Missing evidence is Incomplete; confirmed defects, including pre-existing ones, require repair and independent re-review. Run the maintainability gate before authoritative build/test evidence because it injects analyzer configuration. Six independent Auto review layers, planning approval for changed reusable boundaries, and final scope/simplification review remain required.

Publish approved work after required local proof and hooks; do not wait for CI. Remote build/artifact/deployment qualification stays unverified unless established independently. Browser/E2E, mutation, detailed performance and device qualification remain at their named later gates.

## Technical Decisions

Use one package per element, a lossless bundle as the single source, general typed capability contracts and compile-to-state tables. Core behavior cannot branch on element identity, package class, geometry convention or product-specific recipe. Compile explicit ownership and bindings; runtime operates on compiled state rather than walking authoring objects.

Use C# where possible, strict TypeScript for necessary glue/tooling, enums for closed sets and strongly typed extensible identities. Validate strings only at genuine external boundaries. Authoring, simulation and presentation use f32.

Keep Machine, Physics and Rendering graphs separate: dedicated WASM SIMD physics at 120 Hz/480 Hz substeps, independent 60 Hz cosmetic animation, display-paced instanced rendering and zero-copy committed poses. Physics feeds presentation one-way. Typed commands mutate; typed queries observe; discrete occurrences travel through the event ring. Reset restores authored construction and connections; saves preserve canonical field values and refuse saving during Run.

## UX & Interaction Patterns

Preserve the approved palette, readable mechanism/state cues and contextual construction controls. Players place and connect typed parts, Run to observe outcomes, then Reset and adjust. Architecture repairs introduce no visual redesign and preserve current catalogue appearances.

## Cross-Story Dependencies

The declaration-only repair queue runs 9A.46a through 9A.46m before Conveyor 11.1b and later catalogue additions. Explicit prerequisite for 9A.46h is 9A.46g's composable signal nodes; story specifications own detailed acceptance. Guard closure follows capability repairs. Earlier numbered and merged delivery stories above provide orientation, not an instruction to reload historical work or repeat completed gates.

Load only the selected contract and genuine prerequisites. Preserve unrelated Conveyor and policy work. Implementation uses the isolated candidate baseline df4ac982105f1924ef222a7731feaf7036ef90c2; this context does not assert current delivery status or implementation approval.
