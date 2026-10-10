# Story 7.3 independent review

Reviewer /root/reviewer; implementer /root/implementation; coordinator /root. Source90db56b54fd4d5320cc5ce07ec2d921828951bf2. Date10October2026. Reviewer edits evidence only.

**SnapshotApproval Pass; terminal completion pending exact local committed-tree verification.** Actual source acceptance read99e11e and spec3d8502 require exact obsolete-test deletion and solution tests100%pass. Independent inventory:417ledgerpaths=416uncompiledC# plus WoundSpringTests.cs.orig,3,308,638bytes. Alltracked/no symlinks;442rootC# minus26explicit local compile inputs exactly equals416. Independent evaluatedMSBuild36items equal retainedbefore and have no overlap417deletions. Authorized only this set after immediate byte/membership drift equality and Anvil.

| Criterion/impact | Result/pending |
| --- | --- |
| Deletion/compilation membership | Entry Pass below; final exact deletion/unchanged36items pending |
| Current tests/fixtures | Preserve26local+2linkedSimulation+8package sources, testdata, bin/obj and assertions; final hashes pending |
| Acceptance | Required dotnet test solution643currentbaseline; no failures, no new warnings; pending |
| Knowledge/links | Baseline-pinned deleted-source citations must still resolve; final check pending |
| Runtime | No planned runtime/driver delta; prior7.1 proof reuse requires final relevant-input identity check |
| Publication | Exact snapshot approval and localcommitted-tree verification pending; no push |

Raw independent commands:

```sh
python3 - <<'PY'
from pathlib import Path
import re,xml.etree.ElementTree as E,subprocess
s=Path('docs/planning/elements/legacy-disposition.md').read_text().split('## 7.3 ')[1].split('## 7.4 ')[0];paths=re.findall(r'^\| `([^`]+)` \|',s,re.M)
assert len(paths)==417 and len(set(paths))==417
local={str(Path('CuriousContraptions.tests')/e.attrib['Include']) for e in E.parse('CuriousContraptions.tests/CuriousContraptions.tests.csproj').iter('Compile') if not e.attrib['Include'].startswith('../')}
allcs={str(p) for p in Path('CuriousContraptions.tests').glob('*.cs')}
assert allcs-local=={p for p in paths if p.endswith('.cs')}
assert all(Path(p).is_file() and not Path(p).is_symlink() for p in paths)
tr=set(subprocess.check_output(['git','ls-files','-z','CuriousContraptions.tests']).decode().split('\0'));assert set(paths)<=tr
print('PASS417ledgerpaths=416uncompiledcs+oneorig;442rootcs minus26explicitcompiled exactly deletion set;',sum(Path(p).stat().st_size for p in paths),'bytes; no symlinks; tracked all.')
PY
python3 - <<'PY'
import json,subprocess,re,pathlib
r=subprocess.check_output(['dotnet','msbuild','CuriousContraptions.tests/CuriousContraptions.tests.csproj','-getItem:Compile','-p:Configuration=Release','--nologo']);s=json.loads(r);old=json.load(open('.anvil/story-7-3-compile-before.json'));assert s==old
rows=pathlib.Path('docs/planning/elements/legacy-disposition.md').read_text().split('## 7.3 ')[1].split('## 7.4 ')[0];deleted={str(pathlib.Path(p).resolve()) for p in re.findall(r'^\| `([^`]+)` \|',rows,re.M)}
items=s['Items']['Compile'];assert len(items)==36 and not deleted.intersection(x['FullPath'] for x in items)
print('PASS independently evaluated36Compileitems equal retainedbefore; no overlap417deletions')
PY
```

```text
e54626:
PASS417ledgerpaths=416uncompiledcs+oneorig;442rootcs minus26explicitcompiled exactly deletion set; 3308638 bytes; no symlinks; tracked all.

9deca1:
PASS independently evaluated36Compileitems equal retainedbefore; no overlap417deletions

```

No concrete plan finding. Otherstories/Battery/fullBumper retain separate identities and gates.


## Frozen candidate verification and SnapshotApproval

**SnapshotApproval Pass** for `fab090dd5cc0a05a76742c2fc42866990c6116af30c9fac68491f95bcc1e19f0`, source90db56b54fd4d5320cc5ce07ec2d921828951bf2. Independent final deletion/preservation check d5fca8 confirms417exactdeletedpaths,36evaluatedCompileobjects identical,26localactive source bytes identical,229unique baseline-pinned deleted-source citations resolve. Complete `.anvil/story-7-3-unit.log` inspected:643passed/0failed/0skipped/exit0,1second reportedtestduration; original commands/results5937b6/5528c7 retained. Five known xUnit2013 warnings remain in unchanged test assertions and no new warning introduced. Project change is comment-only; active assertions/membership/package references untouched. Current docs/status/ledger diff48679c/7fc099 preserves all source acceptance, clearly retains pending committed-tree check despite BMAD task/status completion marks.

Previous same-reviewer7.1 production/Chrome proof remains applicable: source runtime, UI, driver, export and prior manifests unchanged; exact221file/8HTTP artifact identity was established again during7.2(d06baf) and no bundle rebuild or runtime source edit occurred in7.3. No full Chrome/build duplicate justified. This scope does not establish global warning-free runtime, Battery/fullBumper or7.4completion.

Actual scoped diff `.anvil/story-7-3-final.patch` SHA256cb97ee8e693444581f03e929ee9dca492c02ca987a9c2285b0f4e29a935c3bd9. Final snapshot binds8candidatefiles,417deletionblobs,36compiledinputs,13currentinputs and7excludeddirtyfiles, independently verified2116ea. All pre-publication criteria satisfied with zero unresolved unintended regressions within this scope. Authorize exact427path localcommit=8candidate+417deletions+snapshot+review. Only publication-dependent check: committed-tree exactbytes/membership and excludedwork preservation. No push/deployment. Review-only receipt does not create another gate.

```sh
python3 - <<'PY'
from pathlib import Path
import subprocess,json,re
git=lambda *a:subprocess.check_output(['git',*a])
s=Path('docs/planning/elements/legacy-disposition.md').read_text().split('## 7.3 ')[1].split('## 7.4 ')[0];paths=set(re.findall(r'^\| `([^`]+)` \|',s,re.M));assert len(paths)==417
assert set(git('diff','--name-only','--diff-filter=D').decode().splitlines())==paths
for p in paths:assert not Path(p).exists()
before=json.load(open('.anvil/story-7-3-compile-before.json'));after=json.loads(subprocess.check_output(['dotnet','msbuild','CuriousContraptions.tests/CuriousContraptions.tests.csproj','-getItem:Compile','-p:Configuration=Release','--nologo']));assert before==after
local={i['Identity'] for i in before['Items']['Compile'] if '/' not in i['Identity']};assert len(local)==26
assert {p.name for p in Path('CuriousContraptions.tests').glob('*.cs')}==local
for p in local:assert Path('CuriousContraptions.tests',p).read_bytes()==git('show','HEAD:CuriousContraptions.tests/'+p)
refs=set()
for p in Path('docs/planning/elements').glob('*.md'):
 for path,c in re.findall(r'`([^`@]+)@([0-9a-f]{7,40}):L\d+',p.read_text()):
  if path in paths:refs.add((c,path))
assert refs
for c,p in refs:git('cat-file','-e',c+':'+p)
assert not git('diff','--name-only','--','engine','CuriousContraptions.Simulation','CuriousContraptions.Animation','CuriousContraptions.Animation.Worker','CuriousContraptions.web','ui','parts','tools/e2e','export_presets.cfg')
log=Path('.anvil/story-7-3-unit.log').read_text();assert 'Passed:   643' in log and 'Failed:     0' in log
print('PASS417exactdeletions;36evaluatedCompileidentical;26localactivebyteidentical;all',len(refs),'unique baseline source citationsresolve;runtime/driver/exportunchanged;643tests0fail.')
print(log[-750:])
PY
python3 - <<'PY'
import json,pathlib,hashlib,subprocess
p=pathlib.Path('_bmad-output/implementation-artifacts/story-7-3-snapshot.json');h=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
assert h(p)=='fab090dd5cc0a05a76742c2fc42866990c6116af30c9fac68491f95bcc1e19f0';s=json.loads(p.read_text())
for key in ['candidate','current_inputs','compiled_inputs','excluded_dirty_files']:
 for p,d in s[key]:assert h(p)==d,p
d=dict(s['deleted_tracked_git_blobs']);assert len(d)==417
for p,b in d.items():assert subprocess.check_output(['git','rev-parse',s['source_commit']+':'+p]).decode().strip()==b
assert h('.anvil/story-7-3-final.patch')=='cb97ee8e693444581f03e929ee9dca492c02ca987a9c2285b0f4e29a935c3bd9'
print('PASS exactsnapshot:8candidate/417deletedblobs/36compileinputs/13currentinputs/7excludedfiles; actualdiff hashmatches;expected427commitpaths.')
PY
```

```text
d5fca8:
PASS417exactdeletions;36evaluatedCompileidentical;26localactivebyteidentical;all 229 unique baseline source citationsresolve;runtime/driver/exportunchanged;643tests0fail.
[1mfirst_scan_filesystem[22m
  [0m
  [   0% ] [90m[1mloading_editor_layout[22m | Started Loading editor (5 steps)[39m[0m
  [   0% ] [90m[1mloading_editor_layout[22m | Loading editor layout...[39m[0m
  [  16% ] [90m[1mloading_editor_layout[22m | Loading docks...[39m[0m
  [92m[ DONE ][39m [1mloading_editor_layout[22m
  [0m
  TwoDog: Imported Godot project /Users/aidan/dev/personal/tim
Test run for /Users/aidan/dev/personal/tim/CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   643, Skipped:     0, Total:   643, Duration: 1 s - CuriousContraptions.tests.dll (net10.0)

Exit code: 0


2116ea:
PASS exactsnapshot:8candidate/417deletedblobs/36compileinputs/13currentinputs/7excludedfiles; actualdiff hashmatches;expected427commitpaths.

```
