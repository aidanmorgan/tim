# RAD-13 · Decay clock capsule — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-13](../requirements.md#radiation-13) (P2 potential). A **visibly marked fictional short-half-life toy source**; half-lives are game presets, not real isotopes.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-13 · Decay clock capsule |
| Type | Radiation (decaying photon source) |
| Anchor | [requirements.md#radiation-13](../requirements.md#radiation-13); [named-elements.md#radiation-13](../invest/named-elements.md#radiation-13) |
| Related identities | Steady control [RAD-01](RAD-01-gamma-source-capsule.md); receiver [RAD-08](RAD-08-radiation-rate-meter.md); memory partner [RAD-09](RAD-09-integrating-dosimeter.md). Not a renamed [CAT-022 delay](CAT-022-delay.md) or [CAT-017 clock](CAT-017-clock.md). No CAT spec refined. |
| Proof owner | S623 |
| Roadmap story | unscheduled; campaign 123, 124 (research slots 98, 99) |
| Status | not started |

## 2. Declaration

The requirement fixes: a deterministic exponential envelope; authored half-life presets (no random waits, no linear timer); equal half-life intervals halve the strength; a rate contact releases below its threshold; Reset restores initial activity and age. Values are proposals.

- **Bodies and shapes.** Static box 0.30 × 0.40 × 0.30 m with a 0.10 m radius emitting sphere at the centre, centre-sampled under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) (proposed: the RAD-01 envelope, so only the decay differs).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** none — no electrical control of decay.
- **Sensors and activation.** none; the source ages only with committed simulation time from Run start. Pause does not age it; shielding does not slow it.
- **Source declaration.** `RadiationKind.Gamma`, `PhotonEnergyBand.Medium`, isotropic, A(t) = A₀ · 2^(−t / T½) with A₀ = 32 game units/s (proposed: twice RAD-01, so a fresh capsule 1.5 m from a meter reads ≈ 14.1 and stays above Off for more than two half-lives; inside the research activity range 1/16–256). The Off crossing at 1.5 m comes at t = T½ · log₂(14.1 / 3) ≈ 2.24 · T½.
- **Work and energy stores.** Remaining population (RadioactiveDecay ledger); emitted quanta never exceed the initial population. Age t is state.
- **Parameters.** `DecayPreset` enum, inspector-selected before Run, default Medium (research typed name; values below are proposals inside the research half-life range 1–1800 s).
- **Cosmetic curves and UI bindings.** Field overlay dims ← A(t) (research row); an engraved age dial with half-life tick marks advances ← committed age.
- **Art.** Cream `#fff8e9` capsule with a navy `#293954` band engraved with an hourglass and the preset's tick count, gold `#f7cb52` cap (proposed: visibly marked as the decaying kind, distinct from RAD-01 by shape).
- **Catalogue and inventory.** Id `decay_clock_capsule`, title "Decay capsule", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

### Variant: Short (`DecayPreset.Short`)
- T½ = 4 s (proposed: a quick lesson-scale fade); at 1.5 m the default meter opens at ≈ 8.9 s.

### Variant: Medium (`DecayPreset.Medium`)
- T½ = 8 s (proposed); contact opens at ≈ 17.9 s.

### Variant: Long (`DecayPreset.Long`)
- T½ = 16 s (proposed: still observable within a one-minute Run); contact opens at ≈ 35.8 s.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); remaining population; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| RadioactiveDecay | missing | unscheduled (S607-D/F); deterministic population envelope ([S605 decay row](../invest/decisions.md#s605): "at successive declared intervals the detector reads less; Reset restores the initial reading; a steady source does not fade") |
| SignalPropagation | missing | Story 8.1 (the falling-threshold contact belongs to the meter); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; age and activity restore with Reset; Reset-restored timed state already exists for activation timers (`engine/gpu/ActivationTimers.cs`) |

The map row omits IonizingTransport (missing; unscheduled, S606-D/F) and GeometryQuery (exists now for contact only, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204`; radiation paths unscheduled), although the capsule emits a spatial field and the meter proof needs both (open question 2).

**Dependencies.** RAD-08 rate meter; CAT-005 Battery; a release that acts on a falling transition (an inverting gate such as [EL-136 NOR](../invest/named-elements.md#element-136) or a latch).

## 4. Sources and legacy

- [radiation-13](../requirements.md#radiation-13): "Successive equal half-life intervals halve source strength; a rate contact releases below its threshold, and Reset restores the initial activity and age."
- [Named entry](../invest/named-elements.md#radiation-13), owner S623; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) decay row (S607); research "Decay and material memory" law and slots 98 "Half as Bright", 99 "Catch the Window".
- **Legacy.** No decaying source; the legacy "decay" hits are animation impulse curves (`engine/presentation/AnimationImpulseDefinition.cs`), unrelated. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2040-L2069`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Decay capsule 1.5 m from a supplied Rate meter; the meter contact feeds an inverting supplied gate that releases a Powered gate when the contact opens; select each preset.
- **Positive.** Reading halves at each T½ (≈ 14.1 → 7.1 → 3.5 → 1.8) within f32 rounding; the gate releases once the reading falls below Off (≈ 2.24 · T½).
- **Negative / controls.** Steady RAD-01 at a matching initial reading never produces the falling transition. Pausing the Run does not advance age. A dense slab lowers the reading but does not slow the age dial.
- **Boundaries.** Reading exactly at Off; very long Run (activity tends to zero, never negative); undefined preset rejected.
- **Run/Reset.** Activity returns to A₀ and age to 0 exactly.
- **Save/Load.** Preset and pose round-trip; no in-Run age is saved.
- **Integrations.** No row-level cross-element task names the capsule; it integrates through interaction processes [IX-16 Radioactive decay](../requirements.md#interaction-16), [IX-15 Ionizing transport](../requirements.md#interaction-15) and, through the meter's falling contact, [IX-07 Signal propagation](../requirements.md#interaction-07); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 121–130 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 13 of the [campaign plan](../requirements.md#campaign-plan)), reuse 136–150.

## 6. Open questions

1. Proposed half-lives and A₀ need S607-D — owner decision.
2. Coverage gap: the map/binding omit IonizingTransport and GeometryQuery for an emitting capsule — confirm and correct in the owning map.
3. Should Build preview show aged or fresh activity (research: preview "without advancing decay") — confirm fresh.
