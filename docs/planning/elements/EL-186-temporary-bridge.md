# EL-186 · Temporary bridge named-identity spec

**Current delivery (24 October 2026):** [EL-186 delivery slice](../invest/named-elements.md#element-186). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-186 |
| Name | Temporary bridge |
| Type | Mechanical |
| Anchor | [requirements.md#element-186](../requirements.md#element-186); [named entry](../invest/named-elements.md#element-186); scope [todo-321](../requirements.md#todo-321) |
| Proof owner | S365 |
| Refines / extends | No CAT spec. Built from the [EL-185 releasable joint](EL-185-releasable-assembly-joint.md); deck sized like [CAT-054 ramp](CAT-054-ramp.md) |
| Related | EL-185; EL-159/EL-160 load-limited connectors (the material-failure mode); GAP-05 load-limited connector lesson |
| Roadmap story | [EL-186 delivery slice](../invest/named-elements.md#element-186); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-CONTACT-OBSERVATIONS (the contact loads its break measures) and ENGINE-ISLANDS-SLEEP; then ENGINE-JOINT-KINDS (two pinned attachments on one body inside one package), ENGINE-ASSEMBLY-JOINTS (signal-commanded release) and ENGINE-FRACTURE (declared load limit); then EL-185; then S7. |

### Disposition (21 November 2026)

**To deliver as a single element**, in one slice after its engine stories. Planned package id `temporary_bridge`, directory `elements/TemporaryBridge/`, bound by this spec's final package-source-binding section once S7 widens that binding's preconditions (section 7); no ledger line binds it. Owner key S365. It is not a duplicate of EL-185: it has its own bodies, two attachments and a load limit.

## 2. Declaration

### Declaration to write (21 November 2026)

Starting declaration; the attachment, release and load-limit descriptors are fixed by the two engine stories' Code Maps. It is the whole declaration: the harvest-era subsections that followed it are reduced to a stub below (their text is in git history). Closest delivered shape: `elements/ImpactLever/` (a static base carrying one dynamic beam on a hinge, `IHingeElement`), with `elements/DelayBox/` for the Signal inputs.

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement`, the signal-input interface, and the release and load-limit interfaces the engine stories add (missing).
- **Bodies.** A static base of two abutment posts, each a box 0.6 × 1.0 × 1.3 m, their inner faces 3.02 m apart; and one dynamic deck, a box 3.0 × 0.12 × 1.3 m, hung between the inner faces with 0.01 m clearance at each end and its top flush with the post tops. Deck and posts collide (`BodiesCollide: true` on both attachments), so a swinging deck is stopped by a post or the ground and is never drawn through them. **Correction to the baseline:** the deck does not rest on the posts. A deck lying on its supports would stay up after its attachments were released, so it would not be held "only by its declared attachments".
- **Deck mass and inertia.** 2 kg, constant; box moments about length, vertical and width axes: 0.284, 1.782 and 1.502 kg·m² (m·(b² + c²)/12 with 3.0, 0.12 and 1.3 m); gravity (0, −9.81, 0) m/s².
- **Material.** Deck and posts: restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s (carries rolling cargo without bouncing it off).
- **Attachments.** Two pin attachments, each a revolute coordinate about the deck's width axis at the deck's **lower** end edge: `Left` at (−1.5, −0.06, 0) and `Right` at (+1.5, −0.06, 0) in deck coordinates (repair of 22 November 2026; the draft pinned the upper edges). With both present the deck is fixed. Pinned at a lower corner the deck swings clear: the pinned end's upper corner moves away from its post by 0.12·sin θ, and the free end's upper corner approaches its post by at most 0.12·θ − 1.5·θ² = 2.4 mm (at θ = 0.04 rad), inside the 0.01 m clearance. Pins on the upper edges would drive the lower corner 0.12·sin θ into the post and jam the deck near 5°. **Correction to the baseline:** the attachments are pins, not welds. With welds, releasing one end would leave a rigid cantilever and nothing would move; with pins the deck swings down about the remaining end, which is the behaviour the baseline's own Variants text describes.
- **Signal ports.** `ReleaseLeftIn` at the left post top and `ReleaseRightIn` at the right post top: `SimulationDomain.Signal`, input, `PortRole.ActivationIn`, pulse. Each releases its own attachment and nothing else.
- **Parameter.**

  | Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `end_capacity` | Real (f32) | `ParameterUnit.Newton` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:30`, value 14) | 40 | 25 | 200 | Clamp | BuildOnly |

  The baseline's `support` mode enum is dropped (decision in section 6): both causes of losing support are always active, as the requirement states them of one part.
- **Stores.** None. **Events.** Released and broken occurrences if the engine read models expose them.
- **Presentation.** The deck follows its body (`PresentationBodyBinding`); each clamp's pin goes gold to slate on release and shows a split pin on break, from the committed attachment state.
- **Catalogue.** Id `temporary_bridge`, title "Temporary bridge", description "A crossing held by two pins. Signal a pin to release it; too much load breaks one.", category `ElementCategory.Structure`, palette order 517 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour the Ramp's `#c28f52`.
- **Footprint.** Bodies 2, moving bodies 1, revolute constraints 2.

### Art and solidity

| Node | Mesh | Class | Collider or reason |
| --- | --- | --- | --- |
| `Visual/Visual_PostLeft`, `Visual_PostRight` | cream `#fff8e9` boxes 0.6 × 1.0 × 1.3 m | solid | post boxes (static base) |
| `Visual/Deck/Visual_Deck` | `#c28f52` box 3.0 × 0.12 × 1.3 m with three flush cross-planks lines | solid | deck box (moving body) |
| `Visual/Visual_ClampLeft`, `Visual_ClampRight` | navy `#293954` plates on the posts' inner top edges, flush with the post faces | decorative | printed flat |
| `Visual/Visual_PinLeft`, `Visual_PinRight` | gold `#f7cb52` discs on the plates, flush | decorative | printed flat; recoloured by a binding |

Icon: a plank between two posts with a pin at each end. It differs from the Ramp (a sloped slab) and the Wall by the two posts and pins.

### Harvest-era subsections (Bodies and shapes to Catalogue and inventory entry)

Superseded by the current declaration (sections 2–3: pins on the deck's lower edges, no `support` mode, `end_capacity` 25–200 N, category Structure, Signal-domain release inputs); the harvest text is in git history.

## 3. Engine capabilities

Families: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, StructuralFracture, TopologyTransaction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)). Map: "Actual supports/attachments carry load; removal/failure changes topology, no disappearance based on level goal."

### Capability probe (21 November 2026)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Static base with one dynamic box body on a declared revolute hinge | present | `IHingeElement`, `RevoluteHingeDescriptor` (used at `elements/Motor/MotorElementDeclaration.cs:88`–`:115`) | `RevoluteHingeFrictionTests` and the Impact lever suite (the slice's probe cites the exact test) |
| Pulse input ports | present | `PortRole.ActivationIn` (`CuriousContraptions.Abstractions/PortDescriptor.cs:25`) | as EL-185 |
| Two revolute attachments on the **same** moving body | missing | each `RevoluteHingeDescriptor` declares its own `MovingBody` (`MotorElementDeclaration.cs:103`); no declaration joins one body to its base twice | ENGINE-JOINT-KINDS: a pinned attachment declared inside one package is a joint kind, here two on one moving body |
| A signal-commanded release of a revolute attachment, restored by Reset | missing | a release exists only for a tensile span (`CutRope`, `CuriousContraptions.Abstractions/RuntimeServices.cs:77`) and a retention only for a prismatic coordinate (`IPrismaticRetentionElement`, `MechanicalContracts.cs:85`) | ENGINE-ASSEMBLY-JOINTS (commanded release, here commanded by a declared signal input) |
| A declared load limit on an attachment and a one-way break | missing | only a contact-integrity observer on one body exists (`IContactIntegrityElement`, `MechanicalContracts.cs:119`) | ENGINE-FRACTURE ("a declared load limit on a connection or body, a one-way break that changes topology when the sustained load exceeds it"; measure from one closed set, a declared hold time and the settling transient excluded, `readiness/batch-m.md` section 4 item 4) |
| Attachment-state read and presentation driver | missing | none | ENGINE-ASSEMBLY-JOINTS (attachment state and read); ENGINE-FRACTURE publishes the broken state into it (engine-stories-index C22) |
| A force unit for a parameter | present | `ParameterUnit.Newton` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:30`), used by the Linear pusher's `force` (`elements/LinearPusher/LinearPusherElementDeclaration.cs:17`) | the Linear pusher suite (the slice's probe cites the exact parameter-refusal test) |

Counts: 3 present, 0 present-untested, 0 narrower, 4 missing (1 owned by ENGINE-JOINT-KINDS, 1 by ENGINE-ASSEMBLY-JOINTS, 1 by ENGINE-FRACTURE, and the attachment-state read by ENGINE-ASSEMBLY-JOINTS, into which ENGINE-FRACTURE publishes the broken state).

**Harvest-era lists (Exists now, Missing).** Superseded by the capability probe above; the harvest text is in git history.

**Dependencies.** EL-185 releasable joint; EL-159/EL-160 load-limited connector law (the same ENGINE-FRACTURE limit) for the break.

## 4. Sources and legacy

- Requirement: "Assembled load-bearing crossing loses support only when its declared attachment releases or material fails"; outcome "A release signal cannot move unrelated structures" ([element-186](../requirements.md#element-186)).
- [todo-321](../requirements.md#todo-321): defer until joint lifecycle is reliable; Reset restores authored attachments. Campaign: "releasable bridge/joint", first use 41–50 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); [structure-material profile](../invest/profiles.md#structure-material).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Explicit Detach of an existing joint identity | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | carry forward | Release removes a specific attachment. |
| 2 | Accepted: an impact releases a rope and the load falls; a missed impact leaves it held | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L84-L104` | carry forward | Released versus held control. |
| 3 | Accepted: simultaneous conflicting detaches reject | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L223-L238` | carry forward | One owner per attachment. |

No legacy bridge element exists: searches for `bridge` in parts, engine and content at `a6c914e` return only the `engine/bridge/` interop namespace and level text that calls domino chains "bridges" (level `double_bridge`, `content/puzzles.json@a6c914e:L26885-L26890`).

**Files harvested:**
- `engine/physics/PhysicsJointChange.cs`
- `CuriousContraptions.tests/PhysicsImpactJointTests.cs`
- `parts/catalog/impact_lever.tres` (beam mass reference)
- Checked, no element knowledge: `content/puzzles.json` (level `double_bridge`, domino "bridges"), `engine/bridge/` (host interop namespace).
- Searched with no hit for `temporary bridge` or a bridge part: `parts/`, `engine/` (including `engine/physics/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-186](../requirements.md#element-186) and the [mechanics](../invest/profiles.md#mechanics) and structure-material profiles.
- **Chrome UI recipe (pending, post-catalogue).** Place the bridge across a gap between two ramps, a ball at the top ramp, a switch wired to `ReleaseRightIn`, and a second untouched structure (a wall) nearby. Run.
- **Outcomes, controls and boundaries** are rows A1–A6 below. Save/Load round-trips `end_capacity`, pose and both release wires (A5).
- **Integrations.** Releasable assembly and bridge task [todo-321](../requirements.md#todo-321) with EL-185; campaign row "... releasable bridge/joint": first use 41–50, reuse 61–100, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); GAP-05 load-limited connector lessons 43–44 ([campaign gap allocation](../requirements.md#campaign-gap-allocation)) for the load limit.

### Behaviour and numbers (21 November 2026)

- **Law.** Two pins fix the deck. A release removes one pin without impulse; the deck then swings about the other under gravity and its cargo. A pin breaks, once and for the rest of the Run, when the sustained reaction force it carries exceeds `end_capacity`; The measure and the meaning of "sustained" are ENGINE-FRACTURE's unified text (`readiness/batch-m.md` section 4 item 4, which replaces Batch J's gap 6): every load limit declares a measure from one closed set (axial tension, shear, bending moment, resultant, contact normal) and a hold time of zero or more; the break occurs when the measure has exceeded the limit on every substep for the hold; the settling transient at Run start does not count; a load of another kind or sign never breaks an attachment. Each pin declares the shear measure: the reaction perpendicular to the deck, which is vertical while the deck is level. Two pins on one rigid deck share its axial load indeterminately, so a resultant would depend on how the solver splits that share; the perpendicular reactions are fixed by statics, and every load below is one. Its hold time must be shorter than the 0.46 s a rolling Bowling ball spends above the default limit (row A3) and longer than a landing transient, so that a Basketball dropping from a Ramp lip breaks nothing; the slice fixes the value inside that window. No fragments are declared: a break removes the pin's constraint only.
- **Numbers (statics by hand, g = 9.81 m/s²; not simulated).** Deck weight 19.6 N, so 9.8 N per pin unloaded. Cargo of weight W at distance x from a pin on the 3.0 m span loads that pin with 9.8 + W·(1 − x/3) N. A Basketball (the `ball` package, 1 kg, 9.8 N): 19.6 N on the entry pin as it rolls on and 14.7 N per pin at mid-span, always under the 40 N default and the 25 N minimum. A Bowling ball (4 kg, 39.2 N, `elements/BowlingBall/BowlingBallElementDeclaration.cs:54`): 49.1 N on the entry pin as it rolls on, above 40 N until x exceeds 0.69 m (the first 23 % of the span); 45.1 N when set down 0.3 m in; 29.4 N per pin at mid-span. So at the default a Basketball crosses and a Bowling ball breaks the pin it enters over; a Bowling ball set down at mid-span is carried, and two there (49.1 N per pin) are not. The minimum is 25 N: 20 N left 2 % over the Basketball's entry load, 25 N leaves 27 %. The 200 N maximum carries a rolling Bowling ball four times over.
- **Fit.** The 1.3 m deck width is the pipe bore and the Ramp width, so a ball leaving a Ramp or pipe crosses it; the 3.02 m gap is the Ramp's default length.
- **After a release.** The pins are 0.88 m above the ground (the 1.0 m post less the 0.12 m deck), so the free end's lower corner lands when 3.0·sin θ = 0.88, θ = 17.1°; cargo on the slope rolls into the gap. During the swing with a Basketball at mid-span the remaining pin carries about 23 N resultant at most (moment of inertia about the pin 8.25 kg·m², ω² = 10.7·sin θ; at 17.1° radial 22.8 N, tangential 5.1 N), so its perpendicular (shear) share is lower still and under the 40 N default, so a commanded release does not also break the other pin. Below about 25 N of capacity it may; that is physical and no row asserts it. Nothing else moves.
- **Tick order.** As EL-185: a pulse read in the signal phase removes the pin from the next physics step.

### Acceptance rows (21 November 2026)

Test class `TemporaryBridgeTests`. Tolerances: "holds" is deck movement under 0.005 m over 5 s; the capacity boundary is tested well apart (18.6 N and 29.4 N under, 45.1 N and 49.1 N over, against 40 N), not at the threshold; stall guards 10 game seconds.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given a Basketball rolling off a Ramp onto the bridge at the default capacity, when it crosses, then it reaches the far side, the deck has not moved and no pin has broken | `TemporaryBridgeTests.ABallCrossesAnIntactBridge` | with the bridge removed the ball falls into the gap | a Tennis ball also crosses; the Basketball also crosses at `end_capacity` 25 |
| A2 | Given a switch wired to `ReleaseRightIn`, when it is struck with a Basketball at mid-span, then the right end drops, the ball falls into the gap, the left pin is still attached, and the posts and a nearby Wall have not moved | `TemporaryBridgeTests.AReleaseDropsOnlyItsOwnEnd` | with no signal the deck holds for 5 s under the same ball | both ends signalled in one tick: the deck falls free |
| A3 | Given the default capacity, when a Bowling ball is set down at rest 0.3 m from the left pin (45.1 N), then the left pin breaks and the deck swings down about the right; a Basketball set there (18.6 N) and a Bowling ball at mid-span (29.4 N) break nothing; a Bowling ball rolling onto the deck at no more than 1.5 m/s (0.46 s above the limit) breaks the entry pin before a quarter span | `TemporaryBridgeTests.ABowlingBallBreaksThePinItLoadsAndABasketballDoesNot` | `end_capacity` 200 carries the rolling Bowling ball across | two Bowling balls at mid-span (49.1 N per pin) break a pin; `end_capacity` 25 holds the bare deck and a crossing Basketball |
| A4 | A broken or released pin stays so for the rest of the Run; Reset restores both pins, the deck pose and the pin colours | `TemporaryBridgeTests.ResetRestoresBothAttachments` | — | Reset while the deck is swinging |
| A5 | Save and load round-trip `end_capacity`, pose and both release wires | `TemporaryBridgeTests.SaveLoadRoundTrips` | — | capacity at 25 and 200 |
| A6 | `end_capacity` below 25, above 200 or non-finite is refused atomically; an electrical wire to a release input is refused | conformance suite rows 2 and 5 (S4) once it lands; until then `TemporaryBridgeTests.InvalidCapacityAndForeignWiresAreRefused` | — | — |

Requirement rows preserved: "loses support only when its declared attachment releases or material fails" (A1 control of A2, A3), "a release signal cannot move unrelated structures" (A2).

### Integration

- **Works with.** Delivered: `ramp`, `wall`, every ball, `switch`, `pressure_plate`, `ball_detector`, `delay`. Planned: EL-185 (same release primitive; delivered first), EL-159 and EL-160 (the same load-limit law).
- **Campaign.** "Releasable bridge/joint": first use 41–50, reuse 61–100, 111–120, 136–150; GAP-05 lessons 43–44 for the break ([campaign element coverage](../requirements.md#campaign-element-coverage)).
- **Coverage binding.** A final package-source-binding section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; blocked on S7 (section 7).
- **Lesson fixtures (S8).** Needed where a lesson locks the bridge in place across a fixed gap.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-CONTACT-OBSERVATIONS and ENGINE-ISLANDS-SLEEP; then ENGINE-JOINT-KINDS (pinned attachments inside one package), ENGINE-ASSEMBLY-JOINTS (signal-commanded release) and ENGINE-FRACTURE (declared load limit with measure and hold time); then EL-185; then S7 (the coverage follow-on, which widens the reader, owner decision 15).

1. ENGINE-JOINT-KINDS and ENGINE-ASSEMBLY-JOINTS, then ENGINE-FRACTURE (its stated order).
2. EL-185.
3. This slice: `elements/TemporaryBridge/` (project, declaration, scene, icon), `CuriousContraptions.tests/TemporaryBridgeTests.cs`, the solution line, art classification, and the binding section of section 7 once S7 admits it. Expected tier: data-only if every row is *present* with a test by then; otherwise engine.
- **Disqualified if** the engine stories do not allow an attachment declared inside one package (then the bridge is a deck part joined to two post parts by player-made releasable connections, and this declaration is rewritten); if the break uses a force measure under which a Basketball's 19.6 N entry load and a Bowling ball's 45.1 N end load are not separated by the 40 N default; or if ENGINE-FRACTURE counts a transient landing or rolling load as sustained, so that a Basketball dropping from a Ramp lip breaks a pin (then the declared hold time is the fix, not this part's numbers; ENGINE-FRACTURE, `readiness/batch-m.md` section 4 item 4).

## 6. Open questions

**Resolved under the delegation.**

1. Failing mode as a separate identity or a bridge setting: neither a mode nor a separate part. The bridge always has both causes; `end_capacity` is its one parameter. Reason: the requirement names both causes of one crossing, and a mode switch would be a second rule for the same part (D8, D13).
2. Which load `end_capacity` limits: each limit declares its measure from ENGINE-FRACTURE's closed set (`readiness/batch-m.md` section 4 item 4); this part declares shear, the reaction perpendicular to the deck, because the axial share between two pins on one rigid deck is indeterminate (second audit, 23 November 2026).
3. One slice or two: one, after its engine stories. Reason: delivering release first and adding `end_capacity` later would change the saved shape of a published part, and saves are not migrated.
4. Which cargo crosses (22 November 2026, audit H1): the Basketball crosses and the Bowling ball breaks the pin it enters over. Reason: by the spec's own statics a Bowling ball loads the entry pin to 49.1 N, above the 40 N default, so "a Bowling ball crosses" and "49 N breaks" could not both hold. The minimum rises from 20 N to 25 N so that no admitted capacity is within 2 % of the Basketball's entry load.
5. Where the pins sit (audit H2): on the deck's lower end edges, with deck-to-post collision on. Reason: the hand geometry in section 2; upper-edge pins jam the deck against its post near 5°.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static base of two posts, one dynamic deck, two declared pin attachments each with a commanded release and a declared load limit, two signal inputs, one parameter and bindings. No bridge rule exists in code.
2. **Generic laws.** A revolute attachment between two bodies; its removal at a step boundary on a signal command; a one-way break when the sustained load on a declared attachment exceeds its declared limit; gravity and contact afterwards. The missing capabilities are stated element-free and each has one owner: attachments declared inside one package (ENGINE-JOINT-KINDS), their signal-commanded release (ENGINE-ASSEMBLY-JOINTS), and the load limit with its declared measure and hold time (ENGINE-FRACTURE). The bridge does not fall because a goal or timer says so.
3. **Three layers.** Colliders: three boxes (two posts on the static body, the deck on its own body). Visuals: posts, deck with flush plank lines, flush clamp plates and pin discs; none is a collider. Animation: the pins' colour and split-pin mark, bound to committed attachment state; decorative, nothing written back. The deck art follows the deck body's pose.
4. **Flow and typing.** Two Signal-domain `ActivationIn` ports; `end_capacity` is an f32 Real in newtons; attachment state is a closed set read from the engine.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 3 (boxes); attachments 2 (revolute, releasable, load-limited); drives 0; sensors 0; signal inputs 2; stores 0; presentation bindings 3 (deck body, two pin states).
6. **Performance.** Three primitive colliders; the intact bridge is static in effect. No island sleeping exists today; a resting deck costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. A release or break changes the constraint set at a step boundary; the two stories must do that within the compiled tables, with no allocation during a tick (batch summary).
7. **Tests are scenarios.** Rows A1 to A6 run the production simulation; the load rows are hand statics on delivered balls and prove the general limit law for this declaration.

**Verdict: conforms; blocked on three engine stories for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `temporary_bridge` |
| Named identity | EL-186, `../invest/named-elements.md#element-186` |
| Acceptance | `todo-321` with its co-anchored `sequence-task-374` |
| Decision | S365, `../invest/scope-corrections.md#s365` |
| Mode records | `Configuration=Adjustable` (`end_capacity`) |

What it lacks today: `scope-corrections.md` has no `s365` block on published main; and `todo-321` with `sequence-task-374` is one task source shared with EL-185, while the reader refuses two packages on one task source ("Duplicate named task source binding"). The ledger entry itself qualifies (one todo link, one `Owner **S365**`).
