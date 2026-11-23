# EL-011 · Water T junction — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-011 delivery slice](../invest/named-elements.md#element-011). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-011 · Water T junction · Water |
| Anchors | [element-011](../requirements.md#element-011), co-anchored `sequence-task-564`; source record [todo-335](../requirements.md#todo-335); integration [sequence-task-385](../requirements.md#sequence-task-385); ledger [element-011](../invest/named-elements.md#element-011), owner S432 |
| Disposition | To deliver as a single element in the "Water pipe kit" run. It is the first part with three mouths |
| Planned package | id `water_tee`, directory `elements/WaterTee/`, title "Water T" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-008](EL-008-straight-water-pipe.md), [EL-012](EL-012-water-pipe-cap.md), [EL-020](EL-020-water-diverter.md) (selects one branch instead of sharing), EL-030 |

## Declaration

Closest delivered shape: the Clear pipe (one Tube, two mouths); the third mouth has no delivered pattern.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting declaration of `ENGINE-LIQUID` (W4, W7).
- **Body.** One static body, mass 0. Run along local X, branch along +Y; origin at the centre of the run.
- **Colliders (two Tubes).** Run: bore 0.10, outer radius 0.14, length 0.60, axis X. Branch: same section, from y = 0.14 to y = 0.30 (length 0.16), axis Y.
- **Liquid node.** One fitting, zero volume, no seat, with three mouths.
- **Mouths (pipe bore, Bidirectional).** `Start` at (−0.30, 0, 0), normal −X; `End` at (+0.30, 0, 0), normal +X; `Branch` at (0, 0.30, 0), normal +Y. Any may be the inlet; it splits or merges.
- **Parameters.** None. Throttling a branch is a Tap's job.
- **Presentation.** None driven. **Palette.** `ElementCategory.Structure`; palette order 525 (kit sequence in EL-008; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Run` | Tube 0.60 long, cream `#fff8e9` | solid, hollow; the run Tube collider | body pose (static) |
| `Visual/Stub` | Tube 0.16 long on top of the run, cream | solid, hollow; the branch Tube collider | body pose |
| `Visual/Bands` | Navy `#293954` bands at the three ends, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end`, `Loc_Port_mouth_branch` | Locators, bore 0.10 | none | nothing |

Icon: a cream T. It differs from the Water diverter (a Y with a lever) by its right-angle stub and no lever.

### Behaviour and numbers

The T is one fitting: it has one head, and each opening that leaves it obeys L7. Nothing is copied; the flows at the fitting sum to zero.

- **Equal outlets.** Full tank into `Start`; `End` and `Branch` both lead to free ends at the same height as the tank floor. The tank mouth join (K₀) is in series with two free ends in parallel (2·K₀): 1/K² = 1/K₀² + 1/(2K₀)², K = 0.8944·K₀ = **0.07468**. At 1.0 m depth the tank gives 0.0747 m³/s and each outlet 0.0373 m³/s.
- **One branch capped.** A single route: K = K₀/√2 = 0.05904, 0.0590 m³/s. Opening the second branch raises the total by 26%, never by 100%, and the total (0.0747) stays below what the tank mouth alone could pass (0.0835): "branch totals cannot exceed supply".
- **Unequal outlets.** With the `Branch` route's free end 0.5 m lower than the `End` route's, and the tank surface 1.0 m above the higher one, the fitting head x (above the higher outlet) satisfies √(1 − x) = √x + √(x + 0.5), so x = 0.053 m. Flows: higher outlet K₀·√0.053 = 0.0192 m³/s, lower outlet K₀·√0.553 = 0.0621 m³/s, tank K₀·√0.947 = 0.0813 m³/s = their sum. The lower outlet receives 3.2 times more.
- **Order independence.** The split depends on heads and apertures only; which pipe was placed first cannot matter, because the network is solved from one committed state (L16).
- **Merge.** Two supplies into `Start` and `Branch`: what leaves `End` is their sum.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube colliders | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting |
| W3 tube mouths and snap | narrower | a part declares at most a Start and an End mouth today (`TubeEnd`, `CuriousContraptions.Abstractions/SceneGeometry.cs:37`; a duplicate end is refused at `CuriousContraptions.Bundle/BundleValidator.cs:443`). A third mouth socket is `ENGINE-CAPABILITY-INSTANCES`' (a declared socket list with package-local ids) |
| W7 fitting flow balance with more than two openings | missing | `ENGINE-LIQUID` |
| W8 free ends | missing | `ENGINE-LIQUID` |

Counts: 3 present, 1 untested, 1 narrower, 2 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, two colliders, one fitting, three mouths.
2. **Generic laws.** "Divides available flow among connected branches" is the zero-sum rule at a fitting with L7 on each opening. The earlier "scale every branch by α" rule is not used: it was a second allocation rule beside the head law.
3. **Three layers.** *Colliders:* two Tubes. *Visuals:* the table above. *Animation:* none.
4. **One-way flow and typing.** Mouth ids (including `Branch`) and roles are enums.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 Tubes; joints 0; drives 0; sensors 0; liquid nodes 1 fitting; mouths 3; bindings 0; parameters 0.
6. **Performance fitness.** Two colliders. A fitting with three openings is the one case that needs a head to be solved; the work is bounded per fitting by the sweep count `ENGINE-LIQUID` declares (L18), and the fitting's outflows are then scaled to its inflows, so it never creates volume (L7).
7. **Tests are scenarios.** Rows J1 to J7.

## Sources and legacy

- Requirement: "Divides available flow among connected branches"; outcome "Branch totals cannot exceed supply." No variants.
- Component research [water](../../component-research.md#water): "a split shares supply, never copies it" (line 13); "insertion order never selects a branch" (line 14); recipe Two Gardens (line 53). Refinement [S705](../invest/refinements.md#s705) tee.
- **Legacy search.** No liquid junction. Finite-supply allocation analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Several requests on one finite store are scaled by a common factor so the total never exceeds the store. | `engine/physics/StoredFlowSource.cs@a6c914e:L28-L73` | Carry forward the outcome (never more than the supply; order-independent). Not the scaling rule itself, nor the f64 CPU allocator. |
| 2 | Two requests on a 4 J store get 2 J each; reversing the order changes nothing. | `CuriousContraptions.tests/StoredFlowSourceTests.cs@a6c914e:L17-L40` | Carry forward as rows J1 and J4. |
| 3 | An empty store yields zero. | `StoredFlowSourceTests.cs@a6c914e:L42-L54` | Carry forward as row J5. |

**Files harvested:** `engine/physics/StoredFlowSource.cs`, `CuriousContraptions.tests/StoredFlowSourceTests.cs`.

## Acceptance outline

Authority: [element-011](../requirements.md#element-011). Tolerances L17. Planned class `WaterTeeTests`. Supply: a full Water tank and a pipe into `Start`; each outlet route ends over its own basin.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| J1 | Given two outlets at equal height, when Run advances 8 s, then the two basins' gains are equal within 10% and together equal the tank's loss within 0.004 m³ | `WaterTeeTests.EqualBranchesShareEqually` | J2 | — |
| J2 | Given a cap on `Branch`, then all flow leaves `End`, and the tank's loss after 8 s is less than in J1 by the ratio 0.0590 : 0.0747 within 10% | `WaterTeeTests.ACappedBranchSendsEverythingToTheOther` | J1 | — |
| J3 | Given the branch outlet 0.5 m lower, then the committed outlet flows at the first sampled tick are in the ratio 3.2 within 15% (lower to higher), and their sum equals the tank's outflow | `WaterTeeTests.TheLowerBranchReceivesMore` | J1 | — |
| J4 | Given the J1 construction rebuilt with the branch pipe placed first, then both basins' gains match J1 within 0.004 m³ | `WaterTeeTests.BuildOrderDoesNotChangeTheSplit` | — | — |
| J5 | Given an empty tank, then neither basin gains | `WaterTeeTests.AnEmptySupplySplitsNothing` | J1 | — |
| J6 | Given two half-full tanks into `Start` and `Branch` and one outlet from `End`, then the basin's gain equals the sum of both tanks' losses | `WaterTeeTests.TwoSuppliesMergeToTheirSum` | — | — |
| J7 | Given a ball dropped on the T, it rests on it; Reset restores the tanks; save and load keep the pose and all three joins | `WaterTeeTests.TheTeeIsSolidAndTheRouteSurvivesResetAndReload` | — | — |

Generic cover: W22. Chrome recipe (pending): tank, tap, pipe into a T; pipes from both outlets, each over its own basin.

## Open questions

All resolved; none needs the owner.

1. Unequal split by geometry: no; by heads and apertures only.
2. Equal-split tolerance: 10% on gains (L17), not exact equality; simulated floats are never compared for equality.

## Integration

- **Works with.** The pipe kit, EL-003, EL-020; none delivered.
- **Campaign.** Chapter 7, levels 61–70 (Two Gardens); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-335`; `sequence-task-564` not co-anchored with a todo; no `s432` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), including `ENGINE-CAPABILITY-INSTANCES` for the third mouth socket, then: `ENGINE-LIQUID`; EL-001; EL-008; EL-004; EL-012 for J2; S7.
- **Files.** Adds `elements/WaterTee/WaterTee.csproj`, `WaterTeeElementDeclaration.cs`, `water_tee.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterTeeTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only if a third mouth id is delivered; otherwise the mouth-id change is an engine slice first.
- **Disqualifiers.** Two mouths per part only; a split decided by declaration or placement order; a snap that cannot target a third mouth.
