# Audit: content campaign housekeeping contract (round 2)

Independent, context-free re-audit of `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md` on `2ce73eed`, 24 November 2026, after the round-1 repairs ([round 1](audit-content-housekeeping-1.md)). Read-only; nothing was built or run.

## Scope read

**Contract.** `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md` (untracked, 3,833 words), read on `2ce73eed`. `2ce73eed` is an ancestor of `origin/main` and equals it. Round 1 is `docs/planning/elements/readiness/audits/audit-content-housekeeping-1.md`. The audit was read-only.

**Authorities read:**
- `AGENTS.md`
- `owner-decisions-2026-11-22.md` decisions 22, 24d–24f, 24m and 41
- `campaign-coverage.md` sections 2 (`:33–40`), 5 (`:100–192`), 6 (`:288`, `:303–304`) and 9 (`:559–564`)
- `completion-ledger.md:380–396`
- `deferred-work.md:357–363`
- `delivery-batches.md` §4 (`:203–236`) and `:352`
- `bmad-loop-protocol.md` (Review tiers, Slice record)
- `docs/delivery-workflow.md#new-package-registration-check`
- `docs/testing-guidance.md`
- the lettered level ids run log, Entry 10

**Code read:**
- `PuzzleContent.cs`, `PuzzleCatalogue.cs:8–16` and `:159–177`, `PuzzleDefinition.cs:100–116`
- `LessonFixtures.cs`, `CurrentCatalogue.cs:185–200`, `RequirementDiscovery.cs:60–90`, `CapabilityInputs.cs`, `CurrentOwnerRelations.cs`
- `CurrentOwnerMembership.cs`, `CurrentOwnerMembershipOracles.cs`, `Program.cs:15–20`
- `CurrentOwnerMembershipTests.cs`, `CurrentCatalogueTests.cs:55–62`, `CurrentCapabilityInputsTests.cs:20–75`
- `WorkshopSaveTests.cs:150–158`, `PipeBend90Tests.cs:185–191`, `NativeSceneFixture.cs:165–175`, `TimingDeclarationTests.cs:124–131`
- `workshop-driver.ts:225–237`

**Lanes read:**
- `spec-elements-visible-solid-remainder-b`, `-b2` and `-c`
- `spec-repair-baseball-el-239`
- `spec-s5*` and `spec-s6*`
- `spec-policy-tool-fixes`

**Recomputed independently on `2ce73eed`.** I built a simulated candidate in the scratchpad with jq: the 29 ids removed, the plan-level prefixes applied and the two titles changed. These all match the contract:
- 29 removed ids and 34 kept (15 Playable, 19 Planned).
- 21 lettered levels.
- Fixtures 304 → 122, which is 78 Planned parts plus 44 Playable placements.
- 182 `FIX-` rows, in exactly the 23 cited runs.
- The 14 "Level(s) unlocked" lines.
- Of the 21 `CAT-*-V` criteria, 20 survive; only `CAT-015-V` goes.
- Census recount 545; adding 21 headings gives 566.
- `NegativeCount` 15, from the 15 `Attack` values.
- Titles are pairwise distinct on the candidate; the baseline has "Around the corner" twice.
- No `*.cs` or `*.ts` file names a removed level id or a removed `FIX-` id.
- The removed byte ranges total 21,068 lines.

## Round-1 findings

| Id | Status | Evidence |
|---|---|---|
| B1 | Resolved, conditional on P2 | P2 (`:53`) makes the coordinator restate OS-02, add OS-06 to OS-09, re-own OS-03 and re-point PR-24 before dispatch. Task 0 checks this (`:121`). OS-02 is set to done only at close (`:85`). The frontmatter `deferred` list cites the new rows (`:14`). The ledger has none of these yet (`completion-ledger.md:381`, `:393–394`); that is intended. |
| B2 | Not resolved (see new B1) | All 7 added documents and their values check out on the simulated candidate: CAT-005 9 levels (list ✓), CAT-019 inventory 4, CAT-035 5 Planned, CAT-062 "and 2 others", CAT-063 8 placed / 4 inventory, EL-189 5, EL-186 marked as a check. The table and `:181` both count 15 documents. The CAT-023 and CAT-035 lines are marked "owner pointer only". AC6 is added (`:136`). The closure is still incomplete: 8 more stale lines, all outside the Final diff scope. |
| B3 | Resolved | Task 0b (`:122`); the run log is in the owned scope (`:86`) and the Final diff scope (`:181`). See N6. |
| N1 | Resolved | `:75`, `:81–83`. `CAT-053:84` now conflicts with Never (N4). |
| N2 | Resolved in method, wrong numbers | Python filter only, a no-op `cmp`, and `numstat` (`:52`). The expected counts are wrong (N1). |
| N3 | Resolved | AC4's `sub("^[0-9]{2}[a-z]? / "; "")` runs on both sides. `diff` exits 0 with no output for the baseline against the simulated candidate. It strips `01 / `, `11a / ` and burst's `15 / `. |
| N4 | Resolved | H5 gives each case its own message (`:39`). |
| N5 | Resolved | AC3 uses `-h -c` (`:133`). AC4 is given in executable form (`:134`). AC5 states exit 1 and adds the anchor count, 21 (`:135`). |
| N6 | Resolved | `:40`, `:132`; the H6 fact is added (`:165`). |
| N7 | Resolved | `:162` cites `PipeBend90Tests.cs:190`. |
| N8 | Resolved | DF-2, DF-3 and DF-4 reasons restated (`:62–64`); O1 widened to DF-1 to DF-4 (`:55`). |
| N9 | Resolved | P3 (`:54`) and the disjointness wording (`:185`). |
| N10 | Resolved | `PuzzleCatalogue.cs:161` (`:90`, `:97`), `NativeSceneFixture` `:169–173`, Design Note 5 cites `2ce73eed` (`:150`). |

## New findings

**B1 — Blocking: element-document closure is still incomplete, and "Nothing else" (`:181`) prevents the repair.** These current-state lines become false and are outside the owned scope (`:83`). AC6's review clause (`:136`) will flag every one of them. Values are from the simulated candidate:

| Line | Says now | True after the change |
|---|---|---|
| `CAT-042-motor.md:81` | "a locked fixture in its 17 levels" | 12 |
| `CAT-042-motor.md:225` | Planned list includes 31, 37, 46, 53, 55 | drop them (all removed) |
| `CAT-019-conveyor.md:208` | "31, 37, 46, 49, 53, 55 and 58 wait" | 49 and 58 only (outside P3's three lines) |
| `EL-202-conveyor.md:109` | "planned levels 31, 37, 46, 49, 53, 55 and 58" | 49 and 58 |
| `EL-199-electric-motor.md:126` | "21, 22, 24, 31, 37, 46, 49, 53, 55 and 58" | 21, 22, 24, 49, 58 |
| `EL-194-springboard.md:113` | "All fifteen composite lane instances … 30, 35, 36, 41 (two lanes), 43, 44, 45, 47, 49, 51, 52, 53, 54, 56" | one instance, 49 `double_cold_start` (until DF-3) |
| `CAT-028-fan.md:109` | "22 levels … place it, eight of them as a fixed cold fan" | 5 levels, 2 fixed |
| `CAT-064-tennis.md:94` | "22 levels … place it" | 5 |
| `EL-196-battery.md:99` | "twelve levels place a locked battery" | eight |

Two related gaps:
- `EL-209-fan.md:120` is owned, but its value is not stated. It becomes "5 levels"; `wind_signal` drops from the provable list.
- `CAT-005-battery.md:238` ends "Level 14 needs the `turned` goal kind". Once the list uses plan levels, that must read 12.

**Fix:**
- Add the nine lines to the owned-scope element row and the Final diff scope (15 → 21 documents), with the values above.
- Extend P3 to `CAT-019-conveyor.md:208`.
- Add a P3-style ruling for `CAT-064-tennis.md:94`. It is in lane 1 B2's owned scope (`spec-elements-visible-solid-remainder-b2.md:58`); the alternative is to hand that line to B2.

**N1 — Non-blocking: wrong change counts in U1 and the scope.** `first_principles` is already `01 / Gravity & direction` (plan level 1). So 33 subtitles change, not 34, and the edit changes 35 lines, not 36. The removed ranges are 21,068 lines and 553,839 bytes, not "about 512 KB". Affected text: `:27` "(all 34)", `:52` "36 string fields … 36 changed lines", and `:73` "the 34 kept `subtitle` lines (`6`, …".

**Fix:** "33 subtitles (line 6 unchanged) and 2 titles"; expected `git diff --numstat` = `35	21103	content/puzzles.json`; "about 554 KB".

**N2 — Non-blocking: AC6 cannot pass as written.** `rg -n -w '<29 ids>' docs/planning/elements` also matches `readiness/`. Those hits are:
- `completion-ledger.md:174`, `:297`, `:299`, `:308` (rows DC-43, CA-02, CA-04, CA-13)
- `campaign-coverage.md:104–132`, `:151`, `:562`
- `audits/audit-content-housekeeping-1.md:70`, and this report once saved

None of these is a row pinned to `@a6c914e`, a "was removed" statement, or `EL-186:103`.

**Fix:** use `rg -n -w '<ids>' --glob '!readiness/**' docs/planning/elements`. `campaign-coverage.md` is already covered by its own owned rows.

**N3 — Non-blocking: `CAT-035-lamp.md:117` scope is ambiguous.** `:83` says "owner pointer only". But the line also says "The other 17 levels … power_trip, spring_signal, wind_signal, …" and names 12 removed ids, so AC6 forces a restatement.

**Fix:** state that `:117` also gets "the other 5 levels: `power_trip`, `spring_signal`, `counterweight`, `pulley_depth`, `grand_contraption`", and that the `domino_effect` content change stays with DF-1.

**N4 — Non-blocking: `CAT-053-pulley.md:84` conflicts with Never.** It is an evidence row pinned to `@a6c914e` (row P12, evidence column `content/puzzles.json@a6c914e:…`). Never (`:29`) forbids edits to such rows, but the owned scope (`:83`) retitles `pulley_depth` in it.

**Fix:** name `CAT-053:84` as an exception: the description cell is restated, and the evidence cell stays unchanged. Alternatively, drop it and record why it is kept.

**N5 — Non-blocking: H6 claims more than its test asserts.** H6 (`:40`) says members lose the 182 removed `FIX-` ids and `CAT-015-V`. The named fact (`:165`) asserts only the 21 lettered ids, and no executed test covers `CAT-015-V`'s removal.

**Fix:** in the same fact, assert that `FIX-015-001` and `CAT-015-V` are not members, or that the `FIX-` member count is 122. Otherwise, drop the clause from H6.

**N6 — Non-blocking: task 0b has an undeclared input.** It copies "the merged round total, SnapshotApproval, push SHA" "from the coordinator's publication record" (`:122`). No such record exists in the tree on `2ce73eed`: run log Entry 10 only states the expectation (7317 + 15 = 7332), and `TODO.md:8` still says "remaining for the approver".

**Fix:** add it to P2. The coordinator supplies the merged round total, the SnapshotApproval verdict on `2ce73eed`, and the push receipt before dispatch.

**N7 — Non-blocking: citations.**
- `EL-209-fan.md:87` is in a "files harvested" list, which is historical evidence. State that it gets the same marked check at `a6c914e` as `EL-186:103`.
- `Program.cs:17` (`:90`) passes a root when one is given (`args.Length > 1 ? … : null`). Write "plain `--oracles` passes no root".

**Checked and sound:**
- Preconditions U1, P2, P3 and O1 are each stated with owner, content and stop condition (U1 explicitly). O1 correctly leaves this slice independent of the answer.
- The engine tier is justified by the tool-source literal and the tool test edits (`bmad-loop-protocol.md`, Review tiers).
- AC4 enforces array order and `presentation.order` field equality (H4).
- Discovery stays at 1293: `campaign.md` is not read by `RequirementDiscovery`, and the new entries add no `**Exact source clauses:**` lines, so `CurrentOwnerRelations` gains no relations.
- The existing `ALetteredLevelHeadingInTheCampaignDocumentIsADeclaredOwner` test still finds `level-070` exactly once.
- `WorkshopSaveTests.cs:155` still finds `"part": "ball"`.
- Testing guidance: expectations are transcribed from section 5, and the tests use set, map and string assertions with no hash or float.
- Forward-only: no alias, placeholder or fallback.
- Disjointness with lanes 1, 2 and 4 otherwise holds as the contract states. No other lane edits the seven census documents or their headings.

Verdict: Incomplete
