---
title: 'Story 9R.3: Each Directly Wired Gate Follows Its Own Battery (Story 8.2 Repair)'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r3-each-directly-wired-gate-follows-its-own-battery-story-82-repair'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9-4-state-latch.md'
  - '{project-root}/docs/planning/elements/CAT-051-powered_gate.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Report #2, restated against the current tree.
- Since Story 9.4 D14, the in-run toggle does reach the physics.
  - `WorkshopSimulation.ConfigureElectrical` stores `_electricalControls` (`engine/gpu/WorkshopSimulation.cs:52-65`).
  - `AdvanceCore` passes `WorkshopTickControls.For(..., ElectricalState)` (`:259`).
  - The engine writes `inputs.SourceEnable` into each source record (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:681-685`).
- The defects that remain are these:
  - **(a) Compile-time mode encoding from the authored enable.** `WorkshopPhysicsCompiler` sets the blade drag to 0.0625 only when the wired battery is authored `Enabled` (`engine/gpu/WorkshopPhysicsCompiler.cs:191-194`, `:206`). A battery authored `Disabled` therefore compiles a drag-0 blade, which `ActuatorSliderDrive` never drives.
  - **(b) A global flag.** `batteryPowered` is true when any source is enabled (`WorkshopPhysicsEngine.cs:767-776`). It drives every direct-band gate (`CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:247`).
  - `PoweredGateTests.cs:198-214` pins the 0.0625 drag value.
  - `StateLatchTests.cs:374` asserts that a direct battery→gate compiles no route.

**Approach:** Extend the Story 9.4 compiled route table with a typed direct-route kind. Every Battery → Powered Gate link then compiles one route row: (prismatic, source, Direct). Each tick closes that row from that source's committed player enable only. The solver drives a prismatic only while its own route is closed. Delete the global flag, the direct drag band and the joint-limit gate detection.

## Boundaries & Constraints

**Always:**
- Routes compile once at Run (admission).
- The tick reads only route indices and `PhysicsTickInputs`. There is no authoring read and no per-element solver branch.
- The route kind is a C# enum end to end.

**Never:**
- Drag, joint-limit or element-id detection of a gate.
- Recompiling during Run.
- A shim, or an alias that keeps the old `Switched*` names.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected |
|---|---|---|
| Authored Disabled, enabled in run | Battery(Disabled)→Gate; Run; press **Enable supply** | Blade rises to open; the ball passes |
| Authored Enabled, disabled in run | Battery(Enabled)→Gate; Run; Disable | Blade returns closed (spring) |
| Two circuits | B1→G1, B2→G2; disable B1 | G1 closes; G2 stays open |
| Unpowered | Gate with no supply | Blade stays closed; the ball is blocked |
| Reset | After any of the above | 9R.1 snapshot: authored enable restored, blade closed |

### Decisions (coordinator delegation)

- **D1. One table, renamed to its behaviour.**
  - `SwitchedSupplyRouteDeclaration` → `SupplyRouteDeclaration(PrismaticConstraintId Prismatic, ElectricalSourceId Source, SupplyRouteKind Kind, GpuBodyId Switch)`, with `enum SupplyRouteKind : uint { Direct = 1, Switched = 2 }`. `Switch` is required (and static) only for `Switched`, and is `default` for `Direct`.
  - The other names follow: `SupplyRoute`, `PhysicsSceneDeclaration.SupplyRoutes`, `PhysicsTickInputs.SupplyClosed` and `SimJoint.SupplyClosed`.
  - Rationale: the table now carries unswitched rows, and AGENTS.md forbids alias names.
- **D2. The route selects the driven prismatic.**
  - `ActuatorSliderDrive` drives exactly the joints whose `SupplyClosed` is true.
  - The `Lower == 0 && |Upper − 1.55| < 0.01` detection (`PrismaticConstraintSolver.cs:241`) is deleted, and the drive target uses `joint.Upper`.
  - The drive-law constants (14, 2.8, 60) stay for Story 9A.19.
  - Rationale: only gates have routes, so the heuristic is redundant and must not survive.
- **D3. Interim for plate-fed gates until Story 9R.4.**
  - A Battery → Plate → Gate chain compiles a `Direct` row from the battery that feeds the plate.
  - The existing plate band (blade drag 0.09375) becomes `joint.SupplyClosed && plateLoaded`. Its `batteryPowered` operand is deleted.
  - Story 9R.4 replaces this with a `Switched` row and deletes `_plateLoaded` and the band.
  - Rationale: cat-052 keeps passing, and the global battery flag still dies here.
- **D4. Reset clears `_electricalControls` on commit.** Today only Construct, Run-from-Building and Configure clear it (`WorkshopSimulation.cs:169-171`, `:210`, `:238`), and Reset (`:280-308`) relies on the next Run. Rationale: Reset alone must restore the authored enable.

</frozen-after-approval>

## Code Map

Line numbers are from the working tree on 18 Oct 2026, before Story 9.4b's commit. The symbol is authoritative.

- `engine/gpu/SwitchedSupply.cs:8-52`: the route records, `WorkshopTickControls` and `PhysicsTickInputs.Compose` (`:37-45`, which closes a row only with the switch contact).
- `engine/gpu/WorkshopPhysicsCompiler.cs`:
  - gate compile `:186-221` (`isDirectlyPowered`, `isPlatePowered`, `bladeDrag`);
  - route compile `:286-294`;
  - `SwitchedSupplyFeed` `:297-311`.
- `engine/gpu/PhysicsDeclarations.cs:177-179,208,311-323`: route admission, which requires a static switch.
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:33,73,87`: the `CompiledSupply` record and `Compose`.
- `engine/gpu/ActivationNetwork.cs:208-215`: `ContactClosed`, which throws for owners that are not a switch.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:264` (warm-up inputs), `:612` (per-joint copy), `:767-779` (global flag and drive call).
- `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:35,237-275`.
- UI: `ui/WorkshopBattery.cs` (in-run Enable/Disable supply) and `engine/gpu/BrowserWorkshopClient.Electrical.cs`. These are unchanged consumers.
- Tests:
  - `PoweredGateTests.cs:175,198-214`;
  - `StateLatchTests.cs:347-418` (`:374`);
  - `CounterTests.cs:234`;
  - `WorkshopPhysicsEngineTests.cs:142-149` (Story 9.4b);
  - `WorkshopGpuDeviceTests.cs:295`;
  - `tools/e2e/cat-051.test.ts`.

## Tasks & Acceptance

**Execution:**
- [ ] `CuriousContraptions.tests/PoweredGateTests.cs` -- red first, and delete `:198-214` (the drag pin). Add:
  - the compiler emits one `Direct` row for an authored-Disabled battery→gate;
  - `Compose` closes it only while that source's control is `Enabled`;
  - two batteries and two gates give two rows with distinct sources.
- [ ] `CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs` -- through the real engine, red first:
  - blade rises when its source control flips to Enabled mid-run;
  - blade falls back when it flips to Disabled;
  - in the two-circuit case, only the disabled gate closes.
- [ ] `engine/gpu/SwitchedSupply.cs` → `engine/gpu/SupplyRoute.cs` -- the D1 records and enum. `Compose` closes a row when its source is `Enabled` and (`Kind == Direct`, or its switch contact is closed). `Validate` rejects an undefined kind.
- [ ] `engine/gpu/WorkshopPhysicsCompiler.cs` -- delete `isDirectlyPowered`, the 0.0625 literal and the authored-enable test. Emit a `Direct` row for every gate whose `PowerIn` is fed by a Battery, and for D3 plate chains. Keep the `Switched` rows.
- [ ] `engine/gpu/PhysicsDeclarations.cs` -- per-kind admission:
  - a `Direct` row must have a `default` switch;
  - a `Switched` row must have a static switch;
  - still one row per prismatic.
- [ ] `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`, `PrismaticConstraintSolver.cs`, `WorkshopGpuDevice.cs` -- delete `batteryPowered` and the direct band, and apply D2 and D3. Update the warm-up inputs.
- [ ] `engine/gpu/WorkshopSimulation.cs` -- D4.
- [ ] `CuriousContraptions.tests/StateLatchTests.cs`, `CounterTests.cs`, `WorkshopGpuDeviceTests.cs` -- rename consumers, and change `:374` to expect one `Direct` row.
- [ ] `tools/e2e/cat-051-own-supply.test.ts` -- new suite covering the matrix through actual UI: the drawer, wiring, and the in-run **Enable/Disable supply** control. Include Run/Reset with the 9R.1 snapshot and Save/Load preserving both wires.
- [ ] `tools/e2e/cat-051.test.ts` -- keep passing, with the snapshot added by 9R.1.

**Acceptance Criteria:**
- Given a Battery wired directly to a Powered Gate through real UI, when Run compiles the construction, then a direct supply route row (battery source → gate consumer) is emitted into the same compiled route table as the Story 9.4 `SwitchedSupplyRouteDeclaration` (renamed per D1). Its powered state is the committed electrical state that the typed `ConfigureElectrical` command updates.
- Given that bench:
  - a battery authored Disabled opens its gate when the player presses in-run **Enable supply**;
  - an Enabled battery closes its gate on in-run Disable.
- Given two batteries each wired to their own gate, when one is disabled, then the other gate stays open (negative control for the global flag).
- Given an unpowered gate, when the ball arrives, then the gate still blocks it (control).
- Given Run then Reset, then the state matches the pre-Run snapshot (authored enable restored, blade closed), and Save/Load preserves both wires.
- Given Chrome, when `cat-051` and `tools/e2e/cat-051-own-supply.test.ts` run serially, then both pass 100%.
- Given the gates, when they run:
  - `dotnet build CuriousContraptions.slnx` reports 0 warnings;
  - `dotnet test CuriousContraptions.slnx` passes 100%;
  - every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

## Implementation Notes

Refreshed against main `9b9d26e5` (9.4b, 9R.0, 9R.0b, 9R.1, 9R.2 landed); symbols are authoritative.

- **D1.** `engine/gpu/SwitchedSupply.cs` → `engine/gpu/SupplyRoute.cs` (`SupplyRouteKind`, `SupplyRouteDeclaration`, `SupplyRoute`,
  `PhysicsTickInputs.SupplyClosed`); `PhysicsSceneDeclaration.SupplyRoutes`; `SimJoint.SupplyClosed`. No `Switched*` type,
  member or alias remains. `Compose` is `enabled && (Kind == Direct || contactClosed(Switch))`, so a Direct row never calls
  `ActivationNetwork.ContactClosed` (which throws for non-switch owners).
- **Admission** (`PhysicsSceneDeclaration`): one row per prismatic; Direct ⇒ `Switch == default`; Switched ⇒ admitted static switch;
  an undefined kind is rejected.
- **Compiler.** One `SupplyFeed` resolves the single supplier of a gate's PowerIn (a PowerIn takes one supplier,
  `WorkshopConnections.Validate`): Battery → Direct; Latch/Counter ← Battery → Switched; Plate ← Battery → Direct (D3). The route is
  added inside the gate's own compile; `isDirectlyPowered`, the 0.0625 literal and every authored-enable test are gone. The plate
  band's 0.09375 drag is compiled from the chain alone, whatever the battery's authored enable.
- **Solver/engine (D2, D3).** `ActuatorSliderDrive(joints, plateLoaded, dt)` drives exactly the joints whose `SupplyClosed` is
  true, targets `joint.Upper`, and keeps the plate interim as `!(plate band) || plateLoaded`. `batteryPowered` is deleted; the
  warm-up joint sets `SupplyClosed = true` so the drive path still warms.
- **D4.** `WorkshopSimulation.Reset` clears `_electricalControls` immediately after `_device.Commit`.
- **Ratchet** (`WorkshopPhysicsLawTests`): deleted the two 9R3 rows (0.0625 encoding, 0.03–0.08 band), the engine `batteryPowered`
  row and the `joint.Upper - 1.55f` recognition row (code gone); re-counted the drive literals (`2.8f|14f`, 5 → 4, the
  `1.55f - pos` alternative removed with its code) and the drive identifiers (`gatePowered|plateLoaded`, 7 → 4); removed the unused
  `ViolationOwner.Story9R3` and the `SwitchedSupplyClosed` generic identifier; lowered the frozen totals to 10 entries / 40 matches;
  `KnownViolationsSha256` recomputed from the test's own failure message.
- **Chrome suite** `tools/e2e/cat-051-own-supply.test.ts`, declared in `tools/E2eRunner/SuiteCatalogue.cs`: `SerialAlone`
  (owner-named, coordinator decision after Murdoch-9: a new suite starts SerialAlone until calibration qualifies it; it samples
  over wall-clock windows), with no primary element so cat-051 stays the Powered Gate's primary suite (a gate change still selects
  it as a placer). The first full run (`bcc83118`) ran it on a Parallel worker.

## Spec Change Log

- **20 Oct 2026, design review (coordinator-forwarded), dispositions:**
  1. Ratchet rows: applied as above; `WorkshopPhysicsLawTests` added to the impacted native suites.
  2. Mutation #4 changed to "drop the `SupplyClosed` check, with no detection"; a native unpowered-gate test added
     (`WorkshopPhysicsEngineTests.AnUnpoweredGateStaysClosed`). See the Triage Log for the `WorkshopSpringboardTests` outcome.
  3. D4 native Reset test added (`WorkshopSimulationTests.ACommittedResetClearsInRunSupplyControlsAndACancelledOneKeepsThem`):
     a cancelled Reset keeps the in-run control, a committed one restores the authored enable.
  4. No hand-set inputs: every engine test drives `WorkshopSimulation.ConfigureElectrical` between `Advance`s; pre-asserts added
     (closed before Enable; both open before B1 is disabled, G2 open throughout afterwards), and the same in Chrome.
  5. D3 interim: the plate-fed gate's 0.09375 drag-band recognition stays as a named interim exception owned by Story 9R.4 and
     9A.19. It is recorded here and in code comments, not in Boundaries, because Boundaries is inside the frozen block.
  6. Battery charge: "enable only". The zero-charge boundary is dropped from the Design Notes; supply from remaining charge is a
     follow-up, not this story.
  7. Stale line references: superseded by the symbol-based Implementation Notes above; `Compose` evaluates the kind first.

## Review Triage Log

- **Full run 1 (`bcc83118`, run `20261019T222832Z-84166`): Fail, 35/36 files.** The one failure is cat-062 "Springboard authored
  configuration SoftUndamped": a 120 s `testTimeoutFailure` with no assertion, on Parallel worker slot 1; the suite's other six
  tests pass (`evidence-9r-3/full-run-1/`). The diff does not change Springboard physics: its prismatic (`Upper = 0`) was never
  matched by the deleted 1.55 m recognition and has no supply route now, so it is undriven before and after. Not retried into
  green; a 5× repeat of cat-062 needs the coordinator's go-ahead.

- **Mutation #4 cannot fail `WorkshopSpringboardTests` as written.** That suite never steps the engine, and the engine's
  Springboard test (`ForgivingSpringForwardReceiverOffsetsCaptureInTheShippedEngine`) survives the mutant: driving the springboard
  prismatic toward its `Upper = 0` is within its tolerances. The mutant is killed by `AnUnpoweredGateStaysClosed` and three other
  engine tests (`evidence-9r-3/mutant-4-no-route-check.log`). No springboard test was added for the letter of the plan.
- **Filed, needs a coordinator decision on the frozen matrix row "Authored Disabled, enabled in run → the ball passes".** On the
  cat-051 bench (gate tilted −15°) a ball resting against the closed blade is ejected back out of the entry mouth when the blade
  rises: the blade's prismatic axis leans upstream and the stroke law shoves the ball. The drive law and contact code are unchanged
  by this story; the case is newly reachable because in-run Enable now works. Chrome asserts what the epics AC requires (the gate
  opens on in-run Enable and releases the held ball). Proposed owner: Story 9.4c (mid-stroke blade/ball law) with 9A.19 (drive).

## Design Notes

**Legacy deleted:**
- the global `batteryPowered` flag;
- the direct-mode authored-enable compile;
- the 0.03–0.08 drag band (`PrismaticConstraintSolver.cs:247`) and the 0.0625 encoding;
- the joint-limit gate detection (D2);
- the `PoweredGateTests.cs:198` pin;
- the old `Switched*` names.

**Test-first plan.**
1. The native compiler and engine tests above fail first.
2. Chrome `cat-051-own-supply` covers:
   - in-run Enable;
   - in-run Disable;
   - two circuits;
   - the unpowered control;
   - Reset (snapshot);
   - Save/Load.
3. Boundaries:
   - Enable pressed at Paused, then Resume;
   - Disable mid-stroke (the blade falls closed under the existing spring; full CAT-051 mid-stroke law is Story 9.4c).
   - Gate power is the enable only (coordinator decision, 20 Oct); supply from remaining charge is a follow-up.

**Mutation plan:**
1. `Compose` uses "any source enabled", so the two-circuit test fails.
2. Compile a `Direct` row only when authored `Enabled`, so the in-run Enable test fails.
3. `Compose` ignores the enable for `Direct`, so the Disable test fails.
4. Drop the `SupplyClosed` check, with no detection (design review); see the Triage Log.
5. Skip D4, so a native Reset test that reads `ElectricalState` in Build fails; also clear before `_device.Commit`.
6. `Compose` consults the contact before the kind, so a Direct row asks a non-switch for its contact.

**Impacted suites:**
- Native: `PoweredGateTests`, `StateLatchTests`, `CounterTests`, `ClockTests`, `PressurePlateTests`, `WorkshopPhysicsEngineTests`, `WorkshopGpuDeviceTests`, `ElectricalSupplyTests`, `WorkshopSpringboardTests`, `WorkshopPhysicsLawTests`, `WorkshopSimulationTests`.
- Chrome: cat-051, cat-051-own-supply, cat-052, cat-037, cat-017, cat-020, cat-005, then the full serial run.

**Risks.**
- **Blade dynamics change slightly.** The 0.0625 drag was a real exponential decay on the blade (`WorkshopPhysicsEngine.cs:786`). Re-check `cat-051` "fully opens" (`:149`) and the timing.
- **Source enables only update at substep boundaries where `(sourceOrdinal + s) % 4 == 0`** (`:679-685`). The Chrome assertions use "within N ticks", not the same tick.

**Refresh after Story 9.4b lands.** `WorkshopPhysicsEngineTests`, `ElectricalSupplyTests` and the ledger code are 9.4b working-tree edits.
