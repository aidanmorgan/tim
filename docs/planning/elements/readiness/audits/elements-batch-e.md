# Audit: element specs, Batch E (Epic 13 optics; 22 catalogue specs)

Independent read-only audit, 22 November 2026, checkout `spec-drafts` at `8d1d68aa` plus drafts. Nothing was built or run. Audited against the element-spec brief, the owner decisions of 22 November 2026 (3a and 9 to 16), the engine conformance addendum, the repaired optical conventions (`EL-176-red-laser-emitter.md` section 10, "conventions" below) and `readiness/batch-k.md` notes G6 to G15. Spec paths are under `docs/planning/elements/`. `ABS/` = `CuriousContraptions.Abstractions/`, `SIM/` = `CuriousContraptions.Simulation/`.

**How the specs were read.** Twelve were read line by line: CAT-036, 029, 059, 038, 056, 072, 068, 043, 055, 041, 008, 006. The other ten were read as their complete line differences from the family lead plus their acceptance and open-question sections: CAT-032, 012 (against 056); CAT-021, 040 (against 072); CAT-044 to 047 (against 043); CAT-031, 011 (against 055). Code citations were opened for every "present" row of CAT-036, 029, 059, 038, 056, 041, 006, 043 and 047: all cited symbols and tests exist at the cited lines. At least one arithmetic row was recomputed in each of the 22 (section 7).

**The CAT rows are the single acceptance authority** for 18 closure identities and EL-210 (owner decision 16), so a row that is missing, cannot fail or cites a deleted EL row is a finding against the closure too.

## 1. Verdicts

0 Ready, 20 Ready with fixes, 2 Not ready. Conformance: 0 Conforms, 22 Conform with fixes. No spec asks for element-specific engine code. No package of the batch is delivered; a search of `engine/`, `ui/`, the `CuriousContraptions.*` projects and `elements/` for the 22 package ids finds only `tools/Coverage` requirement tables and `content/puzzles.json` (`flashlight` 3 times, `solar_panel` 6 times), so there is no engine refactor to record. `ElementCategory` (`ABS/ElementCategory.cs:5`) has no Optics member.

Every spec already says "Blocked on the engine programme (owner decision 3a, 22 November 2026)" in its Status row (`:14` in all 22) and in Prerequisites. That part needs no change; what is missing beside it is in X-2.

| Spec | Verdict | Conformance | Reason |
|---|---|---|---|
| CAT-036 Laser | Ready with fixes | with fixes (2, 6) | X-1 to X-7; two "present" rows are unproved (M-1); pipe criterion stale (M-2) |
| CAT-029 Flashlight | Ready with fixes | with fixes (2, 6) | X-1 to X-7; contradicts the conventions on filters in a cone (H-2); no conservation row (H-3); F8 Run length (M-3); harvest facts 5 and 7 (M-9) |
| CAT-059 Solar panel | Not ready | with fixes (2) | Source reads the receiver directly and needs two bespoke source forms (H-4); a requirement criterion is moved out to EL-211 (H-5); EL-211 disagrees on every number (H-6) |
| CAT-038 Laser receiver | Ready with fixes | with fixes (2, 5, 6) | X-1 to X-7; description and rule wording (M-10) |
| CAT-056, 032, 012 primary receivers | Ready with fixes | with fixes | X-1 to X-7; no filtered-cone positive row (M-6) |
| CAT-072, 021, 040 mixed receivers | Ready with fixes | with fixes | X-1 to X-7; R4 does not name its fixture (M-7); CAT-021's thin purity case (L-3) |
| CAT-068 White receiver | Ready with fixes | with fixes | X-1 to X-7 |
| CAT-055, 031, 011 filters | Ready with fixes | with fixes (5, 6) | X-1, X-4 to X-7; no cone row (M-5); CAT-055 F4 lacks the green case (M-8); CAT-011 margin claim (L-1) |
| CAT-041 Mirror | Ready with fixes | with fixes | X-1, X-4 to X-7; "yawed" names the wrong axis (M-11) |
| CAT-008 Beam splitter | Not ready | with fixes | The 128-segment and 16-interaction criteria have no row that can fail (H-7, H-8); Split has no once-per-crossing rule (H-9) |
| CAT-006 Beam combiner | Ready with fixes | with fixes (D1) | X-1, X-4 to X-7; 16-interaction row (H-8); shared outlet is not the conventions' Relay shape (M-12); R9 cannot show two re-entries (M-13); rim colour (M-14) |
| CAT-043 to 047 gates | Ready with fixes | with fixes (2) | Logic inside the optical solve (X-2); cone figure wrong by a factor of 3 and no row (H-10); interaction budget has no row (M-15); hold-band row not buildable as written (M-16) |

## 2. Findings that apply across the batch

**X-1 (High). All 22 specs still state the superseded tick order.** Lines: CAT-036:68, 029:76, 059:68, 038:76, 056:79, 032:80, 012:80, 072:81, 021:82, 040:82, 068:84, 055:57, 031:57, 011:59, 041:56, 008:59, 006:67, 043:65, 044:65, 045:65, 046:66, 047:66. Each says "in Phase 2, after the same tick's Phase 1 supply and logic solve and before the substeps … poses … committed by the previous tick … (decision D7)". No grant exists before the substeps (`SIM/WorkshopPhysicsEngine.cs:1011`, `SIM/WorkshopPhysicsEngine.Supply.cs:72`–`:107`), and owner decision 16 settles the order. Replacement for each paragraph (keep the part-specific last sentence): "One optical solve per 120 Hz tick, after the last substep and before commit (conventions item 4; owner decision 16). It reads the energy granted to each emitter over this tick's substeps, this tick's final collider poses and signal levels as resolved at the start of this tick. Segments, readings and poses commit together; readings act from the next tick. No row asserts a tick except where a next-tick decision is the feature." Dependent edits: every M2 probe row ("nearest hit … at its committed pose") becomes "ordered crossings from ENGINE-SPATIAL-QUERIES' segment cast at this tick's final pose"; "the supply solve grants nothing" (036:65, 029:74) becomes "a tick with no grant over its substeps has no segment"; every Open-questions "resolved by D7" (for example 036:165, 029:178, 059:161, 038:182, 043:168) becomes "owner decision 16; conventions item 4".

**X-2 (High). Coupling: 14 specs let a contact, a source or a relay read a receiver directly, and `ENGINE-SIGNAL-NODES` is named in none of the 22 (0 hits).** Owner decision 16 and conventions item 9 allow one control coupling: committed strength → threshold signal node with a release ratio (ENGINE-SIGNAL-NODES) → the delivered signal-state contact (`ABS/SignalContactControl.cs:12`), or a relay or emitter enable naming a signal level.
- *Eight receivers* (supply-graph row `:30`; `:21`; probe row P6 at 038:89, 056:92, 032:93, 012:93, 072:94, 021:95, 040:95, 068:97; the disqualifier "a contact cannot follow a receiver state"). Replace `:30` with: "One signal node `matched`: a threshold node over this receiver's committed strength, threshold bound to `threshold`, release ratio 0.9 (missing; ENGINE-SIGNAL-NODES). One contact node `contact` under the delivered `SignalStateContactControl` naming `matched`." P6 becomes "present: `SignalStateContactControl`, test `SwitchElectricalTests.UnqualifiedOrUnsuppliedContactCannotMoveConsumer`" plus a new missing row "threshold signal node with release ratio over a committed scalar: ENGINE-SIGNAL-NODES". Interfaces gain `ISignalElement`; domains gain `SimulationDomain.Signal`; the flat-state line "domain nodes 1" becomes "signal nodes 1, supply nodes 1"; the indicator driver is `PresentationDriver.CommittedActivation` on the node (present, untested as used), not a missing optical driver.
- *Five gates* (lines 25, 31, 57, 77 (P12), 84, 92 to 96, 183 of CAT-043; the same text in 044 to 047, one line later for probe and conformance lines in 046 and 047). "A Relay enabled by a `LogicGateOperation` over two receiver levels" puts a logic evaluator inside the optical solve: a second rule beside the signal network, which the review gate fails. Replace with a row "Signal graph (ENGINE-SIGNAL-NODES): two threshold nodes over the committed strengths of A and B, on at 2.5 W, release ratio 0.9; one boolean node `open` = And(A, B) [Nand, Nor, Or, Xor]; no ports", and at `:31` "its enable names the signal level `open`". P12 splits into three missing rows: threshold node (ENGINE-SIGNAL-NODES), boolean node over two levels with the `LogicGateOperation` set (ENGINE-SIGNAL-NODES; `ABS.Electrical/ElectricalContractValues.cs:25` has And, Nand, Nor, Or, Xor), relay enable naming a signal level (ENGINE-OPTICAL). Counts at `:96`: "receivers 2 (one sample each); signal nodes 3 (2 threshold, 1 boolean); relays 1; apertures 3; bindings 9". Lamps A and B are driven from the threshold nodes.
- *Solar panel*: H-4.
- *Status and Prerequisites of all 22*: "the engine programme (owner decision 3a), in which ENGINE-SPATIAL-QUERIES provides the segment cast; ENGINE-SIGNAL-NODES [receivers, gates, Solar panel]; then ENGINE-OPTICAL". ENGINE-SPATIAL-QUERIES is named today only as an amendment to ENGINE-OPTICAL.

**X-3 (High). Conservation is still the per-receiver cap.** "min(P, A × Σ wᵢEᵢ)" at 029:71, 059:27, :62, :82, receivers `:29` (all eight), 038:72, 068:81, gates `:30` and `:61`. Three receivers in one cone can together read more than is emitted. Replacement: "its share A × Σ wᵢEᵢ; when the shares of one diverging segment summed over all receivers exceed its power, every share is scaled by power ÷ sum (conventions item 8; owner decision 16)". The receivers cite "point 7"; it is item 8.

**X-4 (Medium). The work bound is the superseded one in all 22** ("optical conventions 8 and 9 … N × 128 + D × K": 036:100, 029:112, 059:99, 038:109, 056:112, 032:113, 012:113, 072:114, 021:115, 040:115, 068:117, 055:86, 031:86, 011:88, 041:85, 008:88, 006:97, 043:97 and siblings). Replacement: "(conventions items 10 and 11) at most N × 128 segment casts and N × 128 × K shadow casts per tick, that is N × 128 × (1 + K), each costing one spatial-query segment cast; at the segment limit segments are kept breadth-first by depth, then by emitter and surface order. This part adds K = <its sample count>."

**X-5 (Medium). Stale references to EL rows that no longer exist** (batch-k G7). The closure specs now carry no rows. Delete: bracketed "T" ids and "T3, T7, T9 and T10" in CAT-029 (:15, :147 to :160, :164, :177); "rows S1 to S11 / B1 to B8" in the receivers (038: 17, 72, 158, 165, 168, 183, 191; 056: 17, 76, 146, 163, 182, 188; 032: 17, 77, 147, 164, 171, 183, 189; 012: 17, 77, 147, 164, 171, 184, 190; 072: 17, 78, 147, 154, 162, 169, 181, 187; 021: 17, 79, 148, 155, 163, 170, 184, 190; 040: 17, 79, 148, 155, 163, 170, 183, 189; 068: 17, 81, 151, 166, 173, 181, 192); "its rows F1 to F7 / M1 to M9 / C1 to C9 are …" (055:139, 031:139, 011:142, 041:139, 006:158); "EL-138's rows G1 to G9 are this spec's …" (043:158, 044:157, 045:157, 046:158, 047:158) and "the class EL-138 names" (`:137`). "As EL-143/144/145/212 declares it" (055:151, 031:151, 011:154, 041:152) is circular now and becomes "the family geometry of the optical conventions". The coverage line becomes "the EL ledger row cites this part and the tests of rows <ids>". The mappings themselves agree with the repaired EL specs.

**X-6 (Medium). Tolerances: three sets in use and no row names what it excludes.** See section 5 for the one set and the sentence each spec needs.

**X-7 (Low). Settled alternatives still written as open.** The Optics category: delete "if it stays here" (036:85, :181) and "Batch C's summary proposes … the cross-batch pass keeps one" (each M9 probe row; 043:82): `ElementCategory.Optics` and the optics domain member are added by ENGINE-OPTICAL (K-16). Cite owner decision 9, not "decision K-2", for the one receiver law (038:72, :183 and the same lines in the siblings). Absorb by default is stated on every collider row and needs no change, except the two harvest lines in M-9. No spec has a "collimated only" attribute.

## 3. High findings, per spec

**H-2. CAT-029:72, :177 (and harvest header :117): "mirrors, filters, splitters, combiners and gates do nothing to this cone except occlude it".** Contradicts conventions item 3 and the filter specs themselves (055:54, 031:54, 011:55 state the right rule). Replacement at :72 and :177: "Redirecting responses (Reflect, Relay, spread) apply to collimated segments only; a cone is multiplied by each Transmit surface between the lens and a sample and blocked by everything else." At :117 append "(legacy only)".

**H-3. CAT-029 has no row for received ≤ emitted, and the fixture batch-k G6(c) proposes cannot be placed.** Three detectors "each 0.64 m off the axis" are 1.108 m apart centre to centre, but a detector plate is 1.4 m wide (CAT-038:27): the plates intersect. Replacement row F15, `FlashlightTests.ReceiversInOneConeNeverReceiveMoreThanIsEmitted`: "Given two broadband detectors side by side, plates touching, centres 0.70 m either side of the axis, sample plane 2.75 m from the lens (cone radius 0.737 m), then each reads 45.0 W ± 2 % and the two together no more than 90 W (unscaled shares 48.1 W each, sum 96.1 W, scaled by 90 ÷ 96.1). Control: one detector alone reads 48.1 W ± 2 %." The 6.8 % gap between scaled and unscaled is what the row excludes (created energy). State the distance datum (lens face, x 0.625) in the base fixture; "3 m in front" has none.

**H-4. CAT-059:27–:28 and probe row M10b (:83): the panel's source is "available while the receiver's level is on" with a bound that "follows its committed reading".** That is a receiver read directly by the supply solve, and it asks for a store-less source form that code does not have (`SupplySourceDescriptor(Id, Store)`, `ABS/SupplyContractValues.cs:39`; per-phase allowance = min(balance, power limit × phase), `SIM/WorkshopPhysicsEngine.Supply.cs:79`–`:82`). Judgement asked for by the brief:
- *The energy transfer is one general conserved-energy rule, not a second control coupling*, provided it is stated once: "a receiver's absorbed energy, times a declared efficiency of at most 1, is credited at commit to one declared finite store". It moves joules on the ledger the conventions already keep for absorbed light; it switches nothing. Owner decision 16's "only" governs what a receiver level may control, and nothing else in the batch needs an exception.
- *It is a second path today* because conventions item 9 and batch-k G1 give it two forms ("as its allowance for the next tick" for the panel, "into its finite store" for EL-154). Keep the store form only.
- *What the Solar panel should declare:* a receiver node (face rectangle, A = 1.8 m², nine samples of weight 1/9 on the face, all channels sensed, committed reading in watts); a conserved transfer of absorbed energy × 0.2 to store `cell` (capacity 18 W ÷ 120 Hz = 0.15 J, initial 0, power limit 18 W; credit beyond capacity is discarded, so at most one tick of output is kept and "nothing accumulates" stays true); an ordinary supply source `panel` on that store; a threshold signal node `available` over the mean irradiance (17.5 W/m², release ratio 0.9; ENGINE-SIGNAL-NODES); a supply contact with `SignalStateContactControl(available)` between `panel` and port `Supply`; twelve mark bindings on the committed reading. M10b becomes "narrower: a source store takes parameter ids only; fixed values and a conserved credit from a receiver are needed (ENGINE-OPTICAL)". Counts at :98 become 2 supply nodes, 1 signal node, 1 store. Output can then never exceed absorbed energy by construction. Add ENGINE-SIGNAL-NODES at :14 and :173. This settles the summary's open point: the 0.9 release ratio applies, as the threshold node's.

**H-5. CAT-059:81, :132, :150, :173 weaken `current-cat-059`** ("paid downstream work must obey admitted source law", `docs/planning/requirements.md:812`) by assigning it to EL-211 rows P5 and P6 and letting the package ship before ENGINE-SUPPLY-DEMAND. Drive supply is binary today, so a 120 W Motor would run from a 16 W panel. Make ENGINE-SUPPLY-DEMAND an unconditional prerequisite (decision 3a orders it first anyway) and move P5, P6 and P10 into CAT-059 as full rows: P5 granted energy ≤ 0.2 × 81.2 W × 5 s = 81.2 J; P10 two amber Lasers (42 W, 23.3 W/m², 8.4 W electrical) with one Laser (21 W, 11.7 W/m², two marks, no supply) as its control. :132 ("EL-211 lists the whole slice") leaves two authorities; CAT-059 is the one.

**H-6. EL-211 (Batch H) must be re-derived; CAT-059's arithmetic is right.** See section 6.

**H-7. CAT-008:131 (R8), :56, :140, :153(d): the 128-segment criterion of `current-cat-008` is asserted only as an upper bound, on a wrong estimate.** :56 counts live rays and concludes the cap may not be reached within 16 m. Recomputed with the spec's own 0.9 m spacing: one cavity round is 1.41 m and round j adds 2 × 2^j segments; 124 cavity segments exist after five rounds and round six would make 252, so the cap binds after about 7.8 m of cavity path, with every path at 13 interactions and each segment near 0.12 W (above the 0.001 W cut-off). As written R8 passes with no cap or a cap of 256. Replacement: "Given the Laser 2 m from a 45° feed splitter and, 0.9 m beyond it, a splitter square-on between two facing Mirrors 0.9 m either side, then the emitter's committed path has exactly 128 segments; every segment of depth d is present before any of depth d + 1; no segment carries more power in any channel than its parent; a second Run from Reset commits the same count and end points ± 0.02 m. Control: inner splitter removed, 4 segments." Delete :153(d) and the last two sentences of :56.

**H-8. CAT-008 and CAT-006:146 (R9): "a split or relay counts as one interaction" is never tested**, because no fixture reaches 16 (splitter cavity paths stop at 13; a combiner loop allows about three laps). Add to each, reusing the corridor of CAT-041:53: "Given the beam passed once through this part into the two-mirror corridor, then the path ends after 15 reflections; control: this part removed, 16 reflections (CAT-041 R8)". Names: `BeamSplitterTests.ASplitCountsAsOneInteractionOnEachBranch`, `BeamCombinerTests.ARelayCountsAsOneInteraction`. The count is exact (a declared limit).

**H-9. CAT-008:29: Split is declared on both large faces with no once-per-crossing rule, and "both continue from the hit point".** Applied at entry and exit it gives 0.25 transmitted and a spurious second reflection. Replacement: "The response is applied once per crossing of the pane collider, at the entry face. The transmitted segment starts at the exit point with its direction unchanged; the reflected segment starts at the entry point and ignores the pane that produced it. A declaration with ρ + τ > 1 in any channel is refused at bundle time; here ρ + τ = 1." Add "applied once per crossing; the continuing segment starts at the exit face" to the filters' `:29`.

**H-10. Gates `:61` (all five) and `readiness/batch-e.md` section 5: "min(30, 0.581 m² × 46.7 W/m²) = 27.1 W per channel".** 46.7 W/m² is the three-channel total; per channel it is 140.1 ÷ 9 = 15.57 W/m², so a control on the axis at 3 m reads 0.581 × 15.57 = 9.05 W per channel (27.1 W is the total). Still high, so the conclusion stands, but the number is wrong, it uses the deleted cap, and owner decision 9 has no row. Add G13, `OpticalGateTests.AFlashlightConeRaisesAControlByTheSameReceiverLaw`: "Given a Flashlight 3 m from control A on its axis (9.05 W per channel), B per the truth row, then A is high and the carrier follows the table; no cone light leaves the outlet. Control: the same torch at 7 m (140.1 ÷ 49 × 0.581 = 1.66 W) leaves A low." The switch distance is 5.71 m; both cases have margins above 25 %.

## 4. Medium and low findings

**M-1. CAT-036:20, CAT-029:20: "the Fan in its Electrical mode with an activation enable" is not delivered.** The Fan's `ActivationIn` is available only in Self-contained mode and its electrical input has `Enable = null` (`elements/Fan/FanElementDeclaration.cs:53`–`:60`). A latch enabling a wired consumer exists nowhere; proportional sharing (036:64) is proved only as a pure function (`ElectricalSupplyTests.cs:207`). Mark both "present, untested as used", first proved by L1 to L3 and F2, F7, F12, F13. CAT-036's count becomes 5/1/1/4 and its tier is engine unless ENGINE-OPTICAL's own tests cover both; the summary's "data-only if the probe passes" follows.

**M-2. CAT-036:89, :178, :180, :182** keep a three-package Transmit criterion in the Laser slice and say "G5 states no transmission value". G5 now names seven packages at τ (1, 1, 1) as criteria of ENGINE-OPTICAL. Delete the criterion and the three package edits from the Laser slice. `epics.md:1107` still owes the Funnel light proof "at Story 13.1"; repoint it to ENGINE-OPTICAL.

**M-3. CAT-029:154 (F8)** runs 150 s against the 30 s default. Add "needs a declared Run length of 150 s (owner decision 13; ENGINE-RUN-LENGTH)" and list ENGINE-RUN-LENGTH and ENGINE-CONTACT-OBSERVATIONS at :191.

**M-4. Energy-debit rows can fail on correct code.** CAT-036:145 (L7), :146 (L8), CAT-029:153 (F7) assert a one-sided exact bound ("125 J ≤ debit") on an f32 per-phase ledger. Replace with "debit within ± 1 % of 125 J, and emitted energy ≤ debit"; state that the 5 s window starts at the first lit tick. CAT-036:149 (L11) "endpoints exact" for an exponential follow becomes "within 1/255"; CAT-029:155 "store within 1 %" after Reset becomes exact (authored Reset state).

**M-5. Filters: no row proves a filter acts on cone light**, and 055:50, 031:50, 011:50 with Open question 2 say amber stands in "until the Flashlight exists" although the batch order delivers the Flashlight first. Add to each: "Fc | Given a Flashlight 3 m from a Light receiver, square to it, with this filter midway, then the reading has only the passed channel (14.8 W = 140.1 W/sr ÷ 9 m² × 0.950 m², ± 2 %) and the others are below 2 % of it; the matching colour receiver is matched; control: filter removed, three equal channels, colour receiver not matched (purity 33 %) | `ColourFilterTests.AConeIsFilteredLikeABeam` | stall guard 1 s".

**M-6. CAT-056, 032, 012: the torch case is negative only (R11).** Add R11b, `SelectiveReceiverTests.FilteredConeLightMatches`, run in the filters slice: "Given the Flashlight of R11 with a Red (Green, Blue) filter on the axis, then the reading is 14.8 W ± 2 % in the passed channel and the receiver is matched (5.9 times the default)". The rows of CAT-038 (R11) and CAT-068 (R11) already say a torch triggers them; the gates need H-10.

**M-7. CAT-072:155 against the old EL-149:61 (opposite outcomes at `threshold` 9.5 W).** CAT-072 is right for its fixture and the old line was right for a different one. R4 runs on the base fixture, direct lasers at (10, 10, 0) W: min 10 ≥ 9.5 W, matched; 10.5 W, dark from dark. Through a combiner the same pair is (9, 9, 0) W and is off at 9.5 W, which is what the old EL-149 row tested (the repaired EL-149:57 now defers to CAT-072, and the old text is no longer in the file). No outcome changes. Fix: name the fixture in R4 of CAT-072:155, 021:156, 040:156: "Given the base fixture (direct lasers, 10 W per required channel) and `threshold` 9.5 W … (through a combiner the same pair is 9 W and is off at 9.5 W; no row uses that)".

**M-8. CAT-055:128 (F4)** lists only the blue-filtered case; `current-cat-055` requires "green/blue-only … extinguish" and :139 cites a second case. Use the sibling form: "(amber through a green filter, (0, 7.8, 0) W, or through a blue filter, (0, 0, 3.2) W) … (theory)"; zero means below 0.156 W and 0.064 W.

**M-9. Harvest dispositions that contradict the reconciled text.** CAT-029:128 (fact 5) says "do not carry forward … its region test", but the spec carries the button-only result as the contact node's body binding (row F3): replace with "Carry forward the speed threshold and the button-only result, now the contact node's body binding; do not carry forward the callback." CAT-029:130 (fact 7) says "Carry forward, except the underside control (decision 3)"; the spec keeps the underside control: replace with "Carry forward, including the underside control (row F3)." Also CAT-029:124 (lens 0.66 is now 0.625; intensity 24 is now 420.4 W/sr), :125 (button travel is a body pose), CAT-036:119 (`ActivationIn` 0.52 is now 0.5), CAT-036:113 and CAT-029:121 ("transparent panes pass light" is now Absorb by default with declared Transmit).

**M-10. CAT-038:34, :29, :105.** Description "detects a beam" becomes "The front target detects light and closes an electrical contact while lit. Connect a separate battery supply; the light does not create electricity." "Strength rule Mean … purity 0 (none)" becomes "WeightedSum with weights (1/3, 1/3, 1/3) over the sensed set; no purity declared" (conventions item 8). CAT-059:33 "an active flashlight" becomes "an active light".

**M-11. CAT-041:49, :119, :124 and CAT-008:54, :120: "yawed 45°".** The stated normal (−0.7071, −0.7071, 0) is (−1, 0, 0) turned +45° about +Z; a yaw about Y would send the beam along ±Z. The arithmetic is right (d′ = (0, −1, 0)). Replace with "turned +45° about +Z (right-handed)"; R2's "yawed to 30°" likewise.

**M-12. CAT-006:28–:29, :80, :96: the shared outlet is not the conventions' Relay shape** (τ and mask on each outlet of one Relay). State it as declared instances: "An outlet is a declared node with a typed id that commits the per-channel sum of what leaves it; each inlet lists (outlet, τ, mask). Bundle check per inlet: Σ over its outlets of τ × mask = 0.9 ≤ 1 in every channel." The rule is per inlet; three inlets on one outlet conserve energy because each relays only its own input.

**M-13. CAT-006:146 (R9)** cannot show "each re-entry carries less power than the one before": the base fixture leaves 10.6 m at the outlet and a lap is about 7.6 m, so there is one re-entry. Replacement: "Given the Laser 1 m from the First input and three Mirrors returning the output to the Third input over a 6 m lap, then exactly two re-entries occur, the second carrying 0.772 ± 2 % of the first, and the third lap ends inside the cube."

**M-14. CAT-006:52 outlet rim is ochre `#e8b764`**; the catalogue text (:32), `current-cat-006`, `todo-299` and `DESIGN.md:409` say gold. Decide under the delegation; recommended gold `#f7cb52` (requirement wording governs). The gates have the same split (043:34 against :49). `EL-213:46` follows.

**M-15. Gates: the interaction budget has no row** (`requirements.md:715`, `:721`, `:733`: "preserved/remaining interaction budget"); G7 proves range only. Add to G7: "a carrier that reaches the gate after 15 interactions is absorbed at the limit; after 14 it exits."

**M-16. Gates G5 (hold band; 043:145 and siblings).** The numbers are right (3.5 × 0.9³ × 0.95 = 2.424 W). Not buildable as written: no hop lengths or range budget for one split, three Relays and a mirror between 1.4 m cubes; it does not say whether the Relays are combiners or open NAND/NOR gates; and the last clause changes the feed in mid-Run (make it a separate case). Its boundary cell should say what it excludes: a missing mirror loss gives 2.552 W and would raise a low control.

**M-17. Gate latency.** G3 in all five asserts one committed tick from control to carrier. That holds only if the threshold and boolean nodes resolve in one feed-forward pass at tick start; batch-k G3's "one step per tick around any loop" does not say so. Add to each spec's disqualifiers and to the story (section 11): "a feed-forward chain of nodes resolves within one tick; only a loop costs a tick."

**M-18. Buildability gaps.** Solar samples sit at x −0.13, 0.02 m in front of the face (−0.11): put them on the face. The P3 occluder is unspecified (one hidden sample column gives 0.670 of the clear reading: give the ball and position). The button spring's rest position is not declared (CAT-029:26; owner decision 6 requires a declared rest position). CAT-006:50 "0.30 m above its centre" is undefined on the top face. CAT-041:53: the corridor has 1 cm margins; widen the step. CAT-041:127 (R5) asserts a length that depends on the Laser's lens position; assert "the second segment ends on a collider of the Laser". Preview rows (F8, mirror R9, splitter R9, combiner R11) depend on the Build preview's collider source, which conventions item 12 leaves to ENGINE-OPTICAL: add it to each Disqualifiers list.

**M-19. Shared method names.** `ResetAndReloadRestoreTheClosedDarkState` is false for NAND and NOR: rename in all five to `ResetAndReloadRestoreTheDarkStartState`. `OnlyAnInletRoutes` (006:142, `EL-213:111`) becomes `OnlyAnInletRelays`. The receivers' blocking test has three names for one behaviour (056:159, 072:158, 068:162): use `BlockingAnyBeamDarkensTheReceiverUntilItIsCleared`.

**Low.**
- **L-1.** CAT-011:51 "at most one lossy part" and `batch-e.md` gap 9 "survives one mirror and no splitter" are wrong: 3.2 × 0.95⁴ = 2.606 W ≥ 2.5 W and 3.2 × 0.95⁵ = 2.476 W. Replace with "up to four mirrors, or a combiner and two mirrors (2.60 W); never a splitter".
- **L-2.** CAT-011 numbers its Reset row F10 where red and green use F9; make the splitter row the last id.
- **L-3.** CAT-021:157: the dark purity case is 2.5 W of red against a 2.22 W limit (88.9 %), the value the specs avoid elsewhere; use one splitter (5 W, 80 %).
- **L-4.** CAT-068:72, :181: one amber Laser satisfies White at the default; add to the Story 13.6 wording gap "all three channels, from one source or several".
- **L-5.** CAT-006:14 Status omits the Mirror that R9 needs. Gates `:15` cites "gap G6 of batch-k"; it is G9. Gates `:30` "purity 0" becomes "no purity".
- **L-6.** Gate G1's "zero" outcomes need a window ("zero in every committed tick of 1 s"); G11's ± 0.02 m needs its reason (section 5).
- **L-7.** Gate release boundary: off "below 2.25 W" against the legacy "drops at exactly 0.225". No row sits on 2.25 W and the requirement states only the two values; nothing fails.

## 5. One set of tolerances

Today: ± 2 % on collimated readings, ± 5 % on cone readings (CAT-029:143; R11 of all eight receivers), ± 10 % on an ease midpoint (038 R10), 1 s, 0.5°, ± 0.02 m. `batch-e.md` section 5 justifies ± 2 % as "a seventh of the smallest designed threshold margin", which is not true (the smallest margins are 3.0 % in gate G5, 4 % for the combiner mix after two mirrors, 4.8 % in R4 and R12). Keep the CAT values, as one set, with these reasons written once in each spec:

| Quantity | Bound | Gameplay consequence it excludes | Verdict |
|---|---|---|---|
| Power reading, beam or cone | ± 2 % | The smallest declared loss step is 5 % (one mirror); 2 % tells n lossy parts from n ± 1, and a missing 0.9 or 0.5 factor is at least 10 %. A wrong factor flips a detector, a gate or the panel's supply | Justified. Batch K's ± 1 % catches no further defect. Use ± 2 % for cone rows too: with one centre sample the reading is A × I ÷ d² exactly, and at ± 5 % rows P1 and F6 cannot tell the nine-sample mean (45.1) from the centre sample (46.7, + 3.5 %) |
| Stall guard | 1 s of game time | A light that never arrives; the response itself is one tick | Justified; a 12-tick guard is what the guidance warns against |
| Reflected direction | 0.5° | A beam aimed at the centre of a 0.55 m target misses at the 16 m range beyond atan(0.55 ÷ 16) = 1.97°; 0.5° is 0.14 m at 16 m, a quarter of the radius | Justified; add that sentence at CAT-041:123 and to L1, L6, F4. Batch K's 10° equals atan(0.55 ÷ 3), fitted to the fixture, and passes a law that misses by 2.8 m at range |
| Segment end point | ± 0.02 m | Under 5 % of the smallest aperture radius (0.43 m) and below the 0.03 m edge clearance of the gate rows | Justified; state it |
| Threshold brackets | set 5 % either side | Positions of designed cases, not tolerances | State rows assert state only |
| Exact | counts (128, 16), zero where no emitter is active, authored Reset values, typed refusals | Declared limits and intentional zero | Allowed by the guidance |

No row in the batch sits within 2 % of a threshold; the two-split 2.5 W and three-mirror 2.469 W cases are excluded from rows by name. Gate G3's one-tick assertion is feature-defined (every gate requirement row says "next-tick decision").

## 6. The Solar panel's numbers, and what `EL-211-solar-panel.md` must change

Recomputed from the conventions and CAT-059's declared geometry: Ω = 2π(1 − cos 15°) = 0.2141 sr; I = 90 ÷ 0.2141 = 420.4 W/sr; centre sample at 3 m 46.7 W/m²; side samples 45.8 and 45.2; corners 44.3; nine-sample mean 45.1 W/m²; absorbed 1.8 m² × 45.1 = 81.2 W; electrical 0.2 × 81.2 = 16.2 W. All nine samples lie inside the 0.80 m cone radius. The uncapped product passes 90 W at 2.84 m. Availability 17.5 W/m² (31.5 W absorbed, 6.3 W electrical), release 15.8 W/m²; on the axis the level is reached at 4.9 m (mean 18.4 W/m² at 4.75 m, 16.6 at 5.0 m). One amber Laser: 21 W, 11.7 W/m², two marks, no supply. A 1 kg load rises at most 16.2 ÷ 9.81 = 1.65 m/s. **CAT-059's 81 W and 16 W are right; batch-k G8's "about 82 W" is the same figure rounded up.** (The sampled product is the game law, not the exact flux through the rectangle, which is a few per cent lower; the conserved-share rule is what guarantees received ≤ emitted.)

| `EL-211` line | Change |
|---|---|
| :16, :167 | Add "blocked on the engine programme (owner decision 3a)", ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES; ENGINE-SUPPLY-DEMAND unconditional |
| :29 | Reading in watts absorbed (Ē = P ÷ 1.8 m²); shares conserved |
| :31, :83 | The declaration of H-4 (receiver, conserved credit to a finite store, ordinary source, threshold node, signal-state contact) |
| :33, :69, :151 | Availability Ē ≥ 17.5 W/m², release below 15.8 W/m² |
| :46 | Mark levels 4.38, 8.76, 13.1, 17.5 W/m² |
| :67 | Delete the 50 W/m² per unit constant; P_abs = A × Ē = 81 W at 3 m, never above 90 W |
| :68, :72, :150, :170 | 48 W becomes 16 W; 4.9 m/s becomes 1.65 m/s; "near 48 W" becomes "above the availability level" |
| :71 | Conventions item 4 |
| :97, :101 | Bound = 0.2 × absorbed energy; work bound N × 128 × (1 + K), K = 9 |
| :129–:138 | Delete the rows and cite CAT-059 (owner decision 16). P10 becomes two amber Lasers, with one Laser as its control |

Efficiency 0.2 can stay: a free rotor needs about 0.3 W and 6.3 W is offered at the availability level.

## 7. Rows as the acceptance authority: truth tables, purity, arithmetic

**Truth tables (five gates).** Correct in all five: G1 has the four input rows with the carrier present (0.9 × carrier or dark) and G2 the four without. AND passes 11; NAND 00, 01, 10; NOR 00; OR 01, 10, 11; XOR 01, 10. Start and Reset state is the operation's value for two low inputs (AND, OR, XOR closed; NAND, NOR open) at `:36` and row G9. No copy error in operation, package id or closure id. The family is value-only.

**Purity row, per receiver colour** (limit 0.9; all recomputed):

| Receiver | Row | Matched case | Dark case |
|---|---|---|---|
| Red, Green | R5b | 10 ÷ 10.8 = 92.6 % | 10 ÷ 11.6 = 86.2 % |
| Blue | R5b | 10 ÷ 10.975 = 91.1 % | 10 ÷ 11.95 = 83.7 % |
| Yellow | R5 | 20 ÷ 21.6 = 92.6 % | 20 ÷ 23.2 = 86.2 % |
| Cyan | R5 | 20 ÷ 21.25 = 94.1 % | 20 ÷ 22.5 = 88.9 % (L-3) |
| Magenta | R5 | 20 ÷ 21.95 = 91.1 % | 20 ÷ 23.9 = 83.7 % |
| White | R5 | always met; (40, 10, 10) W still matched | cannot reject (declared, D18) |
| Broadband | none | no purity declared | n/a |

Amber alone gives 47.6, 37.1, 15.2, 84.8, 52.4 and 62.9 % for the six coloured members, as the specs say. The release-hold row (R12) exists in all eight and is passable; the mid-Run Battery disable it needs is delivered and tested (`elements/Battery/BatteryElementDeclaration.cs:48`; `SwitchElectricalTests.SupplyLossAndRestorationDoNotEraseOrReplayImpactMemory`). CAT-038 R4b: amber mean 7.0 W; half 3.5 W (40 % above 2.5 W), quarter 1.75 W (30 % below), preserving the requirement's 0.35 and 0.175.

**Arithmetic recomputed, one row or more per spec.** Right: CAT-036 debit 25 W × 5 s = 125 J and 0.14 m at 16 m; CAT-029 420.4 W/sr, F3 drop speeds 1.90 and 2.10 m/s, F14 14.8 W and 2.37 W; CAT-059 section 6; CAT-038 hold band from 6.63 W, 2.5 W mean at 7.3 m; CAT-056 limit 1.11 W; CAT-032 3.9 W and 1.95 W; CAT-012 band from 3.02 W; CAT-072 (9, 7.02, 0) W and limit 2.22 W; CAT-021 2.88 × 0.95 = 2.736 W, × 0.95³ = 2.469 W; CAT-040 limit 1.47 W; CAT-068 2.88 ÷ 2.7 and ÷ 3.1; CAT-055 (10, 0, 0) W, 47.6 %; CAT-031 (0, 7.8, 0) W; CAT-011 3.04 W and 2.88 W; CAT-041 reflection vector, 0.95¹⁶ = 0.440, path 8.6 m; CAT-008 half 3.5 W and quarter 1.75 W means; CAT-006 (9, 7.02, 2.88) W, paths 1.40 and 0.990 m, lap 0.772; CAT-043 2.424 W; CAT-044 two gates in series (8.1, 6.318, 2.592) W; CAT-045 aperture 0.581 m²; CAT-046 range 9.01 m; CAT-047 output mean 6.3 W. Wrong: the gates' cone figure (H-10), CAT-008's cap estimate (H-7), CAT-011's "one lossy part" (L-1).

## 8. Art against visible = solid

Every art node in the 22 specs is classified and says what moves it; no static proud visible solid lacks a collider; the specs agree with Batch K's K-18 and with the repaired geometry of EL-143, EL-146, EL-210, EL-212 and EL-213. Bases are raised to touch (Laser −0.425, torch −0.30, receivers base top −0.70, filters and splitter −0.83, mirror −0.775, combiner and gates −0.70). Rims, lenses, target discs, cells, marks, bars and truth prints are flat prints; panes are box colliders carrying the response; the mirror is one cylinder with a printed rim; the torch's base is solid and its button is its own body. Defects:
- CAT-059: the `PortSupply` sphere (radius 0.085 m at x 0.2) floats 5 mm off the back face (x 0.11); move it to x 0.195.
- CAT-029: `PowerStud` is visible in Self-contained mode with no port; say it is solid in both modes or bind its visibility to the choice. Stud placement differs between Laser (tangent to the face) and torch (centred on it); pick one.
- `DESIGN.md` still says "raised bars" (:402), "raised marks" (:110), "coating boundary" and "paired gold studs" for the splitter (:348), "ochre rim" (:349) and "gold-rimmed outlet" (:409). These are outside the eight places of owner decision 4; batch-k G12 has the replacement sentences. Add for the splitter: "The ring is a printed mark; the whole pane splits. The two gold dots are printed."

## 9. The summary table (`readiness/batch-e.md`) against the specs

- "Other owners: none" and "Prerequisites" omit ENGINE-SIGNAL-NODES (eight receivers, five gates, Solar panel) and ENGINE-SPATIAL-QUERIES (all); the Flashlight row omits ENGINE-RUN-LENGTH; the Solar row makes ENGINE-SUPPLY-DEMAND conditional (H-5).
- Counts change with X-2 and M-1: receivers 6/1/2/5 as Batch K has; gates lose "logic operation" and gain three rows; Laser 5/1/1/4 with tier "engine".
- "Conformance: 22 Conform" should read "22 Conform with fixes" until X-1 to X-4 are applied.
- Section 5 "about 27 W per channel" (H-10); gap 9 "one mirror and no splitter" (L-1); the tolerance reason "a seventh" (section 5); "whether the Solar panel should be exempt from the 0.9 release ratio is undecided" (settled by H-4).
- Section 2 rows "Tick order follows D7 … Phase 2" and the receiver row's direct contact are superseded by owner decision 16.
- "No spec was re-read end to end": confirmed by X-1, X-4 and X-5, which are uniform leftovers of the first pass.

## 10. Cross-batch conflicts

1. **Batch H, EL-211:** every number and the source design (section 6; H-4, H-5).
2. **Batch K conventions (`EL-176` section 10) need four edits found here:** (a) item 9 and G1: one transfer form, "credited at commit to one declared finite store", deleting "as that source's allowance for the next tick"; (b) item 3: say what a Split does to a diverging segment (CAT-008:53 says "receives half of each channel"; recommended "a Split multiplies a diverging segment by its τ; the ρ share is absorbed and ledgered"); (c) item 5: "a relayed segment ignores the collider carrying its outlet at its origin" (an outlet on another face of the same cube; CAT-006:80 needs it); (d) G6(c): the three-detector fixture cannot be placed and its 42 W is 41.5 W (H-3).
3. **Batch K closures:** `EL-213:46` rim colour and `:111` method name follow M-14 and M-19; `EL-138:141` the Reset method name.
4. **Batch C:** `readiness/batch-c.md:76` and CAT-007 state the tick order and the Optics category owner differently; both follow owner decision 16 and K-16.
5. **Fixtures behind shared test names** still differ between CAT rows and what the closure specs describe in prose; with the EL rows deleted this is prose only and needs no row change.

## 11. Engine-story gaps, with proposed text

- **ENGINE-OPTICAL** (`_bmad-output/planning-artifacts/epics.md:4434`): adopt batch-k G1 with the edits of section 10 item 2, and add: "an outlet is a declared node that commits the per-channel sum of what leaves it; the bundle refuses, per inlet, a Relay whose outlets' τ × mask sum above 1 in any channel"; "a segment-limit qualification scene commits exactly the declared number of segments, kept breadth-first by depth, then emitter and surface order"; "each delivered package that declares Transmit is listed and tested (G5), so no element slice edits a delivered package for light".
- **ENGINE-SIGNAL-NODES** (owner decision 3a; not yet in `epics.md`): add to batch-k G3: "a feed-forward chain of nodes resolves within one tick's resolution; only a loop costs a tick"; "a threshold node presents its level as a contact-controllable state" (`SignalContactState` has only `Latched` and `TimerCounting`, `ABS/SignalContactControl.cs:5`); "a node's level is readable by `PresentationDriver.CommittedActivation`".
- **ENGINE-RUN-LENGTH** (owner decision 13; not yet in `epics.md`): first consumer in this batch is CAT-029 F8 at 150 s.
- **ENGINE-SPATIAL-QUERIES:** batch-k G2 as written.
- **Stale Chrome criteria in Epic 13.** `epics.md:4156` (13.1), `:4225` (13.2), `:4255` (13.4), `:4271` (13.5), `:4287` (13.6), `:4303` (13.7), `:4319` (13.8) each end "`tools/e2e/….test.ts` passes 100% in Chrome serially"; Story 13.3 has the same form. Under the browser deferral no slice may gate on them. Replace each with: "**And** the native rows of the CAT specs pass in the full local round; browser proof is queued for the post-catalogue E2E phase." The specs' own "Chrome recipe (pending, post-catalogue)" lines are correctly labelled.
- **Story wording** (requirement rows govern): 13.1 "infinite collimated beam" and "35° cone" become "16 m range" and "15° half-angle, 8 m range"; 13.5 "exact 90°" and "50/50" become "about the mirror's actual normal (within 0.5°)" and "halves each channel (within 2 %)"; 13.6 "requires all three primary colors simultaneously" becomes "all three channels, from one source or several"; 13.7 "immediately cuts motor power" becomes "cuts supply once the light falls below the release level, from the next tick"; 13.8 "emitted output laser beams" becomes "relay a separate carrier at 90 % while the operation holds".
- **Requirement wording:** `sequence-task-398` as batch-k G13 (owner decision 9).

## 12. Items for the owner

1. **Whether owner decision 16's "only" excludes the conserved transfer of absorbed energy to a supply store** (Solar panel, EL-154). Recommended: it does not; it is one conserved-energy ledger rule in a single form (credit to a declared finite store, efficiency at most 1), separate from control coupling, and availability stays on the signal path (H-4). If it is excluded, both parts need a different design. Blocks CAT-059, EL-211 and EL-154.

Decided here under the standing delegation (overridable), not owner items: one tolerance set (section 5); gold outlet rims (M-14); Split on a cone (section 10); the store form of the transfer.

## 13. Not verified

- Nothing was run; every optical row is a design, and no optical code exists.
- Ten sibling specs were read as complete differences from their lead, not line by line; within CAT-040 the repeated boilerplate lines (threshold range, release ratio, tick order, open-question items) and rows R6 to R10 were taken as identical to CAT-072's, and the legacy harvest of CAT-032 and CAT-012 was not read.
- Legacy `@a6c914e` citations and ledger rows.
- The exact text of the old EL-149:61 (taken from the Batch K audit's quotation).
- Stability of the 0.05 kg button, whether a contact node compiles on a child body, whether intra-package collision can be disabled, how `ExponentialFollow` encodes a rate, and whether `CommittedActivation` can read a threshold node.
- Whether parts may be placed closer than 0.9 m (H-7 uses the spec's own figure); whether a scene-extracted cylinder with its axis along X is covered (the cited test proves a Z-axis one).
- Motor loss figures beyond the 0.05 N·m in the declaration; palette orders (none chosen).
