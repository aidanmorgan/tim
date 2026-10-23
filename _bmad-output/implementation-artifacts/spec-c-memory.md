---
title: "C-Memory — Pulse Counter and State Latch generic instances; whitelist deleted (9A.41 + 9A.43)"
status: ready-for-dev
baseline: fc7f7cacccdde0177cb37ab776a502038d71c08d
---

# Outcome and authority

Counter (CAT-020) and State Latch (CAT-037) become `WorkshopGenericInstance`s, with their nodes selected by declared signal capabilities. These leave the engine:
- `WorkshopCounter` and `WorkshopLatch`;
- their factories and arms;
- the `target_count` wire field;
- the `WorkshopInstances.Validate` type whitelist.

This is the last record conversion. Only `WorkshopPipe` (owned by 9A.45) survives.

Roadmap position 46. Start after 9A.22 merges. Counts below are **at ae3cacf9; re-derive on the 9A.22 merge**. Cite symbols, not lines.

**Authority:**
- epics C-Memory, 9A.41, 9A.43, and 9.2/6.5f (closure table);
- CAT-020 §2/§5;
- CAT-037 §2/§5/§6 (ActivationOut level);
- 9A.22 D1 (`ThresholdReached`, `MemoryChanged`);
- 9.4c `LatchSupplyFixtureTests`;
- the C-Clock D1–D6 precedents.

Native real-simulation C# proof is PRIMARY. Browser proof is pending.

**Facts at ae3cacf9:**
- Arms:
  - `WorkshopInput.Counter`/`Latch`;
  - `CaptureInstance`, `MapInstance` and encoder `counter`/`latch`;
  - the prevalidation `case "counter"`;
  - `WorkshopActivationCompiler.Compile` `is WorkshopCounter`/`is WorkshopLatch`;
  - the whitelist `is not (WorkshopCounter or WorkshopLatch or IWorkshopDeclaredColliders)`.
- Already absent (record base-grep controls): `OfType<WorkshopCounter|WorkshopLatch>` and `is WorkshopLatch` in `WorkshopSimulation`.
- Admission is already by node kind (`BistableNodes()`: StateLatch, LevelIndicator, ContactLoadSource).
- Bodies compile from the declaration for every instance. `compiled-geometry-9a16.json` has counter and latch rows.
- Known: Counter 19 + Latch 14 = 33 of 136. The 9 Events rows belong to 9A.22.
- 0 lessons. 28 content templates hold `"target_count": 0`.
- Test sites:
  - `WorkshopInput.Counter(`: 29 in 14 files;
  - `.Latch(`: 30 in 12 files, including `LatchSupplyFixtureTests.Machine`.
- `PresentationDriver.CommittedCount`: only Counter.
- Dead declarations:
  - `SignalCounterDescriptor(Threshold: 3, ResetOnThreshold: false)`, which nothing reads;
  - `SignalLatchMode` (Toggle is unimplemented);
  - the triggers `pulse` and `set_pulse`.

# Finite design decisions

- **D1 Capabilities** (C-Clock D1 shape). Each is a trailing optional payload with a round trip, and each joins node-selecting exclusivity.
  - **Counter:** `SignalCounterDescriptor(string TargetParameter)` with `ICounterElement`. Counter declares `("target_count")`.
  - **Memory:** `SignalMemoryDescriptor(PortSocket Set, PortSocket Reset)` with `IMemoryElement`. The latch declares `(SetIn, ResetIn)`.
  - Delete `SignalLatchMode`, `ISignalElement.LatchMode`/`Counter` and the dead triggers, with no alias. Record the bundle identity.
  - The validator returns a `BundleError` for each of these:
    - the target is not WholeNumber/None within [1, 9] (the network invariant, which is kept);
    - the counter lacks a Pulse `ActivationIn` or `ActivationOut`;
    - Set or Reset is not a declared Pulse input;
    - Memory has no Level `ActivationOut`;
    - there is no `SwitchedContact` supply;
    - the body is Dynamic, or has a slider;
    - there is a `CommittedCount` binding without the counter capability.
- **D2 Compile.** Generic arms emit the identical `Counter` node (`TargetCount = (uint)param`) and `StateLatch` node, in instance order, using generic local names. The network laws and their bounds are unchanged.
- **D3 Parameter.**
  - `target_count`: WholeNumber, 1–9, default 3. The default equals the old `PlacedTargetCount`, so placement is bit-exact.
  - It is **Reject** and BuildOnly.
  - The editor, construction, wire and save boundaries refuse 0, 10, 2.5, −1, NaN and +∞. The editor also refuses text.
  - The latch has no parameters.
- **D4 Whitelist deleted.**
  - Every remaining instance is either generic or a Pipe. Delete the type check.
  - Fold `DeclaredColliders` into `IWorkshopInstance` and delete `IWorkshopDeclaredColliders`.
  - The encoder writes `length` only `if (instance is WorkshopPipe)`.
- **D5 Admission (the 9A.43 "Clear" AC).**
  - The chain is: declared capability → node kind → bistable set. No new command is added.
  - Rejected: a Latched→Clear flip by a sticky lamp, a contact switch, a Latch-timer, a counter or a passage node.
  - Still admitted (positive control): a level-wired lamp and a plate.
- **D6 Wire, save and content.**
  - Delete `target_count`. `InstanceRecordFields` goes 9→8, and `parameters` moves from slot 8 to 7.
  - `CarriesTypedField` becomes {length}. Prevalidation keeps `case "pipe"` only.
  - Delete `AuthoredInstanceTemplate.TargetCount`, its key, and the 28 content entries.
  - Old saves are pinned by base-encoder sha256, at both boundaries, at base and at candidate:
    - counter targets 1–9 are refused **typed**;
    - parameter-bearing generics are refused;
    - latch, pipe and parameterless generics load with identical outcomes, attributed one by one.
    - Untyped refusals must be 0. Any decode to a different valid value is a **Fail**.
- **D7 Ratchet.**
  - Known shrinks by exactly 33, and none are added.
  - The probe (a scratch arm, run before the freeze) prints the full Known −N / Generic +M/−K row list, and D7 pastes it.
  - The expected Generic additions are the `DeclaredCapability` contexts `Compile|Counter`, `Compile|StateLatch`, plus `ICounterElement`, the schema and the validator. These are the same node kinds already classified in `ActivationNetwork.ActivationNetwork`.
  - Remove the `SignalLatchMode`/`LatchMode` contexts.
  - If M is large, rename the capability to drop package words.

# Ordered tasks

- [x] **Suite-first baseline (record path).**
  - Add the `PlacedParts.Counter`/`Latch` adapters, and switch `LatchSupplyFixtureTests` onto them. The fixture is then byte-identical from base to candidate.
  - Assert `DeclaredPhysics.Of(counter|latch).Colliders.Count > 0`.
  - On one build, run these guards: Counter, StateLatch, LatchSupplyFixture, DeclaredPresentation, ClockGuard, BothGate, parity, save/wire/decode, ElementIsolation and the 9A.22 ring.
  - Record goldens (traces + ring bytes):
    - counter(1/3/9) behind a detector;
    - clock→counter→gate;
    - latch Set/Reset/same-tick;
    - the 9.4c fixture.
  - Pin the old-save sha256s.
- [x] **Capabilities (D1, D3):** validator cases and package parity.
- [x] **Conversion (D2):** generic `CaptureInstance`, `MapInstance` and prevalidation. Drop the record and activation arms.
- [x] **Whitelist deletion (D4).**
- [x] **Wire (D6):**
  - Run flatc 25.2.10. It must reproduce the committed output first.
  - Update the `workshop-wire.ts` tail and `run-reset-snapshot.ts`.
  - Update `cat-020.test.ts` to read the parameter. Change `tools/E2eRunner.Tests/TypeCheckTests.cs:31` with it, or the CI tools step fails.
  - Set the `DeclaredOrientationTests` slot literal to 8, and re-kill its mutant.
- [x] **Deletions: 0 production hits.** Record the base controls.
  - `rg -w "WorkshopCounter|WorkshopLatch|DefaultTargetCount|PlacedTargetCount|(Minimum|Maximum)TargetCount|SignalLatchMode|LatchMode|IWorkshopDeclaredColliders"`.
  - `rg 'WorkshopInput\.(Counter|Latch)\b|InstanceRecord\.(Add)?TargetCount\b|Unsupported authored instance declaration'`.
  - `'"counter"|"latch"'` in `engine/gpu`, `MachineWorld.Gpu.cs` and `ui`, and `'"target_count"' engine ui content`.
  - The 9A.43 `Workshop(…|Latch|BallDetector)\b` grep.
  - Delete both `.cs` files and their `.uid`s.
- [x] **Known rows (D7):**
  - 136 → 103, re-derived after 9A.22.
  - No Counter or Latch owner is left.
  - Every record-type row left is Pipe/FinalIsolation.
- [x] **Latest-design alignment:**
  - CAT-020/037 §2/§3;
  - the isolation-doc A3/A5/A6/A11 row (whitelist deleted);
  - the epics C-Memory wording ("no id arm except Pipe, 9A.45");
  - D5 scope.
- [x] **PRIMARY native proof** (production compile, worker engine, tick progression):
  - *Count:*
    - Target 3: crossings 1 and 2 emit nothing; the 3rd emits once.
    - The committed count equals the crossings (9R.1). Running `Consume` twice is identical.
    - Nothing emits after saturation.
    - Targets 1 and 9 emit at crossings 1 and 9.
  - *Ring (9A.22 reference consumer):*
    - Exactly one `ThresholdReached` (Count = target, Source = owner) at crossing N; none at N−1, or after saturation.
    - `MemoryChanged` Level 1 then 0.
    - No record for a re-Set while On, or for a same-tick Set+Reset while Off.
  - *Latch:*
    - Set gives On and Reset gives Off.
    - Same-tick Set+Reset gives Reset, and so does one source wired to both inputs.
    - The `ActivationOut` level follows memory, and the lamp follows it.
    - Occurrence consumers refuse the level link.
    - The D5 controls.
  - *Supply:*
    - `LatchSupplyFixtureTests` is green on base and candidate. It is the native guard for cat-005-n19 and cat-051-interrupt.
    - The counter contact closes at the target until Reset.
  - *Editor (native seam):*
    - 1 and 9 apply; 0, 10, 2.5, NaN and text are refused.
    - Cancel works.
    - An unchanged Apply skips Undo, and one Undo restores 3.
    - Running, Paused and locked are refused.
    - A move keeps the target.
  - *Boundaries:* the D3 literals at each boundary, and the validator edges.
  - *Lifecycle:*
    - Reset is exact: count and memory are clear, rows deep-equal, the epoch is new, ring sequences restart at 1, and the replay is identical.
    - Save/load replays identically, with no counts or memory saved.
  - *Old saves:* the D6 matrix.
- [x] **Identity:**
  - traces, nodes and ring record bytes are bit-identical;
  - the `compiled-geometry-9a16` counter and latch rows are DeepEqual;
  - placed solutions and lessons are byte-identical.
- [ ] **Closing addenda** (additions only):
  - **6.5f:**
    - The Counter and Latch arms are Closed.
    - The Pipe residue (`"pipe"` arms, the 5 `== PartId.Pipe` branches) is attributed to 9A.45.
    - 6.5f stays in-progress, with Chrome pending.
  - **9.2:** native-only. The editor half of #13 is a native Pass. 9.2 stays in-progress until cat-020 runs in Chrome.
- [x] **Checks:**
  - One Debug slnx build with 0 warnings.
  - Then, under bash: `dotnet test CuriousContraptions.slnx --no-build --no-restore --blame-crash`.
  - Run the CI "Test C# tools" step verbatim under bash.
  - Release workers, plus a web publish with `AppBundle` moved out.
  - Strict tsc 6.0.3 and the snapshot unit.
  - `anvil_check` and `git diff --check`.
  - Mutants to kill:
    - target ignored;
    - emit at N−1;
    - Reset not dominant;
    - all kinds bistable;
    - exclusivity omits Memory;
    - Reject→Clamp;
    - fields 8→9;
    - whitelist restored.
- [ ] **Independent review → SnapshotApproval → publication.**
  - Commit, merge and push the exact snapshot.
  - Verify the exact-SHA CI and the deploy.
  - Mark `9a-c-memory-counter-latch-conversion`, `9a-41` and `9a-43` done. Browser stays pending.

# Pending browser cases (post-catalogue)

- `cat-020`:
  - the editor sets 1 and 9 and refuses 0 and 10;
  - target 1;
  - `ThresholdReached` via the 9A.23/9A.24 reads;
  - Reset and Save/Load.
- `cat-037`, `cat-005-n19` and `cat-051-interrupt`.
- `engine-core-2b1`: a C-Clock-base counter save is refused, and the bench is kept.
- Re-run `cat-017`, `cat-052*` and `palette-placement`.

# Open questions

None owner-only. For coordinator reconciliation:
1. **Pipe residue.**
   - These ACs conflict with Pipe, which epics 9A.45 owns:
     - "`CaptureInstance` has no id arm";
     - "no record-type entry";
     - "close 6.5f".
   - The draft scopes them to non-Pipe, and 6.5f stays open.
2. **Bistable kinds.** The 9A.43 rule "Clear rejected for every other instance" is read as rejected for every instance without a declared bistable capability (Memory, a level-wired Indicator, ContactLoad).
3. **Ownership after 9A.22.** If any `ActivationOccurrenceKind.CounterReached` Events rows survive 9A.22, it is not yet decided who owns them.

# Code Map

- **Change:**
  - `Abstractions.Signal/SignalContracts.cs` and `Bundle/{BundleSchema,BundleValidator}.cs`;
  - `elements/*/…Declaration.cs`, for every `ISignalElement`;
  - `engine/gpu/{ActivationNetwork,WorkshopInstances,WorkshopConstruction,WorkshopWireFlatBuffers,WorkshopSavePrevalidation,AuthoredInstanceTemplate,WorkshopPipe}.cs` and `engine/MachineWorld.Gpu.cs`;
  - the schema and its generated C#/TS;
  - `tools/e2e/{workshop-wire,run-reset-snapshot,cat-020.test}.ts` and `tools/E2eRunner.Tests/TypeCheckTests.cs`;
  - `content/puzzles.json`, the isolation fixture and the tests (including `LatchSupplyFixtureTests`, adapter only);
  - CAT-020/037, the isolation doc, the epics, the 6.5f/9.2 reviews, `TODO.md` and `sprint-status.yaml`.
- **Delete:** `engine/gpu/{WorkshopCounter,WorkshopLatch}.cs` and their `.uid`s.
- **Unchanged:**
  - the counter and latch laws;
  - `ActivationMemory`;
  - `BistableNodes()`;
  - the 9A.22 producer;
  - `PartId.cs`;
  - `compiled-geometry-9a16.json`.

# Coordinator decisions (23 October 2026, under owner delegation)

1. **Pipe residue / 6.5f:** 6.5f stays open after C-Memory. The surviving Pipe id arms are owned by the final isolation gate, 9A.45. C-Memory closes only the Counter and Latch arms of 6.5f, through an append-only addendum.
2. **Clear admission (superseded 24 Oct 2026).** ~~A Clear command is rejected only for instances whose package declares no bistable capability. Level lamps and plates are not bistable, so they are rejected by that rule. Rationale: the capability, not a list of element names, decides admission. One typed refusal, with literal tests for a bistable, a non-bistable and an unknown instance.~~ **Superseded by the coordinator, 24 Oct 2026:** a committed Latched→Clear flip is admitted for StateLatch, LevelIndicator and ContactLoadSource nodes and refused `Faulted`/`InvalidRead` for every other node; there is no generic Clear command (epics 9A.43 amended accordingly). The PulseCount rename and the typed old-save refusals are confirmed the same day.
3. **Leftover `CounterReached` rows:** any that survive 9A.22 are owned by, and deleted in, C-Memory, because the counter is its element. Re-derive after 9A.22 merges.

# Current state

*Implementation owner, 24 Oct 2026. Uncommitted native candidate in worktree `story-c-memory` on base `fc7f7cac`. Tracked diff (HEAD, `_bmad-output` and `TODO.md` excluded) sha256 `44b85353…`; new `MemoryGuardTests.cs` `89abbef3…` (byte-identical to the copy run green on the record path), `DeclaredMemoryTests.cs` `39845d70…` (`evidence-c-memory/candidate/identity.txt`). Awaiting independent review. Browser qualification pending.*

**Re-derived on fc7f7cac.** Known 127 rows (94 FinalIsolation, 19 Counter, 14 Latch) → 94. `CounterReached` production hits: 0 (grep G), so coordinator decision 3 has nothing to delete. `WorkshopInput.Counter(` 30 sites in 15 test files, `.Latch(` 32 in 13. Base deletion-grep controls A–F = 42/5/5/30/16/9 (`baseline/deletion-greps-base.txt`). The 9A.43 regex hit only Counter/Latch at base.

**What changed**
- Capabilities: `SignalPulseCountDescriptor(TargetParameter)`/`IPulseCountElement` and `SignalMemoryDescriptor(Set, Reset)`/`IMemoryElement`, trailing optional `SignalPayload.PulseCount`/`Memory` with round trip; `SignalLatchMode`, `ISignalElement.LatchMode`/`Counter`, the old `SignalCounterDescriptor`, its converter and the dead `pulse`/`set_pulse` triggers deleted (bundle `250699…` → `abbfd3…`: counter and latch changed; every other signal entry only lost `LatchMode`, `candidate/bundle-identity.txt`).
- Validator: two more node-selecting capabilities; PulseCount target a unitless WholeNumber inside [1, 9], Pulse `ActivationIn` and `ActivationOut`, `SwitchedContact` supply, not Dynamic/slider and a collider; Memory inputs exactly the two Pulse inputs the shared law reads (SetIn, ResetIn; undefined → UndefinedEnum), Level `ActivationOut`, `SwitchedContact`, body; `CommittedCount` binding without PulseCount; `Contact.Ownership` keeps its strength (PulseCount/Memory replace Counter/LatchMode).
- Compile: one shared helper `WorkshopActivationCompiler.MemoryNode` builds the Counter node (`TargetCount = (uint)param`) or the StateLatch node; `Compile` calls it in the same position. Laws, `ActivationMemory`, `BistableNodes()`, the 9A.22 producer, `PartId.cs` and `compiled-geometry-9a16.json` are untouched.
- Deleted: `WorkshopCounter.cs`/`WorkshopLatch.cs` (+`.uid`), `WorkshopInput.Counter`/`Latch`, the `CaptureInstance`, `MapInstance`, encoder and prevalidation arms, the `TargetCount` decoder, the whole `WorkshopInstances.Validate` whitelist, `IWorkshopDeclaredColliders` (folded into `IWorkshopInstance`), the wire `target_count` field (flatc 25.2.10 regeneration equal to the edited C#/TS, `InstanceRecordFields` 9→8, `CarriesTypedField` = length), `AuthoredInstanceTemplate.TargetCount`, its key and helper, 28 content entries, the TS dump field and `SNAPSHOT_SCHEMA` entry. Encoder writes `length` only for `WorkshopPipe`.
- `cat-020.test.ts` reads the declared parameter; `TypeCheckTests` now a theory over both deleted accessors (`threshold`, `targetCount`).
- Docs: CAT-020/037 §2/§3 current notes; isolation-doc A3/A5/A6/A11 row; epics C-Memory wording (Pipe residue to 9A.45, 6.5f/9.2 stay open).

**Decisions and deviations (reviewer to confirm)**
1. **D5 vs coordinator decision 2.** Taken literally, "level lamps and plates are not bistable, so rejected" breaks play: narrowing `BistableNodes` to StateLatch fails 8 existing guards (level lamps going dark, plate release, 9.4c fixture; `d5-probe/narrowed-bistable.log`, reverted). Implemented the decision's rule, not its parenthetical: the declared capability selects the node kind and the bistable set holds the kinds a declared level capability compiles (memory, a level-wired indicator, a contact load); every other node's Latched→Clear flip is one typed refusal (`Faulted`/`InvalidRead`). Literal tests reach the guarded check on the real engine: 3 admitted, 6 refused (sticky lamp, switch, Latch-timer, counter, passage, oscillator), and an unknown node refused. **Coordinator to confirm.**
2. **Capability name.** D7 allowed a rename: "Counter" is a package word, so the capability is `PulseCount`. The isolation ratchet refuses reclassifying the prior Known keys `Compile|Counter`/`Compile|StateLatch` as Generic (spec D7 expected exactly those), so the node construction is the separate shared helper `MemoryNode` (Generic +2 rows; −16 stale LatchMode/Counter rows; `candidate/known-rows.txt`).
3. **D6 correction.** Base-encoder parameterless generics (lamp, ball, …) do not load: the base encoder wrote an empty parameters vector in slot 8, now beyond the 8-field table, so they are refused typed (the C-Clock precedent). Of 43 base-encoder kinds × 2 boundaries: 6 load (latch, locked rotated latch, pipe; same pose, lock, length), 80 typed refusals, 0 untyped. Counter targets 1–9: 7 VerifierRejected, 4 and 8 OutOfRange (an offset to an empty vector), all at both boundaries, pinned by base-encoder bytes in `DeclaredMemoryTests`.
4. **Supply proof.** The guards assert the counter's and latch's contact through `ActivationNetwork.ContactClosed`, the same query the device passes to the supply resolution of every `SwitchedContact` route (`CuriousContraptions.Simulation/WorkshopGpuDevice.cs`, `owner => network.ContactClosed(...)` into `SupplyRoute.Resolve`); the 9.4c fixture proves the powered consumer end to end.
5. **Three-ball bench.** Three Basketballs fall through one detector onto a 20° Ramp that carries each away; crossings at ticks 39/89/156 by balls 5/6/7. The `ThresholdReached` record's `Other` is the cause ball (derived and asserted).
5. **Closing addenda** (6.5f, 9.2) are the independent reviewer's records, as at C-Clock; facts for them: 6.5f Counter/Latch arms closed, Pipe residue (`"pipe"` arms, `== PartId.Pipe` branches) to 9A.45, Chrome pending; 9.2 editor half a native Pass (`TheCounterTargetEditor…`), open until `cat-020` runs in Chrome.
6. **Expected-change sites** (each attributed; none tuned to a different meaning): `CounterTests` (record validation → declared parameter; wire round trip generic), `StateLatchTests` (`AssertSame` by part id), `ConstructionDecodeBoundaryTests` (counter joins the f32 parameter boundary matrix; length-on-counter refused), `WorkshopWireFlatBuffersTests` (declared-setting rows: clock and counter parameters; pre-conversion rows moved to the verified-boundary pinned saves; whole-instance and collider equality for every kind), `WorkshopSaveRejectionTests` (`Field` loses `TargetCount`; counter rows by parameter, +2 rows), `PayloadInstanceTests` (`TypedField` = {Length}; counter and latch rows), `PackageRuntimeParityTests` (capabilities; generic target row), `BallDetectorTests` (`IsType<WorkshopGenericInstance>`), `PalettePlacementTests` (placement defect proposed on a discovered world; + counter editor), old-save reasons re-derived on the 8-field table in `DeclaredContactLoadTests` (VerifierRejected), `DeclaredOscillatorTests` (clocks VerifierRejected, counters OutOfRange), `DeclaredOrientationTests` (domino VerifierRejected, pipe OutOfRange/MissingField, boundary slot 9→8), `DeclaredIndicatorPassageTests` (OutOfRange), `PreConversionLaunchSaveTests` (domino VerifierRejected, counter OutOfRange). Every other suite file is byte-identical to the base run.
7. **Process disclosures.** Pre-write anvil validation was skipped, or done with a patch not identical to the applied edit, for these writes: `baseline/wire-regen-base.txt`; two `sed -i` shell edits against the user rule (a one-word comment in `DeclaredOrientationTests`, and the `MemoryGuardTests` case count in this spec after the freeze); the eight temporary mutation edits and their restorations; the `count` local rename in `ActivationNetwork`; the `CounterTests` validation and wire-test rewrites; most `ConstructionDecodeBoundaryTests` edits; the two `content/puzzles.json` repair replace-alls (an Edit join defect, repaired to a pure 28-line deletion); the `DeclaredMemoryTests` compile fixes; the `BundleValidatorTests` `Supply = null` and exclusivity edits; the scratch probe's CA1031 fix; the `StateLatchTests` replace-all (subset patch); the Known fixture removals (representative removal diff); the `DeclaredContactLoadTests` rewrite and the `mutant.sh` rewrite (validated with a different patch). Post hoc, `anvil_check` over all 65 changed files found 0 issues and `git diff --check` is clean. Shell file operations: `git rm` of the four record files; `mv`/`cp` of the scratch probes, frozen mutation copies, record-path adapter and `AppBundle` (moved to the scratchpad); a throwaway `git worktree` at fc7f7cac (bundle build only), removed.

**Evidence** (`_bmad-output/implementation-artifacts/evidence-c-memory/`)

| Check | Receipt | Result |
|---|---|---|
| Base build + full suite with the test-only diff and probe | `baseline/build.log`, `baseline/full-test.log` | 0 warnings; 17/77/174/189/2728, rc 0 |
| Base guards (19 classes incl. `MemoryGuardTests`) | `baseline/guards.log` | 641/641 |
| Traces ×9 (nodes, edges, bodies, colliders, supply, admission hash, per-tick bits, Reset, replay, ring records and bytes), 12×3 lessons, 12×3 placed solutions | `baseline/capture/`, `candidate/capture/`, `candidate/capture-compare.txt` | all byte-identical |
| Old saves (43 kinds × 2 boundaries, base-encoder bytes) | `baseline/old-saves.sha256`, `candidate/capture/old-save-outcomes.txt` | base 86 LOADED → 6 identical loads, 80 typed refusals, 0 untyped |
| Wire regeneration (flatc 25.2.10) | `baseline/wire-regen-base.txt` | base reproduced; candidate C#/TS equal to fresh output (pre-existing header blank line aside) |
| Bundle identity | `candidate/bundle-identity.txt` | `250699…` → `abbfd3…` |
| Deletion greps | `candidate/deletion-greps.txt` | A–G 0; record files absent |
| Known / Generic | `candidate/known-rows.txt` | 127 → 94 (−33 Counter/Latch, +0); Generic 533 → 519 |
| Frozen Debug slnx build (`--no-incremental`) | `candidate/freeze-build.log` | 0 warnings, 0 errors |
| Full `dotnet test CuriousContraptions.slnx --no-build --no-restore` (no `--blame-crash`, coordinator 24 Oct) | `candidate/freeze-test.txt` | 17/77/176/190/2775, 0 skipped, rc 0 |
| CI "Test C# tools" (bash, verbatim) | `candidate/ci-tools.log` | 77/176/17/174, rc 0 |
| Release Simulation, Animation.Worker; Release web publish | `candidate/worker-release.txt`, `candidate/web-publish.txt` | rc 0/0, 0 warnings; publish rc 0, static files present; AppBundle moved out |
| Strict tsc 6.0.3; snapshot unit | `candidate/ts-checks.txt` | rc 0; 24/24, rc 0 |
| `git diff --check`; `anvil_check` (65 files) | `candidate/diff-check.txt` | clean; 0 findings |
| Mutations M1–M8 | `mutation/*.log`, `mutation/restoration.txt` | all killed; restored |

**Mutations:** M1 target ignored (17), M2 emit at N−1 (18), M3 Reset not dominant (6), M4 all kinds bistable (6 D5 rows), M5 exclusivity omits Memory (validator), M6 Reject→Clamp (editor + validator), M7 `InstanceRecordFields` 8→9 (boundary slot + parameterless old save), M8 whitelist restored (`TheInstanceCollectionHasNoTypeWhitelist`).

**New tests:** `MemoryGuardTests` (21, suite-first, unchanged since the base run: three-ball counts at targets 1/3/9 with ring, lamp and contact; clock targets 1/9 and saturation; Consume determinism; Reset/epoch/sequence/save-load; Set/Reset schedule with same-tick dominance, one source on both inputs, level-link refusal; D5 admission on the real engine), `DeclaredMemoryTests` (19: capabilities, node identity at 1 and 9, no whitelist, pinned old saves, stale content key), two `BundleValidatorTests`, the counter editor in `PalettePlacementTests`.
