# Planning audit: `spec-s5c-one-wire-schema-file.md` (S5c-1), eighth pass (bounded check of the audit 7 repairs)

Audited 26 November 2026, read-only, planning phase. Checkout `.claude/worktrees/slice-s5c`, branch `slice-s5c`, HEAD `39aaba70`, with the uncommitted planning edits of run log E2, E4 and E5. `git diff --quiet 39aaba70 -- schemas/`: the schema files are unchanged. Policy read from this checkout only: `AGENTS.md`; `_bmad/custom/role-cards.md` (Informed auditors); `_bmad/custom/bmad-loop-protocol.md` (Contract budget, Reviewer duties, Triage); `docs/testing-guidance.md` (skimmed); `audit-s5c-7.md`; the S5c-1 contract; its run log, entry E5.

**Bounded scope (coordinator brief):** (1) for each of A7-1 to A7-9, whether the repair is applied, whether it closes the finding, and whether it creates a new contradiction in the lines it touched; (2) A7-1's narrowed grammar, verified against the merged schema; (3) the word budget. Settled design was not reopened, and nothing else was re-audited.

**Measurements made:**
- `sed -n`, `rg` and `wc -w` on the contract, the run log and the cited sources.
- A scratch parser, `.tools/s5c-evidence/audit8/grammar_check.py` (Anvil-validated; run with `python3 -I .tools/s5c-evidence/audit8/grammar_check.py "$PWD"`). It builds the merged file from the two schema files at `39aaba70` as the contract specifies it:
  - one `namespace CuriousContraptions.Wire;`;
  - `attribute "authoring";`;
  - `enum WireRootIdentifier : uint32` with the four `0x` values, each with its ASCII comment;
  - the nine `(authoring)` marks;
  - no `include`;
  - no `root_type WorkshopResponse`.

  It then parses that file with a recursive-descent implementation of the grammar at Always `:86-89` and nothing more, and runs ten refusal controls and two positive cases.
- AC3's `rg` command, run verbatim from `:224`.

No build, test, mutant or browser run.

## Verdict: Scoped Pass

- **No high or medium finding is open.**
- **A7-1 to A7-9 are all Closed.** A7-1 is Closed with the planner's deliberate narrowing, which this audit accepts. A7-2 and A7-3 are Closed in substance, each with a wording omission in the auditor's own repair text: A8-3 for A7-2 and A8-2 for A7-3.
- **Four new low findings, A8-1 to A8-4,** each with exact repair text. All four are contract text and none reopens design. They can be applied without another audit. The implementation review checks them.
- **The merged file is admitted** under the stated grammar. Every scalar on the closed list is used, and the named refusals are refused.

This is a planning audit. It is not an implementation pass, and it is not build, browser or release qualification.

## Audit 7 dispositions

| Finding | Applied at | Disposition | Evidence and new-contradiction check |
|---|---|---|---|
| A7-1 (medium) | Always `:85`, `:87`, `:88`, `:89`; table row `:279` | **Closed**, with the accepted deviation | **Applied:**<ul><li>`:85` is A7-1's sentence verbatim.</li><li>`:87` carries the enum form `Member [= <decimal or 0x integer>]` and the implicit-value rule.</li><li>`:279` gains the implicit-value boundary.</li></ul>**Deviation (accepted, see "A7-1: the narrowed grammar"):** the scalar list and the default set are closed to the measured inventory instead of A7-1's 21 names and broader default set. `:88` states the closure and the extension rule. `:279` adds an `int16` control that tests the closure.<br>**Real file:** admitted.<br>**New contradiction:** `:279`'s count of its controls (A8-1). |
| A7-2 (medium) | Code Map `:131`; README bullet `:150` | **Closed**; wording A8-3 | **Applied:** `:131` is A7-2's text verbatim, and `:150` names `schemas/README.md:36`.<br>**Cited lines re-read:**<ul><li>`SchemaKind` `:502` and `SchemaDeclaration` `:504`;</li><li>the union arm and its comment, `:550-553`;</li><li>"or union" at `:516`, `:543` and `:999-1001`;</li><li>`GeneratedWireCase.UnionVerify` at `:983`, `:992` and `:1022-1024`;</li><li>`README.md:36`.</li></ul>**Synthetic schema:** the complete synthetic text of the repair is admitted by the scratch parser ("POSITIVE static gate fragment"). Its enum `Kind : uint32 { A, B }` relies on A7-1's implicit values; its scalars `uint32` and `float` are on the list.<br>**Omission:** the theory's summary (`:987-989`) still describes the deleted union case (A8-3). |
| A7-3 (low) | Code Map `:148` | **Closed**; wording A8-2 | **Applied:** the replacement text is verbatim.<br>**The `:92` principle is consistent:** multi-file steps never read through `WireSchemaText`.<br>**Single-file states:** every one that calls `SchemaTypes()` or `GeneratedTypeScriptViolations` is admitted under the grammar. Each probe field the build tests add is `uint32` (`WorkshopWireBuildTests.cs:68`, `:82`, `:173`, `:192`).<br>**Omission:** the rewritten `ADeletedSchemaFileLeavesNoGeneratedSource` still asserts on `SavedConstruction` (A8-2). |
| A7-4 (low) | Table rows S1 `:273`, S9 `:276` | **Closed** | **Applied:** both boundaries are verbatim.<br>**S9 citations re-read:**<ul><li>`WorkshopResponse.bodies: [BodyRead]` (`workshop_wire.fbs:282`, field at `:297`);</li><li>`BodyRead.local: Vec3f` (`:86-89`);</li><li>`InstanceRecord.local: Vec3f` (`workshop_authoring.fbs:49`).</li></ul> |
| A7-5 (low) | Row S10 `:110`; probe `:181`; Code Map `:132` | **Closed** | **Applied:** all three edits are present.<br>**Seam re-read:** `MatchingCompilations` is at `CSharpStaticGateTests.cs:370`, and its caller at `GameFloatGuardTests.cs:131`.<br>**No contradiction:** row S10, the probe and the Code Map now name the same seam. |
| A7-6 (low) | Row S8 `:108` | **Closed** | **Applied:** the Error Handling cell is replaced.<br>**Message re-read:** `WorkshopWireFlatBuffers.cs:42` and `:44` report "The FlatBuffers verifier refused a {identifier} buffer.", which carries the reason and the identifier. |
| A7-7 (low) | AC3 `:224`; Always `:65` | **Closed** | **Applied:** both edits are verbatim.<br>**Command run at `39aaba70`:** it parses and runs. It hits 25 files:<ul><li>the codec, interface and implementer files: `RuntimeCodecs.cs`, `CoreCommandCodec.cs`, `WorkshopAuthoringFlatBuffers.cs`, `WorkshopSaveCodec.cs`, `WorkshopWireFlatBuffers.cs`, `WorkshopWireFlatBuffers.Observation.cs`;</li><li>the 19 test files of the Code Map's repair list.</li></ul>Each hit file is named in the Code Map, and nothing else is hit. |
| A7-8 (low) | Code Map `:152` | **Closed** | **Applied:** the bullet is present.<br>**Lines re-read:** `rg workshop_authoring` hits exactly the cited lines: `EL-124…:127`, `:128`; `EL-125…:130`; `completion-ledger.md:305`, `:309`; `delivery-batches.md:295`, `:314`, `:361`. The two history lines are excluded as stated. |
| A7-9 (low) | Code Map `:151`; `delivery-batches.md:316` | **Closed**; record A8-4 | **Applied:** both edits are present. `:316` now reads "As in the three contracts".<br>**Gap:** `:151` does not name this audit, which the candidate will carry (A8-4). |

## A7-1: the narrowed grammar (scope item 2)

### The merged file under the stated grammar

The scratch parser implements exactly Always `:86-89`:
- `//` comments, and `/*` refused;
- the seven top-level forms with their count rules;
- the enum, struct and table forms, with types from the 12-name scalar list, `string`, declared names and one-level vectors;
- defaults `decimal float | null`;
- the field attribute `required` and the declaration attribute `authoring`;
- every other token refused, and every unresolved name refused.

The merged file is **admitted**. The measured inventory matches E5's table row for row:

| Construct | Measured (merged file) | E5 |
|---|---|---|
| Top level | `enum` 9, `struct` 49 (4 `(authoring)`), `table` 79 (5 `(authoring)`); one namespace, attribute, `root_type`, `file_identifier`; nothing else | same |
| Enum underlying | `uint8` ×7, `uint32` ×2 | same |
| Enum values | decimal ×38, implicit ×12, `0x` ×4; no trailing comma; no negative | same |
| Scalars used | exactly `bool double float int int32 int64 ubyte uint uint32 uint64 uint8 ulong`; no list name is unused | same 12 |
| Vectors | `[<declared>]` ×74, `[uint8]` ×10, `[ulong]` ×6, `[double]` ×1; no `[string]`, no fixed array | same |
| Struct fields | 309 (301 scalar + 8 declared) | same |
| Defaults | `null` ×12, `0.0` ×1 | same |
| Field attributes | `(required)` ×84 | same |
| Comment lines | 39 (36 + 3 at `39aaba70`; the merge deletes 4 at `workshop_wire.fbs:348-351` and adds 4 ASCII comments) | 39 |

### Refusals (controls run through the same parser)

`union`, `(deprecated)`, `(id: 0)`, `include`, `int16`, `(key)`, a fixed array `[uint32:2]`, `/*`, `= true` and the integer default `= 0` are each refused, naming the construct and its line. Two positive cases are admitted:
- the complete static-gate text of A7-2;
- a table with fields named `id` and `key`, as `:89` states.

### Interpretation points the real file depends on

The contract is silent on these points. The faithful reading is the permissive one, and the implementer must not add a rule for any of them:
- **Declaration order is free.** Declarations follow `root_type` today (`workshop_wire.fbs:1122`, then `:1123-1174`), and the build tests append probe tables after it (`WorkshopWireBuildTests.cs:173`, `:192`).
- **Enum values need not ascend.** `CommandKind` has `ConfigureRunControl = 14` before `ObserveMemory = 12` (`workshop_wire.fbs:32-34`). An "ascending" check would refuse the real file. Only `WireRootIdentifier`'s values are stated to ascend (`:61`).
- **`int16` is refused by name.** A parser that knows only the 12 listed names refuses `int16` as an unresolved type name. That is still a refusal naming the construct and line under `:89`, so either message satisfies `:279`.

### Run log E5's last sentence

E5 says the grammar "admits nothing the merged file does not use apart from the named refusals". That holds for constructs and sets, which is the claim the contract itself makes at `:88`. It does not hold for every combination: the grammar admits, for example, `[string]`, `[bool]`, `= null` on any table field and `(required)` on any field, none of which the file uses. Each is valid FlatBuffers or refused by the compiler, and none bypasses a load-bearing refusal. Not a finding. The run log is append-only and the contract's claim is correct.

### Narrowing versus A7-1's broader list: recommendation

Keep the narrowing:
- The S5b lesson applies. Every admitted spelling with no input in the file is parser surface without a positive test.
- The `int16` control makes the closure itself tested.
- The 12 names include both spellings of every width the file uses, so the merge needs nothing more.

**Extension rule:** `:88` states it: "a schema that needs another extends the grammar in its own slice". That suffices for three reasons:
- the boundary "the real file is admitted" (`:279`) fails first on any new construct, so an extension cannot happen silently;
- `:75` and `:89` already record that the `union`, `deprecated` and `id:` refusals carry the `Verify` coverage premise, `WireSchemaLayout` and S9, so an extending slice must re-establish them before it admits one;
- the extending slice replaces or re-aims the `int16` control as part of the change.

**Forward note, not a finding.** `ENGINE-WIRE-ABI` declares the thread-boundary layouts in this same file (Never `:101`; owner decision 24p). It is the likely first slice to extend the grammar (fixed arrays in structs, 16-bit scalars) and should plan that extension in its own contract.

## New findings

### A8-1 (low, test/record): `:279` counts four controls and lists five

**Evidence:** E5 added the `int16` control to `OnlyTheClosedGrammarIsAccepted`'s row. The row reads "Control: four synthetic texts, one with a `union`, one with `deprecated`, one with `id:` and one with `include`, and one using a scalar outside the list (`int16`), are each refused". That is five texts.

**Repair, at `:279`:**
- Replace "four synthetic texts" with "five synthetic texts".
- Replace "one with `include`, and one using" with "one with `include` and one using".

### A8-2 (low, test): the rewritten `ADeletedSchemaFileLeavesNoGeneratedSource` keeps assertions the merge makes false

**Evidence:**
- Today the case asserts that `SavedConstruction` is generated before the deletion (`WorkshopWireBuildTests.cs:342-344`) and absent after it (`:349-352`).
- A7-3's rewrite (`:148`) deletes a probe that includes `workshop_wire.fbs`. After the merge, `SavedConstruction` lives in the surviving `workshop_wire.fbs`, so a rewrite that changes only the deleted file fails its own assertions.
- The contract does not say the assertions move to the probe's type.

**Repair, at `:148`:** after "so deleting it edits no other file", insert "; its assertions (`:342-344`, `:349-352`) name the probe's table in place of `SavedConstruction`".

### A8-3 (low, record): A7-2's deletion list omits the theory summary that describes the union case

**Evidence:** `CSharpStaticGateTests.cs:987-989` ("The union case adds a union to the schema with the source the pinned flatc writes for it (its enum and its Verify class) and is admitted.") describes the case A7-2 deletes. A7-2's repair text, now `:131`, lists the "or union" texts at `:516`, `:543` and `:999-1001` but not this summary. Run log E4 `:40` treats stale text for deleted branches as a Forward-Only finding.

**Repair, at `:131`:** after "(`:516`, `:543`, `:999-1001`)", insert "and the union sentence of the theory summary (`:987-989`)".

### A8-4 (low, record): the planning-records list does not name this audit

**Evidence:** `:151` names `audits/audit-s5c-6.md` and `audit-s5c-7.md` (A7-9). The S5c-1 candidate will also carry `audit-s5c-8.md`, so the Final diff scope check would report it as unowned.

**Repair, at `:151`:** replace "`audit-s5c-7.md` (A7-9)" with "`audit-s5c-7.md` and `audit-s5c-8.md` (A7-9)".

## Budget (scope item 3)

- **Current size:** 5,932 words (`wc -w`), within 6,000.
- **What must still fit:**

  | Text | Words added |
  |---|---|
  | A8-1 to A8-4 together | about 25 |
  | The planner's audit 8 line in the Spec Change Log | about 25 |
  | The update to the Planning verdict (`:120`) | about 10 |

  The total comes to about 5,990, which leaves no room for anything else.
- **After the repairs:** the planner re-measures. If the total exceeds 6,000, the first text to shorten is the Planning verdict's audit history (`:120`), which run log E4 and E5 already hold. This is a record action, not a finding.
