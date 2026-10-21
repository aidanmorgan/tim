# Review: Stories 9A.14 + 9A.18, Bundle Is the Sole Catalogue Source (icons ship in packages; F15-10, F15-11)

Reviewer: Murdoch-21 (independent; no deliverable edited). Rounds 1 and 2 (final). Date: 21 Oct 2026.

## Round 2 (final): terminal verdict **Pass**

Candidate `3be7f97c`; the fix is in `786e4147`. Over `de4d1ea5`, the code diff is `CuriousContraptions.Bundle/BundleSchema.cs` and `CuriousContraptions.tests/PartRegistryTests.cs` only. Evidence: [evidence-9a-14-review/round2/](evidence-9a-14-review/round2/).

- **B1 is closed.**
  - `BundleSchema.Read` now throws `JsonException` for a null `Elements[i]` or a null parameter option. `PartRegistry` wraps it as "The element bundle '…' is malformed: Elements[0] is null.", so `Workshop.ReadyAsync`'s named start-up refusal catches it.
  - The generator shares the same reader, so it is closed there too.
  - Her two new theory cases cover it.
  - My P2/P2b probe, re-run on `3be7f97c`: the `{"Elements": [null]}` case now refuses as a named `InvalidDataException`. 30 of 32 pass. The remaining two are the known non-defects: `SCENE_FOREIGN_PACKAGE` (F16-5, by design) and `SCENE_IS_ICON` (refuses correctly; only the Godot loader's logged errors fail the harness). Raw output: `round2/P2-P3-run.txt`.
- **F16-2 is closed.** Each of my round-1 survivors is now killed (`round2/mutants-summary.txt`, script `round2/run-readd-mutants.sh.txt`):
  - D6 (`"motor"` arm) by `WorkshopIconsKeepsNoElementArm`, which now checks the exact 14-arm set;
  - D8 (`PartId.Tennis`) by `TheUndeliveredPartsHaveNoPartIdConstantOrWireIdentity`;
  - D10 (`BallMaterialResource.cs` + compile item) by `NoCatalogueResourceOrResourceLoaderRemains`.
- **Tier 1, on a fresh copy `murdoch21-r2` at `3be7f97c`** (status 0 lines before and after; `round2/identity.txt`):
  - build, Debug and Release: 0 warnings;
  - `dotnet test`: 2262/2262 (1910 + 164 + 94 + 77 + 17), equal to her count;
  - pre-commit gate: exit 0, 123/123;
  - node 88/88; LifecycleContract 510/0; WireContract 108/0; Ownership: Positive, 22/22 attacks rejected;
  - `anvil check --since de4d1ea5`: no changed non-C# files.
- **Her story-suite run is reused; no Full run.** Run `20261021T052943Z-64074` on `786e4147`: arch-9a-catalogue 25/25, palette-placement 25/25 and the smoke pair, all Clean, debtFaults 0.
  - Its bundle identity `a8e2a981…a7b8` reproduces directly over her `AppBundle` (`round2/appbundle-manifest.txt`).
  - Its suite and runner digests reproduce from a clean `786e4147` (`round2/source-digests-786e4147.txt`).
  - The selection is adequate: the only runtime change is the bundle read at start-up, and arch-9a-catalogue starts the published build and places, runs, resets and save/loads all 22 elements.
- F16-1 and F16-3 to F16-7 stay filed to Story F.16. No player-visible regression.

## Round 1 verdict (superseded by round 2)

**Fail (round 1): one blocking finding, B1.**
- **B1 (blocking, AC breach).** A bundle whose `Elements` array holds a `null` entry (`{"Elements": [null]}`) throws `NullReferenceException` at `engine/PartRegistry.cs:74` (`entry.Metadata.Id` inside `Read`), not `InvalidDataException`.
  - `Workshop.ReadyAsync` catches only `InvalidDataException`, so start-up falls through to `_Ready`'s `HandlerFailed`.
  - Start-up does stop, but the player sees "That action failed and was logged: Object reference not set to an instance of an object." That message does not name the bundle or the problem, and no `FaultSite.Startup` fault is recorded.
  - The 9A.14 AC requires that a "structurally invalid bundle stops start-up with an explicit message naming the problem".
  - Raw evidence: probe P2, `probes/P2-P3-run.txt`, case `{"Elements": [null]}`, with the stack trace.
  - Of my 19 whole-bundle cases (P2), 17 refuse correctly as a named `InvalidDataException`: `{}`, `null`, `[]`, `""`, whitespace, `Elements: null`, null `Metadata`, a null item in `Ports` or `Colliders`, an empty or padded id, palette order 0 or −3, a null icon path, colour R 1.5, a scene path that is an svg, and bowling mass 4.5. One is B1. One (a scene in another package) loads by design and is filed as F16-5.
  - **The full extent of B1** (P2b, 11 cases). A null item refuses correctly in every other list of the schema: `Domains`, `Events`, `Ports`, `Colliders`, `Metadata.Parameters`, `Metadata.Parameters[].Options` (BUNDLE006), `Electrical.Nodes`, `Mechanical.AnchorPoints`, `Mechanical.Sliders`, `Mechanical.Sliders[].MovingBody.Colliders` and `Signal.Triggers`. No package declares a fluid payload. Their element types are structs, enums or validated strings. So the only list that admits a null is the top-level `BundleDocument.Elements` (`BundleEntry` is the one class-record list element).
  - **Fix scope for round 2:** reject a null `Elements` item in `PartRegistry.Read` or `BundleValidator`, as an `InvalidDataException` naming the bundle and the entry index, and add it as a case of `AnInvalidBundleRefusesTheWholeLoadNamingTheProblem`. Round 2 re-reviews that fix and its native test, Tier 1, and the runner's merge-diff selection for the changed runtime file (`engine/PartRegistry.cs`, which is in the web publish). The rule against repeating a Full run still applies.
- Every other AC of 9A.14 and 9A.18 holds, and so do F15-10 and F15-11 (table below).
- **No player-visible regression found.** The palette titles, order, colours and icons equal the pre-cutover catalogue, both in my own oracle (112/112) and in her identity-verified Chrome Full run. Placement, run, reset and save/load of every element pass. The production pck is clean. A save holding Tennis or Balloon is refused as the typed `UnsupportedSave`, exactly as at base.
- Seven findings are filed to **Story F.16, "Catalogue Follow-ups"** (F16-1 to F16-7, below). None blocks.

## Candidate and identities

| Item | Identity |
| --- | --- |
| Candidate worktree | `.claude/worktrees/agent-a1a1514d517207e6f` |
| Review head | `de4d1ea5`. Over the merge it changes only `CuriousContraptions.tests/PackageRuntimeParityTests.cs` (the F15-11 event-kind assertion), four docs and `_bmad-output` |
| Code commit | `0645f57d` |
| Merge | `57826e42` = `0645f57d` + main `679c36cd`. `git diff --stat 0645f57d 57826e42 -- . ':(exclude)_bmad-output'` is empty |
| Clean Tier 1 copy | `.claude/worktrees/murdoch21-9a14`, detached at `de4d1ea5`, fresh. `git status` was empty before and after every check (`tier1/identity.txt`) |
| Mutant and probe copy | `.claude/worktrees/murdoch21-mut`, detached at `de4d1ea5`, restored by `git reset --hard HEAD` after each mutant; final status empty |
| Identity copy | `.claude/worktrees/murdoch21-id`, detached at `57826e42` |
| Evidence | [evidence-9a-14-review/](evidence-9a-14-review/) |

## Acceptance criteria

| AC | Evidence | Result |
| --- | --- | --- |
| 9A.14. `PartRegistry` loads only the bundle; plain records; package scenes | `PartRegistry.Load()` reads `res://elements/elements.bundle.json` only. `PartDefinition` is a plain sealed class (`NoCatalogueResourceOrResourceLoaderRemains` asserts it is not a `Resource`). Scenes are `res://elements/<Name>/…`. `MachineWorld._Ready => Registry.Discover()` is gone, and the only `Load()` caller is `Workshop.ReadyAsync`. | Pass |
| Missing or structurally invalid bundle stops start-up with a message naming the problem | Her 9 cases refuse as `InvalidDataException` naming the bundle and the problem, and nothing is kept from a refused load. Of my 19 P2 cases, 17 refuse that way; one loads by design (F16-5); and **`{"Elements": [null]}` throws NRE: B1.** All 11 of my P2b null-item cases refuse. The Workshop path is read, not tested (F16-6). | **Fail (B1)** |
| Free palette lists every bundle element, grouped by declared category, same titles, categories and order as before (screenshot vs the 9.4b baseline) | My oracle `probes/oracle-compare.py.txt` reads the `.tres` at `679c36cd`, the old `_canonicalOrder` and the old pictogram strings, and compares them with the generated bundle: **112/112**. That is the id set (24 minus tennis and balloon), 22 × title, category, description and colour, the palette order, and 22 icon.svg byte compares (newline-stripped). Chrome: `arch-9a-catalogue` compares both palette screenshots byte-equal to `tools/e2e/baselines`, which equal her base capture (same SHA-256). I viewed both PNGs. **Ruling on CL1:** the base palette was flat. `RefreshPalette` at `679c36cd` iterates `_inventory.Keys` with no category use, so "grouped by category" and "same order as before" cannot both hold. Same order is the player-visible requirement, so I accept it; the AC wording is filed (F16-1). **Ruling on CL2:** her base capture is backed independently by my oracle and by the unchanged `LoadSvgFromString(svg, 2)` rasterisation. | Pass |
| Tennis and Balloon no longer appear | `UndeliveredParts` is a typed enum set. D12 (filter removed) is killed. Her Full run's palette-placement and arch-9a palette tests pass. `rg -i 'tennis\|balloon' engine ui` hits only `UndeliveredParts`. `content/puzzles.json` names tennis, but no runtime code reads it: the lessons are typed C#, and none grants tennis. | Pass |
| `arch-9a-catalogue` in Chrome: 22 rows, each placed through the drawer, one bench run, reset and save/load | Her Full run: 25/25, Clean (identity reproduced, below). | Pass |
| Old loader and static list gone: `git ls-files`, `rg`, no `.tres` scan, no static Free list | At `de4d1ea5`: `git ls-files 'parts/catalog/*.tres' 'parts/scenes/*'` returns 0 and `parts/` does not exist. `rg "res://parts/catalog\|BundleCatalogueParityTests"` returns 0 hits outside `_bmad-output` records. Re-add mutants are below. | Pass |
| 9A.18. `icon.svg` referenced by `IconPath`, palette renders it | `Workshop.RefreshPalette` uses `WorkshopIcons.Element(definition.IconPath)`. The pck carries `elements/*/icon.svg` raw (`keep` importer). Chrome screenshots are byte-equal. | Pass |
| 9A.18. Missing icon fails the bundle build | My V6 (delete `elements/Wall/icon.svg`): the build fails with `error BUNDLE009: Wall.Metadata.IconPath … is not a file in elements/Wall/`. | Pass |
| 9A.18. `WorkshopIcons.cs` keeps no element arm | Derived mechanically: 81 arm keys at `679c36cd`, 14 at HEAD (UI controls and sockets, plus `socket_set`, `socket_reset`, `move`, `front` and `cube` per CL6, all of which have live callers). `rg -c` for each of the 67 deleted keys returns 0 (`identity/workshopicons-deleted-arms-rg.txt`). The `Pictogram` default arm now throws, and every remaining caller (`SocketIcon`; `Apply` `custom:move`, `resize`, `front`, `cube`) names a kept key. | Pass |
| F15-10. Title, description and colour for every package | My oracle, 22 × 4 equal to the `.tres`. Her oracle `.cs.txt` before the deletion. | Pass |
| F15-11. Signal declarations the runtime reads are guarded | PKG-3's surviving probe P1 (switch trigger threshold 0.8 → 0.5), re-run as my V7: now **killed** by `EverySignalDeclarationTheRuntimeReadsEqualsTheRuntime`. The other fields have no runtime reader (her filed residual). | Pass |
| `dotnet build` 0 warnings, `dotnet test` 100%, every e2e suite 100% in one Chrome run against the production preview | Tier 1 below. Her Full run covers exactly the 44 `tools/e2e/*.test.ts` files at `57826e42` (`comm` of the file list against the summary is empty). It used the runner's Full verb, with a SerialAlone phase and then two parallel workers. The owner tiers supersede the literal "serial", as PKG-1, 2 and 3 ruled. | Pass |

## Deletions: re-add mutants (raw output in `mutants/<name>.txt`, script `mutants/run-mutants.sh.txt`)

Each mutant was built and run against the whole `CuriousContraptions.tests` (1907 tests).

| Mutant | Re-added | Result |
| --- | --- | --- |
| D1 | `parts/catalog/ball.tres` | Killed: `NoCatalogueResourceOrResourceLoaderRemains` |
| D2 | static `_canonicalOrder` and `CanonicalOrder` | Killed: `NoStaticFreePaletteListRemains` |
| D3 | `PartRegistry.Discover()` stub | Killed, by the build (CA1822), before the reflection check |
| D4 | a `.tres` catalogue scan pattern string in `PartRegistry` | Killed: `NoCatalogueResourceOrResourceLoaderRemains` |
| D5 | `"tennis"` pictogram arm | Killed: `WorkshopIconsKeepsNoElementArm` |
| D6 | `"motor"` pictogram arm (undelivered element) | **Survives** (F16-2) |
| D7 | `"ball"` pictogram arm | Killed: `WorkshopIconsKeepsNoElementArm` |
| D8 | `PartId.Tennis` constant | **Survives** (F16-2) |
| D9 | `FromNumericId` arm `13 => tennis` | Killed: `PartId_ValidatesValuesAndSupportsEqualityAndComparison` |
| D10 | `engine/BallMaterialResource.cs` (+ uid, + `Compile` item) | **Survives** (F16-2) |
| D11 | stray `parts/scenes/wall.tscn` | Killed: `NoCatalogueResourceOrResourceLoaderRemains` |
| D12 | `UndeliveredParts` filter removed from `Load` | Killed: `AnUndeliveredElementInTheBundleNeverReachesTheRegistryOrThePalette` |

The rule "delete old code in the same story" is met: every Deletes-list item is absent at HEAD. D6, D8 and D10 show only that three re-adds would go unnoticed by `dotnet test`. The AC's `rg` checks and the Story 9A.45a ratchet are the backstop.

## Value mutants and probes

| Mutant or probe | Result |
| --- | --- |
| V1. Ramp and Domino palette order swapped | Killed: `Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder`, `FreeInventoryListsEveryPlayableKindUnlimitedInPaletteRowOrder`, and others |
| V2. Wall title "Walls" | Killed only incidentally: a theory case's anchor text `"DisplayName": "Wall"` is missing. No native test compares a title (F16-3); the Chrome palette screenshot does |
| V3. Wall description edited | Survives (no reader at base or HEAD; F16-3) |
| V4. Switch colour R 0.94 → 0.5 | **Survives** native tests. The colour drives the switch button, and no test compares it (F16-3) |
| V5. Wall `icon.svg` bytes edited | Survives native tests; only the Chrome screenshot catches it (F16-3) |
| V6. Wall `icon.svg` deleted | Killed by the build: BUNDLE009 |
| V7. Switch trigger threshold 0.8 → 0.5 | Killed: `EverySignalDeclarationTheRuntimeReadsEqualsTheRuntime` |
| V8. Start-up refusal swallowed (empty catch) | Killed only by the structural gates `NoCatchBlockIsEmpty` and `EveryUiCatchReportsThroughTheClassificationOrIsAFaultOrRethrows`. No behavioural test at Workshop level (F16-6) |
| P2b. A null item added to each of the 11 lists in the schema (`NullItemPaths`) | All 11 refuse as named `InvalidDataException` (`probes/P2-P3-run.txt`) |
| P2. 19 structural bundle cases (`probes/Murdoch21Probes.cs.txt`) | 17 refuse as named `InvalidDataException`. `{"Elements": [null]}` throws NRE: **B1**. A ball scene path pointing at the Wall package's scene loads, because the runtime has no id-to-package map; only the generator's BUNDLE009 prevents it (F16-5). `SCENE_IS_ICON` refuses correctly; its harness failure is only the Godot loader's logged errors |
| P3. Saves with `switch` → `tennis` and `bowling` → `balloon` (same-length byte replacement) | Both are `WorkshopEditRejectedException(UnsupportedSave, "Unsupported instance kind or parameters.")`, and `WorkshopUiFailure.IsRejection` is true, so `LoadSaveAsync` shows "Load rejected; construction unchanged. …" and leaves the construction unchanged. Control: the unmodified saves decode. The base decode also had no tennis or balloon arm, so behaviour is unchanged. The message does not name the id (F16-4, for 9A.15) |

## Chrome: her Full run reused, identity reproduced

- Her run `20261021T040922Z-28185`, Full verb, on `57826e42`, 04:09:22Z to 04:49:46Z. Verdict Pass: 44 files, 240/240, every observation Clean. Bundle `c12a6d4e…8ffe`, the same at start and end. Chrome 155.0.8059.86.
- The clock-master figures (closeFindings 1, unresponsive 1, debtFaults 1) are that suite's established pattern: 21 earlier summaries in `_bmad-output` show the same figures.
- **Identity, from a clean `57826e42`** (`identity/source-digests-57826e42.txt`):
  - `suiteSourceDigest 09468cd0…` and `runnerSourceDigest ee0dbe73…` reproduce exactly.
  - `sourceStatusDigest 750652f0…` reproduces with exactly one dirty set out of 256 searched (`identity/status-digest-match.txt`, script `identity/status-brute.sh.txt`).
  - That set is modified `PackageRuntimeParityTests.cs` plus the four docs that `de4d1ea5` later commits, and untracked `f15-11-runtime-readers.txt`. These are exactly the paths whose mtime precedes the run start.
  - None of them is in the web publish: the test project and docs are not served. So the run applies to the code candidate.
- **Bundle identity reproduces directly.** Over her `CuriousContraptions.web/AppBundle` (306 files), the runner's manifest (sorted `path\tsha256\n` lines, then SHA-256) is `c12a6d4e750a2b7f9ec4e14da4073c6a7c91814b5116dedc2890605189068ffe`. That equals the run's start and end identity (`identity/appbundle-manifest.txt`). So the pck inspected below is the one the run served (`godot.pck` `5e4365cb…`).
- I re-ran no suite (the Full-run rule).

## Player-visible regression check

- **Palette:** titles, order, colours and icons are equal to the base (above).
- **Placement and behaviour of every element:** her Full run covers it: `arch-9a-catalogue` (each of the 22 rows placed; one bench run, reset and save/load), palette-placement 25/25, and every element suite.
- **Start-up in the production build:**
  - The Full run publishes with `dotnet publish CuriousContraptions.web` and loads the bundle from the pck in Chrome.
  - The pck in her AppBundle (407,796 bytes; part of the reproduced bundle identity) holds `elements/elements.bundle.json`, the 22 package scenes (`.remap`) and the icons, plus the UI icons.
  - It holds no `_bmad-output`, docs or tools content (`strings` count 0; those directories carry `.gdignore`).
  - The export filter change to `all_resources` does not bloat the download.
- **Save/Load of existing machines:**
  - Saves name parts by string `part_id`.
  - Every delivered id still decodes; her bench save/load and every save suite pass.
  - A save holding Tennis or Balloon was never constructible (base `CaptureInstance` threw), and it is refused structurally with a message (P3).
- **Clock interval:** the deleted `clock.tres` `interval_seconds: 1.0` path is replaced by `WorkshopClock.PlacedIntervalSeconds = 1.0f`. `ClockTests` asserts 1.0.
- **Switch button colour:** it is now the bundle colour, which equals `switch.tres`'s colour (oracle).
- **DESIGN.md:** wording only; the palette table is unchanged.

## Tier 1, on the clean copy at `de4d1ea5`

| Check | Result | Raw |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx` (Debug, then Release) | 0 warnings, 0 errors, both | `tier1/build.log`, `tier1/release-build.log` |
| `dotnet test CuriousContraptions.slnx` | 2259/2259 (1907 + 164 + 94 + 77 + 17), equal to her count | `tier1/test.log` |
| C# static gate (`CSHARP_GATE_STAGED_HOOK=1 sh .githooks/pre-commit`) | exit 0; three builds with 0 warnings; 123/123 | `tier1/pre-commit-hook.log` |
| `node --experimental-vm-modules --test tools/*.test.mjs` | 88/88 | `tier1/node.log` |
| LifecycleContract (Release) | 510, 0 failures | `tier1/lifecycle.log` |
| WireContract (Release) | 108, 0 failures | `tier1/wire.log` |
| Ownership `--oracles .` (after the web restore) | exit 0, Positive, 22/22 attacks rejected; line 1 byte-identical to hers (SHA-256 `b4c56fac…`) | `tier1/ownership-oracles.log` |

| `anvil check --since 679c36cd` | 2 changed non-C# files checked, no warnings, exit 0 | `tier1/anvil-check.log` |

Timestamps and status counts: `tier1/identity.txt`, 04:57:31Z to 05:00:50Z. The `anvil check` ran afterwards, and the status stayed at 0 lines.

## Findings

**Blocking**
- **B1.** A null `Elements` item gives an unnamed start-up failure. It is the only list in the schema that admits a null (P2b). Owner: Amelia-20, round 2; the scope is above.

**Filed to Story F.16, "Catalogue Follow-ups" (none blocking)**
- **F16-1. AC wording conflict.** 9A.14's palette AC says "grouped by declared category" and also "same order as before". The pre-cutover palette was flat and not in category order, so the two cannot both hold. I accept CL1 (order and parity win; each definition carries its category). The owner should amend the AC text.
- **F16-2. Three deletion re-adds survive `dotnet test`.**
  - D6: an undelivered pictogram arm such as `"motor"`. `WorkshopIconsKeepsNoElementArm` iterates only bundle and undelivered ids, not the 9A.18 Deletes list.
  - D8: a re-added `PartId.Tennis` constant.
  - D10: a re-added and compiled `BallMaterialResource.cs`.
  - The AC `rg` checks hold today. A guard over the full deleted-symbol list (or Story 9A.45a's ratchet) would catch a re-add.
- **F16-3. Package presentation values are not pinned natively after the oracle was deleted.**
  - Description (V3) and switch colour (V4) survive every test. The switch colour is player-visible on the switch button, and neither the native tests nor the Chrome palette screenshot cover it.
  - Title (V2) and icon bytes (V5) are caught only by the Chrome screenshot, or incidentally by a test anchor.
  - Suggest a small checked-in golden of title, colour and icon digest per id.
- **F16-4. A refused save does not name the part.** A save holding Tennis or Balloon is refused with "Unsupported instance kind or parameters.", unchanged from base. Naming the unknown id is Story 9A.15's AC ("an unknown id is rejected atomically with the id named in the error"). Owner: 9A.15.
- **F16-5. The runtime cannot check package ownership of assets.** A bundle entry may point its scene or icon at another package's file and still load, because only the generator's BUNDLE009 checks ownership. The bundle is a build output, so this is informational.
- **F16-6. The start-up refusal has no Workshop-level behavioural test.** V8 is killed only by the structural catch gates. This re-observes her own filed residual; it becomes relevant to B1's fix, whose test should sit at the registry boundary.
- **F16-7. Cosmetic.** `engine/gpu/WorkshopInventory.cs:50` reads `{    /// <summary>…` (brace and doc comment on one line).

Her own residuals (baselines pinned to Chrome 155.0.8059.86; F15-11 fields with no runtime reader; `Description` has no UI reader) are re-observed and stay as she filed them.

## Process

- I edited no deliverable. Every mutant and probe ran in the disposable copy `murdoch21-mut` and was restored, with a final status of 0 lines.
- The first mutant batch was discarded and rerun. Its restore step (`git checkout -- .`) left D1's staged `.tres` in the index, which contaminated D2. The rerun restores with `git reset --hard HEAD`, and only the rerun's results are recorded.
- The probe source is kept as `probes/Murdoch21Probes.cs.txt`.
- `anvil_validate_write` ran on this record before its first write (allow). The later corrections were made with Edit and were not re-validated. It was not run before the evidence scripts (`identity/status-brute.sh.txt`, `probes/oracle-compare.py.txt`, `mutants/run-mutants.sh.txt`) or the probe file in the disposable copy were written. Those are review-only evidence, not deliverables.
- The closure table assigns 9A.14 no addendum beyond the isolation authority's A13, A15 and A3 (icons) rows, which she updated (CL7). Those rows close at 9A.14's terminal Pass, not this round.
