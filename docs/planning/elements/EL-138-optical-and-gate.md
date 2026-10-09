# EL-138 · Optical AND gate — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). Local +X is the outlet direction.

The five optical gates (EL-138 to EL-142) share one body, one aperture layout and one control law; they differ only in the Boolean operation, catalogue entry, truth relief and icon. Each is specified in full in its own file.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-138 |
| Name | Optical AND gate |
| Type | Optical |
| Anchor | [requirements.md#element-138](../requirements.md#element-138); named entry [named-elements.md#element-138](../invest/named-elements.md#element-138); source record [todo-367](../requirements.md#todo-367) |
| Owner | S510 (proof owner in the binding shard) |
| Refines CAT | [CAT-043 optical_and](CAT-043-optical_and.md) (`optical_and`, "Both light gate"). One catalogue part satisfies both identities; this is not a second implementation. |
| Related | EL-139 to EL-142 (sibling operations); EL-133 electrical AND (separate electrical identity); CAT-036 laser and EL-176 to EL-178 (control and carrier sources) |
| Roadmap story | 13.8 Optical Logic Gates (AND, NAND, NOR, OR, XOR) (CAT-043 through CAT-047) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body. Opaque cube 1.4 × 1.4 × 1.4 m at the origin; opaque base 1.8 × 0.18 × 1.8 m at (0, −0.85, 0). Pick radius 1.3 m. Current types: `RigidBodyDeclaration` with `RigidMotionKind.Static` and two `ColliderShapeKind.Box` colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** none for mass (static, zero mass). Contact material: legacy static default restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`), declared through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`) with rolling resistance 0.
- **Constraints and joints:** none.
- **Typed ports (optical apertures, closed enum `OpticalPortId`):**
  - `First` (control A): centre (−0.76, 0, 0), normal −X, finite disc radius 0.43, interaction Absorb, transmission (1, 1, 1).
  - `Second` (control B): centre (0, 0.76, 0), normal +Y, radius 0.43, Absorb, transmission (1, 1, 1).
  - `Carrier`: centre (0, 0, 0.76), normal +Z, radius 0.43; interaction Route when the gate is open, Absorb when closed; transmission (0.9, 0.9, 0.9).
  - Outlet: (0.76, 0, 0), direction +X. All faces accept front incidence only; the opaque cube blocks everything outside the discs.
  - No electrical or activation ports: the carrier is a separately supplied optical beam; A/B carry conditions, never output energy.
- **Sensors and activation:** two hysteretic optical control sensors. Each control reads broadband strength (mean of R, G, B received power) on its own aperture; it turns on at ≥ 0.25 and stays on while > 0.225 game optical power. Controls are sampled after the optical trace; the open/closed decision advances once before the next optical solve, so a control change affects the carrier on the next tick. Reset: A = B = false and open = AND(false, false) = closed.
- **Work and energy stores:** none. Output power equals 0.9 × the carrier power actually routed; the gate never creates light.
- **Parameters:** none authorable. Fixed declaration constants: operation `And` (closed enum `LogicGateKind`), on threshold 0.25, off threshold 0.225 (game optical power, f32), carrier retention 0.9. The requirement row records "Logic=And" as the only mode.
- **Cosmetic curves and UI bindings:** control lamps (slate `#556573` → gold `#f7cb52`) follow each control's committed Boolean at exponential rate 12 /s; the output lamp follows the committed output RGB through the shared beam-ink rule (slate when dark) at rate 12 /s. Typed observations: Booleans {First, Second, IsOpen} (closed enum) and output R, G, B in game optical power. The outlet lights only when a carrier actually exits, not when truth permits it ([DESIGN.md](../../../DESIGN.md#optical-logic-gates)).
- **Art (DESIGN.md palette):** cyan cube `#66b8c9`; navy foot `#293954`; cream control rims `#fff8e9` (ring r 0.48, tube 0.055) with one raised cream mark on A and two on B; ochre carrier rim `#e8b764`; slate lenses `#556573` (r 0.43, thickness 0.018); output lamp r 0.34 with ochre rim r 0.40. A cream truth relief shows rows 00, 01, 10, 11 with a raised gold dot on row 11 only.
- **Catalogue and inventory entry:** id `optical_and`, title "Both light gate", category Optics, colour (0.40, 0.72, 0.79). Description: "The numbered cream inputs control a separate gold-rimmed carrier. Passes 90% of the carrier when both controls are lit. Changes apply next simulation tick; control beams never supply output light." Toolbox icon `ui/WorkshopIcons.cs@a6c914e:L75-L75` (current file, kept).

### Variants

One. The row names no variants; the other four operations are separate identities (EL-139 to EL-142), not variants. The with-carrier and without-carrier cases are proof dimensions of this one element, not modes.

| A | B | Open | Output with carrier P | Output without carrier |
| --- | --- | --- | --- | --- |
| off | off | no | 0 | 0 |
| off | on | no | 0 | 0 |
| on | off | no | 0 | 0 |
| on | on | yes | 0.9 P | 0 |

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalTransport, SignalPropagation, StateTransaction. The [element-map row](../general-engine-element-map.md) lists the same families without StateTransaction.

**Exists now**
- Static box body and colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- Contact material declaration: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`.

**Missing**
- OpticalTransport: narrow-ray emission, finite-disc apertures, Absorb and Route interactions with a directed outlet, shared range and interaction budget. Emission is built by Story 13.1 and routing by Story 13.5; Story 13.8 adds the switched carrier. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour allocation, S486 optical-commit snapshot boundary, S488 interval transport law.
- SignalPropagation and StateTransaction: hysteretic control sampling and next-tick decision commit (S486).
- FiniteLedger: carrier retention and no-creation accounting (S485).
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no optical port enum in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- S257 (typed power versus signal connections) confirms that optical control is not an electrical or activation wire.

**Element dependencies:** a supplied, enabled carrier source and up to two control sources (CAT-036 laser or EL-176 to EL-178; Story 13.1, plus CAT-005 battery from Story 8.1); an observer for the outlet (CAT-038 light_receiver or EL-214; Story 13.2); CAT-066 wall as occluder (delivered).

## 4. Sources and legacy

**Sources.** Row [element-138](../requirements.md#element-138): carrier output follows both inputs present; prove every input combination plus absent carrier. Source record [todo-367](../requirements.md#todo-367): absorbing A/B inputs, independent carrier routed at 90% power, next-tick decisions, original icons, separate catalogue entries. CAT row [current-cat-043](../requirements.md#current-cat-043): 0.25 on / 0.225 off, transition, retraction, chaining, occlusion, rotation and preserved range/interaction budget. Refinement **optical-and** ([refinements](../invest/refinements.md)): independent A/B/carrier and occlusion controls. Campaign: optical logic first use 51–60, reuse 71–90, 111–125, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); teaching set-up "two-light-windows" teaches optical AND ([refinements](../invest/refinements.md)).

**Legacy.** The complete gate harvest (17 facts, including Boolean observations, lamp-source binding and parameter-ownership tests) is in [CAT-043-optical_and](CAT-043-optical_and.md#legacy-harvest) and applies unchanged to this identity; shared optical transport facts S1–S7 are in [CAT-036-laser](CAT-036-laser.md#legacy-harvest). The facts an EL-138 story needs are restated here:
1. Geometry, apertures, outlet, retention 0.9 and Route-when-open — `parts/OpticalLogicPart.cs@a6c914e:L13-L14`, `parts/OpticalLogicPart.cs@a6c914e:L51-L58`. Carry forward.
2. Controls use broadband strength on First/Second — `parts/OpticalLogicPart.cs@a6c914e:L59-L61`; strength rule `engine/OpticalColour.cs@a6c914e:L35-L42`. Carry forward.
3. Output power = power leaving the outlet; Active only when output > 0 — `parts/OpticalLogicPart.cs@a6c914e:L62-L66`; outlet accounting at the captured outlet pose — `engine/OpticalNetwork.cs@a6c914e:L171-L182`. Carry forward behaviour; not the CPU static solve.
4. Truth evaluation and closed operation enum; undefined operation rejected — `engine/LogicGate.cs@a6c914e:L6-L19`. Carry forward.
5. Hysteresis 0.25 on / 0.225 off; validation finite 0 ≤ off < on; sample validates both inputs before committing either; advance once; reset to evaluate(false, false) — `engine/LogicGate.cs@a6c914e:L44-L80`. Carry forward.
6. Advance runs before the networks each tick — `parts/OpticalLogicPart.cs@a6c914e:L50-L50`; legacy tick order hooks → cone light → optics → acoustics → electrical — `reference/cpu/MachineWorld.cs@a6c914e:L855-L861`. Do not carry forward the per-part hook or the CPU order; carry forward next-tick behaviour (see Open question 5).
7. Control lamps and output lamp follow at rate 12; art and truth relief — `parts/OpticalLogicPart.cs@a6c914e:L38-L43`, `parts/OpticalLogicPart.cs@a6c914e:L67-L111`. Carry forward behaviour; not Godot materials.
8. Catalogue entry and scene (Operation = 0) — `parts/catalog/optical_and.tres@a6c914e:L6-L11`, `parts/scenes/optical_and.tscn@a6c914e:L1-L5`. Carry forward the entry.
9. Acceptance: for every truth row × carrier present/absent, controls are read on the first solve without changing the decision; after one advance the gate opens iff the row is true; output is (0.9, 0.9, 0.9) for a unit carrier only when open with carrier; all outgoing segments carry 0.9 power (controls never exit); removing the carrier clears the output on the next solve; reconfiguration restores A, B and the initial decision — `CuriousContraptions.tests/OpticalLogicTests.cs@a6c914e:L16-L69`. Carry forward.
10. Acceptance: truth rows are an explicit table — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L5-L19`; exact boundaries 0.25 on, 0.24 off, 0.23 held on, 0.225 releases; a bright A cannot impersonate B — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L56-L74`; NaN, ±∞ or negative samples are rejected with no partial commit — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L76-L90`; invalid thresholds and undefined operations rejected — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L92-L107`. Carry forward.
11. Acceptance: a failed tick restores pending inputs, decision and output; the retry commits — `CuriousContraptions.tests/OpticalControlCheckpointTests.cs@a6c914e:L45-L84`. Carry forward as a later (injected-fault) gate fact.
12. Acceptance: the output observation publishes all three channels atomically across rollback, pause, hide, Reset and Save/Load; output equals 0.9 × carrier (±1e-5); lamp follows 1 − e^(−1.2) per 0.1 s — `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L117`. Carry forward.
13. Legacy beam ink and channel marks — `engine/OpticalColour.cs@a6c914e:L49-L76`. Carry forward as presentation rules.
14. Typed Boolean observation of {First, Second, IsOpen} over the control (closed enum) — `engine/LogicGate.cs@a6c914e:L5-L5`, `engine/LogicGate.cs@a6c914e:L56-L60`, `engine/SceneBooleanObservation.cs@a6c914e:L8-L22`; acceptance: every truth row publishes typed controls without inventing carrier energy; lamps ease 1 − e^(−1.2) per 0.1 s and ignore unpublished state; rollback, hide, Reset and Save/Load restore — `CuriousContraptions.tests/BooleanObservationTests.cs@a6c914e:L60-L111`. Carry forward the typed observation; not the Godot binding.
15. Acceptance: parameter validation cannot replace the captured optical control — `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs@a6c914e:L64-L91`. Carry forward at the later fault gate.
16. Acceptance: unknown or foreign Boolean lamp sources reject before Run and a corrected retry succeeds — `CuriousContraptions.tests/BooleanColourBindingTests.cs@a6c914e:L34-L51`. Carry forward.

**Files harvested:** `parts/OpticalLogicPart.cs`, `parts/catalog/optical_and.tres`, `parts/scenes/optical_and.tscn`, `engine/LogicGate.cs`, `engine/SceneBooleanObservation.cs`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/OpticalLogicTests.cs`, `CuriousContraptions.tests/LogicGateTests.cs`, `CuriousContraptions.tests/OpticalControlCheckpointTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`, `CuriousContraptions.tests/BooleanObservationTests.cs`, `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs`, `CuriousContraptions.tests/BooleanColourBindingTests.cs`. No level in `content/puzzles.json` or lesson in `tools/Campaign` uses an optical gate.

## 5. Acceptance outline

- **Chrome UI recipe:** in the actual Workshop drawer place a battery, three lasers (A, B, carrier), the AND gate and a broadband receiver on the outlet axis; wire battery `Supply` → each laser `PowerIn` and a trigger to each laser `ActivationIn` with the connection UI; aim each laser at its aperture with the rotation gizmo. Run.
- **Positive:** with A, B and carrier lit, the outlet lamp lights on the tick after both controls go high, and the receiver reads 0.9 × carrier power.
- **Negative / controls:** each of 00, 01, 10 with carrier (dark outlet); 11 without carrier (dark: truth cannot create power); A beam aimed into B's face only; control beam on the outlet or carrier face; occluder on each input; rear incidence.
- **Boundaries:** control power 0.25 (on), 0.249 (off), hold above 0.225, release at 0.225; carrier exits at 90%; shared range and 16-interaction budget; next-tick latency, including retraction of one input.
- **Run/Reset:** Reset clears A, B, decision (closed), output and lamps exactly. **Save/Load:** placement, orientation and wiring round-trip; no runtime state persists.
- **Integrations:** chaining one gate's outlet into another gate's control or carrier; filters before controls; Story 13.8 suite `tools/e2e/cat-optical-logic.test.ts`. Binding criteria: [element-138](../requirements.md#element-138) and [current-cat-043](../requirements.md#current-cat-043).

## 6. Open questions

1. Story 13.8 says gates "emit output laser beams"; the requirement row and legacy require a separately supplied carrier. Requirement presumed binding; owner to amend the story text.
2. One catalogue part (CAT-043) is presumed to satisfy EL-138; owner to confirm no separate named part is intended.
3. Control strength is the broadband mean, so a single filtered channel reads one third of its power (an amber laser through a blue filter gives 0.107 and cannot switch a control). Confirm this is intended, under S485.
4. Thresholds are fixed constants in legacy and the CAT row; confirm no authorable threshold is required.
5. Tick order: legacy advanced the gate before the networks and traced optics before the electrical solve; the current pipeline ([docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model-and-scalable-mechanics)) runs the electrical phase before optics. On which tick does a control change switch the carrier, and which snapshot sees a carrier laser's supply loss? Unspecified — owner decision (S486).
