# RAD-02 · Powered X-ray emitter — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-02](../requirements.md#radiation-02) (P1 potential). A toy emitter in game units; not real-world X-ray equipment or shielding guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-02 · Powered X-ray emitter |
| Type | Radiation (supplied photon source) |
| Anchor | [requirements.md#radiation-02](../requirements.md#radiation-02); [named-elements.md#radiation-02](../invest/named-elements.md#radiation-02) |
| Related identities | Steady alternative [RAD-01](RAD-01-gamma-source-capsule.md); receivers [RAD-08](RAD-08-radiation-rate-meter.md), [RAD-09](RAD-09-integrating-dosimeter.md) (burst source); [RAD-07 Collimator](RAD-07-collimator-block.md); [EL-128 Dense shield](EL-128-dense-shield.md). Supply [CAT-005 Battery](CAT-005-battery.md); enable from a switch, a supplied logic contact such as [CAT-013 Both gate](CAT-013-both_gate.md), or a [CAT-017 Clock](CAT-017-clock.md). No CAT spec refined. |
| Proof owner | S609 |
| Roadmap story | unscheduled; campaign 107, 114 (research slots 82, 89) |
| Status | not started |

## 2. Declaration

The requirement fixes: supply and enable produce a bounded directional photon field; supply removal stops emission; a disconnected enable cannot fire; penetration depends on the authored energy band, not a universal type ranking. Values are proposals.

- **Bodies and shapes.** One static box 0.70 × 0.45 × 0.45 m (proposed: a tube housing a little larger than the capsule). Emission window 0.15 × 0.15 m on local +X (proposed: a finite emitting surface, centre-sampled under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration)).
- **Mass and material.** Static, zero mass; static default contact material (1, 0.1 m/s, 0.3) (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input; existing socket, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`) and `EnableIn` (Electrical signal, Input; new socket, proposed: S257 keeps power and signal distinct, so enable never supplies power).
- **Sensors and activation.** Emitter state enum `EmitterState` (Unpowered, Disabled, Emitting) (proposed: typed state instead of booleans). Emitting only while `PowerIn` is available and `EnableIn` is a connected true signal; an unconnected `EnableIn` reads false. State changes commit at the tick boundary; re-enabling restarts emission on the next committed tick with no warm-up (proposed: deterministic restart, which also gives RAD-09 clean bursts).
- **Source declaration.** `RadiationKind.XRay`; activity A = 16 game units/s while Emitting (proposed: same strength as the Gamma capsule so the band, not the source type, is the variable); cone half-angle 15° about local +X with uniform intensity inside and zero outside (proposed: "bounded directional"; a 0.30 m meter face 1 m away lies wholly inside, so it reads ≈ 15.8).
- **Work and energy stores.** none in the part. Electrical draw 2 W while Emitting once finite electrical energy exists (proposed: a modest load against the Battery's open capacity question, [CAT-005 OQ1](CAT-005-battery.md#6-open-questions)).
- **Parameters.** `PhotonEnergyBand` (Soft, Medium, Hard), inspector-selected, default Medium. Direction is the part's orientation, set with the rotate gizmo.
- **Cosmetic curves and UI bindings.** Tube glow ← committed emission (research per-element row); selected-only cone overlay.
- **Art.** Cream `#fff8e9` housing, navy `#293954` window frame, restrained gold `#f7cb52` window glow, engraved band marks (one/two/three chevrons for Soft/Medium/Hard) (proposed: shape-coded band, not colour alone).
- **Catalogue and inventory.** Id `xray_emitter`, title "X-ray emitter", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

### Variant: Soft band (`PhotonEnergyBand.Soft`)
- Through a Standard dense slab (μ 64/m × 0.05 m) T ≈ 0.04: a meter at 1 m reads ≈ 0.6.

### Variant: Medium band (`PhotonEnergyBand.Medium`)
- Same band as the Gamma capsule: T ≈ 0.37 through a Standard dense slab, ≈ 5.8 at 1 m.

### Variant: Hard band (`PhotonEnergyBand.Hard`)
- T ≈ 0.61 through a Standard dense slab, ≈ 9.6 at 1 m — more penetrating than the Medium gamma capsule, proving "no universal type ranking".

### Directions
- Every orientation is legal. Proof covers the four axis-aligned facings in the bench plane (±X, ±Z) (proposed: a finite, enumerable proof set for "test every supported direction").

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| ElectricalPower | missing | Story 8.1; the `Electrical` domain and `PowerIn` socket enums exist, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12` |
| FiniteLedger | missing | unscheduled (S010-D); electrical draw and emitted quanta; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); cone-limited sampled paths: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); directional photon source with bands |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; emitter state returns to Unpowered on Reset |

Also used: the `EnableIn` signal (SignalPropagation, missing; Story 8.1); tube glow via `CosmeticCurveDeclaration` (exists, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L16-L17`); band selector in the inspector (unscheduled).

**Dependencies.** CAT-005 Battery; a supplied enable signal (switch, logic gate or clock); RAD-08.

## 4. Sources and legacy

- [radiation-02](../requirements.md#radiation-02): "Supply removal stops emission; a disconnected enable input cannot fire. Test every supported energy preset, direction and restart."
- [Named entry](../invest/named-elements.md#radiation-02), owner S609; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (tube glow ← committed emission; slots 82 "Only While Powered", 89).
- **Legacy.** No X-ray part, law, test, level or lesson. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1659-L1689`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Place the emitter facing a photon Rate meter 1 m away; Connect Battery → `PowerIn` and a switch-driven supplied signal → `EnableIn`; meter `Supply` → Powered gate.
- **Positive.** Supplied and enabled: reading ≈ 15.8, meter on, gate releases (each band, each of the four facings).
- **Negative / controls.** Cut supply with enable true: emission stops (slot 82 control). Enable wire removed: cannot fire. Meter outside the 15° cone: no reading. Gamma capsule beside it ignores the enable signal.
- **Boundaries.** Cone edge (partial: only receiver samples inside the cone count); enable toggled off then on (restart on the next tick); band comparison through the same dense slab (Hard ≈ 9.6 > Medium ≈ 5.8 > Soft ≈ 0.6); undefined band rejected.
- **Run/Reset.** State returns to Unpowered; band and pose unchanged.
- **Save/Load.** Band, pose and both wires round-trip.
- **Integrations.** Source partner in the cross-element task [radiation-11 integration (sequence-task-419)](../requirements.md#sequence-task-419) (gauge source); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150.

## 6. Open questions

1. Is enable a supplied electrical signal or an activation-domain link (a Clock emits activation)? S257 must decide — owner decision.
2. Proposed cone angle, activity and draw need S606-D (and the finite-electrical decision on CAT-005).
3. Is restart instantaneous, or does the tube need a warm-up time — owner decision.
