---
title: 'Story 9R.16: Signal Lamp suite cat-035 (reopened Story 6.5e)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: ''
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9r16-signal-lamp-suite-cat-035-reopened-story-65e'
  - '{project-root}/docs/planning/elements/CAT-035-lamp.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9r-15-cat-063-impact-switch-suite.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem (report #8):** `tools/e2e/cat-035.test.ts` never existed. The lamp has two compiled kinds, chosen per construction (`ActivationNetwork.cs:706-709`):
- `StickyFirstInput`: the first input wins until Reset (`:9-12`, `:443-445`);
- `LevelIndicator`: follows exactly one State Latch (`:145-154`).

Neither kind's behaviour, nor the refusal of a second signal into a level lamp, is proven by an element suite.

**Approach:** Add one free-workshop suite following CAT-035 §5:
- switch → lamp, sticky;
- latch → lamp and plate ActivationOut → lamp, both level, on then off;
- the unwired control;
- the second-signal refusal on a level lamp;
- exact Reset and Save/Load.

## Boundaries & Constraints

**Always:**
- Common 9R rules:
  - Chrome proof uses actual UI controls through `workshop-driver.ts`.
  - Committed reads arrive only on command acks.
  - Exact Run/Reset uses the Story 9R.1 snapshot. Its contract: the tick-0 read from the last Construct or Load ack plus the authored bytes. Every body's cell, local, rotation, com, velocity and angularVelocity, plus the captures, activations, timers and electrical rows, are deep-equal after Reset (model: `cat-062b.test.ts:226-231`).
- Depends on Story 9R.5: the plate's ActivationOut is a level, and the lamp-kind selection is widened from `source is WorkshopLatch` to level sources. This story does not implement it. If 9R.5 has not landed, the story is blocked.
- A refusal is atomic: no Applied Construct, and the authored bytes are unchanged.

**Never:**
- No electrical input on the lamp (CAT-035 §2 forbids fabricating one).
- No any-target predicate.
- No setters.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Sticky | switch ActivationOut → lamp; qualifying hit | lamp latched at the switch's occurrence; target one activation; stays lit to run end | N/A |
| Latch level | switch → latch SetIn, switch → delay → latch ResetIn, latch → lamp (cat-037 recipe) | lamp on at Set, off at Reset (committed latch and lamp phase at Pause acks) | N/A |
| Plate level | plate ActivationOut → lamp; ball rests then rolls off | lamp on while loaded, off after release (9R.5 hold 0.1 s) | N/A |
| Unwired | lamp alone near a hit | lamp Clear; no target sample | N/A |
| Second signal | level lamp already wired from a latch; connect the switch ActivationOut → the same lamp | no Applied Construct; bytes unchanged | atomic reject |
| Reset | after any lit run | snapshot equal; bulb slate, emission off | N/A |

</frozen-after-approval>

## Code Map

- `engine/gpu/ActivationNetwork.cs:9-12`, `:145-154`, `:415-445`, `:555-570`, `:700-709` -- lamp kinds, level edge rule, sticky fan-out, level follow, kind selection.
- `engine/gpu/WorkshopSimulation.cs:79-90`, `:420-440` -- bistable-node compile and admission (where a bad edge surfaces today).
- `ui/WorkshopConnections.cs:77-112` -- the lamp ActivationIn choice is always offered, then `ChangeConnection` → `Validate` → `SubmitConstruction`.
- `engine/gpu/WorkshopActivationParts.cs:25-35` -- `WorkshopLamp`. `parts/scenes/goal_light.tscn` -- the bulb.
- `tools/e2e/cat-037.test.ts:63-120`; `tools/e2e/anim-1b.test.ts:13-14`, `:76-77` -- the latch recipe, target ids, the unwired-lamp control.

## Tasks & Acceptance

**Execution:**
- [ ] Investigate first: wire a second source into a level lamp through the real UI and record the outcome. If the refusal happens only at Run admission (or as a fault) rather than at Construct, add a construction-level rejection. Write a native test first (`WorkshopConnections` and construction validation) so the edit is refused atomically before submission.
- [ ] `tools/e2e/workshop-driver.ts` -- reuse `connectFrom`, `expectNoCommand` and `readCommittedAtPause` (9R.10, 9R.13, 9R.15). Add `expectRejectedConstruct(action, reason)` only if the refusal surfaces as a Rejected ack.
- [ ] `tools/e2e/cat-035.test.ts` (new) -- the matrix cases, each from a fresh page.

**Acceptance Criteria:**
- Given a lamp wired from an Impact Switch through real UI, when the switch fires, then the lamp lights on that first input (committed lamp latch equals the switch occurrence) and stays lit until the run ends.
- Given a lamp wired from a State Latch, or from a Pressure Plate level output (9R.5), when the level goes on then off, then the lamp follows on and off.
- Given an unwired lamp, then it stays dark (control).
- Given a level-following lamp, when a second signal wire is attempted, then it is refused atomically (negative).
- Given any case after Run, when Reset, then the read equals the pre-Run snapshot (Slate). When Save, reload and Load, then the wires are byte-identical and the behaviour replays.
- `cat-035` passes 100% serially in Chrome.
- `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

**§5 map (CAT-035):**
- Construction: AC1.
- Positive: AC1 and AC2.
- Negative/control: AC3, AC4 and the duplicate trigger. Missed impact and disconnected wire are covered by 9R.15.
- Run/Reset and Save/Load: AC5.
- Integrations: Switch (here and 9R.15), Delay (9R.17), Domino (`cat-023b`), Counter (`cat-020`), latch (`cat-037`), plate (9R.5).
- Other lamp levels: Incomplete (not admitted).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

**Decisions (owner playability delegation):**
- "Refuses a second signal" means refusing a second incoming wire on a level lamp. Rationale: the network invariant is "a level indicator follows exactly one State Latch" (`:152-154`), and the player should see the refusal when wiring, not as a Run failure.
- The sticky case asserts "stays lit" through to the end of the run, with a second hit in the same run that adds no lamp activation. Rationale: this is CAT-035 §5 "duplicate triggers produce no extra events".

**§5 items not reachable in UI:**
- Wrong domain: no electrical input exists, so attempt Battery Supply → lamp with `expectNoCommand` (the UI offers no choice).
- The Delay-fed lamp's exact due tick: 9R.17.
- Counter-fed lamp: `cat-020`.

**Test-first plan:** the investigation case goes first and is written as the refusal assertion. The other cases run red against the current build.

**Mutation plan:** each change below must fail a case.
- Make the lamp follow every input (Sticky: a second activation).
- Drop the level-edge rule (Second-signal row).
- Treat a latch-fed lamp as sticky (Latch level off-phase).
- Leave emission on after Reset (snapshot).

**Impacted suites:** `anim-1a`, `anim-1b`, `cat-023b`, `cat-037`, `cat-052`, `cat-063`, `DelayedSignalTests`, `WorkshopActivationAnimationTests`.

**Risks:**
- Wiring order: a lamp wired first from a switch and then from a latch also becomes level with two edges. Cover both orders.
- If 9R.5 lands its level as a different node kind, follow its declaration.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx` -- expected: 0 warnings.
- `dotnet test CuriousContraptions.slnx` -- expected: 100%.
- Serial Chrome run of `tools/e2e/cat-035.test.ts`, then all of `tools/e2e/*.test.ts` against the 8060 production preview -- expected: 100%.
