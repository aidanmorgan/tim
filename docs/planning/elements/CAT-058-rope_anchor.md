# CAT-058 · rope_anchor declaration readiness spec

This is the Story 7.0 CAT-058-D declaration readiness spec for the rope anchor. It also carries the route, length and tension facts of the rope connector itself (EL-205), which has no catalogue entry of its own; [CAT-053 pulley](CAT-053-pulley.md) and [CAT-067 weight](CAT-067-weight.md) cross-reference this file for them. Baseline commit is `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge; current-engine files are cited at the same commit.

Current implementation authority: package/bundle discovery and .tscn assets; C# f32 generic constraints and typed command/query/event interfaces. Pinned source/API citations below describe harvest baseline. Native unit/integration acceptance applies now; Chrome recipes are retained post-catalogue. Entry decisions 1–7 are resolved below for 10.2b by the coordinator on 30 October 2026, using retained source behavior and current runtime-control conventions. The [implementation contract](../../../_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md) records the complete current design and candidate evidence. The candidate implements the straight-rope scope; independent review/publication remain pending, and pulley/browser qualification is not claimed.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-058 |
| Kind | `rope_anchor`, catalogue title "Rope anchor" |
| Requirement anchor | [CAT-058](../requirements.md#current-cat-058); retained behaviour [todo-140](../requirements.md#todo-140) and [todo-141](../requirements.md#todo-141) |
| Mapped identities | [EL-064 Metal loop anchor](../invest/named-elements.md#element-064) (candidate — Batch A confirms); [EL-205 Rope](../invest/named-elements.md#element-205) (candidate — Batch A confirms; the rope is a connection, not a catalogue part) |
| Roadmap story |10.2b Rope Anchor, tensile rope and cutting;10.2c owns later pulley routing. [Independent contracts](../../../_bmad-output/planning-artifacts/epics.md#story-102b-rope-anchor-tensile-rope-and-cutting-cat-058). |
| Status | 10.2b implementation candidate; independent review/publication pending; pulley and browser qualification later |

## 2. Declaration

### Bodies and shapes

- One static box body. Legacy collider: box centred at (0, 0, −0.12) m with full size 0.55 × 0.55 × 0.16 m (half-extents 0.275, 0.275, 0.08) — `parts/RopeAnchorPart.cs@a6c914e:L15-L16`; `AddBox` stores half of the authored size (`reference/cpu/MachinePart.cs@a6c914e:L352-L355`).
- Current types: `RigidBodyDeclaration` with `RigidMotionKind.Static` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`) and one `ColliderShapeKind.Box` `ColliderDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- Pick (selection) radius 0.55 m — `parts/RopeAnchorPart.cs@a6c914e:L15`.

### Mass and material

- None for mass: the anchor is static; static declarations require zero mass, velocity, gravity and drag (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`).
- Material: legacy static parts used restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236`). The anchor declared no override. Declare through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`) with rolling resistance 0, the current value for non-sphere bodies (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L726`). Values are an owner decision (Open question 4).

### Constraints and joints

- The anchor owns no joint. It is a fixed endpoint of a rope route; the rope constraint is declared once per complete route (see Legacy harvest R11–R17).
- New declaration type required (Story 10.2): a rope-route constraint declaration listing ordered body-local attachment points, the authored maximum length, and connected-body collision enabled. No such type exists in `engine/gpu`.

### Typed sockets and ports

- One socket `Tie`, domain `Rope`, direction bidirectional, local position (0, 0, 0.18) m — `parts/RopeAnchorPart.cs@a6c914e:L9-L12`. Attachment role `Anchor`: accepts exactly one rope end (`engine/RopeNetwork.cs@a6c914e:L76-L77`).
- Current `WorkshopConnectionDomain` has only `Activation` and `Electrical`; `WorkshopSocket` has no `Tie` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). Story 10.2 adds the rope domain, the `Tie` socket and the attachment role as enums.
- Wire names at the serialization boundary: socket `tie`, domain `rope`, link field `rope_length` (`engine/MachineData.cs@a6c914e:L67-L70`, `engine/MachineData.cs@a6c914e:L103-L150`).

### Sensors and activation

None — the anchor has no sensor, activation input or output in legacy or requirements.

### Work and energy stores

None — a fixed endpoint stores no energy. The rope is tension-only and massless in legacy (R14); it stores no elastic energy.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| (anchor) none | — | — | — | — | `parts/catalog/rope_anchor.tres@a6c914e:L14` (`Parameters = {}`) |
| Rope length (per span, on the connection) | f32 | 0.05–200 (legacy) | straight-line distance between the two tie sockets at connect time | m | `engine/ConnectionPort.cs@a6c914e:L47-L52`; `reference/cpu/MachineWorld.cs@a6c914e:L686-L691` |

### Cosmetic curves and UI bindings

- No cosmetic curve on the anchor. Rope artwork (all routes): free spans straight when taut and curved when slack, unfinished threading dashed and carrying no tension, gold endpoint knots, no pulley knots; slack curve is a length-matched visual approximation, not collision geometry — `DESIGN.md@a6c914e:L284`.
- The current `AnimationFeedbackSource` closed set (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L6-L7`) has no rope source; Story 10.2 adds the committed-route binding for rope artwork.

### Art

- Scene `parts/scenes/rope_anchor.tscn` is a bare `Node3D` with the part script (`parts/scenes/rope_anchor.tscn@a6c914e:L1-L6`); all meshes are built in code.
- Meshes: cream (`#fff8e9`) mount box matching the collider; catalogue-colour ring radius 0.2 m, thickness 0.055 m at z 0.16 m, rotated 90° about X — `parts/RopeAnchorPart.cs@a6c914e:L16-L18`. DESIGN: "Anchor eyes are gold on cream mounts" (`DESIGN.md@a6c914e:L284`).
- Palette: "Pulley / rope anchor" RGB 0.84, 0.61, 0.28 (`#d69c47`) — `DESIGN.md@a6c914e:L187`; `parts/catalog/rope_anchor.tres@a6c914e:L13`.
- Toolbox pictogram (navy stroke): rounded square, centre circle r 5, stem down — `ui/WorkshopIcons.cs@a6c914e:L97`.

### Catalogue and inventory entry

- Id `rope_anchor`, title "Rope anchor", category "Ropes", description "A fixed tie-off for a rope. Connect a weight to make a hanging load or pendulum." — `parts/catalog/rope_anchor.tres@a6c914e:L8-L14`.
- Declare RopeAnchor metadata, typed parameters/ports and package-owned .tscn in `elements/RopeAnchor`; bundle discovery supplies catalogue/inventory identity. No WorkshopPartKind or manual Free inventory row. Legacy registration citations remain harvest evidence, not instructions.

## 3. Engine capabilities

Capability families: see the CAT-058 row of the [general-engine element map](../general-engine-element-map.md) and the `rope_anchor` entry in [catalogue-elements.json](../../coverage/catalogue-elements.json). Not duplicated here.

**Exists now**

- Static box body and collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.
- Contact material and the Box2D v3 TGS Soft contact solver (soft parameters `makeSoft`, 480 Hz substeps, 8 biased + 4 relax iterations): `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L225-L226`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884`.
- Typed connection storage and validation pattern: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L62`.
- Save codec version enum: `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Current 10.2b candidate**

- Rope/Tie/Bidirectional and declared attachment roles; parameter-evaluated Weight eye and static RopeAnchor package.
- Canonical straight routes, measured 0.05–200m spans, graph refusal and one projected tensile row in the existing TGS schedule. Open guide topology has no equation; finite-radius guided geometry remains 10.2c.
- Committed Open/Slack/Taut geometry and Intact/Cut integrity; Running/Paused cut queues until the next committed physical step. Reset restores authored endpoints and length; Building saves omit cut state.
- Retained strand/knot artwork follows committed attachments. Native proof and review status live in the implementation contract; browser qualification is pending.

**Later gates**
- Pendulum and counterweight qualification on the same rope law — Story 10.4.

**Element dependencies**

- Needs a dynamic load to show behaviour: [CAT-067 weight](CAT-067-weight.md) (first visible interaction is anchor-supported hanging Weight, `docs/planning/invest/current-consumers.md` CAT-058-I).
- Shares the rope law with [CAT-053 pulley](CAT-053-pulley.md); angled/pulley routes are a CAT-058 acceptance item.
- Tether integration with [CAT-065 trampoline](CAT-065-trampoline.md) (R23).

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| R1 | Anchor is a static rope endpoint (`RopeAttachmentKind.Anchor`) with one bidirectional `Tie` socket at local (0, 0, 0.18) m. | `parts/RopeAnchorPart.cs@a6c914e:L8-L12` | carry forward | Typed socket and fixed geometry match the requirement's "fixed typed endpoint". |
| R2 | Collider box half-extents (0.275, 0.275, 0.08) m at (0, 0, −0.12); ring art r 0.2, tube 0.055 at z 0.16; pick radius 0.55. | `parts/RopeAnchorPart.cs@a6c914e:L15-L18`; `reference/cpu/MachinePart.cs@a6c914e:L352-L355` | carry forward | Authored geometry; re-declare as f32 metres. |
| R3 | Attachment roles {None, Load, Anchor, Guide}; rope states {Open, Slack, Taut}. | `engine/ConnectionPort.cs@a6c914e:L11-L12` | carry forward | Closed sets; keep as enums end to end. |
| R4 | A rope link must name explicit matching `Tie` sockets in the same domain, output/bidirectional to input/bidirectional, between two distinct parts; non-rope links must carry no length. | `engine/ConnectionPort.cs@a6c914e:L42-L66` | carry forward | Typed admission rule. |
| R5 | Rope span length must be finite and within 0.05–200 m. | `engine/ConnectionPort.cs@a6c914e:L47-L51` | carry forward (bounds subject to Open question 1) | Authored validation boundary; no requirement value contradicts it. |
| R6 | Connecting two parts sets the span length to the straight-line distance between their world tie sockets at that moment ("Rope length is set when you connect"). | `reference/cpu/MachineWorld.cs@a6c914e:L686-L691`, `reference/cpu/MachineWorld.cs@a6c914e:L658-L659`; `tools/Campaign/Program.cs@a6c914e:L173-L187`; `content/puzzles.json@a6c914e:L5072-L5079` | carry forward | Measured length is the requirement's "complete measured rope route". |
| R7 | Loads and anchors accept one rope end, guides two; a third end, a duplicate span, or a part without an attachment role is rejected ("Ropes cannot branch"). | `engine/RopeNetwork.cs@a6c914e:L69-L78`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L259-L277` | carry forward | Requirement: invalid branching admission. |
| R8 | Open loops through guides and closed guide loops (even with no loads) are rejected. | `engine/RopeNetwork.cs@a6c914e:L100-L112`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L243-L257` | carry forward | Requirement: cycle admission. |
| R9 | A link with a missing length rejects the whole load atomically; the previous construction stays. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L278-L281` | carry forward | Atomic rejection rule. |
| R10 | Routes are walked from endpoints in canonical order (part id, then port), so results do not depend on link direction or declaration order; reversed links give the same motion. | `engine/RopeNetwork.cs@a6c914e:L91-L93`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` | carry forward | Requirement: direction/declaration order. |
| R11 | Route maximum length is the sum of authored span lengths; only the sum constrains a complete rope. | `engine/RopeNetwork.cs@a6c914e:L12-L13`, `engine/RopeNetwork.cs@a6c914e:L97-L110` | carry forward | Defines the rope law input. |
| R12 | A route is complete only if neither end is a guide; an incomplete route declares no physics and the load free-falls (0.981 m/s after 0.1 s at g 9.81). | `engine/RopeNetwork.cs@a6c914e:L34-L35`, `engine/RopeNetwork.cs@a6c914e:L125-L127`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L187-L202` | carry forward | Requirement: incomplete/open routes transmit no tension. |
| R13 | State: Open if incomplete, Slack if current length < max − 0.001 m, else Taut. | `engine/RopeNetwork.cs@a6c914e:L37-L38` | carry forward the states; do not carry forward the 0.001 m literal | The tolerance was a CPU double tolerance; freeze an f32 threshold under the game-grade envelope. |
| R14 | Rope is tension-only: no equality row; inactive while current length < max − tolerance; when active it only resists lengthening. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward the law; do not carry forward the code | Behaviour matches EL-205; the CPU impulse/acceleration solver is replaced by a TGS Soft unilateral row. |
| R15 | Consecutive attachments on the same rigid body contribute a constant length; other spans react on both attachment bodies. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L20-L22`, `engine/physics/PhysicsRopeJoint.cs@a6c914e:L44-L57`, `engine/physics/PhysicsRopeJoint.cs@a6c914e:L84-L95` | carry forward | Needed for pulley entry/exit points on one sheave body. |
| R16 | Fully static routes get no equation but are rejected if fixed geometry exceeds the authored length; a route of only static and prescribed bodies is rejected ("requires a dynamic participant"). | `engine/RopeNetwork.cs@a6c914e:L132-L143`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L204-L221` | carry forward | Matches the CAT-053 requirement text. |
| R17 | Rope routes keep connected-body collision enabled; a slack rope between two loads does not suppress their contact or rebound. | `engine/RopeNetwork.cs@a6c914e:L144`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L223-L255` | carry forward | Requirement: connected-body collision stays enabled. |
| R18 | A missing registered participant body is rejected, never invented as a fixed point. | `engine/SceneRopeJoint.cs@a6c914e:L39-L51`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L306-L307` | carry forward | Requirement text for CAT-053 routes. |
| R19 | Acceptance: a slack anchored rope lets the weight fall freely; it becomes taut, holds the weight at the rope length (±0.001 m) with vertical speed ≤ 0.001 m/s; an upward 1 m/s impulse is not resisted. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L146-L185` | carry forward (re-freeze tolerances under f32) | Requirement: slack test and force reaction. |
| R20 | Acceptance: 3D pendulum (anchor at (0, 6, −0.18), weight mass 2 at (2, 5, 2)) keeps length within ±0.002 m and total energy ≤ initial + 0.5 J for 480 ticks, and swings through depth to z < −1. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L191-L215` | carry forward (re-freeze tolerances under f32) | Requirement todo-141: 3D pendulum work/length bounds. |
| R21 | Rope links cannot be removed while running; after Reset the same link can be disconnected and the load then falls. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L158-L189` | carry forward | Construction edits are build-mode only. |
| R22 | Reset restores exact link lengths and endpoint identities; a second Run replays to the same state signature. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L59-L65`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L257-L280` | carry forward | Requirement: Reset/save preserve endpoint identities and lengths. |
| R23 | Tether integration: a taut anchor rope keeps a weight above a trampoline (no touch, no compression); a lengthened (+2.3 m) slack tether allows a rebound and becomes taut; energy never exceeds 1.01 × initial; Reset restores the saved construction. | `CuriousContraptions.tests/TrampolineRopeTests.cs@a6c914e:L33-L118` | carry forward | Integration acceptance shared with CAT-065. |
| R24 | Routes were point guides ("not a round pulley sheave approximation"). | `engine/SceneRopeJoint.cs@a6c914e:L20-L21`; `engine/physics/PhysicsRopeJoint.cs@a6c914e:L20-L22` | do not carry forward | Superseded: CAT-053 requires finite-radius groove tangents and arcs. |
| R25 | A taut span whose attachments coincide throws and rolls the world back. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L64-L67`; `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L161-L171` | do not carry forward | Game-grade rule: numerical residuals clamp or continue, never fault the tick. |
| R26 | Analytic joint-stop sweeps, per-step event budgets, position projection and transactional rollback for ropes. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L123-L144`; `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L47-L64`, `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L104-L159`, `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L213-L237` | do not carry forward | CPU solver path and proof-grade machinery. |
| R27 | Read-only `Ropes` view and a fresh constraint slot object per Run. | `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L39-L85` | do not carry forward | Legacy object-identity mechanism; the kept fact is R22. |
| R28 | Catalogue entry: id, title, "Ropes" category, description, colour, no parameters. | `parts/catalog/rope_anchor.tres@a6c914e:L8-L14` | carry forward | Catalogue identity. |
| R29 | Historical status: ropes supported explicit lengths, slack, tension and fixed pulley routing; rope cutting and moving-block ratios were future work. | `reference/P0-022-before/source-README.md@a6c914e:L99` | carry forward as context | Confirms scope; rope cutting stays a separate obligation. |

### Files harvested

- `parts/RopeAnchorPart.cs`
- `parts/catalog/rope_anchor.tres`
- `parts/scenes/rope_anchor.tscn`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs`
- `engine/RopeNetwork.cs`
- `engine/SceneRopeJoint.cs`
- `engine/physics/PhysicsRopeJoint.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `reference/P0-022-before/source-README.md`
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the surviving pictogram)
- `tools/Campaign/Program.cs`
- `content/puzzles.json` (checked; no rope_anchor placement or stock)
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- `CuriousContraptions.tests/SceneRopeBindingTests.cs`
- `CuriousContraptions.tests/TrampolineRopeTests.cs`
- `CuriousContraptions.tests/ConnectionPortTests.cs` (checked; generic typed-port rules already captured by R4)
- `engine/physics/JointConstraints.cs`, `engine/physics/JointEquations.cs` (checked, no rope-specific knowledge)
- `engine/bridge/*` (checked, no element knowledge)
- `reference/p054-guide/puzzles-candidate.json` (checked; same rope levels as `content/puzzles.json` apart from the orientation encoding)

## 5. Acceptance outline

Requirement row: [CAT-058](../requirements.md#current-cat-058); story 10.2.

- **Chrome UI recipe.** In free play, place a Rope anchor high on the bench and a Weight below it using the toolbox and move gizmo; connect anchor `tie` → weight `tie` with the contextual wiring control (rope domain). Verify the placed configuration and the typed rope connection from the committed read, not the scene.
- **Positive.** Run: the Weight hangs from the anchor on a taut rope; the route length equals the measured connect-time length.
- **Negative/control.** An unconnected Weight falls to the bench; a rope from a Weight to a lone Pulley (incomplete route) is dashed and carries no tension.
- **Boundaries.** Slack start (weight moved closer after connecting, if the UI keeps the length — Open question 1), wrong-endpoint and branching/cycle admission rejected atomically, angled and pulley routes, force reaction on the load.
- **Run/Reset.** Reset restores exact endpoint identities, span lengths and weight pose; a second Run replays the same committed result.
- **Save/Load.** Save and reload preserve `tie` endpoints and `rope_length` values; unknown sockets or domains reject.
- **Integrations.** Pendulum with CAT-067 (story 10.4), pulley routes with CAT-053, tether over CAT-065.

## 6. Open questions

Resolved entry decisions — coordinator,30October2026. These are explicit implementation decisions, not a claim of a new owner answer. They settle the former blockers; the [10.2b contract](../../../_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md#design-notes) supplies the current command/read and numerical details.

1. Connect-time world Tie distance sets per-span length, inclusive0.05–200m. Moves and mass edits preserve it; reconnect remeasures. Zero, missing, nonfinite and outside values refuse. No separate length editor in10.2b.
2. Massless, tension-only/inextensible gameplay uses the shared projected soft-constraint law. Explicitly extend the existing contact regularization policy (60Hz, damping ratio10) to the new length row through effective-mass spring/damping coefficients. This is numerical conditioning, not a rope material or elastic-energy store. Verify gameplay support, release and energy behavior; do not inherit historical precision pins or a second solver.
3. Rope has no mass/collider. Slack sag is a length-matched cosmetic approximation driven by committed attachments, with no physical work.
4. Anchor uses the sourced restitution1, threshold0.1m/s, friction0.3 and rolling0; static mass0. Existing full3D placement rotations transform collider, Tie and art. No separate wall-mount attachment mechanic is added.
5. Rope is a connection, with no separate inventory stock or metre budget. Existing level endpoint stock and connection permissions still apply.
6. Open means incomplete or cut; Slack means current geometric length below authored maximum; otherwise Taut. No inherited0.001m deadband or universal epsilon. State classification does not assert nonzero tension. Coincident positive-length endpoints are slack and never fault a tick.
7. Cutting remains required in10.2b. Running/Paused accept a typed span-targeted CutRope request through the existing command path. Applied acknowledges queued input; the UI says Cut queued until the next successful committed step removes that route's tensile row and publishes Cut/Open. Paused waits for Step/Resume; no artificial time advance. Pending/already-cut route and unknown span refuse typed without altering other routes. Discarded candidates do not publish or consume an unapplied request. Reset clears cuts and restores original authored endpoints/lengths; Building-only Save/Load stores the uncut authored construction. Build-mode connection deletion/Undo is distinct. Steel-cable resistance remains EL-067; finite-radius pulley routing remains10.2c.
