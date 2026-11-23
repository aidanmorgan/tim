# GAP-07 · Granular sieve — named-identity readiness spec

**Current delivery (24 October 2026):** [GAP-07 delivery slice](../invest/named-elements.md#gap-07). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-07 · Granular sieve · Mechanical (P3 potential; adopted for implementation by the 24 October 2026 owner decision) |
| Anchor | [requirements.md#gap-07](../requirements.md#gap-07) (task `sequence-task-540`); [named-elements entry](../invest/named-elements.md#gap-07); decision block [S640](../invest/scope-corrections.md#s640) |
| Owner key | S640 |
| Related identities | [GAP-06 Granular dispenser](GAP-06-granular-dispenser.md) (supplies the mixed feed and declares the two fractions); [EL-058 Size grate](EL-058-size-grate.md) (the ball-sized neighbour). No CAT refines it. |
| Status | Not started; blocked on the engine programme (owner decision 3a), on `ENGINE-GRANULAR` (owner decision 14) and on `ENGINE-PARAMETER-BOUND-FRAMES`; it follows GAP-06. |

### Disposition

**To deliver as a single element**, gravity-fed only, one package: `granular_sieve`, directory `elements/GranularSieve/`, display name "Sieve". It is not a duplicate of the Size grate: it separates a granular inventory by declared grain diameter, and its openings are chosen by a typed preset. The powered-shake mode is not adopted (section 6). Blocked on the engine programme (owner decision 3a), then `ENGINE-GRANULAR`, then GAP-06.

## 2. Declaration

### Declaration to write

A level static tray whose floor is a row of rods. The tray is a declared granular store; the gaps between the rods are its declared aperture. Colliders follow the delivered Wall's pattern of parameter-bound geometry (`elements/Wall/WallElementDeclaration.cs:26-30`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`:29`), and the granular store and aperture contracts of `ENGINE-GRANULAR`. `KinematicMode.Static`; canonical zero static mass frame.
- **Body.** One static body. Origin at the centre of the rod plane. Rods run along local Z.
- **Colliders (ten boxes).** Material on all: restitution 0.05, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.

  | Collider | Shape | Centre | Size |
  | --- | --- | --- | --- |
  | `col_rod_0` to `col_rod_5` | box, long axis along Z | x = −0.5, −0.3, −0.1, 0.1, 0.3, 0.5; y = 0; z = 0 | half extents (by `aperture`: 0.065 m Narrow, 0.035 m Wide; 0.035; 0.45) |
  | `col_rail_a`, `col_rail_b` | box | (±0.53, 0.095, 0) | half extents (0.03, 0.16, 0.45); the inner face is on the outer rod's axis, so there is no edge gap |
  | `col_rail_c`, `col_rail_d` | box | (0, 0.095, ±0.48) | half extents (0.56, 0.16, 0.03) |

  Rod pitch 0.20 m, so each of the five slots is 0.20 − 2 × (X half extent) wide: 0.20 − 0.13 = 0.07 m Narrow, 0.20 − 0.07 = 0.13 m Wide. The rods are boxes because a cylinder's radius must be at least 1/16 m (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:240`), which would leave a Wide slot of at most 0.20 − 0.125 = 0.075 m, too narrow for Coarse; a box's half extents need only 1/1024 m (`:229`–`:231`), so 0.035 m is admitted. The rods stand 0.035 m above and below the rod plane at either setting. The rails stand 0.255 m above the rod plane. Ten colliders against the Receiver's five: each rod is the edge of a real opening, which the requirement asks for.
- **Granular store (engine story's descriptor).** Interior: 1.0 × 0.9 m between the rails, 0.25 m deep above the rod plane: 0.225 m³, so capacity 0.225 × 6 = 1.35 kg at the 6 kg/m³ bulk density of [GAP-06](GAP-06-granular-dispenser.md#declaration-to-write). That is above the 1.25 kg most Coarse one hopper can feed, so a retained fraction never overflows. Initial amount 0. It holds every declared fraction that enters it, by the diameter its source declares: Fine 0.05 m and Coarse 0.10 m from GAP-06, and row S5's 0.06 m and 0.08 m from a fixture store. That a store admits any entering fraction is a rule of `ENGINE-GRANULAR` (one of the additions in the Batch M summary), not of this package.
- **Aperture (engine story's descriptor).** The five slots. Clear width per slot by `aperture`: 0.07 m Narrow, 0.13 m Wide. Passage ratio 1: a fraction passes while its grain diameter is no more than the slot's clear width (a thin layer on a screen does not arch as a column in a throat does, so the ratio is 1 here and 3 on the hopper; both are declared values of one law). Rate 0.5 kg/s for the fractions that pass, twice the hopper's 0.25 kg/s, so passing grain does not accumulate.
- **Parameters.**

  | Name | Kind | Options | Default | Editor policy |
  | --- | --- | --- | --- | --- |
  | `aperture` | `ParameterKind.Choice` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:11`) | Narrow, Wide | Narrow | build-only; player-editable unless locked (D11) |

  One Choice selects one declared value per option for three things: each rod collider's X half extent, each rod's art scale along X, and the aperture's clear width. That binding is `ENGINE-PARAMETER-BOUND-FRAMES` ("a Choice parameter may select one declared value per option for a collider component"); there is no continuous width.
- **Ports, sensors.** None. Separation is by the declared widths and diameters alone.
- **Presentation bindings.** `Visual/Fill` height ← the store's committed surface height above the rod plane; `Visual/Stream` visibility ← the aperture's committed flow; each rod's art scale along X ← `aperture`, by the same per-option values as its collider.
- **Palette.** Category `ElementCategory.Structure`. Order: 548, next to `granular_dispenser` at 547 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Rod_0` to `Visual/Rod_5` navy `#293954` | bars, length 0.9 m, section 0.13 × 0.07 m Narrow or 0.07 × 0.07 m Wide | solid, `col_rod_*` | nothing (sized by the parameter at Build) |
| `Visual/Rail_A` to `Visual/Rail_D` cream `#fff8e9` | boxes as the colliders | solid, `col_rail_*` | nothing |
| `Visual/Brackets` gold `#f7cb52` | corner brackets printed flush on the rails' outer faces | decorative: printed flat | nothing |
| `Visual/Fill` | the retained grain, drawn with the retained fraction's dot size | the inventory's own picture; its surface supports rigid bodies under the engine story | presentation binding (committed surface height) |
| `Visual/Stream` | grain dots under the slots | decorative: material in flight, drawn by a binding | presentation binding (committed flow) |

Icon: six bars in a frame with small dots below and large dots on top. The selected aperture is read from the bar width itself (wide bars, narrow slots): a silhouette difference, not a number or a colour.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static colliders from package art, visible = solid, checked over parameter sets | present | `ISceneGeometryElement`, `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` |
| Sphere contact with static box colliders | present | `ColliderShapeKind.Box`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12` | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` (balls on the Receiver's five static boxes) |
| A collider component bound to a declared parameter | narrower: a Real parameter only, by multiplier and offset | `ColliderParameterBinding`, `CuriousContraptions.Abstractions/SceneGeometry.cs:24-27` | `TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass`; owner `ENGINE-PARAMETER-BOUND-FRAMES` |
| A Choice selecting one declared value per option for a collider component and a descriptor value | **missing** | none | none; owner `ENGINE-PARAMETER-BOUND-FRAMES` |
| A granular store receiving material that falls into its interior | **missing** | none | none; owner `ENGINE-GRANULAR` |
| An aperture whose passage depends on declared grain diameter against its clear width | **missing** | none | none; owner `ENGINE-GRANULAR` |
| Two fractions conserved separately, one passed and one retained | **missing** | none | none; owner `ENGINE-GRANULAR` |
| A committed inventory read per fraction and the store's surface height as a presentation driver | **missing** | none | none; owner `ENGINE-GRANULAR` |

Counts: present 2, untested 0, narrower 1, missing 5. Owners: `ENGINE-PARAMETER-BOUND-FRAMES` for the narrower row and the first missing row; **`ENGINE-GRANULAR`** for the other four, with the additions listed in the Batch M summary. Nothing needs code that names this part.

## 4. Sources and legacy

- **Requirements.** "A physical aperture separates a taught mixture by grain size. Depend on GAP-06 and real collision openings; selected-only aperture choices use typed presets. Do not classify particles by a hidden desired-output label." Visual: "Shallow cream frame and navy slotted insert on gold supports; distinct aperture silhouettes make the selected size clear without a numeric inspector." Outcome: "Small grains pass and oversize grains remain; an all-oversize control blocks and finite mixed feed conserves both fractions. Verify either gravity-only operation or every separately adopted powered-shake mode before using it" ([gap-07](../requirements.md#gap-07)).
- **Decision block.** "Gravity-fed sieve passage with one passing and one blocked grain size. Powered shake is decided separately only if adopted" ([S640](../invest/scope-corrections.md#s640)).
- **Audit.** "GAP-06 and an independently specified size distribution. Mixed feed sorts; all-oversize control blocks; no hidden label-based routing" (`docs/physics-puzzle-gap-audit.md@a6c914e:L86-L86`).
- **Element map.** The row names GranularTransport ([element map](../general-engine-element-map.md)).
- **Legacy.** None: no sieve, grain or size-sorting code, test or level exists.

## 5. Acceptance outline

Point of truth: [gap-07](../requirements.md#gap-07). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

Laws: the granular inventory law of `ENGINE-GRANULAR` and rigid contact. No fraction carries a destination; a fraction passes when its declared diameter fits the declared slot.

- **Narrow, slot 0.07 m.** Fine (0.05 m) passes with 0.02 m to spare; Coarse (0.10 m) is 0.03 m too big and stays in the tray.
- **Wide, slot 0.13 m.** Coarse passes with 0.03 m to spare. This is the wrong-choice control.
- **Timing.** A hopper feeding 0.6 kg Fine and 0.6 kg Coarse empties in 1.2 / 0.25 = 4.8 s. The sieve passes Fine at up to 0.5 kg/s against an arrival of 0.125 kg/s, so no Fine is left when the feed ends. The process fits the default 30 s Run (`CuriousContraptions.Abstractions/CadenceValues.cs:77`); a slower lesson declares its Run length under `ENGINE-RUN-LENGTH` (owner decision 13).
- **Retained pile.** 0.6 kg of Coarse is 0.1 m³ over a 0.9 m² floor: 0.11 m deep when level, under the 0.25 m interior depth.
- **Real openings.** A Basketball (0.68 m across) dropped on the tray rests on the rods at either setting, because the rods are colliders and 0.68 m is wider than a 0.13 m slot.
- **Fit.** The tray's 1.0 m catching width is wider than the hopper's 0.5 m outlet; a default Receiver stands under it.
- **Performance limits.** Measured by `ENGINE-GRANULAR` on its fixed reference scene; these rows use two stores, two apertures and one pile.
- **Tick order.** Rigid contact is solved in Phase 3 ([solver model](../../gpu-f32-physics.md#solver-model)); the phase in which inventory transfers commit is fixed once by `ENGINE-GRANULAR`. No signal, optical or acoustic read.

### Acceptance rows

Planned class `GranularSieveTests`. The feed comes from a supplied Grain hopper 0.4 m above the tray; amounts are read from the committed inventory read. Stall guard: 15 s of game time (three times the 4.8 s feed). Representation tolerance 0.01 kg, as GAP-06.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| S1 | Given a hopper with 0.6 kg Fine and 0.6 kg Coarse over a Narrow sieve over a Receiver, when Run, then the Receiver's pile holds 0.6 kg Fine and no Coarse, and the tray holds 0.6 kg Coarse and no Fine | `AMixedFeedSeparatesBySizeAndConservesBothFractions` | S3 | at every sampled tick each fraction sums to 0.6 kg across hopper, flight, tray and pile | 0.01 kg per fraction |
| S2 | Given 0.6 kg Coarse only and a Narrow sieve, then nothing is ever below the rod plane and the tray holds 0.6 kg | `AnAllOversizeFeedPassesNothing` | S1's Fine passes | none | exact zero below; 0.01 kg retained |
| S3 | Given `aperture` Wide and S1's feed, then both fractions reach the Receiver and the tray ends empty | `AWideSettingPassesBothSizes` | S2 | Narrow against Wide for Coarse (0.07 m and 0.13 m around 0.10 m) | 0.01 kg |
| S4 | Given a Basketball dropped on an empty sieve at each setting, then it rests on the rods | `TheRodsAreRealCollisionOpenings` | none | Narrow and Wide | ball centre stays above the rod plane |
| S5 | Given the same Narrow sieve fed a fraction of a third declared diameter, 0.06 m, from a test fixture store, then it passes, and one of 0.08 m is retained | `PassageFollowsDeclaredDiameterNotIdentity` | S1 | 0.06 m and 0.08 m around the 0.07 m slot | 0.01 kg |
| S6 | An undefined `aperture` option is refused atomically with a typed reason | `AnUndefinedApertureIsRefused` (deleted when structural story S4 covers it) | Narrow and Wide admitted | the two options | typed refusal |
| S7 | Reset after S1 empties the tray and the pile and restores the hopper's feed; a second Run repeats S1 | `ResetRestoresTheFeedAndRepeatsTheSort` | none | none | 0.01 kg; outcome repeated |
| S8 | Save and load keep `aperture` and the sieve's pose | `SaveAndLoadKeepTheApertureAndPose` | none | none | exact |
| S9 | Given the hopper unpowered, then nothing reaches the tray or the Receiver | `AnUnfedSieveDeliversNothing` | S1 | none | exact zero |
| S10 | Given a scene at the limit `ENGINE-GRANULAR` publishes, then S1's outcome is unchanged | `TheSieveHoldsAtThePublishedLimit` | none | at the limit | set by that story's measured reference scene |

No port, so no wrong-kind connection row. Collaborators the requirement names: GAP-06 (every row), the conserved fractions (S1), the all-oversize control (S2), no hidden label (S5). The powered-shake mode is not adopted. Every row except S4, S6 and S8 is executed on capabilities owned by `ENGINE-GRANULAR`.

### Integration

- **Works with.** The Grain hopper (GAP-06, undelivered and required); Receiver and Basketball (delivered).
- **Campaign.** "Sift Happens": introduction 93, practice 94, reuse 97, 136, 141 ([campaign allocation](../requirements.md#campaign-gap-allocation)). An objective over two delivered fractions needs the engine story's committed amount-reached observation, selected by the goal list. A level that fixes the sieve needs the lesson-fixture story (S8).
- **Coverage.** Section 7.

### Delivery

- **Shape.** Single element, engine tier unless GAP-06's slice has already exercised every capability it uses.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a); `ENGINE-PARAMETER-BOUND-FRAMES`; `ENGINE-LIQUID`; `ENGINE-GRANULAR` with the additions in the Batch M summary; GAP-06; S7 widened to GAP identities; S3 preferred.
- **Files.** `elements/GranularSieve/` (`GranularSieve.csproj`, `GranularSieveElementDeclaration.cs`, `granular_sieve.tscn`, `icon.svg`), `CuriousContraptions.tests/GranularSieveTests.cs`, this spec, the ledger entry and `TODO.md`.
- **Disqualifies the plan.** A Choice cannot select a collider half extent and an aperture width together; the engine story's passage rule is not by declared diameter; or material falling from one aperture is not credited to a store interior beneath it.

## 6. Open questions

Resolved:

1. **Powered shake.** Not adopted. The row asks for "either gravity-only operation or every separately adopted powered-shake mode", and S640 decides shake "only if adopted"; gravity-only satisfies the row and needs no supply, drive or kinematic body.
2. **Aperture presets.** A typed two-option Choice, as the row requires. `ENGINE-PARAMETER-BOUND-FRAMES` is a prerequisite; no continuous width is offered in its place.
3. **Level tray.** The sieve is level and the oversize fraction stays in the tray's store, which is the row's "oversize grains remain". No tilt and no second outlet (decided now: a retained amount is readable in the ledger, and a tilted screen would need a transport rule along the rods that no lesson uses).
4. **Mixed feed.** The two fractions of one hopper (GAP-06); no third preset.
5. **Catalogue entry.** Its own entry (D8), category Structure.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Ten static colliders, one declared store, one declared aperture, one Choice parameter and its per-option values, bindings. No sorting rule, label, update loop or engine branch.
2. **Generic laws.** The granular inventory law and rigid contact. "Small grains pass and oversize grains remain" is a consequence of the declared slot width and each fraction's declared diameter. The missing capabilities are stated element-free, each with one owner.
3. **Three layers.** Colliders: ten boxes (six rods, four rails) on one static body. Visuals: one mesh per collider, printed brackets, and the inventory's fill and stream. Animation: fill height and stream from committed state. Physics reads no art.
4. **Flow and typing.** No ports. `aperture` is a typed Choice; fractions are typed local ids; amounts are f32 kilograms.
5. **Flat state.** Bodies 1 (static); colliders 10 (box); joints 0; drives 0; sensors 0; nodes 0; granular stores 1; apertures 1; presentation bindings 8 (6 parameter-driven rod scales resolved at Build, fill, stream).
6. **Performance.** One static body of primitive colliders. Ten colliders exceed the Receiver's five for the reason given in section 2. The inventory costs one store and one aperture however much grain it holds. The engine has no island sleeping today; a body resting on the rods relies on `ENGINE-ISLANDS-SLEEP`. The measured budget is row S10.
7. **Tests are scenarios.** S1 to S10 run the production simulation.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), `ENGINE-GRANULAR` and `ENGINE-PARAMETER-BOUND-FRAMES`, general capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads. **A GAP identity cannot be bound at all today:** the reader lists only `EL-*.md` files and admits only an `EL-NNN` identity. **Blocked on the coverage follow-on story S7**, which widens the reader to GAP identities (owner decision 15, 22 November 2026).

| Field | Planned value |
| --- | --- |
| Package | `granular_sieve` |
| Named identity | GAP-07, `../invest/named-elements.md#gap-07` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-540` |
| Decision | S640, `../invest/scope-corrections.md#s640` (the block exists on published main) |
| Mode records | `Configuration=Adjustable` (`aperture`) |

What it lacks: GAP admission in the reader; a todo link in the ledger entry.
