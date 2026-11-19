# CAT-003 · balloon — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-003 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

## Current implementation contract — Story 12.1b

The [native Balloon spec](../../../_bmad-output/implementation-artifacts/spec-12-1b-balloon.md) owns the implementation candidate and verification status. Browser/release qualification remains pending. The historical harvest below preserves source acceptance and citations; its fixed-by-kind, shared-art, moving neck anchor, capped drag and empty 0.5 kg proposals are superseded by these decisions.

- Balloon is its own native Godot art and managed C# declaration package. Five BuildOnly controls retain defaults mass 0.5 kg, reference radius 0.36 m, bounce 0.2, buoyancy 11.5 m/s² and drag 0.4/s; ranges are respectively [0.125,8], [0.18,0.5], [0,1], [0,16] and [0,4]. Friction 0.3, bounce threshold 0.1 m/s and rolling resistance zero are declared material data.
- Construction explicitly selects persisted BuildOnly Standard (101325 Pa, relative pressure 1) or Vacuum (0 Pa, relative pressure 0). These are unbounded ambient boundaries under EL-125, not finite ambient inventories. Runtime never supplies a missing pressure default. Unknown or missing serialized presets are refused. Preparation is atomic against expanded physical geometry.
- A finite sealed ideal-gas store owns mass, volume and internal energy. Material R=2000 J/(kg·K), Cv=3000 J/(kg·K), reference temperature 300 K and positive gauge pressure 1000 Pa define a compliant isobaric envelope. The radius control describes Standard reference geometry; the same reference inventory expands adiabatically in Vacuum, exchanging boundary work and envelope potential. Temperature [1,1000] K, density ≤1 kg/m³ and pressure ≤200000 Pa are package admission limits.
- Declared mass is initial total mass. The one centered-inventory mass source subtracts reference gas to obtain dry carrier mass, then adds current gas exactly once. Dry principal moments are fixed declared coefficients 0.4×(0.0625 m)² times dry mass; gas contributes translational mass only. Isotropic outflow carries removed momentum and kinetic energy without adding thrust.
- Upward load is buoyancy×relative ambient pressure×current body mass×remaining gas fraction. Drag is drag×relative pressure, without angular damping. Shared drag admits finite nonnegative values using bounded exponent handling. A heavier no-lift Bowling control falls; changing only Balloon mass does not contradict the harvested mass-independent acceleration.
- A strict >3 N·s accepted-substep impulse ruptures one exact collider-bound observation. Sum final nonnegative normal impulses only for one unordered collider pair whose pre-force approach exceeds 0.1 m/s; no cross-pair/substep accumulation or speculative-trial events. Restitution and reduced mass apply. The committed occurrence opens the exact outlet once; discharge starts the next substep.
- Pressure-driven conductance referenceMass/(1000 Pa·1 s) vents a finite allocation and accounts enthalpy, boundary work and released envelope potential. It is not a timer. Empty state is canonical zero mass/volume/energy, zero lift and dry carrier mass. The physical sphere and committed presentation share radius cubed = dry radius cubed + 3V/(4π), leaving a 0.0625 m dry core.
- The tensile Tie is fixed at the body origin on the dry carrier; the string is naturally occluded inside the envelope and the neck is cosmetic. Existing tensile rows provide taut restraint and slack freedom. Shrinking never moves a constraint anchor.
- A typed IntegrityBroken goal binds exact owner and local integrity observation. Only its committed occurrence solves; another store, intact contact or a stale session epoch cannot. Reset restores full, intact authored preparation, geometry and tether; save/load preserves authored controls, atmosphere and exact goal selector.
- Required native proof covers rise/ceiling, Vacuum and heavier controls, taut/slack/disconnected tether, rupture/depletion, exact selected goal, art and lifecycle. Fan/no-fan/missed/occluded-jet transfer and fan guidance remain with 12.2; campaign and post-catalogue browser recipes remain required at their named owners.

This slice establishes the bounded finite-store/compliant-boundary/loss prerequisite before Balloon consumption. It does not complete S471 connected-volume/valve qualification or S697 gas-network integration; EL-039 and the later gas-network consumers retain those criteria and IDs. No general gas network or closed-world atmosphere is claimed.

## Historical harvest and original acceptance sources

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-003 · `balloon` |
| Requirement anchor | [CAT-003](../requirements.md#current-cat-003); D/I/V row [CAT-003-I](../invest/current-consumers.md#cat-003-i) |
| Mapped identities | [EL-208 Balloon](../invest/named-elements.md#element-208). Distinct neighbours, not merged: [TH-25 Hot-air balloon](../invest/named-elements.md#thermal-25), [EL-090 Steerable blimp](../invest/named-elements.md#element-090), [EL-125 Authored atmosphere preset](../invest/named-elements.md#element-125). No RAD or GAP identity. |
| Capability row | [general-engine-element-map, CAT-003](../general-engine-element-map.md) |
| Roadmap story | [12.1b Balloon](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-003 mode and mapped named-identity obligation. |
| Harvest-baseline status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no level uses it. |

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

  Storage type: the target is canonical f32. At a6c914e `BallMaterial` and its catalogue resource stored half-precision (`engine/gpu/WorkshopConstruction.cs@a6c914e:L41-L53`; `engine/BallMaterialResource.cs@a6c914e:L7-L29`), and the f32 document listed ball materials under the remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L94-L95`); Story 9A.44 migrated them to f32. The balloon arm follows whichever type `BallMaterial` has when Story 12.1 lands.
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
  - GasState (sealed gas store, puncture): decision owner S470 ([decisions](../invest/decisions.md#s470)); named next implementations S471 (gas-state) and S697 (open versus sealed). The linked delivery slice owns the required capability decision and native proof.
  - Tether: rope capability, Story 10.2 (CAT-053 Pulley, CAT-058 Rope anchor).
- **Element dependencies.** Fan (CAT-028) for "fans can guide it"; Rope anchor (CAT-058) for tethering; a Wall (CAT-066) or ceiling for the rise-until-contact control.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material: mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4. | `parts/catalog/balloon.tres@a6c914e:L14-L14` | Carry forward. Do not carry forward the legacy float parameter dictionary. The values enter `BallMaterial`, which was half-precision at a6c914e (listed under the remaining f32 migration, `docs/gpu-f32-physics.md@a6c914e:L94-L95`) and is f32 since Story 9A.44, so f32 is no precondition of Story 12.1. |
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
3. **Gas state and puncture.** EL-208 requires gas-filled behaviour and puncture; the linked Balloon delivery contract requires the S471/S697 gas-state decisions. Settle gas/puncture semantics before the dependent EL-208 mode; buoyancy-only proof cannot close that named obligation.
4. **Rolling resistance.** No legacy value. Unspecified — owner decision.
5. **Configurable material.** The requirement lists mass, bounce, radius, buoyancy and drag as configuration; the engine fixes material per kind. Owner decision.
6. **Ceiling.** Story 12.1 says "until contacting a ceiling or wall"; the Workshop has no ceiling body. Decide whether an authored Wall stands in for it.
