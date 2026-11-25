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
clock and event rings and the physics GPU ABI records) until `ENGINE-WIRE-ABI`, and the generated part of the TypeScript
module below until S5b.

### TypeScript: the generated part of `tools/e2e/workshop-wire.ts`

The e2e suites import one module. Its first part is flatc's per-type output joined in schema declaration order without the
per-file imports; everything after the `hand-written helpers` marker is maintained by hand (the frame checks, the authored-state
dump and the mock encoders) and is kept when the generated part is replaced. Normalize the compiler's optional string-accessor parameter to `flatbuffers.Encoding` and remove its `no-explicit-any` suppression; generated code follows the same strict typing contract as handwritten code. This annotation-only postprocessing preserves the overloads and runtime expressions. Use the pinned compiler the build cached (above), never a `flatc` on `PATH`; any build of the solution provisions it. With `OUT` an empty scratch directory and, on macOS,
`FLATC="$HOME/Library/Application Support/CuriousContraptions/flatc/25.2.10/Mac.flatc.binary/flatc"` (on Linux,
`FLATC="${XDG_DATA_HOME:-$HOME/.local/share}/CuriousContraptions/flatc/25.2.10/Linux.flatc.binary.g++-13/flatc"`):

```sh
"$FLATC" --ts -o "$OUT" schemas/workshop_wire.fbs schemas/workshop_authoring.fbs
{
  echo '// automatically generated by the FlatBuffers compiler, do not modify'
  echo 'import * as flatbuffers from "flatbuffers";'
  echo
  for name in $(grep -h -E '^(enum|struct|table) ' schemas/workshop_wire.fbs schemas/workshop_authoring.fbs | awk '{print $2}' | sed 's/:.*//'); do
    file=$(echo "$name" | sed -E 's/([a-z0-9])([A-Z])/\1-\2/g' | tr 'A-Z' 'a-z')
    grep -v -E '^// automatically generated|^import ' "$OUT/curious-contraptions/wire/$file.ts" | cat -s | sed '1{/^$/d;}; s/optionalEncoding?:any/optionalEncoding?:flatbuffers.Encoding/g; s/, @typescript-eslint\/no-explicit-any//g'
    echo
  done
} > generated.ts   # then replace the part of tools/e2e/workshop-wire.ts above the helpers marker with generated.ts
```

After a schema change, also update the field listing in `tools/e2e/run-reset-snapshot.ts` (`SNAPSHOT_SCHEMA`) and the record field
lists in `CuriousContraptions.tests/WorkshopFormatTests.cs`; both tests fail until they match the schema.
