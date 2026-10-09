# EL-209 · Fan named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-209 |
| Name | Fan |
| Type | Mechanical |
| Anchor | [requirements.md#element-209](../requirements.md#element-209); [named entry](../invest/named-elements.md#element-209); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S313 |
| Refines / extends | Extends [CAT-028 fan](CAT-028-fan.md) ([requirement](../requirements.md#current-cat-028)): CAT-028 is a self-contained activation-controlled fan; EL-209 requires explicit electrical or shaft supply |
| Related | EL-108 Powered airlift (same field), EL-196 Battery, EL-199 Electric motor (shaft source), CAT-069 Wind chimes and CAT-070 Windmill (receivers) |
| Roadmap story | Electrical variant: Story 12.2 (its acceptance names a battery-powered fan). Shaft variant: unscheduled |
| Status | not started |

## 2. Declaration

Geometry, field and parameters are sourced from the legacy fan; the supply ports are the EL extension.

### Bodies and shapes
- **Base (static).** Box full size 0.65 × 0.18 × 1 m at (0, −0.55, 0) (`parts/FanPart.cs@a6c914e:L36-L36`; `AddBox` stores halves).
- **Art.** Post 0.13 × 0.5 × 0.15 m; housing ring radius 0.56 m, tube 0.07 m; rotor hub sphere 0.14 m; three blades 0.1 × 0.6 × 0.16 m; airflow line from x 0.6 to 1.15 (`parts/FanPart.cs@a6c914e:L37-L48`).
- **Air field.** A finite cylinder from the fan origin along local +X with length `reach` and diameter `width` (`parts/FanPart.cs@a6c914e:L19-L23`; `engine/AirflowNetwork.cs@a6c914e:L11-L13`).

### Mass and material
Static, no mass. Base material: the legacy default static surface (restitution 1, threshold 0.1 m/s, friction 0.3) as recorded for delivered static parts (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).

### Constraints and joints
None.

### Typed ports
| Socket | Domain | Direction | Variant | Source |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | Electrical | **proposed** placement on the base; identity `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9` |
| `DriveIn` | Mechanical | Input, −1 per radian on the rotor | Shaft | **proposed**; relay convention `parts/ConveyorPart.cs@a6c914e:L64-L71` |

### Sensors and activation
None in EL-209: supply alone enables emission (CAT-028's activation-controlled start is its own catalogue mode).

### Work and energy stores
- Emission power is capped at `force` × 12 m/s (reference flow speed) — sourced from the legacy stored flow source (`parts/FanPart.cs@a6c914e:L21-L23`; `engine/AirflowNetwork.cs@a6c914e:L24-L24`); 108 W at the default 9 N.
- Only work done on a receiver while it is exposed is drawn; exposure and occlusion checks never create or need supply (legacy rows 6 and 7 below).
- **Electrical.** Power drawn from the connected supply; depleted or disconnected supply emits nothing.
- **Shaft.** Emission power equals the shaft power delivered through `DriveIn`, up to the cap — **proposed**: the rotor converts shaft work only; a stopped shaft emits nothing.
- The legacy self-contained 14,400 J store (`parts/FanPart.cs@a6c914e:L12-L16`) is not used by EL-209.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `force` | f32 | 0–40 | 9 | N | `parts/FanPart.cs@a6c914e:L24-L31`, `parts/catalog/fan.tres@a6c914e:L14-L14` |
| `reach` | f32 | (0, 12] | 5 | m | same |
| `width` | f32 | (0, 4] | 0.85 | m | same |
| `supply` | enum `FanSupply { Electrical, Shaft }` | 2 values | Electrical | — | **proposed**: selects the variant; the legacy `powered` flag is CAT-028's and is not an EL-209 parameter |

### Cosmetic curves and UI bindings
Rotor spins one revolution (0 → 2π) per 0.349 s (animation duration τ/18), looping, while emitting, start/stop on activity (`parts/FanPart.cs@a6c914e:L50-L53`); for the Shaft variant the rotor follows the committed shaft pose instead — **proposed**. Rotor artwork may coast but never emits force ([CAT-028 row](../requirements.md#current-cat-028)). UI: parameters through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`). Pick radius 0.75 m (`parts/FanPart.cs@a6c914e:L35-L35`).

### Art
Housing ring and blades in the Fan colour `#66b8c9` (blades lightened 15%); base `#263d4b`; post `#ccd8dc`; hub `#f4d089`; airflow line `#a9e7e0` (`parts/FanPart.cs@a6c914e:L36-L48`; `parts/catalog/fan.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system)).

### Catalogue and inventory entry
Id `fan`, title "Fan", category Power (`parts/catalog/fan.tres@a6c914e:L8-L14`). Whether EL-209 is the same catalogue part with a supply setting or a separate part is open question 1. Add `WorkshopPartKind.Fan` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
- **Electrical supply.** `PowerIn` from a battery. Positive: the field pushes a light ball. Control: no or depleted battery, no force.
- **Shaft supply.** `DriveIn` from a motor or other shaft. Positive: a turning shaft drives the field. Control: a stopped or disconnected shaft, no force.

## 3. Engine capabilities

Families: AerodynamicDrag, ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Dynamic cargo with gravity and drag: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.

**Missing**
- Open airflow field with paid emission and occlusion: Story 12.2; decisions S470 open-versus-sealed (S697) and S257 ([decisions](../invest/decisions.md#s470)). Owner S313.
- Electrical supply: Story 8.1. Mechanical input and rotor hinge: Stories 10.3 and 11.1.

**Dependencies.** CAT-005 battery (Story 8.1); CAT-042 motor (Story 11.1) for the Shaft variant; CAT-028 field (Story 12.2).

## 4. Sources and legacy

- Requirement: "Explicit electrical or shaft supply drives a bounded air field"; outcome "Blocked flow or absent supply prevents remote force" ([element-209](../requirements.md#element-209)).
- The air-jet facts below match the [CAT-028 harvest](CAT-028-fan.md#legacy-harvest) (its facts 23, 24, 27 and 29); CAT-028 holds the full fan harvest.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Emitter: reach, width, supply with reference speed 12 and power force × 12; impedance force/12 | `parts/FanPart.cs@a6c914e:L19-L23` | carry forward | Bounded field. |
| 2 | Parameter bounds and finite checks | `parts/FanPart.cs@a6c914e:L24-L31` | carry forward | Admission. |
| 3 | Self-contained 14,400 J store; `powered` flag; "no external electrical input yet" | `parts/FanPart.cs@a6c914e:L12-L16`; `parts/catalog/fan.tres@a6c914e:L11-L11` | do not carry forward for EL-209 | EL-209 is explicitly supplied. |
| 4 | Accepted: acceleration 6/mass for 1 kg and 4 kg; a blocker gives zero force and no released energy; kinetic energy ≤ released energy | `CuriousContraptions.tests/AirJetWorldTests.cs@a6c914e:L18-L66` | carry forward | Blocked-flow control. |
| 5 | Level `wind_signal`: moving air strikes the switch to power the lamp; inventory fan 1 | `content/puzzles.json@a6c914e:L1811-L2092` | carry forward | Lesson set-up. |
| 6 | Accepted: a blocker moving through the jet cuts the force only while it overlaps the jet (receiver speed 0.4 m/s for one blocker, 0.3 m/s for a two-part blocker over 0.6 s); released energy equals 4 W × exposed time; only exposed work is committed and the rest is dissipated | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L13-L40` | carry forward (behaviour at substep resolution) | Exposed-only work. |
| 7 | Accepted: exposure and occlusion neither need nor create supply; an unpowered load has zero demand and changes no body; excluded bodies do not occlude | `CuriousContraptions.tests/AirJetGeometryTests.cs@a6c914e:L8-L32` | carry forward | Exposure never creates supply. |
| 8 | Accepted: a stationary blocker occludes a moving receiver only while its collider participates; a non-colliding blocker never occludes | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L48-L72` | carry forward | Non-colliding parts do not block. |
| 9 | Accepted: inlet, outlet and rim crossings in both directions switch exposure on and off; a source whose collider is disabled emits no field | `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L16-L45`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L126-L135` | carry forward (force starts and stops at the field boundary) | Field edges. |
| 10 | Continuous root-finding sweeps for boundary and occlusion times (tolerance 1e-7, tangency and initially-on-surface handling, sweep validation), and whole-world capture/restore replay | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L41-L45`, `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L74-L90`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L102-L124`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L136-L143` | do not carry forward | CPU continuous-sweep solver path and rollback; the worker samples at 480 Hz substeps and never rolls back a tick. |

**Files harvested:**
- `parts/FanPart.cs`
- `parts/catalog/fan.tres`
- `parts/ConveyorPart.cs` (mechanical input convention)
- `engine/AirflowNetwork.cs`
- `CuriousContraptions.tests/AirJetWorldTests.cs`
- `CuriousContraptions.tests/AirJetOcclusionTests.cs`
- `CuriousContraptions.tests/AirJetGeometryTests.cs`
- `CuriousContraptions.tests/AirJetBoundaryTests.cs`
- `content/puzzles.json` (level `wind_signal`)
- The remaining fan and air-jet files are harvested in [CAT-028](CAT-028-fan.md#legacy-harvest) (Batch D). Searched with no hit for an electrically or shaft-supplied fan: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-209](../requirements.md#element-209) and [current-cat-028](../requirements.md#current-cat-028).
- **Chrome UI recipe.** Place the fan aimed across a ball's fall, a battery connected `Supply` → `PowerIn` (Electrical) or a motor `Drive` → `DriveIn` (Shaft), and a receiver downwind. Run.
- **Positive.** The ball is pushed into the receiver.
- **Negative/control.** No supply, a depleted battery or a stopped shaft gives no force; a wall in the jet blocks it; a part moved through the jet cuts the push only while it is in the jet; a non-colliding part in the jet does not block; an unsupplied fan draws and delivers nothing even with a ball in its field; a heavy Bowling ball barely deflects.
- **Boundaries.** `force` 0 and 40; `reach` and `width` limits; a ball crossing the field's inlet, outlet and rim.
- **Run/Reset.** Supply and field state restored.
- **Save/Load.** Parameters, `supply` and connections round-trip.
- **Integrations.** Campaign row 2 ("... fan"): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); fan → windmill rotor → belt teaching in [todo-190](../requirements.md#todo-190); battery supply (CAT-005) and motor shaft (CAT-042).

## 6. Open questions

1. Same catalogue part as CAT-028 with a supply setting, or a separate supplied fan? The CAT-028 row says "no battery/shaft input is invented" while Story 12.2 names battery power. Unspecified — owner decision.
2. Shaft variant conversion (proposed: emission power equals delivered shaft power): owner decision.
