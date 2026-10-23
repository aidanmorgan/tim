---
title: "C-Clock — System Clock generic instance with proven pulses (9A.42 + 9R.10)"
status: review
baseline: 146588838b97d9634406c47f9e383b3dd24f8fa6
---

# Outcome and authority

The System Clock (CAT-017) becomes a `WorkshopGenericInstance` with one parameter, `interval_seconds`. Its Oscillator node is selected by a declared signal capability. `WorkshopClock`, `WorkshopInput.Clock`, the Clock panel, every arm that names them and the last reader of the `threshold` wire field all leave the engine.

The pendulum and the per-pulse flash are proven natively through the existing declared bindings (`CommittedOscillatorPhase`/`Pulse`, 9A.25a), which read the committed oscillator row (9R.1).

Roadmap position 42. Start after C-Plate merges, and re-derive every count there.

**Authority:**
- epics C-Clock, 9A.42, 9R.10 and 9.3;
- CAT-017 §2, §4 (C11–C23) and §5;
- the coordinator's decision of 18 Oct 2026 (pendulum and flash);
- C-Plate D5 ("`threshold` stays for the Clock");
- the 9A.32, C-Lamp+Detector and 9A.39 precedents.

Native real-simulation C# proof is PRIMARY. Browser proof is pending.

**Facts at 61ded7ad:**
- The arms are listed under Deletions.
- Already generic: the body, the supply, the presentation and the memory row. The physics compiler has no clock loop.
- `DefaultIntervalSeconds` and `OfType<WorkshopClock>` are already absent.
- 0 lessons use the clock.
- There are 30 `Owner: Clock` Known rows.
- There are 35 `WorkshopInput.Clock(` test sites in 14 files.
- **Defect (fixed in this slice):** the panel shows `MachinePart.CanonicalIntervalSeconds`, and nothing in production sets it, so the panel shows a stale 1 s.

# Finite design decisions

- **D1 Declared oscillator.**
  - Add `SignalOscillatorDescriptor(string IntervalParameter)` and `IOscillatorElement : ISignalElement`.
  - Add `SignalPayload.Oscillator?` with a round trip. The package declares `("interval_seconds")`.
  - The validator returns a `BundleError` for:
    - a second node-selecting capability;
    - a parameter that is not Real Second with a finite minimum > 0;
    - no Pulse `ActivationOut`, or any `ActivationIn`;
    - no `PowerIn` with a Consumer node;
    - a body that is not Static;
    - a `CommittedOscillator*` binding without the capability. Today that case only fails at runtime.
- **D2 Compile and lanes.**
  - `WorkshopActivationCompiler` reads `Signal?.Oscillator` from a generic instance. It emits `Oscillator` with `IntervalTicks = ActivationTimerDeclaration.Duration(param, cadence)`, the `SupplyTopology` power source (more than one source is refused, `PowerInputSupplied`) and the collider `PartIdentities + 1`, in instance order.
  - Lanes:
    - the interval is f32 from authoring to compile;
    - `IntervalTicks` is uint and `DueTick` is ulong;
    - `ActivationTime` is (uint, f32).
    - The clock path has no Half lane.
  - Parity: the old `(uint)Math.Ceiling(s * (uint)rate)` equals the new value for **every** f32 in [0.1f, 12f] at Hz60, Hz120 and Hz240. An exhaustive one-off probe keeps its receipt. A pinned test covers 0.1f, 1f, 12f and their ±1-ulp neighbours.
- **D3 Parameter policy.**
  - `interval_seconds`: Real, Second, 0.1–12, default 1.0.
  - Editor policy `Clamp`, which preserves the SpinBox behaviour. Record the change in bundle identity.
  - `BuildOnly`.
  - The construction, save and wire boundaries refuse 0, −1, 0.01, 13, NaN and +∞ (C19). Text and NaN entered in the editor are refused.
- **D4 Presentation.**
  - The bindings are unchanged.
  - "Held while paused" means the follow target and the input are unchanged and the sample converges (C20). It does not mean the sample is frozen.
  - The flash fires once per increment of the committed pulse count.
  - Hidden catch-up stays at or below 64 impulses. Saturation (C26–C29) stays at its later gate.
- **D5 Wire, save and content.**
  - Delete `threshold`. Slots: target_count 8→7, parameters 9→8. `InstanceRecordFields` 10→9.
  - `AuthoredScalar` becomes {Length}. Remove `"Threshold"` from the 28 templates. `CarriesTypedField` becomes {length, target_count}.
  - Old saves (every C-Plate-era save holding a generic part included) are refused **typed at both boundaries, measured at base and candidate, with 0 untyped**. A record that decodes to a different valid value is a **Fail**.
  - C-Plate's pinned pre-conversion plate refusals and the 9A.32 old-save tests are expected-change sites, attributed one by one and never tuned.
- **D6 Editor.**
  - The generic `AddDeclaredConfiguration` reads the instance value.
  - An unchanged Apply already skips Undo (`ApplyDeclaredParametersAsync` `Equals` guard), which matches the old panel.
  - A move keeps the interval (`CaptureParameters`).

# Ordered tasks

- [x] **Suite-first native baseline (record path).**
  - Add a `PlacedParts.Clock` adapter for the 35 sites.
  - Run the guard classes on one build:
    - Clock, DeclaredPresentation, StateLatch and BothGate;
    - parity, capacity and save/wire/decode;
    - lessons.
  - Add 9R.10's native pause, reset and unsupplied facts.
  - Record golden traces:
    - 1 s, 0.1 s and 12 s;
    - deadline loss (C13) and restore;
    - clock→counter→gate;
    - clocks→latch.
  - Record the 12×3 lesson probe.
  - Pin the base-encoder old-save sha256s: clock at 0.1, 1 and 12; a wired bench; counter, pipe, latch and plate; every generic.
- [x] **Capability (D1–D2):**
  - contracts, payload, validator cases and round trip;
  - package parity, and the exhaustive probe;
  - retarget `PackageRuntimeParityTests:373/432` to the declared value and the compiled `IntervalTicks`.
- [x] **Conversion.**
  - Generic `CaptureInstance`, `MapInstance` and prevalidation.
  - Drop the encoder case, the whitelist entry, the activation arm, the panel and its dispatch.
  - Make the comments generic.
  - `cat-017` (strict tsc only):
    - Replace the ball-only pause check with committed rows unchanged across a 2.0 s pause.
    - Replace the blade-only reset check with "snapshot equal, then the first pulse at tick 120".
    - Add 5 pulses at 120…600, an unsupplied control and the `interval_seconds` read.
  - `workshop-driver.ts` has no presentation or pixel read, so the pendulum and flash Chrome reads stay pending.
- [x] **Wire regeneration (D5).**
  - Regenerate with pinned flatc 25.2.10. The unchanged schema must reproduce the committed output first.
  - Update the `workshop-wire.ts` tail, `run-reset-snapshot.ts:57` and `content/`.
  - Change the `DeclaredOrientationTests` slot literal 10→9, and re-kill the M11-style mutant.
  - Re-check the `cat-052*` reads.
- [x] **Deletions: 0 production hits.** Record the base counts as the control.
  - `rg -w "WorkshopClock|DefaultIntervalSeconds|AddClockConfiguration|ApplyClockInterval(Async)?|(Placed|Min|Max|Canonical)IntervalSeconds|ApplyIntervalSeconds"`
  - `rg 'WorkshopInput\.Clock\b|"ClockInterval"|InstanceRecord\.(Add)?Threshold\b|AuthoredScalar\.Threshold|\b(r|inst)\.Threshold\b'`, plus `'"clock"'` in `engine/gpu`, `MachineWorld.Gpu.cs` and `ui`, and `'"Threshold"' content`.
  - Delete `WorkshopClock.cs` and its `.uid`.
  - Remove the `ApplyClockInterval*` entries from `.editorconfig:70` and `CSharpStaticGateTests` (65–66, 168 and 810).
- [x] **Known rows shrink only.**
  - Remove exactly the 30 Clock rows (166 → 136). Add none.
  - The 2 `PartId.Clock` FinalIsolation rows stay.
- [x] **Latest-design alignment.**
  - CAT-017 §2/§3: the declared capability.
  - Correct the stale "ActivationTime binary16" note.
  - Mark §6 Q1 (powered) decided.
  - Update the isolation-doc A3/A5/A6/A11 row.
  - No alias, bridge or fallback.
- [x] **PRIMARY native proof** (production compile, worker engine, tick progression):
  - *Period:*
    - 1 s over 5.0 s gives exactly 5 pulses, at 120·k.
    - The count equals ⌊tick/interval⌋ over 7200 ticks for 0.1, 1 and 12 s at each of the 3 cadences.
    - There is no startup pulse.
  - *Occurrences:* `OscillatorTick` from the node, caused by the clock's collider. The counter counts every pulse.
  - *Committed read:* the checkpoint holds Running, due, count and last. `Consume` run twice is identical. A discarded tick leaves the row unchanged.
  - *Presentation outputs:*
    - The swing is `.5+.5·sin(2π·phase)` at known ticks, and Stopped gives .5.
    - One flash per pulse.
    - Pausing behaves as D4.
    - On hidden→shown, retained pulses play on the first visible frame.
    - Reset returns the swing to rest, cancels the flash and leaves the lamp slate.
  - *Editor:* D3 and D6 at the native seam:
    - clamp at 0.05 and 13;
    - NaN and text are refused;
    - Cancel;
    - one Undo restores 1.0;
    - Running, Paused and locked are refused;
    - the displayed value equals the instance value (the defect fix).
  - *Negative and boundary:*
    - unsupplied: Stopped and 0 pulses;
    - deadline loss: no pulse;
    - restore: a full interval, with no catch-up;
    - two sources: refused;
    - a typed field or unknown parameter: refused at both boundaries;
    - the validator edges.
  - *Lifecycle:*
    - Run/Reset is exact: Stopped/0/0, rows deep-equal, replay identical.
    - Save/load replays identically. Counts are not saved.
  - *Old saves:* the D5 matrix at both boundaries.
- [x] **Identity.**
  - The traces and nodes are bit-identical.
  - The `compiled-geometry-9a16` clock row is DeepEqual.
  - These are identical: 12×3 lesson bytes and the state at 480 ticks, and placed solutions at 1200 ticks.
  - The fixtures are untouched.
- [x] **Checks.**
  - One Debug slnx build with 0 warnings.
  - Then, under bash: `dotnet test CuriousContraptions.slnx --no-build --no-restore --blame-crash`.
  - Run the CI "Test C# tools" step verbatim under bash.
  - Release worker; web publish with `AppBundle` moved out.
  - Strict tsc 6.0.3 and the snapshot unit.
  - `anvil_check` and `git diff --check`.
  - Mutations, each of which must be killed:
    - the interval is ignored;
    - the power gate is removed;
    - Ceiling becomes Floor;
    - exclusivity omits the oscillator;
    - the binding coupling is removed;
    - Clamp becomes Reject;
    - the table size goes from 9 to 10.
- [ ] **Independent review, then SnapshotApproval, then publication.**
  - Commit, merge and push the exact snapshot. Verify the exact-SHA CI, artifact and deployment.
  - Mark `9a-c-clock-conversion` and `9a-42` done.
  - Mark `9r-10` done with a native scoped Pass; Chrome stays post-catalogue, as with 9r-15.
  - `9-3` stays in-progress, with a native-only addendum.
  - Record the terminal scoped Pass. Browser qualification stays pending.

# Pending browser cases (post-catalogue)

- `cat-017`:
  - 5 pulses from committed reads;
  - a 2.0 s pause adds nothing;
  - after Reset, the first pulse arrives at 1.0 s;
  - the unsupplied control;
  - pendulum and flash pixel/binding reads at known pulse ticks (this needs a driver presentation read).
- The generic editor through the UI: clamp, NaN, Cancel, Undo, and Save/Load at 0.1 and 12.
- An `engine-core-2b1` variant: a C-Plate-base save holding a generic part is refused, and the bench is kept.
- Re-run `cat-020`, `cat-037`, `cat-051-interrupt`, `cat-052*`, `palette-placement` and `engine-core-2b1`.

# Open questions

None owner-only.

Out of scope:
- The Description's stale "each second".
- Pulse-queue bounds (§6 Q2).
- 9A.44.

# Code Map

- **Change:**
  - `Abstractions.Signal/SignalContracts.cs`, `Bundle/{BundleSchema,BundleValidator}.cs` and `elements/SystemClock/SystemClockElementDeclaration.cs`.
  - `engine/gpu/{ActivationNetwork,ActivationMemory,WorkshopConstruction,WorkshopInstances,WorkshopWireFlatBuffers,WorkshopSavePrevalidation,AuthoredInstanceTemplate}.cs` and `engine/{MachineWorld.Gpu,MachinePart}.cs`.
  - `ui/WorkshopConfiguration.cs` and `.editorconfig`.
  - The schema and the generated C#/TS, and `tools/e2e/{run-reset-snapshot,cat-017.test}.ts`.
  - `content/puzzles.json`, the Known fixture and the tests.
  - CAT-017, the isolation doc, `TODO.md` and `sprint-status.yaml`.
- **Delete:** `engine/gpu/WorkshopClock.cs` and its `.uid`.
- **Unchanged:**
  - the oscillator law;
  - the presentation evaluator;
  - `PartId.cs`;
  - the E2eRunner maps;
  - `compiled-geometry-9a16.json`;
  - the `WorkshopClockMapping`/`Wire`/`Derivation` types.

# Current state

*Implementation owner, 23 Oct 2026. Uncommitted native candidate in worktree `story-c-clock` on base `14658883`. Tracked diff (HEAD, `_bmad-output` and `TODO.md` excluded) sha256 `781d8edd…`; new files `ClockGuardTests.cs` `364021b3…` (unchanged since the base run), `DeclaredOscillatorTests.cs` `7aa5e0f7…` (`evidence-c-clock/candidate/identity.txt`). Awaiting independent review. Browser qualification pending.*

**Re-derived on 14658883.** Known: 30 `Owner: Clock` rows of 166 (→ 136); the 2 `PartId.Clock` FinalIsolation rows stay. 35 `WorkshopInput.Clock(` sites in 14 test files. 28 `"Threshold"` content scalars. Base deletion-grep controls A/B/C/D/E = 30/7/3/28/9 (`baseline/deletion-greps-base.txt`).

**What changed**
- `SignalOscillatorDescriptor(IntervalParameter)` / `IOscillatorElement`; `SignalPayload.Oscillator` (trailing optional) with round trip. The package declares `("interval_seconds")`, the parameter's editor policy is now `Clamp` (Build-only), and its dead `clock_tick` trigger is dropped (no consumer reads declared triggers; the C-Plate precedent). Bundle text changes accordingly: base `23f6a937…` → candidate `5bf3d63a…`, only `clock` changed semantically plus a `null` Oscillator on every other signal entry (`candidate/bundle-identity.txt`).
- Validator (D1), each one `BundleError`: seventh node-selecting capability; interval a Real Second parameter with finite minimum > 0; a Pulse `ActivationOut` and no `ActivationIn`; a `PowerIn` Electrical input and a Consumer node; no Dynamic body, no slider, at least one collider (the clock has no mechanical payload: absent is Static); a `CommittedOscillator*` binding without the capability (`Presentation.Oscillator`, now a bundle error, formerly only a runtime failure).
- `WorkshopActivationCompiler` generic arm (generic identifiers only): same node fields as the record arm; `IntervalTicks = ActivationTimerDeclaration.Duration(param, cadence)`; one upstream source else `PowerInputSupplied`; collider `PartIdentities + 1`.
- Deleted: `WorkshopClock.cs` (+`.uid`), `WorkshopInput.Clock`, the `CaptureInstance`, `MapInstance`, encoder and save-prevalidation arms, the whitelist entry, `MachinePart.CanonicalIntervalSeconds/ApplyIntervalSeconds`, the Clock panel and its dispatch, the `.editorconfig`/`CSharpStaticGateTests` handler entries, the wire `threshold` field (schema, generated C#/TS regenerated with pinned flatc 25.2.10, `InstanceRecordFields` 10→9, `CarriesTypedField` = length/target_count), `AuthoredScalar.Threshold`, 28 content scalars, the TS dump tail and `SNAPSHOT_SCHEMA` entry. The panel defect is gone with the panel: the generic editor shows the instance's value.
- Comments in `ActivationNetwork`/`ActivationMemory` made generic (two exception messages say "oscillator"; no test asserted them).
- `cat-017` (strict tsc only): wiring reads `interval_seconds`; test 4 now asserts a 2.0 s pause commits no tick and leaves the clock and counter rows unchanged (one Step later), then after Reset the first pulse is committed at tick 120 (counter `last_ordinal` 480); new test 6 (one pulse per 120 ticks over 5.3 s, next due one interval later) and test 7 (unsupplied clock with a basketball: Stopped, 0, no due; Reset restores). Pendulum/flash pixel reads stay pending (the driver has no presentation read). None of these recipes has run in Chrome.
- Docs: CAT-017 §2/§3 (declared capability; the stale binary16 note superseded), §6 Q1 decided; isolation A3/A5/A6/A11 row.

**Decisions and deviations (reviewer to confirm)**
1. **Dropped `clock_tick` trigger** (game data, no runtime consumer; bundle identity recorded).
2. **Oscillator `Last`.** The spec's "checkpoint holds … last" and CAT-017 C11's "last = tick" are not true of the oscillator row: `ActivationMemory.Validate` has required `Last == default` for an oscillator since 9R.1, unchanged here. A pulse's time lives in the clock's activation latch (its first `OscillatorTick`, held: ordinal 480 at tick 120) and in each downstream counter's `Last` (tick × 4 per pulse). `ClockGuardTests` pins exactly that on both paths.
3. **9R.10 "native `ClockTests` gain pause and reset facts"** is met by `ClockGuardTests` (pause, resume, Reset, unsupplied, presentation), a sibling class run unchanged on the record path and candidate; `ClockTests` keeps its record-era facts converted.
4. **Cadence matrix.** The 3×3 floor-count matrix and the ±1-ulp parity pins run through production `Compile` + `Consume` (the real network); pause, reset and save/load run on the full engine at 120 Hz. Correction (review L3): the production `WorkshopSimulation` does run Hz60 and Hz240 (the reviewer's probe); only the shared `WorkshopPhysicsEngineTests.Run` helper is fixed at 120 Hz.
10. **Review B1 (forward fix).** `ClockGuardTests.TwoSourcesOnOneClockAreRefused` (kept byte-identical as the suite-first copy) is misnamed: it wires two batteries straight to the clock's PowerIn, which connection validation refuses first ("This power input already has a supplier."), so it never reaches the oscillator arm's own refusal. `DeclaredOscillatorTests.TwoUpstreamSourcesThroughALatchAreRefusedByTheOscillatorItself` routes two batteries through a latch (construction validation admits it) and asserts the arm's reason and message, with a one-battery control; it kills R8. L1: `ANegativeZeroLengthIsATypedFieldOnAGenericRecord` pins a stored −0 length as a typed field (+0 control loads), killing R6. L2: CAT-017 C11 now records where the pulse time lives.
    Receipts (`evidence-c-clock/forward-fix-b1/`): one Debug build, 0 warnings; full slnx `--no-build --no-restore --blame-crash` 17/167/77/189/2628, 0 skipped, 0 duplicate-ID notices, rc 0; `git diff --check` clean; `anvil_check` 0 findings; R8 and R6 killed (`mutation/R8-…`, `mutation/R6-…`) and restored; `ClockGuardTests` byte-identical to base. Tracked diff now `7e19246c…`; `DeclaredOscillatorTests.cs` `4f22025e…`.
5. **`PackageRuntimeParityTests:373`** is retargeted to the declared default (`1f`); the compiled `IntervalTicks` is asserted by `ClockGuardTests.TheCompiledIntervalIsTheCeilingOfTheF32ProductAtTheBoundaries` and `DeclaredOscillatorTests.TheDeclaredOscillatorCompilesItsExactNode` rather than in that table.
6. **NaN by boundary** (D3): `NonFinite` at the save boundary, `OutOfRange` at the command boundary (shared generic rule, C-Plate precedent).
7. **Old saves (D5).** All 35 base-encoder kinds (`baseline/old-saves`, sha256 pinned): base loaded all 70 decodes; candidate refuses 66 typed (0 untyped) and loads pipe and latch with identical outcomes (their slots did not move). A counter's old target (slot 8) is now the parameters offset: 3 → `VerifierRejected`, 4/8 → `MissingField`; all targets 1–9 refused typed at both boundaries (`DeclaredOscillatorTests`; the 1–9 breadth uses the raw `OldLayoutSaves` writer at revision 1, so only targets 3, 4 and 8 are proven base-encoder bytes, the base saves being revision 2). Generic records of that layout wrote parameters in slot 9, beyond the 9-field table.
   Sprint status: `9a-c-clock-conversion` and `9a-42` → review; `9r-10` stays backlog with "native guard green; Chrome ACs pending" (the 9r-9/9r-16 precedent) until the terminal Pass; `9-3` unchanged (in-progress).
   Scripts cited by the receipts are archived in `probe/` (`regen.sh.txt`, `mutant.sh.txt`, `ci-tools.sh.txt`, `deletion-greps.sh.txt`). After the freeze run only `TODO.md`, `sprint-status.yaml` and this spec changed; their readers (`ElementIsolationTests`, `WorkshopPhysicsLawTests`) were re-run on the frozen build: 98/98 (`candidate/freeze-test.txt`).
8. **Expected-change sites** (attributed one by one; none tuned to pass a different meaning):
   - `DeclaredContactLoadTests` (C-Plate's 61ded7ad plate saves): lone plates keep the typed-field message; `lamp_wired`/`two_plate` now refuse first at the battery ("Invalid declared parameters.", its parameters in slot 9 beyond the table). Both OutOfRange at both boundaries.
   - `DeclaredOrientationTests`: 2065d3c5 plate/clock now OutOfRange (old slot 9 beyond the table → declared parameter missing; was VerifierRejected); 2065d3c5 pipe now VerifierRejected at both boundaries (old slot 8 is the parameters slot; was OutOfRange/MissingField; its `Open().TargetCount` inspection removed); the first-slot-beyond boundary moves 10 → 9 (`Layout(10, 8, …)`), killing M7.
   - `PayloadInstanceTests`: pre-conversion domino now VerifierRejected (was OutOfRange); `TypedField` = {Length, TargetCount}; clock rows added with a load control.
   - `WorkshopSaveRejectionTests`: `Field` enum loses `Threshold` (slots renumbered); the impact switch's obsolete-scalar case uses `Length`; "ball carrying a typed threshold" → "typed target count".
   - `WorkshopWireFlatBuffersTests`: `DeclaredSetting` adds `PreConversionClockInterval` (slot 7 value → typed field; 0 → missing parameter) and the clock's own parameter (1 loads, 0 refused).
   - `ConstructionDecodeBoundaryTests`: the clock decodes through its declared parameter; `ACounterThresholdIsNotItsTarget` → `ACounterLengthIsNotItsTarget` (the threshold field no longer exists).
   - `ClockTests`: record validation/ticks fact → declared parameter validation and compiled ticks; wire round trip reads the generic parameter and asserts instance equality.
   - `WorkshopHandlerBoundaryTests` vacuity floor 16 → 15: forced by deleting the `ApplyClockInterval` handler, not a weakened gate.
9. **Process disclosures.** `anvil_validate_write`/`anvil_apply_patch` ran before every write except: `ProbeClockCapture.cs` and `DeclaredOscillatorTests.cs` were written first and validated immediately after (preview + sha256); `ClockGuardTests.cs` was validated by preview + sha256; the Known-fixture removal was validated by a representative removal-only diff; the first validated cat-017 diff differed from what was applied (the final test 7 was re-validated); the DeclaredOscillator counter rows were validated with abbreviated base64. M5's first run did not compile (IDE0060) and was rerun with a compiling mutant. Shell operations: `git rm` of the two deleted files; `mv` of the probe and of the published `AppBundle` to the scratchpad; `cp` of the adapter, probe, guard and grep script into evidence; a throwaway `git worktree` at 14658883 (bundle build only), removed; `python3 -I` read-only JSON comparison of the bundles.

**Evidence** (`_bmad-output/implementation-artifacts/evidence-c-clock/`)

| Check | Receipt | Result |
|---|---|---|
| Base build + full suite (test-only diff `baseline/test-only.diff`, probe included) | `baseline/build.log`, `baseline/full-test.log` | 0 warnings; 17/166/77/189/2600, rc 0 |
| Base guards (22 classes incl. ClockGuardTests) | `baseline/guards.log` | 740/740 |
| Traces ×7, 12×3 lessons, 12×3 placed solutions, exhaustive parity | `baseline/capture/`, `candidate/capture/`, `candidate/capture-compare.txt` | all byte-identical; parity 57,881,396 f32 × 3 cadences, 0 mismatches |
| Old saves (35 kinds × 2 boundaries) | `baseline/old-saves.sha256`, `candidate/capture/old-save-outcomes.txt` | base 70 LOADED → candidate 66 typed refusals, 0 untyped, pipe/latch identical |
| Wire regeneration (flatc 25.2.10) | `candidate/wire-regeneration.txt` | equal to fresh output (pre-existing C# header line aside) |
| Bundle identity | `candidate/bundle-identity.txt` | base `23f6a937…` → `5bf3d63a…`; clock: Oscillator, Clamp, trigger dropped |
| Deletion greps | `candidate/deletion-greps.txt` | 0/0/0/0/0; `WorkshopClock.cs` absent |
| Known list | `candidate/known-rows.diff` | 166 → 136: −30, +0 |
| Frozen Debug slnx build (`--no-incremental`) | `candidate/freeze-build.log` | 0 warnings, 0 errors |
| Full `dotnet test … --no-build --no-restore --blame-crash` (bash) | `candidate/freeze-test.txt`, `freeze-test-full.log` | 17/77/167/189/2624, 0 skipped, 0 duplicate-ID notices, rc 0 |
| Guards on the frozen build | `candidate/guards.log`, `candidate/clock-guard.txt` | 745/745 (+5 wire DeclaredSetting rows, +1 editor, −1 net PayloadInstance rows); ClockGuardTests 56/56 |
| CI "Test C# tools" (bash, verbatim) | `candidate/ci-tools.log` | 77/167/17/174, rc 0 |
| Release Simulation, Animation.Worker; Release web publish | `candidate/worker-release.txt`, `web-publish.txt` | rc 0/0, 0 warnings; publish rc 0, static files present; AppBundle moved out |
| Strict tsc 6.0.3; snapshot unit | `candidate/ts-checks.txt` | rc 0; 24/24, rc 0 |
| `git diff --check`; `anvil_check` (53 files) | `candidate/diff-check.txt` | clean; 0 findings |
| Mutations M1–M7 | `mutation/*.log`, `mutation/restoration.txt` | all killed; restored |

**Mutations:** M1 interval ignored (24 failures), M2 power gate removed (24), M3 Ceiling→Floor (12), M4 exclusivity omits the oscillator (validator), M5 binding coupling removed (validator), M6 Clamp→Reject (editor + validator), M7 `InstanceRecordFields` 9→10 (boundary slot + old ball save).

**New and changed tests:** `ClockGuardTests` (56, suite-first, unchanged since base), `DeclaredOscillatorTests` (node identity, interval and parameter refusals at both boundaries, 8 pinned 14658883 saves, counter targets 1–9), `BundleValidatorTests` (+1), `BundleRoundTripTests` (oscillator), `PalettePlacementTests` (+1 editor: clamp, NaN/Infinity/text, Cancel, unchanged Apply, three Undos, move keeps the interval, Running/Paused/locked), plus the attributed sites above. `PackageRuntimeParityTests.CompiledGeometryMatchesPreCutoverGolden` (the clock row of the untouched `compiled-geometry-9a16.json`) passes on the generic instance.

**Pending browser cases (post-catalogue):** as listed above; cat-017 tests 4, 6 and 7 are new and unexecuted.
