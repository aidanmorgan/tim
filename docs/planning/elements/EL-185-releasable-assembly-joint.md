# EL-185 · Releasable assembly joint named-identity spec

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
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- No body of its own. The joint binds two existing bodies at one anchor. A clamp mesh 0.2 × 0.2 × 0.2 m (full extents) is drawn at the anchor and has no collider — **proposed**: visible but never an extra contact.

### Mass and material
None: the joint is massless; the attached bodies keep their declared masses.

### Constraints and joints
- One weld joint locking all relative motion between body A and body B at the anchor; connected-body collision disabled while attached — **proposed**: a weld is the simplest "physical attachment"; a legacy slider with travel (0, 0) served as a lock (`CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L56-L66`).
- On a valid release the joint row is removed atomically at a substep boundary; both bodies keep their committed linear and angular velocity, so momentum is conserved; connected collision becomes enabled.

### Typed ports
| Socket | Domain | Direction | Source |
| --- | --- | --- | --- |
| `MountA`, `MountB` | Structural | Bidirectional | **proposed**: two typed attachment sockets; the structural domain is shared with EL-112's "declared attachment sockets" |
| `ReleaseIn` | Activation | Input, command Release | **proposed**: an explicit typed command input; other commands reject, as the latch rejects anything but Set/Reset (`parts/LatchPart.cs@a6c914e:L37-L44`) |

### Sensors and activation
`ReleaseIn` consumes one activation occurrence and requests `Release`. A second release of an already released joint is rejected, not ignored silently (legacy rejects a detach of a missing joint, row 3).

### Work and energy stores
None. Release adds no impulse and spends no work; any drive work already spent stays spent (row 2).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `kind` | enum `AttachmentKind { Weld }` | 1 value | Weld | — | **proposed**: one supported kind now; open question 1 asks whether a releasable pin is also required |

### Cosmetic curves and UI bindings
- Clamp art follows body A's committed pose; on release it opens over 0.16 s SmoothStep (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L53-L53`) — **proposed**; the opening is cosmetic only.
- UI: place the joint by selecting two parts with the contextual Connect (Structural domain); wire `ReleaseIn` like any activation input.

### Art
Navy `#293954` clamp body with a gold `#f7cb52` release pin; when released the pin shows slate `#556573` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `release_joint`, title "Release clamp", category Motion — **proposed**. Add `WorkshopPartKind.ReleaseJoint` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; no variants. [todo-321](../requirements.md#todo-321) defers it until compound-body and connection lifecycle behaviour is reliable.

## 3. Engine capabilities

Families: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, StructuralFracture, TopologyTransaction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)). Map: "Commanded joint release conserves momentum; inherited PhaseTopology does not require melting."

**Exists now**
- Typed activation inputs and one-shot latches: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`; `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`.
- Committed f32 angular velocity record, carried across ticks: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L22-L30`.

**Missing**
- Any joint row (JointConstraint): first in Story 6.4. A weld kind and a runtime joint-removal transaction (TopologyTransaction): unscheduled. Decision S543 phase-topology, next implementation S565 ([decisions](../invest/decisions.md#s543)). Owner S364.
- The Structural connection domain and `MountA`/`MountB` sockets: GAP-01 work, unscheduled.

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

## 6. Open questions

1. Is a releasable pin (hinge) attachment also required besides the weld? Unspecified — owner decision.
2. How does the player choose the two attached bodies: structural sockets (proposed) or a placement-time contact pick? Owner decision.
3. Can a released joint re-attach during a Run? Proposed no. Owner decision.
