# EL-154 · Light-charge receiver — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths in metres. The sensing face looks along local −X.

No legacy part implements this identity, and legacy explicitly had no light storage ("No ambient-sky power, storage or voltage/current model", `parts/SolarPanelPart.cs@a6c914e:L8-L9`). Every value below that is not a cited mechanism is **proposed** with a justification.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-154 |
| Name | Light-charge receiver |
| Type | Optical |
| Anchor | [requirements.md#element-154](../requirements.md#element-154); named entry [named-elements.md#element-154](../invest/named-elements.md#element-154); scope index [todo-291](../requirements.md#todo-291) |
| Owner | S516 |
| Refines CAT | none. Extends [EL-153](EL-153-general-light-receiver.md) (same sensing face) with a finite charge store; related to [CAT-059 solar_panel](CAT-059-solar_panel.md) (instantaneous, no store). |
| Related | EL-153 general-light receiver; EL-210 flashlight and EL-155 rope-operated light (sources); EL-211 solar panel (converts rather than switches) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** as EL-153 (**proposed**): opaque cream plate 0.22 × 1.2 × 1.2 m at the origin and navy foot 0.9 × 0.16 × 1.2 m at (0, −0.8, 0); static `RigidBodyDeclaration`, two box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** nine front light samples as EL-153 (**proposed**, legacy solar pattern); electrical `PowerIn` (Input) at (0, −0.5, 0.5) and `Supply` (Output) at (0, −0.5, −0.5) (**proposed**), socket identities existing (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** each tick reads irradiance E by the legacy cone law (see Sources). The contact closes (`PowerIn` → `Supply`) when charge Q ≥ release level and opens when Q < reset level. Output persists while charge remains, which is the "release/persistence policy" that distinguishes it from EL-153.
- **Work and energy stores:** one finite charge ledger Q (unit: game optical energy = game irradiance × s; f32). Under constant light from empty, Q(t) = (η E ÷ λ)(1 − e^(−λ t)); all figures below include the leak:
  - intake dQ = η × E × dt with absorption efficiency η = 0.8 (**proposed**: the declared loss; stored energy is always less than absorbed);
  - leak dQ = −λ × Q × dt with λ = 0.05 /s (**proposed**: dark time only drains; a 2 s burst under a torch at 3 m stores 4.06 units, which decays to 2.46 after 10 s of dark, so a second 2 s burst 10 s later brings Q to 2.46 × e^(−0.1) + 4.06 ≈ 6.29);
  - Q is clamped to [0, capacity], capacity 10 (**proposed**: reached after about 5.3 s of continuous light under a torch at 3 m, where E ≈ 24 ÷ 9 ≈ 2.67);
  - the contact draws nothing from Q (the supply is separate), so the store is purely the activation integrator.
- **Parameters** (all **proposed**):

  | Name | Type | Range | Default | Unit | Justification |
  | --- | --- | --- | --- | --- | --- |
  | `release_level` | f32 | 0.5–10 | 6 | game optical energy | One 2 s torch burst at 3 m stores 4.06 (leak included), below 6; a second burst 10 s later reaches ≈ 6.29, matching "two bursts accumulate". |
  | `reset_level` | f32 | 0–release | 3 | game optical energy | Half the release level gives visible persistence (≈ 14 s of dark from 6) without permanent latching. |

  Fixed constants (**proposed**): capacity 10, η 0.8, λ 0.05 /s.
- **Cosmetic curves and UI bindings:** a vertical gold gauge `#f7cb52` on the plate edge fills to Q / capacity from the committed ledger, with a cream tick `#fff8e9` at the release level (**proposed**: shows why it has not switched yet); the gold centre ring eases slate `#556573` → gold over 0.125 s on contact state (receiver-family legacy indicator).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`, nine warm-cream cells `#fff0a5`, navy foot `#293954`, gold gauge and socket studs `#f7cb52` (**proposed**, consistent with EL-153 plus a gauge).
- **Catalogue and inventory entry:** id `light_charge_sensor`, title "Light charge sensor", category Optics (**proposed**). Description (**proposed**): "Light fills its gauge over time. When the gauge passes the mark it closes a contact until the charge fades; connect a separate supply."

### Variants

One. The row names no variants; it is itself the charging variant referred to by [sequence-task-398](../requirements.md#sequence-task-398).

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): ElectricalPower, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction; source-specific composition: absorbed light charges a finite ledger; output release/persistence differs from instantaneous receivers).

**Exists now:** static body, colliders and material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).

**Missing**
- OpticalTransport cone reception — Stories 13.1 and 13.7; S484 ([decisions](../invest/decisions.md#s484)): S486, S488.
- FiniteLedger charge store with intake, leak and clamp, committed per tick — no story yet; S485 (finite allocation) and refinement S710 **charge** ("store/deplete converted optical energy rather than instantaneous free power", [refinements](../invest/refinements.md)).
- ElectricalPower switched contact — Story 8.1 plus an Epic 13 slice; S257.
- OpticalAbsorption/SensibleHeat — S489/S543 (whether the 20% lost intake becomes heat).
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** EL-153 sensing face, EL-210 flashlight (Story 13.1), CAT-005 battery, a load (CAT-035 lamp), CAT-022 delay or CAT-063 switch to pulse the torch.

## 4. Sources and legacy

**Sources.** Row [element-154](../requirements.md#element-154): integrates absorbed optical energy into a bounded store with declared loss; two bursts accumulate correctly; dark time cannot create charge. [sequence-task-398](../requirements.md#sequence-task-398): a charging variant responds to the actual illumination duration. [todo-311](../requirements.md#todo-311): Reset clears charge. [sequence-task-396](../requirements.md#sequence-task-396): "Charge storage … remain pending". Campaign: first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no part; mechanisms only):
1. Solar panel explicitly has no storage — `parts/SolarPanelPart.cs@a6c914e:L8-L9`. Do not carry forward (the absence); it confirms the store is new.
2. Cone law and nine-sample face — `engine/LightNetwork.cs@a6c914e:L12-L52`, `parts/SolarPanelPart.cs@a6c914e:L26-L34`. Carry forward the law.
3. Torch intensity 24, range 8, 15° — `parts/FlashlightPart.cs@a6c914e:L10-L13`. Carry forward (sizing basis for the proposals).
4. Order-independent readings and Reset clearing optical state — `CuriousContraptions.tests/LightTests.cs@a6c914e:L226-L258`. Carry forward.

**Files harvested:** `parts/SolarPanelPart.cs`, `engine/LightNetwork.cs`, `parts/FlashlightPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/LightTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a flashlight 3 m from the sensor, a switch → delay → torch activation chain (or two balls dropped on the button at different times), a battery and a lamp; wire battery → sensor `PowerIn`, sensor `Supply` → lamp. Run.
- **Positive:** two separate bursts raise the gauge in two steps; the lamp lights when Q crosses 6 and stays lit while Q decays to 3.
- **Negative / controls:** one burst alone stays below 6; a dark run of any length leaves Q at 0; an occluded torch adds nothing; supply unwired (gauge fills, lamp off).
- **Boundaries:** capacity clamp at 10 under continuous light; release at 6 and reset at 3; exact Q after two measured bursts within the f32 envelope; Q never negative.
- **Run/Reset:** Reset sets Q = 0 and opens the contact exactly. **Save/Load:** levels, placement and wiring round-trip; Q is runtime state and does not persist.
- **Integrations:** EL-210, EL-155, delay/timer chains. Binding criteria: [element-154](../requirements.md#element-154).

## 6. Open questions

1. Output policy: proposed as a separately supplied contact with hysteresis. Should it instead release stored energy as electrical supply (a light battery)? Owner S516 with S257.
2. All store constants and parameters are proposed (capacity, η, λ, release and reset levels).
3. Does the lost 20% intake become heat (SensibleHeat) or vanish? Owner S489.
4. Does it also charge from narrow laser light? Proposed: cone light only, as EL-153.
5. Roadmap: unscheduled.
