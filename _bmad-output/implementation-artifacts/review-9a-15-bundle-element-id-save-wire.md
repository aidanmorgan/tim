# Review: Story 9A.15, Saves and Messages Carry the Bundle Element Id

**Reviewer:** Murdoch-23, independent. I edited no deliverable.
**Implementer:** Amelia-22.
**Round:** 1 of at most 2.
**Verdict: Pass** (scoped) on `d3f17d27`. Every AC is met and there is no player-visible regression:
- Save/Load still works.
- A refused load keeps the bench.
- The refusal reads "Load rejected; construction unchanged. This save cannot be loaded: Part <n> names the element 'kite', which this game does not have."

There are no blocking findings; F-1 to F-5 are filed below. This is not a global or runtime qualification.

## Identity

| Item | Value |
| --- | --- |
| Candidate | `d3f17d2789e14cbf6e98a17cfbd609f4f9fe3d27`, worktree `.claude/worktrees/agent-ae9d1607d961be684`, one commit on `0519cedb` |
| Main | `0a45e3b7`. It contains `0519cedb` through the merge `379f84ba`. `git diff --name-only 0519cedb 0a45e3b7` lists only `_bmad-output/` files, so no product source differs between the candidate's base and main |
| Merge note | `git merge-tree 0a45e3b7 d3f17d27` conflicts only in `epics.md` and `sprint-status.yaml`: adjacent status edits, which the coordinator resolves at merge (`identity/merge-tree-0a45e3b7-d3f17d27.txt`) |
| Clean Tier 1 copy | `.claude/worktrees/murdoch23-9a15`, detached at `d3f17d27`, status 0 lines at every batch boundary (`tier1/identity.txt`) |
| Mutant copy | `.claude/worktrees/murdoch23-9a15-mut`, detached at `d3f17d27`, reverted to 0 status lines after each mutant |
| Evidence | `evidence-9a-15-review/` in the main checkout, 256 KB, no file over 5 MB |

## Acceptance criteria

| AC | Evidence | Verdict |
| --- | --- | --- |
| AC1. Saved, sent to a worker or traced, each instance carries its typed `PartId` string, validated against the loaded bundle only at the serialisation boundary | **Save.** `WorkshopSaveCodec.Decode/DecodeStored(IReadOnlySet<PartId> elements, …)`, and `WorkshopSavePrevalidation.Element` refuses an id not in `elements`. The only production caller (`ui/Workshop.cs:898`) passes `World.Registry.Elements`, built atomically in `PartRegistry.Load` from the bundle's deliverable entries. Mutant R2b (a hard-coded list in place of `elements`) is killed by `ASaveIsValidatedAgainstTheLoadedBundle`. R8 (the registry set also admits Tennis/Balloon) is killed by two `PartRegistryTests`. **Message.** The command encoder writes `part_id` strings, pinned by `AWorkerMessageCarriesEachInstancesBundleElementIdString`. **Trace.** `WorkshopTrace.Begin` encodes its Construct command through the same `EncodeCommand`. **Worker boundary (ruling below)** | Pass |
| AC2. An unknown id is rejected atomically, with the id named in the error (negative control) | **Save.** `WorkshopSaveRejection.UnknownElement`, with "This save cannot be loaded: Part <n> names the element '<id>', which this game does not have." It is raised in pre-validation, before any domain construction or `SubmitConstruction`, so the bench is kept. Tests cover kite, tennis, balloon and stored base64. R5 (message drops the id) is killed by 6 tests. **Worker.** The new `WireFaultReason.UnknownElement` names the id, and the router refuses before its executor runs (`TheWorkerRouterRefusesAnUnknownElementBeforeItsExecutor`). R3 (silent empty generic instance, the `_ => 0` class) and R4 (the untyped `ArgumentException`) are each killed by 3 tests. **Chrome.** `engine-core-2b1` test 3 swaps `ball` for `kite` in a real save, loads through the menu, and asserts the typed refusal, the message, that no command is issued, that the bench is kept and that the intact save loads. **Inventory id.** Correct at head (probe P1) but untested by the suite (R6 survives; F-1) | Pass |
| AC3. `rg -w "ToNumericId\|FromNumericId"` returns 0 hits, and no FlatBuffers schema or save field carries a numeric part id | Code, schemas, tools, docs and tests: 0 hits. The literal repo-wide `rg` has hits in 10 files, all under `_bmad-output/` (`identity/ac3-literal-rg.txt`). By class: (a) the AC and Deletes text itself and F17-4 in `epics.md`, which makes 0 literally unreachable; (b) historical audits (`architecture-conformance-audit-2026-10-18.md`, `retro-verification-2026-10-18.md`); (c) earlier specs and reviews that record the deletion (`spec-9a-2`, `spec-9a-14`, `review-9a-14`, `spec-9a-15`); (d) raw evidence (`evidence-9-4/review3`, her `rg-proof.sh` and its output). My own mutant logs in the main checkout add more class-(d) hits, since R1 re-adds the method. None is a current input. The guard `NoNumericPartIdConversionRemains` kills R1. **Schema.** The only element-naming fields are `part_id: string` and `inventory_part: string`. R7 (`part_number: uint32`) is killed by `NoSchemaFieldCarriesANumericPartId`. The guard matches names only (F-4). The schema's numeric `kind` fields hold `ActivationNodeKind`, ten engine node behaviours (ContactSource … ContactLoadSource) shared across elements, not part ids | Pass |
| AC4. No format versions; a structurally invalid save is refused with a player-visible message; no migration | The schema is unchanged: no version field, no capacity. A refusal reaches the status line as "Load rejected; construction unchanged. " + the message (`WorkshopUiFailure.Report` returns `error.Message` for `WorkshopSaveRejectedException`). That text is readable and names the id. There is no migration path | Pass |
| AC5. Every suite that saves and loads passes | Native: all of them, in Tier 1. Chrome: every suite passes in her Full run, including engine-core-2b1 (tests 2 and 3, refused saves keep the bench), engine-core-2a3 and the element suites with Save/Load | Pass |
| AC6. Build 0 warnings, `dotnet test` 100%, every `tools/e2e/*.test.ts` 100% in one serial Chrome run against the production preview | Build: 0 warnings, Debug and Release. Test: 2258/2258 (Tier 1 below). e2e: her Full run on `d3f17d27` ran all 44 suites, 279/279, 0 failures. Its `Incomplete(Contended)` verdict was observation only, and her Clean rerun of the three contended SerialAlone suites resolves it (below) | Pass |
| Deletes | `PartId.ToNumericId`, including `_ => 0`, and `PartId.FromNumericId`: deleted, guarded (R1 killed). Numeric part-id schema/save fields: none existed after 9A.2, and that is now guarded (R7 killed). Beyond the epics list, the spec also deletes the hard-coded `Decodable` list (R2b killed), the untyped refusal (R4 killed) and the per-id generic arm (R9 is an equivalent mutant; not a finding, since it is not in the epics Deletes) | Pass |
| F16-4 (named refused id) | `TheUndeliveredPartsHaveNoPartIdConstantOrWireIdentity` refuses saves naming Tennis and Balloon against the real loaded registry, naming each id. R5 kills it | Closed |
| F17-4 (Coverage from the bundle) | `CurrentCatalogue.DeclaredPartIdentities` parses `Elements[].Metadata.Id` from `elements/elements.bundle.json`. R10 (back to `engine/PartId.cs`) is killed by 4 Coverage tests. It reads a build output (F-2) | Closed |

**Ruling on the worker boundary (AC1).** The worker loads no bundle. It validates an id against its decoder: the `MapInstance` record arms plus `WorkshopCanonicalColliders.Declared`, a hand-written switch. I rule this **not a breach**, for three reasons.
- The worker has one producer: the main thread, whose ids come from the registry that loaded the bundle, after the save boundary validated them.
- The two sets are proven equal in both directions:
  - `EveryLoadedElementIsKnownToTheWorkerDecode` proves that every bundle element is known to the worker;
  - my probe P3 proves the converse. The worker-known set (18 record-arm ids ∪ 9 `Declared` ids) is exactly the 22 deliverable bundle ids; `comm -3` is empty (`probes/P3-reverse-parity.txt`).
- The canonical collider table is Story 9A.16's to delete. Until then it is the worker's single collider source, not a fallback.

The converse is not a committed test (F-3).

## Tier 1 (clean copy at `d3f17d27`)

| Check | Result | Raw log |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx`, Debug and Release | 0 warnings, 0 errors, both | `tier1/build.log`, `tier1/release-build.log` |
| `dotnet test CuriousContraptions.slnx` | 2258/2258 (1906 + 164 + 94 + 77 + 17), equal to her count | `tier1/test.log` |
| `node --experimental-vm-modules --test tools/*.test.mjs`, file by file | 88/88, every file exit 0 | `tier1/node.log` |
| LifecycleContract (Release) | Count 510, Failures 0 | `tier1/lifecycle.log` |
| WireContract (Release) | Count 108, Failures 0. The JSON equals hers (`wire-contract.json`) | `tier1/wire.log` |
| Ownership `--oracles .` (after the web restore) | exit 0, Positive, 22/22 attacks rejected. Line 1 SHA-256 `b4c56fac…` is byte-identical to hers | `tier1/ownership-oracles.log` |
| `CSHARP_GATE_STAGED_HOOK=1 sh .githooks/pre-commit` | exit 0; 0 warnings; 123/123 | `tier1/pre-commit-hook.log` |
| `anvil check --since 0519cedb` | 1 changed checked file, no warnings, exit 0 | `tier1/anvil-check.log` |

Her own Tier 1 and targeted run were taken on the working tree before the commit (`sourceHead 0519cedb`, non-empty status digest). They are not identity proof for `d3f17d27`. The Tier 1 above is.

## Mutants (mine)

Each mutant ran the whole `CuriousContraptions.tests` project (R10: `Coverage.Tests`) on the mutant copy. Script: `mutants/run-mutant.sh.txt`. Each log holds the diff, the failing tests and the revert.

| Mutant | Result |
| --- | --- |
| R1 re-add `ToNumericId` with `_ => 0` | Killed (`NoNumericPartIdConversionRemains`) |
| R2a hard-coded `Decodable` list, `elements` unused | **Invalid.** The build broke on IDE0060, so no test ran |
| R2b R2a with `elements` still referenced | Killed (`ASaveIsValidatedAgainstTheLoadedBundle`) |
| R3 unknown worker id → empty generic instance | Killed (3 tests) |
| R4 unknown worker id → untyped `ArgumentException` | Killed (3 tests) |
| R5 save refusal does not name the id | Killed (6 tests) |
| R6 skip the inventory id check | **Survived** (F-1). Probe P1 kills it. Under the mutant the save is still refused, but as `WorkshopEditRejectedException` "Changed domino effect fixture or goal.", which does not name the id (`probes/P1-under-R6-mutant.log`) |
| R7 schema `part_number: uint32` | Killed (`NoSchemaFieldCarriesANumericPartId`) |
| R8 registry elements also admit Tennis/Balloon | Killed (2 `PartRegistryTests`) |
| R9 re-add the per-id generic arm | Survived: an equivalent mutant, outside the epics Deletes. Not a finding |
| R10 Coverage reads `engine/PartId.cs` | Killed (4 Coverage tests) |

**Probes** (`probes/Murdoch23Probes.cs.txt`, run on the mutant copy at head):
- P1: a Domino Effect lesson save whose inventory is `kite` is refused as `UnknownElement`, naming `'kite'`.
- P2 (control): the intact lesson loads.
- P3: reverse parity, above.
- P4: on a fresh detached copy with no solution build, `dotnet test tools/Coverage.Tests` fails 4 tests with "The element bundle 'elements/elements.bundle.json' is missing; build tools/BundleElements to generate it." (F-2).

## Reuse of her Full run and mutants

**Full run `20261021T075944Z-23703`** (read from its out directory `scratchpad/amelia22-9a15-full-d3f17d27/summary.{txt,json}`, 07:59:44Z to 08:51:19Z):
- `sourceHead` `d3f17d27`, `sourceStatusDigest` `e3b0c442…` (empty: clean).
- `suiteSourceDigest` `72741401…` (equal at start and end) and `runnerSourceDigest` `ee0dbe73…`. I reproduced both from my clean `d3f17d27` copy (`identity/source-digests-murdoch23.txt`, script `identity/digests.sh.txt`).
- `bundleIdentity` `b5348bb1…`, the same at start and end; served check passed at both.
- All 44 `tools/e2e/*.test.ts` suites ran: `comm -3` of the file list against the summary's suites is empty. 47 file records (clock-master, pkg-3 and arch-9a-catalogue each ran twice), all exit 0, 279/279 tests, 0 failures.
- `engine-core-2b1` passed 3/3, including test 3, "A save naming an element the loaded bundle does not declare is refused naming it, and the bench is kept". This is the Chrome proof of AC2.
- **Verdict `Incomplete(Contended)`.** The six SerialAlone records of clock-master, pkg-3 and arch-9a-catalogue were Contended on load average (threshold 7.23).
  - Both clock-master records (08:00 to 08:03Z, load 6.8 to 9.2) and the first pkg-3 record (08:29:54 to 08:30:40Z, load after 18.1) predate my first build at 08:31:15Z.
  - The second pkg-3 record (08:32 to 08:33Z) overlapped my Tier 1 builds. Both arch-9a-catalogue records (08:35 to 08:40Z) overlapped my mutant builds, which ran at `nice -n 10 -m:2`. I record that overlap as mine.
  - Every contended record passed. clock-master logged 1 closeFinding, 1 unresponsive, 1 debtFault and 4 recalibrations under that load, but its 9/9 assertions passed.

**Contended-suite rerun `20261021T085602Z-21949`, verdict Pass** (`evidence-9a-15/e2e-serial-rerun-d3f17d27/`, byte-identical to its out directory). It follows the 9A.4 precedent of a Clean rerun of the contended SerialAlone records.
- `sourceHead` `d3f17d27`; `suiteSourceDigest` `72741401…` and `runnerSourceDigest` `ee0dbe73…`, both reproduced as above.
- `sourceStatusDigest` `53270e67…` is not empty. I could not reproduce it byte for byte, because the file set it hashed no longer exists. Her worktree's status now holds only 56 untracked files under `_bmad-output/implementation-artifacts/evidence-9a-15/` (her Full-run copy, rerun copy and mutant log): no product, suite or runner input. The suite and runner digests confirm that.
- Results:
  - clock-master 9/9, Clean;
  - pkg-3 3/3, Clean;
  - arch-9a-catalogue 25/25, Contended on its first record (09:00Z, while I was idle) and Clean on its second;
  - the smoke pair, 8/8, Clean;
  - 70/70 tests in all.
- The bundle identity (`b947597a…`) differs from the Full run's (`b5348bb1…`) over the same source. That is the known F17-11 non-determinism, not a new finding.

Together, the Full run and the rerun give all 44 suites passing 100% on `d3f17d27`, with every SerialAlone timing suite Clean at least once.

**Her mutants** (`evidence-9a-15/mutants.txt`, SHA-256 in `identity/reused-SHA256SUMS.txt`). M1 to M8 are all killed. Each maps to one of mine, killed by the same tests:

| Hers | Mine |
| --- | --- |
| M1 | R2b |
| M2 | R5 |
| M3 | R3 |
| M4 | R4 |
| M5 | R1 |
| M6 | R7 |
| M7 | R8 |
| M8 | R10 |

Her M8 (Coverage takes `DisplayName`) additionally kills `DeclaredPartIdentitiesComeFromEveryBundleElementId`. Her runner refuses to run tests on a build error, so she has no invalid-mutant case like my R2a. I did not re-run her mutants.

## Findings

None blocks. There is no Story F.18 in the worktree's `epics.md`, so these items are for the coordinator to file:

- **F-1. The inventory id refusal has no committed test.** `CheckPuzzle`'s `Element(inventory, …)` survives mutant R6. Head behaviour is correct (P1). Add a test that a save whose puzzle inventory names an unknown element is `UnknownElement` and names the id.
- **F-2. `tools/Coverage` depends on an untracked build output with no project reference.** `Coverage.Tests` has no reference to `tools/BundleElements`, so on a fresh clone `dotnet test tools/Coverage.Tests` alone fails 4 tests (P4). The solution-wide `dotnet test` passes, because the solution build generates the bundle first. Add a build-order reference, or document the order.
- **F-3. No committed test for reverse parity.** P3 shows the worker-known set equals the bundle's 22 deliverable ids, but only bundle ⊆ worker is a test. Story 9A.16, which carries colliders on the wire, can fold this in.
- **F-4. The schema guard matches field names only.** `NoSchemaFieldCarriesANumericPartId` matches `part`/`element` in field names. A numeric element identity under another name would pass. Low.
- **F-5. Formatting.** 101 test call sites read `Decode(SaveFixture.Elements,bytes)` with no space after the comma. Cosmetic; the hook and analyzers accept it.
