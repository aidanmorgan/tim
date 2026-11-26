---
title: 'ELEMENTS-VISIBLE-SOLID-REMAINDER, part C: Cannon, Windmill and Balloon'
type: 'bugfix'
created: '2026-11-21'
status: 'done'
baseline_revision: 'd37c0b9830f0a98660338e1a9bcb5cda61530943'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: []
deferred:
  - summary: >-
      The Battery's four charge marks and plus symbol stand 15–20 mm proud of its face with no collider.
    evidence: |-
      Review pass 1, BH7. battery.tscn:30–40, :68–94 (BoxMesh 0.02 m and bars 0.015 m deep); pre-existing, in a package this slice does not change, breaks no hard rule. Owned by the Battery redraw planned at docs/planning/elements/readiness/batch-b.md:77 (QuadMesh prints flush), which also rewrites the VisibleSolidColliderTests.cs battery-row comment.
    location: >-
      elements/Battery/battery.tscn
    severity: medium
  - summary: >-
      Decorative classifications that rest on containment (the Balloon's carrier, the Windmill's blade tips) are stated in table comments, not asserted.
    evidence: |-
      Review pass 1, IA6. The visible-solid table's decorative list is unchecked by design (unchanged here); the containment holds by the planning audits' geometry (audit 5). Owned by ENGINE-ART-SOLIDITY, which makes decorative entries declared, cited and checked.
    location: >-
      CuriousContraptions.tests/VisibleSolidColliderTests.cs (Specs rows balloon, windmill)
    severity: low
  - summary: >-
      Lane 2's draft S5c contract cites WindmillAdmissionTests.cs:281-289, which this slice moved to :302-310.
    evidence: |-
      Review pass 2, EFE-R4. The file is lane 2's (spec-s5c-one-wire-schema-file.md); the shared-document rule (owner decision 24m) forbids editing another lane's file without reporting first. The deferred-work.md:381 carry-forward names the move; owner: lane 2 (S5c), retry condition: S5c re-reads its cited lines at dispatch; reported to the lane coordinator.
    location: >-
      _bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md
    severity: low
---

<intent-contract>

## Intent

**Problem:** The owner's rule is that what a player sees as solid is solid. A ball passes through the Cannon's charge track, indicator and two sockets and through the Windmill's output wheel. These two parts and the Balloon are outside the visible-solid table because its matcher cannot read an imported mesh (the Cannon's jacket, collars and sleeve; the Windmill's guard) or an envelope whose drawn size follows the gas it holds.

**Approach:** Give each of those five drawn solids a collider of exactly its drawn shape, teach the matcher to read an imported ring from its mesh and to size the Balloon's envelope from its declared gas, prove on the real part that the envelope is drawn at its collider's radius, and add the three packages to the table, which, with parts A, B and B2 and the Baseball repair, then covers all 45 delivered packages. The Balloon's string stays visible and decorative: it keeps its named exception (owner, 21 November 2026; owner decision 1, 22 November 2026), which `ENGINE-ART-SOLIDITY` declares as the package's cited decorative entry, while every rope, cable, hose and duct is solid.

## Boundaries & Constraints

**Always:** A new collider is a separate proxy that is not rendered, of the same primitive, size and pose as the mesh it backs, appended after the base body's existing proxies so no existing collider index moves, with the body's existing material. The Windmill's own airflow capture is not occluded by its own new collider. Art turned by a presentation binding is decorative under the rule stated once in part A (its D5). Every existing behaviour is preserved: Cannon loading, seating, admission window, charging and firing; Windmill airflow capture, rotation and drive output; Balloon buoyancy, rupture, gas radius and tie. The matcher's tolerance for primitive art does not change; an imported mesh alone is compared with one stated storage allowance. Reset, save and load restore each part as before. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A collider for the Balloon's string, or a second "non-solid tether": that class needs a cited owner decision per use, and only the string has one (owner, 21 November 2026; owner decision 1, 22 November 2026, which makes ropes, cables, hoses and ducts solid). A collider for the Cannon's sleeve, charge bar or ready flag. A collider larger, smaller or elsewhere than the drawn mesh. A matcher rule that names a package. A Balloon scene or declaration change. A change to the shared presentation projection (an engine slice, if the owner asks for it). A new parameter, port, declared `ElementFootprint` or moved port. Hashes, exact arrival ticks, mutants, browser runs, skipped tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| C1 Cannon indicator | A Basketball dropped onto the indicator of an upright Cannon | It stops at the drawn sphere | No error expected |
| C2 Cannon track and sockets | The compiled Cannon | One box at the drawn charge track and one sphere at each drawn socket (proven by shape: they sit between the foot and the jacket, where a 0.34 m ball cannot land first) | The table row fails for a missing one |
| C3 Cannon jacket and collars | The imported meshes and the baked tubes | Each drawn ring matches its tube collider in outer radius, bore, half-length, centre and axis | A mismatch fails naming the mesh |
| C4 Cannon play | A ball loaded, seated, charged and fired | As before: the new colliders are outside the bore and belong to the Cannon's own body | Empty or obstructed attempts retain charge, as before |
| C5 Windmill output wheel | A Basketball dropped onto the wheel's face, the Windmill on its back | It stops at the drawn face | No error expected |
| C6 Windmill's own capture | The compiled Windmill; a Fan behind it | Its capture exempts all ten of its own colliders, the output wheel included; rear airflow turns it as before | A solid that is not its own still occludes |
| C7 Windmill guard | The imported mesh and the baked tube | They match: outer 0.815 m, bore 0.745 m, half-length 0.035 m | A mismatch fails |
| C8 Balloon envelope | Radius 0.18, 0.36 and 0.5 m in Standard atmosphere: the real part once its first declared sample has arrived in build mode, at Run start, and after Reset | The drawn radius equals the collider's radius within 0.1 mm | A mismatch fails naming the radius and the state |
| C9 Balloon string | The table row | Classified decorative as the owner's named exception; it has no collider | Enforced by review in this slice; `ENGINE-ART-SOLIDITY` makes an uncited tether a build refusal |
| C10 imported reader | A ring mesh; meshes that are not rings | The ring is read as inner radius, outer radius and half-length; a box, a capped cylinder and a half ring have no shape or are refused | A non-ring classified hollow fails its row |
| C11 table coverage | The visible-solid test for the three packages | Every solid or hollow mesh has one matching collider and no collider is without art | Existing failure messages |
| C12 retained behaviour | The named existing tests of each part | Pass unchanged | Existing |

</intent-contract>

## Code Map

Delivery shape: one product repair slice over three packages, the last part of `ELEMENTS-VISIBLE-SOLID-REMAINDER` (order A, B, B2, C); not a Family slice, closes no duplicate. Review tier: **engine** (shared matcher code in a test class; probe in run log E2). No new source file and no test-project edit: `CuriousContraptions.tests.csproj:73` compiles every top-level `*.cs`.

**When.** Before the engine programme (owner decision 22). Replanned at dispatch on part B2's published main: `Solid.Inner` (`VisibleSolidColliderTests.cs:75–76`), the bore rule `MatchesHollow` (`:489–508`) and the stop-only theory `ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface` (`:744–752`) are part B's; `Specs` has 41 rows (the Baseball repair has not published). O1 is settled by owner decision 5.

**Baseline.** `baseline_revision` `d37c0b98` (`origin/main`, part B2 published), captured by the lane 1 coordinator at dispatch (coordinator decision 38); every scope guard and `git diff` compares against it. Drafted on `25df3cd5`; every cited line re-read at the baseline (run log E2, audit 5). Baseline full local round 7431/0/0, registration 829; expected totals are baseline + delta.

Planning verdict: **Scoped Pass** at `d37c0b98`, dispatch re-audit with non-blocking corrections N1–N8, applied outside the intent contract (`docs/planning/elements/readiness/audits/audit-visible-solid-C-5.md`; run log E4); it supersedes audit 4 for line anchors. The amended G1 (Spec Change Log 2) re-passed in audit 5's addendum (run log E8).

Implementation owner: fresh agent `a73ff9e7ea447ad6a` (no inherited history, inherited model; run log E6), retained for every repair.

Both matcher changes can be made in the test harness alone. An imported mesh's vertices are readable through the scene (`CannonArtTests.cs:22–24` already does it), and the gas radius is a pure function of declared data.

Owned scope:

- `elements/Cannon/cannon.tscn` -- `Visual/Jacket` 88, `Sleeve` 103, `ChargeTrack` 116, `Indicator` 128, `col_Foot` 155, `PowerIn` 163, `ActivationIn` 170.
- `elements/Windmill/windmill.tscn` -- `load_steps` 1, `Mesh_Output` 61, `col_guard` 88, `Visual/Guard` 112, `Output/Wheel` 202.
- `elements/Windmill/WindmillElementDeclaration.cs:37–38` -- the capture's internal occluders.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- `Tol` 37, `Solid` 75–76, `Specs` 78–215, `Art` 355–383, `BodyPose` 388–416 (binding loop 397–411), `Primitive` 419–454 (shape switch 423–438), `MatchesHollow` 489–508, `Feature` 586–592, `Drop` 602–683, the class, `ArtSpec`, `Solid`, `Art`, `Feature` and stop-only theory summaries (11–28, 43–74, 345–354, 582–585, 737–743), the exception message `:259`, XB-04's stale comments `:87`, `:126`, `:147`, `:179` (`completion-ledger.md:318`; routed by part B2, its run log E4 ruling 2) and the same kind at `:134`, `:142` (audit 5 N6).
- `CuriousContraptions.tests/WindmillAdmissionTests.cs`, `CuriousContraptions.tests/BalloonArtTests.cs` -- one new test each.
- `docs/planning/elements/CAT-016-cannon.md`, `CAT-070-windmill.md`, `CAT-003-balloon.md`, `EL-208-balloon.md` (records and the stale citations of run log E2); `_bmad-output/implementation-artifacts/deferred-work.md` (the `first-committed-sample` entry); `docs/planning/invest/delivery-batches.md` section 5 (Repairs, part C's row); `docs/planning/elements/readiness/completion-ledger.md` (XB-04 `:318`, part C's row `:228`); `docs/planning/elements/readiness/audits/audit-visible-solid-C-5.md`; `TODO.md`; this contract and its run log; part B2's contract and run log (publication receipt and `deferred-work.md` citations, run log E1).

`elements/elements.bundle.json` is build output, not committed (`.gitignore:17`); task 0 keeps a baseline copy outside the checkout (Spec Change Log 1).

Owned records added at review pass 1 (EFE-F7): `docs/planning/elements/readiness/batch-d.md` (`:18`, `:20`, `:41`), `batch-m.md` (`:45`), and `completion-ledger.md` rows DC-09 to DC-11 (`:140–142`); at pass 2 (EFE-R3), `batch-c.md` (`:16`).

Not consumers, checked: the Cannon's chamber names base collider 0 only for participation (`CannonElementDeclaration.cs:29–31`). No test counts the Cannon's or the Windmill's colliders: `WindmillAdmissionTests.cs:188` selects the single tube, `CannonIntegrationTests.cs:444` a gate blade's collider, and `WorkbenchCapacityTests.cs:143` (a grid of 33 of each) and `PackageRuntimeParityTests.cs:667` derive counts from the bundle. `BalloonTests.cs:152` is untouched. No test reads `ChargeTrack`, `Visual/PowerIn` or `Visual/ActivationIn`; `WindmillArtworkTests.cs:77` reads `Visual/Output/Wheel`, which is not renamed. `engine/PlacementPose.cs:113` follows the colliders (Footprint section). `content/puzzles.json` uses neither the Cannon nor the Windmill, and one Balloon (placement 62), unchanged. The pre-commit hook runs two test classes that load no scene (`.githooks/pre-commit:52–54`). Other classes call the helpers `VerticesInRoot`, `AssertFaces`, `AssertPrintedFlat`, `AssertInsideRectangle`, `AssertInsideDisc` (`:910–955`); their signatures stay.

### Capability probe

Re-cited at `d37c0b98` (run log E2). Eight present, one present and untested (test added here), two narrower and owned here (test harness), one narrower and owned by `ENGINE-ONE-WAY-INSTALL` (O1).

- **Present.** Box and sphere colliders from hidden mesh proxies and a cylinder from a `CollisionShape3D`: `SceneGeometryExtractor.BakeProxy` (`tools/BundleElements/SceneGeometryExtractor.cs:99`), in file order per body (`:56–67`); the planned `col_*` names meet no name refusal (`:104–106`, `:171–177`). Tests: `SceneGeometryExtractorTests.NamedBoxAndSphereMeshesAreBuildTimeProxies`, `…CylinderShapesAndUntaperedMeshesBakeAsTrueCylinders`; the drop theory in `VisibleSolidColliderTests`.
- **Present.** Tube colliders from hidden open-cylinder proxies: `SceneGeometryExtractor.Hollow` (`:197–238`). Tests: `WindmillAdmissionTests.ActualPackageSolidsContactProbesWhileBoreAndMissRemainClear`, `CannonIntegrationTests.AuthoredMountAimsAlongItsBarrelAndTenMillimetreMuzzleWallRefuses`.
- **Present.** Chamber occupancy counts dynamic bodies and skips the frame; seat width reads the payload's colliders; muzzle clearance skips the frame's own: `ChamberOccupancy`, `SeatInChamber`, `PayloadExitClearance` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Chambers.cs:80`, `:112`, `:142`, the skip at `:162`). Tests: `CannonIntegrationTests.TwoPhysicalOccupantsPresentUnavailableAndResetToEmpty`, `…OpeningTheMuzzleGateDoesNotReplayAnExpiredRequest`.
- **Present.** A rotary capture exempts its listed own colliders: `RotaryAirflowCaptureDescriptor.InternalOccluders` (`CuriousContraptions.Abstractions/RotaryAirflowValues.cs:14–17`), admitted at `engine/gpu/AirflowSceneValidation.cs:77–80` with no count limit; each entry is (package body, collider), body 0 the base and body 1 the rotor, whose five colliders are in `MechanicalPayload.MovingBodyColliders`. Tests: `RotaryAirflowRuntimeTests.FourActualPoseSamplesLoseOnlyOccludedWeight`, `WindmillIntegrationTests.ActualSourcePoseAndSignedExposureDetermineDrive`.
- **Present.** The gas radius of a declared volume: `IsobaricGas.OuterRadius` (`CuriousContraptions.Abstractions/IsobaricGas.cs:130`) over `GasStoreDescriptor.ReferenceVolume` (`GasStoreDescriptor.cs:9–10`); the presentation driver uses the same function (`engine/gpu/WorkshopDeclaredPresentation.cs:402–405`). Tests: `GasCapabilityTests`; `BalloonTests.NondefaultLiftAndStrongDragChangeStandardMotion` (the compiled collider keeps the authored radius within 0.1 mm).
- **Present.** Reading an imported mesh's arrays from the loaded scene: `ArrayMesh.SurfaceGetArrays`. Test: `CannonArtTests.NativeAnnuliAndPrivateCuesUseTheManagedFactoryAndRestoreAuthoredArt`. It needs the Godot import cache (`.godot/imported`), which a fresh checkout lacks until the build imports the assets; the `.obj.import` files leave mesh compression enabled, which `ImportedStep` budgets for.
- **Present.** No mass is derived from colliders: the Cannon and the Windmill's base are static, the rotor declares a constant frame (`WindmillElementDeclaration.cs:31–32`), the Balloon's mass is its declared mass plus gas (`BalloonElementDeclaration.cs:57–61`). Test: `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight`.
- **Present (part B).** A tube's bore from the art, `Solid.Inner` (`VisibleSolidColliderTests.cs:75–76`), in `MatchesHollow` (`:489–508`), and the stop-only theory `ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface` (`:744–752`).
- **Present, untested.** The drawn radius of gas-radius art at the authored state, before Run and after Reset. By reading: the Animation worker advances whenever a schedule is installed, running or not (`CuriousContraptions.Animation.Worker/Program.cs:86–117`); a schedule is installed at the seed boundary in build mode (`engine/gpu/BrowserWorkshopClient.Schedule.cs:89–99`); the evaluator takes the committed read at installation and holds the anchor time when not running (`engine/gpu/WorkshopDeclaredPresentation.cs:89–155`, `:637–649`). So a sample carrying the committed gas radius reaches the part without Run. No test asserts it: `BalloonArtTests` checks the scale only for an empty store. Test added: C8. It exercises the evaluator, the production client's `TryDeclaredFrame` (`engine/gpu/BrowserWorkshopClient.DeclaredPresentation.cs:133–147`, natively through `WorkshopActivationAnimationTests.NativeClient`; review pass 2, BH2-1) and the part's application; the Animation worker's own loop is proven by reading only and is browser qualification, pending.
- **Narrower, owned by `ENGINE-ONE-WAY-INSTALL` (O1).** Construction and every Reset first draw the zero-input projection of each binding (`engine/MachinePart.cs:211–221`, `:262–267`; `PresentationDriverRules.InitialValue`, `CuriousContraptions.Abstractions/ElementPresentation.cs:184–199`). For the envelope that is clamp(0, 0.0625, 16) = 0.0625 m, and for the neck 0, until the first declared sample arrives. The first committed sample on construction and Reset is that engine-programme story's (order 6; engine stories index, section 2; owner decision 5).
- **Narrower, owned here.** The matcher gives an imported mesh no shape (`Primitive`'s shape switch, `VisibleSolidColliderTests.cs:423–438`). Change G1.
- **Narrower, owned here.** The matcher applies only parameter bindings to art (`BodyPose`, `:397–411`, reads only `Presentation.Parameters`), so the envelope stays at its authored 0.36 m when the radius parameter is swept. Change G2.

### Repairs, mesh by mesh

Measured in each scene; poses are part-local.

| Package | Mesh | Repair |
|---|---|---|
| `cannon` | `Visual/ChargeTrack`: box 0.8 × 0.11 × 0.07 at (0, −0.55, 0.62) | Hidden `MeshInstance3D` `col_ChargeTrack` under `.`, mesh `Track`, same transform |
| `cannon` | `Visual/Indicator`: sphere r 0.07 at (−0.66, 0.65, 0.4), on top of the breech | Hidden `col_Indicator`, mesh `Indicator`, same transform |
| `cannon` | `Visual/PowerIn` (−0.45, −0.55, 0.55) and `Visual/ActivationIn` (0.15, −0.55, 0.55): spheres r 0.07 | Hidden `col_PowerIn` and `col_ActivationIn`, mesh `Socket`. The four follow `col_Foot` in this order with `visible = false`, in the Cannon's own proxy style: base colliders 5–8 |
| `cannon` | `Jacket` (bore 0.48, outer 0.58, half 0.55), `RearCollar` and `MuzzleCollar` (0.48, 0.65, 0.05): imported rings | None: their tube colliders exist; G1 lets the row prove it |
| `cannon` | `Sleeve`: imported ring, bore 0.5, outer 0.565, half 0.07, slid between x −0.22 and −0.38 by the recoil binding | None, confirmed decorative. Over its whole travel (x −0.45 to −0.15) it lies inside the jacket's wall (radii 0.48–0.58, x ±0.55), and a binding moves it |
| `cannon` | `ChargeBar` (scaled by the stored charge), `ReadyFlag` (turned by the firing state) | None: moved by bindings (part A, D5) |
| `windmill` | `Visual/Output/Wheel`: cylinder r 0.24, length 0.12, axis Z, centre (0.25, −0.1, 0.44), 0.105 m in front of the nacelle | `CollisionShape3D` `col_output` under `.`, new `CylinderShape3D` `Shape_Output` (r 0.24, h 0.12), position (0.25, −0.1, 0.44), rotation (1.5707964, 0, 0), after `col_guard`: base collider 4; `load_steps` 21. Its node is turned about the wheel's own axis, which a static cylinder follows exactly (part A, D4) |
| `windmill` | `Output/Spoke0`–`1` | None: turned by the output binding |
| `windmill` | `Rotor/Arm0`–`3/Tip`: 0.075 × 0.06 × 0.21 at the end of each blade | None: each lies inside its blade's box (blade 0.14–0.66 along the arm, tip 0.60–0.66, same section) |
| `windmill` | `Visual/Guard`: imported ring, bore 0.745, outer 0.815, half 0.035 | None: its tube collider exists |
| `balloon` | `Envelope` | None: the collider follows the same gas |
| `balloon` | `String`: rod r 0.006, length 0.85 below the centre | None: the thin-string exception (owner, 21 November 2026; owner decision 1, 22 November 2026), visible and decorative |
| `balloon` | `Neck` (moved by the gas-radius binding), `Carrier` (r 0.0625, inside the envelope at every radius from 0.18 m) | None |

The Windmill's capture gains `new(new(0), new(4))`: its internal occluders are then base colliders 0–4 and rotor colliders 0–4, all ten. It is needed: with a Fan behind the Windmill, the ray to the capture sample at (−0.12, 0, 0.42) crosses the wheel. To any other stream the wheel is an ordinary solid. Repair part D later seats the wheel 0.105 m back on the nacelle (coordinator decision 39); the ray then passes 25 mm in front of the wheel's face, and the entry stays, because a capture exempts every collider of its own part.

`Specs` rows (solid; hollow; arc; decorative; moving roots); no row carries a `Fit`:

- `cannon`: `["Visual/Breech", "Visual/Foot", "Visual/ChargeTrack", "Visual/Indicator", "Visual/PowerIn", "Visual/ActivationIn"]`; `["Visual/Jacket", "Visual/RearCollar", "Visual/MuzzleCollar"]`; none; `["Visual/Sleeve", "Visual/ChargeBar", "Visual/ReadyFlag"]`; none.
- `windmill`: `["Visual/Base", "Visual/Tower", "Visual/Nacelle", "Visual/Output/Wheel", "Visual/Rotor/Hub", "Visual/Rotor/Arm0/Blade"`–`"Arm3/Blade"]`; `["Visual/Guard"]`; none; `["Visual/Rotor/Arm0/Tip"`–`"Arm3/Tip", "Visual/Output/Spoke0", "Visual/Output/Spoke1"]`; `["Visual/Rotor"]`.
- `balloon`: `["Envelope"]`; none; none; `["Neck", "Carrier", "String"]`, the string's comment citing the owner's decision and date; none.

### Matcher changes (test harness)

- **G1, an imported ring.** A private `Annulus(ArrayMesh)` reads the vertices and triangles of the mesh; a mesh with any number of surfaces other than one, or whose surface is not an indexed triangle list, has no shape. About the mesh's local Y axis: outer = the largest radial distance √(x² + z²), inner = the smallest, and the two ends are the least and greatest y. It returns a ring only when all of these hold: inner > 0; outer − inner > 2 · `ImportedStep`; every vertex is within `ImportedSpread` = 2 · `ImportedStep` of the measured inner or outer radius and of one measured end (a vertex and the measured extreme each lie within one `ImportedStep` of the true circle, so they may differ by two; used only to group vertices, never to compare a reading with a literal or a collider); each of the four circles (inner and outer, at each end) holds at least three distinct vertices; no triangle's centroid lies nearer the axis than inner · cos(Δ) − `ImportedStep`, where Δ is the largest angular gap between neighbouring distinct outer-ring vertices, including the gap across the wrap-around. Otherwise the mesh has no shape. In `Primitive`'s shape switch an `ArrayMesh` that is a ring, at uniform scale s (the torus arm's guard), is a `Tube` with extents (outer·s, half-length·s, outer·s), `Inner` = inner·s, its centre at the mid-point of the two ends, mesh error outer·s·(1 − cos(Δ/2)), and `Allowance` = `ImportedStep`. It is matched by the existing Tube rule with part B's bore rule (`MatchesHollow`); the existing mesh-error bound (`:260`) refuses a ring with a gap.
- **`ImportedStep` = 5e-5 m**, a named constant with this reason: an imported mesh may be stored with 16-bit positions across its bounds; the largest bound here is the guard's 1.63 m, 2.5e-5 m a step; two steps. `Solid` gains `float Allowance` (zero for primitive art), added to the tolerance of that mesh's own match only. Boxes, spheres and cylinders in the same rows keep `Tol`.
- **G2, the gas-radius scale.** In `BodyPose`'s per-node loop (`:397–411`), a `Presentation.Bindings` entry for that node with driver `CommittedGasRadius` and property `UniformScale` sets the node's scale to the value the driver has at the authored state in Standard atmosphere, replacing the node's authored scale and keeping its rotation: `IsobaricGas.OuterRadius(shape.DryRadius, store.ReferenceVolume.Evaluate(parameters))`, the parameters keyed by `ParameterId` (`MassFrameValues.cs:12`), clamped to the binding's `From`–`To`, where `shape` is the entry's `GasShapes` entry at the binding's `GasShape` (body, collider) and `store` the gas store its `Store` names (`GasCouplingValues.cs:11`; `CuriousContraptions.Bundle/BundleSchema.cs:46–47`). At the default parameters the scene's authored scale must equal that value within `Tol`, so the authored scene stays checked. No other runtime driver is evaluated. The cube, scale and cube root add about three roundings, under 1e-7 m at 0.5 m, inside `Tol`.

What these remove, and what catches a failure. Nothing is deleted. G1 could admit a wrong shape as a ring; the conditions refuse a zero-wall prism, a capped cylinder, a closed bore and a ring with a gap, and C10 runs a refusing control for each rule of `Annulus` (review passes 1 and 2). G2 could agree with the collider by construction; it does not, because the drawn radius comes from the gas store's declared volume and the collider's from its own radius binding. G2 is a statement about declarations; that the real part draws that radius is C8's production test, not G2.

### Footprint, capture and occlusion

- No declared `ElementFootprint`, pick radius or port moves. Placement extent follows the colliders (`engine/PlacementPose.cs:113`, each collider through `Within`, `:46–62`): the Windmill's is unchanged, because the guard's tube (r 0.815 about x) already reaches ±0.815 m in y and z, beyond the wheel; the Cannon's reach grows from 0.65 m to 0.72 m in +y (the indicator); z is unchanged (track and sockets reach z 0.655 and 0.62, inside the foot's 0.7). No test places a Cannon or Windmill near a bench limit.
- Cannon. The chamber (radius 0.48 m, half-length 0.55 m on the barrel's axis) is unchanged. The new colliders are outside it: the sockets' nearest surface is 0.71 m from the axis, the track 0.76 m and the indicator 0.69 m, against the collars' 0.65 m. Seating, the admission window and firing are untouched (probe).
- Windmill. The wheel is exempt from the Windmill's own capture and occludes other streams. The drive port's rim is declared data; the delivered rigid drive connection, the Mechanical link, is drawn as a schematic and has no collider (owner decision 2, coordinator decision 25). The blades sweep x −0.205 to −0.035; the wheel spans 0.01 to 0.49.
- Balloon. No collider changes.
- Construction refuses only a dynamic body that starts inside a hollow wall (`engine/gpu/WorkshopPhysicsCompiler.cs:498–523`); no hollow collider is added.

### Decisions

Owner decisions (21 November 2026): the charge track, indicator, sockets and output wheel get box, sphere or cylinder colliders; the Balloon's string stays visible and decorative as a named exception.

Coordinator decisions (22 November 2026, under the owner's standing delegation): art turned by a presentation binding is decorative (part A, D5), so the charge bar, ready flag, neck and output spokes stay as they are; a non-solid tether needs a cited owner decision per use; the Balloon's picture before Run is not hidden behind a formula (C8, O1); the order is A, B, B2, C. O1 is settled by owner decision 5 (below).

Decided here, with reason:

- **D1. Proxy style follows each package:** hidden mesh proxies reusing the art's mesh in the Cannon, a `CollisionShape3D` in the Windmill.
- **D2. Imported rings are read from vertices and triangles through the loaded scene.** Bounds alone give no bore, and neither part has tube mouths. Parsing the `.obj` text would test a file the player never sees.
- **D3. The table's gas radius is computed from declared data with the production function;** the real part is proven separately by C8.
- **D4. The track and sockets have no play test.** A 0.34 m ball meets the jacket (upright) or the foot's edge (on its back) first. They are proven by shape (C2), as part A's D1.
- **D5. Collider growth is accepted and recorded:** Cannon 5 to 9, Windmill base 4 to 5.

**Decision O1 (owner decision 5, 22 November 2026).** Each time a Balloon is constructed and each time the world is Reset, the part first draws its envelope at 0.0625 m with the neck at the centre, against a collider of 0.18 to 0.5 m. By reading, the first declared sample then restores the committed radius without Run, so the wrong picture lasts from construction or Reset until that sample; how many frames that is has not been measured. The rule:

- (b) If C8's test confirms the picture is only momentary (the committed radius in build mode and after Reset once the first declared sample has arrived): C8 stands as written, the momentary 0.0625 m picture is the named defect already recorded at [`deferred-work.md#first-committed-sample`](deferred-work.md#first-committed-sample), owned by `ENGINE-ONE-WAY-INSTALL` (sprint key `engine-one-way-install`, order 6 of the engine programme, which owns the first committed sample on construction and Reset), and part C proceeds. C8 settles the native legs only; the `first-committed-sample` entry adds a queued browser case asserting the envelope's radius on the first rendered frame after construction and after Reset, for the post-catalogue E2E phase.
- (a) If it does not: the slice stops at task 1 and is replanned after `ENGINE-ONE-WAY-INSTALL` publishes, because a part that stays wrong in build mode is a visible bug, not the momentary defect owner decision 5 covers.

Under either outcome C8's test asserts the 0.0625 m projection where it occurs, so the defect is on record and its repair must change that assertion.

Floating art is seated in repair part D (`spec-elements-visible-solid-remainder-d.md`; coordinator decision 39, 23 November 2026, consistent with coordinator decision 33 for the Counter): the Cannon's track stands 4.5 cm above its foot and its two sockets 3 cm above the foot and 12.8 cm off the jacket; the Windmill's wheel stands 0.105 m in front of its nacelle; their ports are declared there. This part adds the colliders at the drawn positions and moves no node or port; part D moves each node with its proxy, locator and port after the engine programme. For visibility only: at full charge the Cannon's bar is 4 cm proud of its track, the ready flag stands above the breech and the Balloon's neck is 3 cm proud; all are moved by bindings.

## Tasks & Acceptance

**Execution:**

0. Before any edit -- build at the baseline, keep a baseline copy of `elements/elements.bundle.json` outside the checkout, written by the production generator's own `--out` (no `cp` or shell redirect; Spec Change Log 1), and confirm the build made the import cache `.godot/imported` in this checkout (never copied from another checkout) -- AC2's baseline.
1. `CuriousContraptions.tests/BalloonArtTests.cs` -- add the theory of C8 and run it first. If the build-mode or after-Reset case shows any radius but the collider's once a sample has been applied, or the native harness cannot drive a build-mode sample through the production evaluator, stop and return to planning with the result (O1, outcome a: wait for `ENGINE-ONE-WAY-INSTALL`). The expected radius is the compiled collider's, never `IsobaricGas.OuterRadius` called in the test -- C8.
2. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- add `ImportedStep`, `Solid.Allowance`, `Annulus`, the `ArrayMesh` arm (G1) and the gas-radius scale (G2); bring the class, `ArtSpec`, `Solid` and `Art` summaries and the exception message `:259` (an imported ring) up to date; add the fact `AnImportedRingIsReadFromItsMeshAndOtherMeshesAreRefused` -- C3, C7, C10.
3. `elements/Cannon/cannon.tscn` -- add the four hidden proxies after `col_Foot` -- C1, C2.
4. `elements/Windmill/windmill.tscn` -- add `Shape_Output` and `col_output` after `col_guard`; `elements/Windmill/WindmillElementDeclaration.cs` -- add `new(new(0), new(4))` to the capture's internal occluders -- C5, C6.
5. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- add the three rows; add `Feature` members `CannonIndicator` and `WindmillOutput` with their `Drop` arms to part B's low-restitution theory, and bring the `Feature` and theory summaries (`:582–585`, `:737–743`) up to date; correct XB-04's comments `:87`, `:126`, `:147`, `:179` and the same kind at `:134`, `:142` to state current truth, leaving every row's classification unchanged -- C1, C5, C9, C11.
6. `CuriousContraptions.tests/WindmillAdmissionTests.cs` -- add the fact `TheCaptureExemptsEveryColliderOfItsOwnWindmill` -- C6.
7. The four element documents, `deferred-work.md`, `delivery-batches.md`, `completion-ledger.md`, `TODO.md` -- record each collider with size and pose, the sleeve's confirmation, the string's exception with owner decision 1, the counts of D5, the floating sockets, track and wheel as owned by repair part D (coordinator decision 39; no deferred-work entry), O1's outcome (C8's result added to the existing `first-committed-sample` entry, owner `ENGINE-ONE-WAY-INSTALL`; no second entry), XB-04's four comments corrected (BallDetector and PoweredGate stay open), and the remainder story as complete; refresh the element documents' stale citations and rewrite the lines delivery makes false (run log E2, E4); amend the `deferred-work.md:381` carry-forward for this slice's own `VisibleSolidColliderTests.cs` shift.

**Acceptance Criteria:**

- AC1: Given the three packages, when the visible-solid theory runs, then each row passes and the theory has three more cases than at the baseline.
- AC2: Given the baseline copy and the candidate's bundle, when each entry is compared by decoded value, then every entry but `cannon` and `windmill` is equal; `cannon` has 9 colliders, the first 5 unchanged; `windmill` has 5 base colliders, the first 4 unchanged, and 10 internal occluders; every other field of both entries is equal, including the rotor's five `Mechanical.MovingBodyColliders[0]`.
- AC3: Given the named existing tests (verification list), when they run, then all pass with no edit.
- AC4: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline total + 10 (three theory cases, two drop cases, three envelope cases, two facts).
- AC5: Given the candidate diff, when its paths are listed, then none is under `engine/`, `CuriousContraptions.Simulation/`, `CuriousContraptions.Bundle/`, `CuriousContraptions.Abstractions/`, `schemas/`, `tools/` or `elements/Balloon/`.

Counts in AC1 and AC4 are baseline + delta, counted on the slice's baseline at start, so whether the Baseball repair publishes before or after this part does not change them.

## Spec Change Log

1. Task 0 (coordinator ruling, run log E7): the 733 KB baseline bundle was written by the production generator's `--out` into the slice's log folder, byte-equal to the baseline build's (`cmp`); a Write-tool copy of that size was not feasible and no shell copy or redirect was used. Accepted; task 0 amended.
2. G1 (`bad_spec`, design answer; run log E6 departure 1, E7): grouping vertices against the *measured* extremes needs `ImportedSpread` = 2 · `ImportedStep` (triangle inequality: both lie within one step of the true circle); the Windmill guard's farthest vertex lay 5.74e-5 m from its measured extreme, each extreme within 2.7e-5 m of its literal. Literal checks, the row allowance and the wall and centroid checks keep `ImportedStep`. Bounded planning re-check: audit 5, addendum.
3. C5 (within the row's "such as"): the drop point is world X 0.40, Z 0.25 (part-local (0.40, −0.25) under `Back`) on the wheel's face, 13 mm above the spokes and 0.485 m from the guard; the suggested X 0.37, Z 0.22 left 1.6 mm over a spoke (run log E6).

## Review Triage Log

Full rows of passes 1 to 3: run log E16 (contract budget).

### 2026-11-26 — Review pass 1 (engine tier, all six layers; run log E10)
- verdicts: 36 findings — high 0, medium 3, low 19, false 14, maybe-false 0
- findings:
  - `low` `reject` BH1: spokes proud, no collider — binding-turned art is decorative (intent, part A D5).
  - `low` `reject` BH2: charge bar and ready flag — the Never clause forbids their colliders.
  - `low` `reject` BH3: Balloon neck below the envelope — binding-moved; no Balloon scene change (Never).
  - `false` `reject` BH4: C8 pins the 0.0625 m projection — required by owner decision 5.
  - `low` `patch` BH5: C8 doc overclaimed the world path — reworded (E11; superseded by BH2-1).
  - `medium` `patch` BH6: ring-reader rules lacked refusing controls — controls added (E11, E13).
  - `medium` `defer` BH7: Battery marks proud — owner the Battery redraw (`batch-b.md:77`); in `deferred:`.
  - `low` `reject` ECH1: wheel collider does not turn — the intent's static base-body proxy (part A D4).
  - `low` `reject` ECH2: no non-Standard atmosphere case — C8 is specified in Standard atmosphere.
  - `low` `patch` ECH3: `Annulus` ignored the primitive type — non-triangle surfaces refused (E11).
  - `low` `reject` VG-O1: duplicate of BH2.
  - `low` `patch` IA1: C8's surface — grouped with BH5.
  - `false` `reject` IA2: duplicate of BH4.
  - `false` `reject` IA3: C4 has no new bore test — existing Cannon tests ran unedited; audit 5 clearances.
  - `false` `reject` IA4: C6 by count only — set equality by (body, collider).
  - `false` `reject` IA5: half ring not driven through the table — the `:260` bound refuses its error.
  - `low` `defer` IA6: decorative containment unasserted — owner `ENGINE-ART-SOLIDITY`; in `deferred:`.
  - `false` `reject` IA7: material per proxy — material is per scene body.
  - `false` `reject` IA8: 45-package coverage — the intent counts the unpublished Baseball repair.
  - `false` `reject` IA9: two proxy forms — decision D1.
  - `false` `reject` IA10: index stability — AC2 and C6 check it.
  - `low` `patch` FO1: untested unindexed branch — deleted, refusal controlled (E11).
  - `false` `reject` FO2: `Solid.Allowance` — permitted.
  - `false` `reject` FO3: the `ArrayMesh` arm — permitted.
  - `false` `reject` FO4: G2's oracle — permitted.
  - `false` `reject` FO5: the initial-value projection — permitted.
  - `false` `reject` FO6: two proxy node kinds — permitted.
  - `low` `patch` EFE-F1: `deferred-work.md` citations moved — corrected (E10).
  - `low` `patch` EFE-F2: moved test-line citations — refreshed (E11, E13).
  - `low` `patch` EFE-F3: ledger status cell — set to the commit's `done` at Finalize.
  - `low` `patch` EFE-F4: contract contradictions — corrected (E10).
  - `low` `patch` EFE-F5: EL-208 conditional on C8 — outcome (b) recorded (E11).
  - `low` `patch` EFE-F6: counts against indices — disambiguated (E11).
  - `low` `patch` EFE-F7: stale readiness rows — updated (E10, E11).
  - `low` `patch` EFE-F8: AC2 result only in `.tools/` — inline in E11.
  - `medium` `patch` EFE-F9: Anvil evidence for E6 writes — re-validated (E11, E13, E14); ruling E12.

### 2026-11-26 — Review pass 2 (re-review of repair 1; run log E12)
- verdicts: 12 findings — high 0, medium 5, low 4, false 3, maybe-false 0
- findings:
  - `medium` `patch` BH2-1: C8 bypassed the production client — routed through `TryDeclaredFrame` natively (E13).
  - `medium` `patch` BH2-2: three-vertices rule uncontrolled — grouped with EFE-P2-1.
  - `medium` `patch` VG2-1: four-circles and surface-count rules uncontrolled — grouped with EFE-P2-1.
  - `false` `reject` FO2-1: `Built`'s optional indices — permitted.
  - `false` `reject` FO2-2: `RingArrays`'s open-arc vertex — permitted.
  - `false` `reject` FO2-3: the append-only correction in part B2's run log — permitted.
  - `medium` `patch` EFE-P2-1: four refusing controls and structural preconditions added (E13).
  - `medium` `patch` EFE-P2-2: exact per-file re-validation (E13, E14); breach ruling (E12).
  - `low` `patch` EFE-R1: stale contract cells and lines — corrected (E12, E13).
  - `low` `patch` EFE-R2: CAT-003 and CAT-016 residual wording — corrected (E13).
  - `low` `patch` EFE-R3: batch-d tally and batch-c row — corrected (E13, E14).
  - `low` `defer` EFE-R4: lane 2's S5c contract citation — carried forward, reported; in `deferred:`.

### 2026-11-26 — Review pass 3 (re-review of repair 2; Re-review scope; run log E14)
- verdicts: 12 findings — high 0, medium 0, low 9, false 3, maybe-false 0
- findings (each inside the repair diff or what it directly affects):
  - `low` `patch` BH3-1: C8 also replaces the worker's encoding (applied-at 0) — narrowed (E15).
  - `low` `patch` BH3-2: "no other reason" overclaimed — narrowed, labels fixed (E15).
  - `low` `patch` VG3-1: capped cylinder not isolated — grouped with BH3-2.
  - `false` `reject` FO3-1: the worker stand-in — permitted.
  - `false` `reject` FO3-2: the kept 0.0625 m assertions — permitted.
  - `false` `reject` FO3-3: test builders and preconditions — permitted.
  - `low` `patch` EFE-P3-1: same as BH3-1; E15 corrects E13.
  - `low` `patch` EFE-P3-2: `batch-d.md:27`, `:17` — corrected (E14).
  - `low` `patch` EFE-P3-3: `windmill.tscn` re-validation — auditor's exact call, allow (E14).
  - `low` `patch` EFE-P3-4: EFE-R4 into `deferred:` (E14).
  - `low` `patch` EFE-P3-5: stray file `0` — removed (E14).
  - `low` `patch` EFE-P3-6: missing coordinator entry — E14.

### 2026-11-26 — Review pass 4 (re-review of repair 3: Blind Hunter, Verification Gap, both auditors; Re-review scope; run log E18)
- verdicts: 3 findings — high 0, medium 0, low 2, false 1, maybe-false 0
- findings (each inside the repair diff or what it directly affects; Blind Hunter and Verification Gap none):
  - `false` `reject` FO4-1: the "empty index list also refuses" wording — two guards of one refusal rule (auditor: permitted).
  - `low` `patch` EFE-P4-1: C8 and C10 Run cells cited `repair-r2.log` and an old label — now `repair-r3.log` (E17).
  - `low` `patch` EFE-P4-2: `batch-d.md:27` "in this repair" had no referent — names part C's contract (E18).

## Design Notes

1. **Behaviour delivered.** Five drawn solids become solid; three packages join the visible-solid proof, which then covers the catalogue; the Balloon's drawn radius is proven on the real part. Source: owner rule "visible = solid" (25 October 2026); decisions of 21 and 22 November 2026.
2. **Reusable.** Nothing new in the product. The matcher gains two package-neutral rules.
3. **Declared.** Proxy nodes in two scenes; one collider reference in the Windmill's declaration. No identity appears in engine code.
4. **Execution.** Scene → `SceneGeometryExtractor.Bake` → bundle entry → the unchanged compiler and solver.
5. **Ownership.** Static colliders on existing bodies; no new state. Reset, save and load are unaffected: colliders compile from the bundle.
6. **Generality.** G1 reads any imported ring and refuses any other mesh; G2 reads any gas-radius binding through its declared store. Neither names a package.
7. **Deleted.** Nothing. No raised art, alias or second path is introduced.
8. **Uncertain.** How imported positions are stored (`ImportedStep`); how long the first-sample interval of O1 lasts. Rules E1, E6, E7 and E8 apply; E9 is run as always.

## Verification

Executor: the implementation owner for handoff and repair checks; the coordinator, as designated executor, for the final candidate's capture, maintainability gate and full round after review. Raw logs go under `~/.local/share/tim-toolchain/logs/slice-visible-solid-c/` (outside the checkout); `.tools/visible-solid-remainder-c-evidence/` holds transient review inputs only, and the record is the run log.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log.

### Acceptance-to-test table

Both parts' static restitution is 0, so the drops use part B's stop-only theory: the height of the first landed read lies in [rest − 17 mm, rest + 5 mm]. Each drop was checked by hand and by the auditor; the implementer recomputes it and returns to planning if a clearance is under 5 mm. `Back` puts a part-local point (x, y, z) at (x, z, −y). No bound is fitted to output: a row that disagrees by more than `Tol`, or more than `Tol + ImportedStep` for an imported mesh, is a finding for planning, not a wider fit.

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| C1 | Low-restitution theory, `Feature.CannonIndicator` = (`cannon`, identity, top 0.65 + 0.07, X −0.66, Z 0.4, restitution 0) | Without the collider the ball stops on the breech's top, 0.14 m lower; the rear collar is 0.495 m from the resting centre. Boundary: a sphere apex, first stop only | passed, `scoped-c1.log` (run log E6) |
| C5 | Same theory, `Feature.WindmillOutput` = (`windmill`, `Back`, 0.44 + 0.06, a point on the wheel's face off both spokes such as X 0.37, Z 0.22, restitution 0; the implementer recomputes the guard and spoke clearances and returns to planning if one is under 5 mm) | Without it the ball stops on the nacelle's face, 0.225 m lower. Boundary: a flat face | passed, `scoped-c1.log`, at local (0.40, −0.25) (world X 0.40, Z 0.25): 13 mm above each spoke, 0.485 m from the guard (run log E6) |
| C2, C3, C7, C11, AC1 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, cases `cannon` and `windmill` | Each row fails with "solid art without a matching collider" if a proxy is absent, and for a ring whose bore, radius or length differs from its tube by more than `Tol + ImportedStep` | passed, cases `cannon` and `windmill`, `scoped-c1.log` (run log E6) |
| C9, C11 | The same theory, case `balloon`: default radius, then 0.18 m and 0.5 m | Without G2 the row fails at both bounds (art 0.36 m). The string and carrier are classified and unmatched; a collider added for either fails as a collider beyond the art | passed, case `balloon` (default, 0.18 and 0.5 m), `scoped-c1.log` (run log E6) |
| C8 | `BalloonArtTests.TheEnvelopeIsDrawnAtTheColliderRadiusInBuildModeAtRunStartAndAfterReset` (new theory: radius 0.18, 0.36, 0.5). A part made by `PartRegistry`, with `ResetPhysicalVisuals`; the production presentation's samples are published to the production client (`WorkshopActivationAnimationTests.NativeClient`) and reach the part as `MachineWorld` routes them, `BrowserWorkshopClient.TryDeclaredFrame` for its owner then `ApplyDeclaredProperty` per property (`engine/MachineWorld.Gpu.cs:250–252`, `:317`): `Envelope.Scale.X` is within 0.1 mm of the compiled collider's radius after the first sample in build mode, after the first sample of Run, and after the first sample following Reset; sample values come from `Presentation.Advance(schedule, …)` at the installed schedule's anchor (`engine/gpu/WorkshopDeclaredPresentation.cs:637`), and a build-mode session whose client admits no sample is outcome (a) | Control: `Carrier` stays at scale 1. Boundary and record of O1: directly after `ResetPhysicalVisuals`, before any sample, the scale is the projection 0.0625. 0.1 mm is the preparation bound of `BalloonTests.cs:158–159` for the same radius | passed, three cases, `task1-c8-gate.log` (the task 1 gate, outcome (b)), `scoped-c1.log` (run log E6), `repair-r1.log` (run log E11), `repair-r2.log` (routed through the production client, run log E13) and, on the current source, `repair-r3.log` (run log E15) |
| C10 | `VisibleSolidColliderTests.AnImportedRingIsReadFromItsMeshAndOtherMeshesAreRefused` (new fact): the jacket, collar, sleeve and guard read as (0.48, 0.58, 0.55), (0.48, 0.65, 0.05), (0.5, 0.565, 0.07) and (0.745, 0.815, 0.035) within `ImportedStep`, the literals of their source files; the sleeve's ring at both ends of its binding (x −0.22 and −0.38) lies inside the jacket's ring | Controls, built in the test as `ArrayMesh`: a box centred on Y has no shape (no wall); a capped cylinder with a centre vertex on each cap has no shape (inner 0); a half ring reads with a mesh error over 5 mm, which the theory's bound refuses | passed, `scoped-c1.log`; vertices grouped against a measured extreme within `ImportedSpread` = 2 · `ImportedStep` (run log E6, departure 1). After review pass 1, `repair-r1.log`: also refuses a ring whose bore an end fan closes (centroid rule), a ring with a vertex mid-wall (on-circle rule), and the same ring's arrays as lines (triangle rule) or unindexed (index check) (run log E11). After review pass 2, `repair-r2.log`: also refuses a second surface (single-surface rule), a vertex on the outer radius at mid-height (on-end rule), two outer vertices per circle (three-vertices rule) and a conical two-circle shell (four-circles rule); every built control's surface count, primitive and indexing asserted (run log E13). On the current source, with the capped cylinder labelled "bore and inner-circle rules" and the unindexed ring "index check": `repair-r3.log` (run log E15) |
| C6 | `WindmillAdmissionTests.TheCaptureExemptsEveryColliderOfItsOwnWindmill` (new fact): in the compiled scene of a Fan and a Windmill, the capture's internal occluders are exactly the colliders of the Windmill's two bodies, taken from the compiled colliders by body, ten in all | Behavioural control, existing and unchanged: the `Exposure.Rear` and `EqualOpposition` cases of `WindmillIntegrationTests.ActualSourcePoseAndSignedExposureDetermineDrive`, which lose a sample to the wheel without the new entry. `RotaryAirflowRuntimeTests.FourActualPoseSamplesLoseOnlyOccludedWeight`: a solid that is not exempt removes its sample's weight | passed, `scoped-c1.log` (run log E6) |
| C4, C12 | Existing, unchanged. Cannon: `CannonIntegrationTests` (13, including `TwoPhysicalOccupantsPresentUnavailableAndResetToEmpty`, `AuthoredMountAimsAlongItsBarrelAndTenMillimetreMuzzleWallRefuses`, `GravityReturnsTheSameBallForAnotherPaidShot`, `PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply`), `CannonArtTests` (`BaseballTests.BaseballNeverSeatsInTheCannon` does not exist at the baseline: the Baseball repair has not published). Windmill: `WindmillIntegrationTests` (11), `WindmillAdmissionTests`, `WindmillArtworkTests`, `RotaryAirflowRuntimeTests`, `BodyOwnershipTests.ConsistentlyReauthoredChildOwnershipIsAValidReplacement`, `CoordinateTravelGeneralityTests`. Balloon: `BalloonTests` (10), `BalloonArtTests`, `GasRuntimeTests`, `GasCapabilityTests`. All: `F32BehaviourIdentityTests`, `PackageRuntimeParityTests`, `BundleRoundTripTests`, `WorkbenchCapacityTests.EveryFreePaletteKindCompilesAndStepsPastTheRetiredPartTable` | Existing | passed unedited, `scoped-c1.log` (526 passed, 0 failed) (run log E6) |
| AC2 | Read-only `jq` comparison, by decoded entry, of the baseline bundle (`bundle-baseline.json`, the production generator's `--out` at the baseline, Spec Change Log 1) with the candidate's `elements/elements.bundle.json` | Control: every entry but `cannon` and `windmill` equal, ids in the same order; boundary: the first 5 Cannon and first 4 Windmill base colliders equal, the occluder list equal to the baseline's plus only (body 0, collider 4), rotor `MovingBodyColliders` equal | holds; command and decoded result in run log E11 (E6; `.tools/` copy is transient review input) |
| AC3 | The C4, C12 row's named existing tests, in the contract's scoped filter | Control: `git diff d37c0b98` adds members to `BalloonArtTests.cs`, `WindmillAdmissionTests.cs` and `VisibleSolidColliderTests.cs` and edits no other test file; the named tests in `BalloonArtTests.cs` and `WindmillAdmissionTests.cs` are unchanged, and the G1 and G2 matcher changes in `VisibleSolidColliderTests.cs` are outside the named list | passed unedited, `scoped-c1.log` (526 passed, 0 failed) (run log E6) |
| AC4 | The full local round under `caffeinate -i` with the deferred-browser filter and `CSHARP_STATIC_GATE_BINLOGS=binlogs-c1` | Boundary: total = baseline 7431 + 10 (three theory cases, two drop cases, three envelope cases, two facts); `Skipped: 0` in every project | 7441 passed, 0 failed, `Skipped: 0`, `full-round-c1.log` (run log E6, E7) |
| AC5 | `git diff --stat d37c0b98` path list against the forbidden roots | Control: 22 paths (`git diff --stat d37c0b98` after review pass 2; run log E13), none under `engine/`, `CuriousContraptions.Simulation/`, `CuriousContraptions.Bundle/`, `CuriousContraptions.Abstractions/`, `schemas/`, `tools/` or `elements/Balloon/`; `elements/Baseball/` untouched | holds (run log E13) |

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors; the bundle regenerates.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: 0 failed, 0 skipped.
- The AC2 comparison of `bundle-baseline.json` with the candidate's bundle, by decoded entry -- expected: as AC2 states.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~Cannon|FullyQualifiedName~Windmill|FullyQualifiedName~Balloon|FullyQualifiedName~GasRuntimeTests|FullyQualifiedName~GasCapabilityTests|FullyQualifiedName~RotaryAirflowRuntimeTests|FullyQualifiedName~BodyOwnershipTests|FullyQualifiedName~CoordinateTravelGeneralityTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~BundleRoundTripTests|FullyQualifiedName~WorkbenchCapacityTests"` -- expected: 0 failed, 0 skipped; every test in the table appears.
- `git add -N` this contract and its run log if untracked, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` -- expected: baseline total + 10, 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass. The hook's two test classes load no scene; the imported-mesh read is proven only by the scoped run and the full round, in a working tree whose `.godot/imported` cache the build has made. A missing cache fails loudly: a null mesh classified hollow throws, and C10 asserts the four literals.

**Manual checks (if no CLI):**

- Open the two scenes: the new proxies follow the base body's existing proxies in file order, and the Cannon's four are hidden.

### At replanning

Done at dispatch on `d37c0b98` (run log E2, audit 5): `Specs` 41 rows, part B's names carried, every line re-read. `spec-engine-art-solidity.md`'s "read by their bounds" wording is corrected at that contract's own replanning.

### Running beside other work

Serialised with the Baseball repair on `VisibleSolidColliderTests.cs` (one publisher at a time; the later merges main once and repeats the full round); shares records with the other pre-programme lanes; whichever of S6 and this part publishes second re-reads the probe's `WorkshopDeclaredPresentation.cs` citations (full text: run log E19).

## Auto Run Result

- **Summary.** Five drawn solids get colliders (Cannon track, indicator, two sockets; Windmill wheel, exempt from its own capture); the matcher reads imported rings and gas-sized envelopes; three rows join the table; C8 confirms owner decision 5's outcome (b) natively. Run log E6–E18.
- **Files.** 22 paths, all owned (Final diff scope Pass, E19).
- **Review.** Engine tier, four passes (36, 12, 12, 3 rows); Forward-Only clean every pass; three items deferred to existing owners (`deferred:`). `followup_review_recommended` false: pass 4 patched no `high`.
- **Verification.** Final candidate (E18): capture 0 warnings; maintainability PASS; full round 7441/0/0, `Skipped: 0`; registration 829 (E6).
- **Residual risks.** Worker leg by reading; browser qualification pending; remote build unverified.
