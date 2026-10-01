# P0-005 independent review — R3

**Verdict: Fail. SnapshotApproval and publication remain unauthorized.**

Implementation /root/p0_005, session 01a0f835-2500-73c0-a046-738333d3d7ff.
Reviewer /root/p0_005_review, session 01a0f848-fb01-7a53-90ce-7ff6d51d65e9.
Canonical [snapshot-r3.json](snapshot-r3.json), SHA256
22c6b9e2e5c39d48cfb45af0ed0c1d9e8bbf771fe43a76cba4b26c7b252c91c9;
base 46eb68fa47eee1be50a1da1c7fd1f6debc854522; scoped diff
294cbcf84a3d40ccc18e61a868afd8c16aee977839b1668945a6e06f1736d439.
[Raw results](reviewer-r3-results.json) retain reviewer commands/exits/output.
This is a bounded follow-up to [R2](review-r2.md), not a repeated evidence package.

## Resolved R2 findings

- F01: explicit increasing finite-difference input validation now applies across ScalarMap,
  committed scalar rotation and extents. Angle outputs can decrease or be equal while their
  endpoints/difference remain finite. New fixtures cover positive/reversed/equal/overflow cases.
- F02: samples now carry immutable instance identity, with monotonic nonreuse, captured
  descriptor mapping, activation only after matching registration result, retirement before
  removal completion, and capture-ordered publication sequences. Pending/retired/stale-generation
  values are discarded without target/property retargeting. Presented carries both instance and
  occurrence identity, so a reused ring cannot consume its predecessor's receipt.
- F03: query results carry the immutable required/read revision independently of latest envelope
  metadata; retained request correlation, exact read, mixed batches, stale consumption and
  original-key acknowledgment/release are explicit. The undeclared Save outcome is now InvalidMode.

The identity additions correctly update 26-byte samples: 256 outputs are 6773 bytes;
2516 are 65533; 2517 are 65559 and reject. Complete hidden/stopped output reservation remains.
No current 18-byte target/property-only path or old bound remains in the active contract.
Query result prefix is 32 bytes; unrelated physical sample arithmetic remains unchanged.

## Remaining findings

### R3-F01 — contradictory barrier outcome for pending queries

A02/A04 Fail. reliability.md:128 says Reset/Load converts old pending query results to Cancelled.
The new query-retention section at :185 says pending old-generation requests receive Stale.
They apply to the same admitted, not-yet-completed query crossing a barrier, and no normative
distinction separates them. The implementation owner confirmed this contradiction in read-only
clarification; no candidate files changed.

Choose one explicit barrier result and distinguish it from a same-generation exact-revision miss.
Preserve original key/revision, one terminal result and exactly-once release for both. This is a
finite outcome correction, not a new protocol subsystem. Retain completed old Found replies as
already specified; do not relabel them or use current envelope state.

### R3-F02 — early Presented receipt must not end a wavefront

A02/A06 Fail for an unresolved required behavior choice. bindings.md says after Presented
acknowledgment, A emits distance 8 (hidden) and completes. Its surrounding late-held condition
does not explicitly limit that transition to an already expired occurrence. An ordinary early
visible presentation also sends Presented. Ending at that first receipt would shorten the current
wavefront to one frame.

Independent current-source oracle, SceneAcousticWavefrontRun.cs:
line 98 retires only Presented AND Distance >= Range; line 148 retains unpresented OR distance
below Range; line 156 substitutes MinimumDistance only for unpresented expired occurrences;
line 179 marks/counts first visibility without terminating propagation.

For emission time 0 and speed 12 m/s: first visible at 0.1 s is distance 1.2; after receipt,
0.2 s must still show 2.4 and 0.65 s must still show 7.8; only at 2/3 s is normal propagation over.
If no frame occurred by 0.7 s, hold MinimumDistance until a matching visible receipt. A dropped/
delayed receipt keeps that bounded held occurrence; a duplicate or stale instance/occurrence
receipt cannot release a newer ring. The owner confirmed the intended early continuation and
that the frozen prose does not explicitly distinguish the transition. Freeze these two branches
and their finite boundary controls without changing authority or introducing another phase owner.

## Current criterion disposition

| Criterion | Verdict / evidence |
| --- | --- |
| A01 provenance/source | Pass: all 37 candidates, 130 inputs and 916 runtime hashes match; source/diff identity reproduces. |
| A02 complete finite schema | Fail only for R3-F01/F02 above; corrected widths/identities independently checked. |
| A03 stable identity | Pass for Design: slot-independent authored IDs plus generation and monotonic animation instance identity; old sample/new target controls reject. |
| A04 replay/lifetime | Fail for conflicting query barrier outcome; corrected query read stamp and sample activation/retirement otherwise close prior gaps. |
| A05 bounded ownership | Pass for current shapes: 2516/2517 boundary, required 256 outputs, unchanged 44057-byte physical velocity example, retained 504/8 KiB lease partition. Any later shape change invalidates affected arithmetic. |
| A06 current modes | Fail for unspecified early/late Presented transition; source-compatible scalar ranges now fixed. |
| A07 checks/oracles | Pass for scoped checking: Release tool build, 100 fixtures and independent identity/capacity/wavefront source goldens. These checks expose the two prose failures; no production codec/worker claim. |
| A08 integrity/preservation | Pass: three full audits exit 0 with completion/qualification false; R2 originals+unchanged paths reconstruct all candidate hashes, exact TODO inverse 4436305d1bf3dd237ea1cc84e7db179d5adf5afe68c30ba7f365e15b6aaa9dd5. |
| A09 independent closure | Fail: two required-now findings remain. |
| A10 publication | Incomplete and unauthorized. |

## Scoped impact and evidence reuse

The R2 impact matrix remains applicable for unchanged construction/compiler/save identities,
physical authority/affine values, finite parameter/diagnostic modes, control leases, transfer
cancellation and command result reservations. Current reviewer compared the changed actual
tool/doc closure and all canonical relevant-input hashes; no runtime/build/resource input changed.
R2 source reconciliation and its separately recorded control-size/same-allocation-return arithmetic
are reused only for those unchanged semantics, not new sample/query/wavefront behavior.

Changed sample identity propagates through registration, pending activation, complete history,
removal/Reset, ring reuse and Presented matching; new tool checks and independent tuple/capacity
goldens verify the specified rejection. Changed query revision propagates through retained request,
Found payload, delayed/new-generation consumption, cancellation and cumulative release; the
contradictory cancellation outcome is isolated above. Changed scalar validation matches source
constructor/commit call requirements. New sample widths received fresh arithmetic, not reused
R2 bounds. All required current inexpensive audits ran once at this frozen boundary.

The raw independent models are design checks, not runtime transport or browser tests. No Chrome
action, reload, server start or export occurred. P0-003 device/workload budgets, browser/native
failures, physical-device prerequisites, current parts/modes and the 150-level campaign remain open.
No tool pass upgrades runtime qualification. No deliverable fix was authored by this reviewer.

Keep this snapshot/failure record immutable. Correct the two narrow issues, retain their prior
bytes, then freeze the next scoped revision. Do not restart solved work or duplicate all prior
raw proof. A later independent review may reuse applicable checks after verifying exact inputs.

