# EL-052 · Water-tuned bottle — element readiness spec

**Current delivery (24 October 2026):** [EL-052 delivery slice](../invest/named-elements.md#element-052). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md); the flow-excitation rule S1 and the airflow inlet in [EL-045](EL-045-air-whistle.md#behaviour-and-numbers); the liquid family conventions L1–L18 (marks of 2⁻⁴ m³, store interior, open aperture, stream credit, ledger, tolerances) are [EL-001's](EL-001-finite-reservoir.md#liquid-family-conventions-stated-once-here) and are cited, not restated; the thermal conventions are [TH-04 §11](TH-04-electrical-heating-plate.md#11-thermal-conventions-owned-here-every-thermal-spec-cites-this-section).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-052 · Water-tuned bottle |
| Type | Sound |
| Anchor | [requirements.md#element-052](../requirements.md#element-052); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-052); owner S539 |
| Disposition | **To deliver as a single element.** It refines no CAT part and duplicates nothing: it is the only part whose tone is set by a conserved fill. It is the last sound identity, because it needs two engine domains (acoustics and liquid). |
| Planned package | id `tuned_bottle`, directory `elements/TunedBottle/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | A flow-excited emitter like [EL-045](EL-045-air-whistle.md) whose tone follows a liquid store; filled from an [EL-003 Tap](EL-003-tap.md) over an [EL-001 reservoir](EL-001-finite-reservoir.md); heard by [EL-044](EL-044-tone-selective-sound-meter.md). It is not a resonator in the [EL-049](EL-049-acoustic-resonator.md) sense: it has no excitation store. Campaign: "water-tuned bottle follows chapter 7 water". |
| Roadmap story | [EL-052 delivery slice](../invest/named-elements.md#element-052) `named-el-052`. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception, occlusion and probe candidates from it, and `ENGINE-LIQUID` its stream candidates) and the threshold row of `ENGINE-SIGNAL-NODES` (the S1 on and off levels); then, in the roadmap order of owner decision 18, `ENGINE-LIQUID` (after `ENGINE-GAS-NETWORK`) and `ENGINE-THERMAL` (it supplies the store's thermal node, which no row reads); then Story 14.0, Stories 14.2, 14.1 and 14.3, EL-044 and EL-045. |

## 2. Declaration

Closest delivered shapes: the Balloon for a part that owns a finite fluid store (`IGasStoreElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:109`) and the Fan's airflow source for the stream its probe reads (`:40`); the liquid store itself has no delivered shape and follows whatever ENGINE-LIQUID defines for [EL-001](EL-001-finite-reservoir.md).

**Interfaces.** `IMechanicalElement` (`MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), the liquid store and open-aperture declarations of `ENGINE-LIQUID` (EL-001 L4 and L9; an open aperture is not a fluid port, so the part declares none), and the observation-probe and acoustic emitter declarations of Story 14.0 (N6, N1). It does **not** implement `IAirflowReceiverElement`: a receiver on body 0 of a static element is refused (`CuriousContraptions.Bundle/AirflowDomain.cs:33`–`:34`).

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | One static body. Bottle body: cylinder r 0.3 m, height 1.0 m, axis Y, at the origin *proposed*; neck: Tube r 0.1–0.125 m, height 0.3 m at (0, 0.65, 0), open at the top; base ring: cylinder r 0.34 m, height 0.06 m at (0, −0.53, 0). The collider is the solid outer shape; the liquid store is an inventory, not a rigid cavity. |
| Mass and material | Static (mass 0); the water's mass does not load anything. Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. |
| Constraints | none. |
| Liquid store | One store per [EL-001 L4](EL-001-finite-reservoir.md#liquid-family-conventions-stated-once-here): capacity 0.25 m³, four family marks of 2⁻⁴ m³ (L2); initial `initial_fill` marks; floor datum (0, −0.45, 0), interior height 1.25 m (floor to neck top) and equivalent plan area C ÷ H = 0.2 m² (L4's tapered-vessel rule), all *proposed*. Open aperture (L9): a disc r 0.1 m at the neck top, local (0, 0.8, 0), normal +Y, the top of the declared interior; a stream sample that ends inside the interior is credited to the store (L8; coordinator decision 16). Overflow point at the neck rim, (0.125, 0.8, 0) *proposed*: volume above capacity is pending overflow and leaves there as a stream (L9). No mouth and no drain. **Thermal node:** the store is also a thermal node with an authored starting temperature, here the ambient 288 K (owner decision 20; [TH-04 §11 T2](TH-04-electrical-heating-plate.md#11-thermal-conventions-owned-here-every-thermal-spec-cites-this-section)); no acceptance row of this part requires a thermal row. |
| Airflow probe | One observation-only probe (EL-044 N6; missing today) across the mouth, four weighted samples on a horizontal 0.1 m square centred at local (0, 0.85, 0), 0.05 m above the neck rim, each weight 0.25. It reads the committed flow speed of a declared airflow source at its unoccluded samples, its candidates found through `ENGINE-SPATIAL-QUERIES`; it takes no force and adds no demand. |
| Emitter | One emitter at the mouth (0, 0.85, 0): kind Sustained, pattern Omnidirectional, strength by rule S1 with the Airflow inlet's fixed values (on 4 m/s, off 3.6 m/s, full 12 m/s; the on and off state is the threshold row of `ENGINE-SIGNAL-NODES`). Tone selected by the committed fill (table below); in the Too full state the emitter is disabled. |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Parameters**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_fill` | Choice | None | 0 (Dry) | 0 | 4 | Reject; options `Dry`, `1 step`, `2 steps`, `3 steps`, `Full`, each one 0.0625 m³ step. A Choice, not a Real, so an authored fill is always a whole step. |

The tone is not a parameter: it is derived from the committed fill.

**Ports.** None. Water arrives as a stream credited inside the store's interior through its open aperture (EL-001 L8, L9); air arrives as open airflow. An open aperture is not a fluid port (L5).

**Presentation bindings.** Waterline `Visual/Water` scale Y ← committed fill fraction (new committed store driver from ENGINE-LIQUID; the gas analogue is `PresentationDriver.CommittedStoredFraction`, `CuriousContraptions.Abstractions/ElementPresentation.cs:33`); wavefronts while Sounding ← committed emitter state (N12).

**Palette.** Category `ElementCategory.Control`; palette order 430 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Singing bottle"; colour cyan `#66b8c9`. Description: "Blow air across its neck to make it sing. The water level, not a switch, sets the note: read the bars beside the waterline. Too full, and it will not sing."

**Fill-to-tone mapping** (outcome: "Changing fill changes supported tone; dry and filled cases remain distinct"). More water → smaller air cavity → higher pitch, as in a real bottle; one step per mark keeps the marks readable and conserved.

| State | Committed fill V | Air cavity | Printed in that region of the glass | Tone when excited |
| --- | --- | --- | --- | --- |
| Dry | V < 1 step | > 0.1875 m³ | one bar | Low |
| One step | 1 step ≤ V < 2 steps | 0.1875–0.125 m³ | two bars | Mid |
| Two steps | 2 steps ≤ V < 3 steps | 0.125–0.0625 m³ | three bars | High |
| Too full | V ≥ 3 steps | ≤ 0.0625 m³ | a cross | silent (cavity too small) |

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Glass` | cylinder r 0.3 × 1.0 m, translucent cyan `#66b8c9` | solid, cylinder collider (transparent art, opaque to balls and to sound) |
| `Neck` | tube r 0.125 m, height 0.3 m, translucent cyan | hollow, Tube collider (bore 0.2 m; what passes it is under Consistency) |
| `BaseRing` | cylinder r 0.34 × 0.06 m, navy `#293954` | solid, cylinder collider |
| `Water` | blue column inside the glass, height by a binding | decorative: enclosed, moved by a binding |
| `FillMarks` | three cream `#fff8e9` boundary lines at the 1-, 2- and 3-step levels; in the four regions they bound, from the bottom: one navy bar, two bars, three bars, then a navy cross; all flush on the glass. The region the waterline is in shows the tone, with the same bars every sound part uses (one Low, two Mid, three High) | decorative: printed flat |
| Wavefronts | translucent rings | decorative: glow |

Icon: a bottle with a waterline and a ring above its neck. **At a glance** it differs from the Resonator jar (opaque, wide mouth, glowing top) by being a clear bottle with a narrow neck and a visible waterline.

### Behaviour and numbers

- **Laws.** S1 ([EL-045](EL-045-air-whistle.md#behaviour-and-numbers)) for excitation; L1 (Sustained, Omnidirectional), L2–L4, L6, L7 for the sound; the liquid family's stream credit, open aperture, overflow and ledger (EL-001 L8–L10) for the fill.
- **Tone changes while sounding.** When the committed fill crosses a mark boundary the emitter stops its occurrence and starts a new one in the new tone on the same tick, so a tuned meter for the old band drops out and the one for the new band triggers: the "fill to sing" recipe.
- **Tap placement (rows 2–3).** A default EL-001 reservoir (plan area 1.0 m², full depth 1.0 m, head equal to depth) with its Tap at `opening` 1, the Tap outlet 0.05 m above the neck rim and 0.25 m beside the neck axis. At full head the stream leaves horizontally at 2.56 m/s ([EL-003](EL-003-tap.md)) and falls 0.05 m in √(2 × 0.05 / 9.81) = 0.10 s, travelling 2.56 × 0.10 = 0.26 m, so its centre lands on the axis at the start. After row 3's 0.3125 m³ the reservoir depth is 0.69 m, the exit speed scales by √0.69 = 0.83 and the landing point moves 0.26 × (1 − 0.83) = 0.044 m: under 0.05 m and inside the 0.1 m bore radius less the 0.039 m stream radius (√0.6 × 0.05 m, EL-001 L8), 0.061 m. Without a stated placement the landing point could cross the rim and the fill would count as `Spilled`. The slice re-derives the offset from the EL-003 law.
- **Capacity 0.25 m³ in four marks** so each engraved boundary is exactly one family mark (EL-001 L2). Fill time is not a feature and is asserted only by its stall guard: a fully open Tap over a full reservoir passes about 0.048 m³/s ([EL-003](EL-003-tap.md)), so one mark takes under 2 s and row 3's 0.3125 m³ about 7 s, inside the default 30 s Run.
- **Too full is silent** so that "keep filling" is a failure the player can see and hear, and so three regions map to three bands with no fourth tone to invent.
- **Conservation.** The liquid ledger (EL-001 L10) holds at every committed tick, and a stream is credited in the step it is poured (L8), so there is no liquid in flight. This part's row asserts it at rest (tap closed): reservoir loss = bottle gain + `Spilled`, within 0.004 m³ (L17). The bottle never creates or destroys water.
- **Tick order.** Liquid transfer commits its fill; the Phase 2 acoustic pass reads the committed fill for the tone and the committed airflow exposure for the strength; meters respond per D7.
- **Consistency.** The bore (0.2 m) admits a tap stream. A Tennis ball set below radius 0.1 m (its `radius` goes down to 1/16 m, `elements/TennisBall/TennisBallElementDeclaration.cs:18`) drops in, rests on the glass top at y 0.5 and, by the general laws, shadows stream samples (EL-001 L8) and displaces liquid (L12) once the level reaches it (surface y 0.5 at V = 0.2 m² × 0.95 m = 0.19 m³, in the Too full region). A Fan across the neck at 12 m/s with all four samples exposed gives full strength; a default meter then hears it to 6.1 m.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S539): AcousticPropagation, AerodynamicDrag, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, RigidBodyDynamics, SignalPropagation, TopologyTransaction (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with cylinder and tube colliders (C1, C10) | present | EL-044 C1, C10 |
| Conical airflow with per-sample occlusion (C8) | present | `AirflowGeneralityTests.OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:138`) |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| Conserved liquid store with a declared interior, its committed level observation, stream credit inside the interior through an open aperture, overflow and spill | missing | `ENGINE-LIQUID` (EL-001 L4, L8–L10, L13; stream candidates from `ENGINE-SPATIAL-QUERIES`, L18) |
| The store as a thermal node with an authored starting temperature (owner decision 20) | missing | `ENGINE-THERMAL` (TH-04 §11 T2); no row of this part needs it |
| Emission, propagation, tone, sustained emission (N1–N3) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Emission excited by a committed scalar observation, and the observation-only airflow probe on a static body (N6) | missing | Story 14.0, gap 5 (probe and strength map; probe candidates from `ENGINE-SPATIAL-QUERIES`); the on and off levels are the threshold row of `ENGINE-SIGNAL-NODES`. The delivered `AirflowReceiverDescriptor` is refused on a static body and adds demand (EL-044 C8) |
| Tone or disabled state selected by a committed scalar observation through a declared table (N11) | missing | Story 14.0, gap 10; the level observation itself is ENGINE-LIQUID's |
| Committed fill and emitter-state drivers (N12) | missing | Story 14.0, gap 11; the fill driver from ENGINE-LIQUID |

Counts: 3 present, 0 untested, 0 narrower, 6 missing. Nothing here needs element-specific engine code: the mapping is a declared table from an observed level to a band.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`, the threshold row of `ENGINE-SIGNAL-NODES`); Story 14.0 and `ENGINE-LIQUID`; a water source (EL-001 + EL-003); a Fan (delivered) or Bellows; EL-044 to prove the tone; EL-045 (it defines and first proves S1).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** One liquid store with an open aperture over its declared interior (also a thermal node), one observation probe, one sustained emitter, the S1 excitation values and a four-row table from store level to tone or disabled. The engine evaluates such a table for any emitter that declares one; the package has no update loop and no engine code may branch on `tuned_bottle`.
2. **Generic laws.** Stream credit inside a declared interior, overflow and the ledger (`ENGINE-LIQUID`, EL-001 L8–L10); the delivered conical airflow law read by a probe (N6); excitation by a committed scalar observation (N6), its on and off state the threshold row of `ENGINE-SIGNAL-NODES`; tone selection by a committed scalar observation (N11); acoustic laws L1–L6. "More water, higher note" is the declared table, not a rule about bottles.
3. **Three layers.** *Colliders:* body cylinder, neck Tube and base-ring cylinder on one static body, static default material. *Visuals:* the art table above; the glass is transparent art over an opaque collider, and no art mesh is a collider. *Animation:* read-only bindings. Moved by a binding: `Water` (height from committed fill), wavefronts. Moved by nothing: `Glass`, `Neck`, `BaseRing`, `FillMarks`.
4. **One-way flow and typing.** Committed fill and airflow → emitter → receivers → presentation; nothing returns. `initial_fill` is a closed Choice and the tone is `ToneBand`; volumes and levels are f32 with volume compared in whole steps.
5. **Flat state.** 1 static body; 3 colliders (2 cylinder, 1 tube); 0 joints; 0 drives; 0 sensors; domain nodes: 1 airflow probe of 4 samples, 1 acoustic emitter, 1 threshold signal node (S1 on and off), 1 thermal node (the store's), 0 electrical nodes; stores: 1 liquid store with 1 open aperture and 1 overflow point; 2 presentation bindings.
6. **Performance fitness.** 3 colliders against the Fan's 2 (`elements/Fan/fan.tscn`): the base ring is a proud solid. Static body; the water's mass loads no body, and the part joins no island. The probe, the interior and the aperture are bounded regions whose candidates come from `ENGINE-SPATIAL-QUERIES` (probe occluders here; stream candidates under EL-001 L18). Occurrences follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6; liquid work per substep is bounded by compiled counts as EL-001 L18 states, with its capacities `ENGINE-LIQUID`'s to state.
7. **Scenarios.** Rows 1–6 run the real simulation; none depends on a code path that names this package.

## 4. Sources and legacy

- Requirement row [element-052](../requirements.md#element-052): "Conserved fill volume alters a resonant cavity's tone"; outcome "Changing fill changes supported tone; dry and filled cases remain distinct". Scope index [todo-352](../requirements.md#todo-352): water-tuned bottles keep their individual contract; campaign: "water-tuned bottle follows chapter 7 water" ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- Named entry [element-052](../invest/named-elements.md#element-052), owner S539; component research: "Water store + resonator with level-selected band | fill marks | Fill level selects a marked band; airflow across the mouth excites it | Waterline ← volume"; recipe "Fill to Sing: tap fills bottle to its mid-band; airflow excites it; tuned meter closes the tap and opens a hatch" ([component research](../../component-research.md#sound)).
- Legacy: no bottle, liquid store or tuned resonance exists at a6c914e; tone bands and presentation frequencies are A1 and A11 (`engine/AcousticAudio.cs@a6c914e:L14-L14`).
- **Files harvested:** acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-052](../requirements.md#element-052), [acoustic profile](../invest/profiles.md#acoustic). Planned test class `TunedBottleTests`. Volume and ledger use the liquid family's tolerance, 0.004 m³ ([EL-001 L17](EL-001-finite-reservoir.md#liquid-family-conventions-stated-once-here)); levels use the [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards); the fill stall guard is L17's. Fills use a Tap at `opening` 1, so every row runs inside the default 30 s Run.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given a powered Fan across the neck and three supplied tuned meters (Low, Mid, High) 3 m away, each wired to its own lamp, when `initial_fill` is Dry, 1 step or 2 steps, then only the Low, Mid or High lamp lights respectively (requirement outcome: dry and filled cases remain distinct). | `TunedBottleTests.TheFillLevelSelectsTheTone` | Fan off: no lamp at any fill | each of the three states |
| 2 | Given a Dry bottle under a Tap fed by a reservoir, placed as "Tap placement" states, with the Fan running, when the tap fills one step, then the Low lamp goes out and the Mid lamp lights; at two steps the High lamp; at three steps (Too full) all are out (requirement outcome: changing fill changes tone). | `TunedBottleTests.FillingChangesTheToneAndOverFillSilencesIt` | tap closed: stays Low | the emitter's committed tone (acoustic read model, EL-044 N13) is the lower band at 0.004 m³ below each mark and the higher band at 0.004 m³ above; the lamps follow within the 0.25 s travel time (L2) and one electrical solve |
| 3 | Given the same run and placement, then once the tap has closed and the run is at rest, reservoir loss = bottle fill + `Spilled` within 0.004 m³ (EL-001 L17), and beyond 0.25 m³ the excess is in `Spilled`. | `TunedBottleTests.WaterIsConservedThroughFillAndSpill` | — | exactly Full, then one more mark |
| 4 | Given a Wall between Fan and neck, or the Fan beyond reach, then there is no emission occurrence at any fill; with one of four samples exposed it is silent, with two it sounds at strength 0.5. | `TunedBottleTests.OnlyRealAirflowExcitesIt` | row 1 | 3 m/s (one sample, silent) and 6 m/s (two samples, strength 0.5): the 12 m/s Fan over four samples gives only 0, 3, 6, 9 and 12 m/s |
| 5 | Reset restores `initial_fill`, the reservoir level and silence; save then load round-trips `initial_fill` and pose; the run-time fill is not saved. | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` plus `TunedBottleTests.ResetRestoresTheAuthoredFill` | — | Reset while Too full |
| 6 | `initial_fill` outside 0–4 is refused atomically with a typed reason; the part has no ports, so any link to it is refused. | `TunedBottleTests.AnUndefinedFillIsRefused` | 0 and 4 accepted | −1 and 5 |

**Chrome recipe (pending, post-catalogue).** Reservoir + Tap above the bottle mouth; Fan aimed across the neck; three Tuned sound meters (Low, Mid, High) nearby, each supplied and wired to its own lamp.

## 6. Open questions

**Resolved.**

1. **Fill-to-tone mapping.** The step table above, with a silent Too full state: [D1](../invest/delivery-batches.md#3-family-level-decisions) and [D19](../invest/delivery-batches.md#3-family-level-decisions).
2. **Excitation.** Airflow only; no strike: D19 (no requirement row or lesson strikes a bottle; the Bell is the struck part).
3. **Water units.** 16 kg/m³ and the 2⁻⁴ m³ step: [D2](../invest/delivery-batches.md#3-family-level-decisions) and D4; the bottle uses the one liquid inventory.
4. **Fill parameter type.** A five-option Choice instead of an integer Real: decided under the standing delegation so an authored fill can never be a fraction of a step.
5. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) Nothing declares an airflow receiver on a static body; the flow observer is the missing observation-only probe with one owner, Story 14.0 (as [EL-045](EL-045-air-whistle.md)). (b) The liquid-level observation and stream credit inside the store's interior are owned by `ENGINE-LIQUID`; Story 14.0 owns only tone selection by a generic committed observation. (c) The glass prints bars in the regions (one, two, three, then a cross) and the silent state is named Too full: the earlier marks put one bar where the tone is Mid. (d) Conservation is asserted at rest by the liquid ledger (EL-001 L10); a stream is credited in the step it is poured (L8), so there is no in-flight term (Batch F hand-back, `readiness/batch-f.md`). (e) Coverage is bound by the published section form and is blocked on S7 (section 7). (f) Palette order 430 (coordinator decision 36).
6. **Propagation repair, 22 November 2026.** (a) Batch F hand-back applied: no liquid-in-flight term; the volume disqualifier names the family mark (EL-001 L2); the store has an open aperture over a declared interior and is credited by sampled stream paths inside it (coordinator decision 16), with no fluid port. (b) The store is also a thermal node with an authored starting temperature (owner decision 20; TH-04 §11 T2); no acceptance row needs a thermal row. (c) Order: the engine programme, then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` (owner decision 18), then Story 14.0; this part's rows need Story 14.0 and `ENGINE-LIQUID`, and its store's thermal node comes from `ENGINE-THERMAL`.

**Needs the owner.** None.

## 7. Integration

- **Works with:** Fan (delivered); EL-001, EL-003 (water family, planned after ENGINE-LIQUID); EL-044 (planned); EL-045 (planned before it).
- **Campaign:** named in chapter 8's sound row but "follows chapter 7 water"; reuse 81–100, 117–125, 136–150. [sequence-task-407](../requirements.md#sequence-task-407); water task [sequence-task-384](../requirements.md#sequence-task-384), which supplies the fill; [todo-378](../requirements.md#todo-378) "The Mill's Song"; processes [IX-12](../requirements.md#interaction-12), [IX-08](../requirements.md#interaction-08), [IX-11](../requirements.md#interaction-11).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) an acceptance pair of its own, since its ledger entry links only the umbrella `todo-352`, which has no co-anchored `sequence-task-NNN` in `requirements.md` and is shared by nine identities (the reader refuses two identities on one task source); (2) a decision anchor, since its ledger owner S539 has no `<a id="s539">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies both; it is not written earlier, because the reader throws on a malformed block. Mode records: one per `initial_fill` option.
- **Lesson fixtures:** needed where a level pre-places a part-filled bottle. Such a level waits for [S8, lesson fixtures for any admitted package](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element, data-only, last of the sound identities.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception, occlusion and probe candidates from it, and `ENGINE-LIQUID` its stream candidates) and the threshold row of `ENGINE-SIGNAL-NODES`; then, in the roadmap order of owner decision 18, `ENGINE-LIQUID` (after `ENGINE-GAS-NETWORK`) and `ENGINE-THERMAL` (it supplies the store's thermal node; no row reads it); then Story 14.0 amended with gaps 1, 2, 5, 10 and 11; EL-001 and EL-003; Stories 14.2, 14.1 and 14.3; EL-044; EL-045 (Airflow inlet); S7.
- **Files.** Adds `elements/TunedBottle/` (`TunedBottle.csproj`, `TunedBottleElementDeclaration.cs`, `tuned_bottle.tscn`, `icon.svg`) and `CuriousContraptions.tests/TunedBottleTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only.
- **Disqualified if** the probe finds the liquid store cannot be read by an emitter's tone selection, that an open aperture and its interior cannot sit on a body with a solid collider, or that the liquid family's volume mark ([EL-001 L2](EL-001-finite-reservoir.md#liquid-family-conventions-stated-once-here)) is no longer 2⁻⁴ m³ (the capacity and marks are then re-derived at planning).
