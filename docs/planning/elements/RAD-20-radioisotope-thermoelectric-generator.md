# RAD-20 · Radioisotope thermoelectric generator — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-20](../requirements.md#radiation-20) (P3 potential). A **contained toy decay-heat module**; values are game fiction, never real-world RTG data.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-20 · Radioisotope thermoelectric generator |
| Type | Radiation + Thermal + Electrical (decay-heat source with thermoelectric converter) |
| Anchor | [requirements.md#radiation-20](../requirements.md#radiation-20); [named-elements.md#radiation-20](../invest/named-elements.md#radiation-20) |
| Related identities | Generic converter [TH-30 Thermoelectric generator](../invest/named-elements.md#thermal-30) (RTG = decay heat + the same record); cooling [TH-10 Finned heat sink](../invest/named-elements.md#thermal-10), fan [CAT-028](CAT-028-fan.md); loads such as [CAT-042 Motor](CAT-042-motor.md); startup supply [CAT-005 Battery](CAT-005-battery.md). Not a battery with decorative fins. No CAT spec refined. |
| Proof owner | S631 |
| Roadmap story | unscheduled; campaign 131, 132 (research slots 106, 107) |
| Status | not started |

## 2. Declaration

The requirement fixes: a contained decay-heat module uses a real hot/cold temperature difference to supply bounded electrical power; cooling and load create the puzzle; equal temperatures give no output; heat, electrical work, load and Reset state are accounted for. Values are proposals.

- **Bodies and shapes.** Static box 0.80 × 0.90 × 0.80 m with a cold-side plate 0.60 × 0.60 × 0.04 m on local +X (proposed: Battery-plus scale; the plate is the only cold interface).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Fully contained: emits no ionizing field outside the casing (proposed: the requirement's "contained"; no shielding puzzle here).
- **Constraints.** none.
- **Thermal declaration (SensibleHeat).**
  - Hot core node: heat capacity 10 J/K, decay heat 20 W constant within a Run (proposed: a long half-life source at puzzle time scales; heats noticeably in seconds).
  - Converter: the same generic TH-30 constitutive law, with explicit proposed declaration overrides K = 0.4 W/K, hot capacity 10 J/K, cold capacity 2 J/K and output cap 1 W (compact embedded plates respond faster than TH-30's standalone plates). The cold face has no ambient path when unconnected; its 64 W/K contact face pairs at 32 W/K with TH-10's 64 W/K foot. The attached TH-10 sink retains its 225 J/K capacity and 5 W/K still-air rejection. These are parameter differences, not another converter law.
  - Casing loss core → ambient 0.1 W/K; ambient 288 K (the shared thermal-family ambient); all nodes start at ambient (proposed: an uncooled module settles about 200 K above ambient, inside a sane envelope, and starts with zero gradient).
- **Electrical declaration (ThermoelectricConversion).** Reuse TH-30 exactly: Q_h = K·max(T_h − T_c, 0); P_available = min(output_cap, 0.3·max(1 − T_c/T_h, 0)·Q_h); P_delivered = min(P_available, load demand); Q_c = Q_h − P_delivered. The RTG's explicit parameter overrides above give about 0.56 W with the TH-10 sink after warming, while equal temperatures give zero. Electrical work plus rejected heat equals extracted heat; no alternative ΔT-squared constitutive record is introduced.
- **Typed ports.** `Supply` (Electrical, Output; existing socket, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`) and `ThermalCold` (new `WorkshopConnectionDomain.Thermal`, proposed: the cold plate couples to a sink by an explicit thermal contact, never implicitly to air).
- **Sensors and activation.** none; no on/off switch.
- **Work and energy stores.** Core and cold-node enthalpy; decay inventory (RadioactiveDecay ledger, no depletion within a Run).
- **Parameters.** none player-editable.
- **Cosmetic curves and UI bindings.** Fin/plate tint ← committed temperature, plus an engraved thermometer scale (shape cue); output meter ← committed electrical power (research row).
- **Art.** Cream `#fff8e9` casing, navy `#293954` cold plate with fins, gold `#f7cb52` power meter needle, terracotta `#a96f5c` hot-core band (proposed: approved palette values; no glow).
- **Catalogue and inventory.** Id `radioisotope_thermoelectric_generator`, title "RTG", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row; uncooled, cooled, overloaded and startup states are separately proven.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| ElectricalPower | missing | Story 8.1; the `Electrical` domain and `Supply` socket enums exist, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12` |
| FiniteLedger | missing | unscheduled (S010-D); heat and electrical work accounting; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| RadioactiveDecay | missing | unscheduled (S607-D/F); decay heat source |
| SensibleHeat | missing | unscheduled (S491-D; S543 coupling) |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; temperatures restore with Reset (unscheduled with this element) |
| ThermoelectricConversion | missing | unscheduled (S560-D/F; [S416 thermoelectric row](../invest/decisions.md#s416): "hot/cold pair lights a lamp; equal temperatures or open load light nothing") |

Also needed: the Thermal connection domain (unscheduled with the thermal family).

**Dependencies.** TH-30 converter record; a cold sink (TH-10, fan CAT-028 with a finite startup supply); an electrical load.

## 4. Sources and legacy

- [radiation-20](../requirements.md#radiation-20): "Cooling permits useful output; an equal-temperature control produces no thermoelectric output. Heat, electrical work, load and Reset state are accounted for."
- [Named entry](../invest/named-elements.md#radiation-20), owner S631; map composition "decay deposition feeds SensibleHeat and thermal ports before ThermoelectricConversion; cooling/rejection paths constrain output"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) decay row; [thermal research TH-S09](../../thermal-component-research.md#th-s09) ("a radioisotope generator is decay heat plus this same record"); research "Conversion" law (a cooling fan loop must start from a finite supply, never power itself); slots 106, 107.
- **Legacy.** None. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15438-L15441`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Place the RTG at its initial 288 K, couple ThermalCold by thermal contact to a still-air TH-10 sink, and wire Supply to a Motor authored at 0.1 N·m and 1 rad/s (0.1 W maximum demand), with no external mechanical load. Observe the RTG's available-power and temperature readouts as well as rotor motion.
- **Positive.** From ambient, the core warms and a gradient forms. Available output is about 0.50 W by 60 s, 0.55 W by 120 s and tends toward 0.56 W (hot about 331 K, cold about 292 K); these are matched-load estimates (P_delivered = P_available), while the actual Motor recipe delivers only its demanded share and rejects the rest as heat. Evaluate its measured temperatures with that actual demand rather than asserting the matched-load numbers as exact motor readings. The finite supplied load runs and heat rejection remains accounted.
- **Negative / controls.** Cold plate unconnected: after a small startup transient, the cold node equalises with the hot core; available power is about 0.009 W at 60 s and 0.003 W at 120 s, tending to zero as both approach 488 K. This is reduced output during equalisation, not instant zero. Equal-temperature initial state gives exactly zero output before decay creates the gradient. Overload: apply resisting mechanical loads to the connected Motor shafts and use their real torque/speed controls until the measured summed requested power exceeds the current P_available. Verify that condition before the control; an unloaded pair is not an overload. Delivered power scales within availability and heat/work remains conserved. Fan-cooled variant: the fan's startup must come from a finite Battery; disconnecting the Battery before the RTG supplies the fan stops the loop (no perpetual motion).
- **Boundaries.** First tick ΔT = 0 and P = 0; energy balance (decay heat = casing loss + rejected heat + electrical work + stored enthalpy change) within f32 rounding.
- **Run/Reset.** Temperatures return to ambient, output to zero.
- **Save/Load.** Pose and thermal/electrical links round-trip.
- **Integrations.** Thermal scenario [TX-11 Stored heat powers an electrical load (thermal-scenario-11)](../requirements.md#thermal-scenario-11); interaction processes [IX-16 Radioactive decay](../requirements.md#interaction-16), [IX-43 Sensible heat storage](../requirements.md#interaction-43), [IX-34 Thermoelectric conversion](../requirements.md#interaction-34) and [IX-06 Electrical power transfer](../requirements.md#interaction-06); thermal and electrical port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 131–135 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 14 of the [campaign plan](../requirements.md#campaign-plan), RTG at 131–132), reuse 136–150.

## 6. Open questions

1. Thermal and electrical coefficients need S560-D and S491-D; decay heat over a Run needs S607-D — owner decision.
2. Is the RTG one part, or an assembly of a decay-heat capsule plus a placeable TH-30 converter — owner decision.
3. Thermal connection UI (explicit wire versus contact by placement) — thermal-family owner decision.
