# EL-104 · Toy firework — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

**Safety boundary.** An abstract toy: a declared impulse budget and a timed visual or event release. No real pyrotechnic composition, quantity, fuse construction or instructions are modelled or described (requirement row).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-104 · Toy firework · Specialist |
| Requirement anchor | [element-104](../requirements.md#element-104); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-104](../invest/named-elements.md#element-104); source owner **S377** |
| CAT spec refined or extended | None. It shares the thrust law of [EL-092 Toy rocket](EL-092-toy-rocket.md) and the timer of [CAT-022 Delay](CAT-022-delay.md). |
| Related identities | EL-092 Toy rocket, EL-105 Toy missile, EL-094 Detonation plunger, CAT-063 Switch |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The firework differs from the rocket in two ways: a shorter, weaker flight, and a timed **burst**. The burst is an authored visual plus an optional typed activation occurrence; it applies no blast force (**proposed**: keeps it a show piece distinct from EL-093's impulse).

- **Bodies and shapes.** One dynamic body: a box 0.16 × 0.6 × 0.16 m, half extents (0.08, 0.3, 0.08), with the nozzle at local −Y (**proposed**: smaller than the rocket, so the two read differently).
- **Mass and material.** 0.2 kg, restitution 0.2, friction 0.4 (**proposed**: a light paper tube).
- **Constraints.** None.
- **Typed ports.**

  | Socket | Domain | Direction | Status |
  | --- | --- | --- | --- |
  | `ActivationIn` | Activation | Input | **proposed**: ignition command |
  | `ActivationOut` | Activation | Output | **proposed**: one `Burst` occurrence for downstream machines |

- **Sensors and activation.** Ignition starts the burn and a burst timer. The burst fires once at `burst_delay` after ignition, wherever the body is. A spent firework ignores further ignition until Reset.
- **Work and energy stores.** A finite impulse store: `thrust` along body +Y until `total_impulse` is delivered, then zero (the EL-092 law). The burst consumes no physical energy and adds none.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `thrust` | f32 | 1–6 | 4 | N | **proposed**: 20 m/s² at 0.2 kg, a quick climb |
  | `total_impulse` | f32 | 0.2–4 | 2.2 | N·s | **proposed**: a 0.55 s burn; the default flight peaks at about 3.1 m at about 1.12 s |
  | `burst_delay` | f32 | 0.2–5 | 1.2 | s | **proposed**: just after the default apex (about 1.12 s); the Delay box duration range is the precedent ([CAT-022](CAT-022-delay.md)) |
  | `burst_colour` | enum `BurstColour` | Gold, Coral, Cyan, Green | Gold | — | **proposed**: approved palette colours only |

- **Cosmetic curves and UI bindings.** At the committed `Burst` occurrence, the animation worker plays a spherical spray of particles in `burst_colour` for 1.0 s, centred on the committed body position, and the body shows a spent cap. It casts no light into the optics domain (**proposed**: the burst is not an optical source; see Open questions).
- **Art (DESIGN.md).** A cream `#fff8e9` tube with gold `#f7cb52` spiral stripes and a navy `#293954` cap. Burst colours come from the palette: gold `#f7cb52`, coral `#de7058` and green `#62aa78` (`DESIGN.md@a6c914e:L154-L158`), and cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`). Calm, with no screen-wide flash (`DESIGN.md@a6c914e:L264-L264`).
- **Catalogue and inventory entry.** Id `toy_firework`, title "Toy firework", category Motion, colour `#f7cb52` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-02.json` (ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, StateTransaction, StructuralFracture, TopologyTransaction).

- **Exists now.** An activation timer node with a declared duration and Ready/Counting/Finished phases (`engine/gpu/ActivationTimers.cs@a6c914e:L66-L78`). Timer-sourced cosmetic curves (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L58`). Dynamic boxes (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`).
- **Missing.** The body-fixed finite thrust actuator (shared with EL-092): owner S377 or whichever of S662/S377/S378 lands first. ChemicalReaction as the abstract charge: S543 reaction, next S554. A particle-burst presentation binding: owner S377.
- **Dependencies.** CAT-063 Switch or EL-094 Plunger for ignition; CAT-022 Delay timer law (delivered).

## 4. Sources and legacy

- **Requirement row.** [element-104](../requirements.md#element-104): a finite abstract toy charge creates bounded flight and an authored visual or event release; a spent charge does not retrigger; no real pyrotechnic instructions.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "firework" and "pyro" found nothing. Timer and store facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Clock pulse feedback: a 0.25 s linear-decay impulse that defers until visible | `parts/ClockPart.cs@a6c914e:L35-L39` | Carry forward the "presentation waits for visibility" rule for the burst |
| L2 | A finite store debit never exceeds stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | Carry forward |

**Files harvested:** `parts/ClockPart.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Stand the Toy firework upright; wire a Switch to `ActivationIn` and its `ActivationOut` to a Lamp. Drop a ball on the Switch. Run.
- **Positive.** It climbs for 0.55 s, coasts to its apex near 3.1 m at about 1.12 s, and bursts once at 1.2 s; the Lamp lights at the burst.
- **Negative or control.** No ignition: nothing. A second ignition after the burst: nothing. A Wall above it stops the climb by contact, and the burst still fires at 1.2 s. Nearby balls are not pushed by the burst.
- **Boundaries.** Impulse delivered equals `total_impulse`; exactly one `Burst` per Run; parameters out of range are refused.
- **Run/Reset.** Reset restores the pose, full store, timer and unspent state. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-07 signal propagation](../requirements.md#interaction-07) (`Burst` output), [IX-28 chemical reaction](../requirements.md#interaction-28) and [IX-29 reaction ignition](../requirements.md#interaction-29). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no firework row; the nearest family is the row beginning "Original cat/mouse lure/escape roles" (rocket; 91–100, reuse 136–150) and chapter 10 "Oddly Satisfying" ("toy rocket/ignition/destruction variants") in the [campaign plan](../requirements.md#campaign-plan); the ledger task must add a separate row. Element integrations: Switch (CAT-063) or EL-094 Plunger ignition, Signal lamp on `Burst` (CAT-035), EL-092 thrust law, Delay timer law (CAT-022).

## 6. Open questions

1. Whether the burst should be an optical source that lights a Light receiver. Unspecified — owner decision (S377).
2. Whether the burst should push nearby bodies (not proposed; that is EL-093's role).
3. Burst at a fixed delay (proposed) or at the flight apex.
