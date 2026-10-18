# Adversarial Review Report: Story 9.4 — State Latch Bistable Memory (CAT-037)

**Reviewer:** Murdoch (`bmad_reviewer`), independent of the implementer (Amelia)
**Mandate:** Independent adversarial review (REQ-14: self-review never qualifies)
**Story under review:** Story 9.4 ([`spec-9-4-state-latch.md`](spec-9-4-state-latch.md)); source acceptance `_bmad-output/planning-artifacts/epics.md` Story 9.4; element spec [`docs/planning/elements/CAT-037-latch.md`](../../docs/planning/elements/CAT-037-latch.md); requirement rows `current-cat-037` / `todo-267`.
**Date:** 2026-10-18
**Candidate:** working tree on HEAD `9107148` (18 earlier verified, uncommitted stories in the tree). Raw evidence: [`evidence-9-4/review/`](evidence-9-4/review/).

---

## 1. Executive Summary & Verdict

- **Verdict: Fail.** The cause is one known violation of a deliverable assigned to Story 9.4 (finding F1). The runtime behaviour is otherwise sound.
- **Behaviour.** The State Latch behaves correctly in real Chrome:
  - Set latches On and holds it.
  - Reset clears it.
  - Simultaneous Set + Reset resolves Off, and an independent witness probe confirms the impact really occurred.
  - Memory survives a supply interruption.
  - Pause preserves state.
  - Workbench Reset restores Off, with the rocker at rest and the lamp Slate.
  - Save → reload → Load replays the same Set-then-Reset behaviour.
  - Negative control: Battery → Latch → Powered Gate with SetIn unwired stays closed for 6.48 s (151 publications, max py 3.00 m).
- **Gates.** Build, unit tests and anvil are clean.
- **Pre-existing failures.** The cumulative failures are exactly the suites assigned to Story 9.4b. I independently confirmed that 9.4 did not cause them.
- **Why Fail.** DESIGN.md (Set/Reset latch row) and CAT-037 §3/§5 require **icon-only** raised-bar / ring socket choices in the wiring drawer, and CAT-037 §3 assigns this to Story 9.4. The shipped drawer shows English text buttons with `Icon = null` ("ActivationOut ▯ SetIn (bar)", where ▯ is the missing "→" glyph). No owner or coordinator waiver exists in the spec change log (D1–D12), `epics.md` or `decisions.md`.
- **Incomplete items for the same fix round.** Three requirement-row qualifications are unproven: adjacent-tick separation, reversed-order same-tick conflict, and failed-tick pending state (F2).

---

## 2. Criterion & Impact Matrix

| # | Requirement / invariant (source) | Status | Evidence (own run unless stated) |
| :-- | :-- | :-- | :-- |
| E1 | Set pulse → output high, held indefinitely (epics 9.4 AC1; spec AC2) | **Pass** | `e2e-cat-037.log` test 2: gate 3.00 → 4.55 m, held > 2.5 s, blend 1 → rocker +0.25 rad, lamp (255,255,116) Gold. Unit `Network_SetHoldsResetClears…` ticks 120–179 held. |
| E2 | Reset pulse → output low (epics AC2; spec AC3) | **Pass** | test 5: On/Gold, then after the 1 s Delay Reset the gate closes, blend < 0.01 → rocker −0.25 rad, lamp (144,161,163) Slate. |
| E3 | Simultaneous Set + Reset → Reset dominance (epics AC3) | **Pass** | test 4 (one switch wired to both SetIn and ResetIn): max py 3.00, no Memory samples, Slate. **Independent probe** `probe.log` P1: switch witness target 7 = 234 samples (the impact occurred), max py 3.00, latch samples 0. Control P0 (same layout, SetIn only): switch 234, gate 4.55, latch 234. Distinct-source same-tick dominance: unit test tick 360 (`StateLatchTests.cs:149`). |
| E4 | `tools/e2e/cat-037.test.ts` passes 100% in Chrome serially (epics AC4) | **Pass** | 6/6 standalone (`e2e-cat-037.log`), 6/6 inside the cumulative run (`e2e-all.log`). |
| E5 | Negative control: never-Set latch keeps gate closed > 6 s | **Pass** | test 1: 151 publications over 6483 ms, max py 3.00, 0 Memory samples, Slate. Test 2 then opens only after the Set. |
| S1 | AC1 placement, typed SetIn / ResetIn / PowerIn / Supply wiring through the UI | **Pass (behaviour)** | Saves decode distinct sockets 2/3/7/8 (tests 1, 4, 5). Wrong-socket rejection: `StateLatchTests.cs:118-121`. |
| S1b | Icon-only raised-bar / ring socket choice in the wiring drawer (DESIGN.md:340; CAT-037 §3 "Missing … Story 9.4", §5) | **Fail** | `ui/WorkshopConnections.cs:16,19-24,45-58` render text labels with `Icon = null`. Screenshot: `probe-socket-choice-drawer.png` / `-crop.png`. → **F1** |
| S2 | AC4 (amended) memory persists while supply is disabled; re-energises without a new Set | **Pass** | test 3: Disable → gate falls to 3.00 while the lamp stays Gold; Enable → 4.55 with no new Set. The impact switch is single-shot per Run. |
| S2b | AC4 amendment faithful and justified | **Pass** | Wire edits are Build-only (`ui/Workshop.cs:56` `CanEdit` requires Building). The Battery's in-run toggle (`ui/WorkshopBattery.cs:32`) is a genuine supply interruption. Requirement intent ("memory survives supply loss") is preserved. |
| S3 | AC5 Pause preserves state; workbench Reset restores Off, rocker rest, Slate | **Pass** | test 3: paused blade Δpy < 1e-6 over 800 ms; resumed still open. After Reset, lamp is Slate and the next Run starts closed with no Memory sample. |
| S4 | AC6 Save / reload / Load reproduces behaviour | **Pass (via behaviour)** | test 6: replay opens then closes, blends On then Off. A swapped or duplicated socket would fail this replay. The post-Load `assertWiring` reads the slot written in test 5, so it proves nothing about the load (**F3**). |
| S5 | Rocker and lamp reach On and Off | **Pass** | Animation blend 1 / 0 → ±0.25 rad; rendered pixels Gold / Slate (tests 2, 3, 5). Duration 0.16 s vs declared 0.125 s (**F5**). |
| R1 | `current-cat-037`: qualify each input | **Pass** | Set: tests 1–2. Reset: test 5. Unit tests cover both inputs. |
| R2 | duplicate events | **Pass** | Repeated Sets while On emit one LatchSet and keep On (`StateLatchTests.cs:176-189`). |
| R3 | reordered same-tick events | **Incomplete** | Order independence holds by inspection: requests are OR-accumulated, then resolved after propagation (`ActivationNetwork.cs:441-446,478-492`). Only one node ordering is tested (Set source id 1 < Reset source id 2). → **F2** |
| R4 | adjacent-tick separation (Set at t, Reset at t+1 ⇒ On then Off; CAT-037 T12) | **Incomplete** | No test. The nearest is 60 ticks apart (`StateLatchTests.cs:146-147`). → **F2** |
| R5 | missing supply | **Pass** | Unit `Routes_…` (unsupplied feed never powers, `StateLatchTests.cs:225-228`); e2e test 3 (disabled supply). |
| R6 | failed-tick pending state; Reset clears queue | **Incomplete** | Structurally sound: requests are stack-local (`ActivationNetwork.cs:255-256`) and memory lives in the committed checkpoint. No test injects a failed or discarded tick with a pending Set. → **F2** |
| A1 | Build `dotnet build CuriousContraptions.slnx` | **Pass** | 0 errors, 1 warning, IL2125 Google.FlatBuffers (Story 8.1) (`build.log`). |
| A2 | Anvil 0 warnings | **Pass as defined; no C# signal** | MCP `anvil_check` on all 43 files of the 9.4 touched set → 0/0/0 (`anvil-mcp.json`). The CLI `anvil check --changed` → "No warnings", but it scans only 10 web/script files and **no .cs** (`anvil-cli.log`). A probe `.cs` file with two empty catch blocks also returned 0 warnings, while the same patterns in `.ts` raised 3 (`anvil-csharp-coverage-probe.txt`). **Anvil provides no C# anti-pattern coverage on either path.** |
| D1 | Latch-dependent deferred integrations: CAT-005 N19 supply-loss control (epics.md:702, Story 8.1) and CAT-051 "supply interruption mid-stroke" (epics.md:718, Story 8.2), both "owed once the Latch (Story 9.4) lands" | **Not in 9.4 scope; unscheduled** | Neither epics Story 9.4 nor CAT-037 acceptance includes them, and no story (9.4b included) schedules them. cat-037 test 3 interrupts supply only with the gate fully open, so it is not mid-stroke. These are now due, and the coordinator should assign them. |
| A3 | `dotnet test CuriousContraptions.slnx` 100% | **Pass** | 956/956, 0 skipped (`dotnet-test.log`). |
| A4 | `dotnet publish` CuriousContraptions.web + preview :8060 | **Pass** | exit 0, IL2125 only (`publish.log`). WASM identities equal Amelia's (`bundle-identity.txt`); `godot.pck` differs because Godot packs are not byte-reproducible. Preview stopped; port 8060 free. |
| A5 | Cumulative serial Chrome run of all 32 `tools/e2e/*.test.ts` | **113/132** | Every failure is in the 9.4b set (§6). DoD tension recorded in §8. |
| I1 | Typed `ActivationEdge.Input`: save codec and FlatBuffers impact | **Pass** | `ActivationEdge` is compiled in memory from the already-persisted `WorkshopConnection.Input` (`ActivationNetwork.cs:744-745`). `schemas/` is unmodified since 9.3, and `to_socket` already existed (`workshop_wire.fbs:187`). `WorkshopSaveCodec.cs` is untouched by 9.4; the only wire edits add `latch` cases (`amelia-shared-engine-edits.txt`). Existing saves and levels load: unit, cumulative and lesson suites pass. Unsupported shapes are rejected explicitly by `Enum.IsDefined` plus declared-port ownership (`WorkshopConnections.cs:155-168`) via `WorkshopSaveCodec.Validate`. No migration or shim. |
| I2 | Counter/Clock 3 s shortcut, engine crossing counter, collider-size detection, blade drag 0.125 removed; no new shim | **Pass** | `grep-remnants.txt`: 0 hits for each. No compatibility/fallback/alias identifiers. The Pressure Plate dimension detection (`WorkshopPhysicsEngine.cs:831-853`) and gate identification by joint limit and drag code (`PrismaticConstraintSolver.cs:241-249`) remain; both predate 9.4. |
| I3 | cat-017 (9.3) and cat-020 (9.2) still pass | **Pass** | 5/5 and 5/5 in the cumulative run. |
| I4 | cat-020 recipe change faithful to CAT-002 / CAT-020, no weakened assertion | **Pass** | Engine arms at X < −RearmClearance − r (`PassageSensorDeclaration.cs:106,173`); clearance 0.02 (`ActivationNetwork.cs:701`); r = 0.34 (`WorkshopConstruction.cs:56`). So the threshold is −0.36 m, while the old 18 px lift was 0.31 m (inside). Edits only add assertions (lamp unlit below target, lit at target, saturation, replay) and remove none (`amelia-cat020-driver-edits.txt`). The two-ball negative is now stronger, because both balls are genuinely counted. |
| I5 | `workshop-driver.ts` palette re-measure | **Pass** | Every re-measured bottom-row tool is exercised by a suite that passed: pipe→048a, ball_detector→002/020, bend45→049, bend90→050, funnel→030, powered_gate→051, pressure_plate→052, counter→020, clock→017, latch→037. |
| P1 | f32 / no new `Half` | **Pass with note** | No new Half-typed field, property or API. New memory state uses the existing `WorkshopAnimationControl` / `WorkshopAnimationSample` types (`BrowserWorkshopClient.Activation.cs:13-16`). New Half **values** flow into existing binary16 lanes: `WorkshopCosmetic.cs:63`, `MachinePart.cs` bindings, and compiler material/body declarations. This is inherited debt under CAT-037 open question 2, which has no owner decision (F7). `WorkshopPorts.LocalPosition` moved to f32 (D7). |
| P2 | Closed sets as enums end-to-end | **Pass** | `ActivationNodeKind.StateLatch`, `SimulationLatchPhase`, `[Flags] StateLatchRequest`, `WorkshopSocket.SetIn/ResetIn`. No new string dispatch; the `"latch"` part id appears only at the existing extensible-PartId boundary. |
| P3 | No shims or legacy aliases; sticky lamp `Latch` not conflated | **Pass** | Distinct `StateLatch = 8`; D8 single-supplier rule identical to HEAD (`git show HEAD:engine/gpu/WorkshopConnections.cs:111-115`). |
| P4 | Actual UI controls in e2e (no setters) | **Pass** | Palette, canvas, Move lift, contextual Connect, menu Save/Load/Pause/Resume and the Battery toggle all drive the UI (`workshop-driver.ts:687-752`). Observation is read-only. |
| P5 | Approved palette | **Pass** | `#66b8c9`, `#293954`, `#fff8e9`, `#556573`, `#f7cb52` are all established DESIGN.md rows (lines 197, 201, 204, 227, 332). |
| P6 | Tri-Graph compile-to-state | **Concern** | → **F6** |

---

## 3. Isolation of the 9.4 Diff

- **Touched set.** I took every non-`.uid` file modified after review-9-3 (00:45, 18 Oct): 55 files (`anvil-mcp.json` lists the code subset).
- **Amelia's edits.** Amelia's own Edit/Write calls were extracted from her transcript: `amelia-shared-engine-edits.txt` and `amelia-cat020-driver-edits.txt`. Her shell edits targeted scratchpad copies only, and her temporary `zz-diag-cat-020` copy no longer exists.
- **Partial pre-Amelia work.** An actor outside the Claude transcripts did partial 9.4 work. Amelia surveyed it at session start; it was limited to:
  - `PartId`, `WorkshopInstances`, `WorkshopCanonicalColliders`, `WorkbenchCapacity`, `WorkshopInventory`, `WorkshopConstruction`, `WorkshopConnections`;
  - `tools/BundleElements`, `elements/StateLatch`, `latch.tscn/.tres`, the bundle JSON.
- **Conclusion.** No partial-work file is a physics, worker, wire, save or e2e-fixture file of the failing suites. 9.4's shared-engine change is therefore exactly Amelia's extracted edits:
  - the switched-supply drive;
  - removal of the timer, crossing counter, collider-dimension detection and drag code;
  - `latch` cases in the wire codecs;
  - the `WorkshopSimulation` bistable admission.

---

## 4. Findings

| ID | Severity | Location | Finding |
| :-- | :-- | :-- | :-- |
| **F1** | **High (blocking: known violation)** | `ui/WorkshopConnections.cs:16,19-24,45-58`; DESIGN.md:340; `CAT-037-latch.md:41` (§3), §5 | **Required:** "Matching icon-only choices appear in the existing part drawer when wiring an ambiguous target", using the raised-bar (Set) and ring (Reset) icons; CAT-037 §3 assigns this to Story 9.4. **Shipped:** text buttons "ActivationOut ▯ SetIn (bar)" and "ActivationOut ▯ ResetIn (ring)" with `Icon = null`. The "→" renders as a missing-glyph box. **Waiver:** none in D1–D12, epics, decisions or sprint status. Evidence: `probe-socket-choice-drawer-crop.png`. **Fix:** icon-only bar/ring choices in the drawer, plus Chrome proof that the choice is selected by icon. |
| **F2** | Medium (Incomplete) | `CuriousContraptions.tests/StateLatchTests.cs` (no such facts) | `current-cat-037` requires qualification of reordered events and failed-tick pending state, and CAT-037 T12 carries forward adjacent-tick separation. Missing: (a) Set at t, Reset at t+1 ⇒ On then Off; (b) same-tick conflict with the Reset source ordered before the Set source; (c) a discarded or failed tick holding a pending Set leaves committed memory Off. Code inspection suggests all three hold; they need proof. |
| F3 | Low | `tools/e2e/cat-037.test.ts:340` | After `load()`, `assertWiring(await saved(driver))` re-reads the IndexedDB slot written in test 5. It cannot detect a faulty load. The behavioural replay at lines 343-347 compensates. Prefer `save()` after `load()` before decoding, as cat-062b does. |
| F4 | Low | `tools/e2e/cat-037.test.ts:292-309` | The dominance test lacks an in-run witness that the impact occurred, so "never opens" would also pass if the switch never fired. My probe adds the witness and passes (`probe.log` P0/P1). Fold the switch-target sample assertion into the suite. |
| F5 | Low | `engine/gpu/WorkshopCosmetic.cs:63` | `CosmeticCurves.StateLatch` lasts 0.16 s; CAT-037 §2 and T17 declare 0.125 s SmoothStep (half-way at 0.0625 s). Unexplained deviation (D10 states 0.16 s with no rationale). |
| F6 | Medium (non-blocking, architecture) | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:604`; `Program.cs:167-173`; `engine/gpu/WorkshopLatch.cs:31-58` | The physics step calls the static host `Program.SwitchedSupplies` per joint per tick. That resolves the route by LINQ over the **authoring** construction (Logical graph) instead of a route compiled at Run into the Physics graph. It also rides the pre-existing gate identification by joint limit 1.55 m and drag code. Element-type branches (`is WorkshopLatch or WorkshopCounter`, `WorkshopSimulation.cs` bistable check) follow the established per-element-record pattern. Recommend compiling switched routes into the physics declaration when the gate drive is generalised. Detailed performance is deferred to its named gate. |
| F7 | Note | `WorkshopCosmetic.cs:63`, `engine/MachinePart.cs` latch bindings, `WorkshopPhysicsCompiler.cs` latch body | New Half values go into existing binary16 lanes; no new Half storage. CAT-037 open question 2 still needs an owner decision. |
| F8 | Low (record accuracy) | `spec-9-4-state-latch.md` §6 item-3 table (cat-062b, cat-023b rows) | The classification is correct, but the first failing assertions are incomplete. cat-062b's fifth test fails at `cat-062b.test.ts:257` (`goal_body` 1 ≠ replacement 4), because the FlatBuffers encoder writes `Goal.Body` (the ball) for Captured goals (`WorkshopWireFlatBuffers.cs:640`), not at "guide is zero". cat-023b also fails at `:182` (lamp history 0 samples after the lamp lit and the goal was solved) and "Pose slot must be readable". Both are pre-existing (§6). |
| F9 | Medium (non-blocking, same fix round) | `engine/gpu/ActivationNetwork.cs:9-10`; `story-7-0-owner-questions.md:56`; `CAT-037-latch.md` §3 | The Batch B reviewer confirmed in Story 7.0 that "`ActivationNodeKind.Latch` (the Signal lamp's first-input lock) **must be renamed before the Set/Reset latch**", and CAT-037 §3 says the two "must not be conflated or share a name". 9.4 kept `ActivationNodeKind.Latch` for the lamp and added `StateLatch` (D2), relying on a doc comment to tell them apart. The lamp node is still called `Latch`, next to `StateLatch`, `SimulationLatchPhase` and the generic `ActivationLatch` record. Rename the lamp node (for example, a sticky first-input name) and update every caller in one step, with no alias. |
| N1 | Note (not failing) | `ActivationNetwork.cs:487-491` | `LatchSet` is published only when an event buffer is passed. The runtime passes none (the event ring has no producer or consumer for any element), so it is unit-proven only (`StateLatchTests.cs:153-159,185-189`). This **does not violate** source acceptance: epics 9.4 and CAT-037 require memory, output and visuals, not events, and DESIGN allows bindings to committed observables. Spec §2.3 should say "unit-proven" rather than imply runtime delivery. |
| N2 | Note (not failing) | spec §5 process note | Honest record: the slice was not test-first, and one temporary cat-020 copy was edited with python and deleted. The staged-step rollback gap is closed for the latch (memory in the committed checkpoint). Counter counts and Clock phase remain in private `ActivationNetwork` arrays (`:86-92`), which predates 9.4 (9.2/9.3). |

---

## 5. Raw Commands and Results

| Gate | Command | Result | Log |
| :-- | :-- | :-- | :-- |
| Build | `dotnet build CuriousContraptions.slnx --no-incremental` | exit 0; 0 errors; 1 warning IL2125 (Google.FlatBuffers) | `build.log` |
| Unit | `dotnet test CuriousContraptions.slnx --no-build` | Passed 956, Failed 0, Skipped 0 | `dotnet-test.log` |
| Anvil (MCP) | `anvil_check` on the 43-file 9.4 touched set | 0 errors, 0 warnings | `anvil-mcp.json` |
| Anvil (CLI) | `anvil check --changed` | "No warnings"; 10 files checked, .ts/.js only | `anvil-cli.log` |
| Anvil C# coverage | MCP `anvil_check` on a temporary `.cs` file with empty catch blocks (then deleted) | 0 warnings (the same patterns in `.ts` gave 3) → no C# coverage | `anvil-csharp-coverage-probe.txt` |
| Publish | `dotnet publish CuriousContraptions.web` | exit 0; IL2125 only | `publish.log`, `bundle-identity.txt` |
| Preview | `dotnet run --project tools/Preview` (AppBundle, 127.0.0.1:8060) | served, then stopped | `preview.log` |
| CAT-037 | `node --import ~/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs --test --test-concurrency=1 --test-timeout=150000 tools/e2e/cat-037.test.ts` | 6/6 | `e2e-cat-037.log` |
| Cumulative | same loader, `tools/e2e/*.test.ts` (32 suites) | tests 132, pass 113, fail 19, skipped 0; 23.6 min | `e2e-all.log`, `per-suite-counts.txt`, `failing-section.txt` |
| Probe | same loader, `murdoch-probe-9-4.test.ts` (copy: `murdoch-probe-9-4.test.ts.txt`) | 2/2 (P0 control, P1 dominance witness) | `probe.log`, `probe-socket-choice-drawer*.png` |
| Isolated reruns | same loader, each failing suite alone (cat-023b twice) | cat-005 2/7, cat-015a 1/2, sidekick 2/3, cat-015b 1/4, cat-062b 2/7, cat-023b 1/4 then 0/4 | `e2e-rerun-*.log`, `e2e-rerun2-cat-023b.log` |
| Remnants | `rg` for removed shortcuts and shim identifiers | 0 production hits | `grep-remnants.txt` |

Plain `node --test` cannot parse the enums in `workshop-wire.ts`, so the tsx loader from `tools/e2e/README.md` is required.

---

## 6. Cumulative Chrome Result and Pre-existing Classification

**Per suite (cumulative serial run, `per-suite-counts.txt`):**
- **Passing:**
  - ANIM-1a 3/3, ANIM-1b 3/3, ANIM-1c 4/4;
  - CAT-002 5/5, CAT-014 4/4, CAT-017 5/5, CAT-020 5/5, CAT-023a 4/4, CAT-030 7/7, CAT-037 6/6;
  - CAT-048a 4/4, CAT-048b 5/5, CAT-049 5/5, CAT-050 5/5, CAT-051 5/5, CAT-052 5/5, cat-062 7/7;
  - ENGINE-CORE-2a1 5/5, 2a2 4/4, 2a3 5/5, 2a4 2/2, 2b1 1/1, 2b2 1/1, 2b3 1/1, 2b4 2/2, 2c 2/2.
- **Failing:** cat-005 2/7, cat-015a 1/2, cat-015a-sidekick 2/3, cat-015b 1/4, cat-023b 0/4, cat-062b 2/7.
- **Total:** 113/132.

The failing set equals the Story 9.4b list exactly. No other suite failed.

| Suite | First failing assertion (own runs) | Caused or worsened by 9.4? | Independent basis |
| :-- | :-- | :-- | :-- |
| cat-005 | "Every connected store must receive positive credit" (×3); `budget.debit>0 && budget.credit>0` (`:345`); null `.enabled` | **No** | Passed 7/7 in review-8-2; review-9-1 already set it aside as a "diagnostic fixture", so its breakage predates 9.4. 9.4 made no Bumper or contact-work edit (`amelia-shared-engine-edits.txt`). |
| cat-015a | "Later empty contact must not spend energy" | **No** | Bumper contact-work path untouched by 9.4; deterministic in both runs. |
| cat-015a-sidekick | `powered.length>0 && remainingEnergy()<32` (`:72`) | **No** | Same untouched paid-work path; unpaid and miss routes pass. |
| cat-015b | `paid.length>0` (`:128`) ×3 | **No** | Same paid-work path. |
| cat-062b | "Precise Receiver guide is zero" (0.5 ≠ 0) ×4; `goal_body` 1n ≠ 4n (`:257`) ×1 | **No** | Deterministic. The test (16 Oct), `workshop-wire.ts` (16 Oct) and `schemas/` predate 9.4. Guide 0.5 comes from the declared Receiver force region. The goal-body mismatch comes from the existing encoder (`WorkshopWireFlatBuffers.cs:640`), which 9.4 did not modify (it only added `latch` cases). |
| cat-023b | `CCGPU_TRANSPORT_FAILURE 3 … Clock mapping recovery exhausted its bounded episode` (tests 2–4); test 1 lamp history 0 samples after the lamp lit and the goal was solved; "Pose slot must be readable" | **No (not caused); not worsened, with residual uncertainty** | The same transport fault is recorded before 9.4: Story 7.4 (`.anvil/reviewer-story-7-4-chrome.log:100,117`, 10 Oct) and review-9-2b:15. cat-023b uses only Domino and Lamp, so it has no prismatic joint and the per-joint `SwitchedSupplies` never runs. `PumpMemory`/`IsMemoryTarget` act only on Memory-cosmetic (latch) owners (`BrowserWorkshopClient.Activation.cs:33,45-51`). The domino→lamp activation chain still works (lamp lit, CCGOAL_SOLVED once). 9.4's remaining reachable code is O(nodes) no-op loops plus one `Any` per committed activation. No pre-9.4 bundle exists for an A/B rate comparison. |
| cat-020 | — (5/5) | Unmasked by D11, fixed by D12 | See I4. |

**Classification confirmed.** 9.4 did not cause any of these failures, and on the evidence above did not worsen them. The one residual is cat-023b's transport flake rate: it cannot be compared against a pre-9.4 baseline. Amelia's "first failing assertion" column is incomplete for cat-062b and cat-023b (F8). IL2125 is confirmed to come from Story 8.1's `Google.FlatBuffers` reference, not from 9.4.

---

## 7. Zero-Legacy / Shim Check

- **Remnant grep:** 0 production hits for `counterReached`, `_counterCrossings`, `_ballPassageState`, `hasClock`/`hasCounter`, the 3 s tick test, `0.125f`, `isCounterPowered`, collider-half-extent detection of detector/clock/counter, `StateLatchSupplied`/`StateLatchRoutes`/`GetStateLatchPhase`/`UpdateLatchPresentation` (`grep-remnants.txt`).
- **No new compatibility path:** no compatibility, fallback, alias or migration path was introduced.
- **Remaining debt, all predating 9.4:**
  - Pressure Plate dimension-based load detection;
  - drag-coded gate modes 0.0625 / 0.09375;
  - gate identification by joint limit.

---

## 8. Terminal Verdict

**Fail** (scoped to Story 9.4).

**Blocking:** F1. The icon-only Set/Reset socket choice, required by DESIGN.md and assigned to Story 9.4 by CAT-037 §3, is not delivered; the drawer shows text labels with a missing glyph.

**Also required in the same fix round (currently Incomplete):** F2 (adjacent-tick, reversed-order and failed-tick qualification).

**Recommended for the same round:** F9 (rename the lamp `ActivationNodeKind.Latch`, a confirmed Story 7.0 readiness item).

**Recommended (non-blocking):** F3, F4, F5, F6, F8.

**For the coordinator:**
- The latch-dependent deferred integrations (CAT-005 N19 and CAT-051 mid-stroke supply interruption) are now due but unscheduled (D1).
- Anvil gives no C# signal (A2).

**Waivers.** No owner waiver was found for F1 or F7. I searched spec D1–D12, epics, decisions, TODO, sprint status, `story-7-0-owner-questions.md` and the 18 Oct owner scheduling decisions.

**Return path:** findings return to the same implementer (Amelia). After the fix, an independent re-review must re-run:
- the affected unit tests;
- cat-037, including a Chrome proof of icon selection;
- the cumulative suites affected by any UI change to `ui/WorkshopConnections.cs` (every suite that wires through the drawer).

**DoD tension.** The autonomous-development guide's DoD #4 requires a 100% cumulative pass. The coordinator has assigned the six pre-existing suites to Story 9.4b, and this review confirms 9.4 did not cause them. That assignment does not by itself satisfy DoD #4 for marking 9.4 done; it is the coordinator's and owner's decision.

`sprint-status.yaml`, the spec and all code were not modified by this review.

---

## Re-review (round 2) — 2026-10-18

**Scope.** Amelia's round-2 changes (spec D13–D16, §7), plus owner decisions A–D and the delegated playability decisions (CAT-037 §6). The candidate is the working tree on HEAD `9107148`. The round-2 touched set is every file modified after my round-1 evidence (11:47): 34 files, 27 of them code, tests, scene or bundle. Raw logs: [`evidence-9-4/review2/`](evidence-9-4/review2/). Amelia's `round2/` evidence was treated as claims; every gate below is my own run.

### R2.1 Gates

| Gate | Command | Result | Log |
| :-- | :-- | :-- | :-- |
| Build | `dotnet build CuriousContraptions.slnx --no-incremental` | exit 0; 0 errors; 1 warning IL2125 (Story 8.1) | `review2/build.log` |
| Unit | `dotnet test CuriousContraptions.slnx --no-build` | **961/961**, 0 skipped | `review2/dotnet-test.log` |
| Anvil | MCP `anvil_check` on the 27 round-2 code/test/scene/bundle files; CLI `anvil check --changed` | 0/0/0; "No warnings" (CLI: .ts/.js only). Anvil has no C# coverage (round-1 probe). | `review2/anvil-mcp.json`, `review2/anvil-cli.log` |
| Publish | `dotnet publish CuriousContraptions.web` | exit 0; IL2125 only. WASM, bundle and test identities equal spec §7 (`4ce8fcf6`, `ea15ce31`, `27c3852a`, `6565ab89`, `c9e76870`) | `review2/publish.log`, `review2/bundle-identity.txt` |
| Preview | `dotnet run --project tools/Preview` (:8060) | served; **stopped**, port free | `review2/preview.log` |
| CAT-037 | tsx loader, `--test-concurrency=1 --test-timeout=150000 tools/e2e/cat-037.test.ts` | **6/6** | `review2/e2e-cat-037.log`, `review2/cat-037-socket-choice-drawer.png` |
| Cumulative | same loader, all 32 `tools/e2e/*.test.ts`, serial, one Chrome | **117/132**, 0 skipped, 23.8 min | `review2/e2e-all.log`, `review2/per-suite-counts.txt`, `review2/failing-section.txt` |
| Retained harness | `node --experimental-vm-modules --test tools/workshop-rigid-body.test.mjs` | 1 pass / 50 fail / 10 skipped (failure at module load) | `review2/rigid-body-harness.log` |

**Cumulative per suite:**
- **Passing at 100%:**
  - ANIM-1a 3/3, ANIM-1b 3/3, ANIM-1c 4/4;
  - CAT-002 5/5, CAT-014 4/4, CAT-017 5/5, CAT-020 5/5, CAT-023a 4/4, **CAT-023b 4/4**, CAT-030 7/7, CAT-037 6/6;
  - CAT-048a 4/4, CAT-048b 5/5, CAT-049 5/5, CAT-050 5/5, CAT-051 5/5, CAT-052 5/5, cat-062 7/7;
  - ENGINE-CORE 2a1 5/5, 2a2 4/4, 2a3 5/5, 2a4 2/2, 2b1 1/1, 2b2 1/1, 2b3 1/1, 2b4 2/2, 2c 2/2.
- **Failing, all in suites accepted as carried by Story 9.4b (owner decision D):** cat-005 2/7, cat-015a 1/2, cat-015a-sidekick 2/3, cat-015b 1/4, cat-062b 2/7.
- **First failing assertions** are identical to round 1: store credit ×3, budget, null `.enabled`, "Later empty contact", `paid.length>0` / `powered.length>0`, guide 0.5 ≠ 0 ×4, goal_body 1n ≠ 4n.
- **No suite outside the accepted six failed.**

### R2.2 Round-1 findings

| ID | Status | Evidence |
| :-- | :-- | :-- |
| F1 icon-only drawer | **Resolved** | Drawer screenshot (`review2/cat-037-socket-choice-drawer.png`) shows a filled navy raised bar and a hollow navy ring with no text. cat-037 asserts navy and cream pixels and the absence of text at five x positions, selects the choice by icon, and the save decode shows SetIn/ResetIn. `SocketIconButton`/`WorkshopIcons` `socket_set`/`socket_reset`; "→" replaced by "to". |
| F2 qualification | **Resolved** | `StateLatchTests.cs`: `Network_SetThenResetOnAdjacentTicks_TurnsOnThenOff` (On at 10, Off at 11, stays Off); `Network_SameTickConflictResolvesOffWhicheverSourceIsOrderedFirst` (Reset source first in node order; 3 substep orders × 2 batch orders); `Network_FailedOrDiscardedTickNeverCommitsAPendingSet` (throwing Consume leaves committed Off; a discarded candidate On is never committed; retry stays Off). |
| F3 Load assertion | **Resolved** | `cat-037.test.ts:413-416`: `load()` → `save()` → decode. |
| F4 dominance witness | **Resolved** | `cat-037.test.ts:361,368` asserts that switch target 7 animated; it passed. |
| F5 0.125 s | **Resolved** | `WorkshopCosmetic.cs:65` StateLatch SmoothStep 0.125. |
| F6 per-tick authoring read | **Resolved (owner A)** | See R2.3. |
| F7 binary16 lanes | **Closed by owner decision C/D16** | Moved to Epic 9A (9A.44); no new binary16 API in round 2. |
| F8 classification record | **Superseded** | Owner decision D accepts the six suites under 9.4b. |
| F9 `ActivationNodeKind.Latch` | **Resolved** | `ActivationNetwork.cs:12` `StickyFirstInput = 2`; `rg "ActivationNodeKind\.Latch\b"` → 0 hits; no alias. |

### R2.3 Impact investigation

**Compiled switched supply (owner A).**
- **Compiled once.** `WorkshopPhysicsCompiler.cs:285-297` compiles Battery → Latch/Counter → Powered Gate once per admission into `SwitchedSupplyRouteDeclaration` (`PhysicsDeclarations` `SwitchedSupplies`).
- **Device composes inputs from committed state.** `WorkshopGpuDevice.cs:67-73` keeps a `CompiledSupply` (routes, prismatic and source counts). `Advance` (`:75-90`) validates `WorkshopTickControls`, then calls `PhysicsTickInputs.Compose` (`SwitchedSupply.cs:37-45`), which reads only the compiled routes, the player's per-source enables and the committed `ContactClosed` value.
- **Engine reads only the explicit inputs.** `WorkshopPhysicsEngine.Advance(byte[], byte[], PhysicsTickInputs)` (`:382`) sets `joint.SwitchedSupplyClosed = inputs.SwitchedSupplyClosed[index]` (`:612`).
- **Deleted, with zero references remaining:** `Program.SwitchedSupplies`, the `[JSExport] ElectricalState`, `SwitchedSupplyRoutes`/`Feed`.
- **Bistable admission set.** It is compiled once per `ConstructionRevision` (`WorkshopSimulation.cs:77-89`). The cache key is sound, because `Construct` admits only revision + 1 (`:154-156`).
- **No element-specific branch was added to the solver.** `ActuatorSliderDrive` is unchanged. The gate drag-code, joint-limit and plate-size heuristics remain, assigned to Epic 9A per owner A.

**Latch ActivationOut / LevelIndicator (owner B).**
- **Network.** `ActivationNetwork.cs:12` adds `LevelIndicator = 9`. Edge validation requires StateLatch-source ⇔ LevelIndicator-target (`:146`) and exactly one input per indicator (`:153`). The mirror is applied after Reset-dominant resolution on the same boundary (`:500-507`). `ValidateRead` treats indicators like sticky lamps through `EarliestInput` (`:561-569`).
- **Compiler.** It chooses `LevelIndicator` only for lamps wired from a latch (`:706-709`).
- **Authoring rules.** `WorkshopConnections.cs:172-178` adds two: a latch output may target only a Signal lamp, and a lamp following a latch takes no other signal. Both apply only to latch-sourced links, so every existing lamp fan-in stays valid.
- **Save, wire and levels.**
  - No codec or schema change: socket 1 already existed, and `WorkshopSaveCodec`/`WorkshopWire*` were not modified in round 2.
  - Existing saves and levels contain no latch ActivationOut link and load unchanged; the lesson and puzzle suites pass.
  - Unsupported shapes reject atomically through `Validate` (`Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes`).
- **requirements.md CAT-037:** the edit is additive only.

**Lamp animation change (every lamp now on the Memory path).**
- `CosmeticCurves.SignalLamp` changed only its source, Activation → Memory. The curve and duration (SmoothStep 0.16 s) equal HEAD, so DESIGN.md:332 is no worse than before.
- `PumpMemory` acts only on Memory-declared owners. A sticky lamp's committed value never clears within a run, so it still latches.
- Every lamp consumer passed in Chrome: ANIM-1a/1b/1c, CAT-002, 017, 020, 023a, 023b (4/4, including the lamp-glow history and CCGOAL_SOLVED), cat-062, the pipe/funnel level suites, and cat-037's Signal-lamp bulb (dark 151,169,170 / lit 255,255,255).

**`tools/workshop-rigid-body.test.mjs`: a broken retained tool, broken before round 2.**
- **What fails.** 50 of 61 tests fail at module load: `Export 'applyConstraintImpulse' is not defined`.
- **Why.** The harness imports JS kernels that `CuriousContraptions.Simulation/wwwroot/worker.js` no longer exports. `worker.js` was last modified 17 Oct 21:15, before Story 9.4 began, and is untouched in round 2.
- **The stub.** Its `ElectricalState` stub (`:858`) is therefore dead. 9.4's export removal neither causes nor changes the breakage.
- **Why it isn't simply dead.** It is still advertised as a current tool in `tools/e2e/README.md:20` and `docs/simulation-presentation-bridge.md:105`. Under the zero-legacy rule it must be deleted or repaired, which belongs to Epic 9A / 9.4b, not to 9.4.

**Index alignment of compiled routes and source enables (traced; multi-route untested).**
- **Prismatic index.** Route indices refer to `_prismatics` sorted by Id (`PhysicsDeclarations.cs:307-321`). The ABI writes prismatic record *i* from `scene.Prismatics[i]` (`PhysicsGpuAbi.Constraints.cs:16-19`), and the engine reads record `index` and sets `SwitchedSupplyClosed[index]` (`WorkshopPhysicsEngine.cs:584-612`).
- **Source index.** Route source indices refer to `scene.Electrical.Sources`. The ABI writes source *i* from that table (`PhysicsGpuAbi.Electrical.cs:18-22`); the committed read returns sources in ABI index order (`:43-55`), and that read feeds `WorkshopTickControls.For` (`SwitchedSupply.cs:15-21`). The engine reads `inputs.SourceEnable[supply]` by ABI index (`WorkshopPhysicsEngine.cs:684`).
- **Conclusion.** Every order is identical by construction. However, `Compiler_SwitchedSupplyRoutesCompileOnce…` covers a single route only (R2-F4).

**Source-enable path now carried by tick inputs (`ElectricalState` export deleted).**
- **Masked suites.** The suites that exercise it (cat-005, cat-015*) are among those accepted under 9.4b. Their failure locations (file:line:col) are **identical** in round 1 and round 2 (`diff` of `review/failing-section.txt` vs `review2/failing-section.txt`, excluding cat-023b, which now passes).
- **Independent probe on a restarted preview.** Direct Battery → Powered Gate, all through the UI (`review2/probe-r2.log`, script `review2/murdoch-probe-r2.test.ts.txt`):
  - Run: open at 4.55 m;
  - in-run **Disable supply**: closed at 3.00 m;
  - **Enable supply**: open at 4.55 m;
  - 70 publications per phase, no page errors.
- **Conclusion.** No regression is hidden behind decision D. cat-037 test 3 separately proves the switched route.

**Delegated playability decisions (CAT-037 §6): sound.**
- **Level output drives only Signal lamps.** It rejects occurrence consumers with an explicit message; their edge semantics were undefined.
- **One cause per following lamp.**
- **Socket placement and chevron marker**, kept away from the Set/Reset and power sockets.
- **One Memory presentation path**, with no visible change for existing lamps.
- **"to" for the missing glyph.**
- **None of these waives source acceptance.** The owner's Q1 decision ("high while On, low while Off") is met. The lamp-only restriction is a recorded playability limit, not a reduction of epics 9.4 acceptance.

### R2.4 Findings (round 2)

| ID | Severity | Location | Finding |
| :-- | :-- | :-- | :-- |
| R2-F1 | Low | `engine/gpu/WorkshopPhysicsCompiler.cs:204` | Stale comment: "resolved per tick from committed contact state (SwitchedSupplyRoutes)". That type is deleted, and routes now compile at Run (`:285`). |
| R2-F2 | Low (pre-existing, not 9.4) | `tools/workshop-rigid-body.test.mjs:42,858`; `tools/e2e/README.md:20`; `docs/simulation-presentation-bridge.md:105` | Broken retained harness (above). Schedule deletion or repair in Epic 9A / 9.4b. |
| R2-F3 | Low | `CuriousContraptions.tests/StateLatchTests.cs:67` | The test name `LatchScene_ExtractsHousingFootAndFourPortLocators` now asserts five locators, including `activation_out` (`:87`). |
| R2-F4 | Low (test gap) | `CuriousContraptions.tests/StateLatchTests.cs:347` | The compiled-route fact uses one gate and one battery, so the route always sits at prismatic 0 and source 0. Alignment is correct by trace (R2.3), but no test pins a switched gate that is not the first prismatic (for example Springboard plus gate, or two gates with only the second switched) or a second battery. |

### R2.5 Terminal verdict (round 2)

**Pass**, scoped to Story 9.4 (CAT-037):
- Every round-1 finding is resolved or closed by an explicit owner decision.
- Owner-widened scope A and B is delivered, with no unintended regression in any suite outside the six that owner decision D accepted under Story 9.4b. Those six fail exactly as before 9.4.
- Gates: build 0 errors (IL2125 only, Story 8.1), unit 961/961, anvil 0, publish exit 0, cat-037 6/6, cumulative 117/132.
- The four Low findings are non-blocking.

**Decision sources.**
- **Recorded (owner, 18 Oct 2026):** decisions A (F6 fixed in 9.4), B (ActivationOut) and C (binary16 → 9A.44) are recorded in `epics.md:107-109` (open scheduling item g, follow-on owner decisions).
- **Round-1 D1:** now scheduled as Story 9.4c (`epics.md:873`).
- **Decision D, not separately recorded.** The acceptance of the six pre-existing suites for 9.4 completion appears in the repo only as Story 9.4b's scope (`epics.md:850-868`). I found no separate owner record that it satisfies DoD #4 for Story 9.4. This Pass relies on the coordinator's statement of decision D, and the record should be added before sprint-status is updated.

This is a scoped Pass. It is not global or runtime qualification. Detailed performance and the full 100% cumulative baseline remain at Story 9.4b and their named gates. `sprint-status.yaml`, the spec and all code were not modified by this review.

---

## Re-review (round 3) — 2026-10-18

**Scope.** Only R2-F1, R2-F3 and R2-F4. Raw logs: [`evidence-9-4/review3/`](evidence-9-4/review3/).

- **Change set.** Two non-documentation files changed after my round-2 evidence (`review2/probe-r2.log`):
  - `engine/gpu/WorkshopPhysicsCompiler.cs` (runtime);
  - `CuriousContraptions.tests/StateLatchTests.cs` (test).
  
  Amelia's recorded edit to the compiler is comment-only, at `:204-205` (`review3/amelia-edits-all.txt`). No other runtime file changed, so no runtime behaviour changed and Chrome was not re-run. Hashes match Amelia's `round3/identity.txt`: compiler `9393fa20…`, tests `860f8fb5…`.
- **R2-F1: resolved.** The comment now describes compile-once routes through `PhysicsSceneDeclaration.SwitchedSupplies` and `PhysicsTickInputs`. No reference to `SwitchedSupplyRoutes` remains.
- **R2-F3: resolved.** The test is renamed `LatchScene_ExtractsHousingFootAndFivePortLocators`, and `FourPortLocators` has 0 hits.
- **R2-F4: resolved.** `Compiler_TwoSwitchedGatesEachFollowOnlyTheirOwnRoute` covers three gates (an unswitched one at prismatic 0) and two batteries (B authored Disabled). It asserts that each route's prismatic and source index equals its own gate's and battery's index, and that both indices are non-zero. Mental mutation shows the test fails if routes are crossed:
  - a swapped prismatic or source index fails the index assertions;
  - a route reading the other battery fails the "crossed" case (latch A closed, battery A off, battery B on ⇒ gate A must stay closed);
  - a route reading the other latch fails the "onlyB" case;
  - an unswitched gate driven by a route fails `both[plain] == false`.
- **Gates.**

  | Gate | Result | Log |
  | :-- | :-- | :-- |
  | `dotnet build CuriousContraptions.slnx --no-incremental` | 0 errors, 1 warning IL2125 (Story 8.1) | `review3/build.log` |
  | `dotnet test CuriousContraptions.slnx --no-build` | **962/962** | `review3/dotnet-test.log` |
  | Filter on the new and renamed facts | 2/2 | — |

**Terminal verdict (round 3): Pass**, scoped to Story 9.4. The round-2 Pass stands, with R2-F1, R2-F3 and R2-F4 resolved. R2-F2 (the broken retained rigid-body harness, pre-existing) remains for Epic 9A / 9.4b. The decision-D recording caveat from R2.5 still applies. `sprint-status.yaml`, the spec and all code were not modified by this review.
