---
title: 'Story 9R.4: Each Pressure Plate Honours Its Own Minimum Mass (Reopened Story 9.1)'
type: 'bugfix'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r4-each-pressure-plate-honours-its-own-minimum-mass-reopened-story-91'
  - '{project-root}/docs/planning/elements/CAT-052-pressure_plate.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9r-3-direct-gate-own-supply-route.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Report #1. The live plate is a size heuristic with one global flag.
- `WorkshopPhysicsEngine` treats any static collider with half-extents 0.9/0.05/0.9 at Ty 0.03 as "the plate" (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:834,838`).
- It counts contacts with a loose filter: normal Y ≥ 0.7, local y 0.02–0.35 (`:850`).
- It sets one global `_plateLoaded = load >= 0.5f` (`:13,38,856`), ignoring `WorkshopPressurePlate.MinimumMass` (`engine/gpu/WorkshopPressurePlate.cs:10`). That value is never compiled.
- The flag gates every plate-band gate (blade drag 0.09375: `engine/gpu/WorkshopPhysicsCompiler.cs:195-206`; `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:248`), so plates cross-talk.
- `ContactLoadSensorTracker` (`engine/gpu/ContactLoadSensorDeclaration.cs:52-145`) has no production caller. `PressurePlateTests.cs:193-305` therefore tests dead code.
- Its declaration stores `MinimumMass` and `MinimumAlignment` as `Half` (`:23-24`). CAT-052's alignment of 1 − 10⁻⁶ rounds to 1.0, and non-dyadic thresholds lose the instance value.

**Approach:** Follow the Story 9.4 compiled-table pattern.
1. At Run, each plate compiles its own contact-load row (CAT-052 §2), carried in the physics document like the orientation sensors.
2. Each tick the engine evaluates every row through `ContactLoadSensorTracker` and writes its phase and mass into the candidate.
3. A capability node, `ContactLoadSource`, commits the plate level into the activation checkpoint.
4. The plate's supplied contact is a `Switched` row (Battery → plate → gate) in the Story 9R.3 `SupplyRouteDeclaration` table.

## Boundaries & Constraints

**Always:**
- The row values come from the instance and from CAT-052 §2:
  - frame = the plate body;
  - region x, z ∈ [−0.9 − d, 0.9 + d] and y ∈ [0.08 − d, 0.08 + d], with d = 0.0001 (z uses 10⁻⁶);
  - normal +Y;
  - alignment 1 − 10⁻⁶;
  - `minimum_mass` as f32 kg;
  - the comparison is inclusive (≥).
- Each body counts once, and bodies sum.
- Data flows one way: physics read (tick N) → committed level → that route's `SupplyClosed` at tick N+1.

**Never:**
- Collider-size, drag or element-id detection.
- A global plate flag.
- A per-plate solver branch.
- A default threshold substituted for a missing or zero value.
- Loosening the §2 region or alignment without an owner decision.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected |
|---|---|---|
| Underweight | Domino 0.4 kg at rest, min 0.5 | `Underweight`; contact open; gate closed |
| Loaded | Basketball 1 kg | `Loaded`; gate opens |
| Exactly at minimum (native) | Basketball 1 kg, `minimum_mass` 1.0 | `Loaded` (inclusive) |
| Sum | Two dominoes 0.8 kg, min 0.5 | `Loaded` |
| Removal | Body lifted or rolled off | Contact open on the next tick |
| Two plates | P1→G1, P2→G2; load P1 | G1 opens; G2 stays closed |
| Size impostor (native) | Static box 0.9/0.05/0.9 at Ty 0.03 with no plate declaration | Never switches a gate |
| Instance threshold (native) | `minimum_mass` 2.0, saved and loaded; 1 ball, then 2 balls | Underweight, then Loaded |
| Side or underside | Ball against the side or the base | Not counted |

### Decisions (coordinator delegation)

- **D1. `ContactLoadSensorDeclaration` fields become f32** (`float MinimumAlignment`, `float MinimumMass`). Rationale: the f32 authority. The declaration has no production caller, so the change is free (audit B6 otherwise belongs to Story 9A.44).
- **D2. A tick's measured phase is the maximum of its per-substep qualifying mass.** Rationale: a contact the player sees within a tick is never missed between substeps. A resting body gives the same result either way.
- **D3. The plate's committed level lives in the activation checkpoint** as a `ContactLoadSource` node (`Latched` exactly while the committed phase is `Loaded`). Every plate owns one activation node. There is no activation-slot limit (owner decision, 20 Oct 2026): activation tables are sized from the compiled network, and Build never refuses a machine for its activation count. Rationale: the plate behaves exactly like the latch and counter switches, and the hold that Story 9R.5 adds becomes committed state.
- **D4. Evaluation is allocation-free.** `ContactLoadSensorTracker.Measure` stops allocating a `Dictionary` and stops using LINQ per call; it uses caller-owned scratch. Rationale: it runs every substep in the WASM tick, and 9.4b measured tick allocations.

</frozen-after-approval>

## Code Map

Refreshed against `647f5d72` (9.4b, 9R.0, 9R.0b, 9R.1, 9R.2 and 9R.3 landed). Before → after this story; the symbol is authoritative.

- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`: `_plateLoaded` `:13,38`, the half-extent detector `:841-871` and the drive call `:790` (all deleted); now `MeasureContactLoads` `:402`, the per-tick row read `:722-736`, the substep call `:879`, the D2 state write `:1051-1057`.
- `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs`: the band `:248-252` (deleted); `ActuatorSliderDrive(joints, dt)` `:240`.
- `engine/gpu/WorkshopPhysicsCompiler.cs`: the 0.09375 literal `:198-199`, the plate colliders `:225-238`, `SupplyFeed` `:291-311` (9R.3's Direct-via-plate row `:308-309`); now the plate row `:238`, the Switched feed `:312`, `ContactLoadSensor` `:333`.
- `engine/gpu/ContactLoadSensorDeclaration.cs`: rewritten (f32 row, `PhaseOf`, read, record, static allocation-free tracker).
- `engine/gpu/PhysicsDeclarations.cs`: `ContactLoadSensorCapacity` `:175`, row validation `:300-311`.
- `engine/gpu/PhysicsGpuAbi.ContactLoad.cs` (new): the table after the prismatic rows; `CollidersOffset` moves after it (`PhysicsGpuAbi.cs:79`); the prismatic unused-row check now ends at `ContactLoadSensorsOffset` (`PhysicsGpuAbi.Constraints.cs`).
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:153-159`: reads every row and passes it to `Consume`.
- `engine/gpu/ActivationNetwork.cs`: `ContactLoadSource = 10` `:13`, edge rules `:146,149`, `ContactClosed`, `CommitContactLoads` `:481`, `ValidateRead`/`ValidateOccurrence` branches, compiler `:749`.
- `engine/gpu/ActivationTimers.cs`: `ActivationOccurrenceKind.ContactLoad = 7`; `engine/gpu/WorkshopWire.cs` (`ValidatePhysicalRead`) and `engine/gpu/WorkshopSimulation.cs:94` (`BistableNodes`) admit it.
- `engine/gpu/WorkbenchCapacity.cs:51`: plate footprint, activation nodes 1.
- Ratchet: `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs:502-536`.

The 18 Oct references below are kept as the original map.

- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`: `_plateLoaded` `:13,38`; the detector `:826-856`; the drive call `:779`.
- `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:237-249`: the plate band. After Story 9R.3, D3 leaves `SupplyClosed && plateLoaded` here.
- `engine/gpu/WorkshopPhysicsCompiler.cs`:
  - `isPlatePowered` and the 0.09375 literal `:195-206`;
  - plate bodies and colliders `:232-245`;
  - the route feed `:297-311` (`is WorkshopLatch or WorkshopCounter`).
- `engine/gpu/ContactLoadSensorDeclaration.cs:17-145`: the declaration, the read and the tracker.
- `engine/gpu/PhysicsDeclarations.cs:164-214`: add `ContactLoadSensors`, following `OrientationSensors`.
- `engine/gpu/PhysicsGpuAbi.cs:44-47,290,398,415`: the orientation-sensor table pattern to mirror. Place the new table after Story 9R.2's layout settles.
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:121-140`: per-tick sensor reads passed to `Consume`.
- `engine/gpu/ActivationNetwork.cs`:
  - `ActivationNodeKind` `:12`;
  - `Consume` `:247`;
  - `ContactClosed` `:208-215`;
  - compiler `:697-766`.
- `engine/gpu/WorkshopPressurePlate.cs:5-23`: `MinimumMass` range 0.1–16.
- Codec: `engine/gpu/WorkshopWireFlatBuffers.cs`. Story 9.4b D4 makes a zero threshold rejected.
- Tests: `PressurePlateTests.cs:146-159,193-305,349-352,366` and `tools/e2e/cat-052.test.ts`.

## Tasks & Acceptance

**Execution:**
- [ ] `CuriousContraptions.tests/PressurePlateTests.cs` -- red first. Delete the 0.09375 pin (`:349-352`). Using the real-engine harness (`WorkshopPhysicsEngineTests`, Story 9.4b), add every row of the matrix as tests through the live compiler → engine → device → network path, including the size impostor and the 2.0 kg save/load case. Keep the tracker unit tests, now using f32 declarations.
- [ ] `engine/gpu/ContactLoadSensorDeclaration.cs` -- D1 and D4. `Validate` rejects a non-finite threshold or one ≤ 0.
- [ ] `engine/gpu/WorkshopPhysicsCompiler.cs`:
  - Emit one `ContactLoadSensorDeclaration` per plate from the instance.
  - Admit `WorkshopPressurePlate` as a switch in the supply feed, emitting a `Switched` row with `Switch = plate`.
  - Delete `isPlatePowered`, the 0.09375 literal and the D3 interim `Direct` plate row from Story 9R.3.
- [ ] `engine/gpu/PhysicsDeclarations.cs`, `PhysicsGpuAbi.cs` -- the contact-load table: a declaration plus a committed phase and mass per row, validated like orientation sensors.
- [ ] `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`:
  - Each substep, build contact point records for row frames and call the tracker.
  - Write the D2 tick phase.
  - Delete `_plateLoaded`, the half-extent detector and the 0.5 literal.
- [ ] `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs` -- delete the `plateLoaded` parameter and the band. The drive becomes `joint.SupplyClosed` only.
- [ ] `CuriousContraptions.Simulation/WorkshopGpuDevice.cs`, `engine/gpu/ActivationNetwork.cs`:
  - Read the rows and pass them to `Consume`.
  - Add `ContactLoadSource` (a capability name, not an element name): it commits `Latched`/`Clear` from the phase, and every row must be read each tick.
  - `ContactClosed` admits the new kind.
  - Edge validation (`ActivationNetwork.cs:147`) lists `ContactLoadSource` among the non-target kinds, alongside `ContactSource`, `OrientationSource`, `PassageSource` and `Oscillator`.
- [ ] `engine/gpu/WorkbenchCapacity.cs` -- plate footprint gets activation nodes = 1.
- [ ] `tools/e2e/cat-052.test.ts` -- add domino Underweight, two dominoes summing, removal, and the 9R.1 snapshot. Save/Load asserts `minimum_mass`.
- [ ] `tools/e2e/cat-052-two-plates.test.ts` -- new suite. Build two batteries, plates and gates through actual UI. Loading P1 opens only G1. Then the snapshot and Save/Load.

**Acceptance Criteria:**
- Given Pressure Plates built through real UI, when Run compiles the construction, then each plate emits its own compiled contact-load row (owner body, region, normal, alignment, and `minimum_mass` from that instance; CAT-052 §2). `ContactLoadSensorTracker` evaluates those rows at runtime, so its native tests prove the live path.
- Given a plate's supplied contact, when Run compiles, then it is a switch in the compiled switched-supply route table (Battery → plate contact → gate).
- Given the matrix masses:
  - a domino (0.4 kg) leaves the plate Underweight and the gate closed;
  - a basketball (1 kg) closes it;
  - mass exactly at the minimum closes it (inclusive ≥);
  - two bodies sum.
- Given a body removed, then the contact opens on the next tick. (Story 9R.5 later applies its 12-tick release hold to the same committed level; see 9R.5 D2.)
- Given two plates wired to two gates, when one is loaded, then only its own gate opens (negative control for cross-talk).
- Given a static box with the plate's exact half-extents but no plate declaration, then it never switches a gate (negative control for size detection).
- Given a native test with a non-default `minimum_mass` (2.0 kg, saved and loaded), then the instance value governs. The UI editor arrives with Story 9A.40.
- Given Run then Reset, then the state matches the snapshot (Empty, gate closed), and Save/Load preserves `minimum_mass`.
- Given Chrome, when `cat-052` and `tools/e2e/cat-052-two-plates.test.ts` run serially, then both pass 100%.
- Given the gates, when they run:
  - `dotnet build CuriousContraptions.slnx` reports 0 warnings;
  - `dotnet test CuriousContraptions.slnx` passes 100%;
  - every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

## Implementation Notes

- **Native probe first** (`evidence-9r-4/probe-contacts.txt`, `Probe9R4Tests.cs.txt`): a resting basketball, one and two standing dominoes and a flat domino all report normal exactly +Y and gap ≈ −0.00027 m against the top collider. The ball's point is at y 0.079987 (inside the §2 band); every box point is at y 0.079852 (outside 0.08 ± 10⁻⁴), so a y band would have dropped every domino. The normal and gap rule below counts them.
- **Row shape (f32):** frame body, sensing-face collider (the plate's top box, `first + 2`), x/z footprint, local normal, minimum alignment, maximum gap d, minimum mass. The face collider plus +Y normal plus gap ≤ d replaces the y band; the base box can never count.
- **ABI:** a 16-byte table header holds the count (the 128-byte world header has no free word); rows are 64 bytes (56 declaration + phase + f32 mass). `ValidateCandidate` checks the declaration bytes, the phase against `PhaseOf(mass)` and the unused rows.
- **Engine:** each substep, every static-side contact becomes a `ContactPointRecord`; each row is measured through `ContactLoadSensorTracker.Measure` with caller-owned scratch; the tick keeps the maximum (D2) and overwrites the copied state, so removal reads Empty on the next tick.
- **Network:** `ContactLoadSource` nodes commit Latched (occurrence kind `ContactLoad`, cause = row id, plate, face collider, tick boundary) exactly while the read is Loaded; it is bistable for `WorkshopSimulation`. It is neither an edge source nor target (no activation port until 9R.5).
- **No activation limit (owner decision, 20 Oct 2026):** the fixed activation table, its constant, the Build check and the `WorkbenchTable` member are deleted. The network sizes every table (activations, timers, memory rows, pending fanout) from its own compiled population. The read wire sections, the device's passage and contact-load reads and the contact-load ABI rows are bounded by `WorkshopInstances.Capacity`, because each instance owns at most one node and the worker preallocates a fixed response buffer (`Program.ReadOutput`, `WorkshopPoseHistory`). That bound can never refuse a machine Build admitted. Every activation part now reaches the 32-instance palette maximum (`WorkbenchCapacityTests` steps 32 of each). The Impact Switch is still bounded by the physics trigger table (8), which is not an activation slot; that is left as a scope question.

## Spec Change Log

- 20 Oct 2026, design review folded in (coordinator):
  1. Ratchet: the six Story9R4 entries, the 9A19 drag-band entry and the `gatePowered|plateLoaded` entry (its count fell to 0, so it is deleted, not pinned at 0) are gone with the `Story9R4` owner; maxima lowered to 2 entries / 6 matches; hash recomputed; `9r-4` marked done in `sprint-status.yaml` in the same commit.
  2. Release is on the next tick; the 12-tick hold belongs to Story 9R.5 (coordinator, 18 Oct, CAT-052 Q3; epic AC amended by the coordinator).
  3. Lifted bodies: the contact record carries the gap; only gap ≤ d counts (tracker row: 0.002 m above → 0).
  4. Box contacts: the load region is the face collider, the upward normal and the gap limit, not a y band (probe above).
  5. f32: the measured and committed mass are f32 and compared before any rounding (row: minimum one f32 above 1 kg stays Underweight).
  6. Matrix: two balls side by side, a stacked pair (only direct contact counts), a flat domino bridging two plates (both gates open).
  7. `PoweredGateTests`: the interim pin is now a Switched row with `Switch = plate`; plate without battery and plate without gate compile no route.
  8. The engine-harness tests in `PressurePlateTests` (`RunPhases` through the production `WorkshopSimulation`) are the live-path proof; the impostor bench has a real 2.0 kg plate and its gate beside it.
  9. Code Map refreshed against `647f5d72`.
- 20 Oct 2026, owner decision: no limit on activation slots (the 8-slot cap "was not in the description and is an unreasonable limitation"). D3's capacity clause is replaced in place on the owner's renegotiation; see Implementation Notes.
- 20 Oct 2026, Murdoch-10 B1: the 2.0 kg rows save, reload and run through `WorkshopSaveCodec`.

## Review Triage Log

## Design Notes

**Legacy deleted:**
- the global `_plateLoaded` and its 0.5 kg literal;
- the half-extent plate detection;
- the plate drag band and the 0.09375 encoding;
- the 9R.3 interim plate `Direct` row;
- the `Half` threshold fields.

**Contact-point convention, verified.** `CollideBoxSphere` puts the point on the box surface by clamping, with the normal from that point to the sphere centre (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:300-316`). A ball resting on the top box therefore yields local y = 0.03 + 0.05 = 0.08 and normal exactly +Y, inside §2.

**Test-first plan.**
1. The native live-path tests fail first: today `MinimumMass` 2.0 is ignored and the impostor switches.
2. Chrome proof: `cat-052` and `cat-052-two-plates`, using actual UI placement and drops.
3. Negative controls:
   - the domino;
   - the impostor;
   - cross-talk;
   - no supply (plate Loaded, gate closed).
4. Boundaries:
   - exactly at the minimum;
   - 0.1 and 16 accepted, out of range rejected (`:146-159`);
   - a 20–40° rotated plate (CAT-052 P12).
5. Run/Reset uses the snapshot.
6. Save/Load covers `minimum_mass` 2.0.

**Mutation plan:**
1. The compiler writes 0.5 instead of the instance value, so the 2.0 test fails.
2. Restore one global loaded flag shared by rows, so two-plates fails.
3. Make the comparison `>`, so exactly-at-minimum fails.
4. Count a body per contact point rather than once, so the single-ball-at-min-2.0 test flips.
5. Re-add size detection, so the impostor test fails.

**Impacted suites:**
- Native: `PressurePlateTests`, `PoweredGateTests`, `StateLatchTests`, `CounterTests`, `WorkbenchCapacityTests`, `WorkshopWireFlatBuffersTests`, `WorkshopPhysicsEngineTests`.
- Chrome: cat-052, cat-052-two-plates, cat-051, cat-051-own-supply, cat-037, then the full serial run.

**Risks.**
- **Box contacts are uncertain.** Box–box contacts (domino on plate) may place points on the domino face, outside y 0.08 ± 10⁻⁴. The domino control would still read open, but the two-domino sum would fail. Verify natively first; if it fails, stop and raise it with the owner rather than widening d.
- The new ABI table depends on the Story 9R.2 layout.

**Refresh after Stories 9R.2, 9R.3 and 9.4b land.** Re-read this spec after Story 9R.2 (ABI layout) and Story 9R.3 (route names). 9.4b D4 changes threshold decode: a zero threshold now rejects.
