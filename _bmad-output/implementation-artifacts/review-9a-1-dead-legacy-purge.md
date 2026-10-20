# Review: Story 9A.1 Dead Legacy Purge, with Story F.10 merged in (Murdoch-16)

## Round 2 (final): Pass (scoped)

- **Candidate:** `b71d32fd`, whose code is `57850c85`. `b71d32fd` adds only `_bmad-output` evidence (`git diff --name-only 57850c85 b71d32fd` shows no other path). Main `7264ec5a` is merged into it (`0ee1ff4a`), and the worktree status is clean.
- **B-1 is resolved.** Three test additions:
  - `WorkshopTraceTests.ATraceRecordIsAdmittedByItsStructureAlone` covers six defects: truncated, extended, extra body, unknown cadence, unknown physical profile, and header padding.
  - `WorkshopActivationAnimationTests.AnAnimationChannelRecordOfAnyOtherLengthRejects` covers one byte short and one byte long.
  - The Node test "a master peer whose leading padding holds …" covers bytes 0 and 3.
- **The five mutants re-run on `b71d32fd`** (`evidence-9a-1-review/round2/mutants/`) are all killed:
  - MD: 2 Node tests fail.
  - ME: 3 cases fail (Extended, UnknownCadence, UnknownPhysicalProfile).
  - MEF2: 5 cases fail (Truncated, ExtraBody, Extended, and both animation lengths).
  - MF: the animation `change: 1` case fails.
  - MA: `NoProductionSourceNamesAFormatVersion` reports `STATE_VERSION` and `RING_VERSION`.
  
  The tree was reverted clean after each one.
- **F13-1 is resolved.** The ratchet pattern is now `\w*(?:Version|VERSION|(?<![A-Za-z0-9])version)\w*`, which is case-sensitive, so "conversion" doesn't match. It is green on the tree, and MA is killed.
- **Merge items.**
  - M-1 to M-4 are applied:
    - both `WorkshopFormatTests` facts are present;
    - no open "B7, B8" or "Format versions" rows remain in the isolation doc's §4.1;
    - `engine-contracts.md` keeps the 9R.7 `UnsupportedSave` wording and adds the 9A.1 sentence, with no "still carries" pointer;
    - `gpu-f32-physics.md` takes the candidate's paragraph.
  - M-5 is mostly applied: F.12 is present and `sprint-status` has the `review` statuses plus `f-11` and `f-12`. The leftover is filed below as F13-7.
  - No conflict markers remain.
- **C-1 is resolved.** `scratch/` is absent from the main checkout. The owner approved the deletion and the coordinator carried it out.
- **`policyVersion`:** `workshop-isolation*.js` is unchanged since round 1, so the round-1 proof stands: the waiting-worker refusal and the reply-shape refusal were each killed by their mutants (MJ, MK). The Node isolation tests pass inside 88/88.
- **Tier 1 on `b71d32fd`** (`evidence-9a-1-review/round2/`):
  - Build: Debug and Release both 0 warnings, 0 errors.
  - `dotnet test CuriousContraptions.slnx`: 17, 78, 156 and 1889, all passing.
  - Node: 88/88.
  - Contracts: WireContract 108/0; LifecycleContract exit 0; Ownership oracles exit 0 (22/22 attacks rejected).
  - `anvil check` over the story's changed code files: 0 warnings. That run excludes the long `engine-contracts.md` line (F13-5).
- **Chrome.** I ran nothing new.
  - The implementer's smoke pair on `57850c85` matches its identity (`sourceHead` `57850c85…`, clean status digest): Selected mode, `engine-core-2a1` 5/5 and `engine-core-2b1` 1/1, verdict Pass.
  - Her Full run on `844981c9` (45/45 files, 239/239 tests) still covers the runtime code, because round 2 changed only tests and docs.
  - I stopped my detached smoke-pair retry loop from round 1 unrun.
- **Filed to F.13, in addition to the round-1 items F13-2 to F13-6:**
  - **F13-7.** The F.10 status line in `epics.md` still reads "Backlog. Merged into Story 9A.1"; it should match the Review/Done status in `sprint-status`.
  - **F13-8.** The JS master-peer bootstrap has no wrong-length or wrong-profile Node control. Both checks predate this story, and the padding check this story changed is now covered.
- **Verdict: Pass (scoped to Story 9A.1 with F.10).** There is no unresolved AC breach and no player-visible regression. This is not a global or runtime qualification.

---

## Round 1

- **Reviewer:** Murdoch-16, independent adversarial reviewer. I edited no deliverable.
- **Date:** 21 Oct 2026.
- **Candidate:** worktree `agent-a04bd550965f8b651`, HEAD `844981c9` (`a8ceeaae` before it), on base 9R.7 `120bf876` plus 9A.0 `9e4d6d86`.
- **Merged review copy:** worktree `murdoch16-9a1`, commit `37e7ab6d` = `844981c9` merged with main `7264ec5a`. I resolved the conflicts hunk by hunk for review only (see M-1 to M-5).
- **Evidence:** `_bmad-output/implementation-artifacts/evidence-9a-1-review/`.

## Verdict: Fail (round 1)

There is one blocking finding: **B-1**. It breaches F.10 AC2 and needs a test-only fix.

There is also one open condition owned by the coordinator, **C-1**: the main checkout's `scratch/` must be deleted. The merge items (M-1 to M-5) must be applied when the candidate merges with main. Everything else is filed to **Story F.13 "Legacy Purge Follow-ups"**.

There is no player-visible regression. The implementer's Full run passed on the exact candidate, and Tier 1 passes on the merged copy.

## Blocking

### B-1 (F.10 AC2): three changed decoders have no negative control for some of truncated, unknown-tag and out-of-range input

F.10 AC2 requires "a negative control per decoder (truncated, unknown-tag and out-of-range input)". The code does reject this input, but nothing proves it: each of the mutants below weakens a check and still passes every test.

| Decoder | Truncated or wrong length | Unknown tag | Out of range | Mutant proof |
| --- | --- | --- | --- | --- |
| JS master-peer bootstrap, `workshop-client.js:259-267` (its leading-padding check is new in this story) | none | none | none | **MD**: deleting `declared.getUint32(0, true) !== 0` survives all 86 Node tests and all 1881 C# tests |
| `WorkshopTraceRecord.ReadProfile` | none (only the leading word is tested) | none (undefined cadence or physical profile) | none (counts) | **ME**: weakening `!=` to `<` and dropping `p.Validate()` survives. **MEF2**: deleting both length guards outright survives too (a short input would then throw `ArgumentOutOfRangeException`, which an exact `Assert.Throws<ArgumentException>` control would catch) |
| `WorkshopAnimationWire.ReadControl` and `Read` (shared `Identity` length check) | none | present (property 99, impulse curve 9) | present | **MF**: weakening `!=` to `<` survives. **MEF2**: deleting the guard outright survives too |

The block rests on the JS peer and the trace record, which fail AC2 under any reading. The animation channel already has unknown-tag and out-of-range controls, so it is in the fix list for the missing length control but is not an independent reason to block.

**Required fix (tests only):**
- Add a Node control to the client lifecycle harness that delivers a master peer with:
  - bytes 0..3 non-zero;
  - a wrong length;
  - a wrong profile.
  
  Each must reject with "Invalid/Missing authoritative master bootstrap".
- Add trace-record controls for a truncated record, a record one byte long, an undefined cadence and a count mismatch.
- Add animation control and output controls for a record one byte short and one byte long.
- Re-run MD, ME and MF to prove each is now killed.

The other decoders already meet the matrix:
- `CanonicalBody.Decode`: `CanonicalBodyTests:86`, the Status and LocalBeyondRange cases.
- The physics header: `WorkshopWireTests:983, 1043, 1103`.
- `DecodePeer`: `WorkshopWireTests:241` and the profile and role checks.
- The memory wire: `WorkshopWireTests:1411-1474`.
- The schedule wire: `WorkshopWireTests:259-278`.
- `attachClockRing`: the length and type cases.

## Open condition (coordinator)

**C-1. `scratch/` remains in the main checkout.** It holds 79 untracked entries (3.7 MB, including Godot `.import` files dated 17–18 Oct), and it is not in `.gitignore`. The candidate worktree never had it, and `git ls-files scratch` is empty, so no commit can delete it. Until the coordinator confirms that no agent is using it and deletes it (the spec's Task 1 hold point), 9A.1 AC1 is not met for that path. The isolation authority's closed row ("B8 (directory) … Gone from the tree") is inaccurate until then.

## Merge items (apply when merging with main `7264ec5a`; five text conflicts)

- **M-1. `CuriousContraptions.tests/WorkshopFormatTests.cs`.** Keep both facts: the candidate's `NoProductionSourceNamesAFormatVersion` and main's `TheVersionlessRecordsDeclareExactlyTheirFields`. The merged copy then passes 1881/1881.
- **M-2. `docs/architecture/element-package-isolation.md`.**
  - Drop main's two re-added open rows: "B7, B8 (remainder) …" and "Format versions … | 9A.1 (Story F.10 merged in)". If either survives, F.10 AC3 is breached.
  - Keep the candidate's closed rows. They already cover 9R.7.
  - Keep main's section header "Current status (at the merge of main …)".
- **M-3. `docs/engine-contracts.md`.**
  - In the codec paragraph, do not keep main's sentence "the version fields the code still carries are listed with their owners in the [isolation authority's Current status]". Nothing remains to list.
  - In the save paragraph, keep main's 9R.7 wording (`UnsupportedSave` …) and append the candidate's 9A.1 sentence.
- **M-4. `docs/gpu-f32-physics.md`.** Take the candidate's paragraph. Main's paragraph still says "The version enums that remain in the code are listed …".
- **M-5. Planning files.**
  - In `epics.md`, keep main's F.12 heading. Main's F.10 status line ("Backlog. Merged into Story 9A.1") must become the candidate's Review status.
  - In `sprint-status.yaml`, keep main's `f-11` and `f-12` rows and the candidate's `review` statuses.

## Filed to Story F.13 (non-blocking)

- **F13-1. The ratchet regex is case-sensitive.** `NoProductionSourceNamesAFormatVersion` matches `Version` or a lower-case `version`.
  - Mutant **MA** re-adds the exact deleted symbol `const STATE_VERSION:u32=13u;` in the WGSL string in `PhysicsGpuAbi.cs`, plus `export const RING_VERSION = 3;` in `clock-ring.js`. It survives 1881/1881 C# and 46/46 Node tests.
  - A functional re-add is still stopped by the padding rejects (MC, and the implementer's M3–M8). The scan also covers only the top directory of `SourceProjects` and the `wwwroot` folders.
  - Do not use `RegexOptions.IgnoreCase`: "conversion" in `WorkshopWall.cs`, `WorkshopDelay.cs` and `WorkshopNativeClock.cs` would match at once.
  - Add a case-sensitive alternative instead: `\w*(?:Version|(?<![A-Za-z0-9])version)\w*|[A-Z0-9_]*VERSION[A-Z0-9_]*`. Over the scanned trees, `rg '[A-Z0-9_]*VERSION[A-Z0-9_]*'` returns 0 hits on `37e7ab6d`, so the ratchet stays green and MA is killed.
- **F13-2. The committed `parts/elements.bundle.json:2` still has `"Version": "1.0"`.**
  - `tools/BundleElements` no longer writes the field, but the generated file was not regenerated, and the ratchet doesn't scan it.
  - No runtime reader exists today; Story 9A.14 makes the bundle the sole source.
  - The closed row "the bundle … versions" overstates this until the file is regenerated (Story 9A.4 owns the bundle build). A trivial Low for round 2: reword the closed row to say the tool no longer writes the field. Regenerating the bundle stays here or in 9A.4, because it changes the e2e selection.
- **F13-3. The `sessionStorage` isolation marker changed shape** from 4 elements to 3. A tab that is mid-isolation-reload across this deploy gets one "Invalid isolation startup attempt" failure, and the next load is clean because `stopIsolation` removes the marker. This is Low.
- **F13-4. A re-added `export function writeEventRing` (dead JS export) fails no test.** `rg` is the only guard. It could be guarded with an exported-symbol allowlist for `worker.js`.
- **F13-5. A pre-existing doc line is too long to scan.** `docs/engine-contracts.md:208` (4784 bytes, the same on main) is longer than anvil's `max_line_bytes`, so `anvil check` reports "Secret scan could not prove these files clean" for that file. Every code file is clean (0 warnings).
- **F13-6. The Node harnesses need `--experimental-vm-modules`.** Without it, `node --test tools/*.test.mjs` fails 6/82 (`vm.SourceTextModule`). Tier 1 docs should state the flag.

## `policyVersion` (service worker) decision

`policyVersion` is removed, and a structural check proves the replacement:
- **Another deploy's worker.** A worker whose bytes differ installs as a waiting worker, because `updateViaCache: 'none'` makes the browser fetch the worker script on every update. That waiting worker is refused at `workshop-isolation.js:119/123`. The test "another deploy's changed isolation worker waiting behind the controller is refused" covers this, and mutant **MJ** (both waiting checks removed) is killed.
- **A controller answering in another shape is refused.** Mutant **MK** (the shape check removed) is killed. The worker answers only `{policyRequest: true}` from an eligible client; the implementer's M9 proves this.
- **A genuine old worker (`policyVersion = 1`) is silent** to `policyRequest: true` rather than answering in the wrong shape. That case can only arise behind a waiting worker, which is refused first, or else the 5 s deadline catches it.

The old number added no protection: an unchanged `1` accepted every deploy.

## Criteria and impact matrix

| Criterion | Result | Evidence |
| --- | --- | --- |
| 9A.1 AC1, deleted symbols `rg` 0 | Pass, except C-1 | See *9A.1 AC1 census* below |
| 9A.1 AC2, laws covered by named C# tests | Pass | `WorkshopPhysicsEngineTests` (21 facts or theories), `PrismaticConstraintTests` (14), `SoftConstraintTests` (6), `WorkshopSpringboardTests` (11), `ElectricalSupplyTests` (15), `WorkshopBumperTests` (23): all compiled, all inside 1881/1881 |
| 9A.1 AC3, `workshop-observation` and `workshop-pose-ring` pass | Pass | `tier1/node.log`, 86/86 |
| 9A.1 AC4, `ProbePart` behaviour proven through declared bindings | Pass | `BasketballResourceTests`: the three rejections, acceptance, and the real Delay part. **MG** (timer-phase rule removed) and **MH** (unsealed) are killed |
| 9A.1 AC5, 0 warnings, `dotnet test` 100%, full Chrome run 100% | Pass | `tier1/build.log` and `tier1/release-build.log` (0 warnings), `tier1/test-full.log` (17, 78, 156 and 1881, all passing); implementer Full run (below) |
| Seal plus the Godot generators (spec risk) | Pass | The implementer's run has `publish.log` exit 0 (`dotnet publish CuriousContraptions.web`); the commit hook's web build also exited 0 |
| F.10 AC1, deleted symbols `rg` 0, and no wire, ring, trace or record layout has a version field | Pass | See *F.10 AC1 census* below |
| F.10 AC2, structural rejection with a negative control per decoder | **Fail** | B-1 |
| F.10 AC3, isolation Current-status rows removed | Pass on the candidate; at risk at merge | M-2 |
| F.10 AC4, Tier 1 plus the clock, schedule and trace suites | Pass | Tier 1 below; clock-master, cat-017 and cat-023b passed in the Full run |
| Structural guard against re-added versions | Pass with a gap | Ratchet kills **MB** (bundle `Version`); padding rejects kill **MC**; uppercase gap F13-1 (**MA**) |
| Dead worker constants guard | Pass | **MI** (`const MOTION_GRAVITY = 60;` re-added) is killed by `WorkerAbiTests` |

### 9A.1 AC1 census

`rg` over the active tree, excluding `_bmad-output`, finds 0 hits for:
- `writeEventRing`, `ProbePart` and `WORKER_ABI_CANDIDATE`;
- `protected virtual void Build` and `Unmigrated legacy`;
- `EarlyLoss`, `AllocateElectrical`, `AbiOffsets` and `OperationAbi`.

The only other hits are closed-findings rows in the isolation authority, which I treat as history. `git ls-files scratch scratch_test.cs.uid` is empty.

### F.10 AC1 census

These symbols have 0 production hits:
- `WorkshopClockWire.Version`, `clockVersion` and `ClockRingVersion`;
- `WorkshopAnimationWire.Version`, `WorkshopTraceVersion` and `TraceTimingVersion`;
- `WorkshopScheduleVersion`, `PhysicsStateVersion`, `STATE_VERSION`, `BodyRecordVersion` and `HeaderVersionOffset`;
- `policyVersion` and `UnknownVersion`.

The remaining hits are the negative assertions in tests, the WireContract README history note and `docs/planning/delivered-slices.md:26`. I treat that delivery record as history, not current doc.

### Impacts checked

- **Writers no longer write a version word, so each must start from a cleared or fresh buffer.** All do:
  - the Animation worker's `Output` (`Clear()` per `Emit`);
  - `WorkshopAnimationWire.Control`, `WorkshopMemoryWire.Encode` and both `Encode` paths (`new byte[]`);
  - `CanonicalBody` (`data.Clear()`);
  - the schedule header (`destination.Clear()`);
  - the trace record and `PhysicsGpuAbi.Admission` (fresh arrays);
  - `EncodePeer` (fresh array).
- **The WGSL `STATE_VERSION` was declared and never used**, so removing it is safe.
- **Trace-line parsers.** No parser of `CCGPU_TRACE_BEGIN` or `CCGPU_TICK_TIMES_BEGIN` exists outside `WorkshopTraceTests`. `tools/e2e/workshop-driver.ts:428` only prefix-matches. The index shift is covered by the updated tests and by `workshop-console.test.mjs`.
- **`isolationEvidence()` dropped `policyVersion`.** No e2e consumer reads it (`rg`: 0).

## Runtime evidence

- **Implementer Full run, reused after identity checks.**
  - Command: `run --diff 120bf876`, Mode: Full (Tier 3 required).
  - Identity: `sourceHead` `844981c9327c83bc…` and `sourceStatusDigest` `e3b0c442…` (an empty status). I re-checked both against the worktree.
  - Bundle: `bundleIdentity` `d25d0594…`, the same at start and end.
  - Result: verdict **Pass**, 45/45 files, 239/239 tests. anim-1a, anim-1b, anim-1c, clock-master, cat-017, cat-023b, engine-core-2a1 and engine-core-2b1 all passed.
  - Summary copied to `evidence-9a-1-review/implementer-full-run-summary.txt`.
- **Merge-diff selection** (`select --diff 844981c9` on `37e7ab6d`): Mode Selected, the smoke pair `engine-core-2a1` and `engine-core-2b1` only, because the merge brings only main's two test files and docs (`tier1/select-merge-diff.txt`). My smoke-pair run was refused with `RunnerBusy`: another agent's `full` run (worktree `agent-a701795d…`) holds the lock. I started a detached retry loop on `37e7ab6d`. It takes the lock when it frees and writes to `evidence-9a-1-review/merge-run/` and `merge-run.out`. Until that run passes, the merged copy's runtime proof is **Incomplete**, not Pass, even if the coordinator overrules B-1. Both suites passed in the Full run on the unmerged candidate, and the merge changes no browser-served file.
- **Tier 1 on the merged copy `37e7ab6d`:**
  - `dotnet build CuriousContraptions.slnx`: 0 warnings, 0 errors. The Release build is also clean.
  - `dotnet test CuriousContraptions.slnx`: WorkerAotGuard 17/17, Coverage 78/78, E2eRunner 156/156, CuriousContraptions.tests 1881/1881.
  - `node --experimental-vm-modules --test tools/*.test.mjs`: 86/86.
  - WireContract: 108 oracles, 0 failures (one fewer than 109, as the README records). LifecycleContract: 510, 0 failures. Ownership `--oracles .`: exit 0, Positive true, 22/22 attacks rejected.
  - `anvil check`, run three ways:
    - `anvil check <the 44 changed code and doc files since 77db23eb>` exited 1: 0 warnings, but "Secret scan could not prove these files clean" for the pre-existing long line in `docs/engine-contracts.md` (F13-5; `tier1/anvil-check.log`).
    - The same list without that doc exited 0 with 0 warnings (`tier1/anvil-check-code.log`).
    - `anvil check --since 77db23eb` exited 0 with 0 warnings, but covers only 8 JS files (`tier1/anvil-check-since.log`).
    
    `--changed` alone has nothing to check on a clean committed tree, so I gave an explicit file list.
- **Mutants** (`evidence-9a-1-review/mutants/`; each was reverted, and the tree ends clean):

| Mutant | Change | Result |
| --- | --- | --- |
| MA | uppercase `STATE_VERSION` (WGSL) and `RING_VERSION` (`clock-ring.js`) re-added | survives (F13-1) |
| MB | `Version = "1.0"` re-added in `BundleElements` | killed by `NoProductionSourceNamesAFormatVersion` |
| MC | memory leading-padding check disabled | killed by `StoppedMemoryRequestsRejectUnknownEnumsReservedBytesAndInvalidIdentity` |
| MD | JS master-peer leading-padding check removed | **survives (B-1)** |
| ME | trace record over-length and undefined-enum checks weakened | **survives (B-1)** |
| MF | animation channel length check weakened | **survives (B-1)** |
| MEF2 | trace record and animation channel length guards deleted outright | **survives (B-1; no truncated control)** |
| MG | timer-phase binding rule removed | killed by `ATimerPhaseBindingRequiresTimerFeedback` |
| MH | `MachinePart` unsealed | killed by `TheMachinePartIsSealedAndHasNoConstructionHook` |
| MI | dead `worker.js` layout constant re-added | killed by `WorkerAbiTests` |
| MJ | waiting isolation worker accepted | killed (isolation test 8/9) |
| MK | any handshake reply shape accepted | killed (isolation test 8/9) |

## Round 2 scope

- Re-review the B-1 tests and re-run MD, ME and MF.
- Confirm that M-1 to M-5 are applied in the real merge.
- Run the merge-diff smoke pair once the runner lock is free.
- C-1 is the coordinator's action.
- No full run is to be repeated.
