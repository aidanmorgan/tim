# EL-084 · Fragile fish bowl — named-identity spec

**Current delivery (24 October 2026):** [EL-084 delivery slice](../invest/named-elements.md#element-084). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `T/` abbreviates `CuriousContraptions.tests/` and `A/` abbreviates `CuriousContraptions.Abstractions/`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32. Nothing here was built or simulated; every number is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-084 · Fragile fish bowl · Character |
| Anchor | [requirements.md#element-084](../requirements.md#element-084); [named entry](../invest/named-elements.md#element-084); scope index [todo-200](../requirements.md#todo-200) |
| Proof owner | S654 |
| Disposition | To deliver as a single element, the first consumer of body break in ENGINE-FRACTURE. [EL-088](EL-088-fish-tank-lure.md) reuses its break; its glowing fish is its own companion package, a different declaration from this one's; the two are not a family (EL-088 is static, rectangular and emits light). Not a duplicate of the Balloon: a Balloon's break empties a gas store and leaves its body whole. |
| Planned packages | main `fish_bowl`, directory `elements/FishBowl/`, class `FishBowlElementDeclaration`, with seven declared fracture fragments (the shards, ENGINE-FRACTURE); companion package `toy_fish`, directory `elements/ToyFish/`, class `ToyFishElementDeclaration` (owner decision 15; not a new ledger identity) |
| Refines / extends | No CAT spec. Break threshold law of [CAT-003 Balloon](CAT-003-balloon.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-FRACTURE, then S7 |

## 2. Declaration

**What the part is.** An open, dry glass bowl with a toy fish lying in it (owner decision 11: no liquid). The fish is a real loose ball that the walls hold in by collision. A hard enough knock breaks the bowl: its colliders are replaced by seven declared shards and the fish is free. There is no script, callback or per-part break code: the break is one declared threshold on the general contact-impulse observation, and its result is declared fragment bodies. The "character" is the toy fish; it does nothing by itself.

Interfaces: `IContactIntegrityElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:119`) in the form ENGINE-CONTACT-OBSERVATIONS leaves it; the break effect and fragments of ENGINE-FRACTURE; `ISceneGeometryElement` (`A/SceneGeometry.cs:10`). `KinematicMode.Dynamic`; `SupportedDomains = Mechanical`. Closest delivered shape: the Balloon's integrity observer (`elements/Balloon/BalloonElementDeclaration.cs:68`).

### Bodies and colliders

Local frame: origin at the centre of the bowl's underside, +Y up.

| Body | Colliders (m) | Mass and inertia | Material |
| --- | --- | --- | --- |
| 0 bowl (dynamic base) | wall: tube about +Y, outer radius 0.35, inner radius 0.31, half-length 0.20, centred (0, 0.24, 0); floor: cylinder r 0.35, half-height 0.02, centred (0, 0.02, 0) | 0.6 kg (wall 0.41, floor 0.19 by volume); centre of mass (0, 0.17, 0); moments about it (0.040, 0.056, 0.040) kg·m² from the annular-cylinder and disc formulas with the parallel-axis terms; gravity (0, −9.81, 0) | glass: restitution 0.3, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s |
| floor shard (companion body, absent until the break) | the floor collider itself: cylinder r 0.35, half-height 0.02 at (0, 0.02, 0) | 0.19 kg | glass |
| six wall shards (companion bodies, absent until the break) | boxes half extents (0.10, 0.20, 0.01), centred on the circle r 0.325 at y 0.24, one every 60°, each turned so its thin axis is radial | 0.068 kg each (0.41 kg together) | glass |

Geometry check: the fish (0.16 m across) lies inside the 0.62 m inner diameter with the wall 0.44 m high around it. Every shard lies inside the volume of the collider it replaces: the floor shard is the floor; each wall shard's inner face is 0.315 m from the axis (the wall's inner radius is 0.31) and its outer corners are √(0.335² + 0.10²) = 0.3496 m out (the wall's outer radius is 0.35), over the wall's full 0.40 m height. So no shard can start inside the fish, wherever the fish lies, because the fish is always inside the bore (radius 0.31 m). Shard masses sum to the bowl's 0.6 kg. The bowl is about Basketball size and tips only past atan(0.35/0.17) = 64°.

### `toy_fish` (companion package)

A dynamic part, the Tennis ball's kind of package (`elements/TennisBall/TennisBallElementDeclaration.cs:9`): `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`, `PayloadPolicy.None`, one body, no ports, parameters or stores. It is not a second body of `fish_bowl` (a package's further bodies come only from its sliders and hinges, `CuriousContraptions.Bundle/BundleValidator.cs:616`–`:619`) and not a fracture fragment (it exists from Run start). It is a companion package of this identity (owner decision 15), admitted to coverage by S7, with its own catalogue entry. It is declared **not placeable** (owner decision 24b, 23 November 2026): it has no palette entry and appears only with its parent. Each `fish_bowl` instance carries one `toy_fish`, placed with the bowl at a seat pose the bowl declares, resting on the bowl's floor with its centre at the bowl's (0, 0.12, 0), and removed with it; it is never placed by itself. It stays on its seat (the player cannot move it off), but the player may delete it alone in Build and restore it from its bowl, and a level may place a bowl without it; Reset restores the authored state either way (coordinator decisions 33 and 34). The not-placeable declaration and the parent's seat pose are the capability S7 adds with the companion admission (owner decision 24b).

| Body | Collider | Mass and inertia | Material |
| --- | --- | --- | --- |
| 0 fish | sphere r 0.08 at the origin | 0.05 kg; 2/5·m·r² = 0.00013 kg·m² | restitution 0.2, friction 0.4, rolling resistance 0.05, bounce threshold 0.1 m/s |

Metadata: display name "Toy fish", colour gold `#f7cb52`; no palette entry, so no category or palette order (owner decision 24b; today every package declares a positive palette order, `A/ElementMetadata.cs:16`, so the not-placeable form is S7's). Presentation: `Visual/Fish` follows body 0.

### Break declaration

- **Observation** (ENGINE-CONTACT-OBSERVATIONS, audit E6): one integrity observer naming the set of body 0's two colliders; threshold **1.5 N·s**, minimum approach speed 0.1 m/s; no gas store. The measure is the delivered one, per observed collider against each other collider: approaching normal impulse summed over that pair's contact points in one substep, strictly greater than the threshold (`A/ContactIntegrity.cs:17`–`:39`). It publishes a one-way committed occurrence.
- **Effect** (ENGINE-FRACTURE, with contact impulse as one more member of its declared load-measure set): the occurrence removes body 0's colliders and starts the seven shards at their declared poses relative to body 0 with its linear and angular velocity. The substep in which the limit was exceeded is re-solved with the shards in place of the removed colliders (coordinator decision 16), so the breaking contact acts on the shards and momentum is conserved. One `SimulationEventKind.IntegrityBroken` record is published (`A/EventDescriptor.cs:33`). One break per Run; Reset restores the bowl.

### Ports, parameters, stores

None. The threshold is fixed per kind (D13); no parameter unit for an impulse exists (`A/ParameterDescriptor.cs:15`) and none is needed.

### Metadata and bindings

Category `Goals`, display name "Fish bowl", palette order 203 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `#66b8c9`. Presentation: `Visual/Bowl` follows body 0; each `Visual/Shard*` follows its shard (the fish's art is its own package's). `Visual/Bowl` is hidden and the shards shown by a `Visible` binding on a committed-broken driver, which does not exist today and comes with ENGINE-FRACTURE. A crack flash on `Visual/Bowl` uses `PresentationDriver.Event` on `IntegrityBroken`.

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Bowl` | transparent cyan `#66b8c9` glass at low alpha: annular wall and disc floor, the top edge tinted cream `#fff8e9` within the wall's thickness | solid, tube and cylinder colliders; follows body 0 |
| `toy_fish` `Visual/Fish` | gold `#f7cb52` ball r 0.08 with navy `#293954` printed eye and fin lines | solid, sphere collider; follows the fish package's body 0 |
| `Visual/ShardFloor`, `Visual/ShardWall1` to `6` | glass disc and six flat glass plates, drawn to their colliders' outlines | solid; each follows its shard body; hidden until the break |

No water is drawn. Icon: a round bowl outline with a fish inside. It differs from EL-088 by being round, open and without a base slab.

### Behaviour and numbers

The engine multiplies the two restitutions of a contact (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`).

1. **Containment (contact law).** Below the threshold nothing changes: the bowl slides or rocks as a 0.6 kg body and the fish stays inside its walls.
2. **A fall.** The bench and the Wall declare restitution 1 (`engine/gpu/WorkshopPhysicsCompiler.cs:101`; `elements/Wall/WallElementDeclaration.cs:51`), so e = 0.3 × 1 = 0.3 and landing at speed v gives m_eff·(1 + e)·v. Landing flat on its floor, m_eff is the whole 0.6 kg: 0.78·v N·s, so it breaks above v = 1.92 m/s, a fall of v²/(2g) = 0.19 m; a 0.10 m drop (1.40 m/s, 1.09 N·s) holds and a 0.40 m drop (2.80 m/s, 2.18 N·s) breaks (F2). **Attitude.** Landing on an edge lowers the effective mass: 1/m_eff = 1/0.6 + d²/0.040 for a contact point whose lever arm about the centre of mass is d. The bottom rim, (0.35, −0.17) from the centre of mass, gives d ≤ 0.389 m and m_eff ≥ 0.183 kg; the top rim's outer edge, (0.35, +0.27), gives d ≤ 0.442 m and m_eff = 1/(1.667 + 4.885) = 0.153 kg, the least at any attitude. A tumbling bowl also spins: tipping over a shelf edge it gains at most ω = √(2 × 0.6 × 9.81 × 0.17 / 0.0573) = 5.9 rad/s (its whole 0.17 m centre-of-mass drop about the edge, pivot moment 0.040 + 0.6 × 0.17² = 0.0573 kg·m²), which can take up to 5.9 × 0.442 = 2.6 m/s off the rim's approach speed. So a fall breaks it at any attitude only if 0.153 × 1.3 × (v − 2.6) > 1.5, that is v > 10.2 m/s. A 0.6 m shelf (3.43 m/s) breaks it flat (2.68 N·s) but not rim-first (0.153 × 1.3 × 3.43 = 0.68 N·s); F1's 6.0 m shelf drops the centre of mass 6.0 + 0.17 − 0.27 = 5.90 m before a top-rim landing, 10.76 m/s, and gives at least 0.153 × 1.3 × (10.76 − 2.6) = 1.62 N·s.
3. **A strike** while it stands on the bench: impulse = μ·(1 + e)·v with reduced mass μ = m_ball·m_eff/(m_ball + m_eff) and e the product with the ball's restitution. A ball meets the wall at its own centre height, above the bowl's centre of mass (0.17 m) by d = 0.11 m (Bowling, r 0.28), 0.17 m (Basketball, r 0.34) or 0.08 m (Tennis, r 0.25). The bowl's effective mass there lies between its free-body value 1/(1/0.6 + d²/0.040) and its value with the far bottom edge held on the bench and sliding at friction √(0.4 × 0.3) = 0.35: Bowling 0.508–0.604 kg, Basketball 0.419–0.572 kg, Tennis 0.547–0.612 kg. Bowling ball (4 kg, 0.14, e 0.042): 0.470·v to 0.547·v, breaks above **2.74 to 3.19 m/s**. Basketball (1 kg, 0.55, e 0.165): 0.344·v to 0.424·v, above **3.5 to 4.4 m/s**. Tennis ball (0.35 kg, 0.85, e 0.255): 0.268·v to 0.280·v, above **5.4 to 5.6 m/s**. A rolled Basketball at 1 m/s (0.34 to 0.42 N·s) only shoves it, and the bowl does not tip: the hit rocks it at about 1.68 × 0.42 = 0.7 rad/s, about ½ × 0.131 × 0.7² = 0.03 J about the far edge, far below the 0.6 × 9.81 × (0.389 − 0.17) = 1.29 J needed to raise its centre of mass over that edge.
4. **Its own fish cannot break it**: μ 0.046, e 0.06, which would need 31 m/s.
5. **Resting and pressing** do not count: the measure ignores contacts that are not approaching (`T/ContactIntegrityTests.cs:44`).
6. **After the break** the shards and the fish are ordinary bodies; the fish rolls where the bench takes it.
7. **Tick order.** The break is decided and re-solved inside the physics substep in which the impulse occurred. No signal is read.

## 3. Engine capabilities

| Capability | Status | Evidence |
| --- | --- | --- |
| Dynamic body carrying a hollow tube collider and a second child collider, with a declared mass frame | present | `CompoundMassAndRollingTests.HollowChildLocalPoseAndDeclaredCentreGiveRealContactAndEmptySpace` (`T/CompoundMassAndRollingTests.cs:114`) |
| Pairwise approaching-impulse threshold, strict, resting contact excluded | present | `ContactIntegrityTests.ThresholdIsStrictWithoutHiddenTolerance` and `RestingSupportAndApproachAtTheThresholdDoNotQualify` (`T/ContactIntegrityTests.cs:40`, `:44`) |
| A level goal on an integrity break | present, untested here | `WorkshopGoalKind.IntegrityBroken` (`engine/gpu/WorkshopPuzzle.cs:9`); its test body was not opened |
| Integrity observer | narrower | today it names one collider and one gas store, and its only effect is to open that store's outlet (`A/GasCouplingValues.cs:15`); the body and its colliders stay (`BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie`, `T/BalloonTests.cs:118`). Owner: **ENGINE-CONTACT-OBSERVATIONS** (a set of colliders, no gas store, a one-way occurrence consumed by a declared effect) |
| Break effect: remove the observed colliders, start declared fragments inside their volume, re-solve the breaking substep, restore on Reset; a committed-broken presentation driver | missing | **ENGINE-FRACTURE** |
| Declared fragment bodies compiled with the instance, taking part only from the break (the seven shards); no palette entry, appearing only with their parent (owner decision 24b) | missing | **ENGINE-FRACTURE** (owner decision 15 lets fragments be companion content); S7 widens the coverage reader to admit them |
| The fish: a companion package of this identity, an ordinary one-body dynamic sphere present from Run start, declared not placeable and placed with each bowl at the bowl's declared seat pose (owner decision 24b) | present as a package shape (one dynamic sphere, as the Tennis ball); missing: a package with no palette entry (every package declares a positive palette order today, `A/ElementMetadata.cs:16`) placed with its parent at a seat pose the parent declares | owner decision 15 makes it a companion package and owner decision 24b not placeable; its admission and the not-placeable declaration are **S7**'s (its one owner). It needs nothing from ENGINE-FRACTURE, and it cannot be a second body of `fish_bowl` (`BundleValidator.cs:616`–`:619` admits further bodies only through sliders and hinges) |

## 4. Sources and legacy

- Requirement: "Contained fragile payload uses generic impact failure and containment"; outcome "Subthreshold impact retains contents" ([element-084](../requirements.md#element-084)).
- The element map lists FluidAdvection for this row; the bowl is dry by owner decision 11.
- Legacy search at `a6c914e` for "fish", "bowl", "fragile", "fracture", "shatter" and "break" found no fracture or fish source. The legacy impact-effect hook is the nearest source for the criterion.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A contact trigger records the first approach at or above its threshold, once | `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1177` | Carry forward "once per Run, on approach". The quantity is now the delivered impulse measure. |
| L2 | Effect results applied after the ordinary contact response; collider changes committed together after it | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L50` | Do not carry forward the timing: the breaking substep is re-solved with the fragments in place (coordinator decision 16). Carry forward that the change is atomic. |
| L3 | Effect state is captured and restored with the step | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L52-L63` | Carry forward the transactional rule. Do not carry forward the callback class: an element-owned hook is not declaration data. |
| L4 | Clear motion without contact activates nothing | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264` | Carry forward as the missed-contact control |
| L5 | Persistent contact does not retrigger; release and recontact does | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | Carry forward: a bowl resting on the bench is not re-judged each tick |

**Files harvested:** `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

Requirement row: [element-084](../requirements.md#element-084). Planned class `FishBowlTests`. Every row starts with the `toy_fish` that comes with the bowl, resting on its floor at its seat pose (owner decision 24b), unless it says otherwise; a row that needs another fish pose authors it directly, as the native tests place balls (`PayloadInstanceTests.Place`, `CuriousContraptions.tests/PayloadInstanceTests.cs:30`). "Inside" means the fish centre is within 0.31 m of the bowl's axis and above its floor. "Broken" is the committed observer phase and one `IntegrityBroken` record.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| F1 | Given a Wall shelf standing on the bench, 6.0 m high (the Wall's maximum height), 2 m wide along X and 1 m thick, with the bowl on its top, the bowl's axis 0.10 m inside the shelf's +X edge, and a Basketball on the shelf rolling into it along +X at 1 m/s, when it falls, then it breaks on landing at any attitude, seven shards appear and the fish is free | `AFallFromAShelfBreaksTheBowlAndFreesTheFish` | the same push on the bench: unbroken, fish inside (0.34–0.42 N·s against 1.5) | broken within a 10 s guard; worst-case landing impulse 1.62 N·s against 1.5 (behaviour row 2: top rim first, m_eff 0.153 kg, approach 10.76 − 2.6 m/s). The first shove moves the bowl only 0.08–0.10 m (it leaves at 1.165 ÷ (1 + m_eff) = 0.74–0.82 m/s and slides at 0.35 × 9.81 = 3.4 m/s²); the rolling Basketball's re-strikes carry its centre of mass past the edge |
| F2 | Given a drop from 0.10 m, then it holds; from 0.40 m, it breaks | `TheBreakFollowsTheLandingImpulse` (theory) | — | 1.09 and 2.18 N·s against 1.5 N·s; heights chosen clear of the 0.19 m threshold |
| F3 | Given a Bowling ball rolled into it at 1.5 m/s, then it holds; at 3.5 m/s, it breaks | `AHeavyBallBreaksItOnlyWhenFastEnough` (theory) | a Basketball at 2.0 m/s holds | 0.70–0.82 and 1.64–1.91 N·s against 1.5 N·s (behaviour row 3's effective-mass range: 3.5 m/s keeps a 9 % margin even on the free-body 0.508 kg); 0.69–0.85 N·s for the control |
| F4 | Given any holding row, then the fish is still inside 5 s later | `ASubthresholdImpactRetainsTheFish` | F1 | — |
| F5 | Given the bowl resting on the bench for 10 s with the fish settling, then it is unbroken | `RestingDoesNotBreakIt` | — | — |
| F6 | After F3's break: none of the bowl's colliders takes part, exactly seven shards and one fish do, and the shards' masses sum to 0.6 kg | `TheBreakReplacesTheBowlWithItsShards` | before the break the committed read lists the bowl and no shard | counts and declared masses are exact |
| F7 | Given F3's break, then the Bowling ball keeps moving forward through where the wall stood and the bodies' total momentum along its path is unchanged across the break tick | `TheBreakingBlowActsOnTheShards` | at 1.5 m/s the bowl stays whole and is pushed ahead: no `IntegrityBroken` record, and the ball's centre stays at least 0.6 m behind the bowl's axis (0.28 + 0.35 less slop) until both stop. Hand check: after the hit the ball keeps 1.5 × (4 − 0.042 × m_eff) ÷ (4 + m_eff) = 1.30–1.32 m/s and the bowl leaves at 1.5 × 4 × 1.042 ÷ (4 + m_eff) = 1.36–1.39 m/s; the ball's retained spin restores rolling at about 1.36 m/s, so it keeps pressing the bowl along until both stop, and a false break or a missing wall would let its centre come within 0.6 m | ball still moving forward 0.2 s after the break; momentum within 5 % |
| F8 | Given a broken bowl, a second hard impact on a shard publishes nothing more | `ABrokenBowlBreaksOnlyOnce` | F1 | one record per Run |
| F9 | Reset restores the whole bowl with the fish inside and no shards; save and load keep its pose | `ResetRestoresTheBowlAndFish` | — | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`T/CatalogueIntegrationTests.cs:273`) |
| F10 | A connection naming this portless part is refused | `AnyWireIsRefused` | — | typed refusal; no parameters, so no parameter refusal applies |
| F11 | Given a level whose goal is an integrity break of the bowl, then breaking it solves the level | `BreakingTheBowlCanBeAGoal` | unbroken: not solved | existing goal kind |

### Integration

- Collaborators: CAT-001 Basketball, CAT-014 Bowling ball, CAT-064 Tennis ball, CAT-066 Wall as a shelf (all delivered); EL-088 shares the break (its glowing fish is its own companion package).
- Campaign: chapter 10, levels 91–100, "fragile-target roles" (`docs/planning/requirements.md:2295`); coverage ledger "fragile fish-bowl/tank target", reuse 136–150 (`:2411`). A level that must keep the bowl whole needs a forbidden-event goal from the goal engine slices.
- A level that fixes the bowl on a shelf needs the lesson-fixture story (S8).

### Delivery

- **Shape.** Single element slice that delivers the main package and its companion `toy_fish`, each with its own acceptance rows.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS (the observer), ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-CAPABILITY-INSTANCES; then ENGINE-FRACTURE; then S7 (which also admits the companion package and the fragments and adds the not-placeable declaration with the parent's seat pose, owner decision 24b).
- **Names.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/FishBowl/` and `elements/ToyFish/` (project, declaration, scene, `icon.svg` each); `CuriousContraptions.tests/FishBowlTests.cs`; two `CuriousContraptions.slnx` lines; the coverage binding once S7 admits it.
- **Review tier.** Engine tier.
- **Disqualifiers the probe re-checks.** A fracture measure set that cannot take a contact-impulse member; fragments that cannot be declared with poses and masses; a bowl placement that does not put its `toy_fish` at the seat pose inside the bowl's Tube (rest placement lifts a Dynamic part only onto Static tops, `engine/PlacementPose.cs`, and the bowl is dynamic, so the fish's pose must come from the bowl's declared seat, not from rest placement); a Wall shelf whose 6.0 m top cannot carry the bowl near its edge; a landing impulse that the solver spreads over several substeps so that 2.7 N·s never exceeds 1.5 N·s in one (the measure resets each accepted substep, `A/ContactIntegrity.cs:12`).

## 6. Open questions

All resolved.

1. **The fish is a passive payload**, a loose toy ball, not a locomoting character (D19). It is a companion package of this identity (owner decision 15), placed with the bowl at its seat pose and never by itself (owner decision 24b), with S7 as its one owner for admission and the not-placeable declaration.
2. **Dry bowl** (owner decision 11, 22 November 2026).
3. **Seven shards**: the floor disc and six flat plates, the fewest plates 0.02 m thick that fit inside the 0.04 m wall (a longer plate's corners would leave it) (D1).
4. **Threshold is an impulse (1.5 N·s)**, the delivered measure, with no parameter (D13). It is one more member of ENGINE-FRACTURE's declared measure set, not a second kind of limit.
5. **The break re-solves its own substep** with the shards in place (coordinator decision 16); nothing is left standing where the bowl was.
6. **Category `Goals`**: the enum has no character category, and the bowl's campaign role is a fragile target.

7. **The fish is not placeable** (owner decision 24b, 23 November 2026, answering the question raised in the second-audit repair). A companion package can be declared not placeable: it has no palette entry and appears only with its parent. `toy_fish` is so declared and comes with each bowl at the bowl's seat pose; the shards, as fracture fragments, likewise have no palette entry and appear only from their parent's break. S7 adds the capability with the companion admission. The same decision covers EL-088's `glow_fish` and EL-078's `sealed_can`.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Two colliders on one body, seven declared fragments, one threshold; the loose fish is a one-sphere companion package. No callback, script or engine branch on this package.
2. **Generic laws.** Contact, friction and restitution for containment; the contact-impulse observation for the break; the general break-to-fragments effect of ENGINE-FRACTURE.
3. **Three layers.** Colliders: one tube, two cylinders (bowl floor, floor shard), one sphere, six boxes. Visuals: one mesh per collider. Animation: a crack flash from the break event and visibility from the committed-broken state; both read committed state only. Every mesh follows a body pose.
4. **Flow and typing.** Physics → event → presentation and goal evaluation. The event kind is an enum; nothing returns.
5. **Flat state.** `fish_bowl`: bodies 8 fixed at compile time (bowl and seven shards, the shards taking no part until the break); colliders 9 (1 tube, 2 cylinders, 6 boxes); joints 0; drives 0; sensors 1 integrity observer over 2 colliders; signal and supply nodes 0; stores 0; presentation bindings 8 body, 8 visibility, 1 event cue. `toy_fish`: bodies 1; colliders 1 sphere; presentation bindings 1 body; nothing else.
6. **Performance.** The wall is one hollow primitive, not a ring of boxes; the six plates exist only after a break, and their count is the least that fits the wall (section 6). The observer's rows are found by handle under ENGINE-CONTACT-OBSERVATIONS, with no per-contact scan. No sleeping exists today; ENGINE-ISLANDS-SLEEP will let a resting bowl and fish sleep, and nothing here depends on it. A fast free fish relies on the engine's general continuous-collision means.
7. **Tests are scenarios.** Every row drops, rolls or rests real bodies in the production simulation.

**Verdict: conforms.** Not deliverable until the engine programme and ENGINE-FRACTURE exist; each missing capability has one owning story.

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here because the reader fails the whole coverage run on a spec whose fields do not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `fish_bowl` (with its seven fracture fragments); companion `toy_fish` (S7 widens the reader to admit companion packages, owner decision 15) |
| Named identity | EL-084, `../invest/named-elements.md#element-084` |
| Acceptance | `todo-200` and `sequence-task-637` (`docs/planning/requirements.md:5069`) |
| Decision | S654; `scope-corrections.md` has no `s654` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: a decision block for S654; `todo-200` is a scope index shared by EL-082 to EL-085 (`docs/planning/requirements.md:4091`), and its sequence task sits beside the element row, not directly after the todo anchor as the reader requires; the reader does not yet admit companion content.
