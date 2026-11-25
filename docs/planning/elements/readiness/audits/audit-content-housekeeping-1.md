# Audit: content campaign housekeeping contract (round 1)

Independent, context-free planning audit of `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md` on `2ce73eed`, 24 November 2026. Read-only; nothing was built or run.

## Scope read

**Contract.** `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md` (untracked, 3,410 words, within the 6,000-word budget), read on `2ce73eed`. `2ce73eed` is on `origin/main`.

**Authorities read.**
- `AGENTS.md`
- `owner-decisions-2026-11-22.md`: decisions 22, 24d–24f, 24m, 24n, 38 and 41
- `campaign-coverage.md`: sections 2, 5, 8 and 9, lines 38–40, and the 10a rows
- `completion-ledger.md:365–396`
- `deferred-work.md:186–196` and `:357–363`
- `delivery-batches.md` §4, and §5 line 352
- `bmad-loop-protocol.md`: Slice shapes, Review tiers, Slice record, Carry-forward
- `docs/delivery-workflow.md#new-package-registration-check`
- `docs/testing-guidance.md`
- the lettered level ids run log and its close note

**Code read.**
- `PuzzleContent.cs`, `PuzzleCatalogue.cs` and `PuzzleDefinition.cs:100–115`
- `LessonFixtures.cs`, `CurrentCatalogue.cs`, `RequirementDiscovery.cs` and `CapabilityInputs.cs:112–119`
- `CurrentOwnerMembership.cs`, `CurrentOwnerMembershipOracles.cs`, `CurrentOwnerRelations.cs` and `tools/Ownership/Program.cs`
- every test that reads `content/puzzles.json`: 35 `CuriousContraptions.tests` files, `TimingDeclarationTests`, `SelectionTests` and the Coverage tests
- `workshop-driver.ts:225–237`
- the owned scopes of lanes 1, 2 and 4

**Numbers recomputed independently. All match the contract.**
- Fixtures: 304 → 122 (78 Planned parts + 44 Playable placements). 182 removed.
- Sources: 1475 → 1293.
- The 182 `FIX-` rows sit in exactly the 23 runs cited, from `718` to `1010–1012`.
- The 14 "Level(s) unlocked" lines are exact.
- Removed entry ranges in `puzzles.json` are exact: `2659–2941`, `4239–4517`, `11647–24072`, `25093–33172`.
- Title lines 7199 and 33176 are exact. There are 63 subtitles, from line 6 to 36944.
- `campaign.md` has 62 "Placement" paragraphs and 62 "Currently authored" paragraphs, at the cited lines.
- 21 lettered levels hold authored content.
- Heading census recount is 545 (engine 61, gpu-physics 29, refinements 197, decisions 8, scope-corrections 28, current-consumers 72, campaign 150). Adding 21 headings gives 566.
- `CAT-015-V` is the only criterion that loses every fixture row.
- No `*.cs` or `*.ts` file names a removed or converted level id.
- `docs/coverage/**` and `docs/work-orders/**` have no code reader.
- The Python round-trip reproduces `puzzles.json` byte for byte.
- `AGENTS.md` and OS-02 justify the engine tier: the tool-source literal edit puts the slice outside the data-only allow-list.

## Findings

**B1 — Blocking (ledger and handoff integrity).** The contract plans to set "the row for this slice" to done. That row is OS-02 (`completion-ledger.md:393`), and OS-02 also covers DF-1 to DF-4: the conversions, the `domino_effect` re-authoring, moving the 34 levels to their positions, and the Rope anchor.
- Closing OS-02 would falsely close four deferred obligations.
- OS-03 (`:394`) still names "content slice" as its owner. DF-5 moves it to Epic 15 chapter slices.
- PR-24 (`:381`) says "the content slice (OS-02) must author the anchor". DF-4 defers that.
- DF-1 to DF-5 live only in this contract's frontmatter. Neither the ledger nor `deferred-work.md` records them.

**Fix:**
- Owned scope: restate OS-02 to this slice's delivered scope and set it to done at close.
- Add one open row each for DF-1, DF-2, DF-3 and DF-4, with the owner and gate from the Readiness split.
- Re-point OS-03's owner to DF-5's owner and gate.
- Amend PR-24's state text to point at the DF-4 row.
- Name all of these rows in the owned-scope table and in the Final diff scope.

**B2 — Blocking (consumer closure incomplete, and "Nothing else" prevents the repair).** These current-state lines become false and are not in the owned scope. The contract searched by level id only, so it missed counts and authored-position numbers. Values after the change were computed with jq over the 34 kept entries.

| Line | Says now | True after the change |
|---|---|---|
| `CAT-005-battery.md:238` | "14 levels place it" and lists positions 31, 37, 46, 53, 55 (all removed) | 9 levels (8 place it, 1 has it in inventory): Planned 14, 15, 16, 17, 18, 49, 58; Playable 59, 60 |
| `CAT-019-conveyor.md:67` | "inventory in 9 levels" | 4 |
| `CAT-035-lamp.md:14` | "17 planned levels name it" | 5 |
| `CAT-062-spring.md:14` | "`spring_forward` and 15 others" | `spring_forward` and 2 others |
| `CAT-063-switch.md:14` and `:214` | "placed in 19 levels, inventory in 11" | placed in 8, inventory 4 |
| `EL-189-tennis-ball.md:114` | "22 levels place it" | 5 |
| `EL-186-temporary-bridge.md:103` | names level `double_bridge` | the level no longer exists (restate it, or mark it as a historical check) |

Two more problems in the same area:
- The Final diff scope says "the eleven element documents", but the table lists nine.
- `CAT-023:121` and `CAT-035:117` appear both in the owned scope and in DF-1's deferred list. An implementer cannot tell whether those lines are edited now ("name the deferred owner") or left for DF-1.

**Fix:**
- Add these seven documents, with their lines and the values above.
- Correct the document count.
- Mark `CAT-023:121` and `CAT-035:117` "edited now: owner pointer only; the content change stays with DF-1".
- Add a closing check, run over `docs/planning/elements` and excluding `@a6c914e` evidence rows: an id search, plus a review of each "N levels" or "N others" statement and each authored-position list (8, 12, 30–48, 50–57).

**B3 — Blocking (carried handoff omitted).** The lettered level ids run log (Entry 10) says: "The merged commit's full round …, its SnapshotApproval and the push receipt are recorded … in the next lane-3 slice's candidate."
- This slice is that candidate. `2ce73eed` is published, but `TODO.md:8` still reads "remaining for the approver".
- The contract has no task for these receipts, and its "Nothing else" scope excludes `run-logs/spec-tool-lettered-level-ids.md`.
- This breaks the protocol's Carry-forward rule (`bmad-loop-protocol.md:36`).

**Fix:** add task 0b: record the merged round total, the SnapshotApproval and the push SHA `2ce73eed` as an appended entry in that run log, and restate `TODO.md:8`. Add the run log to the owned scope and the Final diff scope.

**N1 — Non-blocking (stale lines in owned files, not listed).**
- `campaign-coverage.md:38`: "census … (545)".
- `campaign-coverage.md:559`: "header note (63 authored …)" and "No `LEVEL-` heading is added".
- `campaign.md:1`: title "150 numbered slots … 1800 retained placement cells".
- `delivery-batches.md:209–212`: the 45 / 11 / 34 counts over 63 levels, next to the restated `:207`.
- `CAT-053-pulley.md:84`: calls `pulley_depth` "Around the corner" in current-state prose that carries the plan level.

**Fix:** list each line, or name it as kept with a reason.

**N2 — Non-blocking (U1 method).** "For example `jq`" contradicts "the diff holds only the intended hunks". `jq --indent 2 .` rewrites 158 lines of the unchanged file, for example `-4.371138828673793e-08` → `E-8` at `puzzles.json:1646`.

**Fix:**
- Name the Python filter only.
- Require a no-op run that reproduces the file (`cmp`) before applying it.
- After applying, check that `git diff --numstat` shows only the 29 entry ranges and 36 changed lines.

**N3 — Non-blocking (gap in the acceptance row).** H2 says "the topic after ` / ` is unchanged". The H2 test compares prefixes only, and AC4 runs `del(.subtitle)`, so nothing checks the topic.

**Fix:** in AC4, replace `map(del(.subtitle))` with `map(.subtitle |= sub("^[0-9]{2}[a-z]? / |^[0-9]{2} / "; ""))`, applied to the candidate only. Alternatively, have the H2 test assert the full subtitle against the transcribed baseline topic.

**N4 — Non-blocking (wrong refusal text in H5).** A removed level left in content, with its row deleted, throws "Fixture content differs from current declared identity, kind or locked state." (`CurrentCatalogue.cs:194–195`). It does not throw "Authored fixture membership differs." (`:198`).

**Fix:** give each case its own message.

**N5 — Non-blocking (commands as written).**
- AC3: `git grep -c` prints `docs/planning/invest/current-consumers.md:1`, not `1`. Use `git grep -h -c`.
- AC4: needs its executable form, `git show <baseline>:content/puzzles.json | jq -S '…'` against `jq -S '…' content/puzzles.json`, compared with `diff`.
- AC5: `rg` with no match exits 1. State "exit 1, no output". "Each preceded by its anchor" has no command; add `rg -U --count-matches '<a id="level-[0-9]{3}[a-z]"></a>\n### LEVEL-[0-9]{3}[a-z]\n'` → 21.

**N6 — Non-blocking (census verification is a recount only).** Plain `--oracles` passes a null root (`Program.cs:17`), and no test calls `OwnershipOracles.Run` with a root. So the literal 566 executes nowhere, and H6's error-handling column cannot be observed. This follows the lettered-ids precedent and `deferred-work.md:357–363`.

**Fix:** say so in H6 and in AC2. Optionally, add a Coverage.Tests fact that the 21 lettered ids are members of `CurrentOwnerMembership.Read` on the real tree, since no executed assertion covers that.

**N7 — Non-blocking (H3 control).** `PipeBend90Tests.cs:190` already asserts on the real file that `quarter_bend`'s title is "Around the corner". That is the executed control, and the reason behind D4's choice.

**Fix:** cite it in place of "inside AC4".

**N8 — Non-blocking (reasons for the deferrals).**
- DF-2 "depends on DF-1" does not hold: the campaign order of Playable levels does not depend on the re-authoring. The real blockers are the order-bound anchors at `workshop-driver.ts:225–237`, which need a browser to re-measure, and S5b's ownership of that file under 24m code disjointness.
- DF-3 and DF-4 say they need goal evaluation. Planned content is indexed but never constructed (`PuzzleCatalogue.cs:10`; `delivery-batches.md:217`), so converting it in Planned shape evaluates nothing. The real reason is to author once, where the layout can be proven.
- DF-1 to DF-5 are each correctly deferred.
- O1 should go to the owner before dispatch, covering all four OS-02 cuts (DF-1 to DF-4), because 24m names "the content slice" as pre-programme work.

**Fix:** restate the Depends-on cells for DF-2, DF-3 and DF-4, and widen O1 to cover DF-1 to DF-4.

**N9 — Non-blocking (disjointness wording).** Lane 1 part B is in progress (`TODO.md:6`), so "re-read at B's dispatch" no longer applies. `CAT-019-conveyor.md` is in B's owned scope and is not on 24m's shared-document list.

**Fix:** write "re-read and merged by hunk at the later publisher's merge" and get a coordinator ruling that this element document is a shared document. Alternatively, hand `:67`, `:147` and `:160` to part B.

**N10 — Non-blocking (citations).**
- `PuzzleCatalogue.ReadDefinitions` starts at `:161`, not `:160` (probe and read list).
- The `NativeSceneFixture` title check is at `:173`.
- Design Note 5 should cite the published `2ce73eed`, not the pre-merge `f60849bd`.

**Disjointness otherwise holds.**
- No other running lane edits `CurrentCatalogueTests.cs`, `CurrentCapabilityInputsTests.cs`, `CurrentOwnerMembershipOracles.cs`, `campaign.md`, `current-consumers.md` or `content/puzzles.json`.
- S6 to S6g only read goal kinds.
- The Baseball repair only reads the Coverage tests.
- Lane 4 adds `CurrentCapabilityInputsTests` to the registration filter. Whichever lane publishes second re-measures, as the contract says.
- The `campaign.md:894` and `:1006` notes (69c, 79b) correctly stay outside AC5's pattern.

**Policy.** No fallback, alias, placeholder or second rule. AC4 compares canonical field values of authored content, and the U1 SHA is Anvil tooling, so there is no hash or byte-identity acceptance. The new tests derive their expectations from `campaign-coverage` section 5, not from current output.

Verdict: Incomplete
