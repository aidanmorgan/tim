# Run log: DOC-ENGINE-STATUS, the documents describe the engine that exists (lane 4)

Append-only. Contract: `_bmad-output/implementation-artifacts/spec-doc-engine-status.md` (drafted after the coordinator's scope decision, E3).

## Handled owner messages

- 25 November 2026: coordinator rulings on the planning report (E3): scope (b); `KinematicMode.Kinematic` to lane 1; restitution +Y to `ENGINE-ISLANDS-SLEEP`; coverage items agreed; `tools/e2e` comments deferred; `DOC:59` left until merge. Handled in the contract (Code Map, Decisions R1–R5).
- 25 November 2026: dispatch of lane 4 for Story DOC-ENGINE-STATUS (owner decision of 25 November, to be recorded as 24q: DOC-ENGINE-STATUS, programme order 1, documents only, may start now in lane 4 in parallel with the remaining pre-programme work; order 2 onwards still waits). Worktree `.claude/worktrees/slice-doc-engine-status`, branch `slice-doc-engine-status`, baseline `origin/main` `37a259c9`. Run through `bmad-build-auto`; stop and report if scope grows past the story text or a finding needs an owner decision; stop after the commit; no fetch, merge, push or SnapshotApproval without the publish token.

## E1 Session environment check (25 November 2026)

- Checkout `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-doc-engine-status`, HEAD `37a259c9a2f08669cd339e36c3f2c6b4a3f7bd68`, tree clean, `git add --refresh` clean.
- Pinned toolchain first on `PATH`: `node --version` v26.12.1; `dotnet --version` 10.0.403 (matches `global.json`); `dotnet workload list` shows `wasm-tools` 10.0.114/10.0.100; `dotnet restore CuriousContraptions.slnx --nologo --disable-parallel -noAutoResponse` exit 0. Logs: `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/session-env.log`, `restore.log`.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --bmad-check --project-root <checkout> --route Auto`: structural findings 0; the three tiered `when` conditions (blind-hunter, edge-case-hunter, intent-alignment) reported for semantic review. The slice is engine tier (none of its documents is on the data-only allow-list), so all six layers run on the first pass. Log `bmad-check-auto.log`.

## E2 Planning scouts (25 November 2026)

Two read-only scouts (fresh general-purpose agents, inherited model, no history) measured the claims at `37a259c9`. Every citation below was opened by the scout at that commit. Shorthand: `SIM/` Simulation, `ABS/` Abstractions, `GPU/` `engine/gpu/`, `WEB/` `CuriousContraptions.web/wwwroot/`, `WPE` `SIM/WorkshopPhysicsEngine.cs`, `DOC` `docs/gpu-f32-physics.md`, `ISO` `docs/architecture/element-package-isolation.md`.

### E2.1 Claim inventory (scout A)

| ID | Line | Claim | Code at `37a259c9` | Verdict |
|---|---|---|---|---|
| C1 | DOC:3 | "lock-free zero-copy shared memory pipeline" | C# builds a new byte array per read (`GPU/WorkshopPoseRing.cs:58-60`); TypeScript copies it into the shared buffer (`WEB/pose-ring.ts:96`); sequence lock (`:95`, `:98`) | PARTLY (lock-free; not zero-copy) |
| C2 | DOC:12 | `worker.js` copies committed bytes into the rings | `SIM/wwwroot/worker.ts:286-290` | PRESENT (name is `worker.ts`) |
| C3 | DOC:21 | worker writes directly into a ring of derived depth | fixed 3 slots (`GPU/WorkshopPoseRing.cs:14`, `WEB/pose-ring.ts:13`); writes via the TypeScript copy (`pose-ring.ts:87-99`) | ABSENT |
| C4 | DOC:21 | renderer samples the ring, uploads GPU instance buffers, zero allocation | `PoseReader` built (`WEB/workshop-client.ts:152`, `:256-257`) and never read; renderer fed by the posted read (`workshop-client.ts:272-275` → `GPU/BrowserWorkshopClient.cs:321-339` → `engine/MachineWorld.Gpu.cs:238-242` → `GPU/WorkshopPoseHistory.cs:285-303` → `engine/MachinePart.cs:342-350`); no instancing anywhere | ABSENT |
| C5 | DOC:40 | physics graph "Flat SoA tables, SIMD arrays, Islands" | AoS byte records (body 160, collider 96: `GPU/PhysicsGpuAbi.cs:22`, `:45`); world copied per tick (WPE:254) and decoded into pooled `SimBody`/`SimCollider` (WPE:324-325, `:419-420`, `:517-518`; classes `SIM/WorkshopCollisionKernels.cs:29`, `:93`) in `List`/`Dictionary` (WPE:31-41) | ABSENT |
| C6 | DOC:42 | BVH with velocity fattening | binary BVH (`SIM/WorkshopDynamicBvh.cs:6-17`); fattening by the speed cap (WPE:810, `:820`) | PARTLY |
| C7 | DOC:44, :189 | lock-free ring between physics and renderer | written (worker.ts:290), no production reader | PARTLY |
| C8 | DOC:48-49, :71 | instanced batches in 1-2 calls, fed from the ring | one node transform per body (`engine/MachinePart.cs:342-350`) | ABSENT |
| C9 | DOC:57, :59, :60, :88 | flat SIMD-aligned tables | flat byte records with typed offsets, AoS, unaligned, decoded per tick | PARTLY |
| C10 | DOC:63, :353 | SoA, 64-byte aligned | none; only the pose record is 64 bytes (`GPU/WorkshopPoseRing.cs:17`) | ABSENT |
| C11 | DOC:64 | BVH "accelerates all broadphase spatial queries" | contact pairs only (WPE:866); full scans: residence WPE:913; guides WPE:1029-1062; compliant WPE:1540; airflow `SIM/WorkshopPhysicsEngine.Airflow.cs:149`; chambers `.Chambers.cs:86`, `:102`, `:117`, `:145`, `:156-160`; gas `.Gas.cs:69`; triggers and work WPE:139-140, `:163-164`; passage `GPU/PassageSensorDeclaration.cs:101-106` | PARTLY |
| C12 | DOC:65 | DSU islands | none; union-find only for restitution groups (`SIM/WorkshopConstraintSolver.cs:609-614`, `:660-664`) and continuous-work components (`.ContinuousReplay.cs:94`) | ABSENT |
| C13 | DOC:74, :161, :172, :344 | fixed 120 Hz, exactly 4 substeps | `cadence==1 ? 8 : cadence==2 ? 4 : 2` (WPE:297); substep 1/480 s (`ABS/CadenceValues.cs:78`) | PARTLY |
| C14 | DOC:111 | SIMD slab tests, impulse updates, box evaluations | BVH test scalar (`SIM/WorkshopDynamicBvh.cs:154-158`); `Vector128` in joint rows (`SIM/PrismaticConstraintSolver.cs:208-216`, `SIM/RevoluteConstraintSolver.cs:213-221`, `GPU/SoftConstraint.cs:51-58`), tensile broadcast (`SIM/TensileConstraintSolver.cs:80-82`), grant scale (`ABS/ProportionalAllocation.cs:26-30`), normal carrier (WPE:198, `GPU/ContactWorkImpulse.cs:15`) | PARTLY (audit stale: more uses) |
| C15 | DOC:145 | motion type Dynamic, Static, Kinematic; island index | `RigidMotionKind { Static, Dynamic }` (`ABS/PhysicsDeclarationValues.cs:8`); declaration `KinematicMode.Kinematic = 2` (`ABS/MechanicalContractValues.cs:7-11`) refused (`:25-26`; `GPU/WorkshopPhysicsCompiler.cs:245-250`) | ABSENT |
| C16 | DOC:146 | "the static hollow Tube, Torus and Frustum"; "participation filter masks" | hollow on dynamic bodies (`SIM/WorkshopConstraintSolver.cs:161`; `SIM/WorkshopCollisionKernels.cs:1996-2003`); participation `{ Enabled, Disabled }` (`ABS/ColliderDescriptor.cs:6`) | contradicted |
| C17 | DOC:148 | joints 4-wide; hinges, sliders, distance links, ropes | revolute and prismatic 4-wide; rope is a tensile route (`SIM/TensileConstraintSolver.cs:12-40`), one row broadcast; `CoordinateKind {Revolute, Prismatic}` (`ABS/PortDescriptor.cs:43-47`); no distance joint | PARTLY |
| C18 | DOC:150 | sensors evaluated continuously; sub-tick aperture root; dwell over substeps | residence once per tick after the substeps (WPE:358), origin point, absolute speed (WPE:913-938); passage in the device after `Advance` (`SIM/WorkshopGpuDevice.cs:243`), stamped at substep ordinal, phase 0 (`GPU/PassageSensorDeclaration.cs:116`, `:137-138`) | PARTLY |
| C19 | DOC:151 | contact triggers on any pair, rearm | static against dynamic only (WPE:1341-1342), owner static side (WPE:143); first per tick (WPE:312-316, `:150-152`); no rearm lane | PARTLY |
| C20 | DOC:164-189 | phase diagram | see E2.2 A | PARTLY (order wrong; optics, acoustics, thermal absent) |
| C21 | DOC:174-179 | per-substep order with island/sleep | broadphase (`.Motors.cs:33`), contacts (`:96`), warm start, solve ×4 (`:98-106`), integrate (`:112`), relax ×4 (`:118-126`), restitution (`:129`), settle (`:131`); no island step | PARTLY |
| C22 | DOC:189 | commit to multi-buffered ring | `SIM/Program.cs:335-348`; worker copy; 3 fixed slots, unread | PARTLY |
| C23 | DOC:193 | dynamic binary BVH | `SIM/WorkshopDynamicBvh.cs:6-19` | PRESENT |
| C24 | DOC:194 | incremental SAH rotations, no rebuild | SAH-cost descent (`SIM/WorkshopDynamicBvh.cs:71-102`), no rotations; cleared and rebuilt (WPE:806-863) every substep and trial (`.Motors.cs:33`, `.ContinuousReplay.cs:22`, `.FiniteWork.cs:136`) | PARTLY |
| C25 | DOC:195-197, :296 | fat box by `|v|h + slop`; no update inside | speed-cap fattening, rebuilt every substep | ABSENT |
| C26 | audit | plane AABB assumes +Y | WPE:826-829 (narrowphase uses `plane.U1`, `SIM/WorkshopCollisionKernels.cs:327`) | defect present |
| C27 | DOC:200 | branchless analytic primitives | closed form, fixed counts, `switch` dispatch (`SIM/WorkshopCollisionKernels.cs:1999-2028`) | PARTLY |
| C28 | DOC:258, :299-306 | speculative for all pairs, `|v_rel·n|h + slop` | `0.004 + (|Δv| + spin reach)·h` (WPE:1112-1117); conservative advancement sphere only (`SIM/WorkshopCollisionKernels.cs:639-645`, `:1940-1949`) | PARTLY |
| C29 | DOC:375 | no tunnelling through 1 cm at 64 m/s | no non-sphere qualification | unestablished |
| C30 | DOC:259 | "Known limitation: per manifold … deferred" | grouped with support expansion (`SIM/WorkshopConstraintSolver.cs:645-669`, `:722-752`); `engine-supported-impact-restitution: done` (`sprint-status.yaml:533`); group cut to 8 points (`:685-688`), undocumented, owner ENGINE-ISLANDS-SLEEP (`epics.md:4566`) | text stale |
| C31 | DOC:259, :376 | restitution after relax; energy bound | `.Motors.cs:129`; `SIM/WorkshopConstraintSolver.cs:709`, `:787` | PRESENT except the cut |
| C32 | DOC:268 | 4 constraints or manifold points in f32x4 | joint rows only; contacts scalar (`.Motors.cs:104`, `:125`) | PARTLY |
| C33 | DOC:270-273, :332-336 | islands, sleeping, wakeup | none | ABSENT |
| C34 | DOC:274 | work-stealing dispatch | single-threaded; only extra worker is the clock master (`SIM/wwwroot/worker.ts:139`); no `WasmEnableThreads` | ABSENT |
| C35 | DOC:283-290 | Quad-BVH | binary nodes, scalar test | ABSENT |
| C36 | DOC:292-297 | dual tree, static not updated, pruned matrix | one tree; static leaves reinserted every substep (WPE:812-862); `HasDynamic` prunes static-only subtrees (`SIM/WorkshopDynamicBvh.cs:15-16`, `:154`, `:178`); static-static pairs dropped (WPE:876) | PARTLY |
| C37 | DOC:308-310 | 32-bit hierarchical `SubShapeID` | none; key `ContactFeatureIdentity(A, B, Feature, OwnerA, OwnerB, FaceA, FaceB)` (`SIM/WorkshopFrictionSolver.cs:9-10`) | PARTLY |
| C38 | DOC:322 | hinges, distance links, gear/pulley ratios unified | ratio coupling (`SIM/MechanicalTransmissionSolver.cs:8-14`, `:29`, `:44`; kinds `ABS/MechanicalContractValues.cs:255`); no distance link | PARTLY |
| C39 | DOC:352 | canonical pair sort | BVH traversal order (`SIM/WorkshopDynamicBvh.cs:145-189`); in-pair order only (`:162-163`, WPE:1108) | PARTLY |
| C40 | DOC:76 | renderer Slerps brackets from the ring | motion records from the posted read, constant-ω rotation (`GPU/PhysicsMotionRead.cs:132-173` via `GPU/WorkshopPoseHistory.cs:285-303`) | ABSENT |
| C41 | DOC:128 | 64-byte pose record | `GPU/WorkshopPoseRing.cs:17` | PRESENT |
| C42 | DOC:377 | crossing timestamp ≤ 0.1 ms | substep ordinal, phase 0 (about 2.08 ms) | ABSENT |
| C43 | DOC:5, :99 | Current status lists the shortfalls | ISO §4 lists none of C5–C40 | ABSENT |
| I1 | ISO:147 | no unresolved finding remains | stale; ISO:143 records `71135ecd` | delete |
| I2 | ISO:35 | everything in C# WASM; worker copies | PRESENT (`SIM/CuriousContraptions.Simulation.csproj:19-22`) | PRESENT |
| I3 | ISO:102 | derived depth; renderer samples with atomic loads, no allocation; bootstrap 16 | fixed 3; new array per read (`GPU/WorkshopPoseRing.cs:60`); no reader; bootstrap 16 holds (`:15`, `:24`) | PARTLY |
| I4 | ISO:21, :106 | queries read the pose ring | no production reader | PARTLY |
| A1–A7 | audit | joint scan per pair (WPE:877-886); 4+4 sweeps (`SIM/WorkshopConstraintSolver.cs:43-44`); allocations (`SIM/WorkshopCollisionKernels.cs:308`, `:474`, `:726`, `:1546`, `:1894`; WPE:866; `GPU/WorkshopPoseRing.cs:60`); motor replays (`.Motors.cs:40-66`); shapes (`SIM/WorkshopCollisionKernels.cs:1999-2028`); GPU naming; `Atomics.wait` (A7, E2.2 B6) | unchanged |

Static-only hollow wording left at the baseline: `DOC:146` and `docs/planning/elements/EL-156-single-lobe-cam.md:28`. Already corrected before the baseline (by `dbaacad1`): `docs/hollow-geometry-design.md:43`, `ABS/PhysicsDeclarationValues.cs:9-11`, `SIM/WorkshopCollisionKernels.cs:1996-1997`. Further latent axis assumption: restitution support expansion admits only upward normals (`SIM/WorkshopConstraintSolver.cs:739`, `:769`).

### E2.2 Tick, wire, owners, sweep, preservation (scout B)

**A. Tick today.** Device before physics: timers (`SIM/WorkshopGpuDevice.cs:200`), frozen endpoint contacts (`:202`, `:329-349`), supply routing from committed state (`:205-209`), stage (`:217`). Engine prelude: rope cuts (WPE:258), validation (`:261-282`), supply prepared (`:294`; `.Supply.cs:13-40`), substep count (`:297`), reads (`:324-346`), substeps (`:348`). Each substep (`AdvanceSubsteps`, WPE:985): guides detached `:993`, gas `:994`, prismatic motors `:995`, chambers `:1002`, finite-work requests (substep 0) `:1003`, airflow `:1010`, supply grants `:1011`, solve `:1012` (inside: broadphase rebuild `.ContinuousReplay.cs:21-22`, continuous-work trials `:24-59`, guides `:65`, membranes `:66`, gravity and drag `:67-73`, motor replays `.Motors.cs:30-73`, narrowphase `:96`, warm start `:97`, solve `:98-106`, integrate and clamp `:112`, relax `:118-126`, restitution `:129`, settle and second clamp `:131`), commit constraints `:1013`, travel `:1014`, contact loads `:1016`, compliant `:1018`, integrity `:1020`, contact observations `:1021`, transmissions `:1022`, motion `:1023`. After the last substep: contact-load phase `:349-354`, chambers `:356`, clearances `:357`, residence `:358`, writeback `:359-369`. Device after: passage (`SIM/WorkshopGpuDevice.cs:243`), supply resolved (`:250-251`), signal network (`:254-255`), events staged (`:279-282`), read composed (`:284-294`), commit (`:351-370`). Publication: response encoded (`SIM/Program.cs:339-341`), event ring (`:346`), pose slot (`:347`), ring written (`SIM/wwwroot/worker.ts:290`), read posted to animation (`:291-294`) and main thread (`:295-296`). Substeps per tick 8/4/2 at 60/120/240 Hz. Supply phase `PhaseSeconds = 1f / 120f` (`GPU/ElectricalSupply.cs:9`), gated `(ordinal + substep) % 4 == 0` (`.Supply.cs:49`): 2, 1 or 0.5 phases per tick. No optical or thermal code exists. Combined tick order: anchor `combined-tick-order` at `docs/planning/elements/EL-176-red-laser-emitter.md:174`, item 4 at `:175`. Coordinator decisions: 16 (`owner-decisions-2026-11-22.md:89`, optics after the last substep, readings from the next tick), 21 (`:90`, thermal links, no tick slot), 26 (`:46`, per-`dt` rates, gas before liquid).

**B. Wire and threading.** Pose ring: producer `SIM/Program.cs:347` → `worker.ts:290` → `WEB/pose-ring.ts:87-99`; no production reader (reader built and swapped only, `workshop-client.ts:84`, `:152`, `:256-257`); test readers in `tools/runtime/` and the deferred `tools/e2e/workshop-driver.ts:1041`, `:1151` (reads a global nothing installs). Renderer feed: `worker.ts:295-296` → `workshop-client.ts:273-275` → `BrowserWorkshopClient.cs:321-339` → `MachineWorld.Gpu.cs:238-242` → `MachinePart.cs:311`, `:342-350`; animation copy `worker.ts:292-293` → `ANIM/wwwroot/worker.ts:114-115` → `ANIM/Program.cs:213-215`. Copies: `ComposeSlot` array, JSImport (`SIM/Program.cs:103`), `target.set` (`pose-ring.ts:96`); read `new Uint8Array` (`worker.ts:288`) and `.slice()` (`:292`); event ring `worker.ts:63-64`, `:76`, `:98`; `WasmEnableThreads` in no project. Slot count fixed 3 (`pose-ring.ts:13`; `WorkshopPoseRing.cs:14`, pinned by `WorkerAbiTests.cs:18`); `WorkshopPoseRing.cs:7` "Lock-free zero-copy triple-buffered", `:13` quotes AGENTS.md "triple-buffered pose ring" (AGENTS.md now says multi-buffered). No `MultiMesh`, `drawElementsInstanced` or `drawIndexed` anywhere. `Atomics.wait`: constants `WorkshopClockDerivation.cs:39-40` (2 ms, 100 ms); `clock-ring.ts:246` startup qualification and `:270` per-interval exchange on the physics worker (`worker.ts:160`, `:168`, `:176-186`); `clock-ring.ts:233` blocks the clock-master context only. FlatBuffers: `schemas/workshop_wire.fbs`, `schemas/workshop_authoring.fbs` (S5c merges them). Hand layouts: pose ring (`WorkshopPoseRing.cs:14-17` / `pose-ring.ts:12-15`), event ring (`ABS/EventRingBuffer.cs:104-106` / `WEB/event-ring-cursor.ts:14-20`, `SIM/wwwroot/worker.ts:26-30`), clock ring (`GPU/WorkshopClockWire.cs:10-17` / `WEB/clock-ring.ts:12`, `:23`), cadence (`GPU/WorkshopCadence.cs:61-63`), and `WorkshopHint.cs`, `WorkshopTraceRecord.cs`, `CanonicalBodyCodec.cs`, `PhysicsBodyWire.cs`, `MechanicalLinkWire.cs`, `WorkshopSchedule.cs:69`.

**C. Owners** (index `docs/planning/elements/readiness/engine-stories-index.md`, finding map `:125`; epics headings): SPATIAL-QUERIES 4478, CONTACT-OBSERVATIONS 4505, SENSOR-VOLUMES 4529 (in-tick passage), ISLANDS-SLEEP 4554 (restitution cap, `epics.md:4566`), ONE-WAY-INSTALL 4581, SUPPLY-DEMAND 4599, CAPABILITY-INSTANCES 4630 (GPU names), WIRE-ABI 4657 (ring reader), WASM-THREADS 4705, INSTANCED-RENDERING 4730, TICK-ALLOCATION 4763, SOA-SIMD 4778, THERMAL 4869, JOINT-KINDS 4925, MECHANICAL-LINKS 4955, PROFILE-COLLIDER 5098, OPTICAL 5173, TUNNEL-QUALIFICATION 5418, PARALLEL-ISLANDS 5442. No owner: distance links; "pulley ratio" as such; the restitution +Y support assumption; `SubShapeID`.

**D. Outside documents.** Present-tense claims of absent features outside DOC and ISO: `DESIGN.md:17`, `:19`, `:21`; `docs/engine-contracts.md:11`, `:25-26`, `:232`; `docs/general-engine-design.md:16`, `:18`, `:137`, `:145`, `:168-169`, `:191`, `:202`, `:217`; `docs/simulation-presentation-bridge.md:33`, `:83`; `docs/shared-clock-cadence.md:58`, `:60`, `:72`; `docs/browser-physics-performance.md:7`, `:9`, `:65`, `:69`; `docs/planning/invest/current-consumers.md:3`; `_bmad-output/planning-artifacts/architecture.md:43`, `:92-101`, `:182`, `:189`, `:240`, `:257`, `:313`, `:319`, `:338`; `EL-156-single-lobe-cam.md:28`. Code comments: `GPU/WorkshopPoseRing.cs:7`, `:13`; `tools/e2e/workshop-driver.ts:2`, `:1147`; `tools/e2e/engine-core-2a1.test.ts:2`. Target or rule statements (not present-tense claims): `AGENTS.md:73`, `:76`; `_bmad/custom/project-engineering-contract.md:47`; `docs/delivery-workflow.md:267-268`; `docs/planning/requirements.md`; `docs/planning/invest/*` (except `current-consumers.md:3`); `prd.md`; `epics.md:46-47`. Excluded as historical: `_bmad-output/implementation-artifacts/**`, `readiness/audits/**`, `docs/work-orders/**`, `delivered-slices.md`, `docs/coverage/history/**`, retros, `docs/*.patch`, epics story texts before `:4421`.

**E. Preservation.** Inbound anchors into DOC: `game-grade-envelope` (130), `solver-model` (33), `compilation-model` (19), `capability-inventory` (14), `precision-status` (12), `numeric-representation-and-precision` (7), `modern-engine-patterns`, `compilation-model-and-tri-graph-architecture`, `8-demand-grown-reusable-scratch-and-scene-buffers` (1 each). Into ISO: `current-status` (8), `target-invariants` (6), `3-balanced-coupling-couplingdev` (2), `2-communication-rings-commands-and-queries` (1). Tests reading the documents: `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs` reads DOC, ISO, AGENTS.md, architecture.md, engine-contracts.md and general-engine-design.md; it bans `res://parts/`, `.tres`, `pose-ring.js`, `CuriousContraptions.Engine`, `getF16`, `setF16` in the five non-isolation documents and checks every relative link resolves (anchors not checked). No test reads `docs/hollow-geometry-design.md`. `tools/Coverage` reads `docs/planning/invest/*.md`, `requirements.md` and element specs, not DOC or ISO.

## E3 Planning report and coordinator rulings (25 November 2026)

The coordinator reported the scope boundary (the Deletes line has no location limit; ISO:9 says no unmet invariant is described as holding elsewhere), the retained `KinematicMode.Kinematic` member in lane 1's file, the owner-less rows and the lane overlaps, with options and recommendations. Rulings, verbatim in substance:

1. Scope (b): line-local corrections also in the nine current files of E2.2 D (`DESIGN.md:17-21`, `engine-contracts.md`, `general-engine-design.md`, `simulation-presentation-bridge.md`, `shared-clock-cadence.md`, `browser-physics-performance.md`, `invest/current-consumers.md:3`, `architecture.md`, `EL-156:28`), each restated as target with a link to Current status; ISO's rule makes this part of the Outcome. Leave required or target design text alone (AGENTS.md, `_bmad/custom` E2, the workflow's Tri-Graph section, requirements, `invest/*`, PRD, epics NFRs) and historical records. Report if the edit count goes much past about 40 lines.
2. `KinematicMode.Kinematic` is deleted by lane 1's `Colliders` slice, which owns `MechanicalContractValues.cs`; this slice changes the DOC wording only and cites lane 1's slice as owner.
3. Restitution +Y support assumption: owner `ENGINE-ISLANDS-SLEEP`; a Current-status row and one Present-today line in that story. Distance links and `SubShapeID`: the words are removed.
4. Coverage agreed: today's tick order in the diagram with optics and thermal marked target; cite the real renderer feed `workshop-client.ts:272-275`; correct the `WorkshopPoseRing.cs:7`, `:13` comments.
5. Overlap agreed: the two `tools/e2e` comments go to `deferred-work.md`, owned by S5c or the browser phase if S5c does not touch those lines; `DOC:59` is left until merge, then re-checked.

Also: record owner decision 24q and amend the sequence wording as briefed.

Owner of the unnamed Quad-BVH node layout: the index finding map (`engine-stories-index.md:125`) gives "broadphase" to order 2, so the Quad-BVH and dual-tree target is `ENGINE-SPATIAL-QUERIES`; SIMD lanes are order 10 (`ENGINE-SOA-SIMD`). Recorded as a mapping, not a new decision.

## E4 Planning audit 1 (25 November 2026)

Fresh general-purpose agent, inherited model, no history; read-only. Verdict **Fail**, ten findings, all repairable in the contract (no owner decision needed):
1. (high) AC1/M1 narrower than the story's "no claim the code contradicts" in DOC and ISO; contradicted off-topic claims found: `DOC:149` "fluid buoyancy" and cone bounds (`BuoyancyCoefficient` declared only, `ABS/FluidContractValues.cs:19`), `DOC:372` and `general-engine-design.md:199` "fast inverse square root" (code uses `MathF.Sqrt`, `Quaternion.Normalize`), `DOC:281` "deterministic replays".
2. (medium) Missed lines: `ISO:119` (`Atomics.waitAsync` only; the physics worker blocks in `Atomics.wait`), `DOC:339-341` (steady reuse, island working storage), `architecture.md:72`, `:106`, `simulation-presentation-bridge.md:99`, `general-engine-design.md:192`, `:203`, `EL-158-rise-hold-fall-cam.md:29` (static only), `EL-065:89`, `EL-069:86`, `readiness/batch-i.md:170` (say the code comment "still" reads static only).
3. (medium) `DOC:59` frozen with "flat SIMD collider tables" (C9) and no plan.
4. (medium) The `rg` pattern misses ring-reading, instancing, SoA, islands, sleep and "static only" phrasing; AC2 runs over owned files only; M5 bounded by the scout list; M7 checks a token, not wording.
5. (medium) Never list forbids `invest/*` while item 6 edits `delivery-batches.md` and `vertical-delivery.md`; more gate lists: `invest/named-elements.md:22`, `completion-ledger.md:128`, `:392` (OS-01). `delivery-batches.md:246-337` "When" lines are per-slice and stay accurate (checked by the coordinator).
6. (medium) AC3's 7407 is derived from lane 1's log at `c3456907` and will differ at a merged commit; restate as no test added or removed.
7. (medium) Anchor check matches explicit ids only; five inbound anchors are heading slugs (`numeric-representation-and-precision`, `compilation-model-and-tri-graph-architecture`, `8-demand-grown-reusable-scratch-and-scene-buffers`, `3-balanced-coupling-couplingdev`, `2-communication-rings-commands-and-queries`).
8. (low) Pose-ring copies understated (C# array marshalled, `new Uint8Array` at `worker.ts:290`, `target.set` at `pose-ring.ts:96`) and target overstated (`ENGINE-WASM-THREADS` removes the producer copy; a consumer keeps at most one, `epics.md:4714`).
9. (low) Citations: renderer feed `workshop-client.ts:273-275` (`:272` is the acknowledgement branch); `ABS/ColliderDescriptor.cs:6` is `CollisionParticipation` (`SensorParticipation` is `ABS/PhysicsDeclarationValues.cs:13`); ENGINE-ISLANDS-SLEEP Present today is `epics.md:4559`; `clock-ring.ts:246` is startup qualification, the 100 ms check is `worker.ts:168`, `:179` → `clock-ring.ts:270`.
10. (low) Narrow "kinematic" to the body/motion kind (`DOC:323` Jacobian sense, `engine-contracts.md:106`); edited lines drop element examples (`DOC:294-295`, `:322`); name lane 1's contract `spec-delete-declaration-colliders.md`; the ~40-line trigger is met by the plan (about 45 product lines in item 4, about 55 with finding 2).

Over 40 citations re-checked, all correct except finding 9. Owners, dispositions, forward-only, element-free, budget (3,141 words) and rulings R1–R5: no finding. All ten repaired in the contract (Spec Change Log, 25 November 2026). The edit-count note (finding 10) is reported to the coordinator with the slice report; it is "about 55", not "much past" 40, so work continues.

## E5 Planning audit 2 (25 November 2026)

Fresh general-purpose agent, no history, read-only. Verdict **Fail**. F1–F10 closed, except that F1's `DOC:149` disposition was still conditional. Six new findings:
- N1 (medium): the M4 pattern returned about 2,820 lines at the baseline, mostly noise (`instanc`, `island`, `sleep`, `kinematic`).
- N2 (medium): lane 1's uncommitted `spec-delete-declaration-colliders.md:175-176` also edits `DOC:145`, `shared-clock-cadence.md:60` and `epics.md:4466`.
- N3 (low): `DOC:149` was undecided. `ENGINE-FIELD-FORCE` (`epics.md:5235`) owns declared force regions and buoyant lift.
- N4 (low): the startup `Atomics.wait` loop was understated (`clock-ring.ts:243-251`; `WorkshopClockDerivation.cs:13-17`, `:37-38`).
- N5 (low): the M7 search also hits `owner-decisions-2026-11-22.md:33` and `readiness/batch-e.md:5`.
- N6 (low): unlinked DOC headings at `:20`, `:270` and `:308` still name absent features.

Over 30 citations were checked; all are correct. Budget is 3,822 words. All six findings are repaired in the contract (Spec Change Log).

The new phrase classes, run with `git grep` at `37a259c9` over the non-excluded paths, give about 250 hits in about 40 files. The top files are `epics.md` (34), DOC (29), `engine-design-gap.md` (15), `architecture.md` (15) and `general-engine-design.md` (8). Each hit can be classified by hand.

Lane overlap N2 is resolved by the 24m and 24o merge rule, recorded in the contract's Concurrency section. It is reported to the coordinator with the slice report.

## E6 Bounded third check, and a coordinator message (25 November 2026)

The bounded third check was a fresh general-purpose agent, run read-only. Its verdict was **Fail**, on three defects in the audit-2 repair text. N2, N4 and N6 are closed.
- **T1 (medium).** The M4 command runs and returns 201 lines in 57 files. E5's "about 250 in about 40" is corrected here. It misses `general-engine-design.md:192`, `:199`, `:203`, `simulation-presentation-bridge.md:33`, `browser-physics-performance.md:9`, `architecture.md:98-100`, `:319` and `ISO:119`. It also misses `architecture.md:6`, `:51` and `:256`, and `simulation-presentation-bridge.md:26`, which were not owned. In addition, `prd.md` and "retros" were not real pathspecs. The scratch output is `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/37b1f527-b6f7-4082-bfd2-686d62e814b3/scratchpad/m4.txt`.
- **T2 (low-medium).** The `DOC:149` disposition was wrong:
  - Ambient lift for a gas-filled body runs today (`ABS/GasCouplingValues.cs:19` → `WorkshopPhysicsCompiler.Gas.cs:36-37` → `SIM/WorkshopPhysicsEngine.Gas.cs:95-97`).
  - Liquid buoyancy belongs to `ENGINE-LIQUID` (`epics.md:4829`, `:4841`).
  - The acceleration bound is 16 m/s² (`GasSceneValidation.cs:78`, `WorldAccelerationDeclaration.cs:25`), not 64.
- **T3 (low).** The M7 search also hits `owner-decisions-2026-11-22.md:61`, `:67`, the element lines `EL-125:142`, `EL-187:123`, `EL-188:124`, `EL-189:134`, `EL-190:118`, `EL-191:121`, `EL-192:126`, `EL-204:128`, `EL-208:141`, and `README.md:411`.

**Stagnation reassessment (protocol: the same check failed twice).** The search net was being treated as the inventory. It is now only a net. The owned-scope list (M5), checked line by line, is the inventory, and the search control records what the net catches. With that change, the check can be completed and still discriminates. The repairs are listed in the Spec Change Log.

**Coordinator message, 25 November 2026 (handled).** Lane 1's unpublished deletion slice deleted `KinematicMode.Kinematic`. It also edited `epics.md:4466`, `DOC:145` and `shared-clock-cadence.md:60`, correcting the last two to static and dynamic only. Instruction: plan for those lines to change, take main's version at merge, and do not duplicate the edit. The contract now leaves those three lines unedited.

## E7 Bounded fourth check (25 November 2026)

The bounded fourth check was a fresh general-purpose agent, run read-only. Its verdict was **Fail**. T1–T3 and the coordinator-message changes were consistent everywhere except in one place.

Findings:
- **G1 (medium, blocking).** AC2, M1, M4 and Design Notes 7 had no exception for the lane 1 lines `DOC:145` and `shared-clock-cadence.md:60`. Those lines are M4 hits that stay unedited before the merge.
- **G2 (low).** Item 4 was missing `simulation-presentation-bridge.md:3` and `:95`, `architecture.md:169`, and `CAT-023-domino.md:105` and `:107`. Separately, the "full inventory" wording contradicted the line-by-line sweep.
- **G3 (low).** M7 was missing `EL-189-tennis-ball.md:55` and `EL-208-balloon.md:50`.
- **G4 (low).** The cited sites throw rather than clamp, so the wording should say "refused" (`GasSceneValidation.cs:78`, `WorldAccelerationDeclaration.cs:25`).
- **G5 (low).** `waitAsync` matches C# `Task.WaitAsync`, and `static hollow` matches `static HollowSolid`.
- **G6 (low).** The verdict line was stale.

Measurement: the M4 command at `37a259c9` returns 236 lines in about 69 files (`scratchpad/m4-check4.txt`). The T2 citations were all confirmed.

All six findings are repaired in the contract.

## E8 Bounded fifth check (25 November 2026)

The fifth check used a fresh general-purpose agent, read-only, limited to the G1–G6 repair text. Verdict: **Fail**. G3, G5 and G6 are repaired correctly.

Four low findings remain:
- **H1 (blocking).** The lane 1 exception left out `epics.md:4466`. That line is a baseline M4 hit and is left unedited (lane 1 owns it).
- **H2 (blocking).** `CAT-023-domino.md:105` and `:107` are legacy-harvest facts pinned to `a6c914e`, where `PhysicsTiltSensorTests` did use `PhysicsMotionType.Kinematic`. They are not current claims.
- **H3 (blocking).** The G4 wording merged two refusals. `WorldAccelerationDeclaration.cs:25` refuses a declared acceleration longer than 16. `GasSceneValidation.cs:78` refuses the combination of gravity and a pressure-scaled load.
- **H4 (deferrable).** Item 4's label is stale.

The M4 command at `37a259c9` returns 220 lines in 56 files (`scratchpad/m4-check5.txt`). The narrowed classes still catch `ISO:119` and `DOC:146`.

All four findings are repaired in the contract. This is the fifth planning round, and its findings were mechanical wording in the repair text, so the sixth check is limited to these four edits.

## E9 Sixth check and planning verdict (25 November 2026)

The sixth check was a fresh general-purpose agent, read-only. It found H1, H2, H3 and H4 closed, with every cited line confirmed at the baseline. The H2 confirmation includes `a6c914e:PhysicsTiltSensorTests.cs`, which uses `PhysicsMotionType.Kinematic`. It reported one low finding: Design Notes 8's list of merge re-checks left out `epics.md:4466`. The repair it prescribed was applied verbatim.

Coordinator verdict: **planning Scoped Pass**. The last finding was a single list item that the check prescribed exactly, and the step-04 informed auditors read the whole contract. The planning gate closes after six rounds. Status is ready-for-dev. The contract is 4,853 words, which is inside the 6,000-word planning budget.

## E10 Implementation (implementation owner, 25 November 2026)

Fresh implementation owner (general-purpose agent, inherited model). Checkout and baseline as E1; HEAD `37a259c9` throughout; the candidate is uncommitted in the worktree.

**Capability probe.** Documents only; no capability is used or delivered. The citations used were reopened at `37a259c9` before writing. Two scout citations were corrected: the static-subtree pruning sites are `SIM/WorkshopDynamicBvh.cs:154` and `:182` (not `:178`), and the restitution cut is `:685-688`. No contract defect was found.

**Reading of the present-fact rule.** A sentence this slice writes or rewrites cites the running code or is marked target with its owner. A correct sentence left untouched keeps its text (Preservation: "All correct content is unchanged"); the rule is not read as "add a citation to every sentence".

**DOC, every contradicted sentence and its disposition** (baseline line → disposition; claim IDs from E2.1 and E4):
- `:3` zero-copy pipeline (C1): target, `ENGINE-WASM-THREADS`.
- `:12` "deterministic, sub-millisecond" and the `worker.js` path (C2): SIMD cited (`SIM/CuriousContraptions.Simulation.csproj:22`); speed target of `ENGINE-TICK-ALLOCATION` and `ENGINE-SOA-SIMD`; determinism unestablished; `worker.ts:286-290` cited.
- `:20` heading retitled "Shared Memory Pipeline" (no inbound link); `:21` (C3, C4): today cited (three slots, copies, renderer feed, one node transform per body); target `ENGINE-WIRE-ABI`, `ENGINE-WASM-THREADS`, `ENGINE-INSTANCED-RENDERING`.
- Diagram `:40`, `:44`, `:48-49` (C5, C7, C8): labelled target, box widths kept at 81 columns.
- `:57`, `:60` (C9): covered by the new status paragraph under `compilation-model` (candidate `:53`); `:59` unedited (R5, re-checked at merge).
- `:63` (C10): target `ENGINE-SOA-SIMD`. `:64` (C6, C11): binary tree and speed-limit fattening cited; target `ENGINE-SPATIAL-QUERIES`. `:65` (C12): target `ENGINE-ISLANDS-SLEEP`.
- `:71` (C8), `:76` (C40): today cited, target named. `:74` (C13): cadence cited (`ABS/CadenceValues.cs:68-74`).
- `:111` (C14): SIMD users cited; slab tests and contact impulses scalar; target `ENGINE-SOA-SIMD`.
- `:117` "awaits independent review and terminal publication": restated (Part B published `6e0e0ee1`).
- `:145` (C15): lane 1 line, unedited. Its "island index" is re-checked at merge (target `ENGINE-ISLANDS-SLEEP`).
- `:146` (C16, C27): hollow kinds on static or dynamic bodies (`SIM/WorkshopConstraintSolver.cs:161`), `CollisionParticipation`, closed-form fixed-count dispatch cited.
- `:148` (C17): revolute, prismatic, tensile routes and ratio transmissions cited; distance links removed.
- `:149` (E4 F1, T2, H3): restated exactly as the contract's `DOC:149` disposition.
- `:150` (C18): residence and passage timing cited; target `ENGINE-SENSOR-VOLUMES`. `:151` (C19): static-owner trigger cited; target `ENGINE-CONTACT-OBSERVATIONS`.
- `:154` network-node declared lanes that no code declares: restated (supply routes today; the rest with their later capabilities).
- `:161` (C13): 8/4/2 sub-steps cited.
- Tick diagram `:164-189` (C20, C21, C22): redrawn to today's order with a citation paragraph; optical and thermal slots target (decisions 16 and 21) citing the combined tick order; supply phase open under `ENGINE-SUPPLY-DEMAND`.
- `:193-197` (C23, C24, C25, C26): tree cited; rebuild and fattening cited; persistent trees, `|v|h + slop` fattening and normal-following plane bounds target `ENGINE-SPATIAL-QUERIES`; the "95%" claim removed.
- `:200` (C27): "branchless" restated as closed-form, fixed-count.
- `:258` (C28): margin cited (`SIM/WorkshopPhysicsEngine.cs:1112-1117`); non-sphere tunnelling owned by `ENGINE-TUNNEL-QUALIFICATION`.
- `:259` (C30, C31): stale limitation deleted; grouping cited (`:645-669`, `:709`, `:722-752`); 8-point cut and +Y support open under `ENGINE-ISLANDS-SLEEP`.
- `:268` (C32): joint rows cited, contact rows scalar; target `ENGINE-SOA-SIMD`.
- `:270-274` (C33, C34): heading "Target: …", a lead line with owners and the single-thread fact, bullets in future tense.
- `:281`: qualified by the new "Implementation status" lead-in (candidate `:298`).
- `:283-290` (C35), `:292-297` (C36): headings "Target: …", owner lead lines, today's single tree cited; element examples dropped from `:294-295`.
- `:301-306` (C28): margin restated to the running form; tunnelling owner named.
- `:308-310` (C37): `SubShapeID` removed; `ContactFeatureIdentity` cited (`SIM/WorkshopFrictionSolver.cs:9-10`, `:20-21`).
- `:322` (C38): distance links and gear/pulley removed; ratio transmissions cited; the Springboard example dropped.
- `:332-336` (C33): heading "Target: …", bullets in future tense with owner.
- `:339` "full combined round/review … pending": restated (published `28a2d302`). `:340-341` (E4 F2): steady reuse and island working storage target (`ENGINE-TICK-ALLOCATION`, `ENGINE-ISLANDS-SLEEP`) with per-tick allocations cited.
- `:344` (C13): sub-step counts cited. `:351-353` (C39, C10): heading "Target: …", today's traversal order and 64-byte pose record cited.
- `:372`: `Quaternion.Normalize` cited (`SIM/WorkshopPhysicsEngine.cs:1194-1195`); no fast inverse square root exists (searched).
- `:375` (C29): target with `ENGINE-TUNNEL-QUALIFICATION`. `:377` (C42): target `ENGINE-SENSOR-VOLUMES`, today's sub-step resolution cited.
- `:387` "All 72 named puzzle elements … the 150 … levels": restated without the stale counts.
- Present and unchanged: C23, C41 (`:128`), C31 except the cut, the rest of the solver-model text.

**Lane 1 lines, baseline → candidate.** `DOC:59` → `:61`, `DOC:145` → `:147`; `docs/shared-clock-cadence.md:60` and `epics.md:4466` at the same line numbers. All four are byte-identical to the baseline (`cmp` of `git show 37a259c9:<file>` line against the candidate line; `lane1-lines.txt`).

**ISO dispositions.** `:21` invariant 3 and `:106` §2.2 (I4): the pose ring is a query surface from `ENGINE-WIRE-ABI`. `:97` table cell: ring written, read from `ENGINE-WIRE-ABI`. `:102` §2.1 (I3): today cited (three slots, copies, no reader, bootstrap 16 at `WorkshopPoseRing.cs:15`) and target. `:119` §2.4 (A7, N4): `Atomics.waitAsync` for requesters (`clock-ring.ts:149`), blocking `Atomics.wait` on the clock master and the Simulation worker, startup and 100 ms check cited. §4 heading dated 25 November 2026; `:143` names `37a259c9` for §4.4 and keeps §4.1–§4.3 on `71135ecd` without claiming a re-audit; `:147` (I1) "no unresolved finding remains" deleted, the rest of the paragraph kept. New §4.4 with 20 rows, one per contract group (C43 resolved). `:35` (I2) present, unchanged.

**WorkshopPoseRing.cs.** `:7` and `:13` rewritten in place, same line count; `SlotCount` (`:14`), `BodyBytes` (`:17`) and `ComposeSlot` (`:58-60`) keep their lines.

**Other current documents (item 4 plus the re-sweep).** 51 product lines edited outside DOC and ISO (`git diff --numstat`: +51 −51):
- Edited: `DESIGN.md:17`, `:19`, `:21`; `docs/engine-contracts.md:11`, `:25`, `:26`, `:232`; `docs/general-engine-design.md:16`, `:18`, `:137`, `:145`, `:168`, `:170` (own sweep: "Bitwise Determinism & Zero GC"), `:174` (new target lead-in for the pipeline), `:199`, `:217`; `docs/simulation-presentation-bridge.md:3`, `:26`, `:33`, `:83`, `:95`, `:99`; `docs/shared-clock-cadence.md:58`, `:62` (own sweep: the writer runs with `target.set`, the reader is target, covering `:63-70`), `:72`; `architecture.md:316` (own sweep: the pose-ring row named `workshop-client.js` as a reader); `docs/browser-physics-performance.md:7`, `:65`, `:69`; `docs/planning/invest/current-consumers.md:3`; `architecture.md:43`, `:51`, `:72`, `:91` (AD-03 rule lead-in), `:99`, `:106`, `:169`, `:182`, `:189`, `:240`, `:256`, `:257`, `:313`, `:319`, `:338`; `EL-156:28`, `EL-158:29`, `EL-065:89`, `EL-069:86`, `EL-069:186` (re-sweep addition, same stale "static only" remark), `batch-i.md:170`.
- Left unchanged as target or rule, named: `general-engine-design.md:169` ("kinematic ratio constraints", the Jacobian sense); `:191`, `:192`, `:202`, `:203` (now under the `:174` target lead-in); `browser-physics-performance.md:9` (a budget rule); `architecture.md:6` (the paradigm's name); `:92`–`:98`, `:100`, `:101` (now under the AD-03 target lead-in with owners per item).
- Element and record wording: EL-158 states the hollow kinds are not used for the dwell, without deciding whether a torus segment could replace the thirteen cylinders (a question for EL-158's own slice).

**Planning records.** Owner decision 24q added after 24p (f). The 24q clause added at `delivery-batches.md:13`, `epics.md:81`, `:4426`, `engine-stories-index.md:5`, `vertical-delivery.md:9`, `:22`, `named-elements.md:22`, `completion-ledger.md:128`, `TODO.md:7`, `:17`. The clause reads, verbatim in each place: "DOC-ENGINE-STATUS, programme order 1, documents only, runs in lane 4 from 25 November 2026 in parallel with the remaining pre-programme work (owner decision 24q); order 2 onwards waits." Three places fit it to their sentence with the same meaning: `vertical-delivery.md:22` ("Nothing else runs before cluster 1, except that …"), `named-elements.md:22` ("… then the rest of the engine programme of owner decision 3a, order 2 onwards, which waits for that work") and `TODO.md:17` ("Then the engine programme from order 2 …", with order 1 in parentheses). `epics.md:4559` gains the +Y support sentence. PD-23 (`completion-ledger.md:268`) set to "in review". `sprint-status.yaml:587` `doc-engine-status: review`. `deferred-work.md` gains the `tools/e2e` comments entry (R5). Planning-record lines: 14 added or changed, plus 7 in `deferred-work.md`.

**Owned-diff self-check (protocol handoff).** The full owned diff was read from the baseline: no unrelated churn, no change to a test, schema, build or code line other than the two comments; every edited engine-rule line names no element.

## E11 M4 search, classification and control (25 November 2026)

**Commands.** Classes `C` (one `-e` each, as written): `-e 'zero[- ]copy' -e 'triple[- ]buffer' -e '(reads?|samples?|sampled|sampling|consumes?|from)\b[^.\n]{0,60}\b(pose|shared(ArrayBuffer)?) ring' -e 'instanced (draw|batch)' -e 'drawElementsInstanced|drawIndexed|MultiMesh' -e 'GPU instance buffer' -e 'Structure-of-Arrays' -e '\bSoA\b' -e 'Quad-BVH' -e 'dual[- ]tree' -e 'sleeping (island|state)' -e '\bislands? (sleep|graph|index)\b' -e '\b(DSU|union-find|disjoint set)\b' -e 'work-steal' -e '\bKinematic\)' -e 'kinematic (bod|motion|kind)' -e 'dynamic (or|and) kinematic' -e 'static[- ]only' -e 'static hollow\b' -e 'SubShapeID' -e 'distance link' -e '\binstanced\b' -e 'put to sleep' -e 'fast inverse' -e 'Atomics\.waitAsync' -e '(pose|shared) ring[^\n]{0,120}renderer|renderer[^\n]{0,120}(pose|shared) ring'`. Exclusions `X`: `_bmad-output/implementation-artifacts/**`, `docs/planning/elements/readiness/audits/**`, `docs/work-orders/**`, `docs/coverage/history/**`, `docs/planning/delivered-slices.md`, `**/*retro*`, `AGENTS.md`, `_bmad/**`, `docs/planning/requirements.md`, `_bmad-output/planning-artifacts/prd.md`, `tools/e2e/**`.
- Control: `git grep -n -i -P C 37a259c9 -- '*.md' '*.cs' '*.ts' ':(exclude)X'…` (one `:(exclude)` per path of X), raw output `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/m4-baseline.txt`: **220 lines in 56 files** (matches E8).
- Candidate: `rg -n -i -P -g '*.md' -g '*.cs' -g '*.ts' -g '!X'… C .` (one `-g '!…'` per path of X) over the working tree, raw output `m4-candidate.txt`: **216 lines in 52 files** (after the E10 own-sweep edits; `general-engine-design.md:170` now matches through the owner name `ENGINE-SOA-SIMD`).
- Per-file counts in `m4-baseline-files.txt` and `m4-candidate-files.txt`.

**What the control catches.** 55 of the 63 owned lines of items 3 and 4 (both `WorkshopPoseRing.cs` comments included), 30 DOC lines and 5 ISO lines. Owned lines the net does not catch, covered by name by M5: `general-engine-design.md:169`, `:203`, `simulation-presentation-bridge.md:33`, `architecture.md:94`, `:95`, `:96`, `:98`, `:100`. Per-file counts that drop at the candidate: `EL-065` 1 to 0, `EL-069` 3 to 1, `EL-156` 1 to 0, `EL-158` 1 to 0, `WorkshopPoseRing.cs` 2 to 0, DOC 30 to 28; ISO rises from 5 to 9 with the §4.4 rows. Target-marked lines keep their phrases, so the net still matches them.

**Classification of every candidate hit.**
- Lane 1 line: `DOC:147` (baseline `:145`), `docs/shared-clock-cadence.md:60`, `epics.md:4466`.
- Target (each names its owner or links Current status): DOC `:3`, `:21`, `:40`, `:44`, `:48`, `:53`, `:65`, `:67`, `:73`, `:113`, `:284`, `:289`, `:290`, `:291`, `:298`, `:303`, `:309`, `:311`, `:312`, `:313`, `:314`, `:352`, `:353`, `:354`, `:372`; ISO `:21`, `:97`, `:199`, `:200`, `:201`, `:210`; `DESIGN.md:17`, `:19`, `:21`; `engine-contracts.md:11`, `:25`, `:26`, `:232`; `general-engine-design.md:16`, `:18`, `:137`, `:145`, `:168`, `:170`, `:174`, `:191`, `:192`, `:202`, `:217`; `simulation-presentation-bridge.md:3`, `:26`, `:83`, `:95`, `:99`; `shared-clock-cadence.md:58`, `:72`; `browser-physics-performance.md:7`, `:65`, `:69`; `current-consumers.md:3`; `architecture.md:43`, `:51`, `:72`, `:91`, `:92`, `:93`, `:97`, `:101`, `:106`, `:169`, `:182`, `:189`, `:240`, `:256`, `:257`, `:313`, `:319`, `:338`; `epics.md` engine stories `:4427`, `:4457`, `:4468`, `:4470`, `:4472`, `:4474`, `:4657`–`:4787`, `:5019`, `:5111`, `:5443`, `:5445`, `:5704`; element and readiness lines that wait on a named story (`EL-039:16`, `:174`, `:239`, `EL-070:111`, `EL-102:130`, `EL-230:15`, `:103`, `EL-231:15`, `:106`, `batch-d.md:42`, `batch-h.md:17`, `:87`, `batch-m.md:13`, `new-identities.md:30`, `:31`, `engine-stories-index.md:5`, `:23`, `:25`, `:125`, `:202`, `completion-ledger.md:372`, `engine-design-gap.md:245`, `:251`).
- Rule (required or target design text, the Never list's `invest/*`, or a design label): `epics.md:46`, `:47` (NFRs); `epics.md:81` (the order item, now with 24q); `docs/delivery-workflow.md:267`, `:268` (Tri-Graph section); `invest/decisions.md:152`, `invest/engine.md:3`, `:170`, `:179`, `invest/gpu-physics.md:1`, `:3`, `:17`, `:57`, `:131`, `invest/vertical-delivery.md:10`, `:23`, `:25`, `:44`, `:144`, `:167`, `invest/delivery-batches.md:14`, `:254`; `architecture.md:6`; `browser-physics-performance.md:9`; ISO `:92`; `autonomous-development-guide.md:10`, `:35`, `:39`; element designs of a static part (`EL-100:21`, `:195`, `EL-101:140`, `EL-161:99`, which is a vibration "ring").
- Negation: ISO `:218`; `DOC:391`; `EL-069:39`; `GAP-07:141`; `batch-i.md:170`; `engine-design-gap.md:19`, `:34`, `:36`, `:45`, `:46`, `:47`, `:49`, `:50`, `:56`, `:263`, `:264`; `engine-stories-index.md:189`; `MechanicalContractValues.cs:25` (the refusal); `epics.md:4559`.
- Cited present: DOC `:12`; ISO `:119`; `architecture.md:274`; `EL-046:22`, `EL-070:96`, `EL-103:102`, `EL-107:74` (static hollow parts); `elements/Funnel`, `Pipe`, `PipeBend45`, `PipeBend90` declaration comments (each declares `KinematicMode.Static`, opened at `FunnelElementDeclaration.cs:44`, `PipeElementDeclaration.cs:69`, `PipeBend45ElementDeclaration.cs:42`, `PipeBend90ElementDeclaration.cs:42`); `WorkshopPhysicsLawTests.cs:191` (the `StaticHollow` helper, a static body); `clock-ring.ts:149`, `tools/runtime/workshop-client-lifecycle.test.ts:32`, `workshop-clock-ring.test.ts:23` (real `Atomics.waitAsync`); `TODO.md:3` (a part's drawn ring); `completion-ledger.md:268` (PD-23, in review); `owner-decisions-2026-11-22.md:15`, `:65`, `:71`, `:81` (dated decisions naming the stories).
- Historical: `epics.md:596`, `:610`, `:784`, `:1275`, `:2827` (story texts before `:4421`); `CAT-023-domino.md:105`, `:107` (pinned to `a6c914e`); `engine-design-gap.md:165`, `:176` (the dated audit's own text); `architecture-conformance-audit-2026-10-18.md:94`, `:125`.

No other hit is a present-tense false claim.

## E12 Verification (implementation owner, 25 November 2026)

Executor for every command: the implementation owner. Pinned toolchain (E1). Logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`. Every run is on the uncommitted candidate over HEAD `37a259c9`; any edit to a document a test reads, or to `engine/gpu/WorkshopPoseRing.cs`, invalidates the builds and the round.

- Capture builds (`docs/delivery-workflow.md:38-53`, binlogs `binlogs/`): playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; 0 warnings, 0 errors; `capture-1.log`, `capture-2.log`.
- Maintainability gate, `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root <checkout>`: **PASS**, violations 0 (metrics 0, coupling 0); both canary `PASS` lines and `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability.log`.
- Registration filter (`docs/delivery-workflow.md#new-package-registration-check`): **821 passed, 0 failed, 0 skipped** (17 + 195 + 83 + 97 + 429); `registration.log`.
- `ArchitectureDocsConsistencyTests` (filtered): **4/0/0**; `architecture-docs.log`.
- Anchor and heading check: explicit ids identical to the baseline (DOC 10, ISO 2); every linked heading text present (`numeric-representation-and-precision`, `compilation-model-and-tri-graph-architecture`, `8-demand-grown-…`, `modern-engine-patterns`, `3-balanced-coupling-couplingdev`, `2-communication-rings-commands-and-queries`); `combined-tick-order` at `EL-176:174` and the epics engine-stories heading present; `anchor-check.txt`.
- M7/AC4 search, `rg -i "only repairs and tooling|before the engine programme"` over current Markdown: every current list carries the 24q clause; the lines without it are exactly the M7 unchanged records (`delivery-batches.md:246-337`, the EL lines and `README.md:411`, `batch-e.md:5`, decisions 22, 24h, 24m) plus historical records under `_bmad-output/implementation-artifacts/**` and `readiness/audits/**`.
- `git add -N` on the contract and run log, then `git diff --check 37a259c9`: clean; `diff-check.log`.
- Full local round (HEAD at top, HEAD-AFTER at bottom, `CSHARP_STATIC_GATE_BINLOGS` = `binlogs/`, deferred-browser filter only): **7407 passed, 0 failed, `Skipped: 0`** (WorkerAotGuard 17, BundleElements 323, Coverage 132, E2eRunner 256, CuriousContraptions.tests 6679); `full-round.log`. No test file is added or removed (`git diff --stat` lists none).
- Final candidate. Three own-sweep document lines were then edited (E10: `general-engine-design.md:170`, `shared-clock-cadence.md:62`, `architecture.md:316`). No compile input changed, so the capture builds, the gate and the registration result stay current. Rerun on the final candidate: `ArchitectureDocsConsistencyTests` **4/0/0** (`architecture-docs-2.log`); `git diff --check 37a259c9` clean (`diff-check-2.log`); the M4 search (E11); the lane 1 byte check (`lane1-lines.txt`); and the full round's test command, `--no-build`, same binlogs and filter: **7407 passed, 0 failed, `Skipped: 0`**, HEAD and HEAD-AFTER `37a259c9` (`full-round-2.log`). This is the handoff round.

## E13 Remaining risks (implementation owner, 25 November 2026)

- The DOC sentence `:349` (baseline; "preserves total energy") is a property of the integrator, not a code claim, and is unchanged.

## E15 Review pass 1 repairs (implementation owner, 25 November 2026)

Repairs returned by the coordinator after review pass 1 (Review Triage Log), under ruling D2. Line numbers are at the repaired candidate unless marked baseline. Every write was Anvil-validated first.

**Per-line dispositions.**
- `browser-physics-performance.md:69` (FO-1): island sleep restated as the `ENGINE-ISLANDS-SLEEP` target with its wake and keep-running conditions; the "sleep denied" control removed; zero scratch allocation target `ENGINE-TICK-ALLOCATION`; speed-cap fattening present, velocity fattening and incremental updates target `ENGINE-SPATIAL-QUERIES`; same-build replay restated as below.
- DOC wire and threading (IA-1): a new paragraph (DOC `:23`) states the FlatBuffers control plane and save (`schemas/workshop_wire.fbs`, `schemas/workshop_authoring.fbs`), the hand-defined shared-memory and worker-message layouts until `ENGINE-WIRE-ABI` (pose, event and clock ring citations), and the physics worker's `Atomics.wait` blocking in the contract's wording, cited.
- Velocity fattening stated as running: `DESIGN.md:17`, `engine-contracts.md:11`, `:25`, `general-engine-design.md:16` now say "fattened by the speed limits" with velocity fattening the `ENGINE-SPATIAL-QUERIES` target; DOC diagram `:44` "binary AABB BVH, speed-cap fattening (target: velocity)"; heading `:210` retitled "Broadphase: Dynamic AABB BVH" (no inbound link).
- Capsule, MNA, optical, acoustics (EC-1, BH-2): `DESIGN.md:17` lists only running shapes and fields and names targets with owners (`ENGINE-OPTICAL`, Story 14.0, MNA with its first consumer); `engine-contracts.md:25` and `general-engine-design.md:16` drop Capsule and name the optical, acoustic and pneumatic owners (`ENGINE-OPTICAL`, Story 14.0, `ENGINE-GAS-NETWORK`); `DESIGN.md:87` says `col_capsule` is refused at bundle time (`tools/BundleElements/SceneGeometryExtractor.cs:111-114`) with no owning story; `DESIGN.md:91` says `col_hull` is refused at bundle time (same lines) and that a convex extruded profile is the `ENGINE-PROFILE-COLLIDER` target (engine stories index C29; completion ledger PD-22).
- WebGPU: DOC `:16`, `:18`, diagram `:50`; `DESIGN.md:19`; `engine-contracts.md:26`; `browser-physics-performance.md:7`; `simulation-presentation-bridge.md:26`: WebGL 2 today (`project.godot:17-18`, `gl_compatibility`), WebGPU the `ENGINE-INSTANCED-RENDERING` target. ISO §4.4 gains a WebGPU row.
- Audio (ISO `:92`, the §2 table header and Audio row, `:110`): no audio worker or audio consumer exists today. Story 14.0 (`ENGINE-ACOUSTIC`) owns audio playback of acoustic events ("audio playback is presentation-only", `epics.md:5329`), so the audio column, row and consumer are marked as its target. No owner coined.
- ISO §4.4 rows added: compliant-surface × dynamic-body reservation (`engine/gpu/PhysicsWorldLayout.cs:90-94`, `ENGINE-CAPABILITY-INSTANCES`) and four fixed membrane samples (`SIM/WorkshopPhysicsEngine.cs:1555`, `ENGINE-COMPLIANT-FRICTION`). The plane-bounds text now states the fixed ±100 m box (`:826-829`). DOC `:360` (actual-demand sizing) names both exceptions with their owners.
- Upward-normal wording (EC-6, EFE-F6b, BH-6b): DOC restitution bullet (`:277`), ISO islands row and `epics.md:4559` now say `:739` admits a contact to a touched body only when its normal has an upward (+Y) component on that body's side, and `:769` admits any normal between two bodies already in the group. `epics.md:4559` no longer says no union-find exists (restitution grouping is one, `SIM/WorkshopConstraintSolver.cs:608-616`, `:660-664`). ENGINE-ISLANDS-SLEEP's *Deletes* line gains the upward-normal support filter. E11's "negation" classification of `epics.md:4559` no longer applies; the line now cites present code.
- Restitution bullet (DOC `:277`): one description: union-find grouping with supporting contacts, one block solve over the group (`ApplyRestitution`, `:700`, `:850-862`), energy bound per group (`:709`), open items with owner. The stale per-manifold sentence and the duplicate grouping sentence are gone.
- Tensile SIMD (EC-8, BH-9): DOC `:115` and the ISO SIMD row say the single tensile row is broadcast into all four lanes, not batched.
- Citations (EC-9, EC-10): trigger rows (DOC `:155`, ISO) cite static owner `:145`, first-per-tick gate `:152-155`, per-tick reset `:311-315`; collider admission cites `ABS/PhysicsDeclarationValues.cs:9-12`, and `SIM/WorkshopConstraintSolver.cs:161` only as dynamic hollow support displacement (DOC `:150`, `EL-156:28`, `EL-158:29`).
- Clock master wording (EC-12, EFE-F6c): DOC `:289` and the ISO parallel row: the physics worker spawns only the clock master (`worker.ts:139`); the Animation worker is a separate runtime started by the main thread (`workshop-client.ts:160`).
- Same-build replay (EC-4, EC-5): DOC `:12`, `:300`, the ISO replay row and `simulation-presentation-bridge.md:33`: exercised by the Reset and replay tests (`CuriousContraptions.tests/TrampolineTests.cs:2174`, `F32BehaviourIdentityTests`), not established in general; canonical pair order is the `ENGINE-SPATIAL-QUERIES` target. The bridge's statement is restored in that form.
- Pose ring reader (EC-2, EC-3, VG-5): DOC `:21`, ISO §2.1 and the pose-ring row, `architecture.md:316`: a `PoseReader` is constructed (`workshop-client.ts:152`, `:256-257`) but never read, an `ENGINE-WIRE-ABI` item.
- Interpolation: DOC renderer line `:80` and `architecture.md:72`: today the renderer interpolates the committed motion pieces of the posted read (`GPU/PhysicsMotionRead.cs:132-185` via `GPU/WorkshopPoseHistory.cs:285-303`); only the ring as feed, with its derived depth, is target. The other edited documents already say "today from the committed read" and agree.
- `architecture.md:100` (AD-03 item 9): canonical evaluation order, no byte-identity claim.
- `general-engine-design.md:174`: endpoint sensor sampling added to the absent steps (residence once after the sub-steps, passage on the device after the tick); `:202` list label retitled "Publication to the SharedArrayBuffer Ring (zero-copy as target)" (not a heading, no inbound link).
- DOC `:369-370`: the quaternion update is the exact half-angle rotation left-composed per tick (`SIM/WorkshopPhysicsEngine.cs:1190-1195`); the energy-preservation sentence restated to defer to the envelope.
- DOC `:79`: particle effects removed; squash-and-stretch restated as the declared uniform-scale mapping (`ABS/AnimationDefinitions.cs:6`). DOC diagram `:36` and `:58`: `SocketPin` replaced by declared ports and `WorkshopConnection` (`engine/gpu/WorkshopConnections.cs:31`).
- DOC Sensor Crossing row (`:398`): the 0.1 ms number dropped; today's sub-step-ordinal stamping cited; the in-tick swept crossing is the `ENGINE-SENSOR-VOLUMES` target.
- ISO §4.4 KinematicMode row (EC-7, VG-3, IA-3): owner `_bmad-output/implementation-artifacts/spec-delete-declaration-colliders.md` (lane 1, unpublished, branch `slice-delete-declaration-colliders`); dropped at merge if lane 1 publishes first.
- `simulation-presentation-bridge.md:83`: anchor now `presentation-bindings.md#one-shot-cues-on-the-event-ring-story-9a25b-and-ui-bindings-anim-1c`.
- `deferred-work.md`: the `tools/e2e` entry reworded per line; a new entry for the automated anchor check (VG-1, owner: the coordinator to assign).
- `owner-decisions-2026-11-22.md`: a blank line before 24q, so it is its own entry.
- Status: `epics.md:4462` and `TODO.md:11` say DOC-ENGINE-STATUS is in review in lane 4.
- Anchor check fixes: the contract's two root-relative links (`spec-doc-engine-status.md:43`, `:70`) now start `../../`; `readiness/batch-i.md:133` (pre-existing) now links `../../requirements.md#todo-199`.

**Ruling D2, unchanged sentences on the topic list, re-checked.** DOC: the pose-ring record sentence (`:132`) now cites `GPU/WorkshopPoseRing.cs:17`; `:63` (lane 1, baseline `:59`) is qualified by the status paragraph `:55`, which also covers the "flat" tables at `:64` and `:92`; `:149` (lane 1, kinematic) is unchanged by ruling; patterns 1, 2, 7 and 10 (`:304`, `:313`, `:353`, `:372`) sit under "Target:" headings with owner lines; pattern 5's four-row normal blocks (`:340`) run today. ISO: §1.1 (`:35`) copies now cite `worker.ts:286-290`; `:110` event-ring consumers restated; §3's "atomic ring layouts" (`:133`) is a coupling rule; `:21`, §2.1, §2.2, §2.4 and §4.4 were already cited or target. No other unchanged DOC or ISO sentence on the topic list remains uncited or unmarked.

**Executed anchor check.** One-off `node -e` script, no committed test: for every Markdown file the slice changes (26 files), every relative link with a `#fragment` must resolve to an explicit `<a id>` or a GitHub heading slug (fenced code skipped, duplicate slugs numbered) in its target. Result: **1176 links, 0 broken** (before the fixes above: 3 broken). Control: the checker reports the baseline bridge fragment `declared-cosmetic-curves-anim-1b-and-ui-bindings-anim-1c` absent from `docs/presentation-bindings.md` and the new fragment present. Output `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/anchor-check-e15-design.txt` (rerun after the `DESIGN.md:91` patch: 1176 links, 0 broken). Explicit ids of DOC (10) and ISO (2) are identical to the baseline; every inbound fragment into DOC and ISO resolves.

**Counts and checks.** Product lines edited outside DOC and ISO: **56** (+56 −56, including `DESIGN.md:91`); DOC +111 −90, ISO +43 −13. Lane 1 lines byte-identical: `DOC:59` → `:63`, `DOC:145` → `:149`, `shared-clock-cadence.md:60`, `epics.md:4466` (`lane1-lines-e15.txt`). M4 candidate: 218 lines in 52 files (`m4-candidate-e15.txt`); the new hits are target lines naming `ENGINE-INSTANCED-RENDERING` (DOC `:16`, `:18`, ISO WebGPU row) and the cited restitution union-find (DOC `:277`). Tests covering the edited files, on the existing build: `ArchitectureDocsConsistencyTests` **4/0/0** (`architecture-docs-4.log`, after the `DESIGN.md:91` patch); `Coverage.Tests` **132/0/0** (`coverage-tests-e15.log`; it reads element specs and planning documents). `git diff --check 37a259c9` clean. No code, test or build input changed, so the capture builds, the gate and the registration result still apply; the full round is the coordinator's.
- Merge re-checks as Concurrency states: `DOC:61` (baseline `:59`), `DOC:147` (baseline `:145`, including "island index"), `shared-clock-cadence.md:60`, `epics.md:4466`, every citation into a file main changed, and the §4.4 `KinematicMode.Kinematic` row.

## E16 Review pass 2 repairs (implementation owner, 25 November 2026)

Repairs returned by the coordinator after review pass 2 (Review Triage Log, "Review pass 2"). Line numbers are at the repaired candidate. Every write was Anvil-validated first.

**Per-line dispositions.**
- Tick order: `general-engine-design.md:174` no longer calls the staged pipeline the target order. The target order of every domain is the combined tick order (`docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order`), and the numbered stages describe what each domain does, not their order. The absent-steps list and its owners are kept.
- Union-finds: DOC `:289`, the ISO §4.4 islands row (`:202`) and `epics.md:4559` name all four: restitution (`SIM/WorkshopConstraintSolver.cs:609-617`, `:660-664`), continuous-work components (`SIM/ContinuousWorkComponents.cs:23`), guide workspaces (`SIM/GuideWorkspace.cs:46-58`) and prismatic trial workspaces (`SIM/PrismaticTrialWorkspace.cs:86-99`).
- `epics.md:4559`: the removed `deferred-work.md:122` citation is replaced by `sprint-status.yaml:591`, the `engine-islands-sleep` key that absorbs `engine-resting-stack-settling`.
- Restitution (DOC `:277`): `ApplyRestitution` at `:701` and `Root` at `:609-617`. Bouncing points are driven to −e times their approach and supporting points to zero normal velocity (`:846-848`, `:870`). The support filter has two parts: `:739` gates expansion between dynamic bodies on an upward normal on the touched body's side, and `:769`'s one-sided terms admit support from a partner outside the group, such as a static body, only under the same condition, while a contact inside the group counts at any normal. The same wording is in the ISO islands row and `epics.md:4559`. The ENGINE-ISLANDS-SLEEP *Deletes* clause (`epics.md:4577`) now covers both filters (`:739`, `:769`).
- Energy: the DOC Finite Work Stores row (`:157`) and the Thermodynamic Energy envelope row (`:397`) say contact-work stores and continuous airflow are debited through `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`, `SIM/WorkshopPhysicsEngine.Supply.cs:107`), that a supplied drive debits nothing, and that drive debit is the `ENGINE-SUPPLY-DEMAND` target. The ISO supply row (`:206`) is extended with the same facts.
- Membrane samples: DOC `:360` and the ISO row (`:213`, retitled "Membrane sample layout") name `ENGINE-CAPABILITY-INSTANCES` (index `:125` "reservation → 8"; that story's "sample layout is declaration data", `epics.md:4641`). Compliant tangential friction stays with `ENGINE-COMPLIANT-FRICTION`.
- New ISO §4.4 rows: acoustic propagation and audio consumer (`:214`, Story 14.0 `ENGINE-ACOUSTIC`), Modified Nodal Analysis circuits (`:215`, "the first element story that needs it", as DOC and general-engine-design word it), gas network (`:216`, `ENGINE-GAS-NETWORK`). The "listed in Current status" pointers in `DESIGN.md:17` and `general-engine-design.md:16` now resolve to real rows. §4.4 has 26 rows.
- KinematicMode row (`:224`): removed by lane 1's `Colliders` deletion slice (`dce23e3a`, on `origin/main` at `466ff4ca`; confirmed with `git show origin/main:CuriousContraptions.Abstractions/MechanicalContractValues.cs`); the row is deleted at this slice's merge with main.
- Capsule and hull: DOC `:71`, `:92`, `:150`, `:218`; `general-engine-design.md:100`, `:103`, `:187`; `engine-contracts.md:88` now say capsule and hull proxies are refused at bundle time (`tools/BundleElements/SceneGeometryExtractor.cs:111-114`), with a convex profile as the `ENGINE-PROFILE-COLLIDER` target. DOC `:63` (lane 1) is untouched.
- WebGPU: `simulation-presentation-bridge.md:3`, `architecture.md:51`, `:106` (AD-04 rule) and `:256` now say WebGL 2 today (`project.godot:17-18`) with WebGPU the `ENGINE-INSTANCED-RENDERING` target. Same defect, also fixed: `architecture.md:50` (presentation subgraph label) and the unlinked DOC heading `:14`, retitled "WebGL 2 Support (WebGPU as Target)". The design-intent sentence DOC `:3` ("an architecture designed for … WebGL 2 and WebGPU compatibility") is left as written.
- Cadence and throughput: `general-engine-design.md:16` says 8, 4 or 2 substeps per tick at 60, 120 or 240 Hz (`ABS/CadenceValues.cs:69-74`), with hundreds of elements at sub-millisecond times the `ENGINE-TICK-ALLOCATION` and `ENGINE-SOA-SIMD` target.
- Connection domains (DOC `:58`): `Activation`, `Electrical`, `Rope`, `Mechanical` (`engine/gpu/WorkshopConnections.cs:11`).
- Trigger row (DOC `:155`): declared data is the owning static body slot (`engine/gpu/PhysicsGpuAbi.cs:145`), a target filter (`BodyTargetKind` and named target, `SIM/WorkshopPhysicsEngine.cs:146-148`) and an approach threshold.
- `architecture.md` AD-03: item 3 (`:94`) gives the running margin `0.004 m + (|Δv| + spin reach)·h` (`SIM/WorkshopPhysicsEngine.cs:1112-1117`); item 4 (`:95`) says normal blocks of at most four points each, demand-sized, no global cap; item 5 (`:96`) cites `ContactFeatureIdentity` (`SIM/WorkshopFrictionSolver.cs:9-10`). `:63` restated: byte-record tables today, flat SIMD-aligned tables the `ENGINE-SOA-SIMD` target.
- Particles: `engine-contracts.md:11`, `:28`, `DESIGN.md:18`, `:45` and `general-engine-design.md:21`, `:213` drop particle effects and squash-and-stretch and name the uniform-scale mapping (`AnimationProperty.UniformScale`, `ABS/AnimationDefinitions.cs:6`), consistent with DOC.
- `EL-158-rise-hold-fall-cam.md:29`: one clause added: whether a dynamic Torus segment replaces the thirteen cylinders is EL-158's own slice decision.
- E15 receipts now cite `architecture-docs-4.log` and `anchor-check-e15-design.txt`.

**Anchor check, re-run over all changed Markdown.** The E15 one-off script over the 26 changed Markdown files: **1177 fragment links, 0 broken** (`anchor-check-e16.txt`; the extra link is the new combined-tick-order link). Explicit ids of DOC and ISO are unchanged, and every inbound heading-slug fragment into them still names an existing heading. The one inbound fragment with no target, `gpu-f32-physics.md#f32-migration-status`, was already missing at the baseline; it occurs only in historical records.

**Counts and checks.** Product lines edited outside DOC and ISO: **70** (+70 −70), up from 56 because of this pass's repairs, all of which the coordinator directed; DOC +116 −95, ISO +46 −13. Lane 1 lines unchanged: `DOC:59` → `:63`, `DOC:145` → `:149`, `shared-clock-cadence.md:60`, `epics.md:4466` (`lane1-lines-e16.txt`). Tests covering the edited files, on the existing build: `ArchitectureDocsConsistencyTests` **4/0/0** (`architecture-docs-e16.log`); `Coverage.Tests` **132/0/0** (`coverage-tests-e16.log`). `git diff --check 37a259c9` clean. No code, test or build input changed; the full round is the coordinator's.

## E17 Review pass 3 repairs (implementation owner, 25 November 2026)

Repairs returned by the coordinator after review pass 3 (Review Triage Log, "Review pass 3"), with FO3-1 and EFE3-F2 fixed at their root. Line numbers are at the repaired candidate. Every write was Anvil-validated first.

**Pipeline section of `docs/general-engine-design.md` (FO3-1, EFE3-F2).** The section now states no order of its own, and each stage says what runs today or is marked target with its owner.
- `:175`: each stage states what runs today and marks targets; the absent steps now include the gas-network domain and drive debit, and "in-tick sensing" replaces the sub-step endpoint wording.
- `:178` Electrical: `ElectricalSupply.Reserve` splits a source's phase allowance between store charging and electrically fed airflow (`SIM/WorkshopPhysicsEngine.Supply.cs:107-114`); drives debit nothing (`ENGINE-SUPPLY-DEMAND`). `:179` Optical and `:180` Acoustic/Pneumatic labelled target (`ENGINE-OPTICAL`; Story 14.0 and `ENGINE-GAS-NETWORK`).
- `:181` Thermodynamic accounting: stores pay through `StorePayment` (contact work `SIM/WorkshopPhysicsEngine.cs:199`, `:205`; store-fed airflow granted by `ProportionalAllocation.Grant`, `Supply.cs:60-67`, paid at `ContinuousCommit.cs:59`; finite-work actuators `FiniteWork.cs:104-118`); ΔK ≤ E_store holds for that work; drives debit nothing.
- `:183` Forces: gravity in a scalar loop (`ContinuousReplay.cs:67-71`); guides, airflow and ambient lift add their accelerations; conveyor-drag regions `ENGINE-FIELD-FORCE`, liquid buoyancy `ENGINE-LIQUID`; drag lane `BodyDragOffset` 92 (`engine/gpu/PhysicsGpuAbi.cs:27`), not +74.
- `:185-186` Broadphase: today's speed-cap fattening and per-sub-step rebuild (`WorkshopPhysicsEngine.cs:806-866`, static–static drop `:876`); velocity fattening, incremental SAH and persistent trees are the `ENGINE-SPATIAL-QUERIES` target; the "85–95%" claim is gone.
- `:191` Islands stage heading marked target (`ENGINE-ISLANDS-SLEEP`, no island today), so `:192-193` are target text. This supersedes the E10/E11 dispositions of baseline `:191`, `:192`, which relied on the `:174` lead-in.
- `:202` Sensors: orientation at every sub-step endpoint (`WorkshopPhysicsEngine.cs:1021`, `:1345-1364`), residence once after the sub-steps (`:358`), passage on the device after the tick (`WorkshopGpuDevice.cs:243`); in-tick sensing `ENGINE-SENSOR-VOLUMES`.
- `:203-204` Publication: one pose slot per committed tick (`SIM/Program.cs:347`, `WEB/pose-ring.ts:87-99`); no renderer reads it; the ring as feed is `ENGINE-WIRE-ABI`. This supersedes the E10/E11 dispositions of baseline `:202`, `:203`.
- `:206`: dependencies run in the combined tick order (linked); a slice with feedback amends that order with its staged order and a bounded iteration budget and never sets a separate order; "within the substep" dropped.

**Debit attribution (DOC `:157`, `:397`; ISO `:206`; `epics.md:4604`).** Stores pay through `StorePayment` for contact work (`ContactWorkImpulse.FromStore`, `WorkshopPhysicsEngine.cs:199`, `:205`), store-fed airflow (granted by `ProportionalAllocation.Grant`, `Supply.cs:60-67`; paid at `ContinuousCommit.cs:59`) and finite-work actuators (`FiniteWork.cs:104-118`). `ElectricalSupply.Reserve` (`Supply.cs:107`) only splits an electrical source's phase allowance between store charging (credited and paid at `:113-114`) and electrically fed airflow (paid at `ContinuousCommit.cs:59-61`). A supplied drive debits nothing. DOC `:397` now points at the Finite Work Stores row.

**Scale wording.** "Uniform or per-axis scale mappings" (`WorkshopVisualProperty.UniformScale`, `ScaleX/Y/Z`, `engine/WorkshopVisualBinding.cs:7`, `:49-55`) at `DESIGN.md:18`, `:45`, `engine-contracts.md:11`, `:28`, `general-engine-design.md:21`, `:214`, DOC `:79`; `architecture.md:47` and `:69` made consistent.

**ISO.** Gas row (`:217`): cardinality dropped; each store is shaped by one sphere on its body (`engine/gpu/GasSceneValidation.cs:56-59`). Acoustic row (`:215`): port refusal cited (`engine/gpu/ElementPortCatalogue.cs:120-127`). New row `:214`, compliant tangential friction: normal force only (`deferred-work.md:223-225`), `ENGINE-COMPLIANT-FRICTION`, beside the membrane-layout row. The story's own citation `deferred-work.md:205` was stale (that line is an unrelated entry); `epics.md:5067` now cites `:223-225`. Islands row (`:202`): title plural ("support filters"), group membership (guide-linked bodies `:629-635`, support partners `:744-748`), and `ExpandRestitutionSupports` (`:719-754`) and `GatherRestitutionIsland` (`:756-783`) named.

**`architecture.md`.** AD-04 heading retitled "(WebGL 2; WebGPU as Target)"; no link uses its slug (`rg -i '#ad-04'` found none). `:151` and `:184`: capsule and hull refused at bundle time, hollow proxies named. `:188`: optical and acoustic paths are the `ENGINE-OPTICAL` and Story 14.0 targets.

**Proxy lists.** `general-engine-design.md:97` restated (analytic and hollow primitives; hulls refused); a hollow-kinds line added (`:102`: open `CylinderMesh` → Tube or Frustum, `TorusMesh` → Torus, `SceneGeometryExtractor.cs:121`, `:196-234`); `engine-contracts.md:88` names the hollow proxies. Every capsule and hull refusal now cites `SceneGeometryExtractor.cs:111-114` and `:125-131` (DOC `:71`, `:150`, `:218`; `general-engine-design.md:100`, `:104`, `:188`; `engine-contracts.md:88`; `DESIGN.md:87`, `:91`; `architecture.md:151`).

**Restitution.** DOC `:277`: guide-linked bodies and support partners join the group, and both functions are named. DOC `:289`: the union-find list names restitution groups of bouncing, guide-linked and supporting bodies, and the three other files carry the `CuriousContraptions.Simulation/` prefix. `epics.md:4559`: membership clause and both functions. `epics.md:4577` (ENGINE-ISLANDS-SLEEP *Deletes*): both filters with their functions; `engine-resting-stack-settling` is absorbed (`sprint-status.yaml:318`, `:591`) and its stacks remain this story's control.

**DOC citations.** `:156` orientation sensors cite `WorkshopPhysicsEngine.cs:1021`, `:1345-1364`.

**Deferred work.** One entry: element documents still cite DOC's removed "Phase 1–5" diagram (EL-046 to EL-052, CAT-009, CAT-069, EL-176 item 4); source review pass 3; owner the coordinator to assign.

**Left alone.** DOC `:3` (rejected), the lane 1 lines, `invest/*` (ruling R1).

**Anchor check, re-run over all changed Markdown.** **1178 fragment links, 0 broken** across the 26 changed Markdown files (`anchor-check-e17.txt`).

**Counts and checks.** Product lines edited outside DOC and ISO: **89 added, 88 removed** (one line added in `general-engine-design.md`), up from 70 because of this pass's directed repairs; DOC +117 −96, ISO +47 −13. Lane 1 lines unchanged: `DOC:59` → `:63`, `DOC:145` → `:149`, `shared-clock-cadence.md:60`, `epics.md:4466` (`lane1-lines-e17.txt`). Tests covering the edited files, on the existing build: `ArchitectureDocsConsistencyTests` **4/0/0** (`architecture-docs-e17.log`); `Coverage.Tests` **132/0/0** (`coverage-tests-e17.log`). `git diff --check 37a259c9` clean. No code, test or build input changed; the full round is the coordinator's.

## E18 — review pass 4 repairs (25 November 2026)

Repairs for the coordinator's pass-4 findings (spec Review Triage Log), under its stagnation rule: every sentence written or rewritten below was checked against the cited code first, and in the `general-engine-design.md` (GED) pipeline each detail that was not verified was replaced by a pointer to DOC, ISO or `docs/hollow-geometry-design.md`. The Code Map Status line and the frontmatter `deferred:` list were not touched.

**Verified in code for this pass.**
- Scale: `CuriousContraptions.Abstractions/ElementPresentation.cs:14` (`PresentationProperty.UniformScale`, `ScaleX`, `ScaleY`, `ScaleZ`); `engine/gpu/WorkshopDeclaredPresentation.cs:741` (all four map to `AnimationProperty.UniformScale`); `engine/WorkshopVisualBinding.cs:49-55` (the presenter sets the declared axis).
- Energy: `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Supply.cs:107` (`ElectricalSupply.Reserve`), `:113` (`StoreCredit`), `:114` (`SourcePayment` for store charging); `WorkshopPhysicsEngine.ContinuousCommit.cs:59` (`StorePayment`, store-fed airflow), `:61` (`SourcePayment`, electrically fed airflow); `WorkshopPhysicsEngine.FiniteWork.cs:117`; route closure `WorkshopGpuDevice.cs:202-209`.
- Angular impulse sources: `RevoluteConstraintSolver.cs:138`, `GuideComponent.cs:219`, `WorkshopPhysicsEngine.cs:1427-1433` (membrane impulse along the membrane normal only).
- Forces: `WorkshopPhysicsEngine.ContinuousReplay.cs:65-71` (guides and membranes, then gravity and drag); `WorkshopPhysicsEngine.Gas.cs:95-114` (a gas ambient load adds to body gravity and drag); `WorkshopPhysicsEngine.ContinuousWindow.cs:32` (airflow impulse before the solve); airflow gathered at `WorkshopPhysicsEngine.cs:1010`; `CuriousContraptions.Abstractions/ConicalAirflow.cs` exists. No ambient "lift" symbol exists; the code calls it an ambient load.
- Solve: `WorkshopPhysicsEngine.Motors.cs:96-126` (warm start, solve, integrate, relax), `:129` → `WorkshopConstraintSolver.cs:701` (restitution after relax); `WorkshopConstraintSolver.cs:9` (`SoftCoefficients`: bias rate, mass scale, impulse scale); `:777` (supporting points only from a manifold with no bouncing point).
- Narrowphase: `WorkshopCollisionKernels.cs:1999-2028` (pair dispatch), `:2001-2007` (shared hollow query); `WorkshopFrictionSolver.cs:9-12` (`ContactFeatureIdentity` for warm starts).
- Publication: `CuriousContraptions.Simulation/Program.cs:306`, `:323` (`EmitRead`), `:347` (`Publish` with the composed pose slot); event staging `WorkshopGpuDevice.cs:279`.
- Gas: `engine/gpu/GasSceneValidation.cs:56-59`, `CuriousContraptions.Bundle/GasCouplingDomain.cs:44`.
- Anchors: `docs/hollow-geometry-design.md` has `## Declaration data` and `## Pair rules`; DOC has `<a id="solver-model">` and `## Solver Model and Scalable Mechanics`; `deferred-work.md` has `<a id="engine-supply-demand">` (`:229`).

**Per-line dispositions.**
- Scale, long form (`DESIGN.md:18`, `engine-contracts.md:11`, GED `:21`, DOC `:79`): declared `PresentationProperty` axis, one evaluated `AnimationProperty.UniformScale`, presenter applies the axis, each cited. Short form (`DESIGN.md:45`, `engine-contracts.md:28`, GED `:212`, `architecture.md:69`): "one scale value per binding (applied uniformly or to its declared axis)". `architecture.md:47` diagram label: "Scale @ 60 Hz".
- Energy: GED `:178` Electrical cites route closure and names `SourcePayment` (`:114`, `ContinuousCommit.cs:61`); GED `:181` Thermodynamic limits $\Delta K \le E_{\text{store}}$ to store-paid work, `FiniteWork.cs:117`. DOC `:157` same limit, `StoreCredit` at `:113`, `SourcePayment` at `:114` and `ContinuousCommit.cs:61` (was `:59-61`). DOC `:397` "store-paid work (contact work, store-fed airflow and finite-work actuators)", electrically fed airflow and store charging paid by `SourcePayment`. ISO `:206` names `SourcePayment` (`:114`, `ContinuousCommit.cs:61`). `epics.md:4604` cites `deferred-work.md#engine-supply-demand` and names `SourcePayment`. `deferred-work.md:232` evidence: `Reserve` grants store charging and electrically fed airflow, paid by `SourcePayment` (line-local).
- Hollow proxies: `engine-contracts.md:88`, GED `:102` and `architecture.md:151` now point to `hollow-geometry-design.md#declaration-data` instead of restating the authoring rule; `DESIGN.md:89` adds the same pointer to the proxy list (one added line). Relative paths: `docs/` files `hollow-geometry-design.md`, `DESIGN.md` `docs/hollow-geometry-design.md`, `architecture.md` `../../docs/hollow-geometry-design.md`.
- Restitution: DOC `:277` and `epics.md:4559` state that a manifold's supporting points join only when it has no bouncing point (`:777`).
- GED stages: `:180` conical airflow runs today, only pressure and duct flow (`ENGINE-GAS-NETWORK`) and acoustic propagation (Story 14.0) are target; `:183` gravity and drag in one loop, ambient loads, guides and membranes before it, airflow before the solve, and angular velocity changed by joint, guide and membrane impulses as well as contacts (replaces "only through contact impulses"; the uncited inertia and drag restatements dropped); `:188` cites the pair dispatch; `:189` hollow query cited, pair rules by pointer (replaces the uncited "hollow SDF with normal gradients"); `:190` warm-start identity cited, friction detail by pointer to DOC; `:195` cites the solve order and points to DOC for row laws, rolling resistance and the recovery cap (replaces the uncited rolling and cap restatement, which also clears the deferred EFE4-d1 text); `:196` restitution is a separate block solve after relax (`Motors.cs:129` → `:701`), not part of the soft mass (replaces the formula line); `:200` event staging cited (`WorkshopGpuDevice.cs:279`); `:202` one pose slot per published read (`Program.cs:306`, `:323`, `:347`); `:212` "(ANIM-1 removes the remaining legacy evaluators)" deleted.
- ISO: `:102` (§2.1) "writes one per published read" with the three `Program.cs` citations (was "per committed tick"); `:214` compliant friction cites `WorkshopPhysicsEngine.cs:1427-1433` first, `deferred-work.md:223-225` second; `:217` gas row: "each gas shape is a distinct sphere collider on its store's body" (`GasSceneValidation.cs:56-59`, `GasCouplingDomain.cs:44`), conical airflow and gas ambient loads cited (was "ambient lift", uncited).
- DOC `:156`: "(Domino: 45°)" dropped.
- `architecture.md:184`: Plane added to the `ColliderTable` list.
- `deferred-work.md:408` (Phase 1–5 entry): scope stated by the selector `rg -l 'Phase [1-5]' docs/planning/elements --glob '!**/readiness/audits/**'`, 43 files, with the known diagram citations; it adds the 9 files that cite the stale `deferred-work.md:211` (selector `rg -l 'deferred-work.md:211' docs/planning/elements --glob '!**/readiness/audits/**'`: EL-217, EL-220, EL-221, EL-222, EL-233, EL-234, EL-237, EL-241, `readiness/new-identities.md`). At baseline `37a259c9` line 211 is the tensile-creep "Resolved by Story 10.3b" status, not the drive-supply entry.

**AC2: M4 on the final candidate.** 219 lines in 52 files (`m4-candidate-e18.txt`; E15: 218 in 52). Against E15 with line numbers stripped, 9 hits changed text on lines E15 already classified (`architecture.md` rendering-graph label, AD-04 rule and graphics-API row; ISO `KinematicMode` row; `engine-contracts.md:11`; GED `:16` and `:175`; DOC `:277`; `simulation-presentation-bridge.md:3`), each still present with a citation or target with its owner. One new hit: `architecture.md:63`, matched by `\bSoA\b` inside `ENGINE-SOA-SIMD`; it is target ("flat, contiguous, SIMD-aligned tables are the `ENGINE-SOA-SIMD` target"). `architecture.md:66` ("A single homogeneous scene record executing in WebAssembly SIMD128 … at 120 Hz (4 sub-steps …)") is not an M4 hit; classified target: its single homogeneous record is the `ENGINE-SOA-SIMD` table that `:63` directly above marks as target, while the SIMD128 build (DOC `:12`) and the 120 Hz default with 4 sub-steps (`CuriousContraptions.Abstractions/CadenceValues.cs:68-74`) are present.

**Counts and checks.** Product lines edited outside DOC and ISO: **94 added, 94 removed** (`DESIGN.md` +8 −7 for its added pointer line; GED +32 −33, the stage 6 restatement and formula lines replaced by two cited lines); DOC +117 −96, ISO +47 −13. Lane 1 lines unchanged: `DOC:59` → `:63`, `DOC:145` → `:149`, `shared-clock-cadence.md:60`, `epics.md:4466`. Fragment check over the 26 changed Markdown files: 1185 links with fragments, 0 broken (`anchor-check-e18.log`); control: the same slug rules flag `docs/hollow-geometry-design.md#no-such-anchor` as broken while passing `#declaration-data` and DOC `#solver-model` (1 of 3 broken). Tests covering the edited files, on the existing build: `ArchitectureDocsConsistencyTests` **4/0/0** (`arch-docs-e18.log`); `Coverage.Tests` **132/0/0** (`coverage-tests-e18.log`). `git diff --check 37a259c9` clean. No code, test or build input changed; the full round is the coordinator's.

## E19 — review pass 5 repairs (25 November 2026)

Repairs for the coordinator's pass-5 findings (spec Review Triage Log, "Review pass 5"). As in E18, the cited code was opened before each sentence was written. The contract's Code Map `DOC:149` bullet and frontmatter `deferred:` list were not touched.

**Correction to E18 (append-only).** E18 called baseline `deferred-work.md:211` the "tensile-creep" status. It is the status line of the Story 10.3b lever entry, `<a id="lever-equal-load-indefinite-balance">` (`git show 37a259c9:_bmad-output/implementation-artifacts/deferred-work.md`, anchor at line 206, status at 211).

**Verified in code for this pass.**
- Ambient loads: `CuriousContraptions.Abstractions/GasCouplingValues.cs:18-20` (`AmbientLoadDescriptor`: body, acceleration, linear drag, optional `InventoryFraction`; "inventory modulation applies only to acceleration"); `engine/gpu/WorkshopPhysicsCompiler.Gas.cs:21`, `:36-39` (acceleration and drag both multiplied by `AtmosphereBoundary.RelativePressure`); `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Gas.cs:95-114` (fraction applied to acceleration only when a store is owned; drag added unscaled by it); `AdvanceGas` (no early return) calls it at `Gas.cs:43`, reached every sub-step from `WorkshopPhysicsEngine.cs:994`.
- Velocity writes outside impulses: `WorkshopConstraintSolver.cs:61-68` (`ClampVelocity`, spin clamp), `:79` (non-finite reset); DOC Maximum Velocities row under `<a id="game-grade-envelope">`.
- Cylinder rims: `WorkshopCollisionKernels.cs:1202-1211` (fixed-count golden-section and rim-sample searches), `GoldenMinimum` `:1345`, `RimMinimum` `:1371`.
- Event staging: `WorkshopGpuDevice.cs:277` (finite work), `:279` (other operations), `:280` (`PreparePhysicalCommit`).
- Route closure: `engine/gpu/SupplyRoute.cs:336` (powered only while enabled and `source.Remaining.Value > 0`).
- Pose slot: `CuriousContraptions.Simulation/Program.cs:335` (`EmitRead`), `:347` (slot composed); callers `:296` (debt stop), `:306`, `:323`.

**Per-line dispositions.**
- Ambient loads, restated with no element names: ISO `:217` (gas row) and `:219` (force-regions row); DOC `:153` Force Regions "Today" list and its application cell (`WorkshopPhysicsCompiler.Gas.cs:36-39` → `WorkshopPhysicsEngine.Gas.cs:95-114`, every sub-step from `WorkshopPhysicsEngine.cs:994`); GED `:183` stage 2.
- GED `:183`: the "Angular velocity changes only through impulses" sentence deleted, replaced by a pointer to DOC's Maximum Velocities row (`gpu-f32-physics.md#game-grade-envelope`); bare `(:65-66)` now `CuriousContraptions.Simulation/WorkshopPhysicsEngine.ContinuousReplay.cs:65-66`.
- GED `:188`: "closed-form" → "analytic … with fixed-count searches for cylinder rims" (cited above).
- GED `:200`: staging restated with `:277`, `:279` and `PreparePhysicalCommit` `:280`.
- GED `:178`: a route closes only while its source is enabled and has energy left (`SupplyRoute.cs:336`).
- GED `:202` and ISO `:102` (§2.1): `EmitRead` (`Program.cs:335`, slot at `:347`) is the single site, with three callers including the debt-stop read at `:296`.
- `:777` no-bounce support rule, classified present today and open under `ENGINE-ISLANDS-SLEEP`: DOC `:277` moves it from the present-tense description into the "Open, owned by `ENGINE-ISLANDS-SLEEP`" list; ISO `:202` islands row adds it, with "Island grouping replaces these rules"; `epics.md:4577` (Deletes) adds it beside the two upward-normal filters, consistent with `:4559` (Present today).
- `deferred-work.md:408`: drive-supply citers widened to `rg -l 'deferred-work\.md:(209|211)' docs/planning/elements --glob '!**/readiness/audits/**'`: 13 files (adds EL-068, EL-196, EL-199 and `readiness/batch-c.md`).
- Not changed, for the coordinator: `architecture.md:230` ("Gas-filled Balloons experience net vertical buoyant acceleration $\mathbf{a}_b = (\rho_{\text{ambient}}/\rho_{\text{gas}} - 1)\mathbf{g}$") names an element and states a formula that the ambient-load code does not compute (it applies a declared acceleration). It is outside this pass's list.

**AC2: M4 on the final candidate.** 219 lines in 52 files (`m4-candidate-e19.txt`), the same set as E18 with line numbers stripped except DOC `:277`, whose text changed and stays target-classified (its open list is owned by `ENGINE-ISLANDS-SLEEP`). `architecture.md:66` classification unchanged (target, E18).

**Counts and checks.** Product lines edited outside DOC and ISO: **94 added, 94 removed** (unchanged; this pass edited lines already in the diff); DOC +117 −96, ISO +47 −13. Lane 1 lines unchanged. Fragment check over the 26 changed Markdown files: 1186 links with fragments, 0 broken (`anchor-check-e19.log`); control: DOC `#game-grade-envelope` resolves and `#no-such-anchor` is flagged. `ArchitectureDocsConsistencyTests` **4/0/0** (`arch-docs-e19.log`); `Coverage.Tests` **132/0/0** (`coverage-tests-e19.log`). `git diff --check 37a259c9` clean. No code, test or build input changed; the full round is the coordinator's.

## E20 — review pass 6 repairs (25 November 2026)

Repairs for the coordinator's pass-6 findings (spec Review Triage Log, "Review pass 6"). The cited code was opened before each sentence was written. The contract's frontmatter `deferred:`, Status line and Code Map `DOC:149` bullet were not touched.

**Verified in code for this pass.**
- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:767-778` (`hasBounce` from the manifold's points; supporting points admitted only when it is false, `:777`); `_bmad-output/implementation-artifacts/spec-engine-supported-impact-restitution.md:92` (a review finding that called dropping those points a defect).
- Event staging: `WorkshopGpuDevice.cs:276-279` (operation switch: `ApplyFiniteWork` → `EventRecordProducer.StageFiniteWork`, otherwise `_candidateEvents.Stage`), `:280` (`PreparePhysicalCommit`); `engine/gpu/EventRecordProducer.cs:156-160` (`StageFiniteWork` appends finite-work records only), `:93` (the advance path appends finite work with the other kinds).
- `WorkshopCollisionKernels.cs:1418` (box-edge `GoldenMinimum` over `SegmentCurve` with `SegmentSteps`).
- DOC envelope rows: Maximum Velocities (`:391`) and Non-Finite Quantities (`:395`) under `<a id="game-grade-envelope">`.
- `rg -l 'deferred-work\.md:(209|211|212)' docs/planning/elements --glob '!**/readiness/audits/**'`: still 13 files; `readiness/batch-c.md:106` cites `:212`, a blank line at `37a259c9`.
- `architecture.md:230` (element-named buoyancy formula); `epics.md:5240` cites `SIM/WorkshopPhysicsCompiler.Gas.cs`, which does not exist (`engine/gpu/WorkshopPhysicsCompiler.Gas.cs` does); `epics.md:4834` ("buoyancy exists only for a gas-filled body in air").

**Per-line dispositions.**
- `:777` rule reverted to present behaviour (pass-5 classification withdrawn): removed from `epics.md:4577` (Deletes) and from DOC `:277`'s open list; stated as present, cited, in DOC `:277`'s description (where E18 had it) and kept in `epics.md:4559` (Present today, unchanged since E18). ISO `:202`: "Island grouping replaces the 8-point cut and both upward-normal filters", followed by the `:777` rule as present behaviour not owned by the row, pointing to the new deferred entry.
- `deferred-work.md` new entries: `#restitution-mixed-manifold-rule` (owner `ENGINE-ISLANDS-SLEEP` planning; worded as the coordinator gave it), `architecture.md:230`, `epics.md:5240`, `epics.md:4834` (owner the coordinator to assign).
- GED `:200` stage 8: staging split by operation, `ApplyFiniteWork` (`:277`, `EventRecordProducer.cs:156-160`) and advance (`:279`, `EventRecordProducer.cs:93`), both before `PreparePhysicalCommit` (`:280`).
- GED `:188`: "fixed-count searches over box edges and cylinder rims for cylinder pairs", adding `SegmentSteps` `:1418`.
- GED `:183`: the pointer names the Maximum Velocities and Non-Finite Quantities rows of the game-grade envelope.
- ISO `:219`: adds the optional inventory fraction (acceleration only), matching `:217`.
- `deferred-work.md:408`: selector widened to `(209|211|212)`; forms now `:209`, `:209-213`, `:211` or `:212`; count unchanged at 13.

**AC2: M4 on the final candidate.** 219 lines in 52 files (`m4-candidate-e20.txt`); with line numbers stripped, the same set as E19 except DOC `:277`, whose text changed and stays classified (present facts cited; its open list owned by `ENGINE-ISLANDS-SLEEP`).

**Counts and checks.** Outside DOC and ISO **94 added, 94 removed** (unchanged); DOC +117 −96, ISO +47 −13. Lane 1 lines unchanged. Fragment check over the 26 changed Markdown files: 1186 links, 0 broken (`anchor-check-e20.log`); control: `deferred-work.md` resolves `restitution-mixed-manifold-rule` and `engine-supply-demand` and flags `no-such-anchor`. `ArchitectureDocsConsistencyTests` **4/0/0** (`arch-docs-e20.log`); `Coverage.Tests` **132/0/0** (`coverage-tests-e20.log`). `git diff --check 37a259c9` clean. No code, test or build input changed; the full round is the coordinator's.

## E21 — review pass 7 repairs (25 November 2026)

Repairs for the coordinator's pass-7 findings (spec Review Triage Log, "Review pass 7"). The cited code and lines were opened before each sentence was written. The contract's frontmatter `deferred:` and Status line were not touched.

**Verified for this pass.**
- `engine/gpu/GasSceneValidation.cs:39-41`: every gas store on a body is gathered and their reference masses summed, so a body may hold several.
- `epics.md:4800` ("one finite gas store per body"); `ENGINE-ISLANDS-SLEEP` *Acceptance* list: cases for sleep, wake, moving-surface/airflow/powered-hinge exclusion, drive confinement, three-box rest and the 8-point cap; none exercises the upward-normal filters (`WorkshopConstraintSolver.cs:739`, `:769`).
- `docs/planning/elements/readiness/batch-c.md:106`: "`deferred-work.md:212` names the non-existent `ElectricalSupply.Allocate`; the symbol is `ElectricalSupply.Reserve`"; `readiness/completion-ledger.md:288` (PD-43) cites `deferred-work.md:221`, which at `37a259c9` is the destination line of the compliant-membrane performance entry (P0-034). `rg -l 'deferred-work\.md:(209|211|212|221)' docs/planning/elements --glob '!**/readiness/audits/**'`: 14 files.

**Per-line dispositions.**
- `:777` ownership, one wording in three places (present behaviour; target decision deferred to `ENGINE-ISLANDS-SLEEP` planning, `deferred-work.md#restitution-mixed-manifold-rule`): ISO `:202` replaces "not owned here"; DOC `:277` adds "target undecided" with a link labelled "deferred to ENGINE-ISLANDS-SLEEP planning" to `../_bmad-output/implementation-artifacts/deferred-work.md#restitution-mixed-manifold-rule`; `epics.md:4559` adds a clause pointing to that entry.
- `deferred-work.md:408` stale-citation sentence: selector widened to `(209|211|212|221)`, 14 files. Binary-drive-supply citers (`:209`, `:209-213`, `:211`) are listed separately from the two that cite the entry's stray `ElectricalSupply.Allocate` wording (PD-43): `readiness/batch-c.md:106` (`:212`, blank at the baseline) and `readiness/completion-ledger.md:288` (`:221`, an unrelated entry at the baseline).
- `deferred-work.md`, `epics.md:4834` entry: source changed to review pass 5, EFE5-d1.
- `deferred-work.md` new entries: `epics.md:4800` "one finite gas store per body" (owner the coordinator to assign); `ENGINE-ISLANDS-SLEEP` acceptance lacks a case catching the upward-normal filters surviving (owner `ENGINE-ISLANDS-SLEEP` planning). Source for both: review pass 7.

**AC2: M4 on the final candidate.** 219 lines in 52 files (`m4-candidate-e21.txt`); with line numbers stripped, the same set as E20 except DOC `:277`, whose text changed and keeps its classification.

**Counts and checks.** Outside DOC and ISO **94 added, 94 removed** (unchanged); DOC +117 −96, ISO +47 −13. Lane 1 lines unchanged. Fragment check over the 26 changed Markdown files: 1187 links, 0 broken (`anchor-check-e21.log`), including the new DOC link; control: the same target flags `#no-such-anchor`. `ArchitectureDocsConsistencyTests` **4/0/0** (`arch-docs-e21.log`); `Coverage.Tests` **132/0/0** (`coverage-tests-e21.log`). `git diff --check 37a259c9` clean. No code, test or build input changed; the full round is the coordinator's.

**Review pass 8 records patch.** The contract's M2 Execution cell now also cites E19 to E21 (ISO §4.4 islands, gas and force-regions rows and §2.1 re-edited); the 27-row count is unchanged.

## E23 Final verification (implementation owner, 25 November 2026)

Executor: the implementation owner, as the contract's Verification names. Candidate: the uncommitted worktree over HEAD `37a259c9a2f08669cd339e36c3f2c6b4a3f7bd68`, after the coordinator's two record edits (`sprint-status.yaml:587` `done`, PD-23 status). Pinned toolchain (E1); every command in the foreground under `caffeinate -i`. Logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`. The binlogs were recaptured into a fresh folder, `binlogs-e23/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e23`), and `CSHARP_STATIC_GATE_BINLOGS` pointed there for every later step.

1. **Capture builds** (`docs/delivery-workflow.md:38-53`, in two invocations under `set -eu`): `dotnet --version`, `dotnet workload list`, the Node v26.12.1 check, solution restore, the playtest and solution `--no-incremental` builds (`capture-e23-a.log`), then the web restore and build and the `CheckStagedTests.cs` and `VerifyPolicy.cs` builds (`capture-e23-b.log`). Both exit 0; 0 warnings, 0 errors in each MSBuild summary. Binlogs: `playtest.binlog`, `solution.binlog`, `web.binlog`, `check-staged-tests.binlog`, `verify-policy.binlog`.
2. **Maintainability gate**, `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .`: **PASS**, violations 0 (metrics 0, coupling 0); both canary lines `PASS canary reported for every metric; test-project canary not reported` and `PASS coupling canary reported every strength and rule; controls clean; test project excluded`; `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e23.log`.
3. **Registration check filter** (`docs/delivery-workflow.md#new-package-registration-check`): **821 passed, 0 failed, 0 skipped** (WorkerAotGuard 17, BundleElements 195, Coverage 83, E2eRunner 97, CuriousContraptions.tests 429); `registration-e23.log`.
4. **Full local round**, `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "$(dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter)" -- RunConfiguration.TreatNoTestsAsError=true`: **7407 passed, 0 failed, `Skipped: 0`** (WorkerAotGuard 17, BundleElements 323, Coverage 132, E2eRunner 256, CuriousContraptions.tests 6679), exit 0. The log opens with `HEAD 37a259c9…` and the filter and closes with `HEAD-AFTER 37a259c9…`; `full-round-e23.log`. The filter excludes only the deferred browser-toolchain cases the E2eRunner lists.
5. **Whitespace check**, `git diff --check 37a259c9`: clean, exit 0; `diff-check-e23.log`.

No file other than this run log and the contract's acceptance table was edited for this entry.

## E22 Coordinator: review passes 1–8, Final diff scope, contract budget and close note (25 November 2026)

**Review summary.**
- Passes 1 and 2 ran all layers. Pass 1 used all six; from pass 2 on, Intent Alignment is not run on later passes. From pass 3 on, each later pass reviewed an exact repair diff built against a tree object of the previous candidate. Those tree objects are `8c9b77d2`, `37658cdd`, `5000c608`, `2ac5a74c`, `81400915` and `4e242aa3`; all diffs are under `.tools/doc-engine-status-evidence/`.
- Pass 6 skipped Edge Case Hunter, because the repair changed no path that layer traces. Pass 8 also skipped Blind Hunter, because the repair touched only records and one link.
- Stagnation triggers fired twice, after pass 3 and after pass 4, and both were reassessed. After pass 4, the implementer was required to verify every sentence against the code, and unverified detail was replaced by pointers.
- Final state: Forward-Only has no violation since pass 4, and Verification Gap has found no gap since pass 5. Pass 8 left two record findings, both repaired.

**Final diff scope (pass-8 Element-Free auditor, re-engaged as the informed existing reviewer): Pass.**
- 28 files, +1473/−224; every hunk was inspected.
- Product edits outside DOC and ISO total +94/−94. The pose-ring comments add another +2/−2.
- R1 exclusions are respected, and the lane 1 lines are byte-identical.
- Nothing was removed, and the reviewer found no justified simplification. Three candidates were considered and rejected: anchor-for-line citations, shortening the scale-mapping wording, and merging the mandated 24q and deferral copies.

**Contract budget.** The contract stood at 10,815 words, over the 8,000-word closing budget. The verbatim triage rows of passes 1–4 are moved here, below. The contract keeps one summary line per pass, plus ruling D2.

**Close note.**
- **Recurring finding classes:**
  - Present-tense restatements in secondary documents kept producing new contradicted claims, because each rewrite added detail nobody had checked. The cure was a sentence-by-sentence code check plus pointers in place of restated detail (pass 4).
  - Two coordinator instructions were themselves wrong. The pass-1 triage gave the membrane samples to `ENGINE-COMPLIANT-FRICTION`, and pass 5 deleted the `:777` rule. Each was caught on the next pass and repaired.
  - Records lagged their repairs: the Status line, the frontmatter `deferred:` entries and acceptance-table cells.
- **Unresolved or cause unknown:** none in the product. Every item deferred under Re-review scope is in the contract's frontmatter `deferred:` list and in `deferred-work.md`, each with an owner:
  - the automated anchor test (VG-1);
  - the stale "Phase 1–5" citations (43 files);
  - the stale `deferred-work.md` line citations (14 files);
  - `architecture.md:230`, `epics.md:5240`, `epics.md:4834` and `epics.md:4800`;
  - the restitution mixed-manifold target;
  - ENGINE-ISLANDS-SLEEP's missing filter acceptance case.
- **Capabilities found missing late:** none; this is a documents-only slice.
- **Role-card lesson for the next policy change, one line:** a coordinator repair instruction that assigns an owner or deletes a rule must cite the owning story's contract or acceptance line, or else route as a deferral.
- **Merge-time work** for the publish-token holder:
  - take main's lines for `DOC:145` (now `:149`), `shared-clock-cadence.md:60` and `epics.md:4466`;
  - fix DOC's remaining "island index" claim;
  - delete the ISO §4.4 `KinematicMode.Kinematic` row;
  - re-check `DOC:59` (now `:63`);
  - re-check every citation into a file that main changed (lane 1 is on `origin/main` at `466ff4ca`).

**Verbatim Review Triage Log rows, passes 1–4, moved from the contract:**

### 2026-11-25 — Review pass 1 (engine tier, all six layers; candidate: uncommitted tree on `37a259c9`; detail in run log E14)
- verdicts: 51 findings. high 1, medium 9, low 26, false 0, maybe-false 0; 15 rejected or descriptive. One is deferred under the intent contract's Never clause and reported to the coordinator.
- coordinator ruling D2 (on EFE-F8): M1 is met when every sentence on the story's topic list, changed or unchanged, cites running code or is target with an owner, and no other sentence of DOC or ISO is contradicted by the code. This is the story's own acceptance. Untouched sentences off the topic list need no new citation.
- findings:
  - `[high]` `[patch]` FO-1 `browser-physics-performance.md:69` keeps island sleep as an option tested with "sleep denied" as a control, which contradicts `epics.md:4455` and coordinator decision 41. Restate it as the `ENGINE-ISLANDS-SLEEP` target, with no sleep-off control.
  - `[false]` `[reject]` FO-b1 `KinematicMode.Kinematic` is lane 1's deletion (ruling R2). The candidate changes no code.
  - `[false]` `[reject]` FO-b2 The kinematic wording on the lane 1 lines is left unedited by ruling and the coordinator message (E6).
  - `[false]` `[reject]` FO-b3 The pose ring is written but never read. This is pre-existing code, the slice changes it nowhere, and `ENGINE-WIRE-ABI`'s Deletes removes the second feed. The documents now state it.
  - `[medium]` `[patch]` IA-1 DOC does not state the `Atomics.wait` blocking, the hand-defined layouts, or the FlatBuffers control plane. The story's wire-and-threading clause says "`DOC` and the isolation authority also state". Add them to DOC.
  - `[false]` `[reject]` IA-2 `DOC:147` is unchanged because it is a lane 1 line (E6). It is re-checked at merge.
  - `[low]` `[patch]` IA-3 The kinematic owner is lane 1's contract, not an engine story. Ruling R2 assigns it there. Repaired with EC-7: name the path and mark it unpublished.
  - `[false]` `[reject]` IA-4 `AGENTS.md:73`, `delivery-workflow.md:268`, `requirements.md:28`, E2 and `epics.md:1275` are required, target or historical text that ruling R1 excludes.
  - `[false]` `[reject]` IA-5 The story's hollow evidence is stale against the baseline. This is recorded as D1; it is not a gap.
  - `[false]` `[reject]` IA-6 Edits outside the named surface are each authorised: the `WorkshopPoseRing.cs` comments by R4, the ISLANDS-SLEEP line by R3, and the nine documents by R1.
  - `[medium]` `[patch]` EC-1, BH-2 `DESIGN.md:17`, `engine-contracts.md:25` and `general-engine-design.md:16` still present Capsule, MNA, optical and acoustic fields as running, and `DESIGN.md:87` maps `col_capsule`. Mark each as target with its owner, or remove it.
  - `[low]` `[patch]` EC-2, EC-3, VG-5 `architecture.md` "no production reader" and the removed reader reference. A `PoseReader` is built at `workshop-client.ts:152`, `:256-257` but never read. Say "constructed, never read" and name it for `ENGINE-WIRE-ABI`.
  - `[medium]` `[patch]` EC-4 The same-build replay wording gives "tree traversal order" as the reason it is unestablished, but that order is deterministic for a fixed collider order. Restate: canonical order is the target, and replay is exercised by the Reset tests (`TrampolineTests.cs:2174`), not established in general.
  - `[medium]` `[patch]` EC-5 `simulation-presentation-bridge.md:33` deleted the same-build replay statement. Restore it as a property exercised by tests, with a citation.
  - `[low]` `[patch]` EC-6, EFE-F6b, BH-6b The "+Y" wording is wrong. `:739` admits any normal with an upward component, and `:769` admits any normal inside the group. Correct it in DOC, ISO and `epics.md:4559`.
  - `[low]` `[patch]` EC-7, VG-3 The §4.4 owner names `spec-delete-declaration-colliders.md`, which is not in this tree. Give its repository path, mark it unpublished on lane 1, and keep the drop-at-merge rule.
  - `[low]` `[patch]` EC-8, BH-9 The tensile row is overstated as SIMD. It broadcasts one row into all four lanes (`TensileConstraintSolver.cs:80-82`); it is not batched.
  - `[low]` `[patch]` EC-9, VG-4, EFE-F6d The trigger citation `:139-143` is wrong. The static owner is at `:145`, the first-per-tick gate at `:152-155`, and the reset at `:311-315`.
  - `[low]` `[patch]` EC-10 The collider enum is cited as `PhysicsDeclarationValues.cs:8`; it is at `:12`. Hollow admission is cited as `WorkshopConstraintSolver.cs:161`; correct both citations (`:161` is support displacement on dynamic hollow bodies).
  - `[medium]` `[patch]` EC-11 The plane broadphase box is a fixed ±100 m cube (`WorkshopPhysicsEngine.cs:826-829`). Add this to the plane-bounds row under `ENGINE-SPATIAL-QUERIES`.
  - `[low]` `[patch]` EC-12, EFE-F6c "The only other worker is the clock master" is wrong as written. Say the physics worker spawns only the clock master; the Animation worker is separate.
  - `[false]` `[reject]` EC-13 AGENTS, the delivery workflow and the PRD are excluded by ruling R1 (rule or target text).
  - `[medium]` `[defer]` VG-1 No automated check covers link anchors in the edited documents. The intent contract's Never clause forbids test changes in this slice. For this candidate, an executed anchor check of every link in the edited documents is added to the patch. An automated anchor test is recorded in `deferred-work.md` and reported to the coordinator.
  - `[low]` `[patch]` VG-2 The anchor `simulation-presentation-bridge.md:83` → `presentation-bindings.md#declared-cosmetic-curves-…` is broken. Point it at the correct heading.
  - `[low]` `[patch]` VG-6 The `deferred-work.md` entry misdescribes the three `tools/e2e` comments.
  - `[medium]` `[patch]` BH-1 Velocity fattening is stated as running in `DESIGN.md:17`, `engine-contracts.md:11`, `:25`, `general-engine-design.md:16`, `browser-physics-performance.md:69`, DOC `:42` and the `:208` heading. Today the fattening uses the speed caps (`:810-820`), so mark it as target.
  - `[false]` `[reject]` BH-3 The lane 1 lines `DOC:147` and `shared-clock-cadence.md:60` are left by ruling. Their owner path is repaired in EC-7.
  - `[false]` `[reject]` BH-4 AGENTS and the other required or target text are excluded by ruling R1.
  - `[medium]` `[patch]` BH-5 Two present claims are contradicted. `browser-physics-performance.md:69` claims "zero scratch allocation", and `DOC:358` claims "no … Cartesian reservation" while the compliant-surface × body reservation exists (`PhysicsWorldLayout.cs:90-94`), as do the four fixed membrane samples (`:1555`). Add §4.4 rows: reservation to `ENGINE-CAPABILITY-INSTANCES`, compliant friction to `ENGINE-COMPLIANT-FRICTION` (index `:125`), allocation to `ENGINE-TICK-ALLOCATION`.
  - `[low]` `[patch]` BH-6 The +Y owner has no obligation to fix it. Add the upward-normal filter to `ENGINE-ISLANDS-SLEEP`'s Deletes line as well as its Present today.
  - `[low]` `[patch]` BH-7 The restitution bullet keeps the stale per-manifold sentence beside "solved together across the group" (`:700`, `:850-862`). Make it one description.
  - `[low]` `[patch]` BH-8 `general-engine-design.md:174` leaves step 8 (sensor evaluation at substep endpoints) reading as current. Add it to the absent list.
  - `[low]` `[patch]` BH-10 AD-03 item 9 in `architecture.md` gives `ENGINE-SPATIAL-QUERIES` a "bitwise identical" target. Restate it as canonical order, with no byte-identity claim.
  - `[low]` `[patch]` BH-11 Interpolation is labelled inconsistently. Today the renderer interpolates motion pieces from the posted read (`PhysicsMotionRead.cs:132-185`); only the feed from the ring is target. Fix DOC's Renderer line and `architecture.md` §4.
  - `[low]` `[patch]` BH-12 The slice's status disagrees across records: the `epics.md:4462` story Status, and the lane 4 bullet at `TODO.md:11`.
  - `[low]` `[patch]` BH-13 24q renders inside 24p's (f) bullet. Add a blank line or start a new list item.
  - `[low]` `[patch]` BH-14 The `DOC:396` Sensor Crossing target of "≤ 0.1 ms" has no gameplay reason. Make the target the in-tick swept crossing stated by `ENGINE-SENSOR-VOLUMES`, with no precision number.
  - `[medium]` `[patch]` EFE-F1 WebGPU is stated as present (DOC `:16`, `:18`, `:48`; `DESIGN.md:19`; `engine-contracts.md:26`; `browser-physics-performance.md:7`; `simulation-presentation-bridge.md:26`). `project.godot:17-18` selects `gl_compatibility`. Mark it as target under `ENGINE-INSTANCED-RENDERING` and add a §4.4 row.
  - `[low]` `[patch]` EFE-F2 ISO `:92-99` and `:110` describe an audio worker, and none exists. If an epics story owns one, cite it. Otherwise state "none today" and report it to the coordinator.
  - `[low]` `[patch]` EFE-F3 `DOC:368` "preserves total energy". Restate it to the bounded behaviour of the energy envelope.
  - `[low]` `[patch]` EFE-F4 The `DOC:367` quaternion formula does not match the code's exact half-angle update (`:1190-1195`).
  - `[low]` `[patch]` EFE-F5 `DOC:77` mentions particle effects, which do not exist and have no owner. Remove them, and restate squash-and-stretch to what exists.
  - `[low]` `[patch]` EFE-F6a `SocketPin` (DOC `:34`) does not exist in the code.
  - `[low]` `[patch]` EFE-F7 `epics.md:4559` says "no union-find exists", but restitution grouping is a union-find (`WorkshopConstraintSolver.cs:608-616`, `:660-664`). The same applies to E11's "negation" classification.
  - `[low]` `[patch]` EFE-F8 The present-fact rule was narrowed. Ruling D2 above. Patch: every sentence on the topic list, unchanged ones included, cites code or is target.
  - `[low]` `[patch]` EFE-F9 The Code Map status line is stale. The coordinator fixes it in the record of this pass.
  - `[false]` `[reject]` EC-14, VG-limits, BH-limits, EFE-limits These are evidence-limit notes, not defects.

### 2026-11-25 — Review pass 2 (engine tier: Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free; no Intent Alignment on a later pass; candidate: uncommitted tree on `37a259c9`; detail in run log E16)
- verdicts: 33 findings. high 1, medium 8, low 17, false 7, maybe-false 0. Every confirmed finding is patched (pass 2 is unrestricted). The edit-count notice was sent to the coordinator.
- findings:
  - `[high]` `[patch]` FO2-1 `general-engine-design.md:174` calls its staged pipeline "the target order", which makes it a second tick-order rule beside the combined tick order. Cite EL-176's combined order and present the stages as what each domain does.
  - `[false]` `[reject]` FO2-B1 The KinematicMode row: dropping it is a merge step, not a path. Wording is clarified under EC2-3.
  - `[false]` `[reject]` FO2-B2 Pose delivery has two paths. This is pre-existing and documented; `ENGINE-WIRE-ABI` deletes the second (`epics.md:4702`).
  - `[medium]` `[patch]` VG2-1, EFE2-F1 "The only union-finds" is false: `GuideWorkspace.cs:46-58` and `PrismaticTrialWorkspace.cs:86-99` are also union-finds. Name them in DOC, ISO and `epics.md:4559`.
  - `[low]` `[patch]` VG2-2 `epics.md:4559` cites a stale `deferred-work.md:122` for `engine-resting-stack-settling`, which was removed by `daba83d0`. Re-point the citation to the sprint key, or drop it.
  - `[low]` `[patch]` VG2-3, EC2-5 The `ApplyRestitution` citation `:700` should be `:701`. The restitution `Root` citation is `:609-617`.
  - `[medium]` `[patch]` EFE2-F2 DOC `:277` says supporting points are driven to −e·vₙ, but the code targets 0 for supporting points (`WorkshopConstraintSolver.cs:846-848`, `:870`).
  - `[medium]` `[patch]` EFE2-F3 DOC `:157` and `:397` say "no free energy, ΔK ≤ E_store" as if it held today, but supplied drives debit nothing. Mark drive debit as the `ENGINE-SUPPLY-DEMAND` target and add or extend the §4.4 supply row.
  - `[low]` `[patch]` EFE2-F4 The four fixed membrane samples belong to `ENGINE-CAPABILITY-INSTANCES` (index `:125`, E6), not `ENGINE-COMPLIANT-FRICTION`. The pass-1 triage wording caused this, so correct DOC `:360` and the §4.4 row.
  - `[medium]` `[patch]` EFE2-F5, BH2-1 "Capsule later" and "follows in its own story" have no owner (DOC `:71`, `:92`, `:150`, `:218`; `:63` is lane 1's). Capsule and hull are still listed as current in `general-engine-design.md:100`, `:103`, `:187` and `engine-contracts.md:88`. Restate them as refused at bundle time (`SceneGeometryExtractor.cs:111-114`), with the convex profile as the `ENGINE-PROFILE-COLLIDER` target.
  - `[medium]` `[patch]` EFE2-F6, BH2-2 WebGPU is still stated as present at `simulation-presentation-bridge.md:3` and `architecture.md:51`, `:106` (AD-04 Rule) and `:256`. Mark each as the `ENGINE-INSTANCED-RENDERING` target. This finding survived one repair, which counts toward the stagnation trigger.
  - `[low]` `[patch]` EFE2-F7 `general-engine-design.md:16` says "fixed 120 Hz" and sub-millisecond for hundreds of elements, as if present. Restate as 8/4/2 substeps by cadence, with throughput as target.
  - `[low]` `[patch]` EFE2-F8 DOC `:58` gives the connection domains as `Electrical` and `Activation`. `WorkshopConnectionDomain` also has `Rope` and `Mechanical` (`WorkshopConnections.cs:11`).
  - `[low]` `[patch]` EFE2-E15 Run log E15 cites the pre-`DESIGN.md` receipts. Cite `architecture-docs-4.log` and `anchor-check-e15-design.txt`.
  - `[medium]` `[patch]` EC2-1, BH2-3 `DESIGN.md:11` and `general-engine-design.md` say acoustics, MNA and the gas network are "listed in Current status", but §4.4 has no row for them. Add rows: acoustics to Story 14.0, MNA to its first consuming element, the pneumatic network to `ENGINE-GAS-NETWORK`.
  - `[low]` `[patch]` EC2-2 The +Y support filter has two parts. Static-partner support is filtered by the one-sided `Ny` terms at `:769`; `:739` gates dynamic–dynamic expansion. Name both in the Deletes line, §4.4 and DOC.
  - `[low]` `[patch]` EC2-3 The §4.4 KinematicMode row needs a merge rule for both publication orders. Lane 1's tip `466ff4ca` is on the local `origin/main` ref, so at merge this slice drops the row. Reword it to "removed by lane 1; this row is deleted at the merge with lane 1's publication".
  - `[low]` `[patch]` EC2-4 The DOC `:155` trigger row should say the owner is the static body slot (`PhysicsGpuAbi.cs:145`; `WorkshopPhysicsEngine.cs:145`), with the target filter `TriggerTargetKind` (`:146-148`).
  - `[false]` `[reject]` EC2-6 DOC `:149` "island index" sits on lane 1's line, so it is fixed at merge as already planned (contract Concurrency, Design Notes 8).
  - `[false]` `[reject]` EC2-7 The other slices' drafted contracts (`spec-engine-goal-evaluation.md` and others) are under `_bmad-output/implementation-artifacts/**`, which the Never list excludes. Each is replanned at dispatch.
  - `[low]` `[patch]` EC2-8 AD-03 pattern 4 in `architecture.md` claims constant-size 4-point manifolds. Normal blocks have at most four points each, but they are demand-sized, with no global cap (DOC §5). Qualify the claim.
  - `[medium]` `[patch]` BH2-4 The AD-03 rule and its numbered items disagree: item 3's d_spec formula against the code's `0.004 + (|Δv| + spin)·h` (`:1112-1117`), and item 5's 32-bit `FeatureId` against `ContactFeatureIdentity` (`WorkshopFrictionSolver.cs:9-10`). Restate the items.
  - `[medium]` `[patch]` BH2-5 Particle effects are still stated as present at `engine-contracts.md:11`, `:28`, `DESIGN.md:18` and `general-engine-design.md:21`, `:213`. No particle evaluator exists. Remove the claim, or restate it to the `UniformScale` mapping.
  - `[false]` `[reject]` BH2-6 The present-tense claims in `delivery-workflow.md:268`, `requirements.md`, `invest/engine.md`, `gpu-physics.md`, `prd.md` and `epics.md:1275` are required, target or historical text that ruling R1 excludes. `architecture.md:63` is in an R1 document, so it is checked under BH2-7.
  - `[low]` `[patch]` BH2-7 `architecture.md:63`: if it states SoA, zero-copy or instancing as present, restate it as target.
  - `[false]` `[reject]` BH2-8 OS-01 (`completion-ledger.md:392`) is a named unchanged record in M7. It lists the pre-programme slices, and DOC-ENGINE-STATUS is programme order 1.
  - `[low]` `[patch]` BH2-9 The `EL-158:29` rationale is lost. Record that whether a dynamic Torus segment replaces the 13 cylinders is EL-158's own slice decision.
  - `[false]` `[reject]` VG2-limits, EC2-limits, EFE2-AC3 AC3 is pending the final full round on the final candidate. This is an evidence limit, not a defect.

### 2026-11-25 — Review pass 3 (Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free; repair diff from the pass-2 tree `8c9b77d2`; Re-review scope applies; detail in run log E17)
- verdicts: 30 findings. high 1, medium 5, low 18, false 4, maybe-false 0. Blocking findings are repaired: those in the repair or what it directly affects, under a hard rule, or under AC1/D2. Two are deferred under Re-review scope.
- findings:
  - `[high]` `[patch]` FO3-1 `general-engine-design.md:200` gives each owning slice its own staged order "within the substep", which is a second tick-order rule (a hard rule). Cite the combined tick order, and say an owning slice amends that order.
  - `[medium]` `[patch]` BH3-1, VG3-1, EC3-1, EFE3-F1 The debits are wrongly attributed to `ElectricalSupply.Reserve` (DOC `:157`, `:397`; ISO `:206`; `epics.md:4603-4604`). Contact work debits through `FromStore` and `StorePayment` (`WorkshopPhysicsEngine.cs:199`, `:205`). Store airflow goes through `Grant` and `StorePayment` (`Supply.cs:60-67`, `ContinuousCommit.cs:59`). Actuators pay through `StorePayment` (`FiniteWork.cs:104-118`). `Reserve` only splits a source's allowance (`Supply.cs:93-114`, `ContinuousCommit.cs:61`). Drives debit nothing.
  - `[medium]` `[patch]` EFE3-F2, BH3-6 `general-engine-design.md:174` now says the stages "describe what each domain does", which makes `:180` (ΔK, no free energy), `:182` (SIMD gravity, conveyor and buoyancy regions, body offset `+74` against `PhysicsGpuAbi.cs:27` = 92), `:184-185` (velocity fattening, SAH rotations) and `:203` (renderer ring access, substep poses) present-tense contradictions. Gas network and drive debit are also missing from the absent list. Mark each such stage as target with its owner, or restate it to what runs. Refresh the E10/E11 dispositions.
  - `[medium]` `[patch]` EC3-9 Squash and stretch was understated. Per-axis scale bindings exist (`WorkshopVisualProperty.ScaleX/Y/Z`, `engine/WorkshopVisualBinding.cs:7`, `:49-55`). Restate the six repaired lines and DOC as "uniform or per-axis scale mappings". `architecture.md:47` and `:69` then read consistently, or are worded to match.
  - `[low]` `[patch]` BH3-2, EFE3-F3 ISO `:216` "one finite gas store per body" is wrong: several stores are admitted (`GasStoreDomain.cs:17-23`, `WorkshopPhysicsCompiler.Gas.cs:28-33`, `GasSceneValidation.cs:39-43`, `RetentionCompositionTests.cs:55`). Drop the cardinality and cite the sphere-per-store rule (`GasSceneValidation.cs:56-59`).
  - `[low]` `[patch]` BH3-4, EC3-5 The AD-04 heading in `architecture.md` still says "WebGL 2 and WebGPU", against its repaired rule. Retitle it if no link points at it; otherwise qualify the first sentence.
  - `[false]` `[reject]` BH3-4b, EC3-5b DOC `:3` "designed for … WebGL 2 and WebGPU compatibility" states design intent, not a runtime claim. The Element-Free auditor agrees.
  - `[low]` `[patch]` BH3-5, EC3-4 `general-engine-design.md:97` says "convex hulls used for physics", in the parent bullet of the repaired lines. Restate it.
  - `[low]` `[patch]` EC3-6 The repaired proxy lists (`engine-contracts.md:88`, `general-engine-design.md:97-103`) omit the admitted hollow proxies (`SceneGeometryExtractor.cs:121`, `:196-234`). Add them.
  - `[low]` `[patch]` EC3-11 Capsule and convex shapes are also refused by shape type (`SceneGeometryExtractor.cs:125-131`). Cite `:111-114` and `:125-131`.
  - `[low]` `[patch]` BH3-5b, EFE3-def1 `architecture.md:151` and `:184` list capsule, hull and "convex hulls" as current. Same class as the repaired lines; fold into the same patch.
  - `[low]` `[patch]` EFE3-def2 `architecture.md:188` lists optical and acoustic paths as current. Mark them as target (`ENGINE-OPTICAL`, Story 14.0).
  - `[false]` `[reject]` VG3-3 `invest/engine.md:179`, `:202` and `vertical-delivery.md:187` (capsule and hull in ENGINE-CORE-2d) are `invest/*` target text, which ruling R1 excludes.
  - `[low]` `[patch]` BH3-7 Compliant friction lost its current-state entry. Add the row "compliant surface applies normal force only today", owned by `ENGINE-COMPLIANT-FRICTION`, beside the membrane-layout row.
  - `[low]` `[patch]` BH3-8, VG3-4, EC3-2 `epics.md:4577` still names the "deferred story `engine-resting-stack-settling`", but it has been absorbed (`sprint-status.yaml:318`, `:591`). Restate it as this story's control.
  - `[low]` `[patch]` VG3-4b, EC3-8 `:769` is in `GatherRestitutionIsland` (`:756-783`), not in support expansion (`:719-754`). Name both functions in Deletes, ISO §4.4 (row title plural) and DOC.
  - `[low]` `[patch]` VG3-5, EC3-7 Restitution `Join` also merges guide-linked bodies (`WorkshopConstraintSolver.cs:629-635`), and expansion merges partners (`:744-748`). Add that clause wherever the group's membership is described.
  - `[low]` `[patch]` EFE3-F4 ISO `:214` "its ports are refused" has no citation. Cite `engine/gpu/ElementPortCatalogue.cs:120-127`.
  - `[low]` `[patch]` EFE3-F5 The DOC `:156` orientation sensors row ("every sub-step endpoint") has no citation. Cite `WorkshopPhysicsEngine.cs:1021`, `:1345-1364`. Required under D2.
  - `[low]` `[patch]` EC3-10 The DOC `:289` citations have no directory. Prefix them with `CuriousContraptions.Simulation/`.
  - `[false]` `[reject]` FO3-b1 `SimulationDomain.Audio` is a placeholder for Story 14.0 that this slice does not touch. It is borderline compliant and has been reported to the coordinator.
  - `[low]` `[defer]` FO3-def1 Stale "Phase 2" references to DOC's old diagram: EL-046 to EL-052 ("Phase 2 acoustic pass"), CAT-009 and CAT-069 (`gpu-f32-physics.md:165` line citations), and EL-176 item 4 ("draws optics in Phase 2"). These are outside the repair, outside DOC and ISO, and break no rule. Recorded in `deferred-work.md` (Re-review scope).
  - `[low]` `[defer]` EC3-def2 The deferred-work anchor test (VG-1) remains. No new action.
  - `[false]` `[reject]` EC/VG/BH/EFE pass-3 evidence limits, and AC3 pending the final round. These are notes.

### 2026-11-25 — Review pass 4 (Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free; repair diff from the pass-3 tree `37658cdd`; Re-review scope; detail in run log E18)
- verdicts: 24 findings. high 0, medium 3, low 15, false 0, maybe-false 0; 3 deferred, 3 notes. Forward-Only: no violation; FO3-1 is repaired at the root.
- stagnation check: the gas-row wording survived two repairs (BH3-2 → B4), and pipeline-stage claims re-emerged after the pass-3 rewrite. Reassessed hypothesis: the restatements in secondary documents carry detail that is not verified against the code. Next attempt: the implementer audits every sentence it writes against the code before handing off. In the pipeline section, each stage states only cited running facts or is labelled target. Detail that is not needed is replaced by a pointer to DOC or ISO, not restated.
- findings:
  - `[medium]` `[patch]` VG4-3, EC4-1, BH4-1 The scale citation is at the wrong layer. `WorkshopVisualProperty` is the main-thread presenter. Cite `PresentationProperty.ScaleX/Y/Z` (`ABS/ElementPresentation.cs:14`). The worker evaluates one scalar, `AnimationProperty.UniformScale` (`WorkshopDeclaredPresentation.cs:741`), and the presenter applies the axis (`WorkshopVisualBinding.cs:49-55`). Fix in `DESIGN.md`, `engine-contracts.md`, `general-engine-design.md` and DOC.
  - `[low]` `[patch]` VG4-1, EC4-3, BH4-2 `epics.md:4604` cites the stale `deferred-work.md:211`. Re-point it to `#engine-supply-demand`. The entry's `:232` evidence still says `Reserve` debits contact-work stores; correct it.
  - `[medium]` `[patch]` BH4-3 The source's own debit, `SourcePayment` (`Supply.cs:114`, `ContinuousCommit.cs:61`), is not named. Name it, and limit ΔK ≤ E_store to work paid by a store.
  - `[medium]` `[patch]` EC4-6, BH4-4 The hollow-proxy authoring rule is incomplete: `wall_thickness` is required, uniform scale is required, a single open cap is refused, and a torus is solid unless declared hollow. Point to `hollow-geometry-design.md#declaration-data` instead of restating, and add the hollow kinds to `DESIGN.md:84-91`.
  - `[low]` `[patch]` BH4-5 Restitution admits supporting points only from manifolds with no bouncing point (`WorkshopConstraintSolver.cs:777`). State this in DOC and in `epics.md`'s Present today.
  - `[low]` `[patch]` VG4-4, BH4-7, EFE4-B1 `general-engine-design.md:183` "angular velocity changes only through contact impulses". Joints, guides, membranes and driven surfaces also apply angular impulse (`RevoluteConstraintSolver.cs:138`, `GuideComponent.cs:219`, `WorkshopPhysicsEngine.cs:1427-1433`).
  - `[low]` `[patch]` BH4-8 `general-engine-design.md:180` puts nozzle momentum transfer under target, but conical airflow runs today. Only pressure, duct and acoustic propagation are target.
  - `[low]` `[patch]` VG4-5, EC4-8, BH4-9, EFE4-B4 The ISO gas row should say "each gas shape is a distinct sphere collider on its store's body" (`GasSceneValidation.cs:56-59`, `GasCouplingDomain.cs:44`). This finding has survived two repairs.
  - `[low]` `[patch]` EFE4-B2 DOC `:156` "(Domino: 45°)" is an element example in an edited line. Drop it.
  - `[low]` `[patch]` EFE4-B3 The ISO compliant tangential friction row cites a record. Cite `WorkshopPhysicsEngine.cs:1427-1433`, which applies normal-only impulse.
  - `[low]` `[patch]` EFE4-B5 `general-engine-design.md:196` says restitution is in the effective constraint mass, but it is a separate block solve after relax (`:701`).
  - `[low]` `[patch]` FO4-1, VG4-2, EC4-5, BH4-6 The deferred Phase 1–5 entry undercounts. Describe its scope by the search selector and give the count.
  - `[low]` `[patch]` EC4-7 `architecture.md:184` leaves Plane out of the closed collider list.
  - `[low]` `[patch]` EC4-9 `general-engine-design.md` pose slot: one slot is written per published read (`Program.cs:306`, `:323`), not per committed tick.
  - `[low]` `[patch]` FO4-2 `general-engine-design.md:214` "(ANIM-1 removes the remaining legacy evaluators)" is stale; no legacy evaluator remains. Delete it.
  - `[low]` `[patch]` EFE4-I1 AC2 has no M4 search run on the final candidate. Re-run it, classify `architecture.md:66`, and refresh the acceptance table.
  - `[low]` `[patch]` EFE4-R1, EFE4-R2 The Code Map Status line was stale and the frontmatter `deferred:` was empty. The coordinator fixed both in this edit.
  - `[low]` `[defer]` EFE4-d1 `general-engine-design.md:195` describes rolling resistance at spheres only, and `:197` uses γ/h² notation. Incomplete or notational, not contradicted; outside the repair; frontmatter `deferred`.
  - `[low]` `[defer]` BH4-2b EL-217, EL-222 and EL-234 cite the stale `:211`. Outside the repair; frontmatter `deferred`.
  - `[low]` `[defer]` EFE4-d3 DOC `:145` "compiled into flat data tables" is not contradicted (byte records are flat). Recorded only.
  - `[false]` `[reject]` FO4-b borderlines (store and electrical airflow selected by data, the two pose feeds, scale enums mirrored across layers, the KinematicMode merge step) are compliant or already ruled.
  - `[false]` `[reject]` Evidence-limit notes, and AC3 pending the final round.

## E24 VG-1 repair: fragment anchor checking in `ArchitectureDocsConsistencyTests` (implementation owner, 26 November 2026)

Test-only repair on top of commit `d4b84b4e`, authorised by the coordinator (VG-1). Uncommitted in the worktree. The coordinator records the intent-contract amendment in the Spec Change Log; the intent contract was not edited here.

**Change.** `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs` gains a fragment checker and eight tests; the four existing tests are unchanged.
- Scope: the six authority documents the test already reads (`CurrentAuthorities` and the isolation authority) plus `SliceEditedDocuments`, an explicit list of the 26 Markdown files from `git diff --name-only 37a259c9 HEAD -- '*.md'` at `d4b84b4e`; 27 distinct documents (`AGENTS.md` is the only authority outside the slice list).
- Checker: `FindMissingFragments(documents, read)` reports each relative link with a `#fragment` whose target defines no such fragment, as `MissingFragment(Document, Line, Fragment)`. A bare `#fragment` is checked against its own document; a target path resolves relative to the linking document; URLs and `mailto:` are skipped; a missing target defines nothing. `Anchors(markdown)` collects each `<a id="…">` (or `name`) and each ATX heading slug, numbering a repeated slug `-1`, `-2`, …; lines inside code fences define and link nothing. `Slug(heading)` is GitHub's: lower case; drops every character except letters, numbers, marks, connector punctuation (`_`), `-` and spaces; each space becomes `-`.
- Tests: `EveryFragmentLinkInTheCheckedDocsResolves` (control, real tree, prints `file:line: #fragment` for each miss); `TheFragmentCheckReportsAMissingAnchorWithItsFileAndLine` (negative, cross-file: expects exactly `docs/a.md`, line 3, `absent`); `TheFragmentCheckReportsAMissingSameFileAnchor` (negative, same file: line 2, `elsewhere`); `TheFragmentCheckResolvesASameFileHeading`; `TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading` (`#setup-1`); `TheFragmentCheckResolvesAnExplicitAnchor` (`<a id>` in `../c/b.md`); `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub` (the heading ``8. Demand-grown (pools) & `Foo_bar` `` gives `8-demand-grown-pools--foo_bar`); `TheFragmentCheckIgnoresHeadingsInsideCodeFences` (a fenced `# Ghost` defines nothing, so the link is reported). In-memory documents only for the checker cases; no fixture file, hash or skip.
- The real tree passed on the first run; no document link needed fixing.

**Records.** Contract: frontmatter `deferred:` VG-1 item removed; acceptance-to-test table gains an "M6 (VG-1 repair)" row. `deferred-work.md`: the anchor-test entry is marked resolved.

**Checks** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e24/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e24`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e24-a.log`, `capture-e24-b.log`, `capture-e24-c.log`. (An incremental test-project build before them, `tests-build-e24.log`, was also clean.)
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e24.log`.
3. Registration check filter: **821/0/0** (17 + 195 + 83 + 97 + 429), unchanged (no test file added); `registration-e24.log`.
4. `ArchitectureDocsConsistencyTests`, run after every record edit in this entry (the checked documents include this run log, the contract and `deferred-work.md`): result below; `arch-docs-e24.log`.
5. `git diff --check d4b84b4e`: result below; `diff-check-e24.log`.

The full round is the coordinator's, on the tip after re-review.

**Results of 4 and 5.** `ArchitectureDocsConsistencyTests` **12/0/0** (the four existing tests and the eight new ones), `Skipped: 0`, after the record edits; `arch-docs-e24.log`. `git diff --check d4b84b4e` clean (exit 0); `diff-check-e24.log`.

## E25 VG-1 re-review pass 1 repairs (implementation owner, 26 November 2026)

Repairs on top of the uncommitted E24 work, over `d4b84b4e`. Files edited: `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs`, `_bmad-output/implementation-artifacts/deferred-work.md` and this run log. The contract is the coordinator's this round and was not edited.

- **FO-1 (one link rule).** A single enumerator, `RelativeLinks(document, text)`, yields `RelativeLinkTarget(Line, Target, Fragment)` for every Markdown link outside code fences that is not a URL or `mailto:`: path and fragment percent-decoded, path resolved against the linking document by `Resolve` (null above the repository root), a bare `#fragment` targeting the document itself. `FindMissingFragments` and the existing `EveryRelativeLinkInTheAuthorityDocsResolves` both use it; the latter keeps its name and intent (every relative link in the six authority docs names an existing file or directory) and now reports `doc:line -> target`. The old whole-text regex, `Split('#')` and `Path.Combine(directory, …)` path is deleted; the file has one link-extraction and resolution rule. Behaviour now shared by both checks: links inside code fences are not links, and paths are percent-decoded.
- **FO-2.** `ExplicitAnchor` accepts `<a id="…">` only; the `<a name>` alternative and "(or `name`)" in the `Anchors` doc comment are removed. E24's description of the checker said "(or `name`)"; that clause no longer holds.
- **VG-a.** Two in-memory negatives: `TheFragmentCheckReportsALinkToAMissingFile` (`docs/a.md` linking to `gone.md#here`, expects exactly `docs/a.md`, line 1, `here`) and `TheFragmentCheckReportsALinkAboveTheRepositoryRoot` (`../../x.md#y` from `docs/a.md`, with a root-level `x.md` defining `# Y` present, so only the root climb can fail it; expects `docs/a.md`, line 1, `y`).
- **deferred-work.md.** The resolved VG-1 entry's owner line now reads "DOC-ENGINE-STATUS, its VG-1 repair (run log E24 and E25)" (line-local).

**Test count.** `ArchitectureDocsConsistencyTests` has 14 tests: the 4 present at `d4b84b4e` (`AgentsLinksTheIsolationAuthority`, `NoAuthorityPresentsRetiredDesignAsCurrent`, `TheBanCheckRejectsAReinstatedPhrase`, `EveryRelativeLinkInTheAuthorityDocsResolves`, the last rewritten on the shared enumerator) and 10 new: `EveryFragmentLinkInTheCheckedDocsResolves`, `TheFragmentCheckReportsAMissingAnchorWithItsFileAndLine`, `TheFragmentCheckReportsAMissingSameFileAnchor`, `TheFragmentCheckResolvesASameFileHeading`, `TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading`, `TheFragmentCheckResolvesAnExplicitAnchor`, `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub`, `TheFragmentCheckIgnoresHeadingsInsideCodeFences`, `TheFragmentCheckReportsALinkToAMissingFile`, `TheFragmentCheckReportsALinkAboveTheRepositoryRoot`. New tests against `d4b84b4e`: **+10**.

**Checks** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e25/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e25`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e25-a.log`, `capture-e25-b.log`, `capture-e25-c.log` (preceded by a clean incremental test-project build, `tests-build-e25.log`).
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e25.log`.
3. Registration check filter: **821/0/0**, unchanged; `registration-e25.log`.
4. `ArchitectureDocsConsistencyTests`, run last, after this entry was written: `arch-docs-e25-final.log` (result reported to the coordinator; a pre-capture run on the same source was 14/0/0, `arch-docs-e25-pre.log`).
5. `git diff --check d4b84b4e`: `diff-check-e25.log`.

The first run of step 4 failed on this entry itself: its VG-a bullet quoted the seeded link in Markdown link syntax inside a code span, and the checker, like the one before it, does not exempt code spans, so it reported `spec-doc-engine-status.md:707` (fragment `here`). The bullet was reworded line-locally and step 4 rerun.

## E27 VG-1 re-review pass 2 repairs and the one link rule (implementation owner, 26 November 2026)

Repairs on top of the uncommitted E24 and E25 work, over `d4b84b4e`, for the coordinator's pass-2 findings and its ruling on ECH#1 (option a: one link rule over all 27 checked documents). Files edited: `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs`, `_bmad-output/implementation-artifacts/deferred-work.md`, this run log, and the document lines listed below. The contract is the coordinator's and was not edited.

**The one link rule** (`FindLinkProblems(documents, read, kindOf)`, one result type `LinkProblem(Document, Line, Kind, Detail)` with the closed `LinkProblemKind` MissingTarget, MissingFragment or UnclosedFence). Every relative link in the 27 checked documents must name an existing file or directory, case-exact; a link with a `#fragment` must also name a Markdown file that defines it. `EveryRelativeLinkInTheAuthorityDocsResolves` (six authorities, existence only) and `EveryFragmentLinkInTheCheckedDocsResolves` are replaced by the single real-tree test `EveryRelativeLinkInTheCheckedDocsResolves` over `LinkCheckedDocuments`. Per finding:
1. Closed fence: `TheFragmentCheckIgnoresHeadingsInsideCodeFences` now also has a broken link inside the fence (not reported) and one after the closing fence (reported, line 6).
2. Fence parsing: a fence opens on three or more backticks or tildes indented at most three spaces, records its character and run length, and closes only on a bare run of the same character at least as long; a fence left open is reported as `UnclosedFence` at its opening line. New fact `TheFenceScanReportsAnUnclosedFenceAndClosesOnlyOnAMatchingMarker` (a different character and a shorter run do not close; a four-space-indented marker is not a fence; the open tilde fence is reported).
3. Root-absolute links: a path starting with `/` resolves from the repository root (`Resolve`); `TheFragmentCheckResolvesAnExplicitAnchor` gains a root-absolute link to the same anchor.
4. and ruling A, existence: the existence rule is part of the one rule, through the `kindOf` callback; the in-memory negatives are `TheLinkCheckReportsALinkToAMissingFile` and `TheLinkCheckReportsALinkAboveTheRepositoryRoot` (renamed from E25's `TheFragmentCheck…` names), each now with a plain link beside the fragment link, both reported as MissingTarget with the destination as written.
5. Code spans: links and explicit anchors are matched after code spans are removed; heading slugs keep code-span text. New fact `TheLinkCheckIgnoresCodeSpans`.
6. Rendered slugs: links become their text and emphasis markers go (`*`, and `_` only at a word boundary) before slugging; `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub` adds a heading with a link (`see-the-guide`) and an emphasised one (`emphasis-and-strong`).
7. Duplicate-slug collision: github-slugger's rule, incrementing until the candidate is new; `TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading` now has a literal `Setup 1` heading and resolves `#setup-1` and `#setup-2`.
8. Heading forms: setext headings and headings inside blockquotes define anchors; front matter defines nothing. New fact `TheFragmentCheckResolvesSetextAndBlockquoteHeadings`.
9. Link forms: titled and angle-bracket inline links, reference definitions (not footnotes) and `<a href>` go through the one enumerator. New fact `TheLinkCheckReadsTitledAngleReferenceAndHrefLinks`.
10. Non-Markdown targets: only a `.md` file defines anchors; a fragment on another file or a directory is reported. New fact `TheFragmentCheckReportsAFragmentOnANonMarkdownTarget`.
11. Case: `FindEntry(path, list)` matches each path segment ordinally against a directory listing (the real tree lists `Root`, memoised per test run). New fact `TheLinkCheckMatchesPathsCaseSensitively`.
12. `AgentsLinksTheIsolationAuthority` asserts that `RelativeLinks("AGENTS.md", …)` has a link whose target is the isolation authority; `Read` is `ReadRepositoryFile(…) ?? throw new FileNotFoundException(…)`.
13. Not implemented, as ruled: `<a name>`, single-quoted or attribute-first anchors.
- Ruling B, quotations: links in a straight-double-quoted span within a line and on a blockquote line are not checked, stated in the `FindLinkProblems` doc comment. New fact `TheLinkCheckIgnoresQuotations` (a broken link inside each form is not reported; one just outside each is). Links this hides in the 27 documents: `batch-i.md:3` (two links, both existing), `:133` (one link, existing), `:137` (the blockquoted EL-242 proposal; its `../elements/CAT-049-pipe_bend_45.md` is broken from this file, as a quotation relative to its destination), and `:139` (the quoted instruction with placeholder destinations and `../elements/CAT-050-pipe_bend_90.md`). No other quoted span in the 27 documents contains a link once code spans are removed. None of these lines was edited.

**Document lines repaired under the one rule** (no other link in the 27 documents failed it):
- `docs/planning/invest/current-consumers.md` `:12` (`../../../parts/BallPart.cs`), `:43` (`BasketPart.cs`), `:137` (`BallPart.cs`), `:148` (`BumperPart.cs`), `:215` (`DelayPart.cs`), `:226` (`DominoPart.cs`), `:342` (`LampPart.cs`), `:525` (`RampPart.cs`), `:614` (`SwitchPart.cs`), `:647` (`WallPart.cs`): each link to the deleted `parts/*Part.cs` file becomes plain text naming the retired file in a code span (no link). Each sentence lists that file as legacy deleted (or, at `:137` and `:226`, retained artwork at the time), so the element package is not that file's current home.
- `DESIGN.md:419`: the links to the deleted `engine/PartArt.cs` and `engine/PartAssistance.cs` become plain text, "part geometry/materials (retired `engine/PartArt.cs`; …)" and "assistance easing (retired `engine/PartAssistance.cs`)"; no current file holds either.
- `docs/planning/elements/readiness/owner-decisions-2026-11-22.md:71`: the contract link `../../../_bmad-output/…/spec-s5c-one-wire-schema-file.md` (which lands under `docs/`) becomes `../../../../_bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md`, which exists at this baseline.

**deferred-work.md.** The VG-1 entry's present-tense sentences are reworded as history ("When this entry was raised, …"), its owner line cites E24, E25 and E27, and its status names the one rule and lists the negatives.

**Test count.** `ArchitectureDocsConsistencyTests` has 20 tests. Three are unchanged from `d4b84b4e` in name (`AgentsLinksTheIsolationAuthority`, rewritten on the enumerator; `NoAuthorityPresentsRetiredDesignAsCurrent`; `TheBanCheckRejectsAReinstatedPhrase`); `d4b84b4e`'s `EveryRelativeLinkInTheAuthorityDocsResolves` is removed, replaced by the one rule. New since `d4b84b4e`, **17**: `EveryRelativeLinkInTheCheckedDocsResolves`, `TheFragmentCheckReportsAMissingAnchorWithItsFileAndLine`, `TheFragmentCheckReportsAMissingSameFileAnchor`, `TheFragmentCheckResolvesASameFileHeading`, `TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading`, `TheFragmentCheckResolvesAnExplicitAnchor`, `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub`, `TheFragmentCheckIgnoresHeadingsInsideCodeFences`, `TheLinkCheckReportsALinkToAMissingFile`, `TheLinkCheckReportsALinkAboveTheRepositoryRoot`, `TheFenceScanReportsAnUnclosedFenceAndClosesOnlyOnAMatchingMarker`, `TheLinkCheckIgnoresCodeSpans`, `TheLinkCheckIgnoresQuotations`, `TheFragmentCheckResolvesSetextAndBlockquoteHeadings`, `TheLinkCheckReadsTitledAngleReferenceAndHrefLinks`, `TheFragmentCheckReportsAFragmentOnANonMarkdownTarget`, `TheLinkCheckMatchesPathsCaseSensitively`. Net change against `d4b84b4e`: 4 → 20 (+17 new, −1 removed).

**Checks** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e27/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e27`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e27-a.log`, `capture-e27-b.log`, `capture-e27-c.log`. Before them an incremental test-project build failed once with two CA1861 errors (constant array arguments in two new assertions), fixed by asserting tuples; the clean build is `tests-build-e27.log`.
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e27.log`.
3. Registration check filter: **821/0/0**, unchanged; `registration-e27.log`.
4. `git diff --check d4b84b4e`: `diff-check-e27.log`.
5. `ArchitectureDocsConsistencyTests`, run last, after this entry: `arch-docs-e27-final.log`. Before the document repairs the real-tree test reported exactly the 13 links above (`arch-docs-e27-pre.log`); after them, 20/0/0 (`arch-docs-e27-pre2.log`).

## E26 Coordinator: VG-1 authorisation, planning-verdict correction and re-review pass 1 (26 November 2026)

**Coordinator message (26 November 2026).**
- It authorised the VG-1 test-only repair: fragment checking in `ArchitectureDocsConsistencyTests`, with a negative case and a control, and an amendment to the contract's "no test changes" line.
- It required re-review by fresh Verification Gap and Forward-Only reviewers.
- It stated that recording Scoped Pass at the sixth planning check without a re-check is not how a verdict is recorded.

**Correction to E9.** E9 records that the sixth check reported one low finding with a prescribed repair. The coordinator applied that repair and then recorded "planning Scoped Pass" without an independent re-check of the applied line. That record was not a valid verdict: an unresolved finding is not a Pass. E9 stays unedited as history. This entry corrects it.

**Re-review pass 1** ran on the repair diff from `d4b84b4e` (evidence `blind-vg1.diff` and `full-vg1.diff`). It had three context-free reviewers:
- **Verification Gap.** One patch: no test covered a missing-file target or a path above the repository root. Two record notes: a stale close note, and a stale owner line in `deferred-work.md`. It also audited the real tree read-only: 1,207 fragment links across 43 targets, none missing.
- **Forward-Only.** Two violations, both patched: two link-extraction rules, and `<a name>` accepted as a second anchor form.
- **Element-Free, with an added duty to re-check the planning line.**
  - Duty 1: no engine change and not mis-tiered. D1-1 found that AC3 contradicted the repair; D1-2 and D1-3 found stale citations and records; D1-4 found that the triage entry was missing.
  - Duty 2: items (a)–(c) pass, so the applied Design Notes 8 line is correct and `epics.md:4466` is unedited. Item (d) failed because the corrected text claimed the check "returned Fail", which E9 does not record. The text is reworded.

The auditor also judged Blind Hunter and Edge Case Hunter in scope for this repair, because it adds new branching test code. Pass 2 therefore runs all five layers.

**Repairs.**
- The implementation owner's repairs are in E25: FO-1, FO-2, VG-a and the `deferred-work.md` owner line.
- The coordinator made these contract edits:
  - the Planning verdict line and the Spec Change Log entry, reworded;
  - AC3 and its table row, now 7407 + 10 = 7417 with registration 821;
  - the M6 (VG-1 repair) row, now citing E25;
  - the Spec Change Log entry for D1-1 and FO-1;
  - the Review Triage Log entry "VG-1 repair re-review, pass 1".
- D1-3 (the Auto Run Result, the Status line and `TODO.md`) is updated after the final round on the tip.

## E28 Coordinator: VG-1 re-review pass 2, ruling (a) and contract budget (26 November 2026)

**Pass 2 reviewers.** Pass 2 ran on the diff from `d4b84b4e` (`blind-vg1-p2.diff`, `full-vg1-p2.diff`).
- **Forward-Only:** Pass. Two borderline items were patched.
- **Verification Gap:** two gaps, a fence-close test and a negative case for the existence rule. Also noted: the root-absolute regression and the rendered-text slugs.
- **Element-Free:** Incomplete on records. The AC3 pointer and the M6 timing clause were patched. D1-3 is scheduled.
- **Edge Case Hunter:** 13 items. ECH-1 escalated, 11 patched, 1 rejected.
- **Blind Hunter:** stalled on a shell command at 04:32 with no output. The coordinator stopped it, and pass 3's fresh Blind Hunter supersedes it.

**Coordinator ruling (a) on ECH-1, 26 November 2026.**
- One existence rule now covers all 27 checked documents, and the broken links are repaired line-locally.
- Quotations are skipped under one stated rule: straight-double-quoted spans and blockquote lines. The coordinator accepted it.
- My first escalation also misreported `owner-decisions-2026-11-22.md:71` as a merge artefact. The coordinator and I both confirmed that the target exists at `37a259c9`. The link lacked one `../`.

The implementation owner's repairs are in E27. They sit above E26 in this file because they were inserted there. E26 and this entry are coordinator entries.

**Test count.** `ArchitectureDocsConsistencyTests` changed from 4 tests to 20: 17 added and 1 removed. AC3 now expects 7423 with registration 821.

**Contract budget.** The contract reached 8,907 words, over the 8,000-word close budget. The contract now keeps a compact summary of the Review Triage Log for passes 5–8. The verbatim rows stay in git as the archive: `_bmad-output/implementation-artifacts/spec-doc-engine-status.md` at commit `d4b84b4e`, lines 418–468.

## E29 VG-1 re-review pass 3 repairs (implementation owner, 26 November 2026)

Repairs on top of the uncommitted E24, E25 and E27 work, over `d4b84b4e`, for the coordinator's pass-3 rulings. Files edited: `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs`, `_bmad-output/implementation-artifacts/deferred-work.md`, this run log, and the document lines below. The contract is the coordinator's and was not edited.

**Test file.**
1. Quotation skips deleted (ruling A, F-1a/F-1b): `RelativeLinks` no longer skips blockquote lines or removes straight-double-quoted spans; a blockquote prefix is stripped only so that a quoted reference definition is still read. The `Quotation` regex is gone and the `FindLinkProblems` doc comment now says links are judged wherever GitHub renders them, blockquotes and quoted text included; only front matter, code fences and code spans are not checked. `TheLinkCheckIgnoresQuotations` is inverted and renamed `TheLinkCheckJudgesLinksInBlockquotesAndQuotedText`: a broken link in a quoted span (line 1) and one in a blockquote (line 2) are both reported.
2. Tracked-tree existence ((a), Blind Hunter): `TrackedKinds(trackedFiles)` gives File for a tracked path, Directory for any proper prefix of one (and the root), Missing otherwise, by ordinal match. The real tree is `TrackedKinds(GitTrackedFiles())`, built once per test run (`Lazy`); `GitTrackedFiles` runs `git -C <root> ls-files -z` and throws `InvalidOperationException` with the command, exit code and stderr, or a "git must be on PATH" message, when git cannot run; nothing is skipped. `ReadRepositoryFile` reads only tracked files. The directory-listing seam of E27 (`FindEntry`, `ListRepositoryDirectory`) is deleted; `TheLinkCheckMatchesPathsCaseSensitively` now runs on `TrackedKinds`. New facts: `TheLinkCheckReportsALinkToAnUntrackedFile` (a readable local file outside the tracked list is reported) and `TheLinkCheckResolvesATrackedFileAndItsDirectory` (control).
3. Slug unescaping ((b)): outside code spans, a backslash escape yields its character literally (so `\_bmad layout` gives `_bmad-layout`), and HTML entities are decoded (`Save &amp; load` gives `save--load`); both cases are added to `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub`.
4. Front matter ((c)): new fact `TheLinkCheckSkipsOnlyAClosedFrontMatter`. In a closed front matter a link and a heading-like line are ignored (the body link to `#heading` on line 5 is reported because the heading does not exist), and the first body line is checked. A first `---` with no closing `---` hides nothing: as on GitHub, where it renders as an ordinary line (a thematic break), the link on line 2 is checked and reported. The `Scan` doc comment states this.

**Document lines changed** (before → after; deletion SHAs confirmed with `git log --diff-filter=D --format=%h`: `engine/PartArt.cs` `8f682f84`, `engine/PartAssistance.cs` `cf46eb99`, `parts/BallPart.cs` `8f682f84`, `parts/DominoPart.cs` `8f682f84`):
- `docs/planning/elements/readiness/batch-i.md:133–137` (now `:133–139`): the blockquoted EL-242 proposal (five `>` lines, three of text and two empty) → the same three text lines and two blank lines inside a `markdown` code fence, without the `>` markers. Wording unchanged.
- `batch-i.md:139` (now `:141`): the two quoted EL-072 texts → the same texts inside code spans, still within their double quotes. Wording unchanged.
- `batch-i.md:3`: unchanged, now checked; its two links resolve.
- `DESIGN.md:70`: "Imperative procedural C# mesh construction (`engine/PartArt.cs`) and hardcoded coordinate/collider tables in code are strictly deprecated and scheduled for complete deletion at `LEGACY-0`." → "Imperative procedural C# mesh construction (the former `engine/PartArt.cs`, removed at `8f682f84`) and hardcoded coordinate/collider tables in code are not permitted."
- `DESIGN.md:104`: "legacy procedural C# primitives (`PartArt.cs`)" → "legacy procedural C# primitives (`engine/PartArt.cs`, removed at `8f682f84`)"; the conversion rule is unchanged.
- `DESIGN.md:419`: "part geometry/materials (retired `engine/PartArt.cs`; procedural mesh generation deprecated in favor of declarative glTF 2.0 `.glb` assets; scheduled for deletion at `LEGACY-0`)" → "part geometry/materials (`engine/PartArt.cs`, removed at `8f682f84`; procedural mesh generation is deprecated in favor of declarative glTF 2.0 `.glb` assets)"; "assistance easing (retired `engine/PartAssistance.cs`)" → "assistance easing (`engine/PartAssistance.cs`, removed at `cf46eb99`)". No current owner of either was found.
- `docs/planning/invest/current-consumers.md:137`: "(`parts/BallPart.cs` is retained as artwork that reads the declared material of either kind)" → "(`parts/BallPart.cs` was retained as artwork that read the declared material of either kind; deleted at `8f682f84`)".
- `current-consumers.md:226`: "(the file remains artwork only)" → "(the file was then retained as artwork only; deleted at `8f682f84`)".
- No other line of the 27 documents failed once the skips were gone: before the `batch-i.md` edits the real-tree test reported only `batch-i.md:137` (CAT-049) and `:139` (the two placeholder `…` links and CAT-050) (`arch-docs-e29-pre.log`); after them, 23/0/0 (`arch-docs-e29-pre2.log`).

**deferred-work.md.** The VG-1 entry's owner and status cite E29, the tracked-tree rule and the new negatives (untracked file, blockquote and quoted-text links reported, front matter, slug escapes and entities).

**Test count.** `ArchitectureDocsConsistencyTests` has 23 tests. Against `d4b84b4e` (4 tests): 1 removed (`EveryRelativeLinkInTheAuthorityDocsResolves`) and **20 added**: `EveryRelativeLinkInTheCheckedDocsResolves`, `TheFragmentCheckReportsAMissingAnchorWithItsFileAndLine`, `TheFragmentCheckReportsAMissingSameFileAnchor`, `TheFragmentCheckResolvesASameFileHeading`, `TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading`, `TheFragmentCheckResolvesAnExplicitAnchor`, `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub`, `TheFragmentCheckIgnoresHeadingsInsideCodeFences`, `TheLinkCheckReportsALinkToAMissingFile`, `TheLinkCheckReportsALinkAboveTheRepositoryRoot`, `TheLinkCheckReportsALinkToAnUntrackedFile`, `TheLinkCheckResolvesATrackedFileAndItsDirectory`, `TheFenceScanReportsAnUnclosedFenceAndClosesOnlyOnAMatchingMarker`, `TheLinkCheckSkipsOnlyAClosedFrontMatter`, `TheLinkCheckIgnoresCodeSpans`, `TheLinkCheckJudgesLinksInBlockquotesAndQuotedText`, `TheFragmentCheckResolvesSetextAndBlockquoteHeadings`, `TheLinkCheckReadsTitledAngleReferenceAndHrefLinks`, `TheFragmentCheckReportsAFragmentOnANonMarkdownTarget`, `TheLinkCheckMatchesPathsCaseSensitively`. Unchanged in name: `AgentsLinksTheIsolationAuthority`, `NoAuthorityPresentsRetiredDesignAsCurrent`, `TheBanCheckRejectsAReinstatedPhrase`. Net: 4 → 23 (+20, −1); against E27's 20, +3 (`TheLinkCheckReportsALinkToAnUntrackedFile`, `TheLinkCheckResolvesATrackedFileAndItsDirectory`, `TheLinkCheckSkipsOnlyAClosedFrontMatter`) and one rename (`TheLinkCheckIgnoresQuotations` → `TheLinkCheckJudgesLinksInBlockquotesAndQuotedText`).

**Checks** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e29/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e29`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e29-a.log`, `capture-e29-b.log`, `capture-e29-c.log` (preceded by a clean incremental test-project build, `tests-build-e29.log`).
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e29.log`.
3. Registration check filter: **821/0/0**, unchanged; `registration-e29.log`.
4. `git diff --check d4b84b4e`: `diff-check-e29.log`.
5. `ArchitectureDocsConsistencyTests`, run last, after this entry: `arch-docs-e29-final.log`.

## E30 VG-1 re-review pass 4: document lines (B) and Markdig (C) (implementation owner, 26 November 2026)

Repairs on top of the uncommitted E24 to E29 work, over `d4b84b4e`, for the coordinator's pass-4 document findings (B) and its latent-parsing ruling (C). Files edited: `DESIGN.md`, `docs/planning/elements/readiness/batch-i.md`, `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs`, `CuriousContraptions.tests/CuriousContraptions.tests.csproj` and this run log. The contract is the coordinator's and was not edited.

**B. Document lines** (before → after; every claim checked in the repository first).
- Evidence: `git ls-files parts` lists nothing; `git log --diff-filter=D --format=%h -- 'parts/*Part.cs'` gives `8f682f84` (eleven files including `BallPart.cs` and `DominoPart.cs`), `a7fb5d05` (`PipePart.cs`) and `cf46eb99` (41 files); `engine/PartArt.cs` and `engine/gpu/WorkshopPartKind.cs` were deleted at `8f682f84`, `engine/PartAssistance.cs` at `cf46eb99`; `git grep WorkshopPartKind -- '*.cs'` finds nothing. `git ls-files '*.glb'` lists nothing; all 14 named elements have a tracked scene (`elements/Ball/ball.tscn` … `elements/Battery/battery.tscn`). `sprint-status.yaml:98` (6.5d), `:100` (6.5e) and `:102` (6.5f) are `in-progress`; `vertical-delivery.md:190` is the LEGACY-PARTART-PURGE (6.5f) row. `ui/Workshop.cs:171-176` builds the workbench with `WorkshopArtwork.Box` and `.Sphere`, which create `BoxMesh` and `SphereMesh` (`ui/WorkshopArtwork.cs:20-27`), and `engine/Workbench.cs:11-14` holds the workbench coordinates. `epics.md:538` and `:933` record the 18 October 2026 owner decision that the element's own `.tscn` scene, not `.glb`, is the authored format. `git log -S` finds both EL-242 edits in `named-elements.md` (EL-072 at `:678`, worded "belong to", and EL-242 at `:2120`) in `daba83d0`.
- `DESIGN.md:70`: "All 3D puzzle elements, fixtures, and tools are delivered in open-source, industry-standard **glTF 2.0 binary (`.glb`)** format. Imperative procedural C# mesh construction (the former `engine/PartArt.cs`, removed at `8f682f84`) and hardcoded coordinate/collider tables in code are not permitted." → "Every puzzle element is authored as its own Godot `.tscn` scene in its package, not as `.glb` (owner decision, 18 October 2026: `epics.md:538`, `:933`; no `.glb` file is tracked). Imperative procedural C# mesh construction and hardcoded coordinate/collider tables in code are not permitted: `engine/PartArt.cs` was removed at `8f682f84`, and the remaining procedural fixture art (the workbench's `BoxMesh` and `SphereMesh`, built through `ui/WorkshopArtwork.cs:20-27` from `ui/Workshop.cs:171-176`) and the workbench coordinate table (`engine/Workbench.cs:11-14`) are owned by LEGACY-PARTART-PURGE (Story 6.5f, in progress: `sprint-status.yaml:102`, `vertical-delivery.md:190`)." The section heading (glTF) is unchanged; no link targets it by slug (its explicit anchor is `technical-art-pipeline`).
- `DESIGN.md:104`: "… must be converted forward from legacy procedural C# primitives (`engine/PartArt.cs`, removed at `8f682f84`) and hardcoded shape tables to self-contained glTF 2.0 (`.glb`) assets in `parts/models/`." → "… are converted forward from the legacy procedural C# primitives (`engine/PartArt.cs`, removed at `8f682f84`) and hardcoded shape tables to the element's own `.tscn` scene in its package, `elements/<Name>/*.tscn` (owner decision of 18 October 2026, `epics.md:538`). Each of the 14 has its scene today (for example `elements/Ball/ball.tscn`); the conversion stories 6.5d and 6.5e stay in progress until their addenda close (`sprint-status.yaml:98`, `:100`)." The scenes exist, but the owning stories are open, so the line states both rather than completed history.
- `DESIGN.md:108`: "- Bespoke per-part C# scripts (`parts/*Part.cs`), procedural mesh generation in `engine/PartArt.cs`, hardcoded dimension resources, and closed part enums (`WorkshopPartKind`) must be completely deleted upon conversion. Strictly zero …" → "- No bespoke per-part C# scripts, procedural mesh generation, hardcoded dimension resources or closed part enums: the `parts/*Part.cs` scripts were deleted at `8f682f84`, `a7fb5d05` (`parts/PipePart.cs`) and `cf46eb99`, and `engine/PartArt.cs` and the closed `WorkshopPartKind` enum (`engine/gpu/WorkshopPartKind.cs`) at `8f682f84`. Strictly zero …" (the rest unchanged).
- `DESIGN.md:419`: "part geometry/materials (`engine/PartArt.cs`, removed at `8f682f84`; procedural mesh generation is deprecated in favor of declarative glTF 2.0 `.glb` assets)" → "part geometry/materials (`engine/PartArt.cs`, removed at `8f682f84`; each element is its own `.tscn` scene, and the remaining procedural fixture code is owned by Story 6.5f, as the technical art pipeline above states)". `assistance easing (… removed at cf46eb99)` unchanged.
- `batch-i.md:141` (now `:143`): "Until both edits are made, CAT-049 acceptance stays an open criterion of EL-072." → "Both edits were applied in `daba83d0` (`docs/planning/invest/named-elements.md:678` for EL-072, worded "the 45° angle and CAT-049 acceptance belong to EL-242", and `:2120` for EL-242), so CAT-049 acceptance is no longer an open criterion of EL-072."
- `batch-i.md:133` (new line, then a blank line, before the fence): "The links in the fenced entry below are written relative to `docs/planning/invest/named-elements.md`, where the entry goes, not to this file." The fenced text is unchanged.

**C. Markdig replaces the hand-written Markdown parsing.**
- Dependency: `Markdig` `1.6.0` (published 4 November 2026, the current stable on nuget.org), an exact `PackageReference` in `CuriousContraptions.tests.csproj` only, in its own item group with a comment (not in the xunit group, whose comment pins packages identical with `tools/Coverage.Tests`). Restored from `https://api.nuget.org/v3/index.json` (`restore-markdig-e30.log`: content hash `rJEpDfPM8wrfXUgkNejjg2bJN9bciy44uVyRwKUI+92CnSmbaLeJTLPYcCdvgM4RFsKrhs8d2H2hO78yZpMl3g==`). Package-list guards: none applies. `CSharpStaticGateTests` pins analyzers by package (`PackageAnalyzers`), compiled package sources, compiler switches and the package-source config, but not a project's package list; Markdig ships no analyzer or compiled source, and the static-gate tests pass on the new binlogs. `tools/BundleElements.Tests/PackageReferenceTests.cs` covers element packages only, `tools/E2eRunner.Tests/FastSelectionTests.cs` tests test selection, and `tools/anvil/VerifyPolicy.cs` has no package check. No allow-list was edited.
- Parsing: one pipeline, `UseYamlFrontMatter`, `UsePipeTables`, `UseAutoLinks`, `UseFootnotes`, `UseEmphasisExtras`, `UseTaskLists` and `UseAutoIdentifiers(AutoIdentifierOptions.GitHub)` (the GitHub-relevant subset of `UseAdvancedExtensions`, which would also register default-mode identifiers). Links are `LinkInline` nodes (images included; reference-style uses skipped, checked at their definition) and `LinkReferenceDefinition` nodes other than the heading and footnote definitions Markdig synthesises; lines are `Line + 1`. HTML `href`, `id` and `name` are read from `<a>` tags in `HtmlInline` tags and `HtmlBlock` lines with one attribute pattern. Anchors are `HeadingBlock.GetAttributes().Id` plus HTML `id` and `name`. The hand-written fence, front-matter, code-span, emphasis, escape, entity, setext, blockquote and slug code and its regexes are deleted.
- Unclosed fence: kept as the typed `UnclosedFence` report, read from Markdig as a `FencedCodeBlock` with `ClosingFencedCharCount == 0`. CommonMark runs such a fence to the end of the document, so every later link silently leaves the rule; reporting it keeps the rule complete. The real tree has none.
- Item 14: a destination is external only for `http`, `https` or `mailto`; anything else, including `name:digits`, is a relative path and is checked. `TODO.md` has no such destination today.
- Item 16: a fragment on a directory target or of GitHub line form (`L10`, `L10-L20`, optionally with columns) is reported as `UnsupportedFragment`; the class summary documents the rule. Neither occurs in the 27 documents.
- Item 18: the tracked-tree control now also links `su`, a non-boundary prefix of tracked `docs/sub/b.md`, which is reported missing.
- Item 19: `PresentKinds` counts a tracked file that is not present as missing, so a tracked file deleted from the worktree is a `MissingTarget` (the real tree checks `File.Exists`; in memory, presence in the document set).
- Item 20: `GitTrackedFiles` reads stdout and stderr with `ReadToEndAsync` concurrently, waits at most 60 s, and on timeout kills the process tree and throws `TimeoutException` naming the command.
- Tests rewritten or removed with the hand parsers: `TheFenceScanReportsAnUnclosedFenceAndClosesOnlyOnAMatchingMarker` (fence-marker parsing; replaced by the outcome test `TheLinkCheckReportsAnUnclosedFence`); `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub` now runs its five cases (punctuation with a code span, a link, emphasis, an escape, an entity) through Markdig's anchors; `TheFragmentCheckReportsAFragmentOnANonMarkdownTarget` keeps the `.sh` case, its directory case moving to `TheLinkCheckReportsUnsupportedFragments`; `TheLinkCheckResolvesATrackedFileAndItsDirectory` is renamed `TheLinkCheckResolvesATrackedFileAndDirectoryButNotAPartialName` (item 18). `TheFragmentCheckResolvesAnExplicitAnchor` adds an HTML `name` anchor. `TheLinkCheckReadsTitledAngleReferenceAndHrefLinks` separates its four forms by blank lines, since in CommonMark a reference definition cannot interrupt a paragraph.

**Test count.** `ArchitectureDocsConsistencyTests` has 26 tests. Against `d4b84b4e` (4 tests): 1 removed (`EveryRelativeLinkInTheAuthorityDocsResolves`) and **23 added**: `EveryRelativeLinkInTheCheckedDocsResolves`, `TheFragmentCheckReportsAMissingAnchorWithItsFileAndLine`, `TheFragmentCheckReportsAMissingSameFileAnchor`, `TheFragmentCheckResolvesASameFileHeading`, `TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading`, `TheFragmentCheckResolvesAnExplicitAnchor`, `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub`, `TheFragmentCheckIgnoresHeadingsInsideCodeFences`, `TheLinkCheckReportsALinkToAMissingFile`, `TheLinkCheckReportsALinkAboveTheRepositoryRoot`, `TheLinkCheckReportsALinkToAnUntrackedFile`, `TheLinkCheckReportsATrackedFileDeletedFromTheWorktree`, `TheLinkCheckResolvesATrackedFileAndDirectoryButNotAPartialName`, `TheLinkCheckReportsAnUnclosedFence`, `TheLinkCheckSkipsOnlyAClosedFrontMatter`, `TheLinkCheckIgnoresCodeSpans`, `TheLinkCheckJudgesLinksInBlockquotesAndQuotedText`, `TheFragmentCheckResolvesSetextAndBlockquoteHeadings`, `TheLinkCheckReadsTitledAngleReferenceAndHrefLinks`, `TheFragmentCheckReportsAFragmentOnANonMarkdownTarget`, `TheLinkCheckReportsUnsupportedFragments`, `TheLinkCheckTreatsOnlyHttpHttpsAndMailtoAsExternal`, `TheLinkCheckMatchesPathsCaseSensitively`. Unchanged in name: `AgentsLinksTheIsolationAuthority`, `NoAuthorityPresentsRetiredDesignAsCurrent`, `TheBanCheckRejectsAReinstatedPhrase`. Net: 4 → 26 (+23, −1). Against E29's 23: +3 (`TheLinkCheckReportsATrackedFileDeletedFromTheWorktree`, `TheLinkCheckReportsUnsupportedFragments`, `TheLinkCheckTreatsOnlyHttpHttpsAndMailtoAsExternal`), one replaced (`TheFenceScanReportsAnUnclosedFenceAndClosesOnlyOnAMatchingMarker` → `TheLinkCheckReportsAnUnclosedFence`) and one renamed (`TheLinkCheckResolvesATrackedFileAndItsDirectory` → `TheLinkCheckResolvesATrackedFileAndDirectoryButNotAPartialName`). A first run on the new parser, before these records, was 26/0/0 (`arch-docs-e30-pre.log`).

**Checks** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e30/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e30`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e30-a.log`, `capture-e30-b.log`, `capture-e30-c.log` (preceded by a clean incremental test-project build, `tests-build-e30.log`).
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e30.log`.
3. Registration check filter: **821/0/0**, unchanged (it includes `CSharpStaticGateTests`); `registration-e30.log`.
4. `git diff --check d4b84b4e`: `diff-check-e30.log`.
5. `ArchitectureDocsConsistencyTests`, run last, after this entry: `arch-docs-e30-final.log`.

## E31 Coordinator: VG-1 re-review passes 3 and 4, rulings, and contract budget (26 November 2026)

**Pass 3.** Verification Gap, Forward-Only, Element-Free and Blind Hunter reported. The Edge Case Hunter stalled after 05:13, and the coordinator stopped it.
- The Forward-Only finding F-1a/F-1b (the quotation skip was a second rule) reopened quoting, so I stopped and escalated.
- The coordinator's rulings:
  - (A) withdraw the quote rule, delete both skips and fence the `batch-i.md` proposals;
  - patch tracked-tree existence, slug unescaping and the front-matter test;
  - fix `DESIGN.md:70`, `:104`, `:419` and `current-consumers.md:137`, `:226`;
  - cap convergence at pass 4.
- The implementation owner's repairs are in E29.

**Pass 4.** All five layers reported.
- Forward-Only blocked on `DESIGN.md:108` and `:419`. Element-Free and Blind Hunter found further `DESIGN.md` and `batch-i.md` lines.
- Twenty latent checker-parsing items, none occurring in the 27 documents, went to the coordinator as one list, as the cap requires.
- The coordinator's ruling: deferral is not available for code the repair added, and patching items one by one does not converge. Replace the hand-written parsing with Markdig at a pinned version in the test project only; add the scheme allow-list and the unsupported-fragment report; patch items 18–20; patch every document line.
- The implementation owner's repairs are in E30.

**Records.** I updated AC3 (7429), its table row, the M6 row and the Spec Change Log, and the `deferred-work.md` VG-1 entry now cites E30 and Markdig.

**Contract budget.** The contract reached 8,858 words, over the 8,000-word close budget. The VG-1 re-review pass 1–4 Review Triage Log entries move below verbatim, and the contract keeps a compact summary.

### Verbatim: Review Triage Log, VG-1 repair re-review passes 1–4, moved from the contract

#### 2026-11-26 — VG-1 repair re-review, pass 1 (Verification Gap, Forward-Only, Element-Free; repair diff from `d4b84b4e`; detail in run log E26)
- **FO-1 (high, patch, fixed E25).** There were two link-extraction and resolution rules: the existing existence test and the new checker. Now one enumerator serves both.
- **FO-2 (high, patch, fixed E25).** `<a name>` was accepted as a second anchor form. Now only `<a id>` is accepted.
- **VG-a (patch, fixed E25).** No test covered a fragment link to a missing file or one above the repository root. Two in-memory negatives were added.
- **D1-1 (patch, fixed).** AC3 contradicted the repair. It is amended (Spec Change Log).
- **D1-2 (patch, fixed).** The M6 row cited a run taken before the last record edit. It now cites `arch-docs-e25-final.log`.
- **D1-3 (patch).** The Auto Run Result, Status line and `TODO.md` were stale. They are updated with the final round on the tip.
- **D1-4 (patch, fixed).** This entry now exists.
- **Planning-line re-check (Element-Free auditor, duty 2).** Items (a)–(c) pass: Design Notes 8 carries `epics.md:4466`, it is consistent with M4, AC2, Verification and Concurrency, and `epics.md:4466` is unedited by this slice. Item (d) failed, because the corrected verdict text said "returned Fail", which E9 does not record. The text is reworded to what E9 supports, and run log E26 records the correction.
- **Not filed:** setext headings and mixed fence characters. The Verification Gap reviewer checked that none occur in the 27 checked documents; a divergence would fail loudly.
- **Re-review:** pass 2, by fresh reviewers, on the pass-1 repairs.

#### 2026-11-26 — VG-1 repair re-review, pass 2 (Verification Gap, Forward-Only, Element-Free and Edge Case Hunter reported; Blind Hunter was launched and stalled with no output; diff from `d4b84b4e`; detail in run log E27)

**Blind Hunter.** It stalled on a shell command at 04:32 and produced no output. The coordinator stopped it. Pass 3's fresh Blind Hunter on the repaired diff supersedes it.

**Forward-Only.** Pass. FO-1 and FO-2 hold. Two borderline items are patched:
- `AgentsLinksTheIsolationAuthority` now uses the one link enumerator;
- the two file readers are collapsed into one.

**Patches** (all fixed in E27 unless stated):
- **Fences** (VG and ECH):
  - a link after a closed fence is still checked;
  - a fence closes only on a matching marker;
  - a fence indented four or more spaces is not a fence;
  - an unclosed fence is reported.
- **Root-absolute targets** (VG and ECH): they resolve from the repository root. The FO-1 refactor had introduced this regression.
- **Existence rule** (VG): it has a negative case.
- **ECH-1** (coordinator ruling (a)): one existence rule now covers all 27 documents, and the broken links are repaired (Spec Change Log).
- **Code spans** (ECH): they no longer yield links or anchors.
- **Slugs** (ECH): rendered text is used. Link text replaces the link, and emphasis markers are removed.
- **Duplicate-slug collision** (ECH): fixed with github-slugger's increment.
- **Headings** (ECH): setext headings and headings inside blockquotes are recognised.
- **Link forms** (ECH): titled links, angle-bracket links, reference definitions and `href` links are recognised.
- **Non-Markdown fragment targets** (ECH): they are reported.
- **Case-exact path match** (ECH).
- **Element-Free record items**:
  - the M6 row names all its tests;
  - AC3 is measured on the repaired tip;
  - `deferred-work.md` cites E24, E25 and E27;
  - the Planning verdict states the verdict.

**Rejected** (ECH): accepting `<a name>`, single-quoted anchors or attribute-first anchors. FO-2 fixed one current authoring form, `<a id="…">`, and any other form already fails loudly as a missing anchor.

**Scheduled:** D1-3 runs before the final round on the tip, because it edits checked documents (pass-3 Element-Free finding 1). It covers:
- frontmatter `status` and `review_loop_iteration`;
- the Status line;
- the Auto Run Result: the deferral count, VG-1, and the E23 figures;
- `TODO.md`.

#### 2026-11-26 — VG-1 repair re-review, pass 3 (Verification Gap, Forward-Only, Element-Free and Blind Hunter reported; Edge Case Hunter stalled after 05:13 and the coordinator stopped it; diff from `d4b84b4e`; detail in run log E29)

**Blocking findings, all patched in E29:**
- **F-1a, F-1b (Forward-Only, high).** The quotation and blockquote skip was a second rule. It hid four broken rendered links in `batch-i.md:137` and `:139` and handled `href` links and anchors inconsistently. Verification Gap and Blind Hunter agreed: the skip also hid `batch-i.md:3`, the document's own note. The coordinator withdrew the rule, so the skips are deleted and the proposal text becomes code.

**Repair-affected, all patched in E29 by coordinator ruling:**
- **Blind Hunter:**
  - existence was checked against the working tree, not the tracked files;
  - slugs did not handle backslash escapes or HTML entities;
  - the front-matter skip had no test.
- **Document text (Element-Free, Verification Gap and Blind Hunter):**
  - `DESIGN.md:419` contradicted itself, and `:70` and `:104` still described `engine/PartArt.cs` as present;
  - `current-consumers.md:137` and `:226` were in the present tense.

**Records (Element-Free), fixed:**
- D1-3 now runs before the final round;
- a Spec Change Log entry for AC3 (7423);
- the pass-2 header names the stalled Blind Hunter.

**Pre-existing (Element-Free), patched by the coordinator:**
- The compacted pass-5 count now gives the actual row tally.
- The restitution deferral now leads with its pass-6 ID, VG6-1b.

**Verification Gap:** the 17 tests are each discriminating, and the repaired links hide nothing that still exists.

#### 2026-11-26 — VG-1 repair re-review, pass 4 (all five layers; diff from `d4b84b4e`; detail in run log E30)
- **Verification Gap:** no gap. **Edge Case Hunter:** no claim falsified.
- **Forward-Only (high, patched in E30):**
  - `DESIGN.md:108` still described the deleted `parts/*Part.cs`, `engine/PartArt.cs` and `WorkshopPartKind` as present.
  - `DESIGN.md:419` called procedural meshes "deprecated", while `:70` said "not permitted".
- **Element-Free and Blind Hunter (patched in E30):**
  - `DESIGN.md:70` had dropped the open owner, 6.5f.
  - `DESIGN.md:70`, `:104` and `:419` still named `.glb`, against the owner's `.tscn` amendment.
  - `DESIGN.md:104` placed an obligation on a deleted file.
  - `batch-i.md:141` was stale (both edits were applied at `daba83d0`).
  - `batch-i.md:134` needed a note saying which file its paths are relative to.
  - The M6 row now names the `AgentsLinksTheIsolationAuthority` rewrite.
- **Twenty latent checker-parsing items, none occurring in the 27 documents.** The coordinator ruled them out of deferral, since they are in code the repair added. Rather than patch them one by one, the checker's hand-written Markdown parsing is replaced by Markdig (Spec Change Log). That retires items 1–13, 15 and 17 at the source. Item 14 becomes the scheme allow-list, item 16 the unsupported-fragment report, and items 18–20 are patched.
- **Re-review:** pass 5, by fresh reviewers, on the changed lines.

## E32 VG-1 re-review pass 5: document lines and the ruled test-file items (implementation owner, 26 November 2026)

Repairs on top of the uncommitted E24 to E30 work, over `d4b84b4e`. Files edited: `DESIGN.md`, `docs/planning/elements/readiness/batch-i.md`, `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs`, `CuriousContraptions.tests/CuriousContraptions.tests.csproj` and this run log. The contract and `deferred-work.md` are the coordinator's and were not edited.

**Correction to E29 and E30 (append-only).** They said `engine/PartArt.cs` was "removed" or "deleted" at `8f682f84`. `git log --diff-filter=D` reported it because it does not follow renames; `git show --name-status 8f682f84` records `R091 engine/PartArt.cs → ui/WorkshopArtwork.cs`, with `engine/PartArt.cs.uid` deleted and a new `ui/WorkshopArtwork.cs.uid` added. The file was renamed, and its procedural builders ship as `ui/WorkshopArtwork.cs`. The other deletions cited there (`engine/gpu/WorkshopPartKind.cs` and every `parts/*Part.cs`) are `D` entries and stand.

**Verified.** `ui/WorkshopArtwork.cs:20-30` holds the `Box`, `Sphere`, `Cylinder` and torus `Ring` builders. `git grep` over `*.cs` finds their callers at `ui/Workshop.cs:171-176` (workbench), `ui/WorkshopConnections.cs:275`, `:289` (wire segments and knots), `engine/MachinePart.cs:139` (selection ring), `ui/WorkshopGuidance.cs:285` (snap ring) and `ui/RotationGizmo.cs:50`, plus direct `CylinderMesh`/`BoxMesh` construction in `ui/RotationGizmo.cs:54`, `:60`, `:105`, `:107` and an `ImmediateMesh` at `ui/PlacementShadows.cs:77`; no element package references `WorkshopArtwork`. `sprint-status.yaml:102` has 6.5f `in-progress` (`epics.md:993` still says Backlog; the sprint status is the authority cited). `git grep` finds no link to the old heading slugs of `DESIGN.md:68` or `:102` (only the explicit `technical-art-pipeline` anchor). Element scenes use `StandardMaterial3D` resources (for example `elements/Ball/ball.tscn`), and 36 element scenes carry `Loc_Port_` nodes. `named-elements.md:2119-2122` has `<a id="element-242">`, owner S361 and `sequence-task-800`, added at `daba83d0`; `:673` has `<a id="element-072">`.

**Document lines** (before → after):
- `DESIGN.md:68` heading: "Technical art pipeline — glTF 2.0 (.glb) standards" → "Technical art pipeline — element `.tscn` scene standards"; the `technical-art-pipeline` anchor on `:67` is kept.
- `DESIGN.md:70`: the E30 text ("… Imperative procedural C# mesh construction and hardcoded coordinate/collider tables in code are not permitted: `engine/PartArt.cs` was removed at `8f682f84`, and the remaining procedural fixture art (the workbench's `BoxMesh` and `SphereMesh`, built through `ui/WorkshopArtwork.cs:20-27` from `ui/Workshop.cs:171-176`) and the workbench coordinate table … are owned by LEGACY-PARTART-PURGE …") → the `.tscn` sentence unchanged, then "For puzzle elements, fixtures and tools alike, imperative procedural C# mesh construction and hardcoded coordinate/collider tables in code are not permitted. The code that still does so is pending removal under LEGACY-PARTART-PURGE (Story 6.5f, in progress at `sprint-status.yaml:102`; its roadmap row, `vertical-delivery.md:190`, covers all procedural mesh building routines in C#): the procedural builder `ui/WorkshopArtwork.cs` (renamed from `engine/PartArt.cs` at `8f682f84`; its `Box`, `Sphere`, `Cylinder` and torus `Ring` builders at `:20-30`), as used for the workbench (`ui/Workshop.cs:171-176`), wire segments and knots (`ui/WorkshopConnections.cs:275`, `:289`) and the selection and snap rings (`engine/MachinePart.cs:139`, `ui/WorkshopGuidance.cs:285`); the rotation gizmo's own `CylinderMesh` and `BoxMesh` handles (`ui/RotationGizmo.cs`); the placement-shadow `ImmediateMesh` (`ui/PlacementShadows.cs:77`); and the workbench coordinate table (`engine/Workbench.cs:11-14`). No element package uses `ui/WorkshopArtwork.cs`."
- `DESIGN.md:76`: "Standard export orientation: glTF 2.0 standard (+Y up, +Z forward / -Z forward standard glTF transform)." → "Orientation in the element's `.tscn` scene is Godot's: +Y up, -Z forward. Art made in Blender (+Z up) is converted to that orientation when Godot imports it, before it is placed in the scene."
- `DESIGN.md:79`: "Standard glTF PBR metallic-roughness workflow (principled BSDF in Blender): …" → "PBR metallic-roughness materials, as the scene's `StandardMaterial3D` resources (Principled BSDF for art made in Blender before import): …" (values unchanged).
- `DESIGN.md:82`: "co-located in the same `.glb` container alongside visual meshes" → "co-located in the element's `.tscn` scene alongside visual meshes".
- `DESIGN.md:99`: "child node branches in the `.glb` scene" → "child node branches in the element's `.tscn` scene".
- `DESIGN.md:102` heading: "Conversion of Existing Implementations to glTF Standards (Zero Backwards Compatibility)" → "Conversion of Existing Implementations to Element Scenes (Zero Backwards Compatibility)".
- `DESIGN.md:104`: "(`engine/PartArt.cs`, removed at `8f682f84`)" → "(`engine/PartArt.cs`, renamed to `ui/WorkshopArtwork.cs` at `8f682f84` and no longer used by any element package)".
- `DESIGN.md:108`: "and `engine/PartArt.cs` and the closed `WorkshopPartKind` enum (`engine/gpu/WorkshopPartKind.cs`) at `8f682f84`." → "and the closed `WorkshopPartKind` enum (`engine/gpu/WorkshopPartKind.cs`) at `8f682f84`; `engine/PartArt.cs` was renamed to `ui/WorkshopArtwork.cs` at `8f682f84`, and its procedural builders are pending removal under Story 6.5f (technical art pipeline above)."
- `DESIGN.md:321`: "named locator nodes (`Loc_Port_<Id>`) embedded in the Blender/glTF rendering mesh hierarchy" → "… embedded in the element's `.tscn` scene hierarchy" (found by the `.glb|glTF` sweep).
- `DESIGN.md:419`: "part geometry/materials (`engine/PartArt.cs`, removed at `8f682f84`; each element is its own `.tscn` scene, and the remaining procedural fixture code is owned by Story 6.5f, …)" → "part geometry/materials (each element's own `.tscn` scene; the procedural builder `ui/WorkshopArtwork.cs`, renamed from `engine/PartArt.cs` at `8f682f84`, and the other procedural builders are pending removal under Story 6.5f, as the technical art pipeline above states)".
- `rg -n '\.glb|glTF' DESIGN.md` now gives only `:70` ("not as `.glb`", "no `.glb` file is tracked") and `:93` ("the Khronos `KHR_physics_rigid_bodies` glTF extension is a planned capability, not admitted today"), both true.
- `batch-i.md:131`: "The number, S owner, requirement anchor and sequence-task are allocated by the cross-batch pass (EL-242 if unclaimed). Proposed entry, in the form of the EL-072 entry:" → "Applied at `daba83d0`: the cross-batch pass ledgered EL-242 (a real link to `../../invest/named-elements.md#element-242`) with owner S361, requirement anchor `element-242` and `sequence-task-800`. The entry as proposed, in the form of the EL-072 entry:".
- `batch-i.md:136`, inside the fence (the one allowed change there): the "existing record" link destination `../../requirements.md#todo-199` → `../requirements.md#todo-199`, the applied form at `named-elements.md:2120`, so every link in the fence is relative to `named-elements.md` as the note on `:133` says.
- `batch-i.md:143`: "Replace "`…CAT-049…`" with "`…`" Both edits were applied in `daba83d0` (`docs/planning/invest/named-elements.md:678` …, and `:2120` for EL-242), so …" → "At `daba83d0`, "`Also retains …CAT-049…, …CAT-050…`" in the EL-072 entry (a real link to `../../invest/named-elements.md#element-072`) was replaced by the form "`…`", worded there "the 45° angle and CAT-049 acceptance belong to EL-242", and EL-242 (a real link to `../../invest/named-elements.md#element-242`) was ledgered, so CAT-049 acceptance is no longer an open criterion of EL-072."

**Test file and csproj (the coordinator's ruled items).**
1. `<a id>` only: the HTML attribute pattern reads whole `href` and `id` names; `name` is withdrawn from the summaries, and the former `named-point` positive is now a negative in the new fact `TheFragmentCheckIgnoresNameAndDataIdAttributes` (an `<a name>` and a `data-id` define nothing).
2. Autolinks: non-email `AutolinkInline` nodes are links, so `<ftp://…>` and `<TODO.md:13>` go through the scheme allow-list; added to `TheLinkCheckTreatsOnlyHttpHttpsAndMailtoAsExternal`.
3. HTML: attributes are read only from `HtmlInline` and from whole `HtmlBlock` text (not per line), excluding `HtmlBlockType.Comment` blocks and blanking `<!-- … -->` comments in place so line numbers hold; values double- or single-quoted, decoded with `WebUtility.HtmlDecode`; an unquoted value, or `href`/`id` on an element other than `<a>`, is the new `LinkProblemKind.UnsupportedHtml`. Tests: `TheLinkCheckReadsTitledAngleReferenceAndHrefLinks` gains a multi-line single-quoted `<a href>` inside a `<div>` HTML block, reported at the attribute's line 11; `TheFragmentCheckResolvesAnExplicitAnchor` gains a single-quoted `id`; new fact `TheLinkCheckReportsUnsupportedHtmlAnchorForms` (unquoted `href`, `id` on `<span>`); `TheLinkCheckIgnoresCodeSpans` is renamed `TheLinkCheckIgnoresCodeSpansAndHtmlComments` and gains a comment block and an inline comment holding links.
4. Fences: `UnclosedFence` only when the fenced block's parent is the `MarkdownDocument`; new fact `TheLinkCheckDoesNotReportAFenceClosedByItsContainer` (a fence in a blockquote and in a list item, each ending with its container).
5. Images: `TheLinkCheckReportsALinkToAMissingFile` gains an image link to `gone.png`, reported on line 2.
6. `Message` has a named arm for every kind and throws `ArgumentOutOfRangeException` otherwise.
7. Wording: `LinkProblem.Detail` is "the parsed destination"; line and directory fragments are "outside the supported subset", a policy.
8. csproj: `Version="[1.6.0]"`, the exact-version range, with its comment saying so.
9. The class summary states the supported subset (CommonMark plus pipe tables, footnotes, YAML front matter and GitHub heading identifiers via Markdig 1.6.0; links from inline, reference, autolink, image and HTML `<a href>`; anchors from headings and `<a id>`; external schemes `http`, `https`, `mailto` only), the typed reports for anything outside it, and the two named limits (Markdig's GitHub slugs can differ for combining marks, `\p{No}` characters, image alt text in headings and empty slugs; submodules and symbolic links are not modelled, and none exist).

**Test count.** `ArchitectureDocsConsistencyTests` has 29 tests. Against `d4b84b4e` (4 tests): 1 removed (`EveryRelativeLinkInTheAuthorityDocsResolves`) and **26 added**: `EveryRelativeLinkInTheCheckedDocsResolves`, `TheFragmentCheckReportsAMissingAnchorWithItsFileAndLine`, `TheFragmentCheckReportsAMissingSameFileAnchor`, `TheFragmentCheckResolvesASameFileHeading`, `TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading`, `TheFragmentCheckResolvesAnExplicitAnchor`, `TheFragmentCheckIgnoresNameAndDataIdAttributes`, `TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub`, `TheFragmentCheckIgnoresHeadingsInsideCodeFences`, `TheLinkCheckReportsALinkToAMissingFile`, `TheLinkCheckReportsALinkAboveTheRepositoryRoot`, `TheLinkCheckReportsALinkToAnUntrackedFile`, `TheLinkCheckReportsATrackedFileDeletedFromTheWorktree`, `TheLinkCheckResolvesATrackedFileAndDirectoryButNotAPartialName`, `TheLinkCheckReportsAnUnclosedFence`, `TheLinkCheckDoesNotReportAFenceClosedByItsContainer`, `TheLinkCheckSkipsOnlyAClosedFrontMatter`, `TheLinkCheckIgnoresCodeSpansAndHtmlComments`, `TheLinkCheckJudgesLinksInBlockquotesAndQuotedText`, `TheFragmentCheckResolvesSetextAndBlockquoteHeadings`, `TheLinkCheckReadsTitledAngleReferenceAndHrefLinks`, `TheLinkCheckReportsUnsupportedHtmlAnchorForms`, `TheFragmentCheckReportsAFragmentOnANonMarkdownTarget`, `TheLinkCheckReportsUnsupportedFragments`, `TheLinkCheckTreatsOnlyHttpHttpsAndMailtoAsExternal`, `TheLinkCheckMatchesPathsCaseSensitively`. Unchanged in name: `AgentsLinksTheIsolationAuthority`, `NoAuthorityPresentsRetiredDesignAsCurrent`, `TheBanCheckRejectsAReinstatedPhrase`. Net: 4 → 29 (+26, −1). Against E30's 26: +3 (`TheFragmentCheckIgnoresNameAndDataIdAttributes`, `TheLinkCheckReportsUnsupportedHtmlAnchorForms`, `TheLinkCheckDoesNotReportAFenceClosedByItsContainer`) and one rename (`TheLinkCheckIgnoresCodeSpans` → `TheLinkCheckIgnoresCodeSpansAndHtmlComments`). A first run before these records was 29/0/0 (`arch-docs-e32-pre.log`).

**Checks** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e32/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e32`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e32-a.log`, `capture-e32-b.log`, `capture-e32-c.log` (preceded by a clean test-project build with restore, `tests-build-e32.log`).
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e32.log`.
3. Registration check filter: **821/0/0**, unchanged; `registration-e32.log`.
4. `git diff --check d4b84b4e`: `diff-check-e32.log`.
5. `ArchitectureDocsConsistencyTests`, run last, after this entry: `arch-docs-e32-final.log`.

## E33 VG-1 re-review pass 6 repairs and the 6.5c–6.5f roadmap rows (implementation owner, 26 November 2026)

Repairs on top of the uncommitted E24 to E32 work, over `d4b84b4e`: the pass-6 present-day defects, the stated limits, and the coordinator's ruling (i) on `docs/planning/invest/vertical-delivery.md:187-190`. Files edited: `DESIGN.md`, `docs/planning/elements/readiness/batch-i.md`, `docs/planning/invest/vertical-delivery.md`, `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs` and this run log. The contract, `deferred-work.md` and `docs/planning/invest/decisions.md` were not edited.

**Verified.** `ui/RotationGizmo.cs:47` calls `WorkshopArtwork.Ring` (the gizmo rings) and `:50` `WorkshopArtwork.Sphere` (the handles). `WorkshopArtwork.Line` is defined at `ui/WorkshopArtwork.cs:32-38`, and `git grep` over `*.cs` finds no caller (the only other `Line(` is `tools/E2eRunner/RunSummary.cs:41`, an unrelated method). No element scene has a `Collision` node: of the `col_*` `CollisionShape3D` nodes in `elements/*/*.tscn`, 229 have `parent="."` and the rest sit under `Visual/…` branches (for example `elements/Clutch/clutch.tscn:190`, `parent="Visual/InputShaft"`); `elements/Ball/ball.tscn:34` is `col_0_0` under the root; the bake recognises proxies by the `col_` name prefix (`tools/BundleElements/SceneGeometryExtractor.cs:28`), and no C# reads a `Collision/` path. The numbered item on `DESIGN.md:81` is bold text, not a heading, so it has no slug. The repository shows no element-facing convention, so the orientation line states only +Y up. `named-elements.md:2122` credits the EL-242 ledger entry to coordinator decision 27, and owner S361 and `sequence-task-800` to coordinator decision 24l.

**Document lines** (before → after):
- `DESIGN.md:70`, the builder clause: "its `Box`, `Sphere`, `Cylinder` and torus `Ring` builders at `:20-30`), as used for the workbench …, wire segments and knots … and the selection and snap rings …;" → "its `Box`, `Sphere`, `Cylinder`, torus `Ring` and `Line` builders at `:20-38`, of which `Line` has no caller), as used for the workbench (`ui/Workshop.cs:171-176`), wire segments and knots (`ui/WorkshopConnections.cs:275`, `:289`), the selection and snap rings (`engine/MachinePart.cs:139`, `ui/WorkshopGuidance.cs:285`) and the rotation gizmo's rings and sphere handles (`ui/RotationGizmo.cs:47`, `:50`);". The rest of the line is unchanged.
- `DESIGN.md:76`: "Orientation in the element's `.tscn` scene is Godot's: +Y up, -Z forward. Art made in Blender (+Z up) is converted to that orientation when Godot imports it, before it is placed in the scene." → "Element scenes use Godot's axes, +Y up." (the forward direction and the import-conversion claim are dropped).
- `DESIGN.md:81-82`: "3. **Co-Located Collision Meshes (`Collision/` Hierarchy):**" / "Simplified proxy geometry co-located in the element's `.tscn` scene alongside visual meshes." → "3. **Co-Located Collision Proxies (`col_*` nodes):**" / "… alongside visual meshes: `CollisionShape3D` nodes named `col_*`, each a direct child of the scene root (for example `elements/Ball/ball.tscn:34`) or a child of the `Visual/` branch whose transform it shares (for example `elements/Clutch/clutch.tscn:190`); there is no `Collision/` node."
- `batch-i.md:131`: "Applied at `daba83d0`: the cross-batch pass ledgered EL-242 (link) with owner S361, requirement anchor `element-242` and `sequence-task-800`." → "Applied at `daba83d0`: EL-242 (link) was ledgered by coordinator decision 27, with owner S361 and `sequence-task-800` by coordinator decision 24l and requirement anchor `element-242`."
- `vertical-delivery.md:187` (6.5c deliverable): "from instantiated `.glb` scene hierarchy" → "from instantiated `.tscn` scene hierarchy".
- `vertical-delivery.md:188` (6.5d deliverable): "to `.glb` + generic `MachinePart`" → "to `.tscn` + generic `MachinePart`".
- `vertical-delivery.md:189` (6.5e deliverable): "to `.glb` with co-located `col_*` proxies" → "to `.tscn` with co-located `col_*` proxies".
- The three rows' titles still say "glTF" ("glTF asset-driven geometry", "conversion to glTF"); the ruling covered deliverable format words, so they are unchanged and are listed here for the coordinator.
- `vertical-delivery.md:190` (6.5f): deliverable "Complete deletion of `engine/PartArt.cs`, `engine/gpu/WorkshopPartKind.cs`, and all procedural mesh building routines in C#" → "Complete deletion of all procedural mesh building routines in C#: `ui/WorkshopArtwork.cs` (renamed from `engine/PartArt.cs` at `8f682f84`) and the other builders listed in `DESIGN.md:70` (`engine/gpu/WorkshopPartKind.cs` was already deleted at `8f682f84`)"; legacy column "Procedural mesh generation in `PartArt.cs`, `WorkshopPartKind.cs`, hardcoded shape tables" → "Procedural mesh generation in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`) and the other C# builders, hardcoded shape tables"; acceptance "Reviewer grep confirms 0 occurrences of `PartArt` or `WorkshopPartKind`; clean build; …" → "0 procedural mesh builders in C#; clean build; …" (the rest unchanged). `WorkshopPartKind` stays only in the title and as the note that its file is already deleted; it is dropped from the deliverable target and the acceptance, since nothing of it remains.

**Test file** (records and two extended facts; no new facts):
- Class summary: "Anything outside the subset is reported as a typed problem" → "… is reported as a typed problem or named below as a limit"; the subset now names `UsePipeTables`, `UseFootnotes`, `UseYamlFrontMatter`, `UseAutoIdentifiers`, `UseAutoLinks`, `UseEmphasisExtras` and `UseTaskLists`; the named limits keep the GitHub-slug differences and submodules and symbolic links, and add the nine ruled limits (markup after a comment block's closing marker on the same line; unclosed raw HTML or comment blocks; attribute-like text or a `>` inside a quoted value; CDATA, processing instructions and declarations; a tag with two offending attributes reported twice; fences left open inside a list item or blockquote; `<img src>` and `srcset`; reference uses with no definition; `UseAutoLinks` linking bare text GitHub leaves as text), stated as having no occurrence in the checked documents.
- `TheLinkCheckReportsUnsupportedHtmlAnchorForms` adds `<area href="b.md">` (an `href` on an element other than `<a>`, line 5) and `<a id=bare>` (an unquoted `id`, line 7), both reported as `UnsupportedHtml`.
- `TheFragmentCheckResolvesAnExplicitAnchor` adds `<a id="a&amp;b">`, reached by the fragment `#a&b`; without `WebUtility.HtmlDecode` the anchor would be `a&amp;b` and the link would be reported.

**Test count.** Unchanged from E32: `ArchitectureDocsConsistencyTests` has 29 tests; against `d4b84b4e` (4), +26 added and −1 removed (`EveryRelativeLinkInTheAuthorityDocsResolves`), the 26 names as listed in E32. A first run before these records was 29/0/0 (`arch-docs-e33-pre.log`).

**Checks** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e33/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e33`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e33-a.log`, `capture-e33-b.log`, `capture-e33-c.log` (preceded by a clean incremental test-project build, `tests-build-e33.log`).
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e33.log`.
3. Registration check filter: **821/0/0**, unchanged; `registration-e33.log`.
4. `git diff --check d4b84b4e`: `diff-check-e33.log`.
5. `ArchitectureDocsConsistencyTests`, run last, after this entry: `arch-docs-e33-final.log`.

## E34 VG-1 pass-6 patch re-review: document repairs under ruling (A) (implementation owner, 26 November 2026)

Line-local document repairs over `d4b84b4e`. Files edited: `DESIGN.md`, `docs/general-engine-design.md`, `docs/planning/invest/vertical-delivery.md`, `docs/planning/invest/decisions.md`, `docs/planning/invest/engine.md` and this run log. The test file, the contract and `deferred-work.md` were not edited by me this round.

**Corrections to E33 (append-only).**
- E33 said "229 have `parent="."`" of the `col_*` `CollisionShape3D` nodes. The `col_*` proxies are 263 nodes: 183 `CollisionShape3D` and 46 `MeshInstance3D` under the scene root (229 in all), and 32 `CollisionShape3D` and 2 `MeshInstance3D` under `Visual…` branches; 48 are `MeshInstance3D`, across 19 scenes.
- E33 quoted half the bake rule: `tools/BundleElements/SceneGeometryExtractor.cs:27-28` admits a node as a proxy when it is a `CollisionShape3D` or when its name starts with `col_`.
- E33 said the contract and `deferred-work.md` "were not edited". The coordinator edited both in that round; I did not.

**Verified.** The proxy counts above (`git grep` of `[node name="col_…"` lines in `elements/*/*.tscn`, by type and parent); `elements/Pipe/pipe.tscn:53` is `col_shell`, a `MeshInstance3D` under the root; `elements/Clutch/clutch.tscn:190-191` puts `col_input_shaft` under `Visual/InputShaft` with its own `quaternion`. `tools/BundleElements/PackageValidator.cs:116-130`: a port on body 0 needs its locator under the scene root (`"."`); a port on body N > 0 needs it under `SceneBodies[N].Root`; the locator must lie within the anchor tolerance of the declared position. `elements/ImpactLever/impact_lever.tscn:96`, `:99` place `Loc_Port_TieA` and `Loc_Port_TieB` under `Visual/Beam`. `SceneGeometryExtractor.cs:111-113` refuses a `col_hull` (and `col_capsule`) proxy. Row 6.5b (`vertical-delivery.md:186`) states that Story 9A.14 retired the `.tres` catalogue scan for the build-time element bundle.

**Document lines** (before → after):
- `DESIGN.md:82`: "…alongside visual meshes: `CollisionShape3D` nodes named `col_*`, each a direct child of the scene root (…ball.tscn:34) or a child of the `Visual/` branch whose transform it shares (…clutch.tscn:190); there is no `Collision/` node." → "…alongside visual meshes. A proxy is a `CollisionShape3D` node or a node named with the `col_` prefix (`tools/BundleElements/SceneGeometryExtractor.cs:27-28`), such as the hollow pipe wall `col_shell`, a `MeshInstance3D` (`elements/Pipe/pipe.tscn:53`). It sits under the scene root (for example `elements/Ball/ball.tscn:34`) or under a `Visual…` branch and moves with that branch (for example `elements/Clutch/clutch.tscn:190`); there is no `Collision/` node."
- `DESIGN.md:97`: "…(e.g. `Loc_Port_PowerIn`, `Loc_Port_ActivationIn`), as a child of the scene root at the declared anchor; the bundle build fails on any other name or position (Story 9A.17)." → "…(e.g. …), at the declared anchor and as a child of the node of the body its port declares: the scene root for body 0, otherwise that declared scene body's root node, such as `Visual/Beam` for the Impact Lever's `Loc_Port_TieA` and `Loc_Port_TieB` (`elements/ImpactLever/impact_lever.tscn:96`, `:99`). The bundle build fails on any other name, parent or position (Story 9A.17; `tools/BundleElements/PackageValidator.cs:116-130`)." This now agrees with `:101` (each secondary sub-tree carries its own socket locators).
- `DESIGN.md:70`: "The code that still does so is pending removal under LEGACY-PARTART-PURGE (Story 6.5f, in progress at `…sprint-status.yaml:102`; its roadmap row, `…vertical-delivery.md:190`, covers all procedural mesh building routines in C#): the procedural builder `ui/WorkshopArtwork.cs` …" → "The element, fixture and tool art builders in `ui/WorkshopArtwork.cs` are pending removal under LEGACY-PARTART-PURGE (Story 6.5f, in progress at `…sprint-status.yaml:102`; roadmap row `…vertical-delivery.md:190`); whether runtime geometry drawn between player-chosen points (wire segments and knots, placement shadows) is in 6.5f's scope is open for 6.5f's story planning. The procedural mesh code in C# today: the procedural builder `ui/WorkshopArtwork.cs` …" (the factual list of builders and callers that follows is unchanged).
- `DESIGN.md:108` (aligned with `:70`): "…and its procedural builders are pending removal under Story 6.5f…" → "…and its element, fixture and tool art builders are pending removal under Story 6.5f…".
- `DESIGN.md:419` (aligned with `:70`): "…the procedural builder `ui/WorkshopArtwork.cs`, renamed from `engine/PartArt.cs` at `8f682f84`, and the other procedural builders are pending removal under Story 6.5f…" → "…the element, fixture and tool art builders in `ui/WorkshopArtwork.cs`, renamed from `engine/PartArt.cs` at `8f682f84`, are pending removal under Story 6.5f…".
- `docs/general-engine-design.md:97`: "`Collision/` (or collision nodes): Contains non-rendered analytic and hollow proxy primitives used for physics (hulls are refused):" → "`col_*` proxy nodes (a `CollisionShape3D` or `MeshInstance3D` named with the `col_` prefix, under the scene root or under the `Visual` branch it moves with; there is no `Collision/` node): non-rendered analytic and hollow proxy primitives used for physics (hulls are refused):". Its `:102` bullet already states the full bake rule.
- `vertical-delivery.md:187` (6.5c): column 2 "glTF asset-driven geometry & collider extraction" → "Element `.tscn` scene-driven geometry & collider extraction"; deliverable "Generic extraction of analytic shapes (`col_*`), convex hulls, and `Loc_Port_<Id>` socket anchors …" → "Generic extraction of analytic shapes (`col_*`) and `Loc_Port_<Id>` socket anchors …" (the "in `WorkshopPhysicsCompiler`" wording kept); legacy "… tables in `PartArt.cs` and physics compiler" → "… tables in `ui/WorkshopArtwork.cs` (formerly `engine/PartArt.cs`) and physics compiler".
- `vertical-delivery.md:188` (6.5d) column 2: "… conversion to glTF" → "… conversion to element `.tscn` scenes".
- `vertical-delivery.md:189` (6.5e) column 2: "… conversion to glTF" → "… conversion to element `.tscn` scenes".
- `vertical-delivery.md:190` (6.5f): deliverable "Complete deletion of all procedural mesh building routines in C#: `ui/WorkshopArtwork.cs` (…) and the other builders listed in `DESIGN.md:70` (…)" → "Complete deletion of the element, fixture and tool art builders in `ui/WorkshopArtwork.cs` (renamed from `engine/PartArt.cs` at `8f682f84`), their art coming from `.tscn` scenes (`engine/gpu/WorkshopPartKind.cs` was already deleted at `8f682f84`)"; legacy "Procedural mesh generation in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`) and the other C# builders, hardcoded shape tables" → "Procedural element, fixture and tool art in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`), hardcoded shape tables"; acceptance "0 procedural mesh builders in C#; clean build; …" → "The element, fixture and tool art builders in `ui/WorkshopArtwork.cs` are deleted and their art comes from `.tscn` scenes; clean build; …" (the rest of the row, including the Chrome E2E clause, unchanged).
- `vertical-delivery.md:197` (LEGACY-0d): legacy "procedural mesh generation in engine/PartArt.cs" → "procedural mesh generation in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`)"; acceptance "all elements defined via declarative .glb assets and catalog resources" → "all elements defined via their `.tscn` scenes and the build-time element bundle".
- `vertical-delivery.md:236` (programme definition of done): "procedural mesh generation (`PartArt.cs`)" → "procedural mesh generation (`ui/WorkshopArtwork.cs`)".
- `decisions.md:122` (the glTF architectural decision, `decision-gltf-element-assets`): "**Status:** Approved / Authoritative" → "**Status:** Superseded for format by the owner decision of 18 October 2026: the authored asset format is the element's own Godot `.tscn` scene, not `.glb` (`…epics.md:538`, `:933`). Its `KHR_physics_rigid_bodies` and compile-time-extraction content stands as written; those questions are still deferred." The decision body is unchanged.
- `decisions.md:143`: "Imperative C# procedural mesh generation (`engine/PartArt.cs`) is deprecated…" → "… (`ui/WorkshopArtwork.cs`, renamed from `engine/PartArt.cs` at `8f682f84`) is deprecated…".
- `engine.md:208`: "the new declarative `.glb` + `PartId` architecture" → "the new declarative `.tscn` + `PartId` architecture".
- `engine.md:211` (6.5c): "from `.glb` scene trees" → "from `.tscn` scene trees".
- `engine.md:212` (6.5d): "to self-contained `.glb` assets in `parts/models/`" → "to the element's own `.tscn` scene in its package (`elements/<Name>/`)".
- `engine.md:213` (6.5e): "to `.glb` models with embedded `col_*` proxies" → "to `.tscn` scenes with embedded `col_*` proxies".
- Not edited, for the coordinator: `engine.md:206` heading ("Existing Element Conversion to glTF & Zero-Remnant Legacy Purge") and `:214` (6.5f, "Permanently delete `engine/PartArt.cs`, `engine/gpu/WorkshopPartKind.cs`, and all procedural mesh building routines in C#"), and `decisions.md:155` ("Procedural mesh code in `PartArt.cs`"), all outside the ruled lines.

**Checks** (pinned toolchain; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`; no C# changed, so no rebuild):
1. `git diff --check d4b84b4e`: `diff-check-e34.log`.
2. `ArchitectureDocsConsistencyTests`, run last, after this entry, on the E33 build: `arch-docs-e34-docs.log`.

## E35 VG-1 final capped re-review: document repairs under ruling (A) (implementation owner, 26 November 2026)

Line-local document repairs over `d4b84b4e`. Files edited: `DESIGN.md`, `docs/general-engine-design.md`, `docs/planning/invest/vertical-delivery.md`, `docs/planning/invest/decisions.md`, `docs/planning/invest/engine.md` and this run log. The test file, the contract and `deferred-work.md` were not edited by me.

**Corrections to E34 (append-only).** `decisions.md:155` should read `:153` (the "Engine Codebase" bullet); `DESIGN.md:101` should read `:100` (each secondary sub-tree carries its own locators); the general-engine-design bake-rule bullet is at `:103`, not `:102`; the `PackageValidator` checks are `tools/BundleElements/PackageValidator.cs:89-97` (locator names: duplicates, tube mouths, undeclared sockets) and `:116-133` (parent by declared body, and position within the anchor tolerance), not `:116-130`. E34 also called the `decisions.md:122` section S257; S257 is the table section at `:8-10`, and `:122` is the status of `decision-gltf-element-assets`.

**Verified.** `epics.md:992-1011` (Story 6.5f): `engine/PartArt.cs` and `engine/gpu/WorkshopPartKind.cs` permanently deleted, and "strictly zero procedural mesh building routines". `docs/engine-contracts.md:92`: the baked bundle "is then the compiler's only collider source". `tools/BundleElements/SceneGeometryExtractor.cs:111-114` refuses `col_capsule` and `col_hull`. `engine/PartArt.cs` was added at `ca84a4cb` as the same 32-line builder file and only renamed at `8f682f84` (`git log -- engine/PartArt.cs`, `git show ca84a4cb:engine/PartArt.cs`), so it never held coordinate or collider tables. `engine/gpu/WorkshopPhysicsCompiler.cs` builds colliders from declared descriptors (`:125-126`) and holds no collider dimension table, so it is dropped from 6.5c's legacy column; the coordinate table in C# today is the workbench's (`engine/Workbench.cs:11-14`). `git grep` finds no link to the old `engine.md:206` heading slug (its section is reached by the explicit `convert-existing-implementations` anchor).

**Document lines** (before → after):
- `DESIGN.md:70`: "… The element, fixture and tool art builders in `ui/WorkshopArtwork.cs` are pending removal under LEGACY-PARTART-PURGE (…; roadmap row …); whether runtime geometry drawn between player-chosen points (wire segments and knots, placement shadows) is in 6.5f's scope is open for 6.5f's story planning. The procedural mesh code in C# today: …" → "… `ui/WorkshopArtwork.cs` and every other procedural mesh building routine in C# are pending removal under LEGACY-PARTART-PURGE (Story 6.5f, in progress at `…sprint-status.yaml:102`; roadmap row `…vertical-delivery.md:190`). The procedural mesh code and tables in C# today: …"; in the caller list, "the rotation gizmo's own `CylinderMesh` and `BoxMesh` handles (`ui/RotationGizmo.cs`)" → "(`ui/RotationGizmo.cs:54`, `:60`, `:105`, `:107`)". The rest of the factual list (including the placement-shadow `ImmediateMesh` and the workbench table) is unchanged; nothing is said about what replaces the runtime geometry.
- `DESIGN.md:97`: "(Story 9A.17; `tools/BundleElements/PackageValidator.cs:116-130`)" → "(Story 9A.17; `tools/BundleElements/PackageValidator.cs:89-97`, `:116-133`)".
- `DESIGN.md:108`: "…and its element, fixture and tool art builders are pending removal under Story 6.5f…" → "…and it and every other procedural mesh building routine in C# are pending removal under Story 6.5f…".
- `DESIGN.md:419`: "…the element, fixture and tool art builders in `ui/WorkshopArtwork.cs`, renamed from `engine/PartArt.cs` at `8f682f84`, are pending removal under Story 6.5f…" → "…`ui/WorkshopArtwork.cs`, renamed from `engine/PartArt.cs` at `8f682f84`, and every other procedural mesh building routine in C# are pending removal under Story 6.5f…".
- `docs/general-engine-design.md:97`: "`col_*` proxy nodes (a `CollisionShape3D` or `MeshInstance3D` named with the `col_` prefix, …)" → "Collision proxy nodes (any `CollisionShape3D`, or any node named with the `col_` prefix, under the scene root or under the `Visual` branch it moves with; there is no `Collision/` node)".
- `docs/general-engine-design.md:105`: "`Locators/` (or locator nodes): Empty nodes named `Loc_Port_<Id>` specifying 3D socket attachment points for electrical, activation, and mechanical connections." → "Locator nodes: empty nodes named `Loc_Port_<Id>` specifying 3D socket attachment points …, each at its declared anchor and under the scene root for a port on body 0, otherwise under that declared scene body's root node (`tools/BundleElements/PackageValidator.cs:116-133`); there is no `Locators/` group node." (as at `DESIGN.md:97`).
- `vertical-delivery.md:187` (6.5c): deliverable "… from instantiated `.tscn` scene hierarchy in `WorkshopPhysicsCompiler`" → the same, then "(not current: the build-time element bundle is the compiler's only collider source, `docs/engine-contracts.md:92`; compiler-side extraction awaits the ENGINE-CORE-2d replanning)"; legacy "Hardcoded C# coordinate and collider dimension tables in `ui/WorkshopArtwork.cs` (formerly `engine/PartArt.cs`) and physics compiler" → "Hardcoded C# coordinate tables, today the workbench's (`engine/Workbench.cs:11-14`)".
- `vertical-delivery.md:190` (6.5f), restoring the source acceptance: deliverable "Complete deletion of the element, fixture and tool art builders in `ui/WorkshopArtwork.cs` (…), their art coming from `.tscn` scenes (…)" → "`ui/WorkshopArtwork.cs` (renamed from `engine/PartArt.cs` at `8f682f84`) is deleted and no procedural mesh building routine remains in C# (`engine/gpu/WorkshopPartKind.cs` was already deleted at `8f682f84`)"; legacy "Procedural element, fixture and tool art in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`), hardcoded shape tables" → "Procedural mesh generation in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`) and the other procedural mesh code in C#, hardcoded shape tables"; acceptance "The element, fixture and tool art builders in `ui/WorkshopArtwork.cs` are deleted and their art comes from `.tscn` scenes; clean build; …" → "`ui/WorkshopArtwork.cs` (renamed from `engine/PartArt.cs` at `8f682f84`) is deleted and no procedural mesh building routine remains in C#; clean build; …" (the rest, including the Chrome clause, unchanged).
- `decisions.md:122` (status of `decision-gltf-element-assets`): "Its `KHR_physics_rigid_bodies` and compile-time-extraction content stands as written; those questions are still deferred." → "Its compile-time extraction in `WorkshopPhysicsCompiler`, its `col_capsule` and `col_hull` proxies and its `.tres` catalogue are not current: the build-time element bundle is the compiler's only collider source (`docs/engine-contracts.md:92`), and both proxies are refused at bundle time (`tools/BundleElements/SceneGeometryExtractor.cs:111-114`). Whether `KHR_physics_rigid_bodies` and compile-time extraction stay planned is deferred to the ENGINE-CORE-2d replanning."
- `decisions.md:153`: "Procedural mesh code in `PartArt.cs` and hardcoded collider dimension tables…" → "Procedural mesh code in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`) and hardcoded collider dimension tables…".
- `engine.md:179`: "**ENGINE-CORE-2d (glTF Asset-Driven Geometry & Collider Extraction):** Extend `WorkshopPhysicsCompiler` to traverse instantiated `.glb` scene hierarchies and extract … from collision proxy nodes (`col_box`, `col_sphere`, `col_capsule`, `col_cylinder`, `col_hull`, and `KHR_physics_rigid_bodies`), compiling them … tables." → "**ENGINE-CORE-2d (Element `.tscn` Scene-Driven Geometry & Collider Extraction):** Extend `WorkshopPhysicsCompiler` to traverse instantiated `.tscn` scene hierarchies and extract … from collision proxy nodes (`col_box`, `col_sphere`, `col_cylinder`; `col_capsule` and `col_hull` are refused at bundle time, `…SceneGeometryExtractor.cs:111-114`, and `KHR_physics_rigid_bodies` is not admitted), compiling them … tables (not current: the build-time element bundle is the compiler's only collider source, `docs/engine-contracts.md:92`; compiler-side extraction awaits the ENGINE-CORE-2d replanning)."
- `engine.md:202`: "…directly from the instantiated `.glb` scene hierarchy at compile-to-state time, converting analytic proxy primitives (`col_box`, `col_sphere`, `col_capsule`, `col_cylinder`), convex hulls (`col_hull`), and `KHR_physics_rigid_bodies` metadata into…" → "…directly from the instantiated `.tscn` scene hierarchy at compile-to-state time (not current: …; compiler-side extraction awaits the ENGINE-CORE-2d replanning), converting analytic proxy primitives (`col_box`, `col_sphere`, `col_cylinder`; `col_capsule` and convex hulls `col_hull` are refused at bundle time, `…SceneGeometryExtractor.cs:111-114`, and `KHR_physics_rigid_bodies` metadata is not admitted) into…".
- `engine.md:206` heading: "Existing Element Conversion to glTF & Zero-Remnant Legacy Purge" → "Existing Element Conversion to Element `.tscn` Scenes & Zero-Remnant Legacy Purge" (the `convert-existing-implementations` anchor on `:205` is kept).
- `engine.md:211` (6.5c): "…from `.tscn` scene trees in `WorkshopPhysicsCompiler`." → the same, then "(not current: the build-time element bundle is the compiler's only collider source, `docs/engine-contracts.md:92`; compiler-side extraction awaits the ENGINE-CORE-2d replanning)."
- `engine.md:214` (6.5f): "Permanently delete `engine/PartArt.cs`, `engine/gpu/WorkshopPartKind.cs`, and all procedural mesh building routines in C#." → "Permanently delete `ui/WorkshopArtwork.cs` (renamed from `engine/PartArt.cs` at `8f682f84`) and all procedural mesh building routines in C# (`engine/gpu/WorkshopPartKind.cs` was already deleted at `8f682f84`)."
- `engine.md:279` (LEGACY-0d): "Permanently delete procedural mesh generation in `engine/PartArt.cs` and hardcoded collider tables in C#, establishing `.glb` assets as the single authoritative geometry definition…" → "Permanently delete procedural mesh generation in `ui/WorkshopArtwork.cs` (formerly `PartArt.cs`) and hardcoded collider tables in C#, establishing the elements' `.tscn` scenes and the build-time element bundle as the single authoritative geometry definition…".

**Checks** (pinned toolchain; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`; no C# changed, so no rebuild):
1. `git diff --check d4b84b4e`: `diff-check-e35.log`.
2. `ArchitectureDocsConsistencyTests`, run last, after this entry, on the E33 build: `arch-docs-e35-docs.log`.

## E36 VG-1 closing capped re-review: line repairs and amendment notes (implementation owner, 26 November 2026)

Line-local repairs over `d4b84b4e`, plus the amendment notes of ruling (i). Files edited: `docs/planning/invest/vertical-delivery.md`, `docs/planning/invest/engine.md`, `docs/general-engine-design.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/prd.md` and this run log. The test file, the contract and `deferred-work.md` were not edited by me; no other `epics.md` or `prd.md` text was changed, and the retro and guide files are untouched.

**Verified.** `epics.md:1006`: "codebase grep confirms 0 occurrences of `PartArt` or `WorkshopPartKind` across all active C# source files". `engine/gpu/WorkshopPhysicsCompiler.cs:125-126`: `AddGeometry` takes the declared `ColliderDescriptor` list. `epics.md:940` admits `col_box`, `col_sphere`, `col_cylinder` and the hollow tube, frustum and torus proxies and keeps `KHR_physics_rigid_bodies` as planned; `DESIGN.md:106` says "a planned capability, not admitted today"; `tools/BundleElements/SceneGeometryExtractor.cs:205-232` (`Hollow`) yields the `Tube`, `Frustum` and `Torus` kinds, as `docs/general-engine-design.md:101` names them. `tools/BundleElements/PackageValidator.cs:127` refuses a locator whose parent is not the expected node ("is not a child of the scene root"). Story 9A.14 retired the `.tres` catalogue: `_bmad-output/implementation-artifacts/spec-9a-14-bundle-sole-catalogue-source.md:17` ("The `.tres` catalogue, its loader and the static palette list are deleted") and `:24` (all 24 `parts/catalog/*.tres`); `git ls-files parts/catalog` lists nothing. The 18 October notes copy the form of `epics.md:933`, `:954` and `:975`.

**Placement of the `epics.md` notes.** Each new note is appended, after a space, to the existing italic status or dependency line rather than added as a new paragraph, so `epics.md` keeps its line count (5,855) and every `epics.md:<line>` citation in the repository still points where it did (the lane 1 line `epics.md:4466` included). A first placement as separate paragraphs shifted 104 cited lines and was reverted before any check ran. `prd.md` has no line citations outside the archived implementation artifacts, so its note is a paragraph of its own.

**Document lines** (before → after):
- `vertical-delivery.md:190` (6.5f acceptance): "`ui/WorkshopArtwork.cs` (renamed from `engine/PartArt.cs` at `8f682f84`) is deleted and no procedural mesh building routine remains in C#; clean build; …" → "… is deleted and no procedural mesh building routine remains in C#; codebase grep confirms 0 occurrences of `PartArt` or `WorkshopPartKind`; clean build; …" (the story's grep clause restored; `:1005` and `:1007` not added).
- `vertical-delivery.md:187` (6.5c legacy column): "Hardcoded C# coordinate tables, today the workbench's (`engine/Workbench.cs:11-14`)" → "Hardcoded C# collider tables (none remain in C#; colliders come from declared descriptors, `engine/gpu/WorkshopPhysicsCompiler.cs:125-126`)". The deliverable's qualifier is unchanged.
- `engine.md:179`: "collision proxy nodes (`col_box`, `col_sphere`, `col_cylinder`; `col_capsule` and `col_hull` are refused at bundle time, `…SceneGeometryExtractor.cs:111-114`, and `KHR_physics_rigid_bodies` is not admitted)" → "collision proxy nodes (`col_box`, `col_sphere`, `col_cylinder` and the hollow Tube, Frustum and Torus kinds, `tools/BundleElements/SceneGeometryExtractor.cs:205-232`; `col_capsule` and `col_hull` are refused at bundle time, `:111-114`, and `KHR_physics_rigid_bodies` is planned, not admitted today)".
- `engine.md:202`: "analytic proxy primitives (`col_box`, `col_sphere`, `col_cylinder`; `col_capsule` and convex hulls `col_hull` are refused at bundle time, `…:111-114`, and `KHR_physics_rigid_bodies` metadata is not admitted)" → "analytic proxy primitives (`col_box`, `col_sphere`, `col_cylinder` and the hollow Tube, Frustum and Torus kinds, `…SceneGeometryExtractor.cs:205-232`; `col_capsule` and convex hulls `col_hull` are refused at bundle time, `:111-114`, and `KHR_physics_rigid_bodies` metadata is planned, not admitted today)".
- `general-engine-design.md:105`: "each at its declared anchor and under the scene root for a port on body 0, otherwise under that declared scene body's root node (`tools/BundleElements/PackageValidator.cs:116-133`)" → "each at its declared anchor and a direct child of the scene root for a port on body 0, otherwise of that declared scene body's root node (`tools/BundleElements/PackageValidator.cs:89-97` for names, `:116-133` for parent and position)".
- `epics.md:911` (Story 6.5b status line), appended: "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): the authored asset format is the element's own Godot `.tscn` scene, not `.glb` (`:538`, `:933`). The `.tres` part catalogue (`parts/catalog/`) named in the acceptance below was retired by Story 9A.14 in favour of the build-time element bundle (`…spec-9a-14-bundle-sole-catalogue-source.md:17`, `:24`).*"
- `epics.md:954` (Story 6.5d's existing 18 October note), extended at its end: "… closes via Stories 9R.11–9R.14, 9R.18 and 9A.16. The `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle.*"
- `epics.md:975` (Story 6.5e's existing 18 October note), extended at its end: "… closes via Stories 9R.8, 9R.15–9R.17, 9R.18, 9A.16 and 9A.17. The `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle.*"
- `epics.md:995` (Story 6.5f dependency line), appended: "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`. The `.glb` + `PartId` clause below reads as "the element's own `.tscn` scene and its `PartId`".*" No acceptance clause changed.
- `epics.md:1015` (Story 6.6, the CAT-048 dependency line), appended: "*Amended 18 Oct 2026 (…): the authored asset format is the element's own Godot `.tscn` scene, not `.glb` (`:538`, `:933`), and the `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle; the dependency above reads accordingly.*"
- `epics.md:1161` (Story 7.4, LEGACY-0d, status line), appended: "*Amended 18 Oct 2026 (…): the authored asset format is the element's own Godot `.tscn` scene, not `.glb` (`:538`, `:933`), and the `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle; the acceptance clause naming `.glb` assets and `.tres` catalog resources reads as "the elements' own `.tscn` scenes and the build-time element bundle".*"
- `prd.md:188` (new paragraph under §3.8's heading at `:186`): "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): the authored asset format is the element's own Godot `.tscn` scene, not `.glb` (`epics.md:538`, `:933`), and the `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle. The `.glb` and `.tres` references in this section read as "the element's own `.tscn` scene" and "the build-time element bundle".*"

**Checks** (pinned toolchain; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`; no C# changed, so no rebuild):
1. `git diff --check d4b84b4e`: `diff-check-e36.log`.
2. `ArchitectureDocsConsistencyTests`, run last, after this entry, on the E33 build: `arch-docs-e36-docs.log`.

## E37 notes-only pass findings: line repairs, note dates and `.tres` readings (implementation owner, 26 November 2026)

Line-local repairs over `d4b84b4e` on the lines E36 changed. Files edited: `docs/planning/invest/vertical-delivery.md`, `docs/planning/invest/engine.md`, `docs/general-engine-design.md`, `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/prd.md` and this run log. `epics.md` keeps 5,855 lines; every note stays on the line E36 put it on. No acceptance text changed; the test file, the contract and `deferred-work.md` were not edited by me.

**Verified.**
- `engine/gpu/WorkshopPhysicsCompiler.cs`: the workbench support plane is built from C# constants at `:95-103` (`var plane = identities.Plane;` at `:95` through `ColliderShapeKind.Plane` at `:103`; the height comes from `WorkshopConstruction.WorkbenchSurface`). The only other `colliders.Add` is `:136`, inside `AddGeometry` (`:125`), which every element body reaches with its declared descriptors (`:176`, `:217`, `:262`). The coordinator's range `:96-103` starts one line late; `:95-103` is used.
- `tools/BundleElements/SceneGeometryExtractor.cs`: `Hollow` begins at `:205`; Tube returns at `:242`, Frustum at `:243`, and the method closes at `:244`.
- `tools/BundleElements/PackageValidator.cs`: the locator loop opens at `:86`; the duplicate check is `:88-89` (`TryAdd` at `:88`, "appears more than once" at `:89`); the undeclared-name refusal ends at `:97`. The coordinator's `:87-97` starts on the loop's opening brace; `:88-97` is used.
- `docs/general-engine-design.md:40`: `tools/BundleElements` discovers the packages at build time and writes one bundle, and the runtime loads only that bundle; `:65`: every element is defined through the declaration in its `elements/<Name>/` package, written into the element bundle.
- `_bmad-output/implementation-artifacts/spec-9a-14-bundle-sole-catalogue-source.md:38`: "`PartRegistry.Load()` reads `res://elements/elements.bundle.json` only". `engine/PartRegistry.cs:16-24` agrees (loaded only from the generated element bundle; `BundlePath`).
- `git show -s 0645f57d`: 2026-10-21, "make the element bundle the sole catalogue …" (Stories 9A.14, 9A.18).
- `epics.md:538` is the 18 October owner decision on `.tscn`. `epics.md:964` and `:986` are the 6.5d and 6.5e `.tres` clauses. `prd.md:203` is the `.tres` scan and `:205` the "authoring its `.glb` model and `.tres` catalog resource" line (both moved by 2 after the E36 paragraph).

**Document lines** (before → after):
- `vertical-delivery.md:190`: "codebase grep confirms 0 occurrences of `PartArt` or `WorkshopPartKind`; clean build;" → "codebase grep confirms 0 occurrences of `PartArt` or `WorkshopPartKind` across all active C# source files; clean build;" (word for word as `epics.md:1006`).
- `vertical-delivery.md:187` (6.5c legacy column): "Hardcoded C# collider tables (none remain in C#; colliders come from declared descriptors, `engine/gpu/WorkshopPhysicsCompiler.cs:125-126`)" → "Hardcoded C# collider tables: element colliders come from declared descriptors (`engine/gpu/WorkshopPhysicsCompiler.cs:125-126`); the one built-in collider is the workbench support plane (`:95-103`)".
- `engine.md:179` and `:202`: "`tools/BundleElements/SceneGeometryExtractor.cs:205-232`" → "`tools/BundleElements/SceneGeometryExtractor.cs:205-244`"; nothing else on either line changed.
- `general-engine-design.md:105`: "`tools/BundleElements/PackageValidator.cs:89-97` for names" → "`tools/BundleElements/PackageValidator.cs:88-97` for names".
- `epics.md:911` (6.5b): "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): … not `.glb` (`:538`, `:933`). The `.tres` part catalogue (`parts/catalog/`) named in the acceptance below was retired by Story 9A.14 in favour of the build-time element bundle (`…:17`, `:24`).*" → "*Amended 26 Nov 2026 (DOC-ENGINE-STATUS, applying the 18 Oct owner decision, `:538`, `:933`; `.tres` catalogue retired by Story 9A.14, 21 Oct 2026): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`. The `.tres` part catalogue (`parts/catalog/`) named in the acceptance below is retired (`…spec-9a-14-bundle-sole-catalogue-source.md:17`, `:24`); its discovery clauses read as "`PartRegistry.Load()` registers every element from the build-time element bundle without hardcoded path lists".*"
- `epics.md:954` (6.5d, appended sentence only): "The `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle." → "The `.tres` part catalogue (`parts/catalog/`) is retired (Story 9A.14, 21 Oct 2026; noted 26 Nov 2026 by DOC-ENGINE-STATUS); the `.tres` clause below reads as "defined by its element package's declaration, which `tools/BundleElements` writes into the build-time element bundle"." The original 18 October note is unchanged.
- `epics.md:975` (6.5e, appended sentence only): the same change as `:954`.
- `epics.md:995` (6.5f): "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): the authored asset format …" → "*Amended 26 Nov 2026 (DOC-ENGINE-STATUS, applying the 18 Oct owner decision, `:538`, `:933`): the authored asset format …"; the rest of the note is unchanged and carries no `.tres` clause.
- `epics.md:1015` (6.6, CAT-048 dependency): "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): … not `.glb` (`:538`, `:933`), and the `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle; the dependency above reads accordingly.*" → "*Amended 26 Nov 2026 (DOC-ENGINE-STATUS, applying the 18 Oct owner decision, `:538`, `:933`; `.tres` catalogue retired by Story 9A.14, 21 Oct 2026): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`, and the `.tres` catalog definition in the dependency above reads as "its element package declaration".*"
- `epics.md:1161` (7.4, LEGACY-0d): "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): … was retired by Story 9A.14 in favour of the build-time element bundle; the acceptance clause … reads as "the elements' own `.tscn` scenes and the build-time element bundle".*" → "*Amended 26 Nov 2026 (DOC-ENGINE-STATUS, applying the 18 Oct owner decision, `:538`, `:933`; `.tres` catalogue retired by Story 9A.14, 21 Oct 2026): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`, and the `.tres` part catalogue (`parts/catalog/`) is retired; the acceptance clause naming `.glb` assets and `.tres` catalog resources reads as "the elements' own `.tscn` scenes and package declarations, written into the build-time element bundle".*"
- `prd.md:188`: "*Amended 18 Oct 2026 (owner decision; Open scheduling item i): … (`epics.md:538`, `:933`), and the `.tres` part catalogue (`parts/catalog/`) was retired by Story 9A.14 in favour of the build-time element bundle. The `.glb` and `.tres` references in this section read as "the element's own `.tscn` scene" and "the build-time element bundle".*" → "*Amended 26 Nov 2026 (DOC-ENGINE-STATUS, applying the 18 Oct owner decision, `epics.md:538`, `:933`; `.tres` catalogue retired by Story 9A.14, 21 Oct 2026): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`, and the `.tres` part catalogue (`parts/catalog/`) is retired. The `.glb` references in this section read as "the element's own `.tscn` scene"; the discovery scan at `:203` reads as "`PartRegistry.Load()` reads the build-time element bundle", and `:205` reads as "authoring its element package (declaration and `.tscn` scene)".*"

**Corrections to E36.**
- E36 cited the extractor's hollow branch as `SceneGeometryExtractor.cs:205-232`; the method runs to `:244` (Tube `:242`, Frustum `:243`).
- E36 said `docs/general-engine-design.md:101` names the hollow kinds; it is `:102`.
- E36 said `prd.md` had no line citations outside the archived implementation artifacts. That grep excluded the whole implementation-artifacts directory, which also holds the live `deferred-work.md`; its `prd.md:196–197` citation moved to `:198–199` with the E36 paragraph (the coordinator has corrected that record). The only other `prd.md` line citation is `spec-elements-visible-solid-remainder-b.md:90` (`prd.md:191`, `:195`), a done spec recording its own review-time lines, left unchanged as history.
- E36's notes were stamped 18 October; E37 re-stamps the notes written by this slice with 26 November and dates the `.tres` retirement 21 October (`0645f57d`).

**Checks** (pinned toolchain; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`; no C# changed, so no rebuild):
1. `git diff --check d4b84b4e`: `diff-check-e37.log`.
2. `ArchitectureDocsConsistencyTests`, run last, after this entry, on the E33 build: `arch-docs-e37-docs.log`.

## E38 Coordinator: closing records, contract budget and verbatim Spec Change Log entries (26 November 2026)

**Reviews after E31.** All were run by fresh context-free reviewers, and the findings are in the coordinator's reports to the lead coordinator.
- Pass 5 (E32), pass 6 (E33) and the pass-6 patch re-review (E34) were followed by a final capped re-review (E35), a closing capped re-review (E36) and a notes-only pass (E37).
- Each IN finding, meaning a false or contradictory changed line, was patched.
- Each Forward-Only finding was repaired or ruled on.
- Each OUT item went to `deferred-work.md` with an owner.
- The lead coordinator's rulings:
  - (i) for the `vertical-delivery.md` rows;
  - (A) twice;
  - (i) for the amendment notes;
  - withdrawal of both 6.5f restatements, with the source acceptance restored;
  - the hard cap.

**The E36 placement deviation is accepted.** The notes are appended inline on existing `epics.md` lines, not added as new paragraphs, so the file keeps 5,855 lines and every `epics.md:<line>` citation in the repo, including lane 1's `:4466`, still lands. `prd.md` takes its note as its own paragraph. The one live citation it shifts is in `deferred-work.md`, corrected to `:198–199`. `spec-elements-visible-solid-remainder-b.md:90`, which cites `prd.md:191` and `:195`, is a done spec, so its numbers are history.

**Contract budget.** The contract reached 8,574 words. The VG-1 Spec Change Log entries for passes 1–5 move here verbatim; the contract keeps the authorisation, the planning-verdict correction, a summary and the current pass-6 entry.

### Verbatim: Spec Change Log, VG-1 passes 1–5, moved from the contract

- 26 November 2026, VG-1 re-review pass 1 (finding D1-1, Element-Free). AC3 still said "no test is added or removed" and expected 7407, which contradicted the authorised repair. AC3 and its acceptance-table row now expect the base plus the ten M6 VG-1 tests (7417), with registration unchanged at 821. FO-1 refactored the existing file-existence test onto the checker's one link enumerator, in the same file. This is inside the amendment, because it removes a second link rule rather than adding a test. KEEP: zero failures, zero skipped, the 821 registration total and the gate with both canaries.
- 26 November 2026, coordinator ruling (a) on VG-1 re-review pass 2, finding ECH-1. Fragment-less links in 21 of the 27 checked documents were never existence-checked. Thirteen were broken at the baseline, outside quotations, so the authorisation is widened.
  - **One rule.** Every relative link in the 27 checked documents must resolve case-exactly to an existing file or directory, and a fragment link must also resolve its anchor. No separate authority-only existence rule remains.
  - **Quotations.** Quoted text is skipped by one stated rule: a straight-double-quoted span or a blockquote line quotes another place, so its links are not this document's. Code spans and fences are also skipped. The coordinator accepted this rule. `batch-i.md:137` and `:139` stay unedited as verbatim quotations.
  - **Document repairs.** The broken links are repaired line-locally: ten in `current-consumers.md`, two at `DESIGN.md:419`, and the wrong relative depth at `owner-decisions-2026-11-22.md:71`. Each points to its current home, or becomes plain text where there is none (run log E27). `owner-decisions:71` is re-checked at merge.
  - KEEP: every other Never item, and test-only scope apart from these link lines.
- 26 November 2026, VG-1 re-review pass 3 (Element-Free finding 2). The pass-2 repairs change AC3 from 7417 to 7423 (7407 + 17 − 1): `EveryRelativeLinkInTheAuthorityDocsResolves` is removed because the one link rule, `EveryRelativeLinkInTheCheckedDocsResolves`, replaces it. `AgentsLinksTheIsolationAuthority` is rewritten onto the shared enumerator, and its count is unchanged (run log E28). KEEP: registration 821, zero failures, zero skipped.
- 26 November 2026, coordinator rulings on VG-1 re-review pass 3.
  - **Quotation skip withdrawn (Forward-Only F-1a/F-1b).** GitHub renders links in blockquotes and in quoted text, so skipping them was a second rule. Both skips are deleted, and links there are judged by the one rule.
    - The verbatim EL-242 and EL-072 proposal text in `batch-i.md:133–139` becomes a code fence and code spans. The wording is unchanged.
    - This supersedes the quotation clause of the ruling-(a) entry above.
  - **Checker.** Existence resolves against tracked files (`git ls-files`). Slugs unescape backslash escapes and decode HTML entities. The front-matter skip gets a test.
  - **Document widening, about six lines beyond the link lines.**
    - `DESIGN.md:70`, `:104` and `:419` say the procedural-art files were removed, with the deleting commits.
    - `current-consumers.md:137` and `:226` are in the past tense, deleted at `8f682f84`.
  - **Convergence cap.** Pass 4 reviews this patch. A further checker-parsing finding with no occurrence in the 27 checked documents goes to the coordinator as one list, not patched.
  - **AC3 (E29).** The total becomes 7426: 7407 + 20 − 1. Three tests are added (an untracked-file negative, a tracked-file control and a front-matter test), and the quotation test is inverted and renamed.
  - KEEP: every other Never item.
- 26 November 2026, coordinator ruling on the VG-1 pass-4 latent-parsing list.
  - **Why a parser.** Twenty latent items were found in code the repair added. Re-review scope does not allow deferring them, and patching them one by one does not converge.
  - **Markdig replaces the hand-written Markdown parsing.** Markdig is a maintained CommonMark/GFM parser. It is added at an exact pinned version to `CuriousContraptions.tests` only, which is the one dependency this slice adds (run log E30).
    - Links come from `LinkInline` and `LinkReferenceDefinition` nodes and from HTML `href`.
    - Anchors come from GitHub auto-identifiers and from HTML `id` attributes. The ruling's `name` clause was withdrawn at pass 5 under FO-2 (entry below).
    - The hand-written fence, code-span, emphasis, setext, blockquote and slug code is deleted with its tests. Behavioural tests of the checker's outcomes remain.
  - **External links** are only those with a scheme on the allow-list `http`, `https`, `mailto`. Any other destination is a relative path.
  - **Unsupported fragments.** GitHub line links (`#L10`) and directory-README fragments are reported as a typed unsupported form.
  - **Patched as well:**
    - a non-boundary path prefix is refused;
    - a file that is tracked but deleted in the worktree gives a typed report;
    - `git ls-files` reads stdout and stderr concurrently, with a bounded wait and a loud timeout.
  - **Documents.** `DESIGN.md:70`, `:104`, `:108` and `:419` and `batch-i.md:134` and `:141` are made true to the current tree and the owners named for its work.
  - **Known limit.** Where Markdig's CommonMark behaviour differs from GitHub's renderer in a case with no occurrence in the 27 checked documents, that is a limit of the chosen parser. It is recorded here and not patched.
  - **AC3 (E30).** The total becomes 7429: 7407 + 23 − 1. Markdig 1.6.0 is restored from nuget.org.
  - KEEP: zero skipped, registration 821, test project only.
- 26 November 2026, coordinator rulings on VG-1 re-review pass 5.
  - **`<a name>` withdrawn (FO5-1, under FO-2).** `<a id="…">` is the one HTML anchor form.
  - **The checker's HTML reading.** It reads only Markdig's `HtmlInline` and `HtmlBlock` nodes and excludes comments. It matches only the whole attribute names `href` and `id`, accepts double- or single-quoted values, and decodes entities. An unquoted value, or `id` on another element, is reported as unsupported.
  - **Other checker changes.**
    - Autolinks go through the scheme allow-list.
    - `UnclosedFence` is reported only for a fence at document level.
    - Markdig is pinned exactly, `[1.6.0]`.
    - AC3 becomes 7432 (7407 + 26 − 1), with three new tests (E32).
  - **DESIGN.md.** It now states that `engine/PartArt.cs` was renamed to `ui/WorkshopArtwork.cs` at `8f682f84` (R091), not removed. It also names 6.5f as owner of every procedural builder.
  - **Supported subset and limits.** The checker's class summary is the authority for the supported Markdown subset, its enabled Markdig extensions and its named limits.
    - Links come from inline, reference, autolink, image and `<a href>` forms. Anchors come from headings and `<a id>`.
    - A construct outside the subset is reported as a typed problem or named as a limit.
    - The limits named include the GFM slug differences and submodules and symlinks; pass 6 added more. No named limit occurs in the 27 checked documents.
  - **Pass 6.** It is the last review pass for checker code. A confirmed present-day defect or hard-rule break is patched, and that patch alone is re-reviewed. Any other zero-occurrence checker item becomes a stated limit of the subset.

## E39 VG-1 last line repairs and final verification (implementation owner, 26 November 2026)

Line repairs from the E37 re-review, over `d4b84b4e`, on lines this slice changed. Files edited: `docs/planning/invest/vertical-delivery.md`, `docs/planning/invest/engine.md`, `_bmad-output/planning-artifacts/epics.md` and this run log. `epics.md` keeps 5,855 lines and the CAT-048 note stays on `:1015`. `docs/engine-contracts.md:92` is not edited (deferred by the coordinator). The test file, the contract and `deferred-work.md` were not edited by me.

**Verified.** The phrase "the build-time element bundle is the compiler's only collider source" occurred on exactly the four lines named: `vertical-delivery.md:187` (column 3) and `engine.md:179`, `:202`, `:211`; neither file has it after the edit. The workbench support plane is built from C# constants at `engine/gpu/WorkshopPhysicsCompiler.cs:95-103`, and element colliders come from declared descriptors through `AddGeometry` (`:125-126`), as E37 recorded. The CAT-048 dependency at `epics.md:1015` reads "CAT-048 is authored purely via declarative .glb asset and .tres catalog definition". `_bmad-output/implementation-artifacts/run-logs/spec-elements-visible-solid-remainder-b.md` cites `prd.md:191` at `:147` and `:203`, and `prd.md:196` at `:552`.

**Same phrase outside the named lines (reported, not edited).** `docs/planning/invest/decisions.md:122`, a line this slice changed, says "the build-time element bundle is the compiler's only collider source (`docs/engine-contracts.md:92`)". `docs/general-engine-design.md:110`, unchanged from `d4b84b4e`, ends "baked into the bundle, which is then the compiler's only collider source". Both carry the same contradiction with the plane; they are left for the coordinator's ruling.

**Document lines** (before → after):
- `vertical-delivery.md:187`, column 3: "(not current: the build-time element bundle is the compiler's only collider source, `docs/engine-contracts.md:92`; …)" → "(not current: the build-time element bundle is the compiler's only element collider source, `docs/engine-contracts.md:92`; …)".
- `vertical-delivery.md:187`, legacy column: "Hardcoded C# collider tables: element colliders come from declared descriptors (`engine/gpu/WorkshopPhysicsCompiler.cs:125-126`); the one built-in collider is the workbench support plane (`:95-103`)" → "Hardcoded C# collider tables: element colliders come from declared descriptors (`engine/gpu/WorkshopPhysicsCompiler.cs:125-126`); the workbench support plane built from C# constants (`:95-103`) is not an element collider, and its status is deferred to the ENGINE-CORE-2d replanning".
- `engine.md:179`, `:202` and `:211`: "the build-time element bundle is the compiler's only collider source" → "the build-time element bundle is the compiler's only element collider source"; nothing else on those lines changed.
- `epics.md:1015` (CAT-048 note, end): "… not `.glb`, and the `.tres` catalog definition in the dependency above reads as "its element package declaration".*" → "… not `.glb`, and the dependency's ".glb asset and .tres catalog definition" reads as "its element's own `.tscn` scene and its element package declaration".*".

**Correction to E37.** E37 said the only other `prd.md` line citation was `spec-elements-visible-solid-remainder-b.md:90`. That slice's run log also cites `prd.md` at `:147` (`prd.md:191`), `:203` (`prd.md:191`) and `:552` (`prd.md:196`). All are history and are left unchanged.

**Checks** (pinned toolchain; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`; no C# changed, so no rebuild; the full round waits for the coordinator):
1. `git diff --check d4b84b4e`: `diff-check-e39.log`.
2. `ArchitectureDocsConsistencyTests`, run last, after this entry, on the E33 build: `arch-docs-e39-docs.log`.

**Coordinator ruling on the two reported lines, and its repair.** Both checks above passed (`diff-check-e39.log` exit 0; `arch-docs-e39-docs.log` 29 passed, 0 failed, 0 skipped). The coordinator then ruled: `docs/planning/invest/decisions.md:122`, a changed line, is IN; `docs/general-engine-design.md:110`, unchanged from `d4b84b4e`, is OUT and joins the coordinator's deferral beside `docs/engine-contracts.md:92`, so it is not edited.
- `decisions.md:122`: "the build-time element bundle is the compiler's only collider source (`docs/engine-contracts.md:92`)" → "the build-time element bundle is the compiler's only element collider source (`docs/engine-contracts.md:92`)"; nothing else on the line changed. After the edit the phrase "only collider source" remains only in `docs/engine-contracts.md:92` and `docs/general-engine-design.md:110`, the two deferred lines.

Checks after this addition (the earlier logs are kept; no rebuild; no full round):
1. `git diff --check d4b84b4e`: `diff-check-e39-b.log`.
2. `ArchitectureDocsConsistencyTests`, run last, after this addition, on the E33 build: `arch-docs-e39-docs-b.log`.

**Final diff scope item and final verification.** Both checks above passed (`diff-check-e39-b.log` exit 0; `arch-docs-e39-docs-b.log` 29 passed, 0 failed, 0 skipped). After the Final diff scope check passed, the coordinator directed one comment repair.
- `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs:17`, the class summary: "one link rule over the authority docs and every Markdown file the slice edited, within a" → "one link rule over the authority docs and every Markdown file the slice edited in <c>37a259c9..d4b84b4e</c>, within a". The range is the one the list's own doc comment gives at `:52` (`git diff --name-only 37a259c9 d4b84b4e -- '*.md'`). No other line of the file changed.

Verification on this source (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. Incremental test-project build after the comment edit: exit 0, 0 warnings, 0 errors; `tests-build-e39.log`.
2. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e39/` (physical path `/Users/aidan/.local/share/tim-toolchain/logs/slice-doc-engine-status/binlogs-e39`), HEAD `d4b84b4e`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e39-a.log`, `capture-e39-b.log`, `capture-e39-c.log`.
3. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4670, model=4, intrusive=0; violations=0`; `maintainability-e39.log`.
4. Registration check filter: **821/0/0** (WorkerAotGuard 17, BundleElements 195, Coverage 83, E2eRunner 97, CuriousContraptions.tests 429); `registration-e39.log`.
5. Full local round (`CSHARP_STATIC_GATE_BINLOGS` = `binlogs-e39/`, the deferred-browser filter from `tools/E2eRunner` only, `RunConfiguration.TreatNoTestsAsError=true`): **7432 passed, 0 failed, `Skipped: 0`** (WorkerAotGuard 17, BundleElements 323, Coverage 132, E2eRunner 256, CuriousContraptions.tests 6704), exit 0, 08:14:11 to 08:22:29 AWST. The log opens with `HEAD d4b84b4ebab8535389888c7f4b91fca231d81be9` and the filter and closes with `HEAD-AFTER d4b84b4ebab8535389888c7f4b91fca231d81be9`; `full-round-e39.log`. 7432 is the expected AC3 total.

The final link run and `git diff --check` wait for the coordinator's E40 and are not run here.

## E40 Coordinator: VG-1 Final diff scope and close note (26 November 2026)

**E37 re-review** (Verification Gap, Forward-Only and Element-Free, each fresh). Every IN finding was patched in E39:
- "only element collider source", with the workbench support plane's status deferred;
- the CAT-048 `.glb` reading;
- `decisions.md:122`;
- the record counts and pointers.

`general-engine-design.md:110`, `engine-contracts.md:92` and `prd.md:196–197`, `:252` and `:260` are OUT and deferred to the ENGINE-CORE-2d replanning (`deferred-work.md`).

**Final diff scope** (a fresh informed auditor, on the full repaired candidate `d4b84b4e` → worktree: 16 files, every hunk). Verdict: **Pass.**
- **Kept.** Every hunk traces to one of:
  - the authorised VG-1 repair;
  - a ruling in the Spec Change Log;
  - a record the contract names.

  Further checks:
  - Nothing is unrelated.
  - Each widening has a recorded ruling.
  - Each acceptance change has authority: AC3 is 7432, 6.5f is restored to its source story, and LEGACY-0d gets a format reading.
  - The lane 1 lines are untouched, and `epics.md` keeps 5,855 lines.
- **Removed:** nothing.
- **Simplification:** no justified opportunity. Each piece of the test that is not Markdig covers something Markdig does not do: the scheme allow-list, HTML attribute reading, path resolution, the tracked-tree model and the bounded `git ls-files` call. Four cosmetic changes were considered and rejected as churn.
- **The auditor's IN items,** all records, are patched:
  - the review count is eleven;
  - the M6 pointer is E39/E40;
  - the 26-file set is qualified with `37a259c9..d4b84b4e`, in the contract, in `deferred-work.md` and in the test's class summary (E39).
- **OUT.** The repair's own edits to `engine.md`, `decisions.md` and `prd.md` add no links and sit outside the checked set. Widening the set is left to the next slice that edits it (`deferred-work.md`).

**Final verification** (E39, implementation owner):
- capture builds `binlogs-e39/`, 0 warnings;
- maintainability gate PASS, with both canaries;
- registration 821/0/0;
- full local round 7432/0/0, `Skipped: 0` (`full-round-e39.log`, HEAD and HEAD-AFTER `d4b84b4e`).

That matches AC3.

**Close note.**
- VG-1 is resolved by a coordinator-authorised, test-only repair, widened by rulings to about 60 document lines. Its review closed under the coordinator's hard cap.
- The contract is within its 8,000-word budget.
- The last steps, after this entry, with no edit after them:
  - `git diff --check d4b84b4e`, to `diff-check-final.log`;
  - `ArchitectureDocsConsistencyTests` on the E39 build, to `arch-docs-final.log`;
  - the commit, as a repair commit on top of `d4b84b4e` through the normal hooks.
- The results of those three are in the coordinator's report.
- Fetch, merge, push and SnapshotApproval wait for the publish token. The merge re-checks the lane 1 lines and `owner-decisions-2026-11-22.md:71`.

## E41 Merge with main `d37c0b98`: citation re-check, link rule and pre-commit verification (implementation owner, 26 November 2026)

The coordinator holds the publish token and started the merge of `origin/main` `d37c0b9830f0a98660338e1a9bcb5cda61530943` into the slice tip `f1a74b43` (uncommitted; `MERGE_HEAD` set; no unmerged paths). The coordinator's own resolutions are listed in its message and are not repeated here. This entry covers the implementation owner's part. Nothing is committed.

**Method.** Main's changed files: `git diff --name-only 37a259c9 d37c0b98` (161 outside `_bmad-output/implementation-artifacts/`; the merge base is `37a259c9`). A read-only script collected every backticked `path:line` citation, with its `:N` continuations, from the lines this slice added or changed (`git diff -U0 37a259c9 f1a74b43`, run logs excluded). It resolved short paths by unique file name and kept the citations whose target main changed: 166. For each, it compared the cited lines at `f1a74b43` with the same lines in the merged worktree. 107 were identical. The other 59 were read by hand against the merged tree.

**Moved by main and corrected** (before → after; each on a line this slice changed):
- `tools/BundleElements/SceneGeometryExtractor.cs`: main deleted 8 lines above the refusals (the data-collider refusal and the per-body bake, `git diff f1a74b43`). The proxy-kind refusal moved from `:111-114` to `:103-106`, the shape switch from `:125-131` to `:117-123`, `Hollow` from `:205-244` to `:197-236` (Tube and Frustum at `:234-235`), and the `BakeProxy` entry check from `:110-114` to `:102-106`. Lines corrected:
  - `:111-114`, `:125-131` → `:103-106`, `:117-123`: `DESIGN.md:87`, `:92`; `docs/engine-contracts.md:88`; `docs/general-engine-design.md:100`, `:104`, `:188`; `docs/gpu-f32-physics.md:71`, `:150`, `:218`; `_bmad-output/planning-artifacts/architecture.md:151`.
  - `:111-114` → `:103-106`: `docs/planning/invest/decisions.md:122`.
  - `:205-244` and `:111-114` → `:197-236` and `:103-106`: `docs/planning/invest/engine.md:179`, `:202`.
  - `:110-114` → `:102-106`: `_bmad-output/implementation-artifacts/deferred-work.md:372`.
- `CuriousContraptions.Bundle/BundleValidator.cs`: the electrical node ratings check (`Electrical`, from `private void Electrical` to the `MaxPower` check) moved from `:865-877` to `:877-889`. `_bmad-output/planning-artifacts/epics.md:4604`: `BundleValidator.cs:865-877` → `BundleValidator.cs:877-889`. The worktree line still had `:865-877` before this edit.
- `tools/e2e/workshop-driver.ts`: the "zero-copy pose ring" comment moved from `:1147` to `:1141`. `deferred-work.md:433`: `workshop-driver.ts:1147` → `workshop-driver.ts:1141`.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs`: the Weight entry moved from `:141` to `:143`. `docs/planning/elements/EL-069-moving-bucket.md:186`: `VisibleSolidColliderTests.cs:141` → `VisibleSolidColliderTests.cs:143`.

**Checked and unchanged.**
- `tools/BundleElements/PackageValidator.cs`: `:88-97` (names), `:116-133` (parent and position) and `:127` keep their positions. Main changed `:118-119` to read `package.Declaration.SceneBodies` directly; the rule they cite is the same.
- `SceneGeometryExtractor.cs:27-28` (the proxy selection) is unchanged.
- ISO §4.4: the only citations into a file main changed were the `KinematicMode.Kinematic` row's (`MechanicalContractValues.cs:7-11`, `:25-26`), and the coordinator deleted that row. Every other §4.4 citation targets a file main did not change or is identical at its range.
- Lane 1 lines: `docs/shared-clock-cadence.md:60` and `epics.md:4466` are still at those numbers, with main's text.
- `owner-decisions-2026-11-22.md:71` is main's 24p line; its link resolves (the link rule below), and 24q follows at `:79`.
- The contract's citations name positions at its baseline `37a259c9` (`baseline_revision`, contract `:86`, `:184`), and the contract is the coordinator's, so none is edited. At the merge, two of them no longer name the same line: `TODO.md:7` and `:17` (contract `:264`) are now `:9` and `:19`, and the per-slice "When" range `delivery-batches.md:246-337` (contract `:177`, `:266`) now ends at `:338` (main inserted `:307`).

**Not edited, reported.**
- `EL-069-moving-bucket.md:186`, on a line this slice changed, also cites `CSharpStaticGateTests.cs:687`–`:703` (the trim list) and `PackageRuntimeParityTests.cs:641` (placement switch) and `:653` (generic list). Those positions held at `e228254a` but not at `37a259c9` or at the slice tip: the targets were replaced by derived membership under S3 (the line says "until S3"). Main did not cause this, and the text is pre-existing, so it is left for the coordinator's ruling.
- Lines this slice did not change still cite positions main moved: `VisibleSolidColliderTests.cs:141` at `docs/planning/elements/CAT-067-weight.md:5`, `EL-069-moving-bucket.md:139`, `EL-070-moving-cage.md:142` and `:187`, and `EL-204-weight.md:43`; `SceneGeometryExtractor.cs:111-114` at `docs/planning/elements/readiness/engine-design-gap.md:41`. Line citations are not links, so the rule leaves them.

**ISO `:143`:** "This view records published main `37a259c9…` for §4.4, the engine features stated as target (DOC-ENGINE-STATUS, 25 November 2026)." → "… (DOC-ENGINE-STATUS, 25 November 2026); the §4.4 citations were re-checked at this slice's merge with published main `d37c0b9830f0a98660338e1a9bcb5cda61530943` (26 November 2026)." The rest of the line is unchanged.

**Link rule.** After an incremental test-project build of the merged tree (exit 0, 0 warnings, 0 errors; `tests-build-e41.log`), `ArchitectureDocsConsistencyTests` passed 29/0/0 over the 27 checked documents (`arch-docs-e41-pre.log`). Main brought no broken link or anchor into them, so there is no link fix.

**Verification on the merged worktree** (pinned toolchain, foreground, `caffeinate -i`; logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`; HEAD `f1a74b43`, `MERGE_HEAD` `d37c0b98`):
1. Capture builds (`docs/delivery-workflow.md:38-53`) into the fresh folder `binlogs-e41/`: playtest, solution, web, `CheckStagedTests.cs`, `VerifyPolicy.cs`; every step exit 0, 0 warnings, 0 errors; `capture-e41-a.log`, `capture-e41-b.log`, `capture-e41-c.log`.
2. Maintainability gate: **PASS**, violations 0 (metrics 0, coupling 0), both canary PASS lines, `Coupling dependencies: contract=4627, model=4, intrusive=0; violations=0`; `maintainability-e41.log`.
3. Registration filter (`docs/delivery-workflow.md:69`, unchanged on main): **829/0/0** (WorkerAotGuard 17, BundleElements 203, Coverage 83, E2eRunner 97, CuriousContraptions.tests 429); `registration-e41.log`.
4. `dotnet run --file tools/anvil/VerifyPolicy.cs`: exit 0, "Policy fixtures: Passed: 78, Failed: 0, Skipped: 0."; `verify-policy-e41.log`. `-- --bmad-check --self-test`: exit 0, "Fixtures: Passed: 37, Failed: 0, Skipped: 0."; `bmad-self-test-e41.log`.
5. `ArchitectureDocsConsistencyTests` on the E41 build, run last, after this entry: `arch-docs-e41-final.log`.

The full round runs on the exact merged commit after the coordinator commits it.

## E42 SnapshotApproval repairs B1 and B2 on the merge `333f64e2` (implementation owner, 26 November 2026)

HEAD `333f64e2` (parents: slice tip `f1a74b43`, `origin/main` `d37c0b98`). Two line-local document edits; no C#, test or build input changed, so nothing was rebuilt.

**B1, `docs/gpu-f32-physics.md:63`.** This re-check is the contract's `DOC:59` merge duty (contract `:105-107`, R5 `:332`, AC1 `:355`): the publish-token holder re-checks this line at the merge and edits it then. `DOC:59` at the baseline is `:63` at HEAD. Three clauses changed; the rest of the line is unchanged.
- Torus clause. Before: "a `TorusMesh` as a Torus (full and solid, or a segment and hollow through `metadata/sweep_degrees` and `metadata/wall_thickness`)." After: "a `TorusMesh` as a Torus (full and solid; `metadata/sweep_degrees` makes it a segment and `metadata/wall_thickness` hollows its tube)." The old wording paired the two keys; the extractor reads them independently (`tools/BundleElements/SceneGeometryExtractor.cs:215` the wall, `:221-222` the sweep, `:223` the bore), so a full hollow torus and a solid segment are both admitted. The new wording follows `docs/hollow-geometry-design.md:36-37`.
- Capsule and hull clause. Before: "Capsule and hull proxies are rejected until their stories admit them." After: "Capsule and hull proxies are refused at bundle time (`tools/BundleElements/SceneGeometryExtractor.cs:103-106`, `:117-123`); a convex profile is the `ENGINE-PROFILE-COLLIDER` target." This is the wording of DOC `:71`, `:150` and `:218`, in `:218`'s sentence form; `:92` states the convex-profile target only. Verified at HEAD: `:103-106` is the `col_capsule`/`col_hull` name refusal (`UnsupportedColliderProxy`), `:117-123` the shape switch whose default refuses any shape other than box, sphere and cylinder.
- Procedural geometry clause. Before: "No procedural geometry or hardcoded shape tables are permitted in C#." After: "No procedural geometry or hardcoded shape tables are permitted in C#; `ui/WorkshopArtwork.cs` and every other procedural mesh building routine in C# are pending removal under LEGACY-PARTART-PURGE (Story 6.5f, `DESIGN.md:70`)." The rule stands; the added clause states today's exception in the words `DESIGN.md:70` uses.
- Checked and left as is: the build-time extraction under Story 9A.16 (`CuriousContraptions.web/CuriousContraptions.web.csproj:63-68` builds `tools/BundleElements`, whose generator writes the bundle); box, sphere and cylinder admission (`SceneGeometryExtractor.cs:117-123`; a `CylinderMesh` needs equal `top_radius` and `bottom_radius`, `:240-252`; equal radial scale, `:135-137`); the Tube and Frustum rules (both caps off and `metadata/wall_thickness`, `:226-228`; equal radii a Tube, tapered a Frustum, `:233-235`).
- "Flat SIMD collider tables" is covered by the status paragraph at DOC `:55`: it sits directly under `## Compilation Model and Tri-Graph Architecture` (`:28`), before `### 1` (`:57`), and states that every flat, contiguous or SIMD-aligned table in this section is the `ENGINE-SOA-SIMD` target. `:63` is inside that section, which runs to `:107`.

**B2, `docs/planning/elements/EL-069-moving-bucket.md:186`.** Before: "One entry beside `VisibleSolidColliderTests.cs:143`; until S3, `CuriousContraptions.slnx`, the trim list at `CSharpStaticGateTests.cs:687`–`:703` and the placement switch at `PackageRuntimeParityTests.cs:641` (generic list `:653`); the binding section …" After: "One entry beside `VisibleSolidColliderTests.cs:143`; one project line in `CuriousContraptions.slnx` (S3, `3a80c6a8`, replaced the trim list and placement switch hand lists with derived membership); the binding section …". The rest of the line is unchanged. S3 was implemented in `e57c225b` and published as `3a80c6a8` (`TODO.md:15`); its commit message keeps one guarded solution line per package (owner decision O1), so the `.slnx` edit stays. `VisibleSolidColliderTests.cs:143` still lands (the Weight entry) at HEAD.

**Reported, not edited.** `git grep -n "until S3" -- docs/` finds 91 other lines in `docs/planning/elements/` (EL-001 to EL-242 and TH-21) with the same "until S3" wording. None is on a line this slice changed (`git diff 37a259c9 f1a74b43 -- docs/planning/elements` touches only EL-065 `:89`, EL-069, EL-156, EL-158 and four readiness files). They are left for the coordinator's ruling.

**Checks** (logs under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`):
1. `git diff --check HEAD`: `diff-check-e42.log`.
2. `ArchitectureDocsConsistencyTests` on the E41 build (`CSHARP_STATIC_GATE_BINLOGS` = `binlogs-e41/`, `--no-build --no-restore`), run last, after this entry: `arch-docs-e42.log`. The class reads the documents from the source tree.
