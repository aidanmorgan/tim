# P0-007 — portable numeric and geometry extraction

Implementation in progress; no snapshot approval, publication or completion claim.
Owner /root/p0_007, session 01a0f8a4-665c-7141-b726-a77bef7df330.
Base revision aec245307d5fc4cf4ea325df6e38f732d60636b3.
Independent reviewer has not yet been assigned.

## Scope

[TODO Order 7](../../../TODO.md#work-p0-007) is Native stage.
[P0-004 assembly roles](../P0-004/assemblies.md) and
[spatial authority](../P0-004/spatial-authority.md) bind this extraction.
[P0-005 identity](../P0-005/identity.md) and
[P0-006 clocks/lifecycle](../P0-006/contract.md) remain required targets.
Their authoritative terminal reviews, not frozen historical status paragraphs, establish prerequisites.

The actual Geometry project contains BCL-only CollisionVector, RigidRotation, RigidPose,
AffineBasis and AffineTransform. SceneGeometryAdapter owns all Godot capture/render conversions.
Specialized convex, hollow, sweep, support and solver algorithms remain their existing owners.
The portable proof compiles their actual source closure and SimulationTransaction without Godot/2dog.
No alternate solver, conversion overload, alias or compatibility path is supported.

Core-local query metadata and captured sweep snapshots contain numeric/geometry values and
PhysicsBodyId, never MachinePart, BodySlot, scene delegates or renderer transforms.
Scene capture wrappers explicitly retain browser ownership. PhysicsBodyId is an existing
runtime-local slot identity, never a wire/save identity. P0-008 owns persistent document/member
identity allocation and installation; P0-014/016 own stamped publication/encoding.
This extraction must not infer stable IDs from current Uid text or array order.

Affine capture preserves all basis columns, including scale, shear, reflection and float roundoff.
Convex instances still require approximately proper rigid bases; they retain accepted anisotropy.
Hollow child centering retains the existing explicit float rounding/subtraction arithmetic.
SupportFootprint is an explicit correctness change: its exact-declared-geometry contract previously
normalized child bases through a quaternion. It now samples the same affine support/features as
collision. Independent diagonal-basis goldens retain the counterexample and downstream guide/
compliant tests must establish the impact; this is not labelled representation-equivalent.

## Required-now criteria and pending evidence

| Criterion | Acceptance |
| --- | --- |
| P0-007/A01 | Actual source/project/reference/content dependency closure and prerequisite reconciliation; immutable reviewed candidate and distinct reviewer identity. |
| P0-007/A02 | Geometry assembly BCL-only; actual specialized geometry/query closure compiles without scene packages; injected Godot/2dog references reject. |
| P0-007/A03 | Double authority, complete affine capture, proper-rigid boundary rejection, independent hollow/rotating/transform goldens. |
| P0-007/A04 | Portable query metadata/snapshots, typed identities and enum modes; retained snapshots survive later scene/metadata mutation and replacement/rollback exactly. |
| P0-007/A05 | Every affected caller compiles; guide/compliant/support/geometry/query controls and boundary checks pass with the SupportFootprint correction assessed explicitly. |
| P0-007/A06 | Current Chrome/Playwright positive/control/integration and motion evidence for affected active behavior; exact Run/Reset/save restoration; Release export. |
| P0-007/A07 | Scoped integrated-browser regression measurements, physics/publication/animation/render cost distinction; known absolute-budget failures remain open. |
| P0-007/A08 | Current inexpensive Coverage/Ownership integrity audits use actual new assembly/reference/source inputs; affected old evidence explicitly stale. |
| P0-007/A09 | Independent criterion/transitive-impact review resolves required-now findings before exact SnapshotApproval. |
| P0-007/A10 | Publication-dependent: normal-hook approved allowlist commit/push, exact remote/parent/blob identities and required deployed checks independently verified before terminal Pass. |

The numeric first cut compiled all game/test callers with zero warnings/errors and passed
129 focused geometry/support native tests. A 76-test baseline comparison returned exit 1:
66 passed and ten failed. These are intermediate observations, not final candidate proof.
Source changed afterwards for portable query ownership. Final evidence must retain the
complete raw output and assess exact applicability; no unchanged-916-input claim is valid.

Chrome is controlled exclusively by the coordinator through Playwright. No child UI input,
reload, server start or uncoordinated export is allowed. The unrelated unexpected input incident
remains unattributed. Browser actions must use real UI controls; no imported solution, game-state
setter or numeric placement menu is proof. No browser qualification is inferred from native tests.

## Frozen-boundary work still required

Complete query caller/fixture replacement, architecture injection controls and analytic tests;
derive and execute current UI recipes through the coordinator; coordinate Release export;
reconcile clean-checkout publication prerequisites rather than stage unrelated dirty runtime;
refresh affected current manifests once at freeze; retain failures and one scoped snapshot.
Historical P0-004/005/006 artifacts remain immutable. Full worker/device/campaign qualification
remains at P0-032/034/035 and the named catalogue proof rows. All 150 levels remain in scope.

## Current implementation checkpoint

The Geometry assembly and namespace are extracted; ordinary and PLAYTEST Release callers compile.
The combined focused native suite passes 332/332 and portable proof passes eight criterion groups.
Injected scene references reject. The final 76-test retained-baseline comparison still exits 1 with
ten failures. See [prerequisite finding](../../verification/P0-007/prerequisite-reopening.md) for the
rejected isolated overlay, static dependency witnesses and proposed existing-owner boundary repair.
That proposal is not an approved prerequisite change: the reviewer must challenge whether a cohesive,
fully verified generic migration of existing parts provides a legal atomic publication boundary.
Current manifest reconciliation, exact clean publication closure, production export, real Chrome UI,
Run/Reset/save, motion and integrated browser cost proof remain Incomplete. No SnapshotApproval or
publication is requested by this implementation/finding freeze. Reviewer identity is pending and will
be recorded in the independent review referencing the immutable snapshot.
