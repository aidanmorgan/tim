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
- **CL7. Stale references reworded.** The three Story 6.5b planning lines (`docs/planning/invest/{decisions,engine,vertical-delivery}.md`) and the isolation authority's closed-findings row for Story 9A.4 no longer name the deleted path or test; the AC's `rg "res://parts/catalog|BundleCatalogueParityTests"` now hits only the `_bmad-output` records. The isolation authority's A13, A15 and A3 (icons) rows describe the delivered state and close at 9A.14's terminal Pass.

## Verification

**Identities.** Code commit `0645f57d`; merge of main `679c36cd` (PKG-3 done; `_bmad-output` only, no code difference) as `57826e42`; the follow-up docs commit adds the F15-11 event-kind assertion (test only), the doc rewording and this record. Full run bundle identity `c12a6d4e750a2b7f9ec4e14da4073c6a7c91814b5116dedc2890605189068ffe` (start = end), Chrome 155.0.8059.86.

**Per-AC proof.**

| AC | Proof | Evidence |
|---|---|---|
| 9A.14 registry loads only the bundle; plain records; package scenes | `PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder`, `Create_InstantiatesAndConfiguresEachLoadedPart`, `NoCatalogueResourceOrResourceLoaderRemains` | tier1-test.txt |
| missing or invalid bundle stops start-up naming the problem | `AnInvalidBundleRefusesTheWholeLoadNamingTheProblem` (9 cases), `AMissingOrEmptyBundleRefusesTheLoad`; `Workshop.ReadyAsync` shows the message | tier1-test.txt |
| palette lists every bundle element, same titles, order and icons | Chrome `arch-9a-catalogue` palette screenshots byte-equal to the base capture; native title/order/icon parity before the cutover | e2e-full-57826e42/, base-capture/, green-parity-before-cutover.txt |
| Tennis and Balloon absent | `AnUndeliveredElementInTheBundleNeverReachesTheRegistryOrThePalette`, `PalettePlacementTests`, palette-placement suite | tier1-test.txt, e2e-full-57826e42/ |
| `arch-9a-catalogue` in Chrome: 22 rows, each placed, one bench run/reset/save-load | 25/25 in the full run | e2e-full-57826e42/arch-9a-catalogue.tap |
| old loader and static list gone (`git ls-files`, `rg`) | 0 tracked files under `parts/`; re-add tests | deletion-rg-proof.txt, mutant-deletion-readds.txt |
| 9A.18 icon.svg via IconPath, palette renders it | `WorkshopIconsKeepsNoElementArm`, pixel parity, Chrome screenshots | mutant-parity-title-icon.txt |
| 9A.18 missing icon fails the bundle build | `AMissingOrForeignPackageAssetFailsTheBuild`; the real build fails with BUNDLE009 | red-missing-icon-build.txt |
| `WorkshopIcons.cs` keeps no element arm | `rg` 0 for every element id; re-add test | deletion-rg-proof.txt |
| F15-10 title, description, colour for every package | oracle parity against the `.tres` text of `8b646ffe` | CatalogueOracleParityTests.cs.txt, green-oracle-parity.txt |
| F15-11 signal declarations the runtime reads | `EverySignalDeclarationTheRuntimeReadsEqualsTheRuntime` | f15-11-runtime-readers.txt |
| 0 warnings, tests 100%, full Chrome run 100% | Debug and Release 0 warnings; 2,259/2,259 tests; full run Pass 44 files, 240/240 | tier1-release-build.txt, tier1-test.txt, e2e-full-57826e42/summary.txt |

**Mutants** (each red, then reverted): palette order, title and icon pixels (mutant-parity-order.txt, mutant-parity-title-icon.txt); colour and description (mutant-oracle-colour.txt, mutant-oracle-description.txt); missing icon (red-missing-icon-build.txt); switch trigger threshold, domino threshold, counter target and clock event kind (mutant-f15-11-*.txt); re-added static palette list, `.tres` scan and `"ball"` pictogram arm (mutant-deletion-readds.txt); palette order in Chrome (mutant-e2e-palette-order.tap: the screenshot test fails).

**Tier 1** (on `57826e42` plus the test-only and docs edits): `dotnet build` Debug and Release 0 warnings; `dotnet test CuriousContraptions.slnx` 2,259/2,259 (tier1-test.txt); Ownership `--oracles .` exit 0, 22/22 attacks rejected (ownership-oracles.log); WireContract 108 oracles, 0 failures, LifecycleContract exit 0 (wire-contract.json, lifecycle-contract.log); Node harnesses 88/88 (tier1-node.txt); `anvil check` on the changed non-C# files, 0 warnings. WireContract, LifecycleContract and the Node harnesses ran before the commit; the later edits touch tests and docs only.

**Runner.** `select --diff 8b646ffe` refuses with `NoPrimarySuite` for `ball` (runner-select-diff-8b646ffe.txt), as PKG-3 CL4 recorded, so the one full run used the `full` verb on the committed merge `57826e42`: verdict Pass, 44 files, 240/240 tests, every observation Clean, no rerun (e2e-full-57826e42/summary.txt). The follow-up edits are `CuriousContraptions.tests/**` and `docs/**`, both NoBrowserImpact.

**Round 2 (Murdoch-21), fix commit `786e4147`.**
- **B1.** A `null` entry in `Elements` (or a null parameter option) threw `NullReferenceException` in `PartRegistry.Read`. `BundleSchema.Read` now refuses it with a `JsonException` naming the path (`Elements[0] is null.`), which the registry reports as `The element bundle '…' is malformed: …`. Red before the fix: red-b1-null-element.txt (two new `AnInvalidBundleRefusesTheWholeLoadNamingTheProblem` cases); green: green-round2-registry.txt.
- **F16-2.** New ratchets: `WorkshopIconsKeepsNoElementArm` pins the pictogram arms to the 14 UI names; `TheUndeliveredPartsHaveNoPartIdConstantOrWireIdentity`; `NoCatalogueResourceOrResourceLoaderRemains` now also refuses `BallMaterialResource` (file, compile item, type, source). Re-adding a `"motor"` arm, `PartId.Tennis` or `engine/BallMaterialResource.cs` each fails (mutant-f16-2-readds.txt).
- **Tier 1:** Debug and Release 0 warnings; 2,262/2,262 tests; Ownership exit 0; WireContract, LifecycleContract and Node 88/88 (round2-*).
- **Chrome:** `select --diff de4d1ea5` is Full only because `CuriousContraptions.Bundle/BundleSchema.cs` matches the shared-runtime rule (round2-select-diff-de4d1ea5.txt); the change is a reader-side null refusal with byte-identical bundle output, so the story's own suites ran instead (no Full, per the coordinator): `run --changed` arch-9a-catalogue 25/25, palette-placement 25/25 and the smoke pair, Pass on `786e4147` (round2-e2e-786e4147/summary.txt).

**Filed (non-blocking residuals).**
- The start-up refusal is proven at the registry; no Workshop-level native test drives `ReadyAsync` with a refused bundle.
- The palette baselines are byte-pinned to this Chrome build (155.0.8059.86); a browser update needs a recapture.
- F15-11 fields with no runtime reader: trigger pulse durations and modes, level thresholds, latch modes and the other event kinds (9A.22, 9A.23 + 9A.25b and the family conversions); port normals have a structural rule only.
- `PartDefinition.Description` has no UI reader yet.
