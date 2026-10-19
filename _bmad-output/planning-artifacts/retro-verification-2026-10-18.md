# Retro-verification of done stories 6.5b–9.4 (18 Oct 2026)

This is a tracked copy of the independent, read-only retro-verification report of 18 Oct 2026. It is the planning evidence for **Epic 9R: Retro-Verification Repair** in [epics.md](epics.md#epic-9r-retro-verification-repair). The original lived in a temporary scratchpad. This copy keeps every finding and its evidence, and adds §H, which says where each defect goes.

- **Auditor:** independent and read-only. No builds, tests, Chrome, preview or repo edits were run.
- **Method:** `git show HEAD:<path>`, `git grep`, `rg` and file reads.
- **Evidence base:** HEAD `8f682f8` ("deliver Stories 6.5b-9.4"). Every story in scope landed in that one commit, so git history cannot date when a defect appeared.
  - Line numbers come from `git show HEAD:<path>`, and every citation also names the symbol. Code changes after HEAD, so **the symbol is authoritative**.
  - The working-tree edits that belong to Story 9.4b (in flight) were noted but not audited.
- **Source precedence:**
  1. acceptance criteria in `epics.md`;
  2. the element spec `CAT-*.md` §5;
  3. `spec-*.md`.
  
  Where a spec narrows the source, the source wins unless an owner decision amends it.
- **Classes:**
  - **P (Proven):** a test that would fail if the behaviour broke.
  - **W (Weakly proven):** a loose assertion; a setter, imported solution or native-only proof standing in for a Chrome AC; or no negative/control case.
  - **U (Unproven):** no test.
  - **C (Contradicted):** the code shows the AC cannot hold.
  - **D (Deferred):** an open owner-sanctioned deferral. It counts as U but is not a defect.
- **DoD gates that cannot be checked read-only** (dotnet test, Chrome pass counts, anvil, build warnings) are marked "unverifiable (record claims X)". They are marked C only when HEAD evidence shows the AC cannot hold.

**Owner decisions recorded after this report (18 Oct 2026):**
- The authored asset format is the element's own Godot `.tscn` scene, not `.glb`. This resolves defect #9 by amending Stories 6.5c, 6.5d and 6.5e.
- CAT-052 Q2: the Pressure Plate gains an ActivationOut level output that drives Signal lamps. This resolves defect #5; the coordinator decided it under owner delegation.

## 0. Cross-cutting findings

| # | Finding | Evidence | Affects |
| --- | --- | --- | --- |
| X1 | **The "Anvil 0 warnings" DoD item gives no signal for C#.** `anvil check --changed` scans only web and script files. A probe `.cs` file with two empty catch blocks returned 0 warnings. | `review-9-4-state-latch.md` row A2 (`anvil-cli.log`, `anvil-csharp-control`) | Every story's "anvil clean" claim on C# |
| X2 | **675 added lines carry `Half` (binary16) since baseline `9107148`: 458 production (48 removed) and 217 test.** Largest files: `WorkshopCanonicalColliders.cs` 295, `MachinePart.cs` 36, `WorkshopPhysicsCompiler.cs` 32, `WorkshopWireFlatBuffers.cs` 29, `WorkshopPhysicsEngine.cs` 13, plus the new element collider tables. | `git diff 9107148 HEAD -- '*.cs' \| rg '^\+.*\bHalf\b'` | 6.6–9.4. The reviews say "inherited lanes", but these stories grew those lanes. |
| X3 | **No native or Chrome suite proves that the bundle or the packages are consumed.** `PartRegistry` loads `.tres`, and nothing reads `parts/elements.bundle.json`. | Architecture audit A13/A14 | 6.5b, 9.2b, every "package" claim |
| X4 | **Silent collider fallbacks.**<br>- Generic capture uses `def.Colliders.Count > 0 ? def.Colliders : WorkshopCanonicalColliders.For(id)`.<br>- `PartDefinition.EnsureExtracted` silently returns empty colliders when the scene is missing or is not a `Node3D`. | `engine/MachineWorld.Gpu.cs:118,120` (`CaptureInstance`); `engine/PartDefinition.cs:73-96` (`EnsureExtracted`) | 6.5c, 6.6–6.11, 8.2, 9.1–9.4 |
| X5 | **The cumulative serial Chrome DoD (#4) was not met by 6.9–9.3.** At HEAD six suites fail: cat-005 5, cat-015a 1, cat-015a-sidekick 1, cat-015b 3, cat-062b 5, cat-023b 1. | `spec-9-4-state-latch.md` §6 gate-6 and §7 round 2 (116/132) | 5.2, 6.2, 6.3, 6.5, 8.1 (Story 9.4b) |
| X6 | **Every "animation follows event" proof runs through main-thread pumps that diff reliable reads, not through the event ring.** | Audit D1–D3 | 6.8, 9.2, 9.3, 9.4 |

## Story 9.2b: modular isolation, pure C# WASM solver, event ring

Review `review-9-2b-modular-isolation.md` passed it on 17 Oct.

| AC | Class | Evidence | Gap |
| --- | --- | --- | --- |
| Each element is its own project and references only Abstractions | C | 9 delivered elements have no package. | A15 |
| Core engine has zero element references, `PartId` constants or element assets | C | `CuriousContraptions.Simulation.csproj:28-29` compiles `../engine/gpu/*.cs` plus `engine/PartId.cs`; `engine/PartId.cs:21-99`; `ui/WorkshopIcons.cs` | A1/A2/A3/A10 |
| A build pipeline produces a bundle that the runtime loads | C | Hand list at `tools/BundleElements/Program.cs:21-36`; the bundle is lossy; no reader; no icons or i18n; `slnx` does not build `elements/*`. | A13/A14 |
| 100% of simulation in C# WASM, **including electrical MNA** | W / C | JS physics was removed, but no test asserts it. **No MNA exists**: `git grep -i 'nodal\|MNA'` returns 0. Electrical is graph reachability plus drag-coded heuristics (`PrismaticConstraintSolver.cs:241-249`). | MNA clause contradicted |
| `worker.js` is a ~50–80 line shell | C | 347 lines. It contains `extractReadIdentity`, occurrence reservation and the raw stall flag. | B1/B4/C2/D3 |
| Pose ring zero-copy; event ring multi-consumer | P / C | Pose ring: `WorkshopPoseRingTests.cs`. Event ring: unit tests only. The only runtime `Consume` call passes no buffer (`WorkshopGpuDevice.cs:152`). | D1/D2 |
| Balanced coupling | C | Wholesale compile; `postMessage` stall flag; `CCGPU` channel | A1/C2/C4 |
| Named Chrome suites | unverifiable (record claims 45/45) | Only a subset ran; the contact-work suites that regressed were not run. | 9.4b |

Counts: P 0 / W 1 / U 0 / C 6. **Pass invalid.**

## Story 9.4: State Latch

Counts: P 13 / W 2 / U 0 / C 3, plus 1 D. **The Pass is valid for behaviour.** There are three side contradictions:

1. The spec AC2 example ("pressure plate wired to `SetIn`") is impossible, because the plate has no ActivationOut (`WorkshopConnections.cs:62-66`).
2. `LatchSet` is never published at runtime (`WorkshopGpuDevice.cs:152`).
3. The compiler reads `WorkshopCanonicalColliders.LatchColliders` (`WorkshopPhysicsCompiler.cs:281`) rather than the latch scene.

Also:
- Only latch- and counter-switched gates moved to compiled routes; direct and plate gates still use a global per-tick heuristic.
- New binary16 values were added (`WorkshopPhysicsCompiler.cs:274-285`).
- `is WorkshopLatch` admission and `SwitchedSupplyFeed` `is WorkshopLatch or WorkshopCounter` (`WorkshopPhysicsCompiler.cs:307-309`) remain.

## Story 6.5b: dynamic part discovery and extensible PartId

| AC | Class | Evidence |
| --- | --- | --- |
| `Discover()` uses `DirAccess.GetFilesAt`, no hard list | W | `engine/PartRegistry.cs:15`. The test asserts `Count == 24` only. Silent skips and a clock exemption at `:30-31,37-38`. |
| Strongly typed **extensible** `PartId` | C | 24 constants (`engine/PartId.cs:21-44`) and closed switches `ToNumericId`/`FromNumericId` (`:46-103`). `ToNumericId` silently returns `_ => 0` (`:72`). |
| `WorkshopPartKind` eliminated | P | |
| `PartRegistry.Create` without a switch | P | `PartRegistry.cs:60-77` (but capture still switches: `MachineWorld.Gpu.cs:93-129`) |
| Free palette populated from discovery | C | Hard `_canonicalOrder` (`WorkshopInventory.cs:49-75`). `GenerateFree` silently drops undiscovered ids (`:83`). Tennis and Balloon cannot be built. |
| Cumulative Chrome | W | 3 of about 20 suites |

Counts: P 2 / W 2 / U 0 / C 2. **Pass invalid.** The review claims "12 canonical parts"; its test asserts 24.

## Story 6.5c: collider and port-locator extraction

| AC | Class | Evidence |
| --- | --- | --- |
| Compiler extracts colliders from the `.glb` scene graph at Run, with no element branching | C | No `.glb` exists. Extraction runs at capture for 6 ids only (`MachineWorld.Gpu.cs:118,120`). Inline per-type collider tables live in `WorkshopPhysicsCompiler.cs:12, 43-285`. |
| `Loc_Port_<Id>` locators resolved dynamically and matching port ids | C | Runtime ports come from `WorkshopPorts.For` and `LocalPosition` (`WorkshopConnections.cs:19-34,97+`). Locators are presentation-only (`MachinePart.cs:575`). Names don't match: battery `Loc_Port_out` vs `Supply`, bumper `Loc_Port_in` vs `PowerIn`. Springboard has none. |
| Child branches share articulated transforms | U | `GltfColliderExtractionTests.cs:157` covers static transforms only. |
| Asset colliders rest stably in Chrome; no element ids in dispatch | C | `PartId.PoweredGate` at `WorkshopPhysicsCompiler.cs:186,288`; `OfType<Workshop*>` |
| Visual and collider separation | P | `GltfColliderExtractionTests.cs:133,487,526` |
| Reset, Save/Load in Chrome | W | No suite places an extraction-driven part. |

Counts: P 1 / W 1 / U 1 / C 3. **Pass invalid.** The review itself admits "legacy collider compilation paths remain functioning in parallel".

## Story 6.5d: ball and obstacle conversion

| AC | Class | Evidence |
| --- | --- | --- |
| A self-contained `.glb` in `parts/models/` | C | No `.glb`, no `parts/models/`. The scenes are `.tscn` with inline meshes. **Resolved by owner decision: the `.tscn` scene is the format.** |
| `.tres` plus generic `MachinePart` | W | `MachinePart.BuildDeclarative` has 24 `PartId ==` arms. |
| Bespoke classes deleted | P | |
| Zero shims, dual loaders or fallbacks | C | X4; `PartRegistry` silent skips; `EnsureExtracted` silent empty |
| Levels simulate identically and solve in Chrome | W | No before/after equivalence |
| `cat-001/014/064/003/066/054/004` pass | C | Only `cat-014` exists. Tennis and Balloon cannot be built. |

Counts: P 1 / W 2 / U 0 / C 3. **Pass invalid.**

## Story 6.5e: mechanism and fixture conversion

| AC | Class | Evidence |
| --- | --- | --- |
| `.glb` with `col_*` and `Loc_Port_*` | C | No `.glb`. Locators are partial and misnamed; only `delay` has both. **Asset format resolved by owner decision (`.tscn`).** |
| Moving sub-assemblies as child branches | W | Branches exist; the bodies are made by the compiler. |
| `.tres` plus generic `MachinePart` | W | A4 |
| Bespoke classes deleted | P | |
| Zero shims | C | X4 |
| `spring_forward`, `domino_effect`, `delayed_signal` solve in Chrome | W | `spring_forward` is in no e2e suite and not in the driver's `WorkshopLevel` union (`workshop-driver.ts:310`). |
| `cat-015/023/062/063/035/022/005` pass | C | `cat-063`, `cat-035` and `cat-022` don't exist. 015a/015b, 023b and 005 fail at HEAD. |

Counts: P 1 / W 3 / U 0 / C 3. **Pass invalid.** The raw cat-062 output is copied (§D).

## Story 6.5f: PartArt / WorkshopPartKind purge

| AC | Class | Evidence |
| --- | --- | --- |
| Files deleted; grep returns 0 | P | |
| All part-kind switches eradicated | C | `PartId` switch tables at `MachinePart.cs:40`, `MachineWorld.Gpu.cs:93`, `WorkshopConnections.cs:19`, `WorkbenchCapacity.cs:35`, `WorkshopWire.cs:396`, `WorkshopWireFlatBuffers.cs:851`, `WorkshopCanonicalColliders.cs:20`, `WorkshopConstruction.cs:59`. 44 `== PartId.X` branches. `ui/WorkshopConfiguration.cs:12-15`, `ui/WorkshopGuidance.cs:273`. |
| Cumulative Chrome | W | 6 of about 22 suites; copied cat-062 output |
| Zero shims | C | X4 |

Counts: P 2 / W 1 / U 0 / C 2. **Pass invalid.**

## Story 6.6: straight pipe (CAT-048a). Weak.

- **W:** passage (`cat-048a.test.ts:34`, final range only).
- **U:** the oversized ball stops at the entrance.
- **U:** visual/collider separation.
- **W:** exact Reset (`:100`, ranges, no pre-Run snapshot; the review claims bit-for-bit).
- **W:** Save/Load (Chrome `:136`, range only; the native codec is exact).

Counts: P 2 / W 3 / U 2 / C 0.

## Story 6.7: clear pipe level (CAT-048b). Weak.

- **W:** solved (`cat-048b.test.ts:60`, one angle, no speed variants).
- **U:** high speed, no bounce, within 1200 ticks.
- **W:** Reset and Save/Load (`:88`, `:107`, ranges, depend on earlier test state).

Counts: P 3 / W 2 / U 1 / C 0.

## Story 6.8: Ball Detector (CAT-002). Weak.

- **W:** "Forward pass lights the lamp once". The predicate (`cat-002.test.ts:81-84`) scans **all** animation targets, so the detector's own pulse passes it. "Once" is not counted.
- **W:** reverse and glancing checks never prove a crossing happened (`:93`, `:132`).
- **W:** the second forward crossing uses Reset plus a new Run (`:171`), not an in-run clearance.
- **W:** Reset bit-for-bit is claimed but not asserted.
- **U:** disabled participant, and the 0.35 s fade.

Counts: P 2 / W 4 / U 1 / C 0.

## Story 6.9: 45° pipe bend (CAT-049)

| AC | Class | Evidence |
| --- | --- | --- |
| `gentle_bend` solved; no added speed | W | `cat-049.test.ts:60`, with no speed assertion |
| Straight-to-bend and bend-to-bend rotated passages through **typed mouth snapping** | C | No snapping code in `engine/` or `ui/` (`git grep -i snap` matches only strings) |
| Missed-connection control | P | `cat-049.test.ts:42` |
| Rim, oversize and sidewall controls; Precise route; icon, rails, collars | U | |
| Nudging | D | |
| Exact Reset and Save/Load | W | Ranges in Chrome; native codec exact (`PipeBend45Tests.cs:142`) |

Counts: P 1 / W 2 / U 3 / C 1 / D 1. **Pass invalid.** The review reports 851/851 tests, the same total as 6.8, despite claiming 12 new tests.

## Story 6.10: 90° pipe bend and joined pipe route (CAT-050)

| AC | Class | Evidence |
| --- | --- | --- |
| `quarter_bend` solved | W | `cat-050.test.ts:60` |
| **`joined_pipe` solved** | C | Exists only at `content/puzzles.json:7930`. No `WorkshopPuzzleId` member (`WorkshopPuzzle.cs:5`), no driver level, no test. |
| Straight/bend joins | C | No snapping |
| Missing-connection control | P | `cat-050.test.ts:42` |
| Rotated, oversize and rim controls; bore, collars, rails, icon | U | |
| Depth-error nudging | D | |
| Exact Reset and Save/Load | W | Ranges; native exact (`PipeBend90Tests.cs:142`) |

Counts: P 1 / W 2 / U 2 / C 2 / D 1. **Pass invalid.**

## Story 6.11: Funnel (CAT-030). Weak, with snapping contradicted.

- **C:** the outlet is snapped to a pipe or bend; no snapping exists (`cat-030.test.ts:218`).
- **W:** gather (off-centre case at 0.70 m where the spec says 0.75; no fast or rotated case).
- **W:** no scripted transport (inspection only).
- **W:** outside drop blocked (`:48`, `px > 3.0` only).
- **U:** rim, oversize and mesh match.
- **D:** air and light integrations.
- **W:** Reset is run-to-run at 0.01 m, not against the pre-Run pose.

Counts: P 0 / W 4 / U 1 / C 1 / D 1.

## Story 6.9b: unbounded collider capacity

| AC | Class | Evidence |
| --- | --- | --- |
| No collider-count validation | P | `WorkbenchCapacity.cs:68-106` |
| Any construction within the 32-instance budget is admitted | C | `ColliderCapacity = 2048` (`PhysicsDeclarations.cs:168`). 32 × `pipe_bend_90` or 32 × `funnel` = 32 × 72 + 1 = **2,305 colliders**. It is admitted at Build, then the `PhysicsSceneDeclaration` constructor throws at Run (`PhysicsDeclarations.cs:211`). The test covers only 30 instances (880 colliders). |
| `AbiOffsets()` exported; `worker.js` binds offsets dynamically | C | `worker.js:34-37` still holds `5408/11552/15680/16192`. Nothing calls `AbiOffsets` (`Program.cs:248`). The review's quoted grep doesn't match the file. |
| `ColliderCapacity` 2048; heavy constructions compile | P | |

Counts: P 3 / W 0 / U 0 / C 2. **Pass invalid.**

## Story 8.1: Battery (CAT-005)

| AC | Class | Evidence |
| --- | --- | --- |
| Battery drives an Electric Motor | D | Owner acceptance conflict (the Motor is Story 11.1) |
| Disconnecting the cable stops it | D | Redefined by Story 9.4c (owner item h, CAT-005 Q6) |
| `cat-005` 100% | C (HEAD) | 5 of 7 fail at HEAD. Treated as a "diagnostic fixture" since review-9-1. |
| Enabled battery powers its consumer | C (HEAD) | `cat-005.test.ts:105` fails |
| UI construction; disabled control; enum enable; Reset; Save/Load | P | `cat-005.test.ts:37,175,199,229-233`; `ElectricalSupplyTests.cs` |
| Consumer→battery refused; loop N17 | U | Native only |
| 0 build warnings | C | IL2125, from Story 8.1's `Google.FlatBuffers` |

Counts: P 5 / W 0 / U 1 / C 3 / D 3. **Pass invalid as recorded.**

## Story 8.2: Powered Gate (CAT-051). Weak, with one functional C.

- **W:** "fully opens" (`cat-051.test.ts:149`, `maxBladeY > 4.2` against 4.55 m).
- **U:** ramp and pipe recipe.
- **P:** unpowered control (`:104`).
- **P:** Delay link rejected (native).
- **U:** bounded drive.
- **U:** closing stops at a ball.
- **D:** mid-stroke cut (Story 9.4c).
- **U:** rotated passage, horizontal mount, retry.
- **U:** Reset closes exactly (`driver.reset()` with no assertion).
- **W:** Save/Load replay.
- **C:** "Gate drive follows its own supply".
  - Direct mode is compiled from the battery's **authored** enable only (`WorkshopPhysicsCompiler.cs:190-206`, `bladeDrag 0.0625`).
  - At tick time `batteryPowered` becomes true when **any** source is enabled (`WorkshopPhysicsEngine.cs:767-776`).
  - (a) A gate wired to a battery authored Disabled never opens when the player presses in-run **Enable supply**.
  - (b) With two batteries, disabling a gate's own battery during Run leaves the gate open.
  - `PoweredGateTests.cs:198` pins the drag code.
- **W:** native "canonical matches scene" (`PoweredGateTests.cs:112`).

Counts: P 2 / W 3 / U 5 / C 1 / D 1.

## Story 9.1: Pressure Plate (CAT-052)

| AC | Class | Evidence |
| --- | --- | --- |
| Plate wired to a Signal lamp; a resting body lights it | C | The plate declares only `PowerIn`/`Supply`; the lamp only `ActivationIn` (`WorkshopConnections.cs:62-66,87-90`). **Resolved by coordinator decision (CAT-052 Q2): the plate gains an ActivationOut level output.** |
| Roll-off deactivates immediately | U | Only `ContactLoadSensorTracker` is tested (`PressurePlateTests.cs:305`), and **no production code calls it**. |
| A bounce gives no sustained activation | U | CAT-052 Q3 open |
| UI construction | P | `cat-052.test.ts:142` |
| Resting mass at or above the threshold closes the contact | W | `cat-052.test.ts:228` |
| **`minimum_mass` governs activation** | C | `_plateLoaded = currentPlateLoad >= 0.5f` (`WorkshopPhysicsEngine.cs:856`) ignores the instance value. The plate is found by half-extents 0.9/0.05/0.9 (`:834,:838`). One global `_plateLoaded` drives every plate-mode gate (`PrismaticConstraintSolver.cs:248`), so plates cross-talk. No UI editor (`ui/WorkshopConfiguration.cs:12-16`). |
| Below threshold, side/underside, exactly at threshold, two-body sum, rotated | U (runtime) | Native tests exercise only the unused tracker (`PressurePlateTests.cs:193-300`). |
| Thresholds 0.1/16 accepted, out of range rejected; Save/Load `minimum_mass` | P | `PressurePlateTests.cs:146-159,366` |
| Run/Reset Empty, slate | U | |

Counts: P 3 / W 1 / U 4 / C 2. **Pass invalid.** review-9-1 cites nine files that don't exist (`engine/src/parts/*`, `engine/src/physics/*`, `ui/src/workbench/workshop-driver.ts`) and a test that doesn't exist.

## Story 9.2: Pulse Counter (CAT-020)

- **P:** N=3 emits on the third ball (`cat-020.test.ts:235`; `CounterTests.cs:294-299`).
- **P:** two crossings don't emit (`:176`).
- **W:** Reset clears the count (native `Clear()` only; the Chrome Reset at `:435` has no assertion).
- **C (story time):** the suite passed only through an in-engine detector-crossing counter shortcut (spec-9-4 D11/D12; fixed in 9.4).
- **W:** a held trigger doesn't recount.
- **W:** targets 1 and 9 (no UI editor).
- **P/U:** Save/Load (partial-count clause untested).
- **P:** supplied contact closes at the target and holds until Reset.
- **U:** three gold dots.
- **C:** AGENTS no-fallback. `inst.Threshold == 0 ? DefaultTargetCount : (int)Math.Round(inst.Threshold)` (`WorkshopWireFlatBuffers.cs:896`) is a silent default plus a float→int coercion.

Counts: P 4 / W 3 / U 2 / C 2. **Pass invalid at story time.**

## Story 9.3: System Clock (CAT-017)

- **W:** 5 pulses in 5.0 s (native only, `ClockTests.cs:189`; Chrome runs 2.2 s and 4.2 s with a target of 3).
- **U:** Pause halts emission (`cat-017.test.ts:267` checks a ball, not the clock).
- **U:** Reset restores phase 0 (`:278` checks the blade only).
- **W:** construction (target 3, which the UI can't change).
- **P:** no-supply and deadline power-loss checks; interval range; Save/Load; Clock→Counter (`ClockTests.cs`).
- **U:** lamp flashes and pendulum.

Counts: P 4 / W 2 / U 3 / C 0. **Pass invalid.** review-9-3 cites four nonexistent files, including the test file credited with "10 new tests" (`CuriousContraptions.tests/SystemClockTests.cs`, `elements/SystemClock/ClockState.cs`, `ClockRenderer.cs`, `ClockInspector.cs`), and a port, range and pulse width that don't match the code.

## D. Review-record integrity

| Review | Problem | Evidence |
| --- | --- | --- |
| 6.5e, 6.5f, 6.6, 6.7, 6.8 | The raw cat-062 output is presented as fresh runs, yet the first three test timings are identical to the nanosecond across five reviews on different days (`14729.544167ms`, `11075.602542ms`, `12073.702125ms`). Each block lists 4 ✔ lines against "tests 7". | `rg -F 14729.544167 _bmad-output/implementation-artifacts/` returns 5 files. |
| 9.1 | Nine linked source files don't exist; wrong values; a phantom test name | review-9-1:23-28,77-78 |
| 9.3 | Four linked source files don't exist | review-9-3:14,24-29,77 |
| 9.2b | AC1/3/4/5 PASS against contradicting code | Audit G8 |
| 6.9 | 851/851 (the same as 6.8) despite 12 new tests; quoted test names don't match | review-6-9 §4 |
| 6.5b | Claims 12 canonical parts; its test asserts 24 | review-6-5b:29,47 |
| 6.9b | The quoted `worker.js` grep doesn't match the file | review-6-9b:79-91 |
| 6.6, 6.10 | Claim bit-for-bit or exact Reset that no assertion checks | review-6-6:37,67; review-6-10:111 |
| All | "anvil 0 warnings" carries no C# signal | X1 |
| 6.6–6.11, 8.2, 9.1, 9.3 | "Exact Reset restoration" marked PASS, but no suite compares against a pre-Run snapshot. The pipe suites reuse earlier test state. | e2e files |

## E. Spot checks, 5.x and 6.1–6.5

| Story | Finding | Home |
| --- | --- | --- |
| 5.1, 6.1 | Suites pass at HEAD | — |
| 5.2 | cat-023b fails at HEAD (test 4 transport; test 1 lamp history) | 9.4b |
| 6.2, 6.3 | cat-015a, sidekick and cat-015b fail at HEAD | 9.4b |
| 6.4 | AC names `cat-062a.test.ts`; the suite is `cat-062.test.ts` | 9A.0 doc fix |
| 6.5 | cat-062b: 5 failing tests (Receiver `supportHeight`; `Goal.Body` encoder at `WorkshopWireFlatBuffers.cs:640`) | 9.4b |

## F. Per-story counts

| Story | P | W | U | C | D | Recorded Pass |
| --- | --- | --- | --- | --- | --- | --- |
| 6.5b | 2 | 2 | 0 | 2 | 0 | invalid |
| 6.5c | 1 | 1 | 1 | 3 | 0 | invalid |
| 6.5d | 1 | 2 | 0 | 3 | 0 | invalid |
| 6.5e | 1 | 3 | 0 | 3 | 0 | invalid |
| 6.5f | 2 | 1 | 0 | 2 | 0 | invalid |
| 6.6 | 2 | 3 | 2 | 0 | 0 | weak |
| 6.7 | 3 | 2 | 1 | 0 | 0 | weak |
| 6.8 | 2 | 4 | 1 | 0 | 0 | weak |
| 6.9 | 1 | 2 | 3 | 1 | 1 | invalid |
| 6.9b | 3 | 0 | 0 | 2 | 0 | invalid |
| 6.10 | 1 | 2 | 2 | 2 | 1 | invalid |
| 6.11 | 0 | 4 | 1 | 1 | 1 | weak (snapping C) |
| 8.1 | 5 | 0 | 1 | 3 | 3 | invalid as recorded |
| 8.2 | 2 | 3 | 5 | 1 | 1 | weak (functional C) |
| 9.1 | 3 | 1 | 4 | 2 | 0 | invalid |
| 9.2 | 4 | 3 | 2 | 2 | 0 | invalid at story time |
| 9.2b | 0 | 1 | 0 | 6 | 0 | invalid |
| 9.3 | 4 | 2 | 3 | 0 | 0 | invalid |
| 9.4 | 13 | 2 | 0 | 3 | 1 | valid (behaviour) |
| **Total** | **50** | **38** | **26** | **36** | **9** | |

## G. Prioritised defects (report numbering)

| # | Sev | Defect | Evidence (HEAD) |
| --- | --- | --- | --- |
| 1 | High (player) | Pressure Plate ignores `minimum_mass`, is detected by size, and one global `_plateLoaded` drives every plate gate (cross-talk). The native tests prove an unused tracker. | `WorkshopPhysicsEngine.cs:834,838,856`; `PrismaticConstraintSolver.cs:248`; `ContactLoadSensorTracker` has no production caller |
| 2 | High (player) | Direct-gate supply is compiled from the battery's authored enable. The in-run toggle never recompiles (`WorkshopSimulation.cs:52-65`). Any enabled battery powers every direct gate. | `WorkshopPhysicsCompiler.cs:190-206`; `WorkshopPhysicsEngine.cs:767-776` |
| 3 | High | Typed mouth snapping does not exist (required by 6.9, 6.10, 6.11 and the CAT-051 recipe). | `git grep -i 'snap\|mouth' HEAD -- engine ui` returns only strings; locators are used only by `MachinePart.TryGetPortTransform` |
| 4 | High | The `joined_pipe` level is unplayable. | `content/puzzles.json:7930` only; no `WorkshopPuzzleId` member (`WorkshopPuzzle.cs:5`) |
| 5 | High | The Pressure Plate cannot be wired to a Signal lamp (9.1 headline AC). | `WorkshopConnections.cs:62-66,87-90` |
| 6 | High | Collider overflow: 2,305 > 2,048, admitted at Build, throws at Run. | `PhysicsDeclarations.cs:168,211`; `WorkbenchCapacity.cs:68-106` |
| 7 | High (process) | Review evidence is not credible (copied cat-062 logs, phantom files, 9.2b G8). | §D |
| 8 | Med | AC-named suites never existed: cat-001/003/004/054/064/066 (6.5d) and cat-063/035/022 (6.5e). | `ls tools/e2e` |
| 9 | Med | No `.glb` or `parts/models/`; the glTF ACs of 6.5c/d/e are contradicted. | `git ls-files \| rg '\.glb$'` returns 0 |
| 10 | Med | 9.3 Pause/Reset never proven; 5 pulses not shown in Chrome. | `cat-017.test.ts:267,278` |
| 11 | Med | Silent collider fallbacks: the canonical table; `EnsureExtracted` returning empty silently; the latch ignoring extraction. | `MachineWorld.Gpu.cs:118,120`; `PartDefinition.cs:73-96`; `WorkshopPhysicsCompiler.cs:281` |
| 12 | Med | The Free palette hard list silently drops parts. `ToNumericId` maps unknown ids to 0. `MachinePart.TryGetPortTransform` resolves sockets through a string alias chain (`"PowerIn"`→`"power_in"`→`"in"`; `"Supply"`→`"out"`). | `WorkshopInventory.cs:49-83`; `PartId.cs:72`; `MachinePart.cs:560-568` |
| 13 | Med | Counter `target_count` and plate `minimum_mass` have no UI editor. Counter decode silently defaults 0 → 3 and coerces float to int. | `ui/WorkshopConfiguration.cs:12-16`; `WorkshopWireFlatBuffers.cs:896` |
| 14 | Med | Exact Run/Reset is never asserted against a pre-Run snapshot in cat-048a/048b/049/050/030/051/052/017/002. | §D |
| 15 | Med | cat-002's forward-pass assertion is vacuous. | `cat-002.test.ts:81-84` |
| 16 | Med | cat-005 treated as diagnostic-only and failing; IL2125. | spec-9-4 §6 |
| 17 | Low | 675 added `Half` lines. | X2 |
| 18 | Low (gate) | anvil scans no `.cs`. | review-9-4 A2 |
| 19 | Low | 9.2b's MNA clause and its ~50–80 line `worker.js` claim are contradicted. | `git grep -i nodal` returns 0 |
| 20 | Low | Stories marked done with open clauses: 6.9, 6.10 (nudging), 6.11 (integrations), 8.1 (Motor), 8.2 (CAT-051 Q6), 9.1 (CAT-052 Q2/Q3). | epics.md |

## H. Disposition (18 Oct 2026)

| Report # | Owner |
| --- | --- |
| 18 (and X1) | Story 9R.0 (C# static gate) |
| 14 | Story 9R.1 (exact Run/Reset snapshot assertions) |
| 6 | Story 9R.2 (collider table sized at Run) |
| 2 | Story 9R.3 (direct gate follows its own supply) |
| 1 | Story 9R.4 (per-plate compiled load rows) |
| 5 | Story 9R.5 (plate ActivationOut level output) |
| 3 | Story 9R.6 (typed mouth snapping) |
| 4 | Story 9R.7 (`joined_pipe` playable) |
| 6.5e `spring_forward` W | Story 9R.8 |
| 15 | Story 9R.9 |
| 10 | Story 9R.10 |
| 8 | Stories 9R.11–9R.17 (cat-001, 004, 054, 066, 063, 035, 022); cat-064 and cat-003 go to Story 12.1 |
| 7 | Story 9R.18 (independent re-verification and dated review addenda) |
| 9 | Owner decision: the `.tscn` scene is the authored format (6.5c, 6.5d, 6.5e amended) |
| 11 | Story 9A.16 (adds `EnsureExtracted` silent-empty and the latch's extraction bypass) |
| 12 | Story 9A.14 (palette), 9A.15 (`_ => 0` rejection), 9A.17 (socket alias chain) |
| 13 | Story 9A.2 (Counter decode default and coercion), 9A.40 and 9A.41 (generic editor proof of non-default values) |
| 16 | Story 9.4b |
| 17, X2 | Story 9A.44 |
| 19 | Story 9A.0 records 9.2b's invalid acceptance, including the MNA clause; `worker.js` → 9A.1–9A.3, 9A.23 |
| 20 | Story 9R.18 records each open clause; owner case review of 8.1's Motor clause |
| 9.4 side contradictions | Plate→SetIn example: 9A.0 spec record (Story 9R.5's level output drives Signal lamps only). `LatchSet` runtime: 9A.22. Latch colliders: 9A.16. |
