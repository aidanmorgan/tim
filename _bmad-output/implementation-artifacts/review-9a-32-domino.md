---
title: "Review — 9A.32 Domino generic instance (CAT-023), independent adversarial review"
candidate: "uncommitted worktree story-9a-32 on base 2065d3c5; tracked diff excluding _bmad-output (git diff HEAD --binary -- . ':(exclude)_bmad-output') sha256 f669a79eccb817f93b7da8803b80e3c2843d5d81852d3e441d757def3e6401c9, the same at the start and end of review; DominoGuardTests.cs 613c2956…634e, DeclaredOrientationTests.cs 356ab6a9…5bf8"
verdict: "Scoped native Pass (23 Oct 2026) after B1-B3 were resolved; SnapshotApproval granted for the final staged tree on base 2065d3c5 (= origin/main). Browser qualification is pending post-catalogue E2E."
date: 2026-10-23
---

# Verdict

**Incomplete.** The conversion is correct, and I independently reproduced its behaviour on the production C# simulation. Two criteria are not yet met:

- **B1.** The boundary of the new shared trailing-field rule is not pinned by any test. My mutant R1 survives.
- **B2.** The player-visible consequence, that every base-era save holding a generic part is now refused, is not in the spec's pending browser cases.

Both go back to the implementation owner. Each needs only a small change and a targeted re-review (see "Smallest resolving checks").

- Browser qualification stays pending under the 21 Oct policy. Nothing here is a browser, release or runtime qualification.
- I edited no source, test, tool or content file.
- Evidence is in `evidence-9a-32-review/`. Raw logs are cited by file name.

# Checks run

There was one matching Debug build, and every test ran with `--no-build --no-restore`.

| Check | Result | Receipt |
|---|---|---|
| Candidate identity, before and after review | Tracked diff `f669a79e…`, matching `candidate/freeze-head.txt`. Both untracked tests match the spec's hashes. | `review-head.txt`, this header |
| `dotnet build CuriousContraptions.slnx -c Debug --no-incremental` | 0 warnings, 0 errors, rc 0. My first incremental build was a no-op, kept only as `build-incremental-noop.log`. | `build.log` |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` on that build | 17 / 77 / 165 / 189 / 2482 = 2930/2930, rc 0 | `full-test.log` |
| Guard filter (`evidence-9a-32/baseline/guards-filter.txt`, sha256 `30b3d871…`) | 1033/1033. The duplicate-ID skip notice is pre-existing: it appears 8 times in the base `guards.log` too. | `guards.txt` |
| CI "Test C# tools" step, extracted verbatim from `pages.yml` and run under `bash -e` | 77 / 165 / 17 / 174, rc 0 | `ci-tools.txt` |
| Release worker: `dotnet build CuriousContraptions.Simulation -c Release --no-incremental` | 0 warnings, rc 0 | `worker-release.txt` |
| Release web publish: `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false`, on the current candidate (a later candidate than the implementer's second freeze) | rc 0; `WORKER_AOT_HOST_FILES=202 identical`; all 5 static files present. AppBundle moved to the scratchpad; `CuriousContraptions.web` status is clean. | `web-publish.txt` |
| Strict tsc 6.0.3 (`--project tools/e2e/tsconfig.json --strict`) and the tsx snapshot unit | tsc rc 0; 24/24 | `ts-checks.txt` |
| Regeneration with pinned flatc 25.2.10 (sha256 `f004709c…`), following `schemas/README.md` | TS generated part identical to fresh output. C# differs only by the header blank line, which base `HEAD` shows against fresh base output as well. | `wire-regeneration.txt` |
| `git diff --check HEAD`, plus `--no-index --check` for the 3 untracked files | Clean. rc 1 from `--no-index` only means the file differs from `/dev/null`; no whitespace errors were printed. | `diff-check.txt` |
| Deletion greps, candidate vs base control (`git grep HEAD`) | Production: 0 for every pattern. Base controls: 17 / 1 / 3 / 8 / 7 / 30, matching the implementer's. My first base run used ERE `git grep`, which has no `\b`, and counted 0 for `WorkshopInput.Domino`; I re-ran it with `-P` (appended to the receipt). The only remaining word hit is prose in `TODO.md`. | `deletion-greps.txt` |
| Known rows | 207 → 181. 26 rows removed, all `Owner: Domino`; 0 added; no other row changed. The 2 `PartId.Domino` FinalIsolation rows are kept. | `known-rows.txt` |
| Identity re-run: archived probe `0733d22e…`, compiled into two throwaway worktrees (candidate copy, and base + `baseline/test-only.diff`) | All 11 captures are byte-identical to the implementer's `candidate/capture/` and `baseline/capture/` respectively. Base vs candidate differ only in `old-save-outcomes` (intended) and the 3 `DominoEffectAuthoredSolutionWired` lines (content fix). | `identity-probe-rerun.txt`, `probe-rerun-capture.sha256` |
| Old-save bytes | 65/65 match `old-saves.sha256` | (command in log) |
| Reviewer mutations R1–R8, with a control (M0: 2482 + 165 pass) | 7 killed, **1 survived (R1)** | `mutation/summary.txt`, `mutation/*.test.log` |
| Carried C-Lamp+Detector receipts | `evidence-c-lamp-detector-review/terminal/` is identical to `carry/clamp-terminal-evidence`. The three review-file hunks and the sprint-status C-Lamp hunks are byte-identical to `carry/clamp-terminal-receipt.patch`. The only extra hunk is this slice's own `9a-32 … review` sprint line. | `carried-integrity.txt`, `carried-actual-hunks.patch` |

# Criterion and impact matrix

| # | Criterion | Independent evidence | Verdict |
|---|---|---|---|
| 1 | The declared threshold (finite, 4–179°, pulse out, dynamic body) is bit-equal to the old 45° | `OrientationThreshold.Degrees(double)`: `45f` widens exactly to `45.0`, so the half-angle cosine is the same Half (`0x3b64`, pinned in the parity test). The validator's float bounds equal the double bounds. Refusals are BundleErrors at 4−ulp, 179+ulp, NaN and ±∞, and for missing, level or input ports, a Static body, no mechanical payload, and a second capability. R5 (`>=4` → `>4`) and R6 (package declares 44°) were killed. A radians-declared π/4 is below 4 and refused by the same lower edge. Traces are bit-identical (row 5). | Pass |
| 2 | Deletions; 4 wire fields; content keys; regeneration | Greps 0 (control non-zero). The schema has none of the 4 fields. TS and C# accessors are gone. Content check: base `puzzles.json`, with `half_extents` and `scalars.{Mass,Bounce,Friction}` stripped by jq from all 28 templates, equals the candidate except for the 8 domino_effect solution coordinates and the one solution connection. Every template now carries scalars {Length, Threshold}. `AuthoredInstanceReader.Require` is strict, so a stale key would be refused. Regeneration matches. | Pass |
| 3a | Trailing-field rule: offsets and logic | Slots match the generated accessors: `SavedConstruction.Construction` `__offset(6)`, `WorkshopCommand.ConstructionRecord` `__offset(26)`, `ConstructionRecord.Instances` `__offset(10)`. `InstanceRecord.Parameters` is `__offset(22)`, slot 9, so there are 10 fields. The vtable walk (`table − soffset`, entry at `vtable+4+2·slot`, loop bound `≤ size`) is correct and reads only inside verifier-checked vtables. R2 (`<` bound), R3 (command slot 24) and R4 (rule moved before pre-validation, changing the counter's reason) were killed. | Pass |
| 3b | No false refusal of current saves | Every current-encoder record in the suite loads at both boundaries: domino, lamp, a Ramp whose parameters fill slot 9, pipe, counter, and every round-trip test. Full suite green. | Pass |
| 3c | The rule's boundary is pinned | **R1 (`InstanceRecordFields = 11`) survives, 2482/2482.** No test forges a present field at exactly slot 10: every old-layout fixture is refused by slot ≥ 11 or by another rule. The spec says "the walk's slots are pinned against the generated accessors", but the field count is not. | **Incomplete (B1)** |
| 3d | Consequence consistent with AGENTS and documented | No migration and no shim. Refusal is typed (save `OutOfRange` with "This save cannot be loaded: …", wire `OutOfRange`), and 0 untyped across the 99-line matrix. This is consistent with AGENTS and the README rule "no format version … refused structurally, never migrated". It is documented in spec Current state item 2 and `TODO.md`, **but not in "Pending browser cases"**. The visible path (stored save → refusal shown → bench kept) is today exercised only by `engine-core-2b1` test 2, with a corrupted save. | **Incomplete (B2)** |
| 3e | `OutOfRange` vs a dedicated reason | The player sees an accurate message: the save cannot be loaded and a record holds a field the current record does not define. That is not misleading, so it is non-blocking (F1). | Pass with F1 |
| 4 | An absent pipe length is typed `MissingField` at the command decoder | `RequiredLength` throws `WireFaultException(MissingField)` on bits 0. A present length goes to construction validation. Base pipe saves: save `OutOfRange`, command `MissingField`. `WireFaultTests`' no-catch rule holds. | Pass |
| 5 | Identity: traces, 12×3 lessons, placed solutions; domino_effect fix | Reproduced independently (table above). In my base run, the legacy layout fails at 0, 0.45 and 1 (`solved=False`): the ball's closest horizontal gap is 1.994 m against a 0.465 m contact distance, and the last tile overlaps the end tile. The new content solves at all 3 (`solved=True`), and its admission, trace and reset hashes equal the hand-built corrected layout on both base and candidate. `lesson-world.txt` is identical, so the fixtures, inventory and goal are untouched. The `fixture_2` → `fixture_3` activation connection is load-bearing: R7 removes it and R8 restores the legacy rows, and both fail `TheAuthoredSolutionSolvesAtEveryPrecision(domino_effect)`. `lessons-484ec38.json` and `compiled-geometry-9a16.json` are absent from status. | Pass |
| 6 | Known 207 → 181, exactly the Domino rows | See `known-rows.txt` | Pass |
| 7 | Guard delta 1039 → 1033 | 23 rows removed and 16 added (1032), plus the domino_effect authored-solution case (1033). Each removal tests a deleted concept. The forged material is now covered by the typed-field and parameter refusals at both boundaries. The `MissingField` save reason is kept by `"no counter target"` (`WorkshopSaveRejectionTests:303`). The wired record-path "passes twice" run is kept with a current-encoder save, and its old bytes are now pinned as refused. Battery and domino reason changes are derived (OutOfRange from the beyond-table rule or parameters). No coverage loss found. | Pass |
| 8 | Mutations | Implementer's M1–M10 all killed. Mine: R2–R8 killed, R1 survived (B1). | see B1 |
| — | Carried C-Lamp receipts | Byte-integrity holds (table above) | Pass |

# Findings

**B1 (blocking): the trailing-field rule's boundary slot is untested.**
- *Criterion:* item 3. The rule refuses any field at slot ≥ 10, and coordinator decision 2 says a record is never loaded with data ignored.
- *Risk:* the off-by-one-up mutant (`InstanceRecordFields = 11`) passes the full suite. A record with one present field at slot 10 would then be loaded with that field silently ignored. That is the exact class the rule exists to refuse, and nothing would detect a later regression or a schema edit that misses this constant.
- *Why blocking:* this is the shared, new refusal rule at both untrusted boundaries. A boundary that is unknown to the tests counts as Incomplete under AGENTS.
- *Smallest resolving check:*
  1. Add one forged current-layout record (for example a bare ball, or `OldLayoutSaves` with `new(10, …)`) whose only extra entry is slot 10. Assert it is refused `OutOfRange` at `WorkshopSaveCodec.Decode` and `WireFaultReason.OutOfRange` at `DecodeCommand`.
  2. Optionally, also assert `InstanceRecordFields` equals the generated table's field count.
  3. The reviewer re-runs R1, expecting it killed, and the affected class.

**B2 (blocking): the player-visible save refusal is missing from the pending browser cases.**
- *Criterion:* item 3 ("documented in spec/pending browser cases"), and the AGENTS rule to accumulate affected browser cases.
- *Risk:* after deployment, every player's stored save that holds any generic part is refused. That is nearly every bench, since the base encoder wrote an empty parameters vector at slot 13. The post-catalogue E2E phase would not know to prove that the new refusal is shown and the bench kept, or that a slots 0–5-only save (the latch) still loads.
- *Smallest resolving check:* add one bullet to the spec's "Pending browser cases". For example: "`engine-core-2b1` variant: a base-encoder (2065d3c5) stored save holding a generic part is refused with 'This save cannot be loaded…' (OutOfRange) and the bench is kept; positive control: a current save loads." Also add `engine-core-2b1` to the re-run list. I found no e2e suite that bakes an old save (`tools/e2e` has no CCWS/base64 save blobs), so no other suite needs re-queuing. This is a doc-only change; the reviewer re-checks the spec diff. In the same spec pass, fix the stale `**Next:**` line, which still asks for a "coordinator decision on item 1" although item 1 is decided (non-blocking on its own).

**Non-blocking**
- **F1.** `OutOfRange` is acceptable because the player message is accurate. However, neither enum's doc comment covers "a field beyond the current table". Either widen both doc comments or later add a dedicated `UnknownField` reason (owner: wire).
- **F2.** The `WorkshopSaveCodec.Decode` summary still says structural pre-validation "is the only refusal". The trailing-field rule now refuses after it, so update the comment.
- **F3.** The response decoder (`WorkshopWireFlatBuffers.cs:568`) maps a construction without the rule. This is acceptable: the channel is worker-to-main, written by the same encoder, not an untrusted store. The other wire tables' trailing-field gap is recorded as out of scope in spec item 2.
- **F4.** `candidate/capture-compare.txt` cites probe `ed3a0b47…`, not the final `0733d22e…`. This does not matter, because my rerun of `0733d22e…` reproduces both capture sets byte for byte.
- **F5.** There is a typo in the `PreConversionLaunchSaveTests` comment: "beyond the table.Old Switch" is missing a space.
- **F6.** The implementer's disclosure that most writes ran without a prior `anvil_validate_write` is a process deviation. The implementer's `anvil_check` reported 0 findings. I did not re-run `anvil_check` myself.

# Process

- These were my own reruns, with no reuse of implementer receipts as proof. The only exception is the carried-receipt byte comparison.
- Throwaway worktrees `review-wt-cand` and `review-wt-base` lived under the scratchpad for the probe and the mutations. They were removed with `git worktree remove --force` and pruned (0 remain). The candidate copy was verified identical to the candidate before mutating and after restoring.
- The AppBundle from my publish was moved to the scratchpad.
- I wrote only under `_bmad-output/implementation-artifacts/`: this file and `evidence-9a-32-review/`. The duplicate probe captures were replaced by a sha256 manifest.
- The candidate tracked-diff hash was unchanged at the end of review.

# Smallest resolving checks (for re-review)

1. **B1:** a slot-10 forged-record test refused at both boundaries. The reviewer re-runs R1 (killed) and `DeclaredOrientationTests`.
2. **B2:** the pending-browser-case bullet and the `engine-core-2b1` re-run entry in the spec.

After both:
- The reviewer records the new tracked-diff hash that replaces `f669a79e…`.
- On one fresh Debug build, the reviewer runs the full `CuriousContraptions.tests` project and R1.
- The reviewer re-checks the changed test file and the spec diff.
- The Release worker and web publish do not need re-running, because test files and the spec are not inputs to those projects.
- Then SnapshotApproval follows, if `origin/main` is still `2065d3c5`.

# Re-review of the fixes (23 Oct 2026)

**Candidate.**
- Tracked diff excluding `_bmad-output`: sha256 `b5226f9e2365c355f4c015d129eecacce432cd9a6a470e2eea281a7e81276c68`, the same at the start and end of the re-review.
- `DeclaredOrientationTests.cs`: `c4e0f23b…`. `DominoGuardTests.cs`: `613c2956…`, unchanged.
- Compared with `f669a79e…`, the tracked diff changes only comments: F2 in the `WorkshopSaveCodec.Decode` summary, and F5 in the `PreConversionLaunchSaveTests` comment.

Evidence is in `evidence-9a-32-review/rereview/`.

| Check | Result | Receipt |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug --no-incremental` | 0 warnings, 0 errors | `build.txt` |
| Full slnx `--no-build --no-restore` | 17 / 77 / 165 / 189 / 2483 = 2931/2931, Skipped 0, rc 0 | `full-test.txt` |
| **B1:** R1 (`InstanceRecordFields = 11`) on a throwaway copy of the candidate | **Killed.** Only `AFieldAtExactlyTheFirstSlotBeyondTheTableIsRefusedAtBothBoundaries` fails. The test writes slot 10 as a literal rather than reading the rule's own constant, and it has a load control. | `R1.test.log`, `crash-standalone-summary.txt` |
| **B2:** spec "Pending browser cases" | Now includes the `engine-core-2b1` variant (a base-era stored save is refused with "This save cannot be loaded…" and the bench is kept) and adds `engine-core-2b1` to the re-run list. The Next line is corrected. **Resolved.** Optional tightening: "holding a generic part", since a base save that uses only slots 0–5 (a latch-only bench) still loads. | spec |
| F2, F5 | Fixed (comment-only) | diff |

**Crash investigation (exit 139 in `review-fix-test-run1-crashed-139.txt`)**
- **Runs.**
  - Candidate (throwaway copy): 5 standalone full `CuriousContraptions.tests` runs, the R1 run, and 3 full-slnx runs, all with `--blame-crash`.
  - Candidate (main worktree): 3 further slnx runs today.
  - Base `2065d3c5` (throwaway copy): 5 standalone and 3 slnx runs, interleaved with the candidate runs.
  - Result: **0 test-host crashes** on either side. Every host exited normally, with no "exit code" or "Catastrophic" line in any log.
- **Blame dumps.** The 21 `.dmp` files blame wrote all belong to `Preview` child processes, which `E2eRunner.Tests` starts and deliberately kills. There are 4 per slnx run, on base and candidate alike, and none from any test host (`blame-dumps.txt`). I deleted them after recording, since they are about 380 MB each.
- **TypeCheck failure in the throwaway copies.** `EverySuiteAndSharedModuleTypeChecks` failed with "Cannot find module 'flatbuffers'". This is environmental: the fresh copies have no `node_modules`. It happens identically on base, and the test passes 189/189 in the main worktree.
- **Code path.**
  - This slice's new code reads only through the vendored `ByteBuffer`'s bounds-checked managed path (`ReadLittleEndian` → `AssertOffsetAndLength`). `UNSAFE_BYTEBUFFER` is not defined anywhere, and the reads stay inside verifier-checked vtables.
  - The unsafe and SIMD paths are unchanged by this slice: `CuriousContraptions.Simulation` (`AllowUnsafeBlocks`; solver and collision kernels) and `EventRingBuffer` (`Unsafe.As`).
  - One observation, not a cause: the crashed host had run about 1:40 at the crash, against the usual ~52 s for the whole project, after only 2045 tests.
- **Classification: unreproducible.** It was observed once, in the implementer's runs only, against 0 in my 12 candidate and 8 base host runs. It is not attributable to the candidate and does not block this slice.
- **Owner:** engine/test infrastructure, assigned by the coordinator. Recommendation: run `--blame-crash` on the `CuriousContraptions.tests` project in future runs so a recurrence names its test and leaves a dump.

**B3 (blocking, user directive "make sure there are zero skipped tests"): 8 theory rows are discarded as duplicates every run.**
- Every assembly reports `Skipped: 0` in all 45 summaries (`skipped-tests.txt`), and there are no static `Skip =` attributes. However, xUnit prints "Skipping test case with duplicate ID" 8 times per `CuriousContraptions.tests` run and never executes those rows. It drops them before counting, so `Skipped: 0` does not include them.
- The rows are `ConstructionDecodeBoundaryTests.{FlatBuffersRecord,SaveCodec}AcceptsExactlyTheAdmittedSetting` × {Clock, PressurePlate} × {2.14748365E+09, NaN}.
- Cause, in `FloatBoundaries`, confirmed with `dotnet fsi`:
  - `(float)int.MaxValue` is bit-identical to `TwoTo31` (`4F000000`); the file's own comment says so.
  - The "negative quiet NaN" `0xFFC00000` row is bit-identical to `float.NaN` on .NET (`FFC00000`).
  - So no coverage is lost, but the rows are still discarded.
- This is pre-existing: base shows the same 8 notices. The file is outside this slice's change set. It is required now only because of the user's explicit instruction.
- **Smallest fix (implementer, test-only):**
  - Delete the `(float)int.MaxValue` row.
  - Delete the `0xFFC00000` row, or replace it with a distinct payload such as `0x7FC00000`.
  - Expected `CuriousContraptions.tests` total: 2483, or 2487 if the NaN row is replaced.
- **Re-review check, on one fresh build:**
  - full slnx;
  - `grep -c "duplicate ID"` = 0 in every log;
  - `Skipped: 0` in all 5 assemblies;
  - the expected totals;
  - the new tracked-diff hash.
- **After the fix lands:**
  1. Re-run `cmp` on the carried C-Lamp receipts.
  2. Check `origin/main`.
  3. SnapshotApproval.
- **CI note (not a skip).** The CI "Test C# tools" step deselects 15 E2eRunner toolchain tests by filter, by design and pre-existing. All 189 run in the full slnx.

**Re-review verdict.** B1, B2, F2 and F5 are verified. The crash is unreproducible and non-blocking. The verdict is **Incomplete on B3 only**, so SnapshotApproval is withheld and nothing is staged.

# Second re-review: B3 (23 Oct 2026)

**Candidate.**
- Tracked diff excluding `_bmad-output`: sha256 `3c434b3018ca7aaf1af08599d07ed440912060cadfefe2d11a2384cd82478281`. Relative to `b5226f9e…`, it adds the `ConstructionDecodeBoundaryTests` row fix.
- `DeclaredOrientationTests.cs`: `c4e0f23b…`. `DominoGuardTests.cs`: `613c2956…`.

Evidence is in `evidence-9a-32-review/rereview2/`.

| Check | Result | Receipt |
|---|---|---|
| B3 diff | The `(float)int.MaxValue` row is removed; it was bit-identical to `TwoTo31`. The `0xFFC00000` row is replaced by the positive quiet NaN `0x7FC00000`, which is distinct from `float.NaN` (`FFC00000`) and from `0x7F800001`. A comment states the reason. | diff |
| `dotnet build CuriousContraptions.slnx -c Debug --no-incremental` | 0 warnings, 0 errors | `build.txt` |
| Full slnx `--no-build --no-restore` | 17 / 77 / 165 / 189 / 2487 = 2935/2935. Skipped is 0 in all 5 assemblies. **0 "duplicate ID" notices** and 0 "Skipping" lines. No crash. rc 0. | `full-test.txt` |
| B2 wording | "a stored save written by base 2065d3c5 holding a generic part is refused …". Resolved. | spec |
| Carried C-Lamp+Detector receipts | The three review-file hunks are byte-identical to `carry/clamp-terminal-receipt.patch`. The sprint-status file differs from the patch only by this slice's own `9a-32 … review` line. `evidence-c-lamp-detector-review/terminal/` is identical to `carry/clamp-terminal-evidence`. | `integrity-and-main.txt` |
| `git diff --check HEAD`, plus `--no-index --check` for the 2 new tests | Clean | `integrity-and-main.txt` |
| `origin/main` after `git fetch` | `2065d3c5951232b3e04332101f3d31b45d5e10f1`, the candidate's base | `integrity-and-main.txt` |

**B3 is resolved.** All blocking findings (B1, B2, B3) are resolved, and every criterion in the matrix is Pass.

The crash classification is unchanged: unreproducible and non-blocking. The owner is engine/test infrastructure, assigned by the coordinator. The full run above is clean.

# Verdict (final)

**Pass**, in the scoped native, required-now scope, and SnapshotApproval is granted below.

Browser qualification remains pending until the post-catalogue E2E phase. The cases are listed in the spec: `cat-023a`, `cat-023b`, the `engine-core-2b1` variant, and re-runs of `cat-014`, `cat-052`, `cat-052-lamp`, `clock-master`, `palette-placement` and `engine-core-2b1`. This is not a browser, release or runtime qualification.

The open non-blocking items are F1, F3, F4 and F6 above, plus the crash owner.

# SnapshotApproval (23 Oct 2026)

I approve the exact staged snapshot for commit, merge and push. Its identity:

- **Base:** `2065d3c5951232b3e04332101f3d31b45d5e10f1`, equal to `origin/main` at approval.
- **Pre-append staged state:** 342 files. Tree `c8c19de2e3a6c2f8d28594f22f52d908ee3f1228`. `git diff --cached --binary HEAD` sha256 `8a8fd6388ef9229ff074d3c374c28024f8e2d05ade1dfffba67457e89ae9744c`.
- **Source portion:** the staged diff excluding `_bmad-output` has sha256 `f5560feb41158f5e31032d3f17b4daf04422eaefc8a03c0efc73d3bdcb25497f`.
- **This approval text:** appending it changes only this file. The final staged tree, after re-staging, is reported to the coordinator, and is the only tree approved.
- **Contents:**
  - the 9A.32 slice: source, tests, content, docs, `TODO.md`, the spec, `evidence-9a-32/`, this review and `evidence-9a-32-review/`;
  - the carried C-Lamp+Detector terminal receipts: `review-c-lamp-detector.md` terminal section, the `review-6-8` and `review-6-5e` addenda, the sprint-status lines, and `evidence-c-lamp-detector-review/terminal/`;
  - this slice's `sprint-status` line (`9a-32 … review`).
- **Whitespace:** `git diff --cached --check` flags only verbatim raw tool logs under `evidence-*`; the base tree already has 868 such files. With those excluded, the check is clean.

**Pre-publication criteria verified for this snapshot:**
- one `--no-incremental` Debug build with 0 warnings;
- full slnx 2935/2935, with 0 skipped and 0 discarded tests;
- CI tools step, Release worker, web publish and strict tsc + snapshot unit (run on `f669a79e…`). The later edits are tests, comments and the spec only, which are not inputs of the worker, web or TS projects; the full slnx run covers every tools test;
- regeneration;
- deletions;
- Known rows;
- identity re-run;
- mutations (R1 to R8 killed after the fixes);
- carried-receipt integrity;
- `origin/main`.

## Publication-dependent checks (for the terminal verification)

1. The pushed commit's tree equals the approved final staged tree, the merge into `main` has the same tree, and `origin/main` points at it.
2. The pre-commit hook (C# static gate and anvil) passes on the exact snapshot, with its log kept.
3. The exact-SHA GitHub Actions run passes every step: static gate, Test C# game (2487), Test C# tools (77 / 165 / 17 / 174), production publish and the static deployment check.
4. The Pages artifact and deployment are for that SHA, and the deployment status is success.
5. The deployed origin serves the new bundle (`index.html`, `godot.wasm`, `godot.pck`, `_framework/dotnet.js`) for that run.
6. After publication, `sprint-status` marks `9a-32-generic-instance-domino` done, with the terminal scoped Pass recorded and browser pending.
