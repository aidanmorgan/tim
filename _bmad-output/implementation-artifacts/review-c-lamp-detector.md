---
title: "Review — C-Lamp+Detector (9A.36 + 9A.37 + 9R.9 + 9R.16), independent adversarial review"
candidate: "uncommitted worktree story-c-lamp-detector on base 54ffd9f0; tracked diff (git diff --binary HEAD) sha256 55b99c26f868502e14b0a3f86603710dacd04036768a9e4cd5912aad86f5b70e, unchanged at start and end of review; LampDetectorGuardTests.cs sha256 1859953…cb3f, DeclaredIndicatorPassageTests.cs 5a8e2694…9955"
verdict: "Pass (native, required-now scope). SnapshotApproval granted (see the section at the end). Publication-dependent checks are pending. Browser qualification is pending post-catalogue E2E."
date: 2026-10-23
---

# Verdict

**Pass.** I found no blocking findings in the native, required-now scope. There are six non-blocking findings (F1–F6). F1 needs a coordinator decision before the post-catalogue `cat-002` case is authored.

- `origin/main` is `54ffd9f0`, the candidate's base, so SnapshotApproval applies directly (no merge needed).
- Browser qualification is pending (21 Oct policy). This scoped Pass is not a browser, release or runtime qualification.
- `9r-9` and `9r-16` stay backlog: their Chrome ACs are pending.

I did not edit any source, test, tool or content file. Evidence is in `evidence-c-lamp-detector-review/`, and raw logs are cited by file name.

# Checks run (one matching Debug build; tests `--no-build --no-restore`)

| Check | Result | Receipt |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors (incremental: up to date) | `build.txt` |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | 17 / 164 / 77 / 189 / 2464 = 2911/2911 | `full-test.txt` |
| CI "Test C# tools" step under bash, extracted verbatim from the workflow | 77 / 164 / 17 / 174, rc 0 | `ci-tools.txt` |
| Release web publish `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false`, in a throwaway worktree of HEAD + the exact candidate | rc 0, 0 warnings, `WORKER_AOT_HOST_FILES=202 identical`, all 5 static-deployment files present | `web-publish.txt` |
| Release worker `dotnet build CuriousContraptions.Simulation -c Release` (same throwaway) | rc 0, 0 warnings | `worker-release.txt` |
| Strict tsc 6.0.3 (`--project tools/e2e/tsconfig.json`, @types/node 26.0.1) and the snapshot schema unit (tsx) | tsc rc 0; 24/24 | `ts-checks.txt` |
| `git diff --check HEAD`; whitespace scan of the two new test files | clean; clean | `diff-check.txt` |
| Deletion greps, candidate vs a base control at `54ffd9f0` | all 0 (base 50 / 63 / 4 / 1 / 2 / 1) | `deletion-greps.txt` |
| Known rows | 234 → 207: 27 removed (Lamp 17, Detector 10), 0 added, 0 left with those owners | see item 7 |
| Identity probe: the implementer's probe and my own, built on base (record adapter) and on the candidate | traces, lessons and placed solutions identical; see item 5 | `identity/` |
| My 8 mutants (control 208/208 + 164/164) | 6 killed, 2 survived (F3, F4), neither equivalent | `mutation/` |
| Battery carry: byte integrity only | identical; see Process | — |

# Criterion and impact matrix

| # | Criterion (source) | What I did | Result |
|---|---|---|---|
| 1 | Lamp node selected by a declared indicator on ActivationIn; generic, no PartId branch (9A.36, D1) | Read the compile arm. It is gated on `WorkshopGenericInstance` plus `Signal?.Indicator`, with no PartId. `IndicatorKind` keeps the old rule (a link from a declared Level output) and narrows it to the declared input. The lamp has only ActivationIn, so the node kinds are unchanged; my traces and the implementer's prove it. Mutant R2 (ignore the link target) killed 5 tests; R6 (admit undefined sockets) killed 1. | Pass |
| 2 | Aperture 0.65 / 0.02 bit-equal; typed validator refusal (9A.37, D2, D3) | The parity test pins `0x3933`/`0x251f` and their equality to `(Half)0.65`/`(Half)0.02`. The candidate's compiled sensor lanes `r=3933 clear=251f` match base in my traces. `BundleValidatorTests` refuses with typed `BundleError`s (UndefinedEnum, NonFinite, OutOfRange) and tests each range end; mutant R3 (any kinematic frame) was killed. Mutant R5 (indicator input direction unchecked) survives: F4. | Pass |
| 3 | 9R.9 native proof (reading: passage count 1→2, first-input lamp stays at 1) | See "Ruling on 9R.9" below. | Pass under the reading; F1, F2 |
| 4 | Negative controls discriminate (glancing, reverse) | Each proves a crossing by positions on both sides of the plane. Mutant R7 (aperture ×2) fails the glancing control; R8 (inverted sensor rotation) fails the reverse control. Neither control passes vacuously. | Pass |
| 5 | Identity: traces, 12×3 lessons, placed solutions, save replays | I rebuilt base + record adapter and the candidate in throwaway worktrees and ran the implementer's probe (sha256 b636a615…, compiled from outside the tree as the implementer did) on both. All 10 traces, `lesson-world` (36 rows) and `placed-solutions` (36 rows) are identical base vs candidate, and identical to the implementer's baseline files. My own bench is also identical over 720 ticks (`reviewer-trace.txt`): a 3D-rotated detector (roll −90° then yaw 37°), two balls (Basketball and Bowling), a sticky lamp, a counter(2) lamp, and a latch Set by the detector feeding a level lamp. The 12 playable puzzles are exactly the probe's 12; the 17 other lamp-bearing entries in `content/puzzles.json` are Planned and never constructed. **Save bytes are not identical across versions.** The generic encoder writes an empty parameters vector, so the candidate's save and command bytes differ from base's. The base-written bytes decode on the candidate to a construction equal to the candidate's own decode and to the authored one (`reviewer-encodings.txt`), and save replays (480 ticks) hash-identical. | Pass |
| 6 | Old saves load as generic at both decode points; typed-field refusal at the command boundary | I re-wrote the old saves with the base encoder; their sha256 values equal the implementer's. The candidate's outcomes are byte-equal to the implementer's: every lamp and detector record (bare, locked/rotated, wired) loads as `Generic … params=[]` at both the save and command boundaries, with no misread fields (cell/local/rotation/locked shown). All 7 forged typed fields: base admitted them at the command boundary and refused them at save; the candidate refuses them OutOfRange at both. A parameter is refused at both. | Pass |
| 7 | Deletions 0; Known 234→207, exactly the owned rows | Greps above. Known: set difference of rows (JSON-normalised) is exactly 27 removed (Lamp 17, Detector 10), with none added or reassigned. | Pass |
| 8 | `LatchSupplyFixtureTests` keeps its assertions (9.4c N2) | Its diff is one line, the builder `WorkshopInput.Lamp` → `PlacedParts.Lamp`. | Pass |
| 9 | `cat-002` rewrite type-checks; semantics change justified | tsc rc 0. The old any-target predicate (`all.some`) was satisfiable by the detector's own pulse (report #15). The new count reads only the lamp target, as dark-to-lit edges per property, relative to a pre-Run count. History surviving Reset is consistent with `anim-1b` (sample counts unchanged across Reset). `LAMP_TARGET='4'` follows from `_nextId` starting at 1 and the node + 2 convention used in `anim-1b` and `cat-023b`. The detector Connect anchor is unaffected: the generic panel adds no rows when there are no parameters (`AddDeclaredConfiguration` returns early). Justified; Chrome pending. F5 notes that "exactly" is not held. | Pass (type-check only) |
| 10 | Mutation strength | Implementer M1–M5 were all killed. Mine: R1 sensor ignores rotation (8 failed), R2 indicator ignores target (5), R3 passage frame any kinematic (1, bundle), R4 sensor ignores Local (survives), R5 indicator direction unchecked (survives), R6 undefined socket admitted (1), R7 aperture ×2 (5), R8 rotation inverted (9). All restored; the worktree was removed. | Pass; F3, F4 |
| 11 | Closed sets, no fallback/shim | `PortSocket`→`WorkshopSocket` maps through a single checked function that rejects undefined values; there are no string identities in the new logic. D6 is decoding of an unchanged layout, not a shim: no branch exists for old data, and the decode is the current generic arm. The renames keep ordinals 7/8 and their text (tested). | Pass |
| 12 | Docs | CAT-035 and CAT-002 (status, sensors, parameters, tests), the isolation-doc row and the f32 status row are consistent with the code. | Pass |

## Ruling on 9R.9 (item 3)

**Accepted as the native proof under the coordinator's reading.**

`OneBallFiresForwardNotInReverseAndFiresAgainAfterClearingUpstream` reads the passages from a wired tally counter's committed memory:
- `Count` goes 0→1→2, never more than one per tick, with strictly increasing `Last` ordinals.
- These are production committed state, written by the worker from the detector's committed pulses.
- Replaying the production `PassageSensorTracker` over the committed bodies gives `PassedCount` 2, fired at the same two ticks.
- The trace proves the reverse leg physically crossed the plane and cleared the rearm line, and the ball stays in the bore.
- The first-input lamp holds the first occurrence, and the counter(2) lamp lights at the second.

The detector's own latch cannot show 2: a `PassageSource` node latches its first occurrence until Reset, on base and on the candidate alike. So the committed self-read gap predates this slice; it is not a regression. The slice is not required to add a read surface; adding one now would pre-empt 9A.22.

**Owner of the gap: 9A.22.** Its `Passage` ring kind ("exactly one ring record per discrete transition") is the committed per-occurrence read (F2).

# Findings (none blocking)

**F1 (Medium, owner: coordinator; decide before post-catalogue `cat-002` is authored).** The epics 9R.9 AC still reads "a second forward crossing in the same run … produces exactly one more [lamp activation, via `readAnimationSamplesForTarget`]". A first-input (sticky) lamp cannot show a second activation in the same run. The "passage count 1→2" reading lives only in the spec, and the spec's pending browser case ("the in-run second crossing after clearance") inherits the impossible assertion.
- Risk: the post-catalogue case is authored against an unreachable predicate.
- Smallest resolving check: amend 9R.9 (and CAT-002 §5) so the in-run case reads either the counter(2) lamp's target or a 9A.22 Passage ring record; re-pulse stays with 9A.23.

**F2 (Low, owner: 9A.22).** There is no committed per-occurrence read of the detector beyond its first latch; the proof uses a wired counter. See the ruling above.

**F3 (Low, test strength; owner: implementation owner, or the next touch of `PassageSensorFor` in 9A.44).** Mutant R4 makes `PassageSensorFor` drop `owner.Local`. It survives the committed suite but is not equivalent: under it the sensor plane moves (trace `local=… Y = -0.439375` becomes `0`) and the r9, rotated-20 and reviewer traces change (`mutation/R4-trace/`).
- Every committed test either places the detector cell-aligned (Local 0) or asserts only relations.
- Smallest resolving check: give the detector in `TheLampAndDetectorCompileTheNodesTheirDeclarationsSelect` (or the aperture boundary bench) an off-grid position, so the `sensor.Local == detector.Local` assertion is non-trivial.

**F4 (Low, test strength; owner: implementation owner, optional).** Mutant R5 drops the `Direction == Input` clause of the `Indicator.Input` rule and survives. It is not equivalent: the validator has no socket/direction agreement rule, so an indicator naming a Level output (e.g. a latch-style ActivationOut) would be admitted.
- Smallest resolving check: add a `Refused(… Indicator = new(PortSocket.ActivationOut) …, "Indicator.Input")` case on an entry with a Level ActivationOut.

**F5 (Low, browser-pending; owner: post-catalogue E2E).** `lightsOnceMore` resolves as soon as the count equals `before + 1`. It never re-reads after a settle period, so a later double flash would pass.
- Smallest resolving check: after the wait, hold for the glow duration plus margin and assert the count is still `before + 1`.

**F6 (Low, coverage note).**
- 9A.37 says "a detector on a pipe route". No production-simulation test runs a ball through a pipe into a detector; none existed on base either.
- The guards cross the detector's own bore (its 17-collider collar on the same compiled sensor), and `PalettePlacementTests` proves the pipe-mouth snap.
- Cosmetic: `CuriousContraptions.tests.csproj` now has two `<Compile>` elements on one line.
- Resolve with the post-catalogue `cat-002`/`cat-020` runs, or add a native pipe-route bench at the next touch.

# Process

- **Implementer disclosures** (spec item 7) are consistent with what I found:
  - The probe sha matches.
  - The probe compiles only from outside the tree. I hit the same CA1849 analyzer errors when I compiled it in-tree, which is consistent with the disclosure that it was compiled from the scratchpad.
  - There is no AppBundle in the candidate.
  - The `.uid` and source deletions are staged as `D`.
- **Battery carry:** byte integrity only, not content. I applied `carry/battery-terminal-receipt.patch` to the HEAD blobs.
  - `review-8-1-battery.md` and `review-9a-39-battery.md` are byte-identical to the worktree.
  - `sprint-status.yaml` differs only in the five C-Lamp+Detector lines (317–325).
  - `evidence-9a-39-review/terminal/` is identical (`diff -r`) to `carry/battery-terminal-evidence`.
- **My own file operations (disclosed):**
  - `git worktree add/remove` of two throwaways under the scratchpad.
  - `cp` of the probes and test files into them.
  - `mv` of the in-tree probe copies out to the scratchpad (CA1849) and `Edit` of throwaway csproj/`PlacedParts`/mutant sites.
  - `sed` only to extract the CI step text into a scratch script.
  - Receipts written by shell redirection or `tee`.
  - Copies of my probe and mutation runner into this evidence directory (`identity/ReviewerLampDetectorProbe.cs.txt`, `mutation/mutant.sh.txt`).
  - `python3 -I` read-only for the JSON comparisons.
- **R2 slip (disclosed).** My first R2 form failed to build (IDE0060, unused `owner`), and the runner went on to run the previous mutant's (R1) stale binaries. That log was overwritten by the compiling rerun (`(link.Target == owner || link.Source != owner)`), and the runner now stops on a build failure.

# SnapshotApproval (23 Oct 2026)

**Granted** for the staged snapshot below. I fetched `origin/main` on 23 Oct 2026: it is `54ffd9f072ffb34acaeeab02f152dabc43c8d71f`, the candidate's base, so no merge re-review is needed.

- **Approved snapshot** (staged before this section was appended):
  - tree `e575cb01f80b0d782ba1bd4716f6fce063cba5c5`;
  - SHA-256 of `git diff --cached --binary HEAD` = `8c67b099423714412fbfb3777dfe13df79e9b0813dd1e000b8e6d5dd5c08d1ff`;
  - 216 added, 2 deleted and 52 modified paths (270); nothing unstaged and nothing untracked.
- **Contents:**
  - the C-Lamp+Detector slice: source, tests, the Known fixture, `cat-002.test.ts`, docs, `TODO.md`, and the `sprint-status.yaml` lines 9a-c-lamp-detector/9a-36/9a-37 `review` and 9r-9/9r-16 `backlog`;
  - the spec, `evidence-c-lamp-detector/`, `evidence-c-lamp-detector-review/` and this record;
  - the coordinator's carried Battery terminal receipt (`review-9a-39-battery.md` terminal section, `review-8-1-battery.md` addendum, the `sprint-status.yaml` 9a-39 `done` and epic-8 comment lines, `evidence-9a-39-review/terminal/`). I verified it byte-exact against `carry/battery-terminal-receipt.patch` and `carry/battery-terminal-evidence`.
- **Pre-publication criteria verified:** every row of the checks table and the criterion matrix above, on source identity `55b99c26…` (tracked diff unchanged from start to end; the new test files' sha256 values are as recorded in the front matter).
- **Whitespace:** `git diff --cached --check HEAD` reports hits only inside raw command logs under `evidence-*` directories. These are unedited tool output, as in the published 9A.39 evidence. Every source, test, doc, spec and record path is clean.
- The re-staged tree after this append differs from the approved tree only in this record. That is the expected receipt delta.

## Publication-dependent checks (for the terminal verification)

1. Commit the staged snapshot on branch `story-c-lamp-detector`. The commit's tree must equal the re-staged tree reported with this approval (this record included). Normal pre-commit hooks run; no `--no-verify`.
2. `git merge --no-ff` into `main` and push.
3. **Tree equivalence:** the merge commit's tree equals the branch commit's tree (main has not moved from `54ffd9f0`); otherwise the merge needs re-review. Remote `refs/heads/main` must equal the published merge.
4. **Hook re-run:** re-run the repository pre-commit hook on the exact published tree (rc 0, gate builds with 0 warnings).
5. **Exact-SHA CI** on the pushed merge SHA: the full Pages workflow is green, including "Test C# tools" (77 / 164 / 17 / 174 expected), Test C# game (2464 expected) and the static-deployment check.
6. **Pages artifact** built from that SHA, and the **deployment** reports success for that SHA; the site serves `/`, `/index.html`, `/_framework/dotnet.js` and `/404.html`.

## Expected changes after approval (carried receipts, not part of the approved tree)

- `sprint-status.yaml`: `9a-c-lamp-detector-conversion`, `9a-36` and `9a-37` set to `done` at publication. `9r-9` and `9r-16` stay backlog (Chrome ACs).
- The `TODO.md` brief, and the spec's Current state and its SnapshotApproval → publication checkbox.
- The Story 6.8 and Story 6.5e (9R.16 part) addenda (native only, browser pending).
- The terminal publication-verification section of this record.
