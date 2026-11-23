# TH-03 · Combustible block — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-03 delivery slice](../invest/named-elements.md#thermal-03). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`); repaired the same day against the Batch N audit (`docs/planning/elements/readiness/audits/elements-batch-n.md`, findings A1, A2, A3, A5). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here (steady values from the steady-state balance, transients from lumped estimates) and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal rules are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); burning follows the [combustion conventions](TH-01-candle.md#combustion-conventions) (F1 to F6). Neither is restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-03 · Combustible block · Thermal |
| Anchors | [requirements.md#thermal-03](../requirements.md#thermal-03) (task `sequence-task-437`); [named-elements.md#thermal-03](../invest/named-elements.md#thermal-03) |
| Owner key / research | S596 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Historical record [todo-196](../requirements.md#todo-196) (shared with EL-155, EL-210, TH-01, TH-06). Box dynamics follow the delivered [CAT-023 Domino](CAT-023-domino.md). Noncombustible control: [TH-26 Thermal storage block](TH-26-thermal-storage-block.md). Lit by [TH-01](TH-01-candle.md), [TH-02](TH-02-fire-bowl.md) or [TH-32](TH-32-tinder-pad.md). [EL-074 Wood barrier](EL-074-wood-barrier.md) is the static combustible; this is the cargo. |
| Campaign | Intro 58; practice 59; reuse 85, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), and on ENGINE-THERMAL, ENGINE-IGNITION-BLAST, ENGINE-FRACTURE and ENGINE-RUN-LENGTH. |

### Disposition

**To deliver as a single element**, after the Candle. Planned package id `combustible_block`, directory `elements/CombustibleBlock/`, display name "Wood block". Not a duplicate: no delivered part burns, shrinks or weakens.

## 2. Declaration

Follows the delivered Domino shape (`elements/Domino/DominoElementDeclaration.cs:36`): one dynamic box, no ports.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) plus the thermal, fuel and load-limit contracts of the engine stories. `KinematicMode.Dynamic`; `PayloadPolicy.None`.
- **Body.** One dynamic body. Mass 0.4 kg when whole; principal moments 0.4 × (0.4² + 0.4²) ÷ 12 = 0.0107 kg·m² on each axis.
- **Collider.** `col_block`: box, half extents (0.20, 0.20, 0.20) m when whole; the face of node `surface`.
- **Bound to the fuel store.** With f the committed remaining fraction of `wood_fuel`: mass m = 0.04 + 0.36·f kg (10% stays as ash); every half extent = 0.20 × (m ÷ 0.4)^⅓ m; moments follow mass and size. At f = 0: 0.04 kg and 0.093 m. Changes apply at tick boundaries and keep the body's velocity.
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0 (the Domino's friction).
- **Thermal nodes** (T2; initial temperature 288 K for all three).
  - `bulk`: 170 J/K (0.4 kg × 425), ambient 2.4 W/K (0.96 m² × 2.5), no face.
  - `surface`: 0.2125 J/K (0.5 g of wood), ambient 0.005 W/K, internal link to `bulk` 0.01 W/K, face `col_block` 16 W/K, ignite row at 573 K, extinction 520 K.
  - `flame`: 0.05 J/K (time constant 0.05 ÷ 0.11 = 0.45 s), ambient 0.1 W/K, internal link to `surface` 0.01 W/K, no face.
- **Region link** (T3). A box 0.40 × 0.30 × 0.40 m in the block's frame centred at (0, 0.36, 0), from 0.01 m above the whole block's top face to 0.51 m, so the block's own face is never in it; axis +Y; fed by `flame`; G_r 1.0 W/K. A face collider's share is the area of overlap of its bounds with the box projected on the block's XZ plane ÷ 0.16 m², granted nearest-first along +Y, the sum capped at 1.
- **Fuel store.** `wood_fuel` on `surface`: 0.4 kg × 15 625 = 6250 J; 96 W while lit; oxidiser 1.2/1024 kg of O₂ per kg (6.1 g/s of wood, 7.2 mg/s of O₂). Burn credit (F1): 8 W to `surface`, 88 W to `flame`.
- **Load limit.** On the body: the measure is sustained normal contact load, held 0.25 s (the measure and hold Batch M adopts for a free body). Limit L = 196 N × f × s, with s = clamp((773 − T_bulk) ÷ 485, 0, 1). Break outcome: two declared fragments, each a box with half extents (0.5, 1, 1) × the block's current half extents and half its current mass, with no fuel and no node.
- **Parameters.** None (D13).
- **Ports.** None.
- **Presentation bindings.** `Visual/Block` albedo warm wood → navy-grey char as the committed fuel fraction falls; `Visual/Flame` scale and emission ← committed burn power.
- **Palette.** Category `ElementCategory.Motion`. Order: 144 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Block` warm wood `#c28f52`, a navy `#293954` flame symbol printed flush on each face | box 0.4 m, edges chamfered inside the collider | solid, `col_block` | body pose and the body's store-bound size |
| `Visual/Frag_A`, `Visual/Frag_B` | half blocks, hidden until the break | solid, the fragments' colliders | fragment body poses |
| `Visual/Flame` gold `#f7cb52` | two teardrops above the top face | decorative: glow | body pose; presentation binding |

The "readable consumed volume" of the requirement is the block itself getting smaller: art and collider shrink together because both follow the body. Icon: a cube with a flame mark. At a glance it differs from the Thermal storage block (a plain ceramic prism with graduation marks) by the printed flame symbol.

### Compiled counts

Bodies 3 (1 dynamic, 2 fragment bodies inactive until a break); colliders 3 (boxes); joints 0; sensors 0; thermal nodes 3; internal links 2; region links 1; fuel stores 1; ignite rows 1; load limits 1; ports 0; presentation bindings 3.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Owner or test |
| --- | --- | --- | --- | --- |
| TH-04 R1 | Dynamic box collider from art | present | as TH-04 | test named in TH-04 |
| TH-04 R9 to R13 | Thermal nodes, conduction, ambient, drivers, units | **missing** | none | ENGINE-THERMAL |
| TH-01 C1 | Store fraction as a presentation driver | narrower | as TH-01 | ENGINE-IGNITION-BLAST |
| TH-01 C2 to C5 | Ignition and burn with a split credit, oxidiser, spent-solid outcome, region link | **missing** | none | ENGINE-IGNITION-BLAST (C2 to C4), ENGINE-THERMAL (C5) |
| W1 | A body's mass and one uniform collider scale bound to a committed store | narrower: exists for a gas store and a sphere only (`IGasShapeElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:114`; `CuriousContraptions.Bundle/MassFrameDomain.cs:40`) | `GasRuntimeTests.FiniteOutflowChangesOneMassAndShapeWithoutThrustOrChangingDrySpinInertia` (`CuriousContraptions.tests/GasRuntimeTests.cs:151`, not opened) | ENGINE-IGNITION-BLAST (the spent-solid outcome, with the addition in the Batch N summary) |
| W2 | A load limit on a free body with declared fragments | **missing** | only contact-integrity observers exist (`IContactIntegrityElement`, `MechanicalContracts.cs:119`) | ENGINE-FRACTURE ([Story ENGINE-FRACTURE](../../../_bmad-output/planning-artifacts/epics.md#story-engine-fracture-load-limited-connections-and-irreversible-break); Batch M's addition) |
| W3 | A load limit that is a declared function of committed scalars (fuel fraction, node temperature), and fragment masses given as fractions of the parent's committed mass | **missing** | none | ENGINE-FRACTURE (addition in the Batch N summary) |

Counts: present 1, untested 0, narrower 2 (C1, W1), missing 11 (R9 to R13, C2 to C5, W2, W3). Each has one owner. ENGINE-FRACTURE depends on ENGINE-JOINT-KINDS and ENGINE-CONTACT-OBSERVATIONS (its *Depends on* line); only its player-made connections also wait for ENGINE-ASSEMBLY-JOINTS (engine-stories-index C9), and this part makes no player-made connection, so it needs nothing from that story. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-03](../requirements.md#thermal-03): transportable structural material with finite reactants, chemical energy and temperature-dependent strength; consumption updates mass and geometry generically; no variants. Visual style: "Warm-wood chamfered block, engraved fuel symbol and readable consumed volume".
- **Named entry** [thermal-03](../invest/named-elements.md#thermal-03): owner S596; "A noncombustible control absorbs heat but does not burn; spent fuel cannot reignite indefinitely".
- **Research** per-element table: "node with ignition threshold + fuel"; "ignites from a hot neighbour, burns and loses mass"; char fraction binding.
- **Element map** row TH-03: ChemicalReaction, EnvironmentState, Extinction, FiniteLedger, GeometryQuery, Ignition, JointConstraint, RigidBodyDynamics, SensibleHeat, StructuralFracture, TopologyTransaction (D14).
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30), [IX-37](../requirements.md#interaction-37), [IX-41](../requirements.md#interaction-41), [IX-42](../requirements.md#interaction-42); [TX-03](../requirements.md#thermal-scenario-03), [TX-12](../requirements.md#thermal-scenario-12).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for combustible, fuel, wood, burn and fracture terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-03](../requirements.md#thermal-03) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

Write x = T_flame − 288 K and s = T_surface − 288 K.

- **To ignite.** `surface` needs 0.2125 × 285 = 60.6 J. Unlit it loses 0.01 W/K to the bulk, 0.005 W/K to ambient and 0.01 × 0.1 ÷ 0.11 = 0.0091 W/K through its cold flame node: 0.0241 W/K.
- **Held over a heat source** (audit A5). The block rests on two Walls of the minimum thickness 0.12 m (`elements/Wall/WallElementDeclaration.cs:30`) laid flat 0.10 m apart, so even the fully burnt 0.186 m block still spans the gap and nothing rests on the source itself. Walls declare no thermal node, so they take no share and conduct nothing. A block laid directly on an ignitable face couples it at 16 × 16 ÷ 32 = 8 W/K: on a candle's dish it puts the candle out, on a burning Tinder pad it smothers the ember (the fibre falls to (0.00425 × 1088 + 0.2125 × 288) ÷ 0.21675 = 304 K).
- **Over a candle** (the Walls' bottoms 0.01 m clear of the dish, the block's underside 0.13 m above it; its bounds cover the candle's whole 0.14 m box, G = 2.0 W/K). With the candle's balance (TH-01 section 5) the candle flame is x_c = (71 + 2·s) ÷ 2.08875, so the surface gains 2.0·(x_c − s) = 67.98 − 0.085·s and keeps 67.98 − 0.1091·s: it heads for 911 K with a time constant of 0.2125 ÷ 0.1091 = 1.95 s and passes 573 K at 1.95 × ln(623 ÷ 338) = 1.2 s. The candle stays lit throughout (its wick never falls below 688 K, TH-01).
- **Over a burning Tinder pad** (Walls laid flat either side of the pad, the block's underside 0.06 m above the wafer and covering the pad's whole box, G = 0.06 W/K; [TH-32](TH-32-tinder-pad.md)). The fibre sits at 272 + 0.75·T_s, so the surface gains 16.32 − 0.015·T_s and keeps 16.32 − 0.015·T_s − 0.0241·(T_s − 288) = 23.26 − 0.0391·T_s: it heads for 595 K, 22 K above ignition, with a time constant of 0.2125 ÷ 0.0391 = 5.4 s, and passes 573 K at 5.4 × ln(307 ÷ 22) = 14.3 s, inside the pad's 48.8 s burn. The ember starts at 488 K, above its 420 K extinction (audit A2).
- **On the Heating plate.** The disc never passes 528 K ([TH-04](TH-04-electrical-heating-plate.md#5-acceptance)), below 573 K: a plate cannot light wood.
- **Burn.** 6250 ÷ 96 = 65.1 s. Lit steady state, nothing held over it (audit A3, recomputed): surface 8 + 0.01·(x − s) = 0.015·s, so s = 320 + 0.4·x; flame 88 = 0.1·x + 0.01·(x − s) = 0.106·x − 3.2, so x = 91.2 ÷ 0.106 = 860. Free flame 1148 K, surface 952 K.
- **A lit block stays lit** (audit A1). With the flame at ambient the surface's own 8 W holds it at 288 + 8 ÷ 0.025 = 608 K, 88 K above its 520 K extinction, so nothing held in its flame puts it out.
- **Shrinking.** At f = 0.5: m = 0.22 kg, half extent 0.20 × 0.55^⅓ = 0.164 m. At f = 0: 0.04 kg, 0.20 × 0.1^⅓ = 0.093 m.
- **Strength.** Cold and whole, L = 196 N: it carries the heaviest delivered Weight (8 kg, 78.5 N; the Weight's mass runs 0.25 to 8 kg, `elements/Weight/WeightElementDeclaration.cs:33`) with a margin of 2.5. Burning, the bulk gains about 0.01 × (1050 − 288) = 7.6 W from the surface (over a candle the surface runs near 1050 K) against its 2.4 W/K ambient: at most 3.2 K above ambient, with a time constant of 170 ÷ 2.41 = 70.5 s, so 3.2 × (1 − e^(−39 ÷ 70.5)) = 1.4 K after 39 s and s = (773 − 289.4) ÷ 485 = 0.997. The limit follows the fuel: L falls to 78.5 N at f = 78.5 ÷ (196 × 0.997) = 0.402, which is 0.598 × 65.1 = 39.0 s after ignition. At the break m = 0.04 + 0.36 × 0.402 = 0.185 kg.
- **Why 196 N.** A whole block must carry every delivered Weight; a half-burnt one must not carry the heaviest.
- **Tick order.** The burn, the mass and size binding and the limit are evaluated in the thermal step (T8) and applied at the tick boundary; the next tick's substeps use the new body.
- Run length these rows need: 90 s (ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s). Row K1 burns out 1.2 + 65.1 = 66.3 s after a lit candle starts, at most 76 s with the 15% time tolerance, and K3 then watches for 10 s; K4 needs 50 s; the others fit in 30 s.
- **Tolerances** (T12): energy 2% or 5 J; times 15%; lengths 0.005 m; mass 0.01 kg; stall guard the smaller of 3 × the hand time and the hand time + 60 s, at least 10 s, never past the Run length.

### Rows

Planned class `CombustibleBlockTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| K1 | Given a block resting on two Walls laid flat 0.10 m apart over a Candle authored lit, its underside 0.13 m above the dish, then it ignites within 10 s, burns 6250 J and goes out between 55 and 75 s after igniting, and ends at 0.04 kg with half extent 0.093 m | `AHeatedBlockBurnsAndLosesMassAndSize` | the same block on Walls 0.5 m to the side never ignites and keeps 0.4 kg | none | 125 J; time 15%; 0.005 m; 0.01 kg; stall guard 90 s (the Run length) |
| K2 | Given a block on a Heating plate wired to a Battery of 14 400 J, then it warms and never ignites | `ASurfaceBelowTheThresholdDoesNotBurn` | K1 | 528 K against 573 K | exact zero burn power |
| K3 | Given K1 after burn-out and the candle still lit under it, then the burn power stays 0 for 10 s | `SpentFuelCannotReignite` | a fresh block there ignites | none | exact |
| K4 | Given an 8 kg Weight on a cold block for 10 s, then nothing breaks; given the same on a block held over a lit Candle from t = 0 (K1's fixture), then it breaks between 34 and 46 s into two fragments whose masses sum to the block's mass at the break (0.185 kg) | `ABurningBlockLosesItsStrength` | the cold block | a 0.25 kg Weight (the lightest) never breaks a block before f = 0.0125 (the last 0.8 s) | time 15%; 0.01 kg |
| K5 | Given K1 in the Vacuum atmosphere, then nothing burns | `ABlockCannotBurnWithoutOxidiser` | K1 | none | exact |
| K6 | Given K1, then fuel debit = heat credited to nodes and the ambient account | `FuelDebitEqualsHeatReleased` | K2: zero | none | 125 J |
| K7 | Given K4 after the break then Reset, then one whole 0.4 kg block is back at its authored pose with a full store | `ResetRestoresMassSizeAndTheUnbrokenBody` | none | none | exact store; pose 0.001 m |
| K8 | Given a placed block, when saved and loaded, then its pose is kept and a fresh Run starts whole and unlit | `SaveAndLoadKeepThePose` | none | none | exact |

No ports and no parameters. The requirement's noncombustible control, "absorbs heat but does not burn", runs with TH-26 in its slice (`ThermalStorageBlockTests`), with K1's side control and K2 standing in until then. "Spent fuel cannot reignite indefinitely" is K3. Focused-light ignition (TX-03) is a later row with TH-06.

## 6. Open questions

Resolved:

1. **Game scale.** D5; stated in TH-04.
2. **Map row.** Navigation only (D14); temperature-dependent strength is row W3.
3. **Burn rate model.** A fixed 96 W while lit (F1).
4. **Fragments.** Two inert halves; unburnt fuel in them stays unburnt (decided now: nothing is created, mass is conserved, and burning fragments would need a store split no lesson uses).
5. **Oxygen model.** D6; F3.
6. **Heat capacity while shrinking.** `bulk` keeps 170 J/K (decided now: no row depends on it and a capacity bound to mass is one more binding).
7. **Flame values** (audit A1, A3, decided now): `flame` 0.05 J/K; 8 W of the burn on the surface, so a body held in the block's flame cannot put it out (with a flame-only credit a 288 K body covering the box holds the surface at 354 K, below its 520 K extinction); surface–flame link 0.01 W/K, so the cold flame node of an unlit block drains a pad-heated surface by only 0.0091 W/K and a burning pad still lights it. The free flame is then 1148 K, the audit's figure.
8. **How a block is held over a source** (audit A5): on two Walls laid flat 0.10 m apart; K4's "lit at t = 0" is "held over a lit Candle from t = 0".
9. **ENGINE-ASSEMBLY-JOINTS** is not a prerequisite (decided now: the block makes no player-made connection, and only ENGINE-FRACTURE's player-made connections wait for that story, engine-stories-index C9).

Needs the owner: none.

## 7. Integration

- **Works with.** TH-01, TH-02, TH-32 (igniters); Weight and Wall (delivered) for K1 and K4; TH-04 for K2; TH-26 (control); TH-06 later.
- **Campaign.** Introduction 58, practice 59; reuse 85, 149. Lesson fixtures need story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the region query, ENGINE-CONTACT-OBSERVATIONS for the sustained contact load in particular); ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL in the order of owner decision 18; ENGINE-FRACTURE as amended; ENGINE-IGNITION-BLAST as amended; ENGINE-RUN-LENGTH for K1, K3 and K4; TH-04; TH-01; S7.
- **Files.** `elements/CombustibleBlock/CombustibleBlock.csproj`, `CombustibleBlockElementDeclaration.cs`, `combustible_block.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/CombustibleBlockTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Engine tier unless W1 to W3 are present and tested when the slice starts.
- **Disqualifiers.** No store-bound mass and size (then the block keeps its size, chars by colour only, and the requirement's "mass and geometry" clause goes to the owner); a load limit that cannot depend on a committed scalar (then strength is a constant and K4 fails); fragments that must sum to the declared, not the current, mass; a burn that cannot split its credit.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic box with a store-bound mass and size, two inactive fragment boxes, three nodes, two internal links, one region link, one fuel store with a split credit, one load limit, three bindings.
2. **Generic laws.** Threshold ignition and store-paid burn; Q̇ = G·ΔT by T9's exchange; "mass and size follow a store"; "a sustained load above a declared limit breaks the body into declared fragments". Nothing reads "wood".
3. **Three layers.** Colliders: the block and two fragments. Visuals: their art, each following its body; a printed symbol; flames. Animation: char colour and flame from committed state.
4. **Flow and typing.** No ports; f32 values.
5. **Flat state.** The counts in section 2; the fragments are compiled at Run and inactive until the break.
6. **Performance.** One primitive collider in play at a time before the break. One bounded region query per tick (T10). The size binding changes one collider's bounds per tick while burning, which the broadphase handles as it does a moving body. A resting block could sleep once ENGINE-ISLANDS-SLEEP exists (no island sleeping exists today).
7. **Tests are scenarios.** K1 to K8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, ENGINE-IGNITION-BLAST and ENGINE-FRACTURE (general capabilities) and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `combustible_block` |
| Named identity | TH-03, `../invest/named-elements.md#thermal-03` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-437` and the ledger entry has no todo link |
| Decision | S596; `scope-corrections.md` on published main has no `s596` block |
| Mode records | `Configuration=Fixed` |
