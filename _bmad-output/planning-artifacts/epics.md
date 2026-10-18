---
status: final
stepsCompleted:
  - validate-prerequisites
  - design-epics
  - create-stories
  - final-validation
inputDocuments:
  - 'docs/gpu-f32-physics.md'
  - 'docs/planning/invest/vertical-delivery.md'
  - 'docs/planning/invest/named-elements.md'
  - 'docs/planning/invest/campaign.md'
  - 'docs/planning/requirements.md'
  - 'docs/delivery-workflow.md'
  - 'TODO.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/prd.md'
---

# Curious Contraptions — Epic Breakdown

## Overview

This document provides the complete, authoritative BMAD epic and story breakdown for Curious Contraptions, decomposing the engineering requirements, INVEST vertical roadmap, element catalogue (CAT-001 through CAT-072 across all 7 families), and 150 campaign challenge levels into implementable vertical slices.

Every story is an INVEST vertical slice: delivering one player-observable behavior through real UI, adding generic data-driven capability, naming the legacy it deletes, and requiring independent adversarial review with pure TypeScript Playwright verification in Chrome.

---

## Requirements Inventory

### Functional Requirements

- **FR-01 (Machine Graph Authoring):** The player can assemble contraptions on the workbench using catalogue puzzle elements, configure parameters, and connect sockets via typed signal wires.
- **FR-02 (Run/Reset Loop):** The player can press Run to compile the machine into a physical simulation, observe dynamic behavior, and press Reset to restore the exact authoring state.
- **FR-03 (WASM SIMD Physics Solver):** Continuous physics advances at 120 Hz (480 Hz substeps) on a dedicated Web Worker via 128-bit WebAssembly SIMD without external physics engine dependencies.
- **FR-04 (TGS Soft Constraint Mechanics):** Contacts, resting stability, and restitution follow the Box2D v3 Temporal Gauss-Seidel Soft Step formulation with compliance $\gamma$ and softness $\beta$.
- **FR-05 (Speculative Contacts Anti-Tunneling):** Fast-moving bodies allocate speculative contacts in narrowphase with target velocity absorption to prevent discrete tunneling through barriers of arbitrary thickness.
- **FR-06 (Decoupled Animation Pipeline):** Cosmetic animations (e.g. halo pulse, switch depression, spring squash) evaluate at 60 Hz on a dedicated animation worker fed one-way by physics events.
- **FR-07 (Catalogue Puzzle Elements):** 72 distinct catalogue elements implemented purely as declarative capability data without bespoke solver branches.
- **FR-08 (150 Campaign Levels):** 150 progressively taught challenge levels structured across 5 chapters (30 levels per chapter, taught in 10-level increments) covering mechanical, electrical, pneumatic, optical, and acoustic principles.
- **FR-09 (Deterministic Save/Load):** Constructions can be saved to and loaded from JSON preserving exact IEEE-754 `f32` coordinates and connection graphs.

### Non-Functional Requirements

- **NFR-01 (Zero External Physics Dependencies):** 100% in-engine custom C# and TypeScript algorithms incorporating Quad-BVH, TGS Soft, and speculative contacts. No Box2D, Jolt, Rapier, or PhysX libraries.
- **NFR-02 (Universal Browser Graphics):** Presentation executes universally on both WebGL 2.0 and WebGPU via instanced draw batches from a lock-free `SharedArrayBuffer` triple pose ring.
- **NFR-03 (Forward-Only Refactoring):** Zero backwards-compatibility shims, legacy flags, or CPU fallback paths. Superseded code is deleted in the slice that replaces it.
- **NFR-04 (Game-Grade Numerical Envelope):** Clamp-or-continue numerical policy. Residuals clamp; non-finite states preserve prior poses; ticks never fault.
- **NFR-05 (Strict Type Safety):** Closed sets are enums end-to-end; extensible identities use strongly typed IDs; string conversions exist only at external boundaries.
- **NFR-06 (Automated Verification Rigor):** Every slice must pass `anvil check --changed` with 0 warnings, `dotnet test CuriousContraptions.slnx` 100% (557+ tests), and serial Playwright E2E tests in actual Chrome (`node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/<suite>.test.ts` plus all cumulative regression suites).

---

## Global Acceptance Criteria & Story Completion Definition of Done (DoD)

Every single story across all epics must formally satisfy the following mandatory Story Completion Criteria before it can be accepted or marked complete:

1. **C# Unit Tests Pass 100%:** `dotnet test CuriousContraptions.slnx` passes 100% with 0 failures (557+ tests).
2. **Anvil Static Analysis Clean:** `anvil check --changed` passes with **0 warnings**.
3. **Browser-Based Playwright E2E Suite Passes 100%:** The story's dedicated browser-based Playwright E2E test suite (`tools/e2e/<suite>.test.ts`) executed in actual Chrome via the Playwright browser connector passes 100%.
4. **Cumulative E2E Regression Suites Pass 100% Serially in Chrome:** All prior cumulative browser-based E2E regression suites (`tools/e2e/*.test.ts`) execute serially in actual Chrome (`node --test --test-concurrency=1 --test-timeout=150000`) and pass 100%. A 100% pass rate across both the slice's E2E test and all cumulative E2E regression suites is mandatory before any story can be marked complete. Any browser failure or regression strictly blocks completion.
5. **Exact Reset and Save/Load Persistence Roundtrip:** Exact state restoration before Run and after Reset, as well as JSON Save/Load persistence roundtrip, is verified in Chrome.
6. **Zero Legacy Remnants or Compatibility Bridges:** Active tree grep confirms complete absence of backwards-compatibility shims, legacy flags, or deprecated code remnants.
7. **Terminal Scoped Pass from Independent Adversarial Reviewer (Murdoch):** Independent adversarial review subagent verifies all criteria and issues an unambiguous terminal scoped `Pass`. Self-review never qualifies.

---

## Epic List

1. **Epic 1: Engine Core Foundation** (ENGINE-CORE-1, 2a1, 2a2, 2a3, 2a4) — *[Done]*
2. **Epic 2: Compliant Solver Streamlining** (ENGINE-CORE-2b1, 2b2, 2b3, 2b4) — *[Done]*
3. **Epic 3: Decoupled Sensor Evaluation** (ENGINE-CORE-2c) — *[In-Progress]*
4. **Epic 4: Dedicated WebAssembly Animation Worker** (ANIM-1a, ANIM-1b, ANIM-1c) — *[Backlog]*
5. **Epic 5: Dynamic Polyhedral Rigid Bodies & Domino Mechanics** (CAT-023a, CAT-023b) — *[Backlog]*
6. **Epic 6: Core Interactive Catalogue Elements & glTF Data-Driven Architecture** (CAT-014 Bowling Ball, CAT-015a-b Bumper, CAT-062a-b Springboard, Story 6.5b Dynamic PartId Discovery, Story 6.5c glTF Collider Extraction, Stories 6.5d-e Existing Implementation Conversion, Story 6.5f Zero-Remnant Purge, CAT-048a-b Pipe, CAT-002 Ball Detector, CAT-049 45° Pipe Bend, CAT-050 90° Pipe Bend, CAT-030 Funnel) — *[In-Progress]*
7. **Epic 7: Legacy Code Purge & Zero-Remnant Clean Architecture** (LEGACY-0a Historical Archives, LEGACY-0b Probe Tools, LEGACY-0c Dead Tests, LEGACY-0d Legacy CPU Physics, Procedural PartArt & Unshipped Engine Purge) — *[In-Progress]*
8. **Epic 8: Supplied Electrical Power** (Battery Power Source, Powered Gate) — *[Backlog]*
9. **Epic 9: Activation, Timing & Electrical Logic** (Pressure Plate, Pulse Counter, System Clock, State Latch, Hold Timer, Dual-Supply Both, Electrical Logic Gates Nand/Nor/Or/Xor) — *[Backlog]*
   - **Epic 9A: Sole Architecture Conformance (Project-per-Element, Command/Query, Event Disruptor)** (Stories 9A.0–9A.45: authority docs, legacy purge, single codec, typed transport controls, lossless bundle, nine missing element packages, bundle as the sole catalogue/collider/port/icon source, declared drive and load sensor, event ring producer and consumers, typed observation port, package presentation bindings, lessons as data, one generic-instance conversion per element, f32 lane completion, compile-time isolation gate). Delivered after Stories 9.4 and 9.4b and before Story 9.5 (owner decision, 18 Oct 2026); Story 9.4c precedes it by coordinator placement. Its section follows Epic 9. — *[Backlog]*
10. **Epic 10: Constrained Mechanics & Elastic Fixtures** (Solid Cylinder Collider & Inertia, Pulley, Rope Anchor, Impact Lever, Counterweight, Trampoline) — *[Backlog]*
11. **Epic 11: Rotary Drive & Mechanical Actuators** (Electric Motor, Conveyor Belt, Mechanical Clutch, Reverse Transmission, Linear Pusher, Wound Spring Motor, Toy Cannon) — *[Backlog]*
12. **Epic 12: Conserved Airflow, Pneumatics & Buoyancy** (Tennis Ball, Floating Balloon, Electric Fan, Windmill, Pneumatic Bellows) — *[Backlog]*
13. **Epic 13: Optical Propagation, Routing & Sensing** (Torch, Laser, Red/Green/Blue Lasers, Tuned Optical Receivers, Beam Shutter, Spectral Color Filters, Flat Mirror, Beam Splitter & Combiner, Secondary Receivers, Solar Panel, Optical Logic Gates) — *[Backlog]*
14. **Epic 14: Acoustic Emission & Reception** (Sound Level Meter, Service Bell, Audio Speaker, Wind Chimes) — *[Backlog]*
15. **Epic 15: 150 Progressively Taught Campaign Levels** (Stories 15.1–15.15 covering all 150 levels in 10-level batches across Chapters 1 to 5, with verified UI solutions and retry loops) — *[Backlog]*
16. **Epic 16: Release Qualification, Performance Audits & Production Packaging** (Cross-platform headless/browser performance audit, WebGL2/WebGPU qualification, 60 FPS display pacing, memory leak audits, and release candidate bundle) — *[Backlog]*

Story order follows the owner re-ordering of 9 Oct 2026 (dependencies): each story comes after what it needs, except the open scheduling items listed in epics.md (below). Every moved story carries a "Depends on" line naming its former number. The old-to-new mapping is in [vertical-delivery.md](../../docs/planning/invest/vertical-delivery.md#owner-reordering-dependencies).

### Open scheduling items

All items are decided and applied below. The owner decided items (b), (c), (e), (f) and (g) on 18 Oct 2026. The coordinator decided items (a), (d) and (h) on 18 Oct 2026 under the owner's delegation that day: "make the decisions based on the intent of the game, it's a physics puzzle game — if there are missing definitions that make the game more playable then you can feel free to add them". Each delegated decision carries a one-line playability rationale. The Battery acceptance that names the Electric Motor in Story 8.1's acceptance (Motor delivery: Story 11.1) is an acceptance conflict for the owner's case-by-case review, not a scheduling item.

- **(a) RGB lasers (EL-176–178) — decided 18 Oct 2026 by coordinator under owner delegation (playability).** They fold into Epic 13 as three new stories, one element each (AGENTS.md): 13.1b Red Laser Emitter (EL-176), 13.1c Green Laser Emitter (EL-177) and 13.1d Blue Laser Emitter (EL-178). They come after Story 13.1 and before Story 13.2. Rationale: players get legible coloured beams before they need to sort colours, and every colour receiver has a direct, teachable source.
- **(b) Switch electrical mode — decided 18 Oct 2026.** Story 9.7 (Electrical NAND & NOR, formerly 9.3) wires its gates to input power switches, which needs the CAT-063 Switch `PowerIn`→`Supply` mode. The CAT-013, CAT-026, CAT-027 and CAT-039 specs cite the same need. Owner decision: a new Story 9.5b (Switch Electrical Mode) after the Hold Timer (9.5) and before the Both Gate (9.6).
- **(c) Sound meter source — decided 18 Oct 2026.** The Sound Level Meter's acceptance uses the Service Bell, which followed it. Owner decision: a separate acoustic-propagation engine story (Story 14.0) comes first, then the Service Bell (14.2), then the Sound Level Meter (14.1) using the Bell as its source, then the Speaker (14.3) and Wind chimes (14.4). Story numbers are kept; the delivery order is 14.0, 14.2, 14.1, 14.3, 14.4.
- **(d) Receivers' colour source — decided 18 Oct 2026 by coordinator under owner delegation (playability).** Story 13.2's pure-channel receivers needed coloured beams. CAT-056 names the red filter (now Story 13.4) as its pure source, and CAT-032 and CAT-012 name the green and blue filters, while the filters use the receivers as observers. That was a loop. Item (a) breaks it: Story 13.2's pure-channel receivers use the Story 13.1b–13.1d coloured lasers as their pure sources, and the filters (Story 13.4) then use the receivers as observers. Rationale: each colour receiver can be taught with a single source and a single beam.
- **(e) Story 10.1 placement — decided 18 Oct 2026.** Owner decision: both the CAT-053 Pulley wheel and the CAT-034 Lever solid fulcrum are true cylinders, so Story 10.1 stays first in Epic 10.
- **(f) Pulley and Lever loop — decided 18 Oct 2026.** Story 10.2 (Pulley) used a Lever as the rope load, but the Lever (Story 10.3) follows it. Owner decision: the Pulley acceptance uses the Counterweight body (CAT-067, shipped with Story 10.2 per CAT-067 §3) as the rope load; the Lever's rope-load integration is proven in Story 10.3.
- **(g) Sole architecture conformance — owner decisions, 18 Oct 2026.** Evidence: the [architecture conformance audit](architecture-conformance-audit-2026-10-18.md).
  1. Retire the old code. What remains must be the only avenue available: **project per puzzle element**, **command/query** and the **event disruptor** pattern are the only allowable design.
  2. There is one source of element declarations: **the element packages plus the bundle**. Each `elements/<Name>` package is the only declaration of its element. The build produces one complete bundle and the runtime loads only that. The `.tres` catalogue files and the C# `PartId` tables are deleted.
  3. Timing: Epic 9A runs right after Stories 9.4 and 9.4b, before any new element (Story 9.5 and later). The coordinator placed the latch-dependent Story 9.4c (which adds no new element) between 9.4b and 9A, so that its two suites guard Epic 9A's supply and drive refactors.
  
  Follow-on owner decisions the same day:
  - Review-9-4 F6 (the compiled switched-supply route, which replaces the per-tick `Program.SwitchedSupplies`/`ElectricalState` walk of the authoring graph) is fixed inside Story 9.4, not in Epic 9A.
  - Story 9A.44 migrates every remaining binary16 (`Half`) lane to f32, so that `Half` appears nowhere in the tree. This resolves CAT-037 open question 2.
  - The State Latch gains an ActivationOut socket in Story 9.4 (CAT-037 open question 1).
  - Story 9.4 may reach done with the six pre-existing failing Chrome suites (cat-005, cat-015a, cat-015a-sidekick, cat-015b, cat-023b, cat-062b) carried by Story 9.4b, which must restore a 100% cumulative serial run before Story 9.4c or Epic 9A start. The independent review confirmed 9.4 did not cause or worsen them (review-9-4, rounds 1 and 2).
- **(h) Definitions decided under owner delegation, 18 Oct 2026 (coordinator, playability).**
  - **CAT-051 Q1.** The Powered Gate's return spring is its fail-closed law (Story 9.4c). Rationale: a gate that falls shut on power loss is predictable and teachable.
  - **CAT-005 Q6.** Wires are not edited during Run; supply loss is a switched contact (Story 9.4c). Rationale: Run is for watching, the bench is for building.
  - **Tennis Ball and Balloon** leave the palette until Story 12.1 ships their packages (Story 9A.14). Rationale: a part that throws when placed is unplayable.
  - **Palette categories stay** as the player sees them today (Story 9A.4). Rationale: a refactor must not reshuffle the toolbox.
  - **A synthetic worker warm-up is kept** (Story 9A.21). Rationale: avoids a visible first-Run hitch.
  - **The e2e observation port** is typed, read-only and part of the single production build (Story 9A.24). Rationale: Chrome proof must exercise the build players get.
  - **Pressure Plate threshold.** The CAT-052 §2 parameter governs: `minimum_mass` f32 kg, range 0.1–16, default 0.5. Story 9.1's "> 0.2 kg" wording is superseded (Story 9A.20). Rationale: as shipped, one light ball is too light and a heavy ball triggers it, which is a clear lesson.
  - **Collider source (Story 9A.16).** `BundleElements` extracts each package's colliders from that package's own scene or glTF at build time, using the Story 6.5c extraction, and bakes them into the bundle. The runtime loads only the bundle. A package with both data-declared and scene colliders fails the build. Rationale: colliders always match the art the player sees.
  - **Saves from before a format bump are rejected** with a visible message, never migrated (Stories 9A.2, 9A.15, 9A.44; AGENTS.md).

---

## Epic 1: Engine Core Foundation

**Goal:** Establish the WebAssembly SIMD multi-body execution core, lock-free `SharedArrayBuffer` triple pose ring, Box2D v3 TGS Soft solver, Dynamic AABB BVH with 15-axis SAT narrowphase, and speculative contacts anti-tunneling, delivering reliable real-time physics on the workbench.

### Story 1.1: Game-Grade Contact & Per-Substep Normalization (ENGINE-CORE-1)
*Status: Done*

As a player,  
I want balls to bounce and settle on the workbench predictably,  
So that contraptions run reliably without tick faults or simulation stalls.

**Acceptance Criteria:**
- **Given** an authored puzzle construction (`first_principles`, `delayed_signal`, or Bumper demo),
- **When** the player runs the simulation across 20 consecutive runs,
- **Then** the simulation completes without any tick faults or numerical divergence.
- **And** quaternion normalization is applied every substep via fast inverse square root.
- **And** legacy single-body shaders and puzzle-keyed guide branches are completely deleted.
- **And** exact authoring state is restored on Reset and Save/Load roundtrip.

### Story 1.2: Multi-Body Pose Ring & Dual-Sphere Simulation (ENGINE-CORE-2a1)
*Status: Done*

As a player,  
I want to place and simulate multiple basketballs simultaneously,  
So that multi-body interactions render smoothly with independent trajectories.

**Acceptance Criteria:**
- **Given** the Free Workshop with 16 body slots in the `SharedArrayBuffer` triple pose ring,
- **When** the player drops two Basketballs from different heights via real UI controls,
- **Then** both balls simulate concurrently in WASM SIMD and render via universal instanced batches.
- **And** empty construction and single-ball control baselines pass cleanly.
- **And** single-body pose slot assumptions are permanently deleted.
- **And** exact Reset and Save/Load restore identical configurations.

### Story 1.3: Box2D v3 TGS Soft Solver & FeatureId Warm-Starting (ENGINE-CORE-2a2)
*Status: Done*

As a player,  
I want resting contacts to settle smoothly without jitter or explosive bouncing,  
So that stacked or resting puzzle elements remain rock-solid under gravity.

**Acceptance Criteria:**
- **Given** balls dropped onto the static workbench or inclined surfaces,
- **When** collisions occur under the TGS Soft solver with compliance $\gamma$ and softness $\beta$,
- **Then** balls settle into quiet resting contact without bounce jitter.
- **And** normal and friction impulses warm-start using persistent 32-bit `FeatureId` caches.
- **And** restitution $e \in [0, 1]$ dissipates kinetic energy (second bounce lower than first).
- **And** obsolete `physics.wgsl` compute shaders, Baumgarte stabilization, and hard velocity clamps are deleted.

### Story 1.4: Dynamic AABB BVH & 15-Axis SAT Narrowphase (ENGINE-CORE-2a3)
*Status: Done*

As a player,  
I want balls to roll realistically down angled Ramps and bounce off resized Walls,  
So that complex contraptions can be constructed and solved.

**Acceptance Criteria:**
- **Given** an authored level containing Ramps and Walls (`first_principles`),
- **When** the simulation executes using Dynamic AABB BVH with velocity fattening and 15-axis SAT narrowphase,
- **Then** the basketball rolls smoothly along the ramp and deflects accurately off walls into the receiver.
- **And** local-axis Wall resizing and arbitrary ramp angles are supported generically.
- **And** brute-force bounding loops and ad-hoc box collision branches are permanently deleted.
- **And** tests across suites 2a1, 2a2, and 2a3 pass 100% serially in Playwright.

### Story 1.5: Speculative Contacts Anti-Tunneling for High-Speed Impacts (ENGINE-CORE-2a4)
*Status: Done*

As a player,  
I want high-speed balls to never pass through thin barriers,  
So that contraption mechanics remain completely reliable regardless of velocity.

**Acceptance Criteria:**
- **Given** a Basketball launched at maximum UI speed against a Wall of any admitted thickness ($\ge 1\text{ cm}$),
- **When** the narrowphase generates speculative contact constraints with margin $d_{\text{spec}} = |\mathbf{v}_{\text{rel}} \cdot \mathbf{n}| \, h + s_{\text{slop}}$ and target velocity absorption $v_{\text{target}} = -g/h$,
- **Then** the ball rebounds cleanly and never tunnels through the wall across 20 runs in Chrome.
- **And** pure TypeScript Playwright suite `tools/e2e/engine-core-2a4.test.ts` passes 100% serially.
- **And** ad-hoc discrete tunneling clamps and legacy ray-cast CCD fallbacks are completely deleted.

---

## Epic 2: Compliant Solver Streamlining

**Goal:** Permanently remove host shadow verification, motion-piece error lanes, clearance/closing certificates, directed rounding math, interval-arithmetic sweeps, and legacy guide predictors, completing the transition to a pure TGS Soft solver.

### Story 2.1: Host Validation & Error Lane Removal (ENGINE-CORE-2b1)
*Status: Done*

As an engine developer,  
I want simulation poses published directly through the lock-free `SharedArrayBuffer` ring without host shadow validation,  
So that execution latency and memory bandwidth overhead are minimized.

**Acceptance Criteria:**
- **Given** the dedicated WASM simulation worker publishing poses to the triple ring,
- **When** the browser client consumes poses for rendering,
- **Then** poses render directly without host per-tick byte validation or motion-piece error lanes.
- **And** Chrome ball drop and ramp solve pass cleanly with zero shadow verification code in `BrowserWorkshopClient`.
- **And** exact Reset and Save/Load persistence remain 100% verified.

### Story 2.2: Certificate & Directed Rounding Removal (ENGINE-CORE-2b2)
*Status: Done*

As an engine developer,  
I want constraint limits enforced exclusively via pure TGS Soft compliance,  
So that complex mathematical certificate verifications and directed rounding checks are removed.

**Acceptance Criteria:**
- **Given** resting contact and multi-body ramp rolling in Chrome,
- **When** constraints are solved solely through compliant TGS Soft formulations,
- **Then** simulation remains completely stable under gravity.
- **And** `ClearanceBoundarySweep`, `LipschitzBoundarySweep`, `JointBoundarySweep`, and directed rounding routines are deleted.
- **And** a grep across the active physics solver confirms zero certificate references.

### Story 2.3: Analytic CCD & Interval Library Removal (ENGINE-CORE-2b3)
*Status: Done*

As an engine developer,  
I want continuous collision handled entirely by speculative contacts,  
So that analytic polynomial sweep solvers and interval arithmetic libraries are eradicated.

**Acceptance Criteria:**
- **Given** a 64 m/s high-speed ball impact against thin walls,
- **When** collisions resolve via speculative contact margins,
- **Then** zero tunneling occurs across 20 Chrome test runs.
- **And** analytic swept roots, interval-arithmetic range libraries, and remaining `physics.wgsl` references are deleted.
- **And** reviewer grep confirms zero interval root-finding routines in active code.

### Story 2.4: Guide Horizon & Departure Ownership Removal (ENGINE-CORE-2b4)
*Status: Done*

As an engine developer,  
I want player assistance forces modeled as declarative spatial force regions,  
So that complex guide horizon predictors and departure ownership code are eliminated.

**Acceptance Criteria:**
- **Given** the `first_principles` level with receiver assistance knots,
- **When** assistance forces are evaluated as declared spatial acceleration fields,
- **Then** the ball guides smoothly into the receiver without moving physical collider walls.
- **And** `GuideHorizon`, departure ownership routines, and `PlanarGuideBoundaryPath.cs` are deleted.
- **And** reviewer grep confirms zero guide predictor code in the solver.

---

## Epic 3: Decoupled Sensor Evaluation

**Goal:** Refactor all sensors and triggers (aperture crossing, residence, impact switches) to evaluate at substep endpoints with tick-accumulated dwell, replacing continuous root-finding with clean discrete events.

### Story 3.1: Endpoint-Sampled Sensors & Dwell Tick Counter (ENGINE-CORE-2c)
*Status: Done*

As a player,  
I want the Receiver to reliably capture balls meeting the speed and dwell criteria,  
So that goal feedback triggers deterministically without continuous curve root-finding.

**Acceptance Criteria:**
- **Given** the Receiver placed in the Workshop,
- **When** the Basketball enters the capture region and remains for the authored dwell duration at $\le 1.5\text{ m/s}$,
- **Then** the Receiver emits a typed captured event and activates its visual halo.
- **And** a fast through-pass ($> 1.5\text{ m/s}$) or glancing boundary contact does NOT trigger capture.
- **And** sensor continuous root-finding, `CurveCuts`, and sub-phase residence intervals are deleted.
- **And** exact Reset clears capture state and timers cleanly.
- **And** `tools/e2e/engine-core-2c.test.ts` passes 100% serially in Chrome alongside all cumulative regression suites.

---

## Epic 4: Dedicated WebAssembly Animation Worker

**Goal:** Establish an independent 60 Hz WebAssembly animation worker consuming committed physics state over a shared event channel, driving cosmetic secondary animations without main-thread or simulation bottlenecks.

### Story 4.1: Dedicated 60 Hz Animation Worker Pipeline & Core Feedback (ANIM-1a)
*Status: Done*

As a player,  
I want glowing lamps and pulsing receiver halos to animate smoothly at 60 Hz,  
So that presentation feedback feels fluid and independent of physics sub-stepping.

**Acceptance Criteria:**
- **Given** active contraptions with a lit Signal lamp and captured Receiver,
- **When** the simulation executes with physics paused or running,
- **Then** cosmetic halo pulses and lamp glow animate continuously via the animation worker at 60 Hz.
- **And** hardcoded main-thread timer loops for halo and lamp are permanently deleted.
- **And** rendering never synchronously blocks waiting for the animation worker.
- **And** `tools/e2e/anim-1a.test.ts` passes 100% in Chrome serially.

### Story 4.2: Mechanical Cosmetic Bindings & Procedural Curves (ANIM-1b)
*Status: Backlog*

As a player,  
I want Bumpers, switches, and delays to visually compress and fill,  
So that physical activation gives immediate, satisfying visual feedback.

**Acceptance Criteria:**
- **Given** mechanical elements receiving impact or activation,
- **When** cosmetic procedural curves (squash/stretch for Bumper, depression for Switch, progress fill for Delay) evaluate,
- **Then** visual components animate smoothly according to declared curve bindings.
- **And** element-specific cosmetic evaluators on the main thread are deleted.
- **And** `tools/e2e/anim-1b.test.ts` passes 100% in Chrome serially.

### Story 4.3: Legacy Presentation Code Retirement (ANIM-1c)
*Status: Backlog*

As an engine developer,  
I want all legacy presentation loops and ad-hoc update hooks deleted,  
So that rendering consumes only the unified pose ring and animation worker channels.

**Acceptance Criteria:**
- **Given** all currently playable contraption parts,
- **When** the full presentation pipeline runs,
- **Then** all visual elements animate correctly through declared bindings.
- **And** retired presentation classes in `engine/presentation/*.cs` and `ui/*.cs` are permanently removed.
- **And** reviewer grep confirms zero element-keyed presentation update loops in the tree.
- **And** `tools/e2e/anim-1c.test.ts` passes 100% in Chrome serially.

---

## Epic 5: Dynamic Polyhedral Rigid Bodies & Domino Mechanics

**Goal:** Deliver full 3D dynamic polyhedral rigid body physics with 3x3 inertia tensors, 4-point area-maximizing contact manifolds, and declarative orientation sensors, unlocking domino cascade puzzles.

### Story 5.1: Dynamic Box Rigid Body & Upright Stability (CAT-023a)
*Status: Backlog*

As a player,  
I want to place Dominoes standing upright on the workbench,  
So that they stand stably under gravity and topple realistically when struck.

**Acceptance Criteria:**
- **Given** an upright Domino placed on the workbench in the Workshop,
- **When** the simulation runs,
- **Then** the domino stands stably without wobbling, drifting, or falling over under gravity.
- **And** when struck by a dropped Basketball, it topples smoothly and comes to rest flat on the workbench without jitter.
- **And** narrowphase evaluates a constant 4-point area-maximizing contact manifold with Box2D v3 TGS Soft friction.
- **And** ad-hoc box inertia approximations are deleted.
- **And** `tools/e2e/cat-023a.test.ts` passes 100% in Chrome serially.

### Story 5.2: Domino Cascade Mechanics & Orientation-Threshold Sensor (CAT-023b)
*Status: Backlog*

As a player,  
I want dominoes to knock each other down in a cascade and trigger connected devices,  
So that chain-reaction contraptions can solve the `domino_effect` level.

**Acceptance Criteria:**
- **Given** four dominoes arranged in sequence connected to a Signal lamp via an orientation threshold sensor,
- **When** the first domino is struck,
- **Then** all dominoes topple in sequence and the lamp lights once.
- **And** an incomplete chain of three dominoes fails to light the lamp (negative control).
- **And** a small 10° nudge does NOT trigger the orientation sensor.
- **And** `parts/DominoPart.cs` ad-hoc physics logic is deleted in favor of generic rigid body and orientation sensor declarations.
- **And** `tools/e2e/cat-023b.test.ts` passes 100% in Chrome serially.

---

## Epic 6: Core Interactive Catalogue Elements

**Goal:** Implement the foundational mechanical and aperture interactive elements by pure data declaration and integrate them into solvable campaign levels.

### Story 6.1: Bowling Ball Dynamic Sphere by Material Declaration (CAT-014)
*Status: Backlog*

As a player,  
I want to use a heavy Bowling Ball to knock down heavy obstacles,  
So that different mass and density properties enable varied physical puzzles.

**Acceptance Criteria:**
- **Given** a Bowling Ball placed alongside a Basketball,
- **When** both strike identical domino targets,
- **Then** the heavier Bowling Ball topples targets that resist the Basketball.
- **And** Bowling Ball is implemented purely via material declaration data without bespoke solver branches.
- **And** `tools/e2e/cat-014.test.ts` passes 100% in Chrome serially.

### Story 6.2: Bumper Radial Contact Impulse & Finite Work Store (CAT-015a)
*Status: Backlog*

As a player,  
I want Bumpers to impart an energetic bounce only when powered or charged,  
So that kinetic boosting respects thermodynamic energy limits.

**Acceptance Criteria:**
- **Given** a Bumper configured with a finite work store,
- **When** a ball contacts the bumper,
- **Then** a radial impulse is applied and the work store debited accordingly.
- **And** an uncharged/unpaid bumper imparts zero boost (regular bounce only).
- **And** `engine/BumperWorkResource.cs` ad-hoc work logic is deleted in favor of generic finite work store declarations.
- **And** `tools/e2e/cat-015a.test.ts` passes 100% in Chrome serially.

### Story 6.3: Bumper Multi-Angle Contacts & Advanced Bumper Puzzles (CAT-015b)
*Status: Backlog*

As a player,  
I want Bumpers to handle glancing and multi-angle collisions accurately,  
So that levels `bumper_depth` and `wall_and_bumper` can be solved.

**Acceptance Criteria:**
- **Given** levels `bumper_depth` and `wall_and_bumper`,
- **When** played in Chrome through real UI controls,
- **Then** both levels are solvable with valid route trajectories.
- **And** glancing impacts do NOT falsely trigger radial boost.
- **And** `tools/e2e/cat-015b.test.ts` passes 100% in Chrome serially.

### Story 6.4: Prismatic Slider & Unified TGS Soft Spring Constraint (CAT-062a)
*Status: Backlog*

As a player,  
I want an uncharged Springboard to compress and rebound elastically,  
So that spring dynamics follow real physical spring-damper laws.

**Acceptance Criteria:**
- **Given** a Springboard placed under a dropped ball,
- **When** impact occurs,
- **Then** the 1D prismatic slider compresses within limits ($-0.25\text{ m}$ to $0\text{ m}$) and rebounds elastically.
- **And** hardcoded spring launch shims are deleted in favor of unified compliant soft constraints.
- **And** `tools/e2e/cat-062a.test.ts` passes 100% in Chrome serially.

### Story 6.5: Preload Energy Store & Springboard Launch (CAT-062b)
*Status: Done — published commit 9107148; independent terminal scoped Pass*

As a player,  
I want to preload a Springboard to launch objects across the workbench,  
So that contraptions can solve `spring_forward`.

**Acceptance Criteria:**
- **Given** the `spring_forward` level,
- **When** the preloaded springboard releases on Run start,
- **Then** the ball is launched across the workbench into the target receiver.
- **And** an uncharged springboard fails to launch the ball.
- **And** no legacy latch implementation remains: `engine/LatchedSpringStore.cs` is absent from sole baseline `a6c914e` and the current tree, so there is no such file to delete. Deliver the intended declarative finite preload energy store without restoring or inventing a legacy latch.
- **And** `tools/e2e/cat-062b.test.ts` passes 100% in Chrome serially.

### Story 6.5b: Dynamic Puzzle Part Discovery & Extensible PartId Architecture
*Status: In-Progress*

As an engine architect and player,  
I want puzzle parts to be discovered dynamically from catalogue data and identified by extensible `PartId`s,  
So that new parts can be authored as declarative resources without modifying closed enums or engine switch statements.

**Acceptance Criteria:**
- **Given** `res://parts/catalog/`,
- **When** `PartRegistry.Discover()` executes at startup via `DirAccess.GetFilesAt`,
- **Then** all valid catalogue `.tres` files are discovered and registered dynamically without hardcoded file path lists.
- **And** parts are identified by strongly typed extensible `PartId` record structs wrapping canonical strings.
- **And** closed enum `WorkshopPartKind` is eliminated from active part registration, creation, inventory, and machine parts.
- **And** `PartRegistry.Create(PartId)` instantiates parts from `definition.Scene` and calls `Configure(definition)` without switch statements.
- **And** Free Workshop mode palette populates dynamically from discovered `PartRegistry.Definitions` with unlimited allowances.
- **And** all 796 native solution tests pass with zero warnings (`dotnet test CuriousContraptions.slnx`).
- **And** existing cumulative Chrome Playwright E2E suites pass 100% serially.

### Story 6.5c: glTF Asset-Driven Geometry & Collider Extraction (ENGINE-CORE-2d)
*Status: Ready-for-Dev*

*Depends on: Story 6.5b (extensible PartId discovery).*

As an engine developer,  
I want `WorkshopPhysicsCompiler` to extract collision shapes and socket anchors directly from glTF 2.0 (`.glb`) scene hierarchies,  
So that physical geometry and connection ports are defined purely by 3D artist assets without hardcoded C# tables.

**Acceptance Criteria:**
- **Given** an instantiated `.glb` scene hierarchy with collision proxy nodes (`col_box`, `col_sphere`, `col_capsule`, `col_cylinder`, `col_hull` / `-convcol`) or `KHR_physics_rigid_bodies`,
- **When** `WorkshopPhysicsCompiler` compiles the physics graph at Run start,
- **Then** it traverses the scene graph and extracts `ColliderDeclaration` records generically based solely on node transforms and geometry without element-specific branching.
- **And** 3D socket anchor points for electrical, activation, and mechanical connections are resolved dynamically from named empty locator nodes (`Loc_Port_<Id>`) matching declared `PortDefinition.Id` values.
- **And** secondary child branches in the `.glb` share articulated transforms automatically.
- **And** placed elements with asset-driven colliders interact, collide, and rest stably in Chrome without hardcoded C# collider tables; zero element identifiers in compiler dispatch.
- **And** the visual mesh and collider mesh are strictly separate entities: visual meshes are dedicated to presentation rendering, while collision proxies are separate nodes whose visual rendering is suppressed; physics colliders are never extracted directly from visual meshes.
- **And** `anvil check --changed` passes with 0 warnings; exact authoring Reset and Save/Load persistence roundtrip in Chrome.

### Story 6.5d: Existing Implementation Conversion — Core Ball & Obstacle Primitives (CAT-001, CAT-014, CAT-064, CAT-003, CAT-066, CAT-054, CAT-004)
*Status: Backlog*

*Depends on: Stories 6.5b and 6.5c.*

As a player and developer,  
I want the existing balls, walls, ramps, and receiver converted from procedural C# code to declarative glTF 2.0 (`.glb`) assets,  
So that core simulation primitives follow the data-driven architecture with zero backwards compatibility shims.

**Acceptance Criteria:**
- **Given** existing ball and obstacle elements (Basketball CAT-001, Bowling Ball CAT-014, Tennis Ball CAT-064, Floating Balloon CAT-003, Wall CAT-066, Ramp CAT-054, Receiver CAT-004),
- **When** they are refactored to the new design,
- **Then** each element's visual mesh and collision geometry are authored in a self-contained `.glb` binary asset in `parts/models/` with standard PBR materials and co-located `col_*` proxy nodes.
- **And** each element is defined by a declarative `.tres` resource in `parts/catalog/` and instantiated via generic `MachinePart`.
- **And** bespoke per-part C# classes (`BallPart.cs`, `WallPart.cs`, `RampPart.cs`, `BasketPart.cs`) and hardcoded dimension resources (`WallDimensionsResource.cs`, `RampDimensionsResource.cs`, `ReceiverGeometry.cs`) are deleted.
- **And** strictly zero backwards-compatibility shims, dual loaders, or fallback branches are introduced.
- **And** all existing ball, ramp, wall, and receiver levels (`first_principles`, `two_ball_lead`, etc.) simulate identically and solve in Chrome.
- **And** dedicated E2E suites (`cat-001`, `cat-014`, `cat-064`, `cat-003`, `cat-066`, `cat-054`, `cat-004`) pass 100% in Chrome serially.

### Story 6.5e: Existing Implementation Conversion — Interactive Mechanisms & Dynamic Fixtures (CAT-015, CAT-023, CAT-062, CAT-063, CAT-035, CAT-022, CAT-005)
*Status: Backlog*

*Depends on: Story 6.5d.*

As a player and developer,  
I want all existing interactive mechanisms, sensors, and timers converted to declarative glTF 2.0 (`.glb`) assets,  
So that all gameplay fixtures use data-driven 3D assets, port locators, and generic `MachinePart` entities with zero backwards compatibility.

**Acceptance Criteria:**
- **Given** existing interactive fixtures (Pinball Bumper CAT-015, Domino CAT-023, Springboard CAT-062, Impact Switch CAT-063, Signal Lamp CAT-035, Delay Box CAT-022, Battery CAT-005),
- **When** they are refactored to the new design,
- **Then** each fixture's visual meshes, collision proxies (`col_*`), and socket locators (`Loc_Port_*`) are authored in a self-contained `.glb` asset in `parts/models/`.
- **And** moving sub-assemblies (e.g. Springboard moving plate, Switch button) are authored as child branches in the `.glb` scene.
- **And** each fixture is defined by a `.tres` resource in `parts/catalog/` and instantiated via generic `MachinePart`.
- **And** bespoke per-part C# classes (`BumperPart.cs`, `DominoPart.cs`, `SpringboardPart.cs`, `SwitchPart.cs`, `LampPart.cs`, `DelayPart.cs`, `BatteryPart.cs`) and dimension/resource files (`SpringboardResource.cs`, `ContactWorkResource.cs`, `DelayDurationResource.cs`, `ElectricalSourceResource.cs`) are deleted.
- **And** strictly zero backwards-compatibility shims, adapters, or fallback loaders are introduced.
- **And** all existing puzzle levels (`spring_forward`, `domino_effect`, `delayed_signal`, etc.) simulate and solve identically in Chrome.
- **And** dedicated E2E suites (`cat-015`, `cat-023`, `cat-062`, `cat-063`, `cat-035`, `cat-022`, `cat-005`) pass 100% in Chrome serially.

### Story 6.5f: Zero-Remnant Procedural Geometry, Per-Part Classes, and WorkshopPartKind Purge
*Status: Backlog*

*Depends on: Stories 6.5d and 6.5e.*

As an engine maintainer,  
I want all procedural mesh generation in `PartArt.cs`, closed enums in `WorkshopPartKind`, and dead per-part classes purged,  
So that Curious Contraptions contains strictly zero procedural mesh building routines or legacy part enums in the active tree.

**Acceptance Criteria:**
- **Given** the completed conversion of all 14 existing elements to declarative `.glb` + `PartId` assets,
- **When** `Story 6.5f` is applied,
- **Then** `engine/PartArt.cs` and `engine/gpu/WorkshopPartKind.cs` are permanently deleted from the codebase.
- **And** all remaining switch statements or branches based on part kind in `ui/`, `engine/`, `parts/`, and tests are eradicated.
- **And** codebase grep confirms 0 occurrences of `PartArt` or `WorkshopPartKind` across all active C# source files.
- **And** `anvil check --changed` passes with 0 warnings.
- **And** `dotnet test CuriousContraptions.slnx` passes 100% (all unit tests).
- **And** all cumulative Playwright E2E suites execute serially in Chrome and pass 100%.
- **And** strictly zero backwards compatibility shims, adapters, or fallback wrappers remain.

### Story 6.6: Straight Pipe Compound Cylindrical Collider (CAT-048a)
*Status: Backlog*

*Depends on: Story 6.5f (zero-remnant purge of PartArt and legacy part scripts; CAT-048 is authored purely via declarative .glb asset and .tres catalog definition without per-part C# code or enum modifications).*

As a player,  
I want balls to roll smoothly through straight pipes without snagging,  
So that enclosed ball transport operates reliably.

**Acceptance Criteria:**
- **Given** a straight pipe collider in the Workshop,
- **When** an admitted ball rolls into the pipe mouth,
- **Then** the ball travels through and exits without snagging or falling through walls.
- **And** an oversized ball stops at the pipe entrance without clipping.
- **And** `tools/e2e/cat-048a.test.ts` passes 100% in Chrome serially.

### Story 6.7: Hollow Signed-Distance Torus Rim Cap Pipe Collider (CAT-048b)
*Status: Backlog*

*Depends on: Story 6.6 (straight pipe collider). Moved from Story 6.8 (owner re-ordering, 9 Oct 2026), because the Ball Detector now follows the pipe stories.*

As a player,  
I want curved and complex pipe paths to guide balls reliably,  
So that level `clear_pipe` can be solved.

**Acceptance Criteria:**
- **Given** level `clear_pipe` played in Chrome,
- **When** the ball enters the mouth at high speed and edge angles,
- **Then** the ball negotiates the pipe geometry and reaches the receiver.
- **And** all legacy annular-specific kernel code, `AnnularFeature` enums, and `reference/pipe/` are deleted.
  - Historical reference and annular cleanup clauses discharged by Stories 7.1 and 7.4 (10 Oct 2026); the fresh implementation must preserve the generic shared-engine/no-annular-identifier rule.
- **And** `tools/e2e/cat-048b.test.ts` passes 100% in Chrome serially.

### Story 6.8: Generic Aperture Sensor with Directional Rearm (CAT-002)
*Status: Backlog*

*Depends on: Stories 6.6 and 6.7 (the detector body is a hollow collar that reuses the pipe's annular collider, rim caps and tube mouths; CAT-002 §3). Moved from Story 6.6 (owner re-ordering, 9 Oct 2026).*

As a player,  
I want a Ball Detector to emit a signal when a ball passes through its aperture,  
So that crossing events can trigger downstream contraption components.

**Acceptance Criteria:**
- **Given** a Ball Detector wired to a Signal lamp,
- **When** a ball passes forward through the aperture,
- **Then** the lamp illuminates once.
- **And** reverse or glancing outside crossings do NOT trigger the lamp.
- **And** a second forward crossing after complete clearance triggers again.
- **And** `tools/e2e/cat-002.test.ts` passes 100% in Chrome serially.

### Story 6.9: 45-Degree Pipe Bend Hollow Torus Segment (CAT-049)
*Status: Backlog*

As a player,  
I want a 45-degree hollow pipe bend that joins straight pipes and other bends,  
So that level `gentle_bend` can be solved by turning a falling ball through a gentle curve.

**Acceptance Criteria:**
- **Given** level `gentle_bend` played in Chrome with the 45-degree bend placed through real UI controls,
- **When** the ball enters the upper mouth and travels the 1.3-unit bore around the fixed-radius curve,
- **Then** it exits the lower mouth without added speed and reaches the receiver; straight-to-bend and bend-to-bend rotated passages succeed through typed mouth snapping.
- **And** rim, oversize, sidewall and missed-connection controls deliver nothing, and an actual valid Precise alternative route is proven.
- **And** the authored assistance controls depend on physical placement nudging, which is not scheduled (owner decision; `docs/planning/invest/vertical-delivery.md` reports physical nudging unsupported in the current playable mode); they are proven when nudging lands, and CAT-049 stays open until then.
- **And** the bend keeps its own icon, rails and collars; no 90-degree proof substitutes for it.
- **And** the element is built from its declaration spec `docs/planning/elements/CAT-049-pipe_bend_45.md`, with exact Reset and Save/Load.
- **And** `tools/e2e/cat-049.test.ts` passes 100% in Chrome serially.

### Story 6.10: 90-Degree Pipe Bend and Joined Pipe Route (CAT-050)
*Status: Backlog*

As a player,  
I want a 90-degree hollow pipe bend that joins a straight pipe into one continuous route,  
So that levels `quarter_bend` and `joined_pipe` can be solved.

**Acceptance Criteria:**
- **Given** levels `quarter_bend` and `joined_pipe` played in Chrome through real UI controls,
- **When** the ball falls into the bend's upper mouth, or through a resized straight pipe snapped to the fixed bend,
- **Then** it follows the continuous turned passage and reaches the receiver.
- **And** straight/bend joins succeed, and rotated, oversize, rim and missing-connection controls deliver nothing.
- **And** the declared depth-error result (Forgiving and Balanced succeed where Precise fails) depends on physical placement nudging, which is not scheduled (owner decision; `docs/planning/invest/vertical-delivery.md` reports physical nudging unsupported in the current playable mode); it is proven when nudging lands, and CAT-050 stays open until then.
- **And** the bend shares the common bore and typed mouth geometry with matching collars, rails and icon; no 45-degree proof stands in for it.
- **And** the element is built from its declaration spec `docs/planning/elements/CAT-050-pipe_bend_90.md`, with exact Reset and Save/Load.
- **And** `tools/e2e/cat-050.test.ts` passes 100% in Chrome serially.

### Story 6.11: Funnel Hollow Frustum Inlet to the Standard Bore (CAT-030)
*Status: Backlog*

As a player,  
I want a wide clear funnel that narrows into the standard tube bore,  
So that loosely aimed balls are gathered into a pipe route by gravity and contact alone.

**Acceptance Criteria:**
- **Given** a funnel placed through real UI controls with its narrow outlet snapped to a straight pipe or bend,
- **When** a ball drops into the 2.6-unit mouth, including off-centre, fast and rotated entries,
- **Then** the physically hollow frustum walls guide it to the 1.3-unit outlet and on into the joined tube, with no attraction or scripted transport.
- **And** inlet, wall, outlet and rim contacts, oversize payloads and straight/bend joins are qualified, an outside drop is blocked by the solid shell, and mesh and contact geometry match.
- **And** two integration proofs are owed, and CAT-030 stays open until they pass: the solid shell blocks air at Story 12.2 (Electric Fan), and the clear wall transmits light except at the opaque collar at Story 13.1 (Flashlight and Laser).
- **And** the element is built from its declaration spec `docs/planning/elements/CAT-030-funnel.md`, with exact Reset and Save/Load.
- **And** `tools/e2e/cat-030.test.ts` passes 100% in Chrome serially.

---

## Epic 7: Legacy Code Purge & Zero-Remnant Clean Architecture

**Goal:** Eradicate all dead reference tarballs, ad-hoc diagnostic probe tools, uncompiled test suites, and superseded CPU physics implementations across four disciplined deletion stages (LEGACY-0a through 0d).

**Entry gate (Stories 7.1–7.4):** no file is deleted until [legacy-disposition.md](../../docs/planning/elements/legacy-disposition.md) has a row for it. The ledger is written by Story 7.0 ([spec](../implementation-artifacts/spec-7-0-element-implementation-readiness.md)).

### Story 7.1: Reference & Historical Archive Purge (LEGACY-0a)
*Status: Done — local commit aaac712; independent committed-tree Pass*

As an engine maintainer,  
I want obsolete tarballs and benchmark dumps in `reference/` purged,  
So that repository bloat is eliminated and active docs are clearly isolated.

**Acceptance Criteria:**
- **Given** the `reference/` directory containing dead tarballs and old candidate archives,
- **When** `LEGACY-0a` is applied,
- **Then** ~4,000 obsolete files are deleted.
- **And** `reference/` retains only active Markdown documentation.
- **And** the active solution compiles and passes all unit and Playwright tests cleanly.

### Story 7.2: Diagnostics & Legacy Probe Tools Purge (LEGACY-0b)
*Status: Done — local commit 90db56b; independent committed-tree Pass*

As an engine maintainer,  
I want obsolete probe tools and ad-hoc harnesses in `tools/` purged,  
So that the codebase contains only active build, lint, and test tools.

**Acceptance Criteria:**
- **Given** `tools/p0-*`, `tools/P0-007-*`, and dead diagnostic folders,
- **When** `LEGACY-0b` is applied,
- **Then** ~2,000 obsolete files are purged.
- **And** remaining tools compile with zero warnings under Anvil.

### Story 7.3: Uncompiled Legacy Test Purge (LEGACY-0c)
*Status: Review — 417 obsolete files removed; all active compile inputs preserved; 643 solution tests passed; scoped commit verification pending*

As an engine maintainer,  
I want uncompiled, commented-out, or obsolete test files in `CuriousContraptions.tests/` purged,  
So that 100% of test files in the project are actively compiled and executed.

**Acceptance Criteria:**
- **Given** dead test files and obsolete native fixtures,
- **When** `LEGACY-0c` is applied,
- **Then** all obsolete test files are deleted.
- **And** `dotnet test CuriousContraptions.slnx` runs with 100% pass rate (0 failures).

### Story 7.4: Legacy CPU Physics, Procedural PartArt & Unshipped Engine Purge (LEGACY-0d)
*Status: Review — all 655 legacy paths removed; current source/resource classification and applicable 45-case Chrome proof complete; procedural PartArt and hardcoded collider tables scheduled for permanent deletion; final scoped commit review pending*

As an engine maintainer,  
I want all superseded CPU physics files in `engine/physics/`, procedural mesh routines in `engine/PartArt.cs`, and dead classes in `parts/` deleted,  
So that Curious Contraptions contains zero legacy physics code or procedural geometry generation in the active tree.

**Acceptance Criteria:**
- **Given** all remaining legacy physics files in `engine/physics/` and `engine/presentation/`,
- **When** `LEGACY-0d` is applied,
- **Then** all superseded physics classes are permanently purged.
- **And** procedural mesh generation in `engine/PartArt.cs`, hardcoded C# collider tables, and dead per-part classes in `parts/` are permanently deleted.
- **And** all elements are defined exclusively via declarative `.glb` assets and `.tres` catalog resources.
- **And** `git ls-files` contains only compiled, shipped, active content, docs, and CI files.
- **And** P0-030 and P0-031 cleanup audits are closed with zero warnings.
- **And** cumulative Playwright E2E suites pass 100% serially in Chrome.
- **And** strictly zero backwards-compatibility shims, legacy flags, or dual-path adapters remain.

---

## Epic 8: Supplied Electrical Power

**Goal:** Implement supplied direct-current electrical networks and battery power sources as declarative network graphs with zero continuous circuit simulation overhead. The Powered Gate is the first supplied consumer (owner decision, 9 Oct 2026).

### Story 8.1: Battery DC Power Source & Network Graph (CAT-005)
*Status: Backlog*

*Depends on: Epic 7 (legacy purge). Moved from Story 9.1 (owner decision, 9 Oct 2026: electrical first, before the activation stories). Its first electrical consumer is the Powered Gate (Story 8.2); the Electric Motor named in its acceptance arrives in Story 11.1 (an acceptance conflict for case-by-case owner review). Its supply-loss control (CAT-005 N19, which uses the Latch supply-switching fixture) is a deferred integration, owed once the Latch (Story 9.4) lands; it is scheduled as Story 9.4c (coordinator, 18 Oct 2026).*

As a player,  
I want Batteries to supply power to connected electrical elements,  
So that powered mechanisms like motors and lamps can operate.

**Acceptance Criteria:**
- **Given** a Battery connected to an Electric Motor via an electrical cable,
- **When** the simulation runs,
- **Then** the motor receives continuous 12V supply and spins its axle.
- **And** disconnecting the cable stops the motor immediately.
- **And** `tools/e2e/cat-005.test.ts` passes 100% in Chrome serially.

### Story 8.2: Powered Gate Passageway Barrier (CAT-051)
*Status: Backlog*

*Depends on: Stories 6.4 (prismatic slider), 6.6–6.7 (pipe mouths and routes) and 8.1 (supply). Moved from Story 11.4 (owner decision, 9 Oct 2026: first electrical consumer). It now precedes the Linear Pusher (Story 11.3), so it first builds the bounded slider drive that the pusher reuses. Its "supply interruption mid-stroke" control (CAT-051) is a deferred integration, owed once the Latch fixture (Story 9.4) lands; it is scheduled as Story 9.4c (coordinator, 18 Oct 2026).*

As a player,  
I want a Powered Gate to open when triggered,  
So that balls previously blocked can pass through unobstructed.

**Acceptance Criteria:**
- **Given** a closed Powered Gate blocking a ball in a ramp,
- **When** an activation pulse is received,
- **Then** the gate opens fully, allowing the ball to roll past.
- **And** `tools/e2e/cat-051.test.ts` passes 100% in Chrome serially.

---

## Epic 9: Activation, Timing & Electrical Logic

**Goal:** Deliver the full suite of sensor and discrete logic elements (Pressure Plate, Pulse Counter, System Clock, State Latch, Hold Timer) evaluating cleanly at substep endpoints with exact tick accumulation, then the electrical Boolean logic gates (NAND, NOR, OR, XOR, Both) as declarative network graphs on the Epic 8 supply.

### Story 9.1: Pressure Plate Surface Contact Load Sensor (CAT-052)
*Status: Backlog*

*Depends on: Stories 8.1 (supplied contact) and 8.2 (supplied consumer). Moved from Story 8.1 (the Battery and Powered Gate now precede this epic).*

As a player,  
I want to use a Pressure Plate that activates only when a body rests upon it,  
So that weight-sensitive mechanisms and sustained gates can be triggered.

**Acceptance Criteria:**
- **Given** a Pressure Plate placed on the workbench wired to a Signal lamp,
- **When** a ball or domino rests on the plate with mass $> 0.2\text{ kg}$,
- **Then** the plate activates and the lamp lights.
- **And** when the body rolls off or is lifted, the plate immediately deactivates.
- **And** light bouncing impacts without resting weight do NOT produce sustained activation.
- **And** `tools/e2e/cat-052.test.ts` passes 100% in Chrome serially.

### Story 9.2: Pulse Counter Event Accumulator (CAT-020)
*Status: Backlog*

*Depends on: Stories 6.8 (Ball Detector, one event per crossing), 8.1 and 8.2. Moved from Story 8.2.*

As a player,  
I want a Pulse Counter to emit a signal after receiving $N$ pulses,  
So that multi-step contraption sequences can be orchestrated.

**Acceptance Criteria:**
- **Given** a Pulse Counter configured with target count $N = 3$,
- **When** three distinct balls trigger its input detector,
- **Then** the counter emits an output pulse on the third event.
- **And** two pulses do NOT emit an output (negative control).
- **And** clicking Reset clears the accumulated count back to zero.
- **And** `tools/e2e/cat-020.test.ts` passes 100% in Chrome serially.

### Story 9.2b: Modular Element Package Isolation, Pure C# WASM Solver Consolidation & Multi-Consumer Event Ringbuffer (ENGINE-CORE-2e)
*Status: Backlog*

*Depends on: Story 9.2 (Pulse Counter completion).*

As an engine architect and contraption engineer,  
I want strict compile-time project isolation between puzzle elements and the core engine, a pure C# WebAssembly simulation core that eliminates custom JavaScript in the worker, and a multi-consumer broadcast event ringbuffer,  
So that the core engine and solver remain completely data-driven with strictly zero element knowledge, zero duplicated solver code, and scalable inter-domain streaming.

**Acceptance Criteria:**
- **Given** the Abstractions package layer (`CuriousContraptions.Abstractions` general package, plus domain-specific abstractions packages `Mechanical`, `Electrical`, `Fluid`, and `Signal`),
- **When** Story 9.2b is applied,
- **Then** every puzzle element is isolated into its own dedicated project (e.g. `elements/*`), referencing ONLY the Abstractions packages and strictly zero references to the Core Engine or sibling elements.
- **And** the Core Engine (`CuriousContraptions.Engine`, `CuriousContraptions.Simulation`) contains strictly zero compile-time references, zero hardcoded `PartId` constants, and zero assets (icons, i18n localization, scenes, cosmetic scripts) for any puzzle element.
- **And** a build-time packaging pipeline aggregates element metadata, assets, icons, and i18n into a unified distribution bundle dynamically loaded by the engine at runtime.
- **And** 100% of physical simulation, numerical integration, broadphase AABB BVH, analytical collision manifolds, TGS constraint solving, electrical Modified Nodal Analysis (MNA), and discrete activation logic is authored in C# and compiled to WebAssembly (`wasm-simd128`).
- **And** `worker.js` is stripped of all physics, collision, and domain logic, reduced to an un-opinionated ~50–80 line platform shell that only boots dotnet, transfers `postMessage` envelopes, and exposes `SharedArrayBuffer` pointers.
- **And** continuous simulation data updates stream via a zero-copy lock-free pose ringbuffer, and discrete simulation occurrences stream via a lock-free multi-consumer Event Ringbuffer supporting independent read cursors and consumer-side predicate filtering.
- **And** all project boundaries, inter-thread communication, and element integrations satisfy the Balanced Coupling model (coupling.dev) with zero intrusive or functional coupling across project/worker boundaries.
- **And** `anvil check` on all modified/new files reports 0 errors and 0 warnings.
- **And** `dotnet test CuriousContraptions.slnx` passes 100% across all unit test suites.
- **And** all existing Playwright E2E suites (`cat-052`, `cat-051`, `cat-050`, `cat-049`, `cat-048b`, `cat-048a`, `cat-002`, `cat-030`, `cat-020`) pass 100% in Chrome serially.

### Story 9.3: System Clock Periodic Pulse Generator (CAT-017)
*Status: Backlog*

*Depends on: Stories 8.1 (power to run), 8.2 and 9.2 (the Counter observes repeated pulses). Moved from Story 8.3.*

As a player,  
I want a System Clock to emit pulses at regular intervals,  
So that rhythmic and timed contraption mechanisms can operate.

**Acceptance Criteria:**
- **Given** a System Clock configured for a 1.0 s period,
- **When** the simulation runs for 5.0 seconds,
- **Then** exactly 5 discrete activation pulses are emitted at 1-second intervals.
- **And** pausing simulation halts clock emission; resetting restores clock phase to 0.
- **And** `tools/e2e/cat-017.test.ts` passes 100% in Chrome serially.

### Story 9.4: State Latch Bistable Memory (CAT-037)
*Status: Done*

*Depends on: Stories 8.1 and 8.2 (supplied contact and consumer). Moved from Story 8.4.*

*Scope added 18 Oct 2026 (owner decisions; Open scheduling item g):*
- *Review-9-4 F6 is fixed here: the switched-supply route is compiled at Run into the Physics graph, which deletes the per-tick `Program.SwitchedSupplies`/`ElectricalState` walk and the `SwitchedSupplyRoutes` graph walk in `engine/gpu/WorkshopLatch.cs`.*
- *The latch gains an ActivationOut socket (CAT-037 open question 1).*

As a player,  
I want a State Latch to hold memory between Set and Reset pulses,  
So that machine state can toggle and persist across dynamic events.

**Acceptance Criteria:**
- **Given** a State Latch with separate Set and Reset inputs,
- **When** a Set pulse arrives,
- **Then** the latch output transitions to high and remains high indefinitely.
- **And** when a Reset pulse arrives, output transitions to low.
- **And** simultaneous Set and Reset pulses resolve in favor of Reset (Reset dominance).
- **And** `tools/e2e/cat-037.test.ts` passes 100% in Chrome serially.

### Story 9.4b: Cumulative Chrome Regression Repair
*Status: Backlog*

*Depends on: Story 9.4. New story (coordinator, 18 Oct 2026): during Story 9.4 the full serial run of every `tools/e2e/*.test.ts` suite found failures that predate 9.4. Stories 6.9–9.3 ran only a subset of suites. Classification and first failing assertions are in `_bmad-output/implementation-artifacts/spec-9-4-state-latch.md` (item 3 table).*

As a player,  
I want every previously delivered element to keep working as later elements are added,  
So that the catalogue I build with stays trustworthy.

**Acceptance Criteria:**
- **Given** the CAT-015 Bumper energy ledger, **When** a later empty contact or a paid sidekick contact occurs, **Then** no energy is debited that was not earned (f32 residual leakage removed at its source, not by loosening assertions), and `cat-015a`, `cat-015a-sidekick` and `cat-015b` pass 100%.
- **And** `cat-062b`'s Precise Receiver guide expectation matches the declared Receiver support geometry and save codec: either the engine or the stale test expectation is corrected, with the source acceptance cited.
- **And** `cat-062b`'s fifth test, which fails at `tools/e2e/cat-062b.test.ts:257` (`goal_body` 1 ≠ replacement 4), is fixed at its source. For Captured goals the FlatBuffers encoder writes `Goal.Body` as the ball, not the goal body (`engine/gpu/WorkshopWireFlatBuffers.cs:640`, goal encoding). The fix makes the encoder write the declared goal body, with a codec unit test for a Captured goal, and the suite passes (review-9-4 F8).
- **And** the `cat-023b` worker transport / "clock mapping recovery exhausted" failure is root-caused and fixed so the suite passes in three consecutive serial runs. The fix also covers the other two failures review-9-4 F8 recorded: test 1's empty lamp history at `tools/e2e/cat-023b.test.ts:182` (0 samples after the lamp lit and the goal was solved), and "Pose slot must be readable".
- **And** if the `cat-023b` root cause lies in the raw `reliableStalled` `postMessage` flag path (architecture audit C2: `CuriousContraptions.web/wwwroot/workshop-client.js:78-86` → `worker.js:315` → `Program.SetReliableStall` at `CuriousContraptions.Simulation/Program.cs:495`, which stops the run at `:428-431`), it is fixed by making the stall a typed command with a sequence and a receipt. Story 9A.3 then deletes only what remains of that path ([audit](architecture-conformance-audit-2026-10-18.md), C2).
- **And** `cat-005` either runs against the production preview (port 8060) and passes, or its diagnostic-only status is made explicit in the suite and the README with the production checks it duplicates named.
- **And** the IL2125 trim warning from the `Google.FlatBuffers` package (Story 8.1) is resolved without suppression, so `dotnet build CuriousContraptions.slnx` reports 0 warnings. This warning has one home, this story; Story 9A.2 (single FlatBuffers codec) builds on the warning-free state.
- **And** fixes here keep to the current channels. The `CCGPU` console acknowledgement and the `globalThis.Workshop*` hooks that `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062b`, `cat-005` and `cat-023b` read stay as they are; Story 9A.24 migrates every suite to the typed observation port.
- **And** the retained JS-worker harness `tools/workshop-rigid-body.test.mjs` (50 of 61 tests fail at module load since `worker.js` stopped exporting its imports on 17 Oct 2026; review-9-4 R2-F2, `tools/workshop-rigid-body.test.mjs:42,858`) is either repaired against the current C# WASM engine boundary or deleted together with its references in `tools/e2e/README.md:20` and `docs/simulation-presentation-bridge.md:105`, with the coverage it uniquely provided named and moved to native or Chrome tests.
- **And** every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run, recorded as the new cumulative baseline that every Epic 9A story must keep.

### Story 9.4c: Latch-Fixture Deferred Integrations (CAT-005 N19, CAT-051 Supply Interruption Mid-Stroke)
*Status: Backlog*

*Depends on: Story 9.4 (the State Latch and its switched supply contact), Story 9.4b (the cumulative baseline), and Stories 8.1 and 8.2. New story (coordinator, 18 Oct 2026): review-9-4 D1 found both latch-dependent integrations due but unscheduled. Sources: [CAT-005](../../docs/planning/elements/CAT-005-battery.md) §4 N17 and N19 and §5 "Integrations"; [CAT-051](../../docs/planning/elements/CAT-051-powered_gate.md) §2 (return spring and drive law), §5 "Boundaries" (supply interruption mid-stroke; fact 14, closing stops at a ball without crushing it); [CAT-037](../../docs/planning/elements/CAT-037-latch.md) T21 (the supply-switching fixture recipe). The coordinator placed it before Epic 9A (18 Oct 2026). The owner's timing decision names Stories 9.4 and 9.4b; this story adds no new element, and its two suites guard Epic 9A's drive and supply refactors.*

*Definitions decided by the coordinator under owner delegation (18 Oct 2026, playability):*
- *CAT-051 open question 1. The return spring (14 N/m at rest 0, damping 4 N·s/m, acting whether or not the gate is powered) is the gate's fail-closed law. When supply drops, the drive stops and the spring closes the blade. Rationale: a gate that falls shut when power is lost is predictable and easy to teach.*
- *CAT-005 open question 6. Wires are not edited during Run. "Disconnecting the cable stops the motor" in Story 8.1 means supply loss through a switched contact (here the latch), or a wire edit between Runs. Rationale: Run is for watching the machine; building happens at the bench; supply changes are activation commands, never live topology edits (N19).*

As a player,  
I want to cut and restore a gate's power with switches during a run, even while the gate is moving,  
So that I can build timed doors that fail safe.

**Acceptance Criteria:**
- **Given** this machine, built through real UI controls with no setters or numeric placement:
  - Battery `Supply` → State Latch `PowerIn`;
  - Latch `Supply` → Powered Gate `PowerIn`;
  - Impact Switch A → Latch `SetIn`;
  - Impact Switch B → Latch `ResetIn`;
  - a ball upstream of the gate;
  
  **When** a ball strikes Switch A, **Then** the latch sets and the gate becomes supplied and opens. The supply change settles two ticks after the activation, and the committed construction shows no topology change (N19).
- **And** when a ball strikes Switch B with the gate fully open, the supply drops: the drive stops, every downstream input is cleared (N17), and the blade closes under its return spring.
- **And** for supply interruption mid-stroke: **Given** the blade strictly between closed and the 1.55 m open limit (sampled from committed reads), **When** Switch B fires, **Then**:
  - within two ticks the blade stops gaining opening and then returns to closed;
  - a ball still upstream stays upstream;
  - a ball partly through the opening stops the closing blade without being crushed (fact 14).
- **And** controls:
  - without the Switch B strike the same run completes the opening;
  - with the Battery disabled the gate never opens, even after Set;
  - the latch's memory survives a supply off/on (CAT-037 T14).
- **And** the setup uses one battery and one gate. Independence across multiple supplies is proved in Story 9A.19.
- **And** Run/Reset restores latch Off, the blade closed and the authored battery state, and Save/Load preserves all five connections with their exact socket names.
- **And** `tools/e2e/cat-005-n19.test.ts` and `tools/e2e/cat-051-interrupt.test.ts` pass 100% in Chrome serially, and every `tools/e2e/*.test.ts` suite still passes in one serial run.

### Story 9.5: Hold Timer Pulse Stretcher (CAT-033)
*Status: Backlog*

*Depends on: Stories 8.1 and 8.2 (supplied contact and consumer), and Epic 9A (owner decision, 18 Oct 2026: architecture conformance runs after Stories 9.4 and 9.4b and before any new element; Story 9.4c precedes it by coordinator placement; Open scheduling item g). Moved from Story 8.5.*

As a player,  
I want a Hold Timer to keep an output energized for a configured duration after a momentary trigger,  
So that timed doors and temporary circuits can stay open.

**Acceptance Criteria:**
- **Given** a Hold Timer configured for 2.0 seconds,
- **When** a momentary impact pulse is received,
- **Then** the output activates immediately and remains active for exactly 2.0 simulated seconds before deactivating.
- **And** re-triggering during the active window does NOT extend the timeout.
- **And** `tools/e2e/cat-033.test.ts` passes 100% in Chrome serially.

### Story 9.5b: Impact Switch Electrical Mode (CAT-063 PowerIn → Supply)
*Status: Backlog*

*Depends on: Stories 8.1 (supply and network graph) and 8.2 (a supplied consumer). New story (owner decision, 18 Oct 2026; Open scheduling item b), placed before the Both Gate (9.6) so every electrical gate story can use real input switches. Source: [CAT-063](../../docs/planning/elements/CAT-063-switch.md) §4 facts 1–5 and §5.*

As a player,  
I want an Impact Switch to pass power from its PowerIn socket to its Supply socket once a qualifying impact latches it,  
So that a physical event can switch an electrical circuit.

**Acceptance Criteria:**
- **Given** a Battery wired to the Impact Switch `PowerIn` and the switch `Supply` wired to a Powered Gate, built through real UI controls,
- **When** a ball strikes the button at or above the trigger threshold,
- **Then** the switch latches once and the Powered Gate becomes supplied through the switch.
- **And** a missed or below-threshold ball, a missing supply, a disabled supply, or a missing wire leaves the consumer unpowered.
- **And** the switch's existing ActivationOut behaviour is unchanged, and both domains can be used on one switch.
- **And** a closed contact still needs upstream supply; a loop of switches alone never supplies.
- **And** Run/Reset restores the unpressed button, the open contact and both domains exactly; connections persist through Save/Load.
- **And** `tools/e2e/cat-063-electrical.test.ts` passes 100% in Chrome serially.

### Story 9.6: Dual-Supply Both Gate (CAT-013)
*Status: Backlog*

*Depends on: Stories 8.1 (supply), 8.2 (supplied consumer) and 9.4 (the Latch supply fixture used by its boundary and integration checks; CAT-013 §5). Moved from Story 9.2 (coordinator decision, 9 Oct 2026: the owner's literal order puts the electrical gates after the activation stories).*

As a player,  
I want a Dual-Supply Both Gate to conduct power only when both inputs are energized,  
So that safety interlocks and dual-condition activations are possible.

**Acceptance Criteria:**
- **Given** a Both Gate connected to two separate Battery circuits and an output lamp,
- **When** both batteries are connected,
- **Then** the lamp illuminates.
- **And** disconnecting either battery extinguishes the lamp.
- **And** `tools/e2e/cat-013.test.ts` passes 100% in Chrome serially.

### Story 9.7: Electrical Logic Gates NAND & NOR (CAT-024, CAT-025)
*Status: Backlog*

*Depends on: Stories 8.1, 8.2, 9.5b (switch electrical mode for its input power switches; Open scheduling item b, decided 18 Oct 2026) and 9.6 (gate element and sockets). Moved from Story 9.3.*

As a player,  
I want Electrical NAND and NOR gates to evaluate Boolean conditions,  
So that complex electrical logic contraptions can be constructed.

**Acceptance Criteria:**
- **Given** Electrical NAND and NOR gates wired to input power switches,
- **When** all four input permutations (00, 01, 10, 11) are tested,
- **Then** output power states match standard Boolean truth tables with 100% accuracy.
- **And** `tools/e2e/cat-024-025.test.ts` passes 100% in Chrome serially.

### Story 9.8: Electrical Logic Gates OR & XOR (CAT-026, CAT-027)
*Status: Backlog*

*Depends on: Stories 8.1, 8.2 and 9.6 (gate element and sockets). Moved from Story 9.4.*

As a player,  
I want Electrical OR and XOR gates to route and differentiate electrical signals,  
So that branching circuits and toggle mechanisms can be built.

**Acceptance Criteria:**
- **Given** Electrical OR and XOR gates,
- **When** all input combinations are evaluated,
- **Then** XOR outputs high only when inputs differ (01, 10) and low when identical (00, 11).
- **And** OR outputs high whenever at least one input is high.
- **And** `tools/e2e/cat-026-027.test.ts` passes 100% in Chrome serially.

---

## Epic 9A: Sole Architecture Conformance (Project-per-Element, Command/Query, Event Disruptor)

**Goal:** Retire every parallel path so that the only design available in the tree is: one project per puzzle element, the bundle built from those projects as the single declaration source, typed commands and queries, and the event ring for discrete occurrences. After this epic, element-specific code outside `elements/` fails the build.

**Delivery order (owner decision, 18 Oct 2026; Open scheduling item g).** Epic 9A runs after Stories 9.4 and 9.4b (and Story 9.4c, which the coordinator placed first) and before Story 9.5 and every later new element. Its stories run in number order, 9A.0 to 9A.45. The section is placed after Epic 9 so that Epic 9's stories stay together; `sprint-status.yaml` and each "Depends on" line carry the delivery order.

**Evidence.** [Architecture conformance audit, 18 Oct 2026](architecture-conformance-audit-2026-10-18.md) (finding IDs A1–A19, B1–B8, C1–C5, D1–D4, G1–G10). Its line numbers are a snapshot of a tree that Story 9.4 was still editing, so every story names the symbol as well as the line. Where they disagree, the symbol wins.

**Rules for every 9A story.**
- The story names the legacy it deletes and deletes it in the same slice.
- No compatibility shim, fallback branch, alias, dual loader or automatic migration is permitted. An old save or wire version is rejected explicitly.
- Every existing element keeps working.
- Any lane the story rewrites lands as IEEE-754 f32; no story adds a `Half`.
- Review-9-4 F6 (audit A8: the per-tick `Program.SwitchedSupplies`/`ElectricalState` walk of the authoring graph, `SwitchedSupplyRoutes` in `engine/gpu/WorkshopLatch.cs`) is fixed inside Story 9.4 (owner decision, 18 Oct 2026), so no 9A story owns it. If any part of it remains when 9A starts, Story 9A.19 deletes that remainder.

### Story 9A.0: Architecture Authority Records the Sole Design
*Status: Backlog*

*Depends on: Story 9.4c. Audit: G1–G10. Documentation only; no runtime change.*

*Deletes:*
- *the stale claims in the untracked `docs/architecture/element-package-isolation.md` (G2): the nonexistent `CuriousContraptions.Engine`, the nested `elements/mechanical/ramp/` layout, the Optical/Pneumatic Abstractions, `IPartDefinition`/`IWorkshopInstance`/`PartId`/`GpuBodyId` "in Abstractions", the "~50–80 line" `worker.js`, and the "compiler guarantees" sentence;*
- *the `.tres` catalogue authority in `architecture.md` AD-11 (lines 145–152) and the source-map rows for `engine/PartId.cs` (line 289) and `CuriousContraptions.Simulation/wwwroot/pose-ring.js` (line 295);*
- *the `.tres` discovery and schema text in `docs/engine-contracts.md` ("Dynamic Part Discovery and Extensible PartId Contract" and "Declarative Part Definition Resource Schema Contract") and in `docs/general-engine-design.md` ("Dynamic Part Discovery and Extensible Part Identities" and "Conversion of existing implementations…");*
- *the `getF16`/`setF16` worker rows in the `docs/gpu-f32-physics.md` "f32 Migration Status" table (G7).*

As an engineer or agent starting any slice,  
I want one tracked document that states the only permitted architecture, and states separately what the tree does not yet meet,  
So that nobody builds on an end state that is described as if it already exists.

**Acceptance Criteria:**
- **Given** the tracked authoritative docs, **When** Story 9A.0 lands, **Then** `docs/architecture/element-package-isolation.md` is tracked and has two separate parts: **Target invariants** and **Current status**. Current status lists each open audit finding with its owning 9A story, and no unmet invariant is described as holding.
- **And** the target invariants state:
  1. **Project per element.** Each `elements/<Name>/` project is the only declaration of its element: metadata, typed parameter schema, ports, colliders, body and material, domain payloads, events, presentation bindings, icon and scene asset. It references only `CuriousContraptions.Abstractions` and the domain Abstractions it needs, never the engine or a sibling.
  2. **One declaration source.** `tools/BundleElements` discovers the packages at build time and writes one versioned bundle. The runtime loads only that bundle. There is no `.tres` catalogue, no per-element C# table and no `PartId` named constant.
  3. **Command/query.** Every mutation is a typed command through `WorkshopCommandRouter` with a sequence and a receipt. The query side is the published committed read model (`WorkshopRead`), the pose ring and typed queries (this resolves G5). Raw `postMessage` flags and console channels are forbidden.
  4. **Event disruptor.** One-shot discrete occurrences travel only through the single-producer, multi-consumer event ring, and each consumer has its own cursor. Level states (activation level, latch memory, timer phase, electrical level) are committed-state queries (this resolves G6).
  5. **Kernels.** Capability-named kernels and `ActivationNodeKind` values selected by declared data are valid. Element-named event kinds such as `CounterReached` and `LatchSet` are not (this resolves G9).
  6. **Forcing function.** The Story 9A.45 build gate enforces these invariants.
- **And** `architecture.md`:
  - AD-06's zero-identifier rule covers all non-element code, not only the solver core (G4);
  - AD-11 names the bundle;
  - a new AD-13, "Sole architecture", links the isolation doc;
  - the diagram text at lines 54 and 65 separates the ring from committed state;
  - the source map no longer cites `pose-ring.js`.
- **And** `docs/engine-contracts.md` and `docs/general-engine-design.md` describe bundle loading and the bundle schema contract (G10): versioned; f32 vectors as `[x, y, z]` and quaternions as `[x, y, z, w]`; mechanical, electrical and signal payloads; a typed parameter schema with key, unit, enum kind, f32 range and default or enum options; and an enum category.
- **And** the f32 table in `docs/gpu-f32-physics.md` names Story 9A.44 as the owner of every remaining binary16 lane.
- **And** `AGENTS.md` links the isolation doc as the sole design authority from its engineering requirements, in one line.
- **And** the Current status section records that review-9-2b's PASS for AC1, AC3, AC4 and AC5 is not supported by the tree (G8). The review file is historical evidence and is not edited.
- **And** the new `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs` asserts that:
  - `AGENTS.md` links the isolation doc;
  - none of `AGENTS.md`, `architecture.md`, `engine-contracts.md`, `general-engine-design.md` or `gpu-f32-physics.md` presents `res://parts/catalog` / `.tres` discovery, `pose-ring.js`, `CuriousContraptions.Engine` or `getF16` as current.
  
  Its negative control feeds the same check a fixture string that reinstates one phrase, and the check fails.
- **And** every relative link in the changed docs resolves, and `dotnet test CuriousContraptions.slnx --filter ArchitectureDocsConsistencyTests` passes (native proof; this documentation-only story needs no Chrome run).

### Story 9A.1: Dead Legacy Purge
*Status: Backlog*

*Depends on: Story 9A.0. Audit: B3, B4, B5, B7, B8, D4 (JS producer) and the dead parts of A17.*

*Deletes:*
- *`scratch/` (untracked), `scratch/CounterTestsDraft.cs.uid` and the root `scratch_test.cs.uid`;*
- *`tools/workshop-rigid-body.test.mjs`, its reference in `tools/e2e/README.md:20` and the comment in `tools/e2e/cat-014.test.ts:15`;*
- *the EarlyLoss case and the `navigator.gpu` device-loss mock in `tools/workshop-observation.test.mjs:161-174`;*
- *in `CuriousContraptions.Simulation/wwwroot/worker.js`: the 20 layout constants at `:13-37` (`BODY_*`, `MOTION_*`, `PRISMATIC_BYTES`, `COLLIDERS_OFFSET`, …) and `writeEventRing` (`:84-108`);*
- *in `CuriousContraptions.tests/WorkerAbiTests.cs`: the facts that pin those constants (`:35`, and `:52` `WorkerDynamicOffsetFallbacksDeclareValidTableOffsets`);*
- *the JS-boundary exports in `CuriousContraptions.Simulation/Program.cs:175-264` (`AllocateElectrical`, `PaidContactImpulse`, `PrismaticRowKinds`, `SpringCoefficients`, `PrismaticJacobians`, `ConstraintRows`, `OperationAbi`, `StateBytes`, `AbiOffsets`) and their host stubs in `tools/workshop-pose-ring.test.mjs` and `tools/workshop-observation.test.mjs`;*
- *the `MachinePart.Build()` subclassing hook (`engine/MachinePart.cs:236`, called at `:128`) and the test `ProbePart` override (`CuriousContraptions.tests/BasketballResourceTests.cs:162-165`);*
- *the dead "unmigrated legacy" skips in `engine/PartRegistry.cs:28-29` and `:35-36`.*

As a maintainer,  
I want code that no current path can reach deleted,  
So that agents cannot copy or revive a dead pattern.

**Acceptance Criteria:**
- **Given** the audit's §7 evidence that nothing live references these items, **When** they are deleted, **Then** an active-tree `rg` for each deleted symbol returns 0 hits outside git history.
- **And** the engine laws that the deleted exports exposed stay covered by their existing C# unit tests; the story names those tests and they pass.
- **And** `node --test tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` passes on the remaining cases.
- **And** `ProbePart` now proves its behaviour through declared bindings.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview (the Story 9.4b baseline).

### Story 9A.2: Single FlatBuffers Codec
*Status: Backlog*

*Depends on: Story 9A.1. Audit: B1, plus the legacy decode arms in A3.*

*Deletes:*
- *the legacy binary codec `engine/gpu/WorkshopWire.cs` (command, response, construction and decode-per-id arms from `:400`);*
- *the swallowing `catch { WorkshopWire.Decode* }` fallbacks in `engine/gpu/WorkshopCommandRouter.cs:17-25` and `engine/gpu/BrowserWorkshopClient.cs:268-275`;*
- *in `worker.js:138-175`: the hand-written vtable decode in `extractReadIdentity` and its fixed-offset `responseAbi` fallback.*

As a player,  
I want every message between the game and its workers to use one checked format,  
So that a malformed message fails visibly instead of being silently reinterpreted.

**Acceptance Criteria:**
- **Given** the remaining `WorkshopWire` callers, **When** this story lands, **Then** each of them encodes and decodes through `WorkshopWireFlatBuffers` and the FlatBuffers schemas only:
  - the schedule endpoints (`CuriousContraptions.Simulation/Program.Schedule.cs:46`, `engine/gpu/BrowserWorkshopClient.Schedule.cs:58`, `CuriousContraptions.Animation.Worker/Program.cs:100`);
  - `engine/gpu/WorkshopTrace.cs:41`;
  - `engine/gpu/WorkshopSaveCodec.cs` (`ByteLength = 24 + WorkshopWire.ConstructionBytes`).
- **And** `tools/e2e/workshop-wire.ts` decodes the FlatBuffers format only.
- **And** a truncated, unknown-tag or trailing-byte buffer is rejected with a typed fault naming the reason. The input is never retried through another codec (negative control in a unit test for commands, responses and saves).
- **And** the save and trace versions are bumped. An older save is rejected explicitly with a player-visible message and the current bench is kept (no migration).
- **And** `rg "WorkshopWire\."` returns 0 hits outside the FlatBuffers codec.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview, including every suite that saves and loads.

### Story 9A.3: Transport Controls as Typed Commands and Queries
*Status: Backlog*

*Depends on: Story 9A.2. Audit: C2, C3.*

*Deletes:*
- *the raw `reliableStalled` `postMessage` flag path (`CuriousContraptions.web/wwwroot/workshop-client.js:78-86` → `worker.js:315` → `Program.SetReliableStall` at `Program.cs:495`, which stops the run at `:428-431`), or whatever of it Story 9.4b left;*
- *the raw `memoryRequest` channel (`worker.js:321-333`, `Program.ObserveMemory` at `Program.cs:144-158`, `BrowserWorkshopClient.cs:325-348`, `workshop-client.js:508-534`).*

As a player,  
I want transport problems handled through the same checked channel as every other command,  
So that a stalled connection stops the run in a predictable way.

**Acceptance Criteria:**
- **Given** a reliable-read stall, **When** it is detected, **Then** either a typed `TransportStalled` command with a sequence and a receipt passes through `WorkshopCommandRouter`, or the worker detects the stall in its own reliable-read accounting. Either way the run stops with `ReliableStalled` and the player sees the existing stall state.
- **And** memory observation is a typed query with a receipt on the command/query transport.
- **And** an untyped message on the old channel names is rejected, and the rejection is visible in the worker fault log (negative control).
- **And** `tools/workshop-observation.test.mjs` exercises the typed command and query.
- **And** `cat-023b` passes in three consecutive serial runs.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.4: Lossless Bundle Build Gate
*Status: Backlog*

*Depends on: Story 9A.0. Audit: A13 (build side), A14, G10.*

*Deletes:*
- *the hand-maintained list of 13 declarations in `tools/BundleElements/Program.cs:21-36` and the anonymous-object serialiser at `:38-50`;*
- *the committed lossy `parts/elements.bundle.json`, which becomes a build output and is git-ignored;*
- *the `ElementMetadata.Parameters` property bag (`IReadOnlyDictionary<string, object>`) and the string `ElementMetadata.Category` (`CuriousContraptions.Abstractions/ElementMetadata.cs`).*

As an element author,  
I want my package to be built, validated and serialised without loss on every build,  
So that the bundle can become the only declaration the game loads.

**Acceptance Criteria:**
- **Given** the solution, **When** it builds, **Then** `CuriousContraptions.slnx` contains every `elements/*` project, the four domain Abstractions and `tools/BundleElements`.
- **And** an MSBuild target runs `BundleElements` before the Godot export and before publish. The target discovers packages by globbing `elements/*/*.csproj` and reflecting over `IPuzzleElementDeclaration` implementations; there is no hand list.
- **And** the bundle schema is typed and versioned:
  - f32 vectors as `[x, y, z]` and quaternions as `[x, y, z, w]`, written bit-exactly;
  - mechanical (mass, inertia, material, anchors), electrical (nodes) and signal (triggers, latch, counter) payloads;
  - a typed parameter schema: key, unit, an enum kind, f32 minimum, maximum and default, or enum options;
  - an enum category.
- **And** generation fails the build, naming the package and field, for a duplicate id, a non-finite value, an unknown enum, or a body without a collider (negative control with fixture declarations).
- **And** a round-trip test proves that every package's declaration equals its deserialised bundle entry, f32 bit for bit.
- **And** a temporary cutover guard, `BundleCatalogueParityTests`, proves that every packaged element's metadata, category, ports, colliders, footprint, material and parameter defaults equal the current runtime values: `parts/catalog/*.tres`, `WorkshopPorts.For`, `WorkshopCanonicalColliders.For`, `WorkbenchFootprint.Of`, and the defaults in `MachineWorld.CaptureInstance`.
- **And** the same test asserts that the catalogue ids without a package are exactly: basket, bowling, delay, funnel, pipe, pipe_bend_45, pipe_bend_90, spring, switch, tennis and balloon. Story 9A.14 deletes this guard together with the tables it compares.
- **And** the package categories adopt the catalogue's current player-facing categories (for example `ball` is "Motion"), so palette grouping does not change for the player. *Coordinator decision under owner delegation, 18 Oct 2026 (playability): the palette must not reshuffle during a refactor.*
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview (no runtime change).

### Stories 9A.5–9A.13: one package per element that has none

Each of these nine stories adds one `elements/<Name>/` project. It does not change runtime behaviour; it makes the atomic catalogue cutover in Story 9A.14 possible without dropping an element.

### Story 9A.5: Receiver Element Package (CAT-004, id `basket`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/basket.tscn`, which is moved (not copied) into `elements/Receiver/`; `parts/catalog/basket.tres` points at the moved scene until Story 9A.14 deletes it. Also deletes `basket` from the parity guard's unpackaged set.*

As an element author,  
I want the Receiver declared only in its own package,  
So that the bundle can carry it when the `.tres` catalogue is deleted.

**Acceptance Criteria:**
- **Given** the Receiver's current runtime values:
  - `basket.tres` (title "Receiver", category "Goals");
  - the `basket` arms of `WorkshopPorts.For`, `WorkbenchFootprint.Of` and `WorkshopCanonicalColliders.For`;
  - `ReceiverCaptureSettings` and `ReceiverForceRegion` (`engine/gpu/WorkshopConstruction.cs:90-125`);
  - `CosmeticCurves.Receiver` (`engine/gpu/WorkshopCosmetic.cs`);
  
  **When** `elements/Receiver/Receiver.csproj` is added, referencing only `CuriousContraptions.Abstractions` and `CuriousContraptions.Abstractions.Mechanical`, **Then** the generated bundle contains `basket` and `BundleCatalogueParityTests` proves its values equal the runtime values bit for bit.
- **And** the capture settings and force region are declared parameters with the current defaults and ranges.
- **And** a project-reference test proves the package references neither the engine nor a sibling.
- **And** `cat-062b` and every other suite that places a Receiver pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.6: Bowling Ball Element Package (CAT-014, id `bowling`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/bowling.tscn`, moved into `elements/BowlingBall/`. Also deletes `bowling` from the parity guard's unpackaged set.*

As an element author,  
I want the Bowling Ball declared only in its own package,  
So that its heavy-ball material lives with the element and not in an engine switch.

**Acceptance Criteria:**
- **Given** the current bowling values:
  - `bowling.tres` (title "Bowling ball", category "Motion", embedded ball material);
  - the bowling `BallMaterial` arm (`engine/gpu/WorkshopConstruction.cs:61-62`), radius 0.28 m and mass 4 kg (owner decision, 9 Oct 2026);
  - the `bowling` arms of the port, footprint and collider tables;
  
  **When** `elements/BowlingBall/BowlingBall.csproj` is added, **Then** the bundle contains `bowling` with that material as declared data, and the parity guard proves equality bit for bit.
- **And** the package references only the Abstractions packages it needs.
- **And** `cat-014`, `cat-015a`, `cat-052` and `cat-005` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.7: Delay Box Element Package (CAT-022, id `delay`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/delay.tscn`, moved into `elements/DelayBox/`. Also deletes `delay` from the parity guard's unpackaged set.*

As an element author,  
I want the Delay Box declared only in its own package,  
So that its duration parameter and activation ports come from the bundle.

**Acceptance Criteria:**
- **Given** the current delay values:
  - `delay.tres` (title "Delay", category "Control");
  - the `delay` port, footprint and collider arms;
  - `DelayDuration` (default and range);
  - `CosmeticCurves.Delay`;
  
  **When** `elements/DelayBox/DelayBox.csproj` is added (Abstractions plus `Abstractions.Signal`), **Then** the bundle contains `delay` with a typed duration parameter and its activation in/out ports, and the parity guard proves equality.
- **And** `anim-1b`, `anim-1c`, `cat-015a-sidekick`, `cat-015b`, `cat-037`, `cat-062` and `cat-062b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.8: Funnel Element Package (CAT-030, id `funnel`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/funnel.tscn`, moved into `elements/Funnel/`. Also deletes `funnel` from the parity guard's unpackaged set.*

As an element author,  
I want the Funnel declared only in its own package,  
So that its hollow-frustum colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current funnel values (`funnel.tres`, the colliders that `WorkshopGenericInstance` receives from the registry or `WorkshopCanonicalColliders.For("funnel")`, footprint and ports), **When** `elements/Funnel/Funnel.csproj` is added, **Then** the bundle contains `funnel` and the parity guard proves its colliders equal the current ones bit for bit.
- **And** `cat-030` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.9: Clear Pipe Element Package (CAT-048, id `pipe`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/pipe.tscn`, moved into `elements/Pipe/`. Also deletes `pipe` from the parity guard's unpackaged set.*

As an element author,  
I want the Clear Pipe declared only in its own package,  
So that its compound-cylinder and torus-rim colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current pipe values (`pipe.tres` title "Clear pipe", the declared colliders, footprint and ports), **When** `elements/Pipe/Pipe.csproj` is added, **Then** the bundle contains `pipe` and the parity guard proves equality bit for bit.
- **And** `cat-048a`, `cat-048b` and `cat-030` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.10: 45° Pipe Bend Element Package (CAT-049, id `pipe_bend_45`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/pipe_bend_45.tscn`, moved into `elements/PipeBend45/`. Also deletes `pipe_bend_45` from the parity guard's unpackaged set.*

As an element author,  
I want the 45° bend declared only in its own package, as a distinct element from the 90° bend,  
So that its torus-segment colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current `pipe_bend_45` values, **When** `elements/PipeBend45/PipeBend45.csproj` is added, **Then** the bundle contains `pipe_bend_45` and the parity guard proves equality bit for bit.
- **And** `cat-049` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.11: 90° Pipe Bend Element Package (CAT-050, id `pipe_bend_90`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/pipe_bend_90.tscn`, moved into `elements/PipeBend90/`. Also deletes `pipe_bend_90` from the parity guard's unpackaged set.*

As an element author,  
I want the 90° bend declared only in its own package,  
So that its torus-segment colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current `pipe_bend_90` values, **When** `elements/PipeBend90/PipeBend90.csproj` is added, **Then** the bundle contains `pipe_bend_90` and the parity guard proves equality bit for bit.
- **And** `cat-050` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.12: Springboard Element Package (CAT-062, id `spring`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/springboard.tscn`, moved into `elements/Springboard/`. The id is `spring`; the catalogue file is `parts/catalog/springboard.tres`. Also deletes `spring` from the parity guard's unpackaged set.*

As an element author,  
I want the Springboard declared only in its own package,  
So that its stiffness, damping and precompression are declared parameters.

**Acceptance Criteria:**
- **Given** the current springboard values:
  - `springboard.tres`;
  - the `spring` port, footprint and collider arms;
  - `SpringboardSettings` (defaults and ranges);
  - the prismatic slider declaration that `WorkshopPhysicsCompiler` emits for it;
  
  **When** `elements/Springboard/Springboard.csproj` is added, **Then** the bundle contains `spring` with typed parameters and its slider declaration, and the parity guard proves equality.
- **And** `cat-062`, `cat-062b` and `cat-051` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.13: Impact Switch Element Package (CAT-063, id `switch`)
*Status: Backlog*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/switch.tscn`, moved into `elements/ImpactSwitch/`. Also deletes `switch` from the parity guard's unpackaged set.*

As an element author,  
I want the Impact Switch declared only in its own package,  
So that its trigger threshold and sockets come from the bundle (Story 9.5b later adds its electrical mode to this package).

**Acceptance Criteria:**
- **Given** the current switch values:
  - `switch.tres` (title "Impact switch", category "Power");
  - the `switch` port, footprint and collider arms;
  - `ContactTriggerSettings` (threshold default and range);
  - `CosmeticCurves.ImpactSwitch`;
  
  **When** `elements/ImpactSwitch/ImpactSwitch.csproj` is added, **Then** the bundle contains `switch` and the parity guard proves equality.
- **And** `cat-037`, `anim-1b`, `anim-1c` and `engine-core-2c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.14: Bundle Is the Sole Catalogue Source
*Status: Backlog*

*Depends on: Stories 9A.5–9A.13. Audit: A13 (runtime side), A17, A18, G3.*

*Deletes:*
- *all 24 `parts/catalog/*.tres` files;*
- *the Godot `Resource` form of `engine/PartDefinition.cs` and `engine/BallMaterialResource.cs`;*
- *in `engine/PartRegistry.cs:13-49`: `PartRegistry.Discover()`'s `.tres` scan, the `clock.tres` / `PartId.Clock` exemption (`:35-36`) and the `Ball.Capture` Basketball/Bowling exemption (`:44-45`);*
- *the remaining 13 `parts/scenes/*.tscn` (moved into their packages) and the emptied `parts/` directory;*
- *the static Free palette list (`engine/gpu/WorkshopInventory.cs` around lines 50–70);*
- *Tennis and Balloon (A18): `tennis.tres`, `balloon.tres`, `PartId.Tennis`/`PartId.Balloon` and their `FromNumericId` arms (`engine/PartId.cs:33-34`, `:90-91`), and their `"tennis"`/`"balloon"` arms in `ui/WorkshopIcons.cs`;*
- *the `BundleCatalogueParityTests` cutover guard.*

As a player,  
I want the toolbox to show exactly the elements the game can build,  
So that every part I pick can be placed and run.

**Acceptance Criteria:**
- **Given** the generated bundle, **When** the game starts, **Then** `PartRegistry` loads only the bundle. Each definition is a plain runtime record built from a bundle entry, and its scene loads from the package path (`res://elements/<Name>/…`).
- **And** a missing, malformed or wrong-version bundle stops start-up with an explicit message naming the problem. Nothing is loaded from elsewhere.
- **And** the Free palette lists every bundle element, grouped by declared category, with the same titles, categories and order the player saw before (screenshot compared against the Story 9.4b baseline).
- **And** Tennis and Balloon no longer appear.
- **And** the new `tools/e2e/arch-9a-catalogue.test.ts` passes. In actual Chrome it:
  - asserts that the palette shows exactly the 22 bundle elements;
  - places each one through the real drawer;
  - runs, resets and save/loads one bench containing all of them.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

*Tennis and Balloon (coordinator decision under owner delegation, 18 Oct 2026).* They are removed from the palette rather than kept, for three reasons:
- They cannot be constructed today: `MachineWorld.CaptureInstance` throws for them (`engine/MachineWorld.Gpu.cs:124`) and `WorkbenchCapacity.cs:55` has no footprint.
- They have no package and no buoyancy or air-drag capability, so no actual-UI proof is possible.
- A part that throws when placed makes the game less playable.

Story 12.1 creates their packages and returns them to the palette.

### Story 9A.15: Saves and Messages Carry the Bundle Element Id
*Status: Backlog*

*Depends on: Story 9A.14. Audit: A2 (numeric switches).*

*Deletes: `PartId.ToNumericId` (`engine/PartId.cs:46-70`) and `PartId.FromNumericId` (`:72-99`), and every numeric part-id field in the FlatBuffers schemas and the save codec.*

As a player,  
I want my saved machines to name each part by the same id the game loads it by,  
So that adding a new element never renumbers existing ones.

**Acceptance Criteria:**
- **Given** a construction, **When** it is saved, sent to a worker or traced, **Then** each instance carries its typed `PartId` string. That string is validated against the loaded bundle only at the serialisation boundary.
- **And** an unknown id is rejected atomically with the id named in the error (negative control).
- **And** the save, wire and trace versions are bumped. Older saves are rejected with a player-visible message and no migration (AGENTS.md: no automatic migration).
- **And** every suite that saves and loads passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.16: Bundle Is the Sole Collider Source
*Status: Backlog*

*Depends on: Story 9A.14. Audit: A3 (colliders), B2.*

*Deletes:*
- *`engine/gpu/WorkshopCanonicalColliders.cs` (whole file: the 10-arm `For` switch at `:20-33`, every per-element table including `CreateLatchColliders`, and 295 `Half` tokens);*
- *the fallbacks `def.Colliders.Count > 0 ? def.Colliders : WorkshopCanonicalColliders.For(id)` at `engine/MachineWorld.Gpu.cs:118` and `:120`, and `colliders ?? WorkshopCanonicalColliders.For(...)` at `engine/gpu/WorkshopConstruction.cs:256, 304, 307, 310, 313`;*
- *the collider re-derivation in decode (`WorkshopWireFlatBuffers.cs:892`);*
- *the inline latch collider loops in `WorkshopPhysicsCompiler.cs:245-300`;*
- *the runtime call sites of collider extraction from scene nodes (`engine/gpu/GltfPhysicsBodyExtractor.cs` callers on the main thread and in the compiler). The Story 6.5c extraction logic itself moves to build time in `tools/BundleElements` and is not reversed.*

As a player,  
I want every part to collide exactly as the art I see,  
So that a part never behaves differently because a fallback table was used.

**Acceptance Criteria:**
- **Given** a package, **When** the bundle builds, **Then** `BundleElements` runs the Story 6.5c extraction over that package's own authored scene or glTF (`col_*` proxies or `KHR_physics_rigid_bodies`) and bakes the resulting colliders into the bundle as f32 values.
- **And** colliders that a package declares in data, with no scene colliders, are baked unchanged.
- **And** a package that declares colliders in data *and* has scene colliders fails the build, naming the package. There is one collider source per package, with no merging (negative control).
- **And** at runtime, colliders come only from the bundle. An element with a body but no collider fails the bundle build (Story 9A.4 validation), and a construction referencing an element with no colliders is rejected explicitly.
- **And** a golden compile test captured before the change proves that every element's compiled collider rows are unchanged within f32 representation.
- **And** `cat-048a`, `cat-048b`, `cat-049`, `cat-050`, `cat-030` and `cat-037` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

*Decision (coordinator under owner delegation, 18 Oct 2026; Open scheduling item h).* Colliders are extracted from the package's own scene at build time and baked into the bundle. Story 6.5c is kept, the package owns its scene, and the bundle stays the single declaration source. Rationale: colliders always match the art the player sees. Story 9A.0's rewrite of AD-11 records this.

### Story 9A.17: Bundle Ports and Footprints
*Status: Backlog*

*Depends on: Story 9A.14. Audit: A3 (ports, footprint).*

*Deletes:*
- *`WorkshopPorts.For` (`engine/gpu/WorkshopConnections.cs:19-34`, 12 arms) and the per-(id, socket) `LocalPosition` lookup (`:97`);*
- *`WorkbenchFootprint.Of` (`engine/gpu/WorkbenchCapacity.cs:35-56`, 18 arms);*
- *any other runtime port-position source found, such as scene `Loc_Port_<Id>` lookups in `engine/MachinePart.cs`.*

As a player,  
I want wires to snap to the sockets each part declares, and parts to occupy the space they declare,  
So that wiring and placement behave the same for every element.

**Acceptance Criteria:**
- **Given** any element, **When** I wire or place it, **Then** its sockets, directions, domains and anchor positions come only from its bundle `PortDescriptor` list, and its bench footprint only from its declaration.
- **And** a golden test proves that every element's ports and footprint are unchanged.
- **And** a wrong-domain or output-to-output link is still refused (negative control).
- **And** every wiring suite passes (`cat-005`, `cat-017`, `cat-020`, `cat-037`, `cat-051`, `cat-052`, `anim-1b`, `anim-1c`).
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.18: Element Icons Ship in Their Packages
*Status: Backlog*

*Depends on: Story 9A.14. Audit: A3 (icons).*

*Deletes: every element arm in `ui/WorkshopIcons.cs`, both delivered elements (ball, bowling, ramp, wall, basket, battery, bumper, spring, switch, domino, lamp, latch, clock, …) and undelivered ones (motor, fan, bellows, windmill, wind_chimes, bell, sound_meter, speaker, clutch). Each undelivered element's icon is authored again in its own package when that element ships. Icons for UI controls and socket choices (resize, set_input, reset_input, first_input, second_input, extended_output, retracted_output) stay.*

As a player,  
I want each part's toolbox icon to come with that part,  
So that new elements bring their own icon without engine edits.

**Acceptance Criteria:**
- **Given** each package, **When** the bundle is built, **Then** the package's `icon.svg` is referenced by `ElementMetadata.IconPath` and the palette renders it.
- **And** a missing icon fails the bundle build (negative control).
- **And** in actual Chrome, palette icon screenshots match the Story 9.4b baseline for every element.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.19: Declared Prismatic Drive for the Powered Gate
*Status: Backlog*

*Depends on: Stories 9A.17 and 9.4 (review-9-4 F6 fixed there). Audit: A7 (gate part).*

*Deletes whatever of the following Story 9.4 has not already deleted (the implementer lists which are present at start):*
- *gate detection by joint limit (`joint.Upper ≈ 1.55`, `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:241`);*
- *the drag bands (`:247-249`, 0.03–0.08 = battery gate, 0.08–0.11 = plate gate) and the `ActuatorSliderDrive` heuristics;*
- *the gate mode encoded as blade drag 0.0625 / 0.09375 (`engine/gpu/WorkshopPhysicsCompiler.cs:186-213`);*
- *the `batteryPowered` flag (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:764-776`, where the first supply found powers all gates);*
- *`SimJoint.SwitchedSupplyClosed` (`PrismaticConstraintSolver.cs:35`);*
- *any remainder of `SwitchedSupplyRoutes` and `Program.SwitchedSupplies`/`Program.ElectricalState` (`Program.cs:160-171`).*

As a player,  
I want each Powered Gate to open only from its own supply,  
So that two gates on separate circuits never move together by accident.

**Acceptance Criteria:**
- **Given** the Powered Gate package, **When** it is compiled, **Then** it declares a prismatic drive capability: the target opening 1.55 m, the speed law sign(d)·min(2.8, √(2·14·|d|)), effort ≤ 60 N, power ≤ 120 W, and the return spring 14 N/m with damping 4 N·s/m (CAT-051 §2).
- **And** the drive is enabled only by its compiled electrical consumer row, including the plate-switched route, which is a compiled supply route through the plate's declared switch contact.
- **And** two gates on two separate batteries, built through real UI, open independently: disabling one battery leaves the other gate open (negative control for the first-supply heuristic).
- **And** a box body with the gate's dimensions and drag but no declared drive never actuates (negative control for the heuristics).
- **And** `cat-051`, `cat-052`, `cat-037`, `cat-005-n19` and `cat-051-interrupt` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.20: Declared Contact-Load Sensor for the Pressure Plate
*Status: Backlog*

*Depends on: Story 9A.19. Audit: A7 (plate part).*

*Deletes whatever of the following Story 9.4 has not already deleted (the implementer lists which are present at start): Pressure Plate detection by collider half-extents 0.9/0.05/0.9 and Ty 0.03 (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:831`, `:835`), the `_plateLoaded` flag and the hard-coded 0.5 kg literal (`:853`).*

As a player,  
I want the Pressure Plate to respond to resting weight because it declares a load sensor, not because of its size,  
So that any element can declare a load sensor and none is triggered by accident.

**Acceptance Criteria:**
- **Given** a Pressure Plate, **When** it is compiled, **Then** it emits a `ContactLoadSensorDeclaration` row whose threshold is the instance's declared `minimum_mass` parameter (f32 kg, range 0.1–16, default 0.5; [CAT-052](../../docs/planning/elements/CAT-052-pressure_plate.md) §2; Open scheduling item h).
- **And** a ball resting on the plate activates it, removing the ball deactivates it, and a bounce without resting weight does not sustain activation (Story 9.1 behaviour).
- **And** a static box with the plate's exact half-extents and offset but no declared sensor never activates (negative control).
- **And** `cat-052` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.21: Declaration-Free Worker Warm-Up
*Status: Backlog*

*Depends on: Story 9A.19. Audit: A9.*

*Deletes: the element warm-up scene in `CuriousContraptions.Simulation/Program.cs:65-72` (Battery, PressurePlate, PoweredGate, Basketball, Ramp) and the dummy joint in `WorkshopPhysicsEngine.cs` (around lines 180–195: `Upper = 1.55`, spring 14).*

As a player,  
I want the first Run to start without a hitch,  
So that the warm-up never depends on which elements exist.

**Acceptance Criteria:**
- **Given** worker start-up, **When** it warms up, **Then** it compiles and steps a synthetic, declaration-free scene: one dynamic sphere body, one static box collider and one prismatic row with arbitrary declared parameters. No element id or element value appears.
- **And** time to the first committed tick after the first Run is recorded before and after the change and has not regressed beyond run-to-run noise (advisory; detailed performance stays at the Epic 16 gate).
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

*Design choice (coordinator, under owner delegation; playability).* A synthetic warm-up is kept rather than deleted, because a first-Run stall is visible to the player.

### Story 9A.22: Event Ring Producer
*Status: Backlog*

*Depends on: Stories 9A.2 and 9.4. Audit: D1, A16.*

*Deletes:*
- *the default-empty `Span<byte> eventBuffer = default` parameter of `ActivationNetwork.Consume` (`engine/gpu/ActivationNetwork.cs:243`) and its publish-only-if-non-empty branches (`:391-402` Clock `TriggerActivated`, `:487-491` Latch);*
- *the element-named kinds `SimulationEventKind.CounterReached` and `LatchSet` (`CuriousContraptions.Abstractions/EventDescriptor.cs:3-14`), replaced by the generic `ThresholdReached` and `MemoryChanged`. The Counter and State Latch packages are updated in the same commit.*

As a player,  
I want every discrete thing that happens in a run (an impact, a crossing, a threshold, a power change) to be announced once, at the moment it is committed,  
So that sound, animation and the interface can all react to the same event.

**Acceptance Criteria:**
- **Given** a run, **When** a tick commits, **Then** the worker publishes exactly one ring record per discrete transition, with the payload layout declared by the element's `EventDescriptor`. The kinds are: Impact, Passage, ContactLoad, PrismaticLimit, ElectricalPowered, ThresholdReached, MemoryChanged and TriggerActivated.
- **And** a discarded or failed tick publishes nothing.
- **And** Reset and each new run restart the ring sequence.
- **And** overrun follows the spec-9-2b §5 policy.
- **And** `worker.js` copies the committed ring bytes into the SharedArrayBuffer the same way `writePoseRing` copies poses, with no domain logic in JS.
- **And** unit tests cover every kind, including a negative case where a non-qualifying contact publishes nothing.
- **And** in actual Chrome, `cat-015a` and `cat-020` gain a ring read through the current `globalThis.WorkshopEventRing` reader (no suite reads it today) and see exactly one Impact per Bumper hit and one ThresholdReached per Counter target.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.23: Event Ring Consumers
*Status: Backlog*

*Depends on: Story 9A.22. Audit: D2, D3, D4 (JS reader).*

*Deletes:*
- *occurrence-carrying reliable reads: `occurrenceReads`, `reserveOccurrenceRead`, `releaseOccurrenceRead` and `publish(..., retainOccurrence)` (`worker.js:11`, `:217-235`), and `EmitRead(retainOccurrence)` (`CuriousContraptions.Simulation/Program.cs:472-491`);*
- *the occurrence clauses of `WorkshopRead.RequiresReliableRead` (`engine/gpu/WorkshopRead.cs:9-18`) and the occurrence arrays in `PhysicsContactWorkRead`;*
- *the sequence-diff pumps in `engine/gpu/BrowserWorkshopClient.ContactWork.cs:27-60` and `BrowserWorkshopClient.Capture.cs`, and the one-shot pumps in `PumpCosmetics` (`BrowserWorkshopClient.Cosmetic.cs:23`);*
- *the main-thread `Transition` relay of one-shots to the Animation worker;*
- *the divergent JS reader `createEventConsumer`/`readEventRing` (`workshop-client.js:734-788`), replaced by the new main-thread consumer. The `globalThis.WorkshopEventRing` hook survives until Story 9A.24 and reads through that new consumer, so the Story 9A.22 ring assertions keep running.*

As a player,  
I want impacts, captures and switch clicks to animate from the event itself,  
So that effects line up with the physics and none is lost or doubled.

**Acceptance Criteria:**
- **Given** the event ring, **When** a run starts, **Then** the Animation worker receives the ring and holds its own cursor with domain and kind filters. It drives the Bumper impact pulse, Receiver capture, and switch and latch one-shots from ring records.
- **And** the main-thread UI holds a separate cursor for goal and sensor readouts.
- **And** level states (lamp level, latch memory, Delay phase, electrical level) stay committed-state queries.
- **And** one consumer implementation per worker reproduces `EventRingConsumer`'s skip-uncommitted semantics, and its overrun is explicit.
- **And** the Story 9A.22 ring assertions in `cat-015a` and `cat-020` still pass through the re-pointed hook.
- **And** `anim-1a`, `anim-1b`, `anim-1c`, `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062b` and `cat-037` pass, with cosmetic timing checked.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.24: Typed Read-Only Observation Port Replaces Console Channels
*Status: Backlog*

*Depends on: Story 9A.23. Audit: C4, C5.*

*Deletes:*
- *`GD.Print("CCGPU " + base64(response))` (`engine/MachineWorld.Gpu.cs:197`, `:363-366`);*
- *`CCGOAL_SOLVED` (`ui/WorkshopAnimation.cs:96`);*
- *`globalThis.WorkshopPoseRing`, `WorkshopAnimation` and `WorkshopEventRing` (`workshop-client.js:796-809`);*
- *the `CCGPU` acknowledgement parsing and global-hook reads in `tools/e2e/workshop-driver.ts` (`:53`, `:117-120`, `:417-420`, `:465-474`, `:786`) and in `cat-005`, `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062`, `cat-062b`, `cat-023b` and `engine-core-2a1`.*

As a tester proving the shipped game,  
I want one typed, read-only observation port,  
So that every e2e suite observes the production build without console scraping or ad-hoc globals.

**Acceptance Criteria:**
- **Given** the production build, **When** a page loads, **Then** a versioned, typed observation port exposes three things:
  - command receipts (sequence, kind, result);
  - a committed read-model query;
  - an independent event-ring cursor.
- **And** the port cannot issue a command or mutate any state (negative control: every mutation attempt through it is rejected and the construction is unchanged).
- **And** `workshop-driver.ts` waits on receipts and goal events through the port, and every suite above is migrated.
- **And** `rg "CCGPU|CCGOAL_SOLVED|globalThis.Workshop"` returns 0 hits.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

*Design choice (coordinator, under owner delegation).* The port ships in the single production artifact rather than in a separate `PLAYTEST` build, because AGENTS.md requires Chrome proof of the production build. It is read-only by construction.

### Story 9A.25: Presentation Bindings Ship in Packages
*Status: Backlog*

*Depends on: Stories 9A.17 and 9A.23. Audit: A4, A10.*

*Deletes:*
- *the `MachinePart.BuildDeclarative` chain of 24 `PartId ==` arms (`engine/MachinePart.cs:237-530`);*
- *`ResizableAxes` by id (`:40-50`);*
- *the Clock and Counter lamp methods (`:106-113`, `:220-221`, `:533`, `:543`);*
- *`CosmeticCurves.ImpactSwitch/SignalLamp/Delay/PinballBumper/Receiver/BallDetector/StateLatch` (`engine/gpu/WorkshopCosmetic.cs:55-63`);*
- *the element-named `AnimationFeedbackSource.Memory` (`:8`), replaced by generic feedback sources: activation level, state level, timer phase, contact pulse and capture.*

As an element author,  
I want my element's moving parts, curves, pick radius and resizable axes declared in its package,  
So that `MachinePart` applies every element's presentation generically.

**Acceptance Criteria:**
- **Given** each package's declared bindings (target node path, enum channel, f32 from/to, curve, duration, driver as a committed-state query or ring kind, pick radius, resizable axes), **When** a part is built, **Then** `MachinePart` applies them with no element branch.
- **And** sampled node properties at fixed curve times equal the pre-change values for every element (golden test). The shipped values are preserved, including whatever Story 9.4 decides for the State Latch curve (review-9-4 F5).
- **And** in actual Chrome, per-element motion screenshots match the Story 9.4b baseline.
- **And** `anim-1a`, `anim-1b` and `anim-1c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.26: Lessons and Puzzles as Content Data
*Status: Backlog*

*Depends on: Stories 9A.14 and 9A.17. Audit: A12, A19.*

*Deletes:*
- *`engine/gpu/BumperAdvanced.cs`, `BumperSidekick.cs`, `SpringboardLessons.cs`, `DominoEffect.cs`, `DelayedSignal.cs`, `ClearPipe.cs`, `GentleBend.cs` and `QuarterBend.cs`;*
- *the closed `WorkshopPuzzleId` enum (`engine/gpu/WorkshopPuzzle.cs:5`) and `WorkshopPuzzle.WallAssistance`'s id check (`:57`);*
- *the `ReadyToRebound` branch in `WorkshopConstruction.WithInstance` (`engine/gpu/WorkshopConstruction.cs:131-133`);*
- *the `WallAndBumper` branch in `WorkshopInventory.Authored` (`engine/gpu/WorkshopInventory.cs:93`);*
- *the puzzle-id switch in `ui/WorkshopPuzzle.cs:18-28`.*

As a player,  
I want every lesson to play exactly as before while being loaded as data,  
So that the 150 campaign levels can be authored without engine code.

**Acceptance Criteria:**
- **Given** `content/puzzles.json`, extended with a typed, versioned schema, **When** a lesson loads, **Then** the lesson is built entirely from that data:
  - an extensible typed puzzle id;
  - placements (part id, pose, locked flag, parameter values validated against each element's declared parameter schema);
  - inventory allowances;
  - assistance profiles;
  - goal predicates.
  
  The construction is compiled on the main thread from that data.
- **And** a content file with an unknown part id, an unknown parameter or an out-of-range value is rejected at load, with the puzzle and field named (negative control).
- **And** each lesson keeps its current layout, inventory and solution: FirstPrinciples, DelayedSignal, DominoEffect, BumperSidekick, BumperDepth, WallAndBumper, SpringForward, ReadyToRebound, ClearPipe, GentleBend and QuarterBend.
- **And** `cat-015a-sidekick`, `cat-015b`, `cat-062`, `cat-062b`, `cat-023b`, `cat-048b`, `cat-049` and `cat-050` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Stories 9A.27–9A.43: one element per story onto the generic declared instance

Each story converts one element from its engine record type to `WorkshopGenericInstance`, which carries an id, part id, pose, locked flag and typed parameters validated against the element's declared parameter schema. Pipe, both bends, Funnel and Powered Gate already use it (`engine/gpu/WorkshopInstances.cs:27`). The worker never resolves an element: the main thread resolves the bundle declaration, and the instance carries the declared capability payloads.

Each story deletes, for its element:
- the record type and its element-named settings records;
- its `WorkshopInput.<X>` factory (`engine/gpu/WorkshopConstruction.cs:152-313`);
- its `MachineWorld.CaptureInstance` arm (`engine/MachineWorld.Gpu.cs:93-125`);
- its arm in the `WorkshopInstances.Validate` type whitelist (`engine/gpu/WorkshopInstances.cs:95`);
- its `OfType<>`/`is` arms in `WorkshopPhysicsCompiler` and `ActivationNetwork.Compile` (`engine/gpu/ActivationNetwork.cs:688-722`);
- its arm in `MachineWorld.Gpu.cs:318-338`;
- its per-id FlatBuffers encode and decode arms (`engine/gpu/WorkshopWireFlatBuffers.cs:853+`).

If the element is the first to need a capability (dynamic body, contact trigger, orientation sensor, timer, memory, counter, electrical source), that story adds the generic compile of that capability from declared data. Each story's golden compile test proves the compiled capability rows are unchanged. Each story bumps the save version and rejects older saves explicitly.

### Story 9A.27: Generic Instance and Parameter Editor — Wall (CAT-066)
*Status: Backlog*

*Depends on: Stories 9A.25 and 9A.26. Audit: A5, A6, A11 (generic editor).*

*Deletes:*
- *`WorkshopWall` and `WallDimensions` (`engine/gpu/WorkshopWall.cs:30`), `WorkshopInput.Wall`, and the `"wall"` arm and the `wallDimensions` parameter of `CaptureInstance`;*
- *the Wall half of `is WorkshopRamp or WorkshopWall` in `WorkshopPhysicsCompiler`;*
- *`WorkshopWall` in the `Validate` whitelist.*

As a player,  
I want to resize and configure a Wall through an editor built from its declared parameters,  
So that every element after it can use the same editor.

**Acceptance Criteria:**
- **Given** a Wall placed through the real drawer, **When** I edit its width and height, **Then** the generic parameter editor (the generic path in `ui/WorkshopConfiguration.cs`) renders the declared parameters with their units and ranges, and the instance stores typed f32 values.
- **And** an out-of-range or unknown parameter is rejected and the instance is unchanged (negative control).
- **And** Run/Reset and Save/Load restore the wall exactly.
- **And** the golden compile rows are unchanged.
- **And** `cat-015b` and every suite that places a Wall pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.28: Generic Instance — Ramp (CAT-054)
*Status: Backlog*

*Depends on: Story 9A.27. Audit: A5, A6.*

*Deletes: `WorkshopRamp` and `RampDimensions` (`engine/gpu/WorkshopInstances.cs:42-56`), `WorkshopInput.Ramp`, the `"ramp"` arm and `rampDimensions` parameter of `CaptureInstance`, the remaining `is WorkshopRamp` in the compiler, and `WorkshopRamp` in the whitelist.*

As a player,  
I want the Ramp configured through the generic editor,  
So that it behaves as before with no engine record.

**Acceptance Criteria:**
- **Given** a Ramp placed and resized through real UI, **When** run, reset, saved and loaded, **Then** its declared length and width parameters are restored exactly and the golden compile rows are unchanged.
- **And** an out-of-range length is rejected (negative control).
- **And** every suite that places a Ramp passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.29: Generic Instance — Receiver (CAT-004)
*Status: Backlog*

*Depends on: Story 9A.27. Audit: A5, A6.*

*Deletes: `WorkshopReceiver`, `ReceiverCaptureSettings` and `ReceiverForceRegion` (`engine/gpu/WorkshopConstruction.cs:90-125`), `CaptureReceiver`, the `WorkshopConstruction.Receiver` accessor, `OfType<WorkshopReceiver>` in the compiler, and `WorkshopReceiver` in the whitelist.*

As a player,  
I want the Receiver to capture balls exactly as before,  
So that every goal keeps working with no engine record.

**Acceptance Criteria:**
- **Given** a Receiver placed through real UI, **When** a ball enters, **Then** capture and guide behaviour match the golden compile rows and the Story 9.4b `cat-062b` expectations.
- **And** a ball that misses is not captured (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-062b`, `cat-062` and `cat-014` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.30: Generic Instance — Basketball (CAT-001)
*Status: Backlog*

*Depends on: Story 9A.27. Audit: A5, A6, A3 (ball materials).*

*Deletes: the `"ball"` arm of `CaptureInstance`/`CaptureBall` and the basketball arm of `BallMaterial` by id (`engine/gpu/WorkshopConstruction.cs:61-62`). The shared `WorkshopBall` record stays until Story 9A.31, because Bowling still uses it.*

As a player,  
I want the Basketball to roll and bounce exactly as before,  
So that its material comes only from its package.

**Acceptance Criteria:**
- **Given** a Basketball placed through real UI, **When** run, **Then** its declared dynamic-sphere body and material compile to rows equal to the golden rows. This story adds the generic dynamic-body compile.
- **And** Run/Reset and Save/Load are exact.
- **And** `engine-core-2a1` to `engine-core-2c`, `cat-002` and `cat-023a` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.31: Generic Instance — Bowling Ball (CAT-014)
*Status: Backlog*

*Depends on: Story 9A.30. Audit: A5, A6.*

*Deletes: `WorkshopBall` and `BallMaterial`-by-id (`engine/gpu/WorkshopConstruction.cs:49-89`), `CaptureBall`, the `WorkshopConstruction.Ball` accessor, `OfType<WorkshopBall>` in the compiler, and `WorkshopBall` in the whitelist.*

As a player,  
I want the Bowling Ball to keep its heavy feel,  
So that the last ball record leaves the engine.

**Acceptance Criteria:**
- **Given** a Bowling Ball placed through real UI, **When** run, **Then** its 0.28 m, 4 kg declared material gives golden-equal rows, and a Basketball on the same bench keeps its own material (control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-014`, `cat-015a`, `cat-052` and `cat-005` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.32: Generic Instance — Domino (CAT-023)
*Status: Backlog*

*Depends on: Story 9A.30. Audit: A5, A6.*

*Deletes: `WorkshopDomino` and `DominoMaterial` (`engine/gpu/WorkshopDomino.cs:22`), `WorkshopInput.Domino`, `OfType<WorkshopDomino>` in the compiler, the `instance is WorkshopDomino` arm in `ActivationNetwork.Compile`, and `WorkshopDomino` in the whitelist.*

As a player,  
I want dominoes to stand, topple and trigger exactly as before,  
So that the orientation sensor is selected by declaration only.

**Acceptance Criteria:**
- **Given** a domino row built through real UI, **When** the first domino is pushed, **Then** the cascade, the 45° orientation-sensor occurrence and the lamp activation match the golden rows. This story adds the generic orientation-sensor compile.
- **And** an upright domino never fires (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-023a` and `cat-023b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.33: Generic Instance — Pinball Bumper (CAT-015)
*Status: Backlog*

*Depends on: Story 9A.30. Audit: A5, A6, A11.*

*Deletes: `WorkshopBumper` and `BumperWork` (`engine/gpu/WorkshopBumper.cs:36`), `WorkshopInput.Bumper`, the Bumper panel in `ui/WorkshopConfiguration.cs:12-16`, and `WorkshopBumper` in the whitelist.*

As a player,  
I want the Bumper's strength set through the generic editor,  
So that its finite work store is declared data.

**Acceptance Criteria:**
- **Given** a Bumper configured through the generic editor, **When** balls strike it, **Then** paid and unpaid contacts and the energy ledger match the golden rows and the Story 9.4b repaired behaviour.
- **And** a later empty contact spends nothing (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-015a`, `cat-015a-sidekick` and `cat-015b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.34: Generic Instance — Springboard (CAT-062)
*Status: Backlog*

*Depends on: Story 9A.30. Audit: A5, A6, A11.*

*Deletes: `WorkshopSpringboard` and `SpringboardSettings` (`engine/gpu/WorkshopSpringboard.cs:17`), `WorkshopInput.Springboard`, `OfType<WorkshopSpringboard>` in the compiler, the Springboard panel in `ui/WorkshopConfiguration.cs`, and `WorkshopSpringboard` in the whitelist.*

As a player,  
I want to set the Springboard's preload through the generic editor,  
So that launches come from declared slider and spring data.

**Acceptance Criteria:**
- **Given** a Springboard configured through the generic editor, **When** a ball lands on it, **Then** the preload and launch routes match the golden rows.
- **And** zero preload gives no launch (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-062` and `cat-062b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.35: Generic Instance — Impact Switch (CAT-063)
*Status: Backlog*

*Depends on: Story 9A.30. Audit: A5, A6.*

*Deletes: `WorkshopSwitch` (`engine/gpu/WorkshopActivationParts.cs:13`) and the element-specific use of `ContactTriggerSettings`, which becomes a declared threshold parameter. Also deletes `WorkshopInput.Switch`, the `is WorkshopSwitch` arms in the compiler (around lines 96 and 115–150) and in `ActivationNetwork.Compile`, and `WorkshopSwitch` in the whitelist.*

As a player,  
I want the Impact Switch to latch on a qualifying hit exactly as before,  
So that contact triggers are selected by declaration only.

**Acceptance Criteria:**
- **Given** a switch wired to a lamp through real UI, **When** a ball hits at or above the declared threshold, **Then** the switch latches once and the lamp lights. This story adds the generic contact-trigger compile.
- **And** a below-threshold hit does nothing (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-037`, `anim-1b`, `anim-1c` and `engine-core-2c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.36: Generic Instance — Signal Lamp (CAT-035)
*Status: Backlog*

*Depends on: Story 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopLamp` (`engine/gpu/WorkshopActivationParts.cs:25`), `WorkshopInput.Lamp`, the lamp arms in the compiler and `ActivationNetwork.Compile`, and `WorkshopLamp` in the whitelist.*

As a player,  
I want the lamp to light on its first input exactly as before,  
So that its sticky first-input node is selected by declaration.

**Acceptance Criteria:**
- **Given** a lamp wired from a switch, **When** the switch fires, **Then** the lamp lights and stays lit (declared activation node kind, renamed in Story 9.4 per review-9-4 F9).
- **And** an unwired lamp stays dark (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-023b`, `anim-1b` and `anim-1c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.37: Generic Instance — Ball Detector (CAT-002)
*Status: Backlog*

*Depends on: Story 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopBallDetector` (`engine/gpu/WorkshopActivationParts.cs:37`), `WorkshopInput.BallDetector`, its `CaptureInstance` registry/collider lookup, the detector arm in `ActivationNetwork.Compile`, and `WorkshopBallDetector` in the whitelist.*

As a player,  
I want the Ball Detector to count each crossing once, with directional rearm, exactly as before,  
So that aperture sensors are selected by declaration.

**Acceptance Criteria:**
- **Given** a detector on a pipe route, **When** balls cross, **Then** there is one occurrence per crossing and a reverse crossing does not rearm (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-002` and `cat-020` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.38: Generic Instance — Delay Box (CAT-022)
*Status: Backlog*

*Depends on: Story 9A.35. Audit: A5, A6, A11.*

*Deletes: `WorkshopDelay` and `DelayDuration` (`engine/gpu/WorkshopDelay.cs:36`), `WorkshopInput.Delay`, the Delay panel in `ui/WorkshopConfiguration.cs`, the delay arm in `ActivationNetwork.Compile`, and `WorkshopDelay` in the whitelist.*

As a player,  
I want to set the Delay through the generic editor,  
So that timers are selected by declaration.

**Acceptance Criteria:**
- **Given** a Delay between a switch and a lamp, **When** the switch fires, **Then** the lamp lights after exactly the declared duration (this story adds the generic timer compile), and it does not light earlier (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `anim-1b`, `anim-1c` and `cat-062` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.39: Generic Instance — Battery (CAT-005)
*Status: Backlog*

*Depends on: Stories 9A.19 and 9A.35. Audit: A5, A6, A11.*

*Deletes:*
- *`WorkshopBattery` (`engine/gpu/WorkshopBattery.cs:18`) and `WorkshopInput.Battery`;*
- *`ui/WorkshopBattery.cs` (whole file) and the Battery panel in `ui/WorkshopConfiguration.cs`;*
- *the `PartId.Battery` visibility rule (`ui/WorkshopGuidance.cs:273`) and `BrowserWorkshopClient.Electrical.cs:29`;*
- *`WorkshopBattery` in the whitelist.*

As a player,  
I want to enable or disable a Battery through the generic editor, including during a run,  
So that power sources are selected by declaration.

**Acceptance Criteria:**
- **Given** a Battery wired to a Powered Gate, **When** I toggle its declared boolean `enabled` parameter in the editor, **Then** a typed `ConfigureElectrical` command with a receipt changes the supply and the gate responds.
- **And** a disabled battery powers nothing (negative control).
- **And** the editor offers this one parameter during Run because the declaration marks it run-editable, not because of an element check.
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-005`, `cat-005-n19`, `cat-051`, `cat-051-interrupt` and `cat-037` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.40: Generic Instance — Pressure Plate (CAT-052)
*Status: Backlog*

*Depends on: Stories 9A.20 and 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopPressurePlate` (`engine/gpu/WorkshopPressurePlate.cs:5`), `WorkshopInput.PressurePlate`, `OfType<WorkshopPressurePlate>` in the compiler, and `WorkshopPressurePlate` in the whitelist.*

As a player,  
I want the plate's minimum mass set through the generic editor,  
So that the load sensor is fully declared.

**Acceptance Criteria:**
- **Given** a plate with a declared minimum mass, **When** a heavier ball rests on it, **Then** the plate activates. A lighter ball does not (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-052` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.41: Generic Instance — Pulse Counter (CAT-020)
*Status: Backlog*

*Depends on: Stories 9A.22 and 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopCounter` and `DefaultTargetCount` (`engine/gpu/WorkshopCounter.cs:5`), `WorkshopInput.Counter`, `OfType<WorkshopCounter>` in the compiler, the counter arm in `ActivationNetwork.Compile`, and `WorkshopCounter` in the whitelist.*

As a player,  
I want the Counter's target set through the generic editor,  
So that counting is selected by declaration.

**Acceptance Criteria:**
- **Given** a Counter with target N = 3, **When** three balls cross its detector, **Then** it emits once on the third (a `ThresholdReached` ring record). Two crossings emit nothing (negative control).
- **And** Reset clears the count.
- **And** Save/Load is exact.
- **And** `cat-020` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.42: Generic Instance — System Clock (CAT-017)
*Status: Backlog*

*Depends on: Story 9A.38. Audit: A5, A6, A11.*

*Deletes: `WorkshopClock` and `DefaultIntervalSeconds` (`engine/gpu/WorkshopClock.cs:5`), `WorkshopInput.Clock`, the Clock panel in `ui/WorkshopConfiguration.cs`, `OfType<WorkshopClock>` in the compiler, the clock arm in `ActivationNetwork.Compile`, and `WorkshopClock` in the whitelist.*

As a player,  
I want the Clock's interval set through the generic editor,  
So that periodic pulses are selected by declaration.

**Acceptance Criteria:**
- **Given** a 1.0 s Clock, **When** it runs for 5.0 s, **Then** it emits exactly 5 pulses.
- **And** Pause halts emission and Reset restores phase 0 (control).
- **And** Save/Load is exact.
- **And** `cat-017` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.43: Generic Instance — State Latch (CAT-037)
*Status: Backlog*

*Depends on: Stories 9A.22 and 9A.27–9A.42; it is the last record conversion, so it removes the whole whitelist. Audit: A5, A6.*

*Deletes:*
- *`WorkshopLatch` (`engine/gpu/WorkshopLatch.cs:7`) and `WorkshopInput.Latch`;*
- *the latch-only Latched→Clear admission (`is WorkshopLatch` at `engine/gpu/WorkshopSimulation.cs:57` and `:459`), replaced by a declared memory capability;*
- *`OfType<WorkshopLatch>` in the compiler and the latch arm in `ActivationNetwork.Compile`;*
- *with the last record gone, the whole `WorkshopInstances.Validate` type whitelist (`engine/gpu/WorkshopInstances.cs:95`).*

As a player,  
I want the State Latch to hold memory, switch supply and drive its ActivationOut exactly as before,  
So that the last element record leaves the engine.

**Acceptance Criteria:**
- **Given** a latch with Set, Reset, supply and ActivationOut wired through real UI, **When** Set and Reset pulses arrive, **Then** memory, the switched supply and the output follow Story 9.4's acceptance.
- **And** same-tick Set and Reset resolve to Reset (control).
- **And** the generic `Clear` command is admitted for any instance that declares the memory capability and rejected for every other instance (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `rg "Workshop(Ball|Receiver|Ramp|Switch|Lamp|Wall|Delay|Bumper|Domino|Battery|Springboard|PressurePlate|Counter|Clock|Latch|BallDetector)\b"` returns 0 production hits.
- **And** `cat-037`, `cat-005-n19` and `cat-051-interrupt` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.44: f32 Lane Completion — Remove Every binary16 Lane
*Status: Backlog*

*Depends on: Story 9A.43. It is placed after the stories that delete the largest `Half` holdings (`WorkshopCanonicalColliders` in 9A.16, `MachinePart` bindings and `CosmeticCurves` in 9A.25, the puzzle lanes in 9A.26) so that no lane is migrated and then deleted. Owner decision, 18 Oct 2026. Audit: B6. Resolves CAT-037 open question 2.*

*Deletes every binary16 lane, with no conversion shim:*
- *`System.Half` in every production and test `.cs` file (69 production files at audit time). This includes:*
  - *`Metres`, `Kilograms`, `Acceleration`, `InverseSeconds`, `Restitution` and `HalfBits` (`engine/gpu/WorkshopConstruction.cs:11-15`, `:335`);*
  - *`PhysicsDeclarations.cs`, `RigidMassProperties.cs`, `OrientationSensorDeclaration.cs`, `PhysicsGpuAbi.cs`, `PhysicsMotionRead.cs` and `WorkshopPhysicsCompiler.cs`;*
  - *`WorkshopSaveCodec.cs`, `ActivationTimers.cs`, `ActivationNetwork.cs` (`(Half)0` at `:385`), `ContactWorkDeclaration.cs`, `WorkshopPuzzle.cs` and `WorkshopPuzzleWire.cs`;*
  - *`engine/presentation/AnimationValues.cs`, `WorkshopCosmetic.cs`, `WorkshopHint.cs` and `ContactTriggerSettings`;*
  - *the port and pose vectors.*
- *`decodeFloat16` in `CuriousContraptions.web/wwwroot/workshop-client.js` and any float16 decode in `tools/e2e/workshop-wire.ts`;*
- *every half-width field in `schemas/*.fbs`.*

As a player,  
I want every game value stored and computed in one precision,  
So that sizes, masses, timings and animations behave identically everywhere.

**Acceptance Criteria:**
- **Given** the tree, **When** this story lands, **Then** `rg -w "Half|HalfBits|Float16|float16|getF16|setF16"` over the active tree, excluding `archives/` and `node_modules/`, returns 0 hits.
- **And** every former lane carries IEEE-754 f32 end to end: authoring, construction, wire, save, solver, read model and animation.
- **And** the schema, save and trace versions are bumped, and older saves are rejected explicitly with no migration.
- **And** the `docs/gpu-f32-physics.md` "f32 Migration Status" table records no remaining lane, and the envelope statements that depended on binary16 are restated for f32.
- **And** golden tests show every element's outcomes stay inside the [game-grade envelope](../../docs/gpu-f32-physics.md#game-grade-envelope).
- **And** an injected non-finite value is rejected at the boundary (negative control).
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.45: Compile-Time Element Isolation Gate
*Status: Backlog*

*Depends on: Stories 9A.1–9A.44. Audit: A1, A2 (constants).*

*Deletes:*
- *every element-named `PartId` constant (`engine/PartId.cs:20-44`);*
- *the wholesale `Compile Include="../engine/gpu/*.cs"` and the `engine/PartId.cs` link in `CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj:28` and `CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj:28`;*
- *any element-specific production code still found by the scan below.*

As the owner,  
I want the build to fail when anyone (person or agent) adds element-specific code outside `elements/`,  
So that project-per-element, command/query and the event ring stay the only possible design.

**Acceptance Criteria:**
- **Given** the solution, **When** it builds, **Then** both workers reference a new element-free `CuriousContraptions.Core` assembly (capability tables, solver contracts, the capability-table codec, the activation network and the event ring) instead of compiling `engine/gpu` wholesale. Authoring, catalogue, lessons and the bundle loader are in a main-thread-only assembly that neither worker references.
- **And** `PartId` is an opaque typed id with no named members, so `PartId.Wall` no longer compiles.
- **And** the `BundleElements` build target also runs an isolation scan that fails the build, citing file and line, for any of the following in production source outside `elements/**`:
  - any bundle element id as a string literal;
  - any `PartId.<Name>` member access;
  - an identifier built from a package name (`Workshop<Package>`, `<Package>Settings`, `<Package>Dimensions`, `<Package>Lessons`, `CosmeticCurves.<Package>`).
  
  The id list comes from the generated bundle; there is no hand list.
- **And** the scan covers `engine/**`, `ui/**`, every `CuriousContraptions.*/**` project except `CuriousContraptions.tests`, `CuriousContraptions.web/wwwroot/**` and `CuriousContraptions.Simulation/wwwroot/**`.
- **And** the scan explicitly excludes, with these reasons:
  - `elements/**`: the owners;
  - `CuriousContraptions.tests/**` and `tools/e2e/**`: consumers that build machines by id or through the UI;
  - `content/**`: level data names elements by design;
  - `tools/Coverage/**`: a planning-coverage tool over all 72 catalogue specs that does not ship;
  - `docs/**`, `_bmad-output/**` and `archives/**`.
- **And** a test plants an element-id literal and, separately, a `Workshop<Package>` type in a temporary engine file, and the build fails with file:line for each (negative control). The unchanged tree builds clean (positive).
- **And** the Story 9A.0 consistency test now asserts that the isolation doc's Current status section lists no open finding.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

---

## Epic 10: Constrained Mechanics & Elastic Fixtures

**Goal:** Deliver the solid cylinder collider and inertia, compliant elastic fixtures, pivots, pulleys, ropes, and counterweights using unified TGS Soft constraints without ad-hoc velocity projections or Baumgarte stabilization.

### Story 10.1: Solid Cylinder Collider & Cylinder Inertia (ENGINE-CYLINDER)
*Status: Backlog*

*Depends on: Stories 1.4 (BVH and SAT narrowphase) and 5.1 (dynamic rigid body and inertia compile). New story (owner decision, 9 Oct 2026), placed before its first consumer, the Story 10.2 Pulley wheel; the owner chose cylinders for both the Pulley wheel and the Story 10.3 Lever fulcrum (Open scheduling item e, decided 18 Oct 2026). ENGINE-CYLINDER is a roadmap-only ID, not yet in `docs/planning/requirements.md`.*

As a player,  
I want shaft wheels, rollers and pivot posts to collide and turn as true solid cylinders,  
So that wheeled and pivoted parts move with their real shape and mass.

**Acceptance Criteria:**
- **Given** a solid cylinder declared by radius, width and mass, built through real UI controls on its first consuming element or on an explicitly selectable authored qualification fixture,
- **When** a ball strikes its curved side and its flat end, and a dynamic cylinder falls onto the workbench,
- **Then** contact follows the true curved side and flat end faces against spheres, boxes and the workbench plane, with no sphere, box or prism-hull stand-in.
- **And** the dynamic cylinder rolls on its side and rests on an end face without jitter.
- **And** cylinder inertia is compiled from the declaration: axial $m r^2 / 2$, transverse $m (3 r^2 + w^2) / 12$.
- **And** the collider shape set gains Cylinder as a closed enum value end-to-end, and a non-positive or non-finite radius, width or mass is rejected atomically before any state changes.
- **And** this is engine capability only, delivered as an engine slice or through one consuming element's slice; no element identifier appears in the physics solver.
- **And** it serves the shaft wheels of CAT-042 Motor (`docs/planning/elements/CAT-042-motor.md`), CAT-019 Conveyor (`docs/planning/elements/CAT-019-conveyor.md`), CAT-018 Clutch (`docs/planning/elements/CAT-018-clutch.md`), CAT-057 Reverse transmission (`docs/planning/elements/CAT-057-reverse_transmission.md`) and CAT-071 Wound spring (`docs/planning/elements/CAT-071-wound_spring.md`), the CAT-053 Pulley wheel (`docs/planning/elements/CAT-053-pulley.md`) and the CAT-034 Impact lever solid fulcrum (`docs/planning/elements/CAT-034-impact_lever.md`) (owner decision, 18 Oct 2026); and, if the owner selects a cylinder for them, the CAT-067 Weight body (`docs/planning/elements/CAT-067-weight.md`) and CAT-069 Wind chimes tubes (`docs/planning/elements/CAT-069-wind_chimes.md`).
- **And** the legacy 24-sided prism-hull shaft (`engine/SceneRotaryShaft.cs`) is deleted, or confirmed already purged by Story 7.4; no hull fallback is introduced.
- **And** exact Reset and Save/Load restore the cylinder poses.
- **And** `tools/e2e/engine-cylinder.test.ts` passes 100% in Chrome serially.

### Story 10.2: Pulley Wheel & Tensile Rope Dynamics (CAT-053, CAT-058)
*Status: Backlog*

*Depends on: Story 10.1 (cylinder wheel collider; owner decision 18 Oct 2026, CAT-053 open question 1). Moved from Story 10.3, ahead of the Lever whose end rope sockets need the ropes. The Counterweight body (CAT-067) ships with Story 10.2 per CAT-067 §3 and is the rope load in this acceptance (Open scheduling item f, decided 18 Oct 2026); the Lever's rope-load integration is proven in Story 10.3.*

As a player,  
I want to route ropes around Pulleys to transmit tensile forces,  
So that dropping one weight can lift an object in a different part of the workbench.

**Acceptance Criteria:**
- **Given** a rope routed through two Pulleys between two Counterweights of unequal mass,
- **When** the heavier counterweight falls under gravity,
- **Then** tensile forces transmit through the rope, lifting the lighter counterweight; equal masses stay balanced.
- **And** slack ropes apply zero force; cutting or releasing tension uncouples the bodies.
- **And** `tools/e2e/cat-053-058.test.ts` passes 100% in Chrome serially.

### Story 10.3: Balanced Impact Lever & Pivot Fulcrum (CAT-034)
*Status: Backlog*

*Depends on: Stories 6.2 (Bumper obstacle), 10.1 (solid cylinder fulcrum; owner decision 18 Oct 2026, CAT-034 open question 2) and 10.2 (ropes for its end rope sockets; CAT-034 §3). Moved from Story 10.2 (owner dependency rule, 9 Oct 2026: after ropes). It carries the Lever rope-load integration moved out of Story 10.2 (Open scheduling item f, decided 18 Oct 2026).*

As a player,  
I want an Impact Lever to pivot smoothly when struck on one arm,  
So that see-saw mechanisms can fling objects or lift connected ropes.

**Acceptance Criteria:**
- **Given** an Impact Lever with a payload resting on one arm,
- **When** a heavy Bowling Ball drops onto the opposing arm,
- **Then** the lever pivots around its fulcrum, strikes its end stop, and launches the payload upward.
- **And** an equal load placed on both arms remains balanced indefinitely.
- **And** the lever pivots on a solid cylinder fulcrum (Story 10.1), not an art-only post.
- **And** with a rope routed through two Pulleys from a falling Counterweight to a lever end rope socket, rope tension lifts that lever arm (the rope-load integration moved from Story 10.2).
- **And** `tools/e2e/cat-034.test.ts` passes 100% in Chrome serially.

### Story 10.4: Counterweight & Multi-Body Pendulum Lifting (CAT-067)
*Status: Backlog*

*Depends on: Story 10.2 (rope anchor and tension-only rope). Number unchanged; the Pulley moved up to Story 10.2 and the Trampoline now follows this story.*

As a player,  
I want Counterweights to swing as pendulums and balance moving loads,  
So that gravitational energy can be harnessed smoothly.

**Acceptance Criteria:**
- **Given** a Counterweight suspended as a 3D pendulum,
- **When** released from an angle,
- **Then** it oscillates with natural period $T = 2\pi\sqrt{L/g}$ and damps gradually.
- **And** exact Reset restores initial angular displacement and zero velocity.
- **And** `tools/e2e/cat-067.test.ts` passes 100% in Chrome serially.

### Story 10.5: Trampoline Compliant Membrane Dynamics (CAT-065)
*Status: Backlog*

*Depends on: Stories 6.4 (elastic spring constraint), 6.6–6.7 (pipe for the drop-to-pipe lesson), 10.2 (ropes) and 10.4 (the Weight for the tether controls; CAT-065 §3). Moved from Story 10.1 (owner decision, 9 Oct 2026: after ropes and the Weight).*

As a player,  
I want dropped objects to bounce high off a Trampoline,  
So that contraptions can launch payloads across vertical obstacles.

**Acceptance Criteria:**
- **Given** a Trampoline placed under a falling Basketball,
- **When** impact occurs,
- **Then** the membrane deflects with compliant stiffness and launches the ball upwards.
- **And** off-center hits deflect at an angle according to surface normal.
- **And** total kinetic energy after rebound does not exceed initial potential energy (no artificial gain).
- **And** `tools/e2e/cat-065.test.ts` passes 100% in Chrome serially.

---

## Epic 11: Rotary Drive & Mechanical Actuators

**Goal:** Implement electric motors, conveyors, clutches, reversing transmissions, linear pushers, wound springs, and toy cannons as declarative actuators.

### Story 11.1: Electric Motor & Continuous Tangential Conveyor Belt (CAT-042, CAT-019)
*Status: Backlog*

As a player,  
I want an Electric Motor to drive a Conveyor Belt,  
So that resting objects are transported horizontally across the workbench.

**Acceptance Criteria:**
- **Given** an Electric Motor coupled to a Conveyor Belt receiving 12V battery power,
- **When** a Basketball lands on the conveyor surface,
- **Then** tangential friction accelerates the ball along the belt travel direction.
- **And** cutting motor power decelerates the belt to a halt under friction.
- **And** `tools/e2e/cat-042-019.test.ts` passes 100% in Chrome serially.

### Story 11.2: Mechanical Clutch & Reverse Transmission (CAT-018, CAT-057)
*Status: Backlog*

As a player,  
I want to engage clutches to connect axles and reverse transmissions to invert rotation,  
So that complex mechanical drivetrains can be governed.

**Acceptance Criteria:**
- **Given** a motor driving a shaft through a Reverse Transmission and Clutch,
- **When** the clutch is engaged, the output shaft rotates in the reversed direction.
- **When** the clutch is disengaged via signal, the output shaft freewheels to a stop.
- **And** `tools/e2e/cat-018-057.test.ts` passes 100% in Chrome serially.

### Story 11.3: Linear Pusher Telescopic Actuator (CAT-039)
*Status: Backlog*

As a player,  
I want a Linear Pusher to extend upon signal activation,  
So that objects in front of it are forcibly pushed forward.

**Acceptance Criteria:**
- **Given** a Linear Pusher with configured stroke 1.0 m wired to a switch,
- **When** the switch activates,
- **Then** the pusher head extends at rated speed and pushes a resting Bowling Ball forward.
- **And** upon reaching maximum stroke, extension halts and holds position.
- **And** `tools/e2e/cat-039.test.ts` passes 100% in Chrome serially.

### Story 11.4: Wound Spring Potential Motor & Toy Cannon Launcher (CAT-071, CAT-016)
*Status: Backlog*

*Depends on: Stories 6.4–6.7 (slider, preload store and annular colliders), 8.1, 10.1 (shaft cylinder), 11.1 and 11.2 (shaft drive and ratio coupling); the cannon feeder integration also uses Stories 8.2, 9.3 and 9.4. Renumbered from Story 11.5 after the Powered Gate moved to Story 8.2.*

As a player,  
I want to fire Toy Cannons and wind up Spring Motors,  
So that high-energy mechanical impulses can launch balls across wide gaps.

**Acceptance Criteria:**
- **Given** a loaded Toy Cannon triggered by an activation wire,
- **When** triggered,
- **Then** the loaded ball launches with high muzzle velocity ($12\text{ m/s}$) along barrel trajectory.
- **And** uncharged or unloaded cannons fire nothing.
- **And** `tools/e2e/cat-071-016.test.ts` passes 100% in Chrome serially.

---

## Epic 12: Conserved Airflow, Pneumatics & Buoyancy

**Goal:** Deliver aerodynamic airflow fields, pneumatic pressure pulses, and buoyant gas dynamics conforming strictly to energy conservation laws.

### Story 12.1: Lightweight Tennis Ball & Floating Balloon Buoyancy (CAT-064, CAT-003)
*Status: Backlog*

*Note (18 Oct 2026, Open scheduling item h): Story 9A.14 removed Tennis and Balloon from the palette because neither could be constructed and neither had a package. This story creates their `elements/TennisBall` and `elements/Balloon` packages (a declaration, scene, icon and parameter schema for each, in the bundle) and so returns them to the palette.*

As a player,  
I want lightweight Tennis Balls to bounce briskly and Balloons to float upward,  
So that aerodynamic and buoyant contraptions are possible.

**Acceptance Criteria:**
- **Given** a Floating Balloon placed on the workbench,
- **When** released,
- **Then** buoyant forces overcome gravity and the balloon rises vertically until contacting a ceiling or wall.
- **And** a Tennis Ball rebounds with high restitution ($e = 0.85$) and decelerates under aerodynamic drag.
- **And** `tools/e2e/cat-064-003.test.ts` passes 100% in Chrome serially.

### Story 12.2: Electric Fan Aerodynamic Airflow Jet (CAT-028)
*Status: Backlog*

As a player,  
I want an Electric Fan to blow light objects across the workbench,  
So that contactless aerial steering can guide balls into goals.

**Acceptance Criteria:**
- **Given** an Electric Fan powered by a battery aimed across the path of a falling Tennis Ball,
- **When** the ball enters the fan's conical airflow cone,
- **Then** aerodynamic drag forces deflect the ball horizontally into the target receiver.
- **And** heavy Bowling Balls experience negligible deflection (mass-scaled drag).
- **And** `tools/e2e/cat-028.test.ts` passes 100% in Chrome serially.

### Story 12.3: Windmill Rotor Aerodynamic Capture & Drive (CAT-070)
*Status: Backlog*

*Depends on: Stories 6.6–6.7 (annular guard collider), 10.3 (hinge), 11.1–11.2 (shaft output and loads) and 12.2 (airflow). Moved from Story 12.4, ahead of the Bellows that needs it.*

As a player,  
I want a Windmill to spin when struck by fan airflow,  
So that airflow can be converted into mechanical shaft work.

**Acceptance Criteria:**
- **Given** a Windmill positioned in the airflow of an active Electric Fan,
- **When** airflow strikes the rotor blades,
- **Then** the windmill spins its axle and delivers rotational torque to connected mechanical components.
- **And** blocking the fan airflow with a wall stops windmill rotation.
- **And** `tools/e2e/cat-070.test.ts` passes 100% in Chrome serially.

### Story 12.4: Pneumatic Bellows Compression Pulse (CAT-010)
*Status: Backlog*

*Depends on: Stories 6.4 (slider and return spring), 11.1 (Conveyor), 12.2 (airflow) and 12.3 (Windmill); CAT-010 §3 and §4 fact 5. Moved from Story 12.3 (owner decision, 9 Oct 2026: after Windmill and Conveyor).*

As a player,  
I want a Pneumatic Bellows to emit a focused gust of air when compressed,  
So that impacting bodies can trigger secondary pneumatic launches.

**Acceptance Criteria:**
- **Given** a Bellows struck by a falling Bowling Ball,
- **When** the plates compress,
- **Then** a high-velocity directional air jet discharges from the nozzle, propelling an adjacent balloon or tennis ball.
- **And** uncompressed bellows discharge zero airflow.
- **And** `tools/e2e/cat-010.test.ts` passes 100% in Chrome serially.

---

## Epic 13: Optical Propagation, Routing & Sensing

**Goal:** Implement raymarched optical propagation, planar reflections, spectral color bandpass filtering (RGB), and optical logic gates as a discrete, deterministic optical graph. The mechanical Beam Shutter joins this epic after the lasers and receivers it switches.

### Story 13.1: Flashlight Torch & Collimated Laser Emitters (CAT-029, CAT-036)
*Status: Backlog*

As a player,  
I want Flashlights to emit divergent light cones and Lasers to emit straight collimated beams,  
So that optical puzzles have both wide-area and pinpoint illumination sources.

**Acceptance Criteria:**
- **Given** a Flashlight and Laser placed in the Workshop,
- **When** powered,
- **Then** the laser projects an infinite collimated beam; the flashlight projects an attenuated $35^\circ$ cone.
- **And** opaque obstacles (walls, bodies) cast crisp geometric shadows blocking downstream transmission.
- **And** `tools/e2e/cat-029-036.test.ts` passes 100% in Chrome serially.

### Story 13.1b: Red Laser Emitter (EL-176)
*Status: Backlog*

*Depends on: Stories 8.1 (supply) and 13.1 (the collimated laser emission capability, CAT-036). New story (coordinator decision under owner delegation, 18 Oct 2026; Open scheduling items a and d). Per [CAT-036](../../docs/planning/elements/CAT-036-laser.md), EL-176 is a separate named identity with its own channel; it does not fold into the amber laser. This story introduces the declared emission-variant mechanism: a spectral channel value declared in the package and consumed by the shared laser emission capability. Stories 13.1c and 13.1d reuse it.*

As a player,  
I want a red laser,  
So that I can send a pure red beam to a matching receiver or filter.

**Acceptance Criteria:**
- **Given** a Red Laser (its own package, `elements/RedLaser`) placed and wired to a Battery through real UI controls (no setters or numeric placement),
- **When** it is powered,
- **Then** it projects a collimated beam carrying only the red channel, drawn in the approved-palette red per DESIGN.md.
- **And** the red channel is a declared emission variant of the shared laser emission capability. No new solver code is added, and `rg` finds no channel- or element-specific branch in the optical solver.
- **And** an unpowered or disabled supply produces no beam, and an opaque Wall blocks the beam (negative controls).
- **And** Run/Reset restores the unpowered emitter exactly, and Save/Load preserves pose, channel and wiring.
- **And** `tools/e2e/el-176.test.ts` passes 100% in Chrome serially.

### Story 13.1c: Green Laser Emitter (EL-177)
*Status: Backlog*

*Depends on: Story 13.1b (the declared emission-variant mechanism). New story (coordinator decision under owner delegation, 18 Oct 2026; Open scheduling items a and d). A separate named identity per CAT-036.*

As a player,  
I want a green laser,  
So that I can send a pure green beam to a matching receiver or filter.

**Acceptance Criteria:**
- **Given** a Green Laser (its own package, `elements/GreenLaser`) placed and wired to a Battery through real UI controls,
- **When** it is powered,
- **Then** it projects a collimated beam carrying only the green channel, drawn in the approved-palette green per DESIGN.md.
- **And** the package reuses Story 13.1b's declared emission-variant mechanism with the green channel value. No optical solver or emission-capability source file changes.
- **And** a Red Laser on the same bench keeps its red channel (control), and an unpowered supply or an opaque Wall leaves no beam downstream (negative controls).
- **And** Run/Reset restores the unpowered emitter exactly, and Save/Load preserves pose, channel and wiring.
- **And** `tools/e2e/el-177.test.ts` passes 100% in Chrome serially.

### Story 13.1d: Blue Laser Emitter (EL-178)
*Status: Backlog*

*Depends on: Story 13.1b (the declared emission-variant mechanism). New story (coordinator decision under owner delegation, 18 Oct 2026; Open scheduling items a and d). A separate named identity per CAT-036.*

As a player,  
I want a blue laser,  
So that I can send a pure blue beam to a matching receiver or filter.

**Acceptance Criteria:**
- **Given** a Blue Laser (its own package, `elements/BlueLaser`) placed and wired to a Battery through real UI controls,
- **When** it is powered,
- **Then** it projects a collimated beam carrying only the blue channel, drawn in the approved-palette blue per DESIGN.md.
- **And** the package reuses Story 13.1b's declared emission-variant mechanism with the blue channel value. No optical solver or emission-capability source file changes.
- **And** Red and Green Lasers on the same bench keep their channels (control), and an unpowered supply or an opaque Wall leaves no beam downstream (negative controls).
- **And** Run/Reset restores the unpowered emitter exactly, and Save/Load preserves pose, channel and wiring.
- **And** `tools/e2e/el-178.test.ts` passes 100% in Chrome serially.

### Story 13.2: Broadband & Pure Channel Optical Receivers (CAT-038, CAT-056, CAT-032, CAT-012)
*Status: Backlog*

*Depends on: Stories 8.1 (switched contact), 8.2 (electrical load), 13.1 (laser source) and 13.1b–13.1d (the red, green and blue lasers are the pure sources for the pure-channel receivers; Open scheduling items a and d, decided 18 Oct 2026). Moved from Story 13.4 (owner decision, 9 Oct 2026: receivers before the mirror and filter stories that need them as observers).*

As a player,  
I want Optical Receivers that respond only to specific colors,  
So that color-tuned optical triggers can activate designated circuits.

**Acceptance Criteria:**
- **Given** a Red Receiver illuminated by red, green, and blue beams successively,
- **Then** only the red beam triggers electrical activation; green and blue produce zero response.
- **And** Broadband Receivers activate under any light exceeding power threshold.
- **And** `tools/e2e/cat-receivers-primary.test.ts` passes 100% in Chrome serially.

### Story 13.3: Beam Shutter Mechanical Guillotine (CAT-007)
*Status: Backlog*

*Depends on: Stories 6.4 (slider), 8.1, 8.2 (bounded slider drive), 10.2 (rope opening), 13.1 (laser) and 13.2 (light receiver); CAT-007 §3. Moved from Story 11.6 (owner decision, 9 Oct 2026: into Epic 13 after the lasers).*

As a player,  
I want a Beam Shutter to block or unblock light paths upon mechanical activation,  
So that optical circuits can be switched physically.

**Acceptance Criteria:**
- **Given** a Beam Shutter positioned across a laser path,
- **When** closed, the laser beam is blocked; when opened by signal or rope, the beam passes through cleanly.
- **And** `tools/e2e/cat-007.test.ts` passes 100% in Chrome serially.

### Story 13.4: Spectral Color Bandpass Filters (Red, Green, Blue) (CAT-055, CAT-031, CAT-011)
*Status: Backlog*

*Depends on: Stories 13.1 (laser source), 13.1b–13.1d (coloured beams for the pass and extinguish checks) and 13.2 (pure-channel receivers as observers). Moved from Story 13.3.*

As a player,  
I want Color Filters to filter out non-matching wavelengths,  
So that multi-chromatic light beams can be separated into pure primary colors.

**Acceptance Criteria:**
- **Given** a white light beam passing through a Red Filter,
- **When** the beam exits the filter,
- **Then** only red spectral energy is transmitted; green and blue are absorbed.
- **And** passing the red beam into a Green Filter completely extinguishes the beam (zero transmission).
- **And** `tools/e2e/cat-filters.test.ts` passes 100% in Chrome serially.

### Story 13.5: Planar Reflection Mirror, Beam Splitter & Beam Combiner (CAT-041, CAT-008, CAT-006)
*Status: Backlog*

*Depends on: Stories 13.1 (laser), 13.2 (receivers as observers) and 13.4 (filters make the distinct channels the Combiner merges; CAT-006 §3). Moved from Story 13.2.*

As a player,  
I want Mirrors to bounce beams and Splitters/Combiners to divide and merge light paths,  
So that optical rays can be routed around obstacles.

**Acceptance Criteria:**
- **Given** a laser directed into a $45^\circ$ Flat Mirror,
- **When** the beam strikes the mirror surface,
- **Then** it reflects at an exact $90^\circ$ right angle.
- **And** Beam Splitters divide incident power 50/50; Beam Combiners merge orthogonal beams into one.
- **And** `tools/e2e/cat-041-008-006.test.ts` passes 100% in Chrome serially.

### Story 13.6: Secondary Spectral Receivers (Cyan, Magenta, Yellow, White) (CAT-021, CAT-040, CAT-072, CAT-068)
*Status: Backlog*

*Depends on: Stories 13.1, 13.2 (single-channel reception), 13.4 (filters) and 13.5 (Combiner mixing). Renumbered from Story 13.5.*

As a player,  
I want Cyan, Magenta, Yellow, and White receivers that require mixed spectral combinations,  
So that advanced color-combining puzzles can be constructed.

**Acceptance Criteria:**
- **Given** a Yellow Receiver,
- **When** both Red and Green beams strike it simultaneously,
- **Then** it activates; a Red-only or Green-only beam fails to activate it.
- **And** White Receiver requires all three primary colors simultaneously.
- **And** `tools/e2e/cat-receivers-secondary.test.ts` passes 100% in Chrome serially.

### Story 13.7: Photovoltaic Solar Panel Energy Conversion (CAT-059)
*Status: Backlog*

*Depends on: Stories 8.1 (network), 11.1 (Motor load) and 13.1 (Flashlight). Renumbered from Story 13.6.*

As a player,  
I want Solar Panels to generate electrical power from incident light,  
So that optical beams can remotely power electrical motors and circuits.

**Acceptance Criteria:**
- **Given** a Solar Panel connected to an Electric Motor,
- **When** illuminated by a Flashlight or Laser,
- **Then** output electrical power energizes the motor.
- **And** blocking the light beam immediately cuts motor power.
- **And** `tools/e2e/cat-059.test.ts` passes 100% in Chrome serially.

### Story 13.8: Optical Logic Gates (AND, NAND, NOR, OR, XOR) (CAT-043 through CAT-047)
*Status: Backlog*

*Depends on: Stories 13.1 (lasers), 13.2 (output observer) and 13.5 (optical routing). Renumbered from Story 13.7.*

As a player,  
I want Optical Logic Gates to perform contactless optical computations,  
So that speed-of-light logic contraptions can operate without electrical wires.

**Acceptance Criteria:**
- **Given** Optical logic gates (AND, NAND, NOR, OR, XOR) illuminated by laser inputs,
- **When** input combinations are tested,
- **Then** emitted output laser beams conform 100% to formal Boolean logic tables.
- **And** `tools/e2e/cat-optical-logic.test.ts` passes 100% in Chrome serially.

---

## Epic 14: Acoustic Emission & Reception

**Goal:** Implement acoustic percussion, directional sound waves, and acoustic resonance sensors as discrete wave occurrences without audio thread blocking.

Delivery order (owner decision, 18 Oct 2026; Open scheduling item c): 14.0, 14.2, 14.1, 14.3, 14.4.

### Story 14.0: Acoustic Propagation, Occlusion & Reception Engine (ENGINE-ACOUSTIC)
*Status: Backlog*

*Depends on: Stories 1.4 (BVH and SAT narrowphase, for occlusion queries) and 9.2b (multi-consumer event ring). New engine story (owner decision, 18 Oct 2026; Open scheduling item c), before the Service Bell (14.2) and every acoustic source. ENGINE-ACOUSTIC is a roadmap-only ID, not yet in `docs/planning/requirements.md`.*

As a player,  
I want sounds to travel through the workshop, fall off with distance and be blocked by solid parts,  
So that acoustic sources and receivers behave consistently wherever I place them.

**Acceptance Criteria:**
- **Given** a declared acoustic emission occurrence (position, direction cone, level, tone band, tick) published on the event ring,
- **When** the engine evaluates reception for every declared acoustic receiver,
- **Then** each receiver gets a level that falls off with distance and cone angle, and is zero when a solid collider occludes the straight path.
- **And** propagation is engine capability only: emissions and receivers are declaration data; no element identifier appears in the propagation code, and nothing flows back from presentation to physics.
- **And** non-finite or out-of-range emission declarations are rejected atomically before any state changes; numerical residuals clamp, never fault the tick.
- **And** reception is deterministic across Run/Reset, and Reset clears every pending emission and received level exactly.
- **And** native tests cover distance falloff, cone edges, occlusion on/off and same-tick multiple emissions; it is proven in Chrome through the first consuming element's slice (Story 14.2) or an explicitly selectable authored qualification fixture, and `tools/e2e/engine-acoustic.test.ts` passes 100% in Chrome serially.

### Story 14.1: Sound Level Meter Threshold Sensor (CAT-060)
*Status: Backlog*

*Depends on: Stories 8.1 (supplied contact), 8.2 (a supplied load), 14.0 (acoustic propagation and reception) and 14.2 (the Service Bell, its acceptance source). Delivered after the Bell (owner decision, 18 Oct 2026; Open scheduling item c). Formerly Story 14.3.*

As a player,  
I want a Sound Level Meter to trigger an electrical contact when sound volume exceeds threshold,  
So that bells and speakers can activate remote circuits without physical wires.

**Acceptance Criteria:**
- **Given** a Sound Level Meter facing a Service Bell,
- **When** the bell rings,
- **Then** the sound meter closes its electrical contact and illuminates a connected lamp.
- **And** placing a solid Wall between bell and meter occludes the acoustic wave, preventing activation.
- **And** `tools/e2e/cat-060.test.ts` passes 100% in Chrome serially.

### Story 14.2: Service Bell Percussion & Resonant Ringing (CAT-009)
*Status: Backlog*

*Depends on: Story 14.0 (acoustic propagation, occlusion and reception; CAT-009 §3). Delivered before the Sound Level Meter (owner decision, 18 Oct 2026; Open scheduling item c). Formerly Story 14.1.*

As a player,  
I want Service Bells to ring with a crisp chime when struck,  
So that physical impacts can produce audible confirmation and trigger acoustic meters.

**Acceptance Criteria:**
- **Given** a Service Bell placed in the path of a rolling ball,
- **When** struck with kinetic energy exceeding threshold,
- **Then** a crisp metallic ring is emitted as a timestamped acoustic event.
- **And** sub-threshold nudges produce no ring.
- **And** `tools/e2e/cat-009.test.ts` passes 100% in Chrome serially.

### Story 14.3: Audio Speaker Directional Pulse Emission (CAT-061)
*Status: Backlog*

*Depends on: Stories 8.1 (supply), 9.3 (System Clock trigger) and 14.0 (propagation and reception; CAT-061 §3). Moved from Story 14.2 (owner decision, 9 Oct 2026: after propagation; 18 Oct 2026: propagation moved to Story 14.0).*

As a player,  
I want an Audio Speaker to emit directional acoustic pressure waves upon signal activation,  
So that electrical pulses can be broadcast acoustically.

**Acceptance Criteria:**
- **Given** an Audio Speaker wired to a system clock,
- **When** energized,
- **Then** directional acoustic pressure waves radiate forward in a $35^\circ$ cone.
- **And** `tools/e2e/cat-061.test.ts` passes 100% in Chrome serially.

### Story 14.4: Resonant Wind Chimes Airflow Percussion (CAT-069)
*Status: Backlog*

As a player,  
I want Wind Chimes to sway and chime in fan airflow,  
So that wind-driven musical contraptions can be solved.

**Acceptance Criteria:**
- **Given** Wind Chimes placed in the airflow of an Electric Fan,
- **When** the airflow strikes the chime clapper,
- **Then** the chime tubes strike each other, producing musical acoustic events.
- **And** calm air produces zero ringing.
- **And** `tools/e2e/cat-069.test.ts` passes 100% in Chrome serially.

---

## Epic 15: 150 Progressively Taught Campaign Levels

**Goal:** Deliver all 150 progressive challenge levels across Chapters 1 to 5 in structured 10-level increments, each verified with real UI solutions, negative controls, and bit-for-bit Reset in actual Chrome.

### Story 15.1: Campaign Levels 1–10 (Chapter 1: "On a Roll", Part 1)
*Status: Backlog*

As a player,  
I want introductory levels teaching ball drops, ramps, switches, lamps, and basic capture,  
So that I learn the core mechanics of Curious Contraptions.

**Acceptance Criteria:**
- **Given** Levels 1 through 10 in Chapter 1 (`first_principles`, `power_trip`, `air_mail`, `spring_forward`, `third_dimension`, `domino_effect`, `spring_signal`, `wind_signal`, `cold_start`, `bumper_sidekick`),
- **When** executed in Chrome via Playwright,
- **Then** each level possesses an admitted inventory, solvable route, and Solved celebration.
- **And** exact Reset restores initial authoring state bit-for-bit.
- **And** `tools/e2e/campaign-01.test.ts` passes 100% in Chrome serially.

### Story 15.2: Campaign Levels 11–20 (Chapter 1: "On a Roll", Part 2)
*Status: Backlog*

As a player,  
I want levels teaching multi-ball trajectories, bumper angles, and wall bouncing,  
So that I develop spatial intuition for kinetic deflection.

**Acceptance Criteria:**
- **Given** Levels 11 through 20 in Chapter 1,
- **When** solved via real UI placements,
- **Then** all 10 levels transition to Solved and reset cleanly.
- **And** `tools/e2e/campaign-02.test.ts` passes 100% in Chrome serially.

### Story 15.3: Campaign Levels 21–30 (Chapter 1: "On a Roll", Part 3)
*Status: Backlog*

As a player,  
I want challenging capstone levels completing Chapter 1,  
So that I master mechanical momentum before moving to electrical systems.

**Acceptance Criteria:**
- **Given** Levels 21 through 30 in Chapter 1,
- **When** played in Chrome,
- **Then** all 10 levels pass with 100% verification and unlock Chapter 2.
- **And** `tools/e2e/campaign-03.test.ts` passes 100% in Chrome serially.

### Story 15.4: Campaign Levels 31–40 (Chapter 2: "Chain Reactions", Part 1)
*Status: Backlog*

As a player,  
I want levels introducing domino cascades, delay boxes, and aperture sensors,  
So that I learn to build multi-stage chain reactions.

**Acceptance Criteria:**
- **Given** Levels 31 through 40 in Chapter 2,
- **When** solved in Chrome,
- **Then** all chain-reaction levels pass with verified Solved transitions.
- **And** `tools/e2e/campaign-04.test.ts` passes 100% in Chrome serially.

### Story 15.5: Campaign Levels 41–50 (Chapter 2: "Chain Reactions", Part 2)
*Status: Backlog*

As a player,  
I want levels teaching hold timers, pulse counters, and spring preloads,  
So that I learn to meter and sequence machine events.

**Acceptance Criteria:**
- **Given** Levels 41 through 50 in Chapter 2,
- **When** tested in Chrome,
- **Then** all 10 levels achieve 100% pass rate serially.
- **And** `tools/e2e/campaign-05.test.ts` passes 100% in Chrome serially.

### Story 15.6: Campaign Levels 51–60 (Chapter 2: "Chain Reactions", Part 3)
*Status: Backlog*

As a player,  
I want capstone chain-reaction puzzles combining dominoes, springs, and pipes,  
So that Chapter 2 culminates in satisfying multi-path contraptions.

**Acceptance Criteria:**
- **Given** Levels 51 through 60 in Chapter 2,
- **When** solved in Chrome,
- **Then** all levels solve and unlock Chapter 3.
- **And** `tools/e2e/campaign-06.test.ts` passes 100% in Chrome serially.

### Story 15.7: Campaign Levels 61–70 (Chapter 3: "Power & Motion", Part 1)
*Status: Backlog*

As a player,  
I want levels teaching batteries, electric motors, and conveyor belts,  
So that I learn to incorporate sustained continuous drive into puzzles.

**Acceptance Criteria:**
- **Given** Levels 61 through 70 in Chapter 3,
- **When** executed in Chrome,
- **Then** electrical power and motor-driven transport solve all 10 levels.
- **And** `tools/e2e/campaign-07.test.ts` passes 100% in Chrome serially.

### Story 15.8: Campaign Levels 71–80 (Chapter 3: "Power & Motion", Part 2)
*Status: Backlog*

As a player,  
I want levels teaching logic gates (AND, OR, XOR), clutches, and pulleys,  
So that I can build decision-making mechanical assemblies.

**Acceptance Criteria:**
- **Given** Levels 71 through 80 in Chapter 3,
- **When** tested in Chrome,
- **Then** all 10 levels pass 100% serially with verified logic solutions.
- **And** `tools/e2e/campaign-08.test.ts` passes 100% in Chrome serially.

### Story 15.9: Campaign Levels 81–90 (Chapter 3: "Power & Motion", Part 3)
*Status: Backlog*

As a player,  
I want capstone power contraptions featuring linear pushers, cannons, and gates,  
So that Chapter 3 concludes with grand mechanical spectacles.

**Acceptance Criteria:**
- **Given** Levels 81 through 90 in Chapter 3,
- **When** executed in Chrome,
- **Then** all 10 levels solve and unlock Chapter 4.
- **And** `tools/e2e/campaign-09.test.ts` passes 100% in Chrome serially.

### Story 15.10: Campaign Levels 91–100 (Chapter 4: "Light & Sound", Part 1)
*Status: Backlog*

As a player,  
I want levels introducing flashlights, mirrors, and service bells,  
So that I learn basic optical and acoustic propagation.

**Acceptance Criteria:**
- **Given** Levels 91 through 100 in Chapter 4,
- **When** played in Chrome,
- **Then** mirror reflections and bell chimes solve all 10 levels.
- **And** `tools/e2e/campaign-10.test.ts` passes 100% in Chrome serially.

### Story 15.11: Campaign Levels 101–110 (Chapter 4: "Light & Sound", Part 2)
*Status: Backlog*

As a player,  
I want levels teaching lasers, spectral color filters (RGB), and solar panels,  
So that I learn chromatic light manipulation and solar power.

**Acceptance Criteria:**
- **Given** Levels 101 through 110 in Chapter 4,
- **When** tested in Chrome,
- **Then** all 10 levels pass 100% serially.
- **And** `tools/e2e/campaign-11.test.ts` passes 100% in Chrome serially.

### Story 15.12: Campaign Levels 111–120 (Chapter 4: "Light & Sound", Part 3)
*Status: Backlog*

As a player,  
I want capstone optical puzzles combining splitters, secondary receivers, and chimes,  
So that Chapter 4 culminates in intricate laser-acoustic symphonies.

**Acceptance Criteria:**
- **Given** Levels 111 through 120 in Chapter 4,
- **When** solved in Chrome,
- **Then** all 10 levels solve and unlock Chapter 5.
- **And** `tools/e2e/campaign-12.test.ts` passes 100% in Chrome serially.

### Story 15.13: Campaign Levels 121–130 (Chapter 5: "Master Contraptions", Part 1)
*Status: Backlog*

As a player,  
I want multi-domain master puzzles combining pneumatics, optics, and mechanics,  
So that I can test my synthesis of all game systems.

**Acceptance Criteria:**
- **Given** Levels 121 through 130 in Chapter 5,
- **When** solved in Chrome,
- **Then** all 10 levels achieve verified Solved status.
- **And** `tools/e2e/campaign-13.test.ts` passes 100% in Chrome serially.

### Story 15.14: Campaign Levels 131–140 (Chapter 5: "Master Contraptions", Part 2)
*Status: Backlog*

As a player,  
I want high-complexity master contraptions requiring precise timing and logic,  
So that I experience the deepest challenge Curious Contraptions has to offer.

**Acceptance Criteria:**
- **Given** Levels 131 through 140 in Chapter 5,
- **When** tested in Chrome,
- **Then** all 10 levels pass 100% serially.
- **And** `tools/e2e/campaign-14.test.ts` passes 100% in Chrome serially.

### Story 15.15: Campaign Levels 141–150 (Chapter 5: "Master Contraptions", Part 3)
*Status: Backlog*

As a player,  
I want the final grandmaster levels concluding the campaign,  
So that solving all 150 levels completes the game with celebratory recognition.

**Acceptance Criteria:**
- **Given** Levels 141 through 150 in Chapter 5,
- **When** solved in Chrome,
- **Then** the final levels complete cleanly, culminating in the 150-Level Campaign Completion achievement.
- **And** `tools/e2e/campaign-15.test.ts` passes 100% in Chrome serially.

---

## Epic 16: Release Qualification, Performance Audits & Production Packaging

**Goal:** Execute formal qualification gates (P0-034, P0-035), cross-platform WebGL2/WebGPU rendering audits, memory leak audits, and production artifact packaging for final distribution.

### Story 16.1: Cross-Platform Simulation Timing & Telemetry Audit (P0-034 Gate)
*Status: Backlog*

As an engine architect,  
I want formal verification that the WASM SIMD physics solver executes within fixed timing budgets,  
So that the game runs smoothly across diverse consumer hardware.

**Acceptance Criteria:**
- **Given** the 3600-tick benchmark scenes under full active workloads,
- **When** profiled across 20 consecutive runs,
- **Then** simulation tick p95 $\le 2.5\text{ ms}$ at 120 Hz, with simulated/wall ratio between 0.99 and 1.01 and zero accumulating clock debt.
- **And** `tools/e2e/perf-timing-audit.test.ts` passes 100% in Chrome.

### Story 16.2: Universal Rendering & WebGL2/WebGPU Hardware Pacing
*Status: Backlog*

As an engine architect,  
I want verified instanced draw batches and sustained 60 FPS display pacing on both WebGL 2.0 and WebGPU,  
So that rendering remains buttery smooth without frame drops or pipeline stalls.

**Acceptance Criteria:**
- **Given** dense contraption scenes with 50+ active elements,
- **When** rendered on both WebGL 2.0 and WebGPU backends,
- **Then** frame rate sustains $\ge 59.4$ FPS with $\le 1\%$ missed cadence slots.
- **And** `tools/e2e/perf-render-pacing.test.ts` passes 100% in Chrome.

### Story 16.3: Long-Session Memory Leak & Lifecycle Audit (20 Cycles)
*Status: Backlog*

As an engine maintainer,  
I want proof that extended gameplay sessions have zero memory leaks or uncollected buffers,  
So that browser memory consumption remains completely flat over hours of play.

**Acceptance Criteria:**
- **Given** 20 continuous Run/Reset cycles and 20 level transitions in Chrome,
- **When** heaps are sampled after garbage collection,
- **Then** zero growth in live simulation worlds, workers, event listeners, or leased buffers is observed.
- **And** `tools/e2e/perf-memory-audit.test.ts` passes 100% in Chrome.

### Story 16.4: Production Build Packaging, Asset Optimizations & Final Release Manifest
*Status: Backlog*

As a release engineer,  
I want optimized, minified production assets and an audited distribution bundle,  
So that Curious Contraptions is ready for public release.

**Acceptance Criteria:**
- **Given** the production build pipeline,
- **When** executed,
- **Then** production WASM modules, scripts, and content packs build cleanly with 0 warnings.
- **And** all 72 puzzle elements, 150 campaign levels, and Free Workshop sandbox are 100% verified in the final distribution bundle.
