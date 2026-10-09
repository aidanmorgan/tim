# RAD-14 · Magnetic deflector — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-14](../requirements.md#radiation-14) (P3 potential). Toy field steering of fictional toy sources in game units.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-14 · Magnetic deflector |
| Type | Radiation (supplied field region for charged channels) |
| Anchor | [requirements.md#radiation-14](../requirements.md#radiation-14); [named-elements.md#radiation-14](../invest/named-elements.md#radiation-14) |
| Related identities | Charged sources [RAD-03 Alpha](RAD-03-alpha-source-cartridge.md), [RAD-04 Beta-minus](RAD-04-beta-minus-source-cartridge.md); neutral controls [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md), [RAD-16](RAD-16-neutron-source-module.md); trail display [RAD-15](RAD-15-track-chamber.md); obstacle [EL-128 Dense shield](EL-128-dense-shield.md). FieldForce sibling [EL-060 Electromagnet](EL-060-electromagnet.md). Mirrors ([CAT-041](CAT-041-mirror.md)) cannot redirect these channels. No CAT spec refined. |
| Proof owner | S624 |
| Roadmap story | unscheduled; campaign 118, 119 (research slots 93, 94) |
| Status | not started |

## 2. Declaration

The requirement fixes: a supplied field chamber with reversible polarity bends charged trajectories; alpha and beta-minus need distinct curvature parameters; polarity reversal swaps the route; gamma/X-ray stay straight; field-off and opposite-charge cases are proven separately. Values are proposals.

- **Bodies and shapes.** Static body: base plate box 0.40 × 0.10 × 1.00 m (x × y × z) and two pole-piece boxes 0.40 × 0.60 × 0.10 m centred at z = ±0.45 m, so the inner pole faces are at z = ±0.40 m (proposed: a short field region keeps every default-strength path clear of the poles). Field region: box 0.40 (along the beam, local X) × 0.60 × 0.80 m between the poles, field along local Y.
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Pole pieces are `RadiationMaterialKind.Dense` (EL-128) so a path that strikes a pole is absorbed.
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input; existing socket, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`). The field exists only while supplied.
- **Sensors and activation.** none. The field is published as `MagneticPolarity` plus strength while supplied; zero otherwise.
- **Field law (charged channels only).** Inside the region a charged path follows a circular arc of radius r = k_kind / strength in the plane normal to the field; kinetic energy is unchanged. k_BetaMinus = 4 m and k_Alpha = 6 m (proposed: at default strength 4, beta-minus r = 1.0 m and alpha r = 1.5 m — distinct radii, alpha gentler, as the research says equal radii are not implied). Lateral offset at the region exit is r − √(r² − 0.40²) and the exit angle is asin(0.40 / r):
  - beta-minus, strength 4: offset 0.0835 m, exit angle 23.6° (stays 0.32 m clear of the poles);
  - alpha, strength 4: offset 0.0543 m, exit angle 15.5°;
  - at strength 16, beta-minus r = 0.25 m curls into a pole and is absorbed (boundary).
  Photon and neutron paths are unaffected.
- **Work and energy stores.** Electrical draw 3 W while supplied once finite energy exists (proposed: a modest coil load); no work is done on particles.
- **Parameters.**
  - `MagneticPolarity` enum (Forward, Reverse), inspector-selected, default Forward (research typed name).
  - Strength f32, 1/16–16, default 4 (research range "signed, 1/16–16"; default proposed as above).
- **Cosmetic curves and UI bindings.** Coil lamp and optional hum ← supply (research row); engraved polarity arrows that flip with the parameter; selected-only predicted arc preview.
- **Art.** Navy `#293954` coil blocks, gold `#f7cb52` pole caps, cream `#fff8e9` base with an engraved field-direction arrow (proposed: arrow shape shows polarity, not colour).
- **Catalogue and inventory.** Id `magnetic_deflector`, title "Magnetic deflector", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

### Variant: Forward polarity
- Beta-minus turns toward local +Z; alpha toward −Z.

### Variant: Reverse polarity
- Senses swap: beta-minus −Z, alpha +Z.

### Mode: field off (unsupplied)
- Every path goes straight; a path that strikes a pole is still absorbed.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| EnvironmentState | exists now | gravity lane of `RigidBodyDeclaration`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; a typed field region is part of FieldForce below |
| FieldForce | missing | unscheduled (LAW-FIELD-D / LAW-FIELD-V) |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); curved charged-path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); signed charged trajectories distinct from photons |
| RigidBodyDynamics | exists now | bodies `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; integration `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L216-L285` (the deflector itself is static) |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; field state joins it with this element (unscheduled) |

Also used: ElectricalPower (missing; Story 8.1) — the map composition asks for a declared finite field supply.

**Dependencies.** CAT-005 Battery; RAD-03 and RAD-04 (10° emission cones); RAD-08 receivers; EL-128 Dense shield as the obstacle; RAD-15 optional for visible trails.

## 4. Sources and legacy

- [radiation-14](../requirements.md#radiation-14): "Polarity reversal swaps the charged route; gamma/X-ray controls remain straight. Field-off and opposite-charge cases are separately proven."
- [Named entry](../invest/named-elements.md#radiation-14), owner S624; map composition "declare finite ElectricalPower/field supply and reversible polarity; charged path model differs from Photon and neutron channels"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) beta-minus row; research "Charged particles" law and slots 93 "Around the Corner", 94 "Opposite Ways".
- **Legacy.** No magnetic deflector, and no magnet or field code at all in `parts/`, `engine/`, tests, levels or Campaign. Hits are coverage snapshots only: the older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2070-L2102`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

Distances run along the beam axis from the region exit; readings use the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration).

- **Beta layout (actual Chrome UI).** Beta-minus cartridge outlet 0.30 m before the region entry, aimed along the axis; a Thick Dense shield turned edge-on (0.10 m across the beam, 0.80 m along it) centred on the axis from 0.20 m to 1.00 m after the exit; a BetaMinus-mode Rate meter centred 1.00 m after the exit at z = +0.52 m; Battery → `PowerIn`; meter → Powered gate.
  - **Positive (Forward).** The beta trajectory is at z = 0.171 m at the start of the obstacle (0.12 m clear of its 0.05 m half-width; the lowest receiver-sample trajectory still clears by about 0.07 m) and reaches z = 0.520 m at the meter. Path ≈ 1.80 m (within the 3.0 m range); all 9 receiver samples are reachable inside the 10° cone; reading ≈ 4.9, above On; the gate releases.
  - **Field off.** The straight path hits the obstacle; the meter's samples lie 13.9°–20.3° off axis, outside the 10° cone, so it reads 0.
  - **Reverse.** Beta lands at z = −0.52 m (the layout is symmetric about the obstacle): the +0.52 m meter reads 0 and a mirrored meter at −0.52 m responds.
  - **Photon control.** A Gamma capsule at the cartridge position: a photon-mode meter at +0.52 m reads the same in Forward, Reverse and field off.
- **Alpha layout (opposite charge).** Same deflector, no obstacle; Alpha cartridge outlet 0.30 m before the entry; an Alpha-mode meter centred 1.00 m after the exit at z = −0.331 m.
  - **Positive (Forward).** Alpha bends −Z to z = −0.331 m; path ≈ 1.74 m (within the 2.0 m alpha range); reading ≈ 5.3, meter on.
  - **Controls.** Field off: only the 3 nearest receiver samples lie inside the 10° cone, reading ≈ 1.8, below Off. Reverse: alpha bends +Z, reading 0. Under Forward, beta goes +Z while alpha goes −Z (opposite charge).
- **Boundaries.** Strength 1/16 (nearly straight) and 16 (beta curls into a pole and is absorbed); undefined polarity rejected; a mirror cannot redirect beta.
- **Run/Reset.** Polarity and strength unchanged; field off until supplied.
- **Save/Load.** Polarity, strength, pose and wire round-trip.
- **Integrations.** No row-level cross-element task names the deflector, and the [generic interaction register](../requirements.md#generic-interaction-register) has no field-force IX row; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) (charged trajectories) and [IX-06 Electrical power transfer](../requirements.md#interaction-06) (field supply); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), reuse 121–125 and 136–150, with RAD-15 for track reading.

## 6. Open questions

1. Is polarity a build-time parameter only, or also switchable during Run by a signal input — owner decision.
2. Does the field also act on magnetic bodies (balls) like EL-060, or only on radiation channels — owner decision (map lists RigidBodyDynamics).
3. Proposed k values, region length and draw need LAW-FIELD-D and S606-D; so does the charged sampling rule (a receiver sample counts when some launch direction inside the cone reaches it) — owner decision.
