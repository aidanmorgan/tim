# Planning audit 10: S5c-1 audit 9 repairs (bounded)

- Checkout: `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-s5c`
- Baseline: `39aaba70` plus the uncommitted candidate (implementation and planning records)
- Date: 26 November 2026
- Role: engineering-auditor, bounded planning re-check (`_bmad/custom/role-cards.md`, Informed auditors); fresh agent, read-only on the repository; no builds, tests, mutants or browser
- Bounded scope:
  1. For each of A9-1 to A9-10 (`audit-s5c-9.md`), whether the repair is applied and closes the finding without a new contradiction in the lines it touched.
  2. One consistency sweep of both contracts: `rg -n "glob|OrderWireSchemas|ListWireSchemas|WireSchemas.list|ASchemaSetWithoutOneRoot|ADeletedSchemaFile|MisnamedSchemas|header only|nothing else in the file"`, checked against the FO1-3 ruling:
     - both generation steps pass exactly `schemas/workshop_wire.fbs`;
     - `OrderWireSchemas` and `ListWireSchemas` are deleted whole in S5c-1;
     - the stray-output case is kept on the one file;
     - no runner file changes.
- Inputs read:
  - `audit-s5c-9.md`;
  - `_bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md` (whole file);
  - `_bmad-output/implementation-artifacts/spec-s5c-2-wire-conformance-and-root-pin.md` `:28-36`, `:88-115`, `:185-200`;
  - `CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj` `:1-12`, `:40-50`, `:115-200`, with the target and comment lines located by `grep -n`;
  - the run log headings (`_bmad-output/implementation-artifacts/run-logs/spec-s5c-one-wire-schema-file.md`).
- Line numbers: contract lines are as the files stand now. Project-file lines are the candidate tree's (still unchanged from the pre-ruling candidate: `WireSchema` glob at `:44`, `ListWireSchemas` at `:119-123`, `OrderWireSchemas` at `:224`).

## Verdict: Scoped Pass

Every A9 finding is closed in substance. A9-3 is closed through the new Spec Change Log entry `:249`, which supersedes `:248`, rather than by editing `:248` as its repair text said. The sweep finds no hit that contradicts the FO1-3 ruling. No high or medium finding is open. Three new low findings are below (A10-1 to A10-3); none blocks.

## Dispositions

| Finding | Repair applied where | Disposition |
|---|---|---|
| A9-1 (medium) | S5c-1 Never `:112` | **Closed.** The clause now admits exactly the FO1-3 changes: `WireSchema` becomes the one item, `OrderWireSchemas` and `ListWireSchemas` are deleted, and `GenerateWireTypeScript` passes `@(WireSchema)` and records its one name. It keeps the Debug-only condition, staging, normalisation, output refusals and Outputs unchanged. `ListWireSchemas` is named, in line with A9-4. The clause does not claim the Inputs or `DependsOnTargets` are unchanged, so it does not contradict `:142`'s removal of the stamp Input. |
| A9-2 (medium) | S5c-1 `:142`, `:217` | **Closed.** `:142` states the header sentence and every FO1-3 change to the project file, including the `ListWireSchemas` deletion from A9-4. `:217` reads "(header and the FO1-3 changes)". `:142`'s comment list adds a new low inconsistency, recorded as A10-1. |
| A9-3 (medium) | S5c-1 `:59`, `:142`, `:162`, D9 `:205`, `:249`; S5c-2 `:93` | **Closed, by supersession.** `:59` ("the `OrderWireSchemas` target (its `flatc -M` parsing, include ordering and file-name check) … deleted whole"), `:142` ("deleted whole, its file-name check included"), `:162` and D9 all state the whole deletion. S5c-2 `:93` says "(`OrderWireSchemas` was deleted by S5c-1; FO1-3 ruling)". The repair text also named `:248`, which was not edited and still reads "`OrderWireSchemas`' `-M` parsing and include ordering … are deleted". It is kept as the historical entry for the first amendment, and `:249`, directly below it, records that both targets are deleted whole. That is an acceptable form for a change log, so it is not a new finding. |
| A9-4 (medium) | S5c-1 `:59`, `:112`, `:142`, D9; S5c-2 `:92` | **Closed.** `:142` deletes `ListWireSchemas`, its `DependsOnTargets` uses, the `WireSchemas.list` Inputs and its `FileWrites` entry, with the stated reason. `:162` no longer says it stays. S5c-2 `:92` reads "(`ListWireSchemas` was deleted by S5c-1, FO1-3 ruling.)". |
| A9-5 (medium) | S5c-1 `:162`; S5c-2 `:110` | **Closed.** The stray-output case is kept as its own `[Fact]` on the one file, with the appended `namespace S5bOther;`, the refusal line naming `s5b-other`, the unchanged write times, assertion on the refusal line only, and `RefusalLine` kept. S5c-2 `:110` moves "S5c-1's one-file case with a second namespace appended", which is consistent. |
| A9-6 (low) | S5c-1 `:162` | **Closed.** `MisnamedSchemas` is deleted with the two tests, and `RefusalLine` stays for the kept case. |
| A9-7 (low) | S5c-1 `:106` | **Closed.** The text matches the repair, and it agrees with `:162` ("Every other case runs on the one closed-grammar file"). |
| A9-8 (low) | S5c-2 `:32`, `:191` | **Closed.** `:32` no longer lists `OrderWireSchemas`. `:191` drops "include ordering" and adds the S5c-1 attribution. |
| A9-9 (low) | S5c-1 `:162` | **Closed.** The summary `:11` and the comment `:310-311` are named for rewording (one schema file; the space-path case proving the quoted full paths of both generation commands). |
| A9-10 (low) | S5c-1 table `:322`, `:323` | **Closed.** The new FO1-3 row is "pending", and the AC1 to AC4 row reads "pending re-execution on the FO1-3 candidate (E7 ran the pre-ruling candidate)". AC3 (`:238`) no longer records an execution result. Audit 9's non-blocking note on the missing run log E9 is still open and is carried as A10-2. |

## Consistency sweep

The pattern was run over both contracts. Every hit:

- **S5c-1 `:59`, `:112`, `:142`, `:162`, `:205` (D9), `:249`.** Each agrees with the ruling: one item, both targets deleted whole, the stray case kept on the one file, no runner file.
- **S5c-1 `:238` (AC3).** The hit is the `--glob` arguments of the `rg` command, not a schema glob. It agrees.
- **S5c-1 `:248`.** Superseded history: it states the first amendment's partial deletion, and `:249` records the whole deletion (see A9-3).
- **S5c-1 `:272` (Review Triage Log, FO1-3) and `:328` (Auto Run Result).** They describe the finding and the pre-ruling blocked state. They make no claim against the ruling, and they are outside the touched lines.
- **S5c-2 `:92`, `:93`, `:108`, `:191`.** They attribute the deletion of `ListWireSchemas`, `OrderWireSchemas` and the two multi-file tests to S5c-1. They agree.
- **"header only" and "nothing else in the file".** No hits remain.
- **`WireSchemas.list`.** The one hit is in `:142`, as a deletion.
- **`MisnamedSchemas`.** The one hit is in `:162`, as a deletion.

No hit restores a glob, a partial `OrderWireSchemas`, a kept `ListWireSchemas`, the deleted multi-file cases, or a runner file change.

## New findings

### A10-1 (low, wrong specified behaviour): `:142`'s comment list rewords a deleted comment and omits a stale one

`:142` ends: "The comments at `:5`, `:119-120` and `:152-169` describe one file." Two problems follow.

- `:119-120` is the comment on `ListWireSchemas` ("Lists the schema files, rewriting the list only when that set changes …"). `:142` deletes that target whole in the same bullet, so its comment cannot be reworded; it must be deleted with the target.
- The comment on `GenerateWireAccessors` (candidate `:125-127`: "whenever the stamp is absent or older than a schema, the schema list or this file") names "the schema list", which is the deleted `WireSchemas.list`. It goes stale, and no line names it.

**Repair.** In S5c-1 `:142`, replace "The comments at `:5`, `:119-120` and `:152-169` describe one file." with: "The comments at `:5`, `:125-127` and `:152-169` are reworded to describe one file. The comment `:119-120` is deleted with `ListWireSchemas`."

### A10-2 (low, record): touched lines cite a run log entry that does not exist

Spec Change Log `:248` and the new `:249` both cite "Run log E9". The run log (`_bmad-output/implementation-artifacts/run-logs/spec-s5c-one-wire-schema-file.md`) still ends at E8 (`:199`). Audit 9 noted this as non-blocking, and the repair added a second citation of the missing entry.

**Repair.** Write run log E9, recording the FO1-3 ruling's amendment and the A9-1 to A9-10 repairs, with this audit's verdict.

### A10-3 (low, citation): `:142` uses candidate-tree lines under a Code Map header that says baseline

The Code Map header (`:130`) says every `path:line` was re-read at `39aaba70`. `:142` cites candidate-tree lines (`WireSchema` `:44`, `OrderWireSchemas` `:212-276`, `:128`/`:174`, `:129`/`:175`, `:148`). Meanwhile `:67` and S5c-2 `:92` cite the same `WireSchema` item at baseline `:43`.

**Repair.** In `:142`, after "the FO1-3 changes", add "(project-file lines are the pre-ruling candidate tree's; baseline is one line earlier from `:44`)", or convert them to baseline lines.

## Not reopened

Everything outside A9-1 to A9-10 and the sweep. That includes the frontmatter `status`, the Auto Run Result status, the Status line `:132`, and whether the stray-output case's `s5b-other` path is what the pinned compiler writes, which audit 9 established.
