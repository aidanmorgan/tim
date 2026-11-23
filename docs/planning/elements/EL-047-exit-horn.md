# EL-047 · Exit horn — element readiness spec

**Current delivery (24 October 2026):** [EL-047 delivery slice](../invest/named-elements.md#element-047). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md); acoustic route rules R1–R6 in [EL-046](EL-046-listening-horn.md#acoustic-route-rules-shared-by-el-046-el-047-el-048).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-047 · Exit horn |
| Type | Sound |
| Anchor | [requirements.md#element-047](../requirements.md#element-047); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-047); owner S534 |
| Disposition | **To deliver as a single element.** It refines no CAT part. It shares the Listening horn's body but emits instead of receiving, so it is neither a duplicate nor a family member of [EL-046](EL-046-listening-horn.md). |
| Planned package | id `exit_horn`, directory `elements/ExitHorn/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | End of the route EL-046 → [EL-048](EL-048-acoustic-duct.md) → EL-047; its output is heard by [CAT-060](CAT-060-sound_meter.md) / [EL-044](EL-044-tone-selective-sound-meter.md) or caught again by another listening horn. Emission pattern matches [CAT-061 Speaker](CAT-061-speaker.md). **Ordering:** third of the three route slices; it carries the full-route acceptance for all three. |
| Roadmap story | [EL-047 delivery slice](../invest/named-elements.md#element-047) `named-el-047`. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Stories 14.0, 14.2, 14.1 and 14.3, EL-046 and EL-048 (itself blocked on ENGINE-CONNECTION-MATERIALS (solid strand)). |

## 2. Declaration

Closest delivered shape: the Funnel for the hollow body (`elements/Funnel/FunnelElementDeclaration.cs:13`); the emitter follows the Speaker's ([EL-180](EL-180-pulse-speaker.md#2-declaration)).

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the acoustic outlet interface Story 14.0 adds (N7).

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | The EL-046 horn body: hollow Frustum, axis +X, length 0.8 m, throat radius 0.1 m at x = −0.3, mouth radius 0.4 m at x = +0.5, wall 0.03 m, centred on y = 0.1; throat collar Tube r 0.1–0.13 × 0.1 m; foot box 0.9 × 0.14 × 0.7 m at (0, −0.37, 0); one socket sphere r 0.075 m. Plus three ribs: Torus colliders of tube radius 0.03 m round the bell at x = −0.1, 0.15, 0.4 (ring radii 0.21, 0.30, 0.39 m) *proposed*. |
| Mass and material | Static (mass 0). Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Opaque to sound; its own colliders are exempt for its own outlet (L6). |
| Constraints | none. |
| Outlet | One release outlet at the mouth centre (0.5, 0.1, 0), axis +X: pattern Cone 35°, speed 12 m/s, range 8 m (A2), coupling `coupling` (rule R4). It has no receiver: sound arriving at its mouth from outside is not captured. |
| Stores | none. With no routed occurrence at `SoundIn` nothing is emitted. |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Parameters**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `coupling` | Real f32 | None | 0.8 | 0.1 | 0.9 | Clamp; BuildOnly. The same finite loss as the listening horn; R2 forbids gain. |

**Ports**

| `PortId` | Role | Domain | Direction | Position (m) |
| --- | --- | --- | --- | --- |
| `SoundIn` (new id) | `SoundIn` (new role) | Audio | Input | (−0.4, 0.1, 0), facing −X |

It accepts ducts from a `SoundOut` (Audio, Output) on [EL-046](EL-046-listening-horn.md); several ducts may end here and each routed occurrence is released (R6). No electrical port: the exit horn is passive.

**Presentation bindings.** A gold dot `Visual/Pulse` arrives at the throat, then forward translucent rings leave the mouth ← the release event cue (`PresentationDriver.Event` on the acoustic event kind, N12), as the Speaker's shadow-free rings ([DESIGN.md Sound speaker](../../../DESIGN.md#sound-speaker)).

**Palette.** Category `ElementCategory.Control`; palette order 433 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Exit horn"; colour cream `#fff8e9`. Description: "Sends sound from a connected duct out of its mouth in a forward cone. With nothing connected it stays silent."

**Variants.** None; the pattern is fixed Cone ("directed acoustic field" in the row).

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Bell` | flared cone, throat r 0.1 → mouth r 0.4 m, length 0.8 m, cream `#fff8e9` | hollow, Frustum collider |
| `Ribs` ×3 | cyan `#66b8c9` rings, tube radius 0.03 m, proud of the bell | solid, three Torus colliders |
| `Rim` | gold `#e8b764` band on the mouth edge, flush | decorative: printed flat |
| `Collar` | navy `#293954` tube at the throat | hollow, Tube collider |
| `Foot` | box 0.9 × 0.14 × 0.7 m, navy | solid, box collider |
| `Pulse` dot, rings | gold dot, translucent rings | decorative: glow, moved by a binding |
| Port sphere | r 0.075 m, gold `#f7cb52` | solid, sphere collider |

Icon: a horn in profile with two arcs leaving the mouth. **At a glance** it differs from the Listening horn by the three raised ribs round its bell (shape, not only their cyan colour).

### Behaviour and numbers

- **Release (R4).** Each routed occurrence reaching `SoundIn` is re-emitted once, tone unchanged, strength × `coupling`; a routed sustained occurrence is released as one sustained emission of the same span. Laws L1–L4, L6, L7 then apply from the mouth.
- **Worked route** (from [EL-046](EL-046-listening-horn.md#behaviour-and-numbers)): speaker 2 m from the listening horn, 4 m duct, both couplings 0.8 → release strength 0.397; a meter 2 m in front reads 0.301 (triggers at the default 0.25), at 3 m reads 0.231 (does not). So the default route reaches about 2.7 m beyond the exit, and a more sensitive meter or a shorter duct extends it.
- **Range.** The release is a new emission: its 8 m range and falloff start at the exit mouth. The no-gain rule is per stage (R2), so the release strength is always below the arrival that fed the route.
- **Tick order.** Release is part of the Phase 2 acoustic pass; a meter that hears it responds per D7.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S534): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with Frustum, Tube and Torus colliders (C1, C10) | present | EL-044 C1, C10 |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| An acoustic port domain (C11) | narrower | owned by N7 |
| Event cue presentation (C12) | narrower | owned by N12 |
| Cone emission, propagation, occlusion, tone (N1, N2), sustained (N3) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Release of a routed occurrence from a typed port (N7) | missing | Story 14.0, gap 6 |
| Acoustic event kind and cue (N12) | missing | Story 14.0, gap 11 |

Counts: 2 present, 0 untested, 2 narrower, 3 missing. Nothing here needs element-specific engine code.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`); Story 14.0; EL-046 and EL-048 (no other source feeds `SoundIn`); a sound source upstream; a meter downstream.

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** One release outlet (point, axis, cone, coupling) and one Audio input port. Release (R4) is the engine's route law for every declared outlet; the package has no handler and no engine code may branch on `exit_horn`.
2. **Generic laws.** Release: routed strength × declared coupling, re-emitted as a cone occurrence (R4, R2); then L1–L4 and L6 from the mouth. Loop bound: R5. `coupling` is the only parameter.
3. **Three layers.** *Colliders:* bell Frustum, collar Tube, foot box, one socket sphere and three rib Tori on one static body, static default material. *Visuals:* the art table above; no art mesh is a collider. *Animation:* read-only bindings on the release event cue. Moved by a binding: `Pulse` dot, rings. Moved by nothing: `Bell`, `Ribs`, `Rim`, `Collar`, `Foot`, port sphere.
4. **One-way flow and typing.** Routed occurrence → outlet → emission → receivers → presentation; nothing returns. Typed Audio port; f32 values.
5. **Flat state.** 1 static body; 7 colliders (1 frustum, 1 tube, 1 box, 1 sphere, 3 torus); 0 joints; 0 drives; 0 sensors; domain nodes: 1 acoustic outlet, 0 signal or electrical nodes; 1 Audio port; 0 stores; 2 presentation bindings.
6. **Performance fitness.** 7 colliders against the Funnel's 5 (`elements/Funnel/funnel.tscn`) and the Listening horn's 4: the three extra are the ribs, which are the shape cue that tells the two horns apart and, being proud, must collide (visible = solid). Static body; it joins no island. The outlet emits at most one occurrence per routed occurrence, and R5 bounds a loop. Its emissions follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6 (tables sized at Run from declarations, no scene ceiling; reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES`).
7. **Scenarios.** Rows 1–9 run the real simulation and observe through a Sound meter; none depends on a code path that names this package.

## 4. Sources and legacy

- Requirement row [element-047](../requirements.md#element-047): "Converts incoming duct energy into a directed acoustic field"; outcome "Disconnected inlet emits nothing".
- Named entry [element-047](../invest/named-elements.md#element-047), owner S534; component research row EL-046–048 and recipe "Quiet Corridor" ([component research](../../component-research.md#sound)).
- Legacy: no exit horn. The speaker's emission facts are the precedent: pulse created at the transformed mouth along local +X with pattern Cone (`parts/SpeakerPart.cs@a6c914e:L84-L86`); five forward rings, 64 segments, shadow-free (`parts/SpeakerPart.cs@a6c914e:L104-L114`).
- **Files harvested:** `parts/SpeakerPart.cs` (emission and ring precedent); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-047](../requirements.md#element-047), [acoustic profile](../invest/profiles.md#acoustic). Planned test class `ExitHornTests`. Tolerances and stall guard: [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given Speaker → listening horn (2 m) → 4 m duct → exit horn, a Wall blocking every direct line, and a supplied Sound meter 2 m in front of the exit, when the speaker pulses, then the meter reads 0.301 within T-level and triggers once, after the speaker's pulse and not before (causal order). | `ExitHornTests.TheRouteCarriesSoundRoundAWall` | Wall kept, route absent: meter 0 | meter at 3.3 m reads 0.397/(1 + 0.08·3.3²) = 0.212 and does not trigger at the default threshold (0.038 below it, outside T-level) |
| 2 | Given the duct removed so `SoundIn` is unconnected, then the exit horn emits nothing, although both horns remain (requirement outcome). | `ExitHornTests.ADisconnectedInletEmitsNothing` | row 1 | — |
| 3 | Given the exit horn turned away from the meter, or a Wall in front of its mouth, then the meter reads 0. | `ExitHornTests.TheReleaseIsADirectedOccludableCone` | row 1 | meter 33° and 37° off the exit axis; meter at 7.9 m and 8.1 m with threshold 0.05 |
| 4 | Given a Speaker aimed straight into the exit horn's mouth, then nothing is captured and nothing appears upstream. | `ExitHornTests.TheMouthDoesNotListen` | a listening horn in the same place captures | — |
| 5 | Given `coupling` 0.1 and 0.9, then the meter reading scales in proportion (0.038 and 0.339 at 2 m), and is always below the level arriving at the listening horn (R2). | `ExitHornTests.CouplingScalesTheReleaseAndNeverAmplifies` | — | both limits |
| 6 | Given a sustained source, then the release is one sustained emission and the meter triggers once. | `ExitHornTests.ASustainedRouteReleasesOneSustainedEmission` | pulse source: one trigger per pulse | — |
| 7 | Reset clears in-transit and released occurrences; save then load round-trips `coupling`, pose and the duct connection. | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` plus `ExitHornTests.ResetClearsTheRoute` | — | Reset with an occurrence inside the duct |
| 8 | `coupling` out of range or non-finite is refused atomically with a typed reason; an Electrical, Signal or Fluid output linked to `SoundIn` is refused. | `ExitHornTests.OutOfRangeCouplingAndForeignLinksAreRefused` | limits accepted | first f32 beyond each limit |
| 9 | Given a loop (the exit horn's mouth aimed at the listening horn that feeds it, 2 m apart) and a supplied Sound meter in the exit cone, when the speaker pulses once, then the release is captured again but does not enter the duct it has already passed (R5): the exit horn releases exactly once, the meter triggers once and the number of routed occurrences does not grow; a second speaker pulse gives one more release and one more trigger ([S528](../invest/decisions.md#s528)). This row is the loop proof for EL-046, EL-047 and EL-048. | `ExitHornTests.ALoopDoesNotGrowAndASecondPulseRoutesAgain` | exit horn aimed away from the listening horn: one release per pulse | a ring of two distinct ducts (horn A → duct 1 → exit B aimed at horn C → duct 2 → exit D aimed at horn A): each duct carries the occurrence once, two releases, then nothing |

**Chrome recipe (pending, post-catalogue).** The "Quiet Corridor" construction: speaker → listening horn → duct → exit horn → supplied Sound meter behind a Wall.

## 6. Open questions

**Resolved.**

1. **Shared geometry.** One horn body for both roles, told apart by three raised ribs on the exit horn: decided under the standing delegation, because the owner's visible-difference rule needs a shape cue and a shared body keeps the pair readable as two ends of one route.
2. **Pattern choice.** Fixed Cone; no omnidirectional option: [D19](../invest/delivery-batches.md#3-family-level-decisions) (no requirement row or lesson uses one, and the row says "directed").
3. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) The loop test that EL-048 assigns to this slice is now row 9, with R5 read as single traversal per duct by lineage. (b) Several ducts may end at `SoundIn`: no delivered rule limits links per port (R6). (c) Row 1's boundary meter moves to 3.3 m so the non-trigger margin exceeds T-level. (d) Coverage is bound by the published section form and is blocked on S7 (section 7). (e) Palette order 433 (coordinator decision 36).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Wall, Battery (delivered); Speaker, Bell, Sound meter, EL-044 (planned before it); EL-046 and EL-048 (planned before it, required).
- **Campaign:** chapter 8, levels 71–80 ("exit horn"), first taught at **79a**, after the Listening horn and Acoustic duct in the same level (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); reuse 81–100, 117–125, 136–150. [sequence-task-407](../requirements.md#sequence-task-407) ("connected horn/ducts carry bounded sound"); process [IX-12](../requirements.md#interaction-12).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) an acceptance pair of its own, since its ledger entry links only the umbrella `todo-352`, which has no co-anchored `sequence-task-NNN` in `requirements.md` and is shared by nine identities (the reader refuses two identities on one task source); (2) a decision anchor, since its ledger owner S534 has no `<a id="s534">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies both; it is not written earlier, because the reader throws on a malformed block. Mode records: `Configuration=Fixed`.
- **Lesson fixtures (S8):** needed only where a level pre-places it.

## 8. Delivery

- **Slice shape.** Single element, data-only.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Story 14.0 amended with gaps 2, 6 and 11; Stories 14.2, 14.1 and 14.3; EL-046; EL-048 (itself blocked on ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1); S7.
- **Files.** Adds `elements/ExitHorn/` (`ExitHorn.csproj`, `ExitHornElementDeclaration.cs`, `exit_horn.tscn`, `icon.svg`) and `CuriousContraptions.tests/ExitHornTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only.
- **Disqualified if** the probe finds no release outlet, that a release cannot carry a sustained occurrence, or that the route needs a rule naming either horn package.
