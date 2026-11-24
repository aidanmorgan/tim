---
title: 'ELEMENTS-VISIBLE-SOLID-REMAINDER, part A: Rope anchor, Pulley, Fan, Impact lever and Trampoline'
type: 'bugfix'
created: '2026-11-21'
status: 'done'
baseline_revision: '0959e8834bacd5dd82226aa737c78ed3afb796e3'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The owner's rule is that what a player sees as solid is solid. At `25df3cd5` twelve delivered packages are outside the visible-solid table (the Baseball repair adds `baseball` separately); in five of them, built from primitive art, a ball can pass through drawn parts: the Rope anchor's eye, the Pulley's post and rim, and the Fan's post and shroud ring. The Impact lever's beam carries raised markings that read as solid, and the Fan draws a free-standing arrow that is not a solid.

**Approach:** Give each of those drawn solids a collider of exactly its drawn shape, on the body that already owns its neighbours; redraw the Impact lever's markings and the Fan's arrow as flat printing; and add the five packages to the existing visible-solid table (33 of the 45 delivered packages at `25df3cd5`). The other packages follow in parts B, B2 and C and the Baseball repair.

## Boundaries & Constraints

**Always:** A new collider is authored as a scene proxy of the same primitive, size and pose as the mesh it backs, appended after the body's existing proxies so no existing collider index moves, with the body's existing material. Colliders stay declaration and scene data: no engine, compiler, solver or wire change. The Fan's own stream is not occluded by the Fan's own new colliders. The intended behaviour of the five parts is preserved: rope ties and routing, the Fan's airflow, supply modes and blades, the lever's motion, the Trampoline's bed. One consequence is intended: with a solid ring, a ball reaches the Fan's hub only through the ring's opening. The existing visible-solid test passes for each package through its new table row. Reset, save and load restore each part as before. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A collider for the Fan's blades (owner decision of 21 November 2026: decorative, turned by a presentation binding; coordinator decision 25 corrects EL-176's statement calling them a defect). A collider for the rope connection: the rope is solid (owner decision 1, 22 November 2026), and its missing collider is the recorded visible-solid defect owned by `ENGINE-CONNECTION-MATERIALS` (CAT-058 section 6, item 3), out of scope here. A collider larger, smaller or elsewhere than the drawn mesh. A raised marking kept beside its printed replacement. A change to the matcher's rules or tolerances. Any package of parts B, B2 and C or of the Baseball repair. A new parameter, port or footprint. Hashes, exact arrival ticks, mutants, browser runs, skipped tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| A1 Rope anchor eye | A Basketball dropped onto the eye's tube | It stops at the drawn tube | A ball small enough for the eye's opening passes through it and stops on the mount |
| A2 Pulley post | A Basketball dropped onto the post's back face with the Pulley face down | It stops at the drawn face and rebounds at the product restitution | No error expected |
| A3 Pulley rim | A Basketball dropped onto the rim's tube with the Pulley on its back | It stops at the drawn tube | No error expected |
| A4 Fan shroud ring | A Basketball dropped onto the top of the ring | It stops at the drawn tube | No error expected |
| A5 Fan's own stream | A powered Fan and a small ball placed where the ring, or the post, lies between the source plane and the ball | The ball is pushed and work is paid: the Fan's base, post, ring and hub are the stream's internal occluders | The same ball behind a solid that is not the Fan's own gets no push and no work |
| A6 Fan post | The compiled Fan | One box collider of the post's drawn size at its drawn pose (a 0.34 m ball cannot reach it: the base, hub and ring enclose it) | No error expected |
| A7a lever markings | The Impact lever's inset and four marks | Each is a flat printed surface just above the beam's top face, with the same position, X and Z extent and colour; the marks lie above the inset; no raised volume remains | A marking more than 0.5 mm off the face fails |
| A7b Fan flow arrow | The Fan's free-standing arrow and tip | Gone. A flat printed arrow (a shaft and two head strokes) on the base's top face points along local +X in the old arrow's colour. Its position and extent are new: it lies on the base, not in the air in front of the ring | A headless or reversed print fails |
| A8 table coverage | The visible-solid test for each of the five packages | Every solid or hollow mesh has one matching collider and no collider is without art | Existing failure messages |
| A9 retained behaviour | The named existing tests of each part | Pass. Two Fan tests named in the Code Map are re-authored for the solid ring and the occluder count, with the same assertions; one Balloon test has its final assertion re-authored for the solid eye (Spec Change Log, 24 November 2026) | Existing |

</intent-contract>

## Code Map

Delivery shape: one product repair slice over five packages of one kind of repair (precedent: `spec-elements-visible-solid-sweep.md`). It is the first of four parts (A, B, B2, C); `ENGINE-ART-SOLIDITY` and the conformance suite (S4) wait for all four and for the engine programme. Review tier: **engine**: every capability is present and the scene and table edits are data, but the proof adds members and arms to the shared `Feature` theory, and `TODO.md` is off the data-only list. Source: the owner's decisions of 21 November 2026, owner decision 1 of 22 November 2026, and the story's rows in `docs/planning/invest/delivery-batches.md` section 5, Repairs.

**When.** Before the engine programme: repair parts A, B, B2 and C and the Baseball repair are among the slices owner decision 22 (23 November 2026) allows before it. Part A runs first; its one prerequisite is the publication of the planning work (Baseline).

**Baseline.** Captured at dispatch (coordinator decision 38, 23 November 2026): `baseline_revision` is the published main that contains the planning work, and every scope guard and `git diff` in this contract compares against it. Drafted and line-checked on `25df3cd5`, which is the drafting revision only, not a baseline. Precondition (coordinator decision 37, 23 November 2026): the planning work of 22–23 November 2026 (the `docs/planning/elements/readiness/` folder, the Repairs table of `delivery-batches.md` section 5 and the element-spec revisions cited here) is published before this slice starts, and the baseline is the main that contains it; the coordinator captures it there, before first implementation, and re-reads every planning `path:line` cited here on it. Line numbers were first measured on `8d1d68aa`; every file cited with a line number is unchanged between that revision and `25df3cd5` (re-checked 23 November 2026). Expected totals are baseline + delta against the current full local round.

Dispatch record (24 November 2026): checkout `.claude/worktrees/slice-visible-solid-a` (branch `slice-visible-solid-a`), baseline `0959e8834bacd5dd82226aa737c78ed3afb796e3`, policy revision the same; implementation owner agent `a6d62335ede1659a8` (fresh general-purpose subagent, no inherited history, inherited model); review tier engine, all six layers on the first pass (run-log E1, E2). The 24 November amendment (Spec Change Log) passed a planning re-audit (Scoped Pass).

Planning verdict: Scoped Pass (fourth audit, `docs/planning/elements/readiness/audits/audit-visible-solid-A-4.md`); fixes applied 23 November 2026. The fourth audit found the third audit's F1 and R1–R6 resolved and raised N1–N3 (frontmatter baseline, Fan puzzle count, the Baseball repair's Fan test), all applied here; they need no re-audit. Planning only; no test had run when it was given.

Why four parts. Some packages cannot enter the table without a change to the matcher itself:

- **Part A (this contract):** `rope_anchor`, `pulley`, `fan`, `impact_lever`, `trampoline`. Primitive art, hidden proxies, static bodies; no matcher change.
- **Part B** (`spec-elements-visible-solid-remainder-b.md`): `linear_pusher`, `wound_spring`, and one package already in the table, `conveyor`. Pulley-wheel, socket and indicator colliders, the pusher's head ring, flush redraw of the raised marks; the eight solids drawn by their own collider proxies are separated into art and hidden proxies (the matcher skips every `col_` mesh, `VisibleSolidColliderTests.cs:249`, and keeps doing so), and the matcher gains hollow art on a moving body, a drawn bore and spanned art.
- **Part B2** (`spec-elements-visible-solid-remainder-b2.md`): `tennis`. The seam is printed flush on the felt with no collider (owner decision 24i, 23 November 2026); no matcher change.
- **Part C** (`spec-elements-visible-solid-remainder-c.md`): `cannon`, `windmill`, `balloon`. Four Cannon colliders and the Windmill's output wheel; the matcher reads an imported ring from its vertices (`VisibleSolidColliderTests.cs:241` gives the Cannon jacket, collars and sleeve and the Windmill guard no shape) and sizes the Balloon envelope from its declared gas. The Balloon string stays decorative as the thin-string exception (owner decision 1).
- **Baseball repair** (`spec-repair-baseball-el-239.md`): `baseball`, delivered at `fa86969f`, enters the table with its band.

Owned scope:

- `elements/RopeAnchor/rope_anchor.tscn` -- mesh `Visual_Eye` at 28–30, proxy `col_mount` at 32–34.
- `elements/Pulley/pulley.tscn` -- proxies `col_mount` 58–60 and `col_wheel` 62–64; meshes `Visual_Post` 75–77 and `Visual_Rim` 85–87.
- `elements/Fan/fan.tscn` -- material `Mat_Arrow` 18, proxy `col_base` 65, `Base` 80, `Post` 84–86, `Ring` 88–90, `Arrow` 123–126, `Tip` 128–131.
- `elements/Fan/FanElementDeclaration.cs` -- the airflow source's internal occluders at line 52.
- `elements/ImpactLever/impact_lever.tscn` -- `Visual_Inset` 108 and `Visual_Mark_0`–`3` from 112.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- `Specs` 63–161 (five new rows); `Feature` 444–449, `Drop` 456–512, the theory's inline data and apex list 524–558; two new tests.
- `CuriousContraptions.tests/FanIntegrationTests.cs` -- `PassiveHubReceivesExternalContactWithoutEmittingUnpaidFlow` 412–434 (re-authored); the occluder count at 465 (2 becomes 4); one new theory; one new fact in `FanArtworkTests` (`FanIntegrationTests.cs:610`).
- `CuriousContraptions.tests/BalloonTests.cs` -- the final assertion of `AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (lines 118–137; the assertion is line 136) only (re-authored; Spec Change Log).
- Controlled closure of stale documentation (review pass 2, R2 and R6): every `docs/planning/elements/**/*.md` outside `readiness/audits/` that cites a line of `FanIntegrationTests.cs`, `BalloonTests.cs` or `VisibleSolidColliderTests.cs` that this slice moved, or cites an `AirflowRuntimeTests` method under the file name `AirflowGeneralityTests`. Each is a citation fix only; the files are listed in run-log E9.
- `CuriousContraptions.tests/TennisBallTests.cs` -- the `Ball` helper gains an optional last parameter `z` (default 0; every existing caller is unchanged), so the part A tests build every Tennis ball through one helper (review pass 1, P1 and P6).
- `docs/planning/elements/CAT-058-rope_anchor.md`, `CAT-053-pulley.md`, `CAT-028-fan.md`, `CAT-034-impact_lever.md`, `CAT-065-trampoline.md` -- collider and art sections.
- `_bmad-output/implementation-artifacts/deferred-work.md` (the per-instance collider-growth entry whose source is `spec-elements-visible-solid-sweep.md`; a new entry `pulley-lesson-weight-start`, Running beside other work), `docs/planning/invest/delivery-batches.md` section 5 (Repairs, part A's row), `TODO.md`, this contract and its run log. No new source file and no test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`).

`elements/elements.bundle.json` is build output and is not committed (`.gitignore:17`); task 1 captures a baseline copy.

### Capability probe

Seven present; none untested, narrower or missing for part A.

- **Present.** A box, sphere or cylinder collider baked from a `CollisionShape3D` proxy: `SceneGeometryExtractor.BakeProxy` (`tools/BundleElements/SceneGeometryExtractor.cs:104`). Test: `SceneGeometryExtractorTests`; in play, `VisibleSolidColliderTests.ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution`.
- **Present.** A full torus collider baked from a hidden `TorusMesh` proxy: `SceneGeometryExtractor.Hollow` (`:191–230`, torus branch `:210–219`); precedents `col_ring2` in `elements/Bumper/bumper.tscn:79–82` and `col_visual_eye` in `elements/Weight/weight.tscn:91`. Test: `VisibleSolidColliderTests.ABallDroppedOntoTheBumpersUpperRingStopsAtItsDrawnTube`.
- **Present.** Several colliders on one static body: the State latch bakes eight (`elements/StateLatch/latch.tscn:209–251`). Test: the `LatchSocket` and `LatchIndicator` cases of the drop theory.
- **Present.** Several colliders, including a torus, on a dynamic body (needed by part B, cited for completeness). Test: `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials`.
- **Present.** Exempting a source's own colliders from airflow occlusion: `AirflowSourceDescriptor.InternalOccluders` (`CuriousContraptions.Abstractions/AirflowValues.cs:45–48`), admitted at `engine/gpu/AirflowSceneValidation.cs:38–41`, applied in the sweep at `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141–162`. Tests: `AirflowRuntimeTests.OnlyExactInternalCollidersAreExemptFromOcclusion` (class `AirflowRuntimeTests`, a partial class whose file is `AirflowGeneralityTests.cs:216`), `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer`.
- **Present.** Matching drawn solids, tori and hinge-body art to colliders: `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (184–238), with rows that already use each feature part A needs.
- **Present.** Flat printing as `QuadMesh` surfaces on a face, and a check that ink covers a point: `Mesh_Mark` in `elements/BothGate/both_gate.tscn:35–37` (0.25 mm stand-off in the `FirstMark` transform, lines 100–101); `FaceMarkingTests.Covers` (`CuriousContraptions.tests/FaceMarkingTests.cs:84–104`). Test: `FaceMarkingTests.EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace`.

### Repairs, mesh by mesh

Dimensions are the mesh's own, read from each scene; poses are part-local.

| Package | Mesh | Repair | New node |
|---|---|---|---|
| `rope_anchor` | `Visual_Eye`: torus, inner 0.1725, outer 0.2275 (centreline 0.2, tube 0.0275), centre (0, 0, 0.18), axis Z | Torus collider | Hidden `MeshInstance3D` `col_visual_eye` under `.`, the same mesh resource and transform, after `col_mount` |
| `pulley` | `Visual/Visual_Post`: box 0.12 × 0.55 × 0.12 at (0, 0.3, −0.13) | Box collider | `CollisionShape3D` `col_post` under `.`, `BoxShape3D` of that size at that position, after `col_wheel` |
| `pulley` | `Visual/Wheel/Visual_Rim`: torus, inner 0.365, outer 0.435 (centreline 0.4, tube 0.035), centre (0, 0, 0.1), axis Z | Torus collider (D4) | Hidden `MeshInstance3D` `col_visual_rim` under `.`, the same mesh resource and transform, after `col_post` |
| `fan` | `Visual/Post`: box 0.13 × 0.5 × 0.15 at (0, −0.3, 0) | Box collider | `CollisionShape3D` `col_post` under `.`, after `col_base` (base body, collider 1) |
| `fan` | `Visual/Ring`: torus, inner 0.49, outer 0.63 (centreline 0.56, tube 0.07), centre at the origin, axis X | Torus collider | Hidden `MeshInstance3D` `col_visual_ring` under `.`, the same mesh resource and rotation, after `col_post` (base body, collider 2) |
| `fan` | `Visual/Arrow` and `Visual/Tip`, 0.61 m long in the air at x 0.6 to 1.21 | Deleted with their two meshes; redrawn as the printed arrow below | `Visual/FlowMark` and three children |
| `fan` | three blades under `Visual/BladeAxis/Blades` | None: decorative, turned by a binding (owner decision) | none |
| `impact_lever` | `Visual/Beam/Visual_Inset` (3.38 × 0.014 × 0.82) and `Visual_Mark_0`–`3` (0.035 × 0.016 × 0.3), raised on the beam's top face (y = 0.12) | Each redrawn as a `QuadMesh` facing up with the same X and Z extent and X, Z position, keeping its name and material: the inset at y = 0.1202, the marks at y = 0.1204 | same names |
| `trampoline` | none | None: its thirteen frame solids already have colliders; `Visual_Bed` is drawn by its deflection binding | none |

The Fan's printed arrow. `Visual/FlowMark` is a `Node3D` at (0.2, −0.4598, 0): 0.2 mm above the base's top face (y = −0.46), in the strip the post (|x| ≤ 0.065) and the ring's tube (|x| ≤ 0.07) leave free. It has three `QuadMesh` children lying flat, facing up, with material `Mat_Arrow`: `Shaft`, 0.14 × 0.03, centred at local (−0.03, 0, 0), so it runs from x 0.10 to 0.24; `HeadA` and `HeadB`, each 0.08 × 0.03, turned −45° and +45° about Y and centred at local (0.0717, 0, −0.0283) and (0.0717, 0, +0.0283), so both end at the point x = 0.30, z = 0.

The Fan's internal occluders become base, post, ring and hub: `[new(new(0), new(0)), new(new(0), new(1)), new(new(0), new(2)), new(new(1), new(0))]`. Streamlines start inside both new colliders, so both must be listed; its own stream is then unchanged. To another Fan's stream the post and ring are ordinary solids.

Table rows to add to `Specs` (solid; hollow; arc; decorative; moving roots):

- `rope_anchor`: `["Visual_Mount"]`; `["Visual_Eye"]`; none; none; none.
- `pulley`: `["Visual/Visual_Mount", "Visual/Visual_Post", "Visual/Wheel/Visual_WheelBody"]`; `["Visual/Wheel/Visual_Rim"]`; none; `["Visual/Wheel/Visual_Spoke", "Visual/Wheel/Visual_Marker"]` (D5); none.
- `fan`: `["Visual/Base", "Visual/Post", "Visual/Hub/Surface"]`; `["Visual/Ring"]`; none; `["Visual/BladeAxis/Blades/*", "Visual/FlowMark/*"]`; `["Visual/Hub"]`.
- `impact_lever`: `["Visual/Visual_Base", "Visual/Visual_Fulcrum", "Visual/Visual_StopLeft", "Visual/Visual_StopRight", "Visual/Beam/Visual_Beam"]`; none; none; `["Visual/Beam/Visual_Inset", "Visual/Beam/Visual_Mark_0", "Visual/Beam/Visual_Mark_1", "Visual/Beam/Visual_Mark_2", "Visual/Beam/Visual_Mark_3"]`; `["Visual/Beam"]`.
- `trampoline`: `["Visual/Visual_BackPlate", "Visual/Visual_SideLeft", "Visual/Visual_SideRight", "Visual/Visual_EndFront", "Visual/Visual_EndBack", "Visual/Visual_Post0"`–`"3", "Visual/Visual_Foot0"`–`"3"]`; none; none; `["Visual/Visual_Bed"]`; none.

### Footprint, capture and occlusion

- The new tori are hollow kinds (`HollowSolid.IsHollow` includes `Torus`, `engine/gpu/HollowGeometry.cs:33–34`): construction refuses a dynamic body that starts inside the eye, rim or ring (`engine/gpu/WorkshopPhysicsCompiler.cs:498–523`), and placement refuses such a pose (`engine/PlacementPose.cs:147`, `:253–254`).
- Placement extent follows the base colliders (`engine/PlacementPose.cs:113–116`): the Fan now reaches 0.63 m in ±z (was 0.5) and 0.63 m above its origin; the anchor 0.2075 m in +z; the pulley 0.435 m radially (was 0.4).
- No authored playable lesson places a dynamic body inside the new tori (checked: `content/puzzles.json`); the two planned pulley lessons are in Running beside other work. A body dropped onto a new torus is also affected, and one test does that: `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (the emptied carrier falls onto the Rope anchor's eye, D7); its final assertion is re-authored (Spec Change Log). The first full local round on the candidate (24 November 2026, 7309 tests, before that repair) found no other failure; it includes every test that places a Fan, Pulley or Rope anchor, the taut-tether Balloon case among them. The repaired candidate's second full round passed (7309 tests, run-log E7, and again after the repairs of review passes 1 and 2, E8 and E9).

### Decisions

Owner decisions (21 November 2026), applied here: repairs follow precedent (posts get box colliders, rings get torus colliders, raised markings are redrawn flush); the Fan's arrow and tip are redrawn as a flush marking on the housing; the Fan's blades stay decorative with no collider.

Decided, with reason:

- **D1. The Pulley post and the Fan post get colliders although a 0.34 m ball cannot reach the Fan's.** The rule is about what is drawn, and smaller payloads exist. The Pulley post is reachable from behind and has a play test; the Fan post has the matcher row and the stream test.
- **D2. Behavioural proof reuses the drop theory and its stated bounds** (stop within 5 mm of the drawn top plus the ball's radius, never deeper than the envelope's penetration; rebound ratio inside the theory's bracket). No new tolerance is introduced for contact.
- **D3. Static collider growth is accepted and recorded**: `rope_anchor` 1 to 2, `pulley` 2 to 4, `fan` base body 1 to 3.
- **D4 (coordinator decision, 21 November 2026, under the owner's standing delegation). The Pulley's rim gets a torus collider.** It turns about its own axis, so a static collider follows it exactly.
- **D5 (coordinator decision, same delegation). The rule for turned art, stated once here and referenced by `spec-engine-art-solidity.md`:** art turned by a presentation binding is decorative; art that is static and proud is solid or flush. A solid of revolution about the turning axis is not moved by the turn and is solid (D4). So the Pulley's spoke and gold marker stay decorative.
- **D6. The Fan's hub behind the ring, for the owner to see.** With a solid ring a ball reaches the hub only through the ring's 0.49 m opening. This follows from the owner's rule and changes one existing test (task 6).

- **D7. A body falling onto the Rope anchor's eye now meets it instead of passing through.** The owner-visible case: an emptied Balloon carrier hanging tied to an anchor beneath it was measured at rest on the upright hoop's top (run-log E6); the Balloon test only bounds it. This follows from the owner's rule and changes one existing test's final assertion (coordinator decision, 24 November 2026; Spec Change Log).

No owner decision is open.

## Tasks & Acceptance

**Execution:**

1. Before any edit -- build at the baseline and copy `elements/elements.bundle.json` to `.tools/visible-solid-remainder-a-evidence/bundle-baseline.json` -- AC2's baseline.
2. `elements/RopeAnchor/rope_anchor.tscn`, `elements/Pulley/pulley.tscn`, `elements/Fan/fan.tscn` -- add the five proxy nodes of the repairs table, each after the body's existing proxies -- A1–A4, A6.
3. `elements/Fan/FanElementDeclaration.cs` -- set the source's internal occluders to the four references above -- A5.
4. `elements/Fan/fan.tscn` -- delete `Arrow`, `Tip` and their two mesh resources; add `Visual/FlowMark` and its three children as specified. `elements/ImpactLever/impact_lever.tscn` -- replace the five raised boxes with the flat surfaces specified -- A7a, A7b.
5. `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- add the five `Specs` rows; add `Feature` members `AnchorEye`, `PulleyPost`, `PulleyRim`, `FanRing` with the `Drop` arms and inline data of the table below, a `Front` rotation beside `Back` (+90° about X), and the three torus features to the theory's apex list; add the two new tests named in the table -- A1–A4, A7a, A8.
6. `CuriousContraptions.tests/FanIntegrationTests.cs` -- change the count at 465 to 4; re-author `PassiveHubReceivesExternalContactWithoutEmittingUnpaidFlow` as the table specifies, keeping its four cases and every assertion; add the stream theory and the `FanArtworkTests` fact -- A5, A7b, A9. `CuriousContraptions.tests/BalloonTests.cs` -- replace only the final assertion of `AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` with the one specified in the Verification table -- A9.
7. The five element documents, `deferred-work.md`, `delivery-batches.md` and `TODO.md` -- mark part A's row in `delivery-batches.md` section 5, Repairs, as delivered; record each new collider with its size and pose, the redraws, the collider counts, D6, and parts B, B2 and C as queued (their package lists and matcher changes are in their contracts); add the `deferred-work.md` entry `pulley-lesson-weight-start` (Running beside other work).

**Acceptance Criteria:**

- AC1: Given each of the five packages, when the visible-solid theory runs its new row, then it passes, and the theory has five more cases than at the baseline (33 at `25df3cd5`).
- AC2: Given the baseline copy and the candidate's bundle, when each entry's decoded `Colliders` and airflow `InternalOccluders` are compared, then the only differences are `rope_anchor` (one more collider), `pulley` (two more) and `fan` (two more colliders and two more internal occluders); every other entry, `impact_lever` and `trampoline` included, is equal.
- AC3: Given the named existing tests of each part (verification list), when they run, then all pass with no edit, apart from: the count at `FanIntegrationTests.cs:465`, which becomes 4; the re-authored hub test; and the final assertion of `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie`. No other test counts these parts' colliders or occluders (searched: `rg -n 'Colliders|InternalOccluders'` over the A9 classes; `PackageRuntimeParityTests` derives its expectation from the bundle).
- AC4: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline total + 15 (five theory cases, four drop cases, three stream cases and three facts).

Counts in AC1 and AC4 are baseline + delta, counted on the slice's baseline at start, so the order in which parts B, B2, C and the Baseball repair publish does not change them.

## Spec Change Log

- 24 November 2026, `bad_spec` (coordinator decision under the owner's standing delegation; visible = solid): the contract said `BalloonTests` passes unedited. Its footprint search covered bodies placed inside the new tori, not a body falling onto one after a rupture. The first candidate's full round (7309 tests, run-log E6) failed `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie`: before the repair its emptied carrier fell through the Rope anchor's drawn eye; now it stops on the eye (measured: balanced at y 1.2897 on the upright hoop's top from tick 400, an exact-symmetry equilibrium; E6). Planning re-audit (24 November 2026): route correct, bounds independent of the observation, but a rest-on-the-hoop assertion is fragile, so the final assertion is one bound that holds balanced or rolled off. Amended: A9 matrix row, owned scope, footprint bullet, task 6, AC3, the two A9 Verification rows, and D7 added. Intent unchanged. Known-good work and KEEP constraints preserved; a second full round follows the repair.

## Review Triage Log

Detail per finding is in run-log E10. Tier: engine. Layers: all six on pass 1; Verification Gap, Forward-Only and Element-Free on passes 2 and 3; Blind Hunter and Edge Case Hunter once more on the cumulative test and scene repair after pass 3 (the repairs changed shared test helpers and bounds, their subject).

- Pass 1 (candidate after the 24 November amendment, 7309 tests): Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free and Intent Alignment returned. Confirmed and repaired (patch, run-log E8): small-ball drop ignored z (P1); CAT-058 overclaimed what the Balloon test proves (P2); over-precise lever print bounds (P3); Balloon bound literal (P4); wrong test-class name `AirflowGeneralityTests` in the contract and a scoped filter that never ran `AirflowRuntimeTests.OnlyExactInternalCollidersAreExemptFromOcclusion` (P5); two near-duplicate helper rules (P6). Rejected with evidence: another Fan's ring control (covered by that engine-level test), post-case Wall control (the post case already fails without the listed collider), occluder index pinning (A5 theory and the hub test pin them), z-fighting stand-off (contract-specified, `both_gate` precedent), lessons `counterweight` and `pulley_depth` (planned only; owned deferred entry), "Delivered" wording (states verified facts only). Intent Alignment: descriptive, no divergence needing action.
- Pass 2 (repair diff of E8): Forward-Only none; Verification Gap and Element-Free confirmed and repaired (patch, run-log E9): CAT-058 and D7 claimed unpinned behaviour; stale test-line citations and the wrong class name across 107 planning documents (a controlled closure, owned scope line); Balloon comment wording; lever lateral tolerance sized to the marks (5 mm) with a 0.5 mm stand-off; stale counts in Implementation Notes.
- Pass 3 (repair diff of E9): Forward-Only none; Verification Gap none; Element-Free one low documentation finding, repaired by the coordinator in the contract (A5 row cites the baseline line 465, now 498). No finding deferred under Re-review scope; no finding open.
- Pass 3b (Blind Hunter and Edge Case Hunter on the cumulative test repair): nothing confirmed. Rejected with evidence: the lever's 5 mm lateral bound (contract A7a requires the same X and Z extent and position as the replaced boxes; sized to 35 mm x 0.3 m marks; the bounds were set once by quantity and stay, per the stagnation rule); the Balloon bound's "collider shrinks" claim and a higher mount-top rest (run-log E6 probe measured the emptied collider at 0.0622 m, so any rest is far below 1.5875); coplanar-print lower bound (contract and `FaceMarkingTests` use the same 0 to 0.5 mm band; z-fighting is deferred render qualification); hand-copied eye constants (commented, documented); hub-spin friction dependence (the contract names it as the original mechanism); deleted helper defaults (VG pass 2 verified the same bodies); post inside the hub sphere (hub tests pass: the hinge pair does not collide).
- Final diff scope: see Auto Run Result.

## Design Notes

1. **Behaviour delivered.** Five drawn solids become solid; two sets of raised art become printing; five packages join the visible-solid proof. Source: owner rule "visible = solid" (25 October 2026) and the owner's decisions of 21 November 2026.
2. **Reusable.** Nothing new: scene collider proxies, the torus shape, internal occluders and the drop theory.
3. **Declared.** Proxy nodes in each package's scene; one list of collider references in the Fan's declaration. No identity appears in engine code.
4. **Execution.** Scene → `SceneGeometryExtractor.Bake` → bundle entry → the unchanged compiler and solver.
5. **Ownership.** Static colliders on existing bodies; no new state. Reset, save and load are unaffected because colliders are compiled from the bundle.
6. **Generality.** Not an engine change. The proof is per collider: its drawn surface stops a ball, and the matcher finds it at its drawn pose.
7. **Deleted.** The Fan's `Arrow` and `Tip` nodes and meshes; the Impact lever's five raised boxes. No raised copy remains.
8. **Uncertain.** None. Rules E1, E6, E7 and E8 apply; E9 is run as always. D1–D3 of the design guide hold (package data owns the variation).

## Implementation Notes

Self-check of the full owned diff from `0959e883` (the diff stat against the baseline is the record; the run log is the one new file). No engine, compiler, solver, wire or matcher file changed, and no new package, port, parameter or footprint. The Fan declaration changes one list. Each new collider is a scene proxy of its mesh's primitive, size and pose, appended after the body's existing proxies. Test changes add rows and arms to existing tables and three small helpers: `VerticesInRoot` and `AssertFacesUp` (shared by the lever and Fan print facts) and `DroppedBall`, which only parameterises the existing drop check (the Basketball is its default), so there is no second rule or code path. No needless branch, duplicate rule or unrelated churn found. The first print transforms were written column by column and transposed once (scene text is row by row) before any counted run. Evidence and decisions: run-log E3 (baseline and first candidate), E4 and E6 (Balloon stop and measurement), E7 (handoff on the repaired candidate). Review pass 1 repair (E8), tests and docs only: the small-ball drop now honours Z through one Tennis helper (`TennisBallTests.Ball` gained an optional `z`, and the second helper in `FanIntegrationTests` was deleted); `DroppedBall` derives its penetration from the same share as the class constant; the lever print fact checks the height above the face to the 0.5 mm print stand-off and position and extent to 5 mm (sized to the 35 mm wide marks after review pass 2; its separate flatness assertion was dropped as implied by the height check); the Balloon bound is derived from named eye and carrier constants; the contract and `CAT-028` name the real class `AirflowRuntimeTests`, which the scoped filter now selects. The test count is unchanged (7309). Review pass 2 repair (E9): D7 and `CAT-058` claim only what is tested (the eye's solidity is the A1 drop case and control; the Balloon scene was measured and the Balloon test only bounds the carrier); the Balloon comment says 0.36 is the declared radius used as a loose bound and that the eye constants mirror `rope_anchor.tscn`; the lever print fact holds position and extent to 5 mm, sized to the mark, and the height to the 0.5 mm stand-off; stale line citations of the moved tests are corrected, and the tests cited under the file name `AirflowGeneralityTests` now name `AirflowRuntimeTests`, in the element documents listed in E9.

## Verification

Executor for every command: the implementation owner. Raw output goes under `.tools/visible-solid-remainder-a-evidence/`.

Environment: the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log.

### Acceptance-to-test table

Drop cases give the part, its pose, the feature's top above the part origin, the drop's X and Z, and the static restitution (1 for all three parts in the baked entries). The planning auditor confirmed each drop line; the implementer recomputes it and returns to planning if any clearance is under 5 mm (recomputed 24 November 2026: the smallest clearance is 50 mm, run-log E3). Run column: **S** is the scoped affected-tests run (`scoped-affected-tests-4.txt`, 900 passed, 0 failed, 0 skipped; the filter selects `AirflowRuntimeTests`) and **F** the full local round (`full-round-4.txt`, 7309 passed, 0 failed, `Skipped: 0`), both on the candidate after review pass 2's repair under `.tools/visible-solid-remainder-a-evidence/`.

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| A1 | Drop theory, `Feature.AnchorEye` = (`rope_anchor`, `Back`, 0.18 + 0.0275, X 0.2, Z 0, 1): the eye lies flat at height 0.18 and the ball lands on its tube | Control: `VisibleSolidColliderTests.ASmallBallPassesThroughTheAnchorEyeOntoTheMount` (new fact): the anchor in `Back`, a Tennis ball of radius 0.1 dropped at the centre passes the 0.1725 m opening and meets the mount's face: its lowest centre height is within 5 mm of −0.04 + 0.1 above the origin, read as the drop theory reads a first impact (the ball rebounds at 0.85). A disc in place of the torus would stop it at 0.2075. Boundary: a torus top, first impact only | S, F: both tests passed |
| A2 | Drop theory, `Feature.PulleyPost` = (`pulley`, `Front`, 0.19, X 0, Z 0.15, 1): face down, the post's back face is 0.105 m above the wheel's; the mount's edge is 0.398 m from the resting ball's centre | Control: the rebound ratio bracket tells restitution 0.55 from none. Boundary: a flat face, full stop and rebound | S, F: passed |
| A3 | Drop theory, `Feature.PulleyRim` = (`pulley`, `Back`, 0.1 + 0.035, X 0.4, Z 0, 1): on its back the rim's tube top is 0.05 m above the wheel's face | Control: without the rim collider the ball first meets the wheel's edge at 0.085 + 0.34 above the origin, 50 mm lower, outside the 17 mm penetration band. Boundary: a torus top, first impact only | S, F: passed |
| A4 | Drop theory, `Feature.FanRing` = (`fan`, identity, 0.56 + 0.07, X 0, Z 0, 1): the top of the upright ring. The Fan is on by default; the ball is outside its cone, whose radius is zero at the origin | Control: without the ring collider the ball falls 0.49 m further, to the hub's top (r 0.14). Boundary: a torus top, first impact only | S, F: passed |
| A5 | `FanIntegrationTests.TheFansOwnRingAndPostDoNotShadowItsStream` (new theory, 3 cases), each with a Tennis ball of radius 0.0625 and no drag: centred at Fan-local (4.2, 0, 0.56), behind the ring, where the cone's radius is 0.714; centred at (2.5, −0.3, 0), behind the post, cone radius 0.425. After 12 ticks the ball's speed along the axis and the source's work are both positive. If the collider were not listed every sample would be shadowed and both would be exactly zero | Control, the third case: the first placement with a default Wall (3 × 2 × 0.25) centred at Fan-local (2, 0, 0.56), which spans z 0.435 to 0.685 and covers all four streamlines, gives exactly zero speed along the axis and zero work. The declared list is asserted at the baseline's `FanIntegrationTests.cs:465`, now line 498 (four) | S, F: the three cases passed (two positive, the Wall control exactly zero); the count test passed |
| A6 | The `fan` row of the visible-solid theory: `Visual/Post` matches a box collider of half-extents (0.065, 0.25, 0.075) at (0, −0.3, 0); and A5's post case | The row fails with "solid art without a matching collider" if the proxy is absent | S, F: the `fan` row and the post case of A5 passed |
| A7a | `VisibleSolidColliderTests.TheLeversBeamMarkingsArePrintedFlatAboveItsTopFace` (new fact): the five nodes are `QuadMesh` surfaces facing up with their previous X and Z extents and positions (to within 5 mm laterally, which excludes a mark moved, resized or missing; the height bound below is the 0.5 mm print stand-off); each lies between 0 and 0.5 mm above the beam's top face, and every mark is above the inset | 0.5 mm is the bound `FaceMarkingTests.cs:60` uses for print; the old boxes stood 8 and 10 mm proud | S, F: passed |
| A7b | `FanArtworkTests.TheFlowArrowIsPrintedOnTheBaseAndPointsAlongTheStream` (new fact): `Visual/Arrow` and `Visual/Tip` do not exist; the three surfaces of `Visual/FlowMark` lie between 0 and 0.5 mm above the base's top face; ink covers Fan-local (0.29, 0) and (0.25, 0.04) and does not cover (0.15, 0.04), tested with a point-in-triangle check on the X, Z projection (`FaceMarkingTests.Covers` is private and projects onto X, Y) | A headless print leaves (0.25, 0.04) clear; a reversed one covers (0.15, 0.04) | S, F: passed |
| A8, AC1 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`, five new cases | Existing cases unchanged | S, F: 38 cases passed (33 + the five new rows) |
| A9 | Re-authored: `FanIntegrationTests.PassiveHubReceivesExternalContactWithoutEmittingUnpaidFlow`. The Fan stays at (0, 5, 0), unpowered. For `hitsHub: true` a Tennis ball of radius 0.1 starts at rest inside the ring at Fan-local (0, 0.30, 0.1) and falls onto the hub off its centre; for `false` the same ball starts at (0, 1, 0.8), outside the ring, and falls past. Assertions unchanged: zero work on every tick, the hinge speed exceeds 0.1 rad/s only when the hub is hit, Reset returns the coordinate to zero. If the small ball does not turn the hub past 0.1 rad/s, return to planning; do not lower the bound | Existing bound and cases | S, F: the four cases passed; the small ball turned the hub past 0.1 rad/s |
| A9 | Re-authored final assertion only: `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie`. The scene and every other assertion (rupture phase, empty gas, one `IntegrityBroken`, the declared tie, the connection list) stay. `Assert.True(Y(Body(h)) < 1, ...)` becomes `Assert.True(Y(Body(h)) < 1.5875f, ...)`, its message saying that the emptied carrier must have fallen from its start height of 3 to the Rope anchor's eye or below. 1.5875 is the eye's tube top (1 + 0.2 + 0.0275) plus the test's own largest carrier radius (0.36): the highest centre at which the carrier could rest on the eye. It is not fitted to the observed rest (y 1.2897, run-log E6). There is no lower or speed bound: the carrier balances on the upright hoop's top only because the scene is exactly symmetric, and the assertion must hold whether it stays balanced or rolls off and falls (testing guidance: the least strict assertion that detects the defect) | Control: retained lift (a centre of 3 or more) fails it. Eye solidity is proven by the `AnchorEye` drop case and the small-ball control (A1), not here. Boundary: the tube top plus the full carrier radius | S, F: passed (the carrier rested at y 1.2897, 0.30 under the bound, run-log E6) |
| A9 | Existing, unchanged. Rope anchor and Pulley: `RopeTests`, `RopeGuideTests`, `WeightTests`, `BalloonTests` apart from the re-authored test above (`col_mount` stays collider 0 and the Tie stays at the eye's centre; CAT-058 section 8). Fan: the rest of `FanIntegrationTests`, `FanAdmissionTests`, `FanGuidanceTests`, `AirflowRuntimeTests` (its files include `AirflowGeneralityTests.cs`). Impact lever: `ImpactLeverTests`. Trampoline: `TrampolineTests`. Classes that place these parts: `ComposedPhysicalObservationTests`, `ComposableAnimationMappingTests`, `LinearPusherTests`, `WorkshopIntegrationHarnessTests`, and the Windmill tests that place Fans, `WindmillIntegrationTests` (Fans at (0, 5, 0) and (6, 5, 0), Windmill at (3, 5, 0): its capture samples project onto the Fan's source plane inside the new post, so these tests exercise the A5 exemption for a rotary capture), `WindmillAdmissionTests`, `BodyOwnershipTests`, `CoordinateTravelGeneralityTests`, `WindmillArtworkTests`; and `BaseballTests.TheFanMovesTheBaseballMoreThanTheTennisBallAndTheBasketball`, if the Baseball repair has published (its R4: three balls on a default Fan's axis; the Basketball's samples project inside the new post, so it consumes the A5 exemption). All five: `F32BehaviourIdentityTests`, `PackageRuntimeParityTests`, `BundleRoundTripTests` | Existing | S, F: all passed with no edit |

The hub test is not re-authored along the axis: a ball travelling along the hinge axis applies no torque about it, and a 0.25 m ball offset 0.25 m from the axis overlaps the ring by 1 cm. The small ball inside the opening keeps the original mechanism, friction from an off-centre fall. The planning auditor estimates the hub's speed at about 2.4 rad/s from a rigid impulse, 24 times the bound.

The physical bounds are the drop theory's own (5 mm stop, the penetration envelope, the rebound bracket), the hub test's existing 0.1 rad/s, and for print 0.5 mm, all reused from existing tests for the same quantities. A5 asserts sign against an exact zero.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors; the bundle regenerates.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` -- expected: 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~FanIntegrationTests|FullyQualifiedName~FanArtworkTests|FullyQualifiedName~FanAdmissionTests|FullyQualifiedName~FanGuidanceTests|FullyQualifiedName~AirflowRuntimeTests|FullyQualifiedName~RopeTests|FullyQualifiedName~RopeGuideTests|FullyQualifiedName~WeightTests|FullyQualifiedName~BalloonTests|FullyQualifiedName~ImpactLeverTests|FullyQualifiedName~TrampolineTests|FullyQualifiedName~ComposedPhysicalObservationTests|FullyQualifiedName~ComposableAnimationMappingTests|FullyQualifiedName~LinearPusherTests|FullyQualifiedName~WorkshopIntegrationHarnessTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~BundleRoundTripTests|FullyQualifiedName~FaceMarkingTests|FullyQualifiedName~Windmill|FullyQualifiedName~BodyOwnershipTests|FullyQualifiedName~CoordinateTravelGeneralityTests|FullyQualifiedName~BaseballTests"` -- expected: 0 failed, 0 skipped; every test in the table appears.
- The AC2 comparison of `bundle-baseline.json` with the candidate's bundle, by decoded entry -- expected: exactly the three differences listed.
- `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` -- expected: baseline total + 15, 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass.

**Manual checks (if no CLI):**

- Open each of the three edited scenes: every new proxy sits after the body's existing proxies and is hidden where it is a mesh.

### Running beside other work

It edits five package scenes, one declaration, `VisibleSolidColliderTests.cs` and `FanIntegrationTests.cs`. Before the engine programme only parts A, B, B2 and C, the Baseball repair, S3, S5 with S5b straight after it, and S6 with its follow-ons S6b–S6g run (owner decisions 22 and 24h). It is serialised with parts B, B2 and C and the Baseball repair, which all edit `VisibleSolidColliderTests.Specs`: one publisher at a time; the later merges main once and repeats the full local round on the merged candidate. It shares `deferred-work.md`, `TODO.md` and `docs/planning/invest/delivery-batches.md` with those slices and with S3, S5, S5b and S6–S6g. Early level proofs wait for the programme; in all 22 authored Fan puzzles (23 Fans) the ball starts 1 m in front of the ring's plane, clear of it, so the solid ring does not change them.

Two planned lessons, `counterweight` and `pulley_depth`, place the 4 kg Weight with its eye through the Pulley's drawn rim (eye 0.423 m below the rim's centre, planes 2 cm apart). With the rim solid that start is an initial overlap. This slice does not edit `content/puzzles.json`; the entry `pulley-lesson-weight-start` in `deferred-work.md` records it for the level-proof slice of those lessons, which lowers each solution weight by at least 0.1 m.

## Auto Run Result

Implementation Pass on the review candidate, before the commit: all six review layers on pass 1; every confirmed finding repaired and re-reviewed (Review Triage Log); local evidence on the repaired candidate: full local round 7309 passed, 0 failed, `Skipped: 0` (`full-round-4.txt`), maintainability gate PASS (`maintainability-gate-4.txt`), registration check 767 passed (`registration-check-4.txt`), AC2 three expected differences (`ac2-comparison-4.txt`), `git diff --check` clean; the matching hook run and the SnapshotApproval verdict are recorded in `TODO.md` and the run log by the next candidate, as the protocol requires.

Final diff scope: Pass (informed existing reviewer, agent `ab4d271bb66560356`). Candidate: baseline `0959e8834bacd5dd82226aa737c78ed3afb796e3` plus the working tree; no source file changed after the full round. Coverage: the full code diff, the five owned CAT documents, `delivery-batches.md`, `deferred-work.md`, and the 107-document citation refresh (every changed line differs only by digits or the class name); every changed path is in the Code Map's owned scope list; the contract is within budget and has no stale statement. Kept: every change serves a matrix row, a named test, the Fan occluder list, a confirmed review repair or the Spec Change Log. Simplification: no justified opportunity (`DroppedBall` only parameterises the existing drop check; the private `Covers` in `FanArtworkTests` stays because the shared one projects onto X and Y). Eight stale citations in the draft contracts of parts B, B2, C, art-solidity and S3 stay with their owners (`deferred-work.md`).

Remote build, artifact and deployment, browser, performance and release qualification are unverified (owner instruction of 29 October 2026).
