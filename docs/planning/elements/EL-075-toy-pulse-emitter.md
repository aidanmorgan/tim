# EL-075 · Toy pulse emitter — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values with no legacy or requirement source are marked **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-075 · Toy pulse emitter · Specialist |
| Requirement anchor | [element-075](../requirements.md#element-075); scope index [todo-214](../requirements.md#todo-214) (shared with [TH-34](../requirements.md#thermal-34)) |
| Named entry | [element-075](../invest/named-elements.md#element-075); source owner **S645** |
| CAT spec refined or extended | Extends [CAT-036 Laser emitter](CAT-036-laser.md) (optical source with separate supply and trigger) |
| Related identities | CAT-005 Battery, CAT-017 Clock, CAT-020 Counter, CAT-038 Light receiver, TH-34 |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

- **Bodies and shapes.** One static body: a housing box 0.9 × 0.6 × 0.6 m, half extents (0.45, 0.3, 0.3) (**proposed**: the Delay box sockets sit at ±0.72 m, so this keeps the emitter a hand-sized module), with the emitting aperture centred on the local +X face. Static box colliders are `ColliderDeclaration` boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Static, zero mass (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L83`). Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3, the shared static-surface convention (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** None; the part is static.
- **Typed ports.**

  | Socket | Domain | Direction | Local position (m) |
  | --- | --- | --- | --- |
  | `PowerIn` | Electrical | Input | (−0.45, 0, 0) rear (**proposed**: the laser puts supply at the rear, `DESIGN.md@a6c914e:L288-L288`) |
  | `ActivationIn` | Activation | Input | (0, 0.3, 0) top (**proposed**: same separation of trigger from supply as the laser) |

  Both sockets exist in `WorkshopSocket` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). No output port: the pulses are optical.
- **Sensors and activation.** One activation request starts one burst of `pulse_count` pulses. **Retrigger rule (proposed):** non-retriggerable; a request that arrives while a burst runs, or a held input, is ignored, and only a fresh occurrence after the burst ends starts the next burst. Justification: the row demands an explicit rule for held input, and ignoring overlap keeps the pulse total bounded by requests × `pulse_count`. An unpowered request emits nothing and is not queued for later supply (the same rule as the laser and the cannon, [CAT-036](CAT-036-laser.md), [CAT-016 H35](CAT-016-cannon.md)).
- **Work and energy stores.** None stored. Emission draws electrical supply only while a pulse is on.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `pulse_count` | integer | 1–9 | 3 | pulses | **proposed**: matches the Counter's 1–9 target range so a counter can confirm the burst (`parts/CounterPart.cs@a6c914e:L39-L41`) |
  | `pulse_on` | f32 | 0.1–2 | 0.25 | s | **proposed**: at least 12 ticks at 120 Hz, so every receiver snapshot sees each pulse |
  | `pulse_interval` | f32 | 0.1–12 | 1.0 | s | **proposed**: the Clock's admitted interval range and default (`parts/ClockPart.cs@a6c914e:L46-L50`; `parts/catalog/clock.tres@a6c914e:L14-L14`) |
  | `pulse_power` | f32 | fixed 1 | 1 | optical power unit | **proposed**: the same unit source power the CAT-036 laser emits |

  `pulse_interval` must exceed `pulse_on`, or the declaration is rejected.
- **Cosmetic curves and UI bindings.** On each committed pulse, the lens eases from slate `#556573` to `#fff0a5` and back, using the lamp pair (`DESIGN.md@a6c914e:L277-L277`). A row of `pulse_count` cream pips on the face lights gold `#f7cb52` per pulse. The binding is a `CosmeticCurveDeclaration` with a new pulse-occurrence feedback source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L58`).
- **Art.** Ochre toy housing `#d69c47`, navy foot `#293954`, cream lens collar `#fff8e9` and a gold top socket, following the laser emitter language (`DESIGN.md@a6c914e:L288-L288`). Repeated-ring pictogram, distinct from the laser. Matte satin material (`DESIGN.md@a6c914e:L193-L193`).
- **Catalogue and inventory entry.** Id `pulse_emitter`, title "Toy pulse emitter", category Optics, colour `#d69c47`, counted inventory allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`) (all **proposed**: mirrors the laser entry).

**Variants.** The requirement row names no variant. Parameter values are not variants.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ElectricalPower, FiniteLedger, GeometryQuery, OpticalTransport, SignalPropagation, StateTransaction).

- **Exists now.**
  - Static box bodies and colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.
  - Typed Activation and Electrical sockets and the connection check: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L62`.
  - Activation network with Timer nodes: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`, timer declaration `engine/gpu/ActivationTimers.cs@a6c914e:L66-L78`.
- **Missing.**
  - ElectricalPower supply evaluation: Story 8.1 (CAT-005); decision owner S257 electrical-port, next S270 ([decisions](../invest/decisions.md#s257)).
  - OpticalTransport source and receiver: Story 13.1 (CAT-029/036); decision owner S484 (S485 finite colour, S486 commit snapshot, S488 optical law) ([decisions](../invest/decisions.md#s484)).
  - A counted-burst timed command (TimedCommand): the current Timer node fires once per duration. Decision owner S645.
- **Dependencies.** CAT-005 Battery, CAT-063 Switch (trigger), CAT-038 Light receiver (observer), CAT-020 Counter (count proof).

## 4. Sources and legacy

- **Requirement row.** [element-075](../requirements.md#element-075): separate supply and control emit an authored bounded number of pulses; an unpowered request emits none; held input follows the explicit retrigger rule. Common contracts: [individual element register](../requirements.md#individual-element-register).
- **Research reference.** The Sierra InterAction Holiday 1994 article cited by [todo-214](../requirements.md#todo-214). Per [todo-228](../requirements.md#todo-228), it establishes intent, not constants.
- **Decisions.** S257, S484 ([decisions](../invest/decisions.md)). No bounded S645 row exists yet.
- **Legacy search.** `git grep -i` at a6c914e over `parts/`, `engine/physics/`, `engine/bridge/`, `engine/*.cs`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` for "pulse emitter", "emitter", "burst" and "pulse" found no pulse-emitter source. The rows below are adjacent facts.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Clock interval is finite and within 0.1–12 s; otherwise construction rejects | `parts/ClockPart.cs@a6c914e:L46-L50` | Carry forward as the proposed interval range |
| L2 | Clock: loss of power resets timing; reconnecting begins a fresh interval | `parts/catalog/clock.tres@a6c914e:L11-L11` | Carry forward by analogy: supply loss cancels the burst (see Open questions) |
| L3 | Clock pulse feedback: 0.25 s linear-decay impulse, maximum overlap, slate to gold | `parts/ClockPart.cs@a6c914e:L35-L39` | Carry forward the cosmetic timing. Do not carry forward the Godot binding. |
| L4 | Counter target is an integer 1–9 | `parts/CounterPart.cs@a6c914e:L39-L41` | Carry forward as the proposed `pulse_count` range |

**Files harvested:** `parts/ClockPart.cs`, `parts/catalog/clock.tres`, `parts/CounterPart.cs`.

## 5. Acceptance outline

- **Chrome recipe.** From the real drawer, place a Battery, a Switch under a dropped Basketball, the pulse emitter and a Light receiver 4 m along its +X, with the receiver wired to a Counter (target 3). Wire Battery `Supply` → `PowerIn` and Switch `ActivationOut` → `ActivationIn` with the connection UI. Run.
- **Positive.** One trigger produces exactly 3 pulses at the authored interval; the receiver lights 3 times; the Counter completes.
- **Negative or control.** Without supply, there are zero pulses, and connecting supply later releases nothing. A second trigger during the burst is ignored (total stays 3). A Wall across the path leaves the receiver dark while the emitter's pips still show 3 pulses.
- **Boundaries.** `pulse_count` 1 and 9; interval 0.1 s and 12 s; `pulse_on` ≥ `pulse_interval` and out-of-range values are rejected atomically at input.
- **Run/Reset.** Reset clears the burst progress, pips and lens exactly. **Save/Load.** Parameters and wiring round-trip, and the burst repeats.
- **Integrations.** Counter, Light receiver, Battery; the TH-34 interaction is proved separately.

## 6. Open questions

1. Retrigger rule: confirm non-retriggerable versus restart-on-request. Unspecified — owner decision (S645).
2. Supply loss mid-burst: cancel the remaining pulses (proposed) or pause and resume.
3. Pulse medium: the binding lists OpticalTransport. Confirm optical pulses rather than activation pulses, and the supported channel set (broadband only is proposed).
4. Historical behaviour from the cited article needs a [todo-228](../requirements.md#todo-228) fixture before campaign use.
