# Engine stories index (23 November 2026)

The one current text of every engine story is in [epics.md, "Engine-first capability stories"](../../../../_bmad-output/planning-artifacts/epics.md#engine-first-capability-stories-21-november-2026). This index lists them in order, maps every capability an element spec marks missing or narrower to one story, records retired and renamed names, and records each contradiction between proposals and which text now governs. Sources: [owner decisions](owner-decisions-2026-11-22.md) (binding), the engine design audit (`docs/planning/elements/readiness/engine-design-gap.md`), the conventions re-audit and its repair (`docs/planning/elements/readiness/audits/conventions-reaudit.md`), the ABI, wire and engine-interface audit (`docs/planning/elements/readiness/audits/abi-interfaces.md`; owner decisions 24j and 24k), the readiness summaries in this folder (batches A to O and new identities) and the element audits (`docs/planning/elements/readiness/audits/elements-*.md`). Coordinator decisions 26 to 30 (23 November 2026) are applied in both files; no capability awaits an owner.

**Gate (owner decisions 3a and 22).** Before the engine programme (orders 1 to 10, which always include 8a `ENGINE-WIRE-ABI` and 8b `ENGINE-WASM-THREADS`, owner decision 24k, and 8c `ENGINE-INSTANCED-RENDERING`, coordinator decision 40) only repairs and tooling run: visible-solid repair parts A, B, B2 and C, the Baseball repair, and structural stories S3, S5 and S6, with S5b and S6's follow-on slices (owner decision 24h) and S5c-1, S5c-2 and S5c-3 after S5b (owner decision 24p; coordinator ruling, 26 November 2026). Under owner decision 24m (24 November 2026) the lane 3 slices (lettered level ids, then the content slice once its contract passes planning) and the lane 4 policy-tool fixes also run before the programme, in up to four lanes. DOC-ENGINE-STATUS, programme order 1, documents only, runs in lane 4 from 25 November 2026 in parallel with the remaining pre-programme work (owner decision 24q); order 2 onwards waits. The five goal slices, Early level proofs, tests-only patches, closure reviews and new elements wait for the programme. After it: gas network, liquid, thermal (owner decision 18); then element slices follow the order in which the campaign levels first teach them, whichever capability they need (owner decision 24a), and every other story lands before the first element in that order that needs it. Document order is the order below; each story's *Depends on* line governs when it may start.

## 1. Stories

"Unblocks" counts the element identities named in the story's Unblocks line (ranges counted in full); "all" means every element spec. These counts and the capability-to-story counts below are derived from the story text, which is the single authority: where a count disagrees with an Unblocks line in epics.md, the line governs and the count is corrected, never the line. Status is today's: every story is Backlog unless stated.

| Order | Story | Sprint key | Depends on | Unblocks | Status today |
|---|---|---|---|---|---|
| 1 | DOC-ENGINE-STATUS | `doc-engine-status` | none | all | Backlog |
| 2 | ENGINE-SPATIAL-QUERIES | `engine-spatial-queries` | 1 | 13, the segment cast of every optical and radiation identity, and the candidate source of 9 later stories | Backlog |
| 3 | ENGINE-CONTACT-OBSERVATIONS | `engine-contact-observations` | 2 | 18 | Backlog |
| 4 | ENGINE-SENSOR-VOLUMES | `engine-sensor-volumes` | 2 | 12 | Backlog |
| 5 | ENGINE-ISLANDS-SLEEP | `engine-islands-sleep` | 2 | 9, and every resting part | Backlog (absorbs `engine-resting-stack-settling`) |
| 6 | ENGINE-ONE-WAY-INSTALL | `engine-one-way-install` | 1 | 1 (CAT-003's first committed sample), and lifecycle for all | Backlog |
| 7 | ENGINE-SUPPLY-DEMAND | `engine-supply-drive-demand` | 1 | 38 | Backlog |
| 8 | ENGINE-CAPABILITY-INSTANCES | `engine-capability-instances` | 4 | 56 | Backlog |
| 8a | ENGINE-WIRE-ABI | `engine-wire-abi` | S5, S5b, S5c-1 to S5c-3, 6, 8 | 0 (wire, shared memory and presentation feed) | Backlog (owner decision 24k) |
| 8b | ENGINE-WASM-THREADS | `engine-wasm-threads` | 8a, 5 | 0 (threads and shared memory) | Backlog (owner decision 24k); first slice is the gate that threads work with the AOT build |
| 8c | ENGINE-INSTANCED-RENDERING | `engine-instanced-rendering` | 8a | 0 (instanced draws from the pose ring; Story 16.2) | Backlog (coordinator decision 40); first slice is the gate that establishes the WebGL 2 instanced path and whether WebGPU is reachable |
| 9 | ENGINE-TICK-ALLOCATION | `engine-tick-allocation` | 5, 8, 8a | 0 (performance) | Backlog |
| 10 | ENGINE-SOA-SIMD | `engine-soa-simd` | 9, 8b | 0 (performance) | Backlog |
| 11 | ENGINE-GAS-NETWORK | `engine-gas-network` | 1–10, 17, 21, 22 | 35 | Backlog |
| 12 | ENGINE-LIQUID | `engine-liquid` | 11, 1–10, 14, 17, 19, 21 | 61 | Backlog |
| 13 | ENGINE-THERMAL | `engine-thermal` | 11, 12, 1–10, 17, 19, 21 | 55 | Backlog |
| 14 | ENGINE-JOINT-KINDS | `engine-joint-kinds` | 1–10 | 39 | Backlog |
| 15 | ENGINE-MECHANICAL-LINKS | `engine-mechanical-link-kinds` | 22 | 4 | Backlog |
| 16 | ENGINE-TENSILE-EXTENSIONS | `engine-tensile-extensions` | 1–10, 14, 22 (cutting clause) | 8, and 2 conditional on their probe | Backlog |
| 17 | ENGINE-SIGNAL-NODES | `engine-signal-nodes` | 1–10 | 70 | Backlog; its self-edge row's slice text is in `epics.md` ("Slice: self-edge row", under this story); the separate drafted contract was retired 23 November 2026 |
| 18 | ENGINE-COMPLIANT-FRICTION | `engine-compliant-friction` | 2, 8 | 2 | Backlog |
| 19 | ENGINE-PARAMETER-BOUND-FRAMES | `engine-parameter-bound-frames` | 1–10 | 37 | Backlog |
| 20 | ENGINE-PROFILE-COLLIDER | `engine-profile-collider` | 2, 10 | 1 (conditional) | Backlog |
| 21 | ENGINE-RUN-LENGTH | `engine-run-length` | 1–10 | 33 | Backlog |
| 22 | ENGINE-CONNECTION-MATERIALS | `engine-connection-materials` | 2, 8 | 22 | Backlog |
| 23 | ENGINE-GOALS (five slices) | `engine-goal-evaluation`, `engine-goal-supplied-occurrence`, `engine-goal-progress-observation`, `engine-goal-ordered-events`, `engine-goal-count-window-hold` | 1–10 (4 especially), S5, 21 | 12, and every multi-goal level | Backlog; first slice drafted (`spec-engine-goal-evaluation.md`, draft, lists all five; waits for the programme and S5) |
| 24 | ENGINE-OPTICAL | `engine-optical-propagation` | 2, 7, 13, 17 | 91 (including the radiation identities that share its solve) | Backlog |
| 25 | ENGINE-RADIATION | `engine-radiation` | 24; 12 (liquid crossing); 19, 21 | 26 | Backlog |
| 26 | ENGINE-FIELD-FORCE | `engine-field-force` | 2, 5, 7, 19 | 11 | Backlog |
| 27 | ENGINE-FRACTURE | `engine-fracture` | 3, 14; 31 for player-made connections | 11 | Backlog |
| 28 | ENGINE-IGNITION-BLAST | `engine-ignition-blast` | 2, 3; burn half 13, 24 | 9 | Backlog |
| 29 | ENGINE-GRANULAR | `engine-granular` | 12, 2, 3 | 2 | Backlog |
| 30 | Story 14.0 (ENGINE-ACOUSTIC) | `14-0-acoustic-propagation-occlusion-reception-engine` | 2, 3, 7, 17, 19 | 18 | Backlog |
| 31 | ENGINE-ASSEMBLY-JOINTS | `engine-assembly-joints` | 14, 8, 17 | 13 | Backlog |
| 32 | Package joints on a dynamic base | `engine-characters` | 3, 7, 14 | 4 | Backlog |
| 33 | ENGINE-REACTION-THRUST (name proposed, not in 3a) | `engine-reaction-thrust` | 2, 7 | 5 | Backlog |
| 34 | ENGINE-SURFACE-STATE (name proposed, not in 3a) | `engine-surface-state` | 3, 23 (first slice) | 2 | Backlog |
| 35 | ENGINE-PARAMETER-COMBINATIONS (name proposed, not in 3a) | `engine-parameter-combinations` | 19 | 2 | Backlog |
| 36 | ENGINE-TUNNEL-QUALIFICATION (name proposed, not in 3a) | `engine-tunnel-qualification` | 2 | 0 | Backlog |
| 37 | ENGINE-ART-SOLIDITY (name proposed, not in 3a) | `engine-art-solidity` | 8 | all packages | Drafted contract, blocked (`spec-engine-art-solidity.md`) |
| 38 | ENGINE-PARALLEL-ISLANDS (name proposed, not in 3a) | `engine-parallel-islands` | 8b, 10, 5 | 0 | Conditional: only if order 10 misses the tick target at P0-034 |

**Execution position (derived from the Depends-on column; the owner confirmed it on 24 November 2026 against decision 18's "the 3a programme, gas, liquid, thermal, the rest": the general stories these engines depend on land first, right after the programme; recorded under owner decision 18).** The Order column is the story's identity, not the start order for every story. A loop starts a story only when every story in its Depends-on is done, so it reads this sequence: (1) orders 1 to 7, 8, 8a, 8b, 8c, 9 and 10 as listed; (2) orders 17, 21 and 22, which gas needs (signal nodes, run length, solid hose strand); (3) order 11, gas; (4) orders 14 and 19, which liquid needs; (5) order 12, liquid; (6) order 13, thermal; (7) every other story by its Depends-on (15, 16, 18, 20, then 23 to 38). The inversion is the dependency of 11 to 13 on 14, 17, 19, 21 and 22, nothing else.

**Execution inversions** (stated in the epics preamble): 17, 14, 19, 21 and 22 land before 11 to 13 where those stories or their first consumers need them; 22 lands before 15 and before 16's cutting clause; 13's radiant-heating row is an interface whose end-to-end proof lands in 24.

**Performance stories** (measured before and after on fixed reference scenes R1 to R6, deterministic counters asserted, wall time recorded; owner decision 3a): 2, 3, 4, 5, 8a (R6), 8b (R6, R1), 8c (R1, R2), 9, 10, 12, 20, 24, 25, 26, and 29 (its own scene).

**Sprint keys.** Every key in the table is in `_bmad-output/implementation-artifacts/sprint-status.yaml` as `backlog`, in this order, in its "Engine-first capability stories" block (23 November 2026); `14-0-acoustic-propagation-occlusion-reception-engine` stays in epic-14 and the block notes its order. `engine-resting-stack-settling` was removed there (absorbed by `engine-islands-sleep`), and the superseded comments "after 12-4" (`engine-gas-network`), "after engine-assembly-joints" (`engine-fracture`) and "after … engine-thermal" (`engine-radiation`) were deleted; the Depends-on lines above govern.

## 2. Capability → story map

**Method.** Every table row in `docs/planning/elements/*.md` whose status cell begins "missing" or "narrower" was extracted (1,192 rows across the CAT, EL, TH, RAD and GAP specs, read 23 November 2026 while two other sessions were editing them) and given one primary owner. Coordinator decisions 28 and 29 then assigned, without re-extraction, the six rows the extraction left unowned and moved one: CAT-010:65, TH-22:82 and CAT-019:81 to ENGINE-CAPABILITY-INSTANCES; CAT-065:119 to ENGINE-COMPLIANT-FRICTION; TH-23:73 and TH-25:70 to ENGINE-THERMAL; EL-218:67 from ENGINE-MECHANICAL-LINKS to ENGINE-JOINT-KINDS. The counts below include them. The extraction's primary owner was the story the row names, or, where it names several, the one that owns the capability (a row reading "X, with candidates from ENGINE-SPATIAL-QUERIES" is owned by X; the spatial query is a dependency). Rows that name no story were classified by their family code (the acoustic N and C codes of EL-044, the liquid W codes of EL-001, the thermal R codes of TH-04, the GAP and station families). Where a row names several stories, the first one named in its owner cell, after removing a spatial-query dependency, is taken; the reconciliations of section 4 then override it. The identities each story owns rows for were diffed against that story's Unblocks line in `epics.md`, and every identity with an owned row was added to it; an identity listed without a table row is one a readiness summary names (a residual, a re-qualification or a conformance gap stated in prose).

| Owning story | Rows | Capability families (element-free) |
|---|---|---|
| ENGINE-OPTICAL | 245 | optical solve, emitters, responses, relays, receivers, credit to a store, optics palette member |
| ENGINE-LIQUID | 146 | stores, apertures, crest rule, streams, immersion, seats, work targets, observations, snap joins compiled to edges |
| ENGINE-THERMAL | 101 | nodes, links, heat moved with liquid, sources, phase rows (one condensing row), expansion and its committed load, heated-envelope lift, credited store, humidity, thermal units and drivers |
| ENGINE-GAS-NETWORK | 85 | fluid port record, stores, lines, valves, chambers, nozzle, stroke and damping, pressure units |
| Story 14.0 | 82 | emission, propagation, occlusion, band mask, paid and excited emitters, routes, Audio domain, event kind, read model |
| ENGINE-SIGNAL-NODES | 80 | threshold, edge, count, boolean, self-edge, rearming timer, coordinate-state and parameter-set contacts, Level outputs |
| ENGINE-SPATIAL-QUERIES | 59 | persistent trees, region and segment queries, scene scans removed |
| ENGINE-RADIATION | 43 | channels, attenuation rows, inventories, accumulators |
| ENGINE-JOINT-KINDS | 42 | fixed, spherical and dynamic-support joints, spring rest position, holds, retention, revolute position drive, crank as a pinned rod |
| ENGINE-SUPPLY-DEMAND | 34 | drive debit, standing demand, work targets, shaft-paid supply |
| ENGINE-PARAMETER-BOUND-FRAMES | 29 | parameter-bound frames, ports, colliders; Choice-bound values; parameter-selected parts |
| ENGINE-FIELD-FORCE | 27 | uniform field, regions, direction as data, payment kinds, scene environment record |
| ENGINE-CAPABILITY-INSTANCES | 26 | list-valued surfaces, snap-socket lists (the liquid W3 rows, whose other two clauses are 11's port record and 12's compiled join), more than two mouths, coordinate drivers whatever the joint kind, wrap mapping |
| ENGINE-FRACTURE | 25 | load limits, measures, fragments, one-way release of rope ties and attachments |
| ENGINE-GOALS | 23 | condition list, count, window, hold, order, supplied occurrence, scalar-band qualifier, progress |
| ENGINE-CONNECTION-MATERIALS | 19 | material record, solid strand, straight static strand, typed values on a connection |
| ENGINE-IGNITION-BLAST | 16 | burn, fuel store, extinction, blast impulse |
| ENGINE-ASSEMBLY-JOINTS | 15 | typed mount and point sockets, player-made connections, commanded release, attachment state |
| ENGINE-SURFACE-STATE | 14 | treatment regions, dose stores, surface tags, `Treated` observation |
| ENGINE-GRANULAR | 12 | fractions, apertures, jams, piles |
| ENGINE-MECHANICAL-LINKS | 10 | force-bounded and toothed couplings, pitch radius, engagement class, link art, transmissions in series |
| ENGINE-SENSOR-VOLUMES | 9 | frame-relative, in-tick, shape-based sensors; passage speed; residence phase on the read |
| ENGINE-TENSILE-EXTENSIONS | 9 | dynamic guide, winding, cutting |
| ENGINE-REACTION-THRUST | 7 | ambient reaction, converging flow, angular drag |
| ENGINE-CONTACT-OBSERVATIONS | 6 | integrity observer without a gas store, stress concentration, repeating qualified hit |
| ENGINE-RUN-LENGTH | 3 | per-level Run length (most rows longer than 30 s state their Run length in prose, not in a probe row) |
| ENGINE-PARAMETER-COMBINATIONS | 2 | excluded option tuples |
| ENGINE-COMPLIANT-FRICTION | 2 | surface friction, stability-bounded admission, deflection read and sag |
| ENGINE-ISLANDS-SLEEP | 1 | waking sleepers on region entry |

Rows owned outside the engine stories: S7 coverage admission 7; Story 15.0 placement assistance 6; story CAT-023c (offset centre of mass in motion samples) 2. Rows that need no capability: 1 accepted limitation (CAT-023 "has fallen" sensing), 1 not needed (EL-059 analogue load read). False positives of the extraction (acceptance-table rows whose names contain "Missing"): CAT-018:159, CAT-042:186, EL-155:117. Counts are of rows, not distinct capabilities; a capability shared by a family appears once per spec.

### Capabilities formerly without an owner (coordinator decisions 28 and 29)

The epics section "Capabilities awaiting an owner" is deleted; each capability is now in its story's text: the continuous coordinate-travel driver for a prismatic coordinate (CAT-010:65, TH-22:82, TH-23) and the wrap mapping (CAT-019:81) in ENGINE-CAPABILITY-INSTANCES; stability-bounded admission, the per-sample deflection read and the sag binding (CAT-065:119 and CAT-065:111's admission clause, EL-215) in ENGINE-COMPLIANT-FRICTION; the first committed sample on construction and Reset (CAT-003 open question O1) in ENGINE-ONE-WAY-INSTALL; the expansion binding's committed load (TH-23:73) in ENGINE-THERMAL; two transmissions in series (EL-054) in ENGINE-MECHANICAL-LINKS; the crank (EL-218:67) in ENGINE-JOINT-KINDS; buoyancy in air (TH-25:70, CAT-003, EL-090) as declared ambient lift, with a heated envelope's lift bound to committed temperature in ENGINE-THERMAL through ENGINE-PARAMETER-BOUND-FRAMES' committed-state binding (coordinator decision 28). The spec rows name these owners (re-pointed by the cross-batch pass, 23 November 2026).

### Capabilities with two owners, and the one that now governs

| Rows | The two owners named | Governs | Reason |
|---|---|---|---|
| RAD-21:68, TH-33:69 (retention of a coordinate) | ENGINE-ASSEMBLY-JOINTS (batch O G6, batch N item 7) | ENGINE-JOINT-KINDS | C2 |
| EL-224:69, EL-225:78, EL-226:68, EL-228:67 (hold plus release) | ENGINE-JOINT-KINDS hold, ENGINE-ASSEMBLY-JOINTS release | ENGINE-JOINT-KINDS | C2 |
| EL-185:106 (rigid weld) | ENGINE-JOINT-KINDS kind, ENGINE-ASSEMBLY-JOINTS player-made | both, as two capabilities | the joint kind is 14's; making it from a player connection is 31's |
| EL-001:109, EL-032:71 (Pa and m³ units) | ENGINE-LIQUID, or ENGINE-GAS-NETWORK "if it lands first" | ENGINE-GAS-NETWORK | C5 |
| CAT-059:87 M10b (credited store row) | ENGINE-THERMAL, or ENGINE-SUPPLY-DEMAND at the coordinator's choice | ENGINE-THERMAL | C8 (coordinator decision 26) |
| EL-186:118 (attachment-state read) | "the same two stories" (assembly and fracture) | ENGINE-ASSEMBLY-JOINTS (state); ENGINE-FRACTURE publishes the broken state into it | C22 |
| CAT-009:57 (repeating qualified hit) | Story 14.0 (N14) and the contact rows | ENGINE-CONTACT-OBSERVATIONS (qualification); Story 14.0 (the emission) | C23 |
| TH-03:66, TH-33:74 (combustion C2 to C5) | ENGINE-IGNITION-BLAST and ENGINE-THERMAL | split by clause as the rows state (C2 to C4 ignition; C5 thermal) | not a conflict |
| TH-36:73 (passive-slider endpoint contact) | ENGINE-SIGNAL-NODES, earlier ENGINE-ASSEMBLY-JOINTS | ENGINE-SIGNAL-NODES | C3 |
| EL-061:117 (revolute rest-window supply contact), EL-221:65 (contact closed while a drive is Blocked) | ENGINE-JOINT-KINDS, as those rows state | ENGINE-SIGNAL-NODES owns the contact; ENGINE-JOINT-KINDS publishes the window and Blocked observations it reads | C3; both rows now name ENGINE-SIGNAL-NODES |
| EL-218:67 (crank coupling) | ENGINE-MECHANICAL-LINKS, fallback ENGINE-JOINT-KINDS | ENGINE-JOINT-KINDS | C25 (coordinator decision 29) |
| EL-001 W15, TH-04 T5 (gas–liquid edge) | ENGINE-LIQUID, ENGINE-GAS-NETWORK | ENGINE-LIQUID | C30 (coordinator decision 26) |
| CAT-065:111 (compliant surface) | ENGINE-SPATIAL-QUERIES (sampling) and ENGINE-CAPABILITY-INSTANCES (slots) | split by clause; not a conflict | batch C decision 1 |

### Engine audit findings and their story

Every finding of the engine audit's sections 1 to 4 has one owner: world copy and decode, SIMD, narrowphase cost, friction preparation → 10; broadphase, fattening, plane bounds, pair order, joint scan, every scan of "broadphase users" → 2 (sensors and guides with 4; contact rows with 3); islands, sleeping, restitution cap, motor and continuous-work replays → 5; allocation, host tick path, activation-network matching → 9; pose ring (its copy, slot array and missing reader) → 8a (owner decision 24k; ABI audit G1, which corrects the engine audit's row 54); instanced draws and per-body node transforms → 8c (coordinator decision 40; the batching ABI audit G1 left without an owner); parallel dispatch → 38; tunnelling → 36; kinematic bodies, documentation drift, hollow static-only text, determinism unestablished → 1; drives (debit) → 7; joints and drives (kinds) → 14; contact events → 3; sensors on moving frames and host passage sensing (L7) → 4; compliant friction → 18, reservation → 8; E1 to E9, E11 to E13, L2 and the GPU names → 8; E14 → closed by S3 owner answer O1 (21 November 2026): one guarded solution line per package; `.slnx` has no wildcard; E10 → 11; L6 → 6; L4 and L5 → not engine (visible-solid repairs and the decisions of 21 November, owner decision 8 and coordinator decision 25).

## 3. Retired and renamed names

| Name in earlier text | Now | Authority |
|---|---|---|
| `powered-part-demand-drain`, `engine-powered-part-demand-drain` | ENGINE-SUPPLY-DEMAND, key `engine-supply-drive-demand` | owner decision 3a |
| `engine-supply-self-edge`, `engine-supply-self-edge-cycle-rule`, ENGINE-SUPPLY-SELF-EDGE | self-edge row of ENGINE-SIGNAL-NODES (the drafted contract's text is now that row's slice in `epics.md`; the contract file is retired) | 3a; coordinator decision 16 |
| `engine-pulse-count-saturation`, ENGINE-PULSE-COUNT-SATURATION, ENGINE-SIGNAL-COUNT-RESET, ENGINE-SIGNAL-EDGE, ENGINE-OSCILLATOR-SUPPLY-NODE, `engine-signal-rearming-elapsed-timer` | rows of ENGINE-SIGNAL-NODES | 3a; coordinator decision 16 |
| ENGINE-SUPPLY-CONTACT-SETTING, `engine-supply-contact-setting`, `engine-supply-parameter-contact` | "contact set by a parameter" row of ENGINE-SIGNAL-NODES | coordinator decision 25 |
| ENGINE-SPRING-REST-COORDINATE | spring rest position of ENGINE-JOINT-KINDS | coordinator decision 21 |
| ENGINE-ISLAND-SLEEP | ENGINE-ISLANDS-SLEEP | 3a |
| `engine-resting-stack-settling` | absorbed by ENGINE-ISLANDS-SLEEP (its stacks are the story's control) | engine audit section 6 item 5 |
| `engine-scene-environment` | scene-region slice of ENGINE-FIELD-FORCE | coordinator decision 16 |
| ENGINE-CHARACTERS ("declared autonomous agents") | "Package joints on a dynamic base", key `engine-characters` kept | coordinator decision 16 |
| ENGINE-PARAMETER-SELECTED-PARTS, ENGINE-CHOICE-BOUND-VALUES | rows of ENGINE-PARAMETER-BOUND-FRAMES | coordinator decision 16 |
| ENGINE-SPAN-CUTTER, ENGINE-TENSILE-GUIDE, `engine-tensile-dynamic-guide` | cutting and dynamic-guide clauses of ENGINE-TENSILE-EXTENSIONS | 3a; coordinator decision 16 |
| ENGINE-PASSAGE-SPEED, "Frame-relative sensors" | ENGINE-SENSOR-VOLUMES | 3a |
| `engine-axial-gas-nozzle`, ENGINE-PRISMATIC-STROKE-OBSERVATION | ENGINE-GAS-NETWORK | engine audit section 5 and 6 item 13 |
| ENGINE-CONVERGENT-AIRFLOW | flow-sense clause of ENGINE-REACTION-THRUST | batch L section 4 |
| ENGINE-BOUNDED-QUERIES | ENGINE-SPATIAL-QUERIES (regions, sweeps) and ENGINE-ISLANDS-SLEEP (drive power trials) | batch C section 4 E4 |
| ENGINE-COMPLIANT-SURFACE | split: ENGINE-SPATIAL-QUERIES, ENGINE-CAPABILITY-INSTANCES, ENGINE-COMPLIANT-FRICTION (which also takes admission, deflection read and sag) | batch C decision 1; coordinator decision 29 |
| ENGINE-ROPE-PORT-ROLES | ENGINE-CAPABILITY-INSTANCES | batch C decision 3 |
| ENGINE-PRISMATIC-TRAVEL-PRESENTATION | coordinate drivers of ENGINE-CAPABILITY-INSTANCES | coordinator decision 29 |
| "Displaced-density lift for gas-store bodies" (the merge's recommendation) | withdrawn: declared ambient lift; a heated envelope's lift bound to committed temperature (ENGINE-THERMAL) | coordinator decision 28 |
| `engine-goal-window-hold` | `engine-goal-count-window-hold` | owner decision 12 |
| ENGINE-ACOUSTIC | Story 14.0 (number and key kept; its text moved into the engine section, Epic 14 keeps a pointer) | owner decision of 18 October 2026 |
| `engine-mechanical-link-kinds`, `engine-optical-propagation`, `engine-goal-evaluation` | sprint keys of ENGINE-MECHANICAL-LINKS, ENGINE-OPTICAL and the first goal slice | kept |
| ENGINE-ASSEMBLY-JOINTS' former items (holding brake, retention with latched release, strike-and-hold and position drive, elastic hinge, winch, elastic span, crank coupling, shaft-to-electrical conversion) | ENGINE-JOINT-KINDS (crank included), ENGINE-TENSILE-EXTENSIONS, ENGINE-CONNECTION-MATERIALS, ENGINE-SUPPLY-DEMAND | 3a; coordinator decisions 16 and 29 |

The anchors that specs cite were kept: the section heading, and the ENGINE-OPTICAL, ENGINE-LIQUID, ENGINE-GOALS and Story 14.0 headings are unchanged.

## 4. Contradictions reconciled

Each entry: what disagreed, what governs, why.

- **C1. Table bounds.** Batch F (liquid), batch N (thermal), batch M (granular item h), batch O (a per-cast crossing cap) and EL-044 proposed declared capacities with a typed refusal at Run or compile; design contract D5 forbids arbitrary population ceilings. *Governs:* D5: tables sized at Run from compiled declarations, no scene ceiling; a typed refusal only for a per-declaration limit a package declares (an emitter's segment or interaction limit); a segment cast returns every crossing on its segment, with no crossing cap (coordinator decision 26). *Why:* D5 is a binding design contract; batch D already rewrote the acoustic bound this way.
- **C2. Retention of a coordinate and its release.** Batch O G6, batch N item 7, RAD-21 and TH-33 give it to ENGINE-ASSEMBLY-JOINTS; new identities and batch I split hold (JOINT-KINDS) from release (ASSEMBLY-JOINTS). *Governs:* ENGINE-JOINT-KINDS owns every hold on a coordinate and its release by physical request; ENGINE-ASSEMBLY-JOINTS' commanded release removes a connection or attachment. *Why:* 3a reduces ASSEMBLY-JOINTS to player-made connections and commanded release; a single-package part must not wait for player assembly; the physical-request mechanism already releases prismatic retention.
- **C3. Contacts driven by coordinate state.** Batch I G2 and new identities G3 put the revolute window and Blocked-phase contacts in JOINT-KINDS; batches N and K put endpoint, release-travel and passive-slider contacts in SIGNAL-NODES. *Governs:* SIGNAL-NODES owns every supply contact; JOINT-KINDS publishes the observations they read. *Why:* one contact family in one compiled network (coordinator decisions 16 and 25).
- **C4. The material mechanism.** Batch J's MECHANICAL-LINKS item (a) and batch I's CONNECTION-MATERIALS both define a material on a connection. *Governs:* the record is CONNECTION-MATERIALS'; coupling law kinds, pitch radius and engagement class are MECHANICAL-LINKS'; CONNECTION-MATERIALS lands first. *Why:* batch J itself resolved audit C3 toward batch I's mechanism; one record for every domain.
- **C5. Pressure and volume units.** EL-001 W13 and EL-032 say LIQUID, or GAS-NETWORK "if it lands first". *Governs:* GAS-NETWORK. *Why:* owner decision 18 puts it first; "whichever lands first" is not one owner.
- **C6. The nozzle and axial chamber.** Batch D's earlier draft named `engine-axial-gas-nozzle`. *Governs:* GAS-NETWORK. *Why:* engine audit sections 5 and 6 item 13; batches D and H now agree.
- **C7. Gas jets against reaction thrust.** Both proposals describe a jet on a body. *Governs:* gas-fed nozzles and thrust are GAS-NETWORK's; the reaction of any airflow source on its own body when nothing is in the cone is REACTION-THRUST's. *Why:* EL-090 and EL-092 state the nozzle law does not cover it; one boundary, stated in both stories.
- **C8. The credited store row.** Batch O G20 gives it to THERMAL and offers SUPPLY-DEMAND as the coordinator's alternative; the optical, radiation and thermal conventions disagreed (conventions re-audit H-3). *Governs:* THERMAL, for every filler (coordinator decision 26), as CAT-059, EL-154, EL-211, RAD-12, RAD-20 and TH-30 state; the receiver credit is OPTICAL's row and a thermal converter's credit THERMAL's. *Why:* decided.
- **C9. Fracture order.** The old text put ENGINE-FRACTURE after ENGINE-ASSEMBLY-JOINTS; batch G says after JOINT-KINDS. *Governs:* batch G: package-declared limits after JOINT-KINDS and CONTACT-OBSERVATIONS; player-made connections additionally after ASSEMBLY-JOINTS. *Why:* package joints do not need player assembly.
- **C10. Radiation order.** The old text put RADIATION after THERMAL (decay heat); batch O shows no radiation row needs thermal. *Governs:* batch O. *Why:* decay heat is a thermal inventory source, not a radiation emitter.
- **C11. Thermal before optical.** Owner decision 18 puts THERMAL early; its radiant-heating row reads an optical receiver. *Governs:* the row is delivered as an interface in THERMAL and proved end to end in OPTICAL. *Why:* keeps decision 18's order without a thermal story that cannot be tested.
- **C12. Characters.** The old ENGINE-CHARACTERS described scripted agents with a behaviour table. *Governs:* coordinator decision 16's reduction to package joints on a dynamic base. *Why:* the owner's instruction that nothing is scripted.
- **C13. ENGINE-ONE-WAY-INSTALL scope.** The audit asked for an owner decision. *Governs:* 3a's wording ("remove physics' wait for animation/renderer acknowledgement"), widened by C35 to the read stall, the animation port and the Presented wait. *Why:* the decision is made.
- **C14. Items listed as missing that exist.** The old ENGINE-ASSEMBLY-JOINTS listed a holding brake and an elastic hinge as missing; a supplied zero-speed drive holds and a constant hinge spring exists (batch I G4, batch J item 5). *Governs:* JOINT-KINDS states what exists and adds only the spring rest position and holds that hold without supply where a consumer needs them.
- **C15. Acoustic bounds.** EL-044 proposed a declared occurrence capacity with a typed refusal; batch D proposed D5 bounds. *Governs:* batch D's text (C1).
- **C16. Field-force scope.** The old text promised the Gravity identities but described only bounded regions acting on a responding property (batch M contradiction 1). *Governs:* the one uniform-field law with the scene region and Force/Acceleration quantities (batches L and M, coordinator decision 16).
- **C17. Field direction.** The unified text said "a constant vector"; EL-060's force points at its pole (batch I). *Governs:* coordinator decision 29: a field declares its direction as data, a constant vector or toward a declared point, in the one uniform-field law; EL-060 uses the point form.
- **C18. Loop tolerance.** Batch M's "closed loop holds within 0.01 m for 10 s" names no gameplay failure. *Governs:* the game-grade envelope (float rules).
- **C19. Cantilever tolerance.** Batch M's 0.02 m for 5 s is kept with its reason (the shared end-point tolerance, under socket clearance).
- **C20. Supply debit tolerance.** Batches J and L proposed [work, 1.01 × work]. *Governs:* never below the committed work and above it by no more than the finite-energy ledger's stated residual. *Why:* the 1 % had no gameplay justification; the ledger already states its residual.
- **C21. Acoustic tick order.** EL-044 L7 and batch D place acoustics before the substeps with excitation in tick t + 1; the optical and thermal steps run after the last substep. *Governs:* decided once in Story 14.0's Code Map and added to the one combined tick order (C30), not stated beside it; recommended the start-of-tick acoustic phase. *Why:* distinct phases are not a contradiction once stated in one order.
- **C22. EL-186's attachment read.** See section 2.
- **C23. CAT-009's repeating hit.** See section 2.
- **C24. Fan blades.** EL-176 called them a visible-solid defect; CAT-028 keeps them decorative. *Governs:* coordinator decision 25 (decorative); OPTICAL lists them as passing light.
- **C25. Crank coupling (EL-218).** New identities give it to MECHANICAL-LINKS with a fallback to a rod pinned by JOINT-KINDS' dynamic-support hinges; MECHANICAL-LINKS is force-bounded. *Governs:* coordinator decision 29: a rod pinned at both ends by JOINT-KINDS' dynamic-support hinges, with its own collider; no new coupling law.
- **C26. Buoyancy in air.** *Governs:* coordinator decision 28: one law, a body's declared ambient lift (a declared acceleration). A heated envelope's lift is that acceleration bound to its committed store temperature, a = A × (1 − T_ref ÷ T) with A = envelope volume × reference air density × |g| ÷ dry mass, clamped inside the 16 m/s² domain and acting from the next tick (TH-25 H3), owned by THERMAL with the non-linear clamped binding in PARAMETER-BOUND-FRAMES; FIELD-FORCE scales it once and bounds it only by the body's combined |gravity + load| ≤ 16 m/s² (TH-25 H4). The delivered Balloon keeps its declared lift (owner decision 24); buoyancy in liquid stays the displaced-volume law. *Withdrawn:* no store's lift is replaced by a displaced-density law; batch N's earlier proposal to do so, and the engine-story merge's recommendation of it, are void.
- **C27. Elastic span, winch, shaft-to-electrical, crank.** The old ASSEMBLY-JOINTS owned them. *Governs:* CONNECTION-MATERIALS (span stiffness), TENSILE-EXTENSIONS (winding), SUPPLY-DEMAND (shaft-paid supply), C25 (crank).
- **C28. Light-to-supply transfer.** Earlier drafts had a next-tick allowance and a store-less source. *Governs:* the store credit only (coordinator decisions 21 and 25).
- **C29. Documents against code.** ENGINE-CORE-2d promises convex hulls while `docs/gpu-f32-physics.md:63` refuses hull proxies; hollow colliders are documented static only while the solver admits dynamic hollow bodies. *Governs:* DOC-ENGINE-STATUS corrects the documents; a convex profile is ENGINE-PROFILE-COLLIDER.
- **C30. The five conventions against each other (conventions re-audit, repaired 23 November 2026).** The liquid, pneumatic, optical, thermal and radiation conventions disagreed on tick order, accounts, the gas–liquid edge, absorbed energy and bounds. *Governs:* coordinator decision 26, as repaired into the conventions and now into GAS-NETWORK, LIQUID, THERMAL, OPTICAL and RADIATION: one [combined tick order](../EL-176-red-laser-emitter.md#combined-tick-order) stated once in optical convention 4 (gas step per substep, before the liquid step; liquid step; grants; solve; one optical and radiation solve; thermal step; sensors and commit); every rate per dt; the gas–liquid edge is LIQUID's and debits the gas store, reading the gas pressure just committed; heat moves with each liquid transfer per substep; a gas store may hold an air–vapour mixture by Dalton's law (GAS-NETWORK); absorbed light or radiation is booked once on the absorbing collider's record, the store credit and THERMAL's absorptance share are taken from that record, and a reading credits nothing; one near-field rule and one own-body rule; candidate receivers come from a cone query; no scene-capacity refusal; the liquid ledger gains `Evaporated` and the thermal ledger its sink, source and work terms, boiling counted once; condensing has one wall-limited row. *Why:* each was a second rule or an unclosed ledger.
- **C31. Drive above its allowance.** Several specs assumed a supplied drive could do more work in a tick than its source grants. *Governs:* coordinator decision 27: SUPPLY-DEMAND limits it to its per-tick allowance (power limit × dt), scaling its torque or force so its work equals the energy granted; the specs re-derive their rows.
- **C32. Supply phase and full stores.** The delivered supply phase is 1/120 s at every cadence, which the conventions could not reconcile with per-dt rates; a heater on a store at its limit would be debited for heat the store refuses. *Governs:* coordinator decision 30: SUPPLY-DEMAND states a grant at any cadence as energy per tick = power limit × dt, and a consumer that can accept nothing (THERMAL's heat target on a store at its limit) requests nothing; a receiver that must read the same whatever way it faces declares one sample per face and reads their mean (RADIATION), with no second own-body rule.
- **C33. Remaining one-sentence boundaries (`docs/planning/elements/readiness/audits/reaudit-group-3.md` section 5 item 11).** The two-input boolean signal node is a row of SIGNAL-NODES; the N-input boolean supply node is CAPABILITY-INSTANCES' reshaping of the delivered two-input supply node. Revolute retention is JOINT-KINDS' authored-position retention (C2); ASSEMBLY-JOINTS owns only commanded release of a connection or attachment.
- **C34. The pose ring (ABI audit, owner decision 24k).** `ENGINE-TICK-ALLOCATION` claimed "the pose slot written into reused storage" while the ring has no reader and could not feed interpolation as laid out. *Governs:* `ENGINE-WIRE-ABI` owns the ring, its derived multi-buffered depth (4, 5 and 7 slots at 60, 120 and 240 Hz) and the renderer feed; `ENGINE-TICK-ALLOCATION` keeps the host read path, measured after 8a. *Why:* two stories must not both claim the ring.
- **C35. `ENGINE-ONE-WAY-INSTALL` scope (ABI audit G3, G4).** C13 held the story to install receipts. *Governs:* the story also stops a 500 ms read stall from faulting the run, makes the animation port latest-only and removes the Presented wait. *Why:* the same one-way rule (physics never waits for presentation) under owner decisions 3a and 24j; coordinator decision 40 (23 November 2026) records that the widening applies owner decision 3a's "physics commits alone".
- **C36. Enum sources (owner decisions 24j and 24k; ABI audit G10).** The audit offered schema enum types or integer fields. *Governs (as amended by owner decision 24p, coordinator ruling on S5c finding A1-7, 25 November 2026):* every closed set has exactly one declaration.
  - **Wire closed sets.** For a closed set that crosses the wire (`CommandKind`, `ResponseKind`, `SimulationPhase`, `CommandOutcome`, `ElectricalEnable`, `RunControlLane`, `RunControlValueKind` and `ObservationPayload`), that declaration is the schema enum. C# and TypeScript both take it from `flatc`.
  - **`ENGINE-WIRE-ABI`** makes the C# domain use the generated enum. Its planning decides whether Abstractions references the dependency-free FlatBuffers assembly or the domain duplicate is deleted. It also routes every remaining enum field through one refusal helper.
  - **Every other closed set.** Its declaration is the C# enum. TypeScript mirrors of it come only from `ENGINE-CAPABILITY-INSTANCES`' generator, which emits no wire enum.
  - **Until then,** S5c's interim test checks that each C# mirror of a wire enum equals its generated enum.
- **C37. Chain ratio (coordinator decision 24l).** EL-201 kept the coordinate ratio, so drawn teeth on unequal pitch radii did not mesh. *Governs:* a material declares its ratio source as data; the drive chain declares the pitch-radius ratio of its ports (`ENGINE-MECHANICAL-LINKS`).
- **C38. Instanced drawing (coordinator decision 40; ABI audit G1).** The audit left instanced batching without an owner, and `ENGINE-WIRE-ABI` took it as a conditional clause ("delivered here or recorded in `DOC` as open"). *Governs:* `ENGINE-INSTANCED-RENDERING` (order 8c, after 8a and 8b, depending on 8a) owns instanced draw batches on WebGL 2 and WebGPU, fed by the pose ring and keyed from compiled declarations; `ENGINE-WIRE-ABI` keeps the ring as the renderer's feed and no longer deletes per-body node transforms. *Why:* the wire story makes the feed; drawing from it is a separate renderer capability with its own gate (whether WebGPU is reachable with the pinned Godot export), and a conditional clause would have left a second draw path open.

## 5. Not reconciled here (outside this file's and the epics section's scope)

Reconciled on 23 November 2026 and no longer open: the roadmap's delivery order (`docs/planning/invest/vertical-delivery.md` now follows owner decisions 3a, 18, 22 and 24a: after the engines, elements in the campaign's first-teaching order), and the ledger's order paragraph with it (`docs/planning/invest/named-elements.md`, **Order**); `engine-supported-impact-restitution` (published at `a39014f4` with receipt `3df93f14`, both on published main; its remaining eight-point group cut is ENGINE-ISLANDS-SLEEP's) and `engine-cap-block` (published at `6ed1a3e0`), both corrected in `epics.md` and the roadmap, and the supported-impact entry in `deferred-work.md` marked resolved; the Chrome criteria and Depends-on lines of Stories 14.1 to 14.4; the five goal slices in `spec-engine-goal-evaluation.md`; the engine sprint keys in `sprint-status.yaml`; the requirement edits `sequence-task-398` (owner decision 9) and radiation-01's scope (readings flat inside 1 m, coordinator decision 21); lesson-fixture story S8, now defined in `docs/planning/invest/delivery-batches.md` section 5; and the element-spec re-pointing of the cross-batch pass (`docs/planning/elements/readiness/audits/cross-batch.md` findings 5-1 and 7-1): CAT-003, CAT-010, CAT-018, CAT-019, CAT-057, CAT-065, EL-054, EL-060, EL-061, EL-090, EL-186, EL-215, EL-218, EL-221, TH-22, TH-23 and TH-25 now name the owners above, and EL-224, EL-225, EL-226, EL-228, RAD-21 and TH-33 give the hold and its release to ENGINE-JOINT-KINDS (C2).

Still open, each in another owner's file:
- The dependent specs listed under "Dependent specs that need updating" in the repair section of `docs/planning/elements/readiness/audits/conventions-reaudit.md` (near-field, own-body, per-dt credits, work bounds, credited-store owner, radiant credit, crossing cap, scene refusals, gas–liquid edge owner, `GasIn`, gas step rate, units owner, capture region, overflow velocity, immersed mouths, condensing rule, evaporation account), plus the rows that coordinator decision 27 asks to re-derive.
