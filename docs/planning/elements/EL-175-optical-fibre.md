# EL-175 · Optical fibre — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths in metres.

No legacy part implements a fibre or light pipe. It reuses the legacy routing mechanism (input aperture to directed outlet with internal travel counted against range) and the legacy length-at-connect rule of rope links; every other value is **proposed** with a justification. The source record marks this element **Optional**; that status is retained.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-175 |
| Name | Optical fibre |
| Type | Optical |
| Anchor | [requirements.md#element-175](../requirements.md#element-175); named entry [named-elements.md#element-175](../invest/named-elements.md#element-175); source record [todo-307](../requirements.md#todo-307) (Optional) |
| Owner | S521 (refinement S526 fiber) |
| Refines CAT | none. Distinct from the ball tubes [CAT-048 pipe](CAT-048-pipe.md) and bends; routing law shared with [CAT-006 beam_combiner](CAT-006-beam_combiner.md) / EL-213. |
| Related | EL-212 flat mirror (the routing challenge it must not erase); EL-213 combiner; EL-176 to EL-178 lasers |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** two separately placed static couplers (entry and exit), each an opaque box 0.5 × 0.5 × 0.5 m with a navy foot 0.6 × 0.1 × 0.6 m at (0, −0.3, 0) (**proposed**: small enough to tuck beside a mirror path, large enough to aim at). The cable between them is a drawn connection with no collider, like a wire (**proposed**: keeps it separate from ball tubes, which carry balls).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none (the cable is not a physical rope).
- **Typed ports:**
  - Entry coupler: optical input aperture at (−0.26, 0, 0), normal −X, finite disc radius 0.3, front only (**proposed**: smaller than the 0.43 combiner port so coupling is deliberate); connection socket `FibreOut` (domain Optical, Output) (**proposed**).
  - Exit coupler: connection socket `FibreIn` (domain Optical, Input) and a directed outlet at (0.26, 0, 0), direction +X (**proposed**).
  - A link must join exactly one `FibreOut` to one `FibreIn` of a different coupler; incompatible ends, a second link, or a self-link are refused at connection (**proposed**, following the legacy typed-link rules).
- **Sensors and activation:** none.
- **Optical law (proposed):** light entering the entry aperture leaves the exit outlet with power × 0.9 (entry coupling) × e^(−0.02 × L) (attenuation) × 0.9 (exit coupling), where L is the cable length; the remaining range is reduced by L plus internal travel; the transfer uses one interaction. An unconnected entry absorbs; an unconnected exit emits nothing. Values: coupling 0.9 per end (**proposed**: the legacy routing retention), attenuation 0.02 /m (**proposed**: about 15% extra loss over the 8 m maximum, so long routes visibly cost light).
- **Work and energy stores:** none; output ≤ 0.81 × input.
- **Parameters:** cable length `length` — f32, 0.5–8 m, set to the straight-line distance between couplers at connect time (**proposed**: the length-at-connect rule of legacy rope links; the 8 m maximum is the legacy pipe maximum, `engine/MachineData.cs@a6c914e:L72-L75`).
- **Cosmetic curves and UI bindings:** the cable glows faintly in the beam ink while carrying light and is slate `#556573` when dark (**proposed**); the exit lens follows the committed output colour at rate 12 /s (**proposed**: the combiner's legacy output-lamp rule).
- **Art (DESIGN.md palette):** cream couplers `#fff8e9`, gold input rim `#f7cb52` on the entry and ochre outlet rim `#e8b764` on the exit (matching the combiner's input/outlet cue), navy feet `#293954`, slate cable (**proposed**).
- **Catalogue and inventory entry:** id `optical_fibre`, title "Light pipe", category Optics (**proposed**). Inventory: at most one per level unless a level author grants more (**proposed**: the source requires restricting it where unrestricted routing would bypass the mirror puzzle). Description (**proposed**): "Carries light from the gold-rimmed entry to the outlet of its partner. Some light is lost at each end and along the cable."

### Variants

One; no variants are named.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box bodies and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); typed connection records (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`) without an Optical domain.

**Missing**
- OpticalTransport routing between two parts over a typed link (cross-part outlet) — no story; Story 13.5 builds same-part routing. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488; refinement S526 ([refinements](../invest/refinements.md)).
- An Optical connection domain and `FibreOut`/`FibreIn` sockets (closed enums) — S257-style typed connection decision.
- Per-level inventory limit — the existing inventory capability (`engine/gpu/WorkshopInventory.cs`) must express it.
- OpticalAbsorption/SensibleHeat for losses — S489/S543, mode-specific.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a laser source (CAT-036 or EL-176 to EL-178; Story 13.1), CAT-005 battery (Story 8.1), a receiver (CAT-038 / EL-214; Story 13.2).

## 4. Sources and legacy

**Sources.** Row [element-175](../requirements.md#element-175): compatible endpoints carry bounded optical power with coupling loss and finite path length; disconnected endpoints cannot bridge a gap. [todo-307](../requirements.md#todo-307) (Optional): explicit compatible endpoints, coupling limits and loss; keep separate from ball tubes; restrict inventory where it would bypass the mirror puzzle. Refinement S526 **fiber**. Campaign: fiber/light pipe first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no fibre; mechanisms only):
1. Route interaction: routed ray leaves a directed outlet with power × transmission and remaining range minus internal travel — `engine/OpticalNetwork.cs@a6c914e:L119-L125`; routed optics require a finite directed outlet — `engine/OpticalNetwork.cs@a6c914e:L58-L62`. Carry forward, extended across a link.
2. Internal travel counts against range — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L91-L117`. Carry forward (cable length counts the same way).
3. Routing retention 0.9 — `parts/BeamCombinerPart.cs@a6c914e:L12-L12`. Carry forward as the proposed coupling.
4. Typed links with explicit matching sockets and a length set at connect — recorded as rope facts R4–R6 in [CAT-058-rope_anchor](CAT-058-rope_anchor.md) (`engine/ConnectionPort.cs@a6c914e:L42-L66`). Carry forward the pattern.
5. Pipe length bounds 1–8 m — `engine/MachineData.cs@a6c914e:L72-L75`. Carry forward the 8 m maximum as the proposed cable bound.

**Files harvested:** `engine/OpticalNetwork.cs`, `parts/BeamCombinerPart.cs`, `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, a triggered laser, the entry coupler on its beam, the exit coupler behind a wall, and a receiver facing the exit; join the couplers with the connection tool (Optical link); wire the receiver to a lamp. Run.
- **Positive:** the receiver behind the wall reads input × 0.81 × e^(−0.02 L) and lights.
- **Negative / controls:** couplers placed but not linked — nothing leaves the exit (no gap bridging); a laser aimed at the exit coupler's back does not enter; linking two entries or two exits is refused; a ball rolling at a coupler bounces (no ball transport).
- **Boundaries:** cable length 0.5 and 8 m; range budget includes the cable; inventory limit refuses a second fibre.
- **Run/Reset:** Reset clears paths and the cable glow. **Save/Load:** both couplers, link and length round-trip.
- **Integrations:** mirror puzzles (EL-212) where the fibre is the scarce shortcut. Binding criteria: [element-175](../requirements.md#element-175).

## 6. Open questions

1. Coupling, attenuation, aperture size and the one-per-level limit are proposed; owner to confirm.
2. Is the fibre reciprocal (either coupler can be the entry)? Proposed: directional entry → exit.
3. Should the cable obey any minimum bend or route around bodies, or be a straight logical link (proposed)?
4. Optional status and roadmap: unscheduled; owner to decide whether it is built at all.
