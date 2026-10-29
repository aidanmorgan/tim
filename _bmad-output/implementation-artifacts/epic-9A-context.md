# Epic 9A Context: Sole Architecture Conformance (Project-per-Element, Command/Query, Event Disruptor)

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Remove parallel implementations so each puzzle element has one isolated declaration package, the runtime consumes one generated bundle, mutations and reads use typed commands and queries, and discrete occurrences use the event ring. Preserve existing gameplay while making element-specific engine code a build failure.

## Stories

- Story 9A.0: Sole architecture authority
- Story 9A.1: Dead legacy purge
- Story 9A.2: Single FlatBuffers codec
- Story 9A.3: Typed transport commands and queries
- Story 9A.4: Lossless bundle build gate
- Story 9A.5: Receiver package
- Story 9A.6: Bowling Ball package
- Story 9A.7: Delay Box package
- Story 9A.8: Funnel package
- Story 9A.9: Clear Pipe package
- Story 9A.10: 45-degree Pipe Bend package
- Story 9A.11: 90-degree Pipe Bend package
- Story 9A.12: Springboard package
- Story 9A.13: Impact Switch package
- Story 9A.14: Bundle as sole catalogue
- Story 9A.15: Bundle element identity in saves and messages
- Story 9A.16: Bundle as sole collider source
- Story 9A.17: Bundle ports and footprints
- Story 9A.18: Packaged icons
- Story 9A.19: Declared Powered Gate drive
- Story 9A.20: Declared Pressure Plate sensor
- Story 9A.21: Remove synthetic startup work
- Story 9A.22: Event ring producer
- Story 9A.23: Event ring consumers
- Story 9A.24: Typed observation port
- Story 9A.25: Packaged presentation bindings
- Story 9A.25a: Committed-state presentation cues
- Story 9A.25b: One-shot presentation bindings
- Story 9A.26: Lessons and puzzles as data
- Story 9A.27: Generic instance/editor — Wall
- Story 9A.28: Generic instance — Ramp
- Story 9A.29: Generic instance — Receiver
- Story 9A.30: Generic instance — Basketball
- Story 9A.31: Generic instance — Bowling Ball
- Story 9A.32: Generic instance — Domino
- Story 9A.33: Generic instance — Pinball Bumper
- Story 9A.34: Generic instance — Springboard
- Story 9A.35: Generic instance — Impact Switch
- Story 9A.36: Generic instance — Signal Lamp
- Story 9A.37: Generic instance — Ball Detector
- Story 9A.38: Generic instance — Delay Box
- Story 9A.39: Generic instance — Battery
- Story 9A.40: Generic instance — Pressure Plate
- Story 9A.41: Generic instance — Pulse Counter
- Story 9A.42: Generic instance — System Clock
- Story 9A.43: Generic instance — State Latch
- Story 9A.44: Remove binary16 lanes
- Story 9A.44B: f32 game values and integer-tick clock
- Story 9A.45: Compile-time isolation gate
- Story 9A.45a: Early isolation ratchet
- Story PKG-1: Receiver, Bowling Ball and Springboard packages
- Story PKG-2: Funnel and pipe packages
- Story PKG-3: Delay Box and Impact Switch packages
- Story C-Switch: Switch conversion and threshold editor
- Story C-Ramp+Receiver: Ramp and Receiver conversion
- Story C-Balls: Basketball and Bowling Ball conversion
- Story C-Bumper+Spring: Bumper and Springboard conversion
- Story C-Gate: Gate drive and startup conversion
- Story C-Lamp+Detector: Lamp and detector conversion
- Story C-Plate: Plate sensor and instance conversion
- Story C-Clock: Clock conversion and pulse behaviour
- Story C-Memory: Counter and latch conversion

## Requirements & Constraints

Preserve every existing element, unlimited Workshop, authored construction and the 150-level campaign target. Each slice deletes its superseded path and updates necessary callers, content and tests together. No fallback, old-format adapter, alias, migration, format version or second behavioural path survives. Invalid inputs receive typed, visible, atomic refusal.

Use f32 end to end for game values, retaining integers for identities/counts and named external adapters for external numeric types. Replace floating game time with integer ticks plus f32 phase; validate finite values, ranges, enums and overflow at boundaries.

Acceptance protects puzzle outcomes, causal order, plausible motion, stable rest and lifecycle. Reuse quantity-specific tolerances justified by functional clearance and observation duration; no universal epsilon or historical trajectory oracle. Use the least strict assertion that detects the named defect, with generous finite completion guards. Preserve deliberate timer/delay/cadence semantics, exact authored state and typed refusals. Assert decoded semantic fields, never output hashes or whole-buffer identity. Before a second numerical repair, identify the gameplay defect; otherwise correct the assertion while retaining meaningful controls.

Real production native tests cover positive, control, boundary, progression and applicable Reset/save/load behaviour. One full local solution round is authoritative with zero failures/skips; normal hooks and independent Auto review remain required. Reuse passing evidence unless relevant inputs change or a concrete concern remains. Browser qualification and mutation testing wait until all named elements pass native proof. Use incremental builds; CI owns Release production compilation/publish and deployment. After local approval, publish and continue without waiting for CI; remote build/artifact/deployment qualification remains unverified unless established.

## Technical Decisions

Each element project owns metadata, typed parameters, ports, colliders/materials, domain payloads, events, presentation bindings and assets. It references only shared/domain abstractions, never engine or sibling packages. One build-generated bundle is the declaration authority. Both workers must reference an element-free Core assembly containing capability tables and their codec, solver contracts, activation and event infrastructure. Authoring, catalogue, lessons and bundle loading stay in a main-thread-only assembly that neither worker references. Element identity is opaque, without named element members; build isolation derives prohibited identities from the generated bundle and eventually has no known-violation list.

The main-thread authoring graph compiles at Run into contiguous SIMD-aligned physics tables. Pure C# WASM SIMD runs generic capability kernels at 120 Hz with 480 Hz substeps. Animation runs independently at 60 Hz; rendering uses WebGL2/WebGPU instancing and the shared triple pose ring. Information flows from simulation to presentation only.

Mutations use typed sequenced commands with receipts; queries use committed reads and poses. A read-only production observation port exposes receipts, committed queries and an independent event cursor, without mutation authority, console channels or ad-hoc globals. Discrete occurrences use one producer and independent consumer cursors; persistent levels remain committed state. Element names never select engine logic.

Use C# wherever possible; affected browser/worker glue and harnesses use strict TypeScript and pinned compilation, with tests exercising shipped output. Closed sets remain enums; extensible identities use typed IDs. Structural boundary validation replaces version checks. Storage retains actual logical counts and sparse interaction state, starts at common category defaults, and grows geometrically at demand watermarks. No arbitrary hard counts or worst-case owner-by-target reservation. Event growth preserves retained history, sequence and same-epoch cursors; failed candidates publish nothing.

## UX & Interaction Patterns

Preserve placement, parameter editing, typed wiring and Run/Pause/Step/Reset flows. Reset restores authored state exactly through semantic fields. Save/load preserves supported constructions; refusal leaves them unchanged. Packaged live cues expose state without influencing physics. Preserve the approved palette and design language.

## Cross-Story Dependencies

Merged delivery stories retain all source acceptance; split stories close only when every child closes. Current execution order supersedes historical numerical order. Bundle tooling precedes package consumers; the generic editor supports instance conversions; declared capabilities and presentation bindings precede their consuming elements. The early isolation ratchet shrinks during conversion; the final gate removes remaining exceptions.

Current sequencing has published the typed observation port; the final isolation gate is next before continuing the named catalogue. Observation depends on event consumers; isolation closes the remaining declaration violations. Preserve source acceptance while applying the current local-test, browser-deferral and no-mutation policies. Implement one slice from published main, complete independent Auto review and publication, then begin the next. CI and deployment monitoring do not block continuation.
