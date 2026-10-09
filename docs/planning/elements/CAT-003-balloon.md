# CAT-003 · balloon — declaration readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-003 · `balloon` |
| Requirement anchor | [CAT-003](../requirements.md#current-cat-003); D/I/V row [CAT-003-I](../invest/current-consumers.md#cat-003-i) |
| Mapped identities | [EL-208 Balloon](../invest/named-elements.md#element-208). Distinct neighbours, not merged: [TH-25 Hot-air balloon](../invest/named-elements.md#thermal-25), [EL-090 Steerable blimp](../invest/named-elements.md#element-090), [EL-125 Authored atmosphere preset](../invest/named-elements.md#element-125). No RAD or GAP identity. |
| Capability row | [general-engine-element-map, CAT-003](../general-engine-element-map.md) |
| Roadmap story | Epic 12, Story 12.1 "Lightweight Tennis Ball & Floating Balloon Buoyancy" (CAT-064, CAT-003); roadmap family "Conserved airflow and buoyancy" ([vertical-delivery](../invest/vertical-delivery.md#existing-element-order)) |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no level uses it. |

## Declaration

- **Bodies and shapes.** One dynamic sphere: `WorkshopBall` plus a `BallMaterial` keyed by kind (`engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L77`), compiled to one dynamic body, material and sphere collider (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`). Radius 0.36 m.
- **Mass and material.** Add a balloon arm to `BallMaterial.For(kind)` (`engine/gpu/WorkshopConstruction.cs@a6c914e:L48-L53`):

  | Field | Value | Source |
  | --- | --- | --- |
  | Radius | 0.36 m | `parts/catalog/balloon.tres@a6c914e:L14-L14` |
  | Mass | 0.5 kg | same |
  | Bounce | 0.2 | same |
  | Buoyancy | 11.5 m/s² (lift acceleration, scaled by pressure) | same; law in fact 3 |
  | Drag | 0.4 1/s (scaled by pressure) | same |
  | Friction | 0.3 | legacy ball default, `reference/cpu/MachinePart.cs@a6c914e:L236-L236` |
  | Bounce threshold | 0.1 m/s | same |
  | Rolling resistance | unspecified | see Open questions |

  Bounds: drag 0.4 exceeds the admitted body drag range 0–0.125 1/s, and body gravity is a per-body vector bounded at magnitude 16 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L82`). See Open questions.

  Storage type: the target is canonical f32. Today `BallMaterial` and its catalogue resource store binary16 (`engine/gpu/WorkshopConstruction.cs@a6c914e:L41-L53`; `engine/BallMaterialResource.cs@a6c914e:L7-L29`), and the f32 document lists ball materials under the remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L94-L95`). The balloon arm follows whichever type `BallMaterial` has when Story 12.1 lands.
- **Constraints and joints.** None of its own. "Applicable tether interactions" ([CAT-003](../requirements.md#current-cat-003)) attach through the rope capability.
- **Typed sockets and ports.** None.
- **Sensors and activation.** None owned. EL-208 adds puncture behaviour ([EL-208](../invest/named-elements.md#element-208)); no legacy implementation exists.
- **Work and energy stores.** None in legacy. The current gas family declares a sealed gas store for the balloon ([finite-gas-foundation, per-element declarations](../../finite-gas-foundation.md)).
- **Parameters.** Material is fixed per kind and admitted bit-for-bit (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`). The current gas doc lists "lift, volume, drag" for the Balloon. See Open questions.
- **Cosmetic curves and UI bindings.** Legacy none. The gas doc names "Skin scale ← committed volume" as the binding once a gas store exists.
- **Art.** Shared ball artwork (`parts/BallPart.cs@a6c914e:L6-L17`). Legacy also drew a string from (0, −r, 0) to (0.06, −r − 0.5, 0), colour `#e9dfcc`, thickness 0.015, for any ball with buoyancy > 0 (`reference/cpu/BallPart.cs@a6c914e:L26-L27`); the current `BallPart` has no string. Palette `#ed6378` (`DESIGN.md@a6c914e:L168-L168`); icon `ui/WorkshopIcons.cs@a6c914e:L42-L42`.
- **Catalogue and inventory entry.** Id `balloon`, title "Balloon", category Motion, description "Buoyancy lifts it; fans can guide it. Pressure changes its ascent." (`parts/catalog/balloon.tres@a6c914e:L8-L14`). The current form adds a `BallMaterialResource` sub-resource (`engine/BallMaterialResource.cs@a6c914e:L7-L29`).

## Engine capabilities

Families: the [CAT-003 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AerodynamicDrag, Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, GasState, GeometryQuery, RigidBodyDynamics, SlidingFriction).

- **Exists now.** Dynamic sphere, contact, friction, geometry query, declared linear drag and per-body gravity vector (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L82`).
- **Missing.**
  - Buoyancy consumption and world pressure (EnvironmentState). `BallMaterial.Buoyancy` is carried but never compiled, and gravity is a constant 9.81 with no pressure (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L124`). Built by Story 12.1.
  - Drag above 0.125 1/s: admission widening, Story 12.1.
  - Airflow receiver: Story 12.2 (CAT-028).
  - GasState (sealed gas store, puncture): decision owner S470 ([decisions](../invest/decisions.md#s470)); named next implementations S471 (gas-state) and S697 (open versus sealed). No epic story schedules it.
  - Tether: rope capability, Story 10.2 (CAT-053 Pulley, CAT-058 Rope anchor).
- **Element dependencies.** Fan (CAT-028) for "fans can guide it"; Rope anchor (CAT-058) for tethering; a Wall (CAT-066) or ceiling for the rise-until-contact control.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material: mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4. | `parts/catalog/balloon.tres@a6c914e:L14-L14` | Carry forward. Do not carry forward the legacy float parameter dictionary. The values enter `BallMaterial`, which is binary16 today and listed under the remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L94-L95`); f32 is the target, not a precondition of Story 12.1. |
| 2 | Contact material: restitution = bounce, threshold 0.1, friction 0.3. | `reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/physics/PersistentContactPair.cs@a6c914e:L23-L23` | Carry forward as declared values. |
| 3 | Lift law: upward force = buoyancy × world pressure × mass on every dynamic root body; drag rate = drag × pressure. Default world pressure 1. | `reference/cpu/MachineWorld.cs@a6c914e:L869-L883`; `engine/MachineData.cs@a6c914e:L171-L171` | Carry forward the law as declaration data. Do not carry forward the per-substep CPU force loop. |
| 4 | Drag law: the drag rate is per second and the force is −velocity × rate × mass (angular: −angular momentum × rate). So at pressure 1 and g = 9.81 the net upward acceleration is 11.5 − 9.81 = 1.69 m/s², and the linear-drag terminal rise speed is 1.69 / 0.4 ≈ 4.2 m/s, independent of mass (arithmetic on facts 1 and 3). At pressure 0 the balloon falls freely. | `engine/physics/BodyDragLoad.cs@a6c914e:L6-L32` | Carry forward as an acceptance expectation, subject to Open question 1. The current solver's exponential decay e^(−c Δt) of linear velocity has the same terminal speed. |
| 5 | Legacy tick 1/120 s with 4 substeps (480 Hz physical step). | `reference/cpu/MachineWorld.cs@a6c914e:L20-L21` | Carry forward only as context; the current canonical physical step is also 480 Hz. |
| 6 | Buoyant balls drew a hanging string. | `reference/cpu/BallPart.cs@a6c914e:L26-L27` | Carry forward as art. |
| 7 | The balloon must carry all five ball parameters; missing ones reject; Run/Reset restores the save exactly. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | Carry forward the rejection and Reset facts. Do not carry forward the string-keyed dictionary. |
| 8 | No level in `content/puzzles.json` or `tools/Campaign` uses the balloon. | `docs/coverage/catalogue-elements.json` (empty fixture list) | Recorded; lesson design is open. |

**Files harvested:**
- `parts/catalog/balloon.tres`
- `parts/scenes/ball.tscn` (no element knowledge: script reference only)
- `reference/cpu/BallPart.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `engine/MachineData.cs`
- `engine/physics/BodyDragLoad.cs`
- `engine/physics/PersistentContactPair.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`

Consulted, no balloon knowledge: `engine/physics/SealedGasState.cs`, `engine/physics/IdealGasMaterial.cs`, `engine/physics/PhysicsGasNode.cs`, `CuriousContraptions.tests/SealedGasStateTests.cs`, `CuriousContraptions.tests/GasChamberWorldTests.cs`. These are an unused sealed-gas foundation with synthetic fixture materials (for example R = 200, Cv = 400 in `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L7-L8`); no part consumes them. The law they implement is held by the current [gas family document](../../finite-gas-foundation.md).

## Acceptance outline

Point of truth: [CAT-003](../requirements.md#current-cat-003) and [simulation and element acceptance](../requirements.md#accept-simulation).

- **Chrome construction.** Through the real palette, place a Balloon and a Basketball with the move gizmo at the same height; optionally a Wall above as a ceiling.
- **Positive.** On Run the balloon rises vertically until it contacts the ceiling or wall; the Basketball falls.
- **Negative or control.** Zero pressure: the balloon does not rise. No fan: no lateral drift. With a Fan (Story 12.2): the jet guides it; an occluded jet does not. A heavier body in the same set-up does not rise.
- **Boundaries.** Rise acceleration and terminal speed as fact 4 within the game-grade envelope; contact with the ceiling rests without penetration.
- **Run/Reset.** Reset restores exact position and zero velocity.
- **Save/Load.** Kind and material survive save, reload and Load.
- **Integrations.** Fan guidance; rope tether once Story 10.2 lands; puncture once the gas store exists.

## Open questions

1. **Pressure.** The current world has no pressure parameter. Decide whether the balloon declares net lift as a per-body gravity vector (gravity − buoyancy × pressure fits the magnitude-16 bound) or waits for an EnvironmentState pressure (EL-125). Unspecified — owner decision.
2. **Drag bound.** 0.4 1/s exceeds the admitted 0.125 1/s. Widen the bound or change the balloon value. Unspecified — owner decision.
3. **Gas state and puncture.** EL-208 requires gas-filled behaviour and puncture; no epic story schedules S471/S697. Decide whether Story 12.1 delivers the buoyancy-only mode with the gas mode deferred, or a gas story is added.
4. **Rolling resistance.** No legacy value. Unspecified — owner decision.
5. **Configurable material.** The requirement lists mass, bounce, radius, buoyancy and drag as configuration; the engine fixes material per kind. Owner decision.
6. **Ceiling.** Story 12.1 says "until contacting a ceiling or wall"; the Workshop has no ceiling body. Decide whether an authored Wall stands in for it.
