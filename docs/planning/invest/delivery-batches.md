# Delivery batches — family slices, closures, decisions and structural work

Planning document, written 21 November 2026 against published main `4f193bb2` and brought on 23 November 2026 to the [element-spec decisions of 22 November 2026](../elements/readiness/owner-decisions-2026-11-22.md) (owner decisions 0–24a, coordinator decisions 16 and 21 and 25–31) and the [readiness summaries](../elements/readiness/) of batches A to O and the new identities, against published main `25df3cd5` (45 element packages, the Baseball included). It records work the next coordinator would otherwise rediscover: which identities can share a slice, which are closed by another part, which family-level questions are already decided, which planned levels can be proven, and which repair and structural stories make an element slice cheaper.

It adds no delivery rule. The rules it relies on are **Family slice**, **Duplicate closure** and **Early level proof**, stated in [AGENTS.md, Delivery slices](../../../AGENTS.md#delivery-slices-and-independent-review) and defined in the [loop protocol, Slice shapes](../../../_bmad/custom/bmad-loop-protocol.md#slice-shapes) (owner, 21 November 2026), together with that protocol's [capability probe](../../../_bmad/custom/bmad-loop-protocol.md#capability-probe) and the [named-identity ledger](named-elements.md). On any conflict those documents win, and the [roadmap's delivery order](vertical-delivery.md#capability-cluster-order) governs order. Section 3 records decisions made under the owner's standing delegation; they are decisions, not rules. Thermal and radiation identities stay in the first release (owner, 21 November 2026).

**Classification source.** Every grouping below is taken from the element specs as repaired on 22–23 November 2026 and their batch summaries, which read each capability against the code. Each slice's capability probe is still the authority: a grouping whose probe fails is re-planned at planning, and a missing capability becomes its own engine-first slice.

**Family or run.** A family is variants of one part: presets, materials, colours or sizes. Only a family may share a slice, and each member keeps its own acceptance rows and tests. A *run* is a list of unrelated parts that reuse the same capability; each is its own slice. A run is planning reference only: under owner decision 24a each member takes its own place in the campaign teaching order, and listing it in a run never moves it earlier.

## 0. Sequence (owner decisions 3a, 18, 22, 24a and 24m, 22–24 November 2026)

1. **Before the engine programme, only repairs and tooling run** (owner decision 22): the visible-solid repair parts A, B, B2 and C, the Baseball repair, and structural stories S3, S5 and S6 ([section 5](#5-structural-stories)); owner decision 24h (23 November 2026) adds S5b, straight after S5, and S6's follow-on slices S6b to S6g, with S6 (coordinator decision 42, 24 November 2026, split S6f into S6f and S6g). Owner decision 24p (25 November 2026) adds S5c, one schema file for the FlatBuffers ABI, after S5b and before S6. Order: S3 and S5 one at a time, S5b, S5c, the first S6 slice, then S6b, S6c, S6d, S6e, S6f, S6g; S6b to S6g are replanned at dispatch from the main that contains the slice before them (coordinator decision 43, 24 November 2026). Under [owner decision 24m](../elements/readiness/owner-decisions-2026-11-22.md) (24 November 2026) the lane 3 slices (lettered level ids, then the content slice once its contract passes planning) and the lane 4 policy-tool fixes also run before the programme, in up to four parallel lanes: lane 1 the collider repairs (parts A, B, B2 and C and the Baseball repair), lane 2 the structural stories (S3, S5, S5b, S5c, S6 and S6b–S6g), lane 3 and lane 4. Each lane runs one slice at a time in its own worktree, and a lane is admitted only when its code is disjoint from every other running lane's. Merge rule (24m): the coordinator holds one publish token; the holder merges `origin/main` once into its slice branch (a merge, never a rebase or force push), keeping both sides' entries in the shared documents, repeats the full local round on the merged commit, gets SnapshotApproval on that merged commit, pushes with a plain fast-forward and releases the token. Repair part D is not a running lane: it waits for the programme (step 4). DOC-ENGINE-STATUS, programme order 1, documents only, runs in lane 4 from 25 November 2026 in parallel with the remaining pre-programme work (owner decision 24q); order 2 onwards waits.
2. **The engine programme** (owner decision 3a), in its order: `DOC-ENGINE-STATUS`, `ENGINE-SPATIAL-QUERIES`, `ENGINE-CONTACT-OBSERVATIONS`, `ENGINE-SENSOR-VOLUMES`, `ENGINE-ISLANDS-SLEEP`, `ENGINE-ONE-WAY-INSTALL`, `ENGINE-SUPPLY-DEMAND`, `ENGINE-CAPABILITY-INSTANCES`, `ENGINE-WIRE-ABI` (order 8a), `ENGINE-WASM-THREADS` (order 8b), `ENGINE-INSTANCED-RENDERING` (order 8c), `ENGINE-TICK-ALLOCATION`, `ENGINE-SOA-SIMD`. Owner decision 24k (23 November 2026, after the [ABI audit](../elements/readiness/audits/abi-interfaces.md)) added 8a and 8b: every boundary record generated from the pinned `flatc` on both sides, the pose ring made the renderer's real feed with a multi-buffered depth derived from tick rate and render delay, batched animation output, and WebAssembly threads so workers share memory and islands can run in parallel; the first `ENGINE-WASM-THREADS` slice proves threaded WebAssembly with the ahead-of-time build and the isolation headers. Coordinator decision 40 (23 November 2026) added 8c: instanced drawing is its own story, `ENGINE-INSTANCED-RENDERING`, after `ENGINE-WIRE-ABI` (which makes the pose ring the renderer's feed), not part of the wire story; it draws every body in GPU instanced batches on WebGL 2 and WebGPU, keyed from compiled declarations, and its first slice establishes whether WebGPU is reachable with the pinned Godot export. The stories and their *Depends on* lines are in the [engine stories index](../elements/readiness/engine-stories-index.md).
3. **Gas, liquid and thermal engines** (owner decision 18): `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID`, `ENGINE-THERMAL`, with the land-first prerequisites the roadmap names.
4. **Every remaining element, in the order the campaign levels first teach it** (owner decision 24a), each after its own *Depends on* engine stories. Every Family slice, run member, closure review, refinement and Early level proof below takes its place in this order; the structural stories S4, S7 and S8, the goal slices, tests-only patches, repair part D and the other repairs listed after the programme also wait for step 2.

Every row of sections 1 and 2 is therefore "after the engine programme (owner decision 3a)"; the tables name only the further stories each waits for.

## 1. Family slices and runs

### 1a. Families (one slice each, if the probe passes)

| Family | Identities | Waits for, beyond the programme | Source |
|---|---|---|---|
| Ball presets | [EL-077 Soccer ball](../elements/EL-077-soccer-ball.md), [EL-091 Cannonball](../elements/EL-091-cannonball.md) | S7 (the coverage reader admits a package that is not a catalogue part) | Batch L; the delivered Tennis ball declaration shape (`elements/TennisBall/TennisBallElementDeclaration.cs:9`–`:57`). [EL-239 Baseball](../elements/EL-239-baseball.md) is delivered (`fa86969f`); its repair is in section 5. Disqualified if the Cannon's chamber does not admit the cannonball's radius |
| Barrier materials | EL-073 Brick barrier, EL-074 Wood barrier | S7; EL-073's strength row: `ENGINE-FRACTURE`; EL-074's combustion row: the thermal engines and `ENGINE-IGNITION-BLAST` | Static resizable box (`elements/Wall/WallElementDeclaration.cs:8`, `:26`–`:30`) |
| Rope materials | EL-205 Rope (the declared rope material, not a closure), EL-067 Steel cable | `ENGINE-CONNECTION-MATERIALS` (solid strand), which converts the delivered rope in the same change | Batches I and J; owner decision 1 |
| Connection materials | EL-200 Drive belt, EL-201 Drive chain (new declared materials the player chooses; the delivered rigid connection stays the default) | `ENGINE-CONNECTION-MATERIALS`, then `ENGINE-MECHANICAL-LINKS` | Batch J; owner decision 2 |
| Edge detectors | EL-181 Rising-edge detector, EL-182 Falling-edge detector | the edge row of `ENGINE-SIGNAL-NODES` | Batch H |
| Cam profiles | EL-156, EL-157, EL-158 | none (hinge-carried cylinder colliders, a spring slider, `DriveIn`); `ENGINE-PROFILE-COLLIDER` only if EL-158's fourteen-cylinder dwell arc fails | Batch J item 13 |
| Metal bends | EL-072 Curved metal ball pipe (90°), [EL-242](named-elements.md#element-242) 45° metal ball pipe bend | S7; `ENGINE-OPTICAL` for the light-occlusion row | Batch I; coordinator decision 27; D12 |
| Pin-ended members | EL-113, EL-115 | `ENGINE-JOINT-KINDS` | Batch M item 17 |
| Limited connectors | EL-159, EL-160, EL-161 | `ENGINE-JOINT-KINDS`, `ENGINE-FRACTURE` | Batch M item 17 |
| Treatment stations | EL-118, EL-119 | `ENGINE-SURFACE-STATE` | Batch M item 17 |
| Sighted walkers | EL-082, EL-083, EL-087 (dynamic chassis on hinge-carried rollers, internal source, receiver-controlled contact) | package joints on a dynamic base (`engine-characters`), `ENGINE-OPTICAL` | Batch L item 18 |
| Colour filters | CAT-055 red, CAT-031 green, CAT-011 blue | `ENGINE-OPTICAL` | Batch E |
| Colour receivers, Story 13.2 | CAT-056 red, CAT-032 green, CAT-012 blue | `ENGINE-OPTICAL` | Batch E item 8. CAT-038, the Laser receiver, is not a member: it is taught at 51 with the Laser, before the other members (54a), whose rows need the colour lasers taught at 54, so it is its own single-element slice at level 51 (coordinator decision 37) |
| Colour receivers, Story 13.6 | CAT-072 yellow, CAT-021 cyan, CAT-040 magenta, CAT-068 white | `ENGINE-OPTICAL` | Batch E item 8 |
| Optical gates | CAT-043 AND, CAT-046 OR, CAT-047 XOR, CAT-045 NOR, CAT-044 NAND (they differ only in the declared operation, truth print and catalogue text) | `ENGINE-OPTICAL` | Batches E and K |
| Laser colours | EL-176, EL-177, EL-178 (after CAT-036) | `ENGINE-OPTICAL` | — |
| Water elbows | EL-009 (45°), EL-010 (90°) | `ENGINE-LIQUID` | — |
| Actuated taps | EL-166 Mechanically actuated tap, EL-167 Solenoid tap (after EL-003; EL-165 closes inside EL-003, section 2b) | `ENGINE-LIQUID` | Batches F and G |
| Meters | EL-030, EL-031, EL-032 | `ENGINE-GAS-NETWORK` (units), `ENGINE-LIQUID` | Batch G |
| Floating bodies | EL-028, EL-169 (after EL-016) | `ENGINE-LIQUID` | Batch G |
| Sensible stores | TH-26, TH-29 | `ENGINE-THERMAL` | Batch N item 5 |
| Steam vessels | TH-17, TH-35 | `ENGINE-THERMAL` | Batch N item 5 |
| Shield materials | EL-126, EL-127, EL-128 | `ENGINE-RADIATION` | Batch O G10. RAD-18, the Neutron absorber panel, is not a member: its scene rows need RAD-16, RAD-17 and EL-132, taught at 125 and 126, long after the family's 102 to 104, so it is its own single-element slice at its teaching level, 127 (coordinator decision 37) |
| Transmission gauges | EL-129, EL-130 (EL-130 only once a vessel is delivered) | `ENGINE-RADIATION` | Batch O G10 |
| Charged cartridges | RAD-03, RAD-04 | `ENGINE-RADIATION` | Batch O G10 |
| Neutron detectors | EL-131, EL-132 | `ENGINE-RADIATION` | — |
| Goal kinds (level data, not parts) | EL-120 and EL-122 inside `engine-goal-ordered-events`; EL-121 and EL-123 after `engine-goal-count-window-hold` | the goal slices (owner decision 12) | Batch M item 17 |

### 1b. Runs (each its own slice; planning reference only)

| Run | Identities, in the order their batch proposes | Waits for, beyond the programme |
|---|---|---|
| Static geometry | EL-058 Size grate (its edge-carried sphere row is untested); EL-162 Fixed ball diverter, then EL-163, EL-164; EL-216 Capture cradle | none; EL-162's typed lane choice is the Choice clause of `ENGINE-PARAMETER-BOUND-FRAMES`, not a prerequisite |
| Ball handling | EL-107 Spiral gravity-delay tube, EL-108 Powered airlift, EL-109 Speed-sensitive trapdoor, EL-184 Mechanical ball gate | EL-109: the minimum-speed clause of `ENGINE-SENSOR-VOLUMES`; EL-184: `ENGINE-CONNECTION-MATERIALS` |
| Rotary | EL-110 Flywheel, EL-053, EL-054, EL-055 | EL-110: `ENGINE-RUN-LENGTH`; EL-054: two transmissions in series (`ENGINE-MECHANICAL-LINKS`) |
| Supplied sliders | EL-217 Reload shuttle, EL-220 Docking lift, EL-237 Boxing glove, EL-097 Pool cue, EL-062 | none named |
| Stored drives | EL-234 Mouse motor, EL-235 Jack-in-the-box, EL-222 Supplied pinball flipper | none named (`ENGINE-JOINT-KINDS` for EL-222 only if an untested row fails) |
| Selective passage | EL-238 Selective trap door, EL-241 Ball hopper (after EL-237) | EL-241: the spring rest position of `ENGINE-JOINT-KINDS` |
| Containers and supports | EL-079 Electric mixer, EL-096 Tipsy platform, EL-099 Pool pocket, EL-116 Passive wheel, EL-095 Toy revolver (the Cannon's shape), EL-102 Large-bore ball pipe (a variant of the Clear pipe with its own entry; the bore differs) | none named |
| Compliant surface | EL-215 Damped cushion | `ENGINE-COMPLIANT-FRICTION` for admission by the stability bound (the probe is settled) |
| Tensile | EL-065 Load hook; EL-207 Moving pulley; EL-069 Moving bucket; EL-070 Moving cage | EL-207 (and EL-069, EL-070 if their probe shows the same need): the dynamic-guide clause of `ENGINE-TENSILE-EXTENSIONS`; a dynamic pulley is not available today |
| Joints | EL-056, EL-057 (one-way and stepped shafts); EL-061 (indexing); EL-112, EL-114, EL-117, GAP-04 | `ENGINE-JOINT-KINDS`; EL-061's contact: `ENGINE-SIGNAL-NODES` |
| Holds and releases | EL-221 Parallel gripper, EL-224 Counterweighted scoop catapult, EL-225 Latched slingshot cradle, EL-226 Hinged elastic catapult, EL-228 Zipline signal carriage | `ENGINE-JOINT-KINDS` (every hold and its release, index conflict C2); EL-221 also `ENGINE-ASSEMBLY-JOINTS` (its mounting socket only) and `ENGINE-SIGNAL-NODES` (its `HeldOut` contact) |
| Couplings | EL-218 Crank-slider (a rod pinned by dynamic-support hinges, coordinator decision 29), EL-219 Rack-and-pinion slide (Duplicate-closure check against EL-053), EL-232 Gears | EL-218: `ENGINE-JOINT-KINDS`; EL-219: `ENGINE-ASSEMBLY-JOINTS`; EL-232: `ENGINE-MECHANICAL-LINKS`, `ENGINE-PARAMETER-BOUND-FRAMES` |
| Supply and signal | EL-233 Generator; EL-236 Egg timer; EL-183 Resettable counter; EL-094 | EL-233: the shaft-paid source of `ENGINE-SUPPLY-DEMAND`; EL-236, EL-183, EL-094: `ENGINE-SIGNAL-NODES` (rearming timer, count reset, threshold node); EL-236: `ENGINE-RUN-LENGTH` |
| Cutters and winch | EL-066, EL-068 (cutters); EL-063 (winch); EL-085; EL-078 Can opener | the cutting clause and winding of `ENGINE-TENSILE-EXTENSIONS`; EL-085: the winch and the solid strand; EL-078: the cutting clause and the fixed joint |
| Field force | EL-060, EL-227, EL-089, EL-103; EL-124 (scene-region slice); EL-125's residual | `ENGINE-FIELD-FORCE` (EL-227 also `ENGINE-PARAMETER-BOUND-FRAMES`) |
| Reaction and blast | EL-092, EL-104, EL-090, EL-100, EL-105; EL-093, EL-106 | `engine-reaction-thrust`; the blast half of `ENGINE-IGNITION-BLAST` |
| Fracture and characters | EL-084, EL-088 (after EL-084 and the Sighted walkers); EL-086; EL-101 | `ENGINE-FRACTURE`; EL-086: package joints on a dynamic base and the edge node; EL-101: the integrity observer's stress concentration (`ENGINE-CONTACT-OBSERVATIONS`) |
| Optical sources | EL-075 | `ENGINE-OPTICAL` and the oscillator row of `ENGINE-SIGNAL-NODES` |
| Programmable parts | EL-076 Programmable ball, then EL-080 Programmable box; EL-098 Pool ball (zero-parameter ball shape, table contact, flat printed stripe by owner decision 11) | EL-076, EL-080: the Choice clause of `ENGINE-PARAMETER-BOUND-FRAMES` and `ENGINE-PARAMETER-COMBINATIONS` |
| Liquid core | EL-001, EL-003, EL-004, EL-002, EL-006, EL-005, EL-007 | `ENGINE-LIQUID` |
| Liquid bodies and controls | EL-016, EL-014, EL-015, EL-019, EL-020, EL-017, EL-021, EL-018, EL-022 | `ENGINE-LIQUID` |
| Water pipe kit | EL-008, EL-012 (directly after EL-008), EL-011, EL-013 | `ENGINE-LIQUID` |
| Over the rim | EL-024, EL-168, EL-035 | `ENGINE-LIQUID` |
| Porous | EL-034, EL-171 | `ENGINE-LIQUID` |
| Liquid machines | EL-229 Hydraulic piston, EL-240 Waterwheel | `ENGINE-GAS-NETWORK` (fluid port record), `ENGINE-LIQUID`, `ENGINE-RUN-LENGTH` |
| Pneumatic kit | EL-038 (after `ENGINE-CONNECTION-MATERIALS`), EL-039 (level 71 teaches the hose before the reservoir, coordinator decision 37), EL-040, EL-043, EL-042, EL-037, EL-041, then EL-230, EL-231 | `ENGINE-GAS-NETWORK` |
| Acoustic | CAT-009 Bell, CAT-060 Sound meter (with EL-044), CAT-061 Speaker (EL-180 closes inside it), CAT-069 Wind chimes; then EL-179, EL-050, EL-051, EL-049, EL-046, EL-048, EL-047, EL-045, EL-052 (EL-052 after `ENGINE-LIQUID`) | Story 14.0; CAT-060: `ENGINE-SIGNAL-NODES`; CAT-069: the spherical joint of `ENGINE-JOINT-KINDS`, `ENGINE-RUN-LENGTH` |
| First heat | TH-04, TH-37, TH-21, TH-08, TH-09, TH-10 | `ENGINE-THERMAL` |
| Combustion | TH-32, TH-01, TH-02, TH-31, TH-33, TH-03 | `ENGINE-THERMAL`, `ENGINE-IGNITION-BLAST` |
| Thermal expansion | TH-23, TH-22 | `ENGINE-THERMAL` |
| Heated gas | TH-24, TH-25 (with its companion `balloon_cargo`, owner decision 23) | `ENGINE-THERMAL` |
| Converters | TH-12, TH-30 | `ENGINE-THERMAL` |
| Ice | TH-14, TH-15, TH-13 | `ENGINE-THERMAL` |
| Thermal singles | TH-06, TH-07, TH-11, TH-16, TH-18, TH-19, TH-20, TH-27, TH-28, TH-34 (with its companion `toast_slice`), TH-36 | `ENGINE-THERMAL` |
| Granular | GAP-06, then GAP-07 | `ENGINE-GRANULAR` (owner decision 14) |
| Fragmentation | GAP-09 (with its companion `breakable_block`) | `ENGINE-FRACTURE` |

Every thermal row also waits for `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` (owner decision 18), and every identity that gets a package of its own and is not a catalogue part waits for S7. Identities named in no row are single elements: the roadmap's teaching order places them, and each spec's Delivery section names the engine stories it waits for (the optical, radiation and other domain singles wait for their domain's story).

### Identities ledgered after EL-241

[EL-242](named-elements.md#element-242) (ledgered 23 November 2026, coordinator decision 27) is classified above with EL-072. If the ledger gains identities after EL-242, classify each here before planning from this document, and update the counts at the end.

## 2. Duplicate closures

Under the Duplicate closure rule an identity with any behaviour, parameter or mode of its own is a variant or an element, not a duplicate. A closed duplicate gets no package and no catalogue or palette entry. **Closure check:** executed proof of the identity's ledger entry (exact source outcome, negative/control, Reset and save criteria) through the satisfying part, with the ledger row citing the part and the test. A row with no executed test is not closed. The catalogue (CAT) spec's acceptance rows are the single authority for a closure identity (coordinator decision 16), and where a CAT spec and a named spec describe one package the CAT spec is its single declaration (coordinator decision 27).

### 2a. Candidate duplicates of delivered parts — closure-review slices after the engine programme

Each group is one ordinary closure-review slice (one record, one candidate commit, one approval) whose owned diff is tests and ledger rows. Owner decision 22 holds every closure review until the engine programme is done.

| Group | Identity → satisfying part | Rows to add, or the further story it waits for |
|---|---|---|
| Electrical gates | EL-133 → CAT-013 Both gate; EL-134 → CAT-026; EL-135 → CAT-027; EL-136 → CAT-025; EL-137 → CAT-024 | the self-edge row of `ENGINE-SIGNAL-NODES` (Batch B) |
| Drive train | EL-202 → CAT-019 Conveyor; EL-203 → CAT-057 Reverse gear | none; the rigid connection is unchanged (owner decision 2), so no order against `ENGINE-MECHANICAL-LINKS` |
| Rope parts | EL-204 Weight → CAT-067; EL-206 Fixed pulley → CAT-053; EL-064 Metal loop anchor → CAT-058 | `ENGINE-CONNECTION-MATERIALS` re-runs the checks when the strand gains its collider; one EL-064 row uses the player's runtime cut |
| Cargo | EL-189 Tennis ball → CAT-064; EL-208 Balloon → CAT-003 | EL-189: one Tennis row (`bounce` 0.14, `drag` 0). EL-208: two rows, an occluded jet on a Balloon and a heavier Balloon that ruptures on the ceiling that holds the default |
| Launch parts | EL-195 Pinball bumper → CAT-015 | the CAT-015 row and the EL-195 ledger row are ticked only after Story 15.0's isolated-assistance control |
| Fan modes | EL-209 → CAT-028 Fan (its Electrical and Shaft modes) | none |
| Wires | EL-197 Electrical wire → the delivered electrical connection (Battery, Motor) | none |

### 2b. Candidate duplicates of undelivered parts — closed inside that part's slice

The closure is acceptance rows and named tests inside the part's slice; it has no record of its own.

| Identity | Closed inside the slice for |
|---|---|
| EL-138 to EL-142 Optical gates | CAT-043 AND, CAT-046 OR, CAT-047 XOR, CAT-045 NOR, CAT-044 NAND (in that order) |
| EL-143, EL-144, EL-145 Colour filters | CAT-055 red, CAT-031 green, CAT-011 blue |
| EL-146 to EL-152 Selective receivers | CAT-056 red, CAT-032 green, CAT-012 blue, CAT-072 yellow, CAT-021 cyan, CAT-040 magenta, CAT-068 white |
| EL-212 Flat mirror | CAT-041 |
| EL-213 Optical combiner | CAT-006 Beam combiner |
| EL-214 Broadband receiver | CAT-038 Light receiver |
| EL-165 Manual tap | EL-003 Tap; check: `TapTests` rows T2, T4, T5, T7 and T9 (`TapTests.TheOpeningIsFixedDuringRun`) |
| EL-180 Pulse speaker | CAT-061 Speaker (Story 14.3), by CAT-061 rows 1–14 (coordinator decisions 16 and 27) |

### 2c. Refinements and variants with behaviour of their own

Each has rows of its own and stays open on them; where its residual is delivered and tested at planning it becomes a candidate duplicate and joins a 2a group.

| Identity | Refines | Its own rows, and where they are delivered |
|---|---|---|
| EL-187 Basketball | CAT-001 Ball | Two drop heights (tests only); buoyancy is the declared ambient lift, closed by one test (owner decision 24); the R-N3 reference is retired (owner decision 8) |
| EL-188 Bowling ball | CAT-014 | Matched-Basketball lever control (tests only); buoyancy is the declared ambient lift (owner decision 24) |
| EL-190 Ramp | CAT-054 | Uphill, misaligned and minimum-length tests; assistance under Story 15.0 |
| EL-191 Wall | CAT-066 | Gap, resize-in-Run and thin-wall tests; light and sound occlusion by the same box (after `ENGINE-OPTICAL` and Story 14.0) |
| EL-192 Receiving basket | CAT-004 Receiver | Residence and guide in a moving frame (`ENGINE-SENSOR-VOLUMES`) |
| EL-193 Domino | CAT-023 | Offset centre of mass, closed inside story CAT-023c (value (0, −0.05, 0) m, owner decision 8) |
| EL-194 Springboard | CAT-062 | Its own typed signal output that fires on launch, a declared sensor and port on general capabilities (owner decision 7); `spring_signal`; its two open level criteria |
| EL-196 Battery | CAT-005 | Drive work does not debit the store (`ENGINE-SUPPLY-DEMAND`); every other row is delivered |
| EL-198 Switch | CAT-063 Impact switch | CAT-063 is the single declaration and absorbs the residual: the `contact` setting, per-option controls and the Impact-only node and event (coordinator decision 31), after the "contact set by a parameter" row of `ENGINE-SIGNAL-NODES` |
| EL-199 Electric motor | CAT-042 | Closed inside `ENGINE-SUPPLY-DEMAND` by its acceptance rows |
| EL-210 Flashlight | CAT-029 (undelivered) | A `supply` choice (Self-contained or Electrical) with its own `PowerIn`; delivered inside CAT-029's slice, whose rows F7, F10, F12 and F13 are its acceptance |
| EL-211 Solar panel | CAT-059 (undelivered) | Delivered inside CAT-059's slice by its rows P5, P6 and P10 |
| EL-044 Tone-selective sound meter | CAT-060 Sound meter | A variant with its own entry, delivered with CAT-060 in the acoustic run |
| EL-179 Continuous-tone speaker | CAT-061 Speaker | A variant with its own entry (one sustained emission, D17), after Story 14.3 |
| EL-059 Weight tray | CAT-052 Pressure plate | A variant with its own entry |

### 2d. Related but never closures

- **EL-071** Straight metal ball pipe and the **Metal bends** family (EL-072, EL-242, section 1a) are variants with their own catalogue entries (D12). They are mentioned here only because CAT-048 to CAT-050 name them as mapped identities.
- **TH-05 Friction brake** extends [EL-055 Mechanical brake](../elements/EL-055-mechanical-brake.md) ("one brake identity with an added thermal proof"). EL-055's slice delivers the brake; TH-05 is the later thermal criterion on the same package.

## 3. Family-level decisions

The element specs hold 1,141 list items under their "Open questions" headings; 689 mention an owner decision (counted 21 November 2026 by scanning every spec for list items under a heading containing "open questions" or "open decisions"). Most repeat a handful of family questions. Under the owner's standing delegation (decide by game intent, a playable physics puzzle; ask only when cutting scope or waiving a gate) the following are decided. A slice that finds a decision causes a gameplay defect records the defect and decides again at planning. The binding [decisions of 22 November 2026](../elements/readiness/owner-decisions-2026-11-22.md) govern where they differ.

The line counts below were measured by keyword on an earlier index of 1,005 of those items, so they are lower bounds and they overlap; on that index the patterns together match 657 items. The rest are single-element questions, covered by D19.

| # | Decision | Reason | Lines it resolves |
|---|---|---|---|
| D1 | Every value a spec marks **proposed** is the starting value. No owner confirmation per value. | Outcomes, not numbers, are the acceptance target (28 October policy) | 307 items containing "proposed", from `EL-003-tap.md:85` to `GAP-09-fragmentation-station.md:96` |
| D2 | Liquid density is 16 kg/m³ for the whole liquid family. | The Basketball floats and the Bowling ball sinks, which is the lesson the buoyancy row asks for | 46 items, `EL-001-finite-reservoir.md:84` onward |
| D3 | There is no second way to join two mouths: liquid mouths join the way clear ball-pipe mouths join today, and gas and liquid share one fluid port record with two socket kinds (link-joined and mouth-joined), owned by `ENGINE-GAS-NETWORK` (coordinator decision 16). | One current path (forward-only rule) | `EL-001-finite-reservoir.md:86` and the family rows that defer to it |
| D4 | One liquid inventory representation, owned by the liquid engine slice. Meltwater, condensate and moderator water exchange with it. | A second liquid model would be a second rule for the same behaviour | `TH-14-ice-block.md:74`, `EL-001-finite-reservoir.md:87`, `RAD-17-water-moderator-tank.md:81` |
| D5 | The thermal "Batch N game scale" is adopted as written, with its declared saturation curve for boiling: ambient 101 325 Pa; optical watts per the optical conventions; water 16 kg/m³ (D2); one thermal step per tick after the optical solve; boiling with the game-scale latent heat of owner decision 19. | One consistent scale keeps heating times within a puzzle's length | 37 items, one per `TH-` spec |
| D6 | Combustion reads oxygen from a finite gas inventory, not a boolean flag. | The requirement row (TX-14) asks for it and the gas store capability exists | 4 items: `TH-01-candle.md:76`, `TH-03-combustible-block.md:79` and two more |
| D7 | Tick order is the one [combined tick order](../elements/EL-176-red-laser-emitter.md#combined-tick-order) (coordinator decisions 16 and 26): the optical and radiation solve runs once after the last substep, and every receiver reading of a tick commits together and acts from the **next** tick. | See the note under this table | 22 items headed "Tick order", `EL-138-optical-and-gate.md:108` to `CAT-072-yellow_receiver.md:89` |
| D8 | Each identity that is **not** a duplicate (section 2) gets its own catalogue entry. A mode inside one identity is a parameter of that entry (EL-044's band is one entry with a `band` setting). A closed duplicate gets none. A declared companion package (owner decision 15) belongs to its parent identity. | The owner requires every named element; players should not see two entries for one part | 21 items, `EL-003-tap.md:83` to `EL-207-moving-pulley.md:105` |
| D9 | Art, icon and colour follow [DESIGN.md](../../../DESIGN.md) and the spec's proposal. No per-part confirmation. | The approved palette already governs | 20 items |
| D10 | Authored durations are seconds of game time, never ticks. | Cadence is selectable; a tick count would change behaviour with it | `CAT-009-bell.md:116`, `CAT-061-speaker.md:105`, `CAT-069-wind_chimes.md:121` |
| D11 | A declared parameter is player-editable in the Workbench unless the level locks it. | Matches every delivered part | 7 items, `EL-058-size-grate.md:116` to `RAD-09-integrating-dosimeter.md:83` |
| D12 | Metal ball pipes keep the 1.3 m bore and join clear pipes. They differ in material and in hiding the cargo. The required outcome "oversized cargo jams rather than traversing" is kept and proven with a ball whose `radius` parameter is set above the 0.65 m bore radius (the Tennis ball admits up to 16 m, `elements/TennisBall/TennisBallElementDeclaration.cs:18`). | A 0.62 m gauge would not pass the Basketball (0.68 m across). The spec's objection that this option "needs a new larger body" predates parameterised ball radius | `EL-071-straight-metal-ball-pipe.md:114`, `EL-072-curved-metal-ball-pipe.md:111` |
| D13 | Capacity and size are fixed per kind unless the requirement row names a size parameter. | Fewer controls per part; resizing is the Wall's lesson | 28 items, `EL-001-finite-reservoir.md:85` onward |
| D14 | Capability-family map rows are navigation. The slice's capability probe is the authority; a missing family is added in the Code Map without owner sign-off. | The map predates the current engine | 27 items, `EL-027-canal-lock-chamber.md:93` to `TH-37-heat-sensitive-target.md:77` |
| D15 | Where legacy behaviour or a research table conflicts with the requirement row, the row governs. | Archives record past results, not requirements | The conflict questions among 110 items mentioning legacy or historical behaviour, and `TH-36-lamp-trigger-apparatus.md:86` |
| D16 | A question addressed to a source owner ("owner S418") is decided by the delivering slice at planning and recorded in its Code Map. | The S owner is the slice; no separate decision stage exists | 304 items naming an `S` owner |
| D17 | A continuous sound source is one sustained emission, not back-to-back pulses. | Pulses would retrigger pulse listeners | `EL-179-continuous-tone-speaker.md:79` |
| D18 | Amber stands in for white light in filter and receiver tests until a white source exists; the amber laser failing a purity test is the intended lesson. | The amber laser carries all three channels | `EL-143-red-optical-filter.md:85`, `CAT-055-red_filter.md:89`, `EL-149-yellow-selective-receiver.md:78` |
| D19 | Default for every single-element question not listed: take the spec's proposal; add an optional extra socket, output or mode only when a requirement row or a campaign lesson uses it. | Keeps each part to what a puzzle needs | The remaining items |

**Note on D7.** The requirement says receiver readings "commit … together before the electrical solve" that reads them (`docs/planning/requirements.md:686`, `:6617`). That holds: the readings of a tick commit together after the optical solve, and the electrical solve that consumes them is the next tick's, so no solve sees a half-updated set of readings and none reads an uncommitted value. The cost is one tick between light arriving and a supplied part responding, which a player cannot see and which no lesson times. The alternative would let an electrically switched laser light in the same tick, the two-way flow the authority forbids. If the optics engine slice finds a lesson that needs same-tick response, that is a gameplay defect and it goes to the owner.

### Needs the owner

None open.

EL-098's variant B (a no-gravity pool table, `EL-098-pool-ball.md:72`–`:74`) is decided where it is made: the EL-098 slice decides it at its planning (owner decision of 21 November 2026, recorded by coordinator decision 35), and the campaign's pool lessons use the normal table.

EL-050's question (is reflection required of the acoustic screen) is decided: the screen blocks and attenuates only and does not reflect; redirection is EL-051's (owner decision of 21 November 2026). The requirement row `element-050` and the ledger entry say so.

RAD-01's question (may a radiation source keep a fixed strength for a whole Run) is decided: every steady source debits a finite inventory sized for the 300 s maximum Run, so IX-16 holds as written (coordinator decision 21).

The companion-placement question is decided: a companion package is declared not placeable, has no palette entry and appears only with its parent; S7 adds that capability with the companion admission (owner decision 24b, 23 November 2026).

## 4. Early level proof

All of this section waits for the engine programme (owner decision 22). The counts were measured on 21 November 2026 against 44 delivered packages.

`content/puzzles.json` holds 34 levels: 15 `Playable` and 19 `Planned`, after owner decision 24d removed 29 (content housekeeping, completion-ledger OS-02). Level numbers below are plan levels, the prefix of each level's `subtitle`.

- **16** planned levels use only delivered parts (part kinds in `inventory`, `parts` and `solution` against the 44 delivered package ids). The other three (51a `solar_motor`, 51b `solar_shadow`, 51c `delayed_solar`) need the Flashlight and Solar panel.
- **9 of the 16 can be proven with single-goal evaluation.** The engine evaluates one goal per level, of kind `None`, `Captured`, `ActivatedAfter` or `IntegrityBroken` (`engine/gpu/WorkshopPuzzle.cs:9`, `:75`). Ten planned levels have a single `captured` or `activated` goal, but level 11 `power_trip`'s `activated` goal has no fixed predecessor node, which `ActivatedAfter` requires (`engine/gpu/WorkshopPuzzle.cs:22`–`:24`; its fixed parts are a ball and the lamp).
- **7 wait for the goal slices** (`ENGINE-GOALS`; the goals domain, EL-120 to EL-123): 5 have two to five goals (3 with two, 1 with three, 1 with five), level 12 `battery_motor` has a single `turned` goal, a kind the engine does not have, and level 11 `power_trip` needs an activation goal with no predecessor. `powered_after` also appears (levels 12a `switched_motor` and 19b `double_cold_start`).
- A catalogue part placed as a lesson fixture is admitted today through the Coverage fixture table (`FIX-` rows in `current-consumers.md`, `tools/Coverage/CurrentCatalogue.cs:117`). A level that places any other package as a fixture, or locks a fixture's parameters or connections, needs structural story S8.
- **Owner decision 24d (23 November 2026) removed 29 planned levels and converts 2** as repeats of an already introduced mechanic ([campaign coverage, section 5](../elements/readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)); the content housekeeping slice deleted the 29 from `content/puzzles.json`. The 34 reconcile as 15 Playable + 9 provable with single-goal evaluation + 7 after the goal slices (11, 11d, 12, 12a, 19a, 19b, 20; 19b and 20 in their converted form, completion-ledger OS-08) + 3 blocked on parts.

**Every level must first be authored into the playable shape.** Planned entries use a different content shape (`goals` array, `parts`) that the engine indexes but never constructs (`engine/gpu/PuzzleCatalogue.cs:10`; `engine/gpu/PuzzleDefinition.cs:100`–`:101` refuses any entry that is not `Playable`). A playable entry has `goal`, `placements`, `profiles`, `presentation` and `connections_allowed`. So each level is an authoring task plus a proof, not a test over existing data.
**Proven natively** means what the existing level tests already do (`CuriousContraptions.tests/DominoEffectTests.cs` is the pattern), under [testing guidance](../../testing-guidance.md):

1. The level loads through `PuzzleCatalogue` with its authored fixtures, inventory and goal.
2. The intended solution, placed through the production edit interface, runs in the real C# simulation and reaches the goal within a generous stall guard.
3. A control fails: the unsolved start, or the solution with one required part removed, does not reach the goal.
4. Reset restores the authored state, and save/load of the solved construction round-trips.

No arrival tick, trajectory or hash is asserted. Browser proof stays pending. A level changes to `Playable` only in the slice that proves it.

**Provable with single-goal evaluation (9), after the engine programme.** Numbers are plan levels, the prefix of each level's `subtitle` (content housekeeping, completion-ledger OS-02). Group levels that share parts into one slice.

| Group | Levels (number, id, goal) |
|---|---|
| Signals | 11c `spring_signal` (activated) |
| Air and capture | 11a `air_mail`, 11b `third_dimension`, 03 `a_gentle_rebound` (all captured) |
| Drive | 14a `conveyor_courier`, 14b `belt_relay`, 14c `reverse_belt` (all captured) |
| Ropes | 12b `counterweight`, 12c `pulley_depth` (both activated) |

**After the goal slices (7).** 11, 11d, 12, 12a, 19a, 19b and 20 (19b and 20 in their converted form). Level 11 needs an activation goal with no predecessor node; level 12 needs the `turned` kind; 12a and 19b also use `turned` or `powered_after`.

These levels were authored against the legacy engine. Expect some solutions to need re-tuning on the current solver; that is the useful finding, and it is cheaper before the browser phase.

## 5. Structural stories

### Repairs

Visible = solid repairs of delivered parts (owner decision of 21 November 2026; [engine art-solidity contract](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md), `ENGINE-ART-SOLIDITY`, order 37 of the engine stories index).

| Repair | Scope | When |
|---|---|---|
| Part A ([contract](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md)) | Rope anchor, Pulley, Fan, Impact lever and Trampoline | before the engine programme (owner decision 22). **Delivered** in the candidate on baseline `0959e883`: colliders for the eye, the Pulley's post and rim and the Fan's post and ring, the lever's marks and the Fan's arrow printed flat, five table rows; full local round 7309/0/0 (`Skipped: 0`). Parts B2 and C and the Baseball repair stay queued. |
| Part B ([contract](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b.md)) | Conveyor, Linear pusher and Wound spring | before the programme, after part A. **Implemented** in the candidate on baseline `37fbd3a4`: the two pulley wheels, the pusher's indicator, sockets and ring and the spring's socket are solid, the eight drawn proxies are separated into art and hidden proxies, the raised marks are printed flat, two table rows, and every scene-extractor refusal is reported under its own bundle error code (coordinator decision 3), and the bundle tool has one collider source (coordinator decision 4); independently reviewed; full local round 7353/0/0 (`Skipped: 0`). Lane 1's next slice, before B2, deletes the declaration `Colliders` member and its bundle copy (coordinator decision 6); parts B2 and C and the Baseball repair stay queued. |
| Part B2 ([contract](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b2.md)) | Tennis ball: its seam printed flush, with no collider (owner decision 24i) | before the programme, after part B |
| Part C ([contract](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md)) | Cannon, Windmill and Balloon; the Balloon's first-drawn envelope is recorded as the named defect of owner decision 5 if its test confirms it is momentary | before the programme, after parts B and B2 |
| Baseball repair ([contract](../../../_bmad-output/implementation-artifacts/spec-repair-baseball-el-239.md)) | EL-239 to the owner decisions | before the programme (owner decision 22), serialised with part B2 |
| Part D ([contract](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-d.md), blocked on the programme) | Clutch, Reverse gear and Motor art (prints, the Clutch axle's collider, and the shafts, wheels and rotor seated on their housings); the Counter's floating foot, face panel and sockets ([CAT-020 repair 3](../elements/CAT-020-counter.md#repair-3-seated-foot-face-panel-and-sockets), coordinator decision 33); the floating art that parts B and C leave: the Linear pusher's barrel (its foot raised to it) and upper sockets, the Wound spring's sleeve (its rails widened to it), socket, wheel (a static axle) and latch lever, the Cannon's sockets and charge track, and the Windmill's output wheel (coordinator decisions 39 and 42) | after the programme |
| Face marks and bindings (Batches A and B) | Battery charge marks and plus sign printed flat; Ball detector chevron as two planar strokes; Springboard stripe printed flush; the Switch button's `LocalY` binding deleted (the latch shows by colour); the Delay box's twelve flush-printed marks (owner decision 4) | after the programme |

**Part D, proposed repair contract (Batch C, coordinator decision 6).** "ELEMENTS-VISIBLE-SOLID-REMAINDER part D: Clutch (print both shaft spokes flat, keeping the node names `Visual_InputSpoke` and `Visual_OutputSpoke`; add a static cylinder collider for the axle, r 0.055 m × 1.3 m), Reverse gear (print both index bars, 40 mm proud, and both marks, 100 mm proud, flat) and Motor (print the rotor index bar, 0.48 × 0.07 × 0.004 m and 4 mm proud, as a quad at most 0.5 mm proud); update their visible-solid rows. Scene and table data only." Coordinator decision 33 adds the Counter: seat its foot, face panel and `ActivationIn`, `ActivationOut`, `PowerIn` and `Supply` sockets on the housing, art and hidden proxy moved together, with the seated centres and the re-derived test rows of [CAT-020 repair 3](../elements/CAT-020-counter.md#repair-3-seated-foot-face-panel-and-sockets). Coordinator decision 39 (23 November 2026) adds the floating art that parts B and C leave as drawn: the Linear pusher's two upper sockets, the Wound spring's socket, shaft wheel (a static axle) and latch lever, the Cannon's sockets and charge track, and the Windmill's output wheel, each seated on the solid it belongs to with its proxy, locator and declared port moved together. Coordinator decision 42 (24 November 2026) adds the other floating solids the fifth audit found: the Linear pusher's barrel (seated by raising its foot), the Wound spring's sleeve (seated by widening its rails to it), the Clutch shafts, the Motor rotor and the Reverse gear wheels (each moved back onto its housing with its hinge, rim, port and locator). It runs after the engine programme; its review tier is engine, because the candidate carries `TODO.md` and `sprint-status.yaml`. Drafted 23 November 2026 as [spec-elements-visible-solid-remainder-d.md](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-d.md) (coordinator decision 37), status `blocked` until the programme stories of owner decisions 3a and 24k and coordinator decision 40 (`DOC-ENGINE-STATUS` to `ENGINE-SOA-SIMD`, thirteen in `epics.md` including `ENGINE-WIRE-ABI`, `ENGINE-WASM-THREADS` and `ENGINE-INSTANCED-RENDERING`) are done; the sixth audit's findings and coordinator decisions 42 and 43 are applied (24 November 2026), the revolute connected-collision filter gains a behavioural test in this slice, and the final re-audit is at dispatch.

### S1. Derive new-package registration from declarations — published

Published 21 November 2026 from another session (`spec-registration-derivation.md`, baseline `4f193bb2`; candidate `06ac9aa2`, merged to main as `8d1d68aa`). Its owned scope covered the registry, bundle, capacity, catalogue, palette, coverage-membership and impact tables. It does **not** cover the lists in S2 and S3 below or the capability requirement tables under `tools/Coverage/`.

### S2. Test project compile list becomes a glob

- **Outcome.** A new test file compiles without editing `CuriousContraptions.tests/CuriousContraptions.tests.csproj`.
- **Status.** Delivered by the slice recorded in `_bmad-output/implementation-artifacts/spec-s2-test-compile-glob.md` (21 November 2026): one rule, `<Compile Include="*.cs" />`, compiles every C# file directly in the test project directory, beside the 2 linked files.
- **Before (baseline `7c019d64`).** Every test file was listed by hand: 236 local files plus 2 linked `RuntimeProviders.cs` files. The local list equalled the directory's 236 `.cs` files, none missing and none extra (checked 21 November 2026), and the project file had changed in 20 commits since 5 November. `EnableDefaultCompileItems` stays false (`CuriousContraptions.tests/CuriousContraptions.tests.csproj:7`).
- **Acceptance.** The project compiles exactly the same test sources before and after (compare the compiler logs' source lists). The two linked files stay explicit. Any static-gate test that pins the list is rewritten to assert the rule instead. The full round's test total is unchanged, with zero failed and zero skipped.
- **Removes from an element slice.** One file, and one class of first-round failure (a test file that never ran).

### S3. Remaining per-package lists

- **When.** Before the engine programme (owner decision 22).
- **Outcome.** Adding a package edits one guarded solution line and no other shared membership list (owner decision O1, 21 November 2026). A package that declares parameters still adds its row to the literal `PackageRuntimeParityTests.Parameters(id)` expectation table until that table's queued deletion after S4 (owner decision O2, 21 November 2026).
- **Before.** Not owned by S1: the project list in `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` (trim analysis), the placement switches in `CuriousContraptions.tests/PackageRuntimeParityTests.cs:596` and `:653`, and `CuriousContraptions.slnx`.
- **Acceptance.** Derived from the repository file list or the discovered packages (`TestBundle.Packages`): the static gate's trim-analysis project list (every project file one directory below `elements/`), both placement helpers of `PackageRuntimeParityTests` (declared defaults or given values, with no identity switch), the payload, slider and hinge loops (a non-empty check replaces each name pin), and the native palette rows; the literal registry identity list is deleted, covered by `PartRegistryTests`. Executed controls prove an unfamiliar package is covered and an undeclared one is refused. `CuriousContraptions.slnx` keeps one line per package, guarded by `CSharpStaticGateTests.TheSolutionListsEveryCSharpProject` and `SolutionMembershipTests.TheSolutionContainsEveryPackageTheDomainAbstractionsAndTheGenerator`. The literal `Parameters(id)` table stays (owner decision O2, 21 November 2026) as the one per-package expectation a package with parameters still edits. The four delivered sliders keep their authored joint literals in the shared `authored` table of `PackageRuntimeParityTests`; a slice that changes one of their joint values updates that shared check's expectations (its table rows, or the loop's fixed moving-frame and collision assertions). A slice that adds a slider package, or gives an existing package a slider declaration, authors that package's joint literals in its own tests (residual R3). A slice that adds a payload package, or makes an existing package a capture-target payload, asserts that package's payload declaration or a Basket capture in its own tests (residual R5). No shared check enforces either, and the registration check in `docs/delivery-workflow.md` does not cover them.
- **Status.** Implemented by the slice recorded in `_bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md` (baseline `0959e883`); verified before its commit by the full local round and independent review. Its residuals are queued in `deferred-work.md`.
- **Removes.** From an element slice: the edits to the static gate's trim-analysis project list, the placement helpers of `PackageRuntimeParityTests`, the payload, slider and hinge name pins and the native palette rows, which are now derived, and to the literal registry identity list, which is deleted (covered by `PartRegistryTests`). The solution line stays (owner decision O1, 21 November 2026). Only a package that declares parameters still adds its `Parameters(id)` row, until that table's queued deletion after S4.

### S4. One catalogue conformance suite

- **When.** After the engine programme (owner decision 22).
- **Outcome.** Every package gets the same baseline proof without a hand-written test per package.
- **Design.** One theory over `TestBundle.Packages`, each row run through the production interfaces:
  1. save and load of a placed instance round-trips its typed parameters and connections;
  2. every declared parameter refuses a value below its minimum, above its maximum and non-finite, atomically and with a typed reason;
  3. Run then Reset restores the authored state of every body and store the package declares;
  4. every visible solid in the package art has a collider, and no collider lacks art;
  5. every declared port refuses a connection of the wrong kind;
  6. the catalogue entry's metadata (id, title, category, icon, scene path) resolves;
  7. the package compiles into the physics graph with only generic capability families.
- **Acceptance.** The suite passes for all delivered packages. Per-package tests that only repeat a row are deleted, and the record names each deleted test and the row that covers it. A deliberately broken fixture package fails each row.
- **Removes.** Three to six hand-written tests per element slice, and the largest review finding class (missing or weak baseline tests).

### S5. Generate the wire accessors in the build

- **When.** Before the engine programme (owner decision 22).
- **Before.** `engine/gpu/WorkshopWireGenerated.cs` (6,872 lines) and `engine/gpu/WorkshopAuthoringGenerated.cs` (407 lines) are committed FlatBuffers output for `schemas/workshop_wire.fbs` and `schemas/workshop_authoring.fbs`. The wire file changed in 14 commits since 5 November (+2,428 / −752 lines), and every one of those lines went through review. Windmill added `tools/WorkshopWireGeneration`, which post-processes compiler output into the one supported accessor surface and is covered by `CuriousContraptions.tests/WorkshopWireGenerationTests.cs`. It is referenced only by the test project; no build step calls it.
- **Outcome.** A schema change is reviewed as a schema change. Generated C# is build output.
- **Acceptance.** The normal build runs the pinned FlatBuffers compiler 25.2.10 before compiling consumers; the generated files leave the repository; CI builds from a clean checkout; a schema edit with stale output cannot compile. The pinned compiler is provisioned by the build itself (owner decision O1, 21 November 2026): the vendored project downloads the official release binary for the build host, verifies the archive and binary digests pinned in its project file, and caches it per user. The joining tool `tools/WorkshopWireGeneration` that this sentence first named is deleted with its tests (S5 decision D2): it existed only to make the output committable, and the compiler's per-type files compile directly.
- **Implemented (S5, [contract](../../../_bmad-output/implementation-artifacts/spec-s5-generate-wire-accessors-in-build.md)).** The C# accessors are build output of `CuriousContraptions.FlatBuffers`, generated into its intermediate directory from `schemas/*.fbs`. The domain mapping codecs (`WorkshopWireFlatBuffers` and its partial files, `WorkshopAuthoringFlatBuffers`, `WorkshopSaveCodec` and the Core response encoder) remain ordinary typed C# that converts between engine types and the generated accessors (coordinator decision 38). The hand-coded wire stays until its owners replace it: the hand vtable field counts for unknown-field refusal, the opaque `motion` byte vector and the non-FlatBuffers boundary layouts until `ENGINE-WIRE-ABI` (owner decision 24k), and the generated part of `tools/e2e/workshop-wire.ts` until S5b.
- **Removes.** 500 to 1,500 generated lines from each capability slice's diff and reviews.

### S5b. Generate the TypeScript wire accessors in the build

- **When.** Straight after S5, before the engine programme (owner decision 24h, 23 November 2026; S5 owner decision O4, 21 November 2026). [Contract](../../../_bmad-output/implementation-artifacts/spec-s5b-generate-typescript-wire-in-build.md), drafted 23 November 2026; sixth-audit fixes applied 24 November 2026 (coordinator decision 43), final re-audit at dispatch.
- **Now.** The first 12,731 lines of `tools/e2e/workshop-wire.ts` are flatc TypeScript joined by hand (`schemas/README.md:41-62`); the last 370 are hand-written helpers. Between `66da1ae7` (4 November 2026) and `25df3cd5` 16 commits touched it and the two revisions differ by +5,884/−2,068 lines (`git log --oneline 66da1ae7..25df3cd5 -- tools/e2e/workshop-wire.ts | wc -l`; `git diff --shortstat 66da1ae7 25df3cd5 -- tools/e2e/workshop-wire.ts`). Every importer (27 files under `tools/e2e/`) is a deferred browser suite or the deferred `run-reset-snapshot` unit test.
- **Outcome.** S5's generation target also writes the TypeScript from the same pinned `flatc` (`--ts --gen-all` on the authoring schema, which includes the wire schema), with the README's annotation normalisation, into the ignored `tools/e2e/.wire/`; the generated part leaves the repository; importers take generated names from the generated entry module `.wire/curious-contraptions/wire.ts` (no re-export through `workshop-wire.ts`). The TypeScript FlatBuffers runtime is pinned: `flatbuffers` 25.2.10 is an exact devDependency in the root `package.json` and `package-lock.json`, and the Homebrew `paths` mapping (`tools/e2e/tsconfig.json:16`) is removed (ABI audit G12).
- **Acceptance.** A required-round test checks that the generated module exports every schema type with no `any`, and a guard that `workshop-wire.ts` declares no schema type and no importer takes one from it; S5's build tests gain the TypeScript cases; a test reads the pinned runtime version from the lock file; a test reads the build's binary logs and asserts the generation target `GenerateWireTypeScript` started exactly once per graph; the E2eRunner's import closure resolves the generated `.js` specifiers to their `.ts` sources, with a test. Type-checking the e2e graph stays with the browser deferral.
- **Removes.** Generated TypeScript from every schema slice's review (+5,884/−2,068 lines in that file between `66da1ae7` and `25df3cd5`); the unpinned Homebrew runtime path.
- **Remains after it (ABI audit 1.3).** The hand mapping codecs (W1) stay as ordinary domain-to-accessor mapping, with their refusal moved onto generated accessors by `ENGINE-WIRE-ABI`. The vtable literals and partial unknown-field refusal (W2, G9), the `motion` blob (W3), raw enum fields with per-site refusal (W4), the hand TypeScript helpers and `SNAPSHOT_SCHEMA` (W5) and every non-FlatBuffers layout (W7) are removed or routed by `ENGINE-WIRE-ABI` (owner decision 24k); the hand TypeScript enum mirrors in production script (W6) are replaced by `ENGINE-CAPABILITY-INSTANCES`' generator. Owner decision 24p (25 November 2026) moves W2 and W5 to S5c below. Its ruling (f) sends the wire enums among W6 to `ENGINE-WIRE-ABI`, generated by `flatc` from the schema; only the non-wire enums stay with `ENGINE-CAPABILITY-INSTANCES`.

### S5c. One schema file for the FlatBuffers ABI

- **When.** After S5b and before S6, in lane 2 (owner decision 24p, 25 November 2026). [Contract](../../../_bmad-output/implementation-artifacts/spec-s5c-one-wire-schema-file.md), drafted 25 November 2026.
- **Now.** The ABI is two files, `schemas/workshop_wire.fbs` and `schemas/workshop_authoring.fbs` (which includes the first). Four roots exist (`CWCM`, `CWRM`, `CCWO`, `CCWS`), but the schema declares only one identifier; the other three are hand C# and TypeScript literals. Unknown-field refusal uses hand field counts on 6 of 79 tables. `SNAPSHOT_SCHEMA` is a stale full hand copy. A test pins field lists, and about fifteen test files use literal slots. No required run executes the generated TypeScript.
- **Outcome.** One file, `schemas/workshop_wire.fbs`, declares every type, the four root identifiers (as the enum `WireRootIdentifier`, generated on both sides) and the authoring-only declarations (as a declared `authoring` attribute). Both bindings are generated from it alone. Unknown-field refusal is one rule in the vendored verifier, derived from the generated `Verify` code for every table. Every hand restatement is deleted or derived, or has a row with an owner. A required node harness proves that C# and TypeScript agree. The `flatbuffers` 25.2.10 pin moves to the root manifest.
- **Acceptance.** As in the contract, rows S1 to S14:
  - exactly one schema file, read by every compiler run;
  - identifiers generated and agreed on both sides;
  - TypeScript decodes every root, `CommandKind`, `ResponseKind` and payload arm that C# encoded, and C# decodes what TypeScript re-encoded;
  - malformed frames are refused on both sides; a verifier failure and an appended field at any depth are refused in C#;
  - no runtime root reaches an authoring declaration, and no worker compile names one;
  - no hand restatement remains;
  - the root pin.
- **Removes.** The second schema file and its include, the hand field counts and slots, the hand identifier literals, `SNAPSHOT_SCHEMA` and its hand decoder, the field-list test, and S5b's include ordering, Debug-only generation guard and `tools/e2e` manifest.
- **Remains after it.** There is no TypeScript FlatBuffers verifier, because the pinned compiler writes none. Until `ENGINE-WIRE-ABI`'s first production TypeScript table decoder provides one without hand code, a malformed table that passes the frame checks is refused only in C#. The candidate to evaluate there is the compiler's binary schema with `reflection.fbs`. The C# and TypeScript mirrors of the wire enums stay with `ENGINE-WIRE-ABI`, as do the non-FlatBuffers layouts. Under owner decision 24p's ruling (f), the schema enum is the one source, and S5c adds an interim test that each C# mirror equals its generated enum. The non-wire TypeScript enum mirrors stay with `ENGINE-CAPABILITY-INSTANCES`.

### S6. Table-driven capability plumbing

- **When.** Before the engine programme (owner decision 22).
- **Now.** Adding a capability extends a branch chain in several places, and those methods sit at the complexity limit, so the maintainability gate fails on the next addition. Recent slices recorded `WorkshopDeclaredPresentation.Query` (`engine/gpu/WorkshopDeclaredPresentation.cs:373`) at 28, `BundleValidator.PresentationBindingCapabilities` (`CuriousContraptions.Bundle/BundleValidator.cs:258`) at 27, and three methods at 26 including `BundleSchema.Entry` (`CuriousContraptions.Bundle/BundleSchema.cs:18`); each was repaired by extraction inside the slice that tripped it.
- **First step.** Measure. The gate prints only violations, so run the metrics once with the threshold lowered in a scratch copy and list every production member at 20 or above. The three methods named here are from slice records, not a current measurement.
- **Outcome.** Each per-capability enum chain (a dispatch keyed by a closed capability enum that grows one path per capability) becomes a table keyed by that enum, with one entry per capability and a test that every enum value has a route.
- **Acceptance.** Every per-capability enum chain in the Inventory is a table, each with a test that every enum value has a route; behaviour is unchanged (the full round passes with zero failures and zero skipped, and each slice's added tests are the only change to its totals); adding a capability in a follow-up fixture touches one table row per chain; a table that replaces a dispatch on a per-tick, per-frame or per-pair path states a measured before and after on a reference scene and must not regress (owner decision 3a's criterion; coordinator decision 42, no fixed percentage). The member-count limit is not S6's (owner decision 24h): it moves to `ENGINE-CAPABILITY-INSTANCES`.
- **Removes.** The maintainability repair that recent engine slices each paid, and its re-review.
- **Closure (owner decision 24h, 23 November 2026).** S6 closes when every per-capability enum chain is a table with a test that every enum value has a route; the member-count limit moves to `ENGINE-CAPABILITY-INSTANCES`. The Acceptance above states this; it replaced the earlier 15-member acceptance and the first slice's "completeness by each driver's own test". Coordinator decision 42 (24 November 2026) adds that every chain the audits found is assigned to exactly one slice or excluded with a reason, none claimed twice; the story closes when S6g, the last slice, publishes and its closing run of the inventory commands lists only table rows, kept single-member comparisons and excluded enums.
- **Inventory.** Measured 23 November 2026 at `25df3cd5` and re-run on 24 November 2026 by ast-grep scans (switches by arm label, a member scan for ladders and wide members, type-pattern dispatch), excluding `elements/`, tests, tools and generated code (the counts below are that drafting measurement; the replanning re-runs the scans with `tools/` and `elements/` among the roots). Denominator: 211 production enum declarations (210 names); 38 in scope (S6c 8, S6d 8, S6e 14, S6f 3, S6g 5), 172 excluded with a reason (119 from the first inventory and 53 checked on 24 November, none of which has a switch, ladder or wide member, so none is assigned to a slice); 13 chain groups the earlier inventory missed are assigned (S6c 1, S6d 4, S6e 3, S6f 1, S6g 4) and 2 double claims resolved. The definition of a chain, the commands, the rule for unlisted enums, the assignment table and the exclusion lists with reasons are in [S6b's Code Map](../../../_bmad-output/implementation-artifacts/spec-s6b-response-codec-read-family-table.md).
- **Replanning at dispatch (coordinator decision 43, 24 November 2026; progress check after two audit rounds).** S6b to S6g touch the code each earlier S6 slice reshapes, so each is replanned by the loop's own planning step from the published main that contains the slice before it. The contracts are starting points (status `draft`); their sixth-audit findings (`docs/planning/elements/readiness/audits/audit-s6b-6.md` to `audit-s6g-6.md`: `AnimationKind`, overlap and repeat enums to S6c; `SceneGeometryExtractor` to S6f; scan roots include `tools/` and `elements/`; executable closing commands) are carried into that replanning.
- **Slices**, one publisher at a time, all before the engine programme:

| Slice | Chains | Contract |
|---|---|---|
| S6 (first) | `WorkshopDeclaredPresentation.Query`: continuous `PresentationDriver` query | [spec-s6-table-driven-capability-plumbing.md](../../../_bmad-output/implementation-artifacts/spec-s6-table-driven-capability-plumbing.md) |
| S6b | Wire response codec: the 24 read families of the 41-field response (S6 owner decision O1) | [spec-s6b-response-codec-read-family-table.md](../../../_bmad-output/implementation-artifacts/spec-s6b-response-codec-read-family-table.md) |
| S6c | Presentation: every `PresentationDriver` route and rule with the per-driver source-reference ladders of both admission sites, property, animation property, visual property, curve and mapping | [spec-s6c-presentation-route-tables.md](../../../_bmad-output/implementation-artifacts/spec-s6c-presentation-route-tables.md) |
| S6d | Signal network and goals: node, signal, occurrence and event kinds, signal terminals, request targets, goal kinds (evaluation, validation and content reading) | [spec-s6d-signal-node-route-tables.md](../../../_bmad-output/implementation-artifacts/spec-s6d-signal-node-route-tables.md) |
| S6e | Supply, run control, ports and connections: supply node, consumer and terminal kinds, logic operations, run-control lanes and value kinds, domains, port roles (with the one activation terminal mapping), directions and signal kinds, connection domains, parameter units | [spec-s6e-supply-control-port-route-tables.md](../../../_bmad-output/implementation-artifacts/spec-s6e-supply-control-port-route-tables.md) |
| S6f | Collider shapes: geometry rows for `ColliderShapeKind` (both declarations), `ShapeKind` and the private placement `Shape`, and the narrowphase shape-pair table | [spec-s6f-shape-pair-tables.md](../../../_bmad-output/implementation-artifacts/spec-s6f-shape-pair-tables.md) |
| S6g | Mechanical kinds: coordinate kinds, continuous-work inputs (including the per-tick payment), body targets and selectors, mechanical value sources; its closing run of the inventory closes S6 | [spec-s6g-mechanical-kind-tables.md](../../../_bmad-output/implementation-artifacts/spec-s6g-mechanical-kind-tables.md) |

### S7. Coverage admits a named-identity package

- **When.** After the engine programme (owner decision 22), before the first package that is not a catalogue part.
- **Outcome.** The Coverage tool admits a package bound to one named identity that is not a catalogue part, including TH, RAD and GAP identities and declared companion packages under one identity (owner decision 15), and an identity whose acceptance anchor is a sequence task alone (EL-240, EL-241; [new-identities readiness](../elements/readiness/new-identities.md), section 6). It also lets a companion package be declared not placeable: it has no palette entry and appears only with its parent (owner decision 24b, 23 November 2026): the parent declares a seat pose for it, so placing the parent places the companion there and removing the parent removes it (EL-078, EL-084, EL-088, GAP-09, TH-25, TH-34); the companion's parameters are edited on it like any part's; fracture fragments appear only from their parent's break (coordinator decision 33, with `ENGINE-FRACTURE`). A seated companion stays on its seat (the player cannot move it off), but the player may delete it alone in Build and restore it from its parent, and a level may place a parent without its companion; Reset restores the authored state whether the companion is present, deleted or never authored, and save and load keep whether it is present and its parameters (coordinator decision 34; whether the level-data clause is S7's or S8's is settled when S7 is replanned). One identity per placeable part. No CAT number is minted.
- **Now.** Main's reader binds a named package through a final section of its element spec (`tools/Coverage/NamedPackageSources.cs`; pattern [EL-239 Baseball](../elements/EL-239-baseball.md)); it lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor. The drafted contract `spec-s7-coverage-named-identity-admission.md` predates owner decision 15 and the spec-section binding on main, and is replanned from the published main when this story starts. What each batch needs from it is listed in the batch summaries (Batches F, H, J, L, M, N and O).

### S8. Lesson fixtures for any admitted package

- **When.** After the engine programme (owner decision 22). Required before any lesson or Early level proof that places such a fixture or withholds a player control; many specs name it (the three planned solar levels lock the Flashlight, a locked fibre pair needs a pre-made connection, and the rope lessons of levels 11–20 may withhold "Cut rope" so a cutting part is the only way to cut).
- **Outcome.** A level may place any admitted package as an authored fixture with a locked pose, per-parameter locks and locked connections, outside the player's inventory, with no per-package fixture code: the fixture record is derived from the package's declaration. **Withheld player controls (coordinator decisions 24l and 40, 23 November 2026).** The level format gains one level-data field listing the player controls the level withholds, "Cut rope" included; its values are a closed C# enum of player controls, read as text only at the content boundary, and an unknown value is refused. A withheld control is not offered in the UI, and the command it would send is refused for that level with a typed reason at the command boundary, so no other route reaches it. A level without the field withholds nothing.
- **Acceptance.** A level that fixes two differently shaped packages loads; the fixtures cannot be moved, edited, rewired or deleted in Build, and a locked parameter refuses an edit with a typed reason; Run, Reset, save and load keep them; a fixture naming an unknown package, an out-of-range parameter or an inadmissible connection is refused when the level is admitted. A level that withholds "Cut rope" shows no cut control on its ropes during Run, and a `CutRope` command sent on that level is refused with its typed reason while the rope stays intact; the same level without the field offers the control and the cut succeeds; a level naming an unknown control is refused when it is admitted; Reset, save and load keep the withheld set.
- **Now.** The Coverage fixture table binds every lesson fixture to a catalogue part and its `CAT-NNN-V` proof criterion (`tools/Coverage/CurrentCatalogue.cs:117`, `:187`; `tools/Coverage/RequirementDiscovery.cs:84`), so a fixture whose part is not a catalogue part is refused. No level-data field withholds a player control: the authored puzzle record (`schemas/workshop_authoring.fbs:62-77`, `PuzzleRecord`) and the content definition (`engine/gpu/PuzzleDefinition.cs:27-32`) have none, the "Cut rope" button is offered for a selected roped part while Running or Paused with no level condition (`ui/WorkshopConnections.cs:66-73`) and the command is accepted on every level (`engine/MachineWorld.Gpu.cs:134`). The one existing level restriction is the boolean `connections_allowed` (`engine/gpu/PuzzleDefinition.cs:146`; enforced at `engine/gpu/PuzzleCatalogue.cs:95`); the planning scout decides whether it becomes a member of the withheld set, so one rule governs what a level withholds. What else exists today was not surveyed; the planning scout does that first.
- **Source.** Batch J readiness, gap 11, with the S7 planning record's fixture finding.

## Counts and how they reconcile

- **Ledger:** 309 named identities in [named-elements.md](named-elements.md): 242 EL (EL-217 to EL-241 ledgered on 21 November 2026, EL-242 on 23 November 2026), 37 TH, 19 RAD and 11 GAP. Its 22 radiation and 18 gap headings also hold 3 and 7 scope indexes that forward to EL identities and are not counted. Every EL, TH and RAD identity has an element spec; of the GAP identities only GAP-04, GAP-06, GAP-07 and GAP-09 do, and the seven interface capabilities (GAP-10 to GAP-15 and GAP-17) have ledger entries only. There are also 72 CAT specs.
- **Delivered:** 45 packages under `elements/`: 44 matching one CAT spec each, and the Baseball (EL-239). 28 CAT parts remain.
- **Section 2:** 50 refining identities: 35 candidate duplicates (15 of delivered parts, 20 of undelivered parts) and 15 refinements or variants with rows of their own. No identity is listed in more than one of 2a, 2b and 2c; section 2d only explains why EL-071, EL-072, EL-242 and TH-05 are not closures.
- **Section 1** groups identities for planning; an identity named in no row of sections 1 and 2 is a single element, placed by the roadmap's teaching order, whose spec names the engine stories it waits for.
- **Section 4:** 34 levels = 15 playable + 9 provable with single-goal evaluation + 7 after the goal slices + 3 blocked on parts, after owner decision 24d removed 29 (measured 21 November 2026 over the 63 then authored: 15 + 11 + 34 + 3; level 02 moved to the goal slices).
