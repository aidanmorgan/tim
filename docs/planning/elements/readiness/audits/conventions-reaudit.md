# Re-audit: the five convention owners (liquid, pneumatic, optical, thermal, radiation)

Independent read-only audit, 23 November 2026, checkout `spec-drafts` (published main `8d1d68aa` plus drafts). Nothing was built, run, edited or committed. The five specs were being edited by another session while I read them (TH-04 08:13, RAD-08 08:12, EL-176 08:03, EL-001 07:54 local time). I snapshotted all five at 08:17. They were unchanged against that snapshot at 08:24, and every finding below cites that text. `_bmad-output/planning-artifacts/epics.md` was being rewritten during the audit; see check 6.

Read in full: the five convention sections and their specs; `readiness/owner-decisions-2026-11-22.md`; `readiness/engine-conformance.md`; `.tools/audit/engine-design-gap.md`; the first-round audits `elements-batch-f/h/k/n/o.md`. Read in part: `readiness/batch-n.md` sections 5 to 7, `readiness/batch-o.md` G2 and G20, `CAT-059` (store rows) and `TH-17` (head and phase row). Code opened: `WorkshopPhysicsEngine.cs:285-300`, `:985-1020`, `.Motors.cs:14-60`, `CadenceValues.cs`, `ElectricalSupply.cs:1-20`, `SupplyContractValues.cs:30-55`, `BatteryElementDeclaration.cs:36-50`, `BalloonElementDeclaration.cs:60-72`, `BodyMassSourceDeclaration.cs:8-24`, `WorkshopGpuDevice.cs:238-250`, `TubeMouthSnap.cs:24-28`, `element-free-engine-design.md:41`.

Shorthand: `L` = `docs/planning/elements/EL-001-finite-reservoir.md`, `F` = `EL-039-air-reservoir.md`, `O` = `EL-176-red-laser-emitter.md`, `T` = `TH-04-electrical-heating-plate.md`, `R` = `RAD-08-radiation-rate-meter.md` (all under `docs/planning/elements/`).

## 1. Verdicts

| Owner | Verdict | Reason |
|---|---|---|
| Liquid (EL-001, L1–L18, W1–W25) | **Ready with fixes** | The first-round C-1 to C-9 and G-1 are resolved, except C-7 (partial, M-4). Open items: its ledger accounts disagree with the thermal ledger (H-1); heat carried by flow has no tick slot (H-2); the gas–liquid edge has two owners and no energy rule (M-5); a capacity refusal conflicts with D5 (M-6); stream paths are curved but the query is a straight cast (M-10); two owners are written as conditional (M-9). |
| Pneumatic (EL-039, F1–F11, P1–P20, G1–G14) | **Ready with fixes** | P-H1 to P-H4, P-M4, P-M8 and P-M9 are resolved. Open items: P1 and P14 say "single species" while thermal puts a vapour component in a gas store (H-5); the gas step is per tick in F5 but per substep in the code and in L16, and the clamp is pairwise (M-1); the port record is only half joined to L5 (M-4). |
| Optical (EL-176 section 10) | **Ready with fixes**, pending one coordinator ratification | C-1 to C-7 and C-9 of Batch K are resolved. Open items: item 9 leaves the credited-store owner "open" (H-3); the absorbed ledger is double-counted against the thermal credit (H-4); the near-field and own-body rules differ from radiation's (M-3); receivers are not culled per segment (M-7); 120 Hz is hard-coded (M-2). |
| Thermal (TH-04 section 11, T1–T14) | **Ready with fixes** | N1 to N7 are resolved. Open items: T13 lacks `Drained`, `Supplied` and mechanical-work terms (H-1, M-8); advection has no slot (H-2); radiant credit double-counts and is wrong for radiation channels (H-4); the credited store has no owner (H-3); gas materials can exceed their limits (M-8); the capacity refusal conflicts with D5 (M-6); 120 Hz is hard-coded (M-2). |
| Radiation (RAD-08 section 10) | **Ready with fixes**, pending the same ratification | C-1 to C-12 of Batch O are resolved, except C-4 (partial, M-3) and C-11's crossing cap (M-6). Open items: item 10 calls the credited store "delivered", which is false (H-3); the accumulator uses 1/120 s (M-2); the own-body and near-field rules are second rules (M-3); a per-cast crossing cap conflicts with the shared cast and D5 (M-6); line 185 labels coordinator decisions "(owner)" (Low). |

Under the rule used here, a fix that is a sentence edit counts as Ready with fixes. Two items need a coordinator or owner word, not new design. (a) The credited-store owner (H-3): Batch O G20 already chose ENGINE-THERMAL and applied it in CAT-059, EL-211, TH-30, RAD-12 and RAD-20; only the owners' own text disagrees. (b) The gas–liquid edge owner (M-5).

## 2. Combined tick order (check 3)

For one committed tick n of duration dt. dt is 1/120 s at the default cadence; the engine also admits 1/60 and 1/240 s (M-2). The tick has 8, 4 or 2 substeps of 1/480 s.

1. **Tick start.** The signal network resolves routing and memory nodes from tick n−1's commits: F5, O4 ("signal levels as resolved at the start of this tick"), L16, T7 and D7. `PrepareTickSupply` runs (`WorkshopPhysicsEngine.cs:294`).
2. **Each substep s.**
   - (a) Gas advance (`AdvanceGas`, `:994`).
   - (b) Liquid step at the same position (L16), not repeated by trial replays. Replays sit inside `SolveBoundedSubstep` and `SolveContinuousSubstep` (`.Motors.cs:30-60`), so neither gas nor liquid is replayed today.
   - (c) Supply grants (`TransferSupply`, `:1011`). A liquid or gas work target therefore spends the grant of substep s−1; at s = 0 that is tick n−1's last grant (Low-6).
   - (d) The mechanical solve with trial replays; friction work accrues here (T4).
   - (e) Commit of constraints and travel.
3. **After the last substep.** One optical and radiation solve runs on the final poses, this tick's grants and the liquid levels (O4, R11).
4. **Thermal step (T8).** Source credits (grants, friction summed over the substeps, absorbed light) are applied first, then the link exchange (T9). Phase-row mass is queued to enter its stores at the tick boundary.
5. **Sensor evaluation.** This covers liquid level thresholds (L16), receiver levels, temperature thresholds and pressure thresholds (F5). Then commit.
6. **Tick n+1.** Thresholds act; the electrical solve reads them (D7). Phase-row mass moves from tick n+1's substeps (T8, L16).

The owners agree on 1, 3, 4, 5 and 6. Three disagreements remain:
- F5 (`F:90`) sizes "one gas step" as 1/120 s; L16 (`L:41`), T8 (`T:202`) and the code put it per substep (M-1).
- No owner places heat advection by liquid flow (H-2).
- Gas and liquid both run "first in the substep" (L16), but no owner says which runs first or what a gas–liquid edge reads (M-5).

## 3. Numeric chains (check 2)

- **Electrical → heat.** A Battery `power_limit` of 120 W (`BatteryElementDeclaration.cs:44`) with `PhaseSeconds` 1/120 (`ElectricalSupply.cs:9`) gives 1 J per tick, credited to `disc` (15.25 J/K) in T8. At 10 s: 288 + 240·(1 − e^(−10/30.5)) = 355.1 K, so 1200 J debited = 1023 J stored + 177 J to ambient. **Closes.** At 60 Hz cadence the supply phase is still 1/120 s while the tick is 1/60 s; I did not establish what the delivered grant does then (section 7).
- **Friction → heat.** T4 and T8 sum over all substeps: a drive slipping at 2 N·m and 10 rad/s gives 4 × (20 W × 1/480 s) = 0.1667 J per tick. **Closes**, but only if the accrual counts the accepted trial solve alone (Low-5).
- **Light → heat.** A laser pays 0.1 J per tick (12 W) and emits 10/120 J, ledgered to the absorber (O7), plus 2/120 J to the housing (O2). If the absorber is a thermal face with absorptance 0.8, T3 also credits 0.0667 J to its node. Nothing removes that 0.0667 J from the collider ledger, so the accounts total 0.1667 J against 0.1 J paid. **Double-counted (H-4).**
- **Light → supply.** CAT-059 credits 0.2 × 81 W = 16.2 W, which is 0.135 J per tick into a 0.15 J store (18 W × 1/120 s). **Conserving.** The store row has no agreed owner (H-3).
- **Radiation → supply.** The same form (R10). **Conserving.**
- **Radiation → heat.** A Uniform receiver's reading is power crossing it, not power absorbed (R7). Two thin receivers on one 1 W Alpha path, each with absorptance 1, would credit up to 2 W from 1 W under T3. **Creates energy (H-4).**
- **Heat → gas pressure.** The default air store holds 0.0484 kg, so C = m·Cv = 34.75 J/K. Each J adds 0.0288 K and Δp = (γ−1)·ΔU/V = 40 Pa; 1200 J gives +34.5 K and +48 kPa, which matches ρ·R·ΔT = 4.84 × 287 × 34.5 = 47.9 kPa. **Consistent with T2.** At 120 W a sealed store reaches P14's 800 kPa limit at 576 K after (576 − 288) × 34.75 ≈ 10 kJ (83 s). "Clamp-or-continue" then destroys energy (M-8).
- **Expansion work.** P8 (gas U plus body kinetic energy) and T6 (drive work debited from the node) each conserve. T13 has no term for work leaving to bodies, so the thermal ledger cannot close (M-8).
- **Boiling.** On the 120 W plate the kettle has 50.8 W to spare (I re-derived the disc–kettle split at 32 W/K: x = 77.5/32.5 = 2.385 K, Q = 76.3 W, minus 25.5 W of kettle loss). Steam: 50.8/40 000 = 1.27 g/s, that is 10.6 mg per tick. Per tick, latent 0.423 J plus liquid sensible 0.0106 g × 1046 × 373 = 4.14 J gives the vapour enthalpy 1153.2 × 373 × 1.06 × 10⁻⁵ = 4.56 J. **Closes.** Into a rigid head the excess R·T over Cv·T returns to the vessel, net (c − Cv)·T = 354.3 × 373 = 132 kJ/kg; that is conserving. The "T_sat rises four times faster" claim needs the TH-17 table (not verified).
- **Liquid ↔ thermal.** The store node C = 1046 J/(kg·K) × m (L2, T2). Advection is unplaced, and a per-tick reading of it goes negative (H-2). Evaporated mass is missing from L10 (H-1).
- **Gas ↔ liquid.** 400 kPa absolute over 16 kg/m³ is (400 000 − 101 325)/156.96 = 1 903 m of head, so a head cap is required (L15 states this correctly). Nothing debits the gas store for the work done (M-5).

Hand checks that pass: EL-001 (23.95 s, 12.0 s, 101 482 Pa, Weight densities); EL-039 (253 600 Pa, 124 200 Pa, 0.0726 kg, 0.0484 kg, 10.0 kJ, 1.59 g choked per 1/120 s through 2 × 10⁻⁴ m²); EL-176 (8.33 W, 8.82 W, 9.03 W, 8.55 W, 300 s); TH-04 (every row in its section 5; T11's 40 000 and 30 900 J/kg, 1.27 and 3.0 g/s, 2.16 × 10⁻³ m³/s; T14's values); RAD-08 (0.882 W, 0.900 W, the A2 nine-sample 0.248 W, 2.12 and 2.45 m, 0.324, 0.119 and 0.849 W, 1.59 W scaled to 1 W, every inventory in item 14).

## 4. Findings

### High

**H-1. The liquid and thermal ledgers name different accounts, so neither identity closes.**
- `L:28` (L3) and `L:35` (L10): three world accounts (`Spilled`, `Drained`, `Supplied`); phase change counts in `Supplied`/`Drained`.
- `T:199` (T5): evaporated mass "goes to the world account `Evaporated`". TH-28 A3 asserts "`reservoir` has lost 40 g and `Evaporated` has gained the same mass", so L10 fails on every evaporating store.
- `T:207` (T13) has no `Drained` term: enthalpy that enters an EL-006 drain leaves T13. It also has no `Supplied` term.

Replacements:
- L3, the account sentence: "Four world accounts: `Spilled`, `Drained`, `Supplied`, `Evaporated` (mass leaving a free surface by a thermal phase row, T5)."
- L10: "Σ store volumes (pending overflow and prime inventories included) + `Spilled` + `Drained` + `Evaporated` = Σ authored initial volumes + `Supplied`."
- T13: "Σ node enthalpy + `ToAmbient` + the enthalpy carried into `Spilled`, into sink nodes (the part of `Drained` a sink received) and into `Evaporated`, and out of vents + the work done on bodies (T6 drives, gas chambers) = Σ initial enthalpy + Σ source credits + the enthalpy brought in by source nodes (the part of `Supplied` a source delivered) − Σ converter work out. A phase row's exchange between a liquid store and a gas store stays inside Σ node enthalpy and is not counted again." L3 and L10 keep counting that exchanged volume in `Drained` and `Supplied`, so the volume identity still balances; only the enthalpy terms are restricted to sinks and sources.

**H-2. Heat carried by liquid flow has no tick slot, and the obvious per-tick form makes enthalpy negative.**
- `L:27` (L2) and `T:188` (T2) say "inflow mixes by mass and outflow leaves at the store's temperature"; L2 adds "no liquid rule reads temperature".
- T8 (`T:202`) runs once per tick; the liquid step runs per substep (L16). Neither says where advection runs.
- Worked case: a 0.02 m² store (TH-18 size) holds 2 × 10⁻⁵ m³ at 368 K and passes 4.9 × 10⁻⁴ m³ of 288 K inflow in one tick (head 0.5 m, pipe bore). With outflow at the tick-start temperature, H_end = ρc·(368 × 2×10⁻⁵ + 288 × 4.9×10⁻⁴ − 368 × 4.9×10⁻⁴) = ρc × (−0.032) < 0.

Replacement, to add to L2 and T2 (one sentence each, same text): "Enthalpy moves with liquid in the liquid step, per substep: every transfer of volume ΔV carries ΔV × the source's specific enthalpy H ÷ V; a fitting passes the mixed specific enthalpy of its inflows; a stream credits its enthalpy with its volume; spilled, drained and evaporated volume carries its enthalpy into that account. This reads no temperature and, with L7's bound ΔV ≤ V, never makes H negative. ENGINE-THERMAL adds this row to the liquid step; its per-tick step reads H as the last substep committed it."

**H-3. The credited-store row has three different owners.**
- `O:179` (item 9): "which story owns the credited store row is open (Batch O G20)".
- `R:214` (item 10): "The store and the source drawing from it are the delivered finite-store source (`SupplyContractValues.cs:39`)". This is false: `CAT-059-solar_panel.md:87` (M10b) marks it **narrower**, because `SupplySourceStoreDescriptor` takes four parameter ids only.
- `T:198` (T4): names no owner.
- Batch O G20 (`readiness/batch-o.md:139`) and CAT-059 M10b give it to ENGINE-THERMAL. This fails check 6 (exactly one owner).

Replacement, the same sentence in O9, R10 and T4: "The credited store (a supply source whose finite store has declared constant capacity, power limit, initial charge and enable, starts empty and is filled only by a credit) is one row of ENGINE-THERMAL, which lands before ENGINE-OPTICAL (owner decision 18; Batch O G20). The receiver credit is ENGINE-OPTICAL's row; a thermal converter's credit is ENGINE-THERMAL's." In R10, delete "are the delivered finite-store source (…)". This needs the coordinator to ratify G20, or to move all three to ENGINE-SUPPLY-DEMAND together.

**H-4. Absorbed light is double-counted, and the radiant credit is wrong for radiation channels.**
- `O:172` (O2), `O:177` (O7) and `R:214` (R10) ledger absorbed power to the absorbing collider, "not heat until ENGINE-THERMAL consumes it".
- `T:197` (T3, Radiation) credits "a receiver aperture's committed reading × its declared absorptance" to the node. No convention moves that amount out of the collider's ledger, so the laser chain in section 3 accounts for 0.1667 J of 0.1 J paid.
- For radiation channels a reading is power crossing the aperture, not power absorbed (`R:211`, R7: readings on one collimated path "may sum to more"). T3 as written lets two receivers credit more heat than was emitted.
- "Until ENGINE-THERMAL consumes it" promises a later behaviour that no T-item defines.

Replacement for T3, Radiation: "None between nodes. Radiant heating is absorbed light or radiation: where a collider is a face of a thermal node with a declared absorptance a, a × the energy the one optical/radiation solve ledgered to that collider in the tick (optical 7 and 8: collimated power ending on it and its conserved diverging shares; radiation 10: its absorbed power as that item defines it) moves from that collider's ledger to the node in the thermal step; the remainder stays on the ledger. A reading is an observation and credits nothing by itself."

Matching edits:
- In O2, O7 and R10, replace "is not heat until ENGINE-THERMAL consumes it" with "becomes heat only as thermal T3 moves it".
- In T4, "optical (absorbed reading)" becomes "optical and radiation (absorbed energy, T3)".

**H-5. The pneumatic contract is single-species, but thermal stores vapour in a gas store.**
- `F:102` (P1, "a homogeneous gas inventory") and `F:115` (P14, "single-species ideal gas").
- `T:199` (T5) moves boiled mass into "the gas store's vapour component".
- `TH-17-kettle.md:39` condenses at "the vapour's partial pressure"; `TH-17-kettle.md:196` lists "a gas network with single-species stores only" as a disqualifier.
- The mixture text exists only as a proposal (`readiness/batch-n.md:146`, item 4). The pneumatic owner does not adopt it, and F5's equalising clamp is not defined for a two-component store.

Replacement:
- Append to P1: "A store may hold one carrier gas and one vapour component at one temperature: mass, internal energy and pressure are the sums over components (Dalton), U = Σ mᵢ·Cvᵢ·T, p = Σ mᵢ·Rᵢ·T ÷ V; flow along a path carries each component in proportion to its mass fraction (ENGINE-GAS-NETWORK)."
- In P14, change "single-species" to "each component a calorically perfect ideal gas".

### Medium

**M-1. Gas step rate, replay rule, multi-path clamp and work bound.**
- `F:90` (F5) gives "one 1/120 s step of choked flow … is 1.6 g". The delivered advance and L16 are per substep (1/480 s), where the same flow is 0.40 g.
- The clamp is per path. A node joined to several paths (a tee'd Output, F3) can give each path its equalising amount from the same start state and overdraw itself. Example: a 4 × 10⁻⁴ m³ line at 400 kPa feeding two 1 × 10⁻⁴ m³ caps at ambient, each path moving its pairwise equalising amount from the step's start state, ends near 280 kPa (pV 160 − 2 × 24 J) while the caps reach 340 kPa. This is the flaw N1 found in the old thermal clamp.
- F has no work-bound or replay item comparable to L18 and T10.

Replacement for F5's last sentence: "The gas step runs once per 1/480 s substep, first in the substep with the liquid step (L16), and is not repeated by a solver trial replay. Paths are applied one at a time in compiled order from current state, each moving at most the amount that equalises the two volumes it joins, so no node gives more than it holds (at 288 K one substep of choked flow from 400 000 Pa through the hose throat is 0.40 g). Work per substep is one row per store and per path."

**M-2. 120 Hz is hard-coded although the engine admits 60, 120 and 240 Hz.** `CadenceValues.cs:41-78` defines `SimulationCadence.Hz60/Hz120/Hz240` with 8, 4 or 2 substeps per commit. The cadence is configurable through the worker interface (`workshop-observation.ts:29`, `ConfigureCadence` command at `:39`) and is tested at all three rates (`docs/gpu-f32-physics.md:371`, `:373`). Affected text:
- `T:202` (T8): "per 120 Hz tick", "this tick's four substeps".
- `R:213` (R9): "+ strength × 1/120 s each tick". This under- or over-integrates dose by a factor of two at the other cadences.
- `O:174` (O4) and `R:215` (R11): "per 120 Hz tick".
- `F:90` (F5): "1/120 s".

Replacement, one sentence in O4 (cited by R11, T8 and F5): "dt is the committed tick's duration (1/120 s by default; 1/60 or 1/240 s by the Run's simulation cadence); every per-tick sum, credit and accumulator uses dt and the tick's substeps, never a constant." In R9, "× 1/120 s" becomes "× dt"; in T8, "four substeps" becomes "substeps".

**M-3. Rules inside the one solve still differ by channel class.** These are second rules chosen by a condition, and coordinator decision 21 asks for "one own-body rule".
- Near field: `O:171` (O1) keeps facing inside 1 m (E = I·facing ÷ max(1 m, d)²); `R:189` (R3) sets facing to 1 inside 1 m. Replacement for O1's irradiance sentence: "Irradiance at a point is I × facing ÷ d² beyond 1 m and I ÷ 1 m² inside 1 m for a source on the front side of the aperture (facing 1), so a reading is flat inside 1 m." Delete R3's "with one change" clause. A grep of the specs that cite the optical conventions found no optical row asserting an off-axis reading inside 1 m, so no row changes.
- Own body: `O:175` (O5) says "a sample is unoccluded when the first absorbing crossing is the receiver's own aperture face"; `R:193` (R4) says "skip crossings of its frame body". These are two rules chosen by channel class, which coordinator decision 21 ("one own-body rule") forbids. Which form becomes the one rule is the coordinator's choice. Adopting R4's form for light removes self-shadowing: an optical receiver whose own colliders stand in front of its aperture would then be lit through them. Adopting O5's form for radiation makes a Uniform receiver dark to sources behind its own body. I did not check which optical receivers have colliders in front of their apertures. Whichever form is chosen, state it once in O5 and delete the other.
- Default surface response: Absorb for light (`O:176`), τ = 1 for radiation (`R:191`). Keep it, but state it as data in O6: "the default response is declared per channel: τ = 0 (Absorb) for light channels, τ = 1 for radiation channels".

**M-4. The fluid port contract is only half unified (first-round C-7).**
- `L:30` (L5): one record of node, role (Intake, Discharge or Bidirectional), aperture area and socket, with two socket kinds.
- `F:52-57` and `F:88` (F3): new `PortRole` members `GasIn` and `GasOut` plus `PortDirection` Input/Output, and no aperture area on the port. The throat is on the connection kind (`F:95`, F10). That gives two role vocabularies for one record.

Replacement in F3: "Every gas port is a fluid port of the one record (L5): node store 1, role Bidirectional (flow follows pressure), a link-joined socket whose authoring end is Input (`GasIn`) or Output (`GasOut`); the edge's aperture is the throat the connection kind declares (F10)." Drop the new `PortRole` members and keep `PortId`s.

**M-5. The gas–liquid edge has two owners and no energy rule.**
- `L:111` (W15) gives the "gas-to-liquid edge" to ENGINE-LIQUID; `T:199` (T5) calls it "a declared edge of ENGINE-GAS-NETWORK".
- `L:40` (L15) defines input energy for electrical and shaft inputs only. A gas input that raises head therefore takes no energy from the gas store.
- ENGINE-LIQUID lands after ENGINE-GAS-NETWORK (owner decision 18), so only it can own an edge between the two networks.

Replacements:
- T5: "… is a declared edge of ENGINE-LIQUID with its own head cap".
- Add to L15: "On a gas edge the input is a gas store: the energy taken (hydraulic work ÷ efficiency) is debited from that store's internal energy, and the edge adds no head while the store's pressure does not exceed the liquid node's head pressure. The gas step and the liquid step of a substep run in that order; the edge reads the gas pressure the gas step just committed."

**M-6. Scene-capacity refusals contradict D5 and the shared acceptance being written.**
- `L:43` (L18): "ENGINE-LIQUID … refuses a scene over capacity with a typed reason at Run".
- `T:204` (T10): "exceeding one is a typed refusal at compile".
- `R:216` (R12): "ENGINE-RADIATION states the cap on crossings returned by one cast and what happens at the cap (the remaining path is treated as blocked)".

These conflict with `_bmad/custom/element-free-engine-design.md:41` (D5: "Do not add arbitrary population ceilings"). They also conflict with the in-progress shared acceptance in `epics.md` ("a typed refusal is admitted only for a per-declaration limit a package declares … never for a scene population"), and with ENGINE-SPATIAL-QUERIES' cast, which returns "every crossing".

Replacements:
- L18: replace only its last sentence with "`ENGINE-LIQUID` states the sweep and sample counts (per-declaration limits); tables are sized at Run from the compiled declarations and grow geometrically, with no scene ceiling (D5)."
- T10: replace "Table capacities are ENGINE-THERMAL's to state; exceeding one is a typed refusal at compile" with "Tables are sized at Run from the compiled declarations and grow geometrically; there is no scene ceiling (D5)."
- R12: delete the cap sentence. Crossings per cast are the spatial query's, unbounded except by the colliders on the segment; the optical interaction and segment limits (O10) are the per-declaration bounds.

**M-7. Receiver candidates are a scan.** `O:181` (O11) and `R:216` (R12) bound shadow casts by "K receiver sample points in the scene" per diverging segment. That is emitters × all receivers, which breaks conformance point 6 ("nothing proportional to the whole scene") and the in-progress SPATIAL-QUERIES contract ("a source queries its bounded cone"). Replacement in O11: "A diverging segment's candidate receivers come from the spatial query's region query over its cone and range; work is N × L segment casts plus one shadow cast per sample of each candidate receiver." R12 follows.

**M-8. Heat into gas stores can be destroyed, and T13 omits mechanical work.**
- P14's limits (`F:115`: 600 K and 800 kPa) combined with "clamp-or-continue" (P3, `F:104`) destroy heat a link puts into a store at its limit. A gas store of EL-039's default size given a heating link (EL-039 itself declares none) reaches 800 kPa about 83 s into 120 W of heating (section 3).
- T6 (`T:200`) debits drive work from the node "(T13)", but T13 has no work term (see the H-1 replacement).

Replacement, added to T9: "A gas store at its material's temperature or pressure limit accepts no heat: a link or source that would raise it moves nothing in that direction, and the paying store keeps its energy."

**M-9. Two owners are written as conditional or vague.**
- `L:109` (W13): "Owner: ENGINE-LIQUID; ENGINE-GAS-NETWORK adds Pa and m³ if it lands first". Owner decision 18 fixes the order. Replacement: "Pa and m³: ENGINE-GAS-NETWORK (G11); m³/s: ENGINE-LIQUID".
- `L:37` (L12 c): the capture region is "EL-229 and EL-240's capability, in the story text". Replacement: "a row of ENGINE-LIQUID (Batch F E-7)".

**M-10. Stream sample paths are ballistic, but the query interface has only straight casts.** `L:33` (L8) traces samples under gravity to where they land; `L:43` (L18) bounds "a fixed number of sample paths per free end" but not the casts per path. Add to L18: "each sample path is a declared fixed number of segment casts of ENGINE-SPATIAL-QUERIES along its ballistic arc, ending at the world bound".

**M-11. A live requirement anchor contradicts O8.** `docs/planning/requirements.md:3080` (`sequence-task-398`) still reads "Existing laser-only reception is not silently treated as flashlight reception". Owner decision 9 orders that row reworded, and O8 (`O:178`) already applies the one-receiver law. The requirement edit is outstanding and belongs to the coordinator.

### Low

1. `L:32` (L7): the stability "worst cases" are stale. TH-18 declares a 0.02 m² store with a pipe-bore mouth (`TH-18-condenser.md:40`): against a large store c = 0.08349 × 50/480 = 0.0087 and c² = 7.6 × 10⁻⁵ m; between two such stores c² = 3.0 × 10⁻⁴ m. That is still 33 times under 0.01 m, but "four orders" is false. The thermal claim (T9, 0.004 J/K against 16 W/K, G·dt/C 16 to 25) and the gas clamp need for TH-19's 1 × 10⁻⁴ m³ cap (0.40 g per substep through 2 × 10⁻⁴ m² against 0.12 g held) are both confirmed; the exact exchange and the clamp carry them.
2. `T:205` (T11) says "2.2 litres a second", which L1 forbids (`L:26`). Use 0.0022 m³/s. In the same item, 1153.2 ÷ 691.7 = 1.6672, which is above the 5/3 the text calls "the upper limit"; use "≈ 5/3".
3. `L:42` (L17) lacks T12's phase-row exception (`T:206`): grams on the store ledger and no zero-flow threshold (`readiness/batch-n.md:150`, item 3). L1's readout "m³ to two decimals" also shows 0.00 for the kettle's 0.00125 m³. Add: "Liquid moved by a phase row is checked as mass in grams (T12) and is not subject to the zero-flow threshold."
4. `R:185`: "Items settled by coordinator decisions 16 and 21 … are marked **(owner)**" mislabels coordinator items. Mark them "(coordinator)" as O does.
5. T4 and T8: friction work should count only the accepted trial solve of each substep, because replays re-run the solve (`WorkshopPhysicsEngine.Motors.cs:30-60`). Add "(the accepted solve only)".
6. L16 (`L:41`): a work target at `:994` spends the grant made at `:1011` of the previous substep, across the tick boundary at s = 0. State this, and state that an unspent grant is lost on Reset.
7. T9: say that a node holding at a phase threshold counts as "a held temperature" (C_r = the other node's C). Otherwise a melt limited by the node's sensible C is slower than the link allows.
8. `T:205` (T11): the saturation table is "owned by TH-17". That makes shared material data element-owned, although TH-18, TH-19, TH-20 and TH-35 use it. Make it part of the declared "game steam" material row.
9. `T:197` (T3) and `T:198` (T4): the radiant credit lands in ENGINE-THERMAL before receivers exist. The in-progress `epics.md` says it "is delivered as an interface whose end-to-end proof lands in ENGINE-OPTICAL"; say the same in T3. T4's "its availability set by a threshold node" reads as mandatory, while O9 makes it optional (EL-154 has none).
10. F (`F:80`, `F:216`): state that the store is a thermal node under T2 with ambient conductance 0 and no faces, so "adiabatic" is a declaration and not a second rule.
11. Citations. `O:174` cites `WorkshopGpuDevice.cs:245` for contact loads; it is the electrical read, and the loads are at `:246-249`. `L:37` cites `BodyMassSourceDeclaration.cs:18` for "adds at centre"; `:18` is `ResolvedBodyMass`, and the "translation only" comment is at `:20`.
12. `requirements.md:3198` (radiation-01) says "Moving the capsule farther away lowers the meter reading". Flat-inside-1 m (coordinator decision 21) keeps this only for moves that end beyond 1 m. Record that scope in R3 or in the RAD-01 row.

## 5. First-round findings: resolved or not

- **Batch F.** Resolved: C-1 (one level relation, L4 at `:29`; L11 at `:36` reproduces both formulas, re-derived), C-2 (pending overflow), C-3 (L7 c), C-4 (sample paths, no exemption), C-5 (interior, V + V_displaced), C-6 (sockets with ENGINE-CAPABILITY-INSTANCES, 0.20 m and 20°), C-8 (head cap), C-9 (bound in L7, flow floor in L17), G-1, H-5, M-1, M-7, M-8. Partly resolved: C-7 (M-4). Still open: E-7 (owner wording, M-9).
- **Batch H (EL-039 only).** Resolved: P-H1 (F9), P-H2 (F3, F11), P-H3 (A9 on EL-041), P-H4 (F10), P-M4 (boss collider; foot at −0.23), P-M8, X-1 (section 7 form), and the Fan `:19` and Power-drawer citations. P-M9's clamp is in, but the step rate is wrong (M-1).
- **Batch K.** Resolved: C-1 (O4), C-2 (status), C-3 (shares scaled; Relay τ × mask), C-4 (O9), C-5 (O6 with coordinator decision 25), C-6 (O5; origin inside a collider in O2), C-7 (O10, O11), C-9 (O12 hands it to ENGINE-OPTICAL), M-9 (R5 untested), L-2, L-4, L-7. Partly resolved: C-8 (M-3).
- **Batch N.** Resolved: N1 (T9), N2 (T8), N3 (T3 contact link), N4 (region link), N5 (wording), N6 (T12; TH-04 needs 40 s), N7 (citations), E1 (owner decision 19 in T5 and T11), C2 (T12; L17 not mirrored, Low-3), owner item 3 (EL-001 `liquid_temperature`, `L:61`). N5's credit form now double-counts (H-4).
- **Batch O.** Resolved: C-1, C-2, C-3, C-5, C-6, C-7, C-8, C-9, C-10, C-11 (minus the cap, M-6), C-12, M-1, M-12, X-3, X-4, and H-6 (store credit only). Partly resolved: C-4 (the own-body rule differs from optics, M-3).

## 6. Engine-story ownership (check 6)

Every capability the conventions need names a single story from decision 3a, coordinator decisions 16, 21 and 25, or an existing `epics.md` story, with five exceptions: H-3 (credited store), M-5 (gas–liquid edge), M-9 (W13, L12 c), M-6 (R12's crossing cap given to ENGINE-RADIATION although the cast is ENGINE-SPATIAL-QUERIES'), and T3's radiant credit order (Low-9). No coined name was found in the five owners.

State of `epics.md`:
- **At HEAD (commit `7c019d64`).** None of the 3a programme stories (DOC-ENGINE-STATUS, SPATIAL-QUERIES, CONTACT-OBSERVATIONS, SENSOR-VOLUMES, ISLANDS-SLEEP, ONE-WAY-INSTALL, SUPPLY-DEMAND, CAPABILITY-INSTANCES, TICK-ALLOCATION, SOA-SIMD) exists, and neither do JOINT-KINDS, SIGNAL-NODES, PARAMETER-BOUND-FRAMES, RUN-LENGTH or CONNECTION-MATERIALS. ENGINE-GAS-NETWORK (`:4423`), -OPTICAL (`:4434`), -FIELD-FORCE (`:4445`), -ASSEMBLY-JOINTS (`:4456`), -FRACTURE (`:4467`), -LIQUID (`:4489`), -THERMAL (`:4500`), -RADIATION (`:4511`) and -IGNITION-BLAST (`:4522`) exist in their 21 November form, without the conventions.
- **On disk at 08:23 (rewrite in progress, +89/−120 lines against HEAD).** A new engine-programme section has DOC-ENGINE-STATUS, ENGINE-SPATIAL-QUERIES (ordered-crossing segment cast, cone queries), ENGINE-CONTACT-OBSERVATIONS and ENGINE-SENSOR-VOLUMES. The other programme stories mostly do not appear yet; JOINT-KINDS, SIGNAL-NODES, PARAMETER-BOUND-FRAMES, RUN-LENGTH and CONNECTION-MATERIALS appear only in the ordering note.

The line citations in the five owners (`epics.md:4434`, `:4500`, `:4511`, `:4512`) will drift with the rewrite. The proposed story texts the owners point to are in `readiness/batch-f.md` section 6, `batch-g.md` section 6, `batch-h.md` section 4, `batch-k.md`, `batch-n.md` sections 5–6 and `batch-o.md` section 6.

## 7. Check 1 (one data-driven law set per domain)

No convention routes behaviour through element code or branches on a part, kind or package. Store variants (sealed compliant, porous, bound capacity), facing rules (Lambert, Uniform) and responses are declared variants of one capability. The exceptions found are:
- the channel-class rules (M-3);
- element-owned material data (Low-8);
- unbounded or scene-proportional work (M-6, M-7, M-10).

T10, L18 (apart from M-6 and M-10), O11 (apart from M-7) and EL-039 conformance point 6 bound per-tick work by compiled declarations through ENGINE-SPATIAL-QUERIES.

## 8. Check 7 (requirement criteria)

Kept: element-001, element-039, element-176, thermal-04, radiation-08 ("separate on/off thresholds" is met by 0.2 W and 0.15 W), radiation-foundations, IX-15, IX-16 (steady inventory sized to 300 s), current-cat-008 and current-cat-041 (16 interactions, 128 segments). Outstanding: `sequence-task-398` (M-11). Narrowed with a coordinator decision: radiation-01 (Low-12).

## 9. Not verified

- Nothing was run.
- The behaviour of `ElectricalSupply.PhaseSeconds` (1/120 s) at 60 Hz and 240 Hz cadence, and so whether the 1 J-per-tick chain holds there.
- TH-17's saturation table, and so T11's "T_sat rises about four times faster".
- Whether any optical row outside the specs I grepped asserts a reading inside 1 m.
- The full `epics.md` rewrite (in progress); batch-g section 6 and batch-h section 4 texts beyond the lines cited.
- Legacy `@a6c914e` citations; test bodies (only names and lines were checked); the TH-05 friction chain (taken from the Batch N audit).

## Repair (23 November 2026)

Applied to the five owners under coordinator decision 26 (and 28), with three later coordinator flags (`reaudit-group-2.md` EL-029 and the L8/L9 flag; `reaudit-group-3.md` items 3 and 10). Nothing was built or run. Every finding above was checked against its cited text first.

**One combined tick order.** Stated once, in optical convention 4 (`EL-176`, anchor `#combined-tick-order`): tick start; per substep gas → liquid (enthalpy per transfer; gas edge reads the just-committed gas pressure and debits the store) → supply grants → solve with replays (friction from the accepted solve) → commit; the one optical/radiation solve, ending with the store credit from the collider record; the thermal step (credits, then T3's record move, then links and phase rows); sensors and commit; phase-row mass enters at the boundary. dt is defined there. L16, F5, T8 and radiation 11 cite it and do not restate it.

**Per owner.**
- **EL-001:** L1 readout (Low-3); L2 heat moves with each volume transfer per substep (H-2); L3 four accounts with `Evaporated`, evaporation never in `Drained` (H-1); L5 an immersed mouth outside every interior is sealed, never a free end (EL-029 flag); L7 worst case is TH-18's sump, c² = 7.6 × 10⁻⁵ / 3.0 × 10⁻⁴ m (Low-1); L9 overflow is an L8 stream carrying the vessel's velocity, L8 governs (L8/L9 flag); L10 `+ Evaporated`; L12 citation `:20` (Low-11), capture region a row of ENGINE-LIQUID (M-9), air buoyancy is declared lift (decision 28); L15 gas input debits the gas store, edge owned by ENGINE-LIQUID (M-5); L16 cites the combined order, grant lag and Reset loss (Low-6); L17 phase-row grams (Low-3); L18 ballistic casts, no scene refusal (M-6, M-10); W9; W13 (M-9).
- **EL-039:** F3 one port record, role Bidirectional, no new `PortRole`, throat on the connection kind; port table, Typed ports row and G6 to match (M-4); F5 per substep, before liquid, sequential clamp, 0.40 g, mixture by total pressure (M-1); F6 qualified to the default tick; P1 Dalton mixture and P14 per-component in the disposition column (H-5; the harvested fact text is left as legacy evidence); P14 limit accepts no heat (M-8); thermal note (Low-10).
- **EL-176:** O1 one flat near-field rule (M-3); O2, O7 booked once, heat only as T3 moves it (H-4); O4 the combined order and dt (M-2, M-5, Low-5, Low-6, Low-11); O5 one own-body rule for every channel (M-3); O6 default response per channel; O3 and O8 a receiver on an Absorb face books what it receives (collimated plus conserved diverging shares) once on its collider's record, and a reading credits nothing; O9 credit taken from the record at the end of the solve, store owned by ENGINE-THERMAL (H-3); O11 cone-query candidates (M-7).
- **TH-04:** T2 advection and gas mixtures; T3 radiant credit from the absorbed record after any store credit, interface proved in ENGINE-OPTICAL (H-4, Low-9); T4 accepted solve only, absorbed energy, store owner, optional availability node (H-3, Low-5, Low-9); T5 edge owned by ENGINE-LIQUID, `Evaporated`, and **condensing has one form, the wall-limited row** ṁ = G × (T_dew − T_node) ÷ L_v(T_dew) on a wetted link (group-3 item 3); T6 heated-envelope lift binding (decision 28); T8 cites the combined order; T9 held-temperature rule and gas-limit rule (Low-7, M-8); T10 no scene refusal (M-6); T11 m³/s, ≈ 5/3, saturation table part of the game-steam row (Low-2, Low-8); T13 sink, source and work terms, boiling counted once (H-1, M-8).
- **RAD-08:** tags retagged by decision, "(owner)" kept only for the 21 November steady-source decision (Low-4); R3 "one change" removed, radiation-01 scope recorded (M-3, Low-12); R4 own body is optical 5's rule; R7 a reading is flux; R9 × dt (M-2); R10 rewritten, delivered-store claim deleted (H-3, H-4); R11 cites the combined order; R12 cone candidates, no crossing cap (M-6, M-7); an electrical node `relay` (Switch, 12 V, 0.05 Ω, 100 W) added, since `BundleValidator.cs:867` refuses a supply part without one (group-3 item 10).

**Disagreements with the replacement texts.** (1) Decision 26's "the optical forms" was read as: near field = the flat form the audit proposed for O1 (decision 21's "flat inside 1 m" still binds, and RAD-08 A15 depends on it); own body = O5's form, reworded so it works for attenuating channels (the cast ends at the aperture face, or at a Uniform receiver's sample point). (2) M-1's "first in the substep with the liquid step" became "before the liquid step" (decision 26). (3) H-1's L3 sentence also had to exclude evaporation from `Drained`, or L10 counts it twice. (4) H-4's T3 text alone lets a store credit and a thermal face draw 1.2 from 1; the credit now takes its fraction from the record at the end of the solve, and T3 moves a × the remainder. (5) H-3's store sentence also covers parameter-bound stores (EL-154). (6) M-2: rates are per dt, but the supply phase stays 1/120 s in code; the order says what a grant is at 60/240 Hz is ENGINE-SUPPLY-DEMAND's, not established. (7) L5's outside-every-interior case is resolved as sealed, not a free end: a free end would spill a sunk hull's bilge to `Spilled`.

**Dependent specs that need updating** (not edited here).
- Near field `max(1 m, d)²`: CAT-029 (:71, :102, :128), CAT-059 (:64, :115, :171), EL-153 (:87), EL-082 (:47), EL-088 (:81), `readiness/batch-l.md:58`. Numbers beyond 1 m are unchanged; any sample inside 1 m loses its facing factor.
- Own-body skip: RAD-10 (:32), RAD-21 (:32), EL-129 (:32), RAD-03 (:90), RAD-15 (:102, :157). RAD-21 and EL-129 are Lambert faces read from the front, so their numbers hold. **RAD-10 changes materially:** its one Uniform sample sits at the centre of the top face of a 0.50 × 0.40 × 0.50 m box on the default row, so a cast from below crosses up to 0.40 m of the badge (PhotonMedium e^(−0.15 × 0.40) = 0.94; BetaMinus e^(−150 × 0.40) ≈ 0) and from the side up to 0.25 m (0.96; ≈ 0). A tumbling parcel keeps about 94–100 % of a photon reading but loses Beta-minus dose whenever its top face points away. This is a consequence of decision 26's chosen form and goes to the coordinator; no second Uniform rule was written.
- Accumulator or credit `× 1/120 s`: RAD-09 (:31), RAD-10 (:33), EL-154 (:115), CAT-059 (:28, :65), RAD-12 (:103). (CAT-059 :29/:68, CAT-061, EL-179, EL-180, EL-196 and `batch-d.md` quote the supply phase, which stays 1/120 s.)
- "per 120 Hz tick" in the optical tick-order bullet: CAT-006, -008, -011, -012, -021, -029, -031, -032, -036, -038, -040, -041, -043, -044, -045, -046, -047, -055, -056, -059, -068, -072; `readiness/batch-n.md:124`.
- Work bound `N × 128 × (1 + K)`: CAT-006, -008, -011, -012, -021, -031, -036, -045, -046, -059, -072, EL-211 (:71), `batch-e.md:86`, `batch-k.md:99`; CAT-029 (:120).
- Store credit "at commit" and the store row's owner: CAT-059 (:28, :71, :88), EL-211 (:25, :40, :52, :69), EL-154 (M7, R24 give the credited store to ENGINE-OPTICAL; the store is ENGINE-THERMAL's), `batch-e.md`, `batch-k.md`, `batch-h.md:102`.
- Radiant credit from a reading × absorptance: TH-37 (:28, G3 :71), TH-32 (:33, K1 :65, :145), TH-07 (:112), `batch-n.md:66, :120`.
- Crossing cap: RAD-07 (:145), `batch-o.md:97`, `engine-stories-index.md:161`.
- Scene-capacity refusal: `batch-f.md:109`, `batch-n.md:125`.
- Gas–liquid edge owner: `batch-n.md:68`.
- `PortRole` `GasIn` "(new)": EL-040, EL-041, EL-042, EL-043, EL-045 (port tables); check EL-037, EL-038, EL-230, EL-231.
- Gas step per tick, 1.6 g: EL-038 (:60).
- W13 owner of m³ (now ENGINE-GAS-NETWORK): EL-002, EL-014, EL-015, EL-027, EL-033, EL-034, EL-168, EL-171, RAD-17 (:66); Pa: EL-032 (:71), `engine-stories-index.md:115, :165`.
- L12(c) capture region now an ENGINE-LIQUID row: EL-229 (:64), EL-240 (:24, :59).
- Overflow start velocity (L9): EL-025 (moving chamber now pours with its rim velocity), EL-168 (:65), EL-171 (:28, :69).
- Immersed mouth outside every interior is sealed (L5): EL-029 (its V5 row).
- Single-species gas and the condensing rule: TH-17 (:39 condense row gets the wall-limited rate, :83, :106, :174, :196), TH-18 (:42, cite T5 and the game-steam table rather than "the TH-17 condense row" and "the TH-17 table"), TH-19 (its return row).
- Evaporation account: TH-28 (check its ledger rows against L10).

**Not done.** M-11 (`requirements.md:3080`, `sequence-task-398`) and the radiation-01 wording are requirement edits for the coordinator. The dependent specs above are not edited. `epics.md` (ENGINE-THERMAL credited store, condense row, gas edge in ENGINE-LIQUID) was not touched. Whether a grant scales with dt at 60/240 Hz remains unverified. T9's "the paying store keeps its energy" does not yet hold for an electrical heat target, whose grant is debited inside the substeps before the thermal step refuses the credit; ENGINE-THERMAL must have such a target request nothing while its node is at its limit (a design point for that story, not written into T9). The RAD-10 own-body consequence above needs a coordinator word. No other liquid spec was found expecting an immersed port to drain once lifted out (EL-014, EL-015, EL-034 and EL-171 declare none).
