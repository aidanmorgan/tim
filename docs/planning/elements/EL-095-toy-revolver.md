# EL-095 · Toy revolver — named-identity spec

**Current delivery (24 October 2026):** [EL-095 delivery slice](../invest/named-elements.md#element-095). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Every section is written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lx-Ly` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). All game values are canonical f32. No number here was simulated; each is a hand calculation shown beside it.

**Safety boundary.** A toy ball blaster: it pushes a ball the player has loaded. It creates no projectile and models no real mechanism.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-095 · Toy revolver · Specialist |
| Anchor | [requirements.md#element-095](../requirements.md#element-095); [named entry](../invest/named-elements.md#element-095); scope index [todo-197](../requirements.md#todo-197) |
| Proof owner | S665 |
| Disposition | To deliver as a single element, a variant of the delivered Toy cannon's declaration shape. Not a duplicate of `cannon`: it seats a 0.5 m ball where the Cannon seats 0.6–0.84 m, it carries its own finite six-shot supply instead of a `PowerIn`, and one shot costs 8 J instead of 45 J. Not a Family slice: the Cannon is already delivered. The ledger's retained CAT-016 acceptance is kept in rows A2–A8. |
| Planned package | id `toy_revolver`, directory `elements/ToyRevolver/`, class `ToyRevolverElementDeclaration` |
| Refines / extends | [CAT-016 Toy cannon](CAT-016-cannon.md) (`elements/Cannon/CannonElementDeclaration.cs:11`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then S7; no specific engine story beyond the programme |

## 2. Declaration

The package implements the Cannon's interfaces (`CannonElementDeclaration.cs:11`–`:12`): `IEnergyStoreElement`, `IChamberRegionElement` and `IFiniteWorkActuatorElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:35`, `:60`, `:65`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`, `SupportedDomains = Mechanical | Electrical | Signal`. Footprint: 1 body, no constraints.

### Body and colliders

One static body, barrel axis local +X, muzzle at +X. Shared static material with restitution 0, friction 0.3, bounce threshold 0.1 m/s (the Cannon's contact material).

| Collider | Shape | Dimensions (m) | Centre |
| --- | --- | --- | --- |
| Barrel | Tube about X | outer radius 0.33, inner radius 0.28, half-length 0.50 | origin |
| Breech | Cylinder about X | radius 0.33, half-length 0.04 | (−0.54, 0, 0) |
| Drum | Tube about X | outer 0.42, inner 0.33, half-length 0.15 | (−0.25, 0, 0) |
| Grip | Box | 0.25 × 0.60 × 0.25 | (−0.40, −0.63, 0) |
| Foot | Box | 0.90 × 0.10 × 0.60 | (−0.10, −0.98, 0) |
| Socket | Sphere | radius 0.05 | (−0.40, −0.63, 0.125) |

Six colliders against the Cannon's five (`elements/Cannon/cannon.tscn:132`–`:155`); the extra one is the raised socket. The grip's top meets the barrel's underside at y = −0.33 and its bottom meets the foot at y = −0.93.

### Chamber, store, actuator and supply

- **Chamber region** `1` (`ChamberRegionDescriptor`, `CuriousContraptions.Abstractions/ChamberRegionValues.cs:13`): frame body 0, participation collider 0 (the barrel), position (−0.22, 0, 0), rotated −90° about Z so its axis is the barrel's, radius 0.28, half-length 0.28, minimum width 0.45, maximum width 0.55, containment allowance 0.001, maximum relative speed 1 m/s (the last two are the Cannon's, `CannonElementDeclaration.cs:31`–`:32`).
- **Shot store** `1` (`EnergyStoreDescriptor`, `CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`): capacity 8 J fixed, initial 0, charge power the parameter `charge_power`.
- **Actuator** `1` (`FiniteWorkActuatorDescriptor`, `CuriousContraptions.Abstractions/FiniteWorkActuatorValues.cs:26`): store 1, region 1, target speed 12 m/s, exit padding 0.02 m (the Cannon's, `CannonElementDeclaration.cs:34`).
- **Supply graph:** a `SupplySourceDescriptor` `magazine` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:39`) whose store keys are `magazine`, `charge_power`, `initial_charge` and `enabled`; a `SupplyConsumerDescriptor` `charge` targeting store 1; one internal edge `magazine` Output → `charge` PowerIn; no supply ports. Electrical nodes: the Battery's source node and the Cannon's consumer node (`charge`, Consumer, 12 V, 10 Ω, 0 F, 100 W, `CannonElementDeclaration.cs:46`).
- **Signal node:** `SignalPhysicalRequestDescriptor` `release` targeting actuator 1 with `PhysicalRequestOperation.Release`; port binding `ActivationIn` → `release`, Input (`CannonElementDeclaration.cs:47`–`:51`).
- **Events:** `SimulationEventKind.FiniteWorkReleased` for actuator 1.

### Ports

| `PortId` | Domain | Direction | Role | Kind | Local position |
| --- | --- | --- | --- | --- | --- |
| `ActivationIn` | Signal | Input | `ActivationIn` | Pulse | grip front, (−0.40, −0.63, 0.125), normal +Z |

### Parameters

| Key | Kind | Unit | Default | Min | Max | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `magazine` | Real | Joule | 50 | 8 | 96 | Clamp | six 8 J shots with 2 J to spare, so rounding in the transfer cannot cost the sixth |
| `charge_power` | Real | Watt | 16 | 4 | 32 | Clamp | one shot refills in 8/16 = 0.5 s |
| `initial_charge` | Real | None | 1 | 0 | 1 | Clamp | fraction of `magazine` held at Run; 0 is the uncharged control |
| `enabled` | Choice (`Disabled`, `Enabled`) | None | Enabled | — | — | Reject | required by the source store descriptor (`CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`); Build-only here |

### Metadata and bindings

Category `Motion`, display name "Ball blaster", palette order 173 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.40, 0.72, 0.79). Presentation: `Visual/MagazineBar` `ScaleX` on `PresentationDriver.CommittedCharge` of `magazine` (the Battery's driver, `elements/Battery/BatteryElementDeclaration.cs:15`); `Visual/Indicator` albedo (three channel bindings) on `PresentationDriver.CommittedFiniteWorkState` with the Cannon's state colours (`CannonElementDeclaration.cs:63`–`:65`); `Visual/DrumMark` `LocalX` kick on `PresentationDriver.Event` for `FiniteWorkReleased` (the Cannon's sleeve cue, `:66`–`:69`).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Barrel` | cyan `#66b8c9` tube | hollow, Barrel collider | nothing |
| `Visual/Breech` | navy `#293954` disc | solid, Breech collider | nothing |
| `Visual/Drum` | cream `#fff8e9` collar with six round marks printed flat | hollow, Drum collider | nothing |
| `Visual/Grip`, `Visual/Foot` | navy boxes | solid, one collider each | nothing |
| `Visual/Socket` | gold sphere | solid, Socket collider | nothing |
| `Visual/MagazineBar` | gold `#f7cb52` strip printed flat on the grip | decorative: printed flat, scaled by a binding | binding |
| `Visual/Indicator` | disc printed flat on the breech | decorative: printed flat, recoloured | binding |
| `Visual/DrumMark` | printed mark on the drum | decorative: printed flat, moved by a binding | binding |

Icon: a navy short barrel with a drum collar and a grip. It differs from the Toy cannon by the drum collar and the grip (the Cannon has a sleeve and a flat foot) and by its narrower bore.

### Behaviour and numbers

Laws: chamber seating, the paid finite-work release with its clearance check, the finite-store ledger and the supply network, all as delivered for the Cannon.

- **Loading.** The player rolls or drops a ball into the muzzle. The bore is 0.56 m across; a default Tennis ball (0.50 m) enters with 0.03 m of clearance a side and seats. The Baseball seats too (0.52 m across, radius 0.26 m: the repair contract's value, coordinator decision 36). A Basketball (0.68 m) and a Bowling ball (0.56 m, equal to the bore) do not enter.
- **Charging.** From Run the magazine fills the 8 J shot store at 16 W: ready in 0.5 s. 16 W is 0.13 J per tick, well inside any source's per-tick bound. The actuator answers `Uncharged` until the shot store is full (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.FiniteWork.cs:106`).
- **Shot.** A pulse on `ActivationIn` with one seated ball, a full shot store and a clear muzzle releases the store's work along the barrel. A 0.35 kg Tennis ball leaves at √(2 × 8/0.35) = 6.8 m/s, under the 12 m/s target and under the Bumper's 8 m/s. A 0.15 kg Baseball (the repair contract's mass, coordinator decision 36) leaves at √(2 × 8/0.15) = 10.3 m/s, also under the 12 m/s target. Every listed payload is energy-limited, so each shot spends the full 8 J.
- **Finite.** 50 J gives six shots (48 J); the remaining 2 J cannot fill a seventh, so the actuator stays `Uncharged` until Reset. Six shots take at least 6 × 0.5 = 3 s of charging. The count holds for an energy-limited payload, one of at least 2 × 8/12² = 0.11 kg: a lighter ball reaches the 12 m/s target on less than 8 J, leaves the rest in the shot store and so gets more than six shots.
- **Refusals.** Empty chamber, two occupants, an obstructed muzzle or an unfilled store each answer with the Cannon's typed result and spend nothing; a refused pulse is not queued.
- **Tick order.** The pulse is routed in the tick it is published (signal routing is same-tick); the release is a physical request executed at the next physics boundary, as for the Cannon.
- **Tunnelling.** 10.3 m/s is 0.021 m per substep against a 0.26 m radius; the engine's fattened bounds and speculative contacts cover it (`docs/gpu-f32-physics.md:213`, `:276`).

## 3. Engine capabilities

Map row families (navigation only, D14): ContactImpulse, FiniteLedger, FiniteWorkActuation, GeometryQuery, RigidBodyDynamics, SignalPropagation, StateTransaction.

| Capability | Status | Evidence |
| --- | --- | --- |
| Chamber seating, paid release by a Signal request, charge only from supply | present | `ChamberRegionValues.cs:13`, `FiniteWorkActuatorValues.cs:26`; `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` (`CuriousContraptions.tests/CannonIntegrationTests.cs:550`) |
| The same ball returned and fired again, each shot spending one full store, body count unchanged | present | `CannonIntegrationTests.GravityReturnsTheSameBallForAnotherPaidShot` (`CannonIntegrationTests.cs:516`) |
| A source and a consumer joined by a package-internal supply edge, recharging the package's own store | present | `ComposableSupplyRoutingTests.PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` and `.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:499`, `:530`) |
| Width admission at its declared boundaries | present | `ChamberOccupancyTests.WidthAndRelativeArrivalAdmissionIncludeTheirDeclaredBoundaries` (`CuriousContraptions.tests/ChamberOccupancyTests.cs:21`): radii just inside and just outside the declared width, and arrival at and just above the declared speed, give Seated and Loading |
| Wrong-domain connection refusal | present | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| A source store that is **not** run-controlled (`enabled` Build-only) and a charge-fraction bar binding | present, untested | the Battery declares `enabled` as a run control (`BatteryElementDeclaration.cs:48`–`:49`); no opened test covers a Build-only enable or a `ScaleX` charge binding |

Counts: 5 present, 1 present-untested, 0 narrower, 0 missing. Nothing needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Finite toy launcher supplies bounded work to declared physical payloads"; outcome "Empty or uncharged state cannot manufacture a projectile" ([element-095](../requirements.md#element-095)).
- No legacy revolver exists; the Cannon is the precedent and [CAT-016](CAT-016-cannon.md) holds its harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | The cannon never creates ammunition; empty or obstructed attempts retain charge | `parts/catalog/cannon.tres@a6c914e:L9-L9` | carry forward |
| L2 | Charging accepts min(capacity − energy, power × dt) only while supplied | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L46` | carry forward the rule; not the CPU ledger |
| L3 | An empty or uncharged trigger is not queued for a later arrival | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L145-L193` | carry forward |
| L4 | A distinct arriving ball fires second; the part count never changes | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L212-L268` | carry forward as row A5 |

**Files harvested:** `parts/catalog/cannon.tres`, `parts/CannonPart.cs`, `engine/physics/PhysicsEnergyStore.cs`, `CuriousContraptions.tests/CannonTests.cs`.

## 5. Acceptance outline

Requirement row: [element-095](../requirements.md#element-095). Planned test class `ToyRevolverTests`.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a seated Tennis ball, a full shot store and a pulse, then the ball leaves along the barrel | `ASeatedBallIsLaunchedByOnePaidShot` | A2, A3 | muzzle speed 6.0–7.5 m/s (hand 6.8; fast enough to cross a 1 m gap, below the Bumper's 8 m/s); 3 s guard |
| A2 | Given an empty chamber and a pulse, then the result is `Empty`, nothing is spent and the body count is unchanged | `AnEmptyChamberCannotManufactureAProjectile` | A1 | shot store still 8 J; body count exact |
| A3 | Given `initial_charge` 0 or `enabled` Disabled, a seated ball and a pulse, then the result is `Uncharged` and the ball stays | `AnUnchargedBlasterCannotLaunch` (theory, two rows) | A1 | ball speed under 0.01 m/s for 2 s |
| A4 | Given a vertical barrel so gravity returns the default 0.35 kg Tennis ball (energy-limited), and pulses each second, then exactly six shots fire and every later pulse is `Uncharged` | `TheMagazineGivesSixShotsAndNoMore` | `magazine` 96 J gives twelve | count exact (specified); released energy 48 J within 0.5 J; 30 s guard |
| A4b | Given the six shots of A4, then the magazine's debit equals the energy credited to the shot store, and an exhausted magazine credits nothing | `TheMagazinePaysForEveryShot` | — | debit never below the credit and above it by no more than the ledger residual, the bound ENGINE-SUPPLY-DEMAND sets for every source; A4 and A4b fit the default 30 s Run |
| A5 | Given a first ball fired and a second, distinct ball loaded, then the second fires on the next pulse | `ADistinctSecondBallFiresSecond` | — | order of the two release events |
| A6 | Given a pulse 0.2 s after a shot, then `Uncharged`, and nothing fires later without a new pulse | `ARefusedPulseIsNotQueued` | a pulse after 1 s fires | no release in the 2 s after the refusal |
| A7 | Given a Basketball resting on the muzzle, or a Tennis ball with `radius` 0.30 m, then nothing seats and a pulse answers `Empty` | `OversizeCargoIsNotSeated` | default Tennis ball seats | Tennis `radius` 0.27 m (0.54 wide) seats; 0.28 m (0.56) does not enter |
| A8 | Given a Wall 10 mm in front of the muzzle, then `Obstructed` and the charge is kept | `ABlockedMuzzleRefusesAndKeepsItsCharge` | Wall removed: fires | shot store unchanged |
| A9 | Given `charge_power` 40 W or `magazine` 4 J entered, then the value is clamped to its bound; an unknown `enabled` option is refused | `OutOfRangeValuesAreClampedOrRefused` | — | typed result |
| A10 | Reset restores an empty shot store and a full magazine; save and load keep parameters and the wire | `ResetAndSaveLoadRestoreTheBlaster` | — | also `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A11 | A Battery `Supply` offered to `ActivationIn` is refused | covered generically (`BundlePortsTests.cs:262`) | an `ActivationOut` is accepted | typed refusal |

### Integration

- Payloads: CAT-064 Tennis ball and EL-239 Baseball (delivered); [EL-091 Cannonball](EL-091-cannonball.md) only if its radius is set inside 0.225–0.275 m. Triggers: CAT-063 Impact switch, CAT-017 Clock, CAT-022 Delay box (delivered), [EL-094](EL-094-detonation-plunger.md).
- Campaign: chapter 10, levels 91–100, "toy revolver" (`docs/planning/requirements.md:2411`; `docs/planning/invest/campaign-reservations.md:360`); reuse 136–150.
- A level that fixes a blaster with a locked aim needs S8.

### Delivery

- **Shape.** Single element slice; no specific engine story beyond the engine programme.
- **Prerequisites, in order.** The engine programme (owner decision 3a), of which this part leans on ENGINE-SPATIAL-QUERIES (chamber and muzzle-clearance queries), ENGINE-SUPPLY-DEMAND (the magazine's debit) and ENGINE-ISLANDS-SLEEP; then S7 (section 7). S3 optional. Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/ToyRevolver/` (project, declaration, `toy_revolver.tscn`, `icon.svg`); `CuriousContraptions.tests/ToyRevolverTests.cs`; one `CuriousContraptions.slnx` line; until S3, the trim list and placement switch.
- **Review tier.** Engine tier because of the one present-untested row; data-only if the probe finds an exercising test.
- **Disqualifiers the probe re-checks.** A source store whose `enabled` must be a run control; an internal source that cannot feed an energy-store target; a chamber that refuses a 0.28 m radius against a 0.28 m bore.

## 6. Open questions

All resolved.

1. **No feed tube** (D19): the chamber admits exactly one seated ball and a second occupant makes it unavailable, so a stacked hopper would jam it; balls are loaded one at a time through the muzzle.
2. **Its own finite supply, no `PowerIn`** (decided now): "finite toy launcher" is met by a declared source store of six shots; a wired supply would make it a small Cannon.
3. **The drum does not turn physically** (D19): the kick is a presentation cue.
4. **Shot energy fixed at 8 J** (D13).

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** The Cannon's capability set with other values plus one declared source. No engine code names the part.
2. **Generic laws.** Seating is the chamber-region query; the shot is the paid finite-work release; "finite" is the supply network draining a declared source; refusals are the actuator's typed results.
3. **Three layers.** Colliders: two tubes, one cylinder, two boxes and one sphere, all static. Visuals: one mesh per collider; bar, indicator and drum mark printed flat. Animation: five bindings read committed charge, actuator state and the release event; none writes back.
4. **Flow and typing.** One typed Signal input; `enabled` is a Choice; results are the `FiniteWorkRequestResult` enum.
5. **Flat state.** Bodies 1 (static); colliders 6 (tubes 2, cylinder 1, boxes 2, sphere 1); joints 0; drives 0; chamber regions 1; actuators 1; signal nodes 1; supply nodes 2 with 1 edge; electrical nodes 2; stores 2 (shot store, source store); presentation bindings 5.
6. **Performance.** Six static primitives; the chamber is a bounded cylinder. The chamber and clearance queries that scale with the scene are owned by ENGINE-SPATIAL-QUERIES (owner decision 3a); this part does not design around them.
7. **Tests are scenarios.** Every row runs the production simulation with delivered balls.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec carrying that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `toy_revolver` |
| Named identity | EL-095, `../invest/named-elements.md#element-095` |
| Acceptance | `todo-197`, `sequence-task-648` |
| Decision | S665 |
| Mode records | `Configuration=Fixed` |

What it lacks today: `docs/planning/invest/scope-corrections.md` has no `s665` block; `todo-197` is a scope index shared by EL-091 to EL-095 (`docs/planning/requirements.md:4100`) and is not co-anchored with this identity's own task, `sequence-task-648`, which sits beside `element-095` (`docs/planning/requirements.md:5157`).
