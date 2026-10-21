---
title: 'Story 9A.14: Bundle Is the Sole Catalogue Source (with Story 9A.18, element icons ship in their packages)'
type: 'feature'
created: '2026-10-21'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a14-bundle-is-the-sole-catalogue-source'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a18-element-icons-ship-in-their-packages'
  - '{project-root}/docs/architecture/element-package-isolation.md'
---

## Intent

The generated element bundle becomes the only catalogue the game loads. Every definition is a plain runtime record built from one bundle entry; its scene and icon are files in its own `elements/<Name>/` package. The `.tres` catalogue, its loader and the static palette list are deleted in the same story, and so are the element arms of `ui/WorkshopIcons.cs`. Nothing the player sees changes: the palette titles, order and icons are pixel-identical to the pre-cutover palette. The Tennis ball and Balloon stay out through the typed undelivered set until Story 12.1.

## Implementation Notes

Amelia-20, 21 Oct 2026. Base: main `a8a3eab1` fast-forwarded, merged with PKG-3 `8b646ffe`. Evidence: [evidence-9a-14/](evidence-9a-14/).

**Deletes** (each proven by `rg` and a re-add test; see [deletion-rg-proof.txt](evidence-9a-14/deletion-rg-proof.txt)):
- all 24 `parts/catalog/*.tres` (Tennis and Balloon included), the 13 remaining `parts/scenes/*.tscn` (100% `git mv` renames into their packages) and the emptied `parts/` directory, with its stale ignored bundle;
- the Godot `Resource` form of `engine/PartDefinition.cs` (now a plain sealed class) and `engine/BallMaterialResource.cs`;
- `PartRegistry.Discover()`, its `.tres` scan, the `clock.tres` Parameters exemption (also its copy in `MachinePart.Configure`) and the `Ball.Capture` resource-bits path;
- the static Free palette list: `WorkshopInventoryPolicy._canonicalOrder`, `CanonicalOrder`, the lazy static `Free` and `GenerateFree`;
- `PartId.Tennis`, `PartId.Balloon`, their `ToNumericId` and `FromNumericId` arms (13 and 14 now refuse) and their `WorkshopPorts.For` arm;
- every element arm of `ui/WorkshopIcons.cs`, delivered and undelivered; the UI-control and socket-choice pictograms stay, and the table's default arm now refuses an unknown name instead of drawing an arrow;
- the `BundleCatalogueParityTests` cutover guard: its catalogue half is deleted with the catalogue, after the extended proof below ran green on the last tree that still had the `.tres` files; the comparisons against runtime tables that still exist moved to `PackageRuntimeParityTests` (CL4);
- in `tools/E2eRunner`, the `parts/catalog`, `parts/icons` and `parts/scenes` rules and the `.tres` catalogue reader; in `tools/Coverage`, the `.tres` resource binding.

**What was built:**
- **One shared schema, `CuriousContraptions.Bundle`** (trimmable, source-generated System.Text.Json, closed exact-name enum converters): `BundleDocument`, `BundleEntry`, the domain payloads, `BundleSchema.Read/Write/Entry` and `BundleValidator`, moved out of `tools/BundleElements`. The generator and the game read and validate the same schema with the same structural checks.
- **`ElementMetadata`** gains `DisplayName`, `Description`, `Color` (`ElementColor`), `PaletteOrder`, and a required `IconPath` (`DisplayNameKey` is gone). Every package declares its catalogue title, description, colour and category, its palette order (the canonical order, keeping 13 and 14 free for Story 12.1) and its own `icon.svg`.
- **Validation.** New structural rules: positive, distinct palette orders (BUNDLE008), non-empty unpadded titles, descriptions and port ids (BUNDLE010), colour channels in [0, 1], unit port normals, and `res://elements/` asset paths (BUNDLE007). The generator also requires both assets to be files in the package's own directory (BUNDLE009): a missing icon fails the bundle build.
- **Icons (9A.18).** Each package ships `icon.svg` with the exact SVG the old pictogram composed, imported with Godot's `keep` importer so the export carries its bytes; `WorkshopIcons.Element(IconPath)` rasterises it exactly as before (`LoadSvgFromString(svg, 2)`).
- **Runtime.** `PartRegistry.Load()` reads `res://elements/elements.bundle.json` only; a missing file, malformed JSON, an unknown field or enum, any validation error, an unloadable scene or a missing icon throws `InvalidDataException` naming the bundle and the problem, and nothing is kept from a refused load. The Workshop loads the registry inside its start-up path, so a refused bundle stops start-up with the message on the status line (`PARTS UNAVAILABLE`) and a Startup fault. Ball materials come from the bundle (radius, mass, bounce, drag, friction, bounce threshold, rolling resistance, at the binary16 lanes) and are still checked against `BallMaterial.For`. The switch button's colour is the bundle colour. `PartRegistry.Free` is the Free palette in ascending palette order minus `UndeliveredParts`.
- **Export.** The four presets export all resources (minus package `bin/` and `obj/` and the Abstractions sources) and include the bundle and every `icon.svg`; the published `godot.pck` holds the bundle, the 22 package scenes and icons.
- **Chrome suite `tools/e2e/arch-9a-catalogue.test.ts`** (SerialAlone, new-suite rule) with byte-exact palette baselines in `tools/e2e/baselines/`.

## Spec Change Log

Amelia-20, under the owner's delegation to decide small gaps; no AC is weakened.

- **CL1. Order over grouping.** The AC asks for the palette "grouped by declared category, with the same titles, categories and order the player saw before". The palette before the cutover was flat in canonical order with no category headers, so grouping by category would reorder it and break the screenshot comparison, the 9A.4 "no reshuffle" decision and every suite that presses palette rows by position. Packages declare `PaletteOrder`, the registry lists rows in that order, and each definition carries its declared category.
- **CL2. Baseline.** No palette screenshot exists in the Story 9.4b evidence. The baselines were captured from the published build of the base `8b646ffe` (the 9.4b palette minus Tennis and Balloon, removed by Story 9R.0c), byte-identical across two captures ([base-capture](evidence-9a-14/base-capture/)).
- **CL3. Parity at the cutover boundary.** The catalogue-to-package proofs were run against the oracle before it was deleted: the title, palette order and icon pixels in the extended `BundleCatalogueParityTests` (Godot, on the tree that still had the `.tres` files), and the title, description, colour and category of all 22 packages (F15-10) in a one-shot oracle test against the `.tres` text exported from `8b646ffe`. Both test files are kept as evidence (`.cs.txt`) with their red mutants; they cannot run after the oracle is deleted.
- **CL4. `PackageRuntimeParityTests`.** The guard's comparisons against runtime tables that this story does not delete (ports, colliders, footprint, bodies, sliders, placement defaults, ball materials) stay, renamed, until each table's owning story (9A.16, 9A.17, the conversions) deletes it. F15-11 adds the signal declarations the runtime reads: the switch trigger threshold (compiled contact trigger), the domino topple threshold (compiled orientation sensor angle) and the counter target. Level thresholds, pulse durations, latch modes, event kinds and port normals have no runtime reader yet; port normals gain a structural unit-length rule instead.
- **CL5. Description and colour (F15-10).** Both are declared and carried in the bundle; the colour drives the switch button, and the description is carried for the drawer (no current UI reads it).
- **CL6. UI pictograms.** Story 9A.18 keeps the UI-control and socket-choice pictograms; `socket_set`, `socket_reset`, `move`, `front` and `cube`, which have live callers, are kept too.
- **CL7. History hits.** The AC's `rg "res://parts/catalog|BundleCatalogueParityTests"` returns hits only in history: the Story 6.5b planning cards (`docs/planning/invest/{decisions,vertical-delivery,engine}.md`), the isolation authority's closed-findings row for Story 9A.4, and the `_bmad-output` records.
