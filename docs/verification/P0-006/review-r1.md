# P0-006 independent R1 review

**Verdict: Fail. No SnapshotApproval; publication and successor work are not authorized.**
Known required-now violations below affect A05/A07. Remaining uncertain closure is Incomplete,
not a claim that every unmentioned criterion passed.

Implementation: /root/p0_006, session 01a0f87c-eef8-7f41-bbda-011b5a5733c1.
Independent reviewer: /root/p0_006_review, session 01a0f88e-63e6-7fc1-8879-5f1bb7f8fc96.
Parent: /root, session 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5.
The reviewer authored only review evidence, no deliverable fixes.

Canonical immutable candidate: [snapshot.json](snapshot.json), SHA256
ef69ac2c5a0a78e576bcac8bc62efe0b44533efb6146f5b36e8b47262b97b1a3.
Source base 854bbc71dc528c0e083389ffbabc3d5df71423f6; scoped diff
25e1765b31291d43236d8deaa6873a713cbda73449269257446e51d430838428.
[Raw reviewer commands/results](reviewer-r1-results.json) retain builds, audits, fixtures and
counterexample arithmetic. This record references the original manifest rather than copying it.

## Required findings

### R1-F01 — simultaneous animation streams are not fully accounted or specified

lifecycle.md makes world animation and standalone UI simultaneously active with distinct animation
generations and request windows. P0-005 wire.md gives AnimationSamples one animation generation;
its envelope has one world generation, zero for standalone UI. clocks.md nevertheless retains at
most three complete A samples, indexes all of them as one stream, and admits 3*P+3*A+512 bytes
using A as a complete envelope size. A single complete envelope cannot combine the world and UI
generations. Three total samples do not provide the declared three-deep history for each stream;
three per stream are not counted by the published formula.

Concrete canonical capacities: P=44057 bytes; world A=117+26*256=6773 bytes;
UI A=117+26*842=22009 bytes. All individual samples fit 64 KiB, and 1098 total animation outputs
are below 2516. The stated single-stream calculation is 153002 bytes; three histories of all
three streams require 219029 bytes, above 196608. This is a capacity-contract failure, not a
measured browser failure. Raw exploratory arithmetic also retains a 22000-byte illustrative
size; the canonical record-size calculation above corrects that to 22009.

The associated separate request windows must explicitly preserve P0-005's aggregate 128 pending/
result entries and two retained result generations. Define transitions with live UI controls plus
world Run/Reset/Load, outstanding UI/world results, and further generation replacement. Do not
silently multiply windows or forget one live generation when choosing the next ID. Distinguish
active evaluator generations, retained result generations and clock generations. The current text
does not resolve those interactions sufficiently to implement without a new choice.

Required remedy: explicit existing-wire-compatible per-stream histories, composition/time selection,
aggregate count/byte admission and transition/ack rules; account peak old/new generations and
metadata/scratch. Add independent positive/control/boundary goldens for simultaneous UI/world
sampling and lifecycle replacement. Keep P0-003/005 quotas intact.

### R1-F02 — timestamp interval excludes admitted uncertainty

clocks.md declares offset interval [w2-b3,w1-b0] and sample browser interval
[w-offsetUpper,w-offsetLower], but adds quantization only to the scalar uncertainty. Drift later
expands the interval, without explicitly adding quantization. The disjoint-new-probe test also
compares intervals that are not defined to include endpoint uncertainty.

For the documented golden raw offset [49.6,50.2] ms with .01 ms timestamp precision bounds,
a true offset 49.59 ms is allowed by quantization. Worker stamp 175 maps to browser 125.41,
outside the declared raw upper bound 125.4. The reported midpoint +/- .32 does cover it,
so the two normative representations disagree. A valid probe can likewise appear disjoint if
quantization bounds are omitted from the consistency check.

Required remedy: define one conservative bounded interval including timestamp quantization and
elapsed drift, then derive uncertainty and all mapping/consistency/admission comparisons from it.
Specify whether a sample's own timestamp precision is already included or must be added.
Add boundary controls for quantization-only overlap, genuinely disjoint probes, drift, expiry and
mapped sample containment. No physical-clock rescaling or relaxed <=1 ms qualification.

### R1-F03 — finite mutation choices bypass the enum requirement

ContractFixture.cs declares MutationId(int Value), validates numeric 1..28, and Program.cs iterates
integer indices 1..28. These are exactly the closed M01–M28 mutation families used to select each
failure case, not an extensible identifier registry. The integer wrapper/range does not provide
compiler-checked named members or enum-typed callers required by AGENTS.

Required remedy: represent the closed mutation family choices with an enum through Expected,
Result, iteration and negative boundary controls. Keep extensible stable identities typed as they
are. No aliases or named integer/string constants replacing enum members.

## Criteria and impact

| Criterion | Independent result |
| --- | --- |
| A01 source/provenance | Identity portion Pass: all 11 candidate and 134 relevant hashes match, scoped diff reproduces, all 916 source/build/content hashes unchanged, distinct actual sessions. Full transitive semantic closure remains Incomplete pending the generation/history corrections. |
| A02 lifecycle matrix | Five-mode/16-command fixture and IntegrityLost controls pass. Session/animation-generation transition consistency remains Incomplete under F01; no general runtime lifecycle claim. |
| A03 exact construction/save | Source confirms Start mutates ropes/initial/corrections before capture finishes, and LoadSave calls LoadLevel before World.LoadMachine. Design explicitly requires candidates and distinguishes durable Save from S Applied. Final composition review remains Incomplete pending revised lifecycle dependencies. |
| A04 mutation/restoration | Inventory names 28 families, actual 20 RuntimeState overrides, query metadata, transaction/publication split, real free/version arrays and empty current authoritative RNG set. No runtime injection or exhaustive transaction enrollment proof claimed. Affected animation ownership/composition closure remains Incomplete. |
| A05 clocks/history/limits | Fail: F01/F02. Existing 120 Hz/four substeps, 60 Hz animation, 5 s startup, 50 ms reliable-age failure and 8 MiB quotas are stated, but incomplete stream accounting and intervals invalidate design closure. |
| A06 finite failure oracle | 336 classification pairs pass as a small expected-outcome model; not state-vector rollback tests. Before/after commit, restore failure, indeterminate process death and first terminal dispositions are distinguished. Full revised clock/generation controls remain Incomplete. |
| A07 build/schema/integrity/typing | Release build clean, 470 enumerated fixtures pass, all three full Coverage audits exit 0. Qualification/completion remain false. Links resolve; exact TODO inverse matches 2397aab38d17b0030d7b2fb93b65194bf09dd1caeee20773d48b116c7b3e6ff4. Fail for F03. |
| A08 independent closure | Fail: three unresolved findings. |
| A09 publication | Incomplete and not authorized. No deployment required for this Design-only scope; exact publication review follows only a future approved snapshot. |

| Impact closure | Invariant / evidence / disposition |
| --- | --- |
| Host command admission, transaction, result, queries | Reviewed P0-005 replay/reliability original identity, first terminal query and barrier dispositions against lifecycle/oracles; these must survive fixes unchanged. No hidden retry after commit or worker death. |
| Animation generations, UI consumers, browser histories | F01: actual wire cannot represent both namespaces in one sample. Aggregate memory/result ownership and simultaneous lifecycle need explicit revised controls. |
| Clock calibration, sample ages, interpolation and qualification | F02: conservative mapping intervals must match reported uncertainty and measured-error gate. Simple arithmetic tests do not prove actual clock error. |
| Compiler/Workshop construction and persisted content | Actual Start/LoadSave ordering supports named defects; design-only candidate changes do not fix current runtime. Exact browser state, settings, stable IDs and storage transaction remain P0-020/026/032. |
| Physics topology/query registration and failed-tick publication | Inventory preserves P0-004 authority split and explicit transaction-before-publication gap. Current source unchanged; full implementation/injection remains P0-014/020/032. |
| Standalone tooling/build/content/export | BCL-only net10.0 project; tools excluded from game compilation. No runtime source/bundle change or UI action. Broad source preservation verified; no native/browser pass reused as new qualification. |
| Enum/schema tests | Schema missing/unknown/duplicate/undefined values reject; mutation integer closed set violates end-to-end enum rule (F03). |
| Documentation/status/protections | Only authorized two-line TODO inverse; gate incident retained. See below. |

## Applicability and procedural incident

P0-003, P0-004 and P0-005 terminal reviews were read as source/design prerequisites, with their
current contractual files in the checked relevant-input manifest. Their prior runtime/tooling
proof is not reused to pass this new contract. New Release build, fixtures and full cheap audits
ran independently. The unchanged 916-entry broad closure and actual standalone project exclusion
justify no fresh game export or Chrome behavior test for this Design-only diff; future runtime
gates remain mandatory. No browser input, server operation or export occurred.

The implementation's initial TODO write after a blocked gate was a real procedural violation.
commands.json retains the block and records allowed reversal, exact predecessor hash verification,
and separately allowed reapplication. Independent inverse hashing confirms the current two-line
state precisely. This corrects the final content/protection sequence, not the historical violation.
There is no proposal to bypass the gate. Revised writes must keep validation and application in
separate steps with an explicit decision check. The reviewer did so for its own records.

Return findings to the same implementation owner. Preserve this R1 snapshot and raw failures;
freeze one revised candidate and re-review affected scope before any publication.
