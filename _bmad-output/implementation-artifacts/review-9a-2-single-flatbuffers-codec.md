# Review: Story 9A.2, Single FlatBuffers Codec

Reviewer: Murdoch-22 (independent; no deliverable edited). Round 1. Date: 21 Oct 2026.

## Verdict: **Pass** (scoped), no blocking finding

- Every AC of Story 9A.2 holds on candidate `0519cedb` (code `052d1134`, suite fix `49a88043`, over main `761f7ddc`).
- Every Deletes-list item, and every deletion she claims, is absent at HEAD. 17 of my 21 mutants are killed. The 4 survivors are re-adds that the head does not contain, so they are filed, not blocking (F17-5).
- No player-visible regression. Save/Load passes in every suite of the Full run. The timing counters are unchanged per suite. The only changed refusal text is the intended one (D3).
- AC6 is satisfied under the coordinator's ruling. `49a88043` touches only `tools/e2e/cat-020.test.ts` plus evidence files; it changes no product file.
- Nine findings are filed to **Story F.17** as F17-5 to F17-13 (below). The 9.2 closing addendum is appended to [review-9-2-pulse-counter.md](review-9-2-pulse-counter.md).

Evidence: [evidence-9a-2-review/](evidence-9a-2-review/).

## Candidate and identities

| Item | Identity |
| --- | --- |
| Candidate worktree | `.claude/worktrees/agent-a1891c8929744c470`, head `0519cedb3c103fa7ed4eb504e3106c2451042984` |
| Code commit | `052d11345c5d04de38b8d6ea48cdfea7262d0575`, parent `761f7ddc` (main included: `git merge-base --is-ancestor 761f7ddc 052d1134` true) |
| Suite fix | `49a880438bab5110f8599a68ecfcf7cf1a53b1a7`. `git show --stat`: `tools/e2e/cat-020.test.ts` (2+, 1-) and 47 files under `_bmad-output/.../evidence-9a-2/e2e-full-052d1134/`. `git diff --stat 052d1134 0519cedb -- . ':(exclude)_bmad-output'` lists only `tools/e2e/cat-020.test.ts`. No product file is touched, so the AC6 ruling applies |
| Clean Tier 1 copy | `.claude/worktrees/murdoch22-9a2`, detached at `0519cedb`, fresh; status 0 lines at each batch boundary and at the end (`tier1/identity.txt`) |
| Mutant copy | `.claude/worktrees/murdoch22-mut`, detached at `0519cedb`, restored by `git reset --hard` + `git clean -fd` after each mutant; final status 0 lines |
| Identity copy | `.claude/worktrees/murdoch22-id`, detached at `052d1134` and `49a88043` (digests), then a fresh publish of `052d1134` |
| Probe copy | `.claude/worktrees/murdoch22-probe`, at `0519cedb` plus my probe file and its Compile Include (review-only) |

## Acceptance criteria

| AC | What I checked | Result |
| --- | --- | --- |
| AC1. The remaining callers encode and decode through `WorkshopWireFlatBuffers` only | The diffs of `Program.Schedule.cs`, `BrowserWorkshopClient.Schedule.cs`, Animation `Program.cs`, `WorkshopTrace.cs` (`EncodeCommand`) and `WorkshopSaveCodec.cs` (`ByteLength` deleted; one `CCWS` size-prefixed root). `Program.ReadOutput`/`EncodeResponse`'s legacy branch are gone (`identity/deletions-rg.txt`). Reads publish `Publish(read, readIdentity24, poseSlot, …)`; `worker.js` copies the C#-composed slot (`writePoseRing`). Mutant R1 (a re-added `WorkshopWire` class) is killed | Pass |
| AC2. `tools/e2e/workshop-wire.ts` decodes FlatBuffers only | `openFrame` (`:2168-2185`) checks the size prefix and the identifier, and every decode goes through generated accessors. No `DataView` layout read remains except the size word. Her M17 (unframed TS read) is red, 19 of 24 (`evidence-9a-2/mutant-ts-ac2.txt`) | Pass |
| AC3. Truncated, unknown-tag or trailing-byte buffers are a typed fault naming the reason, never retried (unit negative control for commands, responses and saves) | `WireFaultTests`: `DamagedBufferIsATypedFaultNamingItsReason` (4 roots × truncated, trailing byte, wrong identifier, corrupt root, each with an undamaged control), `AnotherRootIsAnUnknownIdentifier`, `ShortBufferIsTruncated`, `RejectedInputIsNeverRetried` (executor count 0, then 1 for the control). Killed: M1 (verifier skipped, 13 red), M2 (trailing check dropped, 10 red), R2 (router `catch (ArgumentException)` retry), R3 (router `catch (WireFaultException)` frame-repair retry, 2 red), R8 (re-framing retry of an unframed save, 10 red). The frozen I/O matrix defines "unknown tag" as the wrong root identifier, which is tested. An undefined command kind is an untyped `ArgumentException` (F17-6) | Pass |
| AC4 (epic). Saves are structurally pre-validated before domain construction; a violation is a typed `WorkshopSaveRejectedException` with a closed reason, shown to the player, and the bench is kept; domain guards stay faults | `WorkshopSavePrevalidation.Check` runs on the verified record before `Construct`. `EveryViolationIsItsTypedRefusal` (31 rows) plus `EveryReasonIsCovered` cover every reason. `FuzzedSavesAreRefusedOrDecodedAndNothingElse` (every truncation, every bit flip, oversized, prefixed, 2,000 random). `DuplicateKeyDefectAfterPreValidationIsAFault` shows a post-validation defect is an `ArgumentException`, not a rejection. `PreValidationAdmitsExactlyWhatTheDomainAdmits` / `IntegerLanesMatchTheDomain` give parity. Killed: R12 (codec catch-all, 3 red), R13 (catch-all at the UI Load, killed by `WorkshopUiFailureTests.EveryUiCatch…`), M4 (refusal no longer a UI rejection), M5 (counter range widened by one, 3 red). Chrome: engine-core-2b1 test 2 (truncated, trailing byte, unframed, foreign identifier; bench kept; intact save loads) passed in the Full run (F17-9) | Pass |
| No format versions; a save failing validation is refused, bench kept, no migration | `rg -i version` over the schema, codec, save codec, prevalidation and messages: 0 hits. `WorkshopWireVersion` appears only in the negative assertion `WorkshopFormatTests.cs:27`. R8 (a migration-style retry) is killed | Pass |
| AC5. `rg "WorkshopWire\."` returns 0 hits outside the FlatBuffers codec | The literal command returns 3 hits outside `_bmad-output`, all the path string `WorkshopWire.cs`: the re-add ratchet (`WireFaultTests.cs:284`), `docs/work-orders/P0-004/ownership/part-023.json:107` (closed work order) and `docs/coverage/engine/capabilities-01.json:9893`. There is no code reference (`identity/ac5-rg.txt`). The coverage hit is in an inventory that already names 407 of its 446 paths as absent files (31 under `engine`/`ui`), and its audit exits 1 at HEAD (`probes/coverage-dangling-paths.txt`, `probes/coverage-audit-head.txt`). So it is frozen in practice and not a current input this story left stale. Wording filed (F17-7) | Pass (intent) |
| AC6. Build 0 warnings, `dotnet test` 100%, every e2e suite 100% in one Chrome run against the production preview, including Save/Load | Tier 1 reproduced (below). Her Full run on `052d1134` covers exactly the 44 `tools/e2e/*.test.ts` files of that commit (`comm` of the file list against the summary is empty). It passed 43/44; CAT-020 tests 1 and 5 failed `expected: 3, actual: 0`, because the suite read the deleted f32 threshold. The targeted run on `49a88043` passed CAT-020 5/5 and the smoke pair. Per the coordinator's ruling, the Full run plus that rerun satisfies AC6 | Pass (by ruling) |

## Deletions (Deletes list and her claims)

Every item was checked absent at HEAD (`identity/deletions-rg.txt`, excluding `_bmad-output` and the frozen `docs/coverage` and `docs/work-orders` snapshots), and re-added as a mutant where a re-add is expressible (`mutants/`).

| Deleted item | Absent at HEAD | Re-add mutant |
| --- | --- | --- |
| `engine/gpu/WorkshopWire.cs` (command, response, construction and decode-per-id arms) | `git ls-files` empty; type absent | R1 killed (`NoSecondCodecOrScriptParserIsReAdded`) |
| The 4 section codecs (`WorkshopActivationWire`, `WorkshopTimerWire`, `WorkshopContactWorkWire`, `WorkshopResponseCodec`) | 0 code hits; 2 stale doc mentions (F17-8) | Covered by the same ratchet's type list (as R1) |
| Router `catch { WorkshopWire.Decode* }` fallback | `WorkshopCommandRouter.cs` decodes once | R2 and R3 killed |
| Client `ReceiveRead` fallback | `BrowserWorkshopClient.cs:271` decodes once | The `ArgumentException` form is ratcheted; a swallowing `catch (WireFaultException) { return; }` (R4) **survives** (F17-5) |
| `responseAbi` / `ResponseAbi` / `CommandAbi` | 0 hits outside the ratchet | Script re-adds are ratcheted. A C# `[JSExport] ResponseAbi()` (R7) **survives** (F17-5) |
| `extractReadIdentity` (both copies) | 0 hits | R5 killed (ratchet) |
| The `sequence()` vtable parse and the 3 `READ_BODIES_OFFSET`/`BODY_WIRE_*` constants | 0 hits in `wwwroot` | R6 killed (ratchet) |
| Save magic | Schema field gone; `SAVE_MAGIC` gone from the driver; `0x53574343` remains only as the negative fixture `WorkshopFormatTests.cs:165` | R8 (accept an unframed former save) killed |
| `WholeCount`, `DefaultTargetCount`, `inst.Threshold == 0 ? … : (int)Math.Round(…)` | 0 hits; `target_count: uint32` (`schemas/workshop_wire.fbs:260`) | R9 (missing → 3) killed, 3 red; R10 (threshold coercion) killed by `ACounterThresholdIsNotItsTarget` |
| Construction blob (`WorkshopCommand.construction: [uint8]`) | Not in the schema; no `CreateConstructionVector`/`GetConstructionArray` | Not expressible without regenerating; rg only |
| Session `: default` decode fallbacks | `RequiredSession` throws `MissingField` | R11 killed (`MessageWithoutSessionIsAMissingField`) |
| Save catch-alls (`IsInvalidStoredValue`, `IsMalformedRecord`) | 0 hits | R12 and R13 killed |
| `PrepareBytes = 80 + ResponseBytes` | Replaced by `PrepareBytes(endpointBytes)`, sized from the endpoint | n/a |
| The 3 `Owned` rows in `WorkerAbiTests` | Edited as the spec says | n/a |

Further mutants from the spec's mutation plan: M3 (motion not encoded) is killed with 30 red; M6 (worker copies only the slot header) is killed by the node harness (87/88). R14 (a swallowing catch on the Animation worker's endpoint decode) and R15 (a retry on the client's endpoint decode) **survive** (F17-5).

## Player-visible regressions: none

- **Save/Load.** Every Save/Load suite ran in the identity-checked Full run, and every one except CAT-020 passed. That includes cat-062b (the MissingReceiver lesson save), engine-core-2a3 tests 4 and 5, arch-9a-catalogue, pkg-1, cat-051, cat-052 and engine-core-2b1 test 2 (the new refusal). CAT-020 passed 5/5 in the rerun, test 5 being Save / reload / Load. Her `targeted-1` caught and fixed an over-strict goal-reference refusal before the commit (`EveryAuthoredLessonSaveLoadsIncludingAMissingGoalPart` pins it).
- **Timing counters.** Per suite, they equal 9A.14's Full run `20261021T040922Z-28185`:
  - clock-master: closeFindings 1, unresponsive 1, recal 4, renewedLate 1, debtFaults 1;
  - arch-9a-catalogue: wideOffset 1.
  - The run-total `wideOffset` fell from 2 to 1.
  - The only suite whose duration moved by more than 25% is engine-core-2b1 (8,822 to 14,062 ms), which gained the new test.
  - Wall time: 2,424 s, then 2,460 s.
- **Refusal messages.** A refused stored save now reads "This save cannot be loaded: <reason>" (D3), on the rejection channel (`WorkshopUiFailure.IsRejection`), not as "Unexpected fault". `WorkshopEditRejection.UnsupportedSave` and its "Unsupported construction save." text are gone from C#. The JS `loadConstruction` width reject (`workshop-client.js:599`) is unchanged from base.

## Tier 1 (fresh copy `murdoch22-9a2` at `0519cedb`)

| Check | Result | Raw log |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx` (Debug, then Release) | 0 warnings, 0 errors, both | `tier1/build.log`, `tier1/release-build.log` |
| `dotnet test CuriousContraptions.slnx` | 2244/2244 (1892 + 164 + 94 + 77 + 17), equal to her count | `tier1/test.log` |
| `node --experimental-vm-modules --test tools/*.test.mjs` | 88/88 | `tier1/node.log` |
| LifecycleContract (Release) | Count 510, Failures 0 | `tier1/lifecycle.log` |
| WireContract (Release) | Count 108, Failures 0 | `tier1/wire.log` |
| Ownership `--oracles .` | The first run exited 1 (missing web restore). After `dotnet restore` of the web project: exit 0, Positive, 22/22 attacks rejected; line 1 SHA-256 `b4c56fac…` is byte-identical to hers | `tier1/ownership-oracles.log`, `tier1/web-restore.log` |

| C# static gate (`CSHARP_GATE_STAGED_HOOK=1 sh .githooks/pre-commit`) | exit 0; three builds with 0 warnings; 123/123 | `tier1/pre-commit-hook.log` |
| `anvil check --since 761f7ddc` | 13 changed non-C# files checked; **1 warning** (GS-001, F17-13); exit 0 | `tier1/anvil-check.log` |

Timestamps: 07:32:59Z to 08:01:29Z (`tier1/identity.txt`). Status was 0 lines at the start and end of each batch, and again after the later `rg` and coverage-audit probes (the last line of `tier1/identity.txt`).

**Tests removed by the story.** 19 test methods of the base are gone (`probes/removed-test-methods.txt`). Each one exercised the deleted binary codec, its section codecs, the codec sniffing or the version word. Their FlatBuffers-side assertions survive in `WireFaultTests`, `WorkshopWireDecodeBoundaryTests` (renamed `…AsTypedFaults`) and `WorkshopSaveRejectionTests`. The domain half of `MalformedTimerRead…` stays as `MalformedTimerReadRejectsWithoutChangingCommittedState`. The response decoder still rejects undefined section phases through the record guards (`ActivationLatch`, `CaptureLatch`, `ActivationTimerState`, `ActivationMemory` call `Enum.IsDefined`, and `ValidatePhysicalRead` is unchanged from base). One fixture narrowed (F17-10).

## Reuse of her Chrome runs

| Run | Reproduced identity |
| --- | --- |
| Full `20261021T063928Z-88501`, 06:39:28Z to 07:20:29Z | `sourceHead 052d1134`; status digest `e3b0c442…` (empty, clean); `suiteSourceDigest 2ba66e40…` and `runnerSourceDigest ee0dbe73…`, both reproduced from my clean `052d1134` copy (`identity/source-digests.txt`, script `identity/digests.sh.txt`); start and end digests equal |
| Targeted `20261021T072341Z-44668`, 07:23:41Z to 07:26:52Z | `sourceHead 49a88043`, clean; `suiteSourceDigest 3205c172…` reproduced from my clean `49a88043` copy; bundle `287dced1…` equals her current `AppBundle` (`identity/manifest-her-appbundle-287dced1.tsv`) |

**Bundle identity.** The two runs have different bundle identities (`574d3e37…` and `287dced1…`) over identical product source. My fresh publish of `052d1134` gives a third, `3d7bdd99…` (`identity/publish-052d1134.txt`).
- **Across commits (shown).** I compared my `052d1134` manifest with her current `49a88043` `AppBundle` (`identity/manifest-diff-paths.txt`). They have the same 306 paths; only the compiled assemblies, their `.br`/`.gz`, `dotnet.boot.js` and `godot.pck` differ. Each assembly embeds its commit: `strings` shows `1.0.0+052d1134…` in mine and `1.0.0+49a88043…` in hers.
- **Between two publishes of the same commit (cause not isolated).** Mine (`3d7bdd99…`) and her Full run's (`574d3e37…`) differ, but that bundle no longer exists on disk, so I could not compare manifests. It may be the worktree path or a non-deterministic export.
- Reuse therefore rests on the reproduced source, status and suite identities, as the ruling does (F17-11, informational).

Her raw logs that this record relies on are copied byte for byte into [evidence-9a-2-review/reused/](evidence-9a-2-review/reused/) (`SHA256SUMS.txt`): both CAT-020 runs, the Full run's summary and engine-core-2b1 TAP, and `mutant-ts-ac2.txt`. Other `evidence-9a-2/…` citations point into her candidate worktree and resolve in main once the candidate merges.

## Findings

**Blocking:** none.

**Filed to Story F.17, "Single Codec Follow-ups" (none blocking):**
- **F17-5. Four decode-site re-adds survive `dotnet test`.** The ratchet's catch scan covers only the router, the codec, the save codec and prevalidation, plus one `catch (ArgumentException)` string in `BrowserWorkshopClient.cs`. Survivors:
  - R4: `catch (WireFaultException) { return; }` around `ReceiveRead`'s decode (a refused read silently dropped);
  - R14: the same catch around the Animation worker's endpoint decode;
  - R15: a retry of the client endpoint decode;
  - R7: a re-added C# `[JSExport] ResponseAbi()`.
  - None is at HEAD. Suggest extending the catch scan to every `WorkshopWireFlatBuffers.Decode*` call site, and adding `ResponseAbi`/`CommandAbi` to the C# symbol ratchet.
- **F17-6. A verified command with an undefined kind is an untyped refusal.** Probe P1 (`probes/P1-run.txt`): kind 99 gives `ArgumentException: Unsupported Workshop command shape.`, not a `WireFaultException` naming a reason. Likewise a counter target of 10 on the command path is the domain's `ArgumentException`, where the spec's I/O matrix row says `WireFault(OutOfRange)`; the save path does give `OutOfRange`. The epic AC ("rejected") holds, and the matrix defines "unknown tag" as the root identifier, which is tested.
- **F17-7. AC5 wording against history.** The literal `rg "WorkshopWire\."` returns 3 path-string hits (above). Her proof's stated reason for excluding `docs/coverage`, that "no tool reads it", is inaccurate: `engine-capabilities.json` lists the shard and `audit-capabilities` reads it. It is frozen in practice (407 of 446 paths absent), so the AC text should exclude history, or the inventory should be refreshed at its qualification gate.
- **F17-8. Stale current docs.**
  - `docs/engine-contracts.md:203` still says an inadmissible slot "is refused as `UnsupportedSave`". That enum member is deleted, and the refusal is now `WorkshopSaveRejectedException`.
  - `docs/planning/elements/CAT-022-delay.md:38` and `CAT-063-switch.md:38` name the deleted `WorkshopTimerWire.cs` and `WorkshopActivationWire.cs`.
- **F17-9. The Chrome refusal proof covers frame faults only.** engine-core-2b1 test 2 ("A save that fails structural pre-validation is refused…") damages the frame: truncated, trailing, unframed, foreign identifier. No content reason (for example a counter target of 10) is proven in Chrome; those are native (`TheRefusalIsAPlayerRejectionNotAFault`, `EveryViolationIsItsTypedRefusal`). The test also reads the `CCGPU_UI_REJECTION` line rather than the status label, which shows the same `error.Message` via `WorkshopUiFailure.Report`.
- **F17-10. The response fuzz fixture narrowed.** `WorkshopWireDecodeBoundaryTests.PhysicalRead` used to carry contact-work and electrical sections. It now reuses `WireFaultTests.PhysicalRead(3)`, which has neither, so the response fuzz no longer reaches those tables. Their round trip is still covered at `WorkshopWireFlatBuffersTests.cs:333`.
- **F17-11. Bundle identity is not reproducible (informational).** It differs across commits, because each assembly embeds `1.0.0+<commit>` (shown). It also differed between two publishes of the same commit `052d1134` (cause not isolated). Reviewers must anchor reuse on source identities (above).
- **F17-13. Tier 1 `anvil check` reports 1 warning, and her Tier 1 evidence has no anvil log.** `anvil check --since 761f7ddc` on the clean copy gives GS-001 at `tools/e2e/workshop-driver.ts:985`: `return rejections[0]!;` in the new `loadRefused`, added in `052d1134` (`tier1/anvil-check.log`). The preceding length check makes it safe at runtime. It is a Tier 1 hygiene miss, not an AC breach (AC6 names build, test and e2e only).
- **F17-12. A hand list in pre-validation.** `WorkshopSavePrevalidation.Decodable` repeats the decoder's 22 element arms and its per-kind `switch` on `PartId.Value`. A new decode arm needs a second edit, and a forgotten one refuses saves of that element as `UnknownElement`. It is caught only if the parity test's `EveryKind` is updated. This belongs with the decode-arm owners (Stories 9A.16 and 9A.27–9A.43) and Story 9A.45a.

Her own filings F17-1 to F17-4 are re-observed and stay as she filed them.

## Process

- I edited no deliverable. Mutants ran only in `murdoch22-mut`; the scripts are `mutants/run-mutants.sh.txt` and `mutants/run-mutants-2.sh.txt`, with a per-mutant log and `mutants/summary.txt`. Every mutant built with `build_exit=0` and 0 warnings, so no result is from a stale binary. The final status was 0 lines after each batch.
- The probe source is kept as `probes/Murdoch22Probes.cs.txt`.
- `anvil_validate_write` ran before writing this record and before appending the 9.2 addendum (allow both times). The later corrections to both were made with Edit and were not re-validated. It was not run on the review-only evidence scripts and the probe file, and the evidence logs are redirected command output.
- No full suite was run. Both Chrome runs are hers, reused after the identity checks above. My only extra runtime was a production publish of `052d1134`, to examine the bundle identity.
- Closure: per the reopened-record table, this review appends the 9.2 addendum (retro-verification #13, the Counter decode default). Story 9.2 still closes with C-Memory, so `9-2-pulse-counter-event-accumulator` stays `in-progress`. Her `sprint-status.yaml` edit touches only `9a-2` and `f-17`.
