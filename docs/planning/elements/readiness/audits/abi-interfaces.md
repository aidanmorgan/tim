# ABI, wire and engine-interface audit — 23 November 2026

Read-only audit of published main `25df3cd5` (checkout `spec-drafts`), for owner decision 24j (`docs/planning/elements/readiness/owner-decisions-2026-11-22.md:62`). Nothing was built, run or measured. Engine internals (SoA, BVH, islands, tick allocation inside the solver) are in `.tools/audit/engine-design-gap.md` (cited as `EDG`) and are not repeated; this report covers what crosses a boundary: schemas, codecs, worker messages, shared memory and the interfaces packages and the engine implement.

Shorthand: `SIM/` = `CuriousContraptions.Simulation/`, `ABS/` = `CuriousContraptions.Abstractions/`, `GPU/` = `engine/gpu/`, `WEB/` = `CuriousContraptions.web/wwwroot/`, `ANIM/` = `CuriousContraptions.Animation.Worker/`, `FBS` = `schemas/workshop_wire.fbs`, `DOC` = `docs/gpu-f32-physics.md`, `epics` = `_bmad-output/planning-artifacts/epics.md`. Severity: **C** blocks correctness, **S** blocks scale, **H** hygiene.

## Summary

- FlatBuffers carries only the control plane and the save: commands, the per-tick committed read, the compiled-scene install and `SavedConstruction`. The C# accessors are committed `flatc` output; S5 makes them build output. S5b (no contract on disk) would regenerate only the TypeScript copy used by deferred e2e tooling. **After S5 and S5b, most wire knowledge is still hand-coded:** about 3,500 lines of C# mapping codecs, including hand-built vtable arithmetic; an opaque `motion: [uint8]` blob carrying a hand-laid physics record inside the schema; and about fifteen non-FlatBuffers binary layouts hand-mirrored in TypeScript. Some of those TypeScript offsets are pinned by no test.
- **The `SharedArrayBuffer` pose ring is write-only.** The simulation fills it every tick, but no production code reads it. The renderer is fed by a full FlatBuffers read posted by `postMessage` every tick and decoded on the main thread (and again on the animation worker). This corrects `EDG` row 54, which calls the ring "functionally sound". The ring's reader also uses the plain-access seqlock pattern the project itself rejected for the event ring.
- **Physics is back-pressured by both cosmetic consumers.** This goes beyond the install receipts `ENGINE-ONE-WAY-INSTALL` already names: a 500 ms main-thread stall stops the run, and the animation worker receives every tick with no coalescing.
- The engine interfaces packages implement are data-only: property bags, no element callbacks, typed ids, enums. That part meets 24j. But one capability fans out across about nine hand-maintained central lists, from the package interface to the TypeScript mirror. Schema enums are mostly raw integers. On the current runtime configuration (no WASM threads), neither parallel islands nor true zero-copy between the C# runtimes is possible; one owner decision covers both.

## 1. FlatBuffers generation

### 1.1 Schemas and where they are used

| Schema | Roots | Carries | Evidence |
|---|---|---|---|
| `FBS` (1,174 lines; 8 enums, 74 tables, 45 structs, no `union`) | `WorkshopResponse` (`CWRM`), `WorkshopCommand` (`CWCM`), `WorkshopObservationBatch` (`CCWO`) | commands; the per-tick committed read (`FBS:282-331`, 41 fields, one vector per capability); the compiled-scene install (`CompiledSceneRecord`, `FBS:1000-1050`); diagnostics | `FBS:1122` root_type, framing comment `FBS:348-352` |
| `schemas/workshop_authoring.fbs` (95 lines) | `SavedConstruction` (`CCWS`) | authored construction and saves; parameters are `key:string` pairs (`:40`) | `:1-2` include, `:94-95` |

There is no format version by owner decision (`schemas/README.md:20-21`; `DOC:134`). Refusal is structural: size prefix, identifier, the vendored verifier, then hand-written unknown-field and enum checks (see 1.3). The bundle is not FlatBuffers. It is source-generated `System.Text.Json` with unknown-name, unknown-field and missing-field refusal (`CuriousContraptions.Bundle/BundleSchema.cs:12-15`).

### 1.2 What is generated, and how

| Artefact | Generated? | Pinned? | In build? | Committed? |
|---|---|---|---|---|
| `GPU/WorkshopWireGenerated.cs` (6,872 lines), `GPU/WorkshopAuthoringGenerated.cs` (407) | `flatc --csharp`, then joined by `tools/WorkshopWireGeneration` | 25.2.10 by README text only (`schemas/README.md:24-26`); runtime stamp `FLATBUFFERS_25_2_10` (`CuriousContraptions.FlatBuffers/FlatBufferConstants.cs:35`) | No; linked into the vendored project (`CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj:16-19`) | Yes |
| Upper part of `tools/e2e/workshop-wire.ts` (lines 1-12731) | `flatc --ts`, joined by the shell and `sed` pipeline at `schemas/README.md:41-58` | No | No | Yes |
| TypeScript FlatBuffers runtime | — | **No.** `tools/e2e/tsconfig.json` maps `flatbuffers` to `/opt/homebrew/lib/node_modules/flatbuffers`. It is not in `package.json` or `package-lock.json`. | — | — |

S5 (`_bmad-output/implementation-artifacts/spec-s5-generate-wire-accessors-in-build.md`) fixes row 1 properly: digest-pinned `flatc` 25.2.10, generation into `obj/`, the committed files and the joining tool deleted. It explicitly excludes the TypeScript (`:42`) and the hand-written codecs (`:41`). S5b is decided (owner decision 24h, `owner-decisions-2026-11-22.md:59`) but has no contract or story text anywhere in `docs/` or `_bmad-output/`.

No production TypeScript reads FlatBuffers. `workshop-wire.ts` is imported only by `tools/e2e/*` (Chrome suites, deferred at `tools/E2eRunner/BrowserDeferral.cs:42`, `:52`). The browser main thread and both workers decode FlatBuffers in C#. S5b therefore affects deferred tooling only, and it must pin the TypeScript runtime.

### 1.3 Hand-coded wire code that remains after S5 and S5b

| # | Hand code | Evidence | Why it matters |
|---|---|---|---|
| W1 | Domain-to-accessor mapping codecs, about 3,470 lines | `GPU/WorkshopWireFlatBuffers*.cs` (10 files), `GPU/WorkshopAuthoringFlatBuffers.cs`, `GPU/WorkshopSaveCodec.cs` | A schema field needs a hand encode and a hand decode. This is normal mapping, but it is where per-field validation lives. |
| W2 | Hand vtable arithmetic, with field counts and slot indices typed in by hand | `GPU/WorkshopWireFlatBuffers.cs:45-53` (`CommandIdentifier => 12, ResponseIdentifier => 41`; `4 + fieldCount*2`), `:59-82` (scene slot 11/27, presentation slot 29, declaration slot 5, field limits 17 and 20); `GPU/WorkshopAuthoringFlatBuffers.cs:44-73` (puzzle vtable 10..14 fields), `:76-79` (`InstanceRecordFields = 7`, slots 6 and 10), `:84-100` (`__offset`, `__indirect`, `bb_pos`) | Recounted against `FBS`: today every literal matches (WorkshopResponse 41 fields with `scene` at 27; WorkshopCommand 12 with `scene` at 11; CompiledSceneRecord `presentation` at 29; CompiledPresentationRecord 17 with `declaration` at 5; CapabilityPresentationBindingRecord 20). Drift would fail loudly, so this is not a live defect. The gaps: unknown-field refusal is hand-coded for 6 tables and absent for the other ~70; no test ties a literal to the schema (none found in `CuriousContraptions.tests/`); and the literals are wire knowledge outside generation. **H**, with a partial-refusal gap. |
| W3 | `motion: [uint8]` tunnels a hand-laid record (`PhysicsMotionRead`, 80-byte motion pieces, `DOC:128`) through the schema | `FBS:305-306`; encode `GPU/WorkshopWireFlatBuffers.cs:495` (`committed.Bytes.ToArray()`); decode `:694-697`; `GPU/PhysicsMotionRead.cs:47-52` | The bytes the renderer interpolates from are not schema-defined, and a per-read copy is made at each end. **H/S** |
| W4 | Raw-integer enums: about 51 kind, phase, mode, state and reason fields are `uint`/`ubyte`, against 8 schema enums | e.g. `FBS:286` `reason: uint8`, `:132` `kind: uint32`, `:205`, `:234`, `:469` `shape: uint`, `:746`, `:821-837`; `schemas/workshop_authoring.fbs:16`, `:68` | Generated TypeScript gets no enum types, so every enum is hand-mirrored (W6). Refusal is per-site hand code. It is present where sampled: `GPU/WorkshopWireFlatBuffers.cs:586-593` (`Enum.IsDefined` on the response discriminants); `ObservationEnum<T>` with an `IsDefined` and round-trip check (`GPU/WorkshopWireFlatBuffers.Observation.cs:56-59`), used throughout `.Compiled.cs` (e.g. `:62`, `:136`, `:293`). One sampled site casts without a local check: `GPU/WorkshopWireFlatBuffers.cs:864` (activation memory kind and phase). `ABS/ActivationMemory.cs:29` validates them, but I did not confirm the decode path calls it. **H**, latent C at unchecked sites. |
| W5 | Hand TypeScript helpers below the generated part: frame checks, `dumpAuthored`, and mock encoders driving `flatbuffers.Builder` by hand | `tools/e2e/workshop-wire.ts:12732-13101` (encoders at about `:12915`, `:13060`); field listing `tools/e2e/run-reset-snapshot.ts:14` (`SNAPSHOT_SCHEMA`) | Deferred tooling, but each is a second hand copy of the schema. **H** |
| W6 | Hand TypeScript enum mirrors in **production** runtime script | `WEB/workshop-observation.ts:3-49` (about 45 enums: `WorkshopRejection`, `ActivationNodeKind`, `PortRole`, `SimulationEventKind` …); known drift `EDG` E13 | Owned by `ENGINE-CAPABILITY-INSTANCES` ("TypeScript mirrors are generated from the C# owners in the build", `epics` contract). **C (latent)** |
| W7 | Every non-FlatBuffers binary layout (section 2), hand-offset in C# and mirrored by hand in TypeScript | section 2 table | Not addressed by S5, S5b or any story. |

**Answer to the owner's question:** no. S5 and S5b as planned make the FlatBuffers *accessors* generated, for C# in the build and for TypeScript only in deferred e2e tooling. W1-W7 remain. "Never hand-coded" (24j) is unmet for W2, W3, W5, W6 and every layout in section 2.

## 2. ABI suitability per format

| Format | Defined by | Typed refusal | f32 / enums | Aligned, zero-copy | Element-free, data-driven | Sized from declarations | Findings |
|---|---|---|---|---|---|---|---|
| **Compiled scene install** (`CompiledSceneRecord`) | `FBS:1000-1050` | Verifier, plus W2 and W4 hand checks | f32 yes; enums raw (W4) | Decoded into objects; not zero-copy (not hot) | One vector per capability, about 44 vectors. `CompiledPresentationRecord` carries one optional id field per capability (`FBS:853-871`: guide, transmission, retention, finite_work, store, gas_shape, integrity, airflow). `CapabilityPresentationBindingRecord` carries fixed per-capability state tables (`FBS:792-815`, `:817-838`). `CapabilityActivationNodeDeclarationRecord` is a kitchen-sink row whose fields belong to particular kinds (`FBS:743-761`). There is no `union`. | Vectors, yes | Capability-named rather than element-named (product names: `EDG` E1, E2). Every new capability edits shared records. **H/S of change** |
| **Committed read** (`WorkshopResponse`, every tick) | `FBS:282-331` | As above | f32 yes; `reason: uint8` | **Encoded whole every tick**: `SIM/Program.cs:335-348`, builder reused (`GPU/WorkshopWireFlatBuffers.cs:204`, `:467`), `SizedByteArray` copy (`:568`). Decode copies (`:36`), verifies and builds managed objects on the main thread and again on the animation worker (`ANIM/Program.cs:213-215`). | Capability vectors; E1 names in `FBS:157-159` | Vectors, yes | It doubles as the renderer's pose feed (section 3). **S** |
| **Save** (`SavedConstruction`) | `schemas/workshop_authoring.fbs` | Verifier, identifier, unknown fields (W2), unknown bundle element refused (`schemas/README.md:15`) | f32; `domain`, `goal_kind` raw `uint32` (`:16`, `:68`) | n/a | Parameters by key; `inventory_part` singular (`EDG` E11) | yes | Suitable apart from W2 and W4. |
| **Bundle** | C# records, source-generated JSON (`CuriousContraptions.Bundle/BundleSchema.cs:12-15`) | Unknown names, fields, nulls | Enums by name | n/a (load time) | One property per capability interface (`BundleSchema.cs:31-54`); compliant surface, residence sensor and planar guide singular (`:37-40`; `EDG` E6) | yes | Suitable as a format; the shape is `ENGINE-CAPABILITY-INSTANCES`'. |
| **Physics world** (worker-internal) | Hand constants: `GPU/PhysicsGpuAbi*.cs` (17 files, 3,080 lines) and `GPU/PhysicsWorldLayout.cs:14-61` (about 31 positional tables) | Header and padding refusal (`DOC:134`); literal-offset scan `WorkshopAbiLiteralScanTests` | f32 throughout (`DOC:117`, `:128`) | AoS byte records, copied and decoded into objects every tick (`EDG` row 1) | Engagement table surface × dynamic body (`PhysicsWorldLayout.cs:41-42`; `EDG` E6, D5) | Counts from the header, no ceilings (`:6-9`) | Owned by `ENGINE-SOA-SIMD`. **S** |
| **Pose ring** (`SharedArrayBuffer`) | `GPU/WorkshopPoseRing.cs:11-17`; mirrored in `WEB/pose-ring.ts:12-15` | Whole-slot length check (`WorkshopPoseRing.cs:34-39`; `pose-ring.ts:18-22`) | f32 position, quaternion, linear velocity; u64 id at 48 | Each 64-byte record is aligned. The header has no tick, epoch, body count or angular velocity. | yes | Grows from the committed population (`WorkshopPoseRing.cs:19-26`) | Section 3.1: write-only, copied, not torn-read-safe. **C (latent) / S** |
| **Event ring** (`SharedArrayBuffer`) | `ABS/EventRingBuffer.cs`; `WEB/event-ring-cursor.ts:14-20` | Header layout and reserved words (`event-ring-cursor.ts:50-60`), no shrink or rewind (`:105-106`) | 64-byte records of u64 words | Every word access atomic (`:9-11`, `:72-76`); slow readers lap and count skips (`:143-155`) | yes | Capacity from the publication (`WorkerAbiTests.cs:24-25`) | **Best-built shared layout in the tree.** Copies: C# batch → JSImport → `new Uint8Array(batch)` → ring (`SIM/wwwroot/worker.ts:63-64`, `:94-100`). |
| **Clock ring** (`SharedArrayBuffer`) | `GPU/WorkshopClockWire.cs`, `WEB/clock-ring.ts` | Pinned by `ClockRingScriptTests` | integer ns | `Atomics.wait` in the physics worker, up to 2 ms every 100 ms (`clock-ring.ts:246`, `:270`; `GPU/WorkshopClockDerivation.cs:39-40`) | yes | fixed | A bounded block between tick turns. **H**; measure in R6. |
| **Schedule controls** (Prepare, Commit, Prepared, Applied, Presented) | `GPU/WorkshopSchedule.cs`, plus **literal offsets**: `SIM/Program.Schedule.cs:134-140` (`data[32..]` to `[72..]`), `:168-171` (`AsSpan(64/72/76)`), `:248`; `ANIM/Program.cs:232-234` | Header identity checks (`Program.Schedule.cs:164-169`) | enums as u32 | posted by transfer | yes | per install | Not covered by the literal scan, which covers world records only (`DOC:117`). **H, latent C** |
| **Animation control and output** (104 bytes each) | `GPU/WorkshopHint.cs:7-47` (`WorkshopAnimationWire`) | length and kind checks | f32 value, u16 property | One `postMessage` per target sample (section 3.3) | yes | — | Animation worker reads **literal** offsets `getUint32(60)` and `getBigUint64(64)` (`ANIM/wwwroot/worker.ts:78`, `:80`), unpinned (section 2.1). |
| **Clock peer bootstrap** | `GPU/ClockDerivationWire.cs`, `GPU/RuntimeSessionCodec.cs` | Main-thread checks with **literal** offsets 0, 4, 24, 32, 40, 48, 56, 60 (`WEB/workshop-client.ts:232-235`) | — | — | yes | — | Unpinned (section 2.1). |
| **Trace record, canonical body, response/trace body, mechanical link, cadence, receipts** | `GPU/WorkshopTraceRecord.cs`, `GPU/CanonicalBodyCodec.cs`, `GPU/PhysicsBodyWire.cs`, `GPU/MechanicalLinkWire.cs`, `GPU/WorkshopCadence.cs`, `GPU/WorkshopClockWire.cs` (`ReceiptBytes`) | Padding and length refusal | f32 | — | yes | — | All hand layouts (W7). |

### 2.1 TypeScript layout mirrors and what pins them

`CuriousContraptions.tests/WorkerAbiTests.cs:67-83` reads four scripts and checks that every `const NAME = n;` equals its C# owner (`:78`). It does not see:

- the Animation worker script at all: its `Script` enum (`:12`) omits `ANIM/wwwroot/worker.ts`, whose output kind and target offsets are literals (`:78`, `:80`);
- inline literal offsets in the checked scripts: `WEB/workshop-client.ts:232-235` (peer bootstrap), `WEB/pose-ring.ts:131-145` (`b + 11`, `b + 12`, lanes 0-10), `SIM/wwwroot/worker.ts:48` (`getUint32(4)`).

If the C# owner moves a field, these misread silently. **C (latent).**

## 3. Parallel reads and writes

### 3.1 The pose ring is not the presentation feed

- **Producer:** every tick `SIM/Program.cs:347` calls `ComposeSlot`, which allocates a new `byte[]` (`GPU/WorkshopPoseRing.cs:58-64`). JSImport marshals it to JavaScript as a copy (`SIM/wwwroot/worker.ts:286-290`), and `PoseProducer.publish` copies it into the next slot (`WEB/pose-ring.ts:87-99`).
- **Consumers:** none in production. `PoseReader` is constructed and swapped (`WEB/workshop-client.ts:152`, `:256-257`), and nothing calls `read`, `latest` or `TryReadSlot`. No `workshopClient` export or `[JSImport]` reaches it (`GPU/BrowserWorkshopClient.cs:416-491`). The readers are exercised only by `tools/runtime/pose-ring.test.ts` and by `CuriousContraptions.tests/WorkshopPoseRingTests.cs:105`, `:148`, `:164` and `WireFaultTests.cs:273`.
- **Actual feed:** the full `WorkshopResponse` is posted to the main thread by transfer (`SIM/wwwroot/worker.ts:219-225`) and decoded in C#. `WorkshopPoseHistory` keeps seven decoded responses (`GPU/WorkshopPoseHistory.cs:12-15`), and every frame `TrySampleWorld` allocates `new PresentedBody[]` (`:276`, `:301`). `MachinePart.Apply` then writes one Godot node transform per body (`engine/MachinePart.cs:342-350`). No `MultiMesh`, `RenderingServer` or instanced draw exists in `engine/`, `ui/` or `WEB/`. The instanced-batch claim of `DOC:70-71` and `epics:47` is unimplemented; `EDG` section 7 had left it unaudited.
- **The ring could not be the feed as laid out.** Its header holds only a sequence and a capture time (`WorkshopPoseRing.cs:55-57`), and its record has no angular velocity or motion pieces. Presentation brackets on tick and interpolates from motion pieces (`WorkshopPoseHistory.cs:285-306`).
- **Torn reads.** `WEB/event-ring-cursor.ts:9-11` records a review finding (F1): plain payload accesses inside a seqlock can be reordered past the sequence loads and pass both checks torn, so every word there is atomic. The pose ring uses exactly that plain pattern on both sides: the producer's `target.set` (`pose-ring.ts:96`) and the reader's `DataView`/`Float32Array` loads between two `Atomics.load` (`:123-150`). The C# reader uses plain `BinaryPrimitives` with no `Volatile` reads (`WorkshopPoseRing.cs:109-138`). **C (latent)**: nothing reads the ring yet.
- **Reader cost:** `latest()` allocates candidate arrays and a sort (`pose-ring.ts:157-160`); `read()` allocates an object and a `bigint.toString()` per body (`:128-147`). That would not meet "zero heap allocations" (`DOC:21`).
- **Slow reader:** the producer rotates over 3 slots without regard to readers (`:91`). A reader that loses both newest candidates gets `null` (`:161-165`). That is acceptable for a renderer, provided it holds its previous frame.

**This corrects `EDG` row 54**: the ring is a functioning write path with no reader, not a working copy-path ring. `ENGINE-TICK-ALLOCATION`'s clause "the pose slot written into reused storage" (`epics` story text) optimises a buffer nobody reads.

### 3.2 Physics → main thread

- **One read in flight, newer reads coalesced** (`SIM/wwwroot/worker.ts:19-21`, `:219-225`, `:286-296`). The main thread receipts on arrival (`WEB/workshop-client.ts:271-279`).
- **A slow main thread stops physics.** A read unreceipted for 500 ms is a stall (`GPU/WorkshopReliableReadLedger.cs:18`, `:79-80`). `DebtExceeded` then triggers `StopForDebt`, which faults the run (`SIM/Program.cs:275-297`). A main-thread hitch over 500 ms (a long task such as GC or an asset load, or a frozen page) ends a puzzle run, so the renderer flows back into physics. **C (lifecycle)**; absent from `ENGINE-ONE-WAY-INSTALL`.
- **Coalescing against adjacent-tick interpolation.** The loop runs up to 4 ticks per turn (`GPU/WorkshopTickLoop.cs:15`, `:38`), and a receipt can only arrive between turns, so during catch-up the main thread receives tick N and N+k only. `TrySampleWorld` interpolates only between adjacent ticks (`WorkshopPoseHistory.cs:298`) and otherwise reports `AwaitingHistory` (`:241-242`). Expected effect: frames that do not advance during catch-up. **C (presentation smoothness), unproven.** Measurement: a node harness counting non-adjacent brackets and `AwaitingHistory` frames per second under a forced 3-tick catch-up.

### 3.3 Physics → animation worker → main thread

- Every committed read is `slice()`-copied and posted to the animation port, with no coalescing and no back-pressure (`SIM/wwwroot/worker.ts:291-294`). The animation worker verifies and decodes all of it (`ANIM/Program.cs:213-215`) even when it uses little of it. If the worker falls behind, the `MessagePort` queue grows without bound. **S / C (memory).** Measurement: queue depth and heap with the 1 ms service interval (`ANIM/wwwroot/worker.ts:117-124`) throttled in a node harness.
- Output to the main thread is **one 104-byte sample per animated target per `postMessage`, with one sample in flight across all targets** (`ANIM/wwwroot/worker.ts:51-57`, `:76-82`, `:134-138`; `WEB/workshop-client.ts:191-203`). With T targets, a full refresh needs T round trips. **S.** Measurement: the age of the oldest target sample against T.
- **The install protocol waits on both consumers**: Prepared (`SIM/Program.Schedule.cs:97-103`), Applied (`:223-229`) and, for Step, Completed and Reset, Presented (`:231-235`), each with a 250 ms timeout (`:155`). This is already owned by `ENGINE-ONE-WAY-INSTALL`.

### 3.4 Shared mutable state and parallel islands

- No project sets `WasmEnableThreads` (`SIM/CuriousContraptions.Simulation.csproj:2-31`; no `.props` or `.targets` match). The worker is full LLVM AOT (`:19-21`). A managed heap cannot be shared between separate worker runtimes, so as configured, `DOC:274`'s work-stealing over `SharedArrayBuffer` is impossible for managed state. **Open check, owner: `ENGINE-SOA-SIMD` planning:** whether the pinned SDK supports threads with `AOTMode=LLVMOnly`. If it does not, parallel islands require SoA tables in a `SharedArrayBuffer` shared by several runtimes, and that layout decision belongs in `ENGINE-SOA-SIMD` now. **S (later).**
- Process-wide state: `[ThreadStatic]` restitution scratch (`SIM/WorkshopConstraintSolver.cs:642-643`); static worker state in `SIM/Program.cs:13-35` and `SIM/Program.Schedule.cs:9-36` (host-level, not per island); `lock (Gate)` in `GPU/DeclaredPhysics.cs:32-64` and `GPU/ElementPortCatalogue.cs:312-340`. The engine instance owns everything else, but as one world-wide object graph with no islands (`EDG` rows 1, 43-45).
- **Zero-copy floor (same root cause).** The physics worker and the main-thread renderer are separate .NET WASM runtimes. Without `WasmEnableThreads`, C# in either one cannot address a JavaScript `SharedArrayBuffer` or run `Atomics` on it; only JavaScript can. Today every shared-memory exchange therefore costs at least one JavaScript-mediated copy per side: producer storage into the ring, then ring into a C# buffer. "Without copies" (24j; `DOC:21`) is not reachable as configured. The cheapest option to check on the pinned SDK is a JSImport `Span<byte>` parameter marshalled as `JSType.MemoryView` (a view of WASM memory valid for the call) in place of today's `byte[]`. The repository does not use it today (no `MemoryView` marshalling in production code). This and parallel islands are one owner decision: enable WASM threads, or accept one bounded copy per side.
- Nothing in a tick reduces results across threads, so deterministic reduction is untested by construction. `ENGINE-PARALLEL-ISLANDS` (`epics`, order 38) states canonical island commit order, which is the right rule.

## 4. Engine interfaces

| Interface family | Data-driven | Typed | Batch / SoA-friendly | Sufficient for planned capabilities |
|---|---|---|---|---|
| Package declarations: 29 `I*Element` capability interfaces (`ABS*/`) plus `IPuzzleElementDeclaration` (`ABS/IPuzzleElementDeclaration.cs:5-14`) | Yes. Property-only, no methods or callbacks; no `Func`/`Action` in declaration types. | Yes: enums and typed ids | n/a (compile time) | Shape only: singular capability slots (`EDG` E6) and one interface per capability. Adding one touches the interface, `BundleSchema.cs` (`:31-54`), the validator, `FBS` (scene, read and presentation fields), the hand codec, `PhysicsWorldLayout` plus a `PhysicsGpuAbi.*` file, `ICompiledMemoryLayout` getters (`ABS/CompiledRuntimeContracts.cs:49-114`), the W2 literals and the TypeScript mirrors. S6 tables the branch chains, not this fan-out. |
| Compiled layout and lookup (`ICompiledMemoryLayout`, `IPhysicsWorldLayout`) | Yes | Partly: prismatic, revolute, compliant and transmission ids are raw `ulong` (`CompiledRuntimeContracts.cs:107-114`) | Per-id interface calls; slot lookups, not columns | Replaced by `ENGINE-SOA-SIMD` |
| Runtime services (`ABS/RuntimeServices.cs`, `CommandServices.cs`, `RuntimeDiagnostics.cs`) | Yes | Yes | Virtual DI interfaces with delegate parameters on host paths, e.g. a `Func<SupplyNodeId,bool>` per supply resolve (`CompiledRuntimeContracts.cs:229`) | Host-side cost belongs to `ENGINE-TICK-ALLOCATION` |
| Planned spatial query (`ENGINE-SPATIAL-QUERIES` contract) | Yes | — | **Single-query as written**: box, region, point and segment, with results in canonical order | Optics, acoustics, radiation and liquid streams cast many segments per tick. The contract should state batched queries: N inputs in columns, results into caller-provided spans, no delegate or allocation per hit. |
| Signal, fluid, optical and thermal contracts | Declared ports and nodes (`ABS*.Signal`, `.Fluid`) | Enums | Signal network is host-side O(V × N) (`EDG` section 4) | Owned by `ENGINE-SIGNAL-NODES`, `ENGINE-GAS-NETWORK`, `ENGINE-OPTICAL`, `ENGINE-THERMAL`. Each adds read vectors to `WorkshopResponse` and, today, hand codecs (W1-W4). |

## 5. Gaps, severities, measurements and owners

| # | Gap | Sev | Measurement or check | Owner |
|---|---|---|---|---|
| G1 | Pose ring write-only; renderer fed by full FlatBuffers read and decode per tick; per-frame allocation; per-node transforms, no instancing | S (and the stated architecture is false) | R6 publication path; main-thread bytes allocated per frame; decode time per read against body count | **New: ENGINE-WIRE-ABI** (below). Remove the pose clause from `ENGINE-TICK-ALLOCATION`. Instanced batching needs its own owner; not in any story. |
| G2 | Pose ring plain-access seqlock (the F1 pattern) | C (latent) | Code review against `event-ring-cursor.ts:9-11` | ENGINE-WIRE-ABI |
| G3 | 500 ms read stall faults physics | C (lifecycle) | Node harness: withhold receipts for 600 ms; the run must continue | **Amend `ENGINE-ONE-WAY-INSTALL`** |
| G4 | Animation port: every read, no coalescing, full decode | S / C (memory) | Queue depth and heap with the service throttled | Amend `ENGINE-ONE-WAY-INSTALL` |
| G5 | One animation sample in flight across all targets | S | Oldest-sample age against target count | ENGINE-WIRE-ABI (output as a shared table, one publication per pulse) |
| G6 | Catch-up coalescing against adjacent-tick interpolation | C (smoothness), unproven | Non-adjacent brackets and `AwaitingHistory` frames under forced catch-up | ENGINE-WIRE-ABI (the pose feed carries enough history), or amend `ENGINE-ONE-WAY-INSTALL` |
| G7 | Unpinned TypeScript literal offsets; Animation worker script unchecked | C (latent) | Extend the pinning to every script and literal, or remove the literals by generation | ENGINE-WIRE-ABI |
| G8 | Non-FlatBuffers layouts hand-coded on both sides (section 2) | H, latent C | — | ENGINE-WIRE-ABI |
| G9 | Hand vtable literals; unknown-field refusal on 6 of ~76 tables | H (partial refusal) | Test: each literal equals the generated accessor's field count | S5b, or ENGINE-WIRE-ABI if S5b stays TypeScript-only |
| G10 | Raw-integer schema enums (~51); per-site refusal | H, latent C | Grep for a cast without `ObservationEnum` or `IsDefined` | One source of truth per enum: the C# owner (e.g. `WorkshopRejection`, `ABS/RuntimeServices.cs:14`). Its TypeScript mirror comes from the single C#-to-TypeScript generator owned by `ENGINE-CAPABILITY-INSTANCES`. ENGINE-WIRE-ABI only routes every raw field through the one refusal helper, with a test mapping each field to its owner. Schema `enum` types are **not** added, so no enum gets a second generated TypeScript copy. |
| G11 | `motion: [uint8]` opaque blob | H/S | — | ENGINE-WIRE-ABI |
| G12 | TypeScript FlatBuffers runtime resolved from a Homebrew path, unpinned | H (reproducibility) | `npm ls flatbuffers` | **S5b** |
| G13 | S5b has no contract | H (planning) | — | Coordinator: draft before S5b starts |
| G14 | Per-capability fan-out across about nine hand lists | S (of change) | Files touched by the next capability slice | S6 (chains), ENGINE-WIRE-ABI (schema and layout derivation), `ENGINE-CAPABILITY-INSTANCES` (shapes) |
| G15 | Parallel islands impossible as configured; no shared SoA plan | S (later) | SDK thread support under LLVM AOT | `ENGINE-SOA-SIMD` Code Map |
| G16 | Spatial query interface single-query | S | Per-ray cost in the first optics scene | Amend `ENGINE-SPATIAL-QUERIES` |
| G17 | Physics worker `Atomics.wait` up to 2 ms per 100 ms | H | R6 | DOC-ENGINE-STATUS (state it); no change needed unless R6 shows it |

## 6. Proposed story changes

### 6.1 New: ENGINE-WIRE-ABI — "Every boundary layout is declared once and generated; the pose ring feeds the renderer"

Element-free. It takes the shared acceptance of the engine programme (`epics`, "Shared acceptance for every story in this section").

- **Outcome:** every record that crosses a worker, thread or persistence boundary is declared once in a schema and accessed through generated code in C# and TypeScript. The renderer reads poses from shared memory without locks, serialisation or allocation per frame, and with one bounded copy per side. True zero-copy is not reachable as configured (section 3.4); if the owner enables WASM threads, the copy is removed in this story.
- **Contract:**
  - Fixed records are FlatBuffers `struct`s in the schema, which gives fixed, aligned layouts with generated C# and TypeScript accessors from the pinned `flatc`, so no new generator is introduced: pose record and pose-slot header, event record, schedule control and result, animation control and output, clock peer and receipt, motion piece, trace record and canonical body. Shared-memory access stays a thin hand layer that copies whole words atomically into a private buffer and decodes it with the generated accessor (the event ring's pattern).
  - Enums keep one source of truth, the C# owner. Schema fields stay integers annotated with their owner, and every decode goes through the one shared refusal helper (`ObservationEnum<T>`). The TypeScript mirror comes only from `ENGINE-CAPABILITY-INSTANCES`' generator (G10).
  - Unknown-field refusal is derived from the generated accessors for every table, replacing the W2 literals.
  - `motion: [uint8]` becomes schema structs.
  - The pose ring is the presentation feed:
    - Its header carries epoch, tick and body count; its records carry what interpolation needs (pose, velocities, motion pieces or the next tick's bracket).
    - The producer writes in place from the engine's committed state, with no per-tick array.
    - Ring depth is derived, not fixed at three. Presentation renders `DelayNanoseconds` (16.67 ms, `GPU/WorkshopPoseHistory.cs:13`) behind, which is 2 ticks at 120 Hz and 4 at 240 Hz, and it needs both bracket ticks while the producer writes the next slot. So depth ≥ delay ticks at the highest cadence + 2 + 1, unless each slot carries enough motion to stand alone. **Owner decision:** "triple-buffered" in AGENTS.md, `DOC:21` and `epics:47` becomes "a ring of derived depth", or the delay shrinks to fit three slots.
    - Every word is read and written atomically (the event ring's pattern), so a torn read is impossible.
    - The reader is allocation-free per frame, and a slow reader keeps its last good frame.
    - Presentation stops decoding bodies and motion from the per-tick `WorkshopResponse`. The Code Map decides whether the read still carries bodies for latch and goal validation (`GPU/WorkshopMessages.cs:22-41`, `engine/MachineWorld.Gpu.cs:285`).
  - Animation output is one shared table per pulse, not one message per target.
  - Instanced rendering by mesh is either delivered here or recorded in DOC as open with a named owner.
- **Acceptance:**
  - A generated C# and TypeScript accessor pair reads the same fixture bytes to equal field values for every layout.
  - Moving a field in the schema changes both sides with no hand edit.
  - No production source in `GPU/`, `SIM/`, `ANIM/` or `WEB/` holds a numeric byte offset for a boundary record (an extended literal scan that covers TypeScript).
  - In a node harness, the renderer reads N bodies from the ring for 1,000 frames with zero allocations per frame after warm-up and one copy per side, while the producer publishes concurrently at the highest cadence; no torn read is observed.
  - The renderer follows motion through a forced 3-tick catch-up without `AwaitingHistory` frames.
  - An undefined enum value and an unknown appended field are refused with their typed reasons at both decode boundaries.
  - *Performance:* R6 before and after.
- **Deletes:** the hand layouts and their TypeScript constant mirrors, `WorkerAbiTests`' regex pinning, the W2 vtable literals, the `motion` blob, the presentation decode of bodies and motion from the per-tick read, per-target animation messages, `ComposeSlot`'s array.
- **Depends on:** S5 (and S5b for the TypeScript generation path). **Order:** before `ENGINE-TICK-ALLOCATION`. Remove that story's pose-slot clause so the two stories do not both claim the ring. `ENGINE-SOA-SIMD` then writes poses from its columns straight into the ring.

### 6.2 Amendments

- **S5b (needs a contract):** generate `tools/e2e/workshop-wire.ts` in the build from the pinned `flatc`; pin the npm `flatbuffers` runtime to 25.2.10 in `package.json` and remove the Homebrew `paths` mapping (`tools/e2e/tsconfig.json`); keep the hand helpers in a separate module; regenerate or test `SNAPSHOT_SCHEMA` against the schema. If S5b stays TypeScript-only, record that W1-W8 remain and are owned by ENGINE-WIRE-ABI.
- **ENGINE-ONE-WAY-INSTALL:** add to the contract and Deletes:
  - the reliable-read stall stops faulting the run; presentation lag is a presentation state;
  - the animation port gets coalesced, latest-only reads, or reads the shared feed;
  - the Presented wait is removed.

  Acceptance: receipts withheld for 600 ms leave the run advancing; a throttled animation worker holds bounded memory.
- **ENGINE-TICK-ALLOCATION:** drop "pose slot written into reused storage" (moves to ENGINE-WIRE-ABI); keep the host read path, but measured after ENGINE-WIRE-ABI shrinks it.
- **ENGINE-SOA-SIMD:** its Code Map decides whether the SoA tables live in a `SharedArrayBuffer`, after establishing whether .NET WASM threads with LLVM AOT are supported, so `ENGINE-PARALLEL-ISLANDS` stays possible.
- **ENGINE-SPATIAL-QUERIES:** add batched queries over columns writing into caller-provided spans, with no allocation or delegate per hit.
- **DOC-ENGINE-STATUS:** add that the pose ring has no reader, that zero-copy between the C# runtimes is not reachable without WASM threads (one copy per side), that instanced draws do not exist, that `Atomics.wait` blocks the physics worker up to 2 ms per 100 ms, and that the FlatBuffers wire is the control plane while the shared layouts are hand-defined.

### 6.3 Order (owner decision 3a)

S5 → S5b → (3a programme: DOC-ENGINE-STATUS, ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-SENSOR-VOLUMES, ENGINE-ISLANDS-SLEEP, ENGINE-ONE-WAY-INSTALL amended, ENGINE-SUPPLY-DEMAND, ENGINE-CAPABILITY-INSTANCES, **ENGINE-WIRE-ABI**, ENGINE-TICK-ALLOCATION, ENGINE-SOA-SIMD). ENGINE-WIRE-ABI must precede further element delivery: every capability delivered before it adds hand layouts, hand codec fields and TypeScript mirrors that it would then have to convert. **Owner decisions needed:** (a) inserting ENGINE-WIRE-ABI into the 3a programme, whose order and names 3a fixes; (b) WASM threads, or one copy per side (section 3.4, G15); (c) "triple-buffered" against a derived ring depth (section 6.1); (d) the G3 and G4 amendments widening `ENGINE-ONE-WAY-INSTALL`.

## 7. What I could not establish

- Any timing, allocation or queue figure; nothing was run.
- Whether the pinned .NET SDK supports WASM threads with LLVM-only AOT.
- Whether `ActivationMemory`'s validation (`ABS/ActivationMemory.cs:29`) runs on the decode path at `GPU/WorkshopWireFlatBuffers.cs:864`.
- How often catch-up actually produces non-adjacent brackets in the browser (G6).
- The Godot web renderer's draw-call batching beyond the absence of instancing APIs in repository code.
- Test projects were read only where cited.
