# TH-09 · Insulating panel — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-09 delivery slice](../invest/named-elements.md#thermal-09). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); probe rows "TH-04 R<n>" and "TH-08 C<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities) and [TH-08 section 3](TH-08-heat-conducting-bar.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-09 · Insulating panel · Thermal |
| Anchors | [requirements.md#thermal-09](../requirements.md#thermal-09) (task `sequence-task-443`); [named-elements.md#thermal-09](../invest/named-elements.md#thermal-09) |
| Owner key / research | S571 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Parameter-bound size precedent: the delivered Wall ([CAT-066](CAT-066-wall.md)). The control of the first heat lessons with [TH-04](TH-04-electrical-heating-plate.md) and [TH-37](TH-37-heat-sensitive-target.md); protects [TH-14 Ice block](TH-14-ice-block.md) in lesson 67. Conductors lead: [TH-08](TH-08-heat-conducting-bar.md). No CAT refines it. |
| Campaign | Intro 19; practice 20; reuse 67, 84, 139 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (static pairs) and ENGINE-CONTACT-OBSERVATIONS (a dynamic target on the panel); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (N3: a static panel; Run length) and coordinator decision 21. |

### Disposition

**To deliver as a single element** after TH-04 and TH-37. Planned package id `insulating_panel`, directory `elements/InsulatingPanel/`, display name "Insulating panel". Not a duplicate and not a Family member of TH-08: it has two stacked faces and a thickness-bound link instead of two ends and a conductivity-bound one.

## 2. Declaration

Follows the delivered Wall for parameter-bound colliders and art on one static body (`elements/Wall/WallElementDeclaration.cs:11-13`, `:40-45`).

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:29`), and the thermal-node contract of ENGINE-THERMAL.
- **Body.** One static body (`KinematicMode.Static`, zero static mass frame), a slab lying flat: 1.0 m (X) × `thickness` (Y) × 0.8 m (Z), origin at its centre. The player places it with its lower face touching the face it insulates (the Heating plate's disc top at 0.21 m, a static-on-static pair resolved once at Run, T3); cargo rests on its upper face. It is static because thermal links come from touch, not from a narrowphase contact (coordinator decision 21; audit N3); the earlier dynamic panel, made loose only to obtain a contact, is withdrawn.
- **Colliders (metres; t = `thickness`).**

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_lower` | (0, −t ÷ 4, 0) | (0.50, t ÷ 4, 0.40) | lower layer; thermal face of node `lower` |
  | `col_upper` | (0, +t ÷ 4, 0) | (0.50, t ÷ 4, 0.40) | upper layer; thermal face of node `upper` |

  Four `ColliderParameterBinding` rows: `HalfExtentY` = 0.25 × t and `OffsetY` = ∓0.25 × t for each (`GeometryComponent`, `CuriousContraptions.Abstractions/SceneGeometry.cs:24`). Two colliders because a face is a collider (T2) and the two sides must be different nodes.
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0. Like every collider it blocks light and sound.
- **Thermal nodes.** `lower` and `upper`: heat capacity 5 J/K each (light fibre board, declared); ambient conductance 0.2 W/K each (edges and uncovered margin, declared); face conductance 16 W/K (ceramic).
- **Internal link.** `lower` ↔ `upper`, a slab (TH-08 C3): conductivity 0.05 W/(m·K), area 0.8 m², length `thickness`, so G = 0.04 ÷ t: 0.8 W/K at 0.05 m, 0.4 W/K at 0.10 m, 0.2 W/K at 0.20 m.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `thickness` | `ParameterKind.Real` | `ParameterUnit.Metre` | 0.10 | 0.05 | 0.20 | `ParameterEditorPolicy.Clamp`; player-editable unless the level locks it (D11); build-only; set with the size gizmo as the Wall's is |

- **Ports.** None. **Stores.** None beyond the nodes.
- **Presentation bindings.** `Visual` scale Y ← `thickness` (parameter-driven, the Wall's pattern). No temperature cue: the panel is read by its thickness.
- **Palette.** Category `ElementCategory.Structure`. Order: 510 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Lower`, `Visual/Upper` cream `#fff8e9` | two stacked boxes 1.0 × t ÷ 2 × 0.8 m | solid, `col_lower`, `col_upper` | the body pose; height by the parameter binding |
| `Visual/Section` navy `#293954` | a zigzag section pattern printed flush on the four edges | decorative: printed flat | the body pose |

Icon: a slab seen edge-on with a zigzag core. At a glance it differs from the Wall (a plain upright block) by its zigzag edge and lying flat, and from the Heat-conducting bar by being broad with no ringed ends.

### Compiled counts

Bodies 1 (static); colliders 2 (box); joints 0; drives 0; sensors 0; thermal nodes 2; faces 2; internal links 1; ports 0; stores 0; collider parameter bindings 4; presentation bindings 1.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R6 | Colliders from art; parameter bounds | present | as TH-04 | as TH-04 |
| TH-08 C2 | A Real parameter drives collider half extents and an art scale, through Reset, save and load | present (on the static Wall) | as TH-08 | as TH-08 |
| P1 | The same binding including `OffsetY` | present, untested for `OffsetY` | `GeometryComponent.OffsetY`, `CuriousContraptions.Abstractions/SceneGeometry.cs:24` | not opened |
| TH-08 C3 | A slab link whose length is a declared Real parameter | **missing** | none | none; owner ENGINE-THERMAL |
| TH-04 R9, R10, R11, R13 | Nodes, touch links (the static lower face on the disc; a dynamic target on the upper face), ambient exchange, thermal units | **missing** | as TH-04 | none; owner ENGINE-THERMAL |

Counts: present 3, untested 1, narrower 0, missing 5. All five missing rows have one owner, **ENGINE-THERMAL** (R10 with ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-09](../requirements.md#thermal-09): thickness and material properties resist heat transfer while retaining explicitly supported radiative/convective paths; no variants.
- **Named entry** [thermal-09](../invest/named-elements.md#thermal-09): owner S571; "A thinner/lower-resistance comparison leaks heat faster; insulation is not a universal heat deletion field".
- **Research** per-element table: "low-conductance link"; "slows heat loss; protects cargo". The [first heat slice](../../thermal-component-research.md) uses this panel as a control.
- **Element map** row TH-09: FiniteLedger, GeometryQuery, SensibleHeat, ThermalConduction, ThermalRadiation; "insulation never deletes heat". Navigation only (D14).
- **Processes.** [IX-18](../requirements.md#interaction-18), [IX-19](../requirements.md#interaction-19), [IX-20](../requirements.md#interaction-20).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for insulat, conductance and heat terms. The Wall's configurable dimensions (`engine/gpu/WorkshopWall.cs@a6c914e:L8-L10`) are the harvest baseline only.

## 5. Acceptance

Point of truth: [thermal-09](../requirements.md#thermal-09) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact links on each face, one slab link through, ambient at each node (T3). The panel is a body with a small conductance; nothing removes heat except the declared ambient paths.
- **Reference stack.** A supplied Heating plate (T14), the panel lying on its disc (metal on ceramic, 12.8 W/K), a TH-37 target lying on the panel (ceramic on ceramic, 16 × 16 ÷ 32 = 8 W/K; target 0.45 W/K to ambient). θ is the rise above 288 K. Steady state at 0.10 m:
  - target: 8·(θ_u − θ_t) = 0.45·θ_t, so θ_t = 0.9467·θ_u and it takes 0.426·θ_u;
  - `upper`: 0.4·(θ_l − θ_u) = (0.2 + 0.426)·θ_u, so θ_l = 2.565·θ_u;
  - `lower`: 12.8·(θ_d − θ_l) = 0.2·θ_l + 0.626·θ_u = 1.139·θ_u, so θ_d = 2.654·θ_u;
  - disc: 120 = 0.5 × 2.654·θ_u + 1.139·θ_u = 2.466·θ_u, so θ_u = 48.7 K.
  Result: disc 417 K, `lower` 413 K, `upper` 337 K, target 334 K; 30.5 W cross the panel. Check: 0.5 × 129.2 + 0.2 × 124.9 + 0.2 × 48.7 + 0.45 × 46.1 = 64.6 + 25.0 + 9.7 + 20.7 = 120 W.
- **Thickness.** The same algebra with G = 0.8 (0.05 m): θ_l = 1.7825·θ_u, 120 = 1.912·θ_u, target 288 + 0.9467 × 62.8 = 347 K, 39.3 W crossing. With G = 0.2 (0.20 m): θ_l = 4.13·θ_u, 120 = 3.574·θ_u, target 320 K, 21.0 W crossing. Without the panel the target settles at 412 K ([TH-37](TH-37-heat-sensitive-target.md)).
- **At 60 s** (two-stage estimate: the plate side settles with a 28 s time constant, the upper side lags by 15.5 J/K ÷ 1.03 W/K = 15 s): target rise about 46.1 × (1 − e^(−45 ÷ 28)) = 37 K at 0.10 m, about 47 K at 0.05 m, about 25 K at 0.20 m, against about 110 K with no panel. Rows therefore assert order with margins.
- **Run length these rows need: 60 s** (A1 to A4 observe at 60 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s; the plate's Battery has `capacity` 7200 J so it lasts the 60 s).
- **Panel alone on the plate.** `upper` loses only to ambient: 0.4·(θ_l − θ_u) = 0.2·θ_u gives θ_u = 0.667·θ_l; `lower`: θ_d = 1.026·θ_l; 120 = 0.513·θ_l + 0.333·θ_l, so `lower` 430 K and `upper` 383 K. The far side warms and sheds heat by its declared path.
- **Tick order.** T8. **Tolerances.** T12. Stall guard 180 s.

### Rows

Planned class `InsulatingPanelTests`. All rows use a Battery with `capacity` 7200 J.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the reference stack at 0.10 m, when 60 s pass, then the target has risen more than 2 K and less than half the rise it shows with no panel | `APanelSlowsHeatWithoutDeletingIt` | the target directly on the disc | none | as stated |
| A2 | Given the stack at 0.05 m and at 0.20 m, when 60 s pass, then the thin panel's target is at least 5 K warmer | `AThinnerPanelLeaksFaster` | A1 | `thickness` at its minimum and maximum | 5 K margin against an estimated 22 K |
| A3 | Given A1 at 60 s, then the Battery's debit equals the heat stored in the five nodes plus the ambient account | `InsulationConservesHeat` | an unsupplied stack: every node at 288 K, debit 0 J | none | 2% of the debit |
| A4 | Given the panel alone on the supplied plate, when 60 s pass, then `upper` has risen more than 10 K and the ambient account has grown | `TheFarFaceKeepsItsAmbientPath` | none | none | as stated |
| A5 | Given A1 then Reset, then every node is at 288 K, the stack is at its authored pose and a second Run repeats A1 | `ResetRestoresThePanelAndItsNodes` | none | none | as A1 |
| A6 | Given a placed panel with `thickness` 0.15, when saved and loaded, then the value and pose are kept and the colliders are 0.15 m tall in total | `SaveAndLoadKeepThickness` | none | none | exact value |
| A7 | `thickness` outside 0.05 to 0.20 is clamped (`ParameterEditorPolicy.Clamp`) | generic: TH-04 R6 | bounds admitted | 0.05, 0.20 | clamped value exact |

No port, so no wrong-kind connection row. The requirement's clauses: thinner leaks faster (A2); not a deletion field (A3); retained convective paths (A4).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Thickness control.** A continuous Real parameter (D11), as the Wall's.
3. **Face convection.** Kept as each node's declared ambient conductance (T3); the map row's omission is navigation only (D14).
4. **Radiation.** None between nodes (T3); the "radiative path" the row names is light, which the panel blocks as any collider does.
5. **Static or dynamic.** Static, lying flat (coordinator decision 21; audit N3): touch, not contact force, makes a link, so both faces conduct while they touch within the tolerance.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04, TH-37 (planned before it), TH-21, TH-26, TH-29, TH-14 (lesson 67: a panel under an ice block slows its melting).
- **Campaign.** Introduction 19, practice 20; reuse 67, 84, 139. Loose cargo; fixed partners need the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS); ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04; TH-37; S7; S3 preferred.
- **Files.** `elements/InsulatingPanel/InsulatingPanel.csproj`, `InsulatingPanelElementDeclaration.cs`, `insulating_panel.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/InsulatingPanelTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if the probe finds every row present and tested; otherwise the engine tier.
- **Disqualifiers.** No slab link with a parameter-bound length (then `thickness` becomes a three-option Choice selecting one of three fixed links and three collider sets); `OffsetY` bindings failing (P1); static pairs not resolved at Run; faces per body instead of per collider (the panel would then be one node and conduct perfectly).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One static body, two boxes, two nodes, one slab link, one parameter, five bindings. No update loop or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT on contact, slab and ambient links. "Insulates" is a small declared conductance.
3. **Three layers.** Colliders: `col_lower`, `col_upper`. Visuals: two layer boxes each with its collider; a printed edge pattern. Animation: none beyond the parameter-driven height.
4. **Flow and typing.** No ports; one f32 parameter in metres.
5. **Flat state.** The counts in section 2.
6. **Performance.** Two primitive colliders, both needed as face boundaries; thermal work is two nodes, one link and one term per live touch pair in the link table (T10). Static, it adds no solver body; the link table persists through sleep when ENGINE-ISLANDS-SLEEP (planned) lands.
7. **Tests are scenarios.** A1 to A6 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `insulating_panel` |
| Named identity | TH-09, `../invest/named-elements.md#thermal-09` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-443` and the ledger entry has no todo link |
| Decision | S571; `scope-corrections.md` on published main has no `s571` block |
| Mode records | `Configuration=Fixed` |
