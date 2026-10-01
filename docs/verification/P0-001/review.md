# P0-001 independent adversarial review

Reviewer: actual collaboration agent `/root/p0_001_review`. Implementer: actual collaboration agent `/root/p0_001`. Coordinator: `/root`. The reviewer did not author the contract, hash manifest, TODO edits or fixes. Its own NEW_TASK assignment and an independent `collaboration.list_agents` call showed both implementation and reviewer agents running simultaneously. Underlying opaque authentication/session identifiers are not exposed; none is invented. Coordinator additionally supplied the original spawn receipt `{"task_name":"/root/p0_001"}`; the implementation agent directly messaged this reviewer its repaired snapshot identity. These are actual orchestrator identities, not role labels.

## Initial reviewed snapshot (superseded below)

SnapshotApproval = Rejected. Terminal verdict = Fail for the reviewed preservation regression, pending implementer correction and independent re-review. Publication and successor work are not authorized.

Reviewed HEAD: `658b366c318a2977d92bed99b5f4893b5d5fa805`.
Contract SHA-256: `198c83e9d8256e26550925002e3db54d97b4f001f21ab7766862a42087a307a3`.
Hash-manifest SHA-256: `b79c203f33cf6f550b9a9cfb9de924efcff52a933485c0f1115f98c170475c09`.
Initial reviewed full tracked dirty diff (`git diff --binary HEAD`) SHA-256: `6da95ec0165f39ae352880c97d032661d09366401b106325251e93ecc713641d`. This includes unrelated preexisting migration and excludes untracked files; it is an identity, not a reproducible source archive.
Initial TODO SHA-256: `d5c1c686f361df3c331719035f638774a789cb981d4b7ab4ba714b177248409f`.
Implementation later reported repaired TODO SHA-256 `8f772005047470d4119c89b4961df38bb82f67a07e0b9eaccf62baf187fd2fe9`; this does not resolve finding R02 below.

Proposed publication scope is only `docs/work-orders/P0-001/contract.md` and `docs/verification/P0-001/hashes.json`. Scoped working-tree TODO status/handoff edits must also pass preservation review; the external rewrite must not be staged. Review-only evidence is not an implementation change. Historical 658b366 publication predates REQ-14 and is not retroactively independently approved.

## Findings

- R01, Fail: Initial TODO handoff announced next P0-002 and the previous coordinator-reviewed publication; P0-001 register row/anchor was replaced by literal `undefined`. This falsely advanced dependency readiness and broke links. Returned to implementer. Implementer reports corrected handoff and one P0-001 anchor among 4,558 rows; final independent recheck pending.
- R02, Fail: After R01 repair, reviewer independently read TODO lines 7358–7371 and confirmed the Chapter 1 naming table row was replaced by literal `undefined`. Implementer attributes this to its initial broad row matcher. This is an unintended scope/preservation regression, even though the TODO rewrite is not proposed for publication. Restore exact original from retained pre-amendment evidence and recheck all changed lines. Reviewer will not invent or author replacement content.
- No changes to runtime behavior or thresholds were found in the two proposed publication files. Existing ten native failures, browser budget failures, unknown input attribution and incomplete device/worker proof remain explicit.

## Criterion matrix

| Criterion | Independent check and observed outcome | Verdict |
| --- | --- | --- |
| A01 | Python SHA-256 recomputation of all 916 sourceEntries in s001-source-baseline.json: zero mismatches. All 33 artifact entries in identity.json match, encompassing the 20 original S001 artifacts and additional linked baseline evidence. All three entries in hashes.json match. TODO preservation currently fails R01/R02. | Fail for preservation; source/artifact identity passes |
| A02 | Retained Chrome readback has HTTP200, visible localhost8060,216 input events and latest Reset pointer-up900812.7999999523ms. Reviewer independently fetched the two served assets with Python urllib and compared exact bytes to local AppBundle: PCK1095052 bytes SHA c4750a03507b9aba726ebd8695d08b54c432440c299f42f3bca2aeec06bdfeb0; managedWASM2310425 bytes SHA4d126142a28fabcc675a5c63e56907cff25fc0c4d99e913ba77c2d197c1ad784. This HTTP read supplements retained Chrome provenance; it is not new UI proof. | Pass for continuity |
| A03 | Reviewer parsed retained XML independently:76total,66passed,10failed,0skipped,0errors. Five WoundSpring and five Bellows failures remain. Read build-log tail: succeeded,0warnings,0errors,16.69s. No unchanged-source native run was replayed. | Pass as baseline; runtime Fail retained |
| A04 | Independently read all CommittedCompliantContactTests.cs and matched13named XML cases: all Pass. Seven enum invalid-contact cases assert unchanged committed snapshot after rejection/discard; other tests cover lease invalidation, undefined PoseSample, topology and pending-body coherence. This is retained scoped boundary evidence, not worker protocol qualification. | Pass as baseline |
| A05 | Structural JSON equality recomputed for receiver/control/save run versus reset: all true. Normalizing only control ball Z from1.996268 to0 makes entire construction exactly equal to receiver. Saved loaded run equals receiver. Positive final ball[5.0501,1.160114,approximately0]; control[approximately0,-0.11990005,1.996268]. Both simulation outcomes are timeout at3600ticks; positive means physical basket reception, not campaign win. Original observer timeout remains in receiver.failure. Actions use palette/handles and actual Save/Load clicks. | Pass for existing fixture capture |
| A06 | Independently sorted adjacent RAF timestamp deltas and used nearest rank ceil(q*n)-1. Physical796intervals p95/p99=83.59999999997672/84.29999999993015ms; combined838=83.69999999995343/84.10000000009313ms. Animation actions60 toggles/30reveals; combined30toggles/15reveals. Motion file hashes match retained records; native overlap explicitly excludes motion from timing. | Pass for capture, not performance |
| A07 | Recomputed selected compliant production window using start<=timestamp<=start+50000:437intervals,p95=150.09999999999854,p99=159.0,max241.6999999999971ms. Compared stage records: positive tick17.1ms/control10.5ms versus2.5ms; publication.2,animation.1,submissionapproximately.1 are separate percentile summaries, not additive CPU/GPU. 60/90Hz budgets remain failed. | Pass for truthful accounting |
| A08 | Parsed216 input events; preserved unexpected down/up680288.5/680363.2999999523 and682790.5/682873.2000000477 at675.43359375,822.109375. These precede physical start815254.1 and follow animation end662570.4. Parsed20 retained prior-run samples in control-stages. Incident remains unattributed. | Pass for preservation |
| A09 | Read successful production-export tail and current native project/source paths. Historical export invocation absence remains disclosed; no fresh build/export claimed. Both relative Markdown links in contract resolve. | Pass within Baseline stage |
| A10 | Distinct actual agents confirmed; exact diff read; criterion and impact review performed. Unintended TODO regression R02 remains. | Fail |
| A11 | No approved amendment publication yet. Historical receipt is insufficient. | Incomplete |

## Impact matrix and scope justification

| Reachable impact | Expected invariant/intended change | Investigation and result |
| --- | --- | --- |
| Contract and manifest, direct | Historical coordinator sign-off cannot close REQ-14; exact hash maps to amended bytes | Read exact git diff against658b366 and recomputed all manifest hashes. Contract separates A10 snapshot approval from A11 publication and retains Incomplete. Pass. |
| TODO handoff, register, chapter content, direct | Sole handoff blocks successor until terminal review; no task/teaching scope lost | Read active handoff, register and naming table. Discovered R01 and R02 rather than treating uncommitted TODO as outside review. Fail until repaired. |
| P0-002 and all baseline consumers, transitive | No dependency-ready state based on historical coordinator approval; all finite requirements preserved | Read REQ-14, paired workflow, stage table and P0-002 dependency. Missing anchor/false readiness in initial snapshot is consequential; repaired snapshot must be independently checked. |
| Physics/publication callers and authored content | No runtime change implied or certified by documentation |916source hash equality and exact two-file diff provide bounded unchanged-dependency evidence; source test assertions and retained UI configurations examined. Unrelated dirty migration is not newly reviewed or published. |
| Build outputs, worker clocks, memory/lifecycle | Existing bytes unchanged; future proof not waived | Served/local exact hashes and retained build/export records checked. Reset/save equality independently reproduced from retained data. No actual workers, GPU service, clock freshness,20-cycle memory or devices are newly qualified. |
| Performance interpretation and input contamination | No failed/partial timing reclassified as success | Independent timestamp calculations, stage/sample identity checks and retained failures inspected. CPU/GPU non-additivity and uncertain input source remain explicit. |
| Enum identities, palette and campaign | No new executable closed sets, palette changes or reduced scope | Proposed diff adds documentation only. Existing global audit and150-level scope stay open. R02 teaching-content loss blocks preservation approval. |
| Source graph | No graph-only claim of regression absence | Anvil query first not_ready, retry ready/miss for Markdown contract. Direct source/artifact and active-document checks supplied the evidence; graph miss is not no-impact proof. |

## Reviewer commands and evidence

Read-only checks used `git diff -- docs/work-orders/P0-001/contract.md docs/verification/P0-001/hashes.json`, `git rev-parse HEAD`, `git diff --binary HEAD`, `sed -n '73,116p;145,154p' TODO.md`, `sed -n '7358,7371p' TODO.md`, `rg -n 'REQ-14|paired-subagent|P0-001|A0[1-9]|A10|A11|Baseline' TODO.md`, the complete contact-test file, contract/identity/readback/manifest, and retained log tails. Python3 standard-library json/hashlib/xml.etree.ElementTree/math/urllib.request independently parsed and calculated the values above. Exact reproducible calculation definitions:

```python
sha256(Path(path).read_bytes()).hexdigest() == expected
all(sha256(Path(p).read_bytes()).hexdigest() == h for p,h in source["sourceEntries"])
all(sha256(Path(p).read_bytes()).hexdigest() == h for p,h in identity["artifacts"].items())
assembly = ElementTree.parse("docs/s001-native-baseline.xml").getroot().find("assembly")
intervals = sorted(b-a for a,b in zip(times,times[1:]))
p95 = intervals[math.ceil(.95*len(intervals))-1]
p99 = intervals[math.ceil(.99*len(intervals))-1]
restored = fixture["run"] == fixture["reset"]
served = urllib.request.urlopen("http://127.0.0.1:8060/"+asset).read()
exact_bundle = served == Path("CuriousContraptions.web/AppBundle",asset).read_bytes()
```

Tool outputs are retained in this reviewer session; this record retains independently observed raw numeric results and checks, not new runtime measurements. No browser input, code/test/tool changes, build/export, commit or push was performed by the reviewer. This document is reviewer-owned evidence and does not alter acceptance criteria or deliverables.

## Revised snapshot re-review and approval

This section supersedes the initial rejection while retaining its findings. R01 and R02 are resolved by the implementation agent and independently rechecked. SnapshotApproval = Approved. Terminal verdict = Incomplete pending A11 publication verification. No successor work is authorized yet.

Reviewer independently inspected implementation session transcript line190's actual Anvil patch: its first two hunks were identical before/after, while only the P0-001 and Chapter1 rows became undefined. Line189 preserved the original Chapter1 row. Current row matches it exactly: `| 1 | **On a Roll** | First principles; Spring forward; A little bounce of faith |`. The current handoff now explicitly blocks P0-002, and P0-001 status is Incomplete pending the paired gate. No literal undefined line remains. Independently counted 4,558 numbered anchored register rows, exactly one P0-001 anchor. Original acceptance text and chapter scope are preserved. Thus the direct and transitive documentation impacts now pass; no unresolved unintended regression remains in this justified amendment scope.

Exact approved snapshot:
- HEAD `658b366c318a2977d92bed99b5f4893b5d5fa805`.
- Full tracked dirty-diff SHA-256 `4eb5e6955427229595656f4394ca2b86e34eed4c667ceed15d8b407967c97708`.
- Working-tree TODO SHA-256 `ce7624edd6fc1219e30a6ae67226e59b58a6d923714beb5dffdb94f78ef087eb`; do not publish external rewrite.
- Approved publication file `docs/work-orders/P0-001/contract.md` SHA-256 `198c83e9d8256e26550925002e3db54d97b4f001f21ab7766862a42087a307a3`.
- Approved publication file `docs/verification/P0-001/hashes.json` SHA-256 `b79c203f33cf6f550b9a9cfb9de924efcff52a933485c0f1115f98c170475c09`.
- Reused916source entries,33artifact hashes and both bundle hashes remain as independently checked above. Historical identity.json/readback bytes remain unchanged.

Actual distinct session identities were subsequently recovered from session_meta rather than invented: implementer `01a0f6f2-ab92-7a60-a45e-8b02d0f47dc5`; reviewer `01a0f713-c651-7c72-b8f5-60b37ed30afd`. Reviewer session_meta directly identifies agent_path `/root/p0_001_review`, depth1, parent_thread_id `01a0f5f1-ab9a-79a3-9181-0dbb87285ec5`. Provenance files are `/Users/aidan/.codex/sessions/2026/10/01/rollout-2026-10-01T18-11-33-01a0f6f2-ab92-7a60-a45e-8b02d0f47dc5.jsonl` and `rollout-2026-10-01T18-47-43-01a0f713-c651-7c72-b8f5-60b37ed30afd.jsonl` in the same directory. Inspection was limited to identity metadata and the implementer's directly relevant original patch/recovery evidence.

A01–A10 now Pass for this Baseline documentation amendment. Runtime failures remain failures. A11 alone remains publication-dependent: implementer may commit/push exactly the two approved publication files; reviewer must independently inspect committed changed paths, compare blob hashes, verify remote main hash and confirm reviewed dependencies unchanged. No new runtime/deployed behavior is introduced by this documentation amendment, so receipts suffice for its publication-dependent scope; they do not certify runtime deployment or performance. Review-only record publication, if retained separately, does not alter approved deliverable bytes.

## Publication verification and terminal verdict

Terminal verdict = Pass for P0-001 Baseline reconciliation only. SnapshotApproval remains Approved for the exact two-file amendment. This final section supersedes the earlier provisional verdicts. A01–A11 pass within the documented scope; R01/R02 were corrected and independently re-reviewed before publication. Runtime correctness, device qualification, worker integration and browser performance remain incomplete/failing as recorded.

Reviewer independently ran:
- `git show --name-status --format=fuller 887a157a38ece446a92705125ea4cd58cf5ea5ba`: exactly two modified paths, hashes.json and contract.md; no runtime, TODO or unrelated files.
- `git ls-remote origin refs/heads/main`: `887a157a38ece446a92705125ea4cd58cf5ea5ba refs/heads/main`, exit0.
- Python SHA-256 of `git show <commit>:<path>`: contract `198c83e9d8256e26550925002e3db54d97b4f001f21ab7766862a42087a307a3`, manifest `b79c203f33cf6f550b9a9cfb9de924efcff52a933485c0f1115f98c170475c09`; both exactly equal approved and working-tree bytes.
- Rehashed all916source entries and all33artifact entries again after publication: zero mismatches.
- TODO remains exactly `ce7624edd6fc1219e30a6ae67226e59b58a6d923714beb5dffdb94f78ef087eb`, so reviewed scope/dependencies remain unchanged.
- HEAD equals published commit. Postcommit full tracked dirty-diff SHA-256 is `d0aa73607915ef364431690ce7fa4524ec97e6f027b220b30933bd439e6cea8f`; change from precommit identity is expected because approved files moved into HEAD.
- Anvil reviewer-record check returned0warnings, local backend, daemonStatus not-wired; this is not repository-wide compliance.

A11 is Pass. No newly deployed runtime is part of this amendment, and no production-origin runtime proof was replaced by a receipt. P0-002 may now become eligible under the register. Updating active deliverable status remains implementation work requiring the paired workflow; this reviewer-only terminal record is the independent gate, not an edit to the frozen contract or TODO.

## Local TODO receipt reconciliation

Independent read-only review of implementer status/handoff delta: Pass. Current TODO SHA-256 `31a963fbb0091ae4e5d08bcbc0705259ab47373d15c4cb72ee42abe5c1f6a469`. Reversing only the two receipt/status text replacements in memory exactly reproduces approved prior SHA-256 `ce7624edd6fc1219e30a6ae67226e59b58a6d923714beb5dffdb94f78ef087eb`. Independently counted4558numbered anchored rows, oneP0-001anchor and no undefined line. Acceptance criteria, historical failures and scope are byte-preserved. The handoff and P0-001 row correctly cite the terminal Baseline Pass and887a157 receipt, while retaining runtime/qualification failures. P0-002 readiness is now consistent with the terminal gate.

The reviewed TODO delta stays local and unstaged: HEAD lacks the externally rewritten register/handoff contexts, so publishing this delta alone is not possible without publishing unrelated external work. This is a receipt/status reconciliation, not a new runtime deliverable. Review-only evidence through the preceding terminal verdict was published as `7350fc34f4fd8d66dbe26e9b1408b489c693d824`; reviewer independently verified remote main matched that commit and its only added path was this review record.
