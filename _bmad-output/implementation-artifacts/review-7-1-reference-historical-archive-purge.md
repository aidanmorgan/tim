# Story 7.1 independent review

Reviewer: /root/reviewer. Implementation owner: /root/implementation. Coordinator: /root. Date:10 October2026. Root:/Users/aidan/dev/personal/tim. Source documentation commit:34ad0ae0a64fc56336022de0ada49774098492cf. Reviewer has changed no deliverable.

**SnapshotApproval: Pass for the exact candidate recorded below; terminal completion pending local committed-tree verification.** Exact reference deletion, archive preservation, post-deletion builds/unit/Node checks and all45 current Chrome acceptance cases are verified. One CAT023b diagnostic-classifier defect was fixed and independently re-reviewed below. This is the single ongoing independent review record; final candidate identity and build/Chrome evidence are recorded below; the local commit receipt follows after verification.

## Criterion and impact matrix

| Criterion | Independent result / remaining check |
| --- | --- |
| Source acceptance and bounded deletion | Story7.1 epics AC and LEGACY-0a reviewed; all existing unit/Playwright suites expressly required, despite deletion-only scope |
| Ledger preservation |392 tracked files match all26 reference rows with exact counts; no local tracked reference edits; Story7.0 committed-tree proof retained |
| Untracked/ignored preservation |2474 untracked files are exactly two1237-file bundles;1816 ignored files are build/import outputs including archived export templates; no symlinks |
| Current consumers/docs |13 Markdown files historical; scan of tracked source/config/docs finds only four export exclusions as actual current consumers; no active incoming Markdown link found |
| Recovery design | Exact prearchive paths/sizes/modes/SHA/directory set; reject duplicate/unsafe/special members; separate extraction; compare all bytes/membership; recheck originals immediately before deletion. Plan concern resolved in resumed six-step procedure |
| Staged artifact | Independent tar stream and original/recovered full-set checks Pass below |
| Durable archive | Owner10October instruction explicitly superseded external storage with keeping everything inside tim; retained archive verified below |
| Deletion | Exact4682-file reference tree authorized after retained archive verification, fresh manifest/inventory comparison and mandatory Anvil; actual deletion result pending |
| Builds/tests/Chrome/lifecycle | Pending post-deletion active solution build/unit, production and diagnostic export, affected Node and complete existing serial actual Chrome suites; no stale Bumper worktree proof substituted |
| Local commit | Pending exact candidate SnapshotApproval and independent committed-tree verification; no push/deployment |

## Plan inspection evidence

Original commands/results retained in reviewer transcript:425b60 (TODO/spec);b28cb5 (skill/roadmap);d73e9c and982090 (ledger,consumers,status);8b3224 (independent file/ledger enumeration);6ca2a4 (tracked consumer scan);16837b (13 Markdown headers/solution);74a369 (revised six-step recovery procedure). Inventory:4682 regular files,1957553725bytes=392tracked+2474untracked+1816ignored. All26 ledger row counts match; no tracked reference modifications; no symlinks. Source-free prose matches were distinguished from actual dependencies. Two initial ancillary searches used unsupported Git negative pathspec syntax and a nonexistent guessed driver filename; corrected tracked-file scan and actual workshop-driver.ts read were used, not those failed searches as proof.

## Exact staged identity

- Archive:/Users/aidan/dev/personal/tim/.anvil/story-7-1-staging/p025-appbundles.tar.gz
- Archive bytes:208369673; SHA256:5267bbe9fda12ee295b0b044fdc05bf14295e687eaddd740a3545356b13a1d0a
- Manifest:/Users/aidan/dev/personal/tim/.anvil/story-7-1-staging/members.json
- Manifest SHA256:9ae5e8efcfee451939d05604744cbdd91dbd511ba98f061cb36f4cc1bbf3c13b
- Recovery:/Users/aidan/dev/personal/tim/.anvil/story-7-1-staging/recovered
- Source:/Users/aidan/dev/personal/tim/reference/p025-production-isolated-20261004
- Scope:2474 regular files,12directories,402121675 original file bytes. No external copy and originals untouched.

Implementer reports digest-bound Anvil preview validation for binary archive/recovery writes (partial scans, not full binary policy scan); full manifest validation warned on18 ICU filename strings. These limits are retained explicitly. Reviewer read original verifier and raw result but used an independently written read-only check for acceptance. Existing recovered tree is compared in full; no second extraction/write was needed to validate actual restored bytes.

### Independent raw command

```sh
python3 - <<'PY'
from pathlib import Path,PurePosixPath
import hashlib,json,tarfile,stat
stage=Path('.anvil/story-7-1-staging'); archive=stage/'p025-appbundles.tar.gz'; manifest=stage/'members.json'; m=json.loads(manifest.read_text()); entries=m['entries']; expected={e['path']:e for e in entries}; assert len(entries)==len(expected)
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
assert sha(archive)=='5267bbe9fda12ee295b0b044fdc05bf14295e687eaddd740a3545356b13a1d0a'
assert sha(manifest)=='9ae5e8efcfee451939d05604744cbdd91dbd511ba98f061cb36f4cc1bbf3c13b'
seen=set()
with tarfile.open(archive,'r:gz') as tf:
 for t in tf:
  n=t.name.rstrip('/'); parts=PurePosixPath(n).parts
  assert n not in seen and n in expected and not n.startswith('/') and '..' not in parts
  seen.add(n); e=expected[n]; assert t.mode==e['mode']
  if e['type']=='file':
   assert t.isfile() and t.size==e['size']
   h=hashlib.sha256()
   with tf.extractfile(t) as f:
    for b in iter(lambda:f.read(1048576),b''):h.update(b)
   assert h.hexdigest()==e['sha256'],n
  else:assert t.isdir()
assert seen==set(expected)
source=Path(m['source']); recovered=stage/'recovered'
for label,base in [('source',source),('recovered',recovered)]:
 actual=set()
 roots=[base/'diagnostic-appbundle',base/'production-appbundle'] if label=='source' else list(base.iterdir())
 for root in roots:
  for p in [root,*root.rglob('*')]:
   n=p.relative_to(base).as_posix();assert n in expected and n not in actual,(label,n);actual.add(n);e=expected[n];s=p.lstat();assert not p.is_symlink() and stat.S_IMODE(s.st_mode)==e['mode']
   if e['type']=='file':assert stat.S_ISREG(s.st_mode) and s.st_size==e['size'] and sha(p)==e['sha256'],(label,n)
   else:assert stat.S_ISDIR(s.st_mode)
 assert actual==set(expected)
 print(label,'exact entries/modes/hashes',len(actual))
print('PASS independently verified staged archive208369673 bytes,2474 files,12 directories,402121675 original bytes; external copy and deletion NOT verified or authorized')
PY
```

Original execution3345e4/session65736, completion5a77da, exit0:

```text
source exact entries/modes/hashes 2486
recovered exact entries/modes/hashes 2486
PASS independently verified staged archive208369673 bytes,2474 files,12 directories,402121675 original bytes; external copy and deletion NOT verified or authorized

```

The command independently rejects unsafe/duplicate/unexpected tar members, hashes all streamed archive file bytes, compares mode/size/type and full original/recovered membership and hashes, and includes unexpected recovery top-level entries in rejection. External-copy identity/recovery and fresh predelete source checks remain necessary.


## Owner location supersession and pre-deletion approval

Owner10October instruction: “keep everything inside the tim directory”. This explicitly supersedes external storage only. Current frozen spec intent reflects local retention, with unchanged scope/recovery/build/unit/Chrome requirements. Earlier external-blocker paragraphs above describe the prior boundary and no longer apply.

Final archive:/Users/aidan/dev/personal/tim/archives/story-7-1-20261009/p025-appbundles.tar.gz. Manifest, recovered tree and verifier receipts moved alongside without repackaging. Archive and manifest hashes remain exactly as recorded above. Precise Git ignore rule /archives/story-7-1-20261009/ and local .gdignore independently verified. No symlink directory redirect; archive lies outside reference deletion scope. Four export exclusions must replace reference/* with this archive scope before acceptance.

Independent retained-path verification command:

```sh
python3 - <<'PY'
from pathlib import Path,PurePosixPath
import hashlib,json,tarfile,stat
stage=Path('archives/story-7-1-20261009'); archive=stage/'p025-appbundles.tar.gz'; manifest=stage/'members.json'; m=json.loads(manifest.read_text()); entries=m['entries']; expected={e['path']:e for e in entries}; assert len(entries)==len(expected)
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
assert sha(archive)=='5267bbe9fda12ee295b0b044fdc05bf14295e687eaddd740a3545356b13a1d0a'
assert sha(manifest)=='9ae5e8efcfee451939d05604744cbdd91dbd511ba98f061cb36f4cc1bbf3c13b'
seen=set()
with tarfile.open(archive,'r:gz') as tf:
 for t in tf:
  n=t.name.rstrip('/'); parts=PurePosixPath(n).parts
  assert n not in seen and n in expected and not n.startswith('/') and '..' not in parts
  seen.add(n); e=expected[n]; assert t.mode==e['mode']
  if e['type']=='file':
   assert t.isfile() and t.size==e['size']
   h=hashlib.sha256()
   with tf.extractfile(t) as f:
    for b in iter(lambda:f.read(1048576),b''):h.update(b)
   assert h.hexdigest()==e['sha256'],n
  else:assert t.isdir()
assert seen==set(expected)
source=Path(m['source']); recovered=stage/'recovered'
for label,base in [('source',source),('recovered',recovered)]:
 actual=set()
 roots=[base/'diagnostic-appbundle',base/'production-appbundle'] if label=='source' else list(base.iterdir())
 for root in roots:
  for p in [root,*root.rglob('*')]:
   n=p.relative_to(base).as_posix();assert n in expected and n not in actual,(label,n);actual.add(n);e=expected[n];s=p.lstat();assert not p.is_symlink() and stat.S_IMODE(s.st_mode)==e['mode']
   if e['type']=='file':assert stat.S_ISREG(s.st_mode) and s.st_size==e['size'] and sha(p)==e['sha256'],(label,n)
   else:assert stat.S_ISDIR(s.st_mode)
 assert actual==set(expected)
 print(label,'exact entries/modes/hashes',len(actual))
print('PASS independently verified staged archive208369673 bytes,2474 files,12 directories,402121675 original bytes; owner-approved retained location verified; deletion still subject to exact scope and Anvil')
PY
```

Raw3e6159/session23997, completion073b49, exit0:

```text
source exact entries/modes/hashes 2486
recovered exact entries/modes/hashes 2486
PASS independently verified staged archive208369673 bytes,2474 files,12 directories,402121675 original bytes; owner-approved retained location verified; deletion still subject to exact scope and Anvil

```

Additional rawc19c37, exit0: precise ignore diff, directory metadata/.gdignore, git check-ignore, and fresh Python ledger/inventory reconciliation confirmed4682regular files,392tracked,26matching rows, no symlinks or tracked content changes. Entire original bundle membership/modes/hashes remained equal to the prearchive manifest immediately at review.

**Pre-deletion authorization:** same owner may remove only the exact checked reference/ tree under mandatory Anvil, with its own immediate final manifest/inventory drift check, and update the four export exclusions. Keep archives/ and unrelated work intact. This is not final SnapshotApproval for commit or terminal Story7.1 Pass. Required post-deletion build/unit/actual Chrome verification and exact final diff approval remain pending.


## Post-deletion checks and affected proof applicability

Independent9a13a7: exactly392tracked paths deleted, equal to HEAD reference-tree membership; no other tracked deletion; reference/ absent. No runtime source/project changes. Export config equals HEAD with exactlyfour reference/* → archives/story-7-1-20261009/* substitutions. Independent44f71b: all13unique retained reference citation blobs resolve through a6c914e; diagnostic PCK contains no archive/reference path names. Archive identity/recovery proof above remains applicable after deletion.

Implementer raw .anvil/story-7-1-build.log records Release build success; .anvil/story-7-1-unit.log records643/643 unit tests passed; .anvil/story-7-1-node.log records83/83 existing Node tests passed. Reviewer independently inspected these receipts (c7e220,4c2a1b). Five xUnit2013 warnings occur at unchanged WorkshopActivationAnimationTests.cs lines456,459,464,494,504; independent diff proves that source unchanged. These pre-existing warnings remain with the existing test owner, outside this purge; no clean-zero-warning/global claim. Node experimental VM warning is the existing required harness invocation. No unrelated repeat of these unchanged checks.

Production publish succeeded with PlaytestDiagnostics=false, raw .anvil/story-7-1-production.log and full pre-overwrite .anvil/story-7-1-production-identity.json. The production PCK is45588bytes, SHA256c031e38eb188e569120b946ec3bdcbd1e9bb7114ab14b350063d0b1d46d4d9b4. Diagnostic publish succeeded, then runtime/artifacts were held unchanged through independent Chrome. No production-origin behavior or remote publication is claimed.

### Diagnostic bundle identity and actual Chrome

Immutable build receipt .anvil/story-7-1-diagnostic-identity.json SHA2560f861841a4019e4cf3a5548859c0db8d4740506d4f8205d7442a9b9c8634dbdd. Reviewer independently checked complete221-file bundle membership/bytes/hashes and8HTTP-served entry identities at http://127.0.0.1:8060/. Current runtime source commit34ad0ae0a64fc56336022de0ada49774098492cf; runtime sources unchanged. Chrome154.0.8037.99; Node26.11.0 (5ec5ef). Direct available Playwright connector tabs call succeeded; actual existing driver uses its Playwright installation and channel chrome, real UI controls. No setter/imported solution substituted.

```sh
python3 - <<'PY'
import json,pathlib,hashlib,urllib.request
p=pathlib.Path('.anvil/story-7-1-diagnostic-identity.json');assert hashlib.sha256(p.read_bytes()).hexdigest()=='0f861841a4019e4cf3a5548859c0db8d4740506d4f8205d7442a9b9c8634dbdd';m=json.loads(p.read_text());root=pathlib.Path('CuriousContraptions.web/AppBundle');actual={str(f.relative_to(root)) for f in root.rglob('*') if f.is_file()};expect={e['path'] for e in m['files']};assert actual==expect
for e in m['files']:
 b=(root/e['path']).read_bytes();assert len(b)==e['bytes'] and hashlib.sha256(b).hexdigest()==e['sha256'],e['path']
for name in ['index.html','godot.pck','_framework/CuriousContraptions.wasm','simulation/worker.js','simulation/_framework/dotnet.boot.js','simulation/_framework/CuriousContraptions.Simulation.wasm','animation/worker.js','workshop-client.js']:
 e=next(x for x in m['files'] if x['path']==name);h=hashlib.sha256(urllib.request.urlopen('http://127.0.0.1:8060/'+name).read()).hexdigest();assert h==e['sha256'];print('HTTP',name,h)
print('PASS exact diagnostic bundle file membership/bytes/hashes',len(actual),'PlaytestDiagnostics',m['PlaytestDiagnostics'])
PY
```

Rawc8abdb, exit0:

```text
HTTP index.html a9ce285101eff2a600c2275d9b133ea95e357cf8c974545993c74818fd3346ef
HTTP godot.pck 8062013e38eb62dc6c99662b3ac3745f9b7634bab0c966458c572ce12dbb535d
HTTP _framework/CuriousContraptions.wasm f380b187b7cf039eb958a14544d00da2c193632f403ec301da5c7bdae7c60264
HTTP simulation/worker.js 4116abb0d8fb6e0257b435bb5f13023a7ace758f567ac72bed6b56e94330b04a
HTTP simulation/_framework/dotnet.boot.js 1b769f95961f7debeffadfe41e7ab73b7224a96de93a951bd3816a8537e6e8b5
HTTP simulation/_framework/CuriousContraptions.Simulation.wasm 6c3b798bf6149a4df1b29206d72d57dc929fc66305357ee6567a0ab59c2bed83
HTTP animation/worker.js 9b0e0259ee0a1eab08004ff7110a2d8a7acaeb00de753b9d8e1a81245e6b9c12
HTTP workshop-client.js 1ff22feed79266984e2867931440a17c3c26a89f1e8bd5dc00a63b61866f26c8
PASS exact diagnostic bundle file membership/bytes/hashes 221 PlaytestDiagnostics True

```

### Independent full existing Chrome suite, original failed attempt retained

```sh
node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts
```

Reviewer session78972 ran all15suites/45cases:41passed,4CAT023bfailed,370759.083209ms. Complete passing-case output and summary below. The final reporter's158535-token error dump was tool-truncated (013a68); that missing full failure detail was explicitly treated as incomplete, not silently discarded. A justified focused repeat captured all four original error arrays to disk before any fix.

```text
[chunk a368e3]

[chunk 8e8717]
▶ ANIM-1a: Dedicated 60 Hz WebAssembly Animation Worker Pipeline & Core Feedback
  ✔ 1. Animation worker qualification & Receiver capture halo ramp upon goal solve (5200.689ms)
  ✔ 2. Cosmetic animation evaluation continues at 60 Hz independent of simulation pause (1765.350292ms)

[chunk fd80c8]
  ✔ 3. Exact Reset and Save/Load persistence roundtrip with clean animation re-activation (12399.811083ms)
✔ ANIM-1a: Dedicated 60 Hz WebAssembly Animation Worker Pipeline & Core Feedback (23640.226042ms)
▶ ANIM-1b: Mechanical cosmetic bindings and procedural curves
  ✔ 1. Switch depression ramps to 1 on the declared curve while the unconnected lamp stays neutral (2719.597209ms)
  ✔ 2. Delay progress fill wired through UI buttons rises monotonically to 1, the lamp lights after, Reset neutralises and Save/Load re-animates (10966.3335ms)

[chunk 7e4ab7]
  ✔ 3. Bumper squash pulse rises and returns to neutral when struck; an un-struck bumper emits nothing (10214.299292ms)
✔ ANIM-1b: Mechanical cosmetic bindings and procedural curves (28079.784ms)

[chunk 88a904]
▶ ANIM-1c: Legacy presentation code retirement
  ✔ 1. Capture halo and goal label animate through declared bindings; Reset retires them and Save/Load re-animates (14084.405584ms)
  ✔ 2. Show hint while a wired Delay counts reveals on the shared lease without exception, the fill continues, and the next press hides (5067.16025ms)
  ✔ 3. Hint reveal and hide in Build mode leave every world-bound target silent (2401.741375ms)
  ✔ 4. Free workshop: a dropped ball captured by a placed Receiver animates the same declared capture target with no goal (4247.04275ms)
✔ ANIM-1c: Legacy presentation code retirement (29991.898167ms)

[chunk 4c1207]
▶ CAT-014: Bowling ball dynamic sphere by material declaration
  ✔ 1. Two lanes in one Run: the Bowling ball topples its Domino past 60 deg, the identically released Basketball leaves its Domino below 20 deg, and both balls come to rest (10918.104125ms)
  ✔ 2. Dropped from the placement plane the Bowling ball barely rebounds and rests at its 0.28 m radius; the Basketball rebounds high; Reset replays both worlds bit-for-bit (8815.676333ms)
  ✔ 3. A Bowling ball dropped into a placed Receiver is captured and the declared capture halo animates (4250.808292ms)

[chunk 242c8d]
  ✔ 4. Save / reload / Load restores both kinds; after Reset the second Run topples the Bowling lane again and leaves the Basketball lane standing (17714.27775ms)
✔ CAT-014: Bowling ball dynamic sphere by material declaration (45983.066792ms)
▶ CAT-023a: Dynamic box rigid body & upright stability (Domino)
  ✔ 1. Negative control: an upright Domino alone on the bench stands still for 3 s within the rest tolerances (4518.72075ms)
  ✔ 2. Struck by a Basketball dropped onto its upper third the Domino topples past 60 deg and settles flat without jitter (7499.4245ms)
  ✔ 3. Reset restores the placed upright pose exactly and the next Run topples the tile again (4014.452667ms)
  ✔ 4. Save / reload / Load restores the Domino kind and pose and the loaded construction topples the same way (5366.102ms)
✔ CAT-023a: Dynamic box rigid body & upright stability (Domino) (25528.340958ms)

[chunk a4bf37]
▶ CAT-023b: Domino cascade mechanics & orientation-threshold sensor
  ✖ 1. Four dominoes wired end → lamp topple in sequence, the lamp lights and CCGOAL_SOLVED prints exactly once (8069.634541ms)
  ✖ 2. Three dominoes leave a gap: the chain breaks, the end Domino stays upright and the lamp never lights (8382.714042ms)
  ✖ 3. A Domino tilted 10 deg through Fine rotate and wired to a lamp rocks back without emitting (8165.3375ms)
  ✖ 4. Save / reload / Load restores the wired chain and solves; Reset rearms the sensor and a second Run solves again (8999.944208ms)
✖ CAT-023b: Domino cascade mechanics & orientation-threshold sensor (37652.313791ms)
▶ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E
  ✔ 1. Clean boot: cross-origin isolation and WorkshopPoseRing initialized (3.592542ms)
  ✔ 2. Empty-construction control: Run advances sequence with 0 bodies, Reset restores Build Mode (1162.555459ms)

[chunk 27621c]
  ✔ 3. Single-ball control: Place 1 Basketball via UI, Run observes gravitational motion, Reset restores elevation (2633.179167ms)
  ✔ 4. Dual-sphere multi-body simulation: Place 2 Basketballs, Run observes independent trajectories (2782.440042ms)
  ✔ 5. Save & Load persistence roundtrip: Save via driver, reload page, Load restores both balls (4083.207709ms)
✔ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E (14876.86025ms)
▶ ENGINE-CORE-2a2: TGS Soft Solver Resting Contact and Dissipative Bounce Restitution
  ✔ 1. Stable resting contact: Basketball settles on workbench without bounce jitter or position projection (4948.892208ms)
  ✔ 2. Dissipative bounce restitution dynamics: Successive bounce peaks decay strictly (h2 < h1) (4915.848709ms)
  ✔ 3. Reset exact restoration after multi-bounce dissipation (4100.406209ms)

[chunk 1b6b83]
  ✔ 4. Save & Load persistence roundtrip: Ball preserves placed location and settles on workbench after load (7099.639334ms)
✔ ENGINE-CORE-2a2: TGS Soft Solver Resting Contact and Dissipative Bounce Restitution (25083.203458ms)
▶ ENGINE-CORE-2a3: Multi-body Ramp and Wall Contact on Generic Physics Core
  ✔ 1. Ramp contact & rolling: Ball contacts inclined ramp and rolls along surface (3749.900333ms)
  ✔ 2. Wall collision & deflection: Ball strikes Wall, rebounds via TGS Soft restitution (4715.427083ms)
  ✔ 3. Local-axis Wall resizing: Resized wall deflects ball at updated collision extents (7484.012084ms)

[chunk 931ba0]
  ✔ 4. [ENGINE-CORE-2a3, ENGINE-CORE-2b1, ENGINE-CORE-2b2, ENGINE-CORE-2b3] First principles 2-ramp solve: Ball rolls down both ramps into Receiver and achieves captured (direct pose ring publishing, pure TGS Soft compliance, speculative contacts without analytic interval sweeps) (5781.754959ms)
  ✔ 5. [ENGINE-CORE-2a3, ENGINE-CORE-2b1, ENGINE-CORE-2b2, ENGINE-CORE-2b3, ENGINE-CORE-2b4, ENGINE-CORE-2c] Exact Reset and Save/Load persistence roundtrip in Chrome (11399.826708ms)
✔ ENGINE-CORE-2a3: Multi-body Ramp and Wall Contact on Generic Physics Core (37110.30425ms)
▶ ENGINE-CORE-2a4: Anti-Tunneling High-Speed Wall Impact via Speculative Contacts CCD
  ✔ 1. High-speed vertical wall impact without tunneling (3833.502458ms)

[chunk 797eec]
  ✔ 2. Exact Reset and Save/Load persistence restoration (8398.903291ms)
✔ ENGINE-CORE-2a4: Anti-Tunneling High-Speed Wall Impact via Speculative Contacts CCD (16381.990292ms)
▶ ENGINE-CORE-2b1: Host Validation & Error Lane Removal (Direct SAB Pose Publishing)
  ✔ 1. Real-time ball drop and Workbench interaction without host validation overhead or errors (3751.634208ms)
✔ ENGINE-CORE-2b1: Host Validation & Error Lane Removal (Direct SAB Pose Publishing) (7715.980833ms)
▶ ENGINE-CORE-2b2: Certificate & Directed Rounding Removal (Pure TGS Soft Compliance)
  ✔ 1. Real-time ball drop and Workbench resting contact without certificate/directed rounding overhead (3752.170334ms)
✔ ENGINE-CORE-2b2: Certificate & Directed Rounding Removal (Pure TGS Soft Compliance) (7876.109542ms)

[chunk 2304c7]
▶ ENGINE-CORE-2b3: Analytic CCD & Interval Library Removal (Pure Speculative Contacts CCD)
  ✔ 1. [ENGINE-CORE-2a4, ENGINE-CORE-2b3] High-speed ball impact against thin wall resolves via speculative contacts CCD with 0 tunneling across 20 iterations (high-speed impact across 20 iterations never tunnels through thin Wall) (32284.093625ms)
✔ ENGINE-CORE-2b3: Analytic CCD & Interval Library Removal (Pure Speculative Contacts CCD) (36249.5415ms)

[chunk 528aec]
▶ ENGINE-CORE-2b4: Guide Horizon & Departure Ownership Removal (Declarative Spatial Force Regions)
  ✔ 1. First principles receiver assistance knots evaluated as declared spatial acceleration fields (smooth entrance without moving collider walls) (5084.298167ms)
  ✔ 2. Multi-body ramp rolling and assistance knot interaction under pure spatial force regions (5730.678875ms)
✔ ENGINE-CORE-2b4: Guide Horizon & Departure Ownership Removal (Declarative Spatial Force Regions) (15011.608459ms)
▶ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation)
  ✔ 1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver and triggers capture with visual halo (5100.433375ms)

[chunk 013a68, exit 1]
Warning: truncated output (original token count: 158535)
Total output lines: 82

  ✔ 2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture (7265.368625ms)
✔ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation) (16364.877833ms)
ℹ tests 45
ℹ suites 15
ℹ pass 41
ℹ fail 4
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 370759.083209


```

## P1 — CAT023b console classifier mistakes encoded bytes for NaN

The old text.includes('NaN') check scans arbitrary base64 trace text. Independent unchanged-source focused rerun recorded complete raw output at .anvil/story-7-1-cat023b-original.log (634714bytes, SHA256c52dbf34e1b765ba36d363074f6f5d82914e4b615ae587dd0fc09713e906f59b). Capture command validated by Anvil preview before write; dynamic log content was not known before execution, so this is preview-only validation. Original test bytes asserted equal HEAD before run. Exit1, four failures,37986.259792ms (d89ec0/d041ce).

```sh
python3 - <<'PY'
import pathlib,subprocess,hashlib,sys
p=pathlib.Path('tools/e2e/cat-023b.test.ts'); baseline=subprocess.check_output(['git','show','HEAD:'+str(p)]);assert p.read_bytes()==baseline,'original classifier already changed'
cmd=['node','--test','--test-concurrency=1','--test-timeout=150000',str(p)]
log=pathlib.Path('.anvil/story-7-1-cat023b-original.log')
with log.open('xb') as f:
 f.write(('Command: '+' '.join(cmd)+'\nOriginal source SHA256:'+hashlib.sha256(baseline).hexdigest()+'\n').encode());f.flush()
 result=subprocess.run(cmd,stdout=f,stderr=subprocess.STDOUT)
print('exit',result.returncode,'log',str(log),'bytes',log.stat().st_size,'sha256',hashlib.sha256(log.read_bytes()).hexdigest())
sys.exit(result.returncode)
PY
```

Independent complete-array analysis:

```sh
python3 - <<'PY'
import pathlib,re,base64,hashlib
p=pathlib.Path('.anvil/story-7-1-cat023b-original.log');text=p.read_text();case=0
for line in text.splitlines():
 if line.startswith('test at '):print(line)
 if 'AssertionError' in line:
  case+=1;assert 'Zero errors expected, got: ' in line
  errors=line.split('Zero errors expected, got: ',1)[1].split('; ')
  print('CASE',case,'error_count',len(errors))
  for error in errors:
   m=re.fullmatch(r'CCGPU_TRACE_CHUNK (\d+) (\d+) (\d+) (\d+) (\d+) ([A-Za-z0-9+/=]+)',error);assert m,error[:150]
   data=base64.b64decode(m[6],validate=True);n=m[6].index('NaN');print({'envelope':error[:error.rfind(' ')+1],'payload_bytes':len(data),'payload_sha256':hashlib.sha256(data).hexdigest(),'NaN_occurrences_in_base64':m[6].count('NaN'),'NaN_base64_context':m[6][max(0,n-12):n+15],'real_error_text_outside_envelope':False})
 if 'at TestContext' in line or line.startswith('ℹ'):print(line)
assert case==4
print('All4failure arrays contain only syntactically valid base64 trace messages; no textual error diagnostic. Every failure is finalerrors assertion after functional assertions.')
PY
```

Raw42728f, exit0:

```text
ℹ tests 4
ℹ suites 1
ℹ pass 0
ℹ fail 4
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 37986.259792
test at tools/e2e/cat-023b.test.ts:115:5
CASE 1 error_count 1
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 6 0 128 ', 'payload_bytes': 55296, 'payload_sha256': '9073170b1fddc62fefeb1575e9a8bb51edbd3732b50804da2413cff1d0cd9cb7', 'NaN_occurrences_in_base64': 2, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
      at TestContext.<anonymous> (file:///Users/aidan/dev/personal/tim/tools/e2e/cat-023b.test.ts:151:16)
test at tools/e2e/cat-023b.test.ts:154:5
CASE 2 error_count 2
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 6 0 128 ', 'payload_bytes': 55296, 'payload_sha256': '9073170b1fddc62fefeb1575e9a8bb51edbd3732b50804da2413cff1d0cd9cb7', 'NaN_occurrences_in_base64': 2, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 6 0 128 ', 'payload_bytes': 47104, 'payload_sha256': 'd3dd26a2edf077b32f54cbf6b456992835d388f573ad4a7a21d779665e5ad9b1', 'NaN_occurrences_in_base64': 1, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
      at TestContext.<anonymous> (file:///Users/aidan/dev/personal/tim/tools/e2e/cat-023b.test.ts:174:16)
test at tools/e2e/cat-023b.test.ts:177:5
CASE 3 error_count 2
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 6 0 128 ', 'payload_bytes': 55296, 'payload_sha256': '9073170b1fddc62fefeb1575e9a8bb51edbd3732b50804da2413cff1d0cd9cb7', 'NaN_occurrences_in_base64': 2, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 6 0 128 ', 'payload_bytes': 47104, 'payload_sha256': 'd3dd26a2edf077b32f54cbf6b456992835d388f573ad4a7a21d779665e5ad9b1', 'NaN_occurrences_in_base64': 1, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
      at TestContext.<anonymous> (file:///Users/aidan/dev/personal/tim/tools/e2e/cat-023b.test.ts:205:16)
test at tools/e2e/cat-023b.test.ts:208:5
CASE 4 error_count 4
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 6 0 128 ', 'payload_bytes': 55296, 'payload_sha256': '9073170b1fddc62fefeb1575e9a8bb51edbd3732b50804da2413cff1d0cd9cb7', 'NaN_occurrences_in_base64': 2, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 6 0 128 ', 'payload_bytes': 47104, 'payload_sha256': 'd3dd26a2edf077b32f54cbf6b456992835d388f573ad4a7a21d779665e5ad9b1', 'NaN_occurrences_in_base64': 1, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
{'envelope': 'CCGPU_TRACE_CHUNK 1 0 3 0 128 ', 'payload_bytes': 55296, 'payload_sha256': 'ceff35aabff72a195e687d67b2b905b2c3ec77bb424776a3d4c607af1fc21f5d', 'NaN_occurrences_in_base64': 2, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
{'envelope': 'CCGPU_TRACE_CHUNK 2 0 3 0 128 ', 'payload_bytes': 55296, 'payload_sha256': 'ed636c34d246554049f8935d48cb6b0a70a478a4f513fdba3883d4b493792844', 'NaN_occurrences_in_base64': 2, 'NaN_base64_context': '/8BAAAAAAAAANaN8jYIggMABAAE', 'real_error_text_outside_envelope': False}
      at TestContext.<anonymous> (file:///Users/aidan/dev/personal/tim/tools/e2e/cat-023b.test.ts:231:16)
All4failure arrays contain only syntactically valid base64 trace messages; no textual error diagnostic. Every failure is finalerrors assertion after functional assertions.

```

All failures are final errors-array assertions at151/174/205/231, after their functional checks. Every captured message is a complete encoded trace; no textual fault diagnostic exists outside its envelope. To avoid equating syntactic validity with numeric health, reviewer decoded every unique payload against actual WorkshopTraceRecord/PhysicsBodyWire layout:

```sh
python3 - <<'PY'
import pathlib,re,base64,struct,math
text=pathlib.Path('.anvil/story-7-1-cat023b-original.log').read_text();payloads=set(re.findall(r'CCGPU_TRACE_CHUNK \d+ \d+ \d+ \d+ \d+ ([A-Za-z0-9+/=]+)',text));records=bodies=0
for encoded in payloads:
 b=base64.b64decode(encoded,validate=True);i=0
 while i<len(b):
  assert struct.unpack_from('<I',b,i)[0]==3;n,c=b[i+36:i+38];width=48+n*64+c*24;assert i+width<=len(b)
  for body in range(n):
   offset=i+48+body*64;values=struct.unpack_from('<10e6f',b,offset+20);assert all(map(math.isfinite,values));bodies+=1
  for cap in range(c):assert math.isfinite(struct.unpack_from('<e',b,i+48+n*64+cap*24+16)[0])
  i+=width;records+=1
 assert i==len(b)
print('All',len(payloads),'unique captured error-payloads decoded:',records,'trace records,',bodies,'body records; every typed pose/material-frame/velocity lane finite, no numericNaN/Infinity')
PY
```

Raw33a04c, exit0:

```text
All 4 unique captured error-payloads decoded: 512 trace records, 2944 body records; every typed pose/material-frame/velocity lane finite, no numericNaN/Infinity

```

Same implementation owner added a narrow console-boundary helper. Only CAT023b consumes it (independent consumer search7bbde8); no other suite/driver/runtime code changed. Real console.error, explicit fault markers, plaintext NaN, malformed/trailing or CR/LF trace diagnostics continue to fail. Reviewer raised the terminal-newline boundary for checking; owner added explicit CR/LF controls. No reproduced product defect was claimed from that regex hypothesis. Independent focused classifier2/2 tests passed (e18888); explicit Anvil affected harness scan reports0warnings (owner c35872).

**P1 resolved:** corrected actualChrome CAT023b4/4 passed,38208.676292ms, exit0 (8f1940/71bcd1). Full raw .anvil/story-7-1-cat023b-corrected.log1171bytes SHA2562ec92ef8648954f1d4f30c084303db15a5caa07daf9ece5768e9b905bd6c2755. Corrected identities: helper6652e11eaee3c7a29dc1239d899bb4d682756e76075278213d9fa88c3b71b843; helper test e0c94557571e99029add2402c786679182002e157831706642b69b939a3f43a3; CAT023b290e7bdbf1b738e7686af7dd2e5842131b1c87b7a8d8c2a359f3d8b820314997.

```sh
python3 - <<'PY'
import pathlib,subprocess,hashlib,sys
names=['tools/workshop-console.mjs','tools/workshop-console.test.mjs','tools/e2e/cat-023b.test.ts'];cmd=['node','--test','--test-concurrency=1','--test-timeout=150000',names[-1]];log=pathlib.Path('.anvil/story-7-1-cat023b-corrected.log')
with log.open('xb') as f:
 f.write(('Command: '+' '.join(cmd)+'\n'+'\n'.join(n+' SHA256:'+hashlib.sha256(pathlib.Path(n).read_bytes()).hexdigest() for n in names)+'\n').encode());f.flush();result=subprocess.run(cmd,stdout=f,stderr=subprocess.STDOUT)
print('exit',result.returncode,'log',str(log),'bytes',log.stat().st_size,'sha256',hashlib.sha256(log.read_bytes()).hexdigest());print('\n'.join(log.read_text().splitlines()[-12:]));sys.exit(result.returncode)
PY
```

```text
exit 0 log .anvil/story-7-1-cat023b-corrected.log bytes 1171 sha256 2ec92ef8648954f1d4f30c084303db15a5caa07daf9ece5768e9b905bd6c2755
  ✔ 2. Three dominoes leave a gap: the chain breaks, the end Domino stays upright and the lamp never lights (8670.710166ms)
  ✔ 3. A Domino tilted 10 deg through Fine rotate and wired to a lamp rocks back without emitting (8132.73375ms)
  ✔ 4. Save / reload / Load restores the wired chain and solves; Reset rearms the sensor and a second Run solves again (8984.894041ms)
✔ CAT-023b: Domino cascade mechanics & orientation-threshold sensor (38009.2085ms)
ℹ tests 4
ℹ suites 1
ℹ pass 4
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 38208.676292

```

The41unaffected cases retain their original independent proof: source/driver at34ad0ae unchanged, same221-file diagnostic build and served origin/environment, and no consumer of the new helper. Their positive/control/Reset/Save-Load assertions all passed. Combined with4affected corrected cases, every45current acceptance case is verified; this is not a claim that the initial full run passed.

Known bounded pre-existing harness risk: broadNaN substring checks remain in tools/e2e/anim-1a.test.ts,cat-014.test.ts,cat-023a.test.ts,engine-core-2b2.test.ts,engine-core-2b3.test.ts,engine-core-2b4.test.ts,engine-core-2c.test.ts. These independent copies passed this unchanged candidate and were not modified; follow-up belongs to harness maintenance. No generalized classifier repair claimed. Root coordinator explicitly accepted this bounded scope.


## Frozen candidate SnapshotApproval — 10 October 2026

**SnapshotApproval: Pass.** Independent reviewer `/root/reviewer` approves the exact scoped candidate implemented by `/root/implementation`: snapshot SHA-256 `b1a549114a7bf67d491272c7b61a13799be9dd36acd5ef946a9435acc6c972a8`, source HEAD `34ad0ae0a64fc56336022de0ada49774098492cf`. All pre-publication criteria for Story 7.1 are satisfied with zero unresolved unintended regressions in the justified scope. Terminal completion remains pending only the local commit's exact membership/tree verification and preservation of the five excluded dirty files. No remote push/deployment or production-origin gameplay is required for this local purge; Battery and full Bumper remain separately Incomplete.

Actual final current-document/config diff read: raw `57f027`; full current spec acceptance/receipt read: `1ac618`. Owner local-archive decision, 4,682 reference files removed, 392 tracked deletion identities, historical citation preservation, 41 unchanged plus four corrected Chrome proofs, and future annular/kernel/purge requirements agree. Sprint 7.0 now reflects its earlier committed Pass; 7.1 review status and final commit pending statements are accurate at this boundary. The actual scoped unified diff is `.anvil/story-7-1-final.patch`, SHA-256 `223d8e89e29d0c2b5a02485f920dfec33b1714df6b715c4c87e4ebc99726c151`. Review evidence and snapshot join the local commit without self-hashing. Expected scope: exactly 407 paths = 392 deletions + 13 snapshot candidate paths + snapshot + this review record. All five unrelated dirty paths are excluded.

Independent final identity command:

```sh
python3 - <<'PY'
import json,hashlib,pathlib,subprocess
p=pathlib.Path('_bmad-output/implementation-artifacts/story-7-1-snapshot.json')
s=json.loads(p.read_text()); h=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
assert h(p)=='b1a549114a7bf67d491272c7b61a13799be9dd36acd5ef946a9435acc6c972a8'
for key in ['candidate','current_inputs','excluded_dirty_files']:
 for path,digest in s[key]: assert h(path)==digest,path
d=dict(s['deleted_tracked_git_blobs'])
actual=subprocess.check_output(['git','diff','--name-only','--diff-filter=D','-z']).decode().strip('\0').split('\0')
assert set(actual)==set(d) and len(d)==392
tree=subprocess.check_output(['git','ls-tree','-r',s['source_commit'],'reference/']).decode().splitlines()
assert {line.split('\t')[1]:line.split()[2] for line in tree}==d
assert not pathlib.Path('reference').exists()
a=s['archive']; assert h(a['path'])==a['sha256']; assert h(a['manifest'])==a['manifest_sha256']
assert h('.anvil/story-7-1-final.patch')=='223d8e89e29d0c2b5a02485f920dfec33b1714df6b715c4c87e4ebc99726c151'
print('PASS: snapshot exact; 13 candidate hashes, 7 input hashes, 5 excluded hashes; all392 deleted paths/blobs equal baseline; archive+manifest unchanged; final diff exact.')
print('Expected local commit407paths =392deletions+13candidate+snapshot+review.')
PY
```

Raw result `4eab00`:

```text
PASS: snapshot exact; 13 candidate hashes, 7 input hashes, 5 excluded hashes; all392 deleted paths/blobs equal baseline; archive+manifest unchanged; final diff exact.
Expected local commit407paths =392deletions+13candidate+snapshot+review.

```
