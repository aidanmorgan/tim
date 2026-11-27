# Workshop wire schema

`workshop_wire.fbs` is the one schema file (owner decision 24p): the ABI between the systems and threads. It declares runtime
commands, responses, diagnostic observations and compiled capability installations, and the authored construction and save
records. It has no `include`, one `root_type` (`SavedConstruction`, the root that lives in files) and one `file_identifier`. Core
owns the runtime codec (`WorkshopWireFlatBuffers`); neither worker receives authored instances or loads element packages.

- **Authoring declarations are marked.** The file declares `attribute "authoring";` and marks each authored-construction and
  save declaration `(authoring)`. Every declaration that references a marked declaration is itself marked (closure under
  reference, enforced by S9). The main assembly owns `WorkshopAuthoringFlatBuffers` and `WorkshopSaveCodec`. No runtime root
  reaches a marked declaration, and no compile of Core, Simulation or Animation.Worker references one, apart from Core's
  save-frame verification behind `ICommandCodec.ValidateSaveFrame` (`WireSchemaGuards`). Main compiles authoring into the runtime
  records before dispatch; there is no second decoder or old-format path.
- **Root identifiers are schema data.** `enum WireRootIdentifier : uint32` has one member per root, named as its root table,
  whose value is the root's four identifier bytes read as a little-endian `uint32` (`WorkshopCommand`, `WorkshopResponse`,
  `WorkshopObservationBatch`, `SavedConstruction`). Every buffer is size-prefixed and carries its root's identifier. C# converts a
  member with the one conversion, `WorkshopWireFlatBuffers.FileIdentifier`, for `FinishSizePrefixed` and the frame boundary; the
  save encoder uses the generated root finisher. TypeScript converts a member with `rootIdentifier` in `tools/runtime/wire-frame.ts`.
- Both languages check the size prefix and root identifier before decoding. C# also runs the generated FlatBuffers verifier
  and reports a typed `WireFaultReason` or `WorkshopSaveRejection`. Table verification for TypeScript belongs to `ENGINE-WIRE-ABI`.
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
sources are absent or older than that file, `tools/runtime/FlatcCompiler.targets`, the host-selection import
`tools/runtime/RuntimeTypeScript.targets`, or its project file (so a clean tree and a `--no-incremental` build regenerate, and
an unchanged tree does not). It replaces the previous output, writes one file
per schema type under `WireAccessors/` in its intermediate directory (`obj/<configuration>/<framework>/` by default) and
compiles those files as the compiler wrote them. The build refuses an `ENABLE_SPAN_T` define, so the nullable `ArraySegment`
byte accessors are the compiled surface. The C# static gate checks that, besides the SDK's and xUnit's own generated files,
only this compile receives sources from a build output directory, that they come from its generation folder and carry the
compiler's header, that they declare only schema types and the `Verify` classes the compiler writes for tables,
that together they declare every schema type, and that the compile has no `ENABLE_SPAN_T` define.

The build provisions the compiler itself and never uses a `flatc` on `PATH`. It downloads the official release archive for the
build host (`Mac.flatc.binary.zip` on macOS Arm64 and X64, `Linux.flatc.binary.g++-13.zip` on Linux X64; any other host fails
the build), verifies the archive and binary SHA-256 digests pinned in `tools/runtime/FlatcCompiler.targets`, and keeps the
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

### TypeScript: generated in every build graph

The TypeScript accessors are build output too and are never committed. `tools/runtime/RuntimeTypeScript.targets` runs
`GenerateWireTypeScript` in `CuriousContraptions.Abstractions` ahead of `BuildRuntimeTypeScript`, using the same pinned compiler
from `tools/runtime/FlatcCompiler.targets` on the one schema file, `schemas/workshop_wire.fbs`, with `--ts --gen-object-api`.
The compiler writes the entry module `curious-contraptions/wire.ts`, which re-exports every `enum` and every `struct`/`table`
together with its `<Name>T` object-API class. The output is the ignored `tools/e2e/.wire/` of the tree being built, outside the
output and intermediate directories, so a clean does not delete it. `StampWireTypeScriptInputs` writes a configuration-neutral stamp under the Abstractions
artifact base with `WriteOnlyWhenDifferent`. Clean deletes the stamp so a clean or `--no-incremental` build regenerates.
An unchanged incremental build reuses the generated output. It generates into the building instance's intermediate staging directory, refuses
stray files outside `curious-contraptions/wire.ts` and `curious-contraptions/wire/` or a missing entry module, normalises every
file (replacing `optionalEncoding?:any` with `optionalEncoding?:flatbuffers.Encoding` and removing the `no-explicit-any` suppression),
deletes the previous output, moves the per-type files into place and moves the entry module last.

`BuildRuntimeTypeScript` invokes the repository-local tsc directly and compiles the generated module, `tools/runtime/wire-frame.ts` (the one
TypeScript frame check and `WireRootIdentifier` conversion) and `tools/runtime/wire-conformance.ts` in every configuration, and
`WireConformanceTests` proves C# and TypeScript agree in both directions, including motion-vector presence on responses.
The TypeScript runtime `flatbuffers` is pinned at the
compiler's version (`25.2.10`) in the root `package.json` `devDependencies` and `package-lock.json`.

`tools/e2e/workshop-wire.ts` keeps only the authored-state dump and the mock encoders, importing `openFrame` and `rootIdentifier`
from `../runtime/wire-frame.ts` and generated types from `./.wire/curious-contraptions/wire.ts`.

After a schema change, also update the field listing in `tools/e2e/run-reset-snapshot.ts` (`SNAPSHOT_SCHEMA`); its test fails until
it matches the schema.
