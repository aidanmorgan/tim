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

See the [evidence](evidence-9a-15/) directory and the commit message for the per-AC proof.
