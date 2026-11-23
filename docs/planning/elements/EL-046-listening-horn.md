# EL-046 · Listening horn — element readiness spec

**Current delivery (24 October 2026):** [EL-046 delivery slice](../invest/named-elements.md#element-046). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md). This spec defines the **acoustic route rules R1–R6** that [EL-047 Exit horn](EL-047-exit-horn.md) and [EL-048 Acoustic duct](EL-048-acoustic-duct.md) reuse.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-046 · Listening horn |
| Type | Sound |
| Anchor | [requirements.md#element-046](../requirements.md#element-046); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-046); owner S540 |
| Disposition | **To deliver as a single element.** It refines no CAT part. It is not a family with the Exit horn: the two share a body but one receives and the other emits, which is a behaviour difference, not a declared value. |
| Planned package | id `listening_horn`, directory `elements/ListeningHorn/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | Route: listening horn → [EL-048 duct](EL-048-acoustic-duct.md) → [EL-047 exit horn](EL-047-exit-horn.md). Sources: [CAT-061 Speaker](CAT-061-speaker.md), [EL-179](EL-179-continuous-tone-speaker.md), [CAT-009 Bell](CAT-009-bell.md), [EL-045](EL-045-air-whistle.md). **Ordering:** this is the first of the three route slices; the full-route acceptance row (6) runs in the EL-047 slice, which is planned after it. |
| Roadmap story | [EL-046 delivery slice](../invest/named-elements.md#element-046) `named-el-046`. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Stories 14.0, 14.2, 14.1 and 14.3. |

## 2. Declaration

Closest delivered shape: the Funnel for a static hollow tapered body (`elements/Funnel/FunnelElementDeclaration.cs:13`); no delivered part has an acoustic port.

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the acoustic aperture interface Story 14.0 adds (N7).

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | One static body. Horn: hollow Frustum, axis +X, length 0.8 m, throat radius 0.1 m at local x = −0.3, mouth radius 0.4 m at x = +0.5, wall 0.03 m, centred on y = 0.1 *proposed* (Speaker-scale; the cabinet is 1.25 × 1.5 × 1.25 m). Throat collar: Tube r 0.1–0.13 m, length 0.1 m at x = −0.35. Foot box 0.9 × 0.14 × 0.7 m at (0, −0.37, 0). One socket sphere r 0.075 m at the throat. |
| Mass and material | Static (mass 0). Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Opaque to sound (transmission 0), so a horn shadows what is behind it; its own colliders are exempt for its own aperture (L6). |
| Constraints | none. |
| Aperture | One capture aperture at the mouth centre, local (0.5, 0.1, 0), axis +X, acceptance half-angle 35° (rule R1), coupling `coupling`. Passive: no supply. |
| Stores | none; passive coupling cannot add energy (R2). |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Parameters**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `coupling` | Real f32 | None | 0.8 | 0.1 | 0.9 | Clamp; BuildOnly. Below 1 so no route can amplify. |

**Ports**

| `PortId` | Role | Domain | Direction | Position (m) |
| --- | --- | --- | --- | --- |
| `SoundOut` (new id) | `SoundOut` (new role) | Audio | Output | (−0.4, 0.1, 0), facing −X |

`SimulationDomain.Audio` exists as an enum member but ports in it are refused today (EL-044 C11); Story 14.0 admits the domain and adds the ids and roles (N7). The matching input is `SoundIn` (Audio, Input) on [EL-047](EL-047-exit-horn.md), joined by an [EL-048](EL-048-acoustic-duct.md) duct.

**Presentation bindings.** A gold dot `Visual/Pulse` moves from mouth to throat ← the capture event cue (`PresentationDriver.Event`, `CuriousContraptions.Abstractions/ElementPresentation.cs:21`, on the acoustic event kind of N12); wavefronts shrink into the mouth on the same cue.

**Palette.** Category `ElementCategory.Control`; palette order 432 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Listening horn"; colour cream `#fff8e9`. Description: "Catches sound that reaches its open mouth and sends it down a connected duct. Sound from the side, behind or behind a wall is not caught."

**Variants.** None; the base declaration is the only required mode.

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Bell` | flared cone, throat r 0.1 → mouth r 0.4 m, length 0.8 m, cream `#fff8e9` | hollow, Frustum collider (a ball smaller than the mouth can roll in and stops where the bore narrows) |
| `Rim` | gold `#e8b764` band on the mouth edge, flush with the bell wall | decorative: printed flat |
| `Collar` | navy `#293954` tube r 0.13 × 0.1 m at the throat | hollow, Tube collider |
| `Foot` | box 0.9 × 0.14 × 0.7 m, navy | solid, box collider |
| `Pulse` dot, wavefronts | gold dot, translucent rings | decorative: glow, moved by a binding |
| Port sphere | r 0.075 m, gold `#f7cb52` | solid, sphere collider |

Icon: a horn in profile with two arcs entering the mouth. **At a glance** it differs from the Exit horn by its plain bell (the Exit horn has three raised ribs round the bell) and from the Funnel by lying on its side on a foot.

### Acoustic route rules (shared by EL-046, EL-047, EL-048)

All *proposed* starting rules (the legacy has direct paths only, A6). They apply to pulse and sustained occurrences alike.

| # | Rule |
| --- | --- |
| R1 | **Capture.** For each admitted arrival (L2, L3) at the aperture point that lies within the 35° acceptance cone of the mouth axis and has an unoccluded straight path (L6), the horn passes one routed occurrence to `SoundOut` carrying the source's tone and strength = arrival level × `coupling`. Wrong-facing or occluded arrivals capture nothing. If several arrive together, only the strongest is captured (L4). |
| R2 | **No gain.** Every coupling and loss factor is below 1, so a routed occurrence is always weaker than the arrival that fed it. |
| R3 | **Transit.** A duct of length L delays a routed occurrence by L / 12 s and multiplies its strength by e^(−0.05·L) (5 % loss per metre: an 8 m duct keeps about two thirds). |
| R4 | **Release.** An exit horn re-emits each routed occurrence that reaches it as a cone emission from its mouth along its axis, strength × its `coupling`, tone unchanged. |
| R5 | **Loops.** Single traversal per duct by lineage: a routed occurrence carries the ducts it and its ancestors have passed and never enters one of them again. Relay depth 4 bounds chains of distinct ducts (component research: "bounded lifetime and relay depth"). A later independent emission from the same source routes again. |
| R6 | **Typed mouths only.** Only a `SoundOut` → `SoundIn` duct carries routed sound; an unconnected port carries nothing, and ball pipes or hoses never carry sound. A port may carry several ducts (delivered connection validation refuses only an identical duplicate link, `engine/gpu/WorkshopConnections.cs:132`–`:137`); each duct carries the occurrence at its own strength and receivers still take the strongest arrival (L4), so a split never raises any level. |

### Behaviour and numbers

- **Acceptance cone 35°** mirrors the Speaker's emission cone (A2), so a facing pair couples and a side-on one does not: a player can read both cones the same way.
- **`coupling` 0.8.** A speaker 2 m in front arrives at 1/(1 + 0.08·4) = 0.758, so 0.606 enters the duct. Through a 4 m duct (× 0.819) and an exit horn (× 0.8) the release strength is 0.397; a meter 2 m from the exit reads 0.397/1.32 = 0.301, above the default 0.25 threshold. With `coupling` 0.5 on both horns the same route gives 0.118: below the default, above the most sensitive 0.05. So the default route works at bench scale and the setting matters.
- **No gain, stated precisely.** R2 is per stage: the strength entering the duct (0.606) is below the level arriving at the mouth (0.758), and the release strength (0.397) is below that. A meter listening to the release therefore always reads less than the same meter would read at the listening horn's mouth. A duct does carry sound further than open air would (a direct 8 m path reads 0.163): that is the part's purpose, a guided path losing 5 % per metre instead of the free-air square law, and it creates nothing.
- **Tick order.** Capture is part of the Phase 2 acoustic pass; D7 applies to whatever finally hears the release.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S540): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with Frustum and Tube colliders (C1, C10) | present | EL-044 C1, C10; the Funnel is the delivered hollow frustum |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| An acoustic port domain (C11) | narrower | `SimulationDomain.Audio` is an enum member whose ports are refused (`CuriousContraptions.tests/BundlePortsTests.cs:180`); owned by N7 |
| Event cue presentation (C12) | narrower | no acoustic event kind; owned by N12 |
| Emission, propagation, occlusion, tone (N1, N2) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Aperture capture into a typed port, routed occurrences, relay bound (N7) | missing | Story 14.0, gap 6 |
| Acoustic event kind and cue (N12) | missing | Story 14.0, gap 11 |
| A committed acoustic read model exposing the routed occurrence at `SoundOut` (N13) | missing | Story 14.0, gap 14; rows 1–5 observe through it |

Counts: 2 present, 0 untested, 2 narrower, 4 missing. Nothing here needs element-specific engine code.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`); Story 14.0; a sound source (Speaker with Battery, or Bell); EL-048 and EL-047 to complete a route; a Sound meter to observe.

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** One capture aperture (point, axis, acceptance half-angle, coupling) and one Audio output port. Rules R1–R6 are engine route laws over every declared aperture, route and outlet; the package has no handler for "sound arrives" and no engine code may branch on `listening_horn`.
2. **Generic laws.** Arrival: L2, L3, L6 at the aperture point. Capture: the acceptance-cone test and the declared coupling factor (R1, R2). Combination: strongest arrival (L4). `coupling` is the only parameter.
3. **Three layers.** *Colliders:* bell Frustum, collar Tube, foot box and one socket sphere on one static body, static default material. *Visuals:* the art table above; no art mesh is a collider. *Animation:* read-only bindings on the capture event cue. Moved by a binding: `Pulse` dot, wavefronts. Moved by nothing: `Bell`, `Rim`, `Collar`, `Foot`, port sphere.
4. **One-way flow and typing.** Emission → aperture → routed occurrence → presentation; nothing returns. Ports are typed `PortId`/`PortRole` in the Audio domain; values are f32.
5. **Flat state.** 1 static body; 4 colliders (1 frustum, 1 tube, 1 box, 1 sphere); 0 joints; 0 drives; 0 sensors; domain nodes: 1 acoustic aperture, 0 signal or electrical nodes; 1 Audio port; 0 stores; 2 presentation bindings.
6. **Performance fitness.** 4 colliders against the Funnel's 5 (`elements/Funnel/funnel.tscn`). Static body; it joins no island. The aperture is one point with a cone test, bounded by each emitter's range; its arrival and occlusion candidates come from `ENGINE-SPATIAL-QUERIES`. Live and routed occurrences follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6 (tables sized at Run from declarations, no scene ceiling); R5 bounds each lineage.
7. **Scenarios.** Rows 1–8 run the real simulation; rows 1–5 read the committed read model, a defined interface, not a package path.

## 4. Sources and legacy

- Requirement row [element-046](../requirements.md#element-046): "Finite aperture couples incident acoustic energy into a duct"; outcome "Wrong-facing or occluded arrivals lose coupling". Scope index [todo-352](../requirements.md#todo-352): "connected horn/ducts carry bounded sound".
- Named entry [element-046](../invest/named-elements.md#element-046), owner S540. Component research: "Listening horn / Exit horn / Acoustic duct (EL-046–048) | Acoustic network nodes | attenuation, delay | Collects sound into typed ports and carries it to an exit around a blocking wall | Pulse travel ← committed transit"; recipe "Quiet Corridor: horn → duct elbow → exit horn routes around a sound-blocking wall".
- Legacy: no horn, duct or routed sound exists; legacy reception is direct-path only with opaque blocking (A6, `engine/Acoustics.cs@a6c914e:L49-L79`). The speaker's 35° cone (`engine/Acoustics.cs@a6c914e:L17-L17`) is reused for acceptance.
- **Files harvested:** `parts/SpeakerPart.cs` (scale reference); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-046](../requirements.md#element-046), [acoustic profile](../invest/profiles.md#acoustic). Planned test class `ListeningHornTests`. Capture is observed as the committed routed occurrence at the horn's `SoundOut` through the committed acoustic read model (EL-044 N13, a Story 14.0 deliverable; no "observation port" exists or is proposed), so rows 1–5 need no exit horn. Tolerances and stall guard: [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given a supplied Speaker 2 m in front of the mouth, facing it, when it pulses, then one routed occurrence of the speaker's tone appears at `SoundOut` with strength 0.758 × 0.8 = 0.606 within T-level. | `ListeningHornTests.AFacingSourceIsCapturedOncePerEmission` | speaker not triggered: nothing at `SoundOut` | `coupling` 0.1 and 0.9 |
| 2 | Given the horn turned 90° or 180°, then nothing is captured (requirement outcome: wrong-facing loses coupling). | `ListeningHornTests.AWrongFacingHornCapturesNothing` | row 1 | source 33° and 37° off the mouth axis (captured, not captured) |
| 3 | Given a Wall between speaker and mouth, then nothing is captured (requirement outcome: occluded loses coupling). | `ListeningHornTests.AnOccludedArrivalCapturesNothing` | row 1 | Wall moved until the path just clears |
| 4 | Given a sustained source, then one routed occurrence spans the whole emission; given two simultaneous sources, then only the stronger is captured. | `ListeningHornTests.SustainedAndSimultaneousArrivalsFollowTheSharedLaws` | — | — |
| 5 | Given a horn with no duct, when sound is captured, then no part hears anything from it (R6). | `ListeningHornTests.AnUnconnectedHornCarriesNothingOnward` | row 6 | — |
| 6 | Full route, run in the EL-047 slice: Speaker → listening horn → 4 m duct → exit horn → meter behind a Wall reads 0.301 within T-level, and less than the 0.758 arriving at the listening horn's mouth (R2). | `ExitHornTests.TheRouteCarriesSoundRoundAWall` | duct removed | see EL-047 |
| 7 | Reset clears captured and in-transit occurrences; save then load round-trips `coupling`, pose and the duct connection. | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` plus `ListeningHornTests.ResetClearsCapturedSound` | — | Reset mid-capture |
| 8 | `coupling` below 0.1, above 0.9 or non-finite is refused atomically with a typed reason; `SoundOut` linked to an Electrical, Signal or Fluid input, or to another `SoundOut`, is refused. | `ListeningHornTests.OutOfRangeCouplingAndForeignLinksAreRefused`; generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` once the Audio domain is admitted | limits accepted | first f32 beyond each limit |

**Chrome recipe (pending, post-catalogue).** Speaker (Battery, Switch) facing a Listening horn 2 m away; a Wall blocks the direct line to a supplied Sound meter; Connect horn `SoundOut` → duct → Exit horn `SoundIn`; exit horn aimed at the meter.

## 6. Open questions

**Resolved.**

1. **Routing model.** R1–R6 are adopted as starting rules: [D1](../invest/delivery-batches.md#3-family-level-decisions) and [D16](../invest/delivery-batches.md#3-family-level-decisions) (the former S529 question is decided by the delivering engine slice, which records any change for all three route specs).
2. **Aperture size.** Only `coupling` scales capture; the mouth radius is art and collider: [D13](../invest/delivery-batches.md#3-family-level-decisions).
3. **Arrival combination.** Strongest arrival only: EL-044 L4.
4. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) Route observation is the Story 14.0 read model (N13), used by EL-046 and EL-048 alike: it lets the first two route slices prove their rows before the exit horn exists. (b) R5 is single traversal per duct by lineage, with depth 4 bounding chains of distinct ducts: the earlier wording did not say what "a route it has already traversed" meant. (c) A port may carry several ducts: no delivered rule limits links per port, and strongest-arrival keeps a split from adding level. (d) Coverage is bound by the published section form and is blocked on S7 (section 7). (e) Palette order 432 (coordinator decision 36).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Battery, Impact switch, Wall (delivered); Speaker, Bell, Sound meter (planned before it); EL-048 and EL-047 (planned after it; row 6 waits for them).
- **Campaign:** chapter 8, levels 71–80 ("listening horn"), first taught at **79a**, after EL-044 and EL-179 (76a) and EL-049 to EL-051 (78a), as its sound order states; the Acoustic duct and Exit horn follow it in the same level (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); reuse 81–100, 117–125, 136–150. [sequence-task-407](../requirements.md#sequence-task-407); process [IX-12](../requirements.md#interaction-12).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) an acceptance pair of its own, since its ledger entry links only the umbrella `todo-352`, which has no co-anchored `sequence-task-NNN` in `requirements.md` and is shared by nine identities (the reader refuses two identities on one task source); (2) a decision anchor, since its ledger owner S540 has no `<a id="s540">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies both; it is not written earlier, because the reader throws on a malformed block. Mode records: `Configuration=Fixed`.
- **Lesson fixtures (S8):** needed only where a level pre-places it.

## 8. Delivery

- **Slice shape.** Single element, data-only.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Story 14.0 amended with gaps 6, 11 and 14; Stories 14.2, 14.1 and 14.3 (a source); S7. In the sound order it follows EL-044, EL-179, EL-050, EL-051 and EL-049.
- **Files.** Adds `elements/ListeningHorn/` (`ListeningHorn.csproj`, `ListeningHornElementDeclaration.cs`, `listening_horn.tscn`, `icon.svg`) and `CuriousContraptions.tests/ListeningHornTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only.
- **Disqualified if** the probe finds no aperture capture or no observable routed occurrence at a port (then rows 1–5 cannot be proven without the exit horn and the three route identities need the engine slice first), or that a hollow frustum does not exempt its own aperture from occlusion.
