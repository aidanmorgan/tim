# Workshop wire schema

`workshop_wire.fbs` owns runtime commands, responses and compiled capability installations. Core owns its codec (`WorkshopWireFlatBuffers`); neither worker receives authored instances or loads element packages.

`workshop_authoring.fbs` owns authored construction and saves. The main assembly owns `WorkshopAuthoringFlatBuffers` and `WorkshopSaveCodec`. It includes the runtime schema for shared numeric structs. Main compiles authoring into the runtime schema before dispatch; there is no second decoder or old-format path.

- Every buffer is size-prefixed and carries its root's identifier: `CWCM` (`WorkshopCommand`), `CWRM` (`WorkshopResponse`) and
  `CCWS` (`SavedConstruction`). A schema declares one `file_identifier`, so the codecs pass each root's identifier to
  `FinishSizePrefixed` and to the verifier.
- A decoder checks the size prefix, then the identifier, then runs the FlatBuffers verifier, and decodes once. A refused buffer is
  a `WireFaultException` naming its `WireFaultReason` (a save: a `WorkshopSaveRejectedException` naming its `WorkshopSaveRejection`).
- Authored element identities are bundle strings (`InstanceRecord.part_id`, `PuzzleRecord.inventory_part`). Save admission validates them against the loaded bundle and refuses unknown identities with `WorkshopSaveRejection.UnknownElement`. Runtime installations contain only generic capabilities, numeric owners and compiled scene identities. Main retains authored capture metadata keyed by session and scene identity.
- Every transport control is a typed command or query (Story 9A.3). A command naming a kind the game does not have is a
  `WireFaultReason.UnknownCommand` refusal naming it. The stopped memory observation is the `ObserveMemory` command; only its applied
  acknowledgement carries `WorkshopResponse.memory`. A reliable-read stall is detected by the Simulation worker's own receipt
  accounting, so no message carries a stall flag.
- There is no format version (owner, 21 Oct 2026): the game and its saves ship in one bundle with one schema, and bad input is
  rejected structurally.

## Regenerating the code

Use flatc **25.2.10**, the version of the vendored runtime (`CuriousContraptions.FlatBuffers/FlatBufferConstants.cs` declares
`FLATBUFFERS_25_2_10`). The release binaries are at <https://github.com/google/flatbuffers/releases/tag/v25.2.10>
(`Mac.flatc.binary.zip` on macOS). A newer flatc stamps another runtime version and does not compile.

### C#: generated in every build

The C# accessors are build output and are never committed. `CuriousContraptions.FlatBuffers`, the one project that compiles
them, runs the pinned flatc on every `schemas/*.fbs` in one invocation before it compiles, whenever its generated sources are
absent or older than a schema, the list of schema files or its project file (so a clean tree, a `--no-incremental` build and
adding or deleting a schema file regenerate, and an unchanged tree does not). It replaces the previous output, writes one file
per schema type under `WireAccessors/` in its intermediate directory (`obj/<configuration>/<framework>/` by default) and
compiles those files as the compiler wrote them. The build refuses an `ENABLE_SPAN_T` define, so the nullable `ArraySegment`
byte accessors are the compiled surface. The C# static gate checks that, besides the SDK's and xUnit's own generated files,
only this compile receives sources from a build output directory, that they come from its generation folder and carry the
compiler's header, that they declare only schema types and the `Verify` classes the compiler writes for tables and unions,
that together they declare every schema type, and that the compile has no `ENABLE_SPAN_T` define.

The build provisions the compiler itself and never uses a `flatc` on `PATH`. It downloads the official release archive for the
build host (`Mac.flatc.binary.zip` on macOS Arm64 and X64, `Linux.flatc.binary.g++-13.zip` on Linux X64; any other host fails
the build), verifies the archive and binary SHA-256 digests pinned in `CuriousContraptions.FlatBuffers.csproj`, and keeps the
verified binary at `<LocalApplicationData>/CuriousContraptions/flatc/25.2.10/<asset>/flatc`: on macOS
`~/Library/Application Support/CuriousContraptions/flatc/25.2.10/Mac.flatc.binary/flatc`, on Linux under `$XDG_DATA_HOME` or
`~/.local/share`. The first build on each machine therefore needs network access to `github.com`; later builds reuse the cache,
and a tree that does not need to generate builds offline. The cached binary is verified before every use; a mismatch fails
the build, naming the file, both digests and the path to delete, and the file is never replaced silently.

The domain mapping codecs (`WorkshopWireFlatBuffers` and its partial files, `WorkshopAuthoringFlatBuffers`,
`WorkshopSaveCodec` and Core's response encoder) are ordinary typed C# that converts between engine types and the generated
accessors; they are not generated. Some wire code is still hand-coded until its owner replaces it: the hand vtable field counts
used for unknown-field refusal, the opaque `motion` byte vector and the non-FlatBuffers boundary layouts (the pose ring, the
clock and event rings and the physics GPU ABI records) until `ENGINE-WIRE-ABI`.

### TypeScript: generated in every Debug build

The TypeScript accessors the e2e suites import are build output too and are never committed. The same project's
`GenerateWireTypeScript` target runs the same pinned compiler, from the same inputs and the same set of `schemas/*.fbs`, with
`--ts` in one invocation over every schema. The order comes from the compiler's own dependency output (`-M`): the schemas
another schema includes first, so the last one, the root, which no schema includes, writes the entry module
`curious-contraptions/wire.ts` that re-exports every `enum`, `struct` and `table` the compiler reaches from it. The C# step has
already run when the TypeScript step starts; before any TypeScript is written the build is refused, naming the schemas, when
two or more schemas are included by none, when each schema is included by another (no root), or when the root's includes do
not reach every schema. The output is the ignored `tools/e2e/.wire/` of the tree being built, outside the output and
intermediate directories, so a clean does not delete it. The target reruns whenever the entry module or the schema record is
absent or older than a schema, the list of schema files or the project file. It generates into `tools/e2e/.wire/.staging/`, refuses, naming them, any
file outside the wire namespace's module (`curious-contraptions/wire.ts` and `curious-contraptions/wire/`) and a missing entry
module, normalises every file there (the compiler's optional string-accessor parameter becomes `flatbuffers.Encoding` and its
`no-explicit-any` suppression is removed, so generated code follows the same strict typing contract as hand-written code; the
overloads and runtime expressions are unchanged), deletes the previous output, moves the per-type files and the schema record
`schemas.txt` (the compiled schema file names, which the e2e runner compares with the current `schemas/*.fbs` names, so a
deleted or renamed schema is refused until a Debug build regenerates) into place and moves the entry module last, so until
the whole set is in place an output is absent or older than an input and the next build regenerates. A refusal removes the staging folder and leaves the previous output as it was; a failure after the previous output
is deleted leaves no entry module, so the next build regenerates and the e2e runner refuses to start meanwhile. The order is
read from the compiler's `-M` output, run in `schemas/` on the quoted file names with warnings off and tokenised on whitespace
whatever its line wrapping. A schema file name must start with a letter, digit or `_` (a leading `-` would be read as an option)
and consist of letters, digits, `_`, `.` and `-`, and an include must name a schema file in
`schemas/` by its exact file name (`include "x.fbs";`; the compiler folds `./x.fbs` to `x.fbs`): every name the compiler lists
must equal a schema file name, compared exactly, or the build is refused naming it, so a parent-directory or case-variant
spelling is refused. Only the
Debug build of the project generates it, so each build graph writes the folder once (CI's Release publish builds its one Debug
instance through the web host's editor-metadata build). The TypeScript runtime the generated code imports is `flatbuffers`
at the compiler's version, an exact devDependency of the e2e suites' own manifest, `tools/e2e/package.json`, installed by
`npm ci` in `tools/e2e` when the browser phase runs the suites; the root manifest does not name it.

`tools/e2e/workshop-wire.ts` keeps only the hand-written helpers (the frame checks, the authored-state dump and the mock
encoders). A suite imports a generated name from the entry module (`./.wire/curious-contraptions/wire.ts`, or
`../.wire/curious-contraptions/wire.ts` from `unit/`) and a helper from `workshop-wire.ts`, which declares and re-exports no
schema type.

After a schema change, also update the field listing in `tools/e2e/run-reset-snapshot.ts` (`SNAPSHOT_SCHEMA`) and the record field
lists in `CuriousContraptions.tests/WorkshopFormatTests.cs`; both tests fail until they match the schema.
