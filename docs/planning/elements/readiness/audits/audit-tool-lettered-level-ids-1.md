# Planning audit 1: `spec-tool-lettered-level-ids.md` (lettered campaign level ids, lane 3)

Independent planning auditor (context-free), 24 November 2026, on the contract as drafted against `0959e883`.

**Verdict: Incomplete**

Two blocking text fixes stand between this contract and a Scoped Pass: finding 1 (a test that the next slice in this lane will break) and finding 2 (an acceptance check that conflicts with the ledger's convention). Finding 3 is a feasibility gap graded blocking. Everything else is non-blocking.

## Findings

**1. Blocking (test).** Location: I/O rows M1 and M2, the acceptance-to-test row M1/M2, Design Note 5 and the deferred-work bullet.
- **Defect:** The real-tree test inserts `LEVEL-069c`, one of the 94 planned lettered levels (section 6 of `campaign-coverage.md` lists `69a`, `69b` and `69c`). The control (M2) pins the real tree's `LEVEL-` members to exactly the 150 numbered ids: a count of today's documents, not one the feature requires (testing-guidance :61). The first slice that adds a lettered heading (OS-02) turns M2 red; if 69c is among its headings, the M1 insertion becomes a duplicate the set silently merges. Design Note 5 ("inserts a heading no document contains") stops being true.
- **Fix:** insert `LEVEL-069z` (outside the planned letters `a`–`e`); assert `Read(inserted).Members` equals `Read(unmodified).Members` ∪ {`LEVEL-069z`}; control: `Read(unmodified).Members` does not contain `LEVEL-069z` and contains every generated id `LEVEL-001`…`LEVEL-150`. Update Design Note 5. Whole-set equality also catches an alias that maps the lettered id to its base number.

**2. Blocking (wrong specified behaviour).** Location: AC4 with task 4 and the CA-01 bullet.
- **Defect:** The AC4 grep on `0959e883` matches `campaign-coverage.md:38` and also `completion-ledger.md:296`, CA-01's item text. Closed ledger rows keep their item text and change only the state column (CA-02 to CA-05, PD-46), so closing CA-01 normally leaves AC4 failing.
- **Fix:** scope AC4 to `campaign-coverage.md` and `campaign.md`; reword the CA-01 bullet to "state column set to done, citing this slice; the item text is unchanged".

**3. Blocking (test).** Location: AC2, the Verification command and the probe's census bullet.
- **Defect:** Nobody has shown that AC2's command runs on the current tree. `--oracles .` reaches `OwnershipOracles.Run` (`OwnershipOracles.cs:47`), which beyond the census runs `CheckCurrentPolicy` → `SourceInventory.Capture`, an msbuild Release `-target:Rebuild -p:BuildProjectReferences=false` of the Animation, main and test projects (`SourceInventory.cs:39–41,125–129`). Those compiles need Release outputs to exist already; earlier runs built the solution in Release first (`evidence-9a-3/tier1.sh:15,22`). This contract's verification is Debug-only and AGENTS.md forbids the local Release worker build. Completion-ledger CP-07 records that these tools have not run on this tree; 545 was re-measured by counting, not by running the command.
- **Fix:** run the command on the baseline before the first edit and record the result; if it fails at the baseline or needs a forbidden build, stop and return to planning, and the coordinator narrows AC2 to `--oracles` without a root plus the M1 real-tree test, or records the root run as an owned prerequisite.

**4. Non-blocking (latent product defect, unreachable today).** Location: `tools/Coverage/CurrentOwnerRelations.cs:43`, missing from the Code Map.
- **Defect:** The role regex `Original owner, all criteria and technical stage: ([A-Z0-9-]+)\.` cannot match an owner with a lowercase suffix (`LEVEL-069c`, `ENGINE-CORE-2d`); such a grant would silently fail. Unreachable today: `campaign.md` and `engine.md` contain no "Exact source clauses" line.
- **Fix:** record it in "Read, not changed" with its owner, or widen it here with a case.

**5. Non-blocking (test).** Location: M3 row and the Never clause.
- **Defect:** M3 asserts "contains"; an alias that adds both `LEVEL-001a` and `LEVEL-001` passes. M3 also calls `001` and `150` "first and last slot" boundaries although D4 says the grammar has no slot range.
- **Fix:** M3 asserts set equality with the fixture's expected set ∪ {that id}; the fixture declares no numbered `LEVEL-` heading; call the cases representative ids.

**6. Non-blocking (probe citation).** `LinkCardinalityNeverGrantsProofAndRepeatedExplicitRolesCoalesce` builds only `S269`/`S270` and never exercises the lowercase-suffix branch of `WorkOrderId`. Cite `CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles` (`:28–32`), which converts `ENGINE-CORE-2d` through `CurrentOwnerRelations.Read`.

**7. Non-blocking (probe classification).** The census is classed "present, untested" but no test is added. Reclassify it as a supplemental CLI check; its direct caller is `OwnershipOracles.cs:47`, reached from `Program.cs:17`.

**8. Non-blocking (handoff completeness).** OS-02's row (`completion-ledger.md:393`) does not mention adding lettered headings, and `campaign.md:9` says a lettered entry is added when it is authored. Reword the deferred-work entry to "every slice that adds a `LEVEL-` heading (first expected: OS-02) raises the census literal and runs `--oracles .`", and extend this slice's edit to OS-02's row.

**9. Non-blocking (disjointness completeness).** The other slices of the running lanes are omitted: part D (shares `completion-ledger.md`), the Baseball repair, S5b, S6 and S6b–S6g. No overlap with `tools/Ownership` or `Coverage.Tests` found. Add them.

**10. Non-blocking (traceability).** Cite `requirements.md:2527,3431` (typed level identity for content and navigation, the `PuzzleId` side) as related and still open.

**11. Non-blocking (ordering wording).** "the grammar refuses every spelling under which it would not be" is broader than true (`LEVEL-069C` would still sort correctly; it is refused as non-canonical). State that no code consumes this order.

## Verified as correct

- Every cited `path:line` holds on `0959e883`, including `:109` reading `LEVEL-[0-9]{3}`, the census literal at `:69`, and `<a id="level-070"></a>` at `campaign.md:896`.
- 94 lettered levels in section 6, all single letters `a`–`e`, 2a to 98a; D1 is sound.
- Census recounted at 545 (engine 61, gpu-physics 29, refinements 197, decisions 8, scope-corrections 28, current-consumers 72, campaign 150); 15 oracle attacks.
- No non-markdown file outside the two Ownership files mentions a `LEVEL-` id; `puzzles.json` ids are snake_case and play order is `presentation.order`.
- `Coverage.Tests` reaches the public `CurrentOwnerMembership.Parse`; its project globs new files; the registration filter does not select the new class.
- Every M4 spelling passes the heading filter at `:26` and is refused by `Identity` with the stated message.
- Engine tier with a correct reason; no fallback, alias, second grammar or element-specific assumption; ids stay typed (D3); ordinal order holds and two- and four-digit spellings are refused.
- No code overlap with S3, S5, parts B, B2, C or the policy-tool fixes; every required section present; 2,581 words.
