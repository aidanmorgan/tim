# GAP-07 · Granular sieve — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-07 · Granular sieve · Mechanical (P3 potential) |
| Anchor | [requirements.md#gap-07](../requirements.md#gap-07); [named-elements entry](../invest/named-elements.md#gap-07) |
| Related identities | Depends on [GAP-06 Granular dispenser](GAP-06-granular-dispenser.md) (grain presets and feed); outputs feed [EL-120 Quantity goal](EL-120-quantity-goal.md). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 93 "Sift Happens", practice 94, reuse 97, 136, 141 ([gap-07](../requirements.md#gap-07)). |
| Status | Not started. Disposition potential/conditional ([element map](../general-engine-element-map.md)). |

## 2. Declaration

Variants: "Verify either gravity-only operation or every separately adopted powered-shake mode before using it" ([gap-07](../requirements.md#gap-07)). `Gravity` is the first mode; `PoweredShake` is specified separately and is conditional on adoption. Aperture size is a typed preset of both.

- **Bodies and shapes.** A static frame holding a slotted insert of parallel static bars: the openings are real collision gaps, never a label test: "Do not classify particles by a hidden desired-output label" ([gap-07](../requirements.md#gap-07)).
  - **Proposed** frame 1.2 m × 0.15 m × 0.9 m with bars 0.06 m wide across the 0.9 m depth — the same footprint class as the dispenser outlet it sits under; tilt set by the rotate ring so oversize grains roll off one edge.
- **Aperture `Narrow`.** **Proposed** slot width 0.18 m — passes Fine grains (0.125 m diameter, GAP-06) and blocks Coarse (0.25 m).
- **Aperture `Wide`.** **Proposed** slot width 0.3 m — passes both presets, the "wrong choice" control.
- **Mass and material.** Static. **Proposed** bar friction 0.3, restitution 0.05 — matches the shared static friction; low bounce so grains settle into slots.
- **Constraints.** None in `Gravity`.
- **Variant `Gravity`.** Grains fall onto the tilted insert; small grains pass through slots under gravity; oversize grains remain on top or roll off the low edge.
- **Variant `PoweredShake` (conditional).** The insert becomes a kinematic body oscillating along its plane while supplied.
  - **Proposed** amplitude 0.03 m at 4 Hz — peak speed 2π·4·0.03 = 0.75 m/s and peak acceleration 4π²·4²·0.03 ≈ 19 m/s² (about 2 g): a grain resting across a slot hops at most 0.75² / (2 × 9.81) ≈ 0.03 m and re-seats, well inside the 0.15 m frame rim.
  - Electrical `PowerIn` socket (domain exists, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); unsupplied it behaves as `Gravity`.
- **Typed ports.** None in `Gravity`; `PowerIn` in `PoweredShake`.
- **Sensors and activation.** None; separation is purely geometric.
- **Work and energy stores.** None in `Gravity`; `PoweredShake` draws finite supply work for the kinematic motion.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `aperture` | enum `SieveAperture { Narrow, Wide }` | closed set | `Narrow` | — | **proposed** (typed presets required by [gap-07](../requirements.md#gap-07)) |
  | `mode` | enum `SieveMode { Gravity, PoweredShake }` | closed set | `Gravity` | — | variants from [gap-07](../requirements.md#gap-07) |

- **Cosmetic curves and UI bindings.** Selected-only aperture choice shown by distinct slot silhouettes, no numeric inspector ([gap-07 visual style](../requirements.md#gap-07)). `PoweredShake` insert motion follows the committed kinematic pose.
- **Art.** Shallow cream frame `#fff8e9`, navy slotted insert `#293954`, gold supports `#f7cb52` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** **Proposed** id `granular_sieve`, title "Sieve", category Materials; appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/gap-01.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, GranularTransport, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static boxes and sphere–box contact with friction (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L345-L407`).
- **Missing.**
  - GranularTransport and the grain feed: GAP-06 / LAW-GRANULAR-I ([S635](../invest/decisions.md#s635)); owner S640. The feed is capped at 12 grains by the 16-body dynamic table (see [GAP-06](GAP-06-granular-dispenser.md)); a larger mixed feed waits on that capacity decision.
  - A kinematic motion kind for `PoweredShake`: today only `Static` and `Dynamic` exist (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L6-L6`), although the f32 inventory lists Kinematic ([gpu-f32-physics](../../gpu-f32-physics.md)). Owner S640.
  - Collider capacity: 64 per document (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L166-L167`); a Narrow insert uses about six bars.
- **Dependencies.** GAP-06 dispenser with a mixed feed; a Receiver per fraction for the conservation check; Battery (CAT-005) for `PoweredShake`.

## 4. Sources and legacy

- **Requirements.** "Small grains pass and oversize grains remain; an all-oversize control blocks and finite mixed feed conserves both fractions" ([gap-07](../requirements.md#gap-07)).
- **Audit.** "GAP-06 and an independently specified size distribution. Mixed feed sorts; all-oversize control blocks; no hidden label-based routing" (`docs/physics-puzzle-gap-audit.md@a6c914e:L86-L86`).
- **Legacy.** None: no sieve, grain or size-sorting code, test or level exists.

## 5. Acceptance outline

Point of truth: [gap-07](../requirements.md#gap-07).

- **Chrome recipe.** Place a Grain hopper with a mixed feed of 6 Fine and 6 Coarse grains (12, the GAP-06 cap) over a Sieve (Narrow, tilted 15°); a Receiver below the sieve and a second at its low edge.
- **Positive (Gravity).** The 6 Fine grains collect in the lower Receiver and the 6 Coarse in the edge Receiver; each fraction's count equals its feed.
- **Positive (PoweredShake, if adopted).** Supplied, grains resting across slots clear faster; unsupplied, behaviour equals `Gravity`.
- **Negative or control.** All-Coarse feed of 12: nothing passes. `Wide` aperture: Coarse passes too (wrong-choice control).
- **Boundaries.** No grain ever passes a slot narrower than its diameter; total count conserved.
- **Run/Reset.** Reset restores feed, sieve pose and empty Receivers.
- **Save/Load.** Aperture, mode and pose survive save and Load.

- **Integrations.** Feed both fractions from GAP-06 into the two Receiver lanes and bind EL-120 quantity goals to their actual counts; if PoweredShake is adopted, disconnect its Battery and repeat the same finite feed.

## 6. Open questions

1. Whether `PoweredShake` is adopted at all, or gravity-only ships. Unspecified — owner decision.
2. Aperture presets (proposed Narrow 0.18 m, Wide 0.3 m) and whether a third size is needed.
3. Whether the mixed feed is a GAP-06 preset or two dispensers (two dispensers share the same 12-grain dynamic-body budget).
