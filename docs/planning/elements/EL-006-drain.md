# EL-006 · Drain — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-006 delivery slice](../invest/named-elements.md#element-006). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-006 · Drain · Water |
| Anchors | [element-006](../requirements.md#element-006), co-anchored `sequence-task-559`; source record [todo-334](../requirements.md#todo-334); integration [sequence-task-384](../requirements.md#sequence-task-384); ledger [element-006](../invest/named-elements.md#element-006), owner S427 |
| Disposition | To deliver as a single element. The only sink node in Batch F |
| Planned package | id `drain`, directory `elements/Drain/`, title "Drain" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-001](EL-001-finite-reservoir.md), [EL-004](EL-004-catch-basin.md), [EL-007](EL-007-open-gutter.md), EL-031 |

## Declaration

Closest delivered shape: a one-box static part such as the Wall (`elements/Wall/WallElementDeclaration.cs`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid sink declaration of `ENGINE-LIQUID` (W4, W9).
- **Body.** One static body, mass 0. Origin at the box centre.
- **Collider (one).** Box 1.0 × 0.08 × 1.0. The grate is drawn flush with the top face, so a ball rolls over it.
- **Material.** Family vessel material (EL-001).
- **Sink.** Local id 1. Open aperture: rectangle 0.8 × 0.8 at y = +0.05, 0.01 m above the box's top face; the interior (L4) is the 0.01 m slab between them. A stream sample that reaches the box top inside that region ends in the interior and enters the sink in that step; it is added to the world `Drained` account and to this sink's own committed total. A sink has no capacity and no rate limit, and nothing ever leaves it.
- **Mouths.** None. A pipe drains into it by ending above the grate.
- **Parameters.** None.
- **Presentation.** `Visual/Needle` `LocalRotationZ` from this sink's committed drained total, linear from 0 to 1.0 m³ over 270°, clamped (W12, missing).
- **Palette.** `ElementCategory.Structure`; palette order 519 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour navy (41/255, 57/255, 84/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Frame` | Box 1.0 × 0.08 × 1.0, cream `#fff8e9` | solid; the box collider | body pose (static) |
| `Visual/Grate` | Navy `#293954` bars, 0.8 × 0.8, on the top face | decorative: printed flat | nothing |
| `Visual/Dial` | Cream dial with navy ticks in one corner of the top face | decorative: printed flat | nothing |
| `Visual/Needle` | Gold `#f7cb52` needle on the dial, flush | decorative: printed flat and moved by a binding | presentation binding |

Icon: a square grate with a small dial. It differs from the Pressure plate (a raised plate with studs) by its barred grate and dial.

### Behaviour and numbers

- **Accounted, never deleted.** Everything that enters is in `Drained` and stays in the ledger identity (L10).
- **Only what crosses the intake.** A stream landing on the 0.1 m frame border, on the bench beside it, or blocked by a solid goes to `Spilled`, not `Drained`.
- **Example.** A full tank through an open tap over the drain: after 12 s the tank has lost 1 − 0.505 = 0.495 m³ ([EL-003](EL-003-tap.md) row T1) and the dial reads 0.495 m³; when the tank is empty the dial reads 1.0 m³, full scale.
- **Bodies.** A ball is a body, not liquid; it rests on or rolls over the box and is never captured.
- **Why no rate limit.** The requirement asks for an accounted boundary. A grate that backs up would need a pooled store on top of it and a second overflow, for no lesson.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body and baked collider | present | frame |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | sink declaration |
| W8 stream capture at an open aperture | missing | `ENGINE-LIQUID` |
| W9 sink node, `Drained` account, ledger identity | missing | `ENGINE-LIQUID`; its text names spill and overflow but not a sink (gap) |
| W12 driver for a committed sink total | missing | `ENGINE-LIQUID` |

Counts: 2 present, 1 untested, 0 narrower, 3 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one sink with an aperture, one binding.
2. **Generic laws.** Capture is L8; accounting is L10. No rule says "when liquid touches the drain".
3. **Three layers.** *Collider:* one box. *Visuals:* the table above; all marks are flush. *Animation:* the needle follows the committed total.
4. **One-way flow and typing.** As EL-001.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 box; joints 0; drives 0; sensors 0; liquid nodes 1 sink; mouths 0; open apertures 1; bindings 1; parameters 0.
6. **Performance fitness.** One collider. A sink is one row with one accumulator.
7. **Tests are scenarios.** Rows D1 to D6.

## Sources and legacy

- Requirement: "An explicit boundary collects and accounts for discharged liquid"; outcome "Only liquid crossing its intake enters the tracked sink." No variants.
- Component research [water](../../component-research.md#water): "a drain is an accounted sink" (line 30); ledger identity with sinks (line 12). Refinement [S704](../invest/refinements.md#s704): "Drain under pressure/head and empty control."
- **Legacy search.** EL-001's scope plus "drain" and "sink": only unrelated queue wording; no liquid sink at a6c914e. **Files harvested:** none.

## Acceptance outline

Authority: [element-006](../requirements.md#element-006). Tolerances L17. Planned class `DrainTests`. The source is a Water tank and an open Tap.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| D1 | Given the tap stream landing inside the grate, when Run advances 12 s, then `Drained` equals the tank's loss (0.495 m³ within 0.05 m³) and `Spilled` is 0 | `DrainTests.LiquidCrossingTheIntakeIsDrainedAndCounted` | D2 | — |
| D2 | Given the drain moved 1.0 m aside, then `Drained` is 0 and `Spilled` equals the tank's loss | `DrainTests.OnlyLiquidCrossingTheIntakeIsDrained` | D1 | a stream landing on the 0.1 m frame border is spilled, not drained |
| D3 | Given an empty tank, then `Drained` stays 0 for 10 s ("empty control") | `DrainTests.AnEmptySupplyDrainsNothing` | D1 | — |
| D4 | Given a bare tank mouth at 1.0 m depth (0.0835 m³/s) over the grate ("under head"), then the drain takes all of it; `Spilled` stays 0 | `DrainTests.TheDrainTakesAnyRate` | — | — |
| D5 | Given a Basketball rolled across the drain, then it crosses and is not captured, and `Drained` is unchanged | `DrainTests.ABallRollsOverTheGrate` | — | — |
| D6 | Given a run, then Reset zeroes `Drained` and the needle; save and load keep the pose | `DrainTests.ResetZeroesTheDrainedTotal` | the total before Reset is positive | needle at full scale for 1.0 m³ and clamped beyond |

Generic cover: W22. Later-partner row: the drain term of sequence-task-384 is `CatchBasinTests.ATankEmptiesThroughATapIntoABasin` (EL-004 row B7), completed in this slice. Chrome recipe (pending): a Water tank (EL-001) and an open Tap (EL-003) whose stream lands inside a Drain's grate (D1); the same tank and tap with the Drain moved 1.0 m aside, so the stream lands on the bench (D2). It uses only parts taught before the Drain (level 61; the Drain is taught at 61a, coordinator decision 37); the Open gutter (EL-007) is taught later, at 61b, and is not used.

## Open questions

All resolved; none needs the owner.

1. Rate-limited grate: no; any liquid crossing the intake enters at once (reason above).
2. Is drained liquid lost for a puzzle budget: yes, it is in `Drained` and never returns; a level that wants to count it reads the total (L13).
3. Balls: never captured; the grate is a flush print on a solid box.
4. Side pipe inlet: dropped (D19). No requirement or lesson uses it, and a pipe can end above the grate.

## Integration

- **Works with.** EL-001, EL-003, EL-004, EL-007, EL-014; none delivered. Delivered balls roll over it.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-334`; `sequence-task-559` not co-anchored with a todo; no `s427` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID` including a sink node; EL-001; EL-003; EL-004 (for B7); S7.
- **Files.** Adds `elements/Drain/Drain.csproj`, `DrainElementDeclaration.cs`, `drain.tscn`, `icon.svg`, `CuriousContraptions.tests/DrainTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** No sink node; a per-sink total that presentation cannot read; drained volume missing from the ledger identity.
