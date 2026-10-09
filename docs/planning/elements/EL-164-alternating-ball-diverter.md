# EL-164 · Alternating ball diverter named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-164 |
| Name | Alternating ball diverter |
| Type | Mechanical |
| Anchor | [requirements.md#element-164](../requirements.md#element-164); [named entry](../invest/named-elements.md#element-164); scope index [todo-246](../requirements.md#todo-246); combination [todo-319](../requirements.md#todo-319) |
| Proof owner | S375 |
| Refines / extends | No CAT spec. Junction and actuator of [EL-163](EL-163-powered-ball-diverter.md); passage sensing of [CAT-002 ball detector](CAT-002-ball_detector.md); supply from [CAT-005 battery](CAT-005-battery.md) |
| Related | EL-162, EL-163 (same scope index); CAT-020 Counter (the todo-319 combination) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
All full extents.
- **Junction (static), blade (dynamic) and actuator housing.** As EL-163: Y channel 1.5 × 0.1 × 0.9 m branches at ±30°, 0.5 m walls, 6° fall; blade 1.0 × 0.5 × 0.06 m hinged at the apex between ±15°; housing 0.4 × 0.3 × 0.4 m — **proposed**, shared so the three diverters read as one family.
- **Two exit apertures.** Planar 0.9 × 0.6 m openings across each branch, 1.2 m past the apex — **proposed**: far enough downstream that a ball has fully left the blade's sweep before it counts as passed.

### Mass and material
Blade 0.5 kg; channel and blade restitution 0.1, friction 0.3 — **proposed**, as EL-163.

### Constraints and joints
Blade hinge −15° to +15° with the EL-163 finite-work servo (maximum 4 rad/s, 16 rad/s², 15 N·m, 40 W) — **proposed**, as EL-163; servo modes Hold/Lower/Upper (`engine/physics/PhysicsServo.cs@a6c914e:L9-L9`).

### Typed ports
| Socket | Domain | Direction | Source |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | existing identity `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`; requirement "supplied blade position" |

No activation input: alternation is driven only by observed passages.

### Sensors and activation
- Each exit aperture is a directional passage sensor. One forward passage through the currently open branch is one occurrence; it requests the opposite branch. Reverse, stationary and outside-aperture motion publish nothing (row 2 below).
- After a passage the sensor rearms only once the ball has physically cleared upstream of the aperture (row 3 below), so a ball resting in the aperture cannot request repeated toggles.
- The requested target is typed discrete state (`DiverterBranch`), applied by the servo only with supply.

### Work and energy stores
None internal; blade work comes from `PowerIn`.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `initial_branch` | enum `DiverterBranch { Left, Right }` | 2 values | Left | — | **proposed**: shared enum with EL-162 and EL-163 |

### Cosmetic curves and UI bindings
- Blade follows the committed hinge pose; each aperture frame flashes pale green `#bdf4bd` for 0.16 s when it counts a passage — **proposed**, reusing the receiver halo colour (`parts/BasketPart.cs@a6c914e:L16-L16`) and the sourced 0.16 s activation curve (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L53-L53`).
- UI: `initial_branch` through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Ramp colour `#c28f52` channel, gold `#f7cb52` blade, cream `#fff8e9` aperture frames, navy `#293954` housing ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `diverter_alternating`, title "Alternating diverter", category Motion — **proposed**. Add `WorkshopPartKind.AlternatingDiverter` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; fixed and powered modes are EL-162 and EL-163.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)). Map: "Actual completed passage occurrence drives alternating target state; separately supplied actuator moves physical blade."

**Exists now**
- Sticky one-shot activation latches and a discrete activation network: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`.
- Residence sensing against a named body: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`.

**Missing**
- Directional aperture passage sensor with rearm clearance: Story 6.8 (CAT-002). Repeating (non-sticky) occurrences per Run: Story 9.2 counter family. Owner S375.
- Hinge, servo and supply: as EL-163 (Stories 10.3, 8.2, 8.1).

**Dependencies.** EL-163 actuator; CAT-002 sensor (Story 6.8); CAT-005 battery (Story 8.1).

## 4. Sources and legacy

- Requirement: "Observed completed passage requests the next supplied blade position"; outcome "Held arrival signal cannot alternate repeatedly without new passages" ([element-164](../requirements.md#element-164)).
- [todo-319](../requirements.md#todo-319): dispenser + alternating junction + counter distribute six balls into two groups of three; withholding an arrival prevents the goal.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Passage sensor declaration: frame, aperture radius, rearm clearance | `engine/ScenePassageSensorDeclaration.cs@a6c914e:L3-L4` | carry forward (data shape) | Exit sensors. |
| 2 | Accepted: outside-aperture passage disarms without counting; reverse or stationary motion publishes nothing | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L96-L107` | carry forward | Only completed forward passages count. |
| 3 | Accepted: re-enabling requires physical upstream clearance; one forward pass counts exactly once | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L109-L124` | carry forward | "Held arrival cannot alternate repeatedly." |
| 4 | Explicit typed command sockets rather than a toggle | `parts/LatchPart.cs@a6c914e:L28-L33` | carry forward (typing) | Target state is a closed enum. |

No legacy alternating diverter exists.

**Files harvested:**
- `engine/ScenePassageSensorDeclaration.cs`
- `CuriousContraptions.tests/PhysicsPassageSensorTests.cs`
- `engine/physics/PhysicsServo.cs`
- `parts/LatchPart.cs` (typed command sockets)
- `parts/BasketPart.cs` (halo colour)
- Searched with no hit for `diverter`, `alternating` or `junction`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-164](../requirements.md#element-164) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place a ball queue on a ramp, the diverter, a battery and receivers under both branches; connect `Supply` → `PowerIn`. Run.
- **Positive.** Successive balls alternate left, right, left, right; six balls give three per branch.
- **Negative/control.** A ball parked inside an exit aperture causes no further switching; without supply the blade stays and every ball takes `initial_branch`; a ball rolling back through an aperture does not count.
- **Boundaries.** Two balls closely spaced (the second may arrive mid-swing); a jammed blade; Bowling ball and Basketball.
- **Run/Reset.** Blade at `initial_branch`; sensor counts and arming clear.
- **Save/Load.** `initial_branch`, pose and supply link round-trip.
- **Integrations.** Y-junction integration task under [todo-246](../requirements.md#todo-246) (alternating mode proved after EL-162 and EL-163); six-ball dispenser + alternating junction + counter combination [todo-319](../requirements.md#todo-319) with CAT-020; campaign row "... fixed/powered/alternating diverter ...": first use 21–30, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

## 6. Open questions

1. Should each exit expose an `ActivationOut` so counters can observe passages? Unspecified — owner decision.
2. If a second ball arrives mid-swing, is it the player's problem (proposed) or should the blade lock while a ball is in the junction? Owner decision.
