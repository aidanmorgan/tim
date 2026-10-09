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
6. **Epic 6: Core Interactive Catalogue Elements** (CAT-014 Bowling Ball, CAT-015a-b Bumper, CAT-062a-b Springboard, CAT-002 Ball Detector, CAT-048a-b Pipe) — *[Backlog]*
7. **Epic 7: Legacy Code Purge & Zero-Remnant Clean Architecture** (LEGACY-0a Historical Archives, LEGACY-0b Probe Tools, LEGACY-0c Dead Tests, LEGACY-0d Legacy CPU Physics & Unshipped Engine Purge) — *[Backlog]*
8. **Epic 8: Activation, Timing & Discrete Logic Elements** (Pressure Plate, Pulse Counter, System Clock, State Latch, Hold Timer) — *[Backlog]*
9. **Epic 9: Supplied Electrical Power & Logic Networks** (Battery Power Source, Electrical Logic Gates Nand/Nor/Or/Xor, Dual-Supply Both) — *[Backlog]*
10. **Epic 10: Constrained Mechanics & Elastic Fixtures** (Trampoline, Impact Lever, Pulley, Counterweight, Rope Anchor) — *[Backlog]*
11. **Epic 11: Rotary Drive & Mechanical Actuators** (Electric Motor, Conveyor Belt, Mechanical Clutch, Reverse Transmission, Linear Pusher, Powered Gate, Wound Spring Motor, Toy Cannon, Beam Shutter) — *[Backlog]*
12. **Epic 12: Conserved Airflow, Pneumatics & Buoyancy** (Floating Balloon, Electric Fan, Pneumatic Bellows, Windmill, Tennis Ball) — *[Backlog]*
13. **Epic 13: Optical Propagation, Routing & Sensing** (Torch, Laser, Flat Mirror, Beam Splitter & Combiner, Spectral Color Filters, Tuned Optical Receivers, Solar Panel, Optical Logic Gates) — *[Backlog]*
14. **Epic 14: Acoustic Emission & Reception** (Service Bell, Audio Speaker, Sound Level Meter, Wind Chimes) — *[Backlog]*
15. **Epic 15: 150 Progressively Taught Campaign Levels** (Stories 15.1–15.15 covering all 150 levels in 10-level batches across Chapters 1 to 5, with verified UI solutions and retry loops) — *[Backlog]*
16. **Epic 16: Release Qualification, Performance Audits & Production Packaging** (Cross-platform headless/browser performance audit, WebGL2/WebGPU qualification, 60 FPS display pacing, memory leak audits, and release candidate bundle) — *[Backlog]*

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
*Status: Backlog*

As a player,  
I want to preload a Springboard to launch objects across the workbench,  
So that contraptions can solve `spring_forward`.

**Acceptance Criteria:**
- **Given** the `spring_forward` level,
- **When** the preloaded springboard releases on Run start,
- **Then** the ball is launched across the workbench into the target receiver.
- **And** an uncharged springboard fails to launch the ball.
- **And** `engine/LatchedSpringStore.cs` is deleted in favor of declarative preload energy stores.
- **And** `tools/e2e/cat-062b.test.ts` passes 100% in Chrome serially.

### Story 6.6: Generic Aperture Sensor with Directional Rearm (CAT-002)
*Status: Backlog*

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

### Story 6.7: Straight Pipe Compound Cylindrical Collider (CAT-048a)
*Status: Backlog*

As a player,  
I want balls to roll smoothly through straight pipes without snagging,  
So that enclosed ball transport operates reliably.

**Acceptance Criteria:**
- **Given** a straight pipe collider in the Workshop,
- **When** an admitted ball rolls into the pipe mouth,
- **Then** the ball travels through and exits without snagging or falling through walls.
- **And** an oversized ball stops at the pipe entrance without clipping.
- **And** `tools/e2e/cat-048a.test.ts` passes 100% in Chrome serially.

### Story 6.8: Hollow Signed-Distance Torus Rim Cap Pipe Collider (CAT-048b)
*Status: Backlog*

As a player,  
I want curved and complex pipe paths to guide balls reliably,  
So that level `clear_pipe` can be solved.

**Acceptance Criteria:**
- **Given** level `clear_pipe` played in Chrome,
- **When** the ball enters the mouth at high speed and edge angles,
- **Then** the ball negotiates the pipe geometry and reaches the receiver.
- **And** all legacy annular-specific kernel code, `AnnularFeature` enums, and `reference/pipe/` are deleted.
- **And** `tools/e2e/cat-048b.test.ts` passes 100% in Chrome serially.

---

## Epic 7: Legacy Code Purge & Zero-Remnant Clean Architecture

**Goal:** Eradicate all dead reference tarballs, ad-hoc diagnostic probe tools, uncompiled test suites, and superseded CPU physics implementations across four disciplined deletion stages (LEGACY-0a through 0d).

### Story 7.1: Reference & Historical Archive Purge (LEGACY-0a)
*Status: Backlog*

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
*Status: Backlog*

As an engine maintainer,  
I want obsolete probe tools and ad-hoc harnesses in `tools/` purged,  
So that the codebase contains only active build, lint, and test tools.

**Acceptance Criteria:**
- **Given** `tools/p0-*`, `tools/P0-007-*`, and dead diagnostic folders,
- **When** `LEGACY-0b` is applied,
- **Then** ~2,000 obsolete files are purged.
- **And** remaining tools compile with zero warnings under Anvil.

### Story 7.3: Uncompiled Legacy Test Purge (LEGACY-0c)
*Status: Backlog*

As an engine maintainer,  
I want uncompiled, commented-out, or obsolete test files in `CuriousContraptions.tests/` purged,  
So that 100% of test files in the project are actively compiled and executed.

**Acceptance Criteria:**
- **Given** dead test files and obsolete native fixtures,
- **When** `LEGACY-0c` is applied,
- **Then** all obsolete test files are deleted.
- **And** `dotnet test CuriousContraptions.slnx` runs with 100% pass rate (0 failures).

### Story 7.4: Legacy CPU Physics & Unshipped Engine Purge (LEGACY-0d)
*Status: Backlog*

As an engine maintainer,  
I want all superseded CPU physics files in `engine/physics/` and dead classes in `parts/` deleted,  
So that Curious Contraptions contains zero legacy physics code in the active tree.

**Acceptance Criteria:**
- **Given** all remaining legacy physics files in `engine/physics/` and `engine/presentation/`,
- **When** `LEGACY-0d` is applied,
- **Then** all superseded physics classes are permanently purged.
- **And** `git ls-files` contains only compiled, shipped, active content, docs, and CI files.
- **And** P0-030 and P0-031 cleanup audits are closed with zero warnings.
- **And** cumulative Playwright E2E suites pass 100% serially in Chrome.

---

## Epic 8: Activation, Timing & Discrete Logic Elements

**Goal:** Deliver the full suite of sensor and discrete logic elements (Pressure Plate, Pulse Counter, System Clock, State Latch, Hold Timer) evaluating cleanly at substep endpoints with exact tick accumulation.

### Story 8.1: Pressure Plate Surface Contact Load Sensor (CAT-052)
*Status: Backlog*

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

### Story 8.2: Pulse Counter Event Accumulator (CAT-020)
*Status: Backlog*

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

### Story 8.3: System Clock Periodic Pulse Generator (CAT-017)
*Status: Backlog*

As a player,  
I want a System Clock to emit pulses at regular intervals,  
So that rhythmic and timed contraption mechanisms can operate.

**Acceptance Criteria:**
- **Given** a System Clock configured for a 1.0 s period,
- **When** the simulation runs for 5.0 seconds,
- **Then** exactly 5 discrete activation pulses are emitted at 1-second intervals.
- **And** pausing simulation halts clock emission; resetting restores clock phase to 0.
- **And** `tools/e2e/cat-017.test.ts` passes 100% in Chrome serially.

### Story 8.4: State Latch Bistable Memory (CAT-037)
*Status: Backlog*

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

### Story 8.5: Hold Timer Pulse Stretcher (CAT-033)
*Status: Backlog*

As a player,  
I want a Hold Timer to keep an output energized for a configured duration after a momentary trigger,  
So that timed doors and temporary circuits can stay open.

**Acceptance Criteria:**
- **Given** a Hold Timer configured for 2.0 seconds,
- **When** a momentary impact pulse is received,
- **Then** the output activates immediately and remains active for exactly 2.0 simulated seconds before deactivating.
- **And** re-triggering during the active window does NOT extend the timeout.
- **And** `tools/e2e/cat-033.test.ts` passes 100% in Chrome serially.

---

## Epic 9: Supplied Electrical Power & Logic Networks

**Goal:** Implement supplied direct-current electrical networks, battery power sources, and electrical Boolean logic gates (NAND, NOR, OR, XOR, Both) as declarative network graphs with zero continuous circuit simulation overhead.

### Story 9.1: Battery DC Power Source & Network Graph (CAT-005)
*Status: Backlog*

As a player,  
I want Batteries to supply power to connected electrical elements,  
So that powered mechanisms like motors and lamps can operate.

**Acceptance Criteria:**
- **Given** a Battery connected to an Electric Motor via an electrical cable,
- **When** the simulation runs,
- **Then** the motor receives continuous 12V supply and spins its axle.
- **And** disconnecting the cable stops the motor immediately.
- **And** `tools/e2e/cat-005.test.ts` passes 100% in Chrome serially.

### Story 9.2: Dual-Supply Both Gate (CAT-013)
*Status: Backlog*

As a player,  
I want a Dual-Supply Both Gate to conduct power only when both inputs are energized,  
So that safety interlocks and dual-condition activations are possible.

**Acceptance Criteria:**
- **Given** a Both Gate connected to two separate Battery circuits and an output lamp,
- **When** both batteries are connected,
- **Then** the lamp illuminates.
- **And** disconnecting either battery extinguishes the lamp.
- **And** `tools/e2e/cat-013.test.ts` passes 100% in Chrome serially.

### Story 9.3: Electrical Logic Gates NAND & NOR (CAT-024, CAT-025)
*Status: Backlog*

As a player,  
I want Electrical NAND and NOR gates to evaluate Boolean conditions,  
So that complex electrical logic contraptions can be constructed.

**Acceptance Criteria:**
- **Given** Electrical NAND and NOR gates wired to input power switches,
- **When** all four input permutations (00, 01, 10, 11) are tested,
- **Then** output power states match standard Boolean truth tables with 100% accuracy.
- **And** `tools/e2e/cat-024-025.test.ts` passes 100% in Chrome serially.

### Story 9.4: Electrical Logic Gates OR & XOR (CAT-026, CAT-027)
*Status: Backlog*

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

## Epic 10: Constrained Mechanics & Elastic Fixtures

**Goal:** Deliver compliant elastic fixtures, pivots, pulleys, ropes, and counterweights using unified TGS Soft constraints without ad-hoc velocity projections or Baumgarte stabilization.

### Story 10.1: Trampoline Compliant Membrane Dynamics (CAT-065)
*Status: Backlog*

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

### Story 10.2: Balanced Impact Lever & Pivot Fulcrum (CAT-034)
*Status: Backlog*

As a player,  
I want an Impact Lever to pivot smoothly when struck on one arm,  
So that see-saw mechanisms can fling objects or lift connected ropes.

**Acceptance Criteria:**
- **Given** an Impact Lever with a payload resting on one arm,
- **When** a heavy Bowling Ball drops onto the opposing arm,
- **Then** the lever pivots around its fulcrum, strikes its end stop, and launches the payload upward.
- **And** an equal load placed on both arms remains balanced indefinitely.
- **And** `tools/e2e/cat-034.test.ts` passes 100% in Chrome serially.

### Story 10.3: Pulley Wheel & Tensile Rope Dynamics (CAT-053, CAT-058)
*Status: Backlog*

As a player,  
I want to route ropes around Pulleys to transmit tensile forces,  
So that dropping one weight can lift an object in a different part of the workbench.

**Acceptance Criteria:**
- **Given** a rope routed through two Pulleys between a Counterweight and a Lever,
- **When** the counterweight falls under gravity,
- **Then** tensile forces transmit through the rope, lifting the lever arm.
- **And** slack ropes apply zero force; cutting or releasing tension uncouples the bodies.
- **And** `tools/e2e/cat-053-058.test.ts` passes 100% in Chrome serially.

### Story 10.4: Counterweight & Multi-Body Pendulum Lifting (CAT-067)
*Status: Backlog*

As a player,  
I want Counterweights to swing as pendulums and balance moving loads,  
So that gravitational energy can be harnessed smoothly.

**Acceptance Criteria:**
- **Given** a Counterweight suspended as a 3D pendulum,
- **When** released from an angle,
- **Then** it oscillates with natural period $T = 2\pi\sqrt{L/g}$ and damps gradually.
- **And** exact Reset restores initial angular displacement and zero velocity.
- **And** `tools/e2e/cat-067.test.ts` passes 100% in Chrome serially.

---

## Epic 11: Rotary Drive & Mechanical Actuators

**Goal:** Implement electric motors, conveyors, clutches, reversing transmissions, linear pushers, motorized gates, wound springs, toy cannons, and beam shutters as declarative actuators.

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

### Story 11.4: Powered Gate Passageway Barrier (CAT-051)
*Status: Backlog*

As a player,  
I want a Powered Gate to open when triggered,  
So that balls previously blocked can pass through unobstructed.

**Acceptance Criteria:**
- **Given** a closed Powered Gate blocking a ball in a ramp,
- **When** an activation pulse is received,
- **Then** the gate opens fully, allowing the ball to roll past.
- **And** `tools/e2e/cat-051.test.ts` passes 100% in Chrome serially.

### Story 11.5: Wound Spring Potential Motor & Toy Cannon Launcher (CAT-071, CAT-016)
*Status: Backlog*

As a player,  
I want to fire Toy Cannons and wind up Spring Motors,  
So that high-energy mechanical impulses can launch balls across wide gaps.

**Acceptance Criteria:**
- **Given** a loaded Toy Cannon triggered by an activation wire,
- **When** triggered,
- **Then** the loaded ball launches with high muzzle velocity ($12\text{ m/s}$) along barrel trajectory.
- **And** uncharged or unloaded cannons fire nothing.
- **And** `tools/e2e/cat-071-016.test.ts` passes 100% in Chrome serially.

### Story 11.6: Beam Shutter Mechanical Guillotine (CAT-007)
*Status: Backlog*

As a player,  
I want a Beam Shutter to block or unblock light paths upon mechanical activation,  
So that optical circuits can be switched physically.

**Acceptance Criteria:**
- **Given** a Beam Shutter positioned across a laser path,
- **When** closed, the laser beam is blocked; when opened by signal or rope, the beam passes through cleanly.
- **And** `tools/e2e/cat-007.test.ts` passes 100% in Chrome serially.

---

## Epic 12: Conserved Airflow, Pneumatics & Buoyancy

**Goal:** Deliver aerodynamic airflow fields, pneumatic pressure pulses, and buoyant gas dynamics conforming strictly to energy conservation laws.

### Story 12.1: Lightweight Tennis Ball & Floating Balloon Buoyancy (CAT-064, CAT-003)
*Status: Backlog*

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

### Story 12.3: Pneumatic Bellows Compression Pulse (CAT-010)
*Status: Backlog*

As a player,  
I want a Pneumatic Bellows to emit a focused gust of air when compressed,  
So that impacting bodies can trigger secondary pneumatic launches.

**Acceptance Criteria:**
- **Given** a Bellows struck by a falling Bowling Ball,
- **When** the plates compress,
- **Then** a high-velocity directional air jet discharges from the nozzle, propelling an adjacent balloon or tennis ball.
- **And** uncompressed bellows discharge zero airflow.
- **And** `tools/e2e/cat-010.test.ts` passes 100% in Chrome serially.

### Story 12.4: Windmill Rotor Aerodynamic Capture & Drive (CAT-070)
*Status: Backlog*

As a player,  
I want a Windmill to spin when struck by fan airflow,  
So that airflow can be converted into mechanical shaft work.

**Acceptance Criteria:**
- **Given** a Windmill positioned in the airflow of an active Electric Fan,
- **When** airflow strikes the rotor blades,
- **Then** the windmill spins its axle and delivers rotational torque to connected mechanical components.
- **And** blocking the fan airflow with a wall stops windmill rotation.
- **And** `tools/e2e/cat-070.test.ts` passes 100% in Chrome serially.

---

## Epic 13: Optical Propagation, Routing & Sensing

**Goal:** Implement raymarched optical propagation, planar reflections, spectral color bandpass filtering (RGB), and optical logic gates as a discrete, deterministic optical graph.

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

### Story 13.2: Planar Reflection Mirror, Beam Splitter & Beam Combiner (CAT-041, CAT-008, CAT-006)
*Status: Backlog*

As a player,  
I want Mirrors to bounce beams and Splitters/Combiners to divide and merge light paths,  
So that optical rays can be routed around obstacles.

**Acceptance Criteria:**
- **Given** a laser directed into a $45^\circ$ Flat Mirror,
- **When** the beam strikes the mirror surface,
- **Then** it reflects at an exact $90^\circ$ right angle.
- **And** Beam Splitters divide incident power 50/50; Beam Combiners merge orthogonal beams into one.
- **And** `tools/e2e/cat-041-008-006.test.ts` passes 100% in Chrome serially.

### Story 13.3: Spectral Color Bandpass Filters (Red, Green, Blue) (CAT-055, CAT-031, CAT-011)
*Status: Backlog*

As a player,  
I want Color Filters to filter out non-matching wavelengths,  
So that multi-chromatic light beams can be separated into pure primary colors.

**Acceptance Criteria:**
- **Given** a white light beam passing through a Red Filter,
- **When** the beam exits the filter,
- **Then** only red spectral energy is transmitted; green and blue are absorbed.
- **And** passing the red beam into a Green Filter completely extinguishes the beam (zero transmission).
- **And** `tools/e2e/cat-filters.test.ts` passes 100% in Chrome serially.

### Story 13.4: Broadband & Pure Channel Optical Receivers (CAT-038, CAT-056, CAT-032, CAT-012)
*Status: Backlog*

As a player,  
I want Optical Receivers that respond only to specific colors,  
So that color-tuned optical triggers can activate designated circuits.

**Acceptance Criteria:**
- **Given** a Red Receiver illuminated by red, green, and blue beams successively,
- **Then** only the red beam triggers electrical activation; green and blue produce zero response.
- **And** Broadband Receivers activate under any light exceeding power threshold.
- **And** `tools/e2e/cat-receivers-primary.test.ts` passes 100% in Chrome serially.

### Story 13.5: Secondary Spectral Receivers (Cyan, Magenta, Yellow, White) (CAT-021, CAT-040, CAT-072, CAT-068)
*Status: Backlog*

As a player,  
I want Cyan, Magenta, Yellow, and White receivers that require mixed spectral combinations,  
So that advanced color-combining puzzles can be constructed.

**Acceptance Criteria:**
- **Given** a Yellow Receiver,
- **When** both Red and Green beams strike it simultaneously,
- **Then** it activates; a Red-only or Green-only beam fails to activate it.
- **And** White Receiver requires all three primary colors simultaneously.
- **And** `tools/e2e/cat-receivers-secondary.test.ts` passes 100% in Chrome serially.

### Story 13.6: Photovoltaic Solar Panel Energy Conversion (CAT-059)
*Status: Backlog*

As a player,  
I want Solar Panels to generate electrical power from incident light,  
So that optical beams can remotely power electrical motors and circuits.

**Acceptance Criteria:**
- **Given** a Solar Panel connected to an Electric Motor,
- **When** illuminated by a Flashlight or Laser,
- **Then** output electrical power energizes the motor.
- **And** blocking the light beam immediately cuts motor power.
- **And** `tools/e2e/cat-059.test.ts` passes 100% in Chrome serially.

### Story 13.7: Optical Logic Gates (AND, NAND, NOR, OR, XOR) (CAT-043 through CAT-047)
*Status: Backlog*

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

### Story 14.1: Service Bell Percussion & Resonant Ringing (CAT-009)
*Status: Backlog*

As a player,  
I want Service Bells to ring with a crisp chime when struck,  
So that physical impacts can produce audible confirmation and trigger acoustic meters.

**Acceptance Criteria:**
- **Given** a Service Bell placed in the path of a rolling ball,
- **When** struck with kinetic energy exceeding threshold,
- **Then** a crisp metallic ring is emitted as a timestamped acoustic event.
- **And** sub-threshold nudges produce no ring.
- **And** `tools/e2e/cat-009.test.ts` passes 100% in Chrome serially.

### Story 14.2: Audio Speaker Directional Pulse Emission (CAT-061)
*Status: Backlog*

As a player,  
I want an Audio Speaker to emit directional acoustic pressure waves upon signal activation,  
So that electrical pulses can be broadcast acoustically.

**Acceptance Criteria:**
- **Given** an Audio Speaker wired to a system clock,
- **When** energized,
- **Then** directional acoustic pressure waves radiate forward in a $35^\circ$ cone.
- **And** `tools/e2e/cat-061.test.ts` passes 100% in Chrome serially.

### Story 14.3: Sound Level Meter Threshold Sensor (CAT-060)
*Status: Backlog*

As a player,  
I want a Sound Level Meter to trigger an electrical contact when sound volume exceeds threshold,  
So that bells and speakers can activate remote circuits without physical wires.

**Acceptance Criteria:**
- **Given** a Sound Level Meter facing a Service Bell,
- **When** the bell rings,
- **Then** the sound meter closes its electrical contact and illuminates a connected lamp.
- **And** placing a solid Wall between bell and meter occludes the acoustic wave, preventing activation.
- **And** `tools/e2e/cat-060.test.ts` passes 100% in Chrome serially.

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
