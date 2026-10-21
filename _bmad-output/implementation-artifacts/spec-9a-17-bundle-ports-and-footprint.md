# Story 9A.17: Bundle Ports and Footprints — implementation record

Implementer: Amelia-23, 21 Oct 2026. Base: main `0a45e3b7` alone. The Story 9A.15 merge (`d3f17d27`) conflicted in
`sprint-status.yaml` and `epics.md`, so, as instructed, it was not kept. 9A.17 depends only on 9A.14.

## What changed

- **One port source.** `PortDescriptor.PortId` (a string) is now `PortDescriptor.Socket`, a typed `PortSocket` enum in
  `CuriousContraptions.Abstractions`. It has the same names and wire values as `WorkshopSocket`, and a test pins that. The bundle writes
  the socket by its exact name and refuses any other name or a number (`ExactEnumConverter<PortSocket>`).
- **`ElementPortCatalogue`** (`engine/gpu/ElementPortCatalogue.cs`) admits every delivered element's `PortDescriptor` list from the
  bundle entries, keeping the declaration order (the drawer order). It refuses each of these structurally, with a typed
  `ElementPortRejection`:
  - an undefined socket;
  - a domain other than Signal or Electrical;
  - a Bidirectional or undefined direction;
  - a socket declared with another socket's domain or direction;
  - a misplaced or undefined signal kind;
  - a non-finite anchor;
  - a duplicate socket or element.

  A question about an unknown element is `UnknownElement`, and one about an undeclared socket is `UndeclaredSocket`.
- **Who reads it:**
  - **Main thread.** `PartRegistry.Load` builds the catalogue atomically with the definitions; a refusal is an `InvalidDataException`
    with the typed cause. The Workshop installs it (`DeclaredPorts.Install`).
  - **Simulation worker.** It embeds the generated `elements/elements.bundle.json`, built through a `BundleElements` project
    reference. At `Bootstrap` it reads the bundle with the same `BundleSchema` and `BundleValidator` and installs the same catalogue.
  - **Users.** Connection validation, the lamp's level compile (`ActivationNetwork`), the connection drawer and the wire artwork read
    only that catalogue.
  - **Installation rule.** A second installation must be an equal catalogue; a different one is refused and the installed one is kept.
  - **Empty constructions.** A construction without connections needs no installed ports.
- **Scene locators.**
  - The 22 socket locators in 12 scenes are renamed to their typed socket exactly (`Loc_Port_power_in` → `Loc_Port_PowerIn`,
    `Loc_Port_out` → `Loc_Port_Supply` / `Loc_Port_ActivationOut`, and so on).
  - The extractor matches `Loc_Port_` names ordinally.
  - The bundle build (`PackageValidator.Locators`) fails in each of these cases:
    - a locator names no declared socket and no tube mouth (BUNDLE011, also for a duplicate);
    - a declared port has no locator (BUNDLE012);
    - a locator is not a child of the scene root, has no readable transform, or is more than 1e-5 m from its declared anchor (BUNDLE013).
  - The bundle validator also refuses a duplicate socket (BUNDLE014) and a misplaced signal kind (BUNDLE015).
  - The native test `BundlePortLocatorTests` compares every declared port with its extracted locator.

## Deletes

- `WorkshopPorts` (`For`, `LocalPosition`, `Has`, `Signal`; 12 switch arms and 23 position arms) in `engine/gpu/WorkshopConnections.cs`.
- `MachinePart.TryGetPortTransform`, `GetPortTransform` and `GetRelativeTransform`, with the string alias chain (`"PowerIn"`→`"power_in"`,
  `…In`→`"in"`, `…Out`→`"out"`, `"Supply"`→`"out"`) and the `FindChild("Loc_Port_…")` scene fallback. None of them had a production caller.
- `PartDefinition.Ports => PortLocators` (an alias).
- The extractor's case-insensitive locator dictionary.
- `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimePortsInOrder`: there is no runtime copy left to compare.
  `BundlePortsTests.EveryElementsBundlePortsAndFootprintEqualTheGoldenTable` replaces it.
- `WorkbenchFootprint.Of` had already been deleted by Story 9R.5b, and no other footprint table exists in the engine or UI
  (`deletion-rg-proof.txt`).

## Decisions (small gaps, decided under the owner's delegation)

1. **D1, typed socket in the Abstractions.** Closed sets are enums end to end, so the declaration names a `PortSocket`, not a string.
   `WorkshopSocket` stays the engine and wire enum (its numbers are stored in saves and on the wire). A test pins the two to the same
   names and values, and the catalogue converts at the bundle boundary.
2. **D2, an installed catalogue, not a parameter.** Construction validation runs deep inside `WorkshopConstruction.Validate()`
   (wire decode, both compilers, the trace, the lessons, save pre-validation).
   - Threading a catalogue through about 120 constructions, or carrying ports on the wire, was rejected. Ports on the wire would also
     make every save restate the declarations, and would pre-empt 9A.16's wire design.
   - Instead, each process installs the bundle's catalogue once, and a different catalogue cannot replace it.
   - The test assembly installs the catalogue from the discovered packages, written and read back through the bundle schema
     (`DeclaredPortsFixture`, an xUnit assembly fixture).
3. **D3, the worker embeds the bundle.** The Simulation worker must refuse an undeclared port and compile the lamp's level input. The
   embedded resource is the same generated bundle the game loads, built in the same build, and the worker validates it as the game does.
   The Animation worker references `CuriousContraptions.Bundle` only because the shared engine sources name `BundleEntry`; it loads no
   bundle.
4. **D4, tube mouths are not sockets.** `Loc_Port_mouth_start` and `Loc_Port_mouth_end` are the tube mouths of Story 9R.6. The bundle
   build admits them by exact name, and admits no other `mouth_*` name or any other undeclared name.
5. **D5, normals.**
   - What is checked: a locator's position against its declared anchor.
   - What is not checked: its rotation against the declared normal. Every socket locator has an identity basis, and no runtime reader
     uses a port normal (the bundle validator holds it to unit length).
   - The start-mouth locators already face outward, as the funnel's does (basis −X, from Story 9R.6). That disagreement was already
     resolved, and it is pinned by
     `TubeMouthSnapTests.EveryTubePartDeclaresAStartAndEndMouthWithABoreFacingOutward`: each mouth's local +X points away from the
     other mouth.
6. **D6, footprint.** "Its bench footprint only from its declaration": no engine or UI table restates a footprint. The compiler still
   emits each instance's rows through its record arm, which leaves with that record's family conversion (finding A5). Until then,
   `PackageRuntimeParityTests` checks the declared footprint against every compiled instance. No new footprint consumer was built,
   because colliders and bodies from the bundle are Story 9A.16.
   - The isolation doc therefore splits the A3 row. Ports close at 9A.17. "The runtime reads the declared footprint" stays open, owned
     by 9A.16 and then each family conversion.
   - **The coordinator decides** whether to accept this or reassign it.
7. **D7, anchor tolerance.** The tolerance is 1e-5 m: the scenes' decimal text and the f32 declarations agree to f32 rounding. Every
   current locator agrees exactly.
8. **D8, the AC3 literal `rg`.** It now has 0 hits in code and in the current documents. The remaining hits are dated history that is
   never edited:
   - planning element baselines at `a6c914e`;
   - earlier reviews, specs and evidence;
   - the story's own text in `epics.md`.

## Closing addenda owed at terminal Pass

6.5c (locators), 6.5e and 6.5f (switch tables): the reviewer appends the dated addenda (epics delivery-order rule).

## Evidence

Tier 1 was run on the merged tree (`2bdd7e27`, main `379cc24a` merged in):
- three builds (default, `PlaytestDiagnostics`, Release), each with 0 warnings;
- dotnet tests 17 + 77 + 110 + 166 + 1930, all passing;
- node 88/88;
- LifecycleContract 510/0;
- WireContract 108/0;
- Ownership `--oracles` Positive, with 22/22 attacks rejected;
- `anvil check` on the 69 changed source and doc files: no warnings (`tier1-anvil.txt`).

`E2eRunner select --diff 379cc24a` refuses with `NoPrimarySuite` (no suite declares `delay` as its Primary; reported to the coordinator to file). The
change touches the Simulation worker (it embeds the bundle and installs ports at bootstrap) and the shared engine sources that both
compilers read, so the one Full run is mandatory and was run.

**Full run** (`e2e-full-2bdd7e27/summary.txt`, source `2bdd7e27`, bundle `570e3d4c…`):
- Every suite passed: 48/48 files and 284/284 tests, with no failure and no not-ok.
- That includes every AC-named wiring suite: cat-005, cat-017, cat-020, cat-037, cat-051, cat-052, anim-1b and anim-1c.
- The verdict was `Incomplete(Contended)`. Other sessions were loading the machine, so cat-017 and pkg-3 passed only under contention,
  twice each.

**Contended suites re-run** (`e2e-contended-rerun-2bdd7e27/summary.txt`, same source):
- Run with `E2eRunner run --changed` on those two suites, with the smoke pair added by the runner, once no other runner or Chrome was
  active.
- Verdict Pass, with clean observations: cat-017 5/5, pkg-3 3/3, engine-core-2a1 5/5 and engine-core-2b1 3/3.
- Its bundle identity is `fdf5b9e3…`. Two publishes of one commit produce different identities, which is the known F17-11 and not
  new here.

`evidence-9a-17/` holds:
- the red bundle build before the scene renames (`red-locators-before-rename.txt`);
- the Tier 1 logs;
- the mutants (`mutants.txt`);
- the rg proof (`rg-proof.sh`, `deletion-rg-proof.txt`);
- the E2E runner output.
