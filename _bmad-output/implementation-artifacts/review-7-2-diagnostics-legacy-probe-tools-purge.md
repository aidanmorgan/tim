# Story 7.2 independent review

Reviewer: /root/reviewer. Implementer: /root/implementation. Coordinator: /root. Source commit: aaac712a5b49f6ce8762ef2b6fcb3372d426d0d5. Date: 10 October 2026. Reviewer edits evidence only.

**F2 correction SnapshotApproval Pass; Story 7.2 terminal completion pending exact forward-correction commit verification.** Exact source acceptance read in epics Story 7.2 and ledger §7.2 (raw c82612); actual draft read aa85bd. Scope is 23 ledger roots, 124 tracked files, 1,166 regular files, 264,930,512 bytes, plus empty tools/p0-002-review. The source approximately 2,000 estimate is preserved with the actual observed count; it does not authorize extra deletion. All 1,042 nontracked files are ignored bin/obj outputs. No additional source archive is needed. Exact deletion authorization requires immediate membership/byte drift equality and Anvil before removals.

| Criterion/impact | Current result |
| --- | --- |
| Ledger and deletion membership | Independent per-row counts all match (a98038);23roots/124tracked. No symlinks. |
| Nontracked preservation | All1,042 ignored bin/obj; no new untracked source (1f16f0). |
| Current project/script/config consumers | Tracked files outside purge roots searched; no actual dependency, only sprint prose and ordinary diagnostic test text (b1690b). Active docs tool pointers require planned correction. |
| Retained tools | Coverage, Coverage.Tests, Ownership, Preview, GpuBodyFixture, LifecycleContract, WireContract, TraceAllocations must build Release with warnings errors; remaining tool source Anvil and affected tests pending. |
| Requirements/knowledge | PERF-02 permits naming correction only; enum, metrics, scenarios, UI, overhead acceptance retained. Baseline-pinned citations must remain retrievable after deletion. |
| Runtime applicability | No planned runtime/driver/config delta. Prior7.1 production/unit/Chrome reuse pending final unchanged relevant-input check, no automatic duplicate suite. |
| Publication | Frozen scoped snapshot approval and local committed-tree verification pending; no push. |

Independent classification command:

```sh
python3 - <<'PY'
from pathlib import Path
import re,subprocess
s=Path('docs/planning/elements/legacy-disposition.md').read_text().split('## 7.2 ')[1].split('## 7.3 ')[0]
roots=re.findall(r'^\| `([^`]+)` \| \d+ \|',s,re.M)
tracked=set(subprocess.check_output(['git','ls-files','-z']).decode().split('\0'))
files=[p for r in roots for p in Path(r).rglob('*') if p.is_file()]
extra=[str(p) for p in files if str(p) not in tracked]
ignored=set(subprocess.check_output(['git','check-ignore','--stdin','-z'],input=('\0'.join(extra)+'\0').encode()).decode().strip('\0').split('\0'))
assert ignored==set(extra)
assert all('bin' in Path(p).parts or 'obj' in Path(p).parts for p in extra)
assert not any(p.is_symlink() for r in roots for p in Path(r).rglob('*'))
print('PASS:23roots,',len(files),'regular files;',len(extra),'nontracked files all ignored bin/obj outputs; no symlinks;',sum(p.stat().st_size for p in files),'bytes')
PY
```

Raw result1f16f0:

```text
PASS:23roots, 1166 regular files; 1042 nontracked files all ignored bin/obj outputs; no symlinks; 264930512 bytes

```

No current concrete plan defect. Runtime/Battery/full Bumper and later purge slices retain separate identities and incomplete gates. Keep the same implementation/reviewer pair through fixes.


## F1 — retained fixture classification contradicted current architecture

Post-deletion owner build found eight compile errors in GpuBodyFixture. Independent actual consumer inspection (bf846b,c140c5,2cda10) shows this is more than compilation: Program constructs Half velocity scaled by1/32, main.js reads Half velocity at64 and multiplies32, and worker dispatches legacy shader-f16 body-integration.wgsl with old Half velocity lanes. Current CanonicalBody keeps80bytes but stores f32 m/s at64/68/72; numeric authority explicitly lists these fixture-only WGSL kernels for future deletion (08d9ad). A compile-only repair would leave a broken tool and upgrading it would retain a second obsolete solver. Coordinator reconsidered its initial repair instruction and directed obsolete-tool retirement assessment within7.2, preserving shared WGSL for the later named gate.

Independent expanded-scope check f06a71/ebb305: five tracked fixture files,265regular files/60,280,924bytes at check time,260ignored bin/obj outputs, no untracked source/symlinks. No solution/workflow dependency found. Current references are the inventory's mistaken “Current fixture”, the numeric migration table and roadmap's historical spike paragraph; no owner keep ruling exists. **F1 remains open until corrected explicit ledger/classification, exact expanded deletion/consumer checks and remaining seven-tool verification.** Original23root deletion approval did not cover this extra root. No runtime source or shared WGSL change authorized by this finding.


## F1 closure, final verification and SnapshotApproval

**F1 resolved.** Corrected actual ledger/spec/current authority diff independently read59e127/f29915; added fixture inventory matched all265files (ca5479). Expanded deletion explicitly approved before removal. Final24ledger roots are absent, exact129tracked deletion set matches, sharedWGSL/currentCanonicalBody/runtime/UI/driver/export untouched (564691). Total1,431regular files/325,211,436bytes includes1,302ignored generated files. Four distinct scope source citations remain retrievable from baseline Git (2be5bd); first citation probe matched zero due wrong spelling and was corrected, never used as preservation proof. No other known violation remains.

Implementer raw evidence independently inspected: `.anvil/story-7-2-builds.json` records six zero-warning Release warning-as-error builds plus failed obsolete fixture evidence. Coverage.Tests seventh build command/output retained separately at `.anvil/story-7-2-build-test-terminal.txt` (original38e4b9/d22f76), also zero warnings/errors. Checks JSON retains complete Lifecycle510/0fail and Wire109/0fail native supplemental contracts, Coverage40/0fail and remaining-source Anvil0warnings. Node exact command and terminal85/85pass summary retained in same terminal file (original3a5676/821b5c,2955.146667ms); initial tool truncation disclosed, no test failure hidden or repeat required. Two long rawJSON lines exceeded Anvil scanning; this limitation is preserved. Independently parsed all contract results, not only owner summary. These native tool checks do not claim runtime qualification.

Runtime proof reuse: Story7.1 source/runtime/driver/export relevant inputs unchanged. Its original same-reviewer45Chrome cases (41initial+4corrected), production/build/unit identities and known warnings remain applicable. Exact221file diagnostic bundle plus8servedHTTP identities independently rechecked d06baf against unchanged manifest0f861841a4019e4cf3a5548859c0db8d4740506d4f8205d7442a9b9c8634dbdd. Environment/origin and production source applicability unchanged; no new runtime behavior claim or broad repeat needed. Documentation changes only correct obsolete command names/classification, preserve PERF02 metrics/scenarios/UI/overhead acceptance and report current statuses (eb2ba0/f05324).

**SnapshotApproval: Pass.** Exact snapshot `d9ae5d596e4413510657ed748825b6333d40255a2f71e7047a7f57418f85169e`, source commit `aaac712a5b49f6ce8762ef2b6fcb3372d426d0d5`. Actual scoped diff `.anvil/story-7-2-final.patch` SHA256 `64ee9313ea2e6d27cd62d048c69b42b3c2387ee4f7e74a6c357d7f54d09a8123`. All pre-publication criteria satisfied within scope. Approve local142path commit:11candidate+129deletions+snapshot+this review record. Six unrelated dirty files excluded. Only publication-dependent check is exact local commit/tree/membership and exclusions preservation. No push/deployment. Battery/fullBumper and7.3/7.4 remain separate.

Independent final commands/results:

```sh
python3 - <<'PY'
import pathlib,json,subprocess,re,hashlib
git=lambda *x:subprocess.check_output(['git',*x])
ledger=pathlib.Path('docs/planning/elements/legacy-disposition.md').read_text().split('## 7.2 ')[1].split('## 7.3 ')[0]
roots=re.findall(r'^\| `([^`]+)` \| (\d+) \|',ledger,re.M)
expected=set()
for r,n in roots:
 files=git('ls-tree','-r','--name-only','HEAD',r).decode().splitlines();assert len(files)==int(n);expected.update(files);assert not pathlib.Path(r).exists()
actual=set(git('diff','--name-only','--diff-filter=D').decode().splitlines());assert expected==actual and len(actual)==129
assert not git('diff','--name-only','--','engine','CuriousContraptions.Simulation','CuriousContraptions.Animation','CuriousContraptions.Animation.Worker','CuriousContraptions.web','ui','parts','tools/e2e','export_presets.cfg')
citations=set()
for p in pathlib.Path('docs/planning/elements').glob('*.md'):
 for commit,path in re.findall(r'`([0-9a-f]{7,40}):([^`]+?)\s+L\d+',p.read_text()):
  if any(path.startswith(root) for root,_ in roots):citations.add((commit,path))
for commit,path in citations:git('cat-file','-e',commit+':'+path)
checks=json.load(open('.anvil/story-7-2-checks.json'))
for name,r in checks.items():
 if 'stdout' in r and r['stdout'].startswith('{'):
  j=json.loads(r['stdout']); print(name,'exit',r['exit_code'],'count',j.get('Count'),'failures',j.get('Failures'));assert r['exit_code']==0 and j.get('Failures')==0
 else: print(name,str(r)[-600:])
print('PASS24roots/129exact tracked deletions; runtime/UI/driver/export unchanged; resolvable pinned scope citations',len(citations))
PY
python3 - <<'PY'
import pathlib,re,subprocess
s=pathlib.Path('docs/planning/elements/legacy-disposition.md').read_text().split('## 7.2 ')[1].split('## 7.3 ')[0];roots=re.findall(r'^\| `([^`]+)` \| \d+ \|',s,re.M);refs=set()
for p in pathlib.Path('docs/planning/elements').glob('*.md'):
 for path,commit in re.findall(r'`([^`@]+)@([0-9a-f]{7,40}):L\d+',p.read_text()):
  if any(path.startswith(r) for r in roots):refs.add((commit,path))
assert refs
for c,p in refs:subprocess.run(['git','cat-file','-e',c+':'+p],check=True)
print('PASS',len(refs),'unique scope source citations resolve from git')
PY
python3 - <<'PY'
import json,pathlib,hashlib,urllib.request
p=pathlib.Path('.anvil/story-7-1-diagnostic-identity.json');assert hashlib.sha256(p.read_bytes()).hexdigest()=='0f861841a4019e4cf3a5548859c0db8d4740506d4f8205d7442a9b9c8634dbdd';m=json.loads(p.read_text());root=pathlib.Path('CuriousContraptions.web/AppBundle');actual={str(f.relative_to(root)) for f in root.rglob('*') if f.is_file()};expect={e['path'] for e in m['files']};assert actual==expect
for e in m['files']:
 b=(root/e['path']).read_bytes();assert len(b)==e['bytes'] and hashlib.sha256(b).hexdigest()==e['sha256'],e['path']
for name in ['index.html','godot.pck','_framework/CuriousContraptions.wasm','simulation/worker.js','simulation/_framework/dotnet.boot.js','simulation/_framework/CuriousContraptions.Simulation.wasm','animation/worker.js','workshop-client.js']:
 e=next(x for x in m['files'] if x['path']==name);h=hashlib.sha256(urllib.request.urlopen('http://127.0.0.1:8060/'+name).read()).hexdigest();assert h==e['sha256'];print('HTTP',name,h)
print('PASS exact diagnostic bundle file membership/bytes/hashes',len(actual),'PlaytestDiagnostics',m['PlaytestDiagnostics'])
PY
python3 - <<'PY'
import json,pathlib,hashlib,subprocess
p=pathlib.Path('_bmad-output/implementation-artifacts/story-7-2-snapshot.json');h=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest();assert h(p)=='d9ae5d596e4413510657ed748825b6333d40255a2f71e7047a7f57418f85169e';s=json.loads(p.read_text())
for key in ['candidate','current_inputs','kept_tools','excluded_dirty_files']:
 for p,d in s[key]:assert h(p)==d,p
d=dict(s['deleted_tracked_git_blobs']);assert len(d)==129
for p,blob in d.items():assert subprocess.check_output(['git','rev-parse',s['source_commit']+':'+p]).decode().strip()==blob
assert h('.anvil/story-7-2-final.patch')=='64ee9313ea2e6d27cd62d048c69b42b3c2387ee4f7e74a6c357d7f54d09a8123'
print('PASS exact snapshot;11candidate,129deleted blobs,80kept tools,6excluded files and all current inputs match. Expected142commitpaths.')
PY
```

```text
564691:
LifecycleContract exit 0 count 510 failures 0
WireContract exit 0 count 109 failures 0
Coverage.Tests {'command': ['dotnet', 'tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll', '-noColor'], 'exit_code': 0, 'stdout': "xUnit.net v3 In-Process Runner v3.2.2+728c1dce01 (64-bit .NET 10.0.12)\n  Discovering: Coverage.Tests\n  Discovered:  Coverage.Tests\n  Starting:    Coverage.Tests\n  Finished:    Coverage.Tests (ID = '4e6c59154fec9dd2045e69debfe442ec5f2ba9cf6c2c685790a4f3461ccd1e4f')\n=== TEST EXECUTION SUMMARY ===\n   Coverage.Tests  Total: 40, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.193s\n", 'stderr': ''}
Anvil , 'tools/e2e/engine-core-2b3.test.ts', 'tools/e2e/engine-core-2b4.test.ts', 'tools/e2e/engine-core-2c.test.ts', 'tools/e2e/workshop-driver.ts', 'tools/workshop-animation-output.test.mjs', 'tools/workshop-client-lifecycle.test.mjs', 'tools/workshop-console.mjs', 'tools/workshop-console.test.mjs', 'tools/workshop-isolation.test.mjs', 'tools/workshop-observation.test.mjs', 'tools/workshop-pose-ring.test.mjs', 'tools/workshop-rigid-body.test.mjs', 'tools/workshop-save-storage.test.mjs'], 'exit_code': 0, 'stdout': '\n  ✓ No warnings found\n', 'stderr': 'Last run written to .anvil/last-check.txt\n'}
PASS24roots/129exact tracked deletions; runtime/UI/driver/export unchanged; resolvable pinned scope citations 0

2be5bd:
PASS 4 unique scope source citations resolve from git

d06baf:
HTTP index.html a9ce285101eff2a600c2275d9b133ea95e357cf8c974545993c74818fd3346ef
HTTP godot.pck 8062013e38eb62dc6c99662b3ac3745f9b7634bab0c966458c572ce12dbb535d
HTTP _framework/CuriousContraptions.wasm f380b187b7cf039eb958a14544d00da2c193632f403ec301da5c7bdae7c60264
HTTP simulation/worker.js 4116abb0d8fb6e0257b435bb5f13023a7ace758f567ac72bed6b56e94330b04a
HTTP simulation/_framework/dotnet.boot.js 1b769f95961f7debeffadfe41e7ab73b7224a96de93a951bd3816a8537e6e8b5
HTTP simulation/_framework/CuriousContraptions.Simulation.wasm 6c3b798bf6149a4df1b29206d72d57dc929fc66305357ee6567a0ab59c2bed83
HTTP animation/worker.js 9b0e0259ee0a1eab08004ff7110a2d8a7acaeb00de753b9d8e1a81245e6b9c12
HTTP workshop-client.js 1ff22feed79266984e2867931440a17c3c26a89f1e8bd5dc00a63b61866f26c8
PASS exact diagnostic bundle file membership/bytes/hashes 221 PlaytestDiagnostics True

ab70bf:
PASS exact snapshot;11candidate,129deleted blobs,80kept tools,6excluded files and all current inputs match. Expected142commitpaths.

```


## Postcommit receipt — terminal scoped Pass

Independent reviewer verified local commit `90db56b54fd4d5320cc5ce07ec2d921828951bf2`, tree `88962cbd241e74e42b1c6c91e679b71cdcac85a0`. Exact142paths,11candidate hashes,129tool deletions, snapshot and reviewed evidence bytes match approval. Six excluded dirty files preserved and their committed bytes unchanged; index empty. **Story7.2 terminal scoped Pass**, no push. No global/runtime/Battery/fullBumper or future purge qualification implied. This postcommit review-only receipt needs no recursive evidence commit.

```sh
python3 - <<'PY'
import pathlib,json,subprocess,hashlib
commit='90db56b54fd4d5320cc5ce07ec2d921828951bf2'
snap='_bmad-output/implementation-artifacts/story-7-2-snapshot.json'
review='_bmad-output/implementation-artifacts/review-7-2-diagnostics-legacy-probe-tools-purge.md'
git=lambda *a:subprocess.check_output(['git',*a])
s=json.loads(pathlib.Path(snap).read_text())
assert git('rev-parse','HEAD').decode().strip()==commit
assert git('rev-parse',commit+'^').decode().strip()==s['source_commit']
expected={p for p,h in s['candidate']}|{p for p,h in s['deleted_tracked_git_blobs']}|{snap,review}
actual=set(git('diff-tree','--no-commit-id','--name-only','-r',commit).decode().splitlines())
assert actual==expected and len(actual)==142
for p,h in s['candidate']:assert hashlib.sha256(git('show',commit+':'+p)).hexdigest()==h,p
for p in [snap,review]: assert git('show',commit+':'+p)==pathlib.Path(p).read_bytes(),p
assert all(not git('ls-tree',commit,p) for p,h in s['deleted_tracked_git_blobs'])
for p,h in s['excluded_dirty_files']:
 assert hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()==h,p
 assert git('show',commit+':'+p)==git('show',s['source_commit']+':'+p),p
assert not git('diff','--cached','--name-only')
print('PASS exact commit',commit,'tree',git('rev-parse',commit+'^{tree}').decode().strip())
print('142 exact paths;11candidate hashes+snapshot+review committed bytes match;129tool deletions;6excluded dirty files preserved and not committed;index empty.')
PY
```

Raw3234df:

```text
PASS exact commit 90db56b54fd4d5320cc5ce07ec2d921828951bf2 tree 88962cbd241e74e42b1c6c91e679b71cdcac85a0
142 exact paths;11candidate hashes+snapshot+review committed bytes match;129tool deletions;6excluded dirty files preserved and not committed;index empty.

```


## F2 reopened after Story 7.4 dependency inspection

**Known regression; current Story7.2 verdict Fail.** Implementer found and reviewer independently confirmed (f25025) an omitted C# consumer: tools/Ownership/SourceInventory.cs line13 defines GeometryProject, Capture line129 unconditionally resolves that now-deleted project, and web configuration identities at120 include it. Retained --capture/--members cannot execute with the removed file despite successful compilation. The earlier project/script/config scan did not cover C# path strings; its “no active consumer” conclusion and resulting7.2 terminal Pass are invalidated. This is not excused by tool-build success or unrelated pre-existing audit incompleteness.

Same implementation owner must remove the obsolete dependency forward, update typed contexts/current callers and README, and execute actual retained tool capture/control checks. No archived fallback or replacement legacy model. Existing ownership audit's deleted work-register dependency predates7.2; retain that as an explicit separate current-authority defect and fix within the justified tool closure, without claiming old assignment evidence is fresh. Coordinator instructed no7.4deletion before bounded correction independently verifies. Original commit90db56b and its exact tree verification remain factual; they no longer establish complete acceptance. Review ownership stays /root/reviewer with /root/implementation.


## F2 forward correction SnapshotApproval

Actual diff395d64 removes obsolete Geometry project resolution/config/reference/inspection context and its obsolete oracle fixture. Current five contexts remain explicit; retired numeric4 rejects, current enum external identities retained. Independent direct command `dotnet tools/Ownership/bin/Release/net10.0/Ownership.dll --oracles` exited0 (0c439b); affected portable positive plus15negative controls retained, full rawbody available in owner rawfile after consoletruncation. Complete raw `.anvil/story-7-2-f2-oracles.json` SHA256b38682b9891fb37b60daff6b7e283641ef8d51ba19432490db17e80bb3b0bd45 independently parsed:22general and15portable rejections all pass. Actual integrated `--members .` exits0 and complete746640byte raw JSON SHA256247c6c36ea6f062e08387c08bca220545ae32049ca32d11e6c674409216f2d03 retained at `.anvil/story-7-2-f2-members.json`; independent9b9b56 verified435source hashes,1260members,zero binding diagnostics,contexts0/1/2/3/5 and noGeometry. No substitute fixtures stand in for actual capture. Zero-warning affected build and Anvil scan inspected via spec/raw8b0bed/ee79ba; original MSB1009 failure8d21da retained. Pre-existing deleted-register/stale-assignment audit remains Story7.4 prerequisite, explicitly not solved or qualified here.

**F2 resolved, SnapshotApproval Pass** for immutable `story-7-2-f2-snapshot.json` SHA2568adf86d917074bb120130bcbf2cd2debae967f7f4f937c9a47c38e24521837da **plus one exact README replacement** SHA256db713c368bfd399714dcb51f2053e93463c322a25a0af06303c36d7b282081dd. The replacement only corrects two historical five→six authored Animation source counts, independently confirmed in actual capture. Original frozen unifieddiff hashf825dc5e13673e7cd87eb1e79ea0cfeca0cdfafa4843329e9f4ca7c166987577 remains its original boundary; this stated README delta is included in approval. No other postfreeze delta. Approve9path localcommit=7candidate+correctionsnapshot+this same review record. Only remaining publication check: exact committed bytes/membership/preservation. No runtime/browser/export mutation or new gameplay proof claim; managed capture builds do not replace the unchanged served bundle. No push.

```sh
python3 - <<'PY'
import pathlib,json,hashlib
h=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
p='_bmad-output/implementation-artifacts/story-7-2-f2-snapshot.json';assert h(p)=='8adf86d917074bb120130bcbf2cd2debae967f7f4f937c9a47c38e24521837da';s=json.load(open(p))
for key in ['candidate','inputs','excludedDirty','rawEvidence']:
 for x in s[key]:
  expected='db713c368bfd399714dcb51f2053e93463c322a25a0af06303c36d7b282081dd' if x['path']=='tools/Ownership/README.md' else x['sha256']
  assert h(x['path'])==expected,x['path']
assert h(s['artifact']['path'])==s['artifact']['sha256']
m=json.load(open('.anvil/story-7-2-f2-members.json'));a=next(x for x in m['Symbols'] if x['Kind']==5);assert sum(p['Value'].startswith('engine/presentation/') for p in a['CompilePaths'])==6
print('PASS immutable F2 snapshot plus exact README replacement;7candidate/4inputs/7excluded/2rawoutputs/OwnershipDLL match;Animation has6authoredsources.')
PY
```

Raw3725e9:

```text
PASS immutable F2 snapshot plus exact README replacement;7candidate/4inputs/7excluded/2rawoutputs/OwnershipDLL match;Animation has6authoredsources.

```
