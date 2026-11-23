# Audit: element specs, Batch O (Radiation; 26)

Independent read-only audit, 22 November 2026, checkout `spec-drafts` at `8d1d68aa` plus drafts. Nothing was built or run. All 26 specs were read in full. Code citations were opened for every "present" row of eleven specs (EL-126, EL-129, EL-131, RAD-02, RAD-06, RAD-08, RAD-15, RAD-17, RAD-20, RAD-21, RAD-22) and spot-checked in the rest. Spec paths are under `docs/planning/elements/`. `SIM/` = `CuriousContraptions.Simulation/`, `ABS/` = `CuriousContraptions.Abstractions/`, `BUN/` = `CuriousContraptions.Bundle/`.

## 1. Verdicts

0 Ready, 21 Ready with fixes, 5 Not ready. Conformance: 0 Conforms, 26 Conform with fixes. No spec asks for element-specific engine code, and no Batch O package is delivered; a search of `ABS*`, `SIM/`, `BUN/`, `engine/` and `elements/` finds no radiation symbol, so there is no engine refactor to record. Every spec already says "blocked on the engine programme (owner decision 3a, 22 November 2026)" in Status and Prerequisites. X-1 to X-4 (section 3) apply to every spec.

| Spec | Verdict | Conformance (addendum point) | Reason |
|---|---|---|---|
| RAD-08 Rate meter | Not ready | with fixes (2) | Owns section 10, which has C-1 to C-12; own rows need M-1, L-1 |
| EL-129 Sheet gauge | Not ready | with fixes (1, 5) | Supply graph is refused by the delivered bundle (H-1) |
| EL-130 Level gauge | Not ready | with fixes (1, 5) | Inherits H-1; may ship without its requirement rows (M-6); Run length |
| RAD-20 RTG | Not ready | with fixes (2, 6) | Cooling fixture cannot be built (H-2); unpaid heat (C-9); two owners (H-3); Run length |
| RAD-21 Material latch | Not ready | with fixes (2) | Release edge is refused by delivered code (H-4) |
| EL-126, EL-127, RAD-18 | Ready with fixes | with fixes (7) | Headline rows queued (M-6); tick wording |
| EL-128 Dense shield | Ready with fixes | with fixes (2) | Default row undermines "strongest shield" (C-7) |
| EL-131, EL-132 | Ready with fixes | with fixes (2) | Coupling rewrite (C-3); EL-132 numbers depend on C-6 |
| RAD-01 Gamma capsule | Ready with fixes | with fixes (2) | Contact reading wrong (C-8); unpaid source (C-9); 60 s row |
| RAD-02 X-ray emitter | Ready with fixes | with fixes (2) | Tick order and grant wording (C-1); loss not ledgered |
| RAD-03, RAD-04 | Ready with fixes | with fixes (6) | Work-bound wording; C-9; RAD-04 counts a RAD-14 capability |
| RAD-06 Shutter | Ready with fixes | with fixes (2) | Coined story name (M-9) |
| RAD-07 Collimator | Ready with fixes | with fixes | Wrong test evidence, open fallback (M-10) |
| RAD-09 Dosimeter | Ready with fixes | with fixes (2) | Coupling (C-3); dose unit (C-10); 120 s row |
| RAD-10 Cargo badge | Ready with fixes | with fixes (2, 5) | Goal qualifier in the wrong story (H-5); Omni undefined for beams (C-5) |
| RAD-12 Scintillator | Ready with fixes | with fixes (2) | Receiver-paid emitter is a third mechanism (H-6); thin light margin (M-3) |
| RAD-13 Decay capsule | Ready with fixes | with fixes (2) | Rows exceed 30 s with no Run length; energy bound fails as sampled (C-9) |
| RAD-14 Deflector | Ready with fixes | with fixes (2) | "Enabled by a supply grant" (C-3); fixed outlets jump the beam (M-4) |
| RAD-15 Track chamber | Ready with fixes | with fixes (2) | Three memory nodes set by receiver levels (C-3) |
| RAD-16 Neutron module | Ready with fixes | with fixes (2) | Unpaid emitter (C-9); preview owner |
| RAD-17 Moderator tank | Ready with fixes | with fixes (3) | Colliders overlap (M-5); fluid port owner; Run length; C-6 |
| RAD-22 Tracer capsule | Ready with fixes | with fixes (3) | Flat band on a ball (M-7); fixture geometry (M-8) |

## 2. Findings on the radiation conventions (`RAD-08:178-206`, `readiness/batch-o.md:46-61`)

Radiation was written on the Optical batch's first conventions. The repaired text is `EL-176:167-182`. Each item gives the replacement.

### High

**C-1. Tick phase is the unrepaired one.** `RAD-08:203`, `:111`; `batch-o.md:58`; repeated at RAD-01:101, RAD-02:99, :103, :104, RAD-03:153, RAD-06:104, RAD-07:99, RAD-09:101, RAD-10:101, RAD-12:99, RAD-13:100, RAD-14:103, RAD-15:104, RAD-21:104, RAD-22:96, :99, EL-126:104, :163, EL-129:95, EL-130:76, RAD-17:101, :158. No grant exists before the substeps (`SIM/WorkshopPhysicsEngine.cs:1011`, `SIM/WorkshopPhysicsEngine.Supply.cs:72-107`). Replacement for item 9: "One solve per 120 Hz tick, after the last substep and before commit (optical convention 4, owner decision 16). It reads the energy granted to each emitter over this tick's substeps, this tick's final collider poses and liquid levels, and signal levels as resolved at the start of this tick. Readings, strengths and accumulators commit with the tick and act from the next tick." Per spec: replace "pose committed by the previous tick" with "this tick's final pose", "Phase 1 of tick n + 1" with "the signal network of tick n + 1", and RAD-02's "(granted ÷ demanded)" with "energy granted over the tick's substeps ÷ energy demanded". The one-tick latency every row states is unchanged; RAD-12 stays one tick to light, two to load.

**C-2. Conservation is per receiver, not shared.** `RAD-08:200` ("min(P, A × Σ wᵢEᵢTᵢ)"), `:202`; RAD-20:101 uses the same form. Replacement: "For each diverging segment the solve sums the uncapped shares A × Σ wᵢEᵢTᵢ of all receivers; if the sum exceeds the segment's power, every share is scaled by power ÷ sum (optical convention 8)." Add one row to `RadiationRateMeterTests`: receivers surrounding a source inside 1 m read at most its power in total. No hand-calculated row in the batch changes (the largest single share is 0.9 W of 125.66 W).

**C-3. Three couplings where owner decision 16 allows one.** Radiation's "level sets and clears a signal memory node" (`RAD-08:33`, `:75-77`, `:200`) is the third coupling the Batch K audit named. A memory node does not fit: its inputs are Set and Reset pulses, hysteresis would be held twice (release ratio and latch), and a contact on it names `SignalContactState.Latched` (`ABS/SignalContactControl.cs:5`), which a level is not. Replacement for item 6: "A receiver commits a continuous reading and strength. Its level is a signal threshold node over that committed scalar with a declared release ratio (ENGINE-SIGNAL-NODES). Supply contacts use the delivered signal-state control; a relay or emitter enable names a signal level; logic is a boolean signal node. Nothing reads a receiver directly." Per spec:
- RAD-08:33, :34, :75, :77; EL-131:24, :42; EL-132:22, :105: `detect` becomes the threshold node; `ActivationOut` is the pulse on its rise (edge row).
- RAD-09:30-32, :61, :67 and RAD-21:33: the accumulator publishes a committed scalar; a threshold node over it replaces "a threshold level that sets a signal node".
- RAD-15:22, :31, :65, :71: three threshold nodes over three committed strengths.
- RAD-14:30-31, :69, :101: "`energised` is a threshold node over the coil consumer's committed grant (on at 6 W, release 0.75); the relay's enable names `energised`", not "enabled by a supply grant".
- EL-129, EL-130: see H-1.
- RAD-02's Set/Reset memory node is a legitimate memory node and its level fits "enable names a signal level"; say so at `RAD-02:32`.
ENGINE-SIGNAL-NODES must state which contact state a threshold node presents to `SignalStateContactControl`.

**C-4. The query is the same one, but three rules are missing.** Optical convention 5 already serves radiation: ordered crossings with entry and exit, transmittance τ × exp(−μ × chord). Radiation's item 4 and 10 name "the segment query" and "shadow queries"; reword both to "the segment cast of ENGINE-SPATIAL-QUERIES returning ordered crossings (entry, exit, collider)". Missing:
- (a) **Default surface τ for radiation channels.** The optical default response is Absorb (τ = 0). Add: "for radiation channels every collider's surface τ is 1 unless declared; attenuation is the row's coefficient × chord only". Without it every pass-through row (RAD-03:63, RAD-04 B2, RAD-22 A3, every shield) reads zero.
- (b) **Overlapping colliders.** Item 4 says "these specs declare attenuating colliders that do not overlap". They do: the meter's own socket spheres sit in its case (`RAD-08:30`), RAD-17's mouth tube enters its base (M-5), and delivered parts overlap freely. Add: "where chords of several colliders of one body overlap, the overlapped length is counted once, with the largest coefficient per channel".
- (c) **Own body.** Optics says a sample is unoccluded when the first absorbing crossing is the receiver's own aperture face; radiation says the receiver's body is skipped. One rule: "crossings of the receiver's frame body are skipped for radiation channels".

**C-5. Omnidirectional receivers: the request.** `RAD-08:200`, used only at RAD-10:31, :68, :94. Batch K left it for Radiation. Text for ENGINE-RADIATION, element-free: "A receiver declares its facing rule as a closed set: Lambert (facing = max(0, −d̂·n)) or Uniform (facing = 1 for every incident direction; the declared area is an effective cross-section; sample points are points on the frame body). A collimated segment is credited to a Uniform receiver when it crosses the aperture rectangle from either side." RAD-10's BetaMinus weight of 0.5 is otherwise undefined.

**C-6. The conversion law cannot be computed from the text.** `RAD-08:185`, `:196`; RAD-17:93, :96; EL-132:63; RAD-18:68; EL-128:72. The specs use slow gained = 0.8 × (1 − e^−1.2) = 0.559 (0.493 W). If converted power is attenuated by the liquid's slow coefficient along the rest of the chord the gain is 0.8 × 2.4 × (e^−0.15 − e^−1.2) ÷ 2.1 = 0.512 (0.451 W), 8.5 % lower against ±3 %. Also "2.4, of which 0.8 converts" reads as a coefficient but is used as a fraction. Replacement: "A row may declare one conversion (from channel a, to channel b, fraction m ≤ 1). Per crossing of length L: out_a = in_a·e^(−μ_a·L); out_b = in_b·e^(−μ_b·L) + m·in_a·(1 − e^(−μ_a·L)). Converted power is not attenuated again inside the crossing that produced it. out_a + out_b ≤ in_a + in_b." The Liquid row then reads "NeutronFast 2.4; conversion NeutronFast → NeutronSlow, m = 0.8". Requirement radiation-17's "slowdown is not absorption" and "documented losses" are met (20 % of removed fast power is lost).

**C-7. The Ordinary default row makes delivered parts as good a shield as the Dense slab.** `RAD-08:189`, `EL-128:31`, `:67`. Recomputed for PhotonMedium (1.5 /m): Dense slab 20 × 0.05 = 1.0, T = 0.368. Wall collider 3 × 2 × 0.25 m (`elements/Wall/wall.tscn:19`): across e^−0.375 = 0.69, end-on e^−4.5 = 0.011, better than four Dense slabs (0.018). Bowling ball chord 0.56 m: 0.43. The default is neither a perfect nor a zero shield, but lesson 104 ("one slab too little, two enough") is solved by one Wall turned end-on. Recommended row, photons ÷ 10: (0.4, 0.15, 0.075, 400, 150, 0.2, 0.2): Wall across 0.96, end-on 0.64, ball 0.92; no hand-calculated row in the batch changes except the meter case's own loss. Also `RAD-04:80`: Polymer's BetaMinus coefficient equals Ordinary (150), so any 0.05 m part stops beta as well as the Polymer shield; raise Polymer to 300 or accept and say so. Requirement `radiation-foundations` forbids a silent fall-back to "a default material": state "the default row is one declared value of the scene, listed and tested by ENGINE-RADIATION; an undeclared channel on a declared row is refused at bundle time".

**C-8. Inside 1 m the reading rises with distance.** `RAD-01:95`, `:116` (A8), `RAD-08:184`. The floor clamps the denominator but not the Lambert facing. Face touching the capsule (0.15 m): edge samples d 0.180, facing 0.832; corners d 0.206, facing 0.728; mean (10 + 4 × 8.32 + 4 × 7.28) ÷ 9 = 8.04 W/m², R = 0.724 W, not the 0.882 W that A8 asserts at ±3 %. So a reading goes from 0.72 W at contact up to 0.88 W at 1 m, against radiation-01's "moving farther lowers the reading". Decision for item 3 (recommended): "inside 1 m the sample term is I ÷ 1 m² with facing 1 for a front-side source", which makes the reading flat at 0.90 W inside 1 m; then A8 reads 0.90 W. Alternative: keep the law and change A8 to 0.724 W "never above the 1 m reading".

**C-9. Fixed-strength sources are unpaid power.** `RAD-08:183`; RAD-01:30, :33, :98; RAD-03:30-32; RAD-04:26; RAD-16:30, :32; RAD-22:33-35; RAD-20:30, :69, :102. A capsule is 125.66 W from a store "not debited", more than the 120 W Heating plate; RAD-20's core is 20 W for a 600 s row. This contradicts optical convention 2 (emitted ≤ energy paid), thermal T4 (`TH-04:196`, "nothing creates heat without a paid store"), T13 and IX-16. The owner's decision fixes strength within a Run; it does not exempt the ledger. Replacement for item 2: "Every source debits a declared finite inventory in joules. A decaying inventory emits remaining × (1 − 2^(−dt ÷ T½)) each tick, so its total can never exceed the inventory P₀·T½ ÷ ln 2. A steady inventory (half-life none; owner decision, RAD-01) emits constant power and holds P₀ × the maximum Run length set by ENGINE-RUN-LENGTH, so no admissible Run spends it. Reset restores it." This also repairs RAD-13:97, :113 (A6): with power sampled per tick the sum is P₀·T½ ÷ ln 2 + P₀·dt ÷ 2, about 1.05 J over the asserted bound. With this text IX-16 holds as written for all five steady sources, the "narrowing" at RAD-03:97, :131, RAD-04:84, :114, RAD-22:99, :132 and `batch-o.md:92`, `:126` is deleted, and no owner item remains. RAD-20: "`core` is credited 20 W from a steady inventory", and the proposed ENGINE-THERMAL line "constant power … fixed for the Run" becomes "paid from a declared finite store".

### Medium

**C-10. Dose is not comparable between parts.** `RAD-08:182`, `:201`. Dose is watts at a declared aperture × time, so one field gives three rates at 1 m from 10 W/sr: dosimeter 0.09 m² → 0.882 J/s (mark 1.6 J, 1.81 s); badge 0.0225 m² Uniform → 0.225 J/s (0.3 J, 1.33 s); latch 0.04 m² → 0.396 J/s (1.0 J, 2.52 s). A dosimeter therefore cannot calibrate a badge budget or a latch dose, though RAD-10:17 calls it the counterpart and RAD-21:147 makes it a prerequisite. Watts and joules are internally consistent and conserve energy, so keep them for the ledger; decide for item 7 (recommended): "the three accumulating receivers declare the standard 0.09 m² area", and re-derive RAD-10's band and RAD-21's 1.0 J. Alternative: dose in J/m². "Exposed" reads only committed strength (≥ 0.001 W) and the committed accumulator: sound.

**C-11. Work bound.** `RAD-08:204`. A pass-through collimated ray is one cast returning every crossing, not 16 queries (RAD-03:153, RAD-04:129); 16 is the relay interaction limit. Replacement: "At most D × K shadow casts and C × L segment casts per tick: D diverging emitters, K receiver sample points in the scene, C collimated emitters, L the optical segment limit; each cast costs what the spatial query's cast costs. Conversion creates no segment. ENGINE-RADIATION states the cap on crossings returned by one cast and what happens at the cap (the remaining path is treated as blocked), with a measured before and after on a fixed reference scene."

**C-12. Excluded physics is stated only in EL-128.** Buildup and scatter are excluded at `EL-128:69`, `:108`. Add to item 4: "No buildup factor and no scattered component: a channel's power only falls along a path, except by a declared conversion." Inverse-square is stated (item 3). Requirement rows are still met: radiation-05 (S2, S3, S4, S9), radiation-07, IX-15 ("blocked transmission cannot activate a downstream detector").

**Checked and sound.** 0.882 W at 1 m (nine samples); Ω and W/sr values; one law for every channel; rows as package data with no engine row names; radiation-03 is met (Thin screen e^(−400 × 0.02) = 0.000335 W; "extra separation" by the 2 m range) and radiation-04 (thin e^−0.4 = 0.670 W passes, polymer e^−7.5 = 0.00055 W). S605's alpha row need not be "corrected": "thin passes, thick stops" is true of thickness (0.002 m Ordinary passes e^−0.8 = 0.449 W); add that law-scene row to `AThinScreenStopsAlpha` and withdraw `batch-o.md:126` G11.

## 3. Other findings

### Every spec

- **X-1.** C-1 wording (list in C-1).
- **X-2. Run length (owner decision 13) is named nowhere** (0 hits for "Run length" or `ENGINE-RUN-LENGTH` in 26 files). Rows over the 30 s default (`ABS/CadenceValues.cs:77`): RAD-01 A5 (60 s), RAD-03 A9 (60 s), RAD-09 D12 (120 s), RAD-13 A1 (48 s), A2 (38.6 s), A6 (60 s), RAD-20 G1 (63 s), G3 and G7 (600 s). Each spec adds "Run length needed: N s" and, where N > 30, the prerequisite `ENGINE-RUN-LENGTH`. RAD-01 and RAD-03 should shorten to 25 s. RAD-13: 60 s for rows; levels 123 and 124 declare 45 s for the 16 s preset. RAD-20: shorten (core 3 J/K gives a 6.4 s time constant) or declare 90 s. RAD-17 and EL-130 fit 30 s: the tank drains in about 5 s (recomputed 2 × 0.25 × √0.70 ÷ 0.08349 = 5.01 s), so W2, W3, W5 to W7 and lessons 126 and 128 must say "mouth joined to a closed Tap".
- **X-3.** Twelve specs list "ENGINE-THERMAL (the story's stated order)" as a prerequisite though none needs it (EL-126:151, EL-129:139, EL-131:113, RAD-01, -02, -03, -08:160, -09, -10, -13, -16:132, -22). Replace with "no dependency on ENGINE-THERMAL (status line of `epics.md:4512` to be amended)".
- **X-4.** `ABS/PortDescriptor.cs:54` is a comment; the record is at `:55`.

### High

**H-1. EL-129:35, :56-57, :113; EL-130:22 — the gauge supply graph cannot compile.** `BUN/SupplyBundleValidation.cs:142` refuses a port bound to more than one endpoint, so "`PowerIn` → both contacts" is refused; `engine/gpu/SupplyRoute.cs:375` powers a logic node only from its own `PowerIn` terminal, which the spec leaves unwired; G10's "typed refusal" is an `ArgumentException` (`SupplyRoute.cs:119`). Replacement (also decision 16): "One receiver commits one strength. Two threshold nodes over it: `valid` (0.05 W, release 0.75) and `clear` (the parameter, release 0.97). One boolean signal node `window` = `valid` AND NOT `clear`. `PowerIn` → contact `relay` with `SignalStateContactControl(window)` → `Supply`." Delete R7, G10 and "several rows on one aperture"; `ResultLamp` follows the committed `window` level; counts become 3 signal nodes, 1 supply node; edit `batch-o.md` O-7.

**H-2. RAD-20:28, :95, :107 — the cooling fixture cannot be built.** The cold plate is a vertical +X face; TH-10 is a dynamic 1 kg body whose thermal face is the underside of its foot (`TH-10:27`, `:32`), so it falls away. Replace with "Cold plate box 0.60 × 0.04 × 0.60 m at (0, 0.47, 0) on the casing's top; its +Y face is the contact face", and move the port. TH-10's ambient conductance is 5.5 W/K (`TH-10:37`), not 5: still air T_h 330.7 K, T_c 291.4 K, P 0.56 W. The contact-face design itself is right: no `ThermalCold` port remains, so `batch-n.md:148` is stale.

**H-3. RAD-20:66, :72, :145 against `TH-30:16`, `:73` — two owners for the store-less supply source** (ENGINE-THERMAL here, ENGINE-OPTICAL in TH-30). Use TH-30's owner and add it to Status and Prerequisites; the summary's "ENGINE-THERMAL only" is then wrong. Add ENGINE-CONTACT-OBSERVATIONS (contact link). Reuse T12 tolerances (`TH-04:204`) instead of ±10 %, 5 % and ±0.5 K (`RAD-20:107`, `:114`, `:119`). "Useful output" is 0.56 W into a 5 W lamp; recommended starting values 60 W source, 5 W cap (T_h ≈ 417 K, P ≈ 4.0 W).

**H-4. RAD-21:33, :67, :104, :150 — a memory node cannot feed a physical request.** Verified: a Memory output is a Level and a PhysicalRequest input a Pulse (`ABS.Signal/SignalContracts.cs:19-22`), `CanConnect` needs equal kinds (`:29-37`), and `BUN/SignalBundleValidation.cs:65-67` refuses the edge. No delivered package declares an internal signal edge and no test builds one; the target admits only a prismatic retention or an actuator (`ABS/PhysicalRequestValues.cs:6-21`). Replace U1 ("present, untested") with "M4 | a pulse on the rise of a level node, routed by a package-internal edge to a physical-request input | **missing** | owner ENGINE-SIGNAL-NODES (edge row)"; counts 5/0/3/4; `:33` becomes "`dose` is a committed scalar; `spent` is a threshold node over it (1.0 J); `release` is a `SignalPhysicalRequestDescriptor` fed by the rise of `spent`". Review tier: engine unless both stories land first. Against requirement radiation-21 the redesign holds: enough exposure releases, shielding retains, Reset restores; "weakens" is a step with cosmetic cracks, which the row itself calls authored fiction. Add behaviour above the 40 N·m holding limit and a boundary row.

**H-5. RAD-10:69, :135, :144 — the goal qualifier is put in ENGINE-RADIATION.** A field of the goal condition model belongs to the goal slices (owner decision 12). ENGINE-RADIATION only publishes the accumulator as a committed scalar. Owner: `engine-goal-count-window-hold`, text: "a condition may require a named committed scalar of its subject to lie in an authored closed band at the condition's occurrence; a failed qualifier leaves the condition unmet and reports the failed state". RAD-10 must name the five slices, not four. `:135` cites a "required-tag qualifier" in `readiness/batch-m.md`; no such text exists; delete. `:101` drops "pose committed by tick n − 1" so capture and accumulator commit on one pose.

**H-6. RAD-12:22, :31, :67 — an emitter paid from a receiver's strength is a third payment mechanism.** `EL-176:172` allows a supply consumer target or a declared store; `EL-176:179` lets a receiver pass absorbed energy × efficiency to a supply source as next tick's allowance. Rewrite: "The tile's collider ledger of absorbed power (convention 8) × 0.5 is the next tick's allowance of a package-internal store-less supply source; one consumer `glow` (demand 2 W) targets the Blue emitter (radiant 2 W)." This reads absorbed energy, which requirement radiation-12 demands ("output cannot exceed absorbed energy"); the spec's hard-coded head-on fractions (0.798, 0.451, 0.259 at `:30`) duplicate the Scintillator row and read 0 for radiation entering another face (T5). Delete M3 and T7.

### Medium

- **M-1. RAD-08:70 (R5).** `PoweredGateTests.cs:240` exercises a contact-load plate contact, not `SignalStateContactControl`. Cite `ComposableSupplyRoutingTests.cs:459` (and `elements/StateLatch/StateLatchElementDeclaration.cs:67`).
- **M-2. RAD-04:52-54** counts RAD-14's M5; report the family as 3/0/1/4, or RAD-03:142's own disqualifier trips. The cartridges, the four shields and the two detectors are value-only. The gauges are value-only in declaration, but EL-130 needs the liquid crossing, so they are one family only after ENGINE-LIQUID.
- **M-3. RAD-12:94, :123, :142 — light margin.** 0.5 W is admissible (`CAT-012:49`: default 2.5, minimum 0.5, policy Reject). Recomputed: 9.26 W/m² × 0.36 m² × 0.451 × 0.5 = 0.75 W; the 1 m floor caps it at 0.81 W, a third of the receiver's default. It falls to 0.5 W with the capsule at 1.22 m or behind 0.27 m of Ordinary collider; release is 0.45 W. The ±3 % tolerance is not the risk; the 0.22 m placement band is. Decide: efficiency 0.8 (1.2 W at 1 m), or a locked 0.5 W receiver in levels 117 and 122.
- **M-4. RAD-14:102.** A beam entering anywhere on the 0.60 × 0.80 m inlet leaves from one fixed point, up to 0.4 m sideways. Add to ENGINE-OPTICAL "an outlet may be declared relative to the inlet hit point and incoming direction (offset, rotation)", or shrink the inlet. State outlet masks ({Alpha}, {BetaMinus}; Σ τ × mask = 0.8 ≤ 1). Requirement radiation-14 is met by two declared outlet sets (r 1.5 m: 0.054 m, 15.5°; r 1.0 m: 0.084 m, 23.6°; recomputed). The coil consumer needs a stated target kind (`ABS/SupplyContractValues.cs:44-54`). Choice-bound values are ENGINE-PARAMETER-BOUND-FRAMES' (decision 16): re-own RAD-14:68 N2, RAD-09:67, RAD-08:73 N2 and RAD-02's band rows.
- **M-5. RAD-17:28.** "None overlap" is false: the mouth tube (outer radius 0.14 m at y −0.30) enters the base (top −0.40) for z −0.35 to −0.27. Move the mouth to (0, −0.26, −0.32); drain time becomes 4.87 s; re-derive `:98` and W4. `:62`: the fluid port contract is ENGINE-GAS-NETWORK's (decision 16), not ENGINE-LIQUID's; add it and ENGINE-CAPABILITY-INSTANCES to `:18`, `:146`. `:30` declares `End` as Discharge while `:128` fills through it: make it Bidirectional.
- **M-6. Requirement rows queued out of the slice.** EL-130:110 ships on L4 and queues L1 to L3 and L5 (its own criteria: "rising conserved waterline"): replace with "delivered after ENGINE-LIQUID and one vessel; otherwise EL-129 ships alone". RAD-18:109 queues N3 and N4: say the ledger row stays open until they pass. EL-126:125 queues S5 to S7, its only distinguishing rows: run them in-slice on law-scene collimated emitters.
- **M-7. RAD-22:45, :48.** A flat printed band on a ball breaks the banded-ball rule; decision 11 excepts the Pool ball only. Delete `Visual/Band` and keep the γ marks (recommended), or add a proud band with a torus collider.
- **M-8. RAD-22:95, :109-110.** In a 0.65 m bore the 0.33 m capsule rides 0.32 m below the pipe axis: R ≈ 0.36 W, not 0.409 W. Fixture text: "meter aperture centre level with the capsule centre, 1 m from it horizontally". `:112` (A6) names no delivered fork: state the fixture or queue on EL-163. A8: add "meters read 0 after capture" and, in ENGINE-OPTICAL, "an emitter on a body whose collision participation is disabled emits nothing".
- **M-9. RAD-06:18, :68, :147.** `ENGINE-SPRING-REST-COORDINATE` is not in decision 3a's list. The capability is narrower, not missing: `PrismaticSliderDescriptor.Compression` (`ABS/MechanicalContractValues.cs:110`) is a rest offset, and `BUN/MechanicalValueDomain.cs:80` refuses a rest position outside the limits. It should live in **ENGINE-JOINT-KINDS** ("revolute position drive with rest window" is already there), as the row: "a spring on a prismatic or revolute coordinate declares a rest position that may lie outside the travel limits, so the coordinate is preloaded against a limit". Counts 5/0/2/4. Edit `batch-c.md:80` and `batch-o.md:118` together.
- **M-10. RAD-07.** `:59` (R3): `ParameterizedBodyIntegrationTests.cs:29` is not a test line; the named method is `TennisBallTests.cs:29` and tests a sphere radius: mark "present, untested for a box". `:124`, `:142`: delete the "Real 0.12 to 0.40 … decided at slice planning" fallback. `:109` (C3): add "the sum over the five receivers is non-decreasing and the power ledgered to the four walls non-increasing from Narrow to Wide". `:89`: give heights (capsule 0.26 m, channel 0.30 m, meter 0.36 m).
- **M-11. Marginal rows.** EL-129:94 (G6): thick 0.654 W is 4 % under the 0.679 W release at ±3 %; use `thin_threshold` 0.715 W. EL-132:65: 0.158 W is 5 % over release. RAD-13:112: "5 s of wall time" is not deterministic; use "while paused no tick advances". RAD-02:101: the 9.86 W loss is "ledgered to the housing collider".
- **M-12. Preview and display owners.** RAD-16:65 and RAD-15:73 give the Build overlay and the trail display volume to ENGINE-RADIATION; optical convention 12 owns the preview and the beam renderer. One owner: ENGINE-OPTICAL.

### Low

- EL-126:62, :31: Domino citations off by one; composite principal moments are a rule, not numbers (about 0.0499, 0.0229, 0.0301 kg·m²).
- EL-130:87: 0.882 × 0.850 = 0.750. EL-129:17 and EL-130:15 use different parameter names.
- RAD-06:118 (S7) has no coordinates; `:119` mixes a bound with a tolerance. RAD-13:109 and RAD-22:111 omit wiring and launcher.
- RAD-12:22, RAD-14:22: the Signal lamp is not an `ISupplyElement`; cite the Powered gate.
- RAD-15:102: tracks land 0.08 m from a segment edge.
- RAD-17:94: state the detector's 0.09 m lift.

## 4. Engine behaviours the summary could not confirm (verified by code)

1. **Three ports sharing role `Supply` (RAD-15): admitted, untested.** Duplicates are checked on `PortId` only (`ElementPortCatalogue.cs:102-103`); supply binds by `PortId` (`BUN/SupplyBundleValidation.cs:137-152`, `WorkshopSupplyCompiler.cs:54-68`); `PortId` wraps a string. Residual: `ui/WorkshopConnections.cs:51` tags socket buttons by role (browser phase).
2. **Memory node feeding a physical request (RAD-21): refused** (H-4).
3. **Connection to a part with no ports: refused today** with `WorkshopEditRejection.InvalidConnection` (`WorkshopConnections.cs:111-114`), tested at `BundlePortsTests.cs:276-280`.
4. **Receiver and a 0.50 m box (RAD-10): admitted.** Capture is a centre-point test in a local box (`elements/Receiver/ReceiverElementDeclaration.cs:12-17`, `SIM/WorkshopPhysicsEngine.cs:931-937`) with speed ≤ 1.5 m/s and 0.35 s dwell; no shape or size test.
5. **Cannon and a 0.66 m sphere (RAD-22): inside the declared window** 0.6 to 0.84 m (`elements/Cannon/CannonElementDeclaration.cs:31-32`, `ABS/ChamberRegionValues.cs:13-15`); the engine's width evaluation was not traced.
Also: `CommittedElectricalSocket` requires a port bound to a logic node (`BUN/SupplyBundleValidation.cs:195`), moot after H-1.

## 5. Cross-batch conflicts

1. Tick order, shares and coupling: O against the repaired K (C-1, C-2, C-3).
2. Query: same cast, but O needs default τ = 1 and an overlap rule that K does not state (C-4).
3. Unpaid sources: O against thermal T4 and T13 (C-9); RAD-20's cold face against TH-10's geometry (H-2); store-less source owner against TH-30 (H-3).
4. Goal qualifier: O against owner decision 12 and Batch M (H-5).
5. Fluid port owner: RAD-17 against decision 16 and Batch F.
6. Spring rest position: Batches C and O use an unlisted name (M-9).
7. Authored-position retention: RAD-21 and TH-33 need one ENGINE-ASSEMBLY-JOINTS row covering prismatic and revolute.

## 6. Engine-story gaps, with proposed text

The proposed `ENGINE-RADIATION` text (`batch-o.md:98-106`) is element-free: it names no part, kind or package. It is not complete and holds six items that belong elsewhere. Delete from it: "several rows on one aperture" (H-1); "threshold level … that set a signal node" (C-3); "relay masks … enabled by a supply grant" (C-3); "an emitter paid by a receiver's previous-tick strength" (H-6); the goal qualifier (H-5); "rows selectable by a Choice option" (ENGINE-PARAMETER-BOUND-FRAMES); the preview and the display volume (ENGINE-OPTICAL). Replacement *Missing* sentence:

> radiation channels as a closed set on the emitters, segments and receivers of the optical solve, run in that solve's one tick slot; a surface transmittance of 1 for radiation channels on every collider unless declared; a declared attenuation row of per-channel coefficients on any collider and one declared default row for the scene, applied as exp(−coefficient × chord) to the ordered crossings of the spatial query's segment cast at this tick's final poses, overlapped chords of one body counted once with the largest coefficient; one optional declared conversion per row (from, to, fraction ≤ 1) with the stated per-crossing law; a liquid store's committed filled region crossed as a volume with the liquid's row; emitters paid by a supply consumer or by a declared finite inventory that decays with a half-life or is steady; a receiver facing rule (Lambert or Uniform); a weighted-sum strength; an accumulator that integrates a receiver's strength into a committed scalar with a capacity and an authored initial value; a collider's absorbed power published as a committed ledger that a declared supply source may take, times an efficiency ≤ 1, as its next-tick allowance; no buildup, scatter, reflection or refraction; presentation drivers for a committed strength, accumulator fraction and inventory fraction; the palette category and domain flag.

Acceptance: keep the five proposed clauses, change "initial power × half-life ÷ ln 2" to "its declared inventory", and add: "**And** a collider with no row attenuates by the default row, and a declaration naming an unknown channel is refused at bundle time. **And** power leaving a crossing, summed over channels, never exceeds power entering it. **And** shares of one diverging segment are scaled so that received never exceeds emitted." Status line: "After ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES and ENGINE-OPTICAL; the liquid crossing after ENGINE-LIQUID; no dependency on ENGINE-THERMAL."

Other stories:
- **ENGINE-SIGNAL-NODES:** "threshold with release ratio over a committed scalar from any domain (a strength, an accumulator, a supply grant); a pulse on the rise of any level node; a package-internal edge from that pulse to a physical-request input; the contact state a threshold node presents".
- **ENGINE-OPTICAL:** dynamic emitter and receiver frames; uniform cone; "an emitter on a body whose collision participation is disabled emits nothing"; inlet-relative Relay outlets (M-4); the store-less supply source (one owner with TH-30). Withdraw G2's "a receiver level sets and clears a declared signal memory node".
- **ENGINE-THERMAL (G3):** "constant power" becomes "paid from a declared finite store".
- **ENGINE-JOINT-KINDS:** the spring rest position row (M-9). **ENGINE-ASSEMBLY-JOINTS (G6):** one authored-position retention row for both coordinate kinds.
- **ENGINE-RUN-LENGTH:** consumers RAD-09, RAD-13, RAD-20 and the steady-inventory size (C-9).
- **`engine-goal-count-window-hold`:** the scalar-band qualifier (H-5).
- `ENGINE-SPATIAL-QUERIES`, `ENGINE-SIGNAL-NODES`, `ENGINE-PARAMETER-BOUND-FRAMES` and `ENGINE-RUN-LENGTH` have no text in `epics.md`; specs should say "proposed" where they cite them.

## 7. Items for the owner

1. **The default attenuation row (C-7).** Recommended: photon coefficients ÷ 10, so no ordinary part rivals a shield. Blocks EL-128's claim and lesson 104.
2. **Steady sources as a finite inventory sized to the maximum Run (C-9).** Recommended: adopt; IX-16 then needs no rewording. Alternative: accept unpaid steady sources and reword IX-16 and thermal T4. Blocks RAD-01, -03, -04, -16, -20, -22.
3. **One dose scale (C-10).** Recommended: one standard aperture area for every accumulating receiver.
4. **Reading inside 1 m (C-8).** Recommended: flat inside 1 m.
5. **Scintillator light (M-3).** Recommended: efficiency 0.8.

The summary's "Needs the owner: none" is therefore wrong.

## 8. Summary table against the specs

- RAD-21 "data-only if a memory node may feed a physical request": it may not; engine tier (H-4). Counts 5/0/3/4.
- RAD-15 "data-only if three ports may share role `Supply`": they may.
- RAD-20 "ENGINE-THERMAL only": also the store-less source's owner and ENGINE-CONTACT-OBSERVATIONS.
- RAD-06 names an unlisted story; counts 5/0/2/4. RAD-04 counts 3/0/1/4.
- RAD-10 names one goal slice; decision 12 has five and the qualifier's owner is the fifth.
- "Conformance: 26 Conform" becomes 26 Conform with fixes; "Needs the owner: 0" becomes 5.
- `batch-o.md:147` unverified items: all five are now settled (section 4).

## 9. Not verified

- Nothing was run; every radiation row is a design. Stability of the 0.02 m slab, the 4.5 kg blade (damping ratio 0.43 by hand) and the 0.8 kg shelf.
- Legacy `@a6c914e` citations, ledger rows, campaign chapter numbers and palette orders.
- Code citations of the fifteen specs not sampled in full; a Battery "run control" for RAD-09 D5; the Weight's 8 kg; Conveyor speeds; a Choice-selected art node; whether `AmbientLoadDescriptor` takes constants (RAD-22); the hopper's size range in code; the Cannon's width evaluation in the engine.
- TH-30 and TH-10 were read for the cited rows only.
