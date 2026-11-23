# Audit: element specs, Batch N (Thermal, TH-01 to TH-37)

Audited 22 November 2026 in checkout `spec-drafts` (published main `8d1d68aa` plus planning drafts). Read-only: nothing was built or run. All 37 specs and `readiness/batch-n.md` were read; at least one heat, time or energy row was recomputed by hand in each spec. Transient figures here are lumped hand estimates of the declared values, as the specs' own are. Line numbers are in the spec files under `docs/planning/elements/` unless a path is given.

## 1. Verdicts

Counts: Ready 0; Ready with fixes 23; Not ready 14. Conformance: Conforms 9; Conforms with fixes 28; Does not conform 0. The summary's "37 Conform, 0 with fixes" does not survive the audit. No thermal package exists and the engine projects hold no thermal code beyond the isobaric gas law, so there is no element-specific engine code to remove (search confirmed).

| Spec | Verdict | Conformance | Row recomputed | Main reason |
|---|---|---|---|---|
| TH-01 Candle | Not ready | With fixes | free flame 1111 K, not 1194 K; 984 K at 10 s | a lit candle goes out (A1); A1 value wrong (A3) |
| TH-02 Fire bowl | Not ready | With fixes | kettle in flame 500.4 K matches; B1 is 1023 K at 10 s | covered tinder goes out, B5 fails (A2) |
| TH-03 Combustible block | Ready with fixes | With fixes | strength chain matches; flame 1149 K, not 1248 K | tinder chain, held-over-flame fixtures (A2, A5) |
| TH-04 Heating plate | Ready with fixes | With fixes | 355.1 K at 10 s, 5.9 s to 330 K, 34.3 s to 450 K: all match | conventions T3, T8, T9 (section 2); Run length; Battery line cites |
| TH-05 Friction brake | Ready with fixes | With fixes | drum 296.4 K, shoe 300.7 K, 601 J: match | status wording; EL-055 file edit; visual clause |
| TH-06 Converging lens | Ready with fixes | With fixes | 420.4 W/sr, 85.5 W of 90 W delivered: match | converging segment against optical convention 3 (C4) |
| TH-07 Solar absorber | Ready with fixes | With fixes | 37.8 W, 28.0 K at 20 s: match | states the unrepaired receiver law (line 38) |
| TH-08 Heat bar | Not ready | With fixes | steady chain 394.1 K, 47.7 W: match | the bar never touches the disc (B1) |
| TH-09 Insulating panel | Ready with fixes | With fixes | target 334 K: match | dynamic-pair contacts; Run length |
| TH-10 Finned sink | Ready with fixes | With fixes | 307.7 / 311.1 K; fanned 291.6 K: match | airflow sample inside its own fin |
| TH-11 Heat exchanger | Not ready | With fixes | 317.0 K, 13.0 kJ, 397 W: match, streams balance | no warm-water vessel exists (C3) |
| TH-12 Heat pump | Not ready | With fixes | freezing chain redone at 5.5 W/K (section 3) | sink cannot touch face B; COP law undefined at start |
| TH-13 Freezing mold | Not ready | With fixes | cold-pack chain redone (section 3) | A3 and A4 values wrong |
| TH-14 Ice block | Ready with fixes | With fixes | 55.8 s total against 55.5 s: match | mass bound to phase fraction has no story text; meltwater spills |
| TH-15 Ice plug | Not ready | With fixes | melting starts at 2.7 s, not 4.0 s; f 0.14, not 0.52 | A1 to A3 wrong |
| TH-16 Fusible link | Not ready | Conforms | 675 J, 29.5 s at 25 W: match | A4 needs Weights heavier than the 8 kg maximum |
| TH-17 Kettle | Not ready | With fixes | 373 K at 53.0 s, 282 J: arithmetic matches | boil row and gas law disagree by R·T per kilogram (E1) |
| TH-18 Condenser | Not ready | With fixes | wall alone 41 s, 14.8 g: arithmetic matches | no face for a sink; inherits E1; chain redone (section 3) |
| TH-19 Steam piston | Ready with fixes | With fixes | 20 kPa full stroke, 89 J: match | blocked-exhaust band ignores the hose volume |
| TH-20 Steam turbine | Not ready | With fixes | 0.276 g/s, 23.5 W: arithmetic matches | inflow 196 W exceeds the kettle's supply (E1) |
| TH-21 Temperature sensor | Ready with fixes | Conforms | 9.8 s assert: match | Run length; contact rating |
| TH-22 Bimetal thermostat | Ready with fixes | With fixes | closes at 4.7 s, 563 J: match | proud stalk without collider; "bend" binding does not exist |
| TH-23 Expansion rod | Ready with fixes | Conforms | 18.9 s, 175 N, 0.32 J: match | Run length; 400 N limit never reached |
| TH-24 Gas bladder | Ready with fixes | With fixes | 340 K, 0.036 m, 641 = 458 + 182 J: match | static neck on static disc |
| TH-25 Hot-air balloon | Ready with fixes | With fixes | lift-off at 3.95 K rise, 43.6 s: match | second buoyancy law; cannot lift any delivered cargo |
| TH-26 Storage block | Ready with fixes | Conforms | 378.2 K and 301.6 K at 10 s: match | Run length; family binding delta |
| TH-27 Phase-change cartridge | Ready with fixes | Conforms | plateau 11.8 s, discharge 100 s: match | Run length 115 s |
| TH-28 Evaporative pad | Ready with fixes | With fixes | 5.64 K depth, 8.7 g in 30 s: match | two owners for the airflow sample; Vacuum energy |
| TH-29 Cold pack | Ready with fixes | Conforms | C5 260.0 K: match; C2 omits about 1 kJ of room gain | C2 expected values |
| TH-30 Thermoelectric generator | Not ready | With fixes | 2.65 W steady: match; block fade wrong | output is 1 to 3% of any delivered load (section 3) |
| TH-31 Flint striker | Ready with fixes | Conforms | 3.72 J, tinder to 605 K: match | S-5 cannot be placed (A4) |
| TH-32 Tinder pad | Not ready | With fixes | focus 16.4 W: match; covered ember 361 K | ember below its own 420 K extinction (A2) |
| TH-33 Spring match | Ready with fixes | Conforms | 4.5 J stored, 6.5 m/s: match | bowl chain fails (tip 397 K); coined story name |
| TH-34 Toaster ejector | Ready with fixes | Conforms | slice rise 23.8 K, 112 W total: match | A3 geometry; companion under-declared |
| TH-35 Coffee pot | Not ready | With fixes | 61.1 s to boil: match | inherits E1; status wording; not value-only |
| TH-36 Pedal lamp | Ready with fixes | With fixes | 23.7 W, 321.9 K: match | palette and owner disagree with repaired Batch K |
| TH-37 Heat target | Ready with fixes | With fixes | about 12 s to 330 K: match | dwell has two homes; Level output can only light a lamp |

## 2. The thermal conventions (TH-04 section 11)

The design is one data-driven network: nodes, one link law, paid sources, phase rows, a ledger. Nothing in T1 to T14 is a per-element routine, and flame nodes and region links are ordinary instances of it. The defects below are in the conventions themselves, so they recur in every spec that cites them.

**N1 (High). T9's stability statement is false, and the clamp does not make the step stable.** `TH-04:201` says the largest unclamped ratio G·dt ÷ C in the family is 0.017. The declared values say otherwise:

| Node | C (J/K) | Largest declared G (W/K) | ΣG ÷ (120·C) |
|---|---|---|---|
| TH-31 `spark` (smallest C in the batch) | 0.004 | region 0.2 | 0.43 |
| TH-32 `fibre` | 0.00425 | region 0.2; face 16 (12.8 against the metal disc) | 0.43 free; 25 on the Heating plate |
| TH-01 `wick` | 0.00425 | face 16 (8 against a body on the dish) | 15.7 in contact |
| TH-02 `bed_surface`, TH-03 `surface` | 0.2125 | face 16 | 0.31 to 0.50 in contact |
| TH-25 `pan` | 2 | face 64 (32), wetted 4 | 0.150 |
| TH-22 `strip` | 4.8 | face 64 | 0.056 |
| TH-08 `end_a` | 6 | face 64 (32), slab up to 5.33 | 0.052 |
| TH-17 `water`, TH-28 `reservoir`, TH-13 charge, TH-14 and TH-15 ice at fraction 0 | falls to 0 | wetted 64 or 16 | unbounded |

Largest conductance: 64 W/K (metal face, wetted link); 32 W/K as a metal pair; 31.9 W/K is the fanned ambient of TH-10. An explicit step needs ΣG·dt ≤ C per node; the batch exceeds that by a factor of 25 at a tinder fibre on the plate. T9's per-link clamp bounds one link only: a node with several links can still overshoot, and the result depends on link order. `TH-32:90` itself states 0.39.

Replacement for T9: "Each link moves, in compiled link order and from current temperatures, ΔQ = ΔT · C_r · (1 − e^(−G·dt ÷ C_r)), with C_r = C_a·C_b ÷ (C_a + C_b) (C_r = C for a link to ambient or to a held temperature). This is the exact two-node solution: one rule, conserving, monotone and stable for any G and C, so no declaration is refused for stiffness. A node, liquid store or phase mass whose capacity is zero is inactive and its links move nothing. A wetted link's conductance is its declared value × the store's fill fraction." Delete "The largest unclamped ratio …". TH-11 already scales by fill (`TH-11:39`); TH-17, TH-28 and TH-13 do not.

**N2 (High). T8 loses three quarters of friction heat and names the wrong optical phase.** `TH-04:200` reads "the contacts and friction work of this tick's last substep". Friction work is done in all four substeps; reading one breaks T4 and T13 for TH-05, TH-31 and TH-33. It also reads "absorbed light (Phase 2)", but repaired optical convention 4 (owner decision 16; `EL-176` section 10) runs the optical solve after the last substep. Replacement: "It reads this tick's supply grants, the friction and drive work summed over this tick's four substeps, the touching face pairs as of the last substep, and the absorbed light of this tick's optical solve, which runs after the last substep and before the thermal step." The same "Phase 2" wording is at `TH-01:112`, `TH-32:91`, `TH-06:99`, `TH-07:102`, `TH-36:111`.

**N3 (High). Conducting fixtures were made dynamic to obtain a contact; this is the wrong fix.** TH-08, 09, 10, 26, 27, 29, 37 are dynamic "so that a contact link exists" (`TH-08:27`, `:136`). Consequences found:
- Geometry fails: the bar rests on the plinth and clears the 0.01 m disc by 3 mm (B1); a sink cannot hang under a pump face (B2); a condenser has no top face for a pack.
- Performance (addendum point 6: static parts are static bodies): bars, panels and six-collider sinks become loose solver bodies in every thermal level.
- Sleep: T10 takes links "from the contacts the narrowphase produced". A sleeping island produces none, so a resting stack would stop conducting under ENGINE-ISLANDS-SLEEP.
- Static pairs still exist anyway and are unlisted in their probes: TH-22 base, TH-15 tab, TH-24 neck, TH-30 and TH-12 on the plate disc, TH-13 tray on a pump face.
- A dynamic body held exactly at a face (TH-23 rod at y = 0.21) has zero penetration and no guaranteed manifold.

General alternative, replacing T3's contact-link bullet: "A contact link exists between two face colliders while their shapes touch or overlap within one declared touch tolerance (ENGINE-THERMAL states it once). Pairs are found through ENGINE-SPATIAL-QUERIES: pairs of static colliders once at Run; pairs with a moving member are added on begin-touch and removed on end-touch (ENGINE-CONTACT-OBSERVATIONS, any body pair) and are held in a compiled-capacity link table that persists while the island sleeps. A link never depends on contact force. G = G_a·G_b ÷ (G_a + G_b)." With it, TH-08, TH-09 and TH-10 become static parts; stores, cartridge, target and kettle stay dynamic because they are cargo. Their specs' "Dynamic or static" decisions are rewritten accordingly.

**N4 (Medium). T3 region link is not computable as written.** "Overlapped cross-section ÷ the box's cross-section" names no axis, no rule for a sphere, cylinder or torus, and no normalisation: stacked faces under one box each take a share, so the sum can exceed G_r. Replacement: "The box declares an axis. A face collider's share is the area of overlap of its bounds with the box, projected on the plane normal to that axis, ÷ the box's area on that plane; shares are granted nearest-first along the axis and their sum is capped at 1."

**N5 (Medium). T3 "Radiation" disagrees with the optical conventions.** It credits "the optical power a collider with a declared absorptance absorbs". Under conventions 3 and 8 a diverging segment (every Flashlight, candle and lamp) delivers power only to receiver apertures, as conserved shares. Replacement: "Radiant heating is light: a receiver aperture's committed reading (optical convention 8, conserved shares) × its declared absorptance is credited to the node that owns the face." `TH-37:73` and summary gap 1 then agree. `TH-07:38` restates the pre-repair cap "min(P, A × Σ wᵢ·Eᵢ)"; replace with "its conserved share of each diverging segment (optical convention 8)".

**N6 (Medium). Owner decision 13 is applied nowhere.** "Run length" appears in none of the 37 specs and `ENGINE-RUN-LENGTH` in none. Rows needing more than the 30 s default (`CadenceValues.cs:77`), with the length each spec must state:

| Run length | Specs |
|---|---|
| 35 to 45 s | TH-04 (A3: 35.5 s; state 40 s), TH-07 (35 s), TH-34 (40 s), TH-01 (45 s) |
| 60 to 80 s | TH-09, TH-21, TH-26, TH-28 (60), TH-10 (70), TH-29 (80) |
| 95 to 130 s | TH-32 (95), TH-18 (130), TH-11 and TH-27 (115), TH-03, TH-08, TH-13, TH-14, TH-15, TH-16, TH-17, TH-20, TH-30, TH-35 (120), TH-12 (132) |
| 150 to 260 s | TH-22, TH-23 (150), TH-25 (170), TH-19 (120; A6 240), TH-24 (260) |

Add to each section 5: "Run length these rows need: N s (ENGINE-RUN-LENGTH, owner decision 13; default 30 s)" and the story to prerequisites. `TH-25` A6 (about 830 s) and the `TH-19` A6 stall guard (600 s) should become direction-only rows inside 240 s. TH-05, TH-06, TH-31, TH-33, TH-36 and TH-37 fit in 30 s.

**N7 (Low).** T10 is sound once N3's link table is adopted: work per tick is the compiled nodes, links, sources and phase rows plus the live face pairs and region overlaps. T1, T4, T6, T7, T12 and T13 are coherent. The 120 W plate is attainable: 1 J per tick from a default Battery (120 W limit, 3600 J). `TH-04:101` cites `BatteryElementDeclaration.cs:42` and `:40`; the values are at `:44` and `:42`. `TH-04:58` says "the Battery's one" collider; `:59` of that file declares `Colliders => []`.

## 3. The three unreconciled values

**TH-12 against TH-10's 5.5 W/K.** The sink path is 5.5 × 32 ÷ 37.5 = 4.69 W/K, not 4.3. Fixed point: Q_c = 81 W, T_c = 270.5 K, T_h = 310.4 K, COP 3.38. Cooling 2562 J ÷ 79 W = 32 s, freezing 1044 ÷ 71 = 15 s: frozen after about 47 s for 1.13 kJ. Replace at `TH-12:101`: "(5.5 W/K to the room behind a 32 W/K contact: 4.7 W/K overall) … Q_c = 81 W gives T_c = 270.5 K, T_h = 310.4 K, COP 3.4. Frozen after about 47 s, for 1.13 kJ"; at `TH-12:139`: "TH-10's 5.5 W/K". `TH-30:94` uses the same 4.3: with 4.69 the sinked case is 296.7 K, 49.3 W in, 3.69 W out.

**TH-30's 400 K block.** TH-26 admits 288 to 500 K with default 288 K (`TH-26:35`); a default block delivers nothing. State `initial_temperature` = 400 in TH-30 row A5 and at `:94`; T14 reads "Storage block 420 J/K; reference hot start 400 K (a level setting; default 288 K)"; delete "The block's 400 K start is TH-26's to confirm". The fade at `TH-30:94` ("0.13 K/s … over minutes") omits the block's own 3.2 W/K loss: it is 0.94 K/s, time constant 116 s; output 1.97 W at start, 1.2 W at 30 s, 0.25 W at 120 s, about 115 J electrical from 47 kJ stored. The law stays under Carnot always and the 16 W cap never binds. See owner item 6 for the load.

**Cold-pack chains.** TH-29: 437.5 J/K, 255 K, 1.2 W/K ambient, face 16 W/K. Alone it reaches 273 K at 288 s and gains 40 W from the room at the start. Against a 273 K load through 8 W/K its ceiling is 5.1 kJ, not 7.9 kJ.
- TH-13: the tray does not stop at 273 K (269.2 K at f = 1), so the mold gives up 3264 J, not 2841 J; f = 1 at about 25 s; the pack then reads 264 K having also taken 0.8 kJ from the room. A pack at 262 K or warmer never completes the freeze. Replace `TH-13:101` with: "On a default Cold pack: f = 1 at about 25 s; the pack has then taken about 3.3 kJ from the mold and 0.8 kJ from the room and reads about 264 K. A pack authored at 262 K or warmer cannot complete the freeze." A3: "heat received by the sink = 785 J + 1044 J × f + 67.5 J/K × (288 K − tray temperature) + the mold's ambient inflow; 2% or 5 J". A4: "Given a Cold pack with `initial_temperature` 263 K, at 60 s f is between 0.15 and 0.9; f never reaches 1 in 120 s."
- TH-18: replace `:98` with: "A Cold pack placed at Run start reaches 259.5 K by the time the kettle boils and 269 K when the kettle is dry; the wall runs about 4 K above it (264 K to 274 K). The pack takes 2.9 kJ from the condenser and 3.5 kJ from the room, 6.4 kJ of its 7.9 kJ below 273 K." The "held at 259 K" is wrong. Figures to be redone once E1 is decided.
- `TH-29:79` C2: "the 20 J between is the small ambient gain" is wrong (about 1 kJ); expected about 265 K and 280 K.

## 4. Findings by severity

### High

**E1. Boiling is not energy-coherent** (`TH-17:122`; `TH-20:31`; T11). The boil row moves m·(c·T_sat + L) = 531 220 J/kg into the head, which equals the vapour's internal energy at 376.8 K. Gas leaving a store carries Cp·T = 698 kJ/kg. The scaled latent heat (141 062 J/kg) is below R·T (172 140 J/kg), so each kilogram vented removes 167 kJ more than was supplied; a pure-steam head settles at 284 K and recondenses. Coherent rate on the plate: 50.8 W ÷ 307 900 J/kg = 0.165 g/s, not 0.36 g/s. Every steam number falls: TH-17 chains 1, 2, 3, 5, 6; `TH-18:95-97`; `TH-19:104`; `TH-20:107-111`. Decision needed (owner item 1).

**A1. A lit candle goes out** (`TH-01:37`, `:46`, `:108`). The burn credits only `flame` (0.5 J/K). An authored-lit wick starts at 288 K, below its 450 K extinction, and is out on the first tick; a struck wick falls below 450 K in 0.3 s while the flame is near 357 K. Replace `:37`: "`flame`: 0.05 J/K"; add to `:46`: "`lit` = Lit selects initial temperatures `wick` 758 K and `flame` 1111 K". Same for TH-02 (`flame` 0.15 J/K; authored `bed_surface` and `flame`) and TH-03 (`flame` 0.05 J/K).

**A2. A covered ember self-extinguishes** (`TH-32:31`; `TH-02:101`; `TH-03:87`; `TH-33:99`). Tinder under a 288 K body: 288 + 16 ÷ 0.22 = 361 K, below 420 K; on the bowl bed 389 K; the match beside the bed 397 K. `FireBowlTests.BurningTinderLightsTheBed` and TH-03's tinder chain fail. Replace tinder G_r 0.2 → 0.08 W/K (448 K, 12.8 W) and match G_r 0.1 → 0.06 W/K (459 K); re-derive the three chains.

**A3. Flame temperatures are wrong** (`TH-01:105`, `:121`; `TH-02:99`, `:114`; `TH-03:89`). The wick link is not "under 1%": effective loss 0.0936 W/K gives 1111 K; with 0.5 J/K the flame is 984 K at 10 s against A1's 1194 ± 45 K. After A1's change: TH-01 A1 "1111 K ± 41 K"; TH-02 B1 "1182 K ± 45 K"; TH-03 "1149 K … 950 K".

**B1. The bar never touches the disc** (`TH-08:37`, `:100`, rows A1, A4, A5). At 7.4° the underside drops 0.013 m between disc rim and plinth edge; the disc is 0.01 m proud. With N3 the bar is static and placed level: "the target stands on a 0.13 m Wall so the bar lies level at 0.21 m; `end_a` overlaps the disc by at least 0.10 m".

**B2. Pump and generator stacks cannot conduct** (`TH-12:27`, `:100`, A5, A6; `TH-30:94`, A5). TH-10's only face is its foot; a dynamic body under a static part has no contact. Needs N3. Until then TX-02 and TX-11 cannot be built.

**B3. The heat pump law is undefined at start-up** (`TH-12:38`). At T_h ≤ T_c the expression divides by zero or goes negative. Replace: "COP = 4 while T_h − T_c ≤ T_c ÷ 8, otherwise 0.5·T_c ÷ (T_h − T_c)". It is then always under Carnot and the three flows sum to zero.

**D1. TH-15 rows A1 to A3 are wrong** (`:93-96`, `:108-110`). The disc starts at 288 K holding 380 J: melting starts at 2.7 s; with `capacity` 960 J f = 0.37 at 8 s; an unsupplied disc still melts the plug (f = 0.14 at 30 s). A1: "no liquid passes in the first 2 s; fully open by 20 s". A2: "the plug stands on the bench; the plate 1 m away heats another body". A3: "`capacity` 600 J; at 10 s the opening is between 0.2 and 0.8".

**D2. TH-16 A4 cannot be built** (`:86`, `:106`): it needs 9.6 and 10.4 kg Weights; the Weight clamps at 8 kg. Replace the limit with 58.8 N × (1 − m); A4 "a 5 kg Weight hangs and a 7 kg Weight is released"; A1 "falls after m passes 0.33". Add ENGINE-CONNECTION-MATERIALS (solid strand, owner decision 1) to Status and prerequisites: every row hangs a rope. A1 names no heat source or time.

**C1. Two buoyancy laws** (`TH-25:68-70`). ρ_amb·V·g beside the delivered Balloon's declared lift acceleration (`elements/Balloon/BalloonElementDeclaration.cs:22`, `:69-71`) is a second rule for one behaviour (forward-only gate; owner decision 16: one gravity and buoyancy law). Add to gap 3: "The displaced-density law replaces the declared lift acceleration; the delivered Balloon is re-declared on it in the same slice with its lift unchanged within its tests' tolerances."

**C2. Thermal water is below the liquid family's resolution** (`TH-17:128`, `TH-18:102`, `TH-28:99`, `TH-13:121`). L17 (`EL-001:42`) has a 0.004 m³ volume tolerance and treats flow under 0.0005 m³/s as none. The kettle's charge is 0.00125 m³ and the condensate stream 2.25 × 10⁻⁵ m³/s. Replace "liquid volume per L17" with "liquid mass by the store's ledger, tolerance in grams"; ENGINE-LIQUID addition in section 6.

**C3. TH-18 has nowhere to put a sink** (`:32-33`): the foot's top lies under the channel. Add "`col_shelf`, box, centre (0, 0.05, 0.30), half extents (0.25, 0.05, 0.20), a face of `wall`, 64 W/K; art `Visual/Shelf`, solid"; colliders become 4.

**A4. `FlintStrikerTests.AFullStrikeLightsACandleWick` cannot be placed** (`TH-31:42`, `:115`; relied on at `TH-01:131`): the region box lies above the shelf collider and a Candle's dish is lower than the box. Extend the box 0.20 m past the shelf's front edge and re-derive the 0.196 coverage.

**A5. "Held over a flame" fixtures are unspecified** (`TH-03:102`, `:105`; `TH-01:124`). A body resting on the dish couples the wick at 8 W/K and snuffs it. State the support (two Walls holding the underside 0.05 m above the dish). TH-03 K4's "a block lit at t = 0" has no means; replace with "held over a lit Candle from t = 0".

### Medium

- **TH-30 load** (`:94`, A1, A5): see section 3 and owner item 6.
- **TH-37** (`:33`, `:73`, `:128`): dwell sits in the node's assert hold while owner decision 12 puts hold goals in `engine-goal-count-window-hold`. State "the level's goal is a plain activation goal; the hold is the signal node's". A Level output can only light a Signal lamp today (`PressurePlateTests.cs:698`), so "actuates a latch" and `TH-21:151` "the logic parts" need a named ENGINE-SIGNAL-NODES row.
- **TH-22** (`:58`, `:61`, `:48`): a 0.40 m proud stalk with a fixed foot is not "art moved by a binding". Draw it flush on a static `col_back` plate. The binding uses a "bend" property and a continuous prismatic-coordinate driver; neither exists (`ElementPresentation.cs:12-37`). Use `LocalRotationZ` and add a probe row "committed prismatic coordinate as a presentation driver: missing".
- **TH-14** (`:32`, `:34`, `:65`): mass and inertia following a phase fraction have no story text (section 6). The meltwater free end points into the support, so everything goes to `Spilled` and TX-07 cannot be shown (owner item 9).
- **TH-13** (`:78`, `:134`, `:149`) and `TH-14:66`, `:137`: the "row-activation rule ENGINE-FRACTURE delivers" is not in that story (`epics.md:4467-4475`). Give the activation interval to ENGINE-THERMAL only and delete ENGINE-FRACTURE from both specs' prerequisites. Poured water freezes with no collider because `col_ice` is sized from authored `water`: use fixed layer colliders with activation intervals, as TH-14 does.
- **TH-19** (`:106`): the stall compresses the rod chamber alone; with the hose's 4.0 × 10⁻⁴ m³ the band is "0.19 to 0.33 m".
- **TH-18 A2** (`:97`, `:111`) asserts a mixture transient as a point value. Replace with "between 9 and 18 g condensed, at least 2 g leaves `GasOut`, the wall ends above 355 K".
- **TH-06** (`:88`): state the focus as one law, "r(d) = max(w, R·|1 − d·(1/f − 1/u)|)", give the segment's remaining range, and cite convention 8 for several receivers in the spot. See C4 in section 5.
- **Stale optical convention numbers** after the Batch K repair (now 1 to 12): `TH-06:33`, `:59`, `:97`, `:123`, `:152`; `TH-07:24`, `:95`, `:156`; `TH-01:83`, `:167`; `TH-36:166`.
- **TH-36** (`:45`, `:73`, `:154`): Batch K moved EL-155 to Power (`readiness/batch-k.md:150`) and deleted the ENGINE-ASSEMBLY-JOINTS blocker (`:151`); gap 7 gives the same capability to ENGINE-SIGNAL-NODES. Use Power and one owner, ENGINE-SIGNAL-NODES.
- **TH-34** (`:121`): A3's Wall must be "0.70 m above the toaster's base, across the slot". Companion: see section 5.
- **TH-05** (`:16`, `:126`): status must read "Blocked on the engine programme (owner decision 3a, 22 November 2026), EL-055's slice and ENGINE-THERMAL"; add `EL-055-mechanical-brake.md` section 2 to the files edited.
- **TH-35** (`:16`, `:20`): status "Blocked as TH-17" must state the 3a wording; "declared values only" becomes "declared values, one additional collider and art".
- **TH-10** (`:37`): the airflow sample point lies inside `col_fin_2`. State "the sample excludes the owner's colliders" and the required Fan direction.
- **TH-26 / TH-29 family**: value-only in structure (one box, one node, one face, no ports); the binding sets differ (four marks against one scale and one opacity). State the delta or give both one pattern.
- **TH-25 A1** asserts a 0.02 m gap that the 52 J in the pan may not produce. Replace with "the pan–disc contact ends and the gap exceeds 0.005 m at least once".
- **Requirement visual clauses changed without a recorded decision**: `TH-07:58` (raised marks → flush), `TH-01:34` (recessed wick → proud dish), `TH-36:56` (lever → pedal), TH-05 (rotor and shoe colours).
- **Dynamic-pair prerequisites**: `TH-09:141`, `TH-26:130`, `TH-27:133`, `TH-29:121` rely on dynamic-against-dynamic face pairs; add ENGINE-CONTACT-OBSERVATIONS.

### Low

`TH-02:24`, `:37` cite `ReceiverElementDeclaration.cs:12-16` as "a static open tray of boxes"; that file has `Colliders => []`. `TH-28:66` cites Fan `:46` for 12 m/s (it is `:48`). `TH-31:36` Ball radius is `:29`. `TH-21:41` declares a 100 W contact under a 120 W plate. `TH-33:156` names ENGINE-TUNNEL-QUALIFICATION, which is neither in `epics.md` nor in decision 3a; cite the engine audit. Materials used but absent from T11: brass (`TH-36:41`), toast 700 J/(kg·K) (TH-34), wax latent 12 500 J/kg (`TH-16:31`). `TH-28:97` sends latent heat to ambient in Vacuum against "the ambient account stays zero". `TH-24` uses g = 9.8, the engine 9.81. `TH-23:112` never reaches its 400 N limit; travel is 0.03 to 0.044 m on a default Battery (consider 0.002 m/K). `TH-34`: an empty cycle heats `element` to about 600 K; record it as TH-04 records its hazard. "Refused or clamped" at `TH-07:119`, TH-08 A8, TH-09 A7: name the declared `ParameterEditorPolicy`.

## 5. Checks the brief named

- **Families.** Sensible stores (TH-26, TH-29): value-only apart from the binding delta above. Steam vessels (TH-17, TH-35): no capability, port, mode or parameter differs, but collider count and art do; a valid Family slice, not value-only.
- **TH-05 on `mechanical_brake`.** Legitimate: `delivery-batches.md:152` and the requirement row describe one brake with an added thermal proof, and adding nodes later is forward-only (one path). EL-055 is not delivered (no `elements/MechanicalBrake/`). The specs agree on drum, bearing friction, torque capacity and ports. Coverage for a second identity on one package is correctly sent to S7 (gap 10).
- **TH-34's `toast_slice`.** Conforms to owner decision 15: no ledger identity, two packages under TH-34, a palette part and not spawned. Missing for a build: inertia rule, rolling resistance, icon, distinction at a glance, its own Reset and save/load rows, and one row warming it on a Heating plate.
- **RAD-20 `ThermalCold`.** Refuted as a current conflict: `RAD-20-radioisotope-thermoelectric-generator.md:129` now says "no thermal wire and no thermal port". Delete contradiction 6 from the summary.
- **Gas port ids.** Confirmed: `EL-230:32` `ChamberPort`, `EL-231:32-33` `ExtendPort` and `RetractPort`; Batch H's contract uses `GasIn` and `GasOut`. Batch H owns the repair. Batch H's closed vent list (`EL-039:89`) must also admit "an Output a package declares `Vent`", which five thermal specs rely on.
- **Balloon gas.** Confirmed: R 2000, Cv 3000, reference 102 325 Pa at 300 K (`BalloonElementDeclaration.cs:65-68`). See C1.
- **C4. Converging lens against optical convention 3.** Confirmed. Gap 6's "terminal converging segment" adds a segment kind, and convention 7 says refraction is declared as its result. General alternative with no new kind: "A Relay inlet may be a receiver aperture: the conserved share it receives from diverging segments feeds its outlets." The lens is then a Relay with one collimated outlet along its axis, range = focal reach; power is conserved by conventions 7 and 8.
- **No warm-water vessel.** Confirmed (`EL-001:62` "No thermal node"). Owner item 3.
- **Pressure and water.** Pascals absolute at 101 325 Pa and 16 kg/m³ are used consistently. Gas meets liquid only through phase rows; `TH-18` should say so for owner decision 16.
- **Story names.** All 37 name owner decision 3a; TH-05 and TH-35 need the exact wording. Coined or misplaced: ENGINE-TUNNEL-QUALIFICATION (`TH-33:156`); `engine-scene-environment` (`TH-28:16`, `:71`, `:141`; Batch M's coinage, not in `epics.md`); "Story 14.0 (airflow probe)" (`TH-28:67`; Story 14.0 is the acoustic engine, `epics.md:4328`). `TH-11:26`, `:74` gives the fluid port contract to ENGINE-LIQUID; owner decision 16 gives it to ENGINE-GAS-NETWORK. TH-30's store-less source is placed in ENGINE-OPTICAL; it is a supply capability (ENGINE-SUPPLY-DEMAND).

## 6. Engine-story gaps and proposed text

The summary's gap 1 text is element-free apart from the examples "(pump; generator …)" and "(flames and jets)", which should be deleted. It is incomplete. Add to ENGINE-THERMAL *Missing*:

"The link step of T9 as amended (exact two-node exchange in compiled order; zero-capacity nodes inactive; wetted conductance scaled by fill). The tick slot: after the optical solve and before sensor evaluation, reading work summed over the tick's substeps. Contact links from shape touch within one declared tolerance, static pairs resolved at Run and moving pairs maintained on begin- and end-touch in a link table of declared capacity that persists through sleep. A region link with a declared axis and shares capped at 1. A body's mass frame bound to a committed phase fraction, recomputed from its active colliders at a tick boundary; at fraction 0 the body, node and faces are inactive, and a collider that becomes inactive wakes the islands it supported. A boil or condense row that moves the vapour's enthalpy as the gas law defines it. One forced-air sample per node that excludes the owner's colliders (sole owner; delete the Story 14.0 mention). A converter declaration whose law can exceed the Carnot fraction is refused at bundle time. Refusal at compile when a table capacity is exceeded."

Add acceptance: "a node of 0.004 J/K joined to a 64 W/K face neither overshoots nor oscillates"; "a stack that has gone to sleep keeps exchanging heat"; "two static faces placed touching exchange heat and the same faces 0.02 m apart do not"; "friction heat over a tick equals the friction work of all its substeps".

ENGINE-LIQUID, add: "A phase-row source or sink and a stream fed by one are not subject to the zero-flow threshold; the ledger resolves 10⁻⁶ m³." Delete "mouth ids for a second passage" from gap 4 (package-local ids, ENGINE-CAPABILITY-INSTANCES).

ENGINE-OPTICAL: replace gap 6's first sentence with C4's Relay text.

Plan gap: ENGINE-SIGNAL-NODES, -CONTACT-OBSERVATIONS, -SPATIAL-QUERIES, -ISLANDS-SLEEP, -SUPPLY-DEMAND, -CONNECTION-MATERIALS, -RUN-LENGTH and `engine-goal-count-window-hold` have no text in `epics.md`, so "the owning story's text covers it" cannot be checked for any row they own. Humidity is not a force field: place "a validated scene relative humidity" in ENGINE-THERMAL with the evaporation row.

## 7. Items for the owner

1. **Boiling energy (E1).** Recommended: the boil row moves m·Cp·T_sat, making the effective latent heat (Cp − c)·T_sat = 307 900 J/kg and removing T11's 141 062 J/kg for boil and condense; halve the kettle's default water to 0.01 kg so boil-dry fits a 14 400 J Battery. Alternative: scale steam R and Cv. Blocks TH-17, 18, 19, 20, 35.
2. **Run length maximum.** Thermal rows need 60 to 260 s. Recommended: ENGINE-RUN-LENGTH's maximum is at least 300 s. Blocks most of the batch's acceptance.
3. **Warm water.** Recommended: EL-001 declares a node, a wetted link scaled by fill and an authored liquid temperature (288 to 368 K). Blocks TH-11.
4. **Static conducting fixtures (N3).** Recommended: adopt the touch-based link; bar, panel and sink become static.
5. **Roadmap.** Levels 14 to 20 teach thermal parts, but ENGINE-THERMAL sits after ENGINE-LIQUID. Recommended: split a first slice (nodes, links, supply heat target, observation, drivers) with no liquid dependency for TH-04, TH-37, TH-21.
6. **TH-30 output.** 1.3 to 3.7 W against a Fan's 108 W and a Motor's 120 W default demand. Recommended: rows name a low load setting and assert delivered power and energy, not rotation; otherwise raise the converter's conductance.
7. **TH-25 cargo.** Best margin 0.375 N (38 g); no delivered part is that light. Recommended: accept "lifts itself only" and say so.
8. **Requirement visual wording** for TH-01, TH-05, TH-07, TH-36: reword the rows to the specs' solid forms.
9. **Ice meltwater.** Recommended: meltwater on a solid support is `Spilled`; add one row with the block on a narrow bar over an open store.

## 8. Summary table against the specs

- Conformance column: 28 specs need fixes (section 1); the count line "37 Conform" is wrong.
- Contradiction 6 (RAD-20) is stale. Contradiction 9 is resolved by section 3.
- TH-13 and TH-14 list ENGINE-FRACTURE for a rule that story does not contain.
- TH-36 lists ENGINE-ASSEMBLY-JOINTS, which Batch K withdrew.
- TH-28 lists Story 14.0 for an airflow sample that gap 1 gives to ENGINE-THERMAL.
- T14 in the summary and in TH-04 agree (kettle 14 + 20.9 = 34.9 J/K).
- "Needs the owner: none" is not tenable (section 7).

## 9. Not verified

- Test bodies were mostly checked for existence at the cited line, not read; exceptions are TH-04's R1 to R8 and two `RevoluteDriveTests` methods.
- Whether a rope hangs from a tie on a dynamic body, whether one consumer may hold two targets, whether a Choice index is a `MechanicalValue.Multiply` operand, and reverse flow through a directed gas port.
- Whether a few watts turn the delivered Fan or Motor; whether Fan flow is still 12 m/s at 1 m.
- The pipe-bore constant at `TH-11:94`; the match's wedge mechanics; TH-07's A7 chain.
- ENGINE-RUN-LENGTH's maximum (no text exists).
- The per-spec reading was done by four parallel readers under one brief; I re-checked the candle, tinder, kettle capacity, Battery, solver-phase, optical-convention, RAD-20 and story-name facts myself, and did not re-derive every chain they reported.
