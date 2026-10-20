---
title: 'Story 9R.5: Pressure Plate ActivationOut Lights a Signal Lamp (Reopened Story 9.1)'
type: 'feature'
created: '2026-10-18'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r5-pressure-plate-activationout-lights-a-signal-lamp-reopened-story-91'
  - '{project-root}/docs/planning/elements/CAT-052-pressure_plate.md'
  - '{project-root}/docs/planning/elements/CAT-037-latch.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9r-4-pressure-plate-per-instance-load-rows.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Report #5. Story 9.1's headline AC ("plate wired to a Signal lamp") is impossible today.
- The plate declares only `PowerIn` and `Supply` (`engine/gpu/WorkshopConnections.cs:62-66`), and the lamp only `ActivationIn` (`:87-90`).
- Level semantics are keyed to one element at every site:
  - `source.PartId == PartId.Latch` (`:172-178`);
  - network edge validation, latch ↔ `LevelIndicator` with "exactly one State Latch" (`engine/gpu/ActivationNetwork.cs:144-154`);
  - the mirror loop, `StateLatch` sources only (`:500-508`);
  - the lamp kind choice, `source is WorkshopLatch` (`:707-709`).

**Approach:**
- Give `WorkshopPort` a declared output kind (`Pulse` or `Level`). Latch and plate `ActivationOut` are `Level`.
- Replace every latch-keyed site with a rule on the source port's declared kind, shared by the latch and the plate.
- The plate's Story 9R.4 `ContactLoadSource` node becomes a level source with a committed 12-tick release hold. That one held level drives both the lamp and the supplied contact.

## Boundaries & Constraints

**Always:**
- The plate level is high immediately on the first qualifying tick (phase `Loaded`).
- With n = consecutive ticks since the last qualifying tick, the level stays high while n ≤ 11 and is low from n = 12. At 120 Hz that is 0.1 s = `ticks(0.1 s, cadence)` = 12.
- The hold counter is committed checkpoint state.
- The message is "A level output can only light a Signal lamp.", identical for the latch and the plate.

**Never:**
- A `PartId` or record-type test for level semantics.
- A mutable network field for the hold.
- A dwell before activation.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected |
|---|---|---|
| Rest | Basketball rests on the plate, which is wired to a lamp | Lamp lights on the first `Loaded` tick |
| Leave | Ball rolls off at tick T (last qualifying T−1) | Lit through n = 11; dark at n = 12 |
| Hold boundary (native) | n = 11 / 12 / 13 after the last contact | lit / dark / dark |
| Bounce | Re-contacts with gaps of ≤ 11 non-qualifying ticks | Steadily lit (no Clear tick) |
| Underweight | Domino 0.4 kg | Lamp dark |
| Refusals | Plate `ActivationOut` → Counter, Delay or latch input | Refused with the message; the latch behaves identically |
| Both domains | Plate → lamp and plate `Supply` → gate | The lamp and the supplied contact share one committed level, which changes on tick N. The gate drive responds at N+1 under the existing route lag (Story 9.4 D6: `Compose` reads committed activations for the next Advance) |

### Decisions (coordinator delegation, 18 Oct 2026, epics Open scheduling item i)

- **D1. CAT-052 Q2, recorded in epics.** The level output is high while Loaded, low otherwise, and drives Signal lamps only.
- **D2 (flagged to the owner). The AC text conflicts with the CAT-052 Q3 override; resolved here.**
  - The 9R.5 AC says "goes dark on the next tick", and the 9R.4 AC says "opens the contact on the next tick".
  - Both conflict with the dated coordinator override ("activation is immediate… release is held for 0.1 s (12 ticks)…", which "replaces the no-dwell choice").
  - Reading: "next tick" means the first tick after the release hold expires (n = 12).
  - The hold also governs the supplied contact, because there is one committed plate level and the AC says it switches the gate "at the same time".
  - "At the same time" means one committed level on the same tick. The gate's mechanical response keeps the existing one-tick route lag.
  - Rationale: the override exists so downstream logic does not chatter. A level that holds for the lamp but not for the gate would contradict "both domains" and "at the same time".
- **D3. Socket anchor.** The plate's `ActivationOut` sits at (0, −0.08, 1.18): the +Z edge, level with the ±X supply sockets. It gets a scene locator `Loc_Port_activation_out` and a `#e8b764` socket sphere of r 0.075, following the CAT-052 art.
  - The inert package declaration `signal_out` at (0, 0, 0.6) (`elements/PressurePlate/PressurePlateElementDeclaration.cs:24`) is corrected to match.
  - Rationale: (0, 0, 0.6) lies under the plate top, invisible to the player.
- **D4. Port order.** The plate lists `ActivationOut` before `Supply`, as the latch does (`WorkshopConnections.cs:41-43`). Rationale: the drawer keeps the Supply row.

</frozen-after-approval>

## Code Map

Line numbers are from the working tree on 18 Oct 2026, before Story 9.4b's commit. The symbol is authoritative.

- `engine/gpu/WorkshopConnections.cs`:
  - `WorkshopPort` `:12`;
  - plate ports `:62-66`;
  - lamp ports `:87-90`;
  - `LocalPosition` `:99-124` (no plate `ActivationOut`);
  - `Validate` `:168-179`.
- `engine/gpu/ActivationNetwork.cs`:
  - kinds `:12`;
  - edge validation `:141-156`;
  - mirror `:500-508`;
  - lamp kind `:706-709`;
  - Clear and Latched handling `:561-570`.
- Story 9R.4's `ContactLoadSource` node and its `ContactClosed` admission.
- `ui/WorkshopConnections.cs:95-110` (`ChangeConnection` shows `error.Message` in `_status`), plus `RefreshConnectionChoices`. The plate panel gains a "Connect ActivationOut" row.
- `parts/scenes/pressure_plate.tscn:97-101` (only the `power_in` and `supply` locators) and `elements/PressurePlate/PressurePlateElementDeclaration.cs:20-25`.
- `docs/planning/elements/CAT-037-latch.md:111` (old message) and `CAT-052-pressure_plate.md` §2 ("No activation port"), §6 Q2/Q3.
- Tests:
  - `StateLatchTests.cs:100,108,137-153` (ports and level mirror);
  - `tools/e2e/cat-037.test.ts`;
  - `tools/e2e/cat-052.test.ts` (pixel-anchored drawer rows).

## Tasks & Acceptance

**Execution:**
- [ ] Native tests, red first:
  - `CuriousContraptions.tests/PressurePlateTests.cs` — the plate declares a `Level` `ActivationOut`; plate → lamp compiles a `LevelIndicator`.
  - The hold boundaries with synthetic committed reads through `ActivationNetwork.Consume`: n = 11 lit, 12 dark, 13 dark; a bounce with a gap of 11 stays lit; a gap of 12 shows exactly one dark tick.
  - Refusals to Counter, Delay and latch carry the exact message.
  - `StateLatchTests` — the latch gives the identical message.
- [ ] `engine/gpu/WorkshopConnections.cs`:
  - Add `enum WorkshopOutputKind : uint { Pulse = 1, Level = 2 }` to `WorkshopPort`, declared on every output.
  - Add plate `ActivationOut` (`Level`, D4) and its `LocalPosition` (D3).
  - Replace `:172-178`: a `Level` source may target only an input declared to accept a level (only the lamp's `ActivationIn`). A lamp fed by a `Level` source takes no other signal.
  - Use the new message.
- [ ] `engine/gpu/ActivationNetwork.cs`:
  - Add `IsLevelSource(kind)` (`StateLatch`, `ContactLoadSource`), used in edge validation, the "exactly one level source" rule and the mirror loop.
  - The lamp kind is chosen from the source port's declared kind.
  - Add `ReleaseHoldTicks` to the `ContactLoadSource` declaration, with the committed hold counter in the checkpoint.
- [ ] `parts/scenes/pressure_plate.tscn`, `elements/PressurePlate/PressurePlateElementDeclaration.cs` -- D3 locator, socket art and the corrected port.
- [ ] `engine/gpu/WorkbenchCapacity.cs` -- unchanged. The plate already carries 1 node (Story 9R.4); confirm by test.
- [ ] Docs:
  - `docs/planning/elements/CAT-037-latch.md:111` — new message.
  - `CAT-052-pressure_plate.md` §2 and §6 — record Q2 and Q3 as decided. This is the current document; the history stays in git.
- [ ] `tools/e2e/cat-052-lamp.test.ts` -- new suite, through actual UI with the icon-only socket choice where ambiguous. Cover:
  - the rest-lights case;
  - roll-off going dark after the hold (read lamp activation per tick from committed reads);
  - the domino control;
  - a bounce dropped from height staying lit;
  - refusals to Counter, Delay and latch, read from the status text;
  - a plate also driving a gate;
  - Reset with the snapshot (lamp Slate);
  - Save/Load of the `activation_out` wire.
- [ ] `tools/e2e/cat-052.test.ts`, `cat-037.test.ts` -- update the drawer row anchors shifted by D4, and update the latch refusal text.

**Acceptance Criteria:**
- Given a Pressure Plate whose `ActivationOut` is wired to a Signal lamp through the real wiring UI (the icon-only socket choice where ambiguous), when a basketball comes to rest on it, then the lamp lights.
- Given the ball rolls off or is lifted, then the lamp goes dark on the next tick, read per D2 as the first tick after the 12-tick release hold.
- Given the controls:
  - a domino below the minimum leaves the lamp dark;
  - a ball that bounces on the plate keeps the lamp steadily lit between contacts closer together than 0.1 s;
  - the lamp is dark 0.1 s (12 ticks) after the last contact, with boundaries at 11, 12 and 13 ticks.
- Given the plate's `ActivationOut` wired to a Counter, Delay or latch input, then it is refused with "A level output can only light a Signal lamp.", and the latch's existing refusal behaves identically (negative control and regression).
- Given the plate's supplied contact, then it still switches a gate at the same time, so both domains work on one plate.
- Given Run then Reset, then the state matches the snapshot (lamp Slate), and Save/Load preserves the `activation_out` wire.
- Given Chrome, when `tools/e2e/cat-052-lamp.test.ts`, `cat-052` and `cat-037` run serially, then all pass 100%.
- Given the gates, when they run:
  - `dotnet build CuriousContraptions.slnx` reports 0 warnings;
  - `dotnet test CuriousContraptions.slnx` passes 100%;
  - every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

## Implementation Notes

Design gaps decided by the implementer at the start (Amelia-10, 20 Oct 2026, owner pace rules). They refine the Tasks; the frozen intent is unchanged.

- **D5. One port-kind enum for both directions.** `enum WorkshopSignalKind : uint { Pulse = 1, Level = 2 }` is declared on every activation port: an output declares what it emits, an input the strongest signal it admits. Only the Signal lamp's `ActivationIn` admits `Level`; every other activation input admits `Pulse`. Electrical ports carry `null` (no activation kind), so no 0-valued enum member exists. The Tasks' name `WorkshopOutputKind` is replaced because inputs declare it too. Rationale: "an input declared to accept a level" needs a typed declaration, and a separate input enum would duplicate the same closed set.
- **D6. The hold is the plate node's memory row.** `ContactLoadSource` joins `ActivationMemory.Holds`. Its row holds only `DueTick`: the boundary on which the held level clears (last qualifying tick + `ReleaseHoldTicks`), or 0 when released. `Count`, `Last` and the phase stay zero. The row is committed checkpoint state: admission publishes it cleared, the B1 and FlatBuffers wire already carry it, and Chrome reads it as `read.activation_memory`. `Consume` rejects a committed row that disagrees with the committed level. The memory table is sized from the compiled nodes, so nothing new depends on a fixed slot count (coordinator, 20 Oct: the 8-slot cap is being removed by 9R.4).
- **D7. `ReleaseHoldTicks` on `ActivationNodeDeclaration`.** It is required non-zero on `ContactLoadSource` and 0 on every other kind. The compiler sets it from `WorkshopPressurePlate.ReleaseHoldTicks(cadence)` (one tenth of the tick rate: 6, 12 or 24).
- **D8. Refusal names and the second message.** `LatchDrivesLampOnly` → `LevelDrivesLampOnly` and `LampFollowsOneLatch` → `LampFollowsOneLevel`, with no alias. The second message becomes "A lamp that follows a level output takes no other signal.", because a plate-fed lamp is not following a latch.
- **D9. Chrome observability.**
  - The status line is drawn on the Godot canvas. The suite reads the refusal from the `CCGPU_UI_REJECTION` console record that `WorkshopUiFailure.Report` writes for the same exception whose message becomes the status text.
  - Per-tick lamp and plate state comes from Pause, then Step: `WorkshopDriver.stepSimulation` now returns each Step acknowledgement's committed read.
- **D10. The package port id stays `signal_out`.** Only its position is corrected to D3. `LocalPosition` is not serialised in `parts/elements.bundle.json`, so the bundle and the runner's port graph are unchanged.
- **D11. Chrome loads.** A Basketball for the rest, bounce and Save/Load rows (AC1). The roll-off uses the bowling ball on the 30° plate, the cat-052 test 7 bench already proven to roll off. The bounce drop is about 2 m, chosen by the native probe `evidence-9r-5/probe-bounce-native.txt`: 2 m gives a 7-tick rebound gap, 5 m exactly 12 (one dark tick) and 8 m 15.

### Candidate evidence (d6f5cacd, on 9R.4 head 15b7431b)

- **Native.**
  - Red: `evidence-9r-5/red-native.txt`, 20 failures. Green: `green-native.txt`.
  - Six mutants, each failing its AC test: `mutant-1-hold-11.txt` through `mutant-6-partid-latch.txt`. They ran before the rebase onto 15b7431b; the hold paths they mutate are unchanged by it.
  - Tier 1: `tier1-dotnet-test.txt` (1775 + 156 + 78 + 17 passed). Default and `PlaytestDiagnostics` builds both report 0 warnings. `node --experimental-vm-modules --test tools/*.test.mjs` gives 82/82, and `run-reset-snapshot.test.ts` 24/24.
- **Chrome.** `E2eRunner run --diff c5a1cdda` selected Full (shared driver changed). Result: verdict Pass, 38/38 files, 187/187 tests, on clean HEAD d6f5cacd (`evidence-9r-5/full-run-1/`). cat-052-lamp, cat-052, cat-052-two-plates and cat-037 all passed. clock-master's close finding and debt fault are its own injected faults.
- **Process.** The raw logs were captured with `tee` and shell redirection, and the probe was moved into the evidence folder with `mv`. These are records only, not authored deliverables; filed for the reviewer.

## Spec Change Log

## Review Triage Log

## Design Notes

**Legacy deleted:**
- the element-keyed rule `source.PartId == PartId.Latch && target.PartId != PartId.SignalLamp`;
- the follow-the-latch check (`WorkshopConnections.cs:172-178`);
- the latch-only network edge rule and mirror;
- the `source is WorkshopLatch` lamp-kind test;
- the old message text.

**Test-first plan.**
1. The native tests above fail first.
2. Chrome proof: `cat-052-lamp`.
3. Negative controls: the domino and the three refusals.
4. Boundaries: n = 11, 12 and 13, natively deterministic. Chrome checks lit-then-dark ordering, and the dark tick is ≥ 12 after the last qualifying committed read.
5. Run/Reset uses the snapshot.
6. Save/Load covers the wire.

**Mutation plan:**
1. A hold of 11 ticks flips the n = 11 case.
2. A hold of 13 ticks flips the n = 12 case.
3. Reset the hold on every tick (no hold), so the bounce test fails.
4. Delay activation by one tick, so the rest-lights first-tick test fails.
5. Let the hold drive only the lamp. The "same time" test then fails: on every tick, the contact (route `SupplyClosed` at N+1) must equal the committed lamp level at N.
6. Re-key the rule to `PartId.Latch`, so the plate refusal test fails.

**Impacted suites:**
- Native: `StateLatchTests`, `PressurePlateTests`, `ContactActivationTests`, `ActivationTimerTests`, `WorkshopWireFlatBuffersTests` (new connection socket).
- Chrome: cat-052, cat-052-lamp, cat-052-two-plates, cat-037, cat-035 when it exists (Story 9R.16), then the full serial run.

**Risks.**
- Drawer pixel anchors shift in cat-052 and possibly cat-051 or cat-005 benches that open the plate panel.
- Cosmetic level animation for the lamp must come from committed level samples, which is the legitimate query (audit §5).

**Refresh after Story 9R.4 lands.** Re-check names and the checkpoint shape against 9R.4's `ContactLoadSource` node as implemented.
