# Readiness summary — batch `new-identities` (EL-217 to EL-241)

> **Current pointers (cross-batch pass, 23 November 2026).** This summary is a dated record. Retired story names used below map to their current stories in the [engine stories index](engine-stories-index.md#3-retired-and-renamed-names) section 3; palette orders are the [palette allocation](palette-allocation.md)'s (coordinator decision 36); `epics.md:NNNN` citations are as of this summary's date (the specs cite stories by heading).

Written 21 November 2026; repaired 22 November 2026 against the independent audit `docs/planning/elements/readiness/audits/elements-new-identities.md` and the owner's engine-conformance addendum of the same day; **propagated 23 November 2026** to the binding [owner and coordinator decisions](owner-decisions-2026-11-22.md) (owner 0–24, coordinator 16, 21 and 25) and to the hand-backs of Batches F, H, I and N (section 5). Checkout `spec-drafts` (published main `8d1d68aa` plus drafts); `origin/main` read at `25df3cd5`. No build or test was run. Counts are present / untested / narrower / missing.

**Second-audit repair, 23 November 2026** (`docs/planning/elements/readiness/audits/reaudit-group-1.md`; coordinator decision 27). EL-225, EL-227 and EL-235 were Ready with fixes in that audit; every finding is applied, and each stays blocked on the engine programme (owner decision 3a) and its named stories. EL-225: the prongs are 1.9 m tall (a launched ball's centre crosses the prong line 1.29–1.58 m above the base top); S2 and S6 assert the Released answer the delivered retention gives a slider held below travel 0 (Empty only within 5e-4 m of it; the hold keeps the deepest point of the first fall, up to twice the static sag), bounded by 2(m·g·sin θ)² ÷ k + 0.5 J; S1 checks the band spans. EL-235: J2 asserts Released (about 0.55 J with the head held near 0.083 m); the lid rests at its authored 0.64 rad at Reset and about 0.47 rad in Run; the ball seats 0.77 m from the hinge (2.4–4.4 m/s, 0.65 m rise); J6 pulses on the next tick. EL-227: restitution is the product of the two colliders' values, so the blow chain is nearly dead (staged ball 1.4–1.8 m/s, I1 floor 1.2 m/s); I7 runs at `well_energy` 5 with a 5 m/s strike; an output seat pad, load node and lamp make output occupancy visible (10 colliders, 2 load sensors, 4 driver bindings). EL-224 and EL-226 were checked for the same release-answer defect: neither asserts an Empty answer and both declare new revolute holds, so neither changed. **For the coordinator:** a pulse on a resting spring-returned retention held below travel 0 answers Released; the delivered law re-arms only within 5e-4 m of travel 0 (`CuriousContraptions.Simulation/SimPrismaticRetention.cs:85`), so where damping or friction stops the return short (EL-225's damped cradle; possibly EL-235's lid friction) the coordinate stays released, and its winding disengaged, until Reset (EL-225 and EL-235 section 6; CAT-071 preamble). The element-free remedy belongs with the holds on a coordinate of `ENGINE-JOINT-KINDS`.

## 1. One row per identity

Conformance is this repair's own reading of each spec against the [conformance standard](engine-conformance.md) (each spec has an "Engine conformance" section with its layer split, what moves each art node and its flat-state counts). It is not an independent verdict. "Conforms; gap named" means the spec is declaration data only and each missing or narrower capability is stated element-free with one owning story.

**Every identity is blocked on the engine programme (owner decision 3a), and under owner decision 22 nothing in this batch runs before it.** The column "Owning stories" names the stories beyond the programme as a whole; "Programme stories relied on" names the 3a stories each spec cites in particular. `ENGINE-ASSEMBLY-JOINTS` now keeps only player-made connections and commanded release (3a; engine design audit §6 item 9); every other capability this batch had given it is re-owned in section 3.

| Identity | Name | Disposition | Planned package | Capabilities | Programme stories relied on | Owning stories (beyond the programme) | Other prerequisites | Collaborators | Tier | Ready to plan a slice | Conformance |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| EL-217 | Reload shuttle | Single element | `reload_shuttle` | 6/1/0/1 | SENSOR-VOLUMES, SUPPLY-DEMAND, CAPABILITY-INSTANCES, ISLANDS-SLEEP | none | coverage reader (slice-owned) | Cannon, Linear pusher, Counter, EL-241 | engine | no: blocked on the programme | Conforms; gap named |
| EL-218 | Crank-slider | Single, after its story | `crank_slider` | 4/0/0/1 | ISLANDS-SLEEP | ENGINE-MECHANICAL-LINKS (position-dependent coupling) | coverage reader | Motor, Clock, Counter, EL-241 | engine | no: programme, MECHANICAL-LINKS | Conforms; gap named |
| EL-219 | Rack-and-pinion slide | Single, after its story; Duplicate-closure check against EL-053 (Batch I) | `rack_slide` | 5/1/0/1 | ISLANDS-SLEEP | ENGINE-ASSEMBLY-JOINTS (attachment socket) | coverage reader | Motor, EL-221, Mirror | engine | no: programme, ASSEMBLY-JOINTS | Conforms; gap named |
| EL-220 | Docking lift | Single element | `docking_lift` | 8/0/0/1 | SUPPLY-DEMAND, ISLANDS-SLEEP | none | coverage reader | Ball detector, Both gate, Powered gate | data-only possible | no: blocked on the programme | Conforms; gap named |
| EL-221 | Parallel gripper | Single, after its stories | `parallel_gripper` | 4/1/0/3 | SUPPLY-DEMAND, CAPABILITY-INSTANCES, ISLANDS-SLEEP | ENGINE-ASSEMBLY-JOINTS (mounting socket); ENGINE-JOINT-KINDS (Blocked-phase contact) | EL-219, EL-220; coverage reader | Hold timer, Funnel | engine | no: programme, both stories | Conforms; gap named |
| EL-222 | Pinball flipper | Single element | `flipper` | 5/2/0/1 | SUPPLY-DEMAND, CAPABILITY-INSTANCES, ISLANDS-SLEEP | ENGINE-JOINT-KINDS only if an untested row fails | coverage reader | Battery, Hold timer, Delay, Funnel | engine | no: blocked on the programme | Conforms; gap named |
| EL-223 | Passive impact scoop | Single element, **in delivery by another session** | `impact_scoop` | — | — | — | — | Ramp, Pipe, Receiver | engine | in delivery; this draft is dropped at merge | not assessed |
| EL-224 | Counterweighted scoop catapult | Single, after its stories | `scoop_catapult` | 5/0/0/1 | ISLANDS-SLEEP | ENGINE-JOINT-KINDS (the hold); ENGINE-ASSEMBLY-JOINTS (commanded release) | coverage reader | Weight, Pulley, Linear pusher, EL-063, EL-014 | engine | no: programme, both stories | Conforms; gap named |
| EL-225 | Latched slingshot cradle | Single, after its stories | `slingshot` | 5/0/1/0 | ISLANDS-SLEEP | ENGINE-JOINT-KINDS (one-way hold of an unwound slider); ENGINE-ASSEMBLY-JOINTS (commanded release) | coverage reader | Weight, Pulley, EL-063 | engine | no: programme, both stories | Conforms; gap named |
| EL-226 | Hinged elastic catapult | Single, after its stories | `elastic_catapult` | 3/1/0/1 | ISLANDS-SLEEP | ENGINE-JOINT-KINDS (revolute hold, spring rest angle); ENGINE-ASSEMBLY-JOINTS (commanded release) | coverage reader | Motor, Delay, Conveyor | engine | no: programme, both stories | Conforms; gap named |
| EL-227 | Magnetic impulse relay | Single, after its stories | `impulse_relay` | 3/0/0/4 | SPATIAL-QUERIES, SENSOR-VOLUMES, ISLANDS-SLEEP | ENGINE-FIELD-FORCE (one field text plus occupancy capacity, G-C); ENGINE-PARAMETER-BOUND-FRAMES (force bound to `well_energy`) | EL-091 with the responding property; coverage reader | EL-091, EL-060, Pipe, Bowling ball | engine | no: programme, both stories | Conforms; gap named |
| EL-228 | Zipline signal carriage | Single, after its stories | `zipline` | 4/1/1/0 | CONTACT-OBSERVATIONS, ISLANDS-SLEEP | ENGINE-JOINT-KINDS (hold of an unwound slider); ENGINE-ASSEMBLY-JOINTS (commanded release, from an internal contact node) | coverage reader | Impact switch, Pulley, Weight, Linear pusher, EL-063, Bell | engine | no: programme, both stories | Conforms; gap named |
| EL-229 | Hydraulic piston | Single, after its stories | `hydraulic_piston` | 1/0/1/2 | SPATIAL-QUERIES, ISLANDS-SLEEP, CAPABILITY-INSTANCES | ENGINE-GAS-NETWORK (fluid port record); ENGINE-LIQUID (E-7, with the wording handed to Batch F); ENGINE-RUN-LENGTH (60 s rows) | tank, pipe, tap; row Y7 in EL-022's slice | EL-001, EL-008, EL-240, EL-022 | engine | no: programme, GAS-NETWORK, LIQUID | Conforms; gap named |
| EL-230 | Spring-return pneumatic piston | Single, after its story | `pneumatic_piston` | 1/0/1/1 | SOA-SIMD, CAPABILITY-INSTANCES, ISLANDS-SLEEP | ENGINE-GAS-NETWORK | EL-039, EL-038 (after CONNECTION-MATERIALS), EL-041; coverage reader | EL-039, EL-041, EL-045 | engine | no: programme, GAS-NETWORK | Conforms; gap named |
| EL-231 | Double-acting pneumatic piston | Single, after its story | `double_acting_piston` | 1/0/1/1 | SOA-SIMD, CAPABILITY-INSTANCES, ISLANDS-SLEEP | ENGINE-GAS-NETWORK (two chambers on one coordinate) | EL-039, EL-038, EL-041, EL-230; Batch H's F3 exception; coverage reader | EL-041, Clock | engine | no: programme, GAS-NETWORK | Conforms; gap named |
| EL-232 | Gears | Single, after its stories | `gear` | 4/0/2/3 | CAPABILITY-INSTANCES, ISLANDS-SLEEP | ENGINE-MECHANICAL-LINKS (mesh material, loop rule); ENGINE-PARAMETER-BOUND-FRAMES (`teeth` bindings) | none (S347 resolves on main) | Motor, Conveyor, Clutch | engine | no: programme, both stories | Conforms; gap named |
| EL-233 | Generator | Single, after its story | `generator` | 4/0/0/2 | SUPPLY-DEMAND (also owns the shaft-paid source), ISLANDS-SLEEP | ENGINE-SUPPLY-DEMAND (shaft-paid source, proposed clause) | coverage reader | Windmill, Motor, Lamp | engine | no: blocked on the programme | Conforms; gap named |
| EL-234 | Mouse motor | Single element | `mouse_motor` | 6/1/0/1 | SUPPLY-DEMAND (stamina debit), CONTACT-OBSERVATIONS, ISLANDS-SLEEP | none | none (S349 resolves on main) | Conveyor, Clutch, Wound spring | engine | no: blocked on the programme | Conforms; gap named |
| EL-235 | Jack-in-the-box | Single element | `jack_in_the_box` | 4/1/0/0 | ISLANDS-SLEEP | none | coverage reader | Motor, Ramp, Impact switch | engine | no: blocked on the programme | Conforms |
| EL-236 | Egg timer | Single, after its story | `egg_timer` | 6/1/1/1 | — | ENGINE-SIGNAL-NODES (rearming-timer row); ENGINE-RUN-LENGTH | coverage reader | Impact switch, Lamp, Latch | engine | no: programme, SIGNAL-NODES | Conforms; gap named |
| EL-237 | Boxing glove | Single element | `boxing_glove` | 7/1/0/1 | SUPPLY-DEMAND, ISLANDS-SLEEP | none | coverage reader | Battery, Clock, Conveyor | engine | no: blocked on the programme | Conforms; gap named |
| EL-238 | Selective trap door | Single element | `trap_door` | 4/1/0/0 | ISLANDS-SLEEP | none | none (S391 resolves on main) | Ramp, Receiver, balls | engine | no: blocked on the programme | Conforms |
| EL-239 | Baseball | Delivered on main as its own slice | `baseball` | — | — | — | — | — | — | delivered; this batch's draft file is removed at merge and main's file governs | not assessed |
| EL-240 | Waterwheel | Single, after its stories | `waterwheel` | 1/0/1/2 | SPATIAL-QUERIES, ISLANDS-SLEEP, CAPABILITY-INSTANCES | ENGINE-GAS-NETWORK (fluid port record); ENGINE-LIQUID (E-7); ENGINE-RUN-LENGTH (60 s rows) | tap, basin; sequence-task-only anchor; row X6 in EL-022's slice | Conveyor, EL-022, EL-233 | engine | no: programme, GAS-NETWORK, LIQUID | Conforms; gap named |
| EL-241 | Ball hopper | Single element | `ball_hopper` | 6/2/0/2 | SUPPLY-DEMAND, ISLANDS-SLEEP | ENGINE-JOINT-KINDS (spring rest position) | sequence-task-only anchor; after EL-237 | Clock, Powered gate, Receiver | engine | no: programme, JOINT-KINDS | Conforms; gap named |

Totals. Disposition: 23 single elements in this batch's hands, 1 in delivery elsewhere (EL-223), 1 delivered (EL-239). Readiness: **0 ready**; all 23 are blocked on the engine programme (owner decisions 3a and 22). Beyond the programme, 7 need no further story (EL-217, EL-220, EL-222, EL-234, EL-235, EL-237, EL-238), and 16 wait on named stories: ENGINE-JOINT-KINDS 7 (EL-221, EL-224, EL-225, EL-226, EL-228, EL-241, and EL-222 as fallback), ENGINE-ASSEMBLY-JOINTS 6 (EL-219, EL-221, EL-224, EL-225, EL-226, EL-228), ENGINE-MECHANICAL-LINKS 2 (EL-218, EL-232), ENGINE-PARAMETER-BOUND-FRAMES 2 (EL-227, EL-232), ENGINE-FIELD-FORCE 1, ENGINE-GAS-NETWORK 4 (EL-229, EL-230, EL-231, EL-240), ENGINE-LIQUID 2, ENGINE-SIGNAL-NODES 1, ENGINE-RUN-LENGTH 3 (EL-229, EL-236, EL-240), ENGINE-SUPPLY-DEMAND's shaft-paid clause 1 (EL-233). Conformance: 2 conform; 21 conform with named engine gaps; 0 do not conform; 2 not assessed (EL-223, EL-239).

## 2. Coverage binding (planned, not yet live)

Main binds a named package through a final section of its element spec, read by `tools/Coverage/NamedPackageSources.cs` (`origin/main` `25df3cd5`; pattern EL-239 Baseball): Package, Named identity, Acceptance (`todo-N` and its co-anchored `sequence-task-M`), Decision (an `SNNN` anchor), Mode records. No spec in this batch carries that section, because the reader parses every `EL-*.md` with its heading; each spec states its planned values under "Coverage binding (planned, not yet live)" and says the section is written in the syntax current on main when its slice starts (audit S1).

- EL-232 (S347), EL-234 (S349) and EL-238 (S391) have an S owner anchor that resolves on main; the section is still written only by each delivering slice, after the programme.
- The other identities are slice-owned. They are admitted only once main's reader admits a slice-owned package: the EL-223 slice plans that change (renaming `**Decision:**` to `**Ownership:**`); otherwise it falls to the coverage follow-on story S7, which owner decision 15 also has widen the reader for companion packages and TH, RAD and GAP identities.
- EL-240 and EL-241 have no `todo` anchor (`requirements.md:2981`, `:2724`); see the ledger proposal in section 6.

## 3. Decisions

### Owner decisions applied (22 and 23 November 2026)

| Decision | Applied in |
| --- | --- |
| 3a, 22: engine order; nothing in this batch runs before the programme | Every status line and Prerequisites paragraph says "Blocked on the engine programme (owner decision 3a)" and names the programme stories it relies on; no row is "ready now", "declarable today" or "ready once EL-223 publishes" |
| 1, 2 and coordinator 25: solid strands; delivered rigid connections stay rigid and schematic | Rope rows (EL-224, EL-225, EL-228, and others using a rope) state that ropes are solid strands once `ENGINE-CONNECTION-MATERIALS` (solid strand) is delivered and that the delivered rope's missing collider is a recorded defect of that story; their routes run clear of other colliders. Hoses (EL-230, EL-231) are EL-038's solid strand. EL-228's cable is a static solid bar, not a strand |
| 6 and coordinator 25: declared spring rest position for spring-returned blades, hatches and gates | EL-241's escapement (the spec calls it "the gate"): rest position −0.4 m, stiffness 20 N/m (8 N preload against 6.9 N of pocket weight; drive keeps 15 N to spare); new row H9 (upright and slider vertical). Checked and not applicable: EL-217 (no spring), EL-221 (jaws), EL-222 (paddle), EL-230 (piston rod), EL-231 (no spring), EL-237 (punch return), EL-238 (counterweight, no spring) |
| 8: hopper and shuttle feed singly for balls 0.62–0.78 m with no size setting; gear ratios follow tooth counts | EL-217, EL-241 (owner item removed); EL-232 (audit O2 closed) |
| 13, 17: Run length per level, at most 300 s | EL-236 E3 (60 s delay: Run length 70 s); EL-229 and EL-240 (60 s stall guard: Run length 60 s); each names `ENGINE-RUN-LENGTH` |
| 15: companion packages; S7 widens the reader | Section 2 |
| 16 (coordinator): one fluid port contract owned by `ENGINE-GAS-NETWORK`; unpowered pump open; threshold node, oscillator and choice-bound values; gravity one field law | EL-229, EL-240 cite the one fluid port record and liquid mouths (EL-001 L5, L6); EL-229 Y7 and EL-240 X6 note that a stopped loop drains back unless a check valve is placed; EL-227 uses the one field text |
| 18: order programme → GAS-NETWORK → LIQUID → THERMAL | EL-229, EL-240 (after GAS-NETWORK); EL-230, EL-231 (GAS-NETWORK first after the programme) |
| 20: every liquid store is a thermal node | EL-229's chamber (starts empty at the thermal conventions' ambient; no row reads it) |
| 21 (coordinator): Weight mass stated where it meets liquid; spring rest position in `ENGINE-JOINT-KINDS` | EL-229, EL-240 state that no Weight meets liquid; EL-241 |

### Decisions made under the standing delegation (22 November 2026, kept)

| Scope | Decision | Reason |
| --- | --- | --- |
| EL-230, EL-231 | Pressure in pascals absolute with ambient 101 325 Pa and the thermal conventions' ambient temperature, citing EL-039 F1–F2 and F6 tolerances | The pneumatic conventions; one unit everywhere |
| EL-217, EL-220, EL-221, EL-222, EL-237, EL-241 | Declared drive power within a default Battery's 120 W at the defaults, or the `power_limit` needed is named | A part that cannot work on a default Battery is a trap. The debit itself waits for `ENGINE-SUPPLY-DEMAND` |
| EL-236 | The dial is declared against what `CommittedTimerRemaining` is, a fraction: one sweep per countdown, no numerals | A seconds scale needs a presentation mapping scaled by a parameter; not planned |
| EL-217, EL-241 | Pocket 0.8 × 0.8 m with walls and shutter 0.8 m high; supported balls 0.62–0.78 m | Confirmed by owner decision 8 |
| EL-240, EL-241 | Sequence-task-only acceptance anchor | Proposal for the ledger, section 6 |
| All | Palette order allocated by the cross-batch pass (only 68, 69, 70 are fixed, by the ball family contract) | Avoids cross-batch collisions |
| Earlier repair decisions | EL-217 level outlet pad; EL-224, EL-225, EL-228 a load lets go by a cut or paid-out rope; EL-225 launch axis, cup and stiffness; EL-227 one-sided region, occupancy capacity one, reverse-blow rearming, own seat pad; EL-228 cable as a 0.02 m solid bar bound to `span`; EL-229 to EL-231 flush port collars; EL-235 box 1.3 m and no self-release; EL-238 counterweight under the sill, D1/D2 from a 5° Ramp; EL-241 `stroke_seconds` 1.4–4; EL-218 ranges; EL-219 no prescribed-speed mode; EL-220 self-lock brake; EL-221 form closure; EL-222 level command; EL-231 rod-side area; EL-237 dwell | Unchanged |

### Decisions made in the propagation repair (23 November 2026)

| Scope | Decision | Reason |
| --- | --- | --- |
| Re-owning `ENGINE-ASSEMBLY-JOINTS` capabilities | Attachment and mounting sockets (EL-219, EL-221) stay with it (player-made connections); a hold on a revolute coordinate or on an unwound prismatic coordinate (EL-224, EL-225, EL-226, EL-228) is `ENGINE-JOINT-KINDS` (its holds on a coordinate, `readiness/batch-i.md` §5 G1) with its pulse release `ENGINE-ASSEMBLY-JOINTS`' commanded release (`readiness/batch-j.md` §4 item 5); crank coupling (EL-218) and the gear mesh with its loop rule (EL-232) are `ENGINE-MECHANICAL-LINKS` (engine design audit §5); strike-and-hold, hinge-spring zero and spring rest position (EL-222, EL-226, EL-241) are `ENGINE-JOINT-KINDS`; parameter-bound rim, collider, mass, art repeat count and field magnitude (EL-232, EL-227) are `ENGINE-PARAMETER-BOUND-FRAMES` | 3a: ASSEMBLY-JOINTS keeps only player-made connections and commanded release; coordinator decision 16 places fixed and dynamic-support joints in JOINT-KINDS and parameter-bound values in PARAMETER-BOUND-FRAMES |
| EL-218 fallback | If `ENGINE-MECHANICAL-LINKS` does not take the position-dependent coupling, the rod becomes a body pinned at both ends by `ENGINE-JOINT-KINDS`' dynamic-support hinge, with its own collider | Coordinator decision 16 |
| EL-221 `HeldOut` | A supply contact closed while a drive is Blocked is `ENGINE-JOINT-KINDS`, beside the coordinate-window contact Batch I places there | A contact controlled by a coordinate's committed state; one owner |
| EL-233 | A supply source paid by shaft work is a clause of `ENGINE-SUPPLY-DEMAND` (the inverse of its work-target clause) | The audit calls it a supply-graph capability; no other 3a name fits and none may be coined |
| EL-234 | The stamina debit is **missing**, owned by `ENGINE-SUPPLY-DEMAND` | A supplied drive does not debit its source today (`deferred-work.md:211`) |
| EL-236 | The rearming timer is a row of `ENGINE-SIGNAL-NODES`; the proposed slice key is withdrawn | Engine design audit §6 item 11 names EL-236 |
| EL-229 | Mouths are liquid snap sockets `Start` (Bidirectional) and `End` (Discharge), pipe bore 0.10 m (EL-001 L5, L6); the chamber's capacity is bound to the rod coordinate (LG4) | The liquid conventions; no link-joined liquid ports |
| EL-230, EL-231 | Ports `GasIn` (EL-230) and `ExtendIn` / `RetractIn` (EL-231), Fluid Inputs; test classes and rows aligned with EL-039 A9 and EL-041 D-A5, D-A13 and D-A15 | Batch H and N hand-back; EL-041 already uses these names |

## 4. Needs the owner

Nothing. Both earlier items are decided: ball sizes by owner decision 8, and the timer's home by 3a (`ENGINE-SIGNAL-NODES`). The elastic bands of EL-225 and EL-226 (presentation spans with no collider) were read under owner decision 1's existing split: they are drawn as 0.02 m cords in the thin-wire class, routed clear of the ball's path, with the routing a stated check for each slice; no gate is waived.

For information: EL-236 refuses zero delay (0.1 s floor) and prints no seconds; EL-238's crossing rule is by game intent; EL-240 and EL-241 take a sequence-task-only anchor (section 6); slice-owned binding is confirmed by the EL-223 slice or S7, not here.

## 5. Hand-backs

### Applied from other batches

| From | Asked | Applied |
| --- | --- | --- |
| Batch H §6 and Batch N item 8 | EL-230 and EL-231 port names to the pneumatic family's Fluid Input / Output convention (EL-039 F3); "the valve's exhaust" is EL-041's `Exhaust` socket | EL-230: `GasIn`; whistle on EL-041's `Exhaust`; an unconnected port is Sealed (F4); U3 and U5 are EL-041 D-A15 and EL-039 A9 with their test names. EL-231: `ExtendIn` and `RetractIn` (the names EL-041 already uses, `EL-041:78`, `:150`); V7 and new V9 are EL-041 D-A13 and D-A5 |
| Batch I §5 (`readiness/batch-i.md:138`) | EL-219: "Its delta over EL-053 is the attachment carriage; if the probe finds no other delta it is a variant of the EL-053 package or a Duplicate closure candidate" | Added to EL-219's disposition |
| Batch I §6 (`:149`) | EL-227's responding property is declared by one rule in `ENGINE-FIELD-FORCE`, not per consumer spec | EL-227 item 8, capability row and open question 2 |
| Batch F §7 (`readiness/batch-f.md:143`) | EL-229 and EL-240: paid-work capture is in the `ENGINE-LIQUID` text (E-7); loop rows named in EL-022; with the pump open when unpowered a stopped loop drains back unless a check valve is placed | EL-240 cites E-7 directly; Y7 and X6 carry the drain-back note. EL-229 cites E-7 with the wording below |

### Returned to other batches

- **Batch H (EL-039 F3).** F3 says "every inlet is `GasIn`". A two-chamber part needs two inlet ids, as the directional valve has `PortA` and `PortB` for two outlets. Proposed: "a part with more than one inlet names each (`ExtendIn`, `RetractIn` on EL-231); `GasIn` stays the id of every single-inlet part." Until then EL-231's names are pending that one edit.
- **Batch F (ENGINE-LIQUID E-7, LG4).** E-7 is worded as a capture region that releases its volume from an outlet, and LG4 says "the liquid exerts no force on the coordinate". The hydraulic piston needs: "or a store whose capacity is bound to a prismatic coordinate receives force ρ·g·h·A on that coordinate from the head at its intake, paid by the volume that enters at that head; the volume stays in the store until expelled through a declared mouth", and LG4 then reads "no force on the coordinate except under E-7".
- **Batch J (ENGINE-MECHANICAL-LINKS).** Its Unblocks should name EL-232 (the mesh as a declared connection material: reversed tooth ratio, toothed engagement class, loop consistency) and EL-218 (a position-dependent coupling). The story is framed as force-bounded with a slip limit; EL-218 needs a rigid coupling with no slip so the crank keeps its phase. Either the story admits a rigid coupling law kind, or EL-218's fallback applies (a rod pinned at both ends by `ENGINE-JOINT-KINDS`' dynamic-support hinge). Batch J's commanded-release text for `ENGINE-ASSEMBLY-JOINTS` ("a declared signal input of the owning part") should also admit a release from an internal signal node: EL-228's pad contact node commands its release through an internal edge, with no input port.
- **Batch I (ENGINE-JOINT-KINDS).** Its holds should state that a hold on a revolute coordinate, or on a prismatic coordinate no transmission winds, is released by a physical request (EL-224, EL-225, EL-226, EL-228), and admit a supply contact closed while a drive is Blocked (EL-221) beside the coordinate-window contact. Its spring rest position (owner decision 6) names EL-241.
- **ENGINE-SUPPLY-DEMAND (whoever writes it).** Add: "a supply source whose available power is at most a declared efficiency times the work absorbed on a declared coordinate, delivering no more than that work" (EL-233).

### For other sessions (kept)

- **EL-223 delivering session.** This batch's draft is marked superseded and is dropped at merge. Audit finding F8 on it: on the draft's own numbers a 3 m/s arrival keeps 0.83 of its speed (2.5 m/s) and rises 2.5² ÷ (2 × 9.81) = 0.32 m, which on a 1.0 m arm is acos(1 − 0.32) = 47°, so row P2's "stalls below 45°" fails; assert "below 60°". Also: 4.9 m/s needs a 1.7 m drop for a rolling ball (1.2 m is the sliding figure); state the cup floor height above the base (0.4 m) and that the lesson supplies an approach deck at that height; in P6 add "+ 0.05 m" and define the kinetic energy as translational plus rotational.
- **Baseball repair (EL-239).** BB4, Receiver capture (an aligned Baseball is captured, a shifted one is not), is in neither main's `BaseballTests` nor the repair contract's R8. Add it to the repair, or cite the generic test that covers a `baseball` capture.
- **Batch I (EL-060, EL-091).** The Weight responds by EL-060; the Cannonball's declaration gains the property in the first field consumer's slice. EL-077 and EL-091 specs are behind the family contract.
- **Cross-batch pass.** Palette orders for this batch are unallocated. Batches I (153–174, 272) and J (170–181) overlap each other at 170–174.

## 6. Gaps: proposed text, re-owned under owner decision 3a

`epics.md` is not edited here. The earlier gap numbers are kept so references stay stable.

- **G1 (was ENGINE-ASSEMBLY-JOINTS) → `ENGINE-MECHANICAL-LINKS` and `ENGINE-PARAMETER-BOUND-FRAMES`.** MECHANICAL-LINKS: "a mesh between two toothed rims of separately placed parts, as a declared connection material: reversed sign, ratio from declared tooth counts, adjacency checked at connection; and a position-dependent coupling between a revolute and a prismatic coordinate" (EL-232, EL-218). **G-B:** "a closed ring of meshes turns when the product of its ratios is +1 and stays at rest otherwise." PARAMETER-BOUND-FRAMES: "a port rim, a hinge body's collider radius, its mass and a drawn repeat count bound to a whole-number parameter" (EL-232).
- **G2 (was ENGINE-ASSEMBLY-JOINTS) → `ENGINE-JOINT-KINDS` with `ENGINE-ASSEMBLY-JOINTS`' commanded release.** "Retention of a prismatic coordinate that no transmission winds (drawn by a rope, or held against gravity), and of a revolute coordinate, holding one way until a physical release request" (EL-224, EL-225, EL-226, EL-228).
- **G3 → `ENGINE-JOINT-KINDS` and `ENGINE-SUPPLY-DEMAND`.** "A supply contact controlled by a drive's committed Blocked phase" (EL-221) goes beside the coordinate-window contact; shaft-to-electrical conversion (EL-233) is the SUPPLY-DEMAND clause in section 5.
- **G-A, qualification fixtures.** Each owning story above takes one declared fixture for its capability: a position-dependent coupling and a mesh between two placed parts (MECHANICAL-LINKS); a retained and released revolute coordinate and a retained coordinate with no winding transmission (JOINT-KINDS); a supply paid by shaft work that delivers no more than the work absorbed (SUPPLY-DEMAND).
- **G-C, `ENGINE-FIELD-FORCE`.** Against the one field text (`readiness/batch-l.md` §4 item 6): "**And** a source may declare an occupancy capacity, acting on no more responding bodies than that until one leaves the region; occupancy is a committed read." The work read is already in that text; a magnitude bound to a parameter is PARAMETER-BOUND-FRAMES (EL-227).
- **G4 → withdrawn.** Replaced by `ENGINE-LIQUID` clause E-7 in Batch F's proposed text, with the EL-229 wording in section 5.
- **G5, `ENGINE-GAS-NETWORK`.** One of its two qualification fixtures is a coordinate with two opposed chambers (EL-231); Batch N's text already adds "two chambers on one coordinate".
- **G6 → `ENGINE-JOINT-KINDS`.** A constant axial spring on a limited hinge exists in the solver and validator (`WorkshopPhysicsEngine.cs:681`, `BundleValidator.cs:799`); what is missing is a package-level test and the declared spring rest position (a parameter-bound hinge spring has no consumer in this batch; EL-226's `winding_ratio` binds like the delivered Wound spring's `winding_lead`) (owner decision 6, coordinator decisions 21 and 25; EL-241).
- **G10 → withdrawn.** The rearming timer is a row of `ENGINE-SIGNAL-NODES`: "**Given** a declared timer, **when** its completion policy is Rearm and its elapsed signal is Activation, **then** it is admitted at either boundary, emits one activation on elapse and is ready again; busy inputs neither restart nor re-emit."
- **`ENGINE-RUN-LENGTH`.** EL-236 (E3, 70 s), EL-229 and EL-240 (60 s guards).

Ledger, requirements and batches:

- **Sequence-task-only anchor (proposal for the ledger).** EL-240 (`sequence-task-386`) and EL-241 (`sequence-task-351`) are bare list items with no `todo` anchor. Proposed: their ledger entries cite the sequence task as the sole acceptance anchor, and the binding reader admits an Acceptance line naming a sequence task alone when the ledger entry carries no `todo` link.
- **G7, `delivery-batches.md`.** Move EL-221, EL-224, EL-225, EL-228, EL-232, EL-233 and EL-236 from 1b to 1c, and EL-222 from 1c to 1b. Line 91's statement that the 24 have no spec is out of date.
- **G8, ledger.** EL-220's "Needs" can read "present"; EL-222's should say strike-and-hold is a velocity drive against a stop, to be checked; EL-236 should record the 0.1 s floor and its `ENGINE-SIGNAL-NODES` row.

## 7. Where the 22 November repair departs from the audit

Each audit finding was checked against the code it cites; all citations hold. Departures:

1. **F1 (EL-217).** The level pad is at the outlet, after the chute, not under the hole.
2. **F6 (EL-217, EL-241).** The plan-view rule is not sufficient: a second ball nestles beside the first. Walls are 0.8 m and the supported range is 0.62–0.78 m (owner decision 8).
3. **F19 (EL-217).** A blocked outlet does not give phase Blocked; A5 asserts the load phase and the held phase at home.
4. **F3 (EL-225).** Cutting the rope is necessary and not enough: stiffness, cup and axis changed.
5. **F4 (EL-227).** The relay reads its own seat; the region is a bounded box; rearming is a reverse blow.
6. **F7 (EL-234).** M4 uses a Bowling ball; the Cannonball is not delivered.
7. **F14 (EL-241).** Range 1.4–4.
8. **F22.** The section title is kept; "Files harvested: none" is added to all 23.
9. **Not in the audit.** EL-236's rearming timer with an elapsed activation is refused by the validator today. EL-226's tip speed is 7.5 m/s, not 9; its range is 5.5–9.5 m/s.

## 8. Contradictions still open

1. Delivery batches and the epic say a revolute strike-and-hold drive does not exist; a supplied velocity drive on a limited hinge is admitted by the validator and untested (EL-222; fallback owner `ENGINE-JOINT-KINDS`).
2. Several ledger "Needs" lines assume sensing on carried parts; contact, passage and load nodes must sit on a static body, and a load region must be an unrotated box (`SignalBundleValidation.cs:172`, `:180`, `:189`–`:190`), until `ENGINE-SENSOR-VOLUMES` and `ENGINE-CONTACT-OBSERVATIONS`.
3. The brief and the S7 draft describe a ledger `**Package:**` line; main uses a spec section.
4. EL-039 F3 ("every inlet is `GasIn`") against a two-inlet part (section 5).
5. The liquid conventions' LG4 ("no force on the coordinate") against EL-229's head on its piston (section 5).

## 9. What was not verified

- No build or test was run. Launch, return and stall figures are hand calculations with stated assumptions; restitution-dependent speeds are given as ranges.
- What a drive does when granted less power than it asks for (EL-222, EL-237 and the named Battery limits).
- Whether two sources may feed one input (EL-233 Q4); the Counter's and Clock's port kinds (EL-217 A6, EL-241 H6).
- Whether two moving bodies of one package collide (EL-235); whether a timer or logic contact can command a drive inside one package (EL-217, EL-234, EL-237, EL-241); whether a hinge spring's zero is its rest angle (EL-222, EL-226); whether collider bindings admit a whole-number parameter or a hinge's moving body (EL-232).
- Whether the hopper may be placed rotated in Build (EL-241 H9 covers the vertical case either way).
- Collider counts of the delivered parts used as comparisons in the conformance sections were not counted from their scenes.
- The EL-223 session's contract is not in this checkout.

## 10. Propagation checklist (23 November 2026)

Greps over the 23 specs of this batch (EL-217 to EL-241 except EL-223 and EL-239), before → after:

| Pattern | Before | After |
| --- | --- | --- |
| "Blocked on the engine programme (owner decision 3a)" (files with it; status and prerequisites) | 0 | 23 (46 lines) |
| "can sleep" | 22 | 0 |
| the literal heading of main's binding section | 23 | 0 |
| "declarable today", "binds today", "Ready for binding", "EL-223 publish", "binding form" | 33 | 0 |
| `engine-signal-rearming-elapsed-timer` | 1 | 0 |
| `kPa` | 2 | 0 |
| `ChamberPort`, `ExtendPort`, `RetractPort` | 3 | 2 (each in an open-question note recording its withdrawal) |
| retired names (`powered-part-demand-drain`, `ENGINE-SPRING-REST-COORDINATE`, `engine-scene-environment`, `ENGINE-CHARACTERS`), "non-solid", "tether", "293", "50 W" | 0 | 2 ("non-solid" for EL-225's and EL-226's thin-wire bands, owner decision 1; none for a rope, cable, hose or chain) |
| `ENGINE-ASSEMBLY-JOINTS` (files) | 10 | 6, each only for an attachment or mounting socket or a commanded release |
| "Coverage binding (planned, not yet live)" (files) | 0 | 23 |
| part-body cream `#e8dccb` (owner decision 4 sets `#fff8e9`) | 20 | 0 |
