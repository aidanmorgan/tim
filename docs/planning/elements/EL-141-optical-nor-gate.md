# EL-141 · Optical NOR gate — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). Local +X is the outlet direction.

The five optical gates (EL-138 to EL-142) share one body, one aperture layout and one control law; they differ only in the Boolean operation, catalogue entry, truth relief and icon.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-141 |
| Name | Optical NOR gate |
| Type | Optical |
| Anchor | [requirements.md#element-141](../requirements.md#element-141); named entry [named-elements.md#element-141](../invest/named-elements.md#element-141); source record [todo-367](../requirements.md#todo-367) |
| Owner | S513 |
| Refines CAT | [CAT-045 optical_nor](CAT-045-optical_nor.md) (`optical_nor`, "Neither light gate"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-138 to EL-140, EL-142 (sibling operations); EL-136 electrical NOR (separate identity); CAT-036 laser and EL-176 to EL-178 (sources) |
| Roadmap story | 13.8 Optical Logic Gates (AND, NAND, NOR, OR, XOR) (CAT-043 through CAT-047) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cube 1.4 × 1.4 × 1.4 m at the origin; opaque base 1.8 × 0.18 × 1.8 m at (0, −0.85, 0); pick radius 1.3 m. Types: static `RigidBodyDeclaration` and two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass. Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`), rolling resistance 0.
- **Constraints and joints:** none.
- **Typed ports (optical apertures, closed enum `OpticalPortId`):** `First` (A) at (−0.76, 0, 0), normal −X; `Second` (B) at (0, 0.76, 0), normal +Y; both finite discs radius 0.43, Absorb, transmission (1, 1, 1). `Carrier` at (0, 0, 0.76), normal +Z, radius 0.43, Route when open and Absorb when closed, transmission (0.9, 0.9, 0.9). Outlet (0.76, 0, 0), direction +X. Front incidence only. No electrical or activation ports.
- **Sensors and activation:** two hysteretic controls on broadband strength (mean RGB): on at ≥ 0.25, held while > 0.225 game optical power; sampled after the trace, decision advanced once before the next optical solve (next-tick). Reset: A = B = false, open = NOR(false, false) = **open**, so a present carrier passes from the first Run tick.
- **Work and energy stores:** none; output = 0.9 × routed carrier power.
- **Parameters:** none authorable. Constants: operation `Nor` (`LogicGateKind`), on 0.25, off 0.225, retention 0.9. Mode: "Logic=Nor".
- **Cosmetic curves and UI bindings:** control lamps slate `#556573` → gold `#f7cb52` follow committed A/B at rate 12 /s; output lamp follows committed output RGB beam ink at rate 12 /s; typed Boolean observations {First, Second, IsOpen} and output R, G, B scalar observations. The outlet lights only when a carrier exits; an open NOR without carrier stays slate ([DESIGN.md](../../../DESIGN.md#optical-logic-gates)).
- **Art (DESIGN.md palette):** cyan cube `#66b8c9`, navy foot `#293954`, cream control rims `#fff8e9` (one mark on A, two on B), ochre carrier and outlet rims `#e8b764`, slate lenses `#556573`. Truth relief: raised gold dot on row 00 only.
- **Catalogue and inventory entry:** id `optical_nor`, title "Neither light gate", category Optics, colour (0.40, 0.72, 0.79). Description: "The numbered cream inputs control a separate gold-rimmed carrier. Passes 90% of the carrier when neither control is lit. Changes apply next simulation tick; control beams never supply output light." Icon `ui/WorkshopIcons.cs@a6c914e:L78-L78` (shared with `electrical_nor`; current, kept).

### Variants

One; no variants are named. Carrier present/absent is a proof dimension.

| A | B | Open | Output with carrier P | Output without carrier |
| --- | --- | --- | --- | --- |
| off | off | yes | 0.9 P | 0 |
| off | on | no | 0 | 0 |
| on | off | no | 0 | 0 |
| on | on | no | 0 | 0 |

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalTransport, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); contact material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).

**Missing**
- OpticalTransport emission (Story 13.1), Route and Absorb apertures (Story 13.5), switched carrier (Story 13.8). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488.
- SignalPropagation and StateTransaction for hysteretic controls and next-tick commit (S486); FiniteLedger for retention (S485).
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) and no optical port in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`. S257 keeps optical control distinct from electrical or activation wiring.

**Element dependencies:** carrier and control sources (CAT-036 or EL-176 to EL-178, Story 13.1; CAT-005 battery, Story 8.1); an outlet observer (CAT-038 or EL-214, Story 13.2); CAT-066 wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-141](../requirements.md#element-141): carrier output follows neither input present; prove every input combination plus absent carrier. [todo-367](../requirements.md#todo-367). CAT row [current-cat-045](../requirements.md#current-cat-045). Refinement **optical-nor** ([refinements](../invest/refinements.md)): those controls, and all ten logic gates retain feedback bounds and Reset. Campaign: optical logic first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy.** The complete gate harvest, including Boolean observations, lamp-source binding and parameter-ownership tests, is in [CAT-045-optical_nor](CAT-045-optical_nor.md#legacy-harvest) (shared gate facts from [CAT-043-optical_and](CAT-043-optical_and.md#legacy-harvest)); the shared gate body and control law are restated in full in [EL-138](EL-138-optical-and-gate.md#4-sources-and-legacy) items 1–16 and apply unchanged; shared optical facts S1–S7 are in [CAT-036-laser](CAT-036-laser.md#legacy-harvest). NOR-specific facts:
1. Apertures, outlet, retention, Route-when-open — `parts/OpticalLogicPart.cs@a6c914e:L13-L14`, `parts/OpticalLogicPart.cs@a6c914e:L51-L58`. Carry forward.
2. NOR truth `!first && !second` — `engine/LogicGate.cs@a6c914e:L16-L16`; reset sets open = evaluate(false, false), which is true for NOR — `engine/LogicGate.cs@a6c914e:L73-L77`; hysteresis and validation — `engine/LogicGate.cs@a6c914e:L44-L71`. Carry forward.
3. Catalogue entry and scene (Operation = 3) — `parts/catalog/optical_nor.tres@a6c914e:L6-L11`, `parts/scenes/optical_nor.tscn@a6c914e:L1-L5`. Carry forward the entry.
4. Acceptance: NOR rows 00→open, others closed, with and without carrier; reconfiguration restores the open initial decision — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L13-L13`, `CuriousContraptions.tests/OpticalLogicTests.cs@a6c914e:L28-L69`. Carry forward.
5. Acceptance: failed-tick rollback and atomic output observation — `CuriousContraptions.tests/OpticalControlCheckpointTests.cs@a6c914e:L45-L84`, `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L117`; typed Boolean control observations for every truth row — `CuriousContraptions.tests/BooleanObservationTests.cs@a6c914e:L60-L111`. Carry forward (rollback at the later fault gate).
6. Per-part advance hook — `parts/OpticalLogicPart.cs@a6c914e:L50-L50`. Do not carry forward the hook; keep next-tick ordering (Open question 5).

**Files harvested:** `parts/OpticalLogicPart.cs`, `parts/catalog/optical_nor.tres`, `parts/scenes/optical_nor.tscn`, `engine/LogicGate.cs`, `engine/SceneBooleanObservation.cs`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/OpticalLogicTests.cs`, `CuriousContraptions.tests/LogicGateTests.cs`, `CuriousContraptions.tests/OpticalControlCheckpointTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`, `CuriousContraptions.tests/BooleanObservationTests.cs`, `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs`, `CuriousContraptions.tests/BooleanColourBindingTests.cs`. No legacy level uses it.

## 5. Acceptance outline

- **Chrome UI recipe:** place a battery, three lasers (A, B, carrier), the NOR gate and a broadband receiver on its outlet axis from the actual drawer; wire supplies and triggers with the connection UI; aim with the rotation gizmo. Run.
- **Positive:** with only the carrier lit the receiver reads 0.9 × carrier from the first tick; it stays lit until a control goes high.
- **Negative / controls:** lighting A or B darkens the outlet on the next tick; no carrier with both controls dark (dark outlet: an open gate creates nothing); occluders; wrong-face and rear incidence.
- **Boundaries:** next-tick close after the first control rises and reopen after the last falls; 0.25 / 0.225 boundaries; shared range and 16-interaction budget.
- **Run/Reset:** Reset restores A = B = false and the open decision, clears output and lamps. **Save/Load:** construction round-trips.
- **Integrations:** NOR as an inverter (one control unused); chaining; Story 13.8 suite `tools/e2e/cat-optical-logic.test.ts`. Binding criteria: [element-141](../requirements.md#element-141), [current-cat-045](../requirements.md#current-cat-045).

## 6. Open questions

1. Story 13.8 wording ("emitted output laser beams") versus the required separate carrier; requirement presumed binding.
2. Confirm CAT-045 alone satisfies EL-141 (no separate named part).
3. The gate starts open at Reset, so the carrier passes on tick 1 before any control is sampled. Legacy intends this; owner to confirm it is the taught behaviour.
4. Broadband-mean control strength (see EL-138 question 3); confirm under S485.
5. Tick order: legacy advanced the gate before the networks and traced optics before the electrical solve; the current pipeline runs electrical before optics. On which tick does a control change switch the carrier? Unspecified — owner decision (S486; as EL-138 question 5).
