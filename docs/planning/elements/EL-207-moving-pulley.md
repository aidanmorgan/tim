# EL-207 · Moving pulley named-identity spec

**Current delivery (24 October 2026):** [EL-207 delivery slice](../invest/named-elements.md#element-207). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-207 |
| Name | Moving pulley |
| Type | Mechanical |
| Anchor | [requirements.md#element-207](../requirements.md#element-207); [named entry](../invest/named-elements.md#element-207); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("fixed/moving pulley roles") |
| Proof owner | S309 |
| Refines / extends | Extends [CAT-053 pulley](CAT-053-pulley.md) (its open questions ask whether this is a variant or a separate element); rope law from [CAT-058](CAT-058-rope_anchor.md) |
| Related | EL-205 Rope, EL-206 Fixed pulley, CAT-067 Weight, EL-204 Weight |
| Roadmap story | [EL-207 delivery slice](../invest/named-elements.md#element-207); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP; then ENGINE-TENSILE-EXTENSIONS (dynamic-guide clause) and ENGINE-CONNECTION-MATERIALS (solid strand); then S7. |

### Disposition (21 November 2026)

**To deliver as a single element**, not a variant or duplicate of CAT-053: it has a dynamic body, mass and a second port, none of which the Pulley has (D8: its own catalogue entry). Planned package id `moving_pulley`, directory `elements/MovingPulley/`, bound to this identity by this spec's final package-source-binding section once S7 widens that binding's preconditions (section 7); no ledger line binds it. Owner key S309. It is listed in the "Tensile" run of [delivery batches 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only) as needing a probe for "a dynamic pulley"; the probe is done here and it fails: see section 3.

## 2. Declaration

### Declaration to write (21 November 2026)

Closest delivered shapes: `elements/Pulley/PulleyElementDeclaration.cs` (guide port, wheel binding) and `elements/Weight/WeightElementDeclaration.cs` (dynamic body with several colliders and a Load port).

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Dynamic`, `PayloadPolicy.None`, `SupportedDomains` Mechanical and Rope.
- **Body.** One dynamic body, origin at the sheave centre, two scene-owned colliders:
  - sheave: cylinder of radius 0.40 m and width 0.12 m, axis on local Z (the cylinder shape's own axis is local Y, so the proxy is turned 90° about X as the Motor rotor is);
  - hook block: box 0.30 × 0.20 × 0.20 m (full extents) centred at (0, −0.55, 0), touching the sheave's lowest point.
- **Mass and inertia.** 0.5 kg, declared constant (`MassFrameDeclaration.Constant`), centre of mass at the origin. Principal moments of the solid disc, as the Motor declares its rotor (`elements/Motor/MotorElementDeclaration.cs:104`–`:111`): axial m·r²/2 = 0.0400 kg·m²; transverse m·(3r² + w²)/12 = 0.0206 kg·m². The hook block's contribution is neglected and stated as such; gravity (0, −9.81, 0) m/s²; linear drag 0. Reason for 0.5 kg: light beside the 0.25 to 8 kg Weights so the two-to-one ratio reads, heavy enough to hang straight.
- **Material.** Restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (a dead contact if a load swings into it).
- **Ports.**

  | `PortId` | Domain | Direction | `PortRole` | Attachment role | Local position (m) | Label |
  | --- | --- | --- | --- | --- | --- | --- |
  | `TieA` | Rope | Bidirectional | `TieA` | Guide, `GuideRadius` 0.4, `GuideOffset` (0, 0, 0) | (0, −0.4, 0), normal −Y | "Rope" |
  | `TieB` | Rope | Bidirectional | `TieB` | Load | (0, −0.65, 0), normal −Y | "Hook" |

  `PortRole` is a closed enum with `Tie`, `TieA`, `TieB` (`CuriousContraptions.Abstractions/PortDescriptor.cs:30`–`:32`), so the load port is `PortRole.TieB` with `RopeAttachmentRole.Load` and the package-owned label "Hook", as the Impact lever declares its ties (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:45`, `:56`). The groove is in the body's own plane (offset zero), unlike the wall-mounted Pulley's 0.12 m: a hanging block with an offset groove would be turned by its own rope.
- **Parameters.** None (D13). **Stores, events.** None.
- **Presentation.** One binding, as the Pulley: `Visual/Wheel`, `LocalRotationZ`, driver `CommittedWheelAngle`, `GuidePort: PortId.TieA`. Pick radius 0.65 m.
- **Catalogue.** Id `moving_pulley`, title "Moving pulley", description "A pulley that rides on its rope and carries a load on its hook.", category `ElementCategory.Motion` (the Pulley's; there is no Ropes category), palette order 107 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour the Pulley's `#d69c47`.
- **Footprint.** Bodies 1, moving bodies 1, no constraints.

### Art and solidity

| Node | Mesh | Class | Collider or reason |
| --- | --- | --- | --- |
| `Visual/Wheel/Visual_WheelBody` | cylinder r 0.40 m, width 0.12 m, `#d69c47` | solid | sheave cylinder |
| `Visual/Wheel/Visual_Spoke`, `Visual_Marker` | navy `#293954` bar and gold `#f7cb52` dot, flush on the wheel face | decorative | moved by a binding (the wheel-angle binding), and drawn flush |
| `Visual/Visual_Block` | cream `#fff8e9` box 0.30 × 0.20 × 0.20 m | solid | hook block box |
| `Visual/Visual_Hook` | gold closed eye below the block: a full ring of ring radius 0.06 m and tube radius 0.015 m, as the Weight's eye (`elements/Weight/weight.tscn:34`, `:66`) | solid | a Torus collider proxy of the same ring, as the Weight's `col_visual_eye` (`weight.tscn:91`). Decided now (22 November 2026, audit L8): a closed eye, not an open hook, so nothing can snag in an opening |

No rim torus is drawn (the Pulley's rim is a recorded defect). Icon: the wheel with a hook below it and two rope legs going up. At a glance it differs from the Pulley by shape: no wall mount or post, a block and hook underneath.

### Harvest-era subsections — removed; the text is in git history.

## 3. Engine capabilities

Families: EnvironmentState, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

### Capability probe (21 November 2026)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Dynamic body with several scene colliders (cylinder, box, torus) and declared mass and moments | present | `IMechanicalElement.MassFrame`; `elements/Weight/WeightElementDeclaration.cs:96` | `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`CuriousContraptions.tests/WeightTests.cs:139`) |
| A Guide port and a Load port on one **dynamic** body (two rope ports on one hinge body are delivered) | present, untested | `elements/ImpactLever/ImpactLeverElementDeclaration.cs:38`–`:62` (`TieA`, `TieB`) | `RopeGuideTests.MultipleGuidePortsOnSameBodyHaveDistinctIdsAndPublishBothObservations` (`CuriousContraptions.tests/RopeGuideTests.cs:414`) covers two guide ports, and `ImpactLeverTests` two load ties; no test puts a guide and a load on one free body |
| Guide geometry that follows the guide body's committed pose | present | `CuriousContraptions.Simulation/TensileConstraintSolver.cs:34`–`:37` reads the guide body's position and rotation each step | `RopeGuideTests.DynamicSimulationRotatesDistinctGuidesInInverseProportionToRadius` (`:109`), with static guide bodies only |
| A guide port on a **dynamic** body is admitted | present, untested | `ValidateRopeGuides` requires only an admitted body (`engine/gpu/PhysicsDeclarations.cs:254`–`:263`) | every guide body in `RopeTests` and `RopeGuideTests` is static |
| **The route's reaction on the guide body** | **missing** | `TensileConstraintSolver.Apply` applies the impulse to the two end bodies only (`TensileConstraintSolver.cs:66`–`:71`), and the row's effective mass counts only those two (`:58`). A dynamic guide would shape the route but feel no rope force and fall | ENGINE-TENSILE-EXTENSIONS (dynamic-guide clause; owner decision 3a) |
| Wheel-angle read and binding for a guide port | present | `PresentationDriver.CommittedWheelAngle` (`CuriousContraptions.Abstractions/ElementPresentation.cs:25`) | `RopeTests.WorkshopDeclaredPresentationAdvancesPulleyWheelRotationAngle` (`CuriousContraptions.tests/RopeTests.cs:967`) |
| Connected-component membership of guide bodies | present, untested for a dynamic guide | `ContinuousWorkComponents.JoinRoute` joins each guide body (`CuriousContraptions.Simulation/ContinuousWorkComponents.cs:70`–`:77`) | none found |

Counts: 3 present, 3 present-untested, 0 narrower, 1 missing. What the engine slice must deliver, as a law and not as a pulley: for each guide of a taut route the tension acts on the guide body along the sum of the two unit tangents at its entry and exit points, applied at those points; the row's effective mass includes every dynamic guide body; a static guide is unchanged; an isolated system of two loads and a dynamic guide gains no momentum or energy. Nothing in this spec needs element-specific engine code.

**Dependencies.** EL-205 rope and EL-206 fixed pulley (Story 10.2); CAT-067 weight (Story 10.4).

## 4. Sources and legacy

- Requirement: "Load-attached sheave moves with the routed rope constraints and shared reaction forces"; outcome "Its displacement/work ratio follows routing rather than a named pulley bonus" ([element-207](../requirements.md#element-207)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The legacy pulley is fixed with "no moving-block ratio or wheel inertia"; moving-block ratios were future work | `parts/PulleyPart.cs@a6c914e:L6-L6`; `reference/P0-022-before/source-README.md@a6c914e:L99-L99` | carry forward (as gap) | Confirms no legacy moving pulley. |
| 2 | Accepted: a dynamic guide and both loads react in one solve; with unit masses the velocities are −7/3, −4/3 and −1/3 (total momentum conserved) and the guide gains no angular momentum; route length stays 4 | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L18-L31` | carry forward (behaviour), do not carry forward (point-guide numbers) | Shared reactions without a bonus. |
| 3 | Accepted: a fixed guide transfers tension but never pushes | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L33-L45` | carry forward | Tension-only through guides. |

**Files harvested:**
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- `parts/PulleyPart.cs`
- `engine/MachineData.cs` (pulley geometry constants)
- `engine/RopeNetwork.cs` (guide role)
- `parts/WeightPart.cs` (load mass range)
- `reference/P0-022-before/source-README.md` (future-work note)
- Searched with no hit for `moving pulley` or a moving-block implementation (`moving-block` appears only in the two notes above): `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-207](../requirements.md#element-207) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place an anchor and a fixed pulley high; hang the moving pulley from a rope running anchor → under moving pulley → over fixed pulley → counterweight; hang a 4 kg Weight from the moving pulley's `TieB` (labelled "Hook"). Run.
- **Positive.** A 2.25 kg counterweight balances the 4 kg load plus the 0.5 kg block; a heavier one lifts it, and pulling the free end down 1 m raises the load 0.5 m.
- **Negative/control.** Replacing the moving pulley with a fixed pulley gives 1:1 travel; a slack route moves nothing; the block's work in equals work out.
- **Boundaries.** Block mass included in balance; swinging load; route ending at the moving pulley (Open) refused to carry tension.
- **Run/Reset.** Block, loads and lengths restored.
- **Save/Load.** Both rope routes and lengths round-trip.
- **Integrations.** Retained rope behaviour [todo-140](../requirements.md#todo-140) and qualification [todo-141](../requirements.md#todo-141); campaign row 2 ("... fixed/moving pulley roles ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-206 and CAT-067 weights.

### Behaviour and numbers (21 November 2026)

- **Law.** The rope is one tension-only length row (EL-205). Passing under the sheave, both rope legs pull the block up, so the block and its load are carried by twice the rope tension T: at balance 2T = (m_load + 0.5 kg)·g, and the free end needs T. With the route's length fixed, a free-end travel of s moves the block s/2. Work in equals work out plus the block's own energy change; there is no bonus term anywhere.
- **Check values.** A 4 kg Weight on the hook balances a 2.25 kg counterweight (inside the Weight's 0.25 to 8 kg range, `elements/Weight/WeightElementDeclaration.cs:33`). A 4 kg counterweight accelerates at g·(4 − 2.25)/(4 + 4.5/4) = 3.35 m/s² at the free end, so 1 m of travel takes about 0.77 s: a visible, unhurried lift.
- **Geometry.** Legs leave the 0.4 m groove 0.8 m apart, the Pulley's groove diameter, so a Pulley placed 0.8 m to the side of an anchor gives two vertical legs.
- No signal, optical or acoustic read is involved; tick order is the tensile solve's.

### Acceptance rows (21 November 2026)

Test class `MovingPulleyTests`, through the Workshop integration harness with the delivered `rope_anchor`, `pulley` and `weight`. Tolerances are chosen from the gameplay consequence: 0.03 m on a 0.5 m lift (a player reads "half as far", and 6 % is far from the 1:1 control's 0.5 m error); 0.02 m of drift for "balanced" over 5 s; the energy envelope and resting envelope reuse the constants of `TrampolineTests` and `RopeTests`. Stall guards are 10 game seconds unless stated.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given anchor → under the moving pulley → over a fixed pulley → a 2.25 kg counterweight, and a 4 kg Weight on the hook, when Run starts, then block and counterweight stay within 0.02 m for 5 s | `MovingPulleyTests.ALoadIsBalancedByHalfItsWeightPlusHalfTheBlock` | a 2.0 kg counterweight lets the load descend more than 0.3 m; a 2.5 kg one raises it more than 0.3 m | the block's 0.5 kg is in the balance: a 2.0 kg counterweight (half the load alone) does not hold |
| A2 | Given a 4 kg counterweight, when it has descended 1.0 m, then the block has risen 0.50 m ± 0.03 m | `MovingPulleyTests.TheLoadRisesHalfTheFreeEndTravel` | the same rig with a fixed Pulley in place of the block and the load on the rope end rises 1.0 m ± 0.03 m | route length never exceeds its authored length by more than 0.01 m |
| A3 | Over the same run, total mechanical energy never exceeds its initial value by more than the energy envelope | `MovingPulleyTests.NoTickGainsEnergyThroughTheBlock` | — | 0.25 kg and 8 kg loads |
| A4 | Given a slack route, when Run starts, then the block falls as a free body (within 0.005 m after 30 ticks) | `MovingPulleyTests.ASlackRouteLeavesTheBlockInFreeFall` | the taut route holds it | a route that ends at `TieA` is open and carries nothing |
| A5 | Given the load released from 30° off vertical, when it swings for 10 s, then the block stays on its rope and the load stays tethered | `MovingPulleyTests.ASwingingLoadStaysTethered` | — | — |
| A6 | Reset restores block, loads, lengths and wheel angle; save and load round-trip both routes | `MovingPulleyTests.ResetAndSaveLoadRestoreBothRoutes` | — | — |
| A7 | A second rope end on `TieB`, a third span on `TieA`, or an electrical wire to either port is refused and changes nothing | conformance suite row 5 (S4) once it lands; until then `MovingPulleyTests.ForeignAndExtraConnectionsAreRefused` | a valid rope is accepted | — |

Parameter refusal does not apply. The requirement outcome "displacement/work ratio follows routing rather than a named pulley bonus" is A2 with its control and A3.

### Integration

- **Works with.** Delivered: rope connection, `rope_anchor`, `pulley`, `weight`. Planned: EL-065 Load hook, EL-069 Moving bucket, EL-070 Moving cage (same "Tensile" run; EL-069 and EL-070 also hang a dynamic body in a route and should be probed against the same engine slice).
- **Campaign.** Row 2 of [campaign element coverage](../requirements.md#campaign-element-coverage): "moving/load variants extend into 41–50", reuse 61–80 and 136–150. No level in `content/puzzles.json` places it today.
- **Coverage binding.** A final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).
- **Lesson fixtures (S8).** Needed only if a lesson locks the anchor and fixed pulley as fixtures; the part itself is ordinary inventory.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP; then ENGINE-TENSILE-EXTENSIONS (dynamic-guide clause) and ENGINE-CONNECTION-MATERIALS (solid strand); then S7 (the coverage follow-on, which widens the reader, owner decision 15).
- **The rope is a solid strand (owner decision 1).** Its collider is a property of the declared connection material (declared diameter, segment rule), compiled from declarations. Blocked on ENGINE-CONNECTION-MATERIALS (solid strand). The delivered rope draws a thick strand with no collider today: a recorded visible-solid defect owned by that story. The slice's probe re-checks that the strand's collider, wrapped under the sheave and leaving the eye, does not snag the block, the eye or the wheel.

1. **ENGINE-TENSILE-EXTENSIONS, dynamic-guide clause** (engine tier, six layers; owner decision 3a): the guide reaction law above, proven on two differently parameterised declared fixtures (two radii and masses), with a static-guide regression control. It shares nothing with player-made joints.
2. **Element slice** (single element). After registration derivation and S3 it adds `elements/MovingPulley/` (`MovingPulley.csproj`, `MovingPulleyElementDeclaration.cs`, `moving_pulley.tscn`, `icon.svg`), `CuriousContraptions.tests/MovingPulleyTests.cs`, one line in `CuriousContraptions.slnx`, the art classification entries once ENGINE-ART-SOLIDITY lands, and the binding section of section 7 once S7 admits it (the test project compiles every top-level `*.cs` by one rule). Expected tier: data-only if the engine slice's tests leave every row above *present*; the two *present, untested* rows make it engine tier unless the engine slice exercises a dynamic guide end to end, which it must.
- **Disqualified if** the slice's probe finds the route geometry needs the groove offset for a hanging block, a torus hook collider that the rope solver or contact cannot carry, or that EL-069/EL-070 need a different guide law (then decide the law once for all three).

## 6. Open questions

**Resolved.**

1. Variant or separate part: a separate catalogue part (D8; it has a body, mass and port the Pulley lacks).
2. Two rope ports or a rigid hook joint: two rope ports, `TieA` (guide) and `TieB` (load). Decided under the delegation: a rigid joint to the load would need a player-made joint (ENGINE-ASSEMBLY-JOINTS) for no gameplay gain, and a rope from the hook is what the Weight already accepts.
3. Groove offset: zero for this part (reason in section 2).
4. The hook is a closed torus eye with its own collider (22 November 2026, audit L8).

**Needs the owner.** None. The engine story's owner and name are decided (owner decision 3a).

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One dynamic body, three colliders, two typed rope ports and one wheel-angle binding. There is no pulley rule, mechanical-advantage constant or engine test on this package.
2. **Generic laws.** The tension-only route constraint with guide geometry, rigid-body dynamics and gravity. The two-to-one travel is a consequence of the route. The one missing piece is general and stated element-free: a taut route's reaction on every dynamic guide body, with those bodies in the row's effective mass. One owner, ENGINE-TENSILE-EXTENSIONS (dynamic-guide clause).
3. **Three layers.** Colliders: a cylinder (sheave), a box (block) and a Torus ring (eye) on the one body. Visuals: wheel, block and eye meshes; spoke and marker drawn flush on the wheel face; none is a collider. Animation: `Visual/Wheel` is turned by the `CommittedWheelAngle` binding of its guide port, which reads the committed route and writes nothing back; the wheel's collider is a cylinder about the same axis, so the turning art and the collider coincide. Block and eye follow the body's pose. The ropes are their connections' own drawings, and each solid strand's collider belongs to the connection material (ENGINE-CONNECTION-MATERIALS).
4. **Flow and typing.** Two Rope-domain ports with closed `PortRole` values `TieA` and `TieB` and attachment roles Guide and Load; no parameters.
5. **Flat state.** Bodies 1 (dynamic); colliders 3 (cylinder 1, box 1, Torus 1); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (wheel angle).
6. **Performance.** Three colliders, as the Weight. No island sleeping exists today; a resting block costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. ENGINE-TENSILE-EXTENSIONS must add the guide bodies to the existing tensile row, not a second solver pass per guide.
7. **Tests are scenarios.** The rows run the production simulation with delivered anchors, pulleys and weights and prove the route law for this declaration.

**Verdict: conforms; blocked on a general tensile capability with one owner, ENGINE-TENSILE-EXTENSIONS.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `moving_pulley` |
| Named identity | EL-207, `../invest/named-elements.md#element-207` |
| Acceptance | none yet: the ledger entry cites `campaign-element-coverage`, not a todo and sequence-task pair |
| Decision | S309, `../invest/scope-corrections.md#s309` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s309` block on published main; and the ledger entry carries no `todo-` link, which the reader requires exactly once, with a sequence task co-anchored to it.
