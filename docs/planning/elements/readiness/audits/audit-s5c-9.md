# Planning audit 9: S5c-1 FO1-3 amendment (bounded)

- Checkout: `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-s5c`
- Baseline: `39aaba70` plus the uncommitted candidate (implementation and planning records)
- Date: 26 November 2026
- Role: engineering-auditor, bounded planning check (`_bmad/custom/bmad-loop-protocol.md`, Planning and repair transitions); fresh agent, read-only on the repository; no builds, tests, mutants or browser
- Bounded scope: the FO1-3 amendment only (run log handled messages, first entry; E8 option A). That is S5c-1 Always `:59` and `:67`, the Code Map `WorkshopWireBuildTests.cs` bullet `:162`, D9 `:205` and Spec Change Log `:248`, plus the three line-local edits in S5c-2 (`:92`, `:93`, `:108`). The checks are:
  1. whether the amendment implements the ruling exactly;
  2. whether it is consistent with the rest of the S5c-1 contract and with S5c-2;
  3. whether it can be implemented with no runner file changed;
  4. whether it leaves a forward-only problem.
- Inputs read:
  - `AGENTS.md` (Refactor forward and its review gate);
  - `_bmad/custom/role-cards.md` (Informed auditors);
  - `_bmad/custom/bmad-loop-protocol.md` (Planning and repair transitions, Reviewer duties);
  - both contracts as they stand now;
  - run log handled messages and E8;
  - `CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj` (whole file);
  - `tools/E2eRunner/Toolchain.cs:74-100`;
  - `CuriousContraptions.tests/WorkshopWireBuildTests.cs:1-60`, `:125-424` and the baseline line numbers.
- Line numbers: contract lines are as the files stand now. Project-file and build-test lines are the candidate tree's, with baseline lines in brackets where the contract cites them.

## Verdict: Fail

The amended lines state the ruling correctly: `:59`, `:67`, `:162`, D9 and the handled message agree. Five of the contract's other lines still forbid or misdescribe the ruled change, though. The partial deletion of `OrderWireSchemas` leaves dead residue, and `ListWireSchemas` loses its purpose and its only test. The deleted test also takes with it the only test of a refusal that the build keeps. Open: 5 medium and 5 low findings.

## Findings

### A9-1 (medium, wrong specified behaviour): Never `:112` forbids the ruled change

`:112` forbids "any change to S5b's TypeScript generation targets or the `WireSchema` item". The ruling changes both:
- the `WireSchema` glob `../schemas/*.fbs` (project `:44`, baseline `:43`) becomes the one item;
- `GenerateWireTypeScript` (`:174-206`) stops calling `OrderWireSchemas` and writes `schemas.txt` from the one item.

An implementer cannot satisfy `:59` and `:112` together.

**Repair.** Replace the clause after "(coordinator ruling, 26 November 2026)" with: "or any change to S5b's TypeScript generation beyond the FO1-3 ruling (`:59`): `WireSchema` becomes the one item, `OrderWireSchemas` is deleted, and `GenerateWireTypeScript` passes `@(WireSchema)` and records its one name; its Debug-only condition, staging, annotation normalisation, output refusals and Outputs are unchanged."

### A9-2 (medium, wrong specified behaviour): owned scope and Concurrency still restrict the project file to its header

Owned scope `:142` says "nothing else in the file changes in S5c-1", and Concurrency `:217` says "the vendored project file (header only)". Under the ruling, the file changes in these places:
- `WireSchema` (`:44`);
- `GenerateWireTypeScript`'s `CallTarget` and the `schemas.txt` line (`:177-179`, `:181`, `:195`);
- the whole `OrderWireSchemas` target (`:212-276`; see A9-3);
- `ListWireSchemas` (see A9-4);
- the comments that describe the glob and the ordering: the header `:5` ("older than schemas/*.fbs, the list of those files"), `:119-120`, and `:152-169` ("The schema order comes from the compiler's own dependency output (OrderWireSchemas) …" and "compares with the current schemas/*.fbs names").

**Repair.**
- Rewrite `:142`: "**`CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj`**: the header's re-apply sentence (`:7-9`) and the FO1-3 changes. `WireSchema` (`:44`) is the one item `../schemas/workshop_wire.fbs`. `OrderWireSchemas` (`:212-276`) is deleted. `GenerateWireTypeScript` passes `@(WireSchema)` and writes `schemas.txt` from `@(WireSchema->'%(Filename)%(Extension)')`. The comments at `:5` and `:152-169` describe one file. A9-4 decides `ListWireSchemas`."
- In `:217`, replace "(header only)" with "(header and the FO1-3 changes)".

### A9-3 (medium, forward-only): `OrderWireSchemas` is only partly deleted, and S5c-2 plans to delete "what remains"

`:59`, `:162`, D9 and `:248` delete "`OrderWireSchemas`' `flatc -M` parsing and include ordering", not the target itself. S5c-2 `:93` then deletes "what remains of `OrderWireSchemas`", which says that S5c-1 leaves something behind. With one fixed item, anything left is dead:
- the file-name check `_WireSchemaMisnamed` (`:229-233`) guarded only the `-M` command line, which passes relative names (a leading `-` read as an option). `GenerateWireAccessors` and the `--ts` step quote `FullPath`, and the name is a literal in the project file, so the check can never fire;
- a `Returns` that passes `@(WireSchema)` through is a second name for the same item.

Either one is a kept branch with no purpose, which the review gate (`AGENTS.md`, Refactor forward) treats as a Fail.

**Repair.**
- In S5c-1 `:59`, `:162`, D9 and `:248`, say: "the `OrderWireSchemas` target (`:212-276`) is deleted whole, its file-name check included; `GenerateWireTypeScript` passes `@(WireSchema)` to `flatc --ts` and writes `schemas.txt` from `@(WireSchema->'%(Filename)%(Extension)')`."
- In S5c-2 `:93`, delete "and what remains of `OrderWireSchemas` (S5c-1 deleted its `-M` parsing and include ordering; FO1-3 ruling)". In its place write "(`OrderWireSchemas` was deleted by S5c-1; FO1-3 ruling)".

### A9-4 (medium, forward-only): `ListWireSchemas` and its stamp have no purpose and no test once the glob is gone

The only stated purpose of `ListWireSchemas` (`:119-123`) is this: "rewriting the list only when that set changes, so adding or deleting a whole schema file reruns GenerateWireAccessors". With `WireSchema` a literal one-item list, `WireSchemas.list` always has the same content. The only edit that could change it is an edit to the project file, which is already an Input (`MSBuildProjectFullPath`, `:129`, `:175`). The stamp detects nothing.

The case that tested it, `ADeletedSchemaFileLeavesNoGeneratedSource` (`:348-367`, baseline `:334-354`), is deleted by the amendment. Even so:
- `:162` says "`ListWireSchemas` and its stamp stay (S5c-2 owns their final form)";
- S5c-2 `:92` says "`ListWireSchemas` (`:118-122`) stays for C#".

This is the multi-file mechanism kept beside the one-file rule.

**Repair.**
- In S5c-1 `:162`, replace "`ListWireSchemas` and its stamp stay (S5c-2 owns their final form)" with "`ListWireSchemas`, its `DependsOnTargets` uses (`:128`, `:174`), the `WireSchemas.list` Inputs (`:129`, `:175`) and its `FileWrites` entry (`:148`) are deleted: with one literal item the list cannot change except through the project file, which is already an Input."
- Add the same deletion to the `:142` rewrite (A9-2).
- In S5c-2 `:92`, delete "`ListWireSchemas` (`:118-122`) stays for C#."

### A9-5 (medium, test): deleting the whole test removes the only test of a refusal the build keeps

`:162` and `:248` say that S1 covers the two deleted tests. That holds for the cases about several schema files and `include`. `ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` also holds the stray-output case (`:257-262`): a schema in another namespace makes flatc write outside the wire module, and the build refuses it while leaving the previous output as it was.

That refusal (`_WireTypeScriptStray`, `:182-186`) stays in `GenerateWireTypeScript` until S5c-2 (D10). It is still reachable with one file: change the namespace, or append a second `namespace` with a table. S1 does not test this build refusal, and nothing else does either (`rg "outside the wire namespace" CuriousContraptions.tests/` finds only `:259`). That breaks the AGENTS.md rule "Delete a test only when its behaviour is removed or provably covered elsewhere".

S5c-2 `:110` also plans to *move* "the stray-output refusal (D10), rewritten on the one file with a second namespace appended", which assumes the case still exists after S5c-1.

**Repair.** Add to `:162`: "The stray-output case is kept as its own `[Fact]` on the one file:
- after a first build, a second `namespace S5bOther;` with one table is appended to `schemas/workshop_wire.fbs`;
- the build is refused on the line 'wrote TypeScript outside the wire namespace's module', naming `s5b-other`;
- the previous TypeScript output's write times are unchanged.

This text holds several namespaces, so it asserts on the refusal line, never through `WireSchemaText` (see A9-7). `RefusalLine` stays for it."

### A9-6 (low, test): the deletion leaves dead helpers

`MisnamedSchemas` (`:308`, baseline `:298`) is used only inside the deleted test. `RefusalLine` (`:323`) is too, unless A9-5's case keeps it.

**Repair.** In `:162`, name `MisnamedSchemas` for deletion with the test. Delete `RefusalLine` as well if A9-5 is instead resolved by deleting the refusal.

### A9-7 (low, wrong specified behaviour): Always `:106` describes cases that no longer exist

`:106` reads: "S5b's copied-tree build cases that deliberately hold several schema files or an `include` assert on generated output and refusal lines, never through `WireSchemaText`." After the amendment no such case remains, and `:162` says every remaining case runs on the one schema file.

**Repair.** Replace `:106` with: "The one copied-tree build case whose schema text leaves the closed grammar (the stray-output case: a second namespace) asserts on its refusal line, never through `WireSchemaText`; every other build case reads one closed-grammar file."

### A9-8 (low, wrong specified behaviour): S5c-2 still lists, as its own deletions, what S5c-1 now deletes

Two S5c-2 lines were not among the three line-local edits:
- `:32` (intent): "S5b's vendored-project target, `OrderWireSchemas`, its staging properties and the schema record `schemas.txt` are deleted (D3)";
- `:191` (Design Notes): "Deleted: S5b's vendored-project TypeScript target, include ordering, Debug-only guard, …".

**Repair.**
- In `:32`, drop "`OrderWireSchemas`, ".
- In `:191`, drop "include ordering, " and add "(`OrderWireSchemas` and the include ordering were deleted by S5c-1, FO1-3 ruling)".

### A9-9 (low, wrong specified behaviour): stale comments are outside the named rewording

`:162` names only the comments at `:79` and `:185` for rewording. These also state the glob or ordering rationale:
- the class summary `WorkshopWireBuildTests.cs:11` ("from schemas/*.fbs");
- the comment on `ACheckoutPathWithASpaceBuildsACompleteModuleSet` (`:310-311`: "The schema order is read from the compiler's output run in the schemas directory on plain file names"), which is now false. The case stays, because its quoting of `FullPath` still matters;
- the project-file header and generation comments (A9-2).

**Repair.** Add to `:162`: "the summary `:11` and the comment `:310-311` are reworded: the one schema file, and the space-path case proving the quoted full paths of both generation commands". The project-file comments are covered by A9-2's rewrite.

### A9-10 (low, test): the acceptance-to-test table and AC3 cite pre-ruling runs for cases the amendment changes

The table's AC row (`:321`) and AC3 (`:238`) record "passed, run log E7" for the TypeScript generation and the `WorkshopWireBuildTests` cases. E7 ran the pre-ruling candidate, which still had the glob, `OrderWireSchemas` and the two multi-file cases. The table has no row for the kept stray-output case (A9-5).

**Repair.**
- Mark the AC1 to AC4 row and the build cases "pending re-execution on the FO1-3 candidate".
- Add a row: "FO1-3 | `WorkshopWireBuildTests` (one-file cases) and the kept stray-output case | Control: the clean copy builds a complete module set from the one file. Boundary: a second namespace is refused with the previous output unchanged | pending".

Non-blocking note: Spec Change Log `:248` cites "Run log E9", which is not yet written (the run log ends its entries at E8). Write E9 with the repairs.

## Checks that hold

1. **The ruling is stated correctly.** `:59`, `:67`, `:162`, D9 (`:205`) and `:248` match the handled message:
   - both steps pass exactly `schemas/workshop_wire.fbs`;
   - the `-M` parsing, the include ordering and the two named multi-file tests are deleted, with S1 as their cover;
   - TypeScript generation stays in the vendored project, Debug-only, writing `schemas.txt` with one name;
   - no runner file changes.

   S5c-2 `:92` (`WireSchema` is "the one item S5c-1 left") and `:108` (the two tests "already deleted by S5c-1") agree. Gaps are confined to A9-1 to A9-10.
2. **No runner edit is needed.** `RequireGeneratedWireModule` (`tools/E2eRunner/Toolchain.cs:80-100`) compares the ordinal-sorted non-empty lines of `schemas.txt` with the ordinal-sorted file names of `schemas/*.fbs`, then checks that the entry module is no older than every schema file and the project file.

   With `schemas.txt` written from `@(WireSchema->'%(Filename)%(Extension)')` (A9-3's repair), the record is `workshop_wire.fbs`. S1 keeps `schemas/` to that one file, so the check passes. The staleness inputs are unchanged.

   The runner tests (`TypeCheckBoundaryTests.cs:148-163`, `IdentityTests.cs:177-192`) build synthetic roots and do not depend on how the build orders schemas. No dead runner branch is created; the runner's record check is S5c-2's to delete (S5c-2 `:148`).
3. **`GenerateWireTypeScript` can pass the one file.** Its `Exec` (`:181`) takes any item list. Dropping the `CallTarget` and using `@(WireSchema)` keeps:
   - the stray and missing-entry refusals;
   - normalisation;
   - the staging moves;
   - Outputs (`wire.ts`, `schemas.txt`);
   - the Debug-only condition.

   Task 1(a) and audit 1 established that plain `flatc --ts` on the merged file writes the entry module re-exporting every declaration (`:226`).
4. **There is no second generation rule once A9-3 and A9-4 are applied.** C# and TypeScript generation read the same one item, and the build performs no dependency-output parsing.
5. **The amendment's scope stays within S5c-1's owned files.** It touches only the project file (already re-pinned in `CSharpStaticGateTests.cs:116`, `:144`) and `WorkshopWireBuildTests.cs`; no `tools/E2eRunner*` or `tools/e2e` file changes. This matches Never `:112`'s directory clause and Concurrency `:216`.
