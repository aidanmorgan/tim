# Engine design gap audit — 22 November 2026

> Dated audit record, kept verbatim as review evidence. Where it proposes wording later changed by decisions (for example the sleep-off comparison at its §6, replaced by coordinator decision 41's before-landing measurement), `epics.md` governs.

Read-only audit of the physics engine at published main `8d1d68aa` (checkout `spec-drafts`). Nothing was built, run or measured. Every performance statement below is reasoning from the code as written; each names the measurement that would confirm it.

Owner requirement audited against: a generalised engine with no custom per-element logic, data-driven components, visuals separate from colliders separate from animations, and a proper high-performance physics engine.

Path shorthand: `SIM/` = `CuriousContraptions.Simulation/`, `ABS/` = `CuriousContraptions.Abstractions/`, `GPU/` = `engine/gpu/`, `DOC` = `docs/gpu-f32-physics.md`, `R-x` = `docs/planning/elements/readiness/batch-x.md`, `DW` = `_bmad-output/implementation-artifacts/deferred-work.md`, `epics` = `_bmad-output/planning-artifacts/epics.md`.

Severity: **C** blocks correctness, **S** blocks scale, **H** hygiene.

## Summary

The engine is element-free in the narrow sense: no engine code names or branches on a package id (a literal search of all 44 package ids returned nothing). The contact, friction, restitution and joint laws are general and carefully built.

It is not the engine `DOC` describes. The structural half of the stated design is absent or nominal:

- There are no SoA tables. Each tick decodes the byte world into a graph of managed objects and writes it back.
- The BVH is a scalar binary tree rebuilt from nothing every substep, fattened by the maximum speed rather than the actual velocity. Only contact pair generation uses it. Sensors, guides, compliant surfaces, airflow, chambers and contact events scan every body, collider or row.
- There are no islands and no sleeping.
- SIMD is limited to joint rows packed four at a time from objects. Contacts, friction, narrowphase and broadphase are scalar.
- The tick allocates managed objects for every contact point.
- Drive budgets and continuous work are found by replaying the whole world's substep many times.

`docs/architecture/element-package-isolation.md:9` says its Current status is the only place that names unmet invariants, and `:147` says none remain. None of the above is listed there. The documents therefore state as present several things the code does not do.

The project has itself recorded a whole-tick p95 of roughly 3 ms against a 2.5 ms target as a retained Fail (`docs/delivery-workflow.md:96`). That is the project's measurement, not mine.

## 1. Stated design versus code

| Feature | `DOC` says | Code does | Sev |
|---|---|---|---|
| SoA layout | Flat contiguous SIMD-aligned SoA tables, 64-byte aligned (`DOC:63`, `:353`) | World is AoS byte records (body 160 bytes, `DOC:128`). Each tick copies the world (`SIM/WorkshopPhysicsEngine.cs:254`) and decodes every body and collider into pooled class instances `SimBody` / `SimCollider` (`:396-486`, `:488-543`; classes at `SIM/WorkshopCollisionKernels.cs:29`, `:93`), held in `List<>` and a `Dictionary<int, SimBody>` (`SIM/WorkshopPhysicsEngine.cs:32-38`). No aligned or columnar storage exists. | S |
| SIMD | 4-wide batching of constraints and manifold points; SIMD broadphase slab tests (`DOC:111`, `:268`) | `Vector128` appears only in joint rows: four rows are gathered from objects into stack arrays, then `SoftConstraint.Solve4` (`SIM/PrismaticConstraintSolver.cs:208-218`, `SIM/RevoluteConstraintSolver.cs:213-221`). The tensile row broadcasts one row into four lanes (`SIM/TensileConstraintSolver.cs:80-82`). `WorkshopCollisionKernels.cs`, `WorkshopConstraintSolver.cs`, `WorkshopFrictionSolver.cs` and `WorkshopDynamicBvh.cs` contain no `Vector128`. The build gate only checks that f32x4 instructions exist in four named types (`SIM/CuriousContraptions.Simulation.csproj:25-26`), which `Vector3` arithmetic satisfies. | S |
| Broadphase structure | Dynamic BVH, incremental SAH rotations, fat AABB by `|v|h + slop`, no update while inside the fat box; 4-wide Quad-BVH; static and dynamic trees (`DOC:192-197`, `:283-297`) | One scalar binary tree (`SIM/WorkshopDynamicBvh.cs:6-17`). `PrepareBroadphase` clears it and reinserts every collider (`SIM/WorkshopPhysicsEngine.cs:808-863`), on every substep and every trial replay (`SIM/WorkshopPhysicsEngine.Motors.cs:33`, `SIM/WorkshopPhysicsEngine.ContinuousReplay.cs:22`, `SIM/WorkshopPhysicsEngine.FiniteWork.cs:136`). Insertion has no rotations (`SIM/WorkshopDynamicBvh.cs:55-141`). Dynamic boxes are fattened by the maximum speed, `64 m/s × h` plus `128 rad/s × reach × h` plus 0.05 m (`SIM/WorkshopPhysicsEngine.cs:810-822`), so at least 0.18 m per side whatever the body is doing. Static leaves are rebuilt too. | S |
| Plane bounds | Any plane | Plane AABB assumes a +Y normal (`SIM/WorkshopPhysicsEngine.cs:825-829`) while the narrowphase uses the plane's own normal (`SIM/WorkshopCollisionKernels.cs` `CollideSpherePlane`, `n = plane.U1`). A tilted plane would be culled wrongly. Already recorded at `DW:59`. | C (latent) |
| Broadphase users | "accelerates all broadphase spatial queries" (`DOC:64`) | Only contact pairs (`SIM/WorkshopPhysicsEngine.cs:866`). Bypasses, each a full scan: residence sensors over all dynamic bodies (`:913`); guides over all dynamic bodies × guides × all colliders (`:1032-1068`); compliant surfaces over all dynamic bodies × all colliders (`:1412`, `:1540`); airflow line of sight over all colliders per sample (`SIM/WorkshopPhysicsEngine.Airflow.cs:149`); chambers over all dynamic bodies × all colliders (`SIM/WorkshopPhysicsEngine.Chambers.cs:86`, `:102`, `:117`, `:145`, `:156-160`); contact triggers and contact work over all rows per contact (`SIM/WorkshopPhysicsEngine.cs:141`, `:165`); contact loads over all contacts per row (`:223-237`); integrity observers over all contacts (`SIM/WorkshopPhysicsEngine.Gas.cs:121`); gas mass over all colliders (`:69`); passage sensors on the host over all participants (`GPU/PassageSensorDeclaration.cs:101-106`). Optical, acoustic and liquid do not exist yet (`epics:4434`, `:4328`, `:4489`), so their queries will be written against whatever exists then. | S |
| Pair filtering | — | Every candidate pair scans every prismatic and revolute joint (`SIM/WorkshopPhysicsEngine.cs:877-886`). | S |
| Pair order | Canonical sort by body pair and feature (`DOC:352`) | Pairs are taken in tree traversal order (`SIM/WorkshopDynamicBvh.cs:145-189`); no sort. | H |
| Narrowphase shapes | Sphere, Box, Plane, Cylinder, Tube, Frustum, Torus; Capsule later | Matches (`SIM/WorkshopCollisionKernels.cs:1999-2028`; hollow pairs through `GPU/HollowContactQuery.cs:50-52`). Capsule and hull proxies are refused at bundle time (`tools/BundleElements/SceneGeometryExtractor.cs:111-114`). There is no convex hull, extruded profile or mesh kind, although `ABS/ColliderDescriptor.cs:8-16` declares `Capsule` and `Mesh` members and a second shape enum exists at `ABS/PhysicsDeclarationValues.cs:12`. | C for consumers (EL-158), H for the duplicate enum |
| Narrowphase cost | "Branchless analytic" | Analytic, but cylinder–box costs 312 distance evaluations per pair and cylinder–cylinder 136 (`DOC:210-211`), run for every candidate the over-fattened broadphase returns. | S |
| Solver | Box2D v3 TGS Soft, 480 Hz substeps, warm starting | Present. 4 solve and 4 relax sweeps per substep (`SIM/WorkshopConstraintSolver.cs:42-43`; loop at `SIM/WorkshopPhysicsEngine.Motors.cs:95-126`). Substeps per commit are 8, 4 or 2 by cadence (`SIM/WorkshopPhysicsEngine.cs:296-297`). Warm starts are keyed by feature identity and rebuilt each substep (`:1325-1335`). All contact rows are solved one manifold at a time, scalar (`SIM/WorkshopConstraintSolver.cs:577-586`). | — |
| Restitution | Per manifold, energy bounded | Coupled over bouncing groups, but a group is silently cut to 8 points (`SIM/WorkshopConstraintSolver.cs:685-689`). `DOC:259` still describes the older per-manifold limitation while `sprint-status.yaml` marks `engine-supported-impact-restitution` done. | C (silent truncation), H (stale text) |
| Islands | DSU islands every substep (`DOC:65`, `:270-272`) | None. A search for island, union-find or disjoint-set finds only the restitution groups above. The only other union-find is the continuous-work component set (`SIM/ContinuousWorkComponents.cs`). | S |
| Sleeping | Islands sleep below 0.01 m/s and 0.02 rad/s for 0.5 s; zero cost (`DOC:273`, `:332-336`) | None. No sleep state, timer, flag or wake path exists in `SIM/`, `engine/`, `ABS/`, Core or Bundle. Every body is integrated and every pair retested every substep. `R-a:64` reached the same finding. | S |
| Parallel dispatch | Work-stealing over worker threads (`DOC:274`) | Single-threaded. The only extra worker is the clock master (`SIM/wwwroot/worker.ts:139`). | S (later) |
| Tunnelling | Speculative contacts for all pairs; 1 cm at 64 m/s (`DOC:258`, `:375`) | Speculative margin is `0.004 + relSpeed × h` including spin reach (`SIM/WorkshopPhysicsEngine.cs:1112-1117`). Only spheres get the conservative-advancement normal (`SIM/WorkshopCollisionKernels.cs:1940-1946`). I found no evidence either way that fast boxes, cylinders or thin spinning bodies meet the envelope; see section 7. | C (unproven) |
| Motion kinds | Dynamic, Static, Kinematic (`DOC:145`) | `RigidMotionKind { Static, Dynamic }` (`ABS/PhysicsDeclarationValues.cs:8`). No kinematic body. | H (doc), C if a consumer needs it |
| Joints | "hinges, sliders, distance links, and ropes", "gear/pulley ratios" (`DOC:148`, `:322`) | Prismatic (5 lock rows, spring, two stops; `GPU/PrismaticRows.cs:6`), revolute (5 lock rows, axial spring, stops, friction, drive; `GPU/RevoluteRows.cs:7-21`), tensile routes, mechanical transmission (`SIM/MechanicalTransmissionSolver.cs`). No spherical, fixed or weld, or distance joint. `CoordinateKind` has only `Revolute` and `Prismatic` (`ABS/PortDescriptor.cs:43-47`). | C for consumers |
| Drives | — | Prismatic: position target with speed, effort and power limits (`DOC:330`). Revolute: velocity only, `RevoluteDriveDeclaration(Joint, TargetSpeed, MaxTorque)` (`ABS/RevoluteConstraintValues.cs:25`). A supplied drive is on or off; it does not debit its source (`DW:211`). | C for consumers |
| Contact events | Any target collision pair (`DOC:151`) | Triggers, contact work and contact loads fire only for a static body against a dynamic body (`SIM/WorkshopPhysicsEngine.cs:1341-1342`, `:226-229`); the owner must be the static side (`:145`, `:169`). A trigger on a moving part cannot fire. | C for consumers |
| Sensors | Convex residence with dwell and velocity threshold; aperture crossing with sub-tick root (`DOC:150`) | Residence tests the body origin point against a box, with absolute speed, once per tick (`SIM/WorkshopPhysicsEngine.cs:929-938`); the frame's own velocity is ignored, so a moving frame mis-measures speed (`R-a:62`). Guides likewise use absolute velocity (`:1071`). Passage sensing runs on the host after the tick, with a per-participant radius (`GPU/PassageSensorDeclaration.cs:89-112`), not in the physics tick. | C on moving frames, S |
| Compliant surface | One of the general force regions | Normal force only, no tangential friction (`DW:205`); four fixed samples (`SIM/WorkshopPhysicsEngine.cs:1555-1556`, `GPU/PhysicsGpuAbi.Constraints.cs:165`); engagement state reserved for every surface × every dynamic body (`GPU/PhysicsWorldLayout.cs:90-94`), which design rule D5 forbids ("no owner×target reservations", `_bmad/custom/element-free-engine-design.md` D5). | C (friction), S (reservation) |
| Allocation in a tick | Reusable scratch; steady demand reuses capacity (`DOC:338-341`) | Every contact point is a new object (`SIM/WorkshopCollisionKernels.cs:305-322`). New `List<ContactPoint>` per box–plane, box–box, cylinder–box and cylinder–cylinder pair (`:474`, `:726`, `:1546`, `:1894`). A new `ContactFeatureSetKey` with an array and a sort per manifold and block per substep (`SIM/WorkshopFrictionSolver.cs:13-31`). Sort lambdas per manifold (`:117`, `SIM/WorkshopHollowKernels.cs:148`). A delegate per broadphase query (`SIM/WorkshopPhysicsEngine.cs:866`). Two `ImmutableDictionary` builders per tick, mutated per sensor–body pair, then frozen (`:249-250`, `:973-976`, `:370`). A `HashSet` per tick (`SIM/WorkshopPhysicsEngine.Motors.cs:17`). Host side per tick: a `Dictionary`, LINQ `Where`/`Single` and closures (`SIM/WorkshopGpuDevice.cs:196-209`, `:329-349`), passage dictionaries and lists (`GPU/PassageSensorDeclaration.cs:96-99`), a full typed read with fresh arrays (`SIM/WorkshopGpuDevice.cs:284-294`), and a new pose slot array (`GPU/WorkshopPoseRing.cs:58-60`). | S |
| Threading and pose ring | Lock-free zero-copy triple buffer written directly by the worker; zero allocation (`DOC:21`) | A three-slot sequence-locked ring (`CuriousContraptions.web/wwwroot/pose-ring.ts:13`, `:89-99`). C# composes a new byte array per tick and the TypeScript host copies it into the shared buffer (`pose-ring.ts:87-99`). It is a copy path, and not allocation-free. Reader side uses a sequence check (`:123`, `:149`). Functionally sound; the "zero-copy" wording is not true of the producer. | H |
| Capacities | Counts compiled from declarations, geometric growth, no ceilings | Holds for tables and scratch (`SIM/WorkshopPhysicsEngine.cs:64-74`, `SIM/WorkshopDynamicBvh.cs:35-47`). Exceptions: the surface × body reservation above, the 8-point restitution cut, the four membrane samples. | S / C as noted |
| Determinism | Stable order, same-build replay | Single thread and list order make same-build replay plausible. Pair order depends on tree shape rather than a canonical sort. Not established by test here; see section 7. | — |
| Drive and continuous-work budgets | — | A motor power budget is enforced by restoring and re-solving the whole world's substep up to about ten more times (`SIM/WorkshopPhysicsEngine.Motors.cs:36-66`). Continuous (airflow) work is solved by whole-world trial substeps inside a restart loop bounded by bodies plus sources, a 32-step scale search and a 24-step bisection (`SIM/WorkshopPhysicsEngine.ContinuousReplay.cs:33-52`, `SIM/WorkshopPhysicsEngine.ContinuousSolve.cs:24-45`, `:61-91`). Neither is confined to the affected bodies. | S |
| Naming | WebGPU compute removed | The CPU engine is still reached through `WorkshopGpuDevice`, `BrowserGpuTransport` (`SIM/BrowserGpuTransport.cs:60`), `PhysicsGpuAbi`, `GpuBodyId` and `engine/gpu/`. | H |

## 2. Element-specific code in the engine

No package id, `new PartId("…")` literal, named `PartId` constant or lesson id selects behaviour anywhere outside `elements/`. `PartRegistry`, `PartDefinition`, `ElementPortCatalogue`, the palette and icons are bundle-driven. What remains is product shape baked into shared contracts.

| # | Finding | Evidence | Forward-only refactor | Sev |
|---|---|---|---|---|
| E1 | Activation kinds named after products. Invariant 5 of the isolation authority allows kinds selected by declared data, but these names are products while the declaration side already uses capability names. | `ABS/ActivationValues.cs:18` (`Counter`, `StateLatch`, `LevelIndicator`), `:24` `StateLatchRequest`; `ABS/CompiledRuntimeContracts.cs:244-245` `GetCounterTarget`, `StateLatchPhase`; messages at `GPU/ActivationNetwork.cs:231`, `:240`; host call `SIM/WorkshopGpuDevice.cs:208`; capability names at `ABS/SignalContractValues.cs:13` | Rename to the capability names (`PulseCount`, `Memory`, `Level`) through compiler, wire, read and the TypeScript mirror; delete the old names. | H |
| E2 | Boolean supply node fixed at two inputs and five named operations. | `ABS/SupplyContractValues.cs:26` (`FirstIn`, `SecondIn`); `ABS/ElectricalContractValues.cs:25`; `ABS/PhysicsPresentationRead.cs:26`; evaluation `GPU/SupplyRoute.cs:375-376`, `:391-395` | One N-input boolean node whose input list and truth function are declared; delete the First/Second terminals and the two-input state enum. | H now, C for any 1- or 3-input part |
| E3 | Socket glyph enum is a per-product table in shared code. | `ABS/PortDescriptor.cs:38` (`Extend`, `Retract`, `Extended`, `Retracted`, `Set`, `Reset`, `First`, `Second`); switch `ui/WorkshopConnections.cs:36-44`; drawings `ui/WorkshopIcons.cs:42-53` | The package ships its socket glyph asset as it ships its icon; delete the enum members and the table. | H |
| E4 | Player-facing strings name parts. | `ui/Workshop.cs:884`, `:371`; `ui/WorkshopPuzzle.cs:80`; `GPU/WorkshopConnections.cs:150`; `GPU/SupplyRoute.cs:70` | Capability wording, or text from declared display names or level content. | H |
| E5 | Tube mouth snapping is one family's recipe: exactly two ends and engine constants taken from CAT-048. | `engine/TubeMouthSnap.cs:23-25`, `:98-99`; `ABS/SceneGeometry.cs:37` `TubeEnd { Start, End }`; `engine/PartDefinition.cs:60-62`, `:71` | A declared list of snap sockets, each with its own capture distance and angle; delete `TubeEnd` and the constants. | H now, C for a three-mouth part |
| E6 | One compliant surface, one residence sensor and one planar guide per element; fixed four-sample recipe; presentation finds the surface by owner. | `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:77`, `:27`, `:32`; `SIM/WorkshopPhysicsEngine.cs:1555-1556`; `GPU/WorkshopDeclaredPresentation.cs:622`; `GPU/CompiledIdentityMap.cs:173` | List-valued capabilities with local ids (design rule D1); sample layout becomes declaration data; bindings name the instance. | C for any two-surface or two-sensor part |
| E7 | Presentation hard-wires axes instead of the declared joint frame. | `engine/MachinePart.cs:162`, `:172`, `:231` | Derive from the declared joint frame; delete the fixed axes. | C for any joint not on those axes |
| E8 | Presentation driver named after one part's wheel. | `ABS/ElementPresentation.cs:25` `CommittedWheelAngle`; `GPU/WorkshopDeclaredPresentation.cs:398`, `:611` | Rename to the guide angle it is; delete the old name. | H |
| E9 | Chamber region is always a cylinder with launcher vocabulary. | `SIM/WorkshopPhysicsEngine.Chambers.cs:177-183` (shape fixed to `Cylinder`), `:141-166` ("muzzle" clearance) | Region shape becomes a declared shape kind; exit direction a declared axis. | H until a second consumer |
| E10 | Gas store coupled to a sphere only. | `ABS/GasCouplingValues.cs:10`; `ABS/IsobaricGas.cs:133` | Belongs to `ENGINE-GAS-NETWORK`. | C for that story |
| E11 | Run control assumes "exactly one driven hinge" per part; a puzzle has a single inventory entry. | `ABS/ParameterDescriptor.cs:48`; `GPU/PuzzleCatalogue.cs:36`, `:90-91` | Controls name their drive by local id; inventory becomes a list. | C for consumers |
| E12 | `BodyTargetKind.AllPayloads` builds a product role into the target filter. | `ABS/ContactWorkDeclarationValues.cs:31` | A declared participation mask (`DOC:146` already promises "participation filter masks"). | H |
| E13 | TypeScript enums are hand-mirrored and have drifted. | `CuriousContraptions.web/wwwroot/workshop-observation.ts:41` lacks `Mechanical = 4` (`GPU/WorkshopConnections.cs:11`); `:12` lacks `Geometry = 4` | Generate the mirrors from the C# owners in the build. | C (latent) |
| E14 | Hand-maintained package list. | `CuriousContraptions.slnx:16-59` | Closed: S3 O1 (owner answer, 21 November 2026): keep one guarded solution line per package; `.slnx` has no wildcard (MSB4014), and no second S3 slice follows. | H |

Tables kept outside packages: none found at runtime. `tools/Coverage/CapabilityTaskRequirements*.cs` holds about a thousand rows keyed by element id; it is a planning tool, not shipped.

## 3. Layer separation

| # | Finding | Evidence | Sev |
|---|---|---|---|
| L1 | Physics does not read art at runtime. Colliders come from the bundle; no simulation, bundle, core or abstraction file references Godot. | `engine/PartDefinition.cs:51`; `tools/BundleElements/SceneGeometryExtractor.cs:9` | clean |
| L2 | Two packages use visible art meshes as their collider proxies, and the extractor allows it. | `elements/LinearPusher/linear_pusher.tscn:50-54` (`col_Foot`, `col_Barrel` are visible `MeshInstance3D`); `elements/WoundSpring/wound_spring.tscn:85-126`; `tools/BundleElements/SceneGeometryExtractor.cs:111` has no hidden check | H |
| L3 | Goals are evaluated from committed reads, not from rendered or animated state. | `GPU/WorkshopGoalEvaluator.cs:7`; `GPU/BrowserWorkshopClient.Ui.cs:90`, `:141` | clean |
| L4 | No binding targets a node that is or parents a `col_` node. But five packages move art by a binding while its static collider stays where it was, so what the player sees and what collides diverge during the animation. | ImpactSwitch button (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:16` against `switch.tscn:85-86`); Bumper head scale (`BumperElementDeclaration.cs:18`); Cannon sleeve (`CannonElementDeclaration.cs:66`); Clutch plates (`ClutchElementDeclaration.cs:26`); Trampoline bed (`TrampolineElementDeclaration.cs:53`, by design a force bed) | C against "visible = solid" unless each is an accepted exception |
| L5 | Fan blades, ring and post have no collider, and the blade spin is driven by a presentation curve rather than the hinge body in two of three supply modes. | `elements/Fan/fan.tscn:65`, `:94`, `:103-121`; `FanElementDeclaration.cs:84-88` | C against "visible = solid" |
| L6 | The simulation waits for the animation worker and the renderer. An install (Run, Reset, Step, completion) sends Prepare to both and waits up to 250 ms for their receipts; commit proceeds only when they arrive, and a failure after the physical commit retires the session. No animation value enters the solver, sensors or goals, but a stalled cosmetic worker can abort a physics transition. | `SIM/Program.Schedule.cs:101-103`, `:152-157`, `:179-180`, `:219-222`, `:232-236`; `CuriousContraptions.Animation.Worker/Program.cs:215-225` | C (lifecycle), against "nothing flows back" |
| L7 | Passage sensing is physics done outside the physics tick, in a helper named `RenderPosition`. | `GPU/PassageSensorDeclaration.cs:80`, `:103`; called from `SIM/WorkshopGpuDevice.cs:243` | H |

## 4. Per-tick cost model

Nothing here was measured. Symbols: B bodies, D dynamic bodies, C colliders, P candidate pairs, K contact points, M manifolds, J joints, T trigger rows, W contact-work rows, L load rows, S residence sensors, G guides, F compliant surfaces, A airflow sources, R airflow receiver samples, H chambers, X driven surfaces, N activation nodes, E activation edges, V events in a tick. "Per substep" means 2, 4 or 8 times per committed tick, and again for every trial replay.

| Phase | Cost as coded | Grows super-linearly? | Evidence |
|---|---|---|---|
| World copy and decode, per tick | O(world bytes) + O(B + C + J) | No | `SIM/WorkshopPhysicsEngine.cs:254`, `:324-332` |
| Broadphase rebuild, per substep | C insertions, each a descent of an unbalanced tree: O(C log C) typical, O(C²) for adversarial order | Yes in the worst case | `:808-863`; `SIM/WorkshopDynamicBvh.cs:55-141` |
| Pair query and filter | O(P × J). P is inflated because every dynamic box is padded by at least 0.18 m per side; in a dense scene P tends toward D × neighbours-within-0.4 m. | Yes | `SIM/WorkshopPhysicsEngine.cs:870-888` |
| Narrowphase | O(P), constant up to 312 distance evaluations per pair, allocating | Through P | `:1099-1121` |
| Friction prepare | O(K × X) plus, per manifold, points × patches × members | Yes | `SIM/WorkshopFrictionSolver.cs:124-144` |
| Solve and relax | 8 sweeps × (J rows + M blocks + friction patches). Linear in K and J; each normal block enumerates active sets of up to 4 rows; scalar | No | `SIM/WorkshopPhysicsEngine.Motors.cs:95-126` |
| Restitution | Each group root scans all manifolds | Yes, roots × M | `SIM/WorkshopConstraintSolver.cs:677-683` |
| Motor budgets | Up to about 11 whole-world substeps when any prismatic motor is active and over budget | Multiplier on everything above | `SIM/WorkshopPhysicsEngine.Motors.cs:36-66` |
| Continuous work | Whole-world substeps × restarts (≤ B + A) × components × scale steps (≤ 32) × bisection (≤ 24) | Yes | `SIM/WorkshopPhysicsEngine.ContinuousReplay.cs:33-52`; `SIM/WorkshopPhysicsEngine.ContinuousSolve.cs:24-91` |
| Guides | O(D × G × C) | Yes | `SIM/WorkshopPhysicsEngine.cs:1032-1068` |
| Compliant surfaces | O(F × D × 4 × C), twice per substep; storage F × D | Yes | `:1406-1460`, `:1532-1548`; `DW:199` |
| Airflow | O(A × R × C) material sweeps | Yes | `SIM/WorkshopPhysicsEngine.Airflow.cs:103-161` |
| Chambers | O(H × D × C) per substep and again per tick; exit clearance O(C²) | Yes | `SIM/WorkshopPhysicsEngine.Chambers.cs:80-166`; `SIM/WorkshopPhysicsEngine.cs:1002`, `:356` |
| Contact events | O(K × (T + W)) | Yes | `:138-216`, `:1338-1343` |
| Contact loads, integrity | O(L × K), O(observers × K) | Yes | `:220-237`; `SIM/WorkshopPhysicsEngine.Gas.cs:117-130` |
| Residence sensors, per tick | O(S × D), each with an immutable-dictionary write | Yes | `SIM/WorkshopPhysicsEngine.cs:894-978` |
| Passage sensors (host) | O(sensors × participants), allocating | Yes | `GPU/PassageSensorDeclaration.cs:96-112` |
| Activation network (host) | O(V × N) to match events, O(V²) duplicate check, O(occurrences × E) propagation | Yes | `GPU/ActivationNetwork.cs:335-342`, `:505`, `:689` |
| Publication | Validate the whole candidate, build a full typed read, encode it, compose and copy a pose slot | No, but a large constant every tick | `SIM/WorkshopGpuDevice.cs:222-294`; `SIM/Program.cs` `EmitRead` |

Because nothing sleeps, a level's cost is set by how many parts are placed, not by how many are moving.

Measurements each claim needs:

1. Tick time against C for a grid of resting boxes, for the broadphase and sleeping claims.
2. Candidate pairs against contacts actually produced, for the fattening claim.
3. Managed bytes allocated per tick at steady state (`GC.GetAllocatedBytesForCurrentThread`), for the allocation claim.
4. Tick time against the number of fans or sensors with the body count fixed, for the scan claims.
5. Substep replays per tick with one motor at its power limit, and with one airflow source loading a hinge, for the replay claims.
6. A profile of the publication path per tick.

## 5. Missing general capabilities

Collected from the element audits and readiness summaries, deduplicated. "Story" is the owning story if one exists in `epics`.

| Capability, stated element-free | Needed by | Names in use | Story |
|---|---|---|---|
| A drive's committed work debits its source; an exhausted source opens the route | CAT-005, CAT-042, CAT-051, CAT-018, EL-196, EL-199, EL-209, EL-211 | `ENGINE-SUPPLY-DEMAND` (`R-j:83`), `engine-supply-drive-demand` (`R-h:89`), `powered-part-demand-drain` (`DW:209`), `engine-powered-part-demand-drain` (`R-c:68`) | none |
| Island sleeping | CAT-001, CAT-014, every resting part | unnamed (`R-a:64`) | none |
| Sensors and guides relative to a moving frame, candidates from the broadphase | CAT-004 | unnamed (`R-a:62`) | none |
| Contact events found by compiled index, on any body pair | CAT-015, CAT-063 | unnamed (`R-a:63`) | none |
| Airflow and compliant-surface candidates from the broadphase | EL-042, EL-209, CAT-065 | unnamed (`R-h:99`, `R-c:72`) | none |
| Tangential friction on a compliant surface | CAT-065 | `ENGINE-COMPLIANT-FRICTION` (`R-c:70`), `compliant-surface-friction` (`DW:203`) | none |
| Spherical joint | CAT-069, EL-115 | addition to `ENGINE-ASSEMBLY-JOINTS` (`R-d:53`) | parent only (`epics:4456`) |
| Revolute position drive and rest window; one-way hold and indexed stop on a coordinate; winch | EL-061, EL-056, EL-057, EL-063 | G1–G3 of `ENGINE-ASSEMBLY-JOINTS` (`R-i:106-108`) | parent only |
| Force-bounded mechanical couplings (belt, chain, mesh, tooth ratio) | EL-200, EL-201, EL-218 and neighbours | `ENGINE-MECHANICAL-LINKS` (`R-j:84`) | none |
| Tension reacting on a dynamic guide; declared tensile materials; span cutting by blades | EL-207; EL-067, EL-205; EL-066, EL-068 | `ENGINE-TENSILE-GUIDE` (`R-j:85`), `ENGINE-CONNECTION-MATERIALS` (`R-i:78`), `ENGINE-SPAN-CUTTER` (`R-i:90`) | none |
| Convex extruded-profile collider on a moving body | EL-158 | `ENGINE-PROFILE-COLLIDER` (`R-j:88`) | none |
| Parameters binding joint frames, ports and child colliders; art and collider present by parameter | CAT-019 | `ENGINE-PARAMETER-BOUND-FRAMES` (`R-c:66`), `ENGINE-PARAMETER-SELECTED-PARTS` (`R-b:87`) | none |
| Signal nodes: saturating count, count reset, edge, threshold over a committed scalar, rearming timer, supply self-edge, contact set by parameter, passage speed predicate | CAT-020, EL-183, EL-181, EL-182, EL-043, CAT-060, EL-236, CAT-013, CAT-024–027, EL-198, EL-109 | eight separate `ENGINE-…` names (`R-b:77-83`, `R-h:81-95`, `R-new:82`, `R-j:82`) | none |
| Axial gas chamber and nozzle; prismatic directional damping, stroke observation, travel presentation | CAT-010 | four names (`R-d:15`) | none; overlaps `ENGINE-GAS-NETWORK` |
| Gas network, optical, acoustic, field force, fracture, goals, liquid, thermal, radiation, ignition, granular, characters | as listed in `epics:4423-4552` | one name each, with sprint-key variants | exist, all Backlog |
| Deflection read and height-field binding; wrapping presentation mapping | CAT-065, CAT-019 | G6, G2 (`R-c:71`, `:67`) | none |

Name reconciliation:

- **Supply drain.** Use story key `ENGINE-SUPPLY-DEMAND` with sprint key `engine-supply-drive-demand`. The two already appear together (`R-h:89`, `R-j:83`), and both state the capability. Retire `powered-part-demand-drain` and `engine-powered-part-demand-drain`: they name a part rather than a capability, and the second occurs once. Re-point the `DW:209` anchor and the CAT-005, CAT-018, CAT-042, CAT-051, EL-196, EL-199, EL-209 and EL-211 references in the same edit. `R-b:44` and `R-h:89` currently disagree on which name wins.
- **Contact setting.** `engine-supply-parameter-contact` and `engine-supply-contact-setting` are one capability; keep the second.
- **Connection variants.** `engine-connection-materials` and `engine-mechanical-link-kinds` are two mechanisms; keep both, state the boundary in each.
- **Nozzle.** `engine-axial-gas-nozzle` and `ENGINE-GAS-NETWORK` both claim the nozzle; give it to the gas network story.
- **Size binding.** `ENGINE-PARAMETER-BOUND-FRAMES` also covers the unnamed carrier gap at `R-j:89`.

Contradictions that affect planning: `engine-supported-impact-restitution` is outstanding in `epics:107` and `DW:127` but done in `sprint-status.yaml`; `engine-cap-block` is blocked at `docs/planning/invest/vertical-delivery.md:160` and published at `epics:126`; ENGINE-CORE-2d promises convex hulls while `DOC:59` refuses hull proxies; hollow colliders are "static only" in `docs/hollow-geometry-design.md:43` and not so in `R-i:157`; `ENGINE-ASSEMBLY-JOINTS` lists as missing two things the code has and is asked to carry about ten unrelated capabilities.

## 6. Proposed engine stories

Ordered by dependency. Every story is element-free and takes the shared acceptance at `epics:4414-4419`. Tolerances come from `docs/testing-guidance.md`; no hashes. Under the project rule, measured timing, allocation and device numbers are qualified at P0-034; each story below says what is asserted in the slice and what waits for that gate.

### Should precede further element delivery

**1. DOC-ENGINE-STATUS (new, documents only).**
- Outcome: the documents stop describing absent features as present.
- Contract: every unmet claim in section 1 moves to the isolation authority's Current status with its owning story below; `DOC` states each as target, not fact.
- Acceptance: each `DOC` sentence about SoA, SIMD batching, Quad-BVH, dual tree, islands, sleeping, parallel dispatch, kinematic bodies, canonical pair order and the zero-copy ring either cites the running code path or is listed as open with an owner.
- Deletes: the stale supported-impact limitation text at `DOC:259`; the "No unresolved … finding remains" sentence.
- Unblocks: every element spec's "Engine conformance" section, which today cites features that do not exist.

**2. ENGINE-SPATIAL-QUERIES (new; corrects Story 1.4's "brute-force bounding loops … permanently deleted").**
- Outcome: one persistent broadphase serves every spatial question.
- Contract: a static tree built at Run; a dynamic tree updated only when a collider leaves a box fattened by its actual velocity; bounds correct for a plane of any normal; three queries (box overlap, segment, shape against region) returning candidates in a canonical order; a compiled body-to-collider range and a compiled jointed-pair set.
- Acceptance, in the slice: for seeded scenes of mixed shapes, each query returns exactly the set a direct all-pairs reference returns; a tilted plane supports a resting body; contact, sensor, guide, airflow, chamber and compliant-surface outcomes on the existing lessons are unchanged in goal, event kinds, owners and causal order. At P0-034: tick time against collider count for resting and moving scenes.
- Deletes: the per-substep rebuild; the maximum-speed fattening; every scan listed in section 1 "Broadphase users"; the joint scan in `AddCandidatePair`.
- Unblocks: CAT-004 on a moving frame, CAT-065 sampling, EL-042, EL-209; and is a prerequisite for `ENGINE-OPTICAL`, Story 14.0, `ENGINE-FIELD-FORCE` and `ENGINE-LIQUID`, which would otherwise each add another full scan.

**3. ENGINE-CONTACT-OBSERVATIONS (new).**
- Outcome: contact triggers, contact work, contact loads and integrity observers work on any pair of bodies and are found by index.
- Contract: each row names its owning collider or body by compiled handle; a contact reports to the rows of both sides; the lookup is by handle.
- Acceptance: a trigger owned by a dynamic body fires once when struck above its threshold and not below it; a control with the owner static gives the same occurrence; a scene with many unrelated rows gives the same events as the same scene without them.
- Deletes: the static-side requirement and the per-contact row scans.
- Unblocks: CAT-015, CAT-063, and any trigger or pad on a moving part.

**4. ENGINE-SENSOR-VOLUMES (new; amends Story 3.1).**
- Outcome: residence, passage and guide evaluation happen inside the physics tick, against collider shapes, relative to the sensor's frame.
- Contract: a sensor is a declared region on a frame body; occupancy is shape overlap from story 2's region query; speed and approach are measured relative to the frame; passage crossing is evaluated on the committed motion inside the tick; several sensors and guides per element, each with a local id.
- Acceptance: a body resting in a region carried by a moving frame is reported resident after its dwell, and is not when it moves relative to the frame faster than the limit; a body whose origin is outside but whose shape overlaps is handled as the declaration states; two sensors on one element report independently.
- Deletes: the origin-point test, absolute-speed tests, the host-side passage tracker and its `RenderPosition` helper, the one-sensor and one-guide properties (E6).
- Unblocks: CAT-004, EL-109 (speed predicate becomes a declared field), CAT-002.

**5. ENGINE-ISLANDS-SLEEP (new).**
- Outcome: bodies joined by contacts, joints, transmissions and tensile routes form islands; an island at rest sleeps and costs nothing until woken.
- Contract: thresholds as `DOC:273`; wake on contact with an awake body, a drive or supply change, a force region, a request, a cut or an edit; a sleeping body's committed pose and velocity do not change; sleep state is in the committed read; Reset and Save/Load restore it exactly. Drive-budget and continuous-work trials are confined to the island they affect. The restitution group has no point cap.
- Acceptance: a settled stack reports asleep within a stated time and its pose does not move while asleep; a ball rolled into it wakes it and the resulting capture, toppling and event order match the same run with sleeping disabled in the test declaration, within position tolerances justified by the receiving clearance; a body on a conveyor, in an airflow or on a powered hinge does not sleep; a motor at its power limit in one island leaves a distant island's outcome unchanged. At P0-034: tick time for a large resting scene.
- Deletes: whole-world trial capture and restore; the 8-point restitution cut (`SIM/WorkshopConstraintSolver.cs:685-689`). Absorbs the story-less `engine-resting-stack-settling` (`epics:486`).
- Unblocks: the "parts that rest can sleep" line every element spec must now make; CAT-001, CAT-014; campaign levels of realistic size.

**6. ENGINE-ONE-WAY-INSTALL (new; needs an owner decision).**
- Outcome: a slow or failed animation worker or renderer cannot abort or retire a physics transition.
- Contract: the simulation publishes each install with its epoch; consumers adopt by epoch; presentation readiness is a presentation concern.
- Acceptance: with the animation consumer withheld in a node harness, Run, Step, Reset and completion commit and the physical read advances; when the consumer returns it adopts the current epoch.
- Deletes: the receipt wait and its 250 ms timeout in `SIM/Program.Schedule.cs`.
- Decision needed: design guide section 1 says not to rebuild unaffected worker and ring infrastructure. The owner should confirm this coupling is in scope.

**7. ENGINE-SUPPLY-DEMAND (new; one name, see section 5).**
- Outcome: a supplied drive's committed work is debited from its source each tick; an exhausted source opens the route.
- Contract: the existing finite-energy ledger (`ABS/FiniteEnergyBalance.cs`) gains drive consumers; several drives on one source share it by one declared rule; an unlimited source is a declared kind.
- Acceptance: stored energy falls by the drive's committed work within the ledger's stated residual; the drive stops when the source is empty and the route reads open; two drives on one source stop together; an unlimited source never drains; Reset restores the authored charge.
- Deletes: the binary-supply limitation recorded at `DW:211`.
- Unblocks: CAT-005, CAT-018, CAT-042, CAT-051, EL-196, EL-199, EL-209, EL-211.

**8. ENGINE-CAPABILITY-INSTANCES (new; refactor).**
- Outcome: the shared contracts stop encoding one product's shape.
- Contract: findings E1, E2, E3, E5, E7, E8, E9, E11, E12, E13 and the rest of E6: capability names for activation kinds; N-input boolean node; socket glyphs and snap sockets as declared lists; presentation from declared joint frames; region shape as a declared kind; controls and inventory by local id; participation masks; generated TypeScript mirrors; the bundle tool refuses a visible collider proxy (L2).
- Acceptance: a qualification fixture declaring two compliant surfaces, a three-input boolean node, a three-socket snap part and a hinge on a rotated axis compiles and behaves by its declaration; every existing lesson keeps its goals and event order; a visible proxy is refused with a typed reason.
- Deletes: each old name and fixed slot in the same slice.
- May be split by contract family; none of the parts is large.

### In roadmap order, before their first consumer

**9. ENGINE-JOINT-KINDS (new; split out of `ENGINE-ASSEMBLY-JOINTS`).** Spherical joint; fixed joint; revolute position drive with rest window; one-way hold and indexed stop on a coordinate. `ENGINE-ASSEMBLY-JOINTS` keeps only player-made connections and commanded release. Acceptance per kind: the locked freedoms hold within the contact-slop scale under load, the free ones move, drive effort is bounded by the declared limit. Unblocks CAT-069, EL-115, EL-056, EL-057, EL-061.

**10. ENGINE-MECHANICAL-LINKS, ENGINE-TENSILE-EXTENSIONS (new).** Force-bounded couplings with declared ratio and slip limit; tension reaction on a dynamic guide, declared tensile materials, span cutting by a declared blade pair. Unblocks EL-200, EL-201, EL-207, EL-067, EL-205, EL-066, EL-068, EL-063.

**11. ENGINE-SIGNAL-NODES (new; merges eight audit names).** Saturating count, count reset, edge, threshold over a committed scalar, rearming timer, self-edge rule, contact set by a parameter. One story, because all are rows of the same compiled network. Unblocks CAT-013, CAT-020, CAT-024–027, CAT-060, EL-043, EL-181–183, EL-198, EL-236.

**12. ENGINE-COMPLIANT-FRICTION, ENGINE-PARAMETER-BOUND-FRAMES, ENGINE-PROFILE-COLLIDER (new, small).** Each before its one consumer (CAT-065; CAT-019; EL-158).

**13. Existing engine-first stories, amended.** `ENGINE-GAS-NETWORK` takes the axial chamber and nozzle, the prismatic damping and stroke observation, and removes the sphere-only store (E10). `ENGINE-OPTICAL`, Story 14.0, `ENGINE-FIELD-FORCE` and `ENGINE-LIQUID` each gain one line: candidates come from story 2's queries, and the capability's per-tick work is bounded by the bodies inside its declared regions. The rest keep their documented order (`docs/planning/invest/vertical-delivery.md:19-30`).

### At the performance gate (P0-034), after islands exist

**14. ENGINE-TICK-ALLOCATION (new).** Pooled contact points, manifolds and keys; no per-pair lists; no immutable-dictionary rebuilds; interaction state in sparse reusable tables; host tick path without LINQ, closures or per-tick arrays; pose slot written into reused storage. In-slice acceptance is behavioural equality on existing scenes. The measured criterion, zero managed bytes per steady-state tick at stable topology, is REQ-10 and belongs at P0-034; it is deterministic and cheap, so the owner may prefer to assert it in the slice.

**15. ENGINE-SOA-SIMD (new).** Body, collider and contact state in columnar tables compiled once at Run and kept across ticks; contact rows solved four at a time within an island. In-slice acceptance: existing behaviour within the game-grade envelope. Measured criterion at P0-034: the 2.5 ms p95 tick target (REQ-04) on the frozen workloads, which the project currently records as failing.

**16. ENGINE-PARALLEL-ISLANDS (new, optional).** Only if story 15 misses the budget. `DOC:274` should say so.

**17. ENGINE-TUNNEL-QUALIFICATION (new, tests first).** Native scenarios for the `DOC:375` envelope with non-sphere bodies: a box, a cylinder and a thin plate at 64 m/s and at high spin against a 1 cm wall, and two fast bodies meeting. If any fails, the fix is a general swept test in story 2's segment query, not a per-shape rule.

Stories 1 to 8 should precede further element delivery. The reason is cost of delay rather than present breakage: every element delivered now adds declarations, tests and tuned values on top of whole-scene scans, static-only contact events, origin-point sensors and one-per-element capability slots, and each of the engine-first stories already queued (optical, acoustic, field force, liquid) will add another scan if story 2 is not there first. Stories 2, 3, 4 and 5 change observable behaviour at the edges (moving frames, dynamic owners, resting bodies), so specs written before them will need rework.

## 7. What I could not establish

- Any timing, allocation rate or pair count. Nothing was run.
- Whether same-build replay is bit-stable, and whether native tests and the WASM worker agree; I read no determinism test.
- Whether fast non-sphere bodies tunnel. The speculative margin looks adequate for translation; rotation of thin bodies and two fast bodies are unproven here.
- Whether the BVH degenerates in practice. Insertion has no rotations, but collider order may be benign.
- Whether hollow colliders on dynamic bodies and hollow-against-hollow pairs are qualified; the pair code exists (`GPU/HollowContactQuery.cs:50-52`) and the documents disagree.
- The internals of the guide solver (`SIM/GuideComponent.cs`, `SIM/GuideQr.cs`), retention, extrusions, rotary airflow, the supply resolver (`GPU/SupplyRoute.cs:321`) and the event record producer were skimmed, not read; their costs are not in section 4.
- `RemoveInternalFeatures` and `GroupRigidSupports` (`SIM/WorkshopHollowKernels.cs`) loop over contacts and manifolds; I did not determine whether they are quadratic.
- Whether L4 and L5 (moving art over static colliders; uncollided fan parts) are accepted exceptions to "visible = solid".
- Whether any element needs a kinematic body, or whether the word in `DOC:145` should simply be removed.
- The Godot main-thread renderer's batching (instanced draws by mesh and pipeline, `DOC:70-71`) was not audited.
- Test projects and the browser suites were excluded.
