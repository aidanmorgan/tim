# Planning audit, pass 2: spec-early-level-proof-batch-1.md

Checkout: `/Users/aidan/dev/personal/tim/.claude/worktrees/spec-drafts` at `8d1d68aa`. Contract: `_bmad-output/implementation-artifacts/spec-early-level-proof-batch-1.md`, 4,474 words (budget 6,000). Phase: planning. Nothing was built or run; tests named in the contract are planned, not executed.

## Verdict: Incomplete

One blocking finding remains. It is a records gap of the same kind as B5: three count statements the slice makes false are not in owned scope or task 6. Everything else from pass 1 is repaired and verified. Once the fix below is applied as written, no other section needs re-audit.

## Blocking finding

**R1. Three stale count statements are outside owned scope and task 6. Classification: wrong specified behaviour.**

Contract lines: `:76` (owned scope) and `:164-170` (task 6).

Defect. Task 6 now gives exact replacement text, so an implementer following it leaves these lines untouched:

- `docs/planning/invest/delivery-batches.md:194` reads "`content/puzzles.json` holds 63 levels: 15 `Playable` and 48 `Planned`." After the slice the file holds 19 and 44.
- `docs/planning/invest/delivery-batches.md:210` reads "**Provable now (12), in campaign order.**" The table under it will list 11.
- `docs/planning/invest/vertical-delivery.md:34` reads "Of the 45 such levels planned today, 12 have a single captured or activated-after goal and can be proven now; the other 33 wait for `engine-goal-evaluation` (21 November 2026 audit)." This file is not in owned scope at all. I did not name it in pass 1; that was my miss.

Consequence. After the slice, section 4 says 15 Playable at `:194` and 19 at `:282`, and the roadmap that governs order still says 12 and 33 while `delivery-batches.md` and `epics.md` say 11 and 34. A reviewer will raise each as a confirmed finding, and the roadmap line cannot be fixed without a scope change.

Fix 1, contract `:76`. Replace the path list's first two items with:

`docs/planning/invest/delivery-batches.md:194,197-198,210,214-215,219,282`, `docs/planning/invest/vertical-delivery.md:34`,

Fix 2, contract task 6. Add three bullets (texts for N = 4; the counts follow N as the task already says):

- `docs/planning/invest/delivery-batches.md:194` -- "15 `Playable` and 48 `Planned`." becomes "19 `Playable` and 44 `Planned` (15 and 48 before early level proof batch 1; the counts below are of those 48)."
- `:210` -- "**Provable now (12), in campaign order.**" becomes "**Provable with single-goal evaluation (11), in campaign order.**"
- `docs/planning/invest/vertical-delivery.md:34` -- "12 have a single captured or activated-after goal and can be proven now; the other 33 wait for `engine-goal-evaluation` (21 November 2026 audit)" becomes "11 can be proven with a single captured or activated-after goal (21 November 2026 recount): levels 03, 05, 12 and 61 are proven natively with browser qualification pending, seven remain, and the other 34 wait for `engine-goal-evaluation`".

Fix 3, contract `:150`. Add `docs/planning/invest/vertical-delivery.md` to the shared files list.

## Pass 1 findings: status

| Finding | Status | Evidence |
|---|---|---|
| B1 exit for an unprovable level | Fixed | `:32`; carried through `:162`, `:164`, AC1, AC5 and the test delta |
| B2 Never line against owned edits | Fixed | `:35`; "lesson" removed from `:26` |
| B3 tier undecided | Fixed | `:55` records engine tier with the reason; no data-only claim remains |
| B4 AC1 against the re-tune rule | Fixed | `:173`, `:225` (literals from the Code Map table) |
| B5 `delivery-batches.md:282` | Fixed | `:167`; arithmetic 19 + 7 + 34 + 3 = 63 holds; line numbers 197, 198, 214, 215, 219, 282 checked against the file |
| B6 second-control theory | Fixed | `:161`: source of the pose, precision 0, 120 Hz, 1,200 ticks, the four one-field copies |
| B7 task 6 not actionable | Fixed except R1 | sprint key and `review # ...` comment match the existing convention (`sprint-status.yaml:275`); `TODO.md` has an **Outcome** paragraph; `epics.md:4560` text given |
| N1 to N8 | Fixed | `:116`, `:71`, `:135`, `:122`, `:121`, `:205`, `:55`, `:161` |

## The three stated departures

- **L05 boundary dropped.** Accepted. The row now asserts only "never captured within the guard at precision 0", which is consistent with `:214-215`. The control still differs from the solving row in one field.
- **`air_mail` takes the Ramp row as `secondary`; one-kind levels keep zeros.** Accepted. It follows `wall_and_bumper`, and only projection reads the field (`engine/gpu/WorkshopPuzzle.cs:79`).
- **`oversized` warning kept and explained.** Accepted. `:55` states the template marker and that the 6,000-word budget governs.

## Non-blocking wording (apply with R1 or leave)

- `:160` (task 2) and `:174` (AC2) still say "the four ids" and "the four levels". Under `:32` they are "the ids of the levels proven" and "the proven levels". The intent's rule already governs, so this is tidiness only.

## Checks that still hold from pass 1

- All four levels use only delivered parts and a single `Captured` goal the engine evaluates today (`engine/gpu/WorkshopGoalEvaluator.cs:27-36`, `engine/gpu/PuzzleDefinition.cs:167-171`).
- The orientation reading and the two quaternions at `:116` agree with the 9A.26 conversion in git history (`2f5650b6^:content/puzzles.json`) and with the Fan's local +X flow direction (`elements/Fan/FanElementDeclaration.cs:47`). No current code reads the planned `orientation` field.
- The capability probe's 11 symbols and tests exist and exercise what is claimed.
- Proof per level is the real C# simulation, two one-cause controls, Reset and save/load at three cadences and three precisions, with the stall guard and tolerances chosen now.
- Test delta is +8 per level proven (+32 with four), in `CuriousContraptions.tests` only.
- Publication beside element slices and beside `spec-engine-goal-evaluation` is stated, and that draft agrees (`spec-engine-goal-evaluation.md:143`).

## Owner

- No decision is open for this contract. Engine tier needs no ruling.
- For information: the allow-list in `_bmad/custom/bmad-loop-protocol.md:54-60` does not list `TODO.md`, which every slice edits. Until it does, no slice can be data-only by the letter.
- For information: the slice corrects the 21 November figure from 12 to 11 provable-now in three planning documents.

## Evidence limits

- No level was simulated. Whether the four solutions solve at every cadence and precision is unknown; `:32` now covers the case that one does not.
- The orientation convention is confirmed against git history and geometry only.
- `spec-engine-goal-evaluation.md` was read for its content shape and ordering statement, not audited.
- `CuriousContraptions.tests/PalettePlacementTests.cs:1256` was not opened to confirm how its picker index is computed.
