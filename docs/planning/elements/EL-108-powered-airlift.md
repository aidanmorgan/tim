# EL-108 · Powered airlift named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-108 |
| Name | Powered airlift |
| Type | Mechanical |
| Anchor | [requirements.md#element-108](../requirements.md#element-108); [named entry](../invest/named-elements.md#element-108); scope index [todo-248](../requirements.md#todo-248) |
| Proof owner | S480 |
| Refines / extends | No CAT spec. Combines the open airflow field of [CAT-028 fan](CAT-028-fan.md) with the tube contract of [CAT-048 pipe](CAT-048-pipe.md) |
| Related | EL-209 Fan (same air field family), EL-107, EL-109 (same scope index), CAT-005 Battery (supply) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
- One static body: a vertical clear tube with standard bore 0.65 m, wall 0.70 m and collars 0.78 m (`engine/gpu/WorkshopPipe.cs@a6c914e:L8-L19`, uncompiled current-tree declaration). Sourced so feed and exit tubes snap to it.
- Tube height 2–6 m, default 4 m — **proposed**: lifts cargo from bench level to a typical upper route without leaving the 9.8 m-deep bench view.
- Blower housing at the bottom: static box 1.6 × 0.5 × 1.6 m — **proposed**: wider than the 1.56 m collar so the housing reads as the tube's base. Side `Inlet` mouth just above the housing; `Outlet` mouth at the top.
- Air field: a cylinder of radius 0.65 m (the bore) and length equal to the tube height, directed along local +Y. The field is the legacy finite cylindrical jet (`engine/AirflowNetwork.cs@a6c914e:L11-L13`) confined to the bore.

### Mass and material
- Static, no mass. Inner wall material as the clear tube: restitution 0.15, bounce threshold 0.1 m/s, friction 0.3 (`parts/PipeBendPart.cs@a6c914e:L12-L12`) — sourced tube material.

### Constraints and joints
None.

### Typed ports
| Socket | Domain | Direction | Source |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | existing socket identity `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`; placement on the housing face — **proposed** |

Tube mouths `Inlet` and `Outlet` (bore 0.65 m) are geometric, not connection sockets.

### Sensors and activation
None. Supply alone enables the field; there is no activation input (the requirement says "supplied airflow").

### Work and energy stores
- No internal store: every newton-metre of lift comes from the connected electrical supply (finite battery, Story 8.1). Emission power is bounded by the supply; the envelope's no-free-energy rule applies (ΔK ≤ E_store; [envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- Only work done while the cargo is actually exposed to the field is drawn from the supply; occlusion and exposure checks themselves never create or need supply (legacy rows 6 and 7 below).
- The legacy fan used a self-contained 14,400 J store (`parts/FanPart.cs@a6c914e:L12-L16`); that is not carried forward for a supplied airlift (open question 2).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `height` | f32 | 2–6 | 4 | m | **proposed** (see Bodies) |
| `force` | f32 | 0–40 | 15 | N maximum field force on one cargo | range sourced from the fan (`parts/FanPart.cs@a6c914e:L24-L31`); default **proposed**: lifts a 1 kg Basketball (9.81 N) but not a 4 kg Bowling ball (39.2 N) |

Reference flow speed 12 m/s is a sourced field constant (`engine/AirflowNetwork.cs@a6c914e:L24-L24`).

### Cosmetic curves and UI bindings
- Blower rotor spins while supplied: one revolution (0 → 2π) per 0.349 s (animation duration τ/18), looping, start/stop on owner activity, as the legacy fan rotor (`parts/FanPart.cs@a6c914e:L50-L53`). Needs a new supply feedback source; the closed `AnimationFeedbackSource` set has none (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).
- UI: `height` and `force` through the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Clear pipe shell `#66b8c9` alpha 0.16 with cream `#fff8e9` collars; blower housing in the Fan colour `#66b8c9` on a dark slate base `#263d4b` (`parts/FanPart.cs@a6c914e:L36-L36`); airflow streak lines `#a9e7e0` (`parts/FanPart.cs@a6c914e:L48-L48`). Palette per [DESIGN colour system](../../../DESIGN.md#colour-system).

### Catalogue and inventory entry
Id `airlift`, title "Powered airlift", category Power — **proposed**, matching the fan's category (`parts/catalog/fan.tres@a6c914e:L8-L10`). Add `WorkshopPartKind.Airlift` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome, no named variants in the row. "Airlift ... variants" in the campaign coverage row are unnamed ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); open question 1.

## 3. Engine capabilities

Families: AerodynamicDrag, EnvironmentState, FiniteLedger, FiniteWorkActuation, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Dynamic sphere cargo with gravity and linear drag: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.
- Bounded force regions are part of the solver model ("acceleration clamped to ≤ 64 m/s²", [capability inventory](../../gpu-f32-physics.md#capability-inventory)); the only implemented region is the receiver planar guide (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L144-L161`).

**Missing**
- Open airflow field with finite paid emission and occlusion (AerodynamicDrag/FiniteWorkActuation): Story 12.2 (CAT-028). Decisions S470 open-versus-sealed (named next implementation S697) and S416/S470 boundary ([decisions](../invest/decisions.md#s470)).
- Electrical supply: Story 8.1 (CAT-005).
- Hollow tube collider: Stories 6.6, 6.7, 6.9 and 6.10.

**Dependencies.** CAT-028 fan field (Story 12.2), CAT-005 battery (Story 8.1), CAT-048 tube (Story 6.6).

## 4. Sources and legacy

- Requirement: "Supplied airflow lifts compatible cargo through a physical passage"; outcome "Insufficient airflow cannot lift the load" ([element-108](../requirements.md#element-108)). Map decision: S416/S470.
- The air-jet facts below match the [CAT-028 harvest](CAT-028-fan.md#legacy-harvest) (its facts 23, 24, 27 and 29).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Fan parameters: force 0–40, reach (0, 12], width (0, 4], powered 0/1; non-finite rejected | `parts/FanPart.cs@a6c914e:L24-L31` | carry forward (force range) | Bounded field strength. |
| 2 | Catalogue defaults force 9, reach 5, width 0.85 | `parts/catalog/fan.tres@a6c914e:L14-L14` | do not carry forward as airlift defaults | Fan geometry, not a confined lift. |
| 3 | Field is a finite cylinder with an explicit conserved source and impedance; reference flow speed 12 m/s | `engine/AirflowNetwork.cs@a6c914e:L11-L13`, `engine/AirflowNetwork.cs@a6c914e:L24-L24` | carry forward | Same open-field family. |
| 4 | Bodies owned by the source or target are excluded; other solid bodies occlude the jet | `engine/AirflowNetwork.cs@a6c914e:L62-L74` | carry forward (occlusion), do not carry forward (per-step CPU network) | Per-element step loop is banned. |
| 5 | Accepted: jet acceleration is 6/mass for 1 kg and 4 kg; a blocker gives zero acceleration and releases no energy; kinetic energy ≤ released energy | `CuriousContraptions.tests/AirJetWorldTests.cs@a6c914e:L18-L66` | carry forward | Mass-scaled lift and the blocked control. |
| 6 | Accepted: a blocker moving through the jet cuts the force only while it overlaps the jet (receiver speed 0.4 m/s for one blocker, 0.3 m/s for a two-part blocker over 0.6 s); released energy equals 4 W × exposed time; only exposed work is committed and the rest is dissipated | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L13-L40` | carry forward (behaviour at substep resolution) | Exposed-only work. |
| 7 | Accepted: exposure and occlusion neither need nor create supply; an unpowered load has zero demand and changes no body; excluded bodies do not occlude | `CuriousContraptions.tests/AirJetGeometryTests.cs@a6c914e:L8-L32` | carry forward | Exposure never creates supply. |
| 8 | Accepted: a stationary blocker occludes a moving receiver only while its collider participates; a non-colliding blocker never occludes | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L48-L72` | carry forward | Non-colliding parts do not block. |
| 9 | Accepted: inlet, outlet and rim crossings in both directions switch exposure on and off; a source whose collider is disabled emits no field | `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L16-L45`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L126-L135` | carry forward (force starts and stops at the field boundary) | Field edges. |
| 10 | Continuous root-finding sweeps for boundary and occlusion times (tolerance 1e-7, tangency and initially-on-surface handling, sweep validation), and whole-world capture/restore replay | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L41-L45`, `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L74-L90`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L102-L124`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L136-L143` | do not carry forward | CPU continuous-sweep solver path and rollback; the worker samples at 480 Hz substeps and never rolls back a tick. |
| 11 | Self-contained supply 14,400 J | `parts/FanPart.cs@a6c914e:L12-L16` | do not carry forward | The airlift is externally supplied. |

**Files harvested:**
- `parts/FanPart.cs`
- `parts/catalog/fan.tres`
- `parts/PipeBendPart.cs` (tube material)
- `engine/AirflowNetwork.cs`
- `CuriousContraptions.tests/AirJetWorldTests.cs`
- `CuriousContraptions.tests/AirJetOcclusionTests.cs`
- `CuriousContraptions.tests/AirJetGeometryTests.cs`
- `CuriousContraptions.tests/AirJetBoundaryTests.cs`
- Searched with no hit for `airlift` or `lift tube`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-108](../requirements.md#element-108) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place the airlift, a battery and a feed ramp into `Inlet`; connect battery `Supply` → airlift `PowerIn` with the contextual Connect; place a receiver at `Outlet`. Run.
- **Positive.** A Basketball rises through the tube and leaves at the top.
- **Negative/control.** A Bowling ball (39.2 N weight above the 15 N field) stays at the bottom; no battery gives no lift; a depleted battery stops lifting mid-tube and the cargo falls back; a solid part inside the tube blocks the field only while it is there, and a non-colliding part does not block; an unsupplied airlift draws nothing even with cargo in the field.
- **Boundaries.** `force` 0 and 40; `height` 2 and 6; a ball held against the inlet wall; cargo crossing the inlet and outlet field edges.
- **Run/Reset.** Cargo, supply and field state return to authored values.
- **Save/Load.** `height`, `force`, pose and the electrical connection round-trip.
- **Integrations.** Scope index [todo-248](../requirements.md#todo-248) (no separate integration task); campaign row "spiral delay, airlift and trapdoor variants": first use 21–30, specialist routes 91–100, reuse 41–50, 111–120, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); battery supply (CAT-005) and tube routes (CAT-048–CAT-050).

## 6. Open questions

1. What are the "airlift variants" named in the campaign coverage row? Unspecified — owner decision.
2. Supply: external electrical only (proposed) or the legacy self-contained fan store? Owner decision.
3. Is cargo compatibility only bore size and weight, or does it include material (for example balloons)? Owner decision.
4. Does the field act on cargo outside the tube near the outlet? Owner decision.
