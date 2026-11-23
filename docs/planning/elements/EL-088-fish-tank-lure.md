# EL-088 · Fish tank lure — named-identity spec

**Current delivery (24 October 2026):** [EL-088 delivery slice](../invest/named-elements.md#element-088). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `T/` abbreviates `CuriousContraptions.tests/` and `A/` abbreviates `CuriousContraptions.Abstractions/`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32. Nothing here was built or simulated; every number is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-088 · Fish tank lure · Character |
| Anchor | [requirements.md#element-088](../requirements.md#element-088); [named entry](../invest/named-elements.md#element-088); scope index [todo-221](../requirements.md#todo-221) |
| Proof owner | S658 |
| Disposition | To deliver as a single element, after [EL-084](EL-084-fragile-fish-bowl.md), whose break it reuses. Not a family member or duplicate of EL-084: this tank is static and rectangular, its panes break one at a time, and its fish is a different companion package that carries its own light. |
| Planned packages | main `fish_tank`, directory `elements/FishTank/`, class `FishTankElementDeclaration`, with four declared fracture fragments (the panes, ENGINE-FRACTURE); companion package `glow_fish`, directory `elements/GlowFish/`, class `GlowFishElementDeclaration` (owner decision 15; not a new ledger identity) |
| Refines / extends | No CAT spec |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-OPTICAL and ENGINE-FRACTURE, then S7 |

## 2. Declaration

**What the part is.** A fixed, dry glass tank (owner decision 11) with a glowing toy fish lying in it. The fish carries a declared optical emitter of green and blue light, paid from its own finite source: it is a companion package of this identity (owner decision 15) holding the emitter, the source and the five parameters, so no supply edge crosses packages. Nothing is notified of anything: a character is drawn to the tank only if that light reaches its eye, so an absorbing body in between stops it and the transmitting glass does not. The fish is a real loose ball held in by the glass. A hard enough knock on a pane breaks that pane, which becomes a free body. No script, callback, perception code or per-part break code.

Interfaces of `fish_tank`: `IContactIntegrityElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:119`) in the form ENGINE-CONTACT-OBSERVATIONS leaves it; the break effect and fragments of ENGINE-FRACTURE; `ISceneGeometryElement` (`A/SceneGeometry.cs:10`). `KinematicMode.Static`; `SupportedDomains = Mechanical`. Interfaces of `glow_fish`: the emitter contract of ENGINE-OPTICAL; `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`); `ISceneGeometryElement`. `KinematicMode.Dynamic`; `SupportedDomains = Mechanical | Electrical` and the optical domain once it exists.

### Bodies and colliders

Local frame: origin at the centre of the base's underside, +Y up, long side along X.

| Body | Colliders (m) | Mass and inertia | Contact material | Optical response |
| --- | --- | --- | --- | --- |
| 0 tank (static base) | index 0 floor: box half extents (0.50, 0.03, 0.30) at (0, 0.03, 0); indices 1–2 long panes: boxes half extents (0.46, 0.30, 0.02) at (0, 0.36, ±0.28); indices 3–4 end panes: boxes half extents (0.02, 0.30, 0.30) at (±0.48, 0.36, 0) | static | floor: restitution 0, friction 0.3, bounce threshold 0.1 m/s; panes: restitution 0.3, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s | floor: absorb; panes: transmit every channel |
| four pane fragments (fracture fragments, each absent until its pane breaks) | each is its pane's own box at its pane's pose | long panes 0.46 kg, end panes 0.30 kg (by volume, 0.0221 and 0.0144 m³, at the 0.5 kg ÷ 0.024 m³ = 20.8 kg/m³ glass density of the earlier draft); box moments from m/12·(b² + c²) | glass, as the panes | transmit every channel |

Geometry check: outside 1.00 × 0.66 × 0.60 m; the panes stand on the floor (y 0.06 to 0.66) and meet at the corners without overlap: the end panes cap the long panes (long panes span x ±0.46 between the end panes' inner faces; end panes span z ±0.30, the full depth). So a long pane's fragment, pushed inward along Z, slides between the end panes' inner faces with nothing in its path, which is why T6 strikes a long pane; an end pane's fragment pushed inward along X meets the long panes' ends (behaviour row 5). The interior is 0.92 × 0.52 m; the fish rests at y 0.14 with 0.76 × 0.36 m of floor for its centre to roll on. Each fragment is exactly the collider it replaces, so it lies within that collider's volume and cannot start inside the fish.

### `glow_fish` (companion package): body, emitter and supply

A dynamic one-body part: not a second body of `fish_tank` (a package's further bodies come only from its sliders and hinges, `CuriousContraptions.Bundle/BundleValidator.cs:616`–`:619`) and not a fracture fragment (it exists from Run start). It is a companion package of this identity (owner decision 15), admitted to coverage by S7, with its own catalogue entry. It is declared **not placeable** (owner decision 24b, 23 November 2026): it has no palette entry and appears only with its parent. Each `fish_tank` instance carries one `glow_fish`, placed with the tank at a seat pose the tank declares, resting on the tank's floor with its centre at the tank's (0, 0.14, 0), and removed with it; it is never placed by itself. It stays on its seat (the player cannot move it off), but the player may delete it alone in Build and restore it from its tank, and a level may place a tank without it; Reset restores the authored state either way (coordinator decisions 33 and 34). Its parameters are edited on the fish as on any part. The not-placeable declaration and the parent's seat pose are the capability S7 adds with the companion admission (owner decision 24b).

| Body | Collider | Mass and inertia | Contact material | Optical response |
| --- | --- | --- | --- | --- |
| 0 fish | sphere r 0.08 at the origin | 0.05 kg; 2/5·m·r² = 0.00013 kg·m² | restitution 0.2, friction 0.4, rolling resistance 0.05, bounce threshold 0.1 m/s | transmit every channel |

- **Emitter** on body 0, at its centre: all directions (Ω = 4π sr), radiant power 8 W **Green** and 8 W **Blue**, range bound to the parameter `reach`, electrical demand 16 W. Radiant intensity per channel I = 8 ÷ 4π = 0.637 W/sr. Conventions as [Batch K](readiness/batch-k.md): emitted power is the radiant power times the granted share of the demand, so a spent or disabled source gives no light (owner decision 9 and coordinator decision 16).
- **Supply nodes**: `source` (`SupplySourceDescriptor`, `A/SupplyContractValues.cs:39`) and consumer `glow` (target: the emitter); edge `source.Output → glow.PowerIn`, the internal-edge shape of `T/ComposableSupplyRoutingTests.cs:499`. Electrical rating (read by nothing): `glow` 12 V, 9 Ω, 16 W.

Green lures EL-082; blue draws EL-087. Because the emitter rides the fish, the light stays with the fish if a pane breaks and the fish rolls out.

### Break declaration

- **Observation** (ENGINE-CONTACT-OBSERVATIONS, audit E6): four integrity observers, one per pane collider (indices 1 to 4), each with threshold **16 N·s** and minimum approach speed 0.1 m/s; no gas store. The measure is the delivered one: approaching normal impulse summed over the contact points of that pane against one other collider in one substep, strictly greater than the threshold (`A/ContactIntegrity.cs:17`–`:39`).
- **Effect** (ENGINE-FRACTURE, contact impulse as one member of its declared measure set): a pane's occurrence removes that pane's collider and starts its fragment in its place; the substep is re-solved with the fragment there (coordinator decision 16), so the blow that broke the pane knocks the fragment away and no pane is left standing. One `SimulationEventKind.IntegrityBroken` record per broken pane (`A/EventDescriptor.cs:33`), each pane at most once per Run. The floor never breaks. Reset restores every pane.

### Ports

None on either package. The stimulus is light, never a wire.

### Parameters and store (on `glow_fish`)

| Key | Kind, unit | Min | Max | Default | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- |
| `reach` | Real, Metre | 1 | 8 | 4 | Reject | the emitter's range: about a quarter of the 16.8 m bench; a level tunes it |
| `capacity` | Real, Joule | 240 | 3840 | 960 | Reject | 16 W for a 30 s Run is 480 J |
| `power_limit` | Real, Watt | 20 | 60 | 30 | Reject | above the 16 W demand; 30 W is 0.25 J per tick, under the 1 J per tick supply bound |
| `initial_charge` | Real, none | 0 | 1 | 1 | Reject | 0 gives a dark tank |
| `enabled` | Choice `Disabled`, `Enabled` | 0 | 1 | 1 | Reject | Build-only; `Disabled` is the "no stimulus" control |

### Metadata and bindings

Category `Goals`, display name "Fish tank", palette order 204 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `#66b8c9`. Presentation: each `Visual/Pane*` is static until its pane breaks and then follows its fragment body; a crack flash on `PresentationDriver.Event` for `IntegrityBroken`. `glow_fish`: display name "Glowing fish", colour gold `#f7cb52`; no palette entry, so no category or palette order (owner decision 24b; today every package declares a positive palette order, `A/ElementMetadata.cs:16`, so the not-placeable form is S7's); `Visual/Fish` follows its body 0, and its glow is drawn by the optical story's shared renderer.

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Base` | navy `#293954` slab 1.00 × 0.06 × 0.60 | solid, box collider; static |
| `Visual/PaneFront`, `PaneBack`, `PaneLeft`, `PaneRight` | transparent cyan `#66b8c9` glass at low alpha, with cream `#fff8e9` edge tint inside each pane's thickness | solid, box colliders; static, then following their fragment bodies |
| `glow_fish` `Visual/Fish` | clear gold-tinted `#f7cb52` ball r 0.08 with navy printed eye and fins | solid, sphere collider; follows the fish package's body 0 |

No water is drawn. Icon: a rectangular tank on a dark base with a fish. It differs from EL-084 by its box shape and base slab.

### Behaviour and numbers

The engine multiplies the two restitutions of a contact (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`).

1. **Stimulus (optical law).** A receiver of aperture A reads A·I·cos φ ÷ d² from the fish beyond 1 m, and flat A·I ÷ 1 m² inside 1 m with the fish in front of its aperture (optical convention 1's one near-field rule), while it is within `reach` and the straight segment between them crosses no absorbing solid. For an EL-082 eye (A 0.00785 m², threshold 40 µW) at 3 m on axis that is 5.6 × 10⁻⁴ W, fourteen times the threshold, so the range, not the threshold, ends the lure up to 8 m. The fish is at 0.14 m and the walkers' eyes at 0.49 and 0.66 m, so the segment leaves through a pane (0.06 to 0.66 m).
2. **Designed interplay.** The light is ordinary green and blue. A colour receiver of the optical catalogue (CAT-038 family, planned; 0.95 m² disc) within 1 m reads at most 0.95 × 0.637 = 0.61 W, so only one set below 0.61 W (its range is 0.5 to 20 W, default 2.5 W) and standing within about a metre is tripped by the fish.
3. **Containment (contact law).** The panes are 0.60 m high around a 0.16 m ball. A Basketball (restitution 0.55, so e = 0.165) rolled into a pane at 3 m/s delivers 1 × 3 × 1.165 = 3.5 N·s and bounces off.
4. **Break.** Against a static pane the impulse is m·(1 + e)·v. Bowling ball (4 kg, restitution 0.14, e = 0.042): 4.17·v, so it breaks a pane above 3.84 m/s, which a ball reaches rolling down 1.05 m (v² = 10/7·g·h). At 3.0 m/s it gives 12.5 N·s and the pane holds; at 4.8 m/s, 20.0 N·s and it breaks. A Basketball at the Bumper's 8 m/s gives 9.3 N·s and does not. An EL-087 ramming at 1.2 m/s gives at most 3.7 N·s. A character pressing on the glass is not approaching and counts for nothing (`T/ContactIntegrityTests.cs:44`).
5. **The broken pane is knocked away.** For T6's long pane: re-solved against its 0.46 kg fragment (pair restitution 0.14 × 0.3 = 0.042), a 4.8 m/s Bowling ball sends the fragment inward at 1.042 × 4 ÷ 4.46 × 4.8 = 4.49 m/s and keeps (4 − 0.042 × 0.46) ÷ 4.46 × 4.8 = 4.28 m/s itself. The fragment slides between the end panes' inner faces on the floor slab (friction √(0.4 × 0.3) = 0.35, 3.4 m/s²), so it is still above 4 m/s 0.1 s later unless it has met the fish (which takes at most about a tenth of its speed). The ball must then climb the floor slab's 0.06 m edge (it meets that edge with its centre at z = 0.47, after the pane at z = 0.58): the slab's restitution is 0, so it continues at between 0.62 × 4.28 = 2.6 m/s (the edge's normal part removed) and 0.85 × 4.28 = 3.6 m/s (rolling over the edge without slip). The fragment runs ahead and meets the back pane first (0.46 × 1.09 × about 4 = 2 N·s); pressed through the fragment, the back pane then takes about (4 + 0.46) × 1.04 × (2.6 to 3.6) = 12 to 17 N·s against 16, so whether it breaks is not asserted. An end pane struck from outside above the threshold also breaks, but its fragment, pushed inward along X, meets the long panes' ends: the re-solved blow is carried by those panes (about 10 N·s each from a 4.8 m/s Bowling ball, under 16) and the fragment stays in place until a later contact moves it. No row strikes an end pane from outside (section 6, item 8).
6. **After T6's break** the end panes and the floor still stand (the 0.56 m ball passes 0.46 − 0.28 = 0.18 m clear of each end pane's inner face), the fish can roll out through the gap, and it still glows.
7. **Tick order.** The optical solve runs after the last substep of a tick and its readings act from the next tick (coordinator decision 16). A break is decided and re-solved inside the substep in which the impulse occurred.

## 3. Engine capabilities

| Capability | Status | Evidence |
| --- | --- | --- |
| Pairwise approaching-impulse threshold, strict, resting contact excluded | present | `ContactIntegrityTests.ThresholdIsStrictWithoutHiddenTolerance` and `RestingSupportAndApproachAtTheThresholdDoNotQualify` (`T/ContactIntegrityTests.cs:40`, `:44`) |
| A package-internal source feeding a consumer of the same package | present | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`T/ComposableSupplyRoutingTests.cs:530`) |
| Integrity observer | narrower | today it names one collider and one gas store, whose outlet is all it opens (`A/GasCouplingValues.cs:15`; `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie`, `T/BalloonTests.cs:118`). Owner: **ENGINE-CONTACT-OBSERVATIONS** (no gas store; a one-way occurrence consumed by a declared effect; several observers on one static body) |
| Break effect on one collider of a static body: remove it, start its declared fragment in its volume, re-solve the breaking substep, restore on Reset | missing | **ENGINE-FRACTURE** |
| Declared fragment bodies, each taking part only from its pane's break; no palette entry, appearing only with their parent (owner decision 24b) | missing | **ENGINE-FRACTURE** (owner decision 15 lets fragments be companion content); S7 admits them in coverage |
| The fish: a companion package of this identity, one dynamic sphere with its own source, consumer and emitter, present from Run start, declared not placeable and placed with each tank at the tank's declared seat pose (owner decision 24b) | present as a package shape (a one-body dynamic package with a package-internal supply edge); missing: a package with no palette entry (every package declares a positive palette order today, `A/ElementMetadata.cs:16`) placed with its parent at a seat pose the parent declares | owner decision 15 makes it a companion package and owner decision 24b not placeable; its admission and the not-placeable declaration are **S7**'s (its one owner). It needs nothing from ENGINE-FRACTURE, and it cannot be a second body of `fish_tank` (`BundleValidator.cs:616`–`:619` admits further bodies only through sliders and hinges); its emitter is the next row's |
| Optical emitter on a dynamic package's body, several channels, parameter-bound range, paid from a supply consumer of the same package; per-collider surface response | missing | **ENGINE-OPTICAL** (`engine-optical-propagation`) |
| Occlusion candidates without a scene scan | missing | **ENGINE-SPATIAL-QUERIES** (owner decision 3a) |
| The emitter's paid energy debits the source; an exhausted source gives no light | missing | **ENGINE-SUPPLY-DEMAND** (owner decision 3a) |

## 4. Sources and legacy

- Requirement: "Fragile enclosure presents declared sensory stimulus while containing its payload"; outcome "Occlusion affects perception and intact walls retain contents" ([element-088](../requirements.md#element-088)).
- [S652](../invest/scope-corrections.md#s652): a missing lure leaves the character idle. Research reference: TIM2 manual ([todo-221](../requirements.md#todo-221)).
- The element map's "lure data participates in shared perception … no unconditional target notification" is met by the emitter and the optical law; there is no perception table.
- Legacy search at `a6c914e` for "fish", "tank", "aquarium" and "lure" found no source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Clear pipe shell passes light; opaque collars block it | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as "glass transmits, absorbing solids block", through ENGINE-OPTICAL |
| L2 | Visibility is a zero-thickness segment that is not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule. Do not carry forward the CPU sweep. |
| L3 | Effect results applied after the ordinary contact response; collider changes committed together after it | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L50` | Do not carry forward the timing: the breaking substep is re-solved with the fragment in place (coordinator decision 16). Carry forward that the change is atomic. |
| L4 | Effect state is captured and restored with the step | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L52-L63` | Carry forward the transactional rule. Do not carry forward the callback class. |
| L5 | Clear motion without contact activates nothing; persistent contact does not retrigger | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264`, `:L303-L315` | Carry forward: a character pressing on the glass is not a new impact |

**Files harvested:** `CuriousContraptions.tests/PipeTests.cs` (occlusion test only), `engine/physics/SegmentOcclusionSweep.cs`, `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

Requirement row: [element-088](../requirements.md#element-088). Planned class `FishTankTests`. Every row starts with the `glow_fish` that comes with the tank, resting on its floor at its seat pose (owner decision 24b), unless it says otherwise; a row that needs another fish pose authors it directly, as the native tests place balls (`PayloadInstanceTests.Place`, `T/PayloadInstanceTests.cs:30`); the parameters named are the fish's. "Inside" means the fish centre is within the panes' inner faces and above the floor. "Stays" for a character means under 0.05 m over 5 s.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| T1 | Given an EL-082 3 m from an end pane and facing it, when run, then it rolls to the tank and rests against the glass; no pane is broken and the fish is inside | `TheFishDrawsACharacterThroughTheGlass` | no tank: the character stays | within a 15 s guard |
| T2 | Given an opaque Wall between them, then the character stays | `AnOpaqueBodyHidesTheLure` | T1 | — |
| T3 | Given `reach` 2 with the character 3 m away, then it stays; `reach` 4, it comes | `TheLureIsSeenOnlyWithinItsReach` (theory) | — | 1 m either side of the distance |
| T4 | Given `enabled` Disabled or `initial_charge` 0, then the character stays | `ADarkTankLuresNothing` (theory) | T1 | — |
| T5 | Given a Basketball rolled into a pane at 3 m/s, then it rebounds, no pane breaks and the fish is inside | `IntactGlassKeepsTheFishIn` | T6's breaking row | 3.5 N·s against 16 N·s |
| T6 | Given a Bowling ball rolled along −Z into the centre of the front long pane (z = +0.28) at 3.0 m/s, then the pane holds; at 4.8 m/s that pane becomes a free body moving away from the ball, one `IntegrityBroken` record names it, and the floor and both end panes stay | `AHeavyFastBallBreaksThePaneItHits` (theory) | the 3.0 m/s row | 4 × 1.042 × v = 12.5 and 20.0 N·s against a strict 16 N·s; the fragment moves faster than 1 m/s within 0.1 s (it leaves at 4.49 m/s, behaviour row 5); the back pane is not asserted |
| T7 | Given T6's break, then the ball keeps moving forward across the front pane's old line (z = 0.26 m) | `NoPaneIsLeftStanding` | at 3.0 m/s the ball's centre stays at z ≥ 0.57 m (its radius outside the pane's outer face at 0.30, less slop) | ball centre past z = 0.26 within 0.5 s of the break (0.32 m at no less than 2.6 m/s, about 0.1 s) |
| T8 | After T6's break, the fish can leave through the gap, and an EL-082 in view of the fish still comes to it | `TheLureStaysWithTheFish` | — | — |
| T9 | Given an EL-087 3 m away, then it comes, presses on the glass, and no pane is broken after 10 s | `APressingCharacterDoesNotBreakTheGlass` | T6 | shared with EL-087 row P4 |
| T10 | Reset restores every pane and the fish inside; save and load keep the fish's five parameters | `ResetAndSaveLoadRestoreTheTank` | — | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`T/CatalogueIntegrationTests.cs:273`) |
| T11 | A `reach` of 0.5 or 9, or a `capacity` of 100 J, is refused; a connection naming either portless part is refused | `OutOfRangeValuesAndAnyWireAreRefused` | defaults accepted | typed refusal, nothing changed |
| T12 | Given a 10 s run, then the source's debit equals the emitter's paid energy; given `initial_charge` 0.05 (48 J), then the light goes out after about 3 s and a character rolling toward it coasts to a stop | `TheSourcePaysForTheLightAndASpentTankGoesDark` | full charge: still lit at 10 s | debit never below the paid energy and above it by no more than the ledger residual; dark before 6 s; the route reads open |

### Integration

- Collaborators: EL-082 (lured by its green), EL-087 (drawn by its blue through the glass), CAT-066 Wall (occluder), CAT-001 Basketball and CAT-014 Bowling ball (delivered); the colour receivers of the optical catalogue read it under the limits in behaviour row 2.
- Campaign: chapter 10, levels 91–100, "walker/lure/predator/fragile-target roles" (`docs/planning/requirements.md:2295`); coverage ledger "fragile fish-bowl/tank target", reuse 136–150 (`:2411`).
- A level that fixes the tank needs the lesson-fixture story (S8).

### Delivery

- **Shape.** Single element slice that delivers the main package and its companion `glow_fish`, after EL-084 and after the "Sighted walkers" family (rows T1 to T4, T8 and T9 need a character).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS (the observers), ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-CAPABILITY-INSTANCES; then ENGINE-OPTICAL and ENGINE-FRACTURE; EL-084; EL-082 and EL-087; then S7 (which also admits the companion package and the fragments and adds the not-placeable declaration with the parent's seat pose, owner decision 24b).
- **Names.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/FishTank/` and `elements/GlowFish/` (project, declaration, scene, `icon.svg` each); `CuriousContraptions.tests/FishTankTests.cs`; two `CuriousContraptions.slnx` lines; the coverage binding once S7 admits it.
- **Review tier.** Engine tier.
- **Disqualifiers the probe re-checks.** An emitter that cannot ride a dynamic package's body or be paid from that package's own source; a tank placement that does not put its `glow_fish` at the seat pose on the tank's floor slab between the panes; a surface response that is per body rather than per collider (the floor must absorb); a break that cannot be limited to one collider of a static body; a pane impulse that the solver spreads over several substeps (`A/ContactIntegrity.cs:12`).

## 6. Open questions

All resolved.

1. **Static tank** (D19): it is a fixed lure; only its glass breaks.
2. **Stimulus.** Ordinary green and blue light (coordinator decision 16); no scent. Its interplay with lamps and colour receivers is designed, within the stated limits.
3. **The light is paid for.** The optical law lets no emitter give more than its source grants, so the fish has its own finite source; `enabled` Disabled is the "no stimulus" control that the earlier `stimulus_enabled` switch provided.
4. **No cover accessory.** A Wall is the occluder.
5. **Dry tank** (owner decision 11, 22 November 2026).
6. **`reach` is the fish's parameter**, not the characters': range is a property of the emitter, which rides the `glow_fish` companion package.
7. **Panes break one at a time**, each at 16 N·s, fixed (D13); a struck long pane is knocked away by the re-solved blow (coordinator decision 16).
8. **One pair of panes is capped.** In a box of four butted panes one pair's fragments are always barred from moving inward by the other pair's ends; here the end panes cap the long panes, so the long panes' fragments are free (T6) and an end pane broken from outside stays in place until something moves it (behaviour row 5). No acceptance row depends on an end pane breaking.

9. **The fish is not placeable** (owner decision 24b, 23 November 2026, answering the question raised in the second-audit repair; stated once in [EL-084](EL-084-fragile-fish-bowl.md) section 6 item 7). `glow_fish` has no palette entry and comes with each tank at the tank's seat pose; the pane fragments likewise appear only from their parent's break. S7 adds the capability with the companion admission.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** `fish_tank`: five static boxes, four declared fragments, four thresholds. `glow_fish`: one sphere, one emitter, a two-node supply graph, one store, five parameters. No callback, notification, perception table or engine branch on this package.
2. **Generic laws.** Optical emission, transmission and occlusion (ENGINE-OPTICAL); the supply rule paying the emitter; contact and restitution for containment; the contact-impulse observation and the break-to-fragment effect (ENGINE-FRACTURE).
3. **Three layers.** Colliders: nine boxes (five static, four fragment) and one sphere, each with a declared optical response. Visuals: one mesh per static collider, reused for its fragment. Animation: a crack flash from the break event. Every mesh follows a body pose or is static.
4. **Flow and typing.** Supply → emitter → optics → other parts' observations; physics → event → presentation. Channels and the event kind are enums; nothing returns.
5. **Flat state.** `fish_tank`: bodies 5 fixed at compile time (static tank, four pane fragments taking no part until their breaks); colliders 9 boxes; joints 0; drives 0; sensors 4 integrity observers; emitters 0; signal and supply nodes 0; stores 0; presentation bindings 4 body, 1 event cue. `glow_fish`: bodies 1; colliders 1 sphere; emitters 1; supply nodes 2; stores 1; presentation bindings 1 body.
6. **Performance.** Five static boxes; the fragments reuse the panes' shapes. The emitter has a bounded range, and receivers find it through ENGINE-SPATIAL-QUERIES, not a scan. The observers' rows are found by handle under ENGINE-CONTACT-OBSERVATIONS. No sleeping exists today; ENGINE-ISLANDS-SLEEP will let the resting fish sleep, and nothing here depends on it.
7. **Tests are scenarios.** Every row places real parts in the production simulation.

**Verdict: conforms.** Not deliverable until the engine programme and the two named stories exist; each missing capability has one owning story.

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here because the reader fails the whole coverage run on a spec whose fields do not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `fish_tank` (with its four fracture fragments); companion `glow_fish` (S7 widens the reader to admit companion packages, owner decision 15) |
| Named identity | EL-088, `../invest/named-elements.md#element-088` |
| Acceptance | `todo-221` and `sequence-task-641` (`docs/planning/requirements.md:5101`) |
| Decision | S658; `scope-corrections.md` has no `s658` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: a decision block for S658; `todo-221` is a scope index shared by EL-086, EL-087 and EL-088 (`docs/planning/requirements.md:4095`), and its sequence task sits beside the element row, not directly after the todo anchor as the reader requires; the reader does not yet admit companion content.
