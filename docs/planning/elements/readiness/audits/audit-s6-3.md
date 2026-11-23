# Planning audit: `spec-s6-table-driven-capability-plumbing.md` (S6, first slice), third pass

Audited on 23 November 2026, in the planning phase. Nothing was built or run. I read the contract in the `spec-drafts` checkout (uncommitted draft, 2,266 words) and read all code with `git show 25df3cd5:<path>`. "Main" below means `25df3cd5`. Policy came from the `spec-drafts` checkout: the loop protocol, the role cards, the engineering contract, both element-free engine guides, the testing guidance and AGENTS.md. Binding decisions came from `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`, which is untracked. No earlier S6 audit file exists (completion ledger DC-55), so I carried nothing over.

## Verdict: Incomplete

The design is sound. The 19-row table is feasible as specified, with static functions and no allocation per read. All cited code and every cited test exist at main and say what the contract claims. The probe holds. Leaving the refusal unexecuted is justified, because the testing guidance forbids test doubles. Three text defects block ready-for-dev:

- B1. The intent's Approach misdescribes two of the table rows.
- B2. The story clauses this slice leaves unmet are deferred to follow-on slices that exist nowhere outside this contract.
- B3. The contract does not name the unpublished planning work it depends on.

All three are fixes to the contract text. With them applied, and the owner's answer recorded for O-1 and O-2 below, this is a Scoped Pass. A new audit is not needed.

## What was checked and holds

- **The switch and the driver count.** `Query` is at main `engine/gpu/WorkshopDeclaredPresentation.cs:373`–`:414`.
  - It has 19 continuous case labels: StoredFraction, Activation, Count, OscillatorPhase, OscillatorPulse, TimerProgress, TimerRemaining, TimerActive, ElectricalSocket, Supply, Charge, RevoluteAngle, ActuatorDrive, WheelAngle, FlowSpeed, GasRadius, SurfaceDeflection, RetentionReleased and TransmissionClosure.
  - `PresentationDriver` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:39`) is `Event` plus 25 committed values. Six of those are discrete and selected through `MapDiscreteState` (`WorkshopDeclaredPresentation.cs:588`–`:600`): FiniteWorkState, ActuatorState, PrismaticPhase, ContactLoadPhase, TimerPhase and TransmissionPhase. 25 − 6 = 19, as the contract states.
- **Static functions are feasible.** `Query` is `private static` and takes only `(Track, WorkshopRead)`. No case uses instance state or a captured local. Seventeen cases call an existing `private static` function with the same two arguments.
  - The gas-radius case (`:402`–`:405`) is inline code that reads only `track` and `read`. It extracts unchanged.
  - The transmission-closure case (`:411`) is `QueryTransmission(track, read).Closure`. It needs a projection, either a static lambda or a new function. See B1 and N2.
  - `Track` is a `sealed class` (`:15`), so a `Func<Track, WorkshopRead, float>` delegate held by static lambdas is cached by the compiler and allocates nothing per read.
  - A frozen or ordinary dictionary keyed by the byte-backed enum, read with `TryGetValue`, boxes nothing and uses no reflection. It is safe under trimming and ahead-of-time compilation in the Animation worker.
- **The refusal is unreachable through real admission.**
  - `WorkshopCompiledScene` refuses an undefined driver (`engine/gpu/WorkshopCompiledScene.cs:326`) and a driver–mapping pair that `PresentationDriverRules.SupportsMapping` rejects (`:335`). `SupportsMapping` admits `StateValues` only for the six discrete drivers and `Identity` only for non-discrete ones (`ElementPresentation.cs:174`–`:181`). Both refusals throw "Compiled presentation track is invalid." (`:227`).
  - So a discrete driver never reaches `Query`, and an undefined value never passes admission.
  - Tests do construct `WorkshopCompiledScene` directly with modified tracks (`ComposableAnimationMappingTests.cs:392`–`:402`), but that constructor validates first.
  - The only other route is a fake `ICompiledScene` passed to the public constructor (`WorkshopDeclaredPresentation.cs:89`). That is a test double of an internal component, which `docs/testing-guidance.md:31` forbids.
  - AC3's manual reading is therefore the honest proof for the refusal. Lesson 4 is met as far as the rules allow.
- **Every cited test exists and asserts a value that only its own source produces.** All 19 cited methods were found at main:
  - `ComposableAnimationMappingTests` `:379`, `:484`, `:515`, `:605`.
  - `DeclaredPresentationTests` `:237`, `:415`, `:490`.
  - `ComposableSignalNodesTests:325`; `HoldTimerTests.cs:470`; `ElectricalNandTests.cs:345`; `BothGateTests.cs:259`.
  - `ClutchGeneralityTests.cs:197`, `:511`; `OwnedFiniteWorkTransactionTests:403`; `FanIntegrationTests` `:541`, `:586`.
  - `BalloonTests:198`; `ClutchTests.cs:1448`; `WoundSpringLifecycleTests:56`.

  The class names used in the filter are real: `HoldTimerPresentationTests` (`HoldTimerTests.cs:463`), `ElectricalNandPresentationTests` (`:342`), `BothGatePresentationTests` (`:252`), `ClutchArtTests` (`ClutchTests.cs:1412`), and `ClutchTests` as a partial class in `ClutchGeneralityTests.cs:14`. I opened seven of the tests, including every test that covers more than one driver:
  - The three-driver follow test feeds distinct sources: guide angle 1.0, surface deflection 0.2 and an oscillator due 60 ticks ahead. It asserts each binding's own end value, 1.0, 0.2 and 0.5 (`:410`–`:441`).
  - The Battery theory asserts the terminal colour from availability and each charge mark from the fraction.
  - The Clock test asserts the pendulum swing from the pulse and the counter dot lamp from the count.
  - The Hold timer test asserts the bar scale from the remaining time and the lamp colour from the active state.
  - The Balloon test asserts the radius sample within 0.31–0.35 m at 70–80 % gas volume (`:221`–`:224`).
  - The Wound spring test asserts the latch angle within one tick's travel.
  - The Clutch revolute test asserts that the sample equals the selected child's coordinate, which differs from the first child's by more than 0.01 (`:213`–`:215`).

  If a row were wired to the wrong function, these tests would see either a wrong value or an "absent observation" refusal. Two pairs share one function by design and are not distinguished at the table: Supply and Charge both route to `QuerySource`, and the three timer drivers both route to `QueryTimer`. That is unchanged behaviour.
- **Consumers.** `Query` has one caller, `StageRead` (`:298`). The file is a `CoreRuntimeSource` (`CuriousContraptions.Core/RuntimeSources.props:124`), imported by Core, Simulation and the game project. It is created through `CorePresentationServices.Create` (`CorePresentationServices.cs:13`). No signature changes, and no test file changes.
- **Measurement command.** It mirrors the gate's own injection: `MaintainabilityGate.WriteInjection` and `Compile` in `tools/anvil/VerifyPolicy.cs:1046`–`:1067` and `:1137`–`:1146` use the same globalconfig, `CodeMetricsConfig.txt`, `WarningsNotAsErrors`, the `IsTestProject` condition, `--no-incremental` and `-p:CustomAfterMicrosoftCommonTargets`. Only the threshold is lowered to 14.
  - The warning that this rebuilds the root game project in place is correct. The Godot SDK fixes that project's output under `.godot/mono/temp` whatever `--artifacts-path` says (`VerifyPolicy.cs:1096`–`:1101`). So running the measurement before the compiler-log captures, as Verification orders, is right.
  - `.tools/` is ignored (`.gitignore:6`), so the three evidence files are never committed.
- **Tier and shape.** Engine tier, one structural slice, one member. This is correct: engine source is outside the data-only allow-list.
- **Engineering rules.**
  - Element-free: the table is keyed by the generic enum only.
  - Forward-only: the switch is deleted, and no second path or default row is added.
  - E9: before 22, accepted at 14 or below.
  - No skipped tests, and the total delta is 0. This is consistent with no test change.
  - The eight design-record answers of `element-free-engine-design.md` §6 are covered by the seven notes. "What remains uncertain" is answered in note 7.
- **Budget and size.** 2,266 words and three acceptance-bearing rows. This is one coherent slice.

## Blocking findings

### B1. The intent's Approach misdescribes two rows (wrong specified behaviour)

- **Where:** contract `:24`, inside `<intent-contract>`, against `:56`, `:107` and AC3 (`:114`).
- **Defect:** The Approach says every row names "the existing query function". Two rows cannot:
  - The gas-radius row names a function this slice creates (`:56`, task 2).
  - The transmission-closure row needs the `.Closure` projection of `QueryTransmission` (main `:411`). That projection is either a lambda in the table or a new function. AC3 allows only "`Query`, the new table and the extracted gas-radius function" as changed members.

  The intent becomes read-only after planning.
- **Consequence:** A reviewer holding the slice to its intent must either fail the gas-radius extraction or accept a row that contradicts the intent. If the implementer extracts a closure function, AC3 is violated.
- **Fix, `:24`:** "Replace the `switch` with one static table keyed by the closed `PresentationDriver` enum, one row per continuous committed driver. Each row is a static function of the track and the committed read: the existing query function, or for the gas radius the inline case moved unchanged into its own static function, or for the transmission closure a static lambda returning `QueryTransmission(track, read).Closure`. Behaviour is preserved exactly; the proof is the existing presentation tests, one per driver."
- **Fix, AC3 (`:114`):** after "the extracted gas-radius function", add "(the transmission-closure row is a static lambda inside the table initialiser, not a new member)".

### B2. Unmet story clauses are deferred to slices recorded nowhere (wrong specified behaviour)

- **Where:** contract `:91`, `:97`, `:109`, against the S6 story at main `docs/planning/invest/delivery-batches.md:267`–`:273`.
- **Defect:** The story's Outcome and Acceptance require three things:
  - each per-capability chain becomes a table "with one entry per capability and a test that every enum value has an entry";
  - "no listed member is above 15 afterwards", where the listed members are every member at 20 or above (120 by the contract's own measurement at `:63`);
  - "adding a capability … touches one table row per chain".

  Task 4 records that this slice meets none of these except for `Query`. Owner decision O1 says S6 "is not narrowed" and that S6b–S6f will finish it. But S6b–S6f appear in no planning document, on main or in the uncommitted planning work: `rg -e S6b -e S6f -g '*.md' -g '*.yaml'` finds only the S5 and S6 contracts. Only S6b's subject (the wire response codec) is named. No slice owns the every-enum-value test or the members at 15 or above.
- **Consequence:** Brief checks 2 and 3. Story acceptance is dropped with no owner. When S6 "closes" after five undefined slices, the unmet clauses go unchecked.
- **Fix, task 4 (`:109`):** replace with this text:

  > `docs/planning/invest/delivery-batches.md` S6 (`:267`–`:273` on main) -- add a **Slices** list. S6a (this slice): `Query`, published. S6b: the wire response codec. S6c–S6f: the remaining members at 20 or above from the run-log measurement, assigned by chain in that list. For each, name the member, its measured value and the slice. State which follow-on adds the test that every continuous and discrete `PresentationDriver` value has exactly one evaluation route. Mark S6 open until S6f publishes. In `deferred-work.md`, under the "S6:" item (`:276` on main), add one line per follow-on with source (this contract), evidence (the run-log measurement) and owner (the coordinator). `TODO.md` -- the slice's outcome.

  If the coordinator cannot assign the members to five slices, raise it as owner question O-2 below before ready-for-dev instead of writing the list.

### B3. The planning publication is a prerequisite, not named (wrong specified behaviour)

- **Where:** contract `:53`, `:101`, `:129`.
- **Defect:** The slice's authority to run before the engine programme is "owner decision 22". That decision lives only in the untracked `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (`git ls-files docs/planning/elements/readiness/` lists nothing) and in uncommitted edits to `delivery-batches.md`:
  - section 0 at worktree `:11`–`:13`;
  - the S6 **When** line at worktree `:301`.

  Main's S6 section (`:267`–`:273`) has no **When** line. Task 4 edits that section, and the uncommitted planning work also rewrites it. The slice starts from a clean checkout at published main.
- **Consequence:** The implementer cannot open the decision the contract relies on, and task 4's edit conflicts textually with the planning publication.
- **Fix, Concurrency (`:101`), add as the first sentence:** "Prerequisite: the planning work of 22–23 November 2026 (the `docs/planning/elements/readiness/` folder, `docs/planning/invest/delivery-batches.md` section 0 and its S6 **When** line, and the sprint-status note on owner decision 22) is published before this slice starts. The coordinator confirms it is on the baseline and re-reads task 4's anchor there."

## Non-blocking findings (apply with the above)

- **N1. Visible-solid part C reads this file** (record). Part C (`spec-elements-visible-solid-remainder-c.md:83`, `:86`) cites `WorkshopDeclaredPresentation.cs:402`–`:405` (the gas-radius case this slice moves), `:89`–`:155` and `:637`–`:649` as present-capability evidence. `docs/planning/elements/readiness/batch-o.md:46` cites `:492`–`:502`. S6's claim that it shares no file with part C is true for edits, but those citations go stale when S6 publishes. Add to Concurrency (`:101`): "Part C cites this file's lines as evidence; whichever of S6 and part C publishes second re-reads those citations on the merged main. `batch-o.md:46` cites `:492`–`:502` and is re-read the same way."
- **N2. Name the delegate shape** (product defect if missed). Add to task 2: "Table type: a frozen dictionary from `PresentationDriver` to `Func<Track, WorkshopRead, float>`, built from `static` lambdas or method groups, read with `TryGetValue`; the miss branch throws `ArgumentException(\"Unsupported committed presentation driver.\")`." This makes "allocates nothing new, no reflection" checkable from the diff.
- **N3. Intent matrix numbering.** The rows are M1 and M3 with no M2, and the acceptance criteria run AC1, AC2, AC3, AC5 with no AC4. The numbering is cosmetic, but the intent is read-only after planning, so renumber now: M3 becomes M2 (`:45`, `:153`) and AC5 becomes AC4 (`:115`, `:156`).
- **N4. The measurement log is unnamed.** At `:136`, `<log>` is never produced. Write "`… > .tools/s6-evidence/before.log 2>&1` (and `after.log`), then `rg \"WorkshopDeclaredPresentation.cs.*CA1502\" .tools/s6-evidence/before.log`". Also note that the file is compiled into three projects, so the same member can appear up to three times.
- **N5. D7 applies.** Note 7 (`:129`) says D6 and D7 are unaffected. This slice changes the presentation evaluator's source dispatch, which is D7's subject, and preserves it. Write "D7 applies and is preserved (source, mapping and evaluator stay separately declared)".
- **N6. Toolchain environment** (the same note appears in all four reports). On this host the default PATH has Homebrew node v26.13.1 and Homebrew dotnet 10.0.403, and that dotnet has no wasm-tools workload. The pinned toolchain at `~/.local/share/tim-toolchain` has `dotnet-10.0.403` with wasm-tools and `node-v26.12.1-darwin-arm64`. The capture block's `test "$(node --version)" = v26.12.1` (`docs/delivery-workflow.md:41`–`:43`) and the web build therefore fail unless the pinned directories come first on PATH and `DOTNET_ROOT` points at the pinned SDK. Neither the contract nor the workflow says this. Add one line to Verification (`:133`): "Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`; record the session environment check in the run log."

## Decisions that need the owner

- **O-1. Does owner decision 22 cover S6b–S6f?** Decision 22 (23 November) allows "S3, S5 and S6" before the engine programme. O1 (21 November) splits S6 into six slices. Are S6b–S6f part of "S6", and so allowed before the programme, or do they wait for it? Recommended default: S6b–S6f run after the programme. Each engine story that adds a capability then extends its chain by one row and repairs at its own boundary, and decision 22's list stays as written. This does not block this first slice.
- **O-2. What closes the S6 story?** Should the acceptance "no listed member above 15" apply to every member at 20 or above (120 measured), or only to the per-capability chains keyed by a closed enum? The contract (`:65`) says the rest are sequential validators and codecs whose repair is extraction. Recommended default: restate S6 as the per-capability enum chains, each closed by its own slice with a test that every enum value has an evaluation route. The general members above 15 stay under the E9 gate's 25 limit, repaired when a slice trips it. This is a narrowing of O1 and is the owner's to approve. B2's fix records whichever is chosen.

## Evidence limits

- Nothing was built or measured. `Query`'s complexity of 22 is the contract's measurement on `8d1d68aa`; the engine file is unchanged at main (`git diff --stat 8d1d68aa 25df3cd5` lists no engine file). The "about 2 after" figure is the contract's estimate.
- I read 7 of the 15 cited presentation tests in detail. For the other eight, I checked only that the method exists at the cited file and line.
- I did not verify that a frozen dictionary keyed by an enum is fully free of per-call allocation under the Animation worker's AOT. N2 makes that a reviewable diff fact.
- The uncommitted planning work and part C's draft were read only for the lines cited above. Both may change.
