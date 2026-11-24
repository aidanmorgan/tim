# EL-216 · Capture cradle named-identity spec

**Current delivery (24 October 2026):** [EL-216 delivery slice](../invest/named-elements.md#element-216). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-216 |
| Name | Capture cradle |
| Type | Mechanical |
| Anchor | [requirements.md#element-216](../requirements.md#element-216); [named entry](../invest/named-elements.md#element-216); campaign 1–10, practice 41–50, reuse 96 and 149 ([campaign reservations](../invest/campaign-reservations.md)) |
| Proof owner | S382 |
| Refines / extends | No CAT spec. Box-wall cup like [CAT-004 basket](CAT-004-basket.md) but without the receiver's capture sensor or guiding force; released through a separately placed gate of the [CAT-051 powered gate](CAT-051-powered_gate.md) / [EL-184](EL-184-mechanical-ball-gate.md) slider family, sized to the cup |
| Related | EL-215 Damped cushion (same campaign slots), EL-184 Mechanical ball gate |
| Roadmap story | [EL-216 delivery slice](../invest/named-elements.md#element-216); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP (cargo resting in the cup); then S7. One capability lane to confirm (section 3). |

### Disposition (21 November 2026)

**To deliver as a single element** (the "Static geometry" run of [delivery batches 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only)). It is not a duplicate of CAT-004 Receiver: it has no capture sensor, guide or event, and it adds an outlet. Planned package id `cradle` (kept from this spec), directory `elements/Cradle/`, bound by this spec's final package-source-binding section once S7 widens that binding's preconditions (section 7); no ledger line binds it. Owner key S382.

## 2. Declaration

### Declaration to write (21 November 2026)

This subsection is the whole declaration; the harvest-era subsections that followed it are reduced to a stub below (their text is in git history). Closest delivered shapes: `elements/Receiver/ReceiverElementDeclaration.cs` (a static open box of scene-owned walls) without its sensor, guide and event, and `elements/Wall/WallElementDeclaration.cs` (collider and art bound to a real parameter, `:40`–`:45`).

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:29`). **Not** `IResidenceSensorElement` or `IPlanarGuideElement`: no capture radius, no guiding force, no `Captured` event. Static, zero mass frame, no ports, no stores, no events, Mechanical domain.
- **Body and colliders (one static body, scene-owned boxes).** Origin at the centre of the floor's top face; +X is the outlet side.
  - floor: 0.78 × 0.15 × 0.78 m, turned 5° about Z so it falls toward +X (top face at y −0.034 at the outlet end and +0.034 at the back);
  - back wall at x = −0.35: 0.08 × 0.36 × 0.62 m, from y −0.06 to +0.30;
  - two side walls at z = ±0.35: 0.78 × 0.36 × 0.08 m, from y −0.06 to +0.30;
  - outlet panel at x = +0.35: 0.08 m thick, 0.36 m tall (y −0.06 to +0.30), its length along Z bound to `outlet_width`.
  The wall tops are level at y = +0.30 and enclose an inner square of 0.62 m.
- **Parameter.**

  | Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `outlet_width` | Real (f32) | `Metre` | 0 | 0 | 0.62 | Reject | BuildOnly |

  Collider bindings on the outlet panel: `HalfExtentZ` = 0.33 − 0.5·`outlet_width`; `OffsetZ` = −0.02 − 0.5·`outlet_width`. The panel therefore runs from z = −0.35, inside the −Z side wall, to z = 0.31 − `outlet_width`. At 0 it closes the cup. At 0.62 it is a 0.04 m block wholly inside the side wall, and the doorway is the full 0.62 m inner width and the full height of the wall: there is no jamb for a ball to catch (repair of 22 November 2026, audit M26; the draft's doorway ran from z −0.29 to +0.31 and left a ball 0.01 m on the stub side). Art binding: `Visual/Visual_Outlet`, `ScaleZ` = 1 − `outlet_width`/0.66, the node's origin at the panel's −Z end.

  **Correction to the baseline:** the baseline's `outlet` Choice of Closed or SideGate cannot be declared. A collider binding takes a bounded real parameter and drives a size or offset (`ColliderParameterBinding`, `SceneGeometry.cs:24`–`:27`); nothing makes a collider present or absent by a choice. A real doorway width does the same job with a delivered capability and gives the player one more honest control.
- **Material.** Restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s: a gentle landing settles, an energetic one still leaves.
- **Presentation.** No bindings beyond the parameter binding. Pick radius 0.6 m.
- **Catalogue.** Id `cradle`, title "Capture cradle", description "A cup that holds what settles in it. Open its side and a gate can let the cargo out.", category `ElementCategory.Motion` (it is not a goal; the Receiver is `Goals`), palette order 105 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour Basket green `#4aab94`.
- **Footprint.** Bodies 1, moving bodies 0.
- **Release gate.** No new part. The delivered Powered gate (`powered_gate`) is the release. It is an in-pipe conduit, not a free blade: a sleeve of outer radius 0.70 m and bore radius 0.65 m, 0.8 m long on its X axis, with collars of outer radius 0.78 m at each end (mouths at x = ±0.49) and the blade sliding up across the bore at its middle (`elements/PoweredGate/powered_gate.tscn:27-39`, `:128-150`). The cup cannot sit in the bore, so the gate stands beside the cup and the ball waits in the gate's mouth; the arrangement and its arithmetic are in section 5. The baseline's "cup-scale gate" is dropped (decision in section 6).

### Art and solidity

| Node | Mesh | Class | Collider or reason |
| --- | --- | --- | --- |
| `Visual/Visual_Floor` | navy `#293954` slab 0.78 × 0.15 × 0.78 m, sloped 5° | solid | floor box |
| `Visual/Visual_Back`, `Visual_SideL`, `Visual_SideR` | `#4aab94` walls with a cream `#fff8e9` top edge drawn flush | solid | the three wall boxes |
| `Visual/Visual_Outlet` | `#4aab94` panel scaled by the parameter binding | solid | the outlet panel box (both follow `outlet_width`) |

Nothing is decorative and nothing glows: there is no halo, which is the at-a-glance difference from the Receiver, together with the sloped floor and the doorway. Icon: a square cup seen from above the rim with a ball seated in it and a gap in one wall.

### Harvest-era subsections (Bodies and shapes to Variants)

Superseded by the current declaration (section 2: a 0.15 m floor at 5°, the real `outlet_width` instead of an `outlet` Choice, the delivered Powered gate beside the cup instead of a cup-scale blade); the harvest text is in git history.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

### Capability probe (21 November 2026)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static body of several scene-owned box colliders, including turned ones | present | `ISceneGeometryElement.SceneBodies` (`SceneGeometry.cs:8`–`:13`) | `PoweredGateTests.PoweredGateScene_ExtractsItsSleeveCollarsFrameSocketsAndThreePortLocators` (`CuriousContraptions.tests/PoweredGateTests.cs:56`); `FunnelTests.WorkshopPhysicsCompiler_CompilesFunnelIntoStaticBodyWithItsHollowColliders` (`CuriousContraptions.tests/FunnelTests.cs:104`) |
| A real parameter drives a collider half extent and the art's scale | present | `ColliderParameterBinding` (`SceneGeometry.cs:27`); `elements/Wall/WallElementDeclaration.cs:11`–`:13`, `:40`–`:45` | `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits` (`CuriousContraptions.tests/WorkshopWallTests.cs:15`); `WorkshopWallTests.RealSimulationRotatedWallBlocksMovingBallWhileAbsentWallDoesNot` (`:88`) |
| A real parameter drives a collider **offset** | present | `GeometryComponent` (`SceneGeometry.cs:24`); `elements/Ramp/RampElementDeclaration.cs:46`, `:49` bind `OffsetZ`; `elements/Weight/WeightElementDeclaration.cs:62` binds `OffsetY` | `VisibleSolidColliderTests` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:935`, `:982`–`:1002`) runs the Ramp's bound `OffsetZ`; `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`CuriousContraptions.tests/WeightTests.cs:139`) covers `OffsetY` |
| A parameter range whose minimum is 0 | present | `ParameterDescriptor` | the Receiver's `capture_margin` (`elements/Receiver/ReceiverElementDeclaration.cs:44`) and its suite |
| Sphere against box contact, friction, restitution and rest in a static cup | present | shared contact law | the Receiver and Funnel suites; `TrampolineTests.ARimStrikeIsNeverLaunchedAndTheBedBearsTheBallOnlyOverTheBed` for rim strikes |
| A separately placed supplied gate that holds a ball while closed and passes it on supply | present | `elements/PoweredGate/PoweredGateElementDeclaration.cs` | `DeclaredPrismaticDriveTests.ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun` (`CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs:403`): a ball at rest against the closed blade of a tilted gate passes when it is supplied |
| Choice parameter that adds or removes a collider | not used | none exists | not needed after the correction in section 2 |

Counts: 6 present, 0 present-untested, 0 narrower, 0 missing. Every capability is delivered with a test, so the slice is data-only.

**Harvest-era lists (Exists now, Missing).** Superseded by the capability probe above; the harvest text is in git history.

**Dependencies.** None undelivered: the release is the delivered Powered gate (row A5).

## 4. Sources and legacy

- Requirement: "A physical retaining cup supports cargo after contact without remote ownership transfer; a separately placed/taught gate can release it"; outcome "Oversize or energetic cargo can escape; no invisible capture radius or forced attachment" ([element-216](../requirements.md#element-216)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Receiver walls as static boxes (floor half (0.75, 0.075, 0.75) at y −0.45; walls 0.5 m half-height) | `engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18` | carry forward (construction pattern), not the sizes | Static box cup. |
| 2 | Receiver capture: margin 0.02, speed limit 1.5 m/s, dwell 0.35 s; planar guide force region | `engine/gpu/WorkshopConstruction.cs@a6c914e:L79-L101` | do not carry forward | A capture sensor and guiding force would be the "invisible capture" the cradle forbids. |
| 3 | Powered gate blade half extents (0.06, 0.72, 0.72), stroke 1.55 m, slider controller | `parts/PoweredGatePart.cs@a6c914e:L15-L22`; `engine/SlidingBlade.cs@a6c914e:L12-L34` | carry forward (slider family), not the sizes | Release-gate mechanism at cup scale. |
| 4 | Accepted: a horizontal gate supports settled cargo while closed and releases it when opened | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L79-L117` | carry forward | Release by a separate gate. |

**Files harvested:**
- `parts/BasketPart.cs` (halo art contrast)
- `parts/PoweredGatePart.cs` (gate blade size)
- `engine/SlidingBlade.cs` (slider gate family)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- Searched with no hit for `cradle`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-216](../requirements.md#element-216) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe (pending, post-catalogue).** Place the cradle under a ramp exit; set `outlet_width` 0.62 and place a Powered gate beside the doorway as in the gate arrangement below, wired to a battery and switch. Run.
- **Outcomes, controls and boundaries** are rows A1–A7 below. Save/Load round-trips pose and `outlet_width`, and Reset restores the cargo and the separately placed gate (A7).
- **Integrations.** Element reservations 1–10, practice 41–50, reuse 96 and 149 ([element-216](../requirements.md#element-216)); campaign row 1 ("cushion/cradle") first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-215 cushion and a CAT-051/EL-184 gate.

### Behaviour and numbers (21 November 2026)

- **Law.** Contact, friction and gravity only. Cargo is never attached, guided or owned by the cradle.
- **Who fits.** Inner square 0.62 m. The Bowling ball (0.56 m across, 4 kg) and the Tennis ball (0.50 m) seat on the floor; the Basketball (0.68 m, `TrampolineTests` uses radius 0.34) cannot pass between the walls and perches on the rim, from which a nudge removes it. That is the requirement's oversize control with delivered parts.
- **Lip.** The wall tops are 0.30 m above the origin. A Bowling ball seated against the closed outlet panel (centre at x 0.03, where the floor top is at y −0.003) has its centre at about y 0.28, below the rim, so it stays; a ball arriving across the rim at speed strikes the far wall near its top and can leave.
- **Floor slope.** 5° toward the outlet: a rolling sphere accelerates at (5/7)·g·sin 5° = 0.61 m/s². If the Bowling ball's rolling resistance of 0.03 (`elements/BowlingBall/BowlingBallElementDeclaration.cs:40`) acts as a deceleration of 0.03·g = 0.29 m/s², the ball still starts and rolls out; the draft's 3° gave 0.37 m/s² against the same 0.29. Neither slope lets the ball climb the closed panel.
- **Doorway.** At `outlet_width` 0.62 the doorway is the whole 0.62 m inner width, 0.03 m clear each side of the 0.56 m Bowling ball when it is centred and never less than 0, because the side walls themselves are the doorway's edges.
- **Gate arrangement (hand calculation, repair of 22 November 2026, audit H11; re-derived 23 November 2026 after the second audit).** The cup's outlet face is at x = +0.39. The Powered gate is placed with its origin at (0.95, 0.56, 0) in cradle coordinates and turned 5° about Z so that its bore falls toward +X like the floor (a gate-local point (a, b) sits at (0.95 + a·cos 5° + b·sin 5°, 0.56 − a·sin 5° + b·cos 5°)). Then:
  - its upstream mouth (gate-local x −0.49) is at (0.462, 0.603); the collar's lowest rim, gate-local (−0.49, −0.78), is its most upstream point and sits at x 0.95 − 0.488 − 0.068 = 0.394, just outside the outlet face, so the gate overlaps no wall or floor of the cup (from the earlier origin x 0.88 that rim reached x 0.324, inside the floor slab and the wall ends);
  - the bore's lowest line at that mouth, gate-local (−0.49, −0.65), is at (0.405, −0.045): a 0.015 m gap after the floor's outlet edge (top at y −0.034) and a 0.011 m step down, which the Bowling ball rolls across and cannot roll back up;
  - the blade's upstream face is at gate-local x −0.06, so the ball (radius 0.28) rests against the closed blade with its centre near gate-local (−0.34, −0.37), at x 0.95 − 0.34·cos 5° − 0.37·sin 5° = 0.58; its back is at x 0.30, so it sits partly in the doorway and partly in the gate's mouth;
  - the gate's lowest point, the downstream collar rim at gate-local (0.49, −0.78), is at y 0.56 − 0.043 − 0.777 = −0.26 m, below the slab's lowest edge (−0.185), so the two parts do not stand on one flat surface; both are static and need not.

  When the gate is supplied the blade lifts and the 5° bore carries the ball on. Starting values; the slice's probe places both parts and re-derives them.

### Acceptance rows (21 November 2026)

Test class `CradleTests`. Tolerances: "stays" is a ball centre inside the inner square and below the rim after 5 s at under 0.05 m/s; "leaves" is a ball centre outside the outer footprint within a 10 s stall guard; the no-influence control compares with a free fall to 0.005 m after 30 ticks, the bound the rope's slack test uses. Speeds are chosen well apart; no escape threshold is asserted.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given a Bowling ball released with its centre 0.5 m above its seated height (y 0.78) at x −0.2, moving 1 m/s toward +X, when it settles, then it stays. By hand its lowest point reaches the 0.30 m rim after 0.2 m of fall (0.20 s), with its centre at x 0.0, inside the inner square, so it lands in the cup and the walls stop its 1 m/s | `CradleTests.AGentleBallSettlesAndStays` | the same release with the cradle removed: the ball lands on the bench and rolls away, leaving the footprint (landing friction leaves about 5/7 of its 1 m/s; 0.71² / (2 × 0.29 m/s²) ≈ 0.9 m of roll against its rolling resistance) | a Tennis ball released the same way also stays |
| A2 | Given a Bowling ball crossing the rim at 4 m/s, then it leaves the cradle | `CradleTests.AnEnergeticBallEscapes` | A1 | — |
| A3 | Given a Basketball lowered onto the cradle, then it rests on the rim with its centre above it and never seats on the floor; a 1 m/s sideways nudge removes it | `CradleTests.AnOversizeBallIsNotRetained` | the Bowling ball of A1 seats | — |
| A4 | Given a ball falling 0.05 m outside a wall, then its fall matches a free fall with no cradle present | `CradleTests.ABallPassingOutsideIsNotInfluenced` | — | the same pass beside a Receiver with its guide force enabled does differ |
| A5 | Given `outlet_width` 0.62 and an unpowered Powered gate placed as in the gate arrangement above, when a Bowling ball is lowered into the cup, then it rolls through the doorway and comes to rest against the closed blade in the gate's mouth, and stays there for 5 s; when the gate is supplied, the ball passes downstream of the blade | `CradleTests.ASeparatelyPlacedGateReleasesTheCargo` (a cross-part row owned by the Powered gate slice: the gate is taught at 24, after the cradle at 8a; coordinator decisions 36 and 41) | with no gate placed the ball rolls straight out and away | `outlet_width` 0 with the gate open: the ball stays in the cup |
| A6 | At `outlet_width` 0, 0.31 and 0.62 the compiled panel has the bound size and position and the art matches it | `CradleTests.TheOutletPanelFollowsItsWidth` | — | below 0, above 0.62 and non-finite are refused atomically (conformance row 2 once S4 lands) |
| A7 | Reset returns cargo and gate to their authored poses; save and load round-trip pose and `outlet_width` | `CradleTests.ResetAndSaveLoadRestoreTheCradle` | — | — |

No wrong-kind connection row: the part has no ports. Requirement rows preserved: "supports cargo after contact without remote ownership transfer" (A1, A4), "a separately placed gate can release it" (A5), "oversize or energetic cargo can escape" (A2, A3), "no invisible capture radius or forced attachment" (A4).

### Integration

- **Works with.** Delivered: every ball, `ramp`, `pipe` and bends, `funnel`, `powered_gate`, `battery`, `switch`. Planned: EL-215 Damped cushion (same campaign slots); EL-184 Mechanical ball gate as a second release in the same beside-the-cup arrangement (it is the same conduit), proven in its own slice.
- **Campaign.** Row 1 of [campaign element coverage](../requirements.md#campaign-element-coverage) ("cushion/cradle"): first use 1–10 (closed cup), practice 41–50 (gate release), reuse 96 and 149. No level places it today.
- **Coverage binding.** A final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).
- **Lesson fixtures (S8).** Needed for the gate lesson only if the gate is a locked fixture beside the cradle.
- **Integration proof.** [todo-411](../requirements.md#todo-411) (`requirements.md:2930`: cargo onto a moving supported catcher, released through a separately taught gate) is a separate task, not this identity's acceptance.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). Preferably S3 and S4 first.
- **The release gate's closed state (row A5).** The Powered gate stands nearly upright (turned 5°), so its unsupplied blade is closed by gravity and its spring today, which is all row A5 needs. Owner decision 6 gives that blade's spring a declared rest position (ENGINE-JOINT-KINDS, spring rest position) so it closes in every mounting orientation; this arrangement does not depend on it, and row A5 is re-run on the gate as that story leaves it.
- **Files.** `elements/Cradle/` (`Cradle.csproj`, `CradleElementDeclaration.cs`, `cradle.tscn`, `icon.svg`), `CuriousContraptions.tests/CradleTests.cs`, the solution line, the art classification once the visible-solid repair programme of `spec-engine-art-solidity.md` (`_bmad-output/implementation-artifacts/`, owner decision 22) lands, and the binding section of section 7 once S7 admits it.
- **Review tier.** Data-only: every capability has a cited test.
- **Disqualified if** the slice's probe finds: placement refuses the gate and the cup 4 mm apart, or the two static parts cannot be posed as in the gate arrangement through the UI (then A5 waits for a free-standing blade part and the rest of the slice proceeds; not verified); the Bowling ball's rolling resistance holds it on a 5° floor (then steepen the floor, a declared value); the ball stops in the bore before reaching the blade (it must still be held by the blade or the step, so A5's "rests against the blade" becomes "rests in the gate's mouth"); or a 0.04 m panel block is below a collider size limit (then cap `outlet_width` lower).

## 6. Open questions

**Resolved under the delegation.**

1. Box cup or rounded bowl: box cup. Reason: delivered static boxes; a bowl would need a new collider for no lesson.
2. Cup-scale release gate: no new gate part and no size option. The delivered Powered gate is the release (D19: no extra part unless a row or lesson needs it).
3. Inner size: 0.62 m stays, so the Basketball is the oversize control (D1, D13).
4. `outlet` Choice: replaced by the real `outlet_width` (correction in section 2).
5. Gate arrangement (22 November 2026, audit H11): the Powered gate is a conduit and stands beside the cup; the ball waits in its mouth. Chosen over deferring A5 to a new free-standing blade part, which D19 does not justify. The floor is 0.15 m thick and 5° steep so that the step into the bore is downward and the Bowling ball starts by itself.
6. Doorway (audit M26): the panel retracts into the side wall, so the full-width doorway has no jamb; `outlet_width` runs to 0.62 m.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One static body of five boxes and one Real parameter bound to a collider's size and offset and to the same panel's art. No capture radius, residence sensor, guide force, attachment or ownership transfer, and no engine test on this package.
2. **Generic laws.** Contact, friction, restitution and gravity. Cargo is retained because walls are in its way and released because a separately placed gate's blade moves out of its way.
3. **Three layers.** Colliders: five boxes (floor, back wall, two side walls, outlet panel); the panel's `HalfExtentZ` and `OffsetZ` are set from `outlet_width` at build time. Visuals: floor, walls and panel; none is a collider and nothing glows, and each visible solid has its box. Animation: none during Run. `Visual/Visual_Outlet` is scaled by a parameter presentation binding on the same value that sizes its collider.
4. **Flow and typing.** No ports; `outlet_width` is an f32 Real in metres with `Reject` policy.
5. **Flat state.** Bodies 1 (static); colliders 5 (boxes); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (parameter).
6. **Performance.** Five static boxes, as the Receiver, with none of its sensor or guide region. Nothing is cosmetic. No island sleeping exists today; a resting ball in the cup costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows A1 to A7 run the production simulation; A4 is the control that proves there is no hidden influence.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `cradle` |
| Named identity | EL-216, `../invest/named-elements.md#element-216` |
| Acceptance | none yet: the requirement row `element-216` has no todo or sequence-task anchor |
| Decision | S382, `../invest/scope-corrections.md#s382` |
| Mode records | `Configuration=Adjustable` (`outlet_width`) |

What it lacks today: `scope-corrections.md` has no `s382` block on published main; the requirement has no `todo-` and co-anchored `sequence-task-` pair for the Acceptance field; and the ledger entry links four other todo anchors (186, 409, 393, 223) where the reader requires exactly one, its own.
