# EL-095 · Toy revolver — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

**Safety boundary.** This is an abstract toy launcher of soft balls. Its energy is a declared store in joules; no real firearm mechanism, propellant or construction is modelled or described.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-095 · Toy revolver · Specialist |
| Requirement anchor | [element-095](../requirements.md#element-095); scope index [todo-197](../requirements.md#todo-197) |
| Named entry | [element-095](../invest/named-elements.md#element-095); source owner **S665** |
| CAT spec refined or extended | Extends [CAT-016 Toy cannon](CAT-016-cannon.md): the same finite-store release law, with a gravity-fed magazine for repeated shots |
| Related identities | EL-091 Cannonball (launcher-independent cargo), CAT-064 Tennis ball (default payload), CAT-005 Battery |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirement row demands that an empty or uncharged state cannot manufacture a projectile. Payloads are real bodies the player loads; the revolver never creates ammunition, the same rule as the cannon (`parts/catalog/cannon.tres@a6c914e:L9-L9`).

- **Bodies and shapes.** One static body (**proposed** sizes: a toy pistol shape around a Tennis-ball bore):
  - Barrel: an open tube, bore radius 0.28 m, half-length 0.5 m along local +X. It needs the annular collider used by the pipe and cannon.
  - Chamber: a cylindrical query region (not a collider) of radius 0.28 m and half-length 0.28 m along local X, centred at x −0.22, so it runs from the closed breech at x −0.5 to x 0.06 (**proposed**: one 0.5 m Tennis ball plus 0.06 m clearance).
  - Magazine: a vertical open tube above the chamber, bore 0.28 m, 1.6 m tall. It holds up to 3 Tennis balls stacked, and gravity feeds the next ball into the chamber after a shot.
  - Grip and foot: box 0.4 × 0.8 × 0.3 m.
- **Mass and material.** Static. Contact material restitution 0, threshold 0.1 m/s, friction 0.3 (the cannon material, [CAT-016 H6](CAT-016-cannon.md)).
- **Constraints.** None.
- **Typed ports.** `PowerIn` (Electrical, Input) at the grip base, and `ActivationIn` (Activation, Input) at the trigger guard (**proposed**: separate supply and trigger, as the cannon, [CAT-016](CAT-016-cannon.md)).
- **Sensors and activation.** A chamber-containment sensor admits exactly one seated payload of width 0.45–0.55 m (**proposed**: a window around the 0.5 m Tennis ball, as the cannon's 0.6–0.84 m window brackets the Basketball, `parts/CannonPart.cs@a6c914e:L44-L45`). A trigger fires only if all three hold:
  - A payload is seated.
  - The store holds at least `shot_energy`.
  - The muzzle is clear.

  Otherwise the result is a typed refusal (`Empty`, `Uncharged`, `Obstructed`) and no energy is spent. Triggers coalesce to the tick boundary and are not queued.
- **Work and energy stores.** One finite store, `capacity`, starts empty. While `PowerIn` is supplied, it charges at `charge_power`, clipped at capacity (`engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L46`). Each shot releases one axial impulse that brings the payload to the target speed, limited by `shot_energy`, and debits exactly the work supplied.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `capacity` | f32 | 5–60 | 24 | J | **proposed**: three default shots, matching the 3-ball magazine |
  | `shot_energy` | f32 | 1–20 | 8 | J | **proposed**: a 0.35 kg Tennis ball leaves at about 6.8 m/s |
  | `charge_power` | f32 | 1–50 | 16 | W | **proposed**: one shot recharges in 0.5 s |
  | `max_speed` | f32 | fixed | 12 | m/s | **proposed**: the Story 11.4 cannon cap ([CAT-016 Open questions](CAT-016-cannon.md)) |

- **Cosmetic curves and UI bindings.** The cylinder rotates one sixth of a turn per accepted shot (an animation driven by the shot occurrence). The charge bar shows the store fill fraction. Rejected triggers show a slate click cue and no recoil.
- **Art (DESIGN.md).** A cyan `#66b8c9` barrel and magazine (the clear pipe tone, `DESIGN.md@a6c914e:L181-L181`), a cream `#fff8e9` cylinder, a navy `#293954` grip and a gold `#f7cb52` charge bar. Clearly a toy (**proposed**).
- **Catalogue and inventory entry.** Id `toy_revolver`, title "Ball blaster", category Motion, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, StateTransaction, StructuralFracture, TopologyTransaction).

- **Exists now.** Static boxes, typed `PowerIn` and `ActivationIn` sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`), and the finite paid-work pattern (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`).
- **Missing.** Every launcher capability that Story 11.4 builds for the cannon: a supply-charged store, chamber containment, muzzle clearance and an axial release impulse ([CAT-016 Engine capabilities](CAT-016-cannon.md)). Annular colliders: Stories 6.6/6.7. ElectricalPower: Story 8.1. ChemicalReaction is inherited and not used (the store is supplied electrically); S665 confirms.
- **Dependencies.** CAT-016 Cannon law (Story 11.4), CAT-048 Pipe annular geometry, CAT-005 Battery, CAT-064 Tennis ball.

## 4. Sources and legacy

- **Requirement row.** [element-095](../requirements.md#element-095): a finite toy launcher supplies bounded work to declared physical payloads; an empty or uncharged state cannot manufacture a projectile.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "revolver", "pistol" and "gun" found nothing. The cannon is the direct legacy precedent; [CAT-016](CAT-016-cannon.md) holds its full harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | The cannon never creates ammunition; empty or obstructed attempts retain charge | `parts/catalog/cannon.tres@a6c914e:L9-L9` | Carry forward |
| L2 | Charging accepts `min(capacity − energy, power × dt)` only while supplied | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L46` | Carry forward. Do not carry forward the CPU ledger. |
| L3 | Firing needs charge; partial charge returns `Uncharged` without spending | `parts/CannonPart.cs@a6c914e:L171-L172` | Carry forward, adapted: per-shot energy instead of full capacity |
| L4 | An empty or uncharged trigger is not queued for a later arrival | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L145-L193` | Carry forward |
| L5 | A distinct arriving ball fires second; the part count never changes | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L212-L268` | Carry forward as the magazine-reload acceptance |

**Files harvested:** `parts/catalog/cannon.tres`, `parts/CannonPart.cs`, `engine/physics/PhysicsEnergyStore.cs`, `CuriousContraptions.tests/CannonTests.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Ball blaster aimed along the bench; drop three Tennis balls into its magazine through the real palette; wire a Battery to `PowerIn` and a Clock (or Switch) to `ActivationIn`. Run.
- **Positive.** Three triggers fire three balls at about 6.8 m/s; each next ball drops into the chamber by gravity.
- **Negative or control.** A fourth trigger with an empty magazine returns `Empty` and launches nothing. Without supply: `Uncharged`, nothing moves. A Wall at the muzzle: `Obstructed`, the charge is kept. A Basketball does not fit the magazine bore.
- **Boundaries.** Delivered kinetic energy per shot ≤ `shot_energy`; total ≤ energy supplied; speed ≤ 12 m/s.
- **Run/Reset.** Reset restores the empty store, the loaded magazine and the ball poses. **Save/Load.** Parameters, wiring and loaded payloads round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-197](../requirements.md#todo-197) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-06 electrical power transfer](../requirements.md#interaction-06) and [IX-07 signal propagation](../requirements.md#interaction-07). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" ("toy revolver") reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Battery supply (CAT-005), Clock (CAT-017) or Switch (CAT-063) trigger, Tennis ball payloads (CAT-064), Wall obstruction (CAT-066), the Toy cannon law (CAT-016).

## 6. Open questions

1. Magazine capacity and payload kind (Tennis ball proposed). Unspecified — owner decision (S665).
2. Supply source: electrical (proposed) or a wound spring (CAT-071 law).
3. Whether the cylinder rotation should be physical (indexing chambers) rather than cosmetic.
