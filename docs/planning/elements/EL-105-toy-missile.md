# EL-105 · Toy missile — named-identity spec

**Current delivery (24 October 2026):** [EL-105 delivery slice](../invest/named-elements.md#element-105). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`, finding H1) and owner decision 3a and coordinator decision 16.** Every section is written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lx-Ly` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). All game values are canonical f32. No number here was simulated; each is a hand calculation shown beside it.

**Repair after the second audit (23 November 2026, `docs/planning/elements/readiness/audits/reaudit-group-2.md`, EL-105).** The eyes were inside the Nose sphere, and their 6° dead band came from a 25° acceptance cone that EL-176 convention 8 does not have. Decided under the standing delegation (decide by game intent): the eyes now sit on the body cylinder's side faces just behind the Nose, facing straight up and straight down, and the dead band comes from convention 8's facing term, the threshold node of convention 9 (`ENGINE-SIGNAL-NODES`) and the eyes' placement on opposite faces. The acceptance cone is withdrawn, not proposed. The threshold, aperture and beacon are stated, and A1's reading at launch is computed from them. A throwaway 2-D point model (1/480 s substeps, one-tick eye lag, release ratio included) was used only to check the hand derivation; it is not acceptance evidence.

**Safety boundary.** A foam toy dart with an energy budget in joules, forces in newtons and two light-sensing eyes. Nothing real is modelled or described.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-105 · Toy missile · Specialist |
| Anchor | [requirements.md#element-105](../requirements.md#element-105); [named entry](../invest/named-elements.md#element-105); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S378 |
| Disposition | To deliver as a single element, last of the jet run after [EL-092](EL-092-toy-rocket.md) and [EL-104](EL-104-toy-firework.md). Not a Family member (it adds sensors, a supply graph, an ambient load and two more jets) and not a duplicate. |
| Planned package | id `toy_missile`, directory `elements/ToyMissile/`, class `ToyMissileElementDeclaration` |
| Refines / extends | No CAT spec. Jet of EL-092; supply-fed airflow of the Fan's Electrical mode (`elements/Fan/FanElementDeclaration.cs:54`); signal-controlled supply contact of the Impact switch (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:71`–`:76`); ambient lift and drag of the Balloon (`elements/Balloon/BalloonElementDeclaration.cs:69`–`:71`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-SIGNAL-NODES (the eyes' threshold nodes), ENGINE-OPTICAL and ENGINE-REACTION-THRUST (with its angular-drag rider), then S7 |

## 2. Declaration

Guidance is not code. Two declared light receivers each publish a threshold level that closes one declared supply contact feeding one declared nose jet. A lit eye therefore turns the nose toward the light, the main jet pushes along the nose, and declared drag makes the dart go where it points. There is no target, no route and no homing rule anywhere.

The package implements `IAirflowSourceElement` and `IAmbientLoadElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:40`, `:124`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the receiver interface that ENGINE-OPTICAL adds. `KinematicMode.Dynamic`, `SupportedDomains = Mechanical | Electrical | Signal` plus the optical domain that story defines. Footprint: 1 body, no constraints.

### Body, ambient load and colliders

One dynamic body, long axis local +X, nose at +X; local +Y is "up" for the eyes and nose jets. Mass 0.3 kg (**starting value**). Mass frame: centre at the origin; moments of a solid cylinder of radius 0.08 m and length 0.8 m: axial ½·0.3·0.08² = 0.00096 kg·m², transverse 0.3·(3·0.08² + 0.8²)/12 = 0.0165 kg·m²; gravity (0, −9.81, 0); mass-frame `linearDrag` 0.

One `AmbientLoadDescriptor` on the body (`CuriousContraptions.Abstractions/GasCouplingValues.cs:19`), all constants, **starting values**:

| Value | Amount | Why |
| --- | --- | --- |
| Lift acceleration | (0, 9.6, 0) m/s² | a light gas-filled foam dart: apparent weight 0.3 × (9.81 − 9.6) = 0.063 N. Under the jet law thrust falls to nothing as speed nears the flow speed, so a wingless dart cannot both fly and carry 2.94 N; see Behaviour |
| Linear drag | 1.4 /s | holds cruise well under the flow speed, so thrust is left over to steer with, and makes the velocity follow the nose. This is the dart's only linear drag |
| Angular drag | 8 /s | stops the turn when the eye goes dark. A general ambient value, default zero, added by the ENGINE-REACTION-THRUST rider (coordinator decision 16) |

Material: restitution 0.2, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s.

| Collider | Shape | Dimensions (m) | Offset |
| --- | --- | --- | --- |
| Body | Cylinder about X | radius 0.08, half-length 0.40 | origin |
| Nose | Sphere | radius 0.08 | (0.40, 0, 0) |
| Fins A | Box | 0.16 × 0.226 × 0.02, rolled +45° about X | (−0.32, 0, 0) |
| Fins B | Box | 0.16 × 0.226 × 0.02, rolled −45° about X | (−0.32, 0, 0) |

The fins form an "×". Their lowest corners reach (0.113 + 0.01) × sin 45° = 0.087 m below the axis (fin half-thickness included), 0.007 m below the body's 0.080 m radius. The dart therefore rests on the fins' front-bottom corners (x = −0.24) and the body's lowest line at the nose (x = 0.40), about atan(0.007 ÷ 0.64) = 0.6° nose-down, and cannot roll.

### Jets, supply graph and signal nodes

Three `AirflowSourceDescriptor` jets on body 0 (`CuriousContraptions.Abstractions/AirflowValues.cs:45`), each with one `ElectricalAirflowInput` naming its consumer and each declaring the ambient reaction (missing capability, section 3). Thrust is each jet's own rated force.

| Jet | Origin | Exhaust axis | Cone (reach, radius) | Maximum force | Flow speed | Maximum power | Consumer |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 main | (−0.40, 0, 0) | −X | 0.6 m, 0.15 m | parameter `thrust` | 16 m/s | `thrust` × 16 m/s | `main` |
| 2 nose-up | (0.30, −0.08, 0) | −Y | 0.3 m, 0.08 m | 0.5 N | 4 m/s | 2 W | `up` |
| 3 nose-down | (0.30, 0.08, 0) | +Y | 0.3 m, 0.08 m | 0.5 N | 4 m/s | 2 W | `down` |

The nose jets sit 0.30 m ahead of the centre of mass: each turns the dart with a torque of 0.5 × 0.30 = 0.15 N·m (and nudges it sideways with its 0.5 N). No acceptance row depends on an exhaust cone reaching another body.

- **Supply graph.** A `SupplySourceDescriptor` `cell` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:39`) with store keys `charge`, `power_limit`, `initial_charge`, `enabled`; three `SupplyContactDescriptor` nodes with `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`): `launch` on node `launched`, `see_up` on node `eye_up`, `see_down` on node `eye_down`, each in state `Latched`; three `SupplyConsumerDescriptor` nodes `main`, `up`, `down`, each with one `SupplyContinuousWorkTarget` for its jet. Internal edges: `cell` → `launch`; `launch` → `main`; `launch` → `see_up` → `up`; `launch` → `see_down` → `down`. No supply ports. A nose jet is fed only while the dart has been launched **and** its eye is lit: the "and" is two contacts in series.
- **Receivers (ENGINE-OPTICAL, [EL-176](EL-176-red-laser-emitter.md#optical-conventions) section 10 convention 8).** Two receivers on the Body collider's side face, just behind the Nose: `rx_up` with a disc aperture of radius 0.02 m centred at (0.36, +0.08, 0), normal +Y; `rx_down` the mirror image at (0.36, −0.08, 0), normal −Y. Area A = π × 0.02² = 1.257 × 10⁻³ m² each; one sample point at the disc centre, weight 1; sensed channels Red, Green and Blue, strength WeightedSum with weights 1/3 each (the ordinary colour channels, coordinator decision 16); no purity fraction. The aperture discs span x 0.34 to 0.38, where the Nose sphere's section is at most √(0.08² − 0.02²) = 0.077 m in radius, inside the cylinder's 0.08 m, so both discs lie on exposed cylinder face and no collider of the dart covers them. No acceptance cone is declared: each eye answers any light by the facing term alone.
- **Signal nodes.** `SignalMemoryDescriptor` `launched`, set from `ActivationIn`. Two threshold nodes over the receivers' committed strengths (EL-176 convention 9, "threshold over a committed scalar, with a release ratio", owned by **ENGINE-SIGNAL-NODES**): `eye_up` over `rx_up` and `eye_down` over `rx_down`, each on at strength ≥ **2.0 × 10⁻⁶ W** (**starting value**) and off below release ratio **0.9** × that (the Sighted walkers' ratio, [EL-082](EL-082-lured-character.md)).

### Ports

| `PortId` | Domain | Direction | Role | Kind | Local position |
| --- | --- | --- | --- | --- | --- |
| `ActivationIn` | Signal | Input | `ActivationIn` | Pulse | tail side, (−0.20, 0, 0.08), normal +Z |

### Parameters

| Key | Kind | Unit | Default | Min | Max | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `thrust` | Real | Newton | 4 | 1 | 8 | Reject | cruise 6.0 m/s with 2.5 N of thrust still in hand |
| `charge` | Real | Joule | 90 | 20 | 200 | Clamp | about 2.0 s and 9.3 m of flight |
| `power_limit` | Real | Watt | 80 | 20 | 160 | Clamp | above the 66 W peak demand shown below; 80 W is 0.67 J per tick |
| `initial_charge` | Real | None | 1 | 0 | 1 | Clamp | 0 is the "no supply" control |
| `enabled` | Choice (`Disabled`, `Enabled`) | None | Enabled | — | — | Reject | required by the source store descriptor (`CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`); Build-only |

### Metadata and bindings

Category `Motion`, display name "Seeker dart", palette order 178 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.87, 0.44, 0.35). Presentation: `Visual/Plume` `ScaleX` on `PresentationDriver.CommittedFlowSpeed` of jet 1; `Visual/EyeUp` and `Visual/EyeDown` albedo on `PresentationDriver.CommittedActivation` of the threshold nodes `eye_up` and `eye_down` (slate `#556573` to gold `#f7cb52`, three channel bindings each); `Visual/ChargeBand` `ScaleX` on `PresentationDriver.CommittedCharge` of `cell`.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` | cream `#fff8e9` cylinder with a navy `#293954` band printed flat | solid, Body collider | body pose |
| `Visual/Nose` | coral `#de7058` dome | solid, Nose collider | body pose |
| `Visual/Fins` | cyan `#66b8c9` fins drawn as an "×" | solid, Fins A and B | body pose |
| `Visual/EyeUp`, `Visual/EyeDown` | two discs radius 0.02 m printed flat on the body's top and bottom just behind the nose, at the receiver apertures | decorative: printed flat, recoloured | binding |
| `Visual/ChargeBand` | gold strip printed flat | decorative: printed flat, scaled | binding |
| `Visual/Plume` | gold cone behind the tail | decorative: glow, scaled | binding |

Icon: a navy horizontal dart with an eye dot on its top and its underside behind a coral nose, and a gold tail flame. It differs from the rocket and firework by lying horizontal, by its "×" fins and by its eyes.

### Behaviour and numbers

Laws: rigid-body dynamics; the delivered ambient load (lift and linear drag) with a declared angular drag (missing); supply-paid jets and their ambient reaction (missing): reaction impulse at the jet origin, opposite its axis, = (granted limit − impulse granted to exposed receivers) × pressure scale × clamp((flow speed + origin velocity · axis) ÷ flow speed, 0, 1), paid at that impulse × flow speed; the supply network with signal-controlled contacts; optical reception by EL-176 conventions 1, 5 and 8 (facing term, near-field rule, own-body occlusion; missing) and the threshold node of convention 9 (missing); the signal memory. Airflow, occlusion and optical candidates come from ENGINE-SPATIAL-QUERIES.

- **Why the dart is nearly weightless.** At forward speed v the main jet gives `thrust` × (1 − v/16). Carrying 2.94 N on the vertical share of that at any useful speed needs the nose far above the light; with pursuit pointing the nose at the light the dart would hit the bench about 3 m out. The declared lift leaves 0.063 N of apparent weight: a sink of at most 0.21/1.4 = 0.15 m/s, about 0.12 m over the 1.4 s it takes to fly 6 m.
- **Rest and launch attitude.** It lies on a ramp about 0.6° nose-down, held by 0.063 N. Friction there is 0.4 × 0.063 = 0.025 N against 4 N of thrust. Leaving a ramp lip with the tail still supported pitches it nose-down at m·g′·x ÷ (I + m·x²) = 0.3 × 0.21 × 0.4 ÷ (0.0165 + 0.048) = 0.39 rad/s² for about 0.11 s (0.4 m of tail at about 3.6 m/s): 0.04 rad/s, which the angular drag stops within 0.04/8 = 0.005 rad, 0.3°.
- **Launch.** A pulse sets `launched`; the `launch` contact closes and the main jet runs. Signal routing is same-tick. Under decision D7 a receiver reading committed in one tick is used by the next tick's electrical solve, so steering lags the light by one tick (1/120 s), which no row times.
- **Forward flight.** Cruise speed 4 ÷ (4/16 + 0.3 × 1.4) = 6.0 m/s with a time constant of 0.3/0.67 = 0.45 s; x(t) = 6 × (t − 0.45 × (1 − e^(−t/0.45))): 6 m takes about 1.4 s. At cruise the jet still gives 4 × (1 − 6/16) = 2.5 N.
- **What each eye reads (convention 8).** For a light in the dart's XY plane, inside the light's cone, at distance d beyond 1 m from an eye and at elevation α above that eye's plane as seen from the eye, `rx_up` reads A × I × sin α ÷ d² (its facing term is the cosine between its +Y normal and the direction to the light, sin α) and `rx_down` reads A × I × sin(−α') ÷ d², α' measured from its own plane at y = −0.08 m; a negative facing reads zero. Inside 1 m the near-field rule reads A × I for a light on the front side of the aperture and zero behind it. Own-body occlusion (convention 5): a ray from `rx_up` toward a light at any α > 0 rises from y = 0.08 m, above the Nose sphere's top (y = 0.08 m at x = 0.40) and the body's radius, so the dart's own colliders never shadow it; for α ≤ 0 the facing term is already zero. The mirror holds for `rx_down`.
- **The dead band (facing term, threshold and the eyes' separation).** Let h be a light's height above `rx_up`'s plane (y = +0.08 m in the body frame) and d its distance from that eye. Beyond 1 m `rx_up`'s facing is h ÷ d, so it reads A × I × h ÷ d³ and is on when h ≥ T × d³ ÷ (A × I) with T = 2.0 × 10⁻⁶ W; `rx_down` is the mirror image below y = −0.08 m. The two eyes face opposite ways and sit 0.16 m apart, so they are **never lit together**, and neither is lit while the light lies in a **slab about the dart's axis line of half-height 0.08 m + T × d³ ÷ (A × I)**. That slab is the dead band. Inside 1 m the near-field rule lights an eye for any light in front of its aperture, so the slab is the eyes' own ±0.08 m: a light inside it is close enough to dead ahead that holding it there brings the nose within about 0.08 m of the light's height. (A release ratio of 0.9 keeps an eye on until its reading falls below 0.9 T, 10% of the threshold term.)
- **The beacon and the numbers.** Rows use a white diverging emitter of radiant power (1, 1, 1) W, half-angle 30° and range 16 m, its axis toward the dart's launch point ("the beacon"). Ω = 2π × (1 − cos 30°) = 0.8418 sr, so I = 1 ÷ 0.8418 = 1.188 W/sr per channel, and the mean-of-three strength of white light equals the one-channel reading. A × I = 1.257 × 10⁻³ m² × 1.188 W/sr = 1.493 × 10⁻³ W·m² (the reading at 1 m with facing 1), so T ÷ (A × I) = 2.0 × 10⁻⁶ ÷ 1.493 × 10⁻³ = 1.34 × 10⁻³ m⁻² and the slab's half-height is 0.08 + 1.34 × 10⁻³ × d³ m.

  | Range d | Slab half-height | Band seen from the dart's centre |
  | --- | --- | --- |
  | 8 m | 0.08 + 0.686 = 0.77 m | ±5.5° |
  | 6.32 m (A1 at launch: √(6² + 2²)) | 0.08 + 0.338 = 0.42 m | ±3.8° |
  | 5 m | 0.08 + 0.168 = 0.25 m | ±2.8° |
  | 3 m | 0.08 + 0.036 = 0.12 m | ±2.2° |
  | 1 m and nearer | 0.08 m | ±4.6° at 1 m, ±9° at 0.5 m |

  The slab narrows all the way in; its angle grows again inside about 2 m only because the eyes' own 0.16 m spacing then subtends more, which is a miss of at most 0.08 m, not a steering loss.

  **A1 at launch:** at rest the dart is pitched 0.6° nose-down, so `rx_up` is at (0.361, 0.076) from the dart's centre and the beacon at (6, 2) is (5.639, 1.924) from it: d = 5.96 m, facing (5.639 × sin 0.6° + 1.924 × cos 0.6°) ÷ 5.96 = 0.333, reading 1.493 × 10⁻³ × 0.333 ÷ 5.96² = **1.40 × 10⁻⁵ W**, 7.0 times the 2.0 × 10⁻⁶ W threshold; `rx_down` reads zero. A light 18.4° off the nose (h ≈ 0.316 d) stays lit while 0.316 d ≥ 1.34 × 10⁻³ d³, out to √(0.316 ÷ 1.34 × 10⁻³) = 15.4 m, so the beacon's 16 m range and the threshold end the sighting at about the same distance. Shares (convention 8) are far under the emitted watt: two apertures of 1.3 × 10⁻³ m² take at most 2 × 1.493 × 10⁻³ W within 1 m.
- **Turning.** A light above the band lights `eye_up`; the nose-up jet's 0.15 N·m gives 0.15/0.0165 = 9.1 rad/s². Damping is the angular drag, 8 /s, plus the jet's own speed factor, 0.5 × 0.30² ÷ (0.0165 × 4) = 0.7 /s: the turn rate settles at 9.1/8.7 = 1.05 rad/s (60°/s) with a 0.115 s time constant. A 15° turn (A1: 18.4° less the 3.8° band) takes about 0.25/1.05 + 0.115 = 0.36 s.
- **Settling.** When the light enters the band the up jet stops and the nose coasts 1.05/8 = 0.13 rad (7.5°). At launch range the band is ±3.8°, 0.132 rad across, about the same as the coast, so the nose stops near the band's far edge (1.05 − 8 × 0.132 ≈ 0 rad/s) without lighting the other eye; if it does cross, `eye_down` stops it within 0.002 rad. Error after first entry: at most 3.8°. Closer in, the band narrows to about ±2.2° at 3 m, and the switching holds the nose to the band plus one tick's coast: one tick of a nose jet adds 9.1/120 = 0.076 rad/s, which coasts 0.076/8 = 0.0095 rad (0.5°). The line of sight turns faster as range closes (the dart flies a pursuit path), so the error bound is stated only until the nose is within 1 m of the light (A5).
- **Going where it points.** With the nose off the velocity, thrust and linear drag pull the velocity round to the nose with the same 0.45 s time constant. A light 2 m off the launch line at 6 m is 18.4° off the nose: the nose is on it in about 0.4 s, and the 2 m offset shrinks by e^(−3) over the remaining three time constants to about 0.1 m, plus the band, about 2.5° at mid-range, 0.13 m. Hand estimate of the miss: about 0.3 m. (2-D check, not evidence: closest approach 0.10 m for A1, 0.05 m for A4, 2.11 m for A2; nose within 6° of the line of sight from 0.35 s and within 2.6° from 1.0 s until the nose is within 1 m; the same bounds hold with the threshold halved or doubled.)
- **Steering range.** One plane (the dart's local XY). The two eyes together see the whole plane except the band ahead and its mirror band behind: a light behind and above lights `eye_up`, so the dart turns up and over toward it; a light inside the slab behind the dart is not steered to. Each eye's facing is the light direction's component along its ±Y normal, so a light out of the plane steers the dart in pitch only; it never yaws.
- **Energy.** The cell pays 64 × (1 − v/16) W: 64 W at launch, 40 W at cruise; E(t) = 64·t − 4·x(t). A 6 m flight costs 91 − 24 = 67 J, plus under 3 J for the nose jets (2 W while one eye is lit; the two are never lit together). The 90 J cell lasts 2.0 s and 9.3 m. Peak demand 64 + 2 = 66 W, under the 80 W `power_limit`.
- **Lost guidance.** With the light hidden, off or out of range, both nose contacts are open: corrective thrust and its work are exactly zero. The dart flies straight on its main jet, sinking slowly.
- **No supply.** `enabled` Disabled, `initial_charge` 0 or an empty cell: nothing runs. Corrective thrust can never exceed what the cell holds or what `power_limit` passes.
- **Vacuum.** Lift, drag and every jet's reaction scale with the atmosphere's pressure scale: in a Vacuum atmosphere it is a 0.3 kg dart that is not propelled; on a ramp it stays where it lies.
- **Fit and tunnelling.** 0.16 m across the body, 0.226 m across the fins: it passes the pipe bore (1.3 m across) and is not a Cannon payload. At 6 m/s it moves 0.0125 m per substep against a 0.08 m radius; the engine's fattened bounds and speculative contacts cover it (`docs/gpu-f32-physics.md:195`, `:258`). Its speed can never pass the 16 m/s flow speed, under the 64 m/s linear envelope (`CuriousContraptions.Abstractions/CanonicalBodyValues.cs:16`).

## 3. Engine capabilities

Map row families (navigation only, D14): FiniteLedger, FiniteWorkActuation, GeometryQuery, RigidBodyDynamics, SignalPropagation, StateTransaction, with the optical families its sensing needs.

| Capability | Status | Evidence |
| --- | --- | --- |
| A source, signal-controlled contacts and consumers joined by package-internal supply edges, the source paying the consumer's target | present | `ComposableSupplyRoutingTests.PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` and `.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:499`, `:530`) |
| A supply-fed conical airflow source whose source is debited by the work delivered | present | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`): debit within [work, 1.01 × work]; unwired, nothing |
| A dynamic source body receives the paired force and moment of what its jet delivers to an exposed receiver | present | `AirflowRuntimeTests.MovingNozzleReceivesPairedForceAndMoment` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:189`) |
| Declared ambient lift and strong linear drag on a body | present | `BalloonTests.NondefaultLiftAndStrongDragChangeStandardMotion` (`CuriousContraptions.tests/BalloonTests.cs:144`): lift 14 m/s² and drag 4 /s give the predicted speeds |
| Declared mass frame independent of several colliders | present | `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:283`) |
| Wrong-domain connection refusal | present | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| Three airflow sources, a source store and an ambient load on one **dynamic** package | present, untested | every delivered package declares one airflow source on a static base |
| One contact output feeding three internal edges | present, untested | the opened internal-edge tests use a single chain |
| **A dynamic source body pushed by its jet when nothing is in the cone (ambient reaction)** | missing | Owner: **ENGINE-REACTION-THRUST** (`engine-reaction-thrust`), as [EL-092 section 3](EL-092-toy-rocket.md#3-engine-capabilities); text in [EL-090 section 3](EL-090-steerable-blimp.md#3-engine-capabilities) and the Batch L summary. This part uses it on supply-fed inputs and off the centre of mass |
| **An ambient angular drag on a body** | missing | `MassFrameDeclaration` and `AmbientLoadDescriptor` carry linear drag only (`CuriousContraptions.Abstractions/MassFrameValues.cs:38`–`:39`, `GasCouplingValues.cs:19`). Owner: the angular-drag rider on **ENGINE-REACTION-THRUST** (coordinator decision 16; audit H1 text, in the summary) |
| **A light receiver on a collider face of a dynamic body (disc aperture, sample point, channel strength), read by the facing term, near-field rule and own-body occlusion of EL-176 conventions 1, 5 and 8** | missing | nothing optical exists. Owner: **ENGINE-OPTICAL** (`engine-optical-propagation`). No acceptance cone is asked for: the 25° cone of the first repair is withdrawn |
| **A threshold node over a committed receiver strength, with a release ratio, usable by a supply contact (EL-176 convention 9)** | missing | Owner: **ENGINE-SIGNAL-NODES** (owner decision 3a; coordinator decision 16: receivers couple through observation → signal node → signal-state contact only) |

Counts: 6 present, 2 present-untested, 0 narrower, 4 missing; the four missing rows have three owners (ENGINE-REACTION-THRUST two, ENGINE-OPTICAL one, ENGINE-SIGNAL-NODES one). Nothing needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Guided toy projectile follows only supported sensing and finite actuation"; outcome "No supply or lost guidance cannot yield unlimited corrective thrust" ([element-105](../requirements.md#element-105)).
- No legacy missile exists (`git grep -i` for "missile", "seeker", "homing" at `a6c914e` finds nothing).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment, not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | carry forward the rule into ENGINE-OPTICAL's transport; not the CPU sweep |
| L2 | A store debit never exceeds stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | carry forward |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

Requirement row: [element-105](../requirements.md#element-105). Planned test class `ToyMissileTests`. "Beacon" is the white diverging emitter of section 2 (radiant (1, 1, 1) W, half-angle 30°, range 16 m, axis toward the dart's launch point), realised by the engine story's qualification emitter until an emitter part is delivered. The dart starts at rest on a Ramp 3 m above the bench, nose toward +X; "the line" is its launch line. Every row fits the default 30 s Run.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a lit beacon 6 m ahead and 2 m above the line, and a pulse, then the dart turns and passes close to the beacon | `TheDartFliesToALightItCanSee` | A2 | closest approach of the nose under 0.6 m (hand 0.3 m); 5 s guard |
| A2 | Given the same scene with the beacon off, then the dart flies straight on | `WithoutALightItFliesStraight` | A1 | closest approach over 1.5 m (hand 2.2 m); both nose jets' work exactly zero |
| A3 | Given an opaque Wall panel directly in front of the lit beacon, then the dart flies straight on as in A2 | `AHiddenLightGivesNoGuidance` | A1 | as A2 |
| A4 | Given the beacon 2 m below the line at 6 m, then the dart turns down to it | `TheDartTurnsDownToALowerLight` | A2 | closest approach under 0.6 m |
| A5 | Given A1's scene, then the nose comes onto the line of sight and stays there | `TheNoseTurnsToTheLightAndSettles` | A2: the nose stays within 2° of level | pitch within 6° of the line of sight within 1.0 s of launch (hand 0.4 s) and at every later sample until the nose is within 1 m of the beacon (hand bound 3.8°: the band at launch) |
| A6 | Given `enabled` Disabled or `initial_charge` 0, a lit beacon and a pulse, then nothing runs | `WithoutSupplyNothingRuns` (theory, two rows) | A1 | displacement under 0.02 m over 3 s |
| A7 | Given a lit beacon and no pulse, then no jet runs | `AnUnlaunchedDartDoesNotSteer` | A1 | all three jets' work exactly zero |
| A8 | Given a dart held nose-first against a Wall with a lit beacon above it, then the jets run until the cell is empty and then stop | `CorrectiveThrustEndsWithTheCell` | — | cell debit never below the jet work and above it by no more than the ledger residual, and at most `charge`; after empty, every jet's work per tick is zero; empty within 90/66 = 1.4 s plus a 2 s guard |
| A9 | Given two identical beacon parts, one lit and one off, at two positions in the dart's vertical plane (2 m above and 2 m below the line at 6 m), with the two swapped between runs, then the dart goes to the lit one each time | `OnlyLightGuidesItNotIdentity` (theory, two rows) | — | closest approach to the lit one under 0.6 m |
| A10 | Given a Vacuum atmosphere and a pulse, then the dart is not propelled; it stays on the ramp | `InVacuumItIsNotPropelled` | A1 | forward travel under 0.1 m; cell debit zero |
| A11 | Given `thrust` 9 N, then the edit is refused; `charge` 300 J is clamped to 200 | `OutOfRangeValuesAreRefusedOrClamped` | defaults accepted | typed result |
| A12 | Reset restores pose, a full cell, a clear latch and unlit eyes; save and load keep parameters and the wire | `ResetAndSaveLoadRestoreTheDart` | — | also `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A13 | A Battery `Supply` offered to `ActivationIn` is refused | covered generically (`BundlePortsTests.cs:262`) | an `ActivationOut` is accepted | typed refusal |
| A14 | Given the dart held level at rest (no pulse) and a lit beacon aimed at it, placed in turn at (3, +0.15, 0) and (3, −0.15, 0) from the dart's centre, then `eye_up` alone is on for the light above and `eye_down` alone for the light below, and the other eye's strength is zero | `TheEyesShareNoDirectionAndLeaveABand` (theory, four rows) | at no sample of any row are both eyes on | boundary: at (3, +0.09, 0) and at (3, 0, 0), inside the slab, neither eye is on |

Row A14 tests the band directly. From `rx_up` at (0.36, 0.08), a light at (3, 0.15) is at (2.64, 0.07): d = 2.641 m, facing 0.07 ÷ 2.641 = 0.0265, reading 1.493 × 10⁻³ × 0.0265 ÷ 2.641² = 5.7 × 10⁻⁶ W, over 2.0 × 10⁻⁶; `rx_down`, at (0.36, −0.08), faces away (facing < 0) and reads zero. At (3, 0.09) the light is 0.01 m above `rx_up`'s plane: reading 1.493 × 10⁻³ × (0.01 ÷ 2.64) ÷ 2.64² = 8.1 × 10⁻⁷ W, under the threshold; at (3, 0) it is below `rx_up`'s plane and above `rx_down`'s, so both facings are negative. The slab's half-height at 3 m is 0.08 + 1.34 × 10⁻³ × 2.64³ = 0.105 m, between 0.09 and 0.15. A receiver with an acceptance cone, eyes placed off their faces, or a threshold node with the wrong comparison fails one of the four.

### Integration

- Launched by CAT-063 Impact switch, CAT-017 Clock (delivered) or [EL-094](EL-094-detonation-plunger.md). Guided by any optical emitter part of Epic 13 with a diverging cone; hidden by CAT-066 Wall. It shares "an eye is a declared receiver feeding a declared contact through a threshold node" with the character specs EL-082, EL-083 and EL-087.
- Campaign: no ledger row names it. It falls under chapter 10's "toy rocket/ignition/destruction variants" (`docs/planning/requirements.md:2295`); owner decision 24c gives it new level **97a** *Follow the Light* (chapter 10): introduction 97a, practice 100, reuse 142 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that fixes a beacon or a launch rail as authored scenery needs S8.

### Delivery

- **Shape.** Single element slice, last of the jet run.
- **Prerequisites, in order.** The engine programme (owner decision 3a), of which this part leans on ENGINE-SPATIAL-QUERIES (airflow and optical candidates and occlusion), ENGINE-SUPPLY-DEMAND (the cell's debit) and ENGINE-ISLANDS-SLEEP; ENGINE-SIGNAL-NODES (the threshold nodes, before their first consumer); then ENGINE-OPTICAL; then ENGINE-REACTION-THRUST with its angular-drag rider; then EL-092; one delivered optical emitter with a diverging cone for the campaign level (the tests can use the story's qualification emitter); then S7 (section 7). Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/ToyMissile/` (project, declaration, `toy_missile.tscn`, `icon.svg`); `CuriousContraptions.tests/ToyMissileTests.cs`; one `CuriousContraptions.slnx` line; until S3, the trim list and placement switch.
- **Review tier.** Engine tier.
- **Disqualifiers the probe re-checks.** A receiver that cannot sit on a collider face of a dynamic body or cannot drive a supply contact through a threshold node; a receiver shadowed by its own body's collider whose face carries it; internal supply fan-out refused; an ambient reaction that cannot be paid through a supply consumer or that applies no moment off the centre of mass; the thrust story published without the angular drag or with another law (then the numbers under Behaviour are redone); a first simulated flight whose miss is over 0.6 m (then the nose-jet force, the angular drag and the eye threshold are re-chosen under D1 before the slice plans).

## 6. Open questions

All resolved.

1. **The beacon is light** (decided now): the row says "supported sensing"; a declared optical receiver is general, has one owning story, and makes "hidden" mean an opaque solid in the way.
2. **One cell for main and nose jets** (D19): one finite budget is what the outcome sentence asks for.
3. **The dart turns toward the light** (coordinator decision 16, audit H1): the nose jets act 0.30 m ahead of the centre of mass and a declared ambient angular drag damps the turn.
4. **The dart is nearly weightless** (decided now, under the standing delegation): the delivered ambient lift is declared at 9.6 m/s². Without it a wingless jet dart cannot hold height under the jet law, and no wing-lift law exists or is proposed. A coordinator or owner who prefers a heavy dart must accept that it reaches only lights above it.
5. **No `seeker_range` or turn-rate parameter** (D13): range follows from the emitter's cone and the cell; the nose-jet force is fixed.
6. **The map row omits the sensing families** (D14): the map is navigation; the capability table above is the authority.
7. **Eyes facing straight up and down, no acceptance cone** (decided 23 November 2026 under the standing delegation, by game intent; second audit EL-105). EL-176 convention 8 has only the facing term, and the convention owner is not adding a cone. Eyes tilted ±28° off the nose, with facing alone, are both lit for every light within (arccos k − 28°) of the nose: their jets cancel, and that both-lit band widens to 62° as the dart closes, so it would stop steering on the way in. Eyes on opposite faces are never lit together; their band is the neither-lit slab of half-height 0.08 m + T·d³ ÷ (A·I) about the axis line, which narrows on approach to the eyes' own ±0.08 m, so steering tightens as the dart closes. Alternative considered: a septum collider between forward-facing eyes (a geometric band from own-body occlusion); not chosen because it adds a collider and the facing design passes every row.
8. **Threshold 2.0 × 10⁻⁶ W, release ratio 0.9, aperture radius 0.02 m** (starting values, D1): chosen so the band at A1's launch range is ±3.8°, close to the ±3° band the first repair intended, with a 7× reading margin at launch. The 2-D check (which placed the eyes at their ±0.08 m offsets) holds the rows with the threshold halved or doubled.

Needs the owner: none (the angular drag is decided by coordinator decision 16; the eye layout by item 7).

## Engine conformance (22 November 2026; eyes revised 23 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One body, one ambient load, three jets, one source store, three contacts, three consumers, one memory node, two receivers, two threshold nodes, one port, five parameters and bindings. No guidance loop, no target lookup, no state machine.
2. **Generic laws.** "It steers toward the light" is: optical reception (the facing term of convention 8) gives each receiver a strength; a threshold node (convention 9) turns it into a level; a supply contact follows that level; the contact feeds a nose jet; the jet's ambient reaction, acting off the centre of mass, turns the body; ambient angular drag stops the turn; the main jet and ambient linear drag carry the body where it points. Each is a general law any package can declare; the four missing rows have three element-free owners.
3. **Three layers.** Colliders: cylinder, sphere, two boxes on the one body. Visuals: body, nose and fins follow the body pose; eyes and band are printed flat. Animation: plume, eye colours and charge band read committed flow speed, threshold level and charge and write nothing back.
4. **Flow and typing.** One typed Signal input; `enabled` is a Choice; contact states are the `SignalContactState` enum. Light is read from committed state and reaches physics only through the supply network of the next tick (D7).
5. **Flat state.** Bodies 1 (dynamic); colliders 4 (cylinder 1, sphere 1, boxes 2); joints 0; drives 0; ambient loads 1; airflow sources 3 (each declaring the ambient reaction); sensors 2 optical receivers (one sample point each); signal nodes 3 (one memory, two threshold nodes); supply nodes 7 with 6 internal edges; stores 1 (source); presentation bindings 8.
6. **Performance.** Four primitives on one body; three short bounded cones; two receivers, each tested only as a candidate that a diverging segment's cone query returns (ENGINE-SPATIAL-QUERIES; EL-176 convention 11), with one shadow cast per sample point. Resting cost from ENGINE-ISLANDS-SLEEP (owner decision 3a; no sleeping exists today). This part does not design around either.
7. **Tests are scenarios.** Every row runs the production simulation; row A9 proves no identity is consulted and row A14 that the eyes' band is the facing law's.

**Verdict: conforms** (four general capabilities missing, three owners; redesigned to audit finding H1 under coordinator decision 16 and to the second audit under the standing delegation).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec carrying that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `toy_missile` |
| Named identity | EL-105, `../invest/named-elements.md#element-105` |
| Acceptance | `todo-227`, `sequence-task-658` |
| Decision | S378 |
| Mode records | `Configuration=Fixed` |

What it lacks today: `docs/planning/invest/scope-corrections.md` has no `s378` block; `todo-227` is a scope index shared by thirteen identities (`docs/planning/requirements.md:6389`–`:6391`) and is not co-anchored with this identity's own task, `sequence-task-658`, which sits beside `element-105` (`docs/planning/requirements.md:5237`).
