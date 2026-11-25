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
