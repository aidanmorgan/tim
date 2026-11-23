# TH-31 · Flint striker — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-31 delivery slice](../invest/named-elements.md#thermal-31). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`); repaired the same day against the Batch N audit (`docs/planning/elements/readiness/audits/elements-batch-n.md`, findings A4 and N2). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here (lumped estimates for the strike transients) and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal rules are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); what burns follows the [combustion conventions](TH-01-candle.md#combustion-conventions). Neither is restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-31 · Flint striker · Thermal |
| Anchors | [requirements.md#thermal-31](../requirements.md#thermal-31) (task `sequence-task-465`); [named-elements.md#thermal-31](../invest/named-elements.md#thermal-31) |
| Owner key / research | S593 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Lights [TH-32 Tinder pad](TH-32-tinder-pad.md) and the [TH-01 Candle](TH-01-candle.md) wick. Pushed by a rolling ball ([CAT-001](CAT-001-ball.md), delivered). Its hinge follows the delivered [CAT-034 Impact lever](CAT-034-impact_lever.md). [TH-33](TH-33-spring-mounted-match.md) is the stored-energy igniter. No CAT refines it. |
| Campaign | Intro 58; practice 59; reuse 85, 99 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES for the region query, and on ENGINE-THERMAL. Its rows also need TH-32 (and TH-01 for S-5). |

### Disposition

**To deliver as a single element.** It has no fuel of its own, so it needs ENGINE-THERMAL only; its rows need one fuel part (TH-32). Planned package id `flint_striker`, directory `elements/FlintStriker/`, display name "Flint striker".

## 2. Declaration

Follows the delivered Impact lever shape: a static support carrying one body on a limited revolute hinge with a friction torque (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:91-105`).

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the thermal contract of ENGINE-THERMAL. `KinematicMode.Static` (the support); footprint 2 bodies, 1 moving, 0 prismatic, 1 revolute.
- **Body 0, the gantry (static; boxes, metres, centre / half extents).** The ball's track is the plane z = 0; the gantry stands behind it.

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_foot` | (0, 0.03, −0.50) | (0.20, 0.03, 0.20) | foot |
  | `col_post` | (0, 0.48, −0.50) | (0.06, 0.42, 0.06) | post |
  | `col_bar` | (0, 0.86, −0.25) | (0.06, 0.04, 0.31) | bar reaching over the track; carries the hinge |
  | `col_shelf` | (0, 0.92, −0.25) | (0.20, 0.02, 0.20) | shelf on the bar, top at 0.94 m; the target stands here |

  A Basketball on the track (radius 0.34 m, `elements/Ball/BallElementDeclaration.cs:29`) passes under the bar (0.82 m) and clear of the post (0.44 m from the track plane) and of the foot (the ball's surface is 0.18 m up at that offset).
- **Body 1, the arm (dynamic).** Box, half extents (0.03, 0.25, 0.05), hanging from the hinge with its centre 0.25 m below it; 0.2 kg; moment about the hinge 0.2 × 0.5² ÷ 3 = 0.0167 kg·m². Its tip hangs at 0.34 m, a Basketball's centre height.
- **Hinge 0.** At (0, 0.84, 0), axis Z; limits 0 to 90°; `RevoluteTravel.Limited`; stiffness 0; damping 0; friction torque 3 N·m; initial angle 0 (hanging); the arm and gantry do not collide.
- **Material (f32).** Friction 0.6, restitution 0.1, rolling resistance 0 on every collider.
- **Thermal node.** `spark`, on body 1: 0.004 J/K, ambient 0.005 W/K, no face.
- **Frictional source** (T4). The work hinge 0's friction row dissipates, summed over all four substeps of the tick, is credited to `spark`, share 1.0. Nothing else heats it.
- **Region link** (T3; audit A4). A box 0.40 × 0.70 × 0.40 m on the shelf top, centred at (0, 1.29, −0.25) on body 0 (from 0.94 m to 1.64 m), axis +Y, fed by `spark`, G_r 0.2 W/K. A face collider's share is the area of overlap of its bounds with the box projected on the XZ plane ÷ 0.16 m², granted nearest-first along +Y, the sum capped at 1. The box is 0.70 m tall so that a Candle standing on the shelf has its dish (0.66 to 0.68 m above its base) inside it.
- **Parameters, ports, stores.** None.
- **Presentation bindings.** `Visual/Arm` follows body 1; `Visual/Spark` emission ← committed temperature of `spark`, 288 K to 800 K mapped 0 to 1.
- **Palette.** Category `ElementCategory.Power` (TH-04 section 6, item 5: a heat source). Order: 322 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Foot`, `Visual/Post`, `Visual/Bar` cream `#fff8e9` | boxes as the colliders | solid, `col_foot`, `col_post`, `col_bar` | nothing |
| `Visual/Shelf` gold `#f7cb52` | box 0.40 × 0.04 × 0.40 m | solid, `col_shelf` | nothing |
| `Visual/Arm` cream, its lower 0.1 m printed gold (the striking face) | box 0.06 × 0.50 × 0.10 m | solid, the arm's collider | body pose |
| `Visual/Wheel` navy `#293954` | a notched disc printed flush on the bar end at the hinge | decorative: printed flat | nothing |
| `Visual/Spark` gold | a small star quad over the shelf | decorative: glow, brief because the node cools in a fraction of a second | presentation binding |

Icon: a gantry with a hanging arm and a star. At a glance it differs from the Impact lever (a level beam on a central pivot) by the single hanging arm under a shelf.

### Compiled counts

Bodies 2 (1 static, 1 dynamic); colliders 5 (boxes); revolute joints 1; drives 0; sensors 0; thermal nodes 1; frictional sources 1; region links 1; ports 0; presentation bindings 1 plus one body pose. Five boxes: the gantry must clear the ball's track and carry a shelf, and each drawn member is solid.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1 | Colliders from art, visible = solid | present | as TH-04 | as TH-04 |
| S1 | A limited revolute hinge whose friction torque resists motion and holds a bar below it | present | `RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` and `.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` (`CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:59`, `:77`; bodies read) |
| S2 | A ball driving a hinged body of a package through its travel | present, untested for a rolling push-through: the delivered tests strike or load a beam (`CuriousContraptions.tests/ImpactLeverTests.cs:73`, `:380`, not opened) | Impact lever package | none for this motion |
| S3 | The work a joint's friction row dissipates over a tick's four substeps, credited to a thermal node | narrower: the friction impulse is committed per tick (`RevoluteHingeFrictionTests.ACommittedFrictionImpulseIsAdmittedUpToTheFrictionBoundAndRefusedBeyondIt`, `:135`, body read); no work or heat is derived from it | — | ENGINE-THERMAL (T4, T8) |
| TH-04 R9, R11 to R13 | Thermal node, ambient exchange, temperature driver, units | **missing** | none | ENGINE-THERMAL |
| TH-01 C5 | Region link | **missing** | none | ENGINE-THERMAL |

Counts: present 2, untested 1 (S2), narrower 1 (S3), missing 5. Every missing or narrower row is ENGINE-THERMAL's. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-31](../requirements.md#thermal-31): mechanical contact supplies a bounded ignition-energy deposit through the same thermal and reaction interface as other sources; no target lookup or guaranteed flame; no variants. Visual style: "Cream hinge mount with gold striking face and a restrained brief spark cue".
- **Named entry** [thermal-31](../invest/named-elements.md#thermal-31): owner S593; "A miss, insufficient work or a nonreactive target produces no sustained combustion".
- **Research** [TH-S03](../../thermal-component-research.md#th-s03) and per-element table: "impact trigger → finite ignition energy"; spark ← occurrence.
- **Element map** row TH-31: ChemicalReaction, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, Ignition, RigidBodyDynamics, SensibleHeat, SlidingFriction, TopologyTransaction (D14).
- **Processes.** [IX-02](../requirements.md#interaction-02), [IX-29](../requirements.md#interaction-29), [IX-01](../requirements.md#interaction-01).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for flint, striker, spark and ignition terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-31](../requirements.md#thermal-31) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact pushes the arm; the hinge's friction row resists with 3 N·m; the work it removes, 3 N·m × the angle swept, summed over every substep, becomes heat in `spark` (T4, T8); the region link carries it to whatever face stands on the shelf (T3). All of the friction work is accounted: it enters `spark` and leaves to the target and to ambient.
- **The push.** A Basketball (1 kg, `elements/Ball/BallElementDeclaration.cs:50-52`) released 1.0 m up a Ramp arrives with 1 × 9.81 × 1.0 = 9.81 J, rolling at √(10 × 9.81 × 1.0 ÷ 7) = 3.74 m/s. It pushes the tip until the tip is above the ball's top: 0.84 − 0.5·cos θ > 0.68 gives θ > 71° (1.239 rad). Friction work 3 × 1.239 = 3.72 J; lifting the arm 0.2 × 9.81 × 0.25 × (1 − cos 71°) = 0.33 J. The ball pays 4.05 J of its 9.81 J and rolls on. The tip travels 0.5 × 1.239 = 0.62 m in about 0.25 s.
- **The arm stays up.** Gravity's torque is at most 0.2 × 9.81 × 0.25 = 0.49 N·m, below 3 N·m: one strike per Run.
- **Tinder on the shelf.** The wafer's bounds (0.30 × 0.30 m) cover 0.09 ÷ 0.16 = 0.5625 of the box: 0.1125 W/K. Nothing else in the box has a face (shelf, bar and tray declare none). `spark` and the pad's fibre together hold 0.004 + 0.00425 = 0.00825 J/K and lose 0.005 + 0.02 = 0.025 W/K; they equalise in (0.004 × 0.00425 ÷ 0.00825) ÷ 0.1125 = 0.018 s, so treat them as one. With 3.72 ÷ 0.25 = 14.9 W for 0.25 s the rise is (14.9 ÷ 0.025) × (1 − e^(−0.25 × 0.025 ÷ 0.00825)) = 596 × 0.531 = 317 K: about 605 K against the 450 K threshold (162 K needed). The fibre lags the spark by a few tens of kelvin, so it passes 450 K at about 0.13 s, during the strike.
- **A candle wick in the box** (audit A4). A Candle stands on the shelf: its 0.40 × 0.40 m holder fits the 0.40 × 0.40 m shelf, the holder and column declare no thermal face ([TH-01](TH-01-candle.md)), and its dish (bounds 0.20 × 0.20 m) is inside the box at 1.60 to 1.62 m, so it takes 0.04 ÷ 0.16 = 0.25: 0.05 W/K. The unlit wick loses 0.0197 W/K (TH-01 section 5). Lumped: 0.00825 J/K losing 0.005 + 0.0197 = 0.0247 W/K, rise (14.9 ÷ 0.0247) × (1 − e^(−0.25 × 0.0247 ÷ 0.00825)) = 603 × 0.527 = 318 K by 0.25 s. The wick takes about 0.00425 ÷ 0.00825 × 14.9 = 7.7 W through the 0.05 W/K link, so the spark leads it by about 7.7 ÷ 0.05 = 153 K, and the wick sits about 0.004 ÷ 0.00825 × 153 = 74 K below the pair's mean: about 288 + 318 − 74 = 532 K at 0.25 s, passing 500 K at about 0.2 s. The lumped estimate puts the lighting threshold near 3.0 J of friction work. Once lit, the wick's own 8 W share holds it above 688 K with the flame at ambient (TH-01), so the struck wick never falls back below 450 K. The candle cannot be lit from beside the shelf: a box extended 0.20 m past any shelf edge would put the dish either above the shelf (no room), over the ball's track (|z| below 0.34 m and under 0.68 m, where the column would block the ball) or against the post and foot.
- **Insufficient work.** A release from 0.1 m brings 0.98 J: the arm stops near 0.32 rad, 0.96 J of friction work in about 0.3 s. Rise (3.2 ÷ 0.025) × (1 − e^(−0.909)) = 76 K: 364 K, no ignition. The threshold for tinder lies near 2 J of friction work (a release from about 0.2 m).
- **A slow push.** The same angle swept over 3 s gives 1.24 W and at most 1.24 ÷ 0.025 = 50 K: no ignition. No speed rule is declared; slowness fails because the heat leaks.
- **A nonreactive target.** A Wood block ([TH-03](TH-03-combustible-block.md)) on the shelf covers the whole box (0.2 W/K) and its 0.2125 J/K surface takes nearly all of the 3.72 J: at most 3.72 ÷ 0.2125 = 17.5 K. It warms and nothing burns.
- **Heat coming back.** A target that then burns in the box (a lit pad or candle) heats `spark` back through the same link, so the spark glow stays lit while it burns. That is the same law in reverse and is cosmetic.
- **Why 3 N·m.** It gives tinder a margin of about 1.8 in friction work (2 J needed, 3.72 J delivered) and the wick 1.24 (3.0 J), from a ball that keeps more than half its energy.
- **Tick order.** The friction work of all four substeps of tick N is credited in the thermal step of tick N (T8).
- **Run length.** Every row fits in the default 30 s.
- **Tolerances.** Friction heat 10% of the work (0.37 J; the tinder margin is 1.8); temperature 2 K or 5% of the rise; angles 5°; stall guard 10 s (T12: three times a hand time under 1 s, raised to the 10 s minimum).

### Rows

Planned class `FlintStrikerTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| S-1 | Given a Tinder pad on the shelf and a Basketball released 1.0 m up a Ramp on the track, then the arm passes 60° and the pad is burning within 10 s | `AFullStrikeLightsTinderOnTheShelf` | the same ball on a track 1 m to the side: the arm stays at 0°, `spark` at 288 K, the pad unlit (the miss) | none | stall guard 10 s |
| S-2 | Given S-1 with the ball released from 0.1 m, then the arm stops below 30° and the pad peaks below 420 K, unlit | `InsufficientWorkLightsNothing` | S-1 | 0.1 m against 1.0 m | as stated |
| S-3 | Given S-1, then the heat credited to `spark` equals 3 N·m × the angle the arm swept, summed over every substep, and the ball lost at least that much kinetic energy | `FrictionWorkBecomesHeatAndNothingElse` | the miss: zero heat | none | 10% |
| S-4 | Given a Wood block (TH-03) on the shelf and S-1's ball, then its surface rises under 20 K and nothing burns | `ANonreactiveOrHighThresholdTargetOnlyWarms` | S-1 | none | as stated |
| S-5 | Given a Candle standing on the shelf (its dish inside the box) and S-1's ball, then the candle is lit within 10 s and its wick stays above 450 K from then on | `AFullStrikeLightsACandleWick` (needs TH-01) | the candle on the bench beside the gantry stays unlit | none | stall guard 10 s |
| S-6 | Given S-1 then Reset, then the arm hangs at 0°, `spark` is at 288 K and the pad is unlit and full | `ResetRestoresTheArmAndEveryTarget` | none | none | 1°; exact store |
| S-7 | Given a placed striker, when saved and loaded, then its pose is kept and a fresh Run starts with the arm hanging | `SaveAndLoadKeepThePose` | none | none | exact |

No ports and no parameters, so no wrong-kind or parameter rows. The requirement's outcome: a miss is S-1's control, insufficient work is S-2, a nonreactive target is S-4.

## 6. Open questions

Resolved:

1. **Game scale.** D5; stated in TH-04.
2. **Deposit law.** All of the friction work becomes heat in one node of the striker; none is discarded and none is a fixed "energy per strike" (decided now: the requirement asks for a bounded deposit from mechanical work, and a fixed amount would be created energy).
3. **Spark region.** A fixed box on the shelf (D19): emitted particles would add bodies for no lesson.
4. **Sustained push.** Accepted: the ball must push the arm through its arc; a tap transfers too little. The stored-energy igniter is TH-33.
5. **Sprung flint plate.** Removed. The resistance is the hinge's declared friction torque, a delivered and tested row, instead of a second sprung body whose normal load would have needed its own proof.
6. **Minimum strike speed.** Removed: it was a rule in code. A slow push fails by heat loss.
7. **Where the candle stands** (audit A4, decided now, disagreeing with the audit's 0.20 m extension past the shelf's front edge): that extension lies over the ball's track, where a candle's column would block the ball; the box is made 0.70 m tall instead, so a Candle standing on the shelf has its dish inside it.

Needs the owner: none.

## 7. Integration

- **Works with.** Basketball and Ramp (delivered); TH-32 (planned before it); TH-01, TH-03 (later rows).
- **Campaign.** Introduction 58, practice 59; reuse 85, 99. Lesson fixtures need story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element, after TH-32.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the region query in particular); ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL as amended (frictional source summed over substeps, region link) in the order of owner decision 18; TH-32 (which brings ENGINE-IGNITION-BLAST); TH-01 for S-5; S7.
- **Files.** `elements/FlintStriker/FlintStriker.csproj`, `FlintStrikerElementDeclaration.cs`, `flint_striker.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/FlintStrikerTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if S2 and S3 are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers.** The slice's probe finds that a rolling ball does not carry the arm past 60° (re-derive the arm length and torque); friction heat credited per body pair with no declared share, or from one substep only; a region box that cannot sit on a different body of the package from its feeding node.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Five colliders, one hinge with a friction torque, one node, one frictional source, one region link, one binding. No strike detector, no rearm state, no target lookup.
2. **Generic laws.** Contact; hinge friction; "dissipated friction work is heat"; Q̇ = G·ΔT by T9's exchange. "Strike lights tinder" is the consequence of 3.7 J reaching a 0.00425 J/K node.
3. **Three layers.** Colliders: four gantry boxes and the arm. Visuals: the same solids, a printed wheel, a spark glow. Animation: the glow from the committed node temperature.
4. **Flow and typing.** No ports; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** One light dynamic body on a hinge; it is held by friction after the strike and could sleep once ENGINE-ISLANDS-SLEEP exists (no island sleeping exists today). One bounded region query per tick (T10). The arm's tip moves at up to 3.7 m/s, 3.7 ÷ 480 = 0.008 m per substep, inside the engine's general speculative margin of 0.004 m + relative speed × substep (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:1117`).
7. **Tests are scenarios.** S-1 to S-7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `flint_striker` |
| Named identity | TH-31, `../invest/named-elements.md#thermal-31` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-465` and the ledger entry has no todo link |
| Decision | S593; `scope-corrections.md` on published main has no `s593` block |
| Mode records | `Configuration=Fixed` |
