---
title: 'Story 9R.18: Independent Re-Verification and Dated Review Addenda'
type: 'chore'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r18-independent-re-verification-and-dated-review-addenda'
  - '{project-root}/_bmad-output/planning-artifacts/retro-verification-2026-10-18.md'
  - '{project-root}/docs/delivery-workflow.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Review evidence for 18 stories is not credible (retro-verification §D, report #7).
- The cat-062 raw output was copied into five reviews: `rg -F 14729.544167 _bmad-output/implementation-artifacts/` matches review-6-5e, 6-5f, 6-6, 6-7 and 6-8.
- Reviews cite phantom files: review-9-1 lists nine and review-9-3 lists four.
- review-9-2b gave PASS against contradicting code (G8).
- Counts are wrong: 6.9 reports 851/851, and 6.5b says 12 parts where its test asserts 24.
- review-6-9b quotes a grep that does not match the file.
- review-6-6 and review-6-10 claim an exact Reset that nothing asserts.
- Every original review in scope is by Murdoch: `review-*.md`, "Reviewer:" lines.

**Approach:**
- After Stories 9R.0–9R.17 land, a third reviewer identity re-runs each story's acceptance on the current tree, with its own raw logs.
- Each review record gets an append-only dated addendum.
- `sprint-status.yaml` moves a story to `done` only on a fresh terminal Pass.

## Boundaries & Constraints

**Always:**
- **Reviewer:** a distinct identity, "Quinn (retro re-verifier)". It must not be Murdoch, the story's implementer, or the implementer of that story's repair story. Record the identity in each addendum.
- **Raw logs:** every log is produced by this run and saved under `_bmad-output/implementation-artifacts/evidence-9r-18/<story>/`. Each log header carries the commit (`git rev-parse HEAD`), the bundle identity (the `Simulation.wasm` SHA-256, as in `evidence-9-4b/final/bundle-identity.txt`) and its own start and end timestamps.
- **Citations:** every quote is byte-identical to its file, and every cited path exists.

**Never:**
- Edit original review text.
- Reuse a log from another run.
- Mark `done` while an AC is owned by a later story.

</frozen-after-approval>

## Code Map

| Story | Record (`_bmad-output/implementation-artifacts/`) | Repair | AC-named suites (epics) | Owned later |
|---|---|---|---|---|
| 6.5b | `review-6-5b-dynamic-part-discovery.md` | — | cumulative | palette → 9A.14; `PartId` → 9A.15 |
| 6.5c | `review-6-5c-gltf-collider-extraction.md` | — (`.tscn` amendment) | cumulative | colliders → 9A.16; locators → 9A.17 |
| 6.5d | `review-6-5d-ball-obstacle-conversion.md` | 9R.11–9R.14 | cat-001, cat-004, cat-054, cat-066, cat-014 | fallbacks → 9A.16; cat-064, cat-003 → 12.1 |
| 6.5e | `review-6-5e-mechanisms-fixtures-conversion.md` | 9R.8, 9R.15–9R.17 | cat-015*, cat-023*, cat-062*, cat-063, cat-035, cat-022, cat-005 | fallbacks → 9A.16 |
| 6.5f | `review-6-5f-partart-partkind-legacy-purge.md` | — | cumulative | switch tables → 9A.16, 9A.17, 9A.25, 9A.27–9A.43 |
| 6.6 | `review-6-6-straight-pipe.md` | 9R.1 | cat-048a | — |
| 6.7 | `review-6-7-clear-pipe.md` | 9R.1 | cat-048b | — |
| 6.8 | `review-6-8-ball-detector.md` | 9R.9 | cat-002 | — |
| 6.9 | `review-6-9-pipe-bend-45.md` | 9R.6 | cat-049 | nudging (D2) |
| 6.9b | `review-6-9b-unbounded-collider-capacity.md` | 9R.2 | cumulative | `AbiOffsets` → 9A.1 (already deleted by 9.4b; 9A.1 verifies) |
| 6.10 | `review-6-10-pipe-bend-90.md` | 9R.6, 9R.7 | cat-050 | nudging (D2) |
| 6.11 | `review-6-11-funnel.md` | 9R.6 | cat-030 | air → 12.2; light → 13.1 |
| 8.1 | `review-8-1-battery.md` | 9.4b | cat-005 (amended: Powered Gate and Bumper store consumers) | Motor → 11.1 (owner, 18 Oct) |
| 8.2 | `review-8-2-powered-gate.md` | 9R.3 | cat-051 | CAT-051 Q6 (D3) |
| 9.1 | `review-9-1-pressure-plate.md` | 9R.4, 9R.5 | cat-052 | — |
| 9.2 | `review-9-2-pulse-counter.md` | 9R.1 | cat-020 | decode default → 9A.2 (9.4b D4 removed the default; 9A.2 adds the integer field) |
| 9.2b | `review-9-2b-modular-isolation.md` | — | cat-002, 020, 030, 048a/b, 049, 050, 051, 052 | → 9A.0 and Epic 9A |
| 9.3 | `review-9-3-system-clock.md` | 9R.10 | cat-017 | — |

## Tasks & Acceptance

**Execution.** Starts only when 9R.0–9R.17 are terminal.
- [R:9R.0] the build gate includes the analyzers.
- [R:9.4b] the cumulative baseline is restored.

1. [ ] `evidence-9r-18/environment.txt`: commit, bundle SHA, Chrome version, preview command (`tools/Preview` on :8060), and the start timestamp.
2. [ ] `evidence-9r-18/global/`:
   - `dotnet build CuriousContraptions.slnx` (0 warnings, 9R.0 gate);
   - `dotnet test CuriousContraptions.slnx` (100%);
   - all `tools/*.test.mjs`;
   - one full serial Chrome run of every `tools/e2e/*.test.ts` against the production preview (100%).
   
   Use one Chrome only: no concurrent run (memory: contention caused false failures).
3. [ ] **Per story, in table order.**
   - Re-run each AC-named suite in isolation into `evidence-9r-18/<story>/`.
   - Derive the expected outcomes from epics, then the element spec §5, then the story spec (retro source precedence). Build a criterion matrix (AC → evidence path → P/W/U/C/D → verdict).
   - For every false or unsupported claim, cite the report's §D row or the per-story table row.
4. [ ] **Per record:** append `## Addendum 18 Oct 2026+, retro-verification (<reviewer>, <date>)` containing:
   - the claims list (original claim, then the correction with a fresh evidence path);
   - the new verdict (Pass, Fail or Incomplete);
   - the owned-later clauses with their story;
   - the raw-log index.
5. [ ] `evidence-9r-18/integrity.txt`:
   - `git diff --numstat <base> -- <record>` for each record, showing 0 deletions;
   - a script check that every backticked path in each addendum exists and that every quoted log line occurs verbatim in its file.
6. [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml`. For each reopened story with a fresh terminal Pass and no owned-later clause, set `done` and add a comment citing the addendum. All others stay `in-progress`, with the owning story in the comment.
   - A weak-Pass story that is already `done` (6.6, 6.7, 6.8, 6.11, 8.2) is reopened to `in-progress` if its fresh verdict is Fail or Incomplete. The comment cites the addendum.

**Acceptance Criteria (1:1 with epics 9R.18):**
- **AC1.** Given each story whose Pass was invalid (6.5b, 6.5c, 6.5d, 6.5e, 6.5f, 6.9, 6.9b, 6.10, 8.1, 9.1, 9.2, 9.2b, 9.3) or weak (6.6, 6.7, 6.8, 6.11, 8.2), when its repair has landed, then a reviewer who is neither its implementer nor its original reviewer re-runs its acceptance against the current tree. The raw logs:
  - come from that run and are saved as files referenced by path;
  - carry the commit, the bundle identity and their own timestamps;
  - are quoted exactly;
  - and every cited file exists.
- **AC2.** Each `review-<story>.md` receives a dated addendum that lists each false or unsupported claim, the corrected result with fresh evidence, and the new verdict. The original is unchanged: `git diff` shows additions only (negative control).
- **AC3.** Where an AC is owned by a later story, the addendum names it (the table above), and the original stays `in-progress` until that story's closing addendum.
- **AC4.** Story 8.1 is re-verified against its amended acceptance: the Powered Gate and Bumper store consumers, with Motor moved to 11.1.
- **AC5.** Each done story with an open clause is recorded with its owner:
  - 6.9 and 6.10 nudging (D2);
  - 6.11 integrations (12.2, 13.1);
  - 8.2 CAT-051 Q6 (D3).
- **AC6.** The re-runs include every AC-named suite, one full serial Chrome run against the production preview, `dotnet build` (9R.0 gate) and `dotnet test`, all at 100%.
- **AC7.** `sprint-status.yaml` marks a reopened story `done` only when its addendum records a fresh terminal Pass.

## Design Notes

**Decisions (owner playability and correctness delegation):**
- **D1. Exclude the repair-story implementer as well as the original pair.** Rationale: AGENTS.md says self-review never qualifies, and the repair is the evidence under review.
- **D2. 6.9 and 6.10 nudging have no scheduled owner.** epics `:645` and `:662` say "not scheduled… stays open until then".
  - The addendum records the owner as "unscheduled: owner decision required".
  - CAT-049 and CAT-050 stay open, as their own ACs require.
  - **Blocking for those two clauses:** the owner must name a story.
- **D3. CAT-051 Q6 (activation pulse versus electrical-only).** Story 8.2's activation clause is **owned by Story 9.5** (Hold Timer, the activation→supply relay) and stays open until 9.5's closing addendum.
  - Story 9.4c's Switch → Latch → gate proof is cited as partial evidence only.
  - Rationale: the player opens a gate with a ball strike while CAT-051 stays electrical.
  - **Owner may confirm.**
- **D4. A story whose fresh run Fails** gets a Fail verdict, a finding returned to the repair owner and a re-review. Its addendum is appended again; it is never rewritten.

**Negative controls:**
- The integrity script must flag a planted fixture addendum with a missing path and a non-verbatim quote (run on a scratch copy).
- `git diff` deletions > 0 on any record fails the story.

**Mutation:** run the quote check on a copy of review-6-6 with its copied cat-062 block. It must report the reused `14729.544167ms` timing against the fresh log.

**Impacted suites:** every suite. This is a verification story with no code change.

**Risks:**
- The suite run is long. Use serial order, one Chrome and no concurrent publisher (memory).
- Repairs still pending (for example 9.4b cat-023b) leave dependent stories Incomplete. Report them; do not wait silently.

## Verification

- `git diff --numstat <9R.17-terminal> -- _bmad-output/implementation-artifacts/review-*.md`: deletions column all 0.
- `rg -c "Addendum 18 Oct 2026\+, retro-verification" _bmad-output/implementation-artifacts/review-{6-5b,6-5c,6-5d,6-5e,6-5f,6-6,6-7,6-8,6-9,6-9b,6-10,6-11,8-1,8-2,9-1,9-2,9-2b,9-3}-*.md`: 18 files, ≥ 1 each.

## Legacy Deleted

None. Review records are append-only.

## Implementation Notes

## Spec Change Log

## Review Triage Log
