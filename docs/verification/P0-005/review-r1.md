# P0-005 independent review — R1

**Verdict: Fail. No SnapshotApproval and no publication authorization.**

Implementation: /root/p0_005, session 01a0f835-2500-73c0-a046-738333d3d7ff.
Independent reviewer: /root/p0_005_review, actual CODEX_THREAD_ID 01a0f848-fb01-7a53-90ce-7ff6d51d65e9.
Parent session: 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5.
The reviewer authored only this review evidence, not deliverable fixes.

Canonical candidate: [snapshot.json](snapshot.json), SHA256
70d4757acb80f084980604b632e7cff09ed758b3f0902024a33daf0a65dae63a.
Base: 46eb68fa47eee1be50a1da1c7fd1f6debc854522.
Scoped diff: 741fb3b669a98734882f866a6147b38b6914267b374fcd980a2589430d171ef2.
The snapshot defines the ten candidate paths and 126 relevant input hashes; no duplicate manifest here.
[Raw commands and results](reviewer-r1-results.json) retain independent build, 50 fixtures,
three full Coverage audits, source/TODO/diff identity checks and adversarial arithmetic.

## Findings

### R1-F01 — incomplete current presentation binding grammar

A02/A06 Fail. wire.md defines topology bindings as target, owner, source body, parent target,
AnimationProperty, BindingOwner and Affine. LocalTranslation expressly uses the binding's
declared axis, but that grammar declares no axis. Current SceneAnimationAdapter.BindTranslation
and BindRotation require independently selected X/Y/Z axes; the same identity baseline does not
encode which one was chosen. BindColour/BindFollowingColour/BindImpulseColour additionally require
two colour endpoints. ClaimScalarExtent requires ScalarExtentDefinition; committed rotation and
colour channels are also distinct current consumers. The contract does not give these consumers
a complete target binding variant or an explicit immutable browser-registry mapping with its
identity, validation and ownership. Merely naming eight property tags does not preserve those
modes. Define the exact value-only/local-registry split and finite mapping, including composition,
units, validation, registration/removal and current callers. This does not require transferring
Godot objects or cosmetic state into physics.

### R1-F02 — reserved control slots do not reserve control bytes

A05 Fail. An edge permits 512 KiB of charged leases and 56 data slots. Eight legal 65536-byte data
envelopes consume all 524288 bytes, leaving eight control slots but zero bytes. If both directions
are saturated, Credit (80+12 bytes under the envelope grammar) cannot be enqueued within the byte
limit. Processing data cannot release its sender charge until Credit, so slot reservation alone
does not establish the promised saturation escape. Calling Credit out-of-band and synchronously
released does not define how its sender reservation is reclaimed across asynchronous contexts or
exempt it consistently from the universal envelope/byte accounting. Admissions is absent from
the enumerated control kinds although rejected lifecycle admission promises an independent
bounded control response path. Freeze control byte/slot ownership and release rules and enumerate
which responses use it. Prove reciprocal full data occupancy still permits bounded credit,
acknowledgement, rejection and lifecycle progress without an accounting exception or hidden queue.

### R1-F03 — original command generation is not represented in results

A02/A04 Fail. Replay identity uses original envelope generation plus command sequence; sequences
restart at one after Reset/Load and two generations may coexist. The envelope describes the
sender's latest installed commit. A result contains sequence and resulting generation, but no
original generation. Reset/Load themselves and old invalidations can therefore belong to a
different generation from both current envelope and resulting generation. The prose requires old
command identity preservation and old-generation cumulative acknowledgement without specifying
an encoding rule that carries it. Freeze an explicit original identity or precise homogeneous
batch identity semantics and update widths/oracles. Include old Reset/Load results, old rejected
pending commands, new sequence-one commands, duplicates and acknowledgements across the barrier.
The raw struct case demonstrates the omitted discriminator, not a claim that a runtime codec
has been exercised or that its two synthetic labels constitute a valid complete execution trace.

### R1-F04 — reliable lifetime and acknowledgement closure is incomplete

A02/A04/A05 Fail. replay.md says the sender retains each admitted reliable record until semantic
acknowledgement. Acknowledge defines Result/Event/Topology/Transfer, but Result is specified only
as stream zero plus contiguous simulation command sequence in its world generation.
Queries/QueryResults have QueryId but no sequence/release acknowledgement rule for the global
64-query reservation. AnimationDefinitions have no registration acknowledgement identity;
AnimationResults uses request sequence without a producer/stream scope or retained-result release
window, although B and S can both send AnimationCommands to A. Definitions/results/requests can
therefore not be assigned a complete bounded lifetime from the specified fields and rules.
Freeze a per-family reliability/owner/reservation/release table, including definition registration,
query completion, animation request and occurrence deduplication, fan-out, failed transfer and
cancellation. Declare who allocates animation request sequences, whether B/S share an instance,
and how competing ordered controls are rejected or ordered. No new general framework is needed.

### R1-F05 — animation registration can exceed a complete sample

A05 Fail. The contract allows 4096 active definitions and treats AnimationSamples as replaceable
complete samples. 4096 distinct target/property outputs require 80+37+18*4096 = 73845 bytes,
exceeding the 65536-byte envelope. The exact boundary is 3634 samples (65529 bytes); 3635 is
already 65547. Only PhysicalState has an explicit whole-sample admission check. Freeze the
animation sample/registration capacity relationship and atomic admission policy, or an exact
coherent subscription grammar with finite bounds. Do not silently truncate/split a claimed
complete sample. The required 256-target workload fits (4725 bytes); that positive case does
not establish all admitted definitions fit. P0-003 workload qualification remains mandatory.

## Criterion matrix

| Criterion | Verdict / evidence |
| --- | --- |
| A01 source/prerequisites | Pass for identity and reconciliation scope inspected. All ten candidate and 126 input hashes match; source base and diff reproduce; 916 runtime/build/content inputs unchanged. P0-003 and P0-004 terminal independent Design reviews are present and applicable prerequisites, not runtime evidence. |
| A02 exact finite grammar | Fail: F01, F03 and F04 leave mandatory fields or meaning unresolved. Fixed scalar widths, affine versus pose widths and most record arithmetic are otherwise consistent. |
| A03 stable identity | Pass for the documented allocation/reorder/slot separation design and exercised map controls; no runtime allocator/codec claim. Generation/result boundary is separately failing A04. |
| A04 application/replay | Fail: F03/F04. Contiguous admission, pre-tick revision, lower-sequence dependencies, cancellation and tick scheduling are useful specified rules; they do not resolve missing endpoint identities/lifetimes. |
| A05 bounded ownership/budgets | Fail: F02/F04/F05. 8 MiB partition arithmetic and small positive workload arithmetic pass, but allowed occupancy does not satisfy all declared progress/sample invariants. |
| A06 current mode mapping | Fail: F01. Current parameter families and finite diagnostic/logic/optical/tone maps were inspected; presentation mode mapping is incomplete. |
| A07 independent fixtures | Pass for the limited executable subset: all 50 pass, exact integer goldens, schema controls and stable map controls execute. Additional adversarial design cases expose failures above. This is not production codec/worker proof. |
| A08 build/integrity/preservation | Pass: independent Release build has zero warnings/errors; all three full audits exit zero with qualification/completion false. TODO two-line reversal exactly matches prior hash; all runtime inputs unchanged. |
| A09 complete independent review | Fail: five unresolved findings. Wider runtime proof remains Incomplete at its later named gates, not retroactively failed merely because this Design is incomplete. |
| A10 publication | Incomplete and not authorized. No approved snapshot exists. |

## Transitive impact and applicability matrix

| Source/consumer closure | Invariant and observed result |
| --- | --- |
| SimulationCommandInbox → MachineWorld controls/transaction → result consumers | Stable generation-qualified commands, exactly once effect and correct ack release. Source is unchanged; target contiguous sequence and cumulative ack are forward replacements. F03/F04 block a complete wire freeze. |
| Compiler → authored MachineData/PartSpec/SavedMachine → registry/UI/save/campaign | Exact construction-only version 5, typed parameters, explicit old-v4 rejection, persistent member IDs and named P0-008/016/020 cutover retain current 150-level/72-variant scope. Existing strings are acknowledged migration work, not declared compliant. No content was rewritten by this row. |
| Collider replacement/query → publication → recipient histories | Full 96-byte affine basis remains distinct from 56-byte pose; authoritative queries remain simulation-owned. Query reservation completion is missing under F04; browser interpolation must not supply authoritative query values. |
| Physical publication/events → B/A fan-out | Complete state versus reliable occurrences remains distinct; per-recipient event cursors and exact event stamps are specified. Control credit deadlock under F02 would prevent reliable progress and barrier completion. |
| AnimationBatch/SceneAnimationRun → adapter/current parts/WorkshopAnimation | Four evaluator families and finite curve/overlap/timing modes remain visible. Source requires axes, endpoints and extents absent from target binding mapping (F01). B/S animation request lifetimes are unresolved (F04); full output capacity fails F05. |
| Clocks/lifecycle/rollback | Target fixed tick and explicit phase do not substitute wall time. P0-006 may choose full mode/timeouts, but cannot repair missing P0-005 identities/ack fields implicitly. No stale-generation mutation or renderer dependency is authorized. |
| Diagnostics/performance | Existing PerformanceStage/Metric/Outcome maps preserve signed counters and per-context recording. Diagnostic loss invalidates evidence. P0-003 exact device/workload/CPU/GPU/age budgets are unchanged; no browser qualification is reused or newly claimed. |
| Build/content/records | WireContract has only standard .NET dependencies; main csproj excludes tools sources, tools are Godot-ignored and docs excluded from browser export. The 916-entry source identity closure establishes no runtime changes. Native/browser reruns would not prove this paper grammar; required new design/tool checks were run instead. |

Reuse is limited to unchanged, independently reviewed P0-003 measurement and P0-004 ownership
contracts as prerequisites. Their actual input contract hashes and runtime source closure match.
No former runtime/build/browser result is upgraded or copied as current P0-005 qualification.
The new tool build/fixtures and required current full audits were independently rerun.
No Chrome input, reload, server start or export occurred.

Anvil graph context initially returned not_ready; no completeness inference was drawn.
Exploratory reads of a nonexistent scripts directory returned errors; actual engine/parts paths
were then resolved with rg. These were read mistakes, not product failures or permission bypasses.
The raw script arithmetic is an independent design counterexample, not execution of a production
codec. Review publication remains separate from implementation publication.

Owner must fix forward, retain this R1 snapshot/failure evidence, freeze the next scoped revision
and return it for independent review. Do not publish or start successor work from R1.

