# CAT-058 · rope_anchor declaration readiness spec

This is the CAT-058 spec for the Rope anchor. It also carries the facts of the rope connection itself (EL-205), which has no catalogue entry; [CAT-053 pulley](CAT-053-pulley.md) and [CAT-067 weight](CAT-067-weight.md) refer here for them. Sections 1 to 3 and 5 to 9 describe the delivered package and connection, checked on 22 November 2026 against checkout `8d1d68aa`; published main is now `25df3cd5`, and its two later commits touch none of this package's files. The [10.2b contract](../../../_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md) records the rope design. Section 4 is the legacy harvest at `a6c914e`, kept verbatim as evidence; it is not a description of the current part. Browser qualification is pending until the post-catalogue phase.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-058 |
| Kind | `rope_anchor`, catalogue title "Rope anchor" |
| Requirement anchor | [CAT-058](../requirements.md#current-cat-058); retained behaviour [todo-140](../requirements.md#todo-140) and [todo-141](../requirements.md#todo-141) |
| Mapped identities | [EL-064 Metal loop anchor](../invest/named-elements.md#element-064), a candidate Duplicate closure, and [EL-205 Rope](../invest/named-elements.md#element-205), blocked on `ENGINE-CONNECTION-MATERIALS` (section 8); the rope is a connection, not a catalogue part |
| Roadmap story | 10.2b Rope anchor, tensile rope and cutting ([contract](../../../_bmad-output/planning-artifacts/epics.md#story-102b-rope-anchor-tensile-rope-and-cutting-cat-058)); 10.2c pulley routing and 10.4 qualification followed |
| Status | **Delivered.** Package id `rope_anchor`, directory `elements/RopeAnchor`. Slice record [10.2b](../../../_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md) (`done`, commit `d2862ebe`). Browser case pending (post-catalogue E2E). Every residual slice (section 8) is blocked on the engine programme (owner decision 3a, 22 November 2026); the story that owns this part's gap is `ENGINE-CAPABILITY-INSTANCES` (rope-port admission by role names), and the rope's missing strand collider belongs to `ENGINE-CONNECTION-MATERIALS` (solid strand). |
| Disposition | Delivered catalogue part. EL-064 Metal loop anchor is a candidate Duplicate closure; EL-205 Rope is blocked on `ENGINE-CONNECTION-MATERIALS` (solid strand) ([section 8](#8-delivery-and-residuals)). |

## 2. Declaration

### As delivered (checked 22 November 2026)

This table is the current declaration (`elements/RopeAnchor/RopeAnchorElementDeclaration.cs`).

| Item | Delivered value | Source |
| --- | --- | --- |
| Interfaces | `IMechanicalElement`, `ISceneGeometryElement` | `:7` |
| Body | one static body (footprint 1 body, 0 moving); zero mass frame | `:14`, `:24`, `:26` |
| Collider | `col_mount` box 0.55 × 0.55 × 0.16 m centred at (0, 0, −0.12), so its front face is at z −0.04 | `rope_anchor.tscn:19`–`:20`, `:32`–`:34` |
| Material | restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s | `:21` |
| Parameters | none. Rope length is a value of the connection (0.05–200 m, measured at connect time), not of the anchor | `:12` |
| Port | `PortId.Tie`, domain Rope, Bidirectional, role `Tie`, attachment `Anchor`, position (0, 0, 0.18), normal +Z | `:17`–`:18` |
| Stores, events, payload, presentation bindings | none; pick radius 0.55 m | `:13`, `:22`, `:25` |
| Palette | category Motion, order 32, colour (0.84, 0.61, 0.28) | `:9`–`:12` |

**Art and solidity.** `Visual_Mount` is solid with `col_mount`. **Defect: `Visual_Eye`**, the gold torus of ring radius 0.2 m and tube radius 0.0275 m lying in the plane z 0.18 (`rope_anchor.tscn:14`–`:17`, `:28`–`:30`), has no collider. Its front reaches z 0.2075, which is 0.2475 m in front of the mount face, and its back at z 0.1525 floats 0.19 m off that face with nothing drawn between them. Owned by [visible-solid remainder part A](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md) (row A1: a torus collider of the drawn shape; a ball small enough for the opening passes through to the mount), status draft. The floating gap is an art observation for that contract; it is not a solidity defect. The rope itself is drawn by the engine from committed span state and is no package mesh. **Defect: the drawn rope has no collider.** Ropes are solid (owner decision, 22 November 2026: ropes, cables, hoses and ducts get colliders; thin wires stay drawings and the Balloon string keeps its exception). The repair is owned by the connection-materials engine story `ENGINE-CONNECTION-MATERIALS` (proposed in [Batch I's summary](readiness/batch-i.md)): the strand's collider is a property of the declared connection material (declared diameter, segment rule), never per-element code, with its cost bounded by compiled declarations.

### Notes on the declaration and the rope connection

- **The anchor.** A static body with no mass, joint, sensor, store or parameter. Its `Tie` is at the centre of the eye and takes exactly one rope end. Any placement rotation turns collider, port and art together. Material: restitution 1, so a ball's own restitution governs its rebound.
- **The rope.** A connection in the Rope domain between two rope ports, with one value, its length: measured between the two Tie positions when the link is made, 0.05–200 m inclusive, kept across moves and mass edits and measured again on reconnection. Loads and anchors take one end; guides take two spans. Branches, cycles, a guide as an end, a route with no dynamic participant and an over-length route between immovable ends are refused before installation.
- **Law.** Massless, tension-only and inextensible in play: one projected length row per complete route in the solver's ordinary schedule, with the contact regularisation (60 Hz, damping ratio 10) as numerical conditioning, not a material. A slack or open route applies nothing.
- **States.** Geometry is Open (incomplete or cut), Slack or Taut; integrity is Intact or Cut. A cut requested while Running or Paused is queued and takes effect at the next committed step; Reset restores the authored rope; a Build save stores the uncut construction.
- **Art.** Cream `#fff8e9` mount, gold eye; DESIGN.md: "Anchor eyes are gold on cream mounts". Catalogue colour `#d69c47`. Icon: `elements/RopeAnchor/icon.svg`. Rope strands are straight when taut, curved to their length when slack and dashed when open, with gold knots at the two ends; all of it is drawn by the engine from committed state. Under Batch I's connection-materials design (`ENGINE-CONNECTION-MATERIALS`) the delivered rope becomes the first declared connection material, with its strand diameter (0.04 m) as material data.
- **Catalogue.** Category Motion, description "A fixed tie-off for a rope. Connect a load to suspend it or make a pendulum."

## 3. Engine capabilities

Capability families: see the CAT-058 row of the [general-engine element map](../general-engine-element-map.md) and the `rope_anchor` entry in [catalogue-elements.json](../../coverage/catalogue-elements.json). Not duplicated here.

**Capability probe (22 November 2026).** Tests were found by name and line; none was run. Counts: 6 present, 1 present-untested, 0 narrower, 0 missing. No engine code names the Rope anchor. One admission rule is not general (section 9, point 2).

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box collider from a scene proxy | present, untested | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | no test brings a body into contact with `col_mount`; the rope tests place the part and never touch it. Part A's row A1 drops a ball through the eye onto the mount and adds the table row |
| Rope port with `Anchor` role | present | `RopeAttachmentRole` (`CuriousContraptions.Abstractions/PortDescriptor.cs:74`) | `RopeTests.ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` (`CuriousContraptions.tests/RopeTests.cs:21`), `StaticFeasibleRoutesDisplayAndImmovableOverlengthRoutesRefuseAtomically` (`:490`) |
| Measured connection length, 0.05–200 m, kept across moves | present | `TensileSpanDeclaration` (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25`) | `RopeTests.AuthoredLengthBoundariesRoundTrip` (`:131`), `InvalidLengthsRefuseBeforeInstallation` (`:145`), `MeasurementPersistsAcrossMovesAndMassEditsButReconnectMeasuresAgain` (`:343`) |
| Tension-only row with mass-weighted reaction | present | `TensileConstraintSolver` (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:27`) | `RopeTests.TheLengthRowHasMassWeightedReactionAndNeverCreatesKineticEnergy` (`:408`), `SlackRopeDoesNotAccelerateTheFallingLoad` (`:34`), `InwardMotionAndCoincidentSlackDoNotReceiveConstraintImpulse` (`:431`) |
| Committed Open/Slack/Taut and Intact/Cut state; queued runtime cut | present | typed cut command and rope read | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`:151`), `MalformedCutCommandsAndCommittedGeometryRefuseWithoutChangingTheInstalledWorld` (`:505`) |
| Rope artwork from committed attachments | present | rope read codec | `RopeTests.SlackArtworkMatchesLengthAndRemainsFiniteForVerticalAndCoincidentEndpoints` (`:217`), `RopeReadCodecAcceptsRotatedSocketsAndMovingCommittedBodies` (`:647`) |
| Angled and pulley routes | present | `RopeGeometry.EvaluateRoute` (`engine/gpu/RopeGeometry.cs:83`) | `RopeTests.SinglePulleyRedirectionSupportsSuspendedWeight` (`:804`), `ThreeDimensionalReleaseSwingsWithoutLosingItsTether` (`:194`) |

**Collaborating parts.** A dynamic load, first the [CAT-067 weight](CAT-067-weight.md); the [CAT-053 pulley](CAT-053-pulley.md) shares the rope law; the [CAT-065 trampoline](CAT-065-trampoline.md) has tether tests.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| R1 | Anchor is a static rope endpoint (`RopeAttachmentKind.Anchor`) with one bidirectional `Tie` socket at local (0, 0, 0.18) m. | `parts/RopeAnchorPart.cs@a6c914e:L8-L12` | carry forward | Typed socket and fixed geometry match the requirement's "fixed typed endpoint". |
| R2 | Collider box half-extents (0.275, 0.275, 0.08) m at (0, 0, −0.12); ring art r 0.2, tube 0.055 at z 0.16; pick radius 0.55. | `parts/RopeAnchorPart.cs@a6c914e:L15-L18`; `reference/cpu/MachinePart.cs@a6c914e:L352-L355` | carry forward | Authored geometry; re-declare as f32 metres. |
| R3 | Attachment roles {None, Load, Anchor, Guide}; rope states {Open, Slack, Taut}. | `engine/ConnectionPort.cs@a6c914e:L11-L12` | carry forward | Closed sets; keep as enums end to end. |
| R4 | A rope link must name explicit matching `Tie` sockets in the same domain, output/bidirectional to input/bidirectional, between two distinct parts; non-rope links must carry no length. | `engine/ConnectionPort.cs@a6c914e:L42-L66` | carry forward | Typed admission rule. |
| R5 | Rope span length must be finite and within 0.05–200 m. | `engine/ConnectionPort.cs@a6c914e:L47-L51` | carry forward (bounds subject to Open question 1) | Authored validation boundary; no requirement value contradicts it. |
| R6 | Connecting two parts sets the span length to the straight-line distance between their world tie sockets at that moment ("Rope length is set when you connect"). | `reference/cpu/MachineWorld.cs@a6c914e:L686-L691`, `reference/cpu/MachineWorld.cs@a6c914e:L658-L659`; `tools/Campaign/Program.cs@a6c914e:L173-L187`; `content/puzzles.json@a6c914e:L5072-L5079` | carry forward | Measured length is the requirement's "complete measured rope route". |
| R7 | Loads and anchors accept one rope end, guides two; a third end, a duplicate span, or a part without an attachment role is rejected ("Ropes cannot branch"). | `engine/RopeNetwork.cs@a6c914e:L69-L78`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L259-L277` | carry forward | Requirement: invalid branching admission. |
| R8 | Open loops through guides and closed guide loops (even with no loads) are rejected. | `engine/RopeNetwork.cs@a6c914e:L100-L112`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L243-L257` | carry forward | Requirement: cycle admission. |
| R9 | A link with a missing length rejects the whole load atomically; the previous construction stays. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L278-L281` | carry forward | Atomic rejection rule. |
| R10 | Routes are walked from endpoints in canonical order (part id, then port), so results do not depend on link direction or declaration order; reversed links give the same motion. | `engine/RopeNetwork.cs@a6c914e:L91-L93`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` | carry forward | Requirement: direction/declaration order. |
| R11 | Route maximum length is the sum of authored span lengths; only the sum constrains a complete rope. | `engine/RopeNetwork.cs@a6c914e:L12-L13`, `engine/RopeNetwork.cs@a6c914e:L97-L110` | carry forward | Defines the rope law input. |
| R12 | A route is complete only if neither end is a guide; an incomplete route declares no physics and the load free-falls (0.981 m/s after 0.1 s at g 9.81). | `engine/RopeNetwork.cs@a6c914e:L34-L35`, `engine/RopeNetwork.cs@a6c914e:L125-L127`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L187-L202` | carry forward | Requirement: incomplete/open routes transmit no tension. |
| R13 | State: Open if incomplete, Slack if current length < max − 0.001 m, else Taut. | `engine/RopeNetwork.cs@a6c914e:L37-L38` | carry forward the states; do not carry forward the 0.001 m literal | The tolerance was a CPU double tolerance; freeze an f32 threshold under the game-grade envelope. |
| R14 | Rope is tension-only: no equality row; inactive while current length < max − tolerance; when active it only resists lengthening. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward the law; do not carry forward the code | Behaviour matches EL-205; the CPU impulse/acceleration solver is replaced by a TGS Soft unilateral row. |
| R15 | Consecutive attachments on the same rigid body contribute a constant length; other spans react on both attachment bodies. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L20-L22`, `engine/physics/PhysicsRopeJoint.cs@a6c914e:L44-L57`, `engine/physics/PhysicsRopeJoint.cs@a6c914e:L84-L95` | carry forward | Needed for pulley entry/exit points on one sheave body. |
| R16 | Fully static routes get no equation but are rejected if fixed geometry exceeds the authored length; a route of only static and prescribed bodies is rejected ("requires a dynamic participant"). | `engine/RopeNetwork.cs@a6c914e:L132-L143`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L204-L221` | carry forward | Matches the CAT-053 requirement text. |
| R17 | Rope routes keep connected-body collision enabled; a slack rope between two loads does not suppress their contact or rebound. | `engine/RopeNetwork.cs@a6c914e:L144`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L223-L255` | carry forward | Requirement: connected-body collision stays enabled. |
| R18 | A missing registered participant body is rejected, never invented as a fixed point. | `engine/SceneRopeJoint.cs@a6c914e:L39-L51`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L306-L307` | carry forward | Requirement text for CAT-053 routes. |
| R19 | Acceptance: a slack anchored rope lets the weight fall freely; it becomes taut, holds the weight at the rope length (±0.001 m) with vertical speed ≤ 0.001 m/s; an upward 1 m/s impulse is not resisted. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L146-L185` | carry forward (re-freeze tolerances under f32) | Requirement: slack test and force reaction. |
| R20 | Acceptance: 3D pendulum (anchor at (0, 6, −0.18), weight mass 2 at (2, 5, 2)) keeps length within ±0.002 m and total energy ≤ initial + 0.5 J for 480 ticks, and swings through depth to z < −1. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L191-L215` | carry forward (re-freeze tolerances under f32) | Requirement todo-141: 3D pendulum work/length bounds. |
| R21 | Rope links cannot be removed while running; after Reset the same link can be disconnected and the load then falls. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L158-L189` | carry forward | Construction edits are build-mode only. |
| R22 | Reset restores exact link lengths and endpoint identities; a second Run replays to the same state signature. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L59-L65`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L257-L280` | carry forward | Requirement: Reset/save preserve endpoint identities and lengths. |
| R23 | Tether integration: a taut anchor rope keeps a weight above a trampoline (no touch, no compression); a lengthened (+2.3 m) slack tether allows a rebound and becomes taut; energy never exceeds 1.01 × initial; Reset restores the saved construction. | `CuriousContraptions.tests/TrampolineRopeTests.cs@a6c914e:L33-L118` | carry forward | Integration acceptance shared with CAT-065. |
| R24 | Routes were point guides ("not a round pulley sheave approximation"). | `engine/SceneRopeJoint.cs@a6c914e:L20-L21`; `engine/physics/PhysicsRopeJoint.cs@a6c914e:L20-L22` | do not carry forward | Superseded: CAT-053 requires finite-radius groove tangents and arcs. |
| R25 | A taut span whose attachments coincide throws and rolls the world back. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L64-L67`; `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L161-L171` | do not carry forward | Game-grade rule: numerical residuals clamp or continue, never fault the tick. |
| R26 | Analytic joint-stop sweeps, per-step event budgets, position projection and transactional rollback for ropes. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L123-L144`; `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L47-L64`, `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L104-L159`, `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L213-L237` | do not carry forward | CPU solver path and proof-grade machinery. |
| R27 | Read-only `Ropes` view and a fresh constraint slot object per Run. | `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L39-L85` | do not carry forward | Legacy object-identity mechanism; the kept fact is R22. |
| R28 | Catalogue entry: id, title, "Ropes" category, description, colour, no parameters. | `parts/catalog/rope_anchor.tres@a6c914e:L8-L14` | carry forward | Catalogue identity. |
| R29 | Historical status: ropes supported explicit lengths, slack, tension and fixed pulley routing; rope cutting and moving-block ratios were future work. | `reference/P0-022-before/source-README.md@a6c914e:L99` | carry forward as context | Confirms scope; rope cutting stays a separate obligation. |

### Files harvested

- `parts/RopeAnchorPart.cs`
- `parts/catalog/rope_anchor.tres`
- `parts/scenes/rope_anchor.tscn`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs`
- `engine/RopeNetwork.cs`
- `engine/SceneRopeJoint.cs`
- `engine/physics/PhysicsRopeJoint.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `reference/P0-022-before/source-README.md`
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the surviving pictogram)
- `tools/Campaign/Program.cs`
- `content/puzzles.json` (checked; no rope_anchor placement or stock)
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- `CuriousContraptions.tests/SceneRopeBindingTests.cs`
- `CuriousContraptions.tests/TrampolineRopeTests.cs`
- `CuriousContraptions.tests/ConnectionPortTests.cs` (checked; generic typed-port rules already captured by R4)
- `engine/physics/JointConstraints.cs`, `engine/physics/JointEquations.cs` (checked, no rope-specific knowledge)
- `engine/bridge/*` (checked, no element knowledge)
- `reference/p054-guide/puzzles-candidate.json` (checked; same rope levels as `content/puzzles.json` apart from the orientation encoding)

## 5. Acceptance outline

Requirement row: [CAT-058](../requirements.md#current-cat-058); story 10.2.

- **Chrome UI recipe.** In free play, place a Rope anchor high on the bench and a Weight below it using the toolbox and move gizmo; connect anchor `tie` → weight `tie` with the contextual wiring control (rope domain). Verify the placed configuration and the typed rope connection from the committed read, not the scene.
- **Positive.** Run: the Weight hangs from the anchor on a taut rope; the route length equals the measured connect-time length.
- **Negative/control.** An unconnected Weight falls to the bench; a rope from a Weight to a lone Pulley (incomplete route) is dashed and carries no tension.
- **Boundaries.** Slack start (weight moved closer after connecting, if the UI keeps the length — Open question 1), wrong-endpoint and branching/cycle admission rejected atomically, angled and pulley routes, force reaction on the load.
- **Run/Reset.** Reset restores exact endpoint identities, span lengths and weight pose; a second Run replays the same committed result.
- **Save/Load.** Save and reload preserve `tie` endpoints and `rope_length` values; unknown sockets or domains reject.
- **Integrations.** Pendulum with CAT-067 (story 10.4), pulley routes with CAT-053, tether over CAT-065.

**Native tests that exist per requirement criterion (22 November 2026; none was run for this review).** Hanging load and its control: `RopeTests.cs:21`. Slack: `:34`, `:431`. Incomplete route: `:230`, `:448`. Wrong endpoint, branching and cycle refusal: `:230`, `:545`, `:949`. Angled and pulley routes: `:194`, `:804`. Force reaction: `:362`, `:408`. Artwork follows the attachment: `:217`, `:647`. Reset and save preserve endpoints and lengths: `:151`, `:131`, `:261`, and the generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CatalogueIntegrationTests.cs:273`). The anchor has no parameter to refuse.

**Residual.** (1) A rope port linked with a non-rope kind is not tested: `BundlePortsTests.cs:262`–`:284` has no Rope case. Shared planned row `RopeTests.ARopePortRefusesANonRopeLinkAndANonRopePortRefusesARopeLink`, typed `InvalidConnection` ([CAT-067](CAT-067-weight.md), section 5). (2) "Open routes dashed" is a drawn style: native tests assert the committed `Open` state (`:230`) and finite slack artwork (`:217`), not the dash pattern; it stays with the post-catalogue browser case. (3) The eye's solidity and contact with the mount: row A1 of visible-solid remainder part A.

## 6. Open questions

Resolved entry decisions — coordinator,30October2026. These are explicit implementation decisions, not a claim of a new owner answer. They settle the former blockers; the [10.2b contract](../../../_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md#design-notes) supplies the current command/read and numerical details.

1. Connect-time world Tie distance sets per-span length, inclusive0.05–200m. Moves and mass edits preserve it; reconnect remeasures. Zero, missing, nonfinite and outside values refuse. No separate length editor in10.2b.
2. Massless, tension-only/inextensible gameplay uses the shared projected soft-constraint law. Explicitly extend the existing contact regularization policy (60Hz, damping ratio10) to the new length row through effective-mass spring/damping coefficients. This is numerical conditioning, not a rope material or elastic-energy store. Verify gameplay support, release and energy behavior; do not inherit historical precision pins or a second solver.
3. The rope has no mass. It is solid (owner decision, 22 November 2026), but the delivered rope has no collider yet: a recorded visible-solid defect owned by `ENGINE-CONNECTION-MATERIALS`, which gives every thick strand a collider from its declared material. Until then a body passes through a drawn rope. Slack sag is a length-matched drawing driven by committed attachments and does no physical work; how a solid slack strand hangs is that story's to define.
4. Anchor uses the sourced restitution1, threshold0.1m/s, friction0.3 and rolling0; static mass0. Existing full3D placement rotations transform collider, Tie and art. No separate wall-mount attachment mechanic is added.
5. Rope is a connection, with no separate inventory stock or metre budget. Existing level endpoint stock and connection permissions still apply.
6. Open means incomplete or cut; Slack means current geometric length below authored maximum; otherwise Taut. No inherited0.001m deadband or universal epsilon. State classification does not assert nonzero tension. Coincident positive-length endpoints are slack and never fault a tick.
7. Cutting remains required in10.2b. Running/Paused accept a typed span-targeted CutRope request through the existing command path. Applied acknowledges queued input; the UI says Cut queued until the next successful committed step removes that route's tensile row and publishes Cut/Open. Paused waits for Step/Resume; no artificial time advance. Pending/already-cut route and unknown span refuse typed without altering other routes. Discarded candidates do not publish or consume an unapplied request. Reset clears cuts and restores original authored endpoints/lengths; Building-only Save/Load stores the uncut authored construction. Build-mode connection deletion/Undo is distinct. Steel-cable resistance remains EL-067; finite-radius pulley routing remains10.2c. The control exists in the game; a level may withhold it, like any player control, as authored level data (coordinator decision 24l, 23 November 2026).

No item needs the owner.

## 7. Integration

- **Collaborators, all delivered:** Weight (`Load`), Pulley (`Guide`), Impact lever (`TieA`/`TieB`, `Load`), Trampoline (tether tests `TrampolineTests.cs:2112`, `:2140`), Linear pusher (`LinearPusherTests.HeadTransfersLoadThroughCargoToARopeAndDisconnectingTheRopeReleasesTravel`, `LinearPusherTests.cs:179`). Both ends of every link are `SimulationDomain.Rope`, Bidirectional.
- **Not delivered:** EL-067 Steel cable (a connection material; needs `ENGINE-CONNECTION-MATERIALS`), EL-066 Scissors and EL-068 Tin snips (cutters), EL-063 Cable winch, EL-065 Load hook.
- **Campaign.** No level in `content/puzzles.json` places a Rope anchor: levels 19 and 20 tie Weights through Pulleys only. The plan teaches the anchor at level 12c, where the authored `pulley_depth` becomes the anchor's lesson; the content slice must add the anchor to that level (it places none today), and a pendulum lesson is further Epic 15 work that would need no engine story.
- **Coverage binding.** Catalogue key `rope_anchor` (CAT-058).

## 8. Delivery and residuals

- **Delivered** by 10.2b; extended by 9A.46a, 10.2c and 10.4.
- **Blocked on the engine programme (owner decision 3a, 22 November 2026).** The residual slice and the closure review below wait for it. The story that owns this part's gap is `ENGINE-CAPABILITY-INSTANCES` (section 9, point 2).
- **Residual slice.** Visible-solid remainder part A: the eye's torus collider and the package's table row; engine tier, as that contract states. Probe re-check: `col_mount` stays collider 0 (the eye is appended), and the Tie stays at the eye centre (0, 0, 0.18).
- **EL-064 Metal loop anchor: candidate Duplicate closure**; no delta. Its requirement row names no cable, and what a rope port admits does not depend on a connector material today. Closure check as recorded in [EL-064](EL-064-metal-loop-anchor.md): the hanging-load test and its unconnected control (`RopeTests.cs:21`), the Reset and save rows (`:151`, `:131`). Its ledger outcome "disconnected rope carries no anchored tension" is the control of `:21` and `:448`. One of its closure rows uses the player's runtime cut, which stays (owner decision of 22 October 2026: Story 10.2 keeps runtime rope cutting). The closure review runs after the engine programme (owner decision 3a).
- **EL-205 Rope:** its behaviour is delivered by the Rope connection domain with no package, and its outcome "slack rope cannot push or pull until tension develops" is `RopeTests.cs:34` and `:431`. It cannot be closed yet: it is blocked on `ENGINE-CONNECTION-MATERIALS` (solid strand). The connection-material story converts the delivered rope in the same change and re-runs the closure rows recorded in [EL-205](EL-205-rope.md).
- **Solid strand (owner decision, 22 November 2026).** The delivered rope is drawn with no collider: a visible-solid defect owned by `ENGINE-CONNECTION-MATERIALS`. No rope test found by name relies on a body passing through a rope; `RopeTests.SlackAndCutConnectionsKeepOrdinaryLoadContact` (`RopeTests.cs:385`) is about contact between the loads. That story's probe must re-run every rope, pulley, lever and tether test, because a solid strand can touch the wheel it wraps, the beam or frame it is tied to, and a load swinging through it. For light the same holds, with no optical exception ([EL-176](EL-176-red-laser-emitter.md) section 10, item 6): today the delivered rope lets light through as it lets bodies through, and once `ENGINE-CONNECTION-MATERIALS` gives it a collider it occludes like any collider.

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The package fix is the eye's collider (section 2, visible-solid remainder part A). No engine code names this element (searched `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions). Two engine items touch the rope it anchors: admission by role name (point 2) and the missing strand collider (point 3).

1. **Declaration data only.** One static body, one box collider, one material, one port with `RopeAttachmentRole.Anchor`. No code.
2. **Generic laws.** Supporting a load: the tension-only span law between two rope ports, with the anchor's body static, so the reaction goes to an immovable body. Slack, open and cut states are states of the generic span, not of the anchor. Contact with the mount: ordinary contact. **Required engine refactor:** a rope port is not admitted by domain, direction and attachment role alone. Three admission sites enumerate the role names `Tie`, `TieA` and `TieB` (`engine/gpu/WorkshopConnections.cs:163`–`:164`, `engine/gpu/ElementPortCatalogue.cs:182`, `CuriousContraptions.Bundle/BundleValidator.cs:543`, `:546`), so a package with a third or differently named rope port needs an engine edit, against design guide D2. Owned by `ENGINE-CAPABILITY-INSTANCES` (owner decision 3a, 22 November 2026), which removes product shapes from shared contracts, the class of engine audit findings E3 and E5: "A rope port is admitted by domain Rope, Bidirectional and its declared attachment role; port identity is the typed id, and no admission site enumerates role names."
3. **Three layers.** Colliders: `col_mount`; part A appends a torus for the eye. Visuals: `Visual_Mount`, `Visual_Eye`. Animation: none on the part. Rope strands and knots are drawn by the engine from committed span state for every rope connection; they are not package art. The strand has no collider today, which breaks visible = solid for a thick strand; the collider layer for strands is `ENGINE-CONNECTION-MATERIALS`'s to add, as material data.
4. **One-way flow and typing.** Committed span state → rope artwork; nothing flows back. Typed `PortId`, `SimulationDomain`, `RopeAttachmentRole`; the connection length is f32.
5. **Flat-state counts.** 1 static body; 1 box collider, 2 after part A; 0 joints, drives, sensors, nodes, stores, events, bindings; 1 port; 0 parameters. Each rope connection adds one span row, counted per connection at compile time.
6. **Performance fitness.** One static primitive, two after part A; the eye torus is required by visible = solid. Island sleeping is not implemented; a resting body is still stepped (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026). The anchor's body is static, so the gap concerns the loads it holds, not the part.
7. **Scenario tests.** `RopeTests` runs real constructions through the production simulation; no test depends on an anchor-specific path.
