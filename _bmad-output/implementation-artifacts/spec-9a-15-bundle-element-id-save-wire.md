---
title: 'Story 9A.15: Saves and Messages Carry the Bundle Element Id'
type: 'refactor'
created: '2026-10-21'
status: 'in-review'
route: 'dispatch'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a15-saves-and-messages-carry-the-bundle-element-id'
---

## Intent

Since Story 9A.2 the one FlatBuffers codec already carries each instance's string `part_id`. What was left:
- `PartId.ToNumericId` (with its silent `_ => 0`) and `PartId.FromNumericId`, read only by `tools/Coverage` (F17-4) and tests;
- a hard-coded `Decodable` list in the save pre-validation instead of the loaded bundle;
- the worker decode's per-id generic list (`"pipe_bend_45" or "pipe_bend_90" or "funnel" or "powered_gate"`, formerly
  `WorkshopWireFlatBuffers.cs:938`) and its untyped `ArgumentException("Unsupported instance kind or parameters.")`;
- F16-4: a refused save must name the unknown id.

## Design (decided by the implementer under owner delegation)

- **Save boundary (main thread).** `WorkshopSaveCodec.Decode(IReadOnlySet<PartId> elements, bytes)` and
  `DecodeStored(elements, text)`. `WorkshopSavePrevalidation.Check` refuses any instance or inventory id not in `elements` with
  `WorkshopSaveRejection.UnknownElement`, message `"This save cannot be loaded: Part <n> names the element '<id>', which this game
  does not have."`. `ui/Workshop.cs` passes `World.Registry.Elements`, the set `PartRegistry.Load` builds atomically from the
  bundle's deliverable entries (the typed undelivered set excluded). The parameter is a plain `IReadOnlySet<PartId>` because
  `engine/gpu` also compiles into the Simulation worker, which has no registry.
- **Worker boundary.** The worker loads no bundle. `MapInstance` decodes an id with a typed record arm as before; every other id
  is a `WorkshopGenericInstance` of its declared colliders (`WorkshopCanonicalColliders.Declared`, which replaces the per-id list);
  an id with neither is `WireFaultException(WireFaultReason.UnknownElement, "... names the element '<id>' ...")`, raised before
  the router's executor. `WorkshopCanonicalColliders` stays the worker's single collider source until Story 9A.16 carries the
  bundle's colliders on the wire; it is not a fallback. A parity test proves every loaded element is known to the worker decode.
- **Traces.** `CuriousContraptions.Simulation/WorkshopTrace.cs:43` encodes its Construct command through the same codec, so the
  trace carries the same `part_id` strings; `WorkshopTraceRecord` and `PhysicsBodyWire` carry no element identity at all.
- **Coverage.** `CurrentCatalogue.DeclaredPartIdentities` reads `Elements[].Metadata.Id` from the generated
  `elements/elements.bundle.json` (a build output; a missing bundle is refused naming `tools/BundleElements`).
- No format version, no capacity, no migration: the schema is unchanged and already string-only.

## Deletes

- `PartId.ToNumericId` and its `_ => 0` default, and `PartId.FromNumericId` (`engine/PartId.cs`).
- The hard-coded `Decodable` element list in `engine/gpu/WorkshopSavePrevalidation.cs`.
- The per-id generic decode list and the untyped unknown-kind `ArgumentException` in `engine/gpu/WorkshopWireFlatBuffers.cs`.
- Coverage's `ToNumericId` table regex (`tools/Coverage/CurrentCatalogue.cs`) and the numeric-id test assertions
  (`PartRegistryTests`, `StateLatchTests`, `Coverage.Tests/CurrentCatalogueTests`).
- No FlatBuffers schema or save field carried a numeric part id after Story 9A.2; `NoSchemaFieldCarriesANumericPartId` keeps it so.

## Out of scope (kept, with their owners)

- The main-thread placement capture keeps its own per-id arm and collider fallback
  (`engine/MachineWorld.Gpu.cs:131-132`): the `CaptureInstance` decode arms belong to the A3 conversions and the
  `?? WorkshopCanonicalColliders.For` fallback to Story 9A.16. It is not a serialisation boundary.
- `WorkshopPorts.For` (`engine/gpu/WorkshopConnections.cs:39`) is Story 9A.17's.

## Implementation notes

The code commit is `d3f17d27`, which Murdoch-23 passed in round 1. The commit on top of it adds evidence only. The generic-list
guard used for M9 (`TheWorkerDecodeKeepsNoPerIdGenericList`) was a working-tree test and is not committed (see `mutants.txt`).
Committing it is filed as a follow-up.

**The AC1 boundary, stated explicitly.** Untrusted element ids enter the game in one place: a stored save. A stored save is
validated against the loaded bundle (`World.Registry.Elements`) before any domain construction. A worker message is built only
from ids already validated there: ids from the registry, from a save it admitted, or from the C# lessons' constants. So every id
a message carries was validated against the loaded bundle at a serialisation boundary.

The worker has no bundle. As a second line of defence, it refuses by name any id it cannot decode
(`WireFaultReason.UnknownElement`). `EveryLoadedElementIsKnownToTheWorkerDecode` proves that every loaded element is known to
the worker decode, so the worker never refuses an element the save boundary admitted. Traces encode the same Construct
command, so they carry the same strings.

| AC | Proof | Mutant (`evidence-9a-15/mutants.txt`) |
|---|---|---|
| 1. Each instance carries its typed `PartId` string, validated against the loaded bundle at the boundary | `ASaveCarriesEachInstancesBundleElementIdString`, `AWorkerMessageCarriesEachInstancesBundleElementIdString`, `ASaveIsValidatedAgainstTheLoadedBundle` (a `ramp` save is refused, naming `'ramp'`, when the passed set omits it; it loads when the set includes it), `TheRegistryElementsAreTheLoadedBundlesDeliverableElements`, `EveryLoadedElementIsKnownToTheWorkerDecode` | M1 (re-add `Decodable`), M7 (the registry set leaks undelivered ids) |
| 2. An unknown id is rejected atomically, naming the id | Save: `ASaveNamingAnUnknownElementIsRefusedNamingIt` (`kite`, `tennis`, `balloon`), `AStoredSaveNaming…`, `TheUndeliveredParts…` (F16-4). Message: `AWorkerMessageNaming…`, `TheWorkerRouterRefusesAnUnknownElementBeforeItsExecutor`. Chrome: `engine-core-2b1` test 3 (the refusal names `'kite'`, the Load issues no command, the bench is kept, and the intact save loads) | M2 (unnamed), M3 (silent admit), M4 (untyped `ArgumentException`) |
| 3. `rg -w "ToNumericId\|FromNumericId"` returns 0 hits, and no schema or save field carries a numeric id | `deletion-rg-proof.txt`; `NoNumericPartIdConversionRemains`, `NoSchemaFieldCarriesANumericPartId` | M5, M5b (re-add `To`/`From`), M6 (`part_id: uint32`), M8 (Coverage, F17-4), M9 (re-add the generic list: red only with the uncommitted guard) |
| 4. No format versions; an invalid save gets a player-visible message, with no migration | The schema is unchanged and has no version field; the refusal reaches the status line through `WorkshopUiFailure.Report` (`engine-core-2b1` tests 2 and 3) | covered by M2 |
| 5. Every suite that saves and loads passes | The Full run below | — |
| 6. 0 warnings, `dotnet test` 100%, every e2e suite 100% in one run | `tier1-build.txt` (default and PlaytestDiagnostics), `tier1-release-build.txt`, `tier1-test.txt` (2,258 tests, 0 failed), `tier1-node.txt`, `wire-contract.json`, `lifecycle-contract.log`, `ownership-oracles.log` (`--oracles .`, exit 0), `tier1-anvil.txt`. Full run below | — |

**Chrome.**
- `targeted-1-working-tree`: Pass. The new test 3 and the smoke pair ran before the commit.
- `e2e-full-d3f17d27`: the one Full run, on the committed code. All 47 suite records Pass, with 0 failures. The verdict is
  `Incomplete(Contended)`: three SerialAlone suites (clock-master, pkg-3, arch-9a-catalogue) passed only under contended
  observations. The load came from other sessions on the machine (load average 7–8 against a threshold of 7.23), not from this
  run.
- `e2e-serial-rerun-d3f17d27`: verdict `Pass`. The same commit reran those three suites plus the smoke pair, and each has a
  Clean passing record.
- The Full run's timing counters are the same as the 9A.2 and 9A.14 Full runs: clock-master closeFindings 1, unresponsive 1,
  debtFaults 1, renewedLate 1, recal 4.
