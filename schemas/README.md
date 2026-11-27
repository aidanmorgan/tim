# Workshop wire schema

`workshop_wire.fbs` is the one schema file (owner decision 24p): the ABI between the systems and threads. It declares runtime
commands, responses, diagnostic observations and compiled capability installations, and the authored construction and save
records. It has no `include`, one `root_type` (`SavedConstruction`, the root that lives in files) and one `file_identifier`. Core
owns the runtime codec (`WorkshopWireFlatBuffers`); neither worker receives authored instances or loads element packages.

- **Authoring declarations are marked.** The file declares `attribute "authoring";` and marks each authored-construction and
  save declaration `(authoring)`. The main assembly owns `WorkshopAuthoringFlatBuffers` and `WorkshopSaveCodec`. No runtime root
  reaches a marked declaration, and no compile of Core, Simulation or Animation.Worker references one, apart from Core's
  save-frame verification behind `ICommandCodec.ValidateSaveFrame` (`WireSchemaGuards`). Main compiles authoring into the runtime
  records before dispatch; there is no second decoder or old-format path.
- **Root identifiers are schema data.** `enum WireRootIdentifier : uint32` has one member per root, named as its root table,
  whose value is the root's four identifier bytes read as a little-endian `uint32` (`WorkshopCommand`, `WorkshopResponse`,
  `WorkshopObservationBatch`, `SavedConstruction`). Every buffer is size-prefixed and carries its root's identifier. C# converts a
  member with the one conversion, `WorkshopWireFlatBuffers.FileIdentifier`, for `FinishSizePrefixed` and the frame boundary; the
  save encoder uses the generated root finisher.
- A decoder checks the size prefix, then the identifier, then runs the FlatBuffers verifier, and decodes once. A refused buffer is
  a `WireFaultException` naming its `WireFaultReason` (a save: a `WorkshopSaveRejectedException` naming its `WorkshopSaveRejection`).
- The vendored verifier refuses a field the schema does not define, present or zero, at every table: a table's vtable may not be
  longer than its declared fields (THIRD-PARTY-NOTICES.md). The refusal is `VerifierRejected`.
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
them, runs the pinned flatc on the one schema file, `schemas/workshop_wire.fbs`, before it compiles, whenever its generated
sources are absent or older than that file or its project file (so a clean tree and a `--no-incremental` build regenerate, and
an unchanged tree does not). It replaces the previous output, writes one file
per schema type under `WireAccessors/` in its intermediate directory (`obj/<configuration>/<framework>/` by default) and
compiles those files as the compiler wrote them. The build refuses an `ENABLE_SPAN_T` define, so the nullable `ArraySegment`
byte accessors are the compiled surface. The C# static gate checks that, besides the SDK's and xUnit's own generated files,
only this compile receives sources from a build output directory, that they come from its generation folder and carry the
compiler's header, that they declare only schema types and the `Verify` classes the compiler writes for tables,
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
accessors; they are not generated. Unknown-field refusal is the vendored verifier's one derived rule, with no hand-written field
count. Some wire code is still hand-coded until its owner replaces it: the opaque `motion` byte vector and the non-FlatBuffers
boundary layouts (the pose ring, the clock and event rings and the physics GPU ABI records) until `ENGINE-WIRE-ABI`.

### TypeScript: generated in every Debug build

The TypeScript accessors the e2e suites import are build output too and are never committed. The same project's
`GenerateWireTypeScript` target runs the same pinned compiler, from the same inputs, on the one schema file,
`schemas/workshop_wire.fbs`, with `--ts`; the compiler writes the entry module `curious-contraptions/wire.ts`, which
re-exports every `enum`, `struct` and `table` the file declares. The C# step has already run when the TypeScript step starts.
The output is the ignored `tools/e2e/.wire/` of the tree being built, outside the output and
intermediate directories, so a clean does not delete it. The target reruns whenever the entry module or the schema record is
absent or older than the schema file, the C# generation stamp or the project file. It generates into `tools/e2e/.wire/.staging/`, refuses, naming them, any
file outside the wire namespace's module (`curious-contraptions/wire.ts` and `curious-contraptions/wire/`) and a missing entry
module, normalises every file there (the compiler's optional string-accessor parameter becomes `flatbuffers.Encoding` and its
`no-explicit-any` suppression is removed, so generated code follows the same strict typing contract as hand-written code; the
overloads and runtime expressions are unchanged), deletes the previous output, moves the per-type files and the schema record
`schemas.txt` (the one schema file name; a missing `schemas/workshop_wire.fbs` fails the build, and a second schema file
is refused by S1, `WireSchemaGuards`, and by the e2e runner, whose `RequireGeneratedWireModule` refuses a `schemas.txt` that
differs from the `schemas/*.fbs` names) into place and moves the entry module last, so until
the whole set is in place an output is absent or older than an input and the next build regenerates. A refusal removes the staging folder and leaves the previous output as it was; a failure after the previous output
is deleted leaves no entry module, so the next build regenerates and the e2e runner refuses to start meanwhile. Only the
Debug build of the project generates it, so each build graph writes the folder once (CI's Release publish builds its one Debug
instance through the web host's editor-metadata build). The TypeScript runtime the generated code imports is `flatbuffers`
at the compiler's version, an exact devDependency of the e2e suites' own manifest, `tools/e2e/package.json`, installed by
`npm ci` in `tools/e2e` when the browser phase runs the suites; the root manifest does not name it.

`tools/e2e/workshop-wire.ts` keeps only the hand-written helpers (the frame checks, the authored-state dump and the mock
encoders). A suite imports a generated name from the entry module (`./.wire/curious-contraptions/wire.ts`, or
`../.wire/curious-contraptions/wire.ts` from `unit/`) and a helper from `workshop-wire.ts`, which declares and re-exports no
schema type.

After a schema change, also update the field listing in `tools/e2e/run-reset-snapshot.ts` (`SNAPSHOT_SCHEMA`); its test fails until
it matches the schema.
