# TH-16 · Fusible link — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-16 delivery slice](../invest/named-elements.md#thermal-16). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and redesigned on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (TH-16: Not ready). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here, checked by a tick-by-tick iteration of the thermal conventions' exact step (T9) at 120 Hz with source credits before links, and is a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); melt-row probe rows I1 to I6 by [TH-14](TH-14-ice-block.md); the load-limit model by [EL-159](EL-159-tension-limited-connector.md) and [Batch M](readiness/batch-m.md) (gap 2).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-16 · Fusible link · Thermal |
| Anchors | [requirements.md#thermal-16](../requirements.md#thermal-16) (task `sequence-task-450`); [named-elements.md#thermal-16](../invest/named-elements.md#thermal-16) |
| Owner key / research | S580 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | Load path: delivered [Pulley](CAT-053-pulley.md) and [Weight](CAT-067-weight.md). Heat from the [TH-04](TH-04-electrical-heating-plate.md) Heating plate's disc touching the collar (the rows), or a conductor or flame region reaching the collar's face. Same load-limit capability as [EL-159](EL-159-tension-limited-connector.md). [RAD-21](../invest/named-elements.md#radiation-21) is a radiation-weakened latch, a different identity. No CAT refines it. |
| Campaign | Intro 85; practice 86; reuse 96, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-CONNECTION-MATERIALS (solid strand: every row hangs a rope, owner decision 1), ENGINE-FRACTURE, ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (D2); redesigned on 23 November 2026 (section 6 item 7) against the second audit's two blockers: the hanging collar could not sit in a heat source without the solid rope and Weight passing through it, and the sub-threshold row had no way to hold the collar below the melt point. |

### Disposition

**To deliver as a single element.** It is not a family member of the load-limited connectors (EL-159 to EL-161): those break attachments between structural parts at a fixed limit; this one is a static rope mount whose limit falls as its wax collar melts. Planned package id `fusible_link`, directory `elements/FusibleLink/`, display name "Fusible link". Not a duplicate of anything delivered.

## 2. Declaration

Follows the delivered Rope anchor for a static body with one anchor tie (`elements/RopeAnchor/RopeAnchorElementDeclaration.cs:14-18`) and the Heating plate's static-face convention (TH-04).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), and the load-limit and node contracts of ENGINE-FRACTURE and ENGINE-THERMAL. `KinematicMode.Static`; `PayloadPolicy.None`; zero static mass frame (as the Rope anchor).
- **Body.** One static body; origin at the centre of the collar's underside, which is the face the player sets on a heat source. Long axis Y, 0.18 m tall.
- **Colliders (metres).** `col_collar`: cylinder, axis Y, centre (0, 0.03, 0), radius 0.065, height 0.06; the thermal face, its underside at y = 0. `col_shackle`: box, centre (0, 0.12, 0), half extents (0.06, 0.06, 0.06). Friction 0.5, restitution 0.1. The collar's radius is at the engine's cylinder minimum of 1/16 m or above (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:240`; 0.065 ≥ 0.0625), and it is proud of the shackle (0.065 against 0.06) so the collar alone meets a flat face set against the part's underside or side.
- **Rope tie.** `Tie` at (0, 0.18, 0), facing +Y: `SimulationDomain.Rope`, bidirectional, `PortRole.Tie`, `RopeAttachmentRole.Anchor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:74`), as the Rope anchor's tie. A rope leaves the part upward, away from whatever heats the collar.
- **Thermal node** `collar` (T2): 12.5 J/K (0.02 kg of wax at 625 J/(kg·K)); ambient conductance 0.08 W/K; face `col_collar`, 16 W/K (a soft face, T11). Against the Heating plate's 64 W/K disc the contact link is 64 × 16 ÷ 80 = 12.8 W/K (T3).
- **Phase row** (T5): melt at 330 K; latent heat 0.02 kg × 12 500 J/kg = 250 J (paraffin's 200 000 J/kg × 1/16, T11); committed melted fraction m from 0 to 1. One-way: no freeze direction is declared, so m never falls during a Run, and a melted collar cools as a liquid with no plateau.
- **Load limit on `Tie`.** Measure: axial tension in the rope tied there. Limit: 58.8 N × (1 − m), a declared clamped linear function of the committed fraction. When the sustained tension exceeds the limit (ENGINE-FRACTURE's one rule; this family's hold time is 0.10 s, EL-159 section 6 item 1), the rope connection at `Tie` is released, once, for the rest of the Run. Nothing else is removed: the part stays where it was placed and the melted wax stays on it.
- **Parameters.** None (D13).
- **Presentation bindings.** `Visual/Gap` scale Y ← m (the navy witness gap widens as the collar melts); `Visual/Band` albedo gold → slate `#556573` when broken (EL-159's convention).
- **Palette.** `ElementCategory.Structure`, with the load-limited connectors. Order: 542 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shackle` gold `#f7cb52`, with an eye printed flush on top | box 0.12 m cube | solid, `col_shackle` | nothing (static) |
| `Visual/Collar` cream `#fff8e9` | cylinder radius 0.065, height 0.06 | solid, `col_collar` | nothing (static) |
| `Visual/Gap` navy `#293954` | flat ring printed on the collar | decorative: printed flat, scaled by a binding | presentation binding |
| `Visual/Band` gold | flat band printed round the shackle | decorative: printed flat, recoloured by a binding | presentation binding |

Icon: a squat cream collar under a gold block with an eye, a rope rising from it. It differs from the Rope anchor (a plain block with a ring) by the cream collar and the printed gap ring, a shape difference, not colour alone.

### Compiled counts

Bodies 1 (static); colliders 2 (1 cylinder, 1 box; the Rope anchor has one, and the collar is what makes the part readable and is its only thermal face); joints 0; rope ties 1; load limits 1; thermal nodes 1; phase rows 1; presentation bindings 2.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol or owner | Test |
| --- | --- | --- | --- | --- |
| TH-04 R1 | Colliders from art | present | as TH-04 | as TH-04 (body read) |
| F1 | A rope from a static anchor tie over a Pulley's guide to a hanging Weight, the tie carrying the Weight's weight | present, untested here | `RopeAttachmentRole.Anchor`, `Guide`, `Load` (`CuriousContraptions.Abstractions/PortDescriptor.cs:74`); route admission `engine/gpu/RopeTopology.cs:28-64` (a route is complete when neither end is a guide) | `RopeTests.ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` (`CuriousContraptions.tests/RopeTests.cs:21`, not opened); the delivered Pulley is frictionless (`RopeTests.TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics`, `:721`, name read) |
| F3 | A one-way release of a rope connection, restored by Reset | narrower: a UI command only | `CutRope`, `CuriousContraptions.Abstractions/RuntimeServices.cs:77` | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`:151`, not opened); owner ENGINE-FRACTURE (one-way release of rope ties) |
| F4 | A declared load limit on a rope tie (axial tension) with a one-way release when sustained load exceeds it | **missing** | none | ENGINE-FRACTURE ("a declared load limit on a connection or body, a one-way break") |
| F5 | A limit multiplied by a declared clamped linear function of a committed scalar | **missing** | none | ENGINE-FRACTURE (text gap, Batch N summary) |
| TH-04 R10 | Conduction between the collar and the Heating plate's disc, a static-on-static pair placed touching | **missing** | none | ENGINE-THERMAL |
| TH-14 I2, I3 | Thermal node with a face; a melt row, here one-way | **missing** | none | ENGINE-THERMAL |
| TH-04 R12 | Phase-fraction presentation driver | **missing** | none | ENGINE-THERMAL |
| F6 | Broken state as a presentation driver | **missing** | none | ENGINE-FRACTURE (Batch M gap 2) |

Counts: present 1, untested 1, narrower 1, missing 7 (F4, F5, R10, I2, I3, R12, F6). Each has one owner. The collar-on-disc pair is in T3's list of static-on-static pairs the family uses ([TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions)). The delivered rope draws a thick strand without a collider; that visible-solid defect belongs to ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026), not to this part. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-16](../requirements.md#thermal-16): finite connector loses strength through its phase/constitutive state; the shared joint/contact lifecycle releases load; no variants.
- **Named entry** [thermal-16](../invest/named-elements.md#thermal-16): owner S580; "subthreshold heating retains support; cooling broken material does not reattach a severed graph".
- **Research** per-element table: "thermal sensor + constraint breaking"; parameter melt T; "breaks a held load above its melt point"; link snap ← state.
- **Element map** row TH-16: EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, SolidMelting, StructuralFracture, TemperatureStrength, TopologyTransaction (D14).
- **Processes.** [IX-42](../requirements.md#interaction-42), [IX-21](../requirements.md#interaction-21), [IX-03](../requirements.md#interaction-03), [IX-37](../requirements.md#interaction-37).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for fusible, link, melt and break terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance

Point of truth: [thermal-16](../requirements.md#thermal-16) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** The delivered tensile law for the rope over the frictionless Pulley; the load-limit rule on `Tie`; the plate's supply grant (TH-04), Q̇ = G·ΔT by T9's exact step, and the melt row for the collar.
- **Rig.** A Heating plate (TH-04) on the bench at x = 0; the link set with its collar's underside on the disc's top (y = 0.21 m), centred, so its tie is at y = 0.39 m. A delivered Pulley placed with its wheel's centre at (0.40, 2.40, 0) m, that is its origin at (0.40, 2.40, −0.12) m, because its guide sits 0.12 m in front of its origin and its rope spans lie in that plane ([CAT-053](CAT-053-pulley.md) P2): its 0.4 m guide radius (`elements/Pulley/PulleyElementDeclaration.cs:22-23`) puts the rising span on x = 0, straight above the tie, and the falling span on x = 0.80 m. One rope from `Tie` over the Pulley's guide to a Weight's tie, the Weight's base 1.0 m above the bench. Nothing passes through the collar or the plate: the rope rises from the top of the shackle, and the Weight hangs 0.80 m from the link's axis. The largest Weight used, 7 kg, has radius 0.0481 × 7 + 0.1593 = 0.496 m (`elements/Weight/WeightElementDeclaration.cs:59`), so its near edge is at x = 0.304 m, clear of the rising rope and the collar (|x| ≤ 0.065 m); the plate's plinth (half width 0.30 m, top 0.20 m) is below the Weight's base. A released Weight falls 1.0 m to the bench.
- **Cold strength.** 58.8 N holds 58.8 ÷ 9.81 = 6.0 kg, so both sides of the limit can be built from delivered Weights, whose `mass` runs from 0.25 to 8 kg (`elements/Weight/WeightElementDeclaration.cs:33`). A default Weight (4 kg) pulls 4 × 9.81 = 39.2 N through the frictionless Pulley.
- **Release point.** 58.8 × (1 − m) < 39.2 when m > 1 − 39.2 ÷ 58.8 = 0.333.
- **Heat to release.** 288 → 330 K: 12.5 × 42 = 525 J. Then 0.333 × 250 = 83 J. Total 608 J into the collar.
- **The source: the Heating plate.** The disc (15.25 J/K, 0.5 W/K to ambient) takes the Battery's grant and passes it to the collar through the 12.8 W/K contact link; the collar loses 0.08 W/K to the room.
  - *Full power* (a default Battery: 120 W, 3600 J, 30 s). Disc and collar warm together at about 120 ÷ (15.25 + 12.5) = 4.3 K/s, the disc about 4 K ahead (the link carries 12.5 × 4.3 = 54 W, 54 ÷ 12.8 = 4.2 K). By hand: 27.75 × 42 = 1166 J less about 12 W of loss gives the disc 330 K near 10.8 s and the collar about 1 s later; then the 83 J melt at 12.8 × (T_disc − 330) ≈ 63 W takes 1.3 s: m = 0.333 near 12.9 s. Tick by tick: the collar reaches 330 K at about 11.6 s; m passes 0.25 at 12.55 s and 0.333 at 12.83 s; the limit falls below 39.2 N in the next tick (12.84 s), and the release follows after the 0.10 s hold. The collar is fully melted at 14.8 s; it peaks at 376 K when the Battery empties at 30 s.
  - *Held below the melt point* (a Battery with `power_limit` 21.5 W, `capacity` 14 400 J, admitted ranges 10 to 480 W and 60 to 14 400 J, `elements/Battery/BatteryElementDeclaration.cs:42-45`; the plate asks 120 W and is granted 21.5 W, 0.179 J a tick, for 14 400 ÷ 21.5 = 670 s). Steady state, with x the collar's rise: the collar's balance 12.8 × (T_disc − T_collar) = 0.08·x puts the disc 0.08 × x ÷ 12.8 = 0.006·x above it, and the disc's balance 21.5 = 0.5 × 1.006·x + 0.08·x = 0.583·x gives x = 36.9 K: **collar 324.9 K, disc 325.1 K**, 5.1 K below the melt. The slow time constant is (15.25 + 12.5) ÷ 0.583 = 47.6 s. Tick by tick: 314.2 K at 60 s, 320.2 K at 100 s, 322.9 K at 140 s, 324.0 K at 180 s, 324.3 K at 200 s, 324.6 K at 240 s; never above 324.62 K; m stays 0 and the limit stays 58.8 N. This is the "subthreshold heating" of the named entry: a steady source that holds the collar at 325 K.
  - *Just above the hold* (`power_limit` 30 W): steady state 288 + 30 ÷ 0.583 = 339.5 K, above the melt; by hand the collar reaches 330 K at 47.6 × ln(51.5 ÷ 9.5) = 80 s. Tick by tick m passes 0.333 at 98.1 s and the limit falls below 39.2 N at 98.3 s. (At 25 W the steady state, 330.9 K, barely passes the melt and m reaches only 0.10 by 240 s, so the boundary row uses 30 W.)
- **Lighter load.** A 1 kg Weight (9.81 N) is released only when m > 1 − 9.81 ÷ 58.8 = 0.833, after 525 + 0.833 × 250 = 733 J. Tick by tick at full power: m passes 0.75 at 14.09 s, 0.833 at 14.33 s (limit below 9.81 N at 14.34 s) and 0.92 at 14.58 s.
- **Heavier loads.** A 5 kg Weight (49.1 N) hangs from a cold link with 9.7 N to spare and is released at m > 0.17; a 7 kg Weight (68.7 N) exceeds the cold limit and is released in the first ticks it hangs.
- **After the break.** The Weight falls with its rope; the link stays where it was placed. Cooling changes nothing: m does not fall and the released connection is not remade until Reset. At full power the collar cools from 376 K once the Battery empties at 30 s (its 12.5 J/K over 0.08 W/K to the room and 12.8 W/K to a disc that itself cools at 0.5 W/K).
- **Other heat sources.** Any face that touches the collar, or a flame region box that covers its bounds (TH-01, TH-02), heats it by the same laws; because the rope leaves upward, a source below or beside the collar never meets the rope or the Weight. These are works-with uses; their rates are the sources' to state, and no row here depends on them.
- **Energy.** Heat in = 12.5 × ΔT + 250 × m + ambient loss; the falling Weight's energy is its own potential energy, not the collar's.
- **Tick order.** T8: the grant and the exchange happen in tick N's thermal step; m commits with the tick; the limit is compared with the rope tension of the next tick's substeps; the release is applied at a step boundary.
- **Run length these rows need: 240 s** (A3 holds for 240 s; A5 observes to 90 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances.** T12 (temperature 2 K; times 15%; stall guard the smaller of 3 × and + 60 s of the hand time); forces 2 N; m ± 0.08.

### Rows

Planned class `FusibleLinkTests`. Rig: as "Rig" above, with the Weight named in each row and the plate on a Battery as named.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the rig with a default 4 kg Weight and the plate on a default Battery, when Run, then the Weight is released and falls to the bench; it had not been released while m was below 0.25 | `AMeltingCollarReleasesTheLoad` | A3 (the same rig held at 324.9 K: no release) | none | m ± 0.08; stall guard 39 s (3 × the 12.9 s hand time) |
| A2 | Given A1's plate and Battery with the link instead standing on a Wall's top face beside the plinth (the same rope and Pulley layout relative to the link; a Wall declares no thermal node), when Run, then the collar stays within 2 K of 288 K and the Weight hangs for 30 s | `HeatThatDoesNotReachTheCollarKeepsTheLoad` | A1 | none | 2 K; exact: no release |
| A3 | Given the rig with a default 4 kg Weight and the plate on a Battery with `power_limit` 21.5 W and `capacity` 14 400 J, when 240 s pass, then the collar never exceeds 328 K, is within 2 K of 324.9 K from 160 s on, m stays 0 and the Weight hangs throughout | `SubthresholdHeatingRetainsSupport` | A1 (full power releases) | `power_limit` 30 W: released, about 98 s (stall guard 158 s) | 2 K; m exactly 0; exact: no release |
| A4 | Given a cold link standing on a Wall's top face (no heat source), then a 5 kg Weight (49.1 N) hangs for 10 s and a 7 kg Weight (68.7 N) is released | `TheColdLinkHoldsUpToItsLimit` | each other | the two masses, 9.7 N under and 9.9 N over the 58.8 N limit (a margin chosen above the rope's settling transient, not a precision bound) | exact: release or none |
| A5 | Given A1, then for the 60 s after the Battery is empty (30 s to 90 s) the Weight's rope stays released, m stays 1 and the collar's temperature at 90 s is below its temperature at 30 s | `CoolingDoesNotReattach` | none | none | direction only; exact for the release and m |
| A6 | Given A1 with a 1 kg Weight, then it is still hanging when m is 0.75 and released by m = 0.92 | `ALighterLoadNeedsMoreMelting` | A1 | none | m ± 0.08 |
| A7 | Reset restores m = 0, 288 K and the rope connection; a second Run repeats A1 | `ResetRestoresTheLinkAndItsRope` | none | none | as A1 |
| A8 | Save and load keep the pose and the rope; no run state | `SaveAndLoadKeepTheRope` | none | none | exact |
| A9 | An electrical or activation link to the tie is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |

Each control can fail: A3 fails on a plate granted more than its source's power limit, a melt row that moves m below 330 K, or a limit below 58.8 N at m = 0; A2 fails if heat reaches a collar that touches no node; A1 fails if the limit does not follow m or the release is not one-way. No parameters, so no parameter-refusal row. The requirement's clauses are A3 (subthreshold) and A5 (no reattachment).

## 6. Open questions

Resolved:

1. **Category.** Structure (TH-04 section 6 item 5; `ElementCategory` has no Ropes value).
2. **Strength curve.** Linear in the melted fraction, full below 330 K (decided now: one declared map; a softening curve below the melt point is a second curve no lesson needs).
3. **Residue.** Melted wax stays on the link; nothing drips (D19).
4. **Shape of the connector.** One body, one rope tie and a load limit on it (decided now: a breakable fixed joint between two bodies needs a fixed joint kind and a second body for no gain, and the rope-tie release is the capability a commanded cut already has).
5. **Refreezing.** The melt row is one-way within a Run (decided now: it makes "cooling does not reattach" true of the material as well as of the break).
6. **Game scale.** D5, TH-04 section 11.
7. **A static mount heated where it stands (decided 23 November 2026 under the owner's standing delegation, by game intent).** The earlier part hung in a rope line between two ropes, and its collar had to sit inside a Candle's region box: the solid rope and the Weight would then have hung through the flame and the wax column, and any offset that cleared them left the collar no share; nothing could hold it at 325 K for the sub-threshold row. The lesson is "heat the link and the load drops; warm it short of the melt and it holds". So the link is now a static mount, like the Rope anchor: the player sets its collar on a heat source, and the rope leaves upward over a Pulley to a Weight hanging beside. A dynamic link cannot rest on a hot face under load (the rope's 39 N would lift a 0.22 kg body off it), and only static fixtures conduct reliably by touch (T3). The Heating plate is the rows' source because its grant is set by its Battery's `power_limit`: full power melts the collar in about 13 s, and 21.5 W holds it at 324.9 K, a real steady state 5.1 K below the melt. The second tie, the upper rope and the dynamic body are removed, and with them the question of a rope hung from a tie on a dynamic body.

Needs the owner: none. The collar-on-disc pair is in T3's list of static-on-static pairs (TH-04 section 11).

## 7. Integration

- **Works with.** Heating plate and Battery (heat, power limit); Pulley, Weight, Wall (delivered); TH-08 bar end or a flame region (other heat paths, no row). The rope is a solid strand under owner decision 1; its collider is ENGINE-CONNECTION-MATERIALS' to deliver.
- **Campaign.** Introduction 85, practice 86; reuse 96, 149. A level teaching the sub-threshold hold declares a Run length of at least 240 s and a Battery `power_limit` near 21.5 W. A link fixed in a level needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS for the touch pair); ENGINE-CONNECTION-MATERIALS (solid strand, owner decision 1); ENGINE-ASSEMBLY-JOINTS (which ENGINE-FRACTURE follows in the roadmap); ENGINE-FRACTURE as amended (F4, F5, F6); ENGINE-THERMAL (after ENGINE-GAS-NETWORK and ENGINE-LIQUID, owner decision 18); ENGINE-RUN-LENGTH; TH-04 (the heat source of the rows); S7.
- **Files.** `elements/FusibleLink/FusibleLink.csproj`, `FusibleLinkElementDeclaration.cs`, `fusible_link.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/FusibleLinkTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if ENGINE-FRACTURE shipped a rope-tie limit with a scalar-scaled value, tested; otherwise engine.
- **Disqualifiers.** Load limits only on structural attachments; a limit that cannot read a thermal observation; a supply grant to the plate that ignores the source's `power_limit` (then A3 needs another steady source).

## 9. Engine conformance (22 November 2026; redesign 23 November 2026)

1. **Declaration data only.** Two colliders on one static body, one rope tie, one load limit, one node with a melt row, two bindings. No temperature switch, no handler.
2. **Generic laws.** The tensile law; a load limit compared with committed tension; conduction by touch and the melt row; the plate's ordinary supply grant. "Lets go when hot" is a limit declared as a function of a committed fraction falling below the load.
3. **Three layers.** Colliders: collar and shackle. Visuals: their art; a printed gap and band. Animation: gap width and band colour from committed state.
4. **Flow and typing.** One Rope tie with an enum role; f32.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static; two primitive colliders; one node, one row, one limit comparison per tick, and one static touch pair found once at Run (T3).
7. **Tests are scenarios.** A1 to A8.

**Verdict: Conforms; blocked on ENGINE-FRACTURE and ENGINE-THERMAL, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `fusible_link` |
| Named identity | TH-16, `../invest/named-elements.md#thermal-16` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-450` and the ledger entry has no todo link |
| Decision | S580; `scope-corrections.md` on published main has no `s580` block |
| Mode records | `Configuration=Fixed` |
