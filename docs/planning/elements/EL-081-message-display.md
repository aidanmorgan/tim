# EL-081 · Message display — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-081 · Message display · Specialist |
| Requirement anchor | [element-081](../requirements.md#element-081); scope index [todo-222](../requirements.md#todo-222) |
| Named entry | [element-081](../invest/named-elements.md#element-081); source owner **S651** |
| CAT spec refined or extended | Extends [CAT-020 Counter](CAT-020-counter.md) (count presentation) and [CAT-035 Lamp](CAT-035-lamp.md) (state presentation) |
| Related identities | CAT-005 Battery (supply), CAT-063 Switch, CAT-037 Latch, EL-120 Quantity goal |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: a typed message or state is bound to a resource through shared UI presentation; FiniteWorkActuation is not required merely to display text ([element map](../general-engine-element-map.md)).

- **Bodies and shapes.** One static body: a housing box 1.2 × 0.8 × 0.3 m, half extents (0.6, 0.4, 0.15), with the face on local +Z (**proposed**: a readable sign at bench scale, about the Counter's footprint widened for text).
- **Mass and material.** Static, zero mass; the shared static material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** None.
- **Typed ports.**

  | Socket | Domain | Direction | Local position (m) | Status |
  | --- | --- | --- | --- | --- |
  | `PowerIn` | Electrical | Input | (−0.6, −0.3, 0) | **proposed**: "supplied display" needs a supply |
  | `ActivationIn` | Activation | Input | (0, 0.4, 0) top | **proposed**: count and state arrive as activation occurrences |

- **Sensors and activation.** Received occurrences only. It does not observe the world, goals or names; an unconnected `ActivationIn` can never show a success message.
- **Work and energy stores.** None. It renders only while supplied; without supply the face is blank, though received counts still accumulate (**proposed**: the count is logic state, the face is a load).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `mode` | enum `DisplayMode` | Count, State | Count | — | **proposed**: the row names "state or count" |
  | `target` | integer | 1–9 | 3 | occurrences | **proposed**: the Counter range (`parts/CounterPart.cs@a6c914e:L39-L41`) |
  | `message` | strongly typed `DisplayMessageId` | closed resource set | `WellDone` | — | **proposed**: resource strings stay at a named boundary, per the enum and typed-identity rule ([AGENTS](../../../AGENTS.md)) |

- **Cosmetic curves and UI bindings.** Count mode shows "n / target" and lights one gold dot per occurrence, in rows of three like the Counter (`parts/CounterPart.cs@a6c914e:L59-L63`). When the target is reached, the message resource is shown and the frame eases slate `#556573` to gold `#f7cb52`. State mode shows the message while the latched state is On and a neutral dash when Off. All text comes from localisable resources; colour is never the only cue.
- **Art.** Ochre housing `#d69c47`, cream face `#fff8e9`, navy text `#25344b` and foot `#293954`, following the Counter (`DESIGN.md@a6c914e:L292-L292`). Original speech-sign pictogram.
- **Catalogue and inventory entry.** Id `message_display`, title "Message display", category Control, colour `#d69c47` (**proposed**).

**Variants (modes).** The requirement row names "state or count". Each mode is specified and proved separately:
- **Count mode.** Each received occurrence increments the count up to `target`; reaching it shows the message once and holds it until Reset.
- **State mode.** The first received occurrence latches On and shows the message; there is no Off input in the first slice (**proposed**: matches the activation domain's latch-only phase, `engine/gpu/ActivationNetwork.cs@a6c914e:L8-L9`).

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Activation input sockets, edges and latch nodes (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L62`; `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L25`). Activation-sourced cosmetic curves (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L58`).
- **Missing.** ElectricalPower supply: Story 8.1; S257 electrical-port, next S270. A counting activation node (the Counter's law): Story 9.2 (CAT-020). A text or message UI binding in the presentation layer: owner S651.
- **Dependencies.** CAT-005 Battery, CAT-020 Counter law, CAT-063 Switch as a source.

## 4. Sources and legacy

- **Requirement row.** [element-081](../requirements.md#element-081): a supplied display renders an explicitly received typed state or count; unconnected inputs cannot display a success event.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Research reference.** TIM2 manual ([todo-222](../requirements.md#todo-222)).
- **Legacy search.** `git grep -i` at a6c914e for "message", "display" and "readout" over every Epic 7 deletion path found only the display-clock bridge (`engine/bridge/CommittedDisplayClock.cs`), which is a presentation clock with no element knowledge. Counter facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Counter target is an integer 1–9 | `parts/CounterPart.cs@a6c914e:L39-L41` | Carry forward as the proposed `target` range |
| L2 | Counter dots are laid out in rows of three, one per target event | `parts/CounterPart.cs@a6c914e:L59-L63` | Carry forward the layout |
| L3 | Counter counts trigger events, emits once at the target and stays closed until Reset; it needs a supply to power downstream devices | `parts/catalog/counter.tres@a6c914e:L11-L11` | Carry forward the once-and-hold rule |

**Files harvested:** `parts/CounterPart.cs`, `parts/catalog/counter.tres`; `engine/bridge/CommittedDisplayClock.cs` (checked, no element knowledge).

## 5. Acceptance outline

- **Chrome recipe.** Place a Battery, a Switch under a Ramp lane and the Message display; wire Battery `Supply` → `PowerIn` and Switch `ActivationOut` → `ActivationIn`. Set Count mode with target 3; roll three Basketballs. Run.
- **Positive.** The count reads 1, 2 and 3 in order; at 3 the message appears once and holds.
- **Negative or control.** With `ActivationIn` unconnected, a solved level or three balls never show the message. Without supply, the face stays blank; connecting supply after three hits shows "3 / 3" and the message. State mode with no occurrence shows the dash.
- **Boundaries.** Target 1 and 9; a fourth occurrence does not change the shown message; an unknown `DisplayMessageId` in a save is rejected.
- **Run/Reset.** Reset clears the count, state and face exactly. **Save/Load.** Mode, target, message and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-222](../requirements.md#todo-222) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-06 electrical power transfer](../requirements.md#interaction-06) and [IX-07 signal propagation](../requirements.md#interaction-07). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row "Size grate, weight tray/material sorter, timed ejector/toaster, egg timer, phazer-like toy pulse source, opener, mixer, box presets and contact display" reserves levels 91–100 (reuse 111–120, 136–150); chapter 10 "Oddly Satisfying" ("configurable container/display") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Battery supply (CAT-005), Switch source (CAT-063), Counter law (CAT-020).

## 6. Open questions

1. Supply semantics: should an unsupplied display still count (proposed) or ignore occurrences? Unspecified — owner decision (S651).
2. The message resource set and localisation boundary.
3. Whether State mode needs an Off input (needs a typed clear command).
