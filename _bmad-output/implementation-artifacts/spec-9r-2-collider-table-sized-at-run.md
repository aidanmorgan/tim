---
title: 'Story 9R.2: Collider Table Sized at Run (Reopened Story 6.9b)'
type: 'bugfix'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r2-collider-table-sized-at-run-reopened-story-69b'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-9b-unbounded-collider-capacity.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** This is report #6 and Story 6.9b AC2.
- `PhysicsSceneDeclaration.ColliderCapacity = 2048` (`engine/gpu/PhysicsDeclarations.cs:168`) is baked into compile-time ABI offsets: `PhysicsGpuAbi.MaterialsOffset` and every later offset up to `ByteLength` (`engine/gpu/PhysicsGpuAbi.cs:35-51`).
- Build admits 32 × `pipe_bend_90` or `funnel` (32 × 72 + 1 = 2,305 colliders) because `WorkbenchCapacity.Validate` (`engine/gpu/WorkbenchCapacity.cs:65-96`) counts no colliders.
- Every admission then throws "Invalid physics document or capacity" (`PhysicsDeclarations.cs:210-214`). Admission happens at Construct, Reset and Configure (`CuriousContraptions.Simulation/WorkshopGpuDevice.cs:67-73`), and `WorkshopSimulation` turns that throw into `Failure(InvalidRead)`, a Faulted session (`engine/gpu/WorkshopSimulation.cs:184`, `:386-392`).
- In practice the 29th bend faults the bench: 29 × 72 + 1 = 2,089.

**A second hidden ceiling, found in this investigation:** the contact cache.
- `PhysicsGpuAbi.CacheCapacity = 888 * 4` (`PhysicsGpuAbi.cs:49`). Admission reserves 4 slots for every static–dynamic collider pair (`:158-172`) and throws "Contact cache capacity exceeded." at more than 888 pairs.
- So 31 bends plus one ball (2,232 pairs) is Build-admitted and fails.
- The existing 30-instance test passes only because its 880 colliders yield at most 888 pairs.

**Approach:** Move the two count-dependent tables (colliders and contact cache) to the tail of the physics buffer. Size them at admission from the compiled scene's actual collider count and pair count, so the buffer length is a function of the scene. Delete both fixed ceilings and every constant derived from them.

## Boundaries & Constraints

**Always:**
- One layout authority: `PhysicsGpuAbi` computes the offsets and the length from the header counts (`R32(data,16)` colliders and `R32(data,112)` cache slots).
- Every consumer validates a buffer against the length derived from that buffer's own header.

**Never:**
- A new collider ceiling.
- A larger constant.
- A fallback buffer size.
- Re-introducing collider counting into Build. Build-admitted ⇒ compiles.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected | Error Handling |
|---|---|---|---|
| Full bend bench | 32 `pipe_bend_90` (2,305 colliders) | Compiles, steps, Run/Reset/Save/Load with no fault | N/A |
| Near-full with ball | 31 bends + 1 Basketball (2,232 colliders, 2,232 dynamic pairs) | Compiles and steps | N/A |
| Over budget | 33 instances | Rejected at Build: "Workbench is full: part table" (`WorkbenchCapacity.cs:13-16`, `:68`) | Existing message |
| Other table | e.g. 17 balls | Rejected at Build by that table's existing message | Unchanged |

### Decisions (coordinator delegation)

- **D1. Tail placement, not a fully dynamic layout.**
  - Only the collider and cache tables vary, so all other offsets stay constants.
  - Rationale: the smallest dependency closure, with one length function.
- **D2 (AC correction, flagged to the owner, like Story 9.4b C1). The property test reads "each catalogue element" as each constructible element at the largest count Build admits.** The literal AC cannot be met: for example, 32 balls exceed the 16 dynamic bodies.
  - The count is 32, unless another declared budget rejects it first. In that case the test asserts that budget's existing Build message, then compiles and steps the largest admitted count. Example limits: dynamic bodies 16 (`PhysicsBodyReadSet.Capacity`), bodies 33, triggers 8, contact works 8, sources 8, prismatics 16.
  - Tennis and Balloon are listed as excluded with this reason: they throw in `CaptureInstance` (`engine/MachineWorld.Gpu.cs:124`, audit A18), and Story 9A.14 removes them from the palette.
  - Rationale: this proves "every Build-admitted construction compiles" without asserting an impossible construction.
- **D3. The WGSL `COLLIDER_CAPACITY` preamble constant is deleted** (`PhysicsGpuAbi.cs:492`). No shader consumes it: `BrowserGpuTransport.Initialize` ignores the preamble (`CuriousContraptions.Simulation/BrowserGpuTransport.cs:19-27`).

</frozen-after-approval>

## Code Map

Line numbers are from the working tree on 18 Oct 2026, before Story 9.4b's commit. The symbol is authoritative.

- `engine/gpu/PhysicsDeclarations.cs:168,211`: the capacity constant and the constructor check.
- `engine/gpu/PhysicsGpuAbi.cs`:
  - offsets `:35-51`;
  - admission `:56-172`, including the O(n²) pair loop `:158-172`;
  - validation `:340-347` (`candidate[CollidersOffset..SensorsOffset]` immutability), `:448-469` (cache diff and unused-cache zero), `:531-540` (header bounds);
  - WGSL `:492`.
- `engine/gpu/PhysicsGpuAbi.Electrical.cs:9`: `ElectricalSourcesOffset = CacheOffset + CacheCapacity*CacheBytes`.
- `engine/gpu/CompiledMemoryLayout.cs:99`: collider offsets.
- `engine/gpu/ContactWorkDeclaration.cs:90`: collider bound against the constant.
- `CuriousContraptions.Simulation/BrowserGpuTransport.cs:9-13,37-40,64`: fixed buffers.
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:78,106`: length checks.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`:
  - mock and warm-up buffers `:177,201-202,270`;
  - `Array.Copy` `:385`;
  - collider reads `:234,240,532`.
- `CuriousContraptions.Simulation/wwwroot/worker.js:35-38`: stale `let COLLIDERS_OFFSET=5408 …` values for a 64-collider table, read nowhere.
- Tests pinning old values:
  - `CuriousContraptions.tests/WorkerAbiTests.cs:52-60`;
  - `WorkshopWallTests.cs:161` (`== 2048`);
  - `WorkshopDelayTests.cs:95`;
  - `WorkshopWireTests.cs:921,1009-1020,1191`;
  - `FunnelTests.cs:203`, `PipeBend45Tests.cs:210`, `PipeBend90Tests.cs:210`, `StraightPipeTests.cs:222`, `WorkshopSpringboardTests.cs:36`.
- `WorkbenchFootprint.Of` (`WorkbenchCapacity.cs:35-56`): pipe 24, `pipe_bend_45` 40, `pipe_bend_90` and `funnel` 72, gate 20.

## Tasks & Acceptance

**Execution:**
- [ ] `CuriousContraptions.tests/WorkbenchCapacityTests.cs` -- red first. Add the property theory over every `WorkbenchFootprint.Of` part (per D2). For each part, build the construction, then `WorkshopPhysicsCompiler.Compile`, `PhysicsGpuAbi.Admission`, and N steps through the real `WorkshopPhysicsEngine`, using the Story 9.4b harness in `WorkshopPhysicsEngineTests`. Cover:
  - 32 bends;
  - 32 funnels;
  - 31 bends + ball;
  - the existing 30-instance mixed case;
  - 33 instances rejected (negative control).
- [ ] `engine/gpu/PhysicsGpuAbi.cs`, `PhysicsGpuAbi.Electrical.cs`:
  - Relocate the collider and cache tables after the prismatic records.
  - Add `ByteLength(int colliders, int cacheSlots)` and `ByteLength(ReadOnlySpan<byte> header)`.
  - Delete `CacheCapacity` and the fixed `ByteLength`.
  - Rewrite the immutability slices as fixed region plus collider tail.
- [ ] `engine/gpu/PhysicsGpuAbi.cs` admission pair loop (`:158-172`):
  - Enumerate only pairs that include a dynamic collider: the outer loop runs over dynamic colliders only.
  - Today both `BodySlot` lookups run before the static–static `continue` (`:162-164`). That is about 2.6M iterations at 2,305 colliders on every Construct, and `capacity-9r` performs 32 growing admissions.
  - The pair order and the cache slot contents must stay byte-identical for existing scenes; pin this with a native test that compares against the current admission bytes.
  - The native theory records a per-admission time bound for 32 bends as telemetry.
- [ ] `engine/gpu/PhysicsDeclarations.cs` -- delete `ColliderCapacity` and its constructor clause.
- [ ] `engine/gpu/CompiledMemoryLayout.cs`, `engine/gpu/ContactWorkDeclaration.cs` -- take offsets and bounds from the scene's count.
- [ ] `CuriousContraptions.Simulation/BrowserGpuTransport.cs`, `WorkshopGpuDevice.cs`, `WorkshopPhysicsEngine.cs`:
  - Allocate current and candidate buffers at Admit from `input.Length`.
  - Validate each length against its own header.
  - Size the warm-up buffers from the warm-up scene.
- [ ] `CuriousContraptions.Simulation/wwwroot/worker.js` -- delete the four stale `let` offsets. Delete the matching facts in `WorkerAbiTests.cs`.
- [ ] Update the pinned tests listed in the Code Map to the derived length, and delete `WorkshopWallTests.cs:161`.
- [ ] `tools/e2e/capacity-9r.test.ts` -- new suite. In the Free workshop, place 32 `pipe_bend_90` through the real drawer (`selectTool` + `placeOnCanvas`). Then Run, Reset (assert the 9R.1 snapshot), Save, reload and Load (assert the snapshot). Assert no worker fault and no CCGPU Faulted acknowledgement.

**Acceptance Criteria:**
- Given a construction within the 32-instance budget, when Run compiles it, then the collider table is sized from the compiled construction's actual collider count, so every Build-admitted construction compiles.
- Given a native property test, when it builds 32 instances of each catalogue element (including `pipe_bend_90` and `funnel`, 2,305 colliders) and the 30-instance mixed case, then each compiles and steps. Elements limited by another declared budget, and the two non-constructible parts, are handled per D2.
- Given a construction over the 32-instance budget, when Build validates it, then it is still rejected with the existing message (negative control).
- Given actual Chrome, when `tools/e2e/capacity-9r.test.ts` places 32 `pipe_bend_90` through the real drawer, runs, resets (snapshot exact) and save/loads, then there is no worker fault.
- Given the gates, when they run:
  - `dotnet build CuriousContraptions.slnx` reports 0 warnings;
  - `dotnet test CuriousContraptions.slnx` passes 100%;
  - every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

## Implementation Notes

Amelia-7, 20 Oct 2026, on main 8a77f9a9 (9.4b, 9R.0, 9R.0b and 9R.1 landed). Line references refreshed against that tree.

**As built (supersedes the Code Map and Tasks where they differ; see the Spec Change Log for why).**
- Layout (`engine/gpu/PhysicsGpuAbi.cs`): header, bodies, materials, sensors, guides, triggers, contact works, work occurrences, orientation sensors, motion, electrical sources and bindings, prismatics, then the collider table as the tail. `CollidersOffset` stays a constant (the end of the fixed region), so `CompiledMemoryLayout`, `ReadTrigger`, `ReadOrientationSensor`, `ReadContactWorks` and the engine's collider reads needed no change. `ByteLength(int colliders)` and `ByteLength(ReadOnlySpan<byte> header)` are the one length authority; the header form checks `Length >= HeaderBytes` first, computes in `long` from `HeaderColliderCountOffset`, and rejects a length beyond `int.MaxValue` with `ArgumentException` (no overflow exception).
- `Header()` requires `data.Length == ByteLength(data)` and the reserved word `HeaderReservedOffset` (112, formerly the cache slot count) to be zero. `ValidateCandidate` also requires equal candidate and source lengths; the header comparison already binds equal collider counts. Immutability is `materials..sensors` plus the whole collider tail `CollidersOffset..`. `ValidatePrismaticCandidate`'s unused-row check now ends at `CollidersOffset` (it was open-ended).
- The ABI contact cache is deleted, not resized (coordinator decision 1): the pair loop, `CacheOffset`, `CacheCapacity`, `CacheBytes`, `ValidateCache`, the already-dead `ValidFeature`, and the cache test `GenericCacheRejectsEveryChangedInactiveOrImmutableByte`. The engine's warm-start cache is its own dictionary and never read the region.
- `ContactWorkOccurrence.Validate` drops its `ColliderCapacity` clause; `ReadContactWorks` still binds a non-zero occurrence's collider slot to the header's collider count. Collider slots are u16; `TheLargestBuildAdmittedColliderPopulationFitsSixteenBitColliderSlots` proves 1 + 32 x 72 fits.
- `BrowserGpuTransport` sizes the current and candidate buffers from the admitted input and reallocates only when the length changes, keeping each array at its world's exact length (`WorkshopGpuDevice` compares with `SequenceEqual`). `WorkshopGpuDevice` validates each buffer against its own header. `WorkshopPhysicsEngine.Advance` requires `source.Length == ByteLength(source)` and an equal candidate length; the warm-up mocks are sized from their own collider counts (0 and 2).
- The stale `worker.js` `let` offsets and their `WorkerAbiTests` facts were already deleted by Story 9.4b; nothing to do.
- `ShaderPreamble` loses `WORLD_BYTES` as well as `COLLIDER_CAPACITY`: deleting the fixed `ByteLength` removes the value it printed. No consumer reads the preamble (`BrowserGpuTransport.Initialize` ignores it).
- Broadphase (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs`, decision 6 reversed, see the Spec Change Log): nodes are an array accessed by `ref`, and each node carries `HasDynamic`; `QueryOverlaps` skips a subtree pair, or a self-traversal, that holds no moving leaf. The skipped pairs are exactly those the engine's callback rejects as static-static, so the tree, the accepted pair order and every trajectory are unchanged (11 scenes bit-identical over 240 ticks: `evidence-9r-2/probes-bvh.cs.txt`).
- Tests: `WorkbenchCapacityTests` (the property theory over `WorkshopInventoryPolicy.Free` with the D2 budget table; 0, 28, 29 and 32 bends; 31 bends with a ball resting in the last bend, slots > 2,048, and its no-cradle control; 33 bends rejected; one transport admitting 1, 32, 1, 32 bends; the u16 fit; the 30-instance mixed bench now stepped), `WorkshopWireTests` (`GenericWorldLengthIsDerivedFromItsOwnColliderCount`, `GenericCandidateRejectsAChangeAnywhereInTheColliderTail`, padding regions re-pointed), the element tests assert `ByteLength(scene.Colliders.Length)`, and `tools/e2e/capacity-9r.test.ts` (declared Parallel in `SuiteCatalogue`; it has no timing-detector hit, so it cannot declare SerialAlone without an owner decision).
- Chrome bench, test 1: a static bench has no committed body, and the 9R.1 snapshot requires one, so its Reset and Load legs compare the Save acknowledgement's committed read (`readSnapshot` + `assertSnapshotEqual`) and the stored slot byte for byte (Load: per-instance identity, cell and rotation, since the page revision restarts). Tests 2 and 3 use the 9R.1 snapshot helper (`bodies: 1`).

**Risks, rebaselined under AOT (9R.0b).**
- Tick cost: natively (Release JIT) the 32-bend bench costs 2.79 ms per tick after the broadphase change (3.70 ms before; insert 2.27 ms, query 0). The BVH is still cleared and every static leaf re-inserted each tick, and insertion is unbalanced, so the per-tick re-insert cost still grows with the static collider count (the query cost no longer does). Filed for the performance qualification gate (static tree built once at admission), which changes pair order and so needs an owner decision.
- Admission cost: gone with the pair loop.

## Spec Change Log

- **20 Oct 2026, design review (coordinator decisions, folded in by Amelia-7).**
  1. The dead ABI contact cache is deleted instead of resized; header word 112 is reserved zero. This removes the 888-pair ceiling (8 Powered Gates already faulted Construct on main with "Contact cache capacity exceeded.": `evidence-9r-2/probes.cs.txt`), the O(n^2) admission loop and the pair-order golden. The Tasks' pair-loop item, its byte-identity test, its time-bound telemetry, the 888/889 boundary rows and mutation 2's "restore the fixed cache" become: re-adding an 888-pair admission ceiling must fail the gate and cradle tests.
  2. Non-vacuous proof: native and Chrome legs with 30 bends, a cradle bend (rotated -135 degrees about Z) and a Basketball resting inside it on collider slots above 2,048 (31 x 72 + plane + ball = 2,234 colliders), plus the no-cradle control where the ball falls.
  3. Transport: reallocate only when the length changes, exact-length arrays; an admit test over 1, 32, 1, 32 bends.
  4. Boundaries: `Length >= HeaderBytes` before any header read; u32 counts computed in `long`, never overflowing; equal candidate and source lengths; the u16 collider slot fit test; plane-only and maximum rows in the matrix.
  5. Line references refreshed; the `worker.js` task dropped (already done); named header constants used throughout.
  6. **Reversed by evidence.** The per-tick static BVH cost was filed as performance, but the first capacity-9r Chrome run (scratchpad run `cap2`, 2026-10-19T20:48Z) raised 5 `CCGPU_DEBT_FAULT`s on the 2,305- and 2,234-collider benches after the smoke pair had finished, which breaches AC4 ("no worker fault"; the Risks section makes a capacity-9r fault a Fail). The behaviour-preserving broadphase change above fixed it (run `cap3`: 3/3, 0 debt faults). The remaining cost (re-insertion) stays filed. Needs coordinator confirmation.

## Review Triage Log

## Review Triage Log

## Design Notes

**Legacy deleted:**
- `ColliderCapacity` and `CacheCapacity`;
- the fixed `PhysicsGpuAbi.ByteLength`;
- the WGSL `COLLIDER_CAPACITY` and `WORLD_BYTES` constants;
- the ABI contact-cache region, its admission pair loop, `CacheOffset`, `CacheBytes`, `ValidateCache`, the dead `ValidFeature`, and the cache test; header word 112 (`HeaderCachePairCountOffset`) is now `HeaderReservedOffset`, required zero;
- the `List<BvhNode>` node store and the unconditional static-static traversal in `WorkshopDynamicBvh`;
- `worker.js` `COLLIDERS_OFFSET`, `MATERIALS_OFFSET`, `TRIGGERS_OFFSET` and `CONTACT_WORKS_OFFSET`, with their `WorkerAbiTests` facts (already deleted by Story 9.4b);
- the `== 2048` assertion and the `<= ColliderCapacity` assertion;
- the Run-time throw for admitted constructions.

**Test-first plan.**
1. The property theory is red today:
   - 29 or more bends fail on the collider ceiling;
   - 31 bends + ball fail on the cache ceiling.
2. Chrome proof: `capacity-9r` through actual UI.
3. Negative control: 33 instances.
4. Boundaries:
   - 28 bends (2,017) and 29 bends (2,089), which straddle the old ceiling;
   - pairs of 888 and 889, which straddle the old cache ceiling.
5. Run/Reset uses the 9R.1 snapshot.
6. Save/Load: the round trip in Chrome, plus native codec tests.

**Mutation plan:**
1. Re-add a 2,048 check, so the theory fails.
2. Restore the fixed cache, so 31 bends + ball fails.
3. Size the transport buffers from the first admission only, then admit a larger scene, so the native test fails.
4. Drop the collider tail from the immutability check, so `WorkshopWireTests` geometry-change detection fails.

**Impacted suites:**
- Native: `WorkbenchCapacityTests`, `WorkshopWireTests`, `WorkshopPhysicsEngineTests`, `WorkshopGpuDeviceTests`, `WorkerAbiTests`, every element test that pins the buffer length, `ContactWork*`.
- Chrome: all suites (shared ABI), plus `capacity-9r`.

**Risks.**
- **Tick cost.** `WorkshopPhysicsEngine.Advance` clears and re-inserts every collider into the BVH each tick (`:623-659`), and Story 9.4b measured 3–4 ms per tick in interpreted WASM. 2,305 colliders can overrun ticks and trigger the unresolved 9.4b clock-recovery transport fault. A fault in `capacity-9r` is a Fail, never waived. Record tick telemetry and coordinate with Story 9R.0b (the AOT build).
- **Admission cost** is addressed by the pair-loop task. If dynamic-only enumeration changes the pair order, the byte-identity test fails; fix the order, never the test.

**Refresh after Story 9.4b lands.**
- The `WorkshopPhysicsEngineTests` harness is untracked 9.4b work.
- 9.4b's clock transport governs the Chrome gate. cat-023b AC4 is NOT MET and needs an owner decision.
