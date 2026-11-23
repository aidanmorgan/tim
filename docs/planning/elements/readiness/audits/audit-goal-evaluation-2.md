# Planning audit 2: ENGINE-GOALS part 1 (`spec-engine-goal-evaluation.md`)

Phase: planning re-audit of the repaired contract. Checkout `/Users/aidan/dev/personal/tim/.claude/worktrees/spec-drafts` at `8d1d68aa`. Read-only apart from this file. Nothing was built or run; planned tests are not executed proof. "Contract:N" is a line of `_bmad-output/implementation-artifacts/spec-engine-goal-evaluation.md`.

## Verdict

**Incomplete**, as it would stand once the compiler prerequisite is met. Two findings remain: one seam defect that makes AC5 and AC7 unwritable as specified (R1), and one contradiction with the intent contract (R2). Both have exact text fixes below. With R1 and R2 applied verbatim, and the three small corrections S1 to S3, the verdict is **Scoped Pass, blocked only on the compiler decision**; the fixes are mechanical and need no third audit, only a coordinator read.

The intent contract (Contract:17-58) is now sound and can become read-only.

## Status of the first audit's findings

| Finding | Status | Evidence |
|---|---|---|
| B1 presentation-sample travel lane | Resolved | Contract:38, :88, :121, :169, :187, :275 |
| B2 no route to a multi-condition authored level | Partly resolved: see R1 | Contract:146, :172, :257 |
| B3 pinned compiler | Correctly held as an open owner decision | Contract:64, :153, :161 |
| B4 ordered-events acceptance unowned | Resolved in intent and split; one stale line: see R2 | Contract:40, :73, :182 |
| B5 TypeScript consumers | Resolved | Contract:93, :178, :248-249, :281 |
| B6 travel occurrence time | Resolved | Contract:113, :144, :266 |
| Should-fix list | Resolved, except the role-string reason: see S3 | Contract:99, :166, :206, :213-221, :264-266 |

## Remaining blocking findings

### R1. The definition-scoped `Create` and `WithPrecision` still re-enter the installed catalogue (product defect in the plan)

- **Location:** Contract:146 and :172; code `engine/gpu/PuzzleCatalogue.cs:42,81`, `engine/gpu/WorkshopConstruction.cs:17-25`, `engine/gpu/WorkshopPuzzle.cs:81-91`.
- **Defect:** both method bodies end with `construction.Validate(supply, bundle)`. That calls `Puzzle.Validate` (`WorkshopConstruction.cs:24`), which calls the static `PuzzleCatalogue.Validate` (`WorkshopPuzzle.cs:90`), which resolves `Definition(puzzle.Id)` from the installed catalogue. Moved verbatim to `PuzzleDefinition`, they still validate against the installed level, not the definition under test.
- **Consequence:** for `ReadDefinitions(unfamiliar json)[id]`, a new id is refused "Unknown or unavailable puzzle", and an installed id with changed goals is refused "Authored goal changed". AC5 (create at another identity, validate, re-create at another precision) and the "before placement the goal is Waiting" half of AC7 cannot be written as the table names them. The implementer would return the contract to planning.
- **Exact fix.**
  - Add to owned scope (after Contract:82): "`engine/gpu/WorkshopConstruction.cs:17-25` -- split `Validate` into an internal `ValidateStructure(supply, bundle)` (revision, settings, atmosphere, instances, connections) followed by the puzzle check."
  - Append to Contract:172: "The definition-scoped `Create` and `WithPrecision` end with `construction.ValidateStructure(supply, bundle)` and then this definition's own `Validate(bundle, puzzle, construction)`; they never call `WorkshopConstruction.Validate`, which re-enters the installed catalogue (`WorkshopConstruction.cs:24`, `WorkshopPuzzle.cs:90`). `WorkshopConstruction.Validate` is `ValidateStructure` plus `Puzzle.Validate`, so the installed path runs the same two steps in the same order: one path."
  - Append to Contract:146: "No definition-scoped method resolves `PuzzleCatalogue.Definition`."
  - Add a control to the AC5 row: "the same reordered definition under an id the installed catalogue does not hold creates and validates" (this is the case that fails today).

### R2. A recorded decision contradicts the intent contract (wrong specified behaviour, text only)

- **Location:** Contract:136, against Contract:40 and :73.
- **Defect:** Contract:136 still says "Any-of, sequences and forbidden events belong to EL-120 to EL-123." The intent and the split now give them to `engine-goal-ordered-events`. A child reading the contract fully sees two owners.
- **Exact fix:** Contract:136 becomes "**All-of only.** Every planned multi-goal level is a conjunction. Any-of, ordered sequences and forbidden events are owned by `engine-goal-ordered-events`; quantity, rate-window and protected-state goals remain EL-120, EL-121 and EL-123."
- **Same repair, two stale counts:** Contract:181 "the two follow-up slices" becomes "the three follow-up slices"; Contract:209 "owned by the two named slices" becomes "Supplied occurrence, progress observation and ordered events are owned by the three named slices."

## Should fix in the same edit (not blocking)

### S1. Make the cue wiring compiler-enforced (closes the narrowed AC3 gap)

- **Location:** Contract:168-169; code `engine/gpu/WorkshopGoalEvaluator.cs:27`, `engine/gpu/BrowserWorkshopClient.Ui.cs:141`.
- **Defect:** if the presentation-sample entry point takes the travel read as an optional parameter, forgetting it at `Ui.cs:141` compiles, and no installed level has a travel condition to expose it.
- **Exact fix:** append to Contract:169: "The presentation-sample entry point takes the `WorkshopPresentationSample` itself (`Occurrence(goal, sample, identities)`), with no optional lane parameter, so `Ui.cs:141` cannot omit an observation kind."

### S2. Do not narrow M7 further than the catalogue forces

- **Location:** Contract:257, :270.
- **Defect:** undefined kind, non-finite or out-of-range minimum, wrong fields and duplicates are refused by the verifier or pre-validation before any catalogue lookup (`GoalSignalSelectorTests.cs:32-45` shows `WorkshopSaveCodec.Decode` returning `OutOfRange` for a forged goal on an installed level). Those cases can use the full `WorkshopSaveCodec.Decode` on a forged multi-condition record. Only "differs from the authored goal" needs the definition-scoped `Validate`.
- **Exact fix:** in Contract:257 replace "M7 and M9 are narrowed to the record mapping ... installed catalogue" with "the malformed-record cases of M7 and the three old-layout fixtures run the full `WorkshopSaveCodec.Decode`, because they are refused before the catalogue is consulted; only the changed-goal cases of M7 (dropped, added, reordered condition) and the M9 round trip use `OpenFrame` (the verifier), pre-validation, `MapConstructionRecord` and the definition-scoped `Validate`, because `Decode`'s last step validates against the installed catalogue."

### S3. Correct the role-string deferral's reason, sites and session check

- **Location:** Contract:147, :182, :242.
- **Defect 1:** the stated reason, "typing them changes the bundle declarations of element packages", is not supported: no file under `elements/` names either role or `NamedTarget` (`rg -n "puzzle_target|interaction_target|NamedTarget" elements` has no hit). What typing changes is the shared `TargetSelector` declaration contract, `BundleValidator.cs:894-902` and two engine sites.
- **Defect 2:** the second site, `engine/gpu/WorkshopPhysicsCompiler.cs:668`, is not cited.
- **Exact fix:** Contract:147 becomes "The role strings `"puzzle_target"` and `"interaction_target"` (`CompiledIdentityMap.cs:105-107`, `WorkshopPhysicsCompiler.cs:668`) are outside the affected scope. They select which bodies a declared sensor admits; this slice calls `TryCapture` exactly as today and changes neither site, and no delivered package declares either role. Typing them changes the shared `TargetSelector` contract and `BundleValidator`. This slice records the closed-set defect in `deferred-work.md` with both sites; destination: the first slice that changes `TargetSelector` or residence target selection (unassigned until then)."
- **Defect 3:** Contract:242 expects `flatc --version` to print 25.2.10, but under the default decision (S5) the pinned binary lives in a per-user cache and the `PATH` binary stays 25.12.19. Fix: "the pinned compiler chosen by the owner decision reports 25.2.10 (under S5, the cached binary, not `flatc` on `PATH`)."

## Questions put by the coordinator

### Do the narrowed proofs leave a required-now gap?

**No, once R1 and S1 are applied.** Reasoning:

- **Cue (AC3).** The slice builds three things: the sample lane, its two fills in `WorkshopPoseHistory`, and the evaluator's use of it. The planned test runs the real pose history and the real evaluator entry point with a capture plus `TravelReached` goal, so all three execute. The cue machinery itself (`PumpGoal`, the animation request, `TryUiGoalSample`) is shape-agnostic: it consumes one nullable occurrence, and it is executed end to end on the 15 installed levels, which after conversion run through the new list model. The only unexecuted piece is the one call at `Ui.cs:141`; S1 makes the compiler enforce it. Contract:281 records the limit honestly.
- **Save (M7, M9).** Every stage of `Decode` (frame and verifier, pre-validation, record mapping, authored-goal equality) is executed on a multi-condition goal; only the static `Definition(id)` lookup is replaced by the definition under test, and the composed `Decode` is executed on installed levels and, with S2, on malformed multi-condition records.
- **Without R1** there is a gap: AC5 and AC7 have no executable route.
- **Carry-forward to record.** The first `TryUiFrame` run with a travel condition falls to a data-only level slice. Add one `deferred-work.md` entry in task 13 (Contract:182) so that slice's planning reads it: "first end-to-end Solved cue with a `TravelReached` condition: add a `TryUiFrame` assertion to the level-14 slice; a failure there is an engine defect and returns to planning."

### Is the role-string deferral acceptable?

**Yes, with S3.**

- **Element-free review policy.** Section 1 limits the gate to the affected scope and says to trace actual dependencies rather than file membership. The strings select sensor admission, not goal behaviour; the slice's use of `TryCapture` is unchanged (`PuzzleDefinition.cs:169`, `WorkshopGoalEvaluator.cs:32`); and no delivered package declares either role, so the branch is not reached by current content. They are role names, not a product identity selecting a physical rule, so this is not a confirmed element-specific assumption in scope, and section 3's bar on deferring one does not apply.
- **Typing rule.** AGENTS.md says existing violations "remain required refactoring", which permits a tracked deferral. `deferred-work.md:147-149` is direct precedent: a closed-set typing violation with destination "the first slice that changes ... (unassigned until then)".
- **Residual risk.** The 4 November repair policy lets a step-04 reviewer raise it as a confirmed finding. The corrected Contract:147 is the recorded, evidence-backed scope refutation triage needs; without S3 the stated reason is wrong and would not survive that challenge.

### Other author decisions

- **Motor observer owned by the level-14 slice:** acceptable; it is package declaration data and data-only tier. That slice changes a delivered package, so it is not parallel-safe with any slice touching the Motor package and must re-run Motor's existing tests. This belongs in that slice's contract, not here.
- **Compiler prerequisite as an open owner decision defaulting to "blocked on S5":** correct handling. It adds no dependency without approval.
- **Fourth part `engine-goal-ordered-events`:** correct; it carries the story acceptance verbatim and keeps ENGINE-GOALS open (Contract:73, :182).

## Decisions for the owner

1. Compiler provisioning: S5 first (default) or a one-off pinned download.
2. Acknowledge the four-part split and that ENGINE-GOALS stays open until `engine-goal-ordered-events` is published.

## Evidence limits

- No build or test was run. I did not re-open all 32 goal-naming test files.
- I did not check whether `SaveFixture.Forged` can encode a multi-condition goal on an installed level's construction without edits; Contract:98 already lists `SaveFixture.cs:100-137` as adapted.
- The level counts at Contract:75 and the "about 3 s" rotor claim at Contract:255 remain unverified.
- The role-string search covered `elements/`, the abstractions, the bundle project and `engine/` only.
