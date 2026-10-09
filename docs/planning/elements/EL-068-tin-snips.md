# EL-068 · Tin snips declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-068 |
| Name | Tin snips |
| Type | Mechanical |
| Requirement anchor | [element-068](../requirements.md#element-068); scope index [todo-210](../requirements.md#todo-210) |
| Named entry | [named-elements.md#element-068](../invest/named-elements.md#element-068); proof owner S357 |
| CAT spec refined or extended | none. Related: [CAT-005 battery](CAT-005-battery.md) (Powered mode supply), [CAT-063 switch](CAT-063-switch.md) (Actuated mode trigger), [CAT-071 wound spring](CAT-071-wound_spring.md) (stored-charge release pattern), [CAT-058 rope anchor](CAT-058-rope_anchor.md) (route topology) |
| Related identities | [EL-066 scissors](EL-066-scissors.md) (same cut criterion, unpowered), [EL-067 steel cable](EL-067-steel-cable.md) (resistant target), EL-205 rope |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static housing: box 0.6 × 0.4 × 0.35 m with a fixed lower jaw box 0.35 × 0.06 × 0.1 m. **Proposed**: compact, heavier-looking than scissors.
- Dynamic upper jaw: box 0.35 × 0.08 × 0.1 m, 0.2 kg, hinged at the housing; throat 0.2 m long. **Proposed**.

### Mass and material

Jaw material: restitution 0.1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0. **Proposed**.

### Constraints and joints

- Upper jaw hinge, travel [0°, 35°] (open 35°). **Proposed**.
- Jaw drive: a bounded joint drive closes the jaw while a cut command is active; it delivers at most `cut_work` per closing stroke and at most `power` watts. A blocked jaw stalls without exceeding its work budget.

### Typed ports

| Socket | Domain | Direction | Mode | Local position (m) |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | Powered | (−0.3, −0.1, 0) |
| `ActivationIn` | Activation | Input | both (cut command) | (0, 0.2, 0) |

Identities exist (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). Positions **proposed**.

### Sensors and activation

- Cut criterion: identical to [EL-066](EL-066-scissors.md) — a rope or cable span in the throat, jaw closed past 5°, delivered closing work while the span is in the throat ≥ the span's `cutting_resistance`; then one topology transaction removes the span. No connector-name special case.
- Cut command: one `ActivationIn` occurrence starts one closing stroke.

### Work and energy stores

- **Powered mode:** no store; each stroke draws up to `cut_work` from the electrical supply at ≤ `power`. No supply, no stroke.
- **Actuated mode:** an internal finite charge of `cut_work` (the "actuated" spring) released once by the trigger; one stroke per run (no recharge). **Proposed** reading of "actuated" (Open question 1).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `mode` | enum `SnipsMode { Powered, Actuated }` | — | Powered | — | Row "powered or actuated"; enum **proposed** |
| `cut_work` | f32 | 5–60 | 30 | J | **Proposed**: the "declared cutting-work range"; default exceeds the 25 J steel cable resistance |
| `power` | f32 | 5–100 | 30 | W | **Proposed** (Powered only): a 30 J cut takes about one second |

Closed enum `SnipsParameter { Mode, CutWork, Power }`.

### Cosmetic curves and UI bindings

Jaw pose is the committed hinge pose; a slate/gold lamp shows supply (Powered) or charge present (Actuated).

### Art

Pale grey jaws `#ccd9df`, cyan housing band `#66b8c9` (powered parts family, `DESIGN.md@a6c914e:L180-L180`), navy foot `#293954`, gold lamp `#f7cb52`. Catalogue colour **proposed**: cyan.

### Catalogue and inventory entry

Id `tin_snips`, title "Tin snips", category "Ropes"; `WorkshopPartKind.TinSnips` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The row names no variants. Its "powered or actuated blades" is specified as two modes, each with its own acceptance:

- **Powered:** electrical supply plus cut command; unlimited strokes while supply lasts.
- **Actuated:** a one-shot stored charge released by the cut command.

## 3. Engine capabilities

Binding: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, StructuralFracture, TensionTransmission, TopologyTransaction (map); coverage JSON adds StateTransaction. Neither lists ElectricalPower or FiniteWorkActuation, although the row says "powered" (Open question 2). Map: "Source D: distinct supported material/cutting capacity; no target-name snip".

**Exists now:** electrical and activation sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`); finite contact-work store pattern on a static owner (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`).

**Missing**

- Hinge: Story 10.3. Bounded joint drive (work and power budget): Story 11.3 (linear pusher drive) is the nearest.
- Electrical supply: Story 8.1.
- Span-in-region query, fracture criterion (S563), topology transaction (S565 pattern): no story.

**Dependencies.** A rope or cable route (Stories 10.2/10.4, EL-067); CAT-005 Battery (Powered); a trigger (CAT-063 Switch, delivered).

## 4. Sources and legacy

- Requirements row: "Insufficient work fails on resistant material; no cable-name special case." No variants.
- Design note: EL-066/068 use the shared fracture criterion, not counterpart-type checks (`docs/general-engine-design.md@a6c914e:L149-L149`).

No legacy implementation (rope cutting recorded as future work, `reference/P0-022-before/source-README.md@a6c914e:L99-L99`). Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A bounded drive declares maximum speed, acceleration, force and power = force × speed. | `parts/LinearPusherPart.cs@a6c914e:L28-L32` (via [CAT-039](CAT-039-linear_pusher.md)) | carry forward (pattern) | Jaw drive budget. |
| 2 | A supplied actuator with no supply cannot start motion. | `CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L136-L141` | carry forward | Powered-mode negative control. |

**Files harvested:** `parts/LinearPusherPart.cs` (via CAT-039), `CuriousContraptions.tests/MotorPredictionTests.cs`, `reference/P0-022-before/source-README.md`. Searched for snip, shear, cutter: no hit.

## 5. Acceptance outline

Requirement row: [element-068](../requirements.md#element-068).

- **Chrome UI recipe (Powered).** Place an Anchor, a Weight hanging by steel cable that passes through the snips' throat, a Battery wired to `PowerIn`, and a Switch linked to `ActivationIn` that a rolling ball presses. Verify mode, parameters, wires and route.
- **Positive (Powered).** The Switch fires, the jaw closes, the cable is cut in one tick and the Weight falls.
- **Negative/control.** `cut_work` 20 J on steel cable (25 J): jaw closes, cable intact. No battery: no stroke. Cable routed beside the throat: intact.
- **Actuated recipe and positive.** Same without a battery, `mode` Actuated: the trigger releases the charge and cuts; a second trigger does nothing.
- **Boundaries.** `cut_work` 24/26 J against steel cable; rope (1 J) always cut; power 5 W (slow stroke still cuts); out-of-range rejected.
- **Run/Reset and Save/Load.** Uncut route, open jaw and Actuated charge restore; mode and parameters round-trip.
- **Integrations.** The scope index [todo-210](../requirements.md#todo-210) defines no separate integration task; shared interactions use [IX-37 structural fracture](../requirements.md#interaction-37), [IX-04 tension transmission](../requirements.md#interaction-04) and, in Powered mode, [IX-06 electrical power transfer](../requirements.md#interaction-06). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of scissors and tin snips in levels 11–20.

## 6. Open questions

1. Is "actuated" a one-shot stored charge (proposed) or mechanical actuation by an external rope/shaft input? Owner decision.
2. The binding omits ElectricalPower and FiniteWorkActuation; confirm they should be added for Powered mode. Owner decision.
3. Is the cut command activation (proposed) or continuous supply (cut while powered)? Owner decision.
