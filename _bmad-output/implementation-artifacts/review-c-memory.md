---
title: "Review — C-Memory (9A.41 + 9A.43), independent adversarial review"
candidate: "uncommitted worktree story-c-memory on base fc7f7cac; tracked diff incl. staged deletions (git diff HEAD -- . ':(exclude)_bmad-output' ':(exclude)TODO.md') sha256 0dad2767164dd6f97114b6baaf8bd67b6c28910da22054c82e7ad7bf9459eeb1 at the start and at the end of review; MemoryGuardTests.cs 89abbef3…, DeclaredMemoryTests.cs 39845d70…"
verdict: "Incomplete (F1, documentation only). Every native implementation criterion passes independently. After F1 is fixed and origin/main (cf35a064) is merged, the expected result is Pass-pending-merge, then a scoped native Pass on re-review. Browser qualification is pending until the post-catalogue E2E."
date: 2026-10-24
---

# Verdict

**Incomplete.** I reproduced the conversion independently on the production C# simulation.

**Identity at both ends.** I worked in two throwaway worktrees: base fc7f7cac with the record-path adapter, and fc7f7cac with the candidate diff applied.
- Both worktrees produced every trace byte-identical to the implementer's baseline: 9 fixtures with nodes, edges, bodies, colliders, supply, the admission hash, per-tick bits, Reset, replay, ring records and ring bytes.
- They also produced byte-identical 12×3 lesson worlds and 12×3 placed solutions.
- At base, the reviewer-regenerated base-encoder old saves (86 files) match the implementer's sha256 pins.
- On the candidate, those old saves give 6 loads (latch, locked rotated latch and pipe, each at both boundaries) and 80 typed refusals. There are 0 untyped refusals.
- The suite-first `MemoryGuardTests` (21) and `LatchSupplyFixtureTests` pass on the record path, and pass unchanged on the candidate.

**Counter, ring and latch.** `ThresholdReached` is checked with an exact equality on (Tick, Source, Count, Other) and the Ordinal:
- the tick comes from the committed count increments of the same run;
- there is none at N−1 and none after saturation;
- targets 1, 3 and 9, and the clock-driven targets 1 and 9, are all covered.

The latch tests cover:
- Set, Reset, and same-tick Reset dominance;
- one source wired to both inputs;
- the level `ActivationOut` and the lamp;
- `MemoryChanged` edges;
- occurrence consumers refusing the level link.

**Whitelist.** The type whitelist is fully deleted. Typed validation now comes from declarations, and I found no data-boundary gap (probe below).

**Wire.** Pinned flatc 25.2.10 (sha256 f004709c…) reproduces the committed base output and the candidate C#/TS. The only difference is the pre-existing blank line in the C# header.

**Ratchet.** Known goes from 127 to 94: −33 (Counter 19, Latch 14), +0. Generic goes from 533 to 519: −16 stale LatchMode/Counter rows, +2 `MemoryNode` rows.

**Deletions.** The deletion greps return 0 production hits.

**Checks.** All of these are green: the build, the full suite, the CI tools step, the node harnesses, the Release workers, strict tsc and the snapshot unit.

**Blocking: F1, documentation only.** The current 9A.43 acceptance contradicts the accepted rule (details under F1).

I edited no source, test, tool or content file. I wrote only this record and `evidence-c-memory-review/`. Both throwaway worktrees (`scratchpad/rev-wt`, `scratchpad/rev-base`) have been removed, and there are no crash dumps.

# Findings

**F1 (Medium, blocking: a current document contradicts the accepted behaviour).**

The current 9A.43 AC in `_bmad-output/planning-artifacts/epics.md` (Story 9A.43, Acceptance Criteria) reads:

> the generic `Clear` command is admitted for any instance that declares the memory capability and rejected for every other instance

The coordinator ruling of 24 Oct 2026 accepted a different rule: the declared capability decides, and level-wired lamps and plates (LevelIndicator, ContactLoadSource) are admitted. The implementation and CAT-037 follow the ruling. The spec's *Coordinator decisions* item 2 still reads "Level lamps and plates are not bistable, so they are rejected by that rule". It carries no supersession note; only *Current state* discloses the deviation.

The AC also names a "generic `Clear` command" that does not exist. D5 adds no command: what is admitted or refused is a committed Latched→Clear flip, checked by read validation.

Fix in the merge pass:
1. Amend the epics AC to the accepted rule. A committed Latched→Clear flip is admitted for StateLatch, LevelIndicator and ContactLoadSource nodes, which are the kinds a declared level capability compiles. Every other node is refused with `Faulted`/`InvalidRead`.
2. Annotate spec decision 2 as superseded by the 24 Oct ruling.

**F2 (Low, record accuracy).** The recorded candidate identity `44b85353…` is `git diff`, which covers unstaged changes only. It omits the four staged deletions (`WorkshopCounter.cs`/`.uid`, `WorkshopLatch.cs`/`.uid`); the content is otherwise identical. Use `git diff HEAD …` (`0dad2767…` here) for the frozen snapshot.

**F3 (Low, player-visible text).** `CounterElementDeclaration` Metadata `Description` still says "At three it emits once…". The target is now editable from 1 to 9 through the generic editor (the 9.2 retro #13 editor). It should say something like "at its target (3 by default)". Non-blocking.

**F4 (Low, informational, not a regression).** `WorkshopInstances.Validate` admits any in-process `IWorkshopInstance` implementation. This is equivalent to base, whose whitelist admitted any `IWorkshopDeclaredColliders`. Folding `DeclaredColliders` into the interface makes the colliders a compile-time requirement.

My probe used a foreign type with `PartId.Counter`:
- `Construct` → `Faulted/InvalidRead`;
- `Compile` → `ArgumentException`;
- the save round trip is refused as typed `OutOfRange`.

No decoder can produce such a type.

**F5 (Info, ruling 1).** `Faulted`/`InvalidRead` is accurate: a device read that flips a committed latch is a read-integrity fault. It is not misleading, and a more specific closed reason is not warranted.

**F6 (Info).** `WorkshopActivationCompiler.MemoryNode` is a public helper whose only production caller is `Compile`, which validates the construction first. If it is called directly with a parameterless counter it throws `KeyNotFoundException`. Its public surface is only test-reached.

# Criterion / impact matrix

| Criterion | Expected (derived) | Evidence | Result |
|---|---|---|---|
| D1 capabilities | `PulseCount("target_count")`, `Memory(SetIn, ResetIn)`. Exclusive; validator edges. `SignalLatchMode`/`LatchMode`/`Counter`/triggers deleted, no alias | diff; `BundleValidatorTests`; R4/R5/R8 killed | Pass |
| D2 compile | Same Counter/StateLatch node in the same position; target = parameter | `DeclaredMemoryTests` 1/9; traces identical; R3 killed (18 red) | Pass |
| D3 parameter | WholeNumber 1–9, default 3, Reject/BuildOnly at every boundary | `ConstructionDecodeBoundaryTests` matrix (0, −0, 2.5, −1, NaN×3, ±∞, 9.5, 10, …); `CounterTests`; probe Construct 0/10/2.5/NaN/missing → `InvalidConstruction`; editor test; R6 killed | Pass |
| D4 whitelist | Type check and `IWorkshopDeclaredColliders` deleted; length encoded only for the Pipe | diff; M8; F4 probe; R7 killed | Pass |
| D5 admission | 3 admitted / 6 refused / unknown refused, each reaching the guard | M4 (6 refused rows red); R1 (LevelLamp red); R2 (Plate red) | Pass (doc: F1) |
| D6 wire/save/content | `target_count` gone; 8 fields; regeneration exact; 28 content lines | flatc regeneration; numstat 0/28, all `"target_count": 0`, JSON parses; stale key refused; old saves 6 identical / 80 typed / 0 untyped | Pass |
| Forged data | Counter with no vector / empty / duplicate / extra / wrong key / 0 / 10 / 2.5 / NaN, and a latch with a target, each at both boundaries | `probe/forged-boundaries.txt`: all typed (`OutOfRange`; save NaN `NonFinite`); controls decode | Pass |
| D7 ratchet | Known −33 exactly, +0 | Remaining 94 are all FinalIsolation: 42 PartId catalogue rows plus Pipe/tube-family rows. This is not literally "all Pipe" | Pass |
| 9.4c guard | `LatchSupplyFixtureTests` green on base and candidate; fixture changes only through the adapter | base worktree 43/43; candidate full suite | Pass |
| Lifecycle | Reset exact, new epoch, sequence restarts at 1, save/load replay | `MemoryGuardTests.ResetIsExact…` | Pass |
| No element branch (ruling 3) | Capability-only dispatch | No production `PartId.Counter`/`Latch` outside `PartId.cs` | Pass |
| Process (sed edits) | Content verified | `DeclaredOrientationTests` comment hunk correct; `MemoryGuardTests` = base copy; implementer mutants restored (frozen = current, 6 files); `test-files.sha256`: only attributed files changed | Pass |
| Docs | Current docs match the behaviour | CAT-020/037 and the isolation doc are consistent; epics 9A.43 AC is not | **F1** |

# Commands and results (`evidence-c-memory-review/`)

- **Candidate worktree.**
  - `dotnet build CuriousContraptions.slnx -c Debug`: 0 warnings, rc 0. This was an up-to-date incremental build.
  - `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore`: 17/176/77/190/2775, 0 skipped, rc 0.
  - Files: `build.log`, `full-test.log`.
- **Throwaway (independent build).** The build compiled from source. The suite gives 2775 game tests green. `TypeCheckTests.EverySuiteAndSharedModuleTypeChecks` failed only because `/tmp` has no parent `node_modules/flatbuffers`; this is environmental, and the test passes in the candidate worktree. Files: `throwaway-*.log`.
- **CI "Test C# tools", verbatim under bash.** 77/176/17/174, rc 0. File: `ci-tools.log`.
- **Node harnesses.** `node --experimental-vm-modules --no-warnings --test tools/*.test.mjs` (the flag is required): 11/11 files rc 0. File: `node-harnesses.log`.
- **Release builds.** Simulation and Animation.Worker: 0 warnings, rc 0. File: `worker-release.txt`.
- **TypeScript checks.** Strict tsc 6.0.3 rc 0; snapshot unit 24/24; `git diff --check` clean, and untracked tests have no trailing whitespace. File: `ts-checks.txt`.
- **Deletion greps.** File: `deletion-greps.txt`. The only hits are in tests:
  - the `PlacedParts` doc comment;
  - a `DeclaredMemoryTests` message literal;
  - the `BundleRoundTripTests` refusal literal `CounterReached`.
- **Mutants:** `mutation/R1–R8.log`, all killed. The first R6 attempt failed to compile (IDE0060) and was rerun in a compiling form. I read the implementer's M7 (`InstanceRecordFields` 8→9): the boundary-slot test and the parameterless old-save row both went red.
- **Old saves, base against candidate decode.** Pipe LOADED lines are identical. Each latch keeps the same cell, local, rotation and lock, with `params=[]`.
- **Untyped consumers.** I searched outside C# (`.js`, `.mjs`, `.html`, `.ts`, `.json`) for `target_count`/`targetCount`/`TargetCount`. The only hits are the updated `cat-020.test.ts` and an unrelated `SceneAnimationAdapter.TargetCount` ownership row. Every raw vtable literal `__offset(this.bb_pos, 18|20)` sits in other tables in the generated head of `workshop-wire.ts`.
- **Identity:** `identity-captures.txt` and `candidate-identity.txt`.

# Merge (Pass-pending-merge conditions)

origin/main = `cf35a064` (fc7f7cac + 10.1 solid cylinder). Text overlaps are `BundleRoundTripTests.cs`, `epics.md`, `sprint-status.yaml` and `TODO.md`. The main delta has no hits for the deleted symbols.

After merging, re-verify:
- the build and full suite;
- the Known/Generic ratchet, because the cylinder code adds scanned files;
- the bundle identity, which main changed;
- the regeneration;
- the identity probe, re-run with base = main on the record path against the merged candidate.

The 6.5f and 9.2 closing addenda are deferred to terminal verification.
