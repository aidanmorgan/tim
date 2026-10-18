# Architecture conformance audit, 18 Oct 2026: project per element, command/query, event disruptor

Tracked copy of the independent, read-only conformance audit of 18 Oct 2026. It is the planning evidence for **Epic 9A: Sole Architecture Conformance** in [epics.md](epics.md#epic-9a-sole-architecture-conformance-project-per-element-commandquery-event-disruptor). The original lived in a temporary scratchpad. This copy keeps every finding and its evidence; the original's §8 remediation table is replaced by §8 below, which maps each finding to its 9A story.

- **Auditor:** independent, read-only. No builds, tests, browsers or repo edits were run.
- **Scope:** the working tree at HEAD `9107148` plus the uncommitted Stories 6.5b–9.4.
- **Method:** `git`, `rg` and file reads.
- **Line numbers are a snapshot.** They come from a tree that Story 9.4's fix round was still editing. Every story therefore cites the **symbol name** as well as the line. When a line has moved, the symbol is authoritative.
- **Later owner decisions (18 Oct 2026) that change this report:**
  - Review-9-4 finding F6 (finding A8 below: the per-tick `Program.SwitchedSupplies`/`ElectricalState` walk of the authoring graph) is fixed inside **Story 9.4**, not in Epic 9A.
  - Every remaining binary16 lane (B6) is migrated to f32 inside Epic 9A (Story 9A.44), not left for the Epic 16 gate.
  - The bundle built from the `elements/<Name>` packages is the **single declaration source**. The `.tres` catalogue and the C# `PartId` tables are deleted (resolves G3 and the "owner decision needed" notes in A13).

## 0. Severity rubric and headline

| Severity | Meaning |
| --- | --- |
| **Blocking** | Breaks the owner rule ("project per puzzle element, command/query, event disruptor as the only allowable design") or an explicit AD rule, and is live in a shipped path. |
| **Debt** | A violation already tracked as sanctioned debt (for example the binary16 lanes), or dead/test-only code that must still be removed. |
| **Doc gap** | The authoritative docs are silent, stale, or contradict each other or the owner's rule. |

Findings are counted by mechanism, not by occurrence.

| Pattern | Verdict | Blocking | Debt |
| --- | --- | --- | --- |
| Project per puzzle element | Non-conformant | 16 | 3 |
| Legacy retirement (cross-cutting) | Non-conformant | 2 | 6 |
| Command/query | Partially conformant | 1 | 3 |
| Event disruptor | Non-conformant (ring inert at runtime) | 3 | 1 |
| **Total** | | **22** | **13** |

There are also 10 documentation gaps (§6).

**Headline.** The `elements/*` packages exist and their project references are clean, but nothing at runtime consumes them and the solution build never builds them. What actually runs is a second declaration authority: `engine/PartId.cs`; `parts/catalog/*.tres` and `parts/scenes/*.tscn`; about a dozen per-element C# switch tables, records and type-pattern branches; and solver heuristics that recognise elements by collider size, drag value and joint limit. Both `CuriousContraptions.Simulation` and `CuriousContraptions.Animation.Worker` compile `../engine/gpu/*.cs` wholesale, so every per-element record, lesson and port table is inside the WASM solver and the animation worker. The event ring is allocated and transferred but has no runtime producer and no runtime consumer; discrete occurrences travel inside reliable committed-read publications that main-thread pumps diff.

## 1. Authoritative definitions found

- **Project per element.** `docs/architecture/element-package-isolation.md` §1–§2 (untracked at audit time): zero engine knowledge of elements (including `PartId` constants), one project per element, element projects reference only the Abstractions layer, and a build task aggregates one bundle that the runtime loads. `spec-9-2b-modular-isolation.md` §3.2–§3.3 and AC1. `architecture.md` AD-06 (line 112: zero element-specific `switch`, class references or branches in the solver; greps for element or level identifiers in the solver core return 0), AD-05 (line 107: no fallback branches or dual-path loaders), AD-11 (lines 145–152: `PartRegistry.Discover()` over `res://parts/catalog/*.tres`, which contradicts the bundle). `docs/engine-contracts.md:138-142` (no per-element solver, evaluator, worker switch or private cosmetic scheduler; data-selected capability kernels are valid, element-selected ones are not). `AGENTS.md` does not name packages or the bundle.
- **Command/query.** isolation.md §3 and §3.2: workers communicate only through ring buffers, commands and queries; ad-hoc `postMessage` payloads and backchannels are forbidden; commands are typed, validated and return receipts; queries read committed memory without side effects. `engine-contracts.md:7` and `:184`. Anchors: `engine/gpu/WorkshopCommandRouter.cs`, `engine/gpu/WorkshopRead.cs`.
- **Event disruptor.** isolation.md §3.3: discrete occurrences go to one single-producer, multi-consumer event ring; the Animation worker, audio and UI read with independent cursors and filters. `spec-9-2b` §5 and AC4. Implementation: `CuriousContraptions.Abstractions/EventRingBuffer.cs`, `EventDescriptor.cs`. `architecture.md:54` (`PG -->|SAB Event Channel| AG`) and `:65`. `engine-contracts.md:142` also permits animation driven by "committed physical observables/events" (G6).

## 2. Pattern A: project per puzzle element (non-conformant)

Every `elements/*/<Name>.csproj` references only `CuriousContraptions.Abstractions` plus domain Abstractions; no engine project references an element and no element references a sibling.

| # | file:line (symbol) | What it is | Severity | Forward fix (no shim) |
| --- | --- | --- | --- | --- |
| A1 | `CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj:28`, `CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj:28` (`Compile Include="../engine/gpu/*.cs"`, plus the `engine/PartId.cs` link) | Both workers compile `engine/gpu` wholesale, so every per-element record, lesson, port and collider table and `PartId` is inside the WASM solver and the animation worker. Root cause: no compile-time forcing function. | Blocking | Element-free core assembly referenced by both workers; authoring/catalogue code main-thread only; a build-failing scan for element ids outside `elements/**`. |
| A2 | `engine/PartId.cs:21-44` (24 named constants), `:46-70` (`ToNumericId`), `:72-99` (`FromNumericId`) | Closed element catalogue inside the engine; numeric ids are the save/wire identity. | Blocking | `PartId` becomes an opaque typed id issued by the loaded bundle; delete constants and both switches; save/wire carry the bundle id. |
| A3 | `engine/gpu/WorkshopConnections.cs:19-34` (`WorkshopPorts.For`, 12 arms), `:97` (`LocalPosition` per id/socket); `engine/gpu/WorkbenchCapacity.cs:35-56` (`WorkbenchFootprint.Of`, 18 arms); `engine/gpu/WorkshopCanonicalColliders.cs:20-33` (10 arms) and its per-element collider tables; `engine/MachineWorld.Gpu.cs:93-125` (`CaptureInstance`, 20 arms); `engine/gpu/WorkshopWire.cs:400+`, `engine/gpu/WorkshopWireFlatBuffers.cs:853+` (decode arms per id); `engine/gpu/WorkshopConstruction.cs:61-62` (ball materials by id); `ui/WorkshopIcons.cs:39+` (24 element icon arms) | Part-id string switch tables, 170+ literals in 9 files, duplicating what packages declare. | Blocking | Derive ports, colliders, footprint and icon from the bundle (`PortDescriptor`, `ColliderDescriptor`, `ElementMetadata.IconPath`); delete every table. |
| A4 | `engine/MachinePart.cs:237-530` (`BuildDeclarative`, 24 `PartId ==` arms), `:40-50` (`ResizableAxes` by id), `:106-113`, `:220-221`, `:533`, `:543` (Clock/Counter lamp updates) | Per-element presentation and setup chain on the generic `MachinePart`: node names (`BumperHead`, `ImpactRing`, `Plate`, `Coil`), pick radii and curves hard-coded per element. | Blocking | Packages declare presentation bindings (target node, property, from/to, curve, source); `MachinePart` applies them generically. |
| A5 | `engine/gpu/WorkshopLatch.cs:7`, `WorkshopBattery.cs:18`, `WorkshopBumper.cs:36`, `WorkshopSpringboard.cs:17`, `WorkshopDomino.cs:22`, `WorkshopWall.cs:30`, `WorkshopDelay.cs:36`, `WorkshopCounter.cs:5`, `WorkshopClock.cs:5`, `WorkshopPressurePlate.cs:5`, `WorkshopActivationParts.cs:13,25,37` (`WorkshopSwitch`, `WorkshopLamp`, `WorkshopBallDetector`), `WorkshopConstruction.cs:78,113` (`WorkshopBall`, `WorkshopReceiver`), `WorkshopInstances.cs:56` (`WorkshopRamp`); factories `WorkshopConstruction.cs:152-313` (`WorkshopInput.*`); element-named settings `BumperWork`, `SpringboardSettings`, `DominoMaterial`, `WallDimensions`, `DelayDuration` | Per-element record types and factories in the engine. Pipe, both bends, Funnel and Powered Gate already use `WorkshopGenericInstance` (`WorkshopInstances.cs:27`). | Blocking | One generic declared instance (id, part id, pose, locked, typed parameters validated against the declaration's parameter schema); one element per slice. |
| A6 | `engine/gpu/WorkshopPhysicsCompiler.cs:43-300` (`OfType<WorkshopBall/Domino/Receiver/Springboard/PressurePlate/Counter/Clock/Latch>`, `is WorkshopRamp or WorkshopWall`, `is WorkshopSwitch or ...` at 96, 115-150); `engine/gpu/ActivationNetwork.cs:688-722` (`instance is WorkshopSwitch/Lamp/Delay/Domino/BallDetector/Counter/Latch/Clock`); `engine/gpu/WorkshopSimulation.cs:57`, `:459` (Latched→Clear admitted only for `is WorkshopLatch`); `engine/MachineWorld.Gpu.cs:318-338`; `engine/gpu/WorkshopInstances.cs:95` (`Validate` type whitelist); `engine/gpu/BrowserWorkshopClient.Electrical.cs:29` | Type-pattern dispatch on element records in the compiler, activation compiler and simulation admission. | Blocking | The compiler iterates declarations and emits capability rows; capability-named `ActivationNodeKind` values stay, only the element→kind mapping moves into declarations. |
| A7 | `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:241` (gate detected by `joint.Upper ≈ 1.55`), `:247-249` (drag bands 0.03–0.08 = battery gate, 0.08–0.11 = plate gate; `ActuatorSliderDrive`); `engine/gpu/WorkshopPhysicsCompiler.cs:186-213` (gate mode encoded as blade drag 0.0625 / 0.09375); `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:831`, `:835` (Pressure Plate found by collider half-extents 0.9/0.05/0.9, Ty 0.03), `:853` (`_plateLoaded`, 0.5 kg literal) | Element detection by dimension, drag and joint-limit heuristics in the solver. | Blocking | Declared prismatic drive capability enabled by a compiled electrical consumer node; declared contact-load sensor (`ContactLoadSensorDeclaration` exists) with a declared threshold. |
| A8 | `WorkshopPhysicsEngine.cs:604`, `:681` → `Program.SwitchedSupplies` / `Program.ElectricalState` (`Program.cs:160-171`) → `engine/gpu/WorkshopLatch.cs:29-58` (`SwitchedSupplyRoutes`; `is WorkshopLatch or WorkshopCounter`, `is WorkshopBattery`) per joint per tick; `batteryPowered` flag (`WorkshopPhysicsEngine.cs:764-776`, first supply found powers all gates) | The solver reads the authoring graph at tick time (also review-9-4 F6). | Blocking | Compiled switched-supply table at Run. **Owner decision 18 Oct 2026: fixed inside Story 9.4.** |
| A9 | `CuriousContraptions.Simulation/Program.cs:65-72` (warmup builds Battery, PressurePlate, PoweredGate, Basketball, Ramp); `WorkshopPhysicsEngine.cs:~180-195` (dummy joint `Upper = 1.55`, spring 14) | Element-specific worker warmup. | Blocking | Declaration-free synthetic warmup, or none. |
| A10 | `engine/gpu/WorkshopCosmetic.cs:55-63` (`CosmeticCurves.ImpactSwitch/SignalLamp/Delay/PinballBumper/Receiver/BallDetector/StateLatch`), `:8` (`AnimationFeedbackSource.Memory`) | Per-element animation curves in the engine. | Blocking | Curves ship in the package and bundle; feedback sources are generic capabilities. |
| A11 | `ui/WorkshopBattery.cs` (whole file), `ui/WorkshopConfiguration.cs:12-16` (Springboard/Battery/Bumper/Clock/Delay panels), `ui/WorkshopGuidance.cs:273` (`PartId.Battery` visibility rule) | Per-element UI code. | Blocking | Generic parameter editor driven by the declared parameter schema; Battery enable is a declared boolean parameter routed to `ConfigureElectrical`. |
| A12 | `engine/gpu/WorkshopConstruction.cs:131-133` (`Puzzle.Id == ReadyToRebound && instance is WorkshopReceiver` → `SpringboardLessons.WithReceiver`); `engine/gpu/WorkshopInventory.cs:93` (`WallAndBumper` inventory) | Level-keyed branches in authoring core, compiled into the solver worker. | Blocking | Lesson constraints become puzzle content data. |
| A13 | `tools/BundleElements/Program.cs:21-36` (hand list of 13 declarations), `:38-50` (anonymous-object serialiser); `parts/elements.bundle.json` (18/18 `"Dimensions": {}`, 20/20 `"LocalPosition": {}`); `engine/PartRegistry.cs:13-49` (loads `.tres`, not the bundle) | The bundle is inert and lossy: no runtime reader, no build target, `Vector3`/`Quaternion` dropped, domain payloads (mass, anchors, electrical nodes, triggers, counter) never serialised, categories disagree with the catalogue. Latch colliders exist three times (`elements/StateLatch/StateLatchElementDeclaration.cs`, `WorkshopCanonicalColliders.CreateLatchColliders`, inline loops in `WorkshopPhysicsCompiler.cs:245-300`). | Blocking | Bundle is the single authority (owner decision 18 Oct 2026); package discovery, lossless typed versioned schema, build target, `PartRegistry` loads the bundle. |
| A14 | `CuriousContraptions.slnx` (only Abstractions, 2dog, main, tests, web); `CuriousContraptions.tests/CuriousContraptions.tests.csproj:41-42` | `elements/*`, the four domain Abstractions and `tools/BundleElements` are never built by the solution or tests. | Blocking | Add them to the solution; bundle round-trip test. |
| A15 | §2.1 | 9 delivered elements have no package. | Blocking | One package per element. |
| A16 | `CuriousContraptions.Abstractions/EventDescriptor.cs:3-14` (`SimulationEventKind.CounterReached`, `LatchSet`) | Element-named event vocabulary in the shared contract. | Debt | Generic kinds (for example `ThresholdReached`, `MemoryChanged`). |
| A17 | `engine/PartRegistry.cs:28-29`, `:35-36` (silent skip of "unmigrated legacy" resources; exemption for `clock.tres` / `PartId.Clock`), `:44-45` (`Ball.Capture` only for Basketball/Bowling) | Element-keyed loader branches and a dead silent skip. | Blocking | Load every declaration; reject malformed ones explicitly. |
| A18 | `engine/gpu/WorkshopInventory.cs:63-64` (Tennis, Balloon in the Free palette) vs `engine/MachineWorld.Gpu.cs:124` (`_ => throw`) and `WorkbenchCapacity.cs:55` | Tennis and Balloon are offered but cannot be constructed; `tennis.tres` and `balloon.tres` reuse `ball.tscn`. | Debt (functional defect) | Remove both from the palette until their own story ships packages. |
| A19 | `engine/gpu/BumperAdvanced.cs`, `BumperSidekick.cs`, `SpringboardLessons.cs`, `DominoEffect.cs`, `DelayedSignal.cs`, `ClearPipe.cs`, `GentleBend.cs`, `QuarterBend.cs`, `WorkshopPuzzle.cs:5` (closed `WorkshopPuzzleId`), `ui/WorkshopPuzzle.cs:18-28` | Lesson and level content as C# compiled into both workers. | Debt | Level content loaded as data; extensible typed puzzle id. |

`tools/Coverage/*` contains 300+ part-id literals; it is a planning-coverage tool over all 72 catalogue specs, not a runtime path.

### 2.1 Delivered catalogue elements without a package

24 catalogue resources, 13 packages (`ball, ball_detector, battery, bumper, clock, counter, domino, lamp, latch, powered_gate, pressure_plate, ramp, wall`).

- **Delivered, no package (9):** `basket` (Receiver, CAT-004), `bowling` (CAT-014), `delay` (CAT-022), `funnel` (CAT-030), `pipe` (CAT-048), `pipe_bend_45` (CAT-049), `pipe_bend_90` (CAT-050), `spring` (Springboard, CAT-062; resource file `springboard.tres`), `switch` (Impact Switch, CAT-063).
- **Catalogued, not constructible, no package (2):** `tennis` (CAT-064), `balloon` (CAT-003). See A18.

## 3. Pattern B: legacy retirement (non-conformant)

The 6.5f purge deleted `engine/PartArt.cs`, `engine/gpu/WorkshopPartKind.cs`, `parts/*Part.cs`, the per-part `*Resource.cs` files and `ReceiverGeometry.cs`. `worker.js` has no solver code (347 lines). These parallel paths remain:

| # | file:line (symbol) | What it is | Severity | Forward fix |
| --- | --- | --- | --- | --- |
| B1 | `engine/gpu/WorkshopCommandRouter.cs:17-25`; `engine/gpu/BrowserWorkshopClient.cs:268-275`; `CuriousContraptions.Simulation/wwwroot/worker.js:138-175` (`extractReadIdentity` hand-decodes the FlatBuffers vtable and falls back to the fixed-offset `responseAbi`) | Dual-path codec: `catch { legacy WorkshopWire.Decode* }` swallows every FlatBuffers error and retries the old binary codec. `WorkshopWire.cs` (462 lines) and `WorkshopWireFlatBuffers.cs` (915 lines) both decode with per-element arms. | Blocking | FlatBuffers only; move schedule endpoints (`Program.Schedule.cs:46`, `BrowserWorkshopClient.Schedule.cs:58`, `Animation.Worker/Program.cs:100`) and `WorkshopTrace.cs:41` onto it. |
| B2 | `engine/MachineWorld.Gpu.cs:118`, `:120` (`def.Colliders.Count > 0 ? def.Colliders : WorkshopCanonicalColliders.For(id)`); `engine/gpu/WorkshopConstruction.cs:256, 304, 307, 310, 313` (`colliders ?? WorkshopCanonicalColliders.For(...)`); `WorkshopWire.cs:429`, `WorkshopWireFlatBuffers.cs:892` (decode re-derives colliders) | Collider fallbacks; save/load rebuilds colliders from code. | Blocking | One source (the bundle); missing colliders are a hard error. |
| B3 | `CuriousContraptions.Simulation/Program.cs:175-255` (`AllocateElectrical`, `PaidContactImpulse`, `PrismaticRowKinds`, `SpringCoefficients`, `PrismaticJacobians`, `ConstraintRows`), `:256-264` (`OperationAbi`, `StateBytes`, `AbiOffsets`) | JS-boundary exports for the deleted JS solver; callers only `tools/workshop-*.test.mjs`; `AbiOffsets` has none. | Debt | Delete exports and their mjs fixtures. |
| B4 | `worker.js:13-37` (20 layout constants `BODY_*`, `MOTION_*`, `PRISMATIC_BYTES`, `COLLIDERS_OFFSET`, …) | Each used only at its declaration; pinned by `CuriousContraptions.tests/WorkerAbiTests.cs:35`, `:52` (`WorkerDynamicOffsetFallbacksDeclareValidTableOffsets`). | Debt | Delete constants and both facts. |
| B5 | `tools/workshop-rigid-body.test.mjs` (28 calls to removed `imports.initialize/stage/commit/read`); `tools/e2e/README.md:20`; `tools/e2e/cat-014.test.ts:15` (comment); `tools/workshop-observation.test.mjs:161-174` (EarlyLoss case and `navigator.gpu` device-loss mock) | Obsolete tests of deleted JS kernels. | Debt | Delete. |
| B6 | 869 `Half` tokens in 69 production `.cs` files (`WorkshopCanonicalColliders.cs` 295, `WorkshopPhysicsCompiler.cs` 68, `AnimationValues.cs` 56, `MachinePart.cs` 45, `PhysicsDeclarations.cs` 36, `WorkshopWireFlatBuffers.cs` 29, …); `ActivationNetwork.cs:385` `(Half)0`; `workshop-client.js` `decodeFloat16` | binary16 lanes against the f32 authority. | Debt | **Owner decision 18 Oct 2026:** migrated in Epic 9A (Story 9A.44). |
| B7 | `engine/MachinePart.cs:236` (`protected virtual void Build() {}`, called at `:128`) | Subclassing hook; only override is test `ProbePart` (`BasketballResourceTests.cs:162-165`). | Debt | Delete hook; test uses declared bindings. |
| B8 | `scratch/` (untracked copies of engine sources, PNGs, `fbs*`, `modularity/`), `scratch/CounterTestsDraft.cs.uid`, root `scratch_test.cs.uid` | Stale scratch copies and orphan `.uid` files; not compiled. | Debt | Delete. |

## 4. Pattern C: command/query (partially conformant)

Conforms: domain mutations go through typed `WorkshopCommand` (`WorkshopCommandKind`: Initialize, Construct, Run, Reset, Dispose, Cancel, ConfigureCadence, Pause, Resume, Step, Save, ConfigureElectrical) via `WorkshopCommandRouter` (sequence-checked, receipts) to `Program.Dispatch` (`Program.cs:282`, `:327-366`). Reads are a pushed committed read model (`WorkshopRead` via `Publish` and the pose ring), not request/response queries (G5).

| # | file:line | What it is | Severity | Forward fix |
| --- | --- | --- | --- | --- |
| C1 | `WorkshopPhysicsEngine.cs:604`, `:681` → `Program.cs:160-171` | Solver pulls live authoring state during the tick (counted as A8). | (A8) | Story 9.4. |
| C2 | `CuriousContraptions.web/wwwroot/workshop-client.js:78-86` → `worker.js:315` → `Program.cs:495` (`SetReliableStall`) → `Program.cs:428-431` (`Simulation.Stop(ReliableStalled)`) | An ad-hoc `postMessage` flag stops the run outside the router and sequence. | Blocking | Typed transport-stall command with a receipt, or stall detection inside the worker's reliable-read accounting. |
| C3 | `worker.js:321-333` (`memoryRequest`), `Program.cs:144-158` (`ObserveMemory`), `BrowserWorkshopClient.cs:325-348`, `workshop-client.js:508-534` | Memory-observation query on its own raw channel; not `PLAYTEST`-gated. | Debt | Typed query on the command/query transport. |
| C4 | `engine/MachineWorld.Gpu.cs:197`, `:363-366` (`GD.Print("CCGPU " + base64(response))`); `ui/WorkshopAnimation.cs:96` (`CCGOAL_SOLVED`) | Production console stdout used as read and event channel by e2e: `tools/e2e/workshop-driver.ts:117-120`, `:465-474` waits on a `CCGPU` acknowledgement for **every** UI command; 7 suites wait on `CCGOAL_SOLVED`; cat-015a, cat-015a-sidekick, cat-015b, cat-062, cat-062b, cat-005, cat-023b decode `CCGPU`. | Debt | Typed observation port; delete the console lines. |
| C5 | `workshop-client.js:796-809` (`globalThis.WorkshopPoseRing`, `WorkshopAnimation`, `WorkshopEventRing`) | Test read hooks on the production global (used by `tools/e2e/workshop-driver.ts:53`, `:417-420`, `:786`; `engine-core-2a1.test.ts`). | Debt | Replace with the typed observation port. |

## 5. Pattern D: event disruptor (non-conformant, inert)

The ring itself is sound and unit-tested (`EventRingBuffer.Publish`, `EventRingConsumer.TryRead`, `EventRingBufferTests.cs`, `StateLatchTests.cs:141-188`). Allocated and transferred: `workshop-client.js:167` (65,600-byte SAB), `:195`, `worker.js:261-266`. The pose ring is written by `worker.js` `writePoseRing` (copying committed bytes from the C# publication into the SAB), which is the model for a ring copy that carries no domain logic.

| # | file:line | What it is | Severity | Forward fix |
| --- | --- | --- | --- | --- |
| D1 | `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:144` (the only runtime `ActivationNetwork.Consume` call passes no event buffer); `engine/gpu/ActivationNetwork.cs:243`, `:391` (Clock `TriggerActivated`), `:487` (`LatchSet`) | No runtime producer; no producer at all for `Impact`, `Passage`, `ContactLoad`, `PrismaticLimit`, `ElectricalPowered`, `CounterReached`. C# has no view of the SAB. | Blocking | The worker owns one ring per run; every discrete transition publishes once at its commit point. |
| D2 | `workshop-client.js:734-788` (`createEventConsumer`/`readEventRing`, reachable only from the test global); `CuriousContraptions.Animation.Worker/` (no ring access) | No runtime consumer. The JS reader `break`s on an uncommitted slot where `EventRingConsumer` skips. | Blocking | Animation worker and main-thread UI hold their own cursors and filters. |
| D3 | `worker.js:11`, `:217-235` (`occurrenceReads`, `reserveOccurrenceRead`, `publish(..., retainOccurrence)`); `Program.cs:472-491` (`EmitRead(retainOccurrence)`); `engine/gpu/WorkshopRead.cs:9-18` (`RequiresReliableRead`); `engine/gpu/BrowserWorkshopClient.Cosmetic.cs:23` (`PumpCosmetics`, 7 pumps); `BrowserWorkshopClient.ContactWork.cs:27-60` (diffs occurrence `Sequence`); `BrowserWorkshopClient.Capture.cs` | The live event channel is a parallel one: occurrences ride in reliable reads; main-thread pumps rebuild edges and relay `Transition` controls to the Animation worker. | Blocking | After D1/D2: delete occurrence-carrying reliable reads, occurrence clauses, ContactWork/Capture pumps and occurrence arrays in `PhysicsContactWorkRead`. |
| D4 | `worker.js:84-108` (`writeEventRing`, no caller); `workshop-client.js:744-788` | Dead duplicate JS producer; divergent JS consumer. | Debt | Delete; one consumer implementation per worker. |

**Committed-state level samples are legitimate queries:** `AnimationFeedbackSource.Activation` (lamp/switch level), `Memory` (latch On/Off), `Timer` (Delay phase) and electrical indicator levels animate functions of committed state from the published read model. **One-shot occurrences must move to the ring:** ContactWork pulses (Bumper impact), Capture (Receiver capture) and activation pulses inferred from transitions. Polled flags standing in for events: `_plateLoaded` (`WorkshopPhysicsEngine.cs:853`), `batteryPowered` (`:764-776`), `SimJoint.SwitchedSupplyClosed` (`PrismaticConstraintSolver.cs:35`).

## 6. Documentation gaps

| # | Where | Gap |
| --- | --- | --- |
| G1 | `AGENTS.md`, `architecture.md`, `docs/engine-contracts.md`, `docs/general-engine-design.md`, `docs/gpu-f32-physics.md` | None names `elements/<Name>` packages, the Abstractions layer, BundleElements and the bundle, command/query, or the event ring; they are defined only in the untracked isolation doc and spec-9-2b. |
| G2 | `docs/architecture/element-package-isolation.md` | Describes the end state as already true; names a nonexistent `CuriousContraptions.Engine`, a nested `elements/mechanical/ramp/` layout (actual `elements/Ramp/`), absent Optical/Pneumatic Abstractions, `IPartDefinition`/`IWorkshopInstance`/`PartId`/`GpuBodyId` "in Abstractions" (not there), a "~50–80 line" `worker.js` (actual 347), and a build "guarantee" that does not exist. |
| G3 | `architecture.md` AD-11 (145–152) and source map (289); `engine-contracts.md` "Dynamic Part Discovery" (39-49) and "Declarative Part Definition Resource Schema" (57-93); `general-engine-design.md` "Dynamic Part Discovery" (34-44) and conversion section (304+) | Make `.tres` discovery and `engine/PartId.cs` the catalogue authority. Resolved by owner decision 18 Oct 2026: the bundle. |
| G4 | `architecture.md` AD-06 (112) | Scopes the zero-identifier grep to the "solver core" only. |
| G5 | isolation.md §3.2 | Defines request/response queries; the implementation is a pushed committed read model plus pose ring. |
| G6 | `engine-contracts.md:142`; `architecture.md:65` | Permit animation from "committed physical observables/events", which legitimises the main-thread relay; level state (query) and one-shot occurrences (ring only) are not separated. |
| G7 | `architecture.md:295`; `gpu-f32-physics.md` f32 Migration Status | `CuriousContraptions.Simulation/wwwroot/pose-ring.js` does not exist; `worker.js` `getF16`/`setF16` are gone. |
| G8 | `review-9-2b-modular-isolation.md:24-27` | AC1, AC3, AC4, AC5 marked PASS although A2–A13 and D1/D2 contradict them; `spec-9-4-state-latch.md:195` already notes the ring has no producer or consumer. |
| G9 | `engine-contracts.md:138-140` | Should say capability-named `ActivationNodeKind` values are valid and element-named event kinds are not. |
| G10 | `spec-9-2b` §3.3, `tools/BundleElements` | No bundle schema or versioning contract (field types, `Vector3` encoding, domain payloads, parameter schema). |

## 7. Dead or legacy files safe to delete

| Path | Evidence nothing live references it |
| --- | --- |
| `scratch/`, `scratch_test.cs.uid` | `EnableDefaultCompileItems=false` in all csprojs; no `Compile Include` mentions `scratch`; orphan `.uid` files have no `.cs`. |
| `tools/workshop-rigid-body.test.mjs` | Calls removed imports; `worker.js` `setModuleImports` exposes only `now, scheduleControl, acknowledge, retire, reserveOccurrenceRead, releaseOccurrenceRead, publish, dispose`; in no package script. |
| `tools/workshop-observation.test.mjs:161-174` | Calls `boundary.imports.initialize`, no longer registered. The rest stays. |
| `worker.js:13-37` constants; `WorkerAbiTests.cs:34-60` | Each constant appears once in `worker.js`, otherwise only in the test. |
| `worker.js:84-108` `writeEventRing` | No caller. |
| `Program.cs` exports `AllocateElectrical`, `PaidContactImpulse`, `SpringCoefficients`, `PrismaticJacobians`, `ConstraintRows`, `PrismaticRowKinds`, `OperationAbi`, `StateBytes`, `AbiOffsets` | Callers only `tools/workshop-rigid-body.test.mjs`, `workshop-pose-ring.test.mjs`, `workshop-observation.test.mjs` host stubs. |
| `engine/MachinePart.cs:236` `Build()` | Only override is the test `ProbePart`. |
| `engine/PartRegistry.cs:28-29`, `:35-36` skips | No `.tres` other than exempted `clock.tres` has `Parameters`. |
| Legacy decode in `engine/gpu/WorkshopWire.cs` | Reached only from the `catch` fallbacks (B1); the encoder is still used by schedule endpoints and `WorkshopTrace` until B1's story moves them. |

Not deletable until replaced: `parts/elements.bundle.json`, `tools/BundleElements`, `elements/*`, `EventRingBuffer` (target design); `WorkshopCanonicalColliders` and the switch tables (live until the bundle loads).

## 8. Disposition in Epic 9A

| Finding | Owning story |
| --- | --- |
| G1–G10 | 9A.0 |
| B3, B4, B5, B7, B8, D4 (JS producer), A17 dead skips | 9A.1 |
| B1 | 9A.2 |
| C2 (whatever Story 9.4b leaves), C3 | 9A.3 |
| A13 (build side), A14, G10 | 9A.4 |
| A15 (one story per missing package) | 9A.5–9A.13 |
| A13 (runtime side), A17 exemptions, A18, Free palette list | 9A.14 |
| A2 numeric switches (save/wire identity) | 9A.15 |
| A3 colliders, B2 | 9A.16 |
| A3 ports, footprint | 9A.17 |
| A3 icons | 9A.18 |
| A7 gate drive (what Story 9.4 leaves) | 9A.19 |
| A7 pressure plate | 9A.20 |
| A9 | 9A.21 |
| D1, A16 | 9A.22 |
| D2, D3, D4 (JS reader) | 9A.23 |
| C4, C5 | 9A.24 |
| A4, A10 | 9A.25 |
| A12, A19 | 9A.26 |
| A5, A6, A11, A3 decode arms and `CaptureInstance`, one element per story | 9A.27–9A.43 |
| B6 | 9A.44 |
| A1, A2 constants, the isolation gate | 9A.45 |
| A8 / review-9-4 F6 | Story 9.4 (owner decision 18 Oct 2026) |
