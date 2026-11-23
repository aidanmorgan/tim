# EL-185 · Releasable assembly joint named-identity spec

**Current delivery (24 October 2026):** [EL-185 delivery slice](../invest/named-elements.md#element-185). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-185 |
| Name | Releasable assembly joint |
| Type | Mechanical |
| Anchor | [requirements.md#element-185](../requirements.md#element-185); [named entry](../invest/named-elements.md#element-185); scope [todo-321](../requirements.md#todo-321) |
| Proof owner | S364 |
| Refines / extends | No CAT spec. Attaches structural members such as EL-112 Structural beam (GAP-01); release command from an activation source such as [CAT-063 switch](CAT-063-switch.md) |
| Related | EL-186 Temporary bridge (uses this joint), EL-159–EL-161 load-limited connectors (failure by load, not command), EL-205 Rope (Story 10.2 "releasing tension uncouples the bodies") |
| Roadmap story | [EL-185 delivery slice](../invest/named-elements.md#element-185); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP; then ENGINE-JOINT-KINDS (fixed joint) and ENGINE-ASSEMBLY-JOINTS (player-made mount-to-point connections, signal-commanded release, released-state read); then one attachable part; then S7. |

### Disposition (21 November 2026)

**To deliver as a single element** after its engine stories. Planned package id `release_joint` (kept from this spec), directory `elements/ReleaseJoint/`, title "Release clamp", bound by this spec's final package-source-binding section once S7 widens that binding's preconditions (section 7); no ledger line binds it. Owner key S364. It is one of the nine Construction identities ENGINE-ASSEMBLY-JOINTS unblocks ([epics](../../../_bmad-output/planning-artifacts/epics.md), "Story ENGINE-ASSEMBLY-JOINTS").

## 2. Declaration

### Declaration to write (21 November 2026)

This is the starting declaration. The attachment and release descriptors do not exist yet; their final names and fields are fixed by the engine story's Code Map and this subsection is then corrected to them. Closest delivered shape: `elements/DelayBox/` for a body with one Signal input (`ActivationIn` at `DelayBoxElementDeclaration.cs:38`), and `elements/Weight/` for a small dynamic body.

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, the signal-input interface the Delay box uses for `ActivationIn`, and the attachment interface ENGINE-ASSEMBLY-JOINTS adds (missing).
- **Body.** One dynamic clamp body: box 0.20 × 0.20 × 0.20 m with a box collider of the same size: a static or proud visible solid has a collider (owner rule, visible = solid). Mass 0.1 kg, constant; moments m·a²/6 = 0.00067 kg·m² on each axis; gravity (0, −9.81, 0) m/s². Reason: light enough not to change the balance of a 2 kg deck or a 1 kg ball it holds.
- **Material.** Restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.
- **Attachment sockets.** `MountA` at (−0.1, 0, 0) and `MountB` at (+0.1, 0, 0), each a mount joined rigidly to one point of another placed part. In the Construction socket vocabulary (`readiness/batch-m.md` section 2 and section 4 item 1) a connection joins one mount to one point, and the mount-carrying body and the point-carrying body do not collide while joined. The rigid hold is a fixed joint (ENGINE-JOINT-KINDS); the player-made connection that creates it, its signal-commanded release and the released-state read are ENGINE-ASSEMBLY-JOINTS. `MountA` is permanent for the Run; `MountB` is the releasable one. The clamp therefore stays on body A after release and nothing disappears.
- **Signal port.** `ReleaseIn`: `SimulationDomain.Signal`, `PortDirection.Input`, `PortRole.ActivationIn`, pulse kind, at (0, 0.1, 0) on the top face. One activation occurrence commands the release of `MountB`.
- **Parameters.** None. A single-valued `kind` choice would not be a control (D13).
- **Stores.** None. **Events.** One: the released occurrence, if the engine story's read model exposes it; otherwise the committed released state alone.
- **Presentation.** Pin colour gold `#f7cb52` to slate `#556573` from the committed released state over 0.16 s (SmoothStep); the driver is the released-state read the engine story adds (the prismatic counterpart is `PresentationDriver.CommittedRetentionReleased`, `CuriousContraptions.Abstractions/ElementPresentation.cs:32`).
- **Catalogue.** Id `release_joint`, title "Release clamp", description "Holds two parts together until it is signalled.", category `ElementCategory.Structure`, palette order 516 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour navy `#293954`.
- **Footprint.** Bodies 1, moving bodies 1.

### Art and solidity

| Node | Mesh | Class | Collider or reason |
| --- | --- | --- | --- |
| `Visual/Visual_Clamp` | navy box 0.20 m cube | solid | the clamp box |
| `Visual/Visual_Pin` | gold disc 0.06 m across on the top face, flush | decorative | printed flat; recoloured by a binding |
| `Visual/Visual_JawA`, `Visual_JawB` | cream chevrons on the two mount faces, flush | decorative | printed flat |

Icon: a square clamp with a pin and two opposed chevrons. It differs from EL-159 to EL-161 (load-limited connectors, which fail by load) by the pin and the signal socket, a shape difference, not colour.

### Harvest-era subsections — removed; the text is in git history.

## 3. Engine capabilities

Families: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, StructuralFracture, TopologyTransaction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)). Map: "Commanded joint release conserves momentum; inherited PhaseTopology does not require melting."

### Capability probe (21 November 2026)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Small dynamic box body with declared mass | present | `IMechanicalElement.MassFrame` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:10`) | `WeightTests.TheBoundaryMassesAreAdmittedAtEverySeam` (`CuriousContraptions.tests/WeightTests.cs:70`) |
| Pulse input port on a part | present | `PortRole.ActivationIn` (`CuriousContraptions.Abstractions/PortDescriptor.cs:25`); `elements/DelayBox/DelayBoxElementDeclaration.cs:38` | the Delay box and Hold timer suites (not re-opened here; the slice's probe cites the exact test) |
| A commanded release of a constraint, restored by Reset | narrower | exists for a tensile span only, as a UI command: `CutRope(RopeSpanId)` (`CuriousContraptions.Abstractions/RuntimeServices.cs:77`), `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`CuriousContraptions.tests/RopeTests.cs:151`). No signal can command it and no other constraint kind can be released | ENGINE-ASSEMBLY-JOINTS ("a commanded release of such a connection") |
| Attachment sockets on a part | present, untested | `AnchorPointDescriptor` and `IMechanicalElement.AnchorPoints` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`; `MechanicalContracts.cs:12`); declared by one delivered package (the Powered gate's `slider` anchor, `elements/PoweredGate/PoweredGateElementDeclaration.cs:69`–`:72`), validated and bundled, and never compiled | ENGINE-ASSEMBLY-JOINTS builds on it or deletes it (forward-only: no second socket contract beside it) |
| A connection the player makes between two placed parts (rigid) | missing | connection domains are Activation, Electrical, Rope and Mechanical only (`engine/gpu/WorkshopConnections.cs:11`); constraints are declared inside one package | ENGINE-ASSEMBLY-JOINTS ("connections the player makes between two separately placed parts at typed attachment sockets (rigid, hinged or sliding)") |
| A rigid (weld) constraint | missing | constraint kinds are revolute, prismatic and tensile (`CoordinateKind`, `PortDescriptor.cs:43`) | ENGINE-JOINT-KINDS (fixed joint); ENGINE-ASSEMBLY-JOINTS makes it from a player-made connection |
| Released-state read and presentation driver | missing | only `CommittedRetentionReleased` for a prismatic retention (`ElementPresentation.cs:32`) | ENGINE-ASSEMBLY-JOINTS (shared acceptance: the read model is part of the story) |

Counts: 2 present, 1 present-untested, 1 narrower, 3 missing; owners ENGINE-ASSEMBLY-JOINTS (player-made connections at typed mount and point sockets, signal-commanded release, released-state read) and ENGINE-JOINT-KINDS (the fixed joint kind). Gap in that story's text, recorded in the batch summary: its acceptance says "a commanded release frees the parts in the tick it commits" but does not say the command can be a declared signal input, which this identity and EL-186 need.

**Dependencies.** Joint lifecycle (Stories 6.4, 10.3); EL-112 structural beam or other dynamic parts to attach; an activation source.

## 4. Sources and legacy

- Requirement: "Typed release removes a specific physical attachment with conserved momentum"; outcome "Unreleased attachment retains load; unsupported command is rejected" ([element-185](../requirements.md#element-185)).
- [todo-321](../requirements.md#todo-321): release a supported temporary assembly and see the resulting physical motion; Reset restores its authored attachments. Campaign: "releasable bridge/joint", first use 41–50 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Joint edits are explicit Attach, Replace or Detach; Attach needs a fresh identity, Replace/Detach an existing one; no upsert | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | carry forward (closed command set) | Typed release of a specific joint. |
| 2 | Accepted: detaching a driven joint ends its force but keeps the work already supplied; the body keeps its velocity; Reset replays identically | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L106-L139` | carry forward | Momentum and work conservation on release. |
| 3 | Accepted: a missing detach, duplicate attach, conflicting batch, foreign body or off-axis attach is rejected and nothing changes | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L180-L220` | carry forward (rejection), do not carry forward (whole-step rollback by exception) | Rejection is admission; the current worker never faults a tick. |
| 4 | Accepted: two effects detaching the same joint in one step reject instead of silently overriding | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L223-L238` | carry forward | Unambiguous release ownership. |
| 5 | Accepted: an impact releases a rope and the load falls; a missed impact leaves the load held at rest | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L84-L104` | carry forward | Released versus unreleased control. |

**Files harvested:**
- `engine/physics/PhysicsJointChange.cs`
- `CuriousContraptions.tests/PhysicsImpactJointTests.cs`
- `parts/LatchPart.cs` (typed command rejection)
- Checked, no element knowledge: `engine/physics/PhysicsLatchedSpring.cs` (its "releasable transmission" is the wound-spring latch of CAT-071).
- Searched with no hit for `releasable` (other than that comment) or a release-clamp part: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-185](../requirements.md#element-185) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place a beam bracket and a dynamic box on it; join them with the release clamp through `MountA`/`MountB`; wire a switch `ActivationOut` → clamp `ReleaseIn`; drop a ball on the switch. Run.
- **Positive.** On the switch the clamp opens and the box falls away with the velocity it had; the bracket stays put.
- **Negative/control.** Without the command the box stays attached under load; an electrical wire into `ReleaseIn` is refused; releasing an already released clamp is rejected visibly.
- **Boundaries.** Release while the assembly is moving (momentum conserved); two release commands in one tick; release with the box resting on another body.
- **Run/Reset.** The attachment is restored exactly.
- **Save/Load.** Mount endpoints and the release wire round-trip.
- **Integrations.** Releasable assembly and bridge task [todo-321](../requirements.md#todo-321) with EL-186; campaign row "... releasable bridge/joint": first use 41–50, reuse 61–100, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); release commanded by CAT-063 switch; GAP-05 deliberate-release lesson at 44 ([campaign gap allocation](../requirements.md#campaign-gap-allocation)).

### Behaviour and numbers (21 November 2026)

- **Law.** While attached, bodies A, clamp and B move as one rigid assembly. The release removes the `MountB` constraint at a step boundary and applies no impulse: each body keeps its committed linear and angular velocity, so linear and angular momentum are unchanged across the release step, and contact between the freed bodies becomes active.
- **Tick order.** The pulse on `ReleaseIn` is read in the signal phase; the constraint is absent from the first physics step after the command commits, the rule the rope cut already follows.
- **Once per Run.** A second pulse after release leaves the committed released state unchanged and publishes no second released occurrence (a wired signal pulse has no command result to refuse); Reset restores the attachment.
- **Scale.** The 0.2 m clamp is smaller than every ball at its default size (the Tennis ball's default radius is 0.25 m) so it reads as hardware, not cargo.

### Acceptance rows (21 November 2026)

Test class `ReleaseJointTests`. Tolerances: momentum across the release step within 1 % plus 0.01 kg·m/s (a release kick of even 0.1 m/s on a 1 kg box is ten times that); "holds" means the held body moves less than 0.005 m relative to its mount over 5 s. Stall guards 10 game seconds.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given a 1 kg body clamped to a fixed part over a drop, when no signal arrives for 5 s, then it stays attached under its own weight | `ReleaseJointTests.AnUnreleasedClampHoldsItsLoad` | the same body with no clamp falls | a 4 kg body is also held |
| A2 | Given a switch wired to `ReleaseIn`, when a ball strikes the switch, then the held body falls and the fixed part and clamp stay where they were | `ReleaseJointTests.ASignalReleasesOnlyItsOwnAttachment` | a second clamp in the scene with no signal still holds | two pulses in one tick release once |
| A3 | Given the assembly moving at 2 m/s when released, then total linear momentum before and after the release step agrees within the tolerance and neither body gains speed beyond gravity's | `ReleaseJointTests.ReleaseAddsNoImpulse` | — | release while the held body rests on another body |
| A4 | An electrical wire to `ReleaseIn`, or a mount to a part without an attachment socket, is refused with a typed reason and changes nothing | conformance suite row 5 (S4) once it lands; until then `ReleaseJointTests.ForeignConnectionsAreRefused` | an activation wire is accepted | — |
| A5 | Given a released clamp, when a second pulse arrives on `ReleaseIn`, then the committed released state is unchanged and no second released occurrence is published | `ReleaseJointTests.ASecondPulseChangesNothing` | the first pulse publishes exactly one | — |
| A6 | Reset restores the attachment and the pin colour; save and load round-trip both mounts and the release wire, before and after a release | `ReleaseJointTests.ResetAndSaveLoadRestoreTheAttachment` | — | — |

Requirement rows preserved: "unreleased attachment retains load" (A1), "unsupported command is rejected" (A4, A5), "conserved momentum" (A3), "removes a specific physical attachment" (A2).

### Integration

- **Works with.** An activation source: `switch`, `pressure_plate`, `ball_detector`, `delay`, `hold_timer` (all delivered). Attachable parts: none is delivered. The first planned part with attachment sockets is EL-112 Structural beam (another batch); this identity cannot prove A1 before at least one attachable part exists, unless the engine story gives the Wall and a dynamic box sockets as its qualification fixtures and they become product data.
- **Campaign.** "Releasable bridge/joint" in [campaign element coverage](../requirements.md#campaign-element-coverage): first use 41–50, reuse 61–100, 111–120, 136–150; the GAP-05 deliberate-release lesson at 44. [todo-321](../requirements.md#todo-321) defers it until connection lifecycle is reliable, which the engine story's acceptance establishes.
- **Coverage binding.** A final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).
- **Lesson fixtures (S8).** Needed: the lessons lock the structure and leave only the signal wiring to the player.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP; then ENGINE-JOINT-KINDS (fixed joint) and ENGINE-ASSEMBLY-JOINTS (player-made mount-to-point connections, signal-commanded release, released-state read); then at least one attachable part; then S7 (the coverage follow-on, which widens the reader, owner decision 15).

1. ENGINE-JOINT-KINDS (fixed joint) and ENGINE-ASSEMBLY-JOINTS (engine tier), the latter including the signal-commanded release and the released-state read.
2. At least one attachable part (EL-112, or sockets added to a delivered part in its own slice).
3. This element slice: `elements/ReleaseJoint/` (project, declaration, scene, icon), `CuriousContraptions.tests/ReleaseJointTests.cs`, the solution line, the art classification, and the binding section of section 7 once S7 admits it. Expected tier: data-only if the engine story leaves every row *present* with a test; otherwise engine.
- **Before EL-186**, which reuses the same release primitive.
- **Disqualified if** the engine story makes a player-made joint a connection with no body (then this part becomes a connection kind with a commanded release and no package, and the clamp art is the connection's artwork), or if it does not deliver a signal-commanded release.

## 6. Open questions

**Resolved under the delegation.**

1. A releasable pin (hinge) besides the weld: not in this part. Reason: no requirement row or lesson uses it (D19); a hinged connection is a kind the engine story offers to a later identity.
2. How the player chooses the two bodies: typed attachment sockets, as the engine story states: this part's two mounts, each joined to one point of another part (`readiness/batch-m.md` section 2).
3. Re-attach during a Run: no; Reset restores (the engine story's acceptance).
4. Part or connection: a small solid part, so the release input has a body to sit on and nothing visible lacks a collider.
5. A second pulse is a no-op observed on committed state, not a typed refusal (22 November 2026, audit M16): a wired signal has no `WorkshopCommandResult`.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One small dynamic body, two declared attachment sockets, one signal input and one colour binding. The release is a general commanded release of a declared attachment; nothing here is clamp-specific code.
2. **Generic laws.** A rigid attachment constraint between sockets of two bodies; its removal at a step boundary with no impulse; contact afterwards. The capabilities that do not exist yet are stated element-free, each with one owner: the fixed joint kind (ENGINE-JOINT-KINDS); attachment sockets compiled to constraints from player-made mount-to-point connections, a signal-commanded release and the released-state read (ENGINE-ASSEMBLY-JOINTS). The story's text gaps are in the batch summary.
3. **Three layers.** Colliders: one box on the clamp body. Visuals: the clamp cube with a flush pin disc and flush chevrons; none is a collider. Animation: the pin's colour, bound to the committed released state; it reads state and writes nothing back. `Visual/Visual_Clamp` follows the body's pose.
4. **Flow and typing.** One Signal-domain `ActivationIn` port; sockets are typed mounts; no parameters.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (box); attachments 2 (one releasable), compiled by the engine story; drives 0; sensors 0; signal inputs 1; stores 0; presentation bindings 1 (colour).
6. **Performance.** One primitive collider. No island sleeping exists today; a resting attached assembly costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. A release changes the constraint set at a step boundary, which the engine story must do without allocating during a tick; that requirement belongs in the story's acceptance (batch summary).
7. **Tests are scenarios.** Rows A1 to A6 run the production simulation; they pass or fail on the general attachment and release law.

**Verdict: conforms; blocked on ENGINE-JOINT-KINDS and ENGINE-ASSEMBLY-JOINTS for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `release_joint` |
| Named identity | EL-185, `../invest/named-elements.md#element-185` |
| Acceptance | `todo-321` with its co-anchored `sequence-task-374` |
| Decision | S364, `../invest/scope-corrections.md#s364` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s364` block on published main; and `todo-321` with `sequence-task-374` is one task source shared with EL-186, while the reader refuses two packages on one task source. The ledger entry itself qualifies (one todo link, one owner key).
