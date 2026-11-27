# S5c-2 Task 1 probes

Inputs are from the carried candidate in `slice-s5c-2` on `cb29a0763d2bba289e5018b82698e3a7f86478a3`. The inherited `.tools/s5c-evidence` artifacts were left intact. These probes wrote separate ignored build outputs under `.tools/s5c-evidence/takeover`.

## Object API and strict import

The compiler was `$HOME/Library/Application Support/CuriousContraptions/flatc/25.2.10/Mac.flatc.binary/flatc`. It reported `flatc version 25.2.10` and matched the pinned compiler digest in `tools/runtime/FlatcCompiler.targets`. The command was:

```sh
"$s5c_flatc" --ts --gen-object-api -o .tools/s5c-evidence/takeover/raw schemas/workshop_wire.fbs
```

The direct scan independently read declarations from `schemas/workshop_wire.fbs` and compared their names and kinds with the entry exports and every per-type declaration in the unmodified `raw/curious-contraptions` tree. Every table or struct exported its accessor and `T` companion with the generated interfaces. Every enum exported only its enum. The probe copied raw output into `normalized/` for compilation and applied the same two replacements as `GenerateWireTypeScript` in `tools/runtime/RuntimeTypeScript.targets`: `optionalEncoding?:any` to `optionalEncoding?:flatbuffers.Encoding` and removal of `, @typescript-eslint/no-explicit-any`. The raw tree was left intact. Output:

```text
Schema declarations 137, generated export rows 137, object API pairs 128, enum/union exports 9
Production normalization replacements: optional Encoding annotations 10, comment suppressions 138
```

Two stub modules used the generated identifier and the FlatBuffers size-prefixed frame and object API. `wire-frame-stub.ts` checks the save size prefix and identifier. `conformance-stub.ts` packs a `SavedConstructionT`, finishes it with the generated save finisher, then uses `getRootAsSavedConstruction(...).unpack().nextBodyId`. Direct CLI options refused `rootDirs` (TS6064), so `tsconfig.probe.json` extends `../../../tsconfig.runtime.json`, overrides only `rootDir` and `outDir` and includes those two stubs. Their imports compile the entire normalized generated module. The commands ran on Node `v26.12.1` and `node node_modules/typescript/bin/tsc --version` reported `Version 5.9.3`.

```sh
node node_modules/typescript/bin/tsc -p .tools/s5c-evidence/takeover/tsconfig.probe.json
node node_modules/typescript/bin/tsc --project tsconfig.runtime.json --noEmit
```

Both exited 0. A Node ESM invocation imported the emitted entry and conformance stub, used `createRequire` relative to the emitted stub to resolve the loaded dependency, and asserted the package version and the decoded `uint64`. The following output replaces the repository path prefix with `$PWD`.

```text
root package $PWD/node_modules/flatbuffers/js/flatbuffers.js 25.2.10
generated roots {
  WorkshopCommand: 'CWCM',
  WorkshopResponse: 'CWRM',
  WorkshopObservationBatch: 'CCWO',
  SavedConstruction: 'CCWS'
}
decoded save probe uint64 18446744073709551615
```

The probe uses generated TypeScript on both sides of the stub round trip. S3–S7 use the production C# codecs for cross-language acceptance.

## Baseline graph replay

Original logs: `$HOME/.local/share/tim-toolchain/logs/slice-s5c/merged-round/{solution,playtest,web}.binlog`. The read-only replay used SDK 10.0.403 and `dotnet msbuild "$log" -nologo -noAutoResponse -verbosity:diagnostic`. The table extracts project contexts from `Target` rows and counts actual command messages once by Exec task ID. Each Exec also has a corresponding Command parameter row.

| Baseline graph | Abstractions RestoreRuntimeTypeScript and BuildRuntimeTypeScript visits | FlatBuffers actual `flatc --ts` |
| --- | --- | --- |
| Solution | 9:65 | 6:12, task 170 |
| PLAYTEST | 10 | 3, task 63 |
| Web | 10:19, 10:58, 10:144 | 5:7, task 64 |

All recorded Abstractions target bodies were up to date. Web visits were respectively ordinary host, `BuildWorkshopEditorMetadata` and `GenerateElementBundle`. FlatBuffers context 5:22 visited generation without an Exec. The baseline compiler command reads just `schemas/workshop_wire.fbs` and generates in the old FlatBuffers location. The candidate changes this ordering and requires its own replay.

## Copied project with networking denied

The copied tree contained exactly these build inputs before executing the target:

```text
./.editorconfig
./CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj
./Directory.Build.props
./Directory.Build.targets
./global.json
./schemas/workshop_wire.fbs
./tools/runtime/FlatcCompiler.targets
./tools/runtime/RuntimeTypeScript.targets
```

In `.tools/s5c-evidence/takeover/offline-copy`, the generation-only probe ran under the macOS network-deny sandbox:

```sh
/usr/bin/time -p /usr/bin/sandbox-exec -p '(version 1) (allow default) (deny network*)' \
  "$DOTNET_ROOT/dotnet" msbuild CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj \
  -t:GenerateWireTypeScript -p:Configuration=Debug -nologo -noAutoResponse -nodeReuse:false -verbosity:minimal
```

Exit 0. Timing from the tool was `real 0.36`, `user 0.30`, `sys 0.15` seconds. The generated entry had 137 exports. The copied tree had no `node_modules`, `obj/project.assets.json` or `tools/e2e/.wire/schemas.txt`. The target reused the pinned, integrity-checked compiler cache. The native fixture covers incremental schema edits, removal and interrupted output states in the full solution round.

## Response motion presence

The planning probe follows the production C# decoder's two `Invalid motion payload width` refusals. It used the diagnostic's incremental Debug Core, Abstractions and FlatBuffers assemblies. Generated source: `tools/e2e/.wire/curious-contraptions/wire/workshop-response.ts`, emitted at the pinned object API settings. Its `motionArray(): Uint8Array | null` reports presence. Its `unpack()` returns an empty array for absent motion and `WorkshopResponseT.pack()` unconditionally writes the vector.

The C# probe used the acknowledgement and read values from `WireConformanceTests.MakeResponse` and added a legal present admission motion to the same read via `PhysicsMotionRead.Decode` with an all-zero buffer of that body set's declared admission width. `WorkshopWireFlatBuffers.EncodeResponse` encoded each case. The strict TypeScript probe used the real `openFrame` and generated `WorkshopResponse` accessors. It used generated `startWorkshopResponse`, vector creation and typed `add*` methods, packed nested structs through their generated object API and called `addMotion` only for a present vector. It finished with the generated root identifier and checked the generated accessor on the result. Production C# compared decoded response fields and separately checked motion presence and decoded admission motion fields.

```sh
node node_modules/typescript/bin/tsc -p .tools/s5c-evidence/takeover/presence/tsconfig.probe.json
dotnet run --project .tools/s5c-evidence/takeover/presence/PresenceProbe.csproj -c Debug --nologo -noAutoResponse
```

The probe config extends `tsconfig.runtime.json`. Node `26.12.1`, TypeScript `5.9.3` and SDK `10.0.403` were selected. Both commands exited 0. Original stdout:

```text
Acknowledgement: generated motion absent, production C# decoded semantic response
Read: generated motion absent, production C# decoded semantic response
Read: generated motion present, production C# decoded semantic response
```

The probe exercised the unchanged repack. The required native suite also checks a named edit for each response and retains the existing cases for the other roots.

### Committed motion control

On the same commands and compiled production C# inputs, the follow-up probe replaced the zero-motion admission control with the existing `WireFaultTests.PhysicalRead(1)` fixture from the compiled test assembly. The TypeScript proof used the generated captures vector API for its one latch. The probe compared the non-motion response fields, checked presence on the generated accessor and the production C# value, and asserted the motion count, substeps, ordinal bounds, half-substep owner/sample with the existing metre position allowance, and committed-endpoint refusal. This exercises a complete nonempty committed window from the original input.

```text
Acknowledgement: generated motion absent, production C# decoded semantic response
Read: generated motion absent, production C# decoded semantic response
Motion: count 4, substeps 4, ordinals 0..4, body 1 sampled
Read: generated motion present, production C# decoded semantic response
```

The strict TypeScript compile and probe exited 0. Runtime and generated wire sources, FlatBuffers version and C# admission stayed the same.
