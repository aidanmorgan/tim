---
title: 'Story 9R.3: Each Directly Wired Gate Follows Its Own Battery (Story 8.2 Repair)'
type: 'bugfix'
created: '2026-10-18'
status: 'ready-for-dev'
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

## Spec Change Log

## Review Triage Log

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
   - Disable mid-stroke (the blade falls closed under the existing spring; full CAT-051 mid-stroke law is Story 9.4c);
   - a battery with zero remaining charge is not "Enabled" power. Assert existing `ElectricalSupply` behaviour is unchanged.

**Mutation plan:**
1. `Compose` uses "any source enabled", so the two-circuit test fails.
2. Compile a `Direct` row only when authored `Enabled`, so the in-run Enable test fails.
3. `Compose` ignores the enable for `Direct`, so the Disable test fails.
4. Re-add the 1.55 joint detection while dropping the route check, so a Springboard prismatic is driven and `WorkshopSpringboardTests` fails.
5. Skip D4, so a native Reset test that reads `ElectricalState` in Build fails.

**Impacted suites:**
- Native: `PoweredGateTests`, `StateLatchTests`, `CounterTests`, `ClockTests`, `PressurePlateTests`, `WorkshopPhysicsEngineTests`, `WorkshopGpuDeviceTests`, `ElectricalSupplyTests`, `WorkshopSpringboardTests`.
- Chrome: cat-051, cat-051-own-supply, cat-052, cat-037, cat-017, cat-020, cat-005, then the full serial run.

**Risks.**
- **Blade dynamics change slightly.** The 0.0625 drag was a real exponential decay on the blade (`WorkshopPhysicsEngine.cs:786`). Re-check `cat-051` "fully opens" (`:149`) and the timing.
- **Source enables only update at substep boundaries where `(sourceOrdinal + s) % 4 == 0`** (`:679-685`). The Chrome assertions use "within N ticks", not the same tick.

**Refresh after Story 9.4b lands.** `WorkshopPhysicsEngineTests`, `ElectricalSupplyTests` and the ledger code are 9.4b working-tree edits.
