---
title: 'ENGINE-ART-SOLIDITY: each package declares which of its art is solid'
type: 'feature'
created: '2026-11-21'
status: 'blocked'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The owner's rule is that what a player sees as solid is solid. Which meshes of a package's art are solid, hollow, an arc chord or decorative is written only in a hand table inside one test, so a package can ship art that nobody classified, and the catalogue conformance suite has no declared data to read for its art row.

**Approach:** Each package declares the solidity of every mesh in its scene, as typed build-time data beside its scene bodies. The bundle generator refuses a scene in which a mesh is classified by no entry or by more than one. The existing visible-solid test reads the declarations for every delivered package, and the hand table is deleted.

## Boundaries & Constraints

**Always:** The classification is a closed enum declared by the package; no engine, tool or test names a package to classify its art. The generator's rule is the same for every package and reads only the scene text and the declaration. A classification entry names a scene node by its path; an entry on a mesh classifies that mesh, and an entry on any other node classifies every art mesh beneath it. The rule applies to every discovered package: a declaration that does not implement `ISceneGeometryElement` has no entries, so any art mesh in its scene is unclassified. A proxy is a `CollisionShape3D`, or a node whose name starts with `col_` or contains `-col_`, ignoring case. A proxy mesh is hidden when its own node states `visible = false`; an ancestor's visibility is not consulted. Every other `MeshInstance3D`, hidden or not, is art and is classified like any other mesh. Every decorative entry states its reason from a closed set, and the tether reason also cites the owner decision that allows it. The visible-solid test keeps its matching rules, tolerances and parameter sweep exactly; only its source of classification changes. The baked bundle, every scene and every collider are unchanged. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A classification read from a node name, a prefix, a wildcard string, scene metadata or a tag. A default class for an unclassified mesh, or a rule that applies only to packages that opted in. A new field in the bundle schema, the wire, the worker or TypeScript. A collider, scene or art change: a visible solid without a collider is repaired by the prerequisite, not here. A per-package art classification left in `VisibleSolidColliderTests`. Hashes, mutants, browser runs, skipped tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| M1 complete classification | A package whose every art mesh is classified by exactly one entry | The bundle builds; no entry gains, loses or changes a field | No error expected |
| M2 unclassified mesh | A scene with an art mesh that no entry classifies | The bundle is refused | `ArtUnclassified`, naming the package and the node path |
| M3 classified twice | A mesh named by its own entry and by an entry on an ancestor, or beneath two classified ancestors | The bundle is refused | `ArtClassifiedTwice`, naming the package, the node path and both entries |
| M4 entry that classifies nothing | An entry whose path is not a scene node, names a collider proxy, or names a node with no art mesh beneath it | The bundle is refused | `ArtEntryUnresolved`, naming the package and the entry's path |
| M5 invalid value | An undefined solidity or reason; a decorative entry with no reason, or a reason on an entry that is not decorative; the reason "moved by a binding" on a node that is not, itself or through an ancestor, the node of a presentation binding that moves, turns, scales or hides it, or the span node of a span binding (not its moving node); the tether reason with an owner-decision citation that is null, empty or white space, or a citation on any other entry; a fit that is non-finite, negative, above 0.005 m, or on a decorative entry | The bundle is refused | Undefined solidity or reason: `UndefinedEnum`. No reason, a misplaced reason, an unsupported binding reason, a missing or misplaced citation: `ArtReasonUnsupported`. A non-finite fit: `NonFinite`. A fit below 0, above 0.005 m or on a decorative entry: `OutOfRange`. Each names the package and the entry |
| M6 collider proxies | `CollisionShape3D` nodes and hidden proxy meshes | Need no entry and are never art; a visible proxy mesh needs an entry | Naming a hidden proxy in an entry is M4 |
| M7 visible solid, from declarations | Each delivered package at its default parameters and at each collider-bound parameter's minimum and maximum | Every mesh declared solid, hollow or an arc chord has one matching collider on the body that owns it, and no collider is left without art; moving bodies are those the package's scene bodies name | A mismatch fails naming the package and the node or collider |
| M8 order and grouping | The same classification written in another order, or with a subtree entry replaced by one entry per mesh | The same result in M1 and M7 | No error expected |

</intent-contract>

## Code Map

Delivery shape: single engine slice, the prerequisite of the catalogue conformance suite (`spec-s4-catalogue-conformance-suite.md`). Review tier: **engine** (abstraction, generator and shared test code). Line numbers were measured on `8d1d68aa`; `baseline_revision` is captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work, captured at replanning before first implementation; scope guards compare against it. Drafted on `25df3cd5` (45 packages, the Baseball included), the drafting revision only, not a baseline.

Planning verdict: Scoped Pass of the `8d1d68aa` draft (planning auditor `a87f2727545777a22`, second pass, 21 November 2026); re-audited at replanning.

State: **blocked on `ELEMENTS-VISIBLE-SOLID-REMAINDER` parts A, B, B2 and C and the Baseball repair, and on the engine programme, including `ENGINE-CAPABILITY-INSTANCES`** (engine stories index order 37 depends on order 8, its visible-proxy refusal; it is not a repair of owner decision 22, so it waits for the programme). Owner: coordinator. Retry condition: parts A–C, B2 and the Baseball repair published and `ENGINE-SOA-SIMD` (the programme's last story) published. Part A is `spec-elements-visible-solid-remainder.md`; the Baseball repair is `spec-repair-baseball-el-239.md`, which brings the Baseball under the visible-solid test. Twenty-two meshes in nine of the eleven packages that the hand table does not cover are visible solids with no collider (table below); the owner decided their repairs on 21 November 2026 and the prerequisite makes them, under the existing hand table, which then covers all 45 (the Baseball through its repair). This contract is replanned from the revision that publishes part C.

Why it must be split, and in this order. The generator's rule has to hold for all 45 packages from the commit that introduces it; a rule that applied only to packages that had declared would be a second path. All 45 must therefore be truthfully classified in this slice, and the visible-solid test then runs for all 45. That test fails for a mesh declared solid without a collider, so the repairs come first. Doing them inside this slice would add colliders or redraw art in nine packages alongside an abstraction change, which is several product behaviours in one slice. "Declaration shape first, classification later" is not available for the same reason: the shape is only valid with every mesh classified.

Owned scope:

- `CuriousContraptions.Abstractions/SceneGeometry.cs` -- 8–13: `SceneBodyGeometry` and `ISceneGeometryElement`.
- `CuriousContraptions.Abstractions/BundleServices.cs` -- 83–105: `BundleErrorCode` (15 codes).
- `tools/BundleElements/PackageValidator.cs` -- `Assets` 32, `Locators` 50; the new check sits beside them.
- `tools/BundleElements/SceneGeometryExtractor.cs` -- the scene reader (`Parse` 302, `ReadSceneNodes` 158) and the proxy rule (22–24), reused by the new check.
- `tools/BundleElements/BundleGenerator.cs` -- 14–16: where the pre-bake checks are gathered.
- `elements/<Name>/<Name>ElementDeclaration.cs` -- all 45 (at drafting); each already implements `ISceneGeometryElement`.
- `tools/BundleElements.Tests/Fixtures.cs` -- 112–122: `SceneBodyFixture`, the one test implementer of the interface. `tools/BundleElements.Tests/ArtSolidityTests.cs` -- new (the project compiles its sources by default).
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- `ArtSpec` 54–55, `Specs` 63–161 and its `Logic` helper, `Names` 167–170, `Listed` 172, the theory 184–238, `Art` 242–249.
- `docs/testing-guidance.md` (reviewer checklist) and `docs/engine-contracts.md` -- the rule for a new or changed decorative entry (task 7).
- `docs/engine-contracts.md` (the declared-anchors section at 78), `docs/hollow-geometry-design.md:111`, `docs/architecture/element-package-isolation.md`, `docs/delivery-workflow.md:62`, `docs/planning/invest/delivery-batches.md` section 5, `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md`, this contract and its run log.

Not consumers, checked: `CuriousContraptions.Bundle/BundleSchema.cs:18–28` builds the entry from named declaration members and does not copy scene geometry, so `elements/elements.bundle.json`, `PartRegistry`, the worker, `schemas/*.fbs`, the generated wire code and every TypeScript reader are untouched. `tools/Coverage` reads catalogue identities and resources, not art. The C# static gate gains no project. `tools/E2eRunner` treats an Abstractions change as full impact already. `ElementIsolation` scans shipping sources for package names; node paths in a package's own declaration are its own data. `CuriousContraptions.tests/FaceMarkingTests.cs:9–13, 25–30` is a second hand list (seven packages and their printed mesh paths) of meshes this slice declares `PrintedFlat`. It is left as it is: driving the flush-print check from every `PrintedFlat` entry is S4's scope and is named in its contract.

### Capability probe

Five present, one present after the prerequisite, one missing and owned here.

- **Present.** Build-time scene description per package: `ISceneGeometryElement.SceneBodies` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10–13`), implemented by all 45 declarations. Tests: `PortLocatorTests.AMovingBodyPortOnADeclarationWithoutSceneBodiesFailsTheBuild`, `BundlePortsTests.EveryDeclaredPortAgreesWithItsSceneLocator`.
- **Present.** Reading a package's text scene at build time: `SceneGeometryExtractor.Bake` (`tools/BundleElements/SceneGeometryExtractor.cs:14`). Tests: `SceneGeometryExtractorTests`.
- **Present.** The collider proxy rule (a `CollisionShape3D`, or a node named `col_…` or containing `-col_`): `SceneGeometryExtractor.cs:22–24`. Tests: `SceneGeometryExtractorTests`; the visible-solid test applies the prefix part of it at `VisibleSolidColliderTests.cs:249`; task 6 aligns it.
- **Present.** A scene check that refuses the whole bundle with a typed code naming package and node: `PackageValidator.Locators` (`tools/BundleElements/PackageValidator.cs:50`) through `BundleGenerator.Generate` (`BundleGenerator.cs:14–16`). Tests: `PortLocatorTests`, `BundleValidatorTests.EveryRealPackageShipsItsIconAndScene`.
- **Present.** Matching drawn primitives to colliders over a parameter sweep: `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (184–238), today for the 33 packages in `Specs`.
- **Present after the prerequisite.** The same match for the other eleven packages, including imported meshes (`cannon` jacket, collars and sleeve; `windmill` guard), which the matcher cannot read today (`VisibleSolidColliderTests.cs:241`: a mesh that is not a box, sphere or cylinder has no primitive shape). The prerequisite extends the hand table to 45 and owns that matching.
- **Missing, owned here.** A declared classification and a generator rule over it. This is the capability the slice delivers.

### Classification of the eleven packages outside the hand table

Read from each scene, declaration and baked entry on `8d1d68aa`; nothing was run. "Finding" means a visible solid with no collider that is not printed flat, enclosed by another solid or moved by a presentation binding. A visible `col_` mesh is its own collider and is not listed. The last column is what the prerequisite repairs or, for the Balloon string and the Fan's arrow, what the owner decided instead.

| Package | Solid or hollow, collider present | Decorative, with the existing reason | Finding (no collider) |
|---|---|---|---|
| `trampoline` | back plate, four sides, four posts, four feet | `Visual_Bed`: moved by a binding (the elastic surface is a declared surface, not a collider) | none |
| `impact_lever` | base, fulcrum, two stops; `Beam/Visual_Beam` on the hinge body | none | `Beam/Visual_Inset` and `Visual_Mark_0`–`3`: raised 8–10 mm above the beam face |
| `tennis` | `Visual/Felt` | none | `Visual/Seam`: a torus 3 mm proud of the felt |
| `balloon` | `Envelope` (drawn radius follows the committed gas radius; it equals the collider at authored state) | `Neck`: moved by a binding; `Carrier`: inside the envelope | `String`: a 0.85 m rod below the envelope |
| `rope_anchor` | `Visual_Mount` | none | `Visual_Eye`: a torus in front of the mount |
| `pulley` | `Visual_Mount`; `Wheel/Visual_WheelBody` | `Wheel/Visual_Rim`, `Visual_Spoke`, `Visual_Marker`: under the binding-turned wheel | `Visual_Post` |
| `fan` | `Base`; `Hub/Surface` on the hinge body | the three blades: turned by a binding | `Post`; `Ring` (the shroud torus); `Arrow` and `Tip` |
| `windmill` | base, tower, nacelle; `Guard` (imported mesh, tube collider); hub and four blades on the hinge body | four blade `Tip`s: coincident with the blade ends; `Output/Spoke0`–`1` | `Output/Wheel`: a wheel outside the nacelle |
| `cannon` | `Jacket`, two collars (imported meshes, tube colliders); `Breech`; `Foot` | `ChargeBar`, `ReadyFlag`, `Sleeve`: moved by bindings | `ChargeTrack`; `Indicator`; `PowerIn`; `ActivationIn` |
| `linear_pusher` | `Head/Sphere` on the slider body | `Rod`: drawn by the clipped extrusion binding | `Indicator`; five `Socket_…` spheres; `Head/Ring`; `TravelMark0`–`4` (raised) |
| `wound_spring` | (its solids are visible proxies) | `Coil/Turn0`–`63`: drawn by compression; `Latch/Lever`, `Mark0`–`4`: moved by bindings; `Shaft/Spoke` | `ActivationSocket`; `Head/ChargeIndex` |

Counts: 22 meshes without colliders in nine packages (1, 1, 1, 1, 4, 1, 4, 7, 2), plus ten raised markings in two packages (`impact_lever` 5, `linear_pusher` 5). Precedents for the repair: sockets, indicators and lamps were given colliders by `spec-elements-visible-solid-sweep.md`; the Basketball seam and the Weight eye are torus colliders; raised markings were reprinted flush by `spec-face-markings-flush.md`. Consequences of the binding rule for the eleven packages, to be met by the prerequisite's rows: `linear_pusher` `Rod` and `wound_spring` `Coil/Turn0`–`63` are supported as span nodes; `wound_spring` `Mark0`–`4` are moved by parameter bindings, which do not support the reason, and `Shaft/Spoke` sits on a hinge body with no binding, so both need a repair or another true reason in part B. The owner's decisions of 21 November 2026 settle the rest: the Pulley rim is raised as one further item in part A's contract; the Cannon sleeve is confirmed by part C when it reads imported meshes.

### Decisions

Decided, with reason:

- **D1. The declaration is build-time data on `ISceneGeometryElement`, not a bundle field.** Only the generator and tests consume it; the runtime never reads art solidity, so shipping it would add a field nothing uses. `SceneBodies` sets the precedent (guide D3, D5).
- **D2. Entries name nodes; a subtree is classified by naming its root.** This replaces the table's string wildcards, makes "classified twice" a definite condition, and keeps large groups (64 coil turns) to one entry (guide D1).
- **D3. Moving bodies come from `SceneBodies`.** The table's separate moving-root column repeats those roots (checked for `spring`, `motor` and `clutch`); task 5 confirms the rest and stops if any differ.
- **D4. `Fit` stays, as a declared per-entry tolerance** bounded at 0.005 m (D8). Two packages use it (`wall` 0.002, `weight` 4e-6), each for a drawing reason stated beside it.
- **D5. Every decorative entry carries a typed reason.** `DecorativeReason` is a closed enum of the table's four reasons (printed flat on a face, a glow, enclosed by another solid, moved by a presentation binding) and the owner's one named exception, a thin non-solid tether. The generator checks the binding reason against the package's declared bindings: a `PresentationBinding` whose `Property` is `LocalX`, `LocalY`, `LocalZ`, `LocalRotationZ`, `UniformScale`, `ScaleX`, `ScaleY`, `ScaleZ` or `Visible`, or a `PresentationSpanBinding`'s span node. `Bodies`, `Parameters` and colour or emission bindings do not support it, so a lamp cannot be declared decorative through its colour binding. The rule for art turned by a binding (decorative unless it is a solid of revolution about the turning axis) is stated once in `spec-elements-visible-solid-remainder.md`, decision D5. The other reasons are not proven by the build; task 7 makes them a reviewer checklist item and S4 owns turning `PrintedFlat` and `Enclosed` into executed checks. The Fan's blades are `MovedByBinding` (owner-confirmed, no collider); the Balloon's string is `NonSolidTether`.
- **D6. A visible proxy mesh is art.** `linear_pusher` and `wound_spring` draw their solids with visible `col_` meshes; part B makes the matcher treat them as art, and the declaration classifies them solid or hollow.
- **D7. `NonSolidTether` requires a cited owner decision on each use** (decided under the owner's standing delegation: it keeps the reason the named exception the owner worded, where a geometric bound would turn it into a general class). The entry carries the citation; the generator refuses the reason without one. The only use is the Balloon string (owner, 21 November 2026).
- **D8. The 0.005 m fit ceiling is a play bound:** 5 mm between a drawn and a solid surface is below what a player can see against the smallest delivered ball. It is not the matcher's tessellation bound.
- **D9. `FaceMarkingTests` is left to S4** (decided under the same delegation): the intent is narrowed to the table this slice deletes. `SceneArt` carries no face or enclosing-solid reference (coordinator decision, 21 November 2026, under the owner's delegation): S4 finds the face a printed entry lies on, and the solid an enclosed entry lies in, geometrically from the package's colliders, which keeps data that no assertion can prove out of the declaration.

### Owner decisions (21 November 2026)

- **Order.** A product repair slice first (`ELEMENTS-VISIBLE-SOLID-REMAINDER`), then this slice, then S4.
- **Repairs follow precedent.** Sockets, indicators, posts, the charge track and the output wheel get box, sphere or cylinder colliders; the four rings get torus colliders; raised markings and the charge index are redrawn flush. All are made by the prerequisite, none here.
- **Fan arrow and tip** are redrawn as a flush marking on the housing (prerequisite, part A).
- **Balloon string** stays visible and is declared decorative as a named exception to "visible = solid": a thin non-solid tether.
- **Fan blades** stay decorative, with no collider.

## Tasks & Acceptance

**Execution:**

1. `CuriousContraptions.Abstractions/SceneGeometry.cs` -- add `enum ArtSolidity : byte { Solid = 1, Hollow = 2, ArcChord = 3, Decorative = 4 }`, `enum DecorativeReason : byte { PrintedFlat = 1, Glow = 2, Enclosed = 3, MovedByBinding = 4, NonSolidTether = 5 }`, `readonly record struct SceneArt(string Node, ArtSolidity Solidity, DecorativeReason? Reason = null, float Fit = 0f, string? OwnerDecision = null)` and the required member `IReadOnlyList<SceneArt> SceneArt { get; }` on `ISceneGeometryElement`. `CuriousContraptions.Abstractions/BundleServices.cs` -- add `ArtUnclassified = 16`, `ArtClassifiedTwice = 17`, `ArtEntryUnresolved = 18`, `ArtReasonUnsupported = 19` -- M2–M5.
2. `tools/BundleElements/SceneGeometryExtractor.cs` -- expose the parsed scene nodes and the proxy test to the validator (one internal method each; no second parser). `tools/BundleElements/PackageValidator.cs` -- add `Art(root, packages)`: the art meshes of a scene are its `MeshInstance3D` nodes that are not hidden proxies; apply M2–M6, reading the package's presentation bindings for the binding reason. `PackageValidator.Art` runs for every discovered package; one that does not implement the interface has no entries -- the rule.
3. `tools/BundleElements/BundleGenerator.cs` -- add an internal overload `Generate(IBundleServices, string root, DiscoveryResult discovered)` that the public method calls, and add `PackageValidator.Art` to its pre-bake checks at 14–16 -- every build enforces it, and a test can prove it is wired in.
4. `tools/BundleElements.Tests/Fixtures.cs` and new `tools/BundleElements.Tests/ArtSolidityTests.cs` -- give `SceneBodyFixture` `SceneArt { get; init; } = []` and a settable `Presentation` (`Fixtures.cs:114`), so each test supplies its own entries and bound nodes; write the tests in the table, each on a scene written to a temporary package folder as `BundleValidatorTests` does for assets -- M1–M6, M8.
5. `elements/<Name>/<Name>ElementDeclaration.cs`, all 45 -- declare `SceneArt` from the package's row of `Specs` (as extended to 45 by the prerequisite): each path or wildcard becomes node entries, each comment becomes the entry's `DecorativeReason` (and stays as a comment where it says more), each `Fit` moves to the entries it applied to. Confirm for each package that the row's moving roots equal `SceneBodies` after the base; stop and return to planning if one differs -- M1.
6. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- the theory's cases become every delivered identity (`SaveFixture.Elements`); it reads `SceneArt` and `SceneBodies` from the discovered declaration (`TestBundle.Packages`); delete `ArtSpec`, `Specs`, `Logic`, `Names` and `Listed`. Keep both classification assertions of 194–195, computed from the declared entries against the art read through Godot: every art mesh is classified by exactly one entry, and every entry classifies at least one art mesh; an entry matches its own node or any mesh beneath it, with no wildcard. Use the generator's proxy name rule (prefix or `-col_` infix), where the test checks the prefix only today. Matching, tolerances and the parameter sweep do not change -- M7.
7. The documents under Owned scope -- state the declaration, the generator rule and that a new package classifies its art in its declaration; mark the story delivered; add to the reviewer checklist of `docs/testing-guidance.md` and to `docs/engine-contracts.md`: "A new or changed `Decorative` entry is a finding unless the entry's comment gives checkable evidence for its reason: `PrintedFlat`, the face it lies on and its height above it (at most 0.5 mm, the bound in `FaceMarkingTests.cs:59`); `Enclosed`, the enclosing solid and both extents; `Glow`, the material; `NonSolidTether`, the owner decision and its date." Record in deferred work, owner S4: executed `PrintedFlat` and `Enclosed` checks driven by the declared reason.

**Acceptance Criteria:**

- AC1: Given the candidate diff, when its paths are listed, then none is under `CuriousContraptions.Bundle/`, the declaration of `BundleEntry` (`CuriousContraptions.Abstractions/BundleServices.cs:16`) is unchanged, no `.tscn` changed (`git diff --stat <baseline_revision> -- 'elements/*/*.tscn'` prints nothing), and `BundleRoundTripTests.EveryPackageEqualsItsDeserialisedEntry` and `SceneGeometryExtractorTests` pass unchanged. The bundle file is git-ignored, so its text is not an acceptance observation.
- AC2: Given a declaration whose scene gains one mesh, when the bundle is generated, then the build fails with `ArtUnclassified` naming that node, before any test runs.
- AC3: Given the candidate, when `git grep -n -E 'new PartId\("' -- CuriousContraptions.tests/VisibleSolidColliderTests.cs` runs, then no match lies in the classification or the all-package theory (the feature tests further down keep theirs).
- AC4: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline total plus the twenty-four new cases of `ArtSolidityTests`; the all-package theory keeps one case per delivered identity.
- AC5: Given the candidate diff, when its paths are listed, then none is under `CuriousContraptions.Bundle/`, `engine/`, `schemas/`, `ui/` or any TypeScript source.

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour delivered.** A package's art classification is declared and enforced at build; the visible-solid proof covers every delivered package from that data. Nothing a player sees changes. Source: owner rule "visible = solid", 25 October 2026; `docs/planning/invest/delivery-batches.md` S4 row 4.
2. **Reusable.** One declaration kind (`SceneArt`, a closed four-value enum) and one generator rule over any scene. Inputs: scene node tree and entries. Output: admitted, or a typed refusal naming the node.
3. **Declared.** Per package: node path, solidity, the decorative reason, the owner-decision citation where the reason is the tether, and an optional fit, beside `SceneBodies`. Exact node references; no inference from names (guide D1, D3).
4. **Execution.** Declaration → `PackageValidator.Art` in `BundleGenerator.Generate` → bundle admitted or refused whole. Nothing reaches the bundle, compiler, worker or renderer.
5. **Ownership.** Build-time only: no state, no writer, no Run, Reset, save or load path. Refusal is atomic: any error yields no bundle (`BundleGenerator.cs:34–35`).
6. **Generality.** Tests use an unfamiliar fixture scene, reordered entries, subtree against per-mesh entries, a nested subtree, a visible and a hidden proxy, and each refusal; expected codes and node paths are written as literals.
7. **Deleted.** `ArtSpec`, `Specs`, `Logic`, `Names`, `Listed` and the wildcard convention in `VisibleSolidColliderTests.cs`. No alias or optional path remains.
8. **Uncertain.** Nothing beyond the prerequisite. Rules E1, E4, E6, E7, E8 and E9 apply; E2, E3 and E5 are untouched (no runtime value, command or storage changes). D1–D5 as cited; D6 and D7 do not apply (no physical law, no animation).

What this removes and what catches the failure afterwards: the hand table failed, at test time and only for its listed packages, when a mesh was not classified once or an entry named no art (`VisibleSolidColliderTests.cs:194–195`). Afterwards the generator fails the build for every package on the same two conditions. The Godot-side classification check stays in the theory as the independent reader: the generator reads scene text, the test reads instantiated nodes. Residual: a mesh declared printed, a glow or enclosed without that being true passes, as it does today; the binding reason is checked.

## Verification

Executor for every command: the implementation owner. Raw output goes under `.tools/engine-art-solidity-evidence/`; each result is one line here citing its run-log entry.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| M1, AC1 | `ArtSolidityTests.EveryRealPackageClassifiesEveryArtMeshOnce` (new fact: `PackageValidator.Art` over the discovered packages returns no error) | The refusal tests below use the same call | planned |
| M2, AC2 | `ArtSolidityTests.AnUnclassifiedMeshFailsTheBuild` (new fact, run through the internal `Generate` overload so it fails if the check is not wired in; expects `BUNDLE016` naming `Fixture` and `Visual/Extra`) | The same scene with the entry added generates a bundle | planned |
| M2 (no declaration) | `ArtSolidityTests.APackageWithoutTheDeclarationIsRefusedForItsArt` (new fact: a declaration that does not implement the interface, with a scene holding one mesh, expects `BUNDLE016`) | The same scene with no mesh passes | planned |
| M6 | `ArtSolidityTests.AnArtMeshUnderAHiddenNodeStillNeedsAnEntry` (new fact: a mesh under a `Node3D` with `visible = false` and no entry expects `BUNDLE016`) | A hidden `col_` mesh with no entry passes | planned |
| M3 | `ArtSolidityTests.AMeshClassifiedTwiceFailsTheBuild` (new theory, 2 cases: own entry plus ancestor; two nested ancestors; expects `BUNDLE017` naming the node) | Removing either entry passes | planned |
| M4, M6 | `ArtSolidityTests.AnEntryThatClassifiesNothingFailsTheBuild` (new theory, 3 cases: unknown path; a hidden `col_` mesh; a node with only proxies beneath it; expects `BUNDLE018`) | A scene whose proxies have no entry passes | planned |
| M5 | `ArtSolidityTests.AnInvalidSolidityOrFitFailsTheBuild` (new theory, 14 cases with literal codes: undefined solidity and undefined reason, `BUNDLE003`; decorative with no reason, a reason on a solid entry, `MovedByBinding` on an unbound node, on a node with only an albedo binding, on a node under a `Bodies` root and on a mesh under a span's moving node, `NonSolidTether` with a null, empty or white-space citation, and a citation on a printed entry, `BUNDLE019`; a non-finite fit, `BUNDLE002`; a negative fit, a fit one f32 step above 0.005 and a fit on a decorative entry, `BUNDLE006`) | Fit of exactly 0 and exactly 0.005 pass; `MovedByBinding` on a child of a bound node passes | planned |
| M8 | `ArtSolidityTests.OrderAndGroupingDoNotChangeTheResult` (new fact: reversed entries, and a subtree entry replaced by per-mesh entries, both pass) | M3 is the case where grouping does change the result | planned |
| M7 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (existing, cases from the delivered identities) | The existing feature tests of that class; each bound parameter at minimum and maximum | planned |
| AC1 | `BundleRoundTripTests.EveryPackageEqualsItsDeserialisedEntry` (existing, unchanged) and the `git diff --stat` of AC1 | Unchanged | planned |

No simulated float is compared; the matcher's tolerances are unchanged and are not restated here.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors; the bundle generates.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary. Run when the candidate first compiles and again here.
- `git diff --stat <baseline_revision> -- 'elements/*/*.tscn'` -- expected: no output.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: baseline count + 24, with `FullyQualifiedName~ArtSolidityTests` added to the filter in task 7; 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~ArtSolidityTests|FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~BundleRoundTripTests|FullyQualifiedName~BundleValidatorTests|FullyQualifiedName~PortLocatorTests|FullyQualifiedName~SceneGeometryExtractorTests|FullyQualifiedName~CSharpStaticGateTests"` -- expected: 0 failed, 0 skipped; every test in the table appears.
- `git add -N tools/BundleElements.Tests/ArtSolidityTests.cs`, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` -- expected: baseline total + 24, 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass; its snapshot build generates the bundle, so it enforces the rule on the index.

### At replanning

Once parts A, B and C of `ELEMENTS-VISIBLE-SOLID-REMAINDER` are published:

1. Re-read `Specs` (one row per delivered package: 45 expected, the Baseball included) and every line number; recount N and take the totals as the current full local round plus this slice's delta.
2. Carry the matcher's three extensions into task 6 exactly as parts B and C delivered them: visible proxy meshes as art, imported meshes read by their bounds, and the Balloon envelope's gas-radius scale.
3. Give each decorative entry of the 45 rows one of the five reasons under the binding rule above. Rows whose comment maps to none today, to be measured then: `conveyor` `Visual/Roller/PulleyIn/*` and `Visual/PulleyOut/*` (measured on `8d1d68aa`: each wheel is a cylinder of radius 0.22 and length 0.1 at z 0.78, outside every collider; the input pulley rides the roller's hinge body with no binding; these are repaired in part B); `clutch` `Visual_Axle`, both spokes and both plates; `reverse_transmission` `Index` and `Mark`; `windmill` blade `Tip`s; `latch` markers; `hold_timer` `Track`. A row that fits no reason is a product finding for the prerequisite or returns to the owner.
4. Confirm the Balloon string and the Fan blades are the only owner-approved exceptions in the table, and how part A settled the Pulley rim.
5. Confirm no package was added since, or classify it; pass the planning audit again.

### Running beside other work

Blocked until its prerequisite publishes. It edits every package declaration (45 at drafting), so it is serialised with every element slice: an element slice published first adds its own `SceneArt`, and one in flight merges main once and adds it. It shares `docs/delivery-workflow.md`, `deferred-work.md`, `TODO.md` and `docs/planning/invest/delivery-batches.md` with S3 and S4 (S2 is published).
