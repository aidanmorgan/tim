# EL-064 · Metal loop anchor declaration readiness spec

**Current delivery (24 October 2026):** [EL-064 delivery slice](../invest/named-elements.md#element-064). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against published main `8d1d68aa`; their `path:line` citations are to that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-064 |
| Name | Metal loop anchor |
| Type | Mechanical |
| Requirement anchor | [element-064](../requirements.md#element-064); scope index [todo-193](../requirements.md#todo-193) |
| Named entry | [named-elements.md#element-064](../invest/named-elements.md#element-064); proof owner S332 |
| CAT spec refined or extended | Refines [CAT-058 rope anchor](CAT-058-rope_anchor.md). The rope route law, admission rules and legacy harvest R1–R29 live in CAT-058 and are not repeated here |
| Related identities | [EL-067 steel cable](EL-067-steel-cable.md), EL-205 rope (Batch J), [EL-065 load hook](EL-065-load-hook.md), [EL-063 cable winch](EL-063-cable-winch.md) |
| Roadmap story | [EL-064 delivery slice](../invest/named-elements.md#element-064); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Closure review not yet run; blocked on the engine programme (owner decision 3a), which comes first and re-qualifies the delivered package (relying most on ENGINE-ISLANDS-SLEEP, for a hung load at rest on a tensile route, and ENGINE-SPATIAL-QUERIES). The satisfying part is delivered and published: `rope_anchor`, Story 10.2b (`_bmad-output/implementation-artifacts/sprint-status.yaml:331`, "done") |
| Disposition | **Candidate Duplicate closure** of the delivered Rope anchor (`rope_anchor`, CAT-058). No behaviour, parameter, port, mode, material or acceptance delta (reasoning below) |
| Planned package | None. A closed duplicate gets no package and no palette entry (D8). The id `metal_loop_anchor` is reserved for this identity only in case the closure check fails; no palette order while it is closed (not placeable: a Duplicate closure; coordinator decision 36). If the closure check fails, the package takes the next free Motion number of the [palette allocation](readiness/palette-allocation.md) (rule 7) |
| Coverage binding | None: a closed duplicate has no package, so no "Package source binding" section is added to this spec. On closure the ledger entry cites `rope_anchor` and the tests of section 5 in prose |
| delivery-batches class | Listed in 2c, "not closures until checked", with the residual "steel-cable admission is its delta over the anchor and needs a connector-material enum; depends on EL-067" (`docs/planning/invest/delivery-batches.md:143`). **Corrected by code:** there is no such delta, so EL-064 moves to 2a, group "Rope parts" |

**Why there is no delta.** The requirement row is "Fixed socket anchors a supported rope endpoint … Disconnected rope carries no anchored tension." It names no cable. The earlier draft of this spec invented the delta "the socket accepts both connector materials". In the code a rope is a connection between two Tie ports, and what a port admits is decided by its domain and direction (`engine/gpu/WorkshopConnections.cs:112`–`:114`) and its port role (`:161`–`:168`); none of the three names a material. A connector material, when [EL-067](EL-067-steel-cable.md) delivers one, is a property of the connection, so every Tie port admits it without any change to the anchor. Proof that a steel cable hangs from the anchor is an acceptance row of EL-067, not a behaviour of EL-064. The ledger's "Current candidate" note says 10.2b is pending review and publication; that is stale (it is done).

## 2. Declaration

The declaration is the delivered one, `elements/RopeAnchor/RopeAnchorElementDeclaration.cs`. Verified against the package on 21 November 2026; the earlier draft's values agree except where marked.

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement` (`RopeAnchorElementDeclaration.cs:7`). `KinematicMode.Static` (`:24`), `PayloadPolicy.None`, domains Mechanical and Rope (`:23`).
- **Body and collider.** One static box, full size 0.55 × 0.55 × 0.16 m centred (0, 0, −0.12) (`elements/RopeAnchor/rope_anchor.tscn`, `col_mount`). Mass 0.
- **Material** (restitution, friction, rolling resistance, bounce threshold): `(1, 0.3, 0, 0.1)` (`RopeAnchorElementDeclaration.cs:21`).
- **Parameters.** None (`:12`).
- **Port.** `Tie`: `PortRole.Tie`, Rope, Bidirectional, no signal, body 0, position (0, 0, 0.18), normal +Z, `RopeAttachmentRole.Anchor` (`:17`–`:18`). An Anchor takes exactly one span end (`engine/gpu/RopeTopology.cs:75`–`:84`).
- **Stores, presentation bindings.** None. Pick radius 0.55 m (`:13`).
- **Palette.** `ElementCategory.Motion`, order 32, display name "Rope anchor", colour `(0.84, 0.61, 0.28)` (`:9`–`:11`).

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Visual_Mount` | cream box 0.55 × 0.55 × 0.16 | solid, `col_mount` |
| `Visual_Eye` | ochre torus, centreline r 0.20, tube r 0.0275, at z 0.18, axis Z | **solid without a collider today**: a delivered visible-solid defect, listed in `_bmad-output/implementation-artifacts/spec-engine-art-solidity.md:93` and repaired by `spec-elements-visible-solid-remainder.md:91` (a Torus collider). It is a residual of CAT-058, not of this identity |

**Stale in the earlier draft:** the ring was proposed in pale grey to read as "metal". The delivered eye is the catalogue ochre with the gold-on-cream language of `DESIGN.md:343` ("Anchor eyes are gold on cream mounts"). A gold ring on a mount is a metal loop; the name needs no second part (resolved, section 6).

**The strand.** The rope tied to `Tie` is solid (owner decision 1): its collider is a property of the declared connection material, and the strand becomes solid with ENGINE-CONNECTION-MATERIALS (solid strand). The delivered rope draws a thick strand with no collider today: a recorded visible-solid defect owned by that story, not by this identity. When the strand gains a collider, the closure review re-runs rows C1 to C8, and its probe re-checks that the strand's collider does not snag the anchor's eye.

### Behaviour and numbers

The anchor is a fixed span endpoint. A complete route (neither end a guide) holds its load at the connect-time length; an incomplete or cut route has no tensile row and carries no tension. Span length 0.05–200 m (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:33`). Nothing here is proposed: every value is delivered.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box body from scene geometry | *present* | `ISceneGeometryElement` `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `CuriousContraptions.tests/RopeTests.cs:21` (the anchor is placed and holds) |
| Tie port with the Anchor role; one span end per anchor; branches and cycles refused | *present* | `RopeAttachmentRole` `CuriousContraptions.Abstractions/PortDescriptor.cs:74`; `engine/gpu/RopeTopology.cs:75` | `CuriousContraptions.tests/RopeTests.cs:230`, `:949` |
| Tension-only span from the anchor to a dynamic load; an unconnected load falls | *present* | `TensileSpanDeclaration` `TensileSpanDeclarationValues.cs:25` | `CuriousContraptions.tests/RopeTests.cs:21`, `:34` |
| An open (incomplete) route carries no tension | *present* | `RopeTopology.Route.Complete` `engine/gpu/RopeTopology.cs:23` | `CuriousContraptions.tests/WeightTests.cs:1232` |
| A cut route carries no tension; Reset restores the rope | *present* | `WorkshopSimulation.CutRope` `engine/gpu/WorkshopSimulation.cs:11` | `CuriousContraptions.tests/RopeTests.cs:151` |
| Save and load of an anchored load | *present* | `WorkshopConnection.RopeLength` `engine/gpu/WorkshopConnections.cs:31` | `CuriousContraptions.tests/WeightTests.cs:1309`; `CuriousContraptions.tests/RopeTests.cs:261` |

Counts: 6 present, 0 untested, 0 narrower, 0 missing.

**Element dependencies.** Weight (delivered) as the load. None undelivered.

### Engine conformance

Checked against the delivered package `elements/RopeAnchor/` on 22 November 2026.

- **Declaration data only.** One static body and one Tie port with the Anchor role. No engine code names the part: a search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Abstractions` and `CuriousContraptions.Bundle` for `rope_anchor` and `RopeAnchor` finds nothing.
- **Generic laws.** Anchoring is the tension-only span law with a static endpoint; "disconnected rope carries no tension" is route completeness in the rope topology.
- **Layers.** Colliders: 1 static box (`col_mount`); the eye's Torus collider arrives with the visible-solid repair. Visuals: mount and eye, neither used as a collider. Animation: none; nothing moves either art node (the body is static). Visible = solid: the mount has its collider; the eye is a delivered visible-solid defect until its Torus collider lands, and the rope's missing collider is ENGINE-CONNECTION-MATERIALS' (solid strand).
- **Flat state.** 1 static body, 1 box collider (2 colliders after the repair), 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 0 bindings.
- **Performance and tests.** A static body with no per-tick work; rows C1 to C8 run the production simulation. No island sleeping exists today; a hung load at rest costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this closure.

## 4. Sources and legacy

- Requirements row: "Disconnected rope carries no anchored tension." No variants.
- todo-193 scope index groups EL-064, EL-065 and EL-066; each keeps its own obligation.
- Legacy harvest: the full rope-anchor harvest is [CAT-058 §4](CAT-058-rope_anchor.md) (R1–R29). Facts specific to this spec:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Anchor is a static `Anchor` endpoint with one bidirectional `Tie` at (0, 0, 0.18). | `parts/RopeAnchorPart.cs@a6c914e:L8-L12` | carry forward | Typed fixed socket. |
| 2 | Box full size 0.55 × 0.55 × 0.16 m at z −0.12; ring r 0.2, tube 0.055 at z 0.16; pick radius 0.55. | `parts/RopeAnchorPart.cs@a6c914e:L15-L18` | carry forward | Geometry. |
| 3 | A route is complete only if neither end is a guide; an untied guide end cannot hold a load. | `engine/RopeNetwork.cs@a6c914e:L34-L35`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L187-L202` | carry forward | Row outcome "disconnected rope carries no anchored tension". |

**Files harvested:** `parts/RopeAnchorPart.cs`, `parts/catalog/rope_anchor.tres`, `engine/RopeNetwork.cs`, `CuriousContraptions.tests/SceneRopeBindingTests.cs` (all also in CAT-058).

## 5. Acceptance outline

Requirement row: [element-064](../requirements.md#element-064).

- **Chrome UI recipe (pending, post-catalogue).** Place the anchor high on a Wall and a Weight below; connect anchor `Tie` → Weight `Tie`. Verify placement and the typed link, Run, cut, Reset. It is the CAT-058 recipe; no second recipe is owed.

**The exact closure check.** The identity closes when each row below is an executed, passing test through `rope_anchor`, and the ledger entry cites the part and the tests. Rows C1 to C5 exist today; C6 to C8 are the only tests the closure-review slice adds.

| Row | Given / when / then | Test | Control | Boundary | Tolerance and its gameplay reason |
| --- | --- | --- | --- | --- | --- |
| C1 fixed socket anchors an endpoint | A 4 kg Weight tied 3 m below the anchor hangs for 3 s | existing `RopeTests.ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` (`RopeTests.cs:21`) | The same Weight unconnected is below 1 m after 1 s | none | Height in a 0.21 m band, smaller than the load and far smaller than its 2 m drop |
| C2 exact source outcome: a disconnected rope carries no anchored tension (never connected) | as C1's control | same test | C1 | none | as C1 |
| C3 the same, for a route left open at a guide | A Weight tied only to a Pulley falls to the shelf | existing `WeightTests.Counterweight_OpenRouteFallsFreelyToBench` (`WeightTests.cs:1232`) | `WeightTests.Counterweight_LiftsLighterLoadOverPulley` (`:1153`) | none | Rest height within 0.06 m of the shelf |
| C4 the same, after a cut; Reset restores | Cut the span during Run: the Weight falls; Reset restores the link, pose and intact rope, and a new Run holds again | existing `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`RopeTests.cs:151`) | Before the cut commits the rope is intact | A second cut is refused `RopeAlreadyCut` | Authored cell, local and rotation exactly; height band as C1 |
| C5 save and load | A saved anchored pendulum loads and keeps swinging | existing `WeightTests.SuspendedPendulum_SaveLoadRoundtripPreservesSwingingState` (`WeightTests.cs:1309`) | none | none | That test's own bounds |
| C6 one end per anchor | A second rope to the same anchor `Tie` is refused with `WorkshopEditRejection.InvalidConnection` and the construction is unchanged | planned `MetalLoopAnchorClosureTests.ASecondRopeOnTheAnchorIsRefusedAndChangesNothing` | The first rope alone is admitted | none | Typed refusal |
| C7 wrong-kind link | An electrical or activation link to the anchor's `Tie` is refused | S4 row R5 once delivered, else planned `MetalLoopAnchorClosureTests.AWireToTheTieIsRefused` | The rope link is admitted | none | Typed refusal |
| C8 the anchor does not move and holds when turned | The anchor rotated 90° onto a wall holds an 8 kg Weight for 3 s and its own committed pose equals its built pose | planned `MetalLoopAnchorClosureTests.ATurnedAnchorHoldsTheHeaviestWeightAndStaysPut` | The Weight unconnected falls | 0.25 kg Weight | Height band as C1; anchor pose exactly (a static body) |

Parameter refusal: none, the part has no parameter. Wrong-placement and visible-solid rows belong to CAT-058.

## 6. Open questions

**Resolved**

1. EL-064 is the same catalogue part as the Rope anchor: a Duplicate closure, not a second part. Decided now from the code: the row names no behaviour the delivered anchor lacks, and D8 gives a closed duplicate no entry so players never see two anchors.
2. No load rating on the anchor. D19 (the spec's proposal). A breaking anchor would be an `ENGINE-FRACTURE` identity of its own (EL-159), not this row.
3. The eye keeps its delivered ochre and gold-on-cream look. D9; `DESIGN.md:343`.
4. Steel-cable admission is not an EL-064 criterion. Decided now: the material is on the connection; the proof row is [EL-067](EL-067-steel-cable.md) K6.
5. The player's Run-time "Cut rope" control is kept. Owner decision, 22 October 2026: Story 10.2 keeps runtime rope cutting (`_bmad-output/planning-artifacts/epics.md@25df3cd5:3923`–`:3945`). Row C4 therefore stands as written on the player's cut, and no cutter slice re-bases it. A level may withhold the control as authored level data (coordinator decision 24l, 23 November 2026); C4 runs on a construction that offers it.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Weight, Pulley, Impact lever, Balloon (delivered, all tie to it today). EL-065 hook, EL-069 bucket, EL-070 cage and the EL-063 winch route hang from it as any `Load` does; EL-066 and EL-068 cut routes that end on it.
- **Campaign.** Hook and anchor first use in levels 11–20 (`docs/planning/invest/campaign-reservations.md:44`); shared interaction [IX-04 tension transmission](../requirements.md#interaction-04).
- **Coverage binding.** None of its own; `rope_anchor` is bound by its catalogue key CAT-058.
- **Lesson fixture (S8).** Not needed: the part is playable.

## 8. Delivery

- **Slice shape.** One closure-review slice for the group "Rope parts" in delivery-batches 2a (EL-204 Weight, EL-206 Fixed pulley, and now EL-064): one record, one candidate commit, one approval.
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which comes first; the closure review then re-runs its rows on the re-qualified package. If S4 has landed, C7 is the suite's row R5 and one planned test is dropped. The rows are re-run again when ENGINE-CONNECTION-MATERIALS (solid strand) gives the rope its collider; that story is not a prerequisite of the closure.
- **Files.** Adds `CuriousContraptions.tests/MetalLoopAnchorClosureTests.cs` (no test-project edit: published main compiles every top-level `*.cs` by one rule). Edits the ledger entry [element-064](../invest/named-elements.md#element-064): replace the stale "Current candidate" paragraph with the closure citation. This spec, the README status row and TODO. No package, no scene, no palette change.
- **Review tier.** Data-only: every capability is present with a cited test and the diff is tests and ledger rows.
- **Disqualifiers the probe must re-check.** Any of C1 to C5 no longer running through the real `rope_anchor` package; a decision that the metal loop must look or behave differently from the Rope anchor (then it is a variant with package `metal_loop_anchor`); a connector-material design that puts admission on the port instead of the connection; once the strand is solid, its collider snagging the anchor's eye.
