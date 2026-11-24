# EL-205 · Rope named-identity spec

**Current delivery (24 October 2026):** [EL-205 delivery slice](../invest/named-elements.md#element-205). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope). **Propagation of 22 November 2026:** re-disposed to the declared rope material under owner decision 1 (strands are solid) and the coordinator's disposition recorded in `readiness/batch-i.md`; the delivered behaviour rows are kept.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-205 |
| Name | Rope |
| Type | Mechanical |
| Anchor | [requirements.md#element-205](../requirements.md#element-205); [named entry](../invest/named-elements.md#element-205); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S307 |
| Refines / extends | No CAT spec of its own: the rope is a connection. Specified jointly with [CAT-058 rope anchor](CAT-058-rope_anchor.md) (rope harvest) and [CAT-053 pulley](CAT-053-pulley.md) |
| Related | [EL-067 Steel cable](EL-067-steel-cable.md) (Family sibling), [EL-066 Scissors](EL-066-scissors.md) and [EL-068 Tin snips](EL-068-tin-snips.md) (cutters), EL-206 Fixed pulley, EL-207 Moving pulley, EL-064 Metal loop anchor, CAT-067 Weight, EL-184 Mechanical ball gate |
| Roadmap story | [EL-205 delivery slice](../invest/named-elements.md#element-205); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Tension behaviour delivered as the Rope connection domain (slice records 10.2b, 10.2c and 10.4 are `done`); the delivered strand has no collider, a recorded visible-solid defect. Not closed; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (strand colliders as query candidates) and ENGINE-ISLANDS-SLEEP; then blocked on ENGINE-CONNECTION-MATERIALS (solid strand), which converts the delivered rope to this declared material in the same change; then S7 |

### Disposition (re-disposed 22 November 2026)

**Declared rope material, a Family slice member with EL-067 Steel cable ("Rope materials").** Not a Duplicate closure. The rope stays a connection, not a placed part: a `WorkshopConnectionDomain.Rope` link (`engine/gpu/WorkshopConnections.cs:11`) between two ports whose `RopeAttachmentRole` is Anchor, Load or Guide (`CuriousContraptions.Abstractions/PortDescriptor.cs:74`). Two things the delivered connection lacks make it a delta, not a duplicate:

1. **The strand is solid.** Owner decision 1 (22 November 2026): ropes, cables, hoses and ducts get colliders; only thin wires and the Balloon string stay non-solid drawings. The delivered rope draws a thick strand with no collider, so a ball passes through it: a recorded visible-solid defect owned by ENGINE-CONNECTION-MATERIALS (solid strand; coordinator decision 16).
2. **The rope is the first declared connection material.** When connections gain a material (ENGINE-CONNECTION-MATERIALS), the delivered rope becomes the first declared material in the same change, forward-only: no default fill and no migration. Its material values are the ones Batch I's cutters need: strand diameter d = 0.04 m and cutting resistance R = 1 J ([EL-066](EL-066-scissors.md) section 2).

The earlier disposition (candidate Duplicate closure, closing now against today's connection) is withdrawn: a closed duplicate must have no delta, and EL-205 now has two. The behaviour rows below are kept: they are already executed through the delivered packages and are re-run on the converted material.

**Delivered behaviour, re-run on the material (exact).** Each ledger criterion is executed today through the delivered packages by these tests; the material slice re-runs them after the conversion and adds A5 to A7 of section 5:

| Ledger criterion | Executed proof |
| --- | --- |
| Finite-length connector couples routed endpoints | `RopeTests.ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` (`CuriousContraptions.tests/RopeTests.cs:21`); `RopeTests.UnequalDynamicLoadsReactToASeparatingFallAndReversingTheEdgePreservesIt` (`:362`) |
| Slack rope cannot pull | `RopeTests.SlackRopeDoesNotAccelerateTheFallingLoad` (`:34`) |
| Slack rope cannot push | `RopeTests.InwardMotionAndCoincidentSlackDoNotReceiveConstraintImpulse` (`:431`); `RopeTests.SlackAndCutConnectionsKeepOrdinaryLoadContact` (`:385`) |
| Tension develops when taut | `WeightTests.SuspendedPendulum_SteepAngleSlackReleaseTransitionsSmoothly` (`CuriousContraptions.tests/WeightTests.cs:1118`); `RopeTests.AtwoodSlackTransitionOnFloorContactReleasesTension` (`:842`) |
| Length boundaries and refusal | `RopeTests.AuthoredLengthBoundariesRoundTrip` (`:131`); `RopeTests.InvalidLengthsRefuseBeforeInstallation` (`:145`) |
| Topology refusal (branch, cycle, open end) | `RopeTests.OpenGuideTopologyIsIncompleteAndCyclesOrBranchesRefuse` (`:230`) |
| Run/Reset | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`:151`); `WeightTests.SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity` (`:1275`) |
| Save and load | `RopeTests.DemandBeyondInitialReservesPreservesIndependentRopesAndSaveLoad` (`:261`); `RopeTests.TwoPulleyRouteSurvivesResetAndSaveLoad` (`:905`) |

## 2. Declaration

### Current declaration (21 November 2026)

This subsection states what is delivered. The planned material record follows it.

- **No package today.** A rope is one `WorkshopConnection` in the Rope domain carrying its measured length; the compiler turns each route into ordered `TensileSpanDeclaration` rows (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25`; `engine/gpu/WorkshopPhysicsCompiler.cs:538`–`:564`).
- **Parameter.** Span length, f32 metres, 0.05 to 200 inclusive, enforced by `TensileSpanDeclaration.Validate` (`TensileSpanDeclarationValues.cs:33`). It is measured between the two sockets at connect time, kept through moves and mass edits and measured again on reconnect (`RopeTests.MeasurementPersistsAcrossMovesAndMassEditsButReconnectMeasuresAgain`, `RopeTests.cs:343`). There is no length editor.
- **Ports.** None of its own. It joins ports of domain `SimulationDomain.Rope` (`CuriousContraptions.Abstractions/SimulationDomain.cs:14`): an Anchor or Load port takes one end, a Guide port two spans.
- **State.** `RopeState { Open, Slack, Taut }` and `RopeIntegrity { Intact, Cut }` (`TensileSpanDeclarationValues.cs:9`–`:10`). A cut is the typed command `CutRope(RopeSpanId)` (`CuriousContraptions.Abstractions/RuntimeServices.cs:77`), committed at the next step and restored by Reset. The player's Run-time "Cut rope" control is kept (owner decision of 22 October 2026, Story 10.2); a level may withhold it, like any player control, as authored level data (coordinator decision 24l, 23 November 2026).
- **Law.** One unilateral length row per complete route with a mass-weighted reaction on the two end bodies (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:29`–`:62`); inactive while the route is shorter than its length, open or cut. No mass, no elastic store, no rope friction. Contact between the two loads stays enabled: a slack rope never suppresses it (row A2).
- **Stores, events.** None. **Palette.** No palette order (not placeable: a connection material; coordinator decision 36).

### Planned material record (ENGINE-CONNECTION-MATERIALS)

| Field | Value | Reason |
| --- | --- | --- |
| Id | `rope` (typed id; the default material of a Rope-domain connection) | The engine names no material |
| Span law | tension only, inextensible; stiffness 60 Hz, damping ratio 10 | The delivered span values ([EL-067](EL-067-steel-cable.md) section 2) |
| Strand diameter d | 0.04 m | The cutting contract's engagement distance; equal to the drawn strand |
| Cutting resistance R | 1 J | Scissors (4 J) and Tin snips (5–40 J) both cut it ([EL-066](EL-066-scissors.md) section 2) |
| Collider | solid strand of diameter d along the committed span, by the material's declared segment rule | Owner decision 1; the rule and its cost bound belong to ENGINE-CONNECTION-MATERIALS (solid strand) |
| Mass | none | The delivered massless span |

### Art and solidity

Rope artwork is drawn by the engine from committed route state: a warm-wood line, straight when taut and curved to its length when slack, gold `#f7cb52` knots at the two ends, dashed while a route is open (`RopeTests.SlackArtworkMatchesLengthAndRemainsFiniteForVerticalAndCoincidentEndpoints`, `RopeTests.cs:217`). It is not a mesh in any package scene, so the art-solidity declaration of [ENGINE-ART-SOLIDITY](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md) does not classify it. **Today the line has no collider and a ball passes through a rope: a recorded visible-solid defect**, owned by ENGINE-CONNECTION-MATERIALS (solid strand), which gives the strand the collider of its declared diameter (owner decision 1). After it, the drawn strand and the strand collider have the same diameter. No icon. It is told from EL-067 Steel cable by a twisted-fibre line against a plain grey strand.

### Harvest-era subsections — removed; the text is in git history.

## 3. Engine capabilities

Families: EnvironmentState, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

### Capability probe (21 November 2026; two rows added 22 November 2026)

Each *present* row cites a symbol and a test that exercises it; both were opened in this checkout.

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Rope connection domain and attachment roles | present | `engine/gpu/WorkshopConnections.cs:11`; `PortDescriptor.cs:74` | `RopeTests.MeasurementUsesBothNamedSocketsRegardlessOfDeclarationOrder` (`RopeTests.cs:301`) |
| Route topology: one end per load or anchor, two spans per guide, no branch or cycle | present | `engine/gpu/RopeTopology.cs:9` | `RopeTests.OpenGuideTopologyIsIncompleteAndCyclesOrBranchesRefuse` (`:230`) |
| Tension-only length row, mass-weighted reaction | present | `TensileConstraintSolver.cs:29`–`:62` | `RopeTests.TheLengthRowHasMassWeightedReactionAndNeverCreatesKineticEnergy` (`:408`) |
| Length admission 0.05 to 200 m | present | `TensileSpanDeclarationValues.cs:33` | `RopeTests.InvalidLengthsRefuseBeforeInstallation` (`:145`) |
| Finite-radius guide geometry (tangent legs and wrapped arc) | present | `engine/gpu/RopeGeometry.cs` (`EvaluateRoute`) | `RopeGuideTests.EvaluateRouteSupportsMultipleGuidesWithDistinctRadiiAndOffsets` (`CuriousContraptions.tests/RopeGuideTests.cs:72`) |
| Commanded cut, restored by Reset | present | `RuntimeServices.cs:77`; `engine/gpu/WorkshopSimulation.cs:11` | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`:151`) |
| Committed rope read and artwork | present | `CuriousContraptions.Abstractions/PhysicsPresentationRead.cs:10` | `RopeTests.RopeReadCodecAcceptsRotatedSocketsAndMovingCommittedBodies` (`:647`) |
| Save and load of rope links | present | `engine/gpu/WorkshopConnections.cs:11` (link with length) | `RopeTests.DemandBeyondInitialReservesPreservesIndependentRopesAndSaveLoad` (`:261`) |
| A declared connection material carried on the connection, wire, save and span read; unknown id refused | missing | none: `WorkshopConnection` carries only a rope length (`WorkshopConnections.cs:31`) | ENGINE-CONNECTION-MATERIALS (story text in `readiness/batch-i.md` section 5) |
| A solid strand: the collider of a span is a property of its declared material (diameter, segment rule), cost bounded by compiled declarations | missing | none | ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1, coordinator decision 16 |

Counts: 8 present, 0 untested, 0 narrower, 2 missing. The reaction of a route on a dynamic guide body is not part of this identity; it belongs to [EL-207](EL-207-moving-pulley.md) and ENGINE-TENSILE-EXTENSIONS.

**Dependencies.** CAT-058 anchor and CAT-067 weight as endpoints; CAT-053 pulley as guide (Story 10.2).

## 4. Sources and legacy

- Requirement: "Finite-length tension-only connector couples actual routed endpoints"; outcome "Slack rope cannot push or pull until tension develops" ([element-205](../requirements.md#element-205)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Tension-only: no equality row; inactive below the maximum; resists only lengthening | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward (law), do not carry forward (CPU impulse code) | The EL behaviour. |
| 2 | Span length finite, 0.05–200 m; non-rope links carry no length | `engine/ConnectionPort.cs@a6c914e:L47-L52` | carry forward | Admission bounds. |
| 3 | Length set from socket distance at connect time | `reference/cpu/MachineWorld.cs@a6c914e:L686-L691` | carry forward | Measured route. |
| 4 | No branching; loads and anchors one end, guides two | `engine/RopeNetwork.cs@a6c914e:L69-L78` | carry forward | Topology. |
| 5 | Open routes and closed loops rejected | `engine/RopeNetwork.cs@a6c914e:L100-L112` | carry forward | Cycle admission. |
| 6 | Accepted: a lengthened rope is Slack and the weight falls; it becomes Taut and holds; an upward impulse is not resisted | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | carry forward (re-freeze tolerances) | Slack cannot push. |
| 7 | Accepted: a fixed guide transfers tension but never pushes | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L33-L45` | carry forward | Tension only through guides. |

The remaining rope facts (3D pendulum bounds, Reset identities, trampoline tether, rejected point-guide geometry) are harvested in [CAT-058](CAT-058-rope_anchor.md#4-legacy-harvest).

**Files harvested:**
- `engine/physics/PhysicsRopeJoint.cs`
- `engine/RopeNetwork.cs`
- `engine/ConnectionPort.cs`
- `reference/cpu/MachineWorld.cs` (connect-time length)
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- The remaining rope files (for example `engine/SceneRopeJoint.cs`, `CuriousContraptions.tests/SceneRopeBindingTests.cs`, `CuriousContraptions.tests/TrampolineRopeTests.cs`) are harvested in [CAT-058](CAT-058-rope_anchor.md#4-legacy-harvest) (Batch C). Searched with no further rope knowledge for this identity: `engine/bridge/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-205](../requirements.md#element-205), [CAT-058](CAT-058-rope_anchor.md) and Story 10.2.
- **Chrome UI recipe (pending, post-catalogue).** Place a rope anchor high and a Weight below; connect `Tie` → `Tie` (rope is the chooser's default material); then edit the construction so the rope is longer than the gap. Run.
- **Positive.** The taut rope holds the Weight at its length.
- **Negative/control.** A slack rope lets the Weight fall freely until taut; pushing the Weight toward the anchor meets no resistance; a third rope end on a Weight is refused; a ball dropped beside a rope falls past it.
- **Boundaries.** Length 0.05 and 200 m; a route ending at a pulley (Open, no tension); 3D pendulum.
- **Run/Reset.** Endpoints, lengths and material restored exactly.
- **Save/Load.** Rope links with endpoints, lengths and material round-trip.
- **Integrations.** Retained rope behaviour [todo-140](../requirements.md#todo-140), qualification [todo-141](../requirements.md#todo-141) and rope artwork [todo-469](../requirements.md#todo-469); campaign row 2 ("... rope ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); routed through EL-206/EL-207 pulleys to CAT-067 weights and EL-184.

### Acceptance rows (21 November 2026; A6 and A7 added 22 November 2026)

Tolerances are the ones the delivered tests already use and are reused, not tightened: 0.005 m between a slack-roped and a free load after 30 ticks (a pull would show as centimetres); the 0.5 mm resting envelope over 3 s for a held load. Stall guards are in game seconds. No arrival tick is asserted.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given a Weight tied to an anchor by a rope longer than the gap, when Run starts, then it falls as a free Weight does until the rope is taut and is then held at the rope's length | `RopeTests.SlackRopeDoesNotAccelerateTheFallingLoad`; `RopeTests.ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` | the unconnected Weight falls to the bench | length 0.05 m and 200 m (`AuthoredLengthBoundariesRoundTrip`) |
| A2 | Given two loads joined by a slack rope and resting in contact, when time passes, then the rope applies no impulse and ordinary contact holds | `RopeTests.SlackAndCutConnectionsKeepOrdinaryLoadContact` | a taut rope reacts (`UnequalDynamicLoadsReactToASeparatingFall…`) | coincident ends (`InwardMotionAndCoincidentSlackDoNotReceiveConstraintImpulse`) |
| A3 | Given a route that ends at a pulley, when Run starts, then it carries no tension and the load falls | `WeightTests.Counterweight_OpenRouteFallsFreelyToBench` (`WeightTests.cs:1232`) | the completed route holds (`RopeTests.SinglePulleyRedirectionSupportsSuspendedWeight`, `:804`) | a branch, a cycle or a third end is refused at edit (`:230`) |
| A4 | Run then Reset restores ends, lengths and an uncut rope; save and load round-trip links and lengths | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope`; `RopeTests.DemandBeyondInitialReservesPreservesIndependentRopesAndSaveLoad` | — | a length outside 0.05 to 200 m is refused before installation (`:145`) |
| A5 (planned, because the generic wrong-domain tests cover Activation and Electrical links only) | Given a Rope link whose other end is an Electrical, Activation or Mechanical port, when it is submitted through the edit interface, then it is refused with a typed reason and the construction is unchanged | `RopeTests.ARopeEndOnAPortOfAnotherDomainIsRefusedAndChangesNothing` | a Tie-to-Tie link is accepted | the generic wrong-kind row of the conformance suite (S4 row 5) replaces this test once S4 lands |
| A6 (planned, ENGINE-CONNECTION-MATERIALS) | Given a rope connection, when compiled, then its span carries the `rope` material's declared stiffness, damping, d and R; a save naming an unknown material id is refused with a typed reason and nothing is loaded; Reset and save/load keep the material | `RopeMaterialTests.TheCompiledRopeSpanCarriesItsDeclaredMaterial` | a steel cable span in the same scene carries its own values | a mixed-material route is refused (EL-067 row K7) |
| A7 (planned, ENGINE-CONNECTION-MATERIALS, solid strand) | Given a taut horizontal 3 m rope between two Rope anchors, when a Tennis ball is dropped 1 m onto its middle, then the ball is stopped or deflected by the strand and never ends below it within 3 s | `RopeMaterialTests.ABallDroppedOnARopeDoesNotPassThroughIt` | the same ball dropped 0.5 m beside the rope falls past it | a slack rope lying on the bench is also met as a solid; ball centre never more than 0.01 m below the strand's top surface while over it, the visible-solid failure owner decision 1 excludes |

The requirement's collaborators are all delivered: Rope anchor, Weight, Pulley, Impact lever (`TieA`, `TieB`) and Trampoline (`TrampolineTests.ATautTetherHoldsTheWeightAboveTheBedAndASlackOneLetsItRebound`, `CuriousContraptions.tests/TrampolineTests.cs:2112`). EL-184 Mechanical ball gate and EL-207 Moving pulley are planned and prove their own rope rows. When the strand becomes solid, the delivered rope lessons and the Rope anchor, Pulley, Impact lever and Trampoline rope tests are re-run in the same change (the strand touching a pulley wheel, a lever beam and a trampoline frame are the probe points of `readiness/batch-c.md` section 4).

### Integration

- **Works with.** Delivered: `rope_anchor`, `weight`, `pulley`, `impact_lever`, `trampoline`. Planned: EL-207, EL-184, EL-064, EL-065, EL-066, EL-067, EL-068, EL-069, EL-070.
- **Campaign.** Row 2 of [campaign element coverage](../requirements.md#campaign-element-coverage): first use 11–20, reuse 31–50, 61–80, 136–150. Planned authored levels 19 `counterweight` and 20 `pulley_depth` (plan levels 12b, where the Rope is first taught, and 12c) use it and are in the "Ropes" group of [Early level proof](../invest/delivery-batches.md#4-early-level-proof); their proof is re-run when the strand becomes solid.
- **Lesson fixtures (S8).** Not needed: a rope link is ordinary authored content.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (`origin/main`, `25df3cd5`; pattern EL-239 Baseball). That section is not written here: the reader parses every spec that carries the heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `rope` (a non-placeable material entry) |
| Named identity | EL-205, `../invest/named-elements.md#element-205` |
| Acceptance | `sequence-task-758`, co-anchored with `element-205` in `requirements.md`; the ledger entry links only the shared `campaign-element-coverage` scope |
| Decision | S307 |
| Mode records | `Configuration=Fixed` |

What it lacks today: the ledger entry has no todo link of its own, `scope-corrections.md` has no `s307` block, and the material entry must be in the generated bundle (ENGINE-CONNECTION-MATERIALS). S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

### Delivery

- **Shape.** Family slice "Rope materials" with EL-067: `rope` and `steel_cable`, each with its own rows. If ENGINE-CONNECTION-MATERIALS converts the delivered rope and runs A6 and A7 itself, this identity is closed by that story's record with the ledger citing it.
- **Later row owned here (coordinator decisions 36 and 41).** [EL-115](EL-115-linkage-connector.md) row L4 (`PinEndedMemberTests.ARopeOfTheSameLengthCannotPush`): in the push rig, a rope of the link's length in place of the link leaves lever 2 where it is. It is a cross-part row owned by this slice, because the Rope and the Weight that loads the rig are taught at 12b, after the linkage at 9; EL-115 keeps its Given, When and Then.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES (strand colliders as query candidates) and ENGINE-ISLANDS-SLEEP; then ENGINE-CONNECTION-MATERIALS (solid strand), which converts the delivered rope in the same change; then S7 (the coverage follow-on, which widens the reader, owner decision 15). The visible-solid repair of the Rope anchor eye and Pulley post and rim ([part A](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md)) are residuals of CAT-058 and CAT-053, not prerequisites.
- **Files.** `docs/planning/invest/named-elements.md` (the `element-205` entry: replace the "Current candidate" paragraph with the Family slice citation); the material entry and tests the story defines; at most `CuriousContraptions.tests/RopeTests.cs` for A5; the review record.
- **Review tier.** Engine, inside ENGINE-CONNECTION-MATERIALS or the Family slice after it.
- **Disqualified if** the reading finds any ledger criterion with no executed test through the delivered packages after the conversion, or a strand collider changes a delivered rope lesson's outcome without a recorded reason: then the row stays open and the missing behaviour is a slice.

## 6. Open questions

**Resolved.**

1. Part or connection: a connection (delivered), carried as a declared material, not a catalogue part.
2. Family with EL-067: yes. Re-decided 22 November 2026: the coordinator's disposition "declared rope material" ([batch-i summary](readiness/batch-i.md) section 3) stands, and the earlier "no" given under the delegation is withdrawn, because the rope now has a delta (owner decision 1 and the material conversion).
3. Length editor: none (coordinator, 30 October 2026, below).
4. The rope is solid. Owner decision 1, 22 November 2026: ropes, cables, hoses and ducts get colliders; thin wires and the Balloon string are the only non-solid strands. Its collider is a property of the declared connection material (ENGINE-CONNECTION-MATERIALS, solid strand); the delivered rope's missing collider is a recorded visible-solid defect owned by that story.

**Needs the owner.**

None.

Resolved for10.2b by the coordinator,30October2026: massless inextensible/tension-only gameplay uses shared numerical regularization, not an elastic-energy store. Length is measured at connect time within0.05–200m, retained through moves/mass edits, and remeasured on reconnect; no length editor. The complete [CAT-058 decisions](CAT-058-rope_anchor.md#6-open-questions) and [implementation contract](../../../_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md) govern cutting, committed state/art and Reset/save. Pulley integration and comprehensive later qualification retain their owners.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026. The rope is a connection, so there is no package to open; the engine's rope code is the general tensile constraint and was read as such.

1. **Declaration data only.** A Rope-domain connection between typed ports with attachment roles Anchor, Load or Guide, one stored value (the length measured at connect time) and, after ENGINE-CONNECTION-MATERIALS, one typed material id whose record holds stiffness, damping, d, R and strand art. No rope rule is attached to any part.
2. **Generic laws.** The tension-only length constraint over a route with guide geometry (`CuriousContraptions.Simulation/TensileConstraintSolver.cs`), a commanded cut that removes the constraint at a step boundary, the one cutting law for blade pairs (ENGINE-TENSILE-EXTENSIONS, cutting clause), and ordinary contact against the strand collider. Slack does nothing; taut carries load.
3. **Three layers.** Colliders: today none, a recorded visible-solid defect; after ENGINE-CONNECTION-MATERIALS, the strand collider generated from the declared diameter and segment rule along the committed route (owner decision 1). Visuals: the strand drawn by the connection's presentation, never used as a collider, at the declared diameter. Animation: the strand art follows the committed route geometry and writes nothing back.
4. **Flow and typing.** `WorkshopConnectionDomain.Rope` and `RopeAttachmentRole` are closed enums; the material is a typed id; the length is an f32 in metres within 0.05 to 200 m.
5. **Flat state.** Per rope: one tensile row with its two ends and its guides, fixed at compile time; one material id; the strand colliders the segment rule fixes at compile; no body, sensor or store.
6. **Performance.** One constraint row per rope; the route geometry is evaluated from the guide poses each step; strand colliders are bounded by compiled declarations and found through ENGINE-SPATIAL-QUERIES. No island sleeping exists today; a resting rope and its loads cost nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this identity.
7. **Tests are scenarios.** The cited rows run the production simulation through delivered anchors, weights and pulleys; A6 and A7 run it on the converted material.

**Verdict: conforms with fixes: the solid strand and the material conversion, both owned by ENGINE-CONNECTION-MATERIALS (stated element-free).**
