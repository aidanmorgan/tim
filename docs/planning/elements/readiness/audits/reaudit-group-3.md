# Second audit, group 3: Batches M, N (thermal) and O

Audited 23 November 2026 in checkout `spec-drafts` (published main `8d1d68aa` plus planning drafts). Read-only: nothing was built, run, edited or committed. Line numbers are in `docs/planning/elements/` unless a path is given.

Method. The thermal chains (candle, fire bowl, tinder, kettle and coffee pot, condenser, turbine, cold pack into mold, heat pump, ice plug, hot-air balloon with cargo) were iterated at 1/120 s with T9's exact two-node step ΔQ = ΔT·C_r·(1 − e^(−G·dt/C_r)), in compiled link order from current temperatures, source credits before links, with every ambient term, the 1 J-per-tick plate grant from the Battery, and the game-scale latent heat L_v = (Cp_v − c)·T = 107.2·T J/kg (`TH-04:205`). Gas heads were modelled as well-mixed air plus steam stores with adiabatic venting and the declared throat laws. Batch M, Batch O with TH-30, and TH-08/TH-11/TH-16 were read by three parallel readers under this brief; I spot-checked their key code citations (`GasCouplingValues.cs:65`, `PhysicsDeclarationValues.cs:240`, `BundleValidator.cs:867` and `:310-312`, `GasSceneValidation.cs:78`), and all were confirmed.

## 1. Verdicts

Counts: **Ready 4; Ready with fixes 22; Not ready 1** (27 specs; TH-25's cargo companion is judged inside TH-25).

| Spec | Verdict | Most important open item |
|---|---|---|
| EL-113 Structural brace | Ready with fixes | B4 half-connected brace overlaps the right beam at Run start |
| EL-115 Linkage connector | Ready with fixes | L3 and L5 controls are invalid as placed |
| EL-123 Protected end-state goal | Ready with fixes | P2b rope shorter than the Balloon radius |
| EL-124 Authored gravity preset | Ready with fixes (blocking fix) | G9 compiles a Balloon load above the 16 m/s² per-load bound |
| EL-159 Tension-limited connector | **Ready** | only the cross-spec Pivot-shaft radius |
| GAP-04 Driven wheel | Ready with fixes | W1 cannot climb onto the Ramp's 0.18 m step |
| GAP-06 Granular dispenser | Ready with fixes | G4/G5 Wall blocks the blade; G8 Basketball cannot enter |
| GAP-07 Granular sieve | Ready with fixes (blocking fix) | Wide rods (r 0.035) below the 1/16 m cylinder minimum |
| TH-01 Candle | **Ready** | none |
| TH-02 Fire bowl | Ready with fixes | B2 pad has no igniter; emitter origin on a held body's underside |
| TH-08 Heat-conducting bar | **Ready** | none |
| TH-11 Heat exchanger | Ready with fixes (blocking fix) | passages stand half full; A4 fails against its own geometry |
| TH-12 Reversible heat pump | Ready with fixes | A6 stall guard breaks T12 |
| TH-13 Freezing mold | Ready with fixes | charge's ambient conductance unstated; A5 ball cannot enter |
| TH-15 Ice plug | Ready with fixes | rig's reservoir volume and placement unstated |
| TH-16 Fusible link | **Not ready** | A1 rig geometrically impossible; A3 has no mechanism |
| TH-17 Kettle | Ready with fixes | relief throat undeclared; condense rate undefined |
| TH-18 Condenser | Ready with fixes | passage has no gas paths declared |
| TH-20 Steam turbine | Ready with fixes | A2's Fan load left to the slice |
| TH-25 Hot-air balloon (+ `balloon_cargo`) | Ready with fixes | "one buoyancy law" contradicts owner decision 24, EL-124 and the epic text; A6 cargo removal has no mechanism |
| TH-30 Thermoelectric generator | Ready with fixes (blocking fixes) | A1, A2, A5 values fail tick by tick |
| TH-32 Tinder pad | Ready with fixes | P5 has no concrete Given |
| TH-35 Coffee-pot steam vessel | Ready with fixes | relief throat undeclared |
| EL-129 Sheet-thickness gauge | Ready with fixes | no electrical node (bundle refuses); "AND NOT" is no gate operation |
| EL-130 Tank-level gauge | Ready with fixes | L5 asserted value wrong when raised |
| RAD-20 RTG | Ready with fixes | two Minor wording fixes |
| RAD-21 Material latch | **Ready** | none |

"Blocking fix" means a row or declaration that would fail or be refused on a correct engine; each has an exact replacement below, so no design decision is left, which is why these stay at Ready with fixes rather than Not ready.

**Conformance verdicts (points 1–7).** A missing capability with exactly one owning story counts as Conforms.

- **Conforms with fixes:**
  - TH-25: point 2, a second buoyancy rule beside the declared lift until the section 5 item 1 decision.
  - EL-129 and EL-130: point 1, "AND NOT" is not a declarable gate operation; the Xor rewrite fixes it.
  - TH-30: point 3, a presentation driver the bundle refuses for its store.
  - GAP-07: point 1, Wide-rod colliders outside the admitted shape set; the box rewrite fixes it.
  - EL-124: point 5, G9's load does not compile under today's bound until the story replaces it.
- **Conforms:** the other 21. This includes TH-18's wall-limited condense row and TH-20's expander Carnot refusal, each a text gap with a single owner, ENGINE-THERMAL and ENGINE-GAS-NETWORK respectively.
- **Element-specific engine code:** none found, since no package of these identities exists.

## 2. The five most important findings

1. **TH-25's buoyancy law contradicts three current authorities** (`TH-25:39`, `:72`, `:148`, `:166`). TH-25 says displaced-density buoyancy ρ_amb·V·g_eff "replaces" the declared lift acceleration and that ENGINE-GAS-NETWORK re-declares the delivered Balloon on it "with its lift unchanged".
   - Owner decision 24 (23 November) keeps "buoyancy" for the three ball identities as the declared ambient lift.
   - The working-tree ENGINE-FIELD-FORCE text says "every declared buoyant lift scales by |effective gravity| ÷ |declared gravity|", and EL-124 G9 scales the Balloon's declared 11.5 m/s².
   - The working-tree ENGINE-GAS-NETWORK text has the vented store but no density buoyancy, and only says CAT-003 is "re-qualified on the new store".
   - The promise is physically unattainable. The default Balloon (radius 0.36 m, `DryRadius` 0.0625, `BalloonElementDeclaration.cs:14`, `:24`) holds 4.189 × (0.36³ − 0.0625³) = 0.194 m³ and displaces 1.2259 × 0.194 = 0.238 kg. It weighs 0.5 kg plus 0.033 kg of gas, so under the displaced-density law it sinks.
   - A coordinator decision is needed (section 5, item 1). The rows of TH-25 itself work under either resolution.
2. **TH-16 is not buildable** (fork finding).
   - The collar must sit inside the Candle's 0.14 m region box, yet the solid rope and Weight hang through the wax column (`TH-16:101`, `:105`).
   - A3's sub-threshold control has no mechanism to stop heat at 325 K (`TH-16:107`).
   - Both need an author decision on the heat source.
3. **TH-30 rows fail tick by tick** (fork finding). The values are: A1 2.18 W and 154 J at 120 s, against bands of 0.95–1.6 W and 48–80 J; A2 a ratio of 1.36, against "at least 1.5"; A5 136 J. The Motor clause contradicts the spec's own A3. Separately, `Visual/Charge` uses `CommittedStoredFraction`, which the bundle refuses on a supply store (`BundleValidator.cs:310-312`).
4. **Three engine-admission refusals hidden in Batch M and O declarations** (fork findings, confirmed in code):
   - EL-124 G9 compiles a Balloon load of 18.76 m/s², above the per-load 16 m/s² bound (`GasCouplingValues.cs:65`).
   - GAP-07's Wide rods use r 0.035 m, below the cylinder minimum of 1/16 m (`PhysicsDeclarationValues.cs:240`).
   - EL-129 and EL-130 declare a contact with no electrical node, which `BundleValidator.cs:867` refuses.
5. **The steam vessels and the condenser leave gas-path data undeclared.**
   - The kettle's and coffee pot's relief paths have no throat (`TH-17:40`, `TH-35:40`), and A5/A3 assert the head never exceeds the relief pressure.
   - The condenser declares a passage store and two ports but no path between them (`TH-18:39-43`).
   - The kettle's condense direction has no rate (`TH-17:39`), while TH-18 uses a wall-limited rate that T5 does not state.
   - A fresh implementer would have to choose all of these.

## 3. Thermal chains worked tick by tick

Every chain below was iterated at 120 Hz with the declared values. "Spec" is the spec's own figure; all pass their row tolerance unless marked.

| Chain | Quantity | Spec | Tick result |
|---|---|---|---|
| Candle, authored lit (TH-01 A1) | flame at 10 s (committed) | 1088 K ± 40 | 1082 K |
| Candle under a 420 J/K block covering the box (A4) | heat to block in 10 s (≥ 654.5 J) | 716 J | 711 J; wick 690 K, stays lit |
| Candle, block covering half (A4 boundary) | heat (≥ 616 J); flame | 687 J; 353 K | 680 J; 349 K |
| Candle, body on the dish (8 W/K to wick) | wick | goes out | 288 K within 2 s |
| Tinder covered by a 288 K body (TH-32 P9) | committed fibre | about 473 K (> 420) | 473 K |
| Fire bowl bed lit by a pad lying on it (TH-02 B5) | bed lights | 10.6 s | 11.4 s |
| Kettle on plate, 14 400 J (TH-17 A1) | boil starts; 2 g out; 1 g left | 53.0; 55.7; ~75 s | 54.3; 57.6; 76.0 s (9.1 kJ) |
| Kettle, default 3600 J (A4) | water peak | ~344 K ± 2.8 | 343.5 K |
| Kettle, 5400 J (A1 boundary) | water peak | ~364 K ± 3.8 | 363.1 K |
| Kettle capped (A5) | boil starts; relief | 61 s; 74 s | 62.0 s; 76.0 s (9.1 kJ) |
| Coffee pot on plate (TH-35 A1, A5) | boil; 2 g out; 1 g left | 61.1; 63.9; ~86 s | 62.7; 66.2; 88.0 s |
| Coffee pot, 3600 J (A2) | water peak | ~339 K ± 2.6 | 338.4 K |
| Coffee pot capped (A3) | relief | 75.8 s | 77.5 s |
| Kettle to condenser, wall alone (TH-18 A2) | condensed; out; held; wall | 11.2 g; 4.4 g; 2.2 g; 366 K | 11.4 g; 4.1 g; 2.4 g; 368 K (≥ 3 g out holds, margin 1.1 g) |
| Kettle to condenser, Cold pack on shelf (A1) | out; pack | < 0.5 g; ~265 K | 0.0 g; 265.8 K |
| Kettle to turbine (TH-20) | peak head; peak P; > 25 rad/s; dry | 140–160 kPa; 46 W; ~57 s; 85–90 s | 150 kPa; 47 W; 60.0 s; 87.4 s (10.5 kJ); available ~1.0 kJ |
| Cold pack 255 K under the mold (TH-13 A1, A3) | freeze start; f = 1; tray; pack; heat from mold | 9 s; 24.6 s; 269.2 K; ~264 K; 3264 J | 9.6 s; 24.6 s; 269.2 K; 264.3 K; 3264 J |
| Packs at 260 / 262 / 263 / 265 K (TH-13 A4) | f | 1 at 43 s / peak 0.83 / 0.59 at 60 s / peak 0.20 | 43.4 s / 0.826 / 0.592 / 0.196 |
| Heat pump unloaded (TH-12 A1, A4) | A, B at 20 s; max D; D at 132 s after cut | < 270, > 310; ≤ 62; < 30 | 263.2, 323.2; 60.0; 17.3 |
| Heat pump, block, no sink / with sink (A5) | block cooling in 60 s | < 6 K / > 7 K | 2.3 K / 8.9 K |
| Heat pump freezes mold (A6); 600 J boundary | frozen; f | ~55 s; 0 | 52.4 s; 0 |
| Ice plug on plate (TH-15) default / 600 J / unsupplied | start; open; f | 2.6 s; 11.1 s / 0.64 at 10 s / f 0.30 at 120 s | 2.61; 11.05 / 0.639 / 0.30 |
| Hot-air balloon on plate (TH-25 A1) | lift-off | 43.6 s, 5.2 kJ | 48.6 s, 5.84 kJ (within 15 %) |
| Balloon over Fire bowl, 1 crate (A9) | power to pan; lift-off; net at burn-out | ~180 W; 81 s; +0.080 N | ~200 W; 71.9 s; +0.138 N |
| Balloon over Fire bowl, 2 crates (A10) | best net force | −0.21 N | −0.155 N; never lifts |

Conclusions:
- Every repaired chain in my set now holds under the exact step. The first round's A1, A2, A3, D1, E1 and the cold-pack values are resolved in substance.
- With owner decision 19's L_v, the boil row moves exactly the vapour's enthalpy. Vented steam carries Cp_v·T = what the water node paid, so the open-spout heads stay at 373 K. In a capped head, boiling returns (c − Cv)·T to the vessel and is self-limiting, as TH-17 item 9 says.
- In the balloon chain the pan is not "near ambient": it runs at about 345–350 K and passes about 200 W. Both crate rows still hold, inside TH-25's own 151–259 W window.

## 4. Per-spec findings

Severity: **Blocking** (a row fails or a declaration is refused on a correct engine), **Major** (a fresh implementer must make a design decision, or a control cannot fail), **Minor**.

### TH-01 Candle: Ready

- **First round.**
  - A1 resolved in substance: `flame` 0.05 J/K, authored-lit starts at 888/1088 K, and an 8 W wick share holds the wick at 688 K with the flame at ambient (`:38-40`, `:111`).
  - A3 resolved: 1088 K re-derived (`:110`).
  - A4 resolved through TH-31's enlarged box (`TH-31:42`, `:97`).
  - A5 resolved: two 0.12 m Walls with the body 0.13 m above the dish (`:115`).
  - N2 "Phase 2" wording resolved (`:118`).
  - N6 resolved: Run length 45 s (`:119`).
- **Rows re-derived.** A1, A4 and the A4 boundary (section 3).
  - A3: 2051 J ÷ 80 W = 25.6 s, band 22–30 s, inside the 45 s Run. It can fail on a store that does not empty or a relight.
  - A6: 0.239 W/sr × 0.950 m² = 0.227 W. It can fail on an unpaid emitter.
  - Every control can fail: an unlit candle must leave the store whole and the nodes at 288 K.
- **Conformance.** Points 1–7 met. Owners: ENGINE-THERMAL, ENGINE-IGNITION-BLAST, ENGINE-OPTICAL, ENGINE-PARAMETER-BOUND-FRAMES, one per row. The visual-clause rewording is under owner decision 24.
- **Optional Minor, `:131` A4 boundary.** Say how half the box is covered: "the block shifted 0.07 m along X on the same Walls".

### TH-02 Fire bowl: Ready with fixes

- **First round.**
  - A2 (covered tinder goes out, B5 fails) resolved in substance: the pad's G_r is 0.06, the bowl is 0.50 m so the pad lies on the bed, and the flame box starts above the wafer (`:37`, `:44`, item 9).
  - A1 and A3 resolved: flame 0.15 J/K, lit starts 1035/1182 K, B1 1182 K ± 45.
  - Low (Receiver citation) resolved (`:24`; `ReceiverElementDeclaration.cs:57`, `basket.tscn:77`).
- **Rows re-derived.**
  - Bed lighting: 11.4 s, against 10.6 s by hand.
  - B4: 214 W × 10 s + 126 J released = 2266 J ≥ 1963.5 J (85 %). It can fail on a missing region share.
  - B4 boundary: a kettle body heats as it takes the heat (34.9 J/K), averaging about 173 W, which is about 80 % ≥ 70 %, so it holds.
  - B2 boundary: 16.3 s ± 15 %.
  - All runs fit 30 s.
- **Minor, `:126` (B2).** "A burning Tinder pad on the ash" has no igniter, and the pad has no lit option. Replace with: "a Tinder pad lying on the ash, lit by a Spring match placed as in B5, burns out and no flame follows".
- **Minor, `:46`.** The emitter origin (0, 0.30, 0) lies exactly on the underside of any body held on Walls (B4, TH-17 chain 2, TH-25 A9). Optical convention 2 makes an emitter whose origin lies inside another collider emit nothing (`EL-176:172`). Replace "Origin (0, 0.30, 0)" with "Origin (0, 0.25, 0)". That point is between the bed top (0.11 m) and a held underside (0.30 m), and between the two Walls.
- **Minor, `:128` (B4 boundary).** "A kettle-sized body" becomes "a default TH-17 kettle held on the same Walls".

### TH-32 Tinder pad: Ready with fixes

- **First round.**
  - A2 resolved in substance: G_r 0.06, covered ember 488 K continuous and 473 K committed (confirmed).
  - N2 resolved (`:92`).
  - Run length resolved: 100 s.
- **Rows re-derived.**
  - P1: disc 519 K final, τ 29.3 s, 450 K at 35.5 s; burn-out at 84.3 s ≤ 97 s inside the 100 s Run.
  - P1 boundary (tray-down): 77.5 s.
  - P2: the disc peaks at 436 K, below 450 K.
  - P9 control: an 8 W/K contact takes the fibre to block temperature in one tick, so it is smothered.
  - All can fail.
- **Minor, `:106` (P5).** No ignition means, bodies, positions or times are given. Replace with: "Given a pad lit at Run start by a Spring match as in P9, lying on a running delivered Conveyor that carries it from under one TH-26 block held on Walls (as in P9) to under a second, then the first block stops gaining within 1 s of the pad's box leaving it and the second starts gaining | `TheFlameFollowsThePad` | the Conveyor stopped: only the first block gains | none | 5 J".

### TH-17 Kettle: Ready with fixes

- **First round.**
  - E1 resolved in substance by owner decision 19 and T11: the row moves m·Cp_v·T_sat, and the tick sim confirms the open head holds 373 K.
  - C2 resolved: grams on the ledger.
  - N1 resolved: wetted link × fill (`:38`).
  - N6 resolved: 140 s.
- **Rows re-derived.** A1, A4, the A1 boundary and A5 (section 3). The stall guards of 116, 135 and 134 s each equal the hand time + 60 s, so they meet T12. Every control can fail: an empty kettle emits nothing, and the default Battery must leave it below the boil.
- **Minor, `:40`: the relief path has no throat**, yet A5 asserts that the head never exceeds 200 000 Pa. Also, the vent sentence is stale, because F4 now admits package-declared Vents (`EL-039-air-reservoir.md:89`). Replace "A one-way relief path `head` → ambient cracks at 200 000 Pa. A vent on an unconnected Output needs … Batch N summary." with:
  > "A one-way relief path `head` → ambient through a throat of 2.0 × 10⁻⁵ m² cracks at 200 000 Pa (at 200 000 Pa and 393.4 K it passes 340.6 kg/(s·m²) × 2.0 × 10⁻⁵ m² = 6.8 g/s, above any source's boil rate). `GasOut` is declared Vent when unconnected (gas convention F4, `EL-039-air-reservoir.md:89`)."
- **Minor, `:39`: the condense direction has no rate.** TH-19's return row depends on it, and TH-18 uses a wall-limited form (see section 5, item 3). Add:
  > "Condensing: while `wall` is below the dew point of the head's vapour partial pressure, vapour moves from `head` to `water` at ṁ = 2 W/K × (T_dew − T_wall) ÷ L_v(T_dew), never more than the vapour present; m·c·T_dew enters `water` and the latent m·L_v enters `wall` (the condenser's row, TH-18)."

### TH-35 Coffee-pot steam vessel: Ready with fixes

- **First round.**
  - Inherited E1 resolved.
  - Status wording resolved (3a).
  - "Not value-only" resolved: it is now a Family member by values, one collider and art.
- **Rows re-derived.** Section 3.
  - A4: Δp ∝ (ṁ/A)². After 5 s of boiling both heads are about 99 % steam (kettle 5.9 g made against a 1.18 g head; pot 5.3 g against 0.88 g), so the ratio is 4. It can fail on a throat law that does not scale with area.
  - Stall guards: 124, 136 and 146 s, each the hand time + 60 s.
- **Minor, `:40`.** Replace "One-way relief `head` → ambient at 150 000 Pa." with "One-way relief `head` → ambient through a throat of 2.0 × 10⁻⁵ m² at 150 000 Pa (239.8 kg/(s·m²) × 2.0 × 10⁻⁵ m² = 4.8 g/s at 384.5 K)". Also delete "(the same F4 admission as TH-17 needs)", because F4 now admits it.

### TH-18 Condenser: Ready with fixes

- **First round.**
  - C3 resolved: `col_shelf` added and its geometry checked (`:34`, `:37`).
  - E1 resolved: chain redone, and the tick sim matches.
  - A2 point value resolved: it is now a band.
  - Owner decision 16 resolved: gas meets liquid only by the phase row (`:42`).
  - ENGINE-CONNECTION-MATERIALS and the 140 s Run length are stated.
- **Rows re-derived.** A1 and A2 (section 3). The A1 control (default Battery, no boil, no condensate) can fail. The A2 lower edge of 3 g out has a 1.1 g margin.
- **Major, `:39-43`: no gas path joins `GasIn`, `passage` and `GasOut`.** The kettle declares its throat; the condenser declares none, so the implementer must choose. Add after `:39`:
  > "**Gas paths.** `GasIn` ↔ `passage` and `passage` ↔ `GasOut`, each through a throat of 1.0 × 10⁻⁴ m², flow following pressure in either direction (gas convention F3); `GasOut` vents when unconnected (F4, `EL-039-air-reservoir.md:89`)."
  - At `:63` add "gas paths 2".
  - At `:43` delete "the vent needs F4 to admit … recorded for the Batch N summary".
- **Minor, `:115` (A1 boundary).** "The same pack 0.02 m clear of the shelf" has no support: the pack is dynamic. Replace with "the same pack lying on the bench beside the shelf, touching nothing of the condenser, gives no contact link and behaves as A2".
- **Medium (convention dependency).** The row "on that wetted link" with a wall-limited rate (`:42`) is a form T5 does not state (`TH-04:199` states node-held thresholds and a declared-rate evaporation). W9 records it as an ENGINE-THERMAL text gap, and the working-tree ENGINE-THERMAL contract still lacks it. See section 5, item 3.

### TH-20 Steam turbine: Ready with fixes

- **First round.** E1 (inflow above supply) resolved in substance: the 3.0 × 10⁻⁶ m² throat holds the operating point under the relief, which the tick sim confirms (peak 150 kPa), and η 0.25 keeps the chain under Carnot. Run length 140 s is stated.
- **Rows re-derived.**
  - A1: above 25 rad/s at 60 s, never above 30 rad/s by the torque law, under 1 rad/s before 45 s (air expansion drives about 8 Pa). It can fail.
  - Energy: about 1.0 kJ available and about 23 J shaft unloaded. A3's ledger can fail.
  - Carnot per kg at the table's top (416.8 K, 400 kPa): 0.25 × 203 kJ/kg ÷ 179 kJ/kg = 0.28, against 0.31, so it holds.
- **Minor, `:128` (A2) and `:153`.** The load is deferred to the slice. The default Fan `force` of 9 N asks 9 × 12 = 108 W (`FanElementDeclaration.cs:22`, `:48`). Replace "linked to a delivered Fan in Shaft mode" with "linked to a delivered Fan in Shaft mode with `force` 0.5 N (declared airflow work 0.5 × 12 = 6 W, below the 11.6 W best shaft power)". Delete the `:153` sentence "Whether 5 to 12 W turns … names a lighter delivered load."
- **Minor, `:43`.** Delete the stale F4 sentence; F4 now admits it.
- **Medium (convention dependency).** T11 says a vapour-expanding work target whose best shaft power could exceed the Carnot fraction "is refused". The working-tree ENGINE-GAS-NETWORK "Work" bullet (the expander's owner) has no such refusal, and ENGINE-THERMAL's refusal covers only two-node converters. A refusal stated against "its source's input" cannot be evaluated from one declaration. A bundle-checkable form, for the TH-04 re-auditor: "refused if η·Cp·T_sat·(1 − (p_amb/p)^((γ−1)/γ)) ÷ (L_v(T_sat) + c·(T_sat − 288)) ≥ 1 − 288 ÷ T_sat at any point of the saturation table".

### TH-12 Reversible heat pump: Ready with fixes

- **First round.**
  - B2 (sink cannot touch face B) resolved by a static sink beside it and the touch-link rule.
  - B3 resolved: the COP law is defined at D ≤ 0, continuous at T_c/8, and under Carnot.
  - Section 3's 4.69 W/K is used.
  - Run length stated.
- **Rows re-derived.** A1, A4, A5 and A6 (section 3). All can fail: unwired controls stay at 288 K, and the A5 sink/no-sink pair separates 2.3 K from 8.9 K.
- **Minor, `:109` and `:123`.** A6's stall guard of 165 s breaks T12's "smaller of 3 × and + 60 s" (`TH-04:206`), which gives 115 s.
  - In A6 replace "stall guard 165 s" with "stall guard 115 s".
  - At `:109` replace "Run length these rows need: 170 s (… A6's stall guard is 3 × 55 = 165 s; …" with "Run length these rows need: 140 s (A4 observes at 20 + 112 = 132 s; A6's stall guard is the smaller of 3 × 55 and 55 + 60 s, 115 s; …".

### TH-13 Freezing mold: Ready with fixes

- **First round.**
  - A3 and A4 wrong: resolved; my iteration reproduces every figure at `:100-101`.
  - ENGINE-FRACTURE removed.
  - Fixed layer colliders, grams tolerance and Run length 120 s are all in place.
- **Rows re-derived.** A1 (f = 1 at 24.6 s; the 75 s guard meets T12), A3 (3264 J), A4 (0.59 at 60 s; never 1). All can fail.
- **Minor, `:39`.** The charge's ambient conductance is unstated. The hand figures assume none, and A3 says "into tray and water".
  - Append to `:39`: "no ambient conductance of its own (the room reaches it through the tray)".
  - At `:119` replace "the room's inflow into tray and water" with "the room's inflow into the tray".
- **Minor, `:121` (A5).** A default Tennis ball (radius 0.25 m, `TennisBallElementDeclaration.cs:18`) cannot enter the 0.36 m cavity. It rests on the rims with its lowest point 0.0035 m above the floor, so the A2 control "reaches the floor" passes only by tolerance. Replace "a Tennis ball dropped into the tray" with "a Tennis ball with `radius` 0.10 m dropped into the tray".

### TH-15 Ice plug: Ready with fixes

- **First round.**
  - D1 resolved in substance: 2.6 s start, open by 11.1 s, an unsupplied disc melts it, A2 now on the bench, A3 at 600 J with a 0.2–0.8 band. All confirmed (section 3).
  - Run length 120 s.
- **Rows re-derived.** A1, A2 and A3. A1's "nothing in the first 2 s" has a 0.6 s margin, inside the 15 % time tolerance. All can fail.
- **Minor, `:107` (rig).** "Surface 0.5 m above the bore" leaves the reservoir's volume and placement to the implementer. Replace "an EL-001 reservoir joined to `Start` with its surface 0.5 m above the bore" with "an EL-001 reservoir with `initial_volume` 0.5 m³, placed so that its `End` mouth joins `Start` (its interior floor level with the bore, surface 0.5 m above it)".

### TH-25 Hot-air balloon with `balloon_cargo`: Ready with fixes

- **First round.**
  - A1 (0.02 m gap) resolved: 0.005 m at least once.
  - A6 Run length resolved: 98 + 60 s.
  - Owner item 7 resolved by the companion crate (owner decisions 15 and 23). The crate is admitted: mass ≥ 1/1024 kg (`PhysicsDeclarationValues.cs:171`), box half extents ≥ 1/1024 m (`:229-231`), smallest moment 3.4 × 10⁻⁵ kg·m².
  - **C1 (two buoyancy laws) is reworded, not resolved.** See finding 1.
- **Rows re-derived.**
  - A1: lift-off at 48.6 s.
  - A9: lifts at 71.9 s, net +0.138 N.
  - A10: best −0.155 N.
  - A6 terminal speed: 0.433 N ÷ (0.4 × 1.065 kg) = 1.02 m/s against the 1.2 m/s bound.
  - All can fail: unlit or unwired means no lift, and A10 is a real control for A9.
- **Major, `:39`, `:72`, `:148`, `:166`: the buoyancy law.** Replace the `:39` sentence "This law **replaces** … one rule for one behaviour)." with:
  > "The displaced-density term is ENGINE-GAS-NETWORK's ambient load of a vented store-bound body. Whether the delivered Balloon and the three balls keep their declared lift acceleration (owner decision 24; ENGINE-FIELD-FORCE scales declared lift) or move to this term with re-sized declared values (the default Balloon displaces 0.238 kg against 0.533 kg and would sink) is a coordinator decision recorded in ENGINE-GAS-NETWORK; this spec does not promise the Balloon's lift unchanged."
  - Make the same change at H3 (`:72`), item 7 (`:148`) and the prerequisites (`:163`). Raise the decision as section 5, item 1.
- **Major, `:129` (A6): "when the cargo is removed" has no defined in-Run mechanism.** Replace the row with:
  > "| A6 | Given A9, then within 60 s after the pan leaves the Walls the balloon climbs, never faster than 1.2 m/s, and its speed at the end of that minute is below its peak | `AWarmedBalloonClimbsWithItsCargo` (run in the TH-02 slice) | A9's `lit` "Unlit": no climb | none | speed bound; direction only; no fixed arrival |"
  - Its peak is about 0.138 N ÷ (0.4 × 1.095 kg) = 0.32 m/s, and it ends inside 81 + 60 = 141 s.
- **Minor, `:36` and `:125`.** The default Tennis ball (0.50 m across) does not fit between the struts, whose inner faces are 0.36 m apart, at the deck's centre.
  - At `:36` replace "a Tennis ball (0.50 m across) fits" with "a Tennis ball of `radius` 0.15 m (0.30 m across) fits between the struts".
  - In A2 write "with a Tennis ball (`radius` 0.15 m, `mass` 0.35 kg) at the deck's centre".
- **Minor, `:107-108`.** "About 180 W … the pan stays near ambient" is wrong. The pan covers the whole box (G = 4.0 W/K) and runs at 345–350 K. Replace with:
  > "about 200 W reaches the pan, which runs near 345 to 350 K (evaluated tick by tick with T9): the gas has risen 16.2 K at burn-out; one crate lifts at about 72 s (net +0.14 N at burn-out); two crates peak at −0.155 N and never lift"
  - Released-flight terminal speed becomes about 1.0 m/s. Both stay inside the 151–259 W window the spec states.
- **Minor, `:115`.** A3 runs from lift-off (48.6 s by tick, up to 50 s at +15 %) plus 120 s, which is 170 s at the edge. Write "Run length these rows need: 180 s".

### TH-08 Heat-conducting bar: Ready (fork)

- **First round.**
  - B1 resolved: the bar is static, level and wholly on the disc; the target sits on a 0.13 m Wall.
  - N3, N6 and the Low clamp item are resolved.
- **Rows.** A1 crosses 330 K at 20.7 s (spec about 21 s). Steady state 394.1 K and 47.7 W, exactly. A3, A5 and A2 can each pass and fail.
- **Optional, `:100`.** State that the puck's zero-clearance placement links within ENGINE-THERMAL's touch tolerance.

### TH-11 Heat exchanger: Ready with fixes (fork)

- **First round.** C3 resolved: EL-001 now declares `liquid_temperature` (`EL-001:61`). Fluid-port owner, Run length and single owners are resolved.
- **Confirmed by my own calculation.**
  - Geometry: the floor datum is 0.10 m, the interior height is 0.0251 ÷ 0.126 = 0.20 m, and the mouths sit at y 0.20 (`TH-11:28`, `:33-36`).
  - Under L7 and L8, a flowing passage stands at its mouth datum plus 0.00014 m, so it is half full.
  - The steady balance at 16.7 W/K per stream gives q = 60 ÷ (1/32 + 2/16.7) = 397 W and T_B = 311.8 K with full passages.
  - With half passages it gives q = 60 ÷ (1/16 + 2/16.7) = 329 W and T_B = 307.7 K.
  - So the row's 311.8 K ± 3 K fails against the spec's own geometry.
- **Blocking, `:28`.** The store lacks L4's interior height and extents and its open/closed status. With mouths at mid-height, a flowing passage stands half full. Replace with:
  > "…capacity 0.0251 m³ each, interior 0.80 × 0.20 × 0.1575 m (plan area 0.126 m², height 0.20 m), floor datum y 0.10 m; no open aperture. The mouths' datum (y 0.20) is mid-height, so a passage with a free outlet stands half full (0.01255 m³, 210.3 J/K)."
- **Blocking, `:39`, `:95`, `:112` (A4).**
  - `:39`: "32 W/K between two full streams" becomes "16 W/K between two flowing (half-full) passages".
  - `:95`: becomes "q = 32 × 60 ÷ 2 ÷ (1 + 32 ÷ 16.74) = 329 W, T_A = 328.3 K, T_B = 307.7 K".
  - A4 Then: "stream B leaves at 307.7 K", with `End`/`EndB` as unjoined free ends over a catch basin. The current 311.7 K figure fails by 4.1 K against 3 K.
- **Major, `:97`.** Replace the A4 boundary with: "stream B stopped (its tap closed) drains to its mouth datum and warms toward 348 K with a time constant of about 35 s; after 115 s it is within 5 K of 348 K".
- **Major, `:109` (A1 rig).** Add: "`End`/`EndB` unjoined; after the taps close each passage drains to its mouth datum before t₀". The ideal figures become 316.1 K and 6.71 kJ.
- **Minor.**
  - `:96`: delete the impossible "Unthrottled" 0.5 m bullet.
  - `:92` and `:135`: change the stale "EL-001 has no node" to "EL-001 declares `liquid_temperature` (273–368 K, `EL-001:61`)".

### TH-16 Fusible link: Not ready (fork)

- **First round.** D2 resolved: a 58.8 N × (1 − m) limit, 5 and 7 kg Weights, ENGINE-CONNECTION-MATERIALS added. The added candle heat source creates new blockers.
- **Rows.** m reaches 0.33 at 9.45 s. A5's "under 300 K" is reached only near 190 s with 0.05 kg of fuel, and never with 0.3 kg inside 300 s.
- **Blocking, `:101`, `:105`.** The collar must lie within 0.115 m of the candle axis to take a region share. The solid rope and Weight would then hang through the dish and the column. Any offset that clears them gives a share of 0. This needs an author decision, for example turning the rope over a delivered Pulley beside the candle and re-deriving the share.
- **Blocking, `:107` (A3, the requirement's sub-threshold control).** No bounded source can hold the collar at 325 K:
  - the candle's minimum burn is 25.6 s, and the collar melts at 9 s;
  - the recessed waist (radius 0.045 against the end blocks' 0.06) cannot be touched by a bar.
  - This needs an author decision: for example, make the collar proud (radius ≥ 0.065) and heat it through a TH-08 bar from a Battery-limited plate; then re-derive A1, A3 and A5.
- **Major, `:109` (A5).** Replace with: "Given A1 with `fuel` 0.05 kg, for 60 s after the candle's store is empty the Weight's rope stays free, m stays 1 and the collar cools" (direction only).
- **Minor.**
  - `:29`: add `RopeAttachmentRole.Load` (`PortDescriptor.cs:74`).
  - `:60`: F2 is "present (topology admits Load–Load spans, `engine/gpu/RopeTopology.cs:37-42`); untested here".
  - `:105`: the stall guard becomes 30 s.

### TH-30 Thermoelectric generator: Ready with fixes (fork)

- **First round.** Load (owner item 6), the 400 K start with the 3.2 W/K fade, B2 (static sink, cold plate on top), 4.69 W/K, the store-less owner and the 240 s Run are all resolved.
- **Blocking, `:95` and A1 (`:111`).** Replace with: "At 120 s, evaluated tick by tick with T9: T_h 391.9 K, T_c 316.4 K, Q_h 37.8 W, P 2.18 W; energy converted by 120 s 154 J". A1's bands become 116–193 J and 1.6–2.7 W.
- **Blocking, `:96` and A2 (`:112`).** Replace with: "at 120 s T_h 386.2 K, T_c 295.1 K, P 3.15 W; 210 J, 1.36 times still air". A2 becomes "at least 1.25 times A1's".
- **Blocking, `:97` and A5 (`:115`).** The energy is 136 J. Split the row:
  - "with a Fan in Electrical mode on `Supply`, the energy delivered by 240 s is 136 J less what the store holds (102–170 J) and the late power is under 0.1 W";
  - "with the Motor it has turned at least 10 revolutions by 60 s and the energy delivered never exceeds the energy credited".
- **Major, `:43`.** `Visual/Charge` uses `CommittedStoredFraction`, which needs a mechanical `EnergyStore` (`BundleValidator.cs:310-312`). Use `PresentationDriver.CommittedCharge` with `SupplyNode: source` (as `BatteryElementDeclaration.cs:15`), and correct N4 to match.

### RAD-20 RTG: Ready with fixes (fork)

- **First round.**
  - H-2 resolved: cold plate on top, static TH-10.
  - H-3 resolved: store-credit form under ENGINE-THERMAL.
  - C-9 resolved: inventory 24 000 J = 80 W × 300 s.
  - X-2 and X-4 resolved.
- **Graphs.** The supply and signal graphs pass `SupplyBundleValidation.cs` (single binding per port, no self-edge or cycle) and the coordinator decision 21 store-credit form. G1 credits 47.6 J by tick (spec 47.4).
- **Minor.**
  - `:32`: "face `col_cold` +Y" becomes "face `col_cold`".
  - `:76` (M4): add "and the contact state it presents to `SignalStateContactControl` (`SignalContactState` has only `Latched` and `TimerCounting`, `SignalContactControl.cs:5`)". The same applies to TH-30's R14.

### RAD-21 Material latch: Ready (fork)

- **First round.**
  - H-4 resolved: threshold → pulse on rise → physical request. Today's validator refuses the edge kinds (`SignalBundleValidation.cs:65-67`) and the spec names that as missing.
  - C-10 resolved: 0.882 J/s, so 2.0 J at 2.27 s.
  - C-1, C-3 and the shared retention row are resolved.
- **Rows.** L1–L4 re-derived; torques and fit checked; all can fail. L7's fixture box size is unstated, but it does not affect the outcome.

### EL-129 Sheet-thickness gauge: Ready with fixes (fork)

- **First round.** H-1 resolved: one contact `PowerIn` → `relay` → `Supply`, with no cycle or self-edge (`SupplyBundleValidation.cs:142-145`, `:162-175`). C-1, C-3, M-11 and X-1 to X-4 are resolved.
- **Major, `:32-36` and `:49`: no electrical node; the bundle refuses it** (`BundleValidator.cs:867`). Add: "| Electrical nodes | `relay`: `ElectricalRole.Switch`, 12 V, resistance 0.05 Ω, capacitance 0, maximum 100 W, as `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:77` |". Add "Electrical nodes 1" to the counts.
- **Major, `:35` (and `EL-130:23`).** "`valid` AND NOT `clear`" is not a `LogicGateOperation`. Replace with: "`window` = `valid` Xor `clear`, a two-input boolean signal node; equal to `valid` AND NOT `clear` because `clear` (≥ 0.0582 W) implies `valid` (≥ 0.05 W)".
- **Minor, `:116` (G7).** Change "refused" to "a finite value outside them is clamped to the nearer bound (Clamp); a non-finite value is refused".

### EL-130 Tank-level gauge: Ready with fixes (fork)

- **First round.** H-1 (inherited), M-6, X-2, C-1 and the rounding note are resolved.
- **Major, `:91` (L5).** Raising the gauge 0.25 m leaves the samples off-axis, giving 0.793 W, not 0.864 W. Replace with: "with the gauge and the capsule both raised 0.25 m (all sample paths above the surface) the strength is 0.864 W and `Supply` is dead".
- **Minor.**
  - `:68`: add "h = 0.40 m above the tank floor; the tank centred between capsule and gauge".
  - `:88`: "a Tap at opening 1 on an EL-001 reservoir".
  - The EL-129 electrical-node and Xor fixes also apply here.

### EL-113 Structural brace: Ready with fixes (fork)

- **First round.** 3a and story names, the rig interpenetration, and B7 (moving a member) are resolved. B1, B2 and B7 were re-derived and can each pass and fail.
- **Major, `:108` (B4).** The free end overlaps the right beam at Run start. Replace with: "Given a brace connected at the left beam's `EndB` only and set pointing up and to the left at 45° (free end near (−0.45, 4.67) m), clear of every body it is not attached to, when Run starts, then it swings from its one pin and the bay folds as in B2".

### EL-115 Linkage connector: Ready with fixes (fork)

- **First round.** L5, the L1 margin and the names are resolved.
- **Major, `:106` (L3).** Replace with: "Given the push rig with the link connected at lever 1's `EndB` only and set hanging straight down from it (to y = 1.8 m, clear of lever 2), then lever 1 still reaches its stop and lever 2 stays".
- **Major, `:108` (L5 control).** Replace with: "with the second connection not made and the lower beam and its Pivots moved 0.15 m down, the link also hangs still".

### EL-123 Protected end-state goal: Ready with fixes (fork)

- **First round.** Stateless evaluator, fifth goal slice and Run length are resolved.
- **Major, `:98` (P2b).** A 0.3 m rope is shorter than the Balloon radius (the tie is at its centre). Replace with: "tied by a 0.45 m rope to a Rope anchor laid on Receiver B's floor … with the rope cut at 1.2 s the centre clears the 0.47 m sensor top after about 0.52 s, near 1.72 s".
- **Minor, `:98`.** The Balloon string is owner decision 1's non-solid exception and gains no collider.

### EL-124 Authored gravity preset: Ready with fixes (fork)

- **First round.** One uniform-field law, direction and humidity are resolved.
- **Blocking, `:41`, `:96`, `:107` (G9).** 11.5 × 1.631 = 18.76 m/s² exceeds the per-load bound (`GasCouplingValues.cs:65`). Replace `:96` with: "**Bound.** At 16.0 the scale is 1.631 … The scene-region slice replaces the per-load bound at `GasCouplingValues.cs:65` with the combined-acceleration bound already at `engine/gpu/GasSceneValidation.cs:78` (|gravity + load| ≤ 16 m/s²), deleting the per-load check; 16.01 is refused." Alternatively, run G9 at 13.0 m/s².
- **Minor.** `WorkshopConstruction.Gravity` is at `engine/gpu/WorkshopConstruction.cs:15`.
- **Note.** G9 treats the Balloon's lift as a declared acceleration, which contradicts TH-25 (finding 1).

### EL-159 Tension-limited connector: Ready (fork)

- **First round.** Rig, "sustained" (0.10 s), names and stress cue are resolved.
- **Rows.** 14.7, 34.3 and 93.2 N; T1 and T2 at 43 % under and 55 % over the limit. Both can pass and fail.

### GAP-04 Driven wheel: Ready with fixes (fork)

- **First round.** The standing vehicle, mount points and wording are resolved. Traction 8.33 N against 11.4 N.
- **Major, `:132` (W1).** The Ramp slab is 0.18 m thick (`elements/Ramp/ramp.tscn:9`); climbing onto it needs about 91 N against 16.7 N available. Replace with: "Given the reference vehicle at rest wholly on a 4 m Ramp at 5°, its driven axle 0.35 m up the slab … its Weight rises more than 0.08 m". Controls: "unwired it rolls back; level and unwired it moves less than 0.02 m".
- **Major, `:132` (15° boundary).** Replace with: "placed the same way wholly on a 15° Ramp, the Weight loses height".
- **Minor, `:157`.** Add the disqualifier that EL-117's 0.05 m shaft is below the 1/16 m cylinder minimum.

### GAP-06 Granular dispenser: Ready with fixes (fork)

- **First round.** ENGINE-GRANULAR scope (owner decision 14), blade rest position (owner decision 6, coordinator decision 25), jam row, fill line, count limit and tunnelling sentence are resolved.
- **Major, `:134-135` (G4, G5).** Replace with: "a Wall whose top is 0.02 m below the blade's underside (0.06 m below the outlet plane)". Add the occlusion-reach line to ENGINE-GRANULAR's additions.
- **Major, `:138` (G8).** Use a Tennis ball (`radius` 0.25 m), not a Basketball, which cannot enter the 0.60 m vessel.
- **Major, `:56`, `:139` (G9).** "`Visual/Fill` height ← the store's committed surface height"; values 0.45 m and 0.25 m.
- **Minor.** Declare "Grain friction 0.6" (`:38-45`); give the outlet height over the Receiver in G1 (`:131`).

### GAP-07 Granular sieve: Ready with fixes (fork)

- **First round.** The Narrow/Wide Choice with ENGINE-PARAMETER-BOUND-FRAMES is resolved.
- **Blocking, `:33`, `:37`.** Cylinder radius must be ≥ 1/16 m (`PhysicsDeclarationValues.cs:240`). Replace with: "`col_rod_0`–`col_rod_5`: box, half extents (by `aperture`: 0.065 m Narrow, 0.035 m Wide; 0.035; 0.45)", with box art. Make counts "10 box", and delete the cylinder probe row.
- **Minor, `:38` against S5.** Add an ENGINE-GRANULAR line saying the sieve's store holds every declared fraction that enters it.

## 5. Cross-spec contradictions

1. **Buoyancy: one law or two.** These four sources disagree:
   - `TH-25:39`, `:72`, `:148` say density buoyancy is the only law and the Balloon is re-declared.
   - Owner decision 24 says the three balls keep declared ambient lift.
   - The working-tree ENGINE-FIELD-FORCE text says "every declared buoyant lift scales by |g_eff| ÷ |declared g|"; EL-124 G9 scales the Balloon's 11.5 m/s².
   - The working-tree ENGINE-GAS-NETWORK text has no density buoyancy.
   - The default Balloon cannot rise under the density law.
   - Needs a coordinator decision. Either (a) a density term of the one ambient-load capability for vented store-bound bodies, beside declared lift for bodies without a store, with TH-25's "replaces" wording withdrawn; or (b) everything moves to density with re-sized Balloon and ball values, which conflicts with owner decision 24.
2. **Fire-bowl power to a body.** TH-25 uses "about 180 W, pan near ambient". TH-02's 182/164/214 W are for a body with no small node at 288 K or 373 K. A 2 J/K pan on a 4 W/K wetted link takes about 200 W at 345–350 K. Rows are unaffected; the texts should agree.
3. **Condense row forms.** T5 (`TH-04:199`) states node-held phase thresholds; TH-17's condense direction has no rate; TH-18 uses a row on a wetted link with ṁ = G·(T_dew − T_wall) ÷ L_v. The working-tree ENGINE-THERMAL contract has "boil and condense against a declared saturation table" only. For the TH-04 re-auditor: T5 should state the wall-limited condense row as the one condense form and TH-17 should cite it, or it is a second rule.
4. **Stale vent text.** `TH-17:40`, `TH-18:43`, `TH-20:43` and `TH-35:40` say F4 must still admit a package-declared Vent; `EL-039:89` now does.
5. **Carnot refusal for an expander.** T11 (`TH-04:205`) requires it, but no story text carries it, and as worded it is not evaluable from one declaration (TH-20 above).
6. **Readiness summary is stale** (fork).
   - `readiness/batch-n.md:58`, `:155` and `TH-11:92`, `:135` say EL-001 has no node, but `EL-001:61` declares `liquid_temperature`.
   - `readiness/batch-n.md:56`, `:193` list TH-16 as Ready.
7. **Liquid L4 (`EL-001`) has no rule for a closed store fed at V = C** (fork). Proposed: "A store with no open aperture is closed: at V = C its mouths pass flow as a lossless fitting and it never overflows."
8. **Cylinder minimum 1/16 m.** EL-114's Pivot shaft (r 0.06) and EL-117's Axle shaft (r 0.05) are both below it. EL-113, EL-115, EL-159 and GAP-04 stand on them. Not in this group; flag to their owner.
9. **`readiness/batch-m.md:94` says the field story "sets the bound on a summed field"**, but the per-load bound (`GasCouplingValues.cs:65`) is still in force, and EL-124 G9 depends on its replacement.
10. **RAD-08 convention.** Its supplied contact appears to declare no electrical node either (`BundleValidator.cs:867`). This is for the RAD-08 re-auditor.
11. **Boolean node and revolute retention owners** (fork).
    - A two-input boolean signal node is in ENGINE-SIGNAL-NODES, while engine gap E2 puts an N-input supply boolean in ENGINE-CAPABILITY-INSTANCES.
    - RAD-21 and TH-33 give revolute retention to ENGINE-ASSEMBLY-JOINTS, while EL-057's holds are in ENGINE-JOINT-KINDS.
    - Each boundary needs one sentence.
12. **EL-123 contradicts owner decision 1.** It calls the Balloon's rope a solid strand, but owner decision 1 keeps the Balloon string non-solid.

## 6. Not verified

- **`epics.md` is being edited concurrently.** The working tree is uncommitted with 632 insertions; the ENGINE-THERMAL heading moved from line 4500 to 4712 during this audit. Spec citations of `epics.md:NNNN` were not checked. Story-text statements above quote the working-tree version read on 23 November.
- **Gas modelling.** Gas heads were modelled as ideal well-mixed stores with adiabatic venting, and line volumes were folded into the head. The kettle's spout and hose throats were treated as lossless at the low flows involved. ENGINE-GAS-NETWORK's per-step clamp and throat discretisation may move the steam times by a few percent; every row here has at least 15 % or 1 g of margin, except as noted.
- **Placement and authoring.**
  - TH-33's match placement for TH-02 B5 (box covering 0.70 of the wafer) was checked only against the stated box extents.
  - Whether the Workbench admits static parts placed off the bench, or touching placements at zero clearance.
  - The Wall laid flat across a 0.50 m bowl needs `width` ≥ 0.45 m; the Wall admits 0.4–8 m, so it is possible but not stated.
- **Fan load.** Whether a Fan at `force` 0.5 N presents the stated 6 W load to a shaft.
- **Fork re-checks.** The fork-audited specs (Batch M, Batch O, TH-08, TH-11, TH-16, TH-30) were re-checked by me only at the cited refusal lines, not row by row.
