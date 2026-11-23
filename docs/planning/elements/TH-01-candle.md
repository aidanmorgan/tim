# TH-01 · Candle — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-01 delivery slice](../invest/named-elements.md#thermal-01). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`); repaired the same day against the Batch N audit (`docs/planning/elements/readiness/audits/elements-batch-n.md`, findings A1, A3, A4, A5), and on 23 November 2026 against `docs/planning/elements/readiness/audits/reaudit-group-3.md` (A4's half-covered boundary). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here (steady values from the steady-state balance, transients from lumped estimates) and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal rules are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); light follows the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) (1 to 12). Neither is restated. This spec leads the combustion specs and owns the [combustion conventions](#combustion-conventions) (section 11).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-01 · Candle · Thermal |
| Anchors | [requirements.md#thermal-01](../requirements.md#thermal-01) (task `sequence-task-435`); [named-elements.md#thermal-01](../invest/named-elements.md#thermal-01) |
| Owner key / research | S598 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Historical record [todo-196](../requirements.md#todo-196) is shared with [EL-155](EL-155-rope-operated-light.md), [EL-210](EL-210-flashlight.md), [TH-03](TH-03-combustible-block.md) and [TH-06](TH-06-converging-lens.md). Lit by [TH-31](TH-31-flint-striker.md) or [TH-33](TH-33-spring-mounted-match.md); heats [TH-17](TH-17-kettle.md), [TH-37](TH-37-heat-sensitive-target.md), [TH-03](TH-03-combustible-block.md). No CAT refines it. |
| Campaign | Intro 56; practice 57; reuse 83, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), and on ENGINE-THERMAL, ENGINE-IGNITION-BLAST, ENGINE-OPTICAL, ENGINE-PARAMETER-BOUND-FRAMES and ENGINE-RUN-LENGTH. |

### Disposition

**To deliver as a single element; the first consumer of ENGINE-IGNITION-BLAST's burn.** Planned package id `candle`, directory `elements/Candle/`, display name "Candle". Not a duplicate of anything delivered.

## 2. Declaration

Follows the delivered Battery shape for a static part that owns a finite store (`elements/Battery/BatteryElementDeclaration.cs:8`, `:36-47`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), the thermal-node contract of ENGINE-THERMAL, the fuel and ignition contract of ENGINE-IGNITION-BLAST and the emitter contract of ENGINE-OPTICAL. `KinematicMode.Static`; `PayloadPolicy.None`; zero static mass frame.
- **Body.** One static body; origin at the centre of the holder's underside.
- **Colliders (metres).**

  | Collider | Shape | Centre | Size | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_holder` | box | (0, 0.03, 0) | half extents (0.20, 0.03, 0.20) | holder plate; no thermal face |
  | `col_wax` | cylinder, axis Y | (0, 0.36, 0) | radius 0.15, height 0.60 | wax column; no thermal face |
  | `col_dish` | cylinder, axis Y | (0, 0.67, 0) | radius 0.10, height 0.02 | wick dish, 0.02 m proud of the wax top (top at 0.68 m); face of node `wick` |

- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0 on all three (the Domino's friction, `elements/Domino/DominoElementDeclaration.cs:36`).
- **Thermal nodes** (T2).
  - `wick`: 0.00425 J/K (10 mg of fibre × 425, T11); initial temperature 288 K, or 888 K when `lit` is "Lit"; ambient 0.015 W/K (the air and the wax column it stands in); face `col_dish` 16 W/K; ignite row at 500 K with extinction at 450 K (F2).
  - `flame`: 0.05 J/K; initial temperature 288 K, or 1088 K when `lit` is "Lit"; ambient 0.085 W/K; internal link to `wick` 0.005 W/K; no face. 0.05 J/K gives the flame a time constant of 0.05 ÷ 0.09 = 0.56 s, so it reaches its working temperature within a second of lighting.
  - The two "Lit" values are the lit steady state of section 5, so an authored-lit candle starts in balance (audit A1).
- **Region link** (T3). A box 0.14 × 0.40 × 0.14 m centred at (0, 0.89, 0), from 0.01 m above the dish top (0.69 m) to 1.09 m, so the dish itself is outside it; axis +Y; fed by `flame`; G_r 2.0 W/K. A face collider's share is the area of overlap of its bounds with the box projected on the XZ plane ÷ 0.0196 m², granted nearest-first along +Y, the sum capped at 1.
- **Fuel store.** `wax_fuel` on `wick`: capacity and initial value `fuel` × 41 016 J/kg (default 12 305 J); burned at 80 W while lit; oxidiser demand 3.4/1024 kg of O₂ per kg (6.5 mg/s). Burn credit (F1): 3 W to the optical emitter, 8 W to `wick`, 69 W to `flame`.
- **Optical emitter** (optical convention 2, paid from a declared store). Frame body 0; origin (0, 0.75, 0); half-angle 180° (all directions); range 2 m; radiant (1, 1, 1) W, paid by the burn (it emits only while the store pays); internal occluders: this part's colliders.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `fuel` | Real | `Kilogram` | 0.3 | 0.05 | 0.5 | Clamp; build-only (D11) |
  | `lit` | Choice | none | 0 | 0 | 1 | options "Unlit", "Lit"; build-only. "Lit" starts the Run burning with the initial temperatures above |

- **Ports.** None. Heat leaves by the region link and contact; light leaves through the emitter.
- **Presentation bindings.** `Visual/Flame` local scale and emission ← committed burn power (0 to 80 W mapped 0 to 1); `Visual/Mark_0` to `Mark_3` albedo navy → cream as the committed fuel fraction falls past 0.75, 0.5, 0.25 and 0.
- **Palette.** Category `ElementCategory.Power` (TH-04 section 6, item 5: a heat source). Order: 320 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Holder` gold `#f7cb52` | box 0.4 × 0.06 × 0.4 m | solid, `col_holder` | nothing |
| `Visual/Wax` cream `#fff8e9` | cylinder radius 0.15, height 0.60 m | solid, `col_wax` | nothing |
| `Visual/Dish` gold `#f7cb52` | cylinder radius 0.10, height 0.02 m, with the wick printed in its centre | solid, `col_dish` | nothing |
| `Visual/Mark_0` to `Mark_3` navy `#293954` | four rings printed flush on the wax | decorative: printed flat, recoloured by a binding | presentation binding |
| `Visual/Flame` gold | a small teardrop above the dish | decorative: glow, scaled by a binding; it is light, not a solid | presentation binding |

The wax column does not shorten: the four rings show the fuel, so the collider and the art always agree. Icon: a column on a square plate with a teardrop. At a glance it differs from the Battery by the round column and teardrop, and from the Fire bowl (a low square tray) by its height.

### Compiled counts

Bodies 1 (static); colliders 3 (1 box, 2 cylinders); joints 0; drives 0; sensors 0; thermal nodes 2; internal links 1; region links 1; fuel stores 1; ignite rows 1; optical emitters 1; ports 0; presentation bindings 6. Three colliders against the Battery's one: the dish is the only ignitable face, and the holder and column must not conduct.

## 3. Engine capabilities

### Capability probe

Shared rows are [TH-04 R1 to R14](TH-04-electrical-heating-plate.md#3-engine-capabilities). Rows C1 to C7 are the combustion probe; the other combustion specs cite them.

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R6 | Static colliders from art; parameter bounds | present | as TH-04 | as TH-04 (bodies read there) |
| TH-04 R9 to R13 | Thermal nodes, contact conduction, ambient exchange, drivers, units | **missing** | none | ENGINE-THERMAL |
| C1 | A finite store whose committed fraction drives presentation | narrower: the driver reads a mechanical energy store (`PresentationDriver.CommittedStoredFraction`, `CuriousContraptions.Abstractions/ElementPresentation.cs:33`) | — | not opened; owner ENGINE-IGNITION-BLAST |
| C2 | An ignition threshold on a node, a finite fuel store burned at a declared rate whose credit is split between declared nodes by declared shares (F1), an authored lit start, extinction below a declared temperature or without fuel | **missing** | none: a search of the engine projects for ignite, fuel and burn finds nothing | ENGINE-IGNITION-BLAST ([Story ENGINE-IGNITION-BLAST](../../../_bmad-output/planning-artifacts/epics.md#story-engine-ignition-blast-ignition-burn-and-blast-impulse)) |
| C3 | Oxidiser read from the scene atmosphere or an enclosing finite gas store | **missing** | `AtmospherePreset` exists (`CuriousContraptions.Abstractions/AtmosphereBoundary.cs:6`); no burn reads it | ENGINE-IGNITION-BLAST ("the declared oxidiser") |
| C4 | The declared outcome for a solid whose fuel is spent | **missing** | none | ENGINE-IGNITION-BLAST (addition already proposed by Batch I) |
| C5 | A region link from a node to face colliders inside a box, with T3's axis, projected share and cap, found through the spatial query interface | **missing** | none | ENGINE-THERMAL (T3, T10; candidates from ENGINE-SPATIAL-QUERIES) |
| C6 | An optical emitter paid from a declared store, not a supply consumer | **missing** | none in code; optical convention 2 now names a store-paid emitter | ENGINE-OPTICAL |
| C7 | Initial node temperatures selected by a Choice parameter (the `lit` start) | **missing** | none | ENGINE-PARAMETER-BOUND-FRAMES (coordinator decision 16: choice-bound values) |

Counts: present 2, untested 0, narrower 1 (C1), missing 11 (R9 to R13, C2 to C7). Owners: ENGINE-THERMAL (R9 to R13, C5), ENGINE-IGNITION-BLAST (C1 to C4), ENGINE-OPTICAL (C6), ENGINE-PARAMETER-BOUND-FRAMES (C7); each row has one. The stories' texts do not yet name an extinction temperature, a split burn credit, the region link or a store-paid emitter (proposed text in the [Batch N summary](readiness/batch-n.md)). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-01](../requirements.md#thermal-01): finite wax inventory; shared combustion; accounted heat and light; heat and oxygen conditions; no variants. Visual style: "Cream wax cylinder with a recessed wick and restrained, state-driven flame", to be reworded to the proud dish this spec declares (owner decision 24, section 6).
- **Named entry** [thermal-01](../invest/named-elements.md#thermal-01): owner S598; "An unlit or exhausted candle produces no sustained output".
- **Research** [TH-S03](../../thermal-component-research.md#th-s03) and the per-element table: "finite fuel source + reaction state + convective link"; flame scale and wax height bindings.
- **Element map** row TH-01: ChemicalReaction, Extinction, FiniteLedger, GeometryQuery, Ignition, SensibleHeat, TopologyTransaction. Navigation only (D14).
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30), [IX-19](../requirements.md#interaction-19); [TX-01](../requirements.md#thermal-scenario-01), [TX-12](../requirements.md#thermal-scenario-12), [TX-14](../requirements.md#thermal-scenario-14).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for candle, combustion, ignition, flame and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-01](../requirements.md#thermal-01) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

Write x = T_flame − 288 K and y = T_wick − 288 K.

- **Laws.** Burn (F1, F2): while lit, the fuel store pays 80 W, split 3 W light, 8 W `wick`, 69 W `flame`. Exchange (T3, with T9's exact two-node step): the flame's ambient term, its region link and its link to the wick; the wick's ambient term and its face.
- **Burn time.** 12 305 ÷ 80 = 153.8 s at the default; 0.05 kg gives 2051 ÷ 80 = 25.6 s; 0.5 kg gives 256 s.
- **Lit steady state, nothing held over it** (audit A3, recomputed). Wick: 8 + 0.005·(x − y) = 0.015·y, so y = 400 + 0.25·x. Flame: 69 = 0.085·x + 0.005·(x − y) = 0.08875·x − 2, so x = 71 ÷ 0.08875 = 800. Free flame 1088 K, wick 888 K. These are the "Lit" initial temperatures.
- **A lit candle stays lit** (audit A1). With the flame at ambient (x = 0, the worst any load can do), the wick's own 8 W holds it at 288 + 8 ÷ 0.020 = 688 K, 238 K above its 450 K extinction temperature. So nothing held in the flame can put the wick out; only an empty store, a missing oxidiser or a face touching the dish (a contact link of 16 × 16 ÷ 32 = 8 W/K to a cold body) does. From ignition the wick starts at 500 K and gains 8 W against at most 0.020 × 212 = 4.2 W of loss, so it rises and never falls below its ignition temperature; from an authored-lit start it begins at its steady value.
- **Under a cold body covering the box** (G = 2.0 W/K, body at 288 K): 69 = 0.08875·x − 2 + 2.0·x, x = 71 ÷ 2.08875 = 34.0: flame 322 K, wick 288 + 400 + 8.5 = 697 K, into the body 2.0 × 34.0 = 68.0 W. Of the 77 W of heat, 68 W goes to the body.
- **Half the box covered** (G = 1.0 W/K): x = 71 ÷ 1.08875 = 65.2: flame 353 K, into the body 65.2 W. The share sets the conductance, not the heat: a half-covering body still takes 85% of it and the flame runs 31 K hotter.
- **Under a kettle at 373 K** (0.2 m across, so its bounds cover the whole 0.14 m box; G = 2.0 W/K): 69 + 2.0 × 85 = 2.08875·x − 2, x = 241 ÷ 2.08875 = 115.4: flame 403 K, into the kettle 2.0 × 30.4 = 60.8 W; into a 288 K kettle 68.0 W ([TH-17](TH-17-kettle.md) chain 2 takes these two values).
- **Held over it.** A body is held in the box on two Walls of the minimum thickness 0.12 m (`elements/Wall/WallElementDeclaration.cs:30`) laid flat 0.10 m apart over the dish, their bottoms 0.01 m clear of the dish top; the body rests across them with its underside 0.13 m above the dish, inside the box. Walls declare no thermal node, so they take no share and conduct nothing; a static part may be placed off the floor with nothing under it, as the locked Wall "shade" of puzzle `solar_shadow` is (`content/puzzles.json:8118`; position y 3 m, height 3 m). A body resting on the dish itself would couple the wick at 8 W/K and put it out.
- **Lighting it.** Unlit, the wick loses 0.015 W/K to ambient and 0.005 W/K to a flame node near ambient (0.005 × 0.085 ÷ 0.09 = 0.0047 W/K in series): 0.0197 W/K. It needs 0.00425 × 212 = 0.90 J to reach 500 K and lights only under more than 0.0197 × 212 = 4.2 W. A full Flint strike with the candle standing on the striker's shelf lights it at about 0.2 s ([TH-31](TH-31-flint-striker.md) row S-5). A Spring match mounted with its guide base 0.50 m above the candle's base, beside the column, so that its region box covers 0.95 of the dish's bounds (G = 0.06 × 0.95 = 0.057 W/K): with the burning tip, 0.057·(a − y) = 0.0197·y and 12 = 0.01·a + 0.0197·y, so a = 1.346·y and 12 = 0.0332·y, y = 362 K of rise: the wick would settle near 650 K and passes 500 K within a few hundredths of a second.
- **Light.** 3 W over 4π sr is 0.239 W/sr: 0.239 W/m² at 1 m. A default broadband detector (0.950 m², threshold 2.5 W) 1 m away reads 0.950 × 0.239 = 0.227 W (optical convention 8; one share of 0.227 W is far below the 3 W emitted, so no scaling applies) and stays dark, which is intended: candle light is accounted, not a trigger.
- **Tick order.** The burn and the exchanges run in the thermal step of tick N (T8). The optical solve of tick N runs after the last substep and before the thermal step (optical convention 4), so the emitter radiates in tick N what the burn paid in tick N − 1; readings act from tick N + 1.
- Run length these rows need: 45 s (ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s). Row A3 observes to 40 s; every other row fits in 30 s.
- **Tolerances** (T12): energy 2% or 5 J; temperature 2 K or 5% of the rise; times 15%; stall guard the smaller of 3 × the hand time and the hand time + 60 s, at least 10 s, never past the Run length. Rows on the 0.00425 J/K wick assert outcomes (lit, out, above extinction), not point temperatures: its committed value can sit a few percent of its rise from the continuous balance, depending on where T8 applies a tick's credit.

### Rows

Planned class `CandleTests` in `CuriousContraptions.tests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a candle with `lit` "Lit", when 10 s pass, then the fuel store has fallen by 800 J, the flame reads 1088 K and the candle is lit throughout | `ALitCandleBurnsItsWaxAtEightyWatts` | "Unlit": fuel unchanged, flame and wick at 288 K, burn power 0 | none | 16 J; ± 40 K (5% of the 800 K rise) |
| A2 | Given A1, then fuel debit = heat credited to the nodes and the ambient account + light emitted | `FuelDebitEqualsHeatPlusLight` | "Unlit": every term 0 | none | 16 J (2% of 800 J) |
| A3 | Given `fuel` 0.05 kg and "Lit", when 40 s pass, then the store is 0, the burn power is 0 and it does not relight | `AnExhaustedCandleProducesNoFurtherOutput` | default fuel is still burning at 40 s | `fuel` 0.05 (the minimum): out between 22 and 30 s (25.6 s ± 15%) | time 15%; stall guard 45 s (the Run length) |
| A4 | Given a lit candle and a Thermal storage block ([TH-26](TH-26-thermal-storage-block.md), 420 J/K at 288 K) held over it as section 5 states, covering the box, when 10 s pass, then the block has gained at least 85% of the 770 J of heat the burn credited (hand: 68.0 W × 10 s + 36 J released by the flame = 716 J) and the candle is still lit | `ABodyHeldInTheFlameTakesMostOfItsHeatAndTheCandleStaysLit` (a cross-part row owned by the TH-26 slice: the block is taught at 81, after the candle at 56) | the same block 0.5 m to the side gains under 5 J | the block moved along the Walls until one end lies over the candle's axis, so that its bounds cover half the box (the section 5 Walls long enough that the block's centre of mass stays over them; a 0.07 m shift would still cover the whole 0.14 m box), still gains at least 80% (hand 687 J) and the flame reads at least 20 K hotter (353 K against 322 K) | as stated; the share of the heat is the gameplay quantity, so the row asserts it and not a point value |
| A5 | Given the Vacuum atmosphere and "Lit", then nothing burns and the store is whole | `ACandleCannotBurnWithoutOxidiser` | Standard atmosphere (A1) | none | exact store value |
| A6 | Given A1 and a broadband detector 1 m away, then its committed reading is 0.227 W; unlit it reads 0 | `CandleLightIsPaidOutOfTheBurn` | "Unlit" | none | ± 2% power (coordinator decision 21's optical set) |
| A7 | Given A1 then Reset, then fuel, temperatures and the lit state are as authored; a second Run repeats A1 | `ResetRestoresWaxFlameAndState` | none | none | as A1 |
| A8 | Given `fuel` 0.2 and "Lit", when saved and loaded, then both values are kept and no run state (temperature, remaining fuel) is saved | `SaveAndLoadKeepFuelAndLitChoice` | none | none | exact parameter values |
| A9 | `fuel` 0.04 or 0.6, or `lit` 2, is not admitted | generic: `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` (TH-04 R6) | 0.05 and 0.5 admitted | the four bounds | typed refusal |

Ignition at the threshold (499 K against 500 K) is proven once on ENGINE-IGNITION-BLAST's fixture and, for this declaration, by `FlintStrikerTests.AFullStrikeLightsACandleWick` in the TH-31 slice, which also asserts that the struck wick stays above 450 K. There is no port, so no wrong-kind row. The enclosed-air control of TX-14 is a later row once an enclosure part exists (F3). The requirement's control, "an unlit or exhausted candle produces no sustained output", is A1's control and A3.

## 6. Open questions

Resolved:

1. **Game scale.** Adopted (D5), stated once in TH-04.
2. **Oxygen model.** A finite gas inventory, never a flag (D6): F3. The atmosphere preset gives the first testable control (A5).
3. **Map composition.** Navigation only (D14); the probe above is the authority.
4. **Light output.** Yes, as a real 3 W emitter, too weak to trip a default detector (decided now: accounted light without a second way to trigger receivers).
5. **Collider.** A cylinder; the engine has one now (TH-04 R1).
6. **Burn-down.** The column keeps its size and four printed rings show the fuel (decided now: a collider that shrank with fuel would be a runtime geometry change no lesson needs).
7. **Flame capacity and authored-lit temperatures** (audit A1). `flame` 0.05 J/K, and "Lit" starts at the lit steady state (decided now: the earlier 0.5 J/K flame and 288 K authored start left the wick below extinction on the first tick).
8. **The wick's own share of the burn** (decided now, disagreeing with the audit's flame-only credit): with all 77 W on the flame, a cold body covering the box holds the flame near 325 K and the wick at (0.02 × 325 + 0.015 × 288) ÷ 0.035 = 309 K, so a candle heating a cold kettle would go out. With 8 W on the wick it never falls below 688 K. The wick–flame link drops to 0.005 W/K so that an unlit flame node drains the wick by only 0.0047 W/K and a full Flint strike still lights it.
9. **No face on the wax column** (decided now: a region box beside or around the column, such as the Flint striker's, then grants its share to the dish, the only ignitable face; the column's conduction is folded into the wick's ambient term).

10. **Requirement visual clause** (owner decision 24, 23 November 2026: the four thermal rows with visual wording are reworded to the declared presentation forms). The row says "recessed wick"; this spec draws a proud gold dish 0.02 m above the wax, because the ignitable face must be a solid collider a flame box or a striker can reach (visible = solid). The clause is reworded to "Cream wax cylinder with a proud gold wick dish and restrained, state-driven flame"; the edit to `requirements.md` is the coordinator's ([Batch N summary](readiness/batch-n.md) section 7 item 7).

Needs the owner: none.

## 7. Integration

- **Works with.** TH-31, TH-33 (igniters); TH-17, TH-37, TH-03, TH-26 (receivers); Wall (delivered) as the support of held bodies; a light detector (A6). Only the Wall is delivered.
- **Campaign.** "A Burning Question": introduction 56, practice 57; reuse 83, 137. A level that fixes the candle in place needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element, first after ENGINE-IGNITION-BLAST's burn.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the region query and ENGINE-CONTACT-OBSERVATIONS for contact links in particular); ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL in the roadmap order of owner decision 18; ENGINE-IGNITION-BLAST as amended; ENGINE-PARAMETER-BOUND-FRAMES for the `lit` start (C7); ENGINE-OPTICAL for the emitter and A6; ENGINE-RUN-LENGTH for A3; TH-04 for A4; S7. Row A4 runs in the TH-26 slice.
- **Files.** `elements/Candle/Candle.csproj`, `CandleElementDeclaration.cs`, `candle.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/CandleTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present and tested after the engine stories; otherwise the engine tier.
- **Disqualifiers.** The burn cannot split its credit between declared nodes; no extinction temperature; an emitter that only a supply consumer can pay, or that cannot radiate in all directions (then the emitter is a 60° upward cone); a region share not computed by T3's projected-bounds rule (re-derive the 68 W); no Choice-selected initial temperature (then "Lit" cannot start in balance and A1 is re-derived from a transient).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three static colliders, two nodes, one internal link, one region link, one fuel store with an ignite row and a split credit, one emitter, two parameters, six bindings. No update loop, handler or engine branch on this part.
2. **Generic laws.** Threshold ignition and a store-paid burn; Q̇ = G·ΔT by T9's exchange; the emitter law. "Heats what is held over it" is a region link; "goes out" is an empty store; "stays lit" is the declared share on the wick.
3. **Three layers.** Colliders: holder, wax, dish. Visuals: the same three solids, four printed rings, a flame teardrop. Animation: flame scale and ring colour from committed burn power and fuel fraction; nothing is written back.
4. **Flow and typing.** No ports; f32 values; `lit` is a Choice; channels and atmosphere are enums.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static body; three primitives. Thermal work per tick: two nodes, one link, one region term per face collider inside a 0.14 × 0.40 × 0.14 m box, found by a bounded query (T10). The emitter's segments are bounded by the optical limits (optical conventions 10 and 11).
7. **Tests are scenarios.** A1 to A8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, ENGINE-IGNITION-BLAST, ENGINE-OPTICAL and ENGINE-PARAMETER-BOUND-FRAMES, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor.

| Field | Planned value |
| --- | --- |
| Package | `candle` |
| Named identity | TH-01, `../invest/named-elements.md#thermal-01` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-435` and the ledger entry has no todo link |
| Decision | S598; `scope-corrections.md` on published main has no `s598` block |
| Mode records | `Lit=Unlit`, `Lit=Lit` |

<a id="combustion-conventions"></a>
## 11. Combustion conventions (owned here; cited by TH-02, TH-03, TH-31, TH-32, TH-33)

Inputs to ENGINE-IGNITION-BLAST's Code Map; they add nothing to T1 to T14.

- **F1 Fuel store.** A finite store in joules (mass × the T11 fuel energy) on one node, the *ignite node*. While lit it pays a declared constant power, split by declared shares (as T4 splits frictional heat): radiant watts to a declared optical emitter, a share to the ignite node and the rest to a separate flame node where the part declares one; a thin fuel credits everything to its ignite node. A part with a flame node gives its ignite node a share large enough that, with the flame at ambient, the ignite node's own balance stays at least 60 K above its extinction temperature (the *floor*, column "Ignite node, flame at ambient" below). Then a lit part stays lit under any load held in its flame. Fuel never refills during a Run.
- **F2 Lit state.** Lit begins when the ignite node is at or above its T11 ignition temperature with fuel and oxidiser present, or at Run start when authored lit (the authored-lit part starts at its lit steady temperatures). It ends when the store is empty, the oxidiser is absent, or the ignite node falls below a declared extinction temperature (fibre 420 K, wax 450 K, wood 520 K). An ignite node with fuel left may light again.
- **F3 Oxidiser.** Present in the Standard atmosphere and absent in Vacuum. A burning body inside a declared enclosure draws its oxygen from that enclosure's finite gas store (wax 3.4/1024, wood and fibre 1.2/1024 kg of O₂ per kg of fuel) and goes out below an oxygen fraction of 0.15 (D6). Enclosure rows wait for ENGINE-GAS-NETWORK and an enclosure part.
- **F4 Flame values.** Steady values from each part's balance, shown in its spec; "covered" means a 288 K body whose bounds cover the whole region box.

  | Part | Burn | Light | Ignite-node share | Flame node (capacity, ambient, link to ignite node) | Region box (m) | G_r | Ignite node, flame at ambient | Free flame (ignite node) |
  | --- | --- | --- | --- | --- | --- | --- | --- | --- |
  | Candle | 80 W | 3 W | 8 W to `wick` | `flame` 0.05 J/K, 0.085 W/K, 0.005 W/K | 0.14 × 0.40 × 0.14, 0.01 m above the dish | 2.0 W/K | 688 K (extinction 450 K) | 1088 K (wick 888 K) |
  | Fire bowl | 240 W | 9 W | 6 W to `bed_surface` | `flame` 0.15 J/K, 0.25 W/K, 0.01 W/K | 0.40 × 0.50 × 0.40, inside the rims from 0.065 m above the bed | 4.0 W/K | 588 K (extinction 520 K) | 1182 K (bed surface 1035 K) |
  | Combustible block | 96 W | 0 | 8 W to `surface` | `flame` 0.05 J/K, 0.1 W/K, 0.01 W/K | 0.40 × 0.30 × 0.40, 0.01 m above the top face | 1.0 W/K | 608 K (extinction 520 K) | 1148 K (surface 952 K) |
  | Tinder pad | 16 W | 0 | all, to `fibre` (thin fuel) | none | 0.36 × 0.35 × 0.36 around the pad | 0.06 W/K | — | ember 1088 K; covered 488 K (extinction 420 K) |
  | Match | 12 W | 0 | all, to `tip` (thin fuel) | none | 0.20 × 0.20 × 0.20 beside the strike pad | 0.06 W/K | — | tip 1488 K; covered 459 K (extinction 420 K) |

  The two thin fuels' region conductances are set so that a covered ember stays above its own 420 K extinction (audit A2): 288 + 16 ÷ (0.02 + 0.06) = 488 K and 288 + 12 ÷ (0.01 + 0.06) = 459 K. A 0.00425 J/K ember's committed temperature can sit up to about 8% of its rise below these continuous values, depending on where T8 applies a tick's credit (the pad about 473 K, the match about 448 K), so rows on embers assert outcomes, not point temperatures. The links from an ignite node to its flame node are small because the flame node is a node near ambient while the part is unlit, and every watt it drains has to be supplied by the igniter.
- **F5 Spent solid.** A solid whose store is empty keeps its collider and its thermal nodes and cannot ignite again; Reset restores the store.
- **F6 Flames are art.** A flame, ember or spark is decorative art driven by committed burn power or temperature. It never blocks, pushes or is sensed.
