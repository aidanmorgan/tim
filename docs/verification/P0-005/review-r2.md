# P0-005 independent review — R2

**Verdict: Fail. No SnapshotApproval or publication authorization.**

Implementation /root/p0_005, session 01a0f835-2500-73c0-a046-738333d3d7ff.
Reviewer /root/p0_005_review, actual session 01a0f848-fb01-7a53-90ce-7ff6d51d65e9.
Canonical [snapshot-r2.json](snapshot-r2.json), SHA256
b2c51ea86b997139bd0b645abfbd34b419e15cf7b14d2fe77e5a4c2c1a81e391.
Base 46eb68fa47eee1be50a1da1c7fd1f6debc854522; scoped diff
3cdc2e70ecb9955563489c1cc6d26415fc63535eaf4cb9a2e8ce85a93618a99f.
[Raw reviewer commands](reviewer-r2-results.json) retain exact exits/output. The canonical snapshot
owns the allowlist/input identity; this record does not duplicate it.

All 25 candidate files and 128 relevant inputs match; scoped diff reproduces; all 916 runtime
inputs remain unchanged. Ten R1 originals reconstruct their original hashes; R1 snapshot and both
review evidence files remain byte-identical. Exact TODO inverse is
4436305d1bf3dd237ea1cc84e7db179d5adf5afe68c30ba7f365e15b6aaa9dd5.
New relative file links resolve. No production/browser change or qualification occurred.

## R1 dispositions

- F01: axes, colour endpoints, extents, physical pose/reference maps, current feedback families,
  timer/enum mappings and wavefront descriptors are now explicit. Source checks confirm the
  ScalarExtent formula and current rotation/translation composition. Two remaining binding issues
  are R2-F01/F02 below; this is not full closure yet.
- F02: fixed for the reviewed Design scope. Data and control reserve separate 504 KiB/8 KiB budgets.
  Return transfers the same original allocation, with explicit lease leg/ledger, rather than
  needing reverse-edge credit. Admissions now fit the returned allocation; removed tag 7 rejects.
  Eight maximum controls fit 8 KiB; the smallest one-query return exactly fits 114 bytes.
  Reciprocal data/control saturation has a finite return/progress path without growing memory.
- F03: fixed for simulation command results. Original generation plus sequence is explicit,
  distinct from resulting generation and current envelope; mixed old/new batches and acknowledgements
  have unambiguous keys. The separate query timestamp gap is R2-F03.
- F04: materially corrected. Per-family semantic retention/release, query acknowledgements,
  browser-only animation request sequencing, definition results, transfer cancellation/disposal,
  Save transfer correlation and restart ownership are specified. No second cosmetic command producer.
  R2-F02/F03 still prevent full coherent consumption.
- F05: fixed for current 18-byte scalar output shape. 3634 outputs require 65529 bytes; 3635 requires
  65547 and rejects before registration. Hidden/stopped definitions reserve output; RGB reserves
  three. 256 outputs remain 4725 bytes. Any R2-F02 encoding change must recompute this bound.

## Blocking findings

### R2-F01 — ScalarMap range validation weakens the current source contract

A02/A06 Fail. bindings.md says InputFrom/InputTo must be finite and unequal.
SceneAnimationRun.cs:311 requires InputTo > InputFrom and a finite difference; the committed
rotation/extent paths at lines 270/296 impose the same increasing finite-range requirement.
Independent controls:

| Endpoints | Written R2 rule | Current source requirement |
| --- | --- | --- |
| 0, 1 | accept | accept |
| 1, 0 | accept | reject |
| -1e308, 1e308 | accept | reject: difference overflows |
| 2, 2 | reject | reject |

The new explicit rule contradicts the source-equivalent mapping claim: Math.Clamp with reversed
bounds throws, and an infinite denominator changes interpolation. Preserve the complete finite
range and associated angle/output validation through all named bindings. Positive, reversed,
equal and overflow controls must exercise the frozen rule; do not silently broaden support.

### R2-F02 — same-generation samples cannot validate a replaced binding

A02/A04/A06 Fail. AnimationSamples encodes generation, clock and target/property/value, with no
instance/descriptor identity or registration revision. bindings.md requires exact descriptor
validation and invalidation on Remove; reused wavefront groups explicitly receive new instance
and descriptor identities. Yet the sample does not identify either one.

Concrete control: capture a value for target T/property P under instance I1/descriptor D1; Remove;
register I2/D2 on the same T/P within the same animation generation; deliver a retained older
sample. The stated fields cannot by themselves distinguish which descriptor produced the value.
No normative result/sample ordering and browser activation barrier resolves that distinction.
Transport EdgeSequence alone does not specify when retained sample capture/queueing must occur
relative to registration/removal result publication and final browser activation.

Freeze a complete sample-binding identity/coherence rule with bounded retention and atomic
registration/removal; it may use an explicit registration identity or a fully specified ordering
barrier, but cannot rely on an unstated implementation convention. Include delayed/reordered sample,
same-generation group reuse, failed registration, duplicate controls and Reset controls. Recompute
sample capacities if the payload changes. The implementation owner acknowledged this gap in a
read-only clarification; no frozen file was edited.

### R2-F03 — delayed query values are stamped with the latest commit

A02/A04 Fail. Query requests contain required revision, but Found result prefix contains original
generation without the queried/execution revision. wire.md assigns query values the authoritative
envelope generation/revision while the envelope is explicitly the sender's latest known commit.
reliability.md retains completed replies until acknowledgment, including old-generation replies.

Concrete control: execute QueryId 1 at required revision R, retain its Found pose under
backpressure, advance to R+1 (or Reset), then transmit. Latest envelope metadata describes a
different authoritative state from the retained payload. The contract neither carries the actual
read stamp nor requires a retained request correlation/stamp validation through consumption.
The owner independently acknowledged the missing normative rule. Freeze exact capture and reply
identity, stale handling, batching and acknowledgment semantics. Include delayed R/R+1 and Reset,
duplicate/rejected/cancelled queries and actual release of the correct reservation.

Small related finite-outcome inconsistency: wire.md calls unsupported runtime Save
UnsupportedMode, but CommandOutcome declares only InvalidMode. Make the normative outcome one
declared tag; no alias or inferred extra outcome.

## Criterion and impact matrix

| Criterion / affected closure | Verdict and independent evidence |
| --- | --- |
| A01 source and provenance | Pass. Exact candidate, dependency, base/diff, original retention and runtime continuity identities checked. |
| A02 finite schema/units | Fail: R2-F01/F02/F03. Other changed widths independently recomputed: registration 82, query prefix 30/result 24, command result 54/62, descriptor prefix 140, physical prefix 57. |
| A03 stable IDs and slot independence | Pass at Design scope for authored stable identities, generation-qualified commands and explicit local slot separation; stale replaced presentation bindings remain failing under A04/A06. |
| A04 ordering/replay/lifetime | Fail: sample rebind and query read-stamp gaps. Original command generations and browser-owned animation requests correct earlier ambiguities. |
| A05 capacity and ownership | Pass for reviewed current shapes: independent 1/8/32 request return arithmetic, maximum control batch sizes, reciprocal saturated return ownership, 3634/3635 output boundary, 44057-byte physical example including all 257 velocity records. R2-F02 may change relevant arithmetic and invalidate this result. Runtime scheduling/fairness/age remains later proof. |
| A06 current consumers | Fail: changed binding validation and re-registration guarantees cannot yet preserve all required semantics. Checked SceneAnimationRun/Adapter, ScalarExtentDefinition, PoseReferenceBinding, SceneAcousticWavefrontRun and their typed read/feedback consumers. |
| A07 fixtures | Pass only for stated Design fixture subset: independent Release build, all 82 bundled fixtures, plus derived saturation, receipt, capacity, generation and range controls. New independent range cases expose F01; prose trace cases expose F02/F03. No codec/worker execution claim. |
| A08 integrity/preservation | Pass: all three full Coverage audits independently exit 0; RuntimeQualified/CompletionProven remain false. Exact TODO inverse and R1 original identities preserved; links resolve. |
| A09 independent closure | Fail: three unresolved required-now findings; no successor authorized. |
| A10 publication | Incomplete, not authorized before SnapshotApproval. |
| Transfer/commands/results | Original identity and finite staging/ack windows checked; failed/unsealed cancellation has an explicit disposition and same retained transfer identity. Current source untouched. |
| Presentation/animation | Four evaluator families and typed mappings remain separate from physics. Wavefront phase/present-once ownership is explicit and browser acknowledges actual visible application; same-group reuse inherits F02. |
| Save/content/compiler | Construction-only forward schema, typed stable members, canonical ordering and old-save rejection unchanged; Save now names its transfer. Full current caller/content cutover remains named implementation work. |
| Performance/clock/resources | P0-003 budgets unchanged; sample sizes do not establish measured performance. P0-006 still owns clock/timeouts and actual restart transition proof. Same-allocation returns do not prove worker scheduler fairness. |
| Build/runtime/browser | Tool-only sources remain excluded from game compilation. All 916 exact source/resource/build entries unchanged, so no runtime UI/export proof is newly reused or claimed. No Chrome action, reload, server or export performed. |

Scoped reuse: unchanged P0-003/P0-004 prerequisite contracts and R1 source ownership/reorder
analysis remain applicable after exact relevant-input comparison. New transport, results, bindings,
tool source and sample arithmetic received fresh inspection/checks. Raw R1 artifacts remain at
their original snapshot and are referenced, never rewritten. Current full integrity checks ran
once at this frozen revision; no repeated per-edit manifest refresh or new framework was introduced.

Exploratory reads of nonexistent ScenePoseMap paths were resolved to actual bridge declarations;
no missing-source assumption entered the verdict. The Python ownership/arithmetic controls are
design models, not fake runtime/browser tests. Independent source/range traces establish known
contract contradictions; missing future runtime proof remains Incomplete at its existing gates.

Owner must retain R2 and fix forward under a new immutable scoped revision. The reviewer has
authored no deliverable changes. Publication and successor remain unauthorized.

