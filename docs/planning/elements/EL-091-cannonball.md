# EL-091 · Cannonball — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-091 · Cannonball · Specialist |
| Requirement anchor | [element-091](../requirements.md#element-091); scope index [todo-197](../requirements.md#todo-197) |
| Named entry | [element-091](../invest/named-elements.md#element-091); source owner **S661** |
| CAT spec refined or extended | Extends the ball family ([CAT-014 Bowling ball](CAT-014-bowling.md)); payload for [CAT-016 Toy cannon](CAT-016-cannon.md) and [CAT-015 Bumper](CAT-015-bumper.md) |
| Related identities | EL-095 Toy revolver (another launcher), EL-097 Pool cue (another launcher), CAT-062 Spring |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirement row separates cargo from launcher: the cannonball declares its own material and mass, and every launcher moves it through generic contact and work, never by identity.

- **Bodies and shapes.** One dynamic sphere: a `WorkshopBall` with a new `BallMaterial.For` arm (`engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L77`), compiled as every ball (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`).
- **Mass and material.**

  | Field | Value | Status |
  | --- | --- | --- |
  | Radius | 0.32 m | **proposed**: a 0.64 m width fits the cannon's seating window of 0.6–0.84 m (`parts/CannonPart.cs@a6c914e:L44-L45`) |
  | Mass | 6 kg | **proposed**: "heavy", above the 4 kg Bowling ball, within the admitted 1/1024–1024 kg (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L83`) |
  | Bounce | 0.08 | **proposed**: cast iron barely rebounds, below the Bowling ball's 0.14 |
  | Rolling resistance | 0.02 | **proposed**: a hard, smooth sphere rolls further than the Bowling ball (0.03) |
  | Drag | 0.04 1/s | Shared ball declaration (`engine/gpu/WorkshopConstruction.cs@a6c914e:L43-L51`) |
  | Buoyancy | 0 | Same |
  | Friction | 0.3 | Same |
  | Bounce threshold | 0.1 m/s | Same |

- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** None owned. It is a payload for the cannon chamber, the bumper's contact work, Switch impact and Pressure plate load.
- **Work and energy stores.** None.
- **Parameters.** None authored; the material is fixed by kind and admitted bit-for-bit (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`).
- **Cosmetic curves and UI bindings.** None (`WorkshopBall.Cosmetic` is `None`).
- **Art (DESIGN.md).** A dark slate `#556573` sphere (the structural slate, `DESIGN.md@a6c914e:L189-L189`) with a pale grey sheen and one gold `#f7cb52` fuse-hole ring (**proposed**: distinct from the Bowling blue `#45639c`, `DESIGN.md@a6c914e:L166-L166`). Satin, not chrome (`DESIGN.md@a6c914e:L193-L193`).
- **Catalogue and inventory entry.** Id `cannonball`, title "Cannonball", category Motion, colour `#556573`, description "A heavy iron ball. Launchers give it far less speed than a light ball." (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction).

- **Exists now.** Every needed family for the body (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`). Contact work from a static owner already pays finite joules to any dynamic body: the Pinball bumper (`engine/gpu/WorkshopBumper.cs@a6c914e:L6-L28`; `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`).
- **Missing.** The `WorkshopPartKind` member, material arm, icon and catalogue resource (S661). The cannon launcher itself: Story 11.4 ([CAT-016](CAT-016-cannon.md)).
- **Dependencies.** CAT-015 Bumper (delivered launcher), CAT-016 Cannon (Story 11.4), CAT-014 Bowling ball (comparison).

## 4. Sources and legacy

- **Requirement row.** [element-091](../requirements.md#element-091): heavy spherical cargo declares material and mass independently of launcher identity; different launchers move the same body through generic contact and work.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i "cannonball"` at a6c914e returned nothing. Cannon payload facts apply; [CAT-016](CAT-016-cannon.md) holds the full launcher harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Cannon seats only payloads whose transverse width is 0.6–0.84 m | `parts/CannonPart.cs@a6c914e:L44-L45` | Carry forward: the proposed radius must seat |
| L2 | Radii 0.29 and 0.43 are unseated, keep charge and are not launched | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L413-L431` | Carry forward as the size control |
| L3 | Cannon release targets an axial speed with impulse limited by stored work (default 45 J) | `parts/CannonPart.cs@a6c914e:L46-L46`; `parts/catalog/cannon.tres@a6c914e:L12-L12` | Carry forward: a 6 kg ball leaves at about √(2·45/6) ≈ 3.9 m/s versus 9.5 m/s for a 1 kg Basketball |
| L4 | A default shot of a 1 kg Basketball gives 9.48–9.50 m/s and releases 44.99–45 J | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L38-L63` | Carry forward as the light-ball comparison |

**Files harvested:** `parts/CannonPart.cs`, `parts/catalog/cannon.tres`, `CuriousContraptions.tests/CannonTests.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place a Cannonball and a Basketball in identical lanes, each rolling into a Pinball bumper; after Story 11.4, load each in turn into a charged Toy cannon. Run.
- **Positive.** The same bumper sends the Cannonball away much slower than the Basketball; the cannon launches it at about 3.9 m/s against the Basketball's 9.5 m/s; the kinetic energy given never exceeds the work paid.
- **Negative or control.** The Bowling ball released identically rebounds higher (0.14 against 0.08) and travels differently. An unpowered cannon does not move it.
- **Boundaries.** It seats in the cannon (width 0.64 m); bench rest within 1 mm; isolated drop rebound ratio 0.08² ± 0.015 (`CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67`).
- **Run/Reset.** Reset restores the pose exactly. **Save/Load.** Kind and material survive reload.
- **Integrations.** Requirement-row integration tasks: the [todo-197](../requirements.md#todo-197) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no cannonball row; its launcher sits in the row beginning "Pusher, boxing glove, flipper, wound launcher" (cannon; first use 41–50, reuse 61–100, 111–120, 136–150), and the ledger task there must add a separate cannonball row. Element integrations: Pinball bumper (CAT-015), Toy cannon (CAT-016), Bowling ball comparison (CAT-014), EL-095 Toy revolver and EL-097 Pool cue as further launchers.

## 6. Open questions

1. Mass 6 kg (proposed): confirm it remains movable by the bumper and spring at readable speeds. Unspecified — owner decision (S661).
2. Whether the cannonball also needs a Pressure plate role (it always presses).
