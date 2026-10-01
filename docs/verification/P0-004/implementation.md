# P0-004 owner evidence and review handoff

R3 Design/tooling candidate; [R1 Fail](review-r1.md) and [R2 Fail](review-r2.md) are retained. Independent re-review
and publication are pending. There is no terminal
Pass or permission to start P0-005. Implementation is /root/p0_004, actual thread
01a0f7b9-0ecd-74b2-81ce-361031596a36. Assigned independent reviewer is /root/p0_004_review,
actual thread 01a0f7d2-92c9-7e11-9fbf-3ea247ea179c.

The current canonical frozen input record is snapshot-r3.json; snapshot-r2.json and snapshot.json remain immutable R2/R1. It alone binds the allowlist, relevant inputs,
actual identities and scoped diff. [R3 commands](commands-r3.json) retain affected exit codes/stdout; [R2 commands](commands-r2.json)
and [R1 commands](commands.json) remain historical;
[initial compile failure](initial-build-failure.log) and the reviewer's separate
[pre-freeze findings](reviewer-pre-freeze.md) remain historical evidence.

## Criterion and impact matrix

| Criterion / impacted closure | Owner observation and independent check required |
| --- | --- |
| A01 prerequisites/source | P0-002/P0-003 terminal records retained; all 916 baseline runtime/build inputs unchanged. Actual generated/package/test compile inputs and four compiler contexts retained, with zero binding diagnostics. Reviewer checks provenance/applicability against snapshot. |
| A02 assembly graph | Eight named assemblies with acyclic dependencies; portable and worker graphs exclude Godot. Protocol schema placement is distinguished from sender/recipient instance ownership. No runtime extraction claimed. |
| A03 members/callers | 4,386 explicit authored-member assignments, including two indexers/72 conservative primary captures; 332,135 member-use entries are bound by fingerprints. 333,066 call edges and 18,551 conservative caller identities are reproducible, not duplicated into every shard. Reviewer must independently challenge semantic ownership and the declared conservative closure. |
| A04 ownership/type transformations | Mixed controllers, eligibility/Visible, retained delegates, network scene keys, live geometry/solver references, acoustic producer/recipient state and generated caches have explicit forward replacements. Stable IDs/generations remain distinct from local storage. Review selected negative guards and all semantic groups; passing enum validation is insufficient. |
| A05 reuse/lifecycle | Four existing shared owners retain their algorithms/storage roles. Exact source hashes and historical proof are referenced from P0-002 reuse-state. Current callback/rollback gaps have named implementing/proof children; no command-buffer rollback shortcut. |
| A06 typed schema/oracles | Closed choices are enums; extensible member/source/caller/type/work identities are typed. Canonical positive plus 22 negative cases and 22 semantic flow controls plus 106 positive/wrong-placement policy pairs pass, including self-consistent wrong-owner/writer/content changes, scene crossing, duplicate keys, null assignment and binding errors. Guards are finite semantic examples, not automated proof of all owners. |
| A07 broad affected consumers | Production/test/generated/compiler/package source and reference closure retained; 51 scripts preserve all 72 catalogue variants and their CAT-I/V children. Main project excludes tools, docs are export-excluded, and runtime baseline is unchanged. No physics, clocks, serialization, authored content, resources, UI or campaign behavior was edited. |
| A08 build/current integrity | Clean Release tool build, actual four-context builds and production audit pass. All three full Coverage audits pass while CompletionProven/RuntimeQualified remain false. R3 changes 69 spatial/query ownership placements and adds policy controls. The R2 classifier/census, 1,820 inputs and four compiler contexts are unchanged. The changed production map is audited again. Four compiler contexts and reference hashes are unchanged; reviewer verifies exact applicability. TODO preservation/local links/required row counts are checked at freeze. |
| A09 independent review | Pending: reviewer examines exact candidate, runs independent attacks/reproduction and resolves PF01–PF07, R1-F01–F04 and R2-F01 against actual files. This owner record is not approval. |
| A10 publication | Pending only after SnapshotApproval: commit/push the exact allowlist plus canonical snapshot, then reviewer independently checks parent, remote identity and every approved blob. No deployed runtime check belongs to this Design-only change. |

The current caller mapping is intentionally conservative for alias/virtual/interface/delegate
flows. Read [analysis bounds](../../work-orders/P0-004/analysis-bounds.md); it must not be described
as exact points-to analysis or current worker isolation. Current JSON/Godot-generated caches and
dispatch are explicitly browser-owned; production/test generated inputs are binding context,
not silently omitted effects.

No new browser action is justified by this unchanged-runtime Design scope. Root exclusively owns
Chrome via Playwright. Earlier native 66/76, browser pacing failures, unexplained input, physical-device
access and every mandatory part/mode lifecycle/performance gate remain open. The working audit also
does not establish ordinary clean-checkout integrated reproduction while runtime prerequisites remain
unpublished. No fallback reconstruction is supported.

## Corrections submitted for independent verification

R2 freezes [declaration/signature closure, producer-parent references and web lifecycle](../../work-orders/P0-004/r2-boundaries.md).
The source reconciliation includes 1,820 inputs with 4,386 unchanged authored member identities.
Semantic operation handling follows casts/parentheses and conservatively tracks aliases/ref/out/returns;
timer authorization now checks Read recipients too. The actual production audit passes independently of
fixture acceptance. R1 originals and a compact shard reversal retain exact failed-snapshot bytes.

R3 adds the complete [spatial/query/publication authority closure](../../work-orders/P0-004/spatial-authority.md):
installed collider metadata and query kernels are Core-owned; compiler candidates transfer once;
host producer pose/reference scratch is split from browser histories and caches. All 69 changed
members have explicit source-derived guards. Six affected shards and five changed source/doc files
retain exact R2 originals/reversal; reconstruct R2 before applying the prior R1 reversal.

## Earlier draft corrections

- Replaced directory-only tests and combined semantic context with actual Compile/ReferencePath,
  package/global-using/assembly/generated sources and separate real contexts; zero diagnostics.
- Added indexers/element accesses, primary captures and readonly reference contents. Added explicit
  conservative call/alias/dispatch closure and actual source-derived lifetime transformations.
- Distinguished stale-fingerprint rejection from refreshed unauthorized writer/contents and valid-but-wrong
  owner/rule attacks. Added scene-reference, duplicate-key, null-record and binding-error rejections.
- Corrected P0-029 from extraction to publication-only, P0-025 to histories and P0-026 to final application;
  preserved all shared-script catalogue variants.
- Retained actual compile/probe failures and the unwritten overlong-line warning; reformatted all shards
  and ran full-content Anvil gates without suppressing or raising scan limits.

Independent review may identify additional defects. A source-derived design assignment remains
reviewable and challengeable even when the structural audit says ContractCurrent.
