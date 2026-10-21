# Review: Story 9A.17, Bundle Ports and Footprints

**Reviewer:** Murdoch-24, independent. I edited no deliverable.
**Implementer:** Amelia-23.
**Round:** 1 of at most 2.

**Verdict: Pass** (scoped) on tree `46e734289726c169d2d4a395a54db29ebb57d0f7`, committed as `d57fbe22`. The final head is `24179187`
(`d57fbe22` plus a docs fix, on main `379cc24a`). I reviewed that delta, and the Pass holds.
- Every AC is met.
- I found no player-visible regression in wiring, the connection drawer, wire endpoints, part footprints or Save/Load.
- No finding blocks. F19-1 to F19-9 below are filed.
- This is not a global or runtime qualification.

## Identity

| Item | Value |
| --- | --- |
| Reviewed candidate | `2bdd7e27a4415d48ecdd0a8b7dc8a6ccf5867064`: WIP `882d6bd3` merged with main `379cc24a` |
| Reviewed tree | `46e734289726c169d2d4a395a54db29ebb57d0f7` |
| Final commit | `d57fbe22546c4bc5f1b720600fd5b980e950c7e9`: one commit, parent `379cc24a`, tree `795c5d2e`. `git diff --name-only 46e73428 d57fbe22^{tree}` lists only `_bmad-output/` paths (her Full-run copy, evidence and spec). **No source, scene, test or tool file differs from the reviewed tree** |
| Final head | `24179187`, parent `d57fbe22`, tree `0b9a8182`. Against `46e73428`, the only path outside `_bmad-output/` that differs is `docs/architecture/element-package-isolation.md`. I reviewed the whole delta (`git diff d57fbe22 24179187`). It splits the A3 row into "A3 (ports)", which closes at 9A.17, and "A3 (footprints)", owned by 9A.16 and then each family conversion. It also adds two spec lines (D5 and D6), and the test they cite, `TubeMouthSnapTests.EveryTubePartDeclaresAStartAndEndMouthWithABoreFacingOutward`, exists. This is documentation only and agrees with F19-7 and the coordinator's ruling. The rerun evidence (16 files) is now committed. **No source, scene, test or tool file differs from the reviewed tree** |
| Reused-run identity | Both of her runs record `sourceHead 2bdd7e27`, with non-empty `sourceStatusDigest`s: `e9207d0e…` for the Full run, `449a3ced…` for the rerun. At the start of my review, `git status` in her worktree listed only `_bmad-output/` paths: the spec and `evidence-9a-17/`. The tested binaries were therefore built from tree `46e73428`, plus evidence-only edits. The runner and suite digests agree between the two runs (`d6a22216…`, `457ef02b…`) |
| Clean Tier 1 copy | A scratch `git worktree` detached at `2bdd7e27`, tree `46e73428`. Status was 0 lines before each batch and after each mutant revert |
| Evidence | `evidence-9a-17-review/` beside this file, about 280 KB; no file over 5 MB |

## Acceptance criteria

| AC | Evidence | Verdict |
| --- | --- | --- |
| AC1. Sockets, directions, domains and anchors come only from the bundle `PortDescriptor` list; the bench footprint only from the declaration | **Ports.** `PortDescriptor.PortId` (a string) became the typed `PortSocket`. `ElementPortCatalogue.FromBundle` is the only port source. **Main thread:** `PartRegistry.Load` builds it atomically from the bundle, and `Workshop` installs it. **Simulation worker:** `Bootstrap` reads the embedded generated bundle with the same `BundleSchema` and `BundleValidator`, before `PrepareGpu` or the warm-up can construct anything. **Readers:** connection validation, `WorkshopActivationCompiler` (lamp level), the drawer (`Ports.Of`, declaration order) and the wire endpoints (`Ports.LocalPosition`) read only the catalogue. `rg` finds no other per-element port source in `engine`, `ui` or the workers. The remaining `WorkshopSocket.*` uses are socket semantics, such as latch Set/Reset and PowerIn routing, not port declarations. **Footprint.** The deleted `WorkbenchFootprint.Of` (at `007a65ed^`) counted table rows for the capacity limits that Story 9R.5b removed. No engine or UI code restates a footprint now, and the bench has no overlap refusal (`WorkshopEditRejection` has no such reason). The compiled rows still come from per-element compile arms, which Story 9A.16 and the family conversions own; `PackageRuntimeParityTests` checks them against the declaration (F19-7) | Pass |
| AC2. A golden test proves every element's ports and footprint unchanged | `BundlePortsTests.EveryElementsBundlePortsAndFootprintEqualTheGoldenTable` covers all 22 elements. **Ports:** I compared the golden table by hand with the deleted `WorkshopPorts.For` and `LocalPosition` arms in the diff: equal in socket, order, domain, direction, signal kind and anchor, bit for bit. **Footprints:** the declarations' `Footprint` lines are not in the diff, so they are unchanged. Mutants R2 (declaration and locator moved together) and R3 (counter drawer order) are each killed by this test | Pass |
| AC3. The literal `rg` finds 0 hits, and `TryGetPortTransform` has no alias chain | `rg "WorkshopPorts\.For\|WorkbenchFootprint\.Of"` outside `_bmad-output` and `docs/planning`: 0 hits (her `deletion-rg-proof.txt`, which my rg confirms). The remaining hits are dated history and the story text. `TryGetPortTransform`, `GetPortTransform` and `GetRelativeTransform` are deleted with the alias chain and the `FindChild("Loc_Port_…")` fallback; they had no production caller. The extractor now matches ordinally. Her M1, M2 and M7 kill the guards. The guard matches names only (R6, F19-1) | Pass |
| AC4. A wrong-domain or output-to-output link is still refused (negative control) | `AWrongDomainOrOutputToOutputLinkIsStillRefused` covers wrong domain both ways, output to output for both domains, an undeclared socket, a part with no ports and the level rule, with positive controls. Her M4b is killed. My R5 (`Has` ignores the domain) is an **equivalent mutant**: `Admit` already ties each socket to its own domain and direction, and `Validate`'s per-domain socket checks refuse the link with the same `InvalidConnection` | Pass |
| AC5. Every wiring suite passes | Her Full run `20261021T092247Z-74820` on `2bdd7e27`: cat-005 7/7, cat-017 5/5, cat-020 5/5, cat-037 6/6, cat-051 5/5, cat-052 7/7, anim-1b 3/3 and anim-1c 4/4. Also passing: cat-051-own-supply, cat-052-two-plates and cat-052-lamp. cat-017 is Clean in the rerun (below) | Pass |
| AC6. The declaration-to-locator disagreements are resolved with one source; start mouths face outward; a test fails on disagreement | Every declared anchor already equalled the old runtime table at base (for example, the detector at (0, 1.12, 0)), and Story 9A.14 had moved the scene locators there. This story renames all 22 socket locators to the exact `PortSocket` name. **Build check:** `PackageValidator.Locators` fails the bundle build on an undeclared or duplicate locator (BUNDLE011), a missing one (BUNDLE012), or one that is not a root child, has no readable transform, or is more than 1e-5 m from its anchor (BUNDLE013); the red build before the renames is in `red-locators-before-rename.txt`. **Native check:** `BundlePortLocatorTests` compares all 22 ports with their instantiated-scene locators. My R1 (a declaration-only move) fails the build with BUNDLE013. **Start mouths:** pipe, both bends, funnel, gate and detector all have basis −X (`-1,0,0,0,1,0,0,0,-1`), outward. **Not covered:** the declared port normal against the locator basis (D5; F19-2) | Pass |
| AC7. Build 0 warnings, `dotnet test` 100%, every e2e suite 100% | **My clean copy:** Debug and Release builds, 0 warnings. Tests 2300/2300 (1930 + 166 + 110 + 77 + 17), equal to her count. Node 88/88. **Her Full run:** 48 files, 284/284 tests, 0 not-ok, verdict `Incomplete(Contended)`, because cat-017 and pkg-3 were Contended in both their records. **Her rerun** `20261021T101828Z-84313` on the same `sourceHead`, same runner and suite digests: verdict Pass, all Clean (cat-017 5/5, pkg-3 3/3, engine-core-2a1 5/5, engine-core-2b1 3/3). Its bundle identity differs (`fdf5b9e3…` against `570e3d4c…`): two publishes of one commit are not byte-identical, the known F17-11. **Serial:** the runner ran SerialAlone suites alone and the rest on 2 parallel workers, under the owner's 19 Oct parallel-Chrome decision | Pass |
| Deletes | `WorkshopPorts` (`For`, `LocalPosition`, `Has`, `Signal`), the `MachinePart` scene lookup and alias chain, `PartDefinition.Ports`, case-insensitive locator matching and the obsolete parity test are all deleted. `WorkbenchFootprint.Of` was already gone (9R.5b). No shim, version or fixed capacity was added | Pass |
| Owner rule: an invalid declaration is refused with a typed reason | `ElementPortRejection` covers 10 reasons, each tested by name and port (`AnInvalidPortDeclarationIsRefusedWithItsTypedReason`, `ADuplicateSocketOrElementIsRefused`, `AnUnknownElementOrUndeclaredSocket…`). The bundle read refuses an unknown socket name or a number (`ExactEnumConverter<PortSocket>`), and the bundle validator adds BUNDLE014 and BUNDLE015. My R4 (the socket-to-domain/direction check disabled) is killed by 2 cases. A refused catalogue at load becomes `InvalidDataException`, which `Workshop` reports as a start-up fault | Pass |

## Player-visible regression check

- **Wiring and the drawer.** The catalogue's port order equals the deleted table, so the drawer rows are unchanged. The connection rules are unchanged apart from where they read ports. cat-005, cat-020, cat-037, cat-051 and cat-052 pass in Chrome.
- **Wire endpoints.** The anchors are bit-identical to the deleted `LocalPosition` arms, so every wire is drawn where it was.
- **"Port hit-targets."** Linking picks the target part, then its declared inputs. There are no per-socket screen targets to regress.
- **Footprints and overlap.** No footprint consumer existed or was added, and the bench has no overlap refusal, so nothing can regress. palette-placement passes 25/25.
- **Save/Load.** The socket wire and save values are unchanged (`PortSocket` and `WorkshopSocket` are pinned to the same names and numbers). Save pre-validation reads the installed catalogue. engine-core-2b1 and engine-core-2a3 pass.
- **Animation worker.** It compiles `WorkshopConnections.cs` but installs no ports. It decodes only clock peers and `WorkshopResponse`, which carries no construction, so it never reaches `DeclaredPorts.Current`. anim-1a, anim-1b and anim-1c pass.

## Tier 1 (clean copy at `2bdd7e27`, tree `46e73428`)

| Check | Result | Raw log |
| --- | --- | --- |
| `dotnet build CuriousContraptions.slnx` | 0 warnings, 0 errors | `tier1-build-debug.txt` |
| `dotnet build CuriousContraptions.slnx -c Release` | 0 warnings, 0 errors | `tier1-build-release.txt` |
| `dotnet test CuriousContraptions.slnx` | 2300/2300 | `tier1-test.txt` |
| `node --experimental-vm-modules --test tools/*.test.mjs`, file by file | 88/88 over 10 files | `tier1-node.txt` |

I reused, without rerunning, her LifecycleContract (510/0), WireContract (108/0), Ownership `--oracles` (Positive, 22/22) and `anvil check`, taken on `2bdd7e27` per her spec. This story changes no wire schema, lifecycle or ownership-policy member.

**Contention note.** My Tier 1 and mutant builds (about 09:20 to 09:40Z) overlapped her first cat-017 records, and probably contributed to their Contended observation. The Clean rerun supersedes those records.

## Mutants (mine; each reverted to 0 status lines)

| Id | Mutation | Result | Log |
| --- | --- | --- | --- |
| R1 | Battery Supply anchor .62 → .63 in the declaration only | Killed by the bundle build: BUNDLE013 "declared at (0, 0.63, 0), Loc_Port_Supply is at (0, 0.62, 0)" | `R1.txt` |
| R2 | The same move in the declaration and the scene together | Killed by `EveryElementsBundlePortsAndFootprintEqualTheGoldenTable` | `R2.txt` |
| R3 | Counter PowerIn and Supply swapped (drawer order) | Killed by the golden test | `R3.txt` |
| R4 | The `Own(socket)` domain/direction check disabled | Killed by 2 `SocketDisagrees` cases | `R4.txt` |
| R5 | `ElementPortCatalogue.Has` ignores the domain | Survives: an **equivalent** mutant (reason in AC4). The first attempt failed to compile on IDE0060 and was rerun | `R5.txt` |
| R6 | A per-(element, socket) anchor switch re-added as `SocketAnchors.Of` | Survives the port-guard and static-gate tests (a filtered run of 114 tests: `BundlePort`, `ElementScan`, `StaticGate`). The anti-re-add guard matches names only (F19-1). The candidate has no such table | `R6.txt` |

I also checked her M1 to M10 (`evidence-9a-17/mutants.txt`): each is killed, or reasoned equivalent (M4). They cover the alias, table, order, typed-boundary, installation and tolerance guards.

## Coordinator questions (21 Oct)

- **Footprints.** The coordinator ruled that A3 is split: ports close here, and the runtime read of declared footprints moves to Story 9A.16 and then the family conversions. Read literally, AC1 says "its bench footprint only from its declaration". That is met in the sense that no engine or UI source restates a footprint. It does not demand a runtime reader, since the bench consumer (`WorkbenchFootprint.Of`, for capacities) was removed by 9R.5b. So this is not a Fail. If the coordinator reads AC1 as requiring the runtime to read footprints, that is an AC-scope question, and the split row and F19-7 already record it.
- **The C# static gate's `/resource` allowance: not a widening, so it does not block.**
  - `resource` is admitted as a switch name, and every `/resource:` value outside the pinned list is reported as "(unpinned value)" (`CSharpStaticGateTests.cs:665-667`).
  - The list holds exactly `../elements/elements.bundle.json,elements.bundle.json`.
  - `/res`, `/linkresource` and `/linkres` remain unpinned switch names, so they still fail.
  - The one residue is that the pinned value is a project-relative string, so it would also admit a project one directory deep under `tools/` embedding a file at `tools/elements/elements.bundle.json`. No such file or project exists (F19-8).

## Filed items (F19-n, for the coordinator; none blocks)

- **F19-1 (Low, test gap).** `NoHandWrittenPortTableFootprintTableOrLocatorAliasIsReAdded` matches the deleted names only, so a renamed per-element anchor or port switch passes it (R6). Make the scan structural: for example, refuse any switch in `engine` or `ui` keyed by an element-id string that yields a `Vector3`, `WorkshopPort` or port list.
- **F19-2 (Low, AC6 depth).** A declared port normal is never compared with its locator's basis (D5). Every socket locator has an identity basis while the declared normals vary (for example, the lamp's −Y). No runtime reader uses a port normal today. Either compare them at bundle build, or define the locator basis convention and drop the redundant one.
- **F19-3 (Low, content).** `content/puzzles.json` still names 40 connection endpoints in the old snake_case form (`from_port`/`to_port`: `supply`/`power_in` ×20, `activation_out`/`activation_in` ×44, plus `drive`/`drive_in` and `tie`). No code reads those fields, so nothing regresses. They are still authored content outside the typed `PortSocket` set. Rewrite or remove them when the campaign connections gain a reader.
- **F19-4 (Low, tooling).** `E2eRunner select --diff 379cc24a` refuses with `NoPrimarySuite`, because no suite declares `delay` as its Primary (already reported by the implementer).
- **F19-5 (Low, design).**
  - `DeclaredPorts` is a process-wide static singleton. UI code reads `World.Registry.Ports`, while validation and the activation compiler read `DeclaredPorts.Current`: two routes to one catalogue.
  - The worker reads its own embedded copy of the bundle, and nothing checks at runtime that it equals the bundle the main thread loaded. Both come from one build today.
  - Fold the catalogue into the construction or session identity (Story 9A.16 or the wire design), so a mismatch is a typed refusal.
- **F19-6 (Low, typing).** `PortSocket` (Abstractions) and `WorkshopSocket` (engine and wire) are one closed set kept as two enums, bridged by `(WorkshopSocket)(uint)` and pinned by a test (D1). Collapse them to one enum once the engine's wire enum can live in the Abstractions.
- **F19-7 (Note, owner Story 9A.16 and the family conversions).** The declared footprint has no runtime consumer. The compiled body and prismatic rows still come from per-element compile arms, checked against the declaration by `PackageRuntimeParityTests`. The requirement is closed when those arms leave.
- **F19-8 (Low, gate hardening).** Pin the `/resource` allowance to the resolved absolute path and to the Simulation project, not to a relative string.
- **F19-9 (Process).** The implementer did not run the mandatory Anvil pre-write validation before each write (she disclosed this through the coordinator). For transparency: I also validated this review record only after writing it (`anvil_validate_write`, decision `allow`, preview with SHA-256, enforcement mode `warn`). Neither case blocks.

## Owed by the coordinator

The spec's "Closing addenda owed at terminal Pass" asks for the dated 6.5c (locators), 6.5e and 6.5f (switch tables) addenda in `epics.md`. I did not edit `epics.md`, since that is outside the reviewer role. The coordinator should append them, and record this Pass and F19-1 to F19-9.
