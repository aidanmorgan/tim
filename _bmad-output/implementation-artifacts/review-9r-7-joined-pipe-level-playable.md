# Review: Story 9R.7, Joined Pipe Level Playable (and Removing Wire and Save Format Versions)

- **Reviewer:** Murdoch-14, the independent adversarial reviewer. I edited no deliverable.
- **Candidate:** worktree `.claude/worktrees/agent-a8fd98fb7ee51d687`, commit `da91cffa`, based on 9R.6's `63ba232c`.
- **Review identity:**
  - My clean detached worktree is `.claude/worktrees/murdoch14-9r7`. It holds `da91cffa` merged with main `2adff597`, which gives the local merge commit `40eb7433`.
  - The merge was clean. `git status --porcelain` was empty before and after Tier 1.
  - Outside `_bmad-output/`, the merge adds only `tools/Ownership/OwnershipPolicy.cs` (9R.5b's analyser fix).
  - On that merge, `select --diff da91cffa` gives **Mode: Selected, Tier 3 required: no**: the path is `NoBrowserImpact`, plus the smoke pair.
  - Main has since moved to `c7d5dbb0`. That commit changes only `_bmad-output/` (planning and evidence), so I did not re-merge.
- **Owner rules applied:**
  - Only a breach of the story's ACs blocks. That includes the owner's 21 Oct 2026 decision on format versions: "why are you versioning the wire format and the save formats, this isnt required and goes against the rule that says we dont have any backwards compatability" (epics line 346: "There are no format versions"). A player-visible regression also blocks.
  - Everything else goes to Story F.10 "Joined Pipe Follow-ups".
  - Old code is deleted in the same story.
- **Round:** 1.

## Static review

### Removing format versions (check 1)

The command wire, the response wire, the cadence settings record and the save are correct:
- `WorkshopWireVersion`, `WorkshopSaveVersion`, `WorkshopSettingsVersion` and `WorkshopSaveCodec.Version` are deleted.
- The `version` fields are gone from `schemas/workshop_wire.fbs` (`CadenceSettingsRecord`, `SavedConstruction`, `WorkshopResponse`, `WorkshopCommand`), from the generated C# and from `tools/e2e/workshop-wire.ts`.
- The driver's `saved.version() !== 13` check is replaced by a check for the magic and the construction.
- Bytes 12–15 of the binary command and response headers are now zero padding, and so are bytes 24–31 of the settings record.

But many format versions remain, in the C# and in the client and worker JS that check 1 names. The implementer's notes call them "outside this change" and file them as a follow-up. The owner's decision and check 1 ("No wire, save or trace version field, constant or check remains anywhere") do not allow that, and neither does the "delete old code in the same story" rule. Every remaining site is in **B1** below.

Not format versions, so they stay:
- the ABA/generation handle counters `AnimationBatch.Version`, `WorkshopPoseHistory.Prepared.Version` and `WorkshopClientCursor.Prepared.Version`;
- the FlatBuffers runtime's `ValidateVersion()` library guard;
- `LanguageVersion`, toolchain versions (`TsxVersion`, `ChromeVersion` and so on), the `BundleElements` manifest `Version = "1.0"`, `System.Runtime.Versioning` and the licence headers;
- `workshop-client.js:546`'s `getUint32(12) !== 1`, which is the memory-result *status* (Accepted), not a version.

### Rejecting bad input (check 2)

- **Save.** `WorkshopSaveCodec.Read` now runs the generated `SavedConstructionVerify` before reading. A failure is `UnsupportedSave`, and so is a missing construction or a foreign magic. Reader bounds failures are mapped to `UnsupportedSave` inside `Read` only.
- **Command and response.** `WorkshopWireFlatBuffers` runs `WorkshopCommandVerify` and `WorkshopResponseVerify`, and failures throw `ArgumentException`.
- **Binary headers and settings.** They check zero padding, enum ranges (`WorkshopCadenceSettings.Validate`) and widths. Nothing compares a version number.
- **Tests.** `WorkshopFormatTests` covers:
  - a foreign root (a command buffer) and a save without its construction: `UnsupportedSave`;
  - an unterminated part-id string, which only the verifier catches (her mutant 9 is killed);
  - an out-of-bounds root offset, on all three decoders;
  - non-zero header padding on the command and the settings record (her mutant 8 is killed).
- **My probe: old saves.**
  - I encoded three saves with the last versioned codec (version 13, at `c36c7e08`): a free bench, `first_principles` and `quarter_bend`.
  - The candidate decodes each one as **REFUSED UnsupportedSave** ("Unsupported construction save."). None is admitted and none throws an untyped exception (`evidence-9r-7-review/probes/`).
  - So a player's existing slot is refused with the existing typed rejection. The UI shows it in the status line (`ui/Workshop.cs` `LoadSaveAsync`), and the current bench is kept. That is not a regression.

### Story ACs (check 3), static

- **Lesson.** `JoinedPipe` follows the `QuarterBend` pattern:
  - The ids are ball, bend and receiver = first, +1 and +2, and `_nextId` advances by 3.
  - Validate demands exactly one locked `pipe_bend_90` equal to the authored fixture (cell, local, rotation, lock and colliders). Otherwise it is `FixedPart`.
  - Any other part, or a locked pipe, is `InventoryUnsupported`. A second pipe is `InventoryExceeded`.
  - The picker item, title, task, hint and status text match `content/puzzles.json` ("Meet in the middle", description and hint verbatim).
- **Pipe length.**
  - `WorkshopPipe` is a typed record with an f32 `PipeLength`: 1–8 m, default 3.6.
  - `FromInput` is the named pointer boundary. It clamps finite input and rejects non-finite input. `Validate` rejects out-of-range lengths.
  - A pipe record without a length decodes the schema's 0, so validation rejects it, never defaults it. The command wire puts the length at slot bytes 112–115, and bytes 116+ must be zero.
  - The generic instance now rejects `PartId.Pipe`.
  - The old scene-extracted collider path for a placed pipe is deleted, so one generated 24-box path remains.
- **Chrome suite `tools/e2e/cat-050-joined.test.ts`.**
  - Every gesture uses real UI controls: the picker, the drawer, the square resize handle, fine rotate, a drag and the precision slider.
  - Test 2 asserts `assertJoined` (1e-4 m and dot < −0.9999) from the saved bytes between the pipe's end and the **locked** bend's start. That is 9R.6's locked-target case (F9-6) in Chrome.
  - Test 4 (Precise) asserts the pose samples never enter the pipe bore or the bend mouth, and checks unsolved at tick ≥ 1200.
  - Test 3 asserts Reset exactness twice, a second solve, and a Save, reload and Load that keeps the pipe's instance entries byte-identical, the joint and the solve.
  - Test 6 drags *past* both limits (3.6 − 3 = 0.6 m and 3.6 + 5 = 8.6 m). It asserts exactly 1 m and exactly 8 m, Undo back to the f32 3.6 (`0x40666666`), and an Escape cancel, with a control drag that commits.

## Runtime evidence (mine, `evidence-9r-7-review/`)

### Tier 1 (`tier1/`, clean `40eb7433`)

All exits were 0 (`tier1/exits.txt`):
- `dotnet build CuriousContraptions.slnx`: 0 Warning(s), 0 Error(s). The Release build is the same.
- `dotnet test CuriousContraptions.slnx`:
  - WorkerAotGuard 17/17;
  - Coverage 78/78;
  - E2eRunner 156/156;
  - CuriousContraptions 1869/1869.
- Node harnesses: 83/83. `tools/e2e/unit`: 24/24.
- Ownership `--oracles .`: **exit 0**. The failure she reported was 9R.5b's, and the merge fixes it.
- LifecycleContract: 510/0. WireContract: 109/0.
- Merge selection: `tier1/select-diff-da91cffa.txt`.

### Mutants (`mutants/`, scratch worktree `.claude/worktrees/murdoch14-9r7-mut` at `40eb7433`)

I applied each mutant alone with the Edit tool, ran the full `CuriousContraptions.tests` suite, and reverted with `git checkout -- .`. The porcelain was empty after each.

| Mutant | Change | Result |
|---|---|---|
| MV1 | Re-adds a response-header version: writes 21 at bytes 12–15 and requires 21 on decode | **Survives** (1869/1869). Part of B1 |
| MV2 | Re-adds a settings-record version: writes 1 at byte 24 and requires 1 | **Killed**: `TheBinaryHeadersRejectNonZeroPaddingWhereTheVersionWas` |
| MV3 | Re-adds a command-header version: writes 21 at byte 12 and requires 21 | **Killed**: the same test |
| MS1 | Removes the pointer clamp (`FromInput` passes the raw length) | **Killed**: 7 rows (`PointerInputClampsFiniteLengths`, `ArtworkAndPickRadiusFollowTheLength`) |
| MS2 | `JoinedPipe.IsFixture` ignores the bend's in-cell `Local` | **Survives**. F10-1 |

Her ten mutants (`evidence-9r-7/mutant-*.txt`) are all recorded as killed. I did not repeat them.

### Her full run (reused after identity checks)

- **Her run:** `20261020T182947Z-96552`, **verdict Pass, Mode Full (Tier 3), 45/45 files, 234/234 tests**. Its evidence root is `agent-a8fd98fb7ee51d687/.anvil/e2e-runs/20261020T182947Z-96552`, and the copy is `evidence-9r-7/full-run-da91cffa/`.
- **Identity checks:**
  - sourceHead is `da91cffa…`, and the status digest is `e3b0c442…` (clean).
  - The bundle `844cf93e…` and the suite digest `6c6c91dc…` are the same at start and end. `servedCheck` passed at start and end.
- **Suites:**
  - cat-050-joined: 6/6 (SerialAlone, Clean).
  - tube-snapping: 13/13. The first attempt was Contended, and the Clean rerun passed. I was running Tier 1 and my mutants at that time.
  - cat-048a 4/4, cat-048b 5/5, cat-049 5/5, cat-050 5/5, cat-030 7/7 and cat-005 7/7.
  - cat-062b 7/7: its Run/Reset model includes save/load.
  - clock-master 9/9. Its counters (closeFindings 1, unresponsive 1, recal 4, renewedLate 1, debtFaults 1) match the 9R.4 to 9R.6 baseline.
- **After her run:**
  - `d636f9fa` changes only evidence.
  - `120bf876` (the current HEAD) adds one test, `ASaveInTheRetiredVersionedLayoutIsTheTypedSaveRefusal` (`WorkshopFormatTests`).
  - I merged `120bf876` with main `2adff597` as `f0a54b0b` in the mut worktree. There, `dotnet build CuriousContraptions.slnx` gives **0 Warning(s), 0 Error(s)**, and `dotnet test CuriousContraptions.tests` gives **1870/1870** (`head-120bf876/`). The runner classes the change as `NoBrowserImpact`.
- **My merge run:** `20261020T192536Z-92896` (`merge-run/summary.txt`), on clean `40eb7433` (bundle `8315192a…`), ran the runner's selection for the merge diff (the smoke pair): **Pass, 2/2 files, 6/6 tests** (`engine-core-2a1` 5/5, `engine-core-2b1` 1/1).
- **The collider-path question (coordinator):**
  - A placed bend, Funnel, Powered Gate or Ball Detector is captured with the scene-extracted boxes, and a loaded one with the canonical boxes. My probe (`probes/collider-paths.cs.txt`) shows:
    - both bends are geometrically identical (≤ 3.4e-7 m);
    - the Funnel differs by up to **0.44 m** at a box corner;
    - the Powered Gate and the Ball Detector differ by up to **0.16 m**.
  - **It predates 9R.7.** This diff changes no `.tscn`, and it leaves the bend, Funnel and gate capture arms and their canonical tables unchanged. It removes only the pipe's own split. So it is owned by 9A.16 and recorded as F10-11.
  - **There is no physics change on Save/Load.** Neither command wire carries colliders. Both decoders (`WorkshopCommandRouter` → `WorkshopWireFlatBuffers` / `WorkshopWire`) rebuild every tube part from `WorkshopCanonicalColliders.For`, so the simulation worker compiles the canonical boxes for placed and loaded parts alike.
  - I did not audit every main-thread placement or selection reader of the record.

## Criterion matrix

| Criterion | Evidence | Result |
|---|---|---|
| No wire, save or trace format version anywhere (owner, 21 Oct 2026; check 1) | Static grep: the Workshop command/response/settings/save versions are gone, but 9 families of format versions remain (B1). MV1 survives | **Not met** (B1) |
| Bad input is rejected structurally through the typed rejection, and tests prove it (check 2) | `WorkshopFormatTests`, `WorkshopSaveTests`, `WorkshopPipeLengthTests` missing-length rows, her mutants 3, 8 and 9 killed, MV2 and MV3 killed, and my old-save probe (3/3 `UnsupportedSave`). The response-header padding is unproven (MV1) | Met for the save, command and settings. The response header is in B1 |
| AC1: picker, drawer, resize, snap and Run solve exactly once | cat-050-joined test 2 (static), plus her full run | Met |
| AC2: an unjoined pipe leaves the goal unsolved | Test 4 (Precise, no bore samples, tick ≥ 1200). Her mutant 4 killed | Met |
| AC3: fixtures and inventory match the content, and an extra part is refused | Test 1 (pose, orientation, lock, knots and inventory from the saved bytes; second pipe: no command and bytes unchanged). Native `FixturesKnotsAndInventoryEqualTheContent`, `ValidateAdmitsTheGrantedPipeOnly` and `ALockedFixedBendIsRequired`. Her mutants 1 and 5 killed | Met |
| AC4: Run/Reset matches the snapshot, and Save/Load keeps the route | Test 3. Native codec round trips. Her mutant 3 killed | Met |
| Typed `length` 1–8 m with clamping on resize (rules line 346) | Native bounds, clamp and NaN rows. Test 6 drags past both limits. MS1 and her mutant 2b killed | Met |
| 9R.6 locked-target join in Chrome | Test 2 `assertJoined` against the locked bend | Met |
| `cat-050-joined.test.ts` 100% serially | Her full run (SerialAlone) | Met |
| Build 0 warnings, `dotnet test` 100%, every suite 100% in one serial Chrome run | My Tier 1. Her full run, 45/45 files, in the runner's default mode (SerialAlone files alone; Parallel files on N = 2, as accepted for 9R.5b and 9R.6) | Met |
| No regressions: tube suites, cat-005 and the save/load suites | Her full run | Met |

## Findings

### Blocking

**B1. Format versions remain outside the four Workshop records.** Check 1 and the owner's decision ("There are no format versions"; delete old code in the same story) cover all of them. The implementer filed them as "outside this change", which this story does not allow. Remove every site below. Each one must instead reject by structure (width, zero padding, enum range, role and session identity), with its tests rewritten to structural rejections:

1. **Clock wire.**
   - `engine/gpu/WorkshopClockWire.cs:12` `Version = 4`, written at `:24` and checked at `:38`, plus the other encoders and decoders in that file.
   - The handoff in `engine/gpu/BrowserWorkshopClient.cs:112`: `(int)WorkshopClockWire.Version` is the first clock-ABI element.
   - Its JS mirror in `CuriousContraptions.web/wwwroot/workshop-client.js:149, 161, 262` (`clockVersion`, checked on the master bootstrap).
2. **Clock ring.**
   - `CuriousContraptions.web/wwwroot/clock-ring.js:12` `ClockRingVersion = 3`, stored at `:35` and checked at `:43`.
   - Its tests in `tools/workshop-clock-ring.test.mjs:55, 153, 155`.
3. **Animation wire.**
   - `engine/gpu/WorkshopHint.cs:16` `WorkshopAnimationWire.Version = 5`, written at `:42` and checked at `:53` and `:93`.
   - Its writer in `CuriousContraptions.Animation.Worker/Program.cs:341`.
4. **Memory observation wire.** `engine/gpu/WorkshopMemoryWire.cs:31` `Version = 1`, written at `:49` and checked at `:68`.
5. **Trace.**
   - `engine/gpu/WorkshopTraceRecord.cs:7` `WorkshopTraceVersion`, written at `:28` and checked at `:46`.
   - `CuriousContraptions.Simulation/WorkshopTrace.cs:11` `TraceTimingVersion`, plus the version token in `CCGPU_TRACE_BEGIN` (`:146`) and `CCGPU_TICK_TIMES_BEGIN` (`:162`).
   - The consumers to update with them: `CuriousContraptions.tests/WorkshopTraceTests.cs:49, 107, 388, 458` and the fixture line in `tools/workshop-console.test.mjs:19`.
   - `tools/workshop-console.mjs` only checks that fields are numeric.
6. **Isolation policy handshake.**
   - `CuriousContraptions.web/wwwroot/workshop-isolation.js:1, 82, 85, 96, 97, 123, 154` and `workshop-isolation-worker.js:2, 11, 13` (`policyVersion`, also kept in the sessionStorage marker).
   - Its test fixture in `tools/workshop-isolation.test.mjs:16`.
   - **Retry condition for this item only.** The service worker and its sessionStorage marker survive a deploy, so "one bundle" does not hold here. Remove the number only if the page still refuses a controller or marker from another deploy by a structural or identity check, such as the deployed bundle identity, with a test. Otherwise the coordinator must rule this item out of scope before round 2. Do not just delete the check: that would let a stale controller pass, which is a player-visible regression.
7. **Shared ABI headers.**
   - Physics state: `engine/gpu/PhysicsGpuAbi.cs:7, 84, 446` (the WGSL `STATE_VERSION`) and `:478`, plus `PhysicsGpuAbi.Header.cs:9` `HeaderVersionOffset`.
   - Canonical body record: `engine/gpu/CanonicalBody.cs:8, 44, 62` `BodyRecordVersion`.
   - Schedule control: `engine/gpu/WorkshopSchedule.cs:6, 134, 148` `WorkshopScheduleVersion`.
8. **Wire design fixture.** `tools/WireContract/ContractFixture.cs:44, 79` (`HeaderFixture.Version`, required to be 1) and the `UnknownVersion` oracle in `tools/WireContract/Program.cs:7, 40, 55`.
9. **Tests that assert the remaining versions, to rewrite with them.**
   - `CuriousContraptions.tests/WorkshopWireTests.cs:242-244, 552-561`: clock-wire "old version" rows and `Assert.Equal(4U, …)`.
   - `ContactActivationTests.cs:46` (`…OldStateVersionReject`).
   - `CanonicalBodyTests.cs:10, 27, 45, 72-77`.
   - `WorkshopTraceTests.cs:46-52`.
   - `tools/workshop-clock-ring.test.mjs:55, 153, 155`.
10. **Current docs that still declare versions.**
   - `docs/shared-clock-cadence.md`:
     - line 74 still gives the typed settings record as "schema U32 at 0 (version 1)". That is stale after *this* story's settings change.
     - lines 76 and 80 give the clock wire and schedule-control versions.
   - `docs/gpu-f32-physics.md:138`, "Forward schema versions are canonical3 …, physics12 and trace4". These values are also stale: physics is 13 and trace is 5.
   - `docs/engine-contracts.md:235` still describes the save as "construction-save v4 envelope (1,624 bytes): typed version …". That is in the paragraph this story edited, and it contradicts the new sentence after it.

Also, **the guard does not hold the removal.**
- My MV1 re-adds a response-header version, writing 21 and requiring 21, and all 1869 tests pass.
- `NoWireOrSaveRecordDeclaresAFormatVersion` only checks three type names and four generated records, so it cannot see a version reintroduced under another name or in a binary header.
- Add a response-header padding row, as for the command header. Also extend the guard to cover what this fix removes.

Retry condition: every site above is removed and its tests are rewritten. Then the implementer runs one full run on the new candidate, and I re-review the B1 diff in round 2.

### Filed to Story F.10 "Joined Pipe Follow-ups"

- **F10-1 (untested fixture field).** MS2 survives: `JoinedPipe.IsFixture`'s `Local` comparison is untested. `ALockedFixedBendIsRequired` changes only the cell. Add a row that moves the bend within its cell. This is not player-visible, because the UI cannot move a locked fixture.
- **F10-2 (physical length unguarded end to end).** Her mutant 2 (colliders fixed at 3.6) is killed only by the collider unit tests, and her native solve probe still passes under it. Add a native or Chrome row where only the generated collider length decides the outcome.
- **F10-3 (string identity in domain logic).** `JoinedPipe.Validate` matches `WorkshopGenericInstance { PartId.Value: "pipe_bend_90", … }` twice, while the same method elsewhere uses `PartId.PipeBend90`. Use the typed id throughout (AGENTS typing rule).
- **F10-4 (naming).** The pipe's assistance profile is stored in `WorkshopPuzzle.RampAssistance`. This follows the existing pattern for pipe and bend lessons, which Story 9A.26 owns.
- **F10-5 (over-inventory proof).** Chrome test 1 proves the spent drawer row issues no command. The engine's `InventoryExceeded` refusal is proven only natively, in `ValidateAdmitsTheGrantedPipeOnly`. Both are valid, so this is a note.
- **F10-6 (old-layout fixture).** My probe shows that real version-13 saves (free bench, `first_principles` and `quarter_bend`) are refused today, only because the slot renumbering breaks them. Her later test-only commit `120bf876` adds a hand-built retired-layout save (`ASaveInTheRetiredVersionedLayoutIsTheTypedSaveRefusal`), which I have not run. It has no instances. Consider also checking in one real pre-change slot with parts, such as `probes/old-saves-v13.txt`.
- **F10-7 (visible rejection message).** No Chrome suite loads a structurally invalid slot and asserts the status message and the kept bench. The message path exists (`ui/Workshop.cs` `LoadSaveAsync`).
- **F10-8 (lesson design).** Her filed note stands: the lesson tolerates a joint left about 0.72 m loose up, down or along the axis, and the ball still reaches the Receiver. Only the left offset fails.
- **F10-9 (formatting).** `ui/WorkshopPuzzle.cs`, line `if (quarter) _status.Text ="Point one mouth …"` has a missing space after `=`, introduced by this diff.
- **F10-10 (planning text, coordinator).** Main's `epics.md` still says "An old save or wire version is rejected explicitly" (line ~2035) and "Each story bumps the save version and rejects older saves explicitly" (line ~2978). Both contradict the 21 Oct decision.

- **F10-11 (owned by 9A.16; predates 9R.7).** A placed Funnel, Powered Gate or Ball Detector holds scene-extracted boxes. They differ from the canonical boxes that a load (and the worker) uses by up to 0.44 m, 0.16 m and 0.16 m at a box corner. The bends match. Physics does not change, because the worker always compiles the canonical boxes. But the main-thread record and native compiles of captured parts use the other table. Collapse them to one path in 9A.16.

### Process disclosures

- My Tier 1 ran while her full run was in progress, so it may have added CPU contention to her run. The runner marked only the first tube-snapping attempt as Contended. Its rerun and every other file ran Clean.
- I created the probes in the review-only worktrees `murdoch13-9r6-mut` (old codec) and `murdoch14-9r7-mut` and then reverted them. Their sources are kept as `.cs.txt` in `evidence-9r-7-review/probes/`.
- The Tier 1 and mutant scripts are the zsh files in `evidence-9r-7-review/scripts/`.
- **B1 sweep.** On `f0a54b0b` I searched case-insensitively for `version` across `*.cs`, `*.js`, `*.mjs`, `*.ts`, `*.fbs` and `*.wgsl`. I excluded `bin`, `obj`, `node_modules`, `_bmad-output` and the FlatBuffers runtime, then removed the classified non-format hits. I also searched for schema, format and ABI-number constants, and checked the current docs. Every format-version hit is listed in B1.
- I made no Chrome mutants. Her Chrome-relevant mutants and the suite's own controls (the Escape-cancel control drag, the unsnapped and default-length negatives) cover the Chrome rows, and B1 forces a new full run anyway.

## Round 1 verdict: **Fail** (scoped to Story 9R.7 at candidate `da91cffa`, verified merged with main `2adff597` as `40eb7433`)

- **Blocking: B1.** Format versions remain in the clock wire, clock ring, animation wire, memory wire, trace, isolation handshake, shared ABI headers, wire design fixture and docs, and the removal is not guarded (MV1 survives).
- The joined-pipe lesson, the typed pipe length, the 9R.6 locked-target join, Reset and Save/Load all meet their ACs. The evidence is static review, my Tier 1, my mutants and her reused full run (45/45 files, 234/234 tests). There are no player-visible regressions: the tube suites, cat-005 and cat-062b all pass, and old saves are refused with the typed rejection.
- 11 items are filed to F.10. F10-11 is owned by 9A.16.
- This is a scoped verdict. It claims no global, performance or runtime qualification.

## Round 2 (final)

- **The coordinator's scope ruling on B1, accepted as given.**
  - 9R.7 owns the formats the owner named: the Workshop command and response wire, the save, and the cadence settings record.
  - Every other site in B1 moves to Story 9A.1: the clock wire and clock ring, the animation, memory and trace formats, the physics, body and schedule headers, WireContract, the docs and `policyVersion`. The isolation retry condition goes with it.
  - What remains of B1 for 9R.7 is the guard gap.
- **The candidate.**
  - `914bbbf5` is test-only beyond `120bf876`: `WorkshopFormatTests.TheVersionlessRecordsDeclareExactlyTheirFields` and `WorkshopWireTests.TheResponseHeaderHasZeroPaddingAndNoVersionWord`, plus evidence.
  - The new field-list guard compares the exact schema field list of `CadenceSettingsRecord`, `SavedConstruction`, `WorkshopResponse` and `WorkshopCommand`. So any field re-added under any name fails it.
- **Identity.** I merged it with main `2adff597` as `c64ec2b0` in the review-only worktree `murdoch14-9r7-mut`. The porcelain was clean before and after (`evidence-9r-7-review/round2/`).
- **Build and test.** `dotnet build CuriousContraptions.slnx` gives 0 Warning(s), 0 Error(s). `dotnet test CuriousContraptions.tests` gives 1872/1872.
- **Re-add mutant (R2-MV1).** I re-applied MV1 unchanged (write 21 at response bytes 12–15 and require 21). It is **killed** by `TheResponseHeaderHasZeroPaddingAndNoVersionWord` (1871/1872; `mutants/R2-MV1-response-header-version-readded.txt`). The round-1 command and settings re-add mutants MV3 and MV2 were already killed by tests this commit doesn't change.
- **Chrome.** No Chrome run was needed: the change is test-only, and her full run on `da91cffa` (45/45 files, 234/234 tests) still covers the unchanged product code.

## Terminal verdict: **Pass** (round 2, final; scoped to Story 9R.7 at code candidate `da91cffa` with test-only `914bbbf5`, verified merged with main `2adff597` as `c64ec2b0`)

- The formats 9R.7 owns (command and response wire, save, cadence settings) carry no version. They reject foreign input by structure, and that is now guarded against any re-added field.
- Every lesson AC is met, and there are no player-visible regressions.
- The other B1 sites are owned by Story 9A.1, by the coordinator's ruling.
- F10-1 to F10-11 remain filed; F10-11 is 9A.16's.
- This is a scoped Pass only. It claims no global, performance or runtime qualification.
