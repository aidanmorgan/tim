---
title: 'Lettered campaign level ids are declared owners (tool slice, lane 3)'
type: 'refactor'
created: '2026-11-24'
status: 'draft'
baseline_revision: 'captured at dispatch (the published main that contains owner decision 24m)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/readiness/campaign-coverage.md'
  - '{project-root}/_bmad/custom/bmad-loop-protocol.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The campaign plan has 244 levels: the 150 numbered slots and 94 lettered levels (`9a`, `28a`–`28c`, `69c`, `55e`, …; owner decisions 24c to 24f, coordinator decisions 35 and 36). A lettered level's owner heading in `docs/planning/invest/campaign.md` would be `LEVEL-069c`. The ownership tool's identity grammar admits only `LEVEL-[0-9]{3}` (`tools/Ownership/CurrentOwnerMembership.cs:109`), while its heading filter (`:26`) and the oracle's census pattern (`CurrentOwnerMembershipOracles.cs:60`) already select such a heading. So the first lettered heading makes `CurrentOwnerMembership.Read` throw `Invalid current owner identity 'LEVEL-069c'`, and with it `CurrentOwnerRelations.Read`, `CapabilityInputs.Prepare` and every Coverage test that reads the current documents. The Coverage tool's grouped-role pattern (`tools/Coverage/CurrentOwnerRelations.cs:43`) also cannot name a suffixed owner. The content slice that authors lettered levels cannot start.

**Approach:** Widen the one grammar arm to `LEVEL-[0-9]{3}[a-z]?` and the grouped-role pattern to the owner grammar, keep the identity a whole typed `WorkId`, refuse every other spelling with the existing typed refusal, prove it in the full local round through the Coverage tool's real document path, and correct the planning documents that say the tool refuses the form. No heading and no content are added.

## Boundaries & Constraints

**Always:** A lettered level id is the number of the slot it follows, zero-padded to three digits, plus one lowercase ASCII letter (`LEVEL-069c`). It is carried as the existing strongly typed identities, `Ownership.WorkId` and `CuriousContraptions.Coverage.WorkOrderId`; level ids are an extensible identity set, so no enum. Campaign order is `LEVEL-069` < `LEVEL-069a` < `LEVEL-069b` < `LEVEL-070`, which is the ordinal order of the canonical spelling; no code consumes it. The refusal stays `InvalidDataException` with the message `Invalid current owner identity '<value>'.` Every other grammar arm is unchanged. Tests run the real `CurrentOwnerMembership` and `CurrentOwnerRelations` code. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A change to `content/puzzles.json` or any `LEVEL-` heading in `campaign.md` (later slices; the first is the content slice, completion-ledger OS-02). A new level type, comparer, sort, base-slot rule or document-order rule. A second grammar for lettered ids, an alias that maps `LEVEL-069c` to `LEVEL-069`, or a fallback that skips an invalid heading. A change to the census count 545. A local Release build. Output hashes, mutants, browser runs or skipped tests.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| M1 | The seven current invest documents with `### LEVEL-069z` (and its anchor) inserted after `LEVEL-069`'s entry; `z` is outside the planned letters `a`–`e` | `CurrentOwnerRelations.Read` succeeds; its members equal the unmodified copy's members plus `LEVEL-069z` and nothing else | No error expected |
| M2 | The same seven documents unmodified | The members contain every `LEVEL-001`–`LEVEL-150` and not `LEVEL-069z` | No error expected |
| M3 | A heading `LEVEL-001a`, `LEVEL-069z` or `LEVEL-150a` in a minimal document set with no numbered `LEVEL-` heading | Admitted as exactly that identity: the result is the fixture's set plus that id | No error expected |
| M4 | A heading `LEVEL-069C`, `LEVEL-069aa`, `LEVEL-069a1`, `LEVEL-69a` or `LEVEL-0069a` | Refused; nothing is returned | `InvalidDataException`, message `Invalid current owner identity '<value>'.` |
| M5 | An all-criteria grant line under `## LEVEL-069c` naming `LEVEL-069c` as its owner | The grouped proof role resolves to `LEVEL-069c` | A grant naming `LEVEL-069` under that heading is refused: `InvalidDataException`, "Grouped role owner differs from its current heading." |
| M6 | The ownership oracles without a root | The positive fixture includes `LEVEL-069c`; every existing attack is still rejected | An accepted attack or a changed positive set fails the oracle |

</intent-contract>

## Code Map

Delivery shape: single structural tool slice (no element, family or closure). Review tier: **engine**, because the owned diff changes tool projects (`tools/Ownership`, `tools/Coverage`) and adds a test to a tool test project, which are not on the data-only allow-list; the capability probe is below. Baseline: captured at dispatch as the published main that contains owner decision 24m (the coordinator's planning edit, uncommitted at drafting); every scope guard and `git diff` compares against it. Drafted and line-checked on `0959e883` (origin/main, 24 November 2026), which is the drafting revision only. Lane 3 of owner decision 24m; the content slice follows it in the same lane.

**Status: draft.** Planning verdict: **Scoped Pass** (ready-for-dev) per the second audit (`docs/planning/elements/readiness/audits/audit-tool-lettered-level-ids-2.md`, 24 November 2026), whose five non-blocking findings are applied; the first audit (`audit-tool-lettered-level-ids-1.md`, Incomplete) found 1–11, all applied. Dispatch waits only for the publication of owner decision 24m and the coordinator ruling after it. The shared-files and disjointness text was aligned with that ruling after the second audit (records only; no behaviour, task or acceptance changed).

Correction to the dispatch brief: `:109` on `0959e883` reads `LEVEL-[0-9]{3}`, with no letter; adding `[a-z]?` is this slice's change.

Owned scope:

- `tools/Ownership/CurrentOwnerMembership.cs` -- `Identity` `:105–112`: the `LEVEL-[0-9]{3}` arm at `:109` becomes `LEVEL-[0-9]{3}[a-z]?`; the comment at `:107–108` names the campaign's lettered level beside the roadmap sub-slices.
- `tools/Ownership/CurrentOwnerMembershipOracles.cs` -- positive fixture `:28` gains `\n## LEVEL-069c` and the expected set `:30` gains `new("LEVEL-069c")`; the census comment `:67–68` adds that each `LEVEL-` heading a later slice adds raises 545 by one.
- `tools/Coverage/CurrentOwnerRelations.cs` -- the grouped-role pattern `:43`, `([A-Z0-9-]+)`, becomes the owner grammar `([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*(?:(?<=[0-9])[a-z][0-9]*)?)` (the `WorkOrderId` pattern, `CapabilityInventory.cs:41`), so a suffixed owner's grant is checked and granted.
- `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` -- new; the four tests of the acceptance-to-test table (11 cases).
- `docs/planning/elements/readiness/campaign-coverage.md` -- `:38` (the paragraph saying the tool accepts only `LEVEL-[0-9]{3}` and the CAMPAIGN slice extends it) and `:564` ("Not done": the two tool changes) restated as done, with the id form and the ordering rule.
- `docs/planning/invest/campaign.md` -- `:9`, the clause "a lettered level's `LEVEL-` entry is added when it is authored" gains the form (`LEVEL-069c`).
- `docs/planning/elements/readiness/completion-ledger.md` -- row CA-01 (`:296`): state column set to done, citing this slice, item text unchanged; row OS-02 (`:393`): item gains "adds the `LEVEL-` headings of the levels it places at lettered positions and raises the census literal".
- `_bmad-output/implementation-artifacts/deferred-work.md` -- one entry: every slice that adds a `LEVEL-` heading (first expected: the content slice OS-02, for relocated levels such as 11a) raises the census literal (`CurrentOwnerMembershipOracles.cs:69`) by the headings it adds and verifies it by recounting the seven documents with the oracle's census pattern (`CurrentOwnerMembershipOracles.cs:60`), as was done for 545 at `0959e883`, unless the coordinator rules that the Release library builds a root `--oracles .` needs are allowed (owner: that slice).
- `TODO.md`, this contract and `_bmad-output/implementation-artifacts/run-logs/spec-tool-lettered-level-ids.md`.

Read, not changed (cite in review): `tools/Ownership/CurrentOwnerMembership.cs:9–13,26` (`Read`, heading filter); `tools/Ownership/WorkId.cs:3`; `tools/Ownership/OwnershipOracles.cs:47,133–135` (calls the current-owner oracles; the policy check returns null without a root); `tools/Ownership/SourceInventory.cs:39–41,125–129` (the root run's Release rebuilds); `tools/Ownership/Program.cs:12,17`; `tools/Ownership/README.md:4,37`; `tools/Coverage/CurrentOwnerRelations.cs:12–21,32,76` (reads the seven documents through `CurrentOwnerMembership.Read`; sorts owners per key with `StringComparer.Ordinal`); `tools/Coverage/CapabilityInputs.cs:65`; `tools/Coverage/CapabilityInventory.cs:34–45` (`WorkOrderId`); `tools/Coverage.Tests/CurrentCapabilityInputsTests.cs:9–14,188–230` (`Root`, `CopyCurrentInputs`); `tools/Coverage.Tests/Coverage.Tests.csproj` (no Compile list: a new file needs no project edit); `engine/gpu/PuzzleContent.cs:9–18,38`, `engine/gpu/PuzzleDefinition.cs:151–153`, `engine/gpu/PuzzleCatalogue.cs:15,174–175` (content ids are snake_case `PuzzleId`s and play order is the integer `presentation.order`; no `LEVEL-` id is read); `tools/E2eRunner/Selection.cs:72,104` and `tools/E2eRunner/SuiteCatalogue.cs:31` (level ids are `puzzles.json` ids); `docs/planning/elements/readiness/palette-allocation.md:9` (rule 3: a lettered level sorts after its number).

### Capability probe

Measured on `0959e883`. No solver, ABI, wire or presentation capability is involved.

- **Narrower, owned here:** the identity grammar. `CurrentOwnerMembership.Identity` (`:109`) admits `LEVEL-[0-9]{3}` only; exercised by the oracle positive `LEVEL-001` (`CurrentOwnerMembershipOracles.cs:28,30`) and, on the real tree, by every Coverage test that reaches `CurrentOwnerRelations.Read` (for example `CurrentCapabilityInputsTests.NamedPackageClosureIsRequiredWithoutGrantingCompletion`, through `CopyCurrentInputs` and `CapabilityInputs.Prepare`). This slice widens it and adds M1–M4.
- **Narrower, owned here:** the grouped-role pattern (`CurrentOwnerRelations.cs:43`) cannot match a suffixed owner; exercised for unsuffixed owners by `CurrentCapabilityInputsTests.LinkCardinalityNeverGrantsProofAndRepeatedExplicitRolesCoalesce` (`:88–109`). Unreachable on today's documents (no "Exact source clauses" line in `campaign.md` or `engine.md`); this slice widens it and adds M5.
- **Present:** heading selection. `CurrentOwnerMembership.cs:26` selects `## LEVEL-…`/`### LEVEL-…` lines; exercised by the same real-tree tests (150 `LEVEL-` headings in `campaign.md`).
- **Present:** the Coverage typed identity. `WorkOrderId` (`CapabilityInventory.cs:41`) admits a lowercase letter after a digit; exercised by `CurrentCapabilityInputsTests.CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles` (`:28–32`), which converts the real `ENGINE-CORE-2d` owner (`engine.md:200`) through `CurrentOwnerRelations.Read`.

Supplemental CLI check, not a capability this behaviour needs: the heading census (`CurrentOwnerMembershipOracles.cs:53–70`, literal 545 at `:69`), reached only through `OwnershipOracles.Run` (`OwnershipOracles.cs:47`) from `Program.cs:17` with a root. Re-counted on `0959e883`: engine 61, gpu-physics 29, refinements 197, decisions 8, scope-corrections 28, current-consumers 72, campaign 150. With a root, `OwnershipOracles.Run` also runs `CheckCurrentPolicy` → `SourceInventory.Capture`, a Release `-target:Rebuild -p:BuildProjectReferences=false` of the game, Animation and test projects (`SourceInventory.cs:39–41,125–129`), which needs Release outputs this workflow does not build (AGENTS.md: no local Release worker build). So this slice runs the oracles without a root (M6) and proves the real tree through M1.

Direct consumers of a lettered id: `CurrentOwnerRelations.Read` and `Parse` (Coverage) and `OwnershipAudit.Validate` (`Program.cs:23,43`), the first and last through `CurrentOwnerMembership.Read`. No consumer reads or counts the number or letter of a level id: `git grep -n "LEVEL-"` over `*.cs`, `*.ts`, `*.json` on `0959e883` finds only the two Ownership files. `docs/coverage/` and `docs/work-orders/` hold no `LEVEL-` id. No test counts `LEVEL-` ids.

### Decisions

- **D1. One grammar arm, single letter.** `LEVEL-[0-9]{3}[a-z]?`, not the roadmap form `[a-z][0-9]*`: the 94 planned lettered levels all use one letter, `a` to `e` (campaign-coverage section 6, counted on `0959e883`).
- **D2. Ordering is the spelling, not code.** No code consumes level order: the only sort is the per-key ordinal sort at `CurrentOwnerRelations.cs:76`, and play order is `presentation.order`. Zero-padded digits and one lowercase letter make ordinal order the campaign order; the rule describes the canonical spelling and adds no tested behaviour. Non-canonical spellings are refused because they are not canonical.
- **D3. Typed ids, no new type.** `WorkId` and `WorkOrderId` are the strongly typed identities (AGENTS.md typing rule); nothing reads a level's number or letter, so splitting them into fields would be unused structure.
- **D4. No base-slot rule.** Every numbered slot `LEVEL-001`–`LEVEL-150` is permanent (`campaign.md:9`: a removed level keeps its slot), and numbered ids are not range-checked today.
- **D5. Census unchanged here; owned by the slices that add headings.** No heading is added, so 545 stays. The root census run needs Release outputs; the slice that first adds a `LEVEL-` heading raises the literal and records how it ran the census (deferred-work entry above; owner decision 24m merge rule 3 names "oracle counts").
- **D6. Discriminating tests in `Coverage.Tests`.** The oracle is not in the full local round, so M1–M5 are xUnit tests there; the oracle's positive fixture gains one lettered heading so it covers every arm it admits (`campaign-coverage.md:564` asks for it). Its attacks are not duplicated.
- **D7. The grouped-role pattern takes the owner grammar.** A lettered owner is first-class in Coverage as well as Ownership; one pattern, the `WorkOrderId` one, replaces the narrower one.

No owner decision is open.

### What this removes

The refusal of a lettered level heading by `CurrentOwnerMembership.Identity`; the narrower grouped-role pattern at `CurrentOwnerRelations.cs:43`; the planning text that defers the grammar change to the CAMPAIGN slice (`campaign-coverage.md:38,564`). Nothing is kept for the old form: a numbered id keeps its one spelling.

## Tasks & Acceptance

**Execution:**

0. Before the first edit, on the baseline: run `dotnet run --project tools/Ownership -- --oracles` and record its exit code and `CurrentOwners` result in the run log. If it fails at the baseline, stop and return to planning.
1. `tools/Ownership/CurrentOwnerMembership.cs:109` -- `LEVEL-[0-9]{3}` → `LEVEL-[0-9]{3}[a-z]?`; comment `:107–108` -- M3, M4.
2. `tools/Coverage/CurrentOwnerRelations.cs:43` -- D7 -- M5.
3. `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` -- the four tests below. The Parse cases use minimal valid engine and consumers documents in the test (an engine document with one owner heading such as `## ENGINE-CORE-1` and one P0 disposition row; one `CAT-` owner and its fixture row; no numbered `LEVEL-` heading), shaped like the oracle's fixtures at `CurrentOwnerMembershipOracles.cs:10–27`. The real-tree case copies the seven documents `CurrentOwnerRelations` reads to a new temporary directory, deleted at the end -- M1–M5.
4. `tools/Ownership/CurrentOwnerMembershipOracles.cs:28,30,67–68` -- the positive heading, the expected identity, the census comment -- M6.
5. `docs/planning/elements/readiness/campaign-coverage.md:38,564`, `docs/planning/invest/campaign.md:9`, `docs/planning/elements/readiness/completion-ledger.md:296,393`, `deferred-work.md` (D5's entry), `TODO.md` -- current documents.

**Acceptance Criteria:**

- AC1: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the full round at the baseline + 11 (all in `Coverage.Tests`, 115 + 11 at drafting), re-measured at the baseline before the first edit; when this slice publishes second, the merged main's full round + 11.
- AC2: Given the candidate, when `dotnet run --project tools/Ownership -- --oracles` runs, then it exits 0 and its `CurrentOwners` result reports `Positive` true and `NegativeCount` 15, as at the baseline (task 0).
- AC3: Given the candidate diff, when its paths are listed, then each is in the Final diff scope; `git diff --quiet <baseline_revision> -- content/puzzles.json` exits 0; and `git diff <baseline_revision> -- docs/planning/invest/campaign.md | grep -c '^+#\{2,3\} LEVEL-'` prints 0. On a merged candidate, the path list and both commands compare against the merge's `origin/main` parent instead of `<baseline_revision>`.
- AC4: Given the candidate, when `git grep -n 'accepts only .LEVEL-\[0-9\]{3}.' -- docs/planning/elements/readiness/campaign-coverage.md docs/planning/invest/campaign.md` and `git grep -n 'Adding them needs two tool changes' -- docs/planning/elements/readiness/campaign-coverage.md` run, then neither lists anything.

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour and scope.** A lettered campaign level id is a declared owner wherever owner ids are validated; numbered ids are unchanged. Source: owner decisions 24c–24f; completion-ledger CA-01; campaign-coverage `:38`. Related rows `requirements.md:2527,3431` (typed level identity for content and navigation, the `PuzzleId` side) stay open; this slice covers only the planning-owner id.
2. **Reusable primitive.** None new: the existing identity grammar and typed `WorkId`.
3. **Declared composition.** Owner headings in the seven invest documents remain the only declarations (`tools/Ownership/README.md:37`).
4. **Execution and ownership.** Tool and test code only; the tests write only their own temporary directory.
5. **Generality.** Representative ids (lowest and highest planned slot numbers, first and last letter), five refused near-miss spellings, and a real-tree heading (`LEVEL-069z`) that no planned level uses.
6. **Forward-only.** Deletes the refusal of the lettered form, the narrower role pattern and the stale planning text; no alias, second grammar or skip.
7. **Prerequisites.** Publication of owner decision 24m; no capability prerequisite. Engineering rules E2 (typed identities), E7, E8, E9 apply; E1, E3–E6 and the element-free design rules D1–D7 are untouched, because no engine, package, wire or presentation code changes.

## Verification

Executor for every command: the implementation owner. Raw output under `.tools/lettered-level-evidence/`; one line per result here, citing its run-log entry. A change to a tool source, test source or project file invalidates the builds and logs.

Environment: the pinned toolchain first on `PATH` (`PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403`); record the session environment check in the run log.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| M1, M2 | `CurrentOwnerMembershipTests.ALetteredLevelHeadingInTheCampaignDocumentIsADeclaredOwner` (fact): copy, insert `<a id="level-069z"></a>` and `### LEVEL-069z` immediately before `<a id="level-070"></a>`, `CurrentOwnerRelations.Read`; its `Members` equal `Read(unmodified copy).Members` ∪ {`LEVEL-069z`} | Control (M2, same fact): the unmodified copy's members contain every generated `LEVEL-001`…`LEVEL-150` and not `LEVEL-069z`. Whole-set equality catches an alias to `LEVEL-069`. On the baseline the inserted copy throws, naming `LEVEL-069z` | planned |
| M3 | `CurrentOwnerMembershipTests.AWellFormedLetteredLevelIdentityIsAdmitted` (theory: `LEVEL-001a`, `LEVEL-069z`, `LEVEL-150a`): `Parse` returns exactly the fixture's expected set ∪ {that `WorkId`} (`SetEquals`) | Representative ids: lowest and highest planned slot, first and last letter | planned |
| M4 | `CurrentOwnerMembershipTests.AMalformedLetteredLevelIdentityIsRefusedNamingIt` (theory: `LEVEL-069C`, `LEVEL-069aa`, `LEVEL-069a1`, `LEVEL-69a`, `LEVEL-0069a`): `Parse` throws `InvalidDataException` whose message equals `Invalid current owner identity '<value>'.` | Each differs from an admitted M3 spelling in one cause: case, letter count, an ordinal, digit count (two, four) | planned |
| M5 | `CurrentOwnerMembershipTests.AGroupedRoleUnderALetteredLevelResolvesToThatLevel` (theory, 2 cases): members {`LEVEL-069c`}, document `## LEVEL-069c` then one line holding `Original owner, all criteria and technical stage: LEVEL-069c.` and one exact-source-clause link (the form at `CurrentCapabilityInputsTests.cs:91,97`); `ResolveProof` returns `LEVEL-069c` with no issue. On the baseline this case reports `UnresolvedProofRole` | Control case: the same grant naming `LEVEL-069` throws `InvalidDataException` "Grouped role owner differs from its current heading." | planned |
| M6, AC2 | `Ownership --oracles` (Verification command) | Existing 15 current-owner attacks; baseline run of task 0 | planned |
| AC1 | Full local round | Totals against the baseline round | planned |

No float is compared; every assertion is a set, an identity or a refusal message.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, with `CSHARP_STATIC_GATE_BINLOGS` exported -- expected: five logs, zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero violations. Run as soon as the candidate compiles and again at handoff.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` (a test file is added) -- expected: its count at the baseline, unchanged (the new class is not in its filter), 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~CurrentOwnerMembershipTests|FullyQualifiedName~CurrentCapabilityInputsTests|FullyQualifiedName~CurrentCatalogueTests"` -- expected: 0 failed, 0 skipped; the eleven new cases listed.
- `dotnet run --project tools/Ownership -- --oracles` -- at the baseline (task 0) and on the candidate: AC2.
- The AC3 and AC4 commands -- expected as stated.
- `git add -N` the new test file, this contract and its run log, then `git diff --check <baseline_revision>` -- expected: no output. Whoever stages the candidate replaces the test file's intent-to-add entry with `git add` (`tools/anvil/CheckStagedTests.cs:35`).
- One full local round under `caffeinate -i` -- expected: AC1.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: its selection at the baseline count, 0 failed, 0 skipped.

### Running beside other work

Prerequisite: the coordinator's planning edit that records owner decision 24m, its merge rule and the coordinator ruling after it (uncommitted in `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` at drafting) is published, and the baseline is captured from that main.

Final diff scope: `tools/Ownership/CurrentOwnerMembership.cs`, `tools/Ownership/CurrentOwnerMembershipOracles.cs`, `tools/Coverage/CurrentOwnerRelations.cs`, `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` (new), `docs/planning/elements/readiness/campaign-coverage.md`, `docs/planning/invest/campaign.md`, `docs/planning/elements/readiness/completion-ledger.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md`, this contract and its run log. Nothing else.

Disjointness, checked against the owned-scope lists on `0959e883` (plus the worktree drafts). Code: `tools/Ownership`, `tools/Coverage` and `tools/Coverage.Tests` are edited by this lane alone. Shared documents, under the coordinator ruling recorded after owner decision 24m (shared policy and workflow documents are shared documents, not lane code): `completion-ledger.md`, `deferred-work.md` and `TODO.md`. A line citation that another slice moves in this contract (for example `tools/anvil/CheckStagedTests.cs:35`, moved by lane 4) is resolved at each later publisher's merge: that publisher re-reads the cited lines on the merged commit and its full round runs the changed gates (24m merge rule 3).

- **S3** (`spec-s3-remaining-per-package-lists.md`): code disjoint (S3 owns `CuriousContraptions.tests/*` and `docs/delivery-workflow.md`). Shared: `deferred-work.md`, `TODO.md`. Shared measured total: the full local round (S3 + 8, this slice + 11); the later publisher re-measures on the merged candidate.
- **S5** (`spec-s5-generate-wire-accessors-in-build.md`): code disjoint (S5 reads `tools/Ownership/SourceInventory.cs:13–16,125–129` and does not change it; this slice does not touch that file). Shared: `deferred-work.md`, `TODO.md`; the full-round total.
- **Visible-solid parts B, B2, C**: code disjoint (packages under `elements/`, `VisibleSolidColliderTests.cs`, CAT documents). Shared: `deferred-work.md` (B, C), `TODO.md`; the full-round total.
- **Part D and the Baseball repair (lane 1); S5b, S6, S6b–S6g (lane 2)**: code disjoint (checked on the drafts; none edits `tools/Ownership`, `tools/Coverage` or `tools/Coverage.Tests`). Part D shares `completion-ledger.md` (its rows, not CA-01 or OS-02); all share `deferred-work.md`, `TODO.md` and the full-round total.
- **Policy-tool fixes (`spec-policy-tool-fixes.md`, lane 4)**: code disjoint (`tools/anvil/` belongs to lane 4 alone; the `_bmad/custom/*.md` and `docs/delivery-workflow.md` it edits are shared documents this slice does not touch). Shared: `completion-ledger.md` (this slice rows CA-01 and OS-02, that slice row OS-04), `deferred-work.md`, `TODO.md`. It adds `CurrentCapabilityInputsTests` to the registration filter; that changes the registration count, not this slice's new class, and whichever publishes second re-measures it on the merged candidate. It moves `CheckStagedTests.cs:35`, which this contract cites; resolved at the later publisher's merge.
- **The content slice (OS-02)** runs after this one in lane 3 and owns `content/puzzles.json`, the first `LEVEL-` headings and the census literal.

One publisher at a time under the owner decision 24m merge rule: the later publisher merges `origin/main` once, keeps both sides' entries in the shared documents, and repeats the full local round on the merged candidate.
