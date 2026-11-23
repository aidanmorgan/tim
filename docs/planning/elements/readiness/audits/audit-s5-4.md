# Planning audit: `spec-s5-generate-wire-accessors-in-build.md` (S5, fourth audit)

Audited 23 November 2026, read-only, planning phase: nothing was built or run, and every test named in the contract is planned, not executed. Checkout `spec-drafts`; code read at published main `25df3cd5`. Policy was read from the same checkout, including the uncommitted binding decisions in `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (owner 0–24k, coordinator 16, 21, 25–37 and 24l). Contract line numbers cite the snapshot `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/37b1f527-b6f7-4082-bfd2-686d62e814b3/scratchpad/audit4-snap/spec-s5-generate-wire-accessors-in-build.md`, taken at 21:33 and identical to the live file when this report was written (4,892 words).

## Verdict: Incomplete

Every blocking and non-blocking finding of the third audit is resolved in substance. Its owner question on S5b's timing is answered by owner decision 24h, and the contract applies that answer. Three new blocking findings remain. All three are text fixes, with no redesign and no new owner decision:

- **N-B1.** The contract does not account for owner decisions 24j and 24k. It says nothing about which wire code it leaves hand-written, and it names no owner for that code beyond S5b's TypeScript.
- **N-B2.** AC3's `git grep` will match the dated audit records that the prerequisite planning publication copies into the tracked readiness folder (coordinator decision 24l), so AC3 is false on the baseline the contract requires.

- **N-B3.** The frontmatter sets `baseline_revision` to the drafting commit `25df3cd5`, although the contract says the baseline is the published main that contains the planning work.

With N-B1, N-B2 and N-B3 applied, the contract is a Scoped Pass (ready-for-dev).

## Third-audit findings: resolution

| Third-audit finding | Status | Where it is resolved (snapshot line) | Substance check |
|---|---|---|---|
| B1: unpublished planning work not named as a prerequisite | Resolved | `:66`, `:133`, `:170` | Concurrency names the planning work (readiness folder, `delivery-batches.md` sections 0 and 5, `named-elements.md`, `requirements.md`, `sprint-status.yaml`, `deferred-work.md`, `AGENTS.md`). The slice starts from the published main that contains it. The coordinator re-reads the S5 section and the "S5:" item before dispatch. |
| B2: `GameFloatGuardTests` missed consumer and lost protection | Resolved | `:81`, `:111`, `:145`, `:182` | `GameFloatGuardTests.cs:39-43` holds the five `CuriousContraptions.Wire.CaptureObservation.*` rows (verified). The guard resolves types through the real compile's metadata references (`CSharpStaticGateTests.MatchingCompilations`, `CSharpStaticGateTests.cs:367-377`). So the claim at `:111`, that a hand-written caller of a new `double` accessor is still flagged, holds after the accessors leave source. |
| B3: AC3 false as written | Resolved as asked, superseded by N-B2 | `:154`, `:184`, `:207` | The `git grep` command and its expected match are in AC3, Verification and the table. It is false again for a new reason (N-B2). |
| B4: AC4 number and M6 case count | Resolved | `:155`, `:181`, `:185`, `:203` | The deltas are arithmetically right against main: −7 (`WorkshopWireGenerationTests`: 2 facts plus 5 `InlineData`, verified), +6 (M1–M5 and M7 facts), +7 (the six-case theory plus `TheVendoredCompileDefinesNoSpanAccessorSymbol`). That gives +6 on the full round, 7,294 → 7,300. The registration filter (`docs/delivery-workflow.md:67`) contains `CSharpStaticGateTests` and neither wire test class, so +7 there. The hook's selection (`.githooks/pre-commit:53`) is `CSharpStaticGateTests` and `WorkshopUiFailureTests`, so +7 there. |
| B5: build tests share state across methods | Resolved | `:146`, `:200`, `:204` | Each method gets its own copy, and the M3 and M7 controls run inside the same method. |
| N1: session toolchain | Resolved | `:176`, `:146` | The pinned `$HOME/.local/share/tim-toolchain` comes first on `PATH` (matches `TODO.md:7`). The child-process environment is stated. |
| N2: copy list | Resolved | `:146` | `.editorconfig` is copied (it has the `[CuriousContraptions.FlatBuffers/**.cs]` section at `.editorconfig:8`). The copy list comes from `git ls-files`. The offline restore is recorded. The root has no `NuGet.config` or `Directory.Packages.props`, so nothing else is missing from the copy list. |
| N3: host key | Resolved | `:141` | Keyed by `OSArchitecture` with `IsOSPlatform`, never `$(RuntimeIdentifier)`. |
| N4: cache writability | Resolved | `:141` | Stop and return to planning if the cache is not writable. |
| N5: two-argument `GetFolderPath` | Resolved | `:30`, `:141` | Recorded in the run log. Linux is proven by the O3 CI check. |
| N6: protection of the new build logic | Resolved | `:112` | The whole-file digest at `CSharpStaticGateTests.cs:112`; the vendored skip at `:1154` (verified). |
| N7: binlog query | Resolved, see N-1 below | `:147` | The query is named; the stop condition is ambiguous. |
| N8: story wording of D2 | Resolved | `:119`, `:149` | Task 10 records the deletion of the joining tool. |
| Owner question: S5b timing | Answered by owner decision 24h | `:68`, `:128`, `:149` | "S5b runs straight after S5"; recorded in `deferred-work.md` with that wording. |

## Re-verified on this pass (holds)

- **Citations at `25df3cd5`.**
  - `CuriousContraptions.FlatBuffers.csproj:4-6` and `:16-19`.
  - `CuriousContraptions.csproj:67-68`; `CuriousContraptions.slnx:63`.
  - `CuriousContraptions.tests.csproj`: `:16` (tool reference) and `:77` (`<Compile Include="*.cs" />`, with `EnableDefaultCompileItems=false` at `:7`).
  - `CSharpStaticGateTests.cs`: `:29`, `:112`, `:475`, `:851`, `:1082`, `:1154`, `:1376`.
  - `GameFloatGuardTests.cs:36-43` and `:64-70`.
  - `tools/anvil/CheckStagedTests.cs`: `:35`, `:43`, `:60`.
  - `tools/anvil/VerifyPolicy.cs`: `:204`, `:961`, `:1092`, `:1110`.
  - `_bmad/custom/bmad-loop-protocol.md:155`; `_bmad/custom/role-cards.md:64`; `docs/delivery-workflow.md:34`; `.github/workflows/pages.yml:35-43`.
- **Consumers by name.** A working-tree search for `WorkshopWireGenerat|WorkshopAuthoringGenerat|WorkshopWireGeneration`, outside `_bmad-output/` and `docs/archive/` and including the untracked planning work, finds 13 files. Twelve are in owned scope; the thirteenth is the S5 section of `docs/planning/invest/delivery-batches.md` (`:294`), which task 10 rewrites and AC3 expects. No other project names the files:
  - `Core/RuntimeSources.props` lists codec sources individually;
  - the Simulation, web and animation projects compile their own directories;
  - the game project compiles `engine/gpu/*.cs` (`CuriousContraptions.csproj:34`), which is why it holds the two `Compile Remove` lines.
- **Executable on main.**
  - **One compile rule.** No test-project `Compile` edit is planned (`:76`).
  - **Derived registration.** S5 adds no package, port, parameter or wire field; the registration check runs only as a delta check.
  - **Staged-source rule.** The new test file is `git add`ed before the commit (`:135`, `:186`). Generated `obj/**/*.cs` is an ignored directory, collapsed and dropped (`CheckStagedTests.cs:43`, `:60`; `.gitignore:12`). Task 1's scratch output under `.tools/` is ignored (`.gitignore:6`, confirmed with `git check-ignore`).
  - **`tools/anvil/` is not edited**, so the policy-harness command (`bmad-loop-protocol.md:180`) is correctly not required.
  - **Pinned toolchain** (`:176`).
  - **Hook.** It builds the index snapshot with `--no-incremental` (`.githooks/pre-commit:23`, `:35-44`), so generation runs from the cache on every hook run.
- **Tier, shape and size.** Engine tier: build configuration, a tool project, test harness and policy files. One structural slice. 4,892 words, within the 6,000 budget.
- **Element-free engine and forward-only.** No engine behaviour, solver law, worker ABI or wire byte changes. Deleted: both generated files and their sidecars, the joining tool and its tests, the two `Compile Remove` lines, the `GeneratedWire` exemptions, the five dead adapter rows and the two blind-diff pathspecs. No fallback compiler, no second generation path, no test-only switch (`:43`).
- **Planned tests discriminate.** Every M row and AC row has a named test or a recorded command, with a control and a boundary. No float, hash or tick assertion: generated-output byte comparison is forbidden at `:44`. Task 1 compares parsed types and members only.
- **Serialisation.**
  - Planning work publishes first (coordinator decision 37).
  - S3 and S5 publish one at a time (`:135`).
  - S5b starts from S5's published main (owner decision 24h).
  - No other pre-programme slice edits `schemas/*.fbs` or the generated files. Checked: the S6 and S6b–S6f contracts, S3, the Baseball repair and visible-solid parts A–D. S6b's Never line excludes schema and accessor changes (`spec-s6b-response-codec-read-family-table.md:35`).
  - Shared files are named, and the later publisher merges main once and repeats the full round.

## New blocking findings

### N-B1. Owner decisions 24j and 24k are not accounted for: the wire code left hand-written and its owners are not stated (wrong specified behaviour)

- **Where:** contract `:41-42` (intent, Never), `:92` (capability probe: "No solver, worker ABI, wire-schema or presentation capability is added or changed"), `:149` (task 10's document rewrites), `:164-170` (Design Notes). No line cites 24j, 24k, `ENGINE-WIRE-ABI` or `ENGINE-WASM-THREADS`.
- **Evidence:** owner decision 24j (`owner-decisions-2026-11-22.md:62`) says "FlatBuffers wire code is generated from schemas, never hand-coded". Owner decision 24k (`:63`) creates `ENGINE-WIRE-ABI`: every boundary record is generated on both sides from the pinned `flatc`, unknown-field refusal is derived from the generated code, and the opaque motion blob is replaced. S5 makes only the C# accessors build output. Independently enumerated at `25df3cd5`, the wire code that S5 leaves hand-written is:
  1. **Domain-to-accessor mapping codecs, 3,468 lines:**
     - `engine/gpu/WorkshopWireFlatBuffers.cs` and its nine partial files (`.Airflow`, `.Chambers`, `.Compiled`, `.CoordinateTravel`, `.Gas`, `.GasRead`, `.Observation`, `.Retention`, `.RotaryAirflow`);
     - `engine/gpu/WorkshopAuthoringFlatBuffers.cs` and `engine/gpu/WorkshopSaveCodec.cs`;
     - the response encoder `CuriousContraptions.Core/CoreRuntimeCodecs.cs:16-19`.
  2. **Hand vtable arithmetic for unknown-field refusal:**
     - `engine/gpu/WorkshopWireFlatBuffers.cs:45` (`CommandIdentifier => 12, ResponseIdentifier => 41`) and `:50` (`4 + fieldCount * 2`);
     - `:53` (scene slots 11 and 27, used in `ValidateBindingShapes`).
  3. **The opaque blob** `motion: [uint8]` (`schemas/workshop_wire.fbs:306`), which tunnels the hand-laid `engine/gpu/PhysicsMotionRead.cs` record.
  4. **Non-FlatBuffers boundary layouts, hand-coded on both sides:**
     - the pose ring, `engine/gpu/WorkshopPoseRing.cs` and `CuriousContraptions.web/wwwroot/pose-ring.ts`;
     - the clock and event rings (`CuriousContraptions.web/wwwroot/clock-ring.ts`, `event-ring-cursor.ts`);
     - the physics GPU ABI records.
  5. **TypeScript wire:**
     - the committed `flatc` output `tools/e2e/workshop-wire.ts`, with its hand helpers below the generated part;
     - the hand enum mirrors `CuriousContraptions.web/wwwroot/workshop-observation.ts:3-49`.

  The ABI audit (`.tools/audit/abi-interfaces.md` §1.3, rows W1–W7, and §5, rows G9, G11, G8) assigns items 2–4 to `ENGINE-WIRE-ABI`, item 5's generated module to S5b and the enum mirrors to `ENGINE-CAPABILITY-INSTANCES`. It calls item 1 "normal mapping" and gives it no owner.
- **What the contract does say:**
  - S5b owns the generated TypeScript (`:42`, `:128`).
  - The hand-written codecs are out of scope (`:41`), with no owner and no reference to 24j.
  - The probe's "no wire-schema capability changed" is true but silent on the residual.
- **Consequence.** The intent contract becomes read-only after planning, and a slice titled "generate the wire accessors" leaves 24j's "never hand-coded" unmet with no recorded owner. In review, the Forward-Only and Intent layers can read the Never line as permanently exempting hand wire code. Task 10's rewrite of `schemas/README.md` and of the S5 story can describe the wire as "generated" when it is not, so the documents would misstate 24j's status.
- **Fix, intent `:41`.** Replace with: "A change to `schemas/*.fbs`, to wire bytes, to the vendored runtime sources, to the hand-written domain-to-accessor mapping codecs, or to the hand-coded wire that owner decision 24k assigns to `ENGINE-WIRE-ABI` (the hand vtable field counts used for unknown-field refusal, the `motion` byte vector and the non-FlatBuffers boundary layouts)." Keep the attribution to 24k narrow. Decision 24k does not name the mapping codecs, and the ABI audit calls them "normal mapping" with no owner (W1). The intent contract is read-only after planning, so it must not assign them.
- **Fix, Code Map.** After the Capability probe, add a subsection **"Wire code left hand-written (owner decisions 24j, 24k)"**. It lists items 1–5 above with their `path:line` and owners:
  - **Item 1, the mapping codecs:** "Remain hand-written domain-to-accessor mapping. Whether `ENGINE-WIRE-ABI` absorbs or generates them is that story's decision."
  - **Items 2–4:** `ENGINE-WIRE-ABI`, under owner decision 24k. Item 2 is the refusal "derived from the generated code". Item 3 is the "opaque motion blob is replaced". Item 4 is the boundary records and the pose ring; its shared-memory layouts go with `ENGINE-WASM-THREADS`. The ABI audit's alternative owner for item 2 ("S5b, or `ENGINE-WIRE-ABI` if S5b stays TypeScript-only", row G9) is settled, because S5b's Never line excludes the C# accessors (`spec-s5b-generate-typescript-wire-in-build.md:37`).
  - **Item 5:** `tools/e2e/workshop-wire.ts` and its helpers go to S5b; the TypeScript enum mirrors go to `ENGINE-CAPABILITY-INSTANCES`.

  End the subsection with: "S5 meets 24j for the C# accessors only; it claims nothing for the rest."
- **Fix, task 10 (`:149`).** Add: "The rewritten `schemas/README.md` and the S5 story state that the C# accessors are build output. They also state that the mapping codecs remain hand-written, and that the hand-coded wire listed in the Code Map remains until `ENGINE-WIRE-ABI` (owner decision 24k)."
- **Fix, Design Notes item 7 (`:170`).** Add "owner decisions 24j and 24k (the generated TypeScript is S5b's; the hand vtable counts, `motion` vector and boundary layouts are `ENGINE-WIRE-ABI`'s)".

### N-B2. AC3's `git grep` matches the audit records that the prerequisite planning publication tracks (wrong specified behaviour)

- **Where:** contract `:154` (AC3), `:184` (Verification), `:207` (table).
- **Evidence:**
  - Coordinator decision 24l (`owner-decisions-2026-11-22.md:64`): "The audit reports cited by tracked planning files are copied into the tracked folder `docs/planning/elements/readiness/audits/` and every citation re-pointed". That copy is part of the planning work that the contract requires to publish first (`:133`).
  - Owner decision 24k cites the ABI audit (`readiness/abi-interfaces.md`), whose source `.tools/audit/abi-interfaces.md:29` and `:33` name `GPU/WorkshopWireGenerated.cs`, `GPU/WorkshopAuthoringGenerated.cs` and `tools/WorkshopWireGeneration`.
  - The contract itself cites `.tools/audit/audit-s5-3.md` (`:68`), which names all three repeatedly and is being re-pointed into the tracked folder.
  - The pathspec excludes only `_bmad-output` and `docs/archive`.
- **Defect:** AC3 requires that the grep "matches only the S5 section of `docs/planning/invest/delivery-batches.md`". On the required baseline it will also match those tracked audit copies, which the slice may not edit (they are dated evidence).
- **Consequence:** the slice's own acceptance fails on its prerequisite baseline. Either the implementer edits historical audit records, or the row is waived.
- **Fix, AC3 (`:154`) and Verification (`:184`).** Replace the expected result with: "every match is either in the S5 section of `docs/planning/invest/delivery-batches.md` or in a dated audit record under `docs/planning/elements/readiness/` that already matched at `baseline_revision` (run the same `git grep` at the baseline and compare; such records are historical evidence and are not edited)".
- **Fix, AC3 table row (`:207`).** Add the same comparison to the control: "the baseline grep lists the expected records".

### N-B3. The frontmatter's `baseline_revision` is the drafting commit, which contradicts the stated prerequisite (wrong specified behaviour)

- **Where:** contract `:6` (`baseline_revision: '25df3cd5…'`). This conflicts with:
  - `:64`: "drafting revision; the coordinator confirms or re-captures it at start";
  - `:133`: the slice starts from the published main that contains the planning work.

  The value is used by `:186` (`git diff --check <baseline_revision>`) and by the role-specific review diffs (`bmad-loop-protocol.md:155`).
- **Defect.** The drafting brief says not to set `baseline_revision`. The protocol captures it once before first implementation, then keeps it and never overwrites it (`bmad-loop-protocol.md:104`). A pre-filled value reads as already captured, and "confirms or re-captures" leaves the choice open. `25df3cd5` cannot be the baseline, because the planning work (`AGENTS.md`, `delivery-batches.md`, `deferred-work.md` and the readiness folder) publishes on top of it first.
- **Consequence.** If the value stays:
  - every review diff and `git diff --check` include the planning publication, plus any pre-programme slice published before S5;
  - N-B2's baseline comparison runs against the wrong tree;
  - the Final diff scope check flags other slices' work as drift.
- **Fix.** Delete `:6`. In `:64`, replace "Baseline: published main `25df3cd5` (drafting revision; the coordinator confirms or re-captures it at start, before first implementation)." with "Baseline: captured by the coordinator at start as the published main that contains the planning work (Concurrency). The contract was drafted and its lines checked on `25df3cd5`, which is not a baseline."
- **Same defect elsewhere.** The Baseball repair, S3 and S6 contracts have it too (see their fourth audits), and so does S5b (`spec-s5b-generate-typescript-wire-in-build.md:6`, out of scope here).

## Non-blocking findings (apply with the above)

- **N-1. Task 8's stop condition is ambiguous** (`:147`).
  - Sequential capture builds always reuse one intermediate directory: playtest and solution both write `CuriousContraptions.FlatBuffers/obj/Debug/net10.0/`, and today every project does the same. Read literally across `web.binlog` and `solution.binlog`, "if two builds share one intermediate directory, stop" fires on normal behaviour.
  - The risk the task guards against is two evaluations inside one build invocation (different global properties, same intermediate directory) racing on the generation folder.
  - Fix: "If, within one binary log, two evaluations of `CuriousContraptions.FlatBuffers` with different global properties share one `IntermediateOutputPath`, stop and return to planning."
- **N-2. Hook comment, no change needed.** `.githooks/pre-commit:24-25` says the guard exempts "the SDK's generated sources" in output directories. After S5 it also admits the generated wire set. The intent requires the hook unmodified (`:37`), and the comment stays true as far as it goes. Record it under "Checked and unchanged" so a reviewer does not raise it as stale.

## Decisions that need the owner

None.

- O1–O3 are owner answers recorded in the contract (`:121-128`).
- O4 is confirmed by owner decision 24h.
- N-B1 only records the owners that decisions 24j and 24k already assign. It moves no scope into S5 and waives nothing.

## Evidence limits

- Nothing was built, downloaded or run. Generation, provisioning, the build tests, task 1's type-and-member equivalence and the Linux asset remain unproven; this is planned, as the contract states.
- Contract lines cite the 21:33 snapshot. Another agent is re-pointing `.tools/audit/` citation strings in contracts; those strings, and the S5b contract, were out of scope.
- The tracked location of the copied ABI audit (`readiness/abi-interfaces.md` or `readiness/audits/`) is not yet on disk. N-B2 holds for either location.
- The residual wire inventory in N-B1 comes from `rg` over `*.cs` and `*.ts` for FlatBuffers and `DataView`/`SharedArrayBuffer` use, plus the ABI audit's tables. Hand-offset layouts outside those patterns were not individually enumerated.
