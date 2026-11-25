# EL-175 · Optical fibre — named-identity spec

**Current delivery (24 October 2026):** [EL-175 delivery slice](../invest/named-elements.md#element-175). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. A coupler's lens looks along local +X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-175 |
| Name | Optical fibre |
| Type | Optical |
| Anchor | [requirements.md#element-175](../requirements.md#element-175); named entry [named-elements.md#element-175](../invest/named-elements.md#element-175); source record [todo-307](../requirements.md#todo-307) (Optional) |
| Owner | S521 (refinement S526 fiber) |
| Disposition | To deliver as a **single element**: one coupler package, placed twice and joined by one Optical connection whose cable is a straight solid strand. It refines no catalogue part and is kept apart from the ball pipes. "Optional" in the source record is a priority mark; the identity is adopted for implementation (index, 24 October 2026). |
| Planned package | id `optical_fibre`, directory `elements/OpticalFibre/`, class `OpticalFibreElementDeclaration` |
| Roadmap story | [EL-175 delivery slice](../invest/named-elements.md#element-175), `named-el-175` |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and on ENGINE-CONNECTION-MATERIALS (solid strand) |

## 2. Declaration

Shape followed: the combiner ([EL-213](EL-213-optical-combiner.md)) for a Relay, and the Rope anchor for a part whose purpose is one end of a player-made connection (`elements/RopeAnchor/`). Each placed coupler is the same declaration; a pair is two instances and one connection.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) plus the optical element interface **(optics)** |
| Body | One static scene body; material restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s |
| Colliders | Coupler box 0.5 × 0.5 × 0.5 m at the origin; foot box 0.5 × 0.1 × 0.5 m at (0, −0.3, 0), flush with the box's sides so it does not overhang the port face; socket sphere r 0.075 m at the link port. Three colliders. |
| Link port **(optics, M8)** | One port of the Optical connection domain, Bidirectional, at (−0.25, 0, 0), normal −X; declared maximum connection count 1. |
| Connection kind **(optics, M8; strand: ENGINE-CONNECTION-MATERIALS)** | Optical link: joins two link ports of different placed couplers; declared maximum length 8 m; its cable is a **straight solid strand** between the two ports, diameter 0.06 m, static, with the collider its declared connection material gives it (owner decisions 1 and 10 and coordinator decision 16, 22 November 2026). A link is **refused where the straight run would cross any collider** other than the two couplers' own. |
| Checks, with typed refusals | At every edit that makes or changes a link, moves a linked coupler, or places or moves another part: port count (a second link on a linked port), self-link, wrong domain, length over 8 m, and a crossing of the straight run. An edit that would break a rule is refused and the construction is unchanged. The same checks run when Run compiles, so a loaded construction that breaks one is refused with the same typed reason. Bodies that move during a Run simply collide with the strand. |
| Relay **(optics)** | Inlet: disc aperture r 0.2 m on the +X face at (0.25, 0, 0), front face only. Outlet: the linked partner's lens centre, direction the partner's +X, half-angle 0. τ = (0.81, 0.81, 0.81): 0.9 for each coupler; one outlet, so outputs never exceed input. Internal path: the link length, the distance between the two link ports when Run compiles. Enabled only while linked; unlinked, the inlet absorbs. The pair is reciprocal: either lens is an inlet and the other its outlet. A segment leaving a lens ignores the face it leaves from. |
| Parameters, stores | None. The link length is a property of the connection. |
| Presentation bindings | `Visual/Lens` albedo from the committed power leaving this coupler's lens, slate (85, 101, 115) when dark, ExponentialFollow 12 /s **(optics, M5)** |
| Footprint | `ElementFootprint(1, 0, 0, 0)`; domain: the optical flag **(optics, M6)** |
| Catalogue | Id `optical_fibre`, "Light pipe", `ElementCategory.Optics` **(optics, M6)**, colour (1, 0.97, 0.91), palette order 627 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Place two and join them with a straight cable where nothing is in the way. Light entering one lens leaves the other, a little weaker. Unjoined, a coupler does nothing." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Coupler` | box 0.5 m, cream `#fff8e9` | solid (coupler box) | nothing |
| `Visual/Foot` | box 0.5 × 0.1 × 0.5 m at y −0.3, navy `#293954` | solid (foot box) | nothing |
| `Visual/Lens` | disc r 0.2 with a gold `#f7cb52` ring, drawn flush in the +X face | decorative: flush, recoloured by a binding | nothing (colour only) |
| Link stud | sphere r 0.075, ochre `#e8b764` | solid (socket sphere) | nothing |
| Cable | straight rod 0.06 m thick between the two studs, slate `#556573`, drawn by the connection | solid strand (collider from the connection material, not from this package) | nothing; it is static |

Icon: an original navy outline of two small boxes joined by a straight line. It differs from the ball pipes by having no bore and a small lens, and from the combiner by being a small cream box placed in pairs.

### Compiled counts

Per coupler: bodies 1 (static), colliders 3 (2 boxes, 1 sphere), relays 1 with 1 outlet (the partner's), bindings 1 (the lens colour). Per pair: one Optical connection and one straight strand; its collider count is what ENGINE-CONNECTION-MATERIALS's segment rule gives a straight run of at most 8 m (one cylinder is enough for a straight strand; that story decides).

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 3 present, 0 untested, 2 narrower, 6 missing.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R3 | Typed connections refused across domains | narrower: the domains are Mechanical, Electrical, Signal, Fluid, Audio and Rope (`CuriousContraptions.Abstractions/SimulationDomain.cs:6`–`:15`); there is no Optical domain; delivered validation refuses a wrong domain and a duplicate link but has no declared connection count or maximum length | `SimulationDomain` | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`). Owner: **ENGINE-OPTICAL** (M8) |
| R9 | A segment cast against solid colliders | narrower: `AirflowLineClear` answers clear or blocked for airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`). The same cast decides whether a link's straight run is clear | `AirflowLineClear` | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing cast: **ENGINE-SPATIAL-QUERIES** |
| M2 | The optical solve: once per tick after the last substep, on ordered crossings from the spatial query | **missing** | — | owner **ENGINE-OPTICAL** |
| M3 | Declared surface responses, here a Relay | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Committed outlet-power driver and beam read model | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |
| M8 | An Optical connection domain whose connection joins a Relay inlet on one placed part to an outlet on another, with the connection length as the internal path; a port's declared maximum connection count and a connection kind's declared maximum length, checked at edit and at Run with typed refusals | **missing** | — | owner **ENGINE-OPTICAL** |
| M9 | A solid strand as a property of a declared connection material, here straight and static, with the rule that a connection is refused where its straight run crosses a collider (checked through the spatial query at edit and at Run) | **missing** | — | owner **ENGINE-CONNECTION-MATERIALS** (proposed in `readiness/batch-i.md`; owner decision 1 and coordinator decision 16) |

ENGINE-OPTICAL is [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception); M8 is not in its text and is in the corrected text of the [batch summary](readiness/batch-k.md), stated as a Relay across a typed connection so that it serves any part. ENGINE-CONNECTION-MATERIALS, ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES each have their story in `epics.md`; the straight static strand this part needs is ENGINE-CONNECTION-MATERIALS' (engine-stories-index section 2).

**Dependencies.** A laser, a detector, the mirror (the lesson it must not erase), Battery, Wall, a ball.

## 4. Sources and legacy

**Sources.** Row [element-175](../requirements.md#element-175): compatible endpoints carry bounded optical power with coupling loss and finite path length; disconnected endpoints cannot bridge a gap. [todo-307](../requirements.md#todo-307) (Optional): explicit compatible endpoints, coupling limits and loss; keep separate from ball tubes; restrict inventory where it would bypass the mirror puzzle. Refinement S526 **fiber**. Campaign: fiber/light pipe first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no fibre; mechanisms only):
1. Route interaction: routed ray leaves a directed outlet with power × transmission and remaining range minus internal travel — `engine/OpticalNetwork.cs@a6c914e:L119-L125`; routed optics require a finite directed outlet — `engine/OpticalNetwork.cs@a6c914e:L58-L62`. Carry forward, extended across a link.
2. Internal travel counts against range — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L91-L117`. Carry forward (cable length counts the same way).
3. Routing retention 0.9 — `parts/BeamCombinerPart.cs@a6c914e:L12-L12`. Carry forward as the coupling.
4. Typed links with explicit matching sockets and a length set at connect — recorded as rope facts R4–R6 in [CAT-058-rope_anchor](CAT-058-rope_anchor.md) (`engine/ConnectionPort.cs@a6c914e:L42-L66`). Carry forward the pattern.
5. Pipe length bounds 1–8 m — `engine/MachineData.cs@a6c914e:L72-L75`. Carry forward the 8 m maximum as the cable bound.

**Files harvested:** `engine/OpticalNetwork.cs`, `parts/BeamCombinerPart.cs`, `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## 5. Acceptance

### Behaviour and numbers

Starting values (D1).

- **What it is for.** A straight cable cannot pass through or around an obstacle. The pair moves a beam to another place and turns it to any direction without aiming: light enters one lens, travels the cable, and leaves the other lens along that coupler's own +X. It does what two mirrors do, at 0.81 instead of 0.95² = 0.9025, with no angles to set, and only where a straight run between the two couplers is clear.
- **Law.** A collimated segment ending on a linked coupler's lens starts one segment at the partner's lens with P′ = 0.9 × 0.9 × P = 0.81 P, the remaining range less the link length, and one more interaction. A 10 W red beam leaves as 8.1 W; an amber beam as (8.1, 6.32, 2.59) W, whose blue is still 4 % above a default blue receiver's 2.5 W. The 19 % is absorbed, half at each coupler. A Flashlight's cone on a lens is absorbed: only collimated segments are relayed.
- **Finite path.** A link is at most 8 m. With the first lens 5 m along the beam from the laser's beam origin and a 4 m link, 16 − 5 − 4 = 7 m of range remains beyond the second lens. There is no per-metre loss (decision K-14).
- **No bridging.** An unlinked coupler's lens absorbs; an unlinked partner emits nothing.
- **Reciprocal.** Light entering either lens leaves the other. A beam returned into the lens it left travels back through the pair; the 16-interaction limit ends any loop.
- **The cable.** A static solid rod 0.06 m thick: a ball that meets it does not pass through it (a ball on a thin round rod rolls off to one side rather than resting on it), and a beam that crosses it is absorbed. Nothing travels inside it but light.
- **Inventory.** Where a fibre would bypass a mirror lesson the level grants none, or one pair; that is level inventory data.

### Rows

Planned class `OpticalFibreTests`. **Coordinates.** x and z are bench coordinates; y is measured from the laser's beam line, which stands 0.585 m above the bench so that the laser's base (bottom 0.505 + 0.08 below the line, [CAT-036](CAT-036-laser.md)) rests on it. Every part lies inside the placement volume x ∈ [−7, 7], y ∈ [0, 9], z ∈ [−4, 4] (`engine/PlacementPose.cs:38`–`:42`); the earlier layout, with B at x 7.75 and the Y4 detector at x 15.5, did not. **Fixture:** Battery; a pulsed red laser centred at (−6.2, 0, 0) aimed along +X (its base's rear edge at x −6.95; its beam origin on the collar face 0.72 m ahead, at x −5.48); coupler A's lens 5 m along the beam, at x −0.48 (A's centre at (−0.23, 0, 0), turned 180° about Y to face the laser, its link port at (0.02, 0, 0) facing +X); coupler B unturned with its lens facing +X, its link port at (2.02, 3.464, 0) (centre (2.27, 3.464, 0), lens at x 2.52, a static part needing no support), linked to A by a clear cable √(2² + 3.464²) = 4.0 m long. The run leaves each port 60° from that port's outward normal, so the cable's edge meets the port face only within 0.03 × tan 60° = 0.05 m of the port, inside the 0.075 m stud; it passes clear of both boxes and more than 0.15 m above both feet. A broadband detector's aperture stands 3 m in front of B's lens, at x 5.52 (centre (5.65, 3.464, 0); its base reaches x 6.08). A Wall stands between the laser's line and the detector's height only where stated. Power tolerance ±2 % (the family's laser tolerance); stall guard 1 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| Y1 | **Clear run.** Given the fixture, then the detector, 3.46 m above the laser's line, reads 8.1 W red, the segment leaving B is along B's +X within 0.5°, and B's lens binding is lit | `OpticalFibreTests.ALinkedPairCarriesLightAlongAClearStraightRun` | Y2 | ±2 % |
| Y2 | **Disconnected endpoints.** Given the same placement without the cable, then the beam ends on A's lens and the detector reads nothing; given A and B facing each other 1 m apart and unlinked, nothing passes (ledger outcome) | `OpticalFibreTests.UnlinkedCouplersBridgeNothing` | Y1 | reading below the 0.001 W floor for 240 ticks |
| Y3 | **Refusal where the run is blocked.** Given a Wall between A and B, when the link is offered, then it is refused with its typed reason and the construction is unchanged. Given the clear fixture linked, when a Wall is moved into the run, or B is moved so the run crosses the Wall, then that edit is refused. Given a saved construction whose run crosses a collider, then Run is refused with the same reason | `OpticalFibreTests.ALinkIsRefusedWhereItsStraightRunCrossesACollider` | Y1 (clear run admitted) | a Wall face 0.05 m into the run is refused; 0.05 m clear of the 0.03 m strand radius is admitted |
| Y4 | **Range spent by the link.** The fixture's 7 m of remaining range cannot be shown inside the bench (B's lens at x 2.52 + 7.5 m = 10.0), so Y4 has its own layout. Given the laser as in the fixture; coupler A's lens 9 m along the beam, at x −5.48 + 9 = 3.52 (A's centre (3.77, 0, 0), turned 180° about Y, link port (4.02, 0, 0) facing +X); coupler B rolled 120° about Z so its lens faces (−0.5, 0.866, 0), its link port at (4.02 + 2, 3.464, 0) = (6.02, 3.464, 0) facing (0.5, −0.866, 0) and its lens at (5.77, 3.897, 0); a 4 m cable that leaves each port 60° from its normal (at B, between the run toward A, (−0.5, −0.866, 0), and the port normal: cos⁻¹(−0.25 + 0.75) = 60°). The range left beyond B's lens is 16 − 9 − 4 = 3 m. Then, with no detector, the segment leaving B ends 3 m from B's lens; a detector with its aperture square to the beam 2.5 m from B's lens (aperture centre (5.77 − 1.25, 3.897 + 2.165) = (4.52, 6.06, 0)) is lit; the same detector 3.5 m from B's lens (aperture centre (4.02, 6.93, 0)) is dark. The highest part is that detector's base, about 7.85 above the line, so 8.44 m above the bench, under 9 | `OpticalFibreTests.TheLinkLengthIsSpentFromTheRange` | the pair fails both ways: if the link were not spent the range left would be 7 m and the 3.5 m detector would be lit; if it were spent twice the range left would be −1 m and nothing would leave B, so the 2.5 m detector would be dark | end point ±0.02 m (the optical end-point tolerance, coordinator decision 21); detectors 0.5 m either side of the 3 m end |
| Y5 | Given the beam on A's back, side or foot, or on its lens face 0.23 m from the centre, then nothing leaves B | `OpticalFibreTests.OnlyTheLensCouples` | Y1 | 0.03 m outside the 0.2 m disc |
| Y6 | Given the laser aimed into B's lens and the detector in front of A's, then the detector reads 8.1 W | `OpticalFibreTests.ThePairIsReciprocal` | Y1 | ±2 % |
| Y7 | Given a third coupler offered to A's linked port, a coupler's port offered to itself, an electrical socket offered to the link port, or a partner 8.5 m away, then each connection is refused; at 7.5 m it is admitted. Given a linked coupler dragged to 8.5 m from its partner, the move is refused | `OpticalFibreTests.OnlyOneCompatibleLinkWithinEightMetresIsAdmitted` | Y1 | typed refusals; 0.5 m either side of 8 m |
| Y8 | Given a second pair linked back to back by a horizontal 2 m cable (A turned to face −X, B unturned 2 m beyond it along +X, both feet on the bench, so the cable's axis is 0.35 m above the bench): a Tennis ball rolled along Z at the cable does not pass the strand (the 0.32 m gap under it is less than the ball's 0.50 m diameter, and the contact lies 0.10 m above the ball's centre); a Tennis ball dropped onto the cable does not pass through: its centre never comes closer to the strand axis than its radius + 0.03 − 0.005 m; given a second laser whose beam crosses the cable, that beam ends on it; given a ball rolled at a coupler's lens, it rebounds and nothing is transported | `OpticalFibreTests.TheCableAndCouplersAreSolidAndCarryOnlyLight` | Y1 | contact within 5 mm of the drawn surface |
| Y9 | Given a lit run, when Reset, then no segment or lens colour remains; a saved and reloaded construction keeps both couplers and their link and behaves as Y1 | `OpticalFibreTests.ResetAndReloadKeepTheLink`; authored state by R12 | Y1 | — |

No parameter, so no parameter-refusal row. Chrome recipe (pending, post-catalogue): Battery, triggered laser, one coupler on its beam, a second coupler 2 m beyond and 3.46 m above its link port joined with the cable tool, a detector facing the second lens; try to join through a Wall and see the refusal; Run.

## 6. Open questions

None open.

1. **Coupling, attenuation, aperture and inventory — decided** (K-14, D19): 0.9 per coupler, no per-metre term, lens radius 0.2 m, inventory restricted per level by level data.
2. **Reciprocal — decided**: yes. One package placed twice has no "entry" and "exit" kinds.
3. **The cable's path — decided by the owner** (decision 10, 22 November 2026): a straight solid strand, refused where it would cross a collider; no draped flexible routing. The earlier row that carried light past a Wall the cable ran through is deleted and replaced by rows Y1 (clear run) and Y3 (refusal).
4. **Optional status — resolved**: adopted for implementation with every named identity (index, 24 October 2026).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Lasers, detectors, mirror [EL-212](EL-212-flat-mirror.md), combiner, prism (undelivered); Wall, balls, Battery (delivered). The link port's domain is Optical at both ends; no other part declares one.
- **Campaign.** Fibre / light pipe first use 51–60, after the mirror's first use so it cannot replace that lesson.
- **Coverage binding (planned, not yet live).** A named package is bound by the binding section that main's `tools/Coverage/NamedPackageSources.cs` reads; **blocked on the coverage follow-on story S7** (decision S521 has no anchor in `docs/planning/invest/scope-corrections.md`). S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15); the section is not written here.
- **Lesson fixtures.** A level that places a linked pair locked needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22), to express a pre-made connection.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary, including M8; ENGINE-CONNECTION-MATERIALS for the straight solid strand and its crossing refusal; a laser and a detector; S7 for the binding.
- **Files.** `elements/OpticalFibre/OpticalFibre.csproj`, `OpticalFibreElementDeclaration.cs`, `optical_fibre.tscn`, `icon.svg`; one guarded `CuriousContraptions.slnx` line; `CuriousContraptions.tests/OpticalFibreTests.cs`; a `VisibleSolidColliderTests` row; this spec's binding section.
- **Review tier.** Engine tier unless both engine stories have delivered M8 and M9; then data-only.
- **Disqualifiers.** No Relay across a connection; no declared connection count or maximum length; a strand rule that serves only tensile connections with no owner for a static one; a connection tool that cannot join two instances of one package.

## 9. Engine conformance

Verdict: **Conforms** (each missing capability has exactly one owning element-free story: ENGINE-OPTICAL for M2, M3, M5, M6 and M8; ENGINE-CONNECTION-MATERIALS for M9; ENGINE-SPATIAL-QUERIES for the cast behind R9).

1. **Declaration data only.** Three colliders, one port with a declared connection count, one Relay row, one binding; a pair is two instances and a connection of a declared kind.
2. **Generic laws.** The Relay law across a typed connection; the connection material's strand; the general rule that a solid strand may not be made through a collider; contact for bodies.
3. **Three layers.** Colliders: two boxes and a sphere per coupler, plus the strand's from its material. Visuals: box, foot, flush lens, stud, straight cable. Animation: the lens colour from committed outlet power.
4. **One-way flow and typing.** Segment in at one lens, segment out at the other; the connection domain is an enum member; refusals are typed.
5. **Flat state.** Counts in section 2; the link length is fixed when Run compiles.
6. **Performance fitness.** Static; three primitives; one new segment per coupled segment; one straight strand per pair; the crossing check is one segment cast per edit, not per tick.
7. **Tests are scenarios.** Rows Y1 to Y9.
