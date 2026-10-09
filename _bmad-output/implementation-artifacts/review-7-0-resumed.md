# Story 7.0 resumed independent review

Review owner: `/root/reviewer`. Implementation owner: `/root/implementation`. Coordinator: `/root`. Supporting independent declaration/variant reviewer: `/root/variant_review`. Date: 2026-10-09. No reviewer edits implementation deliverables.

Status: **scoped documentation Pass / local SnapshotApproval**, including the nine-path final delta recorded below. F13 is independently resolved; local commit identity verification remains pending. The in-progress tables retain the original finding trail; the final disposition below supersedes their pending labels. Local commit verification remains pending. The CAT-015 owner-decision/status delta is included in the final nine-path approval below. Documentation only: runtime/build/Chrome tests are not this slice's acceptance and the frozen contract forbids runtime/code/test changes.

## Original evidence and applicability

`scratchpad/review-7-0-B/`, the only concrete original batch raw-evidence location in the story, is absent. `rg --files -uuu scratchpad` exited 1: `rg: scratchpad: IO error for operation on scratchpad: No such file or directory (os error 2)`. Repository discovery and .scratch discovery found no original 7.0 batch records; .scratch contains unrelated probe binaries. Previous Pass summaries have no independently recoverable original reviewer/snapshot binding. They are historical reports, not reused proof. Fresh review is required and is being performed.

## Criterion and impact matrix

| Criterion / impact | Independent check | Current disposition |
| --- | --- | --- |
| 72 CAT + 276 named specs; six sections | Enumerated actual files and headings | 348 = 72 CAT + 216 EL + 37 TH + 19 RAD + 4 GAP; six sections each |
| 293 named identities including 17 index-only entries | Index and named-source mapping; supporting reviewer covers all 276 full specs | Final index enumeration pending |
| Source citation existence/ranges | Parsed every explicit path@revision:Lx-Ly, git show cached by blob | 2,947 unique ranges / 576 blobs resolved at initial read; affected additions and baseline-only decision require final repeat |
| Links | Every local Markdown destination and explicit/generated heading anchor in 348 specs | Initial check found zero broken targets |
| Deletion scope membership | Independent git ls-tree enumeration plus baseline csproj compile sets and scene/resource references | Found F1, orphan sidecar; corrected, final repeat pending |
| Harvest preservation | Risk-based deleted source samples below, compared with harvest facts | Fresh samples ongoing; F9 missing clock boundary fact |
| Numerical/control correctness | Independent scalar/thermal oracles and geometry reasoning | F2/F3/F5–F8/F10–F12 found; affected fixes require final review |
| Typed connection consistency / shared model closure | Cross-spec Water and thermoelectric contracts | F11/F12 found |
| Story/dependency consistency | Current roadmap and selected references | F4 found; G/N renumber under review |
| All variants and declaration completeness | Supporting reviewer /root/variant_review, all276 source identities/map/JSON | VR-1 GAP09 geometry/joint omission found; correction pending |
| Source acceptance preservation | Requirement file untouched; compare epics/roadmap amendments | Final preservation check pending |
| Anvil | Actual command retained below | Default extension command analyzed no files; explicit Markdown/YAML check required |
| Runtime/build/lifecycle/workers | Documentation-only impact; no runtime code changes in this slice | Not applicable, not claimed |
| Final identity / terminal verdict | One scoped snapshot after fixes | Pending; no Pass from structural checks |

## Findings routed to same implementation owner

| ID | Affected closure | Observed failure / required correction |
| --- | --- | --- |
| F1 | Ledger | Tracked orphan `engine/physics/PreciseScalar.cs.uid` had no row. Independent enumeration covered1577/1578 paths. |
| F2 | TH29/13/17 | TH29 claimed two frozen charges and first pack263K, while coupled model freezes first at25.88125s/264.66988K; second fresh tray charge peaks at0.128817 solid and melts. |
| F3 | TH30/26/10 | Steady held-source7W was claimed for finite450K source with cold module nodes. Actual declared transient peaks5.71816W, and removing sink peaks5.18409W. |
| F4 | CAT018/019/034/042/053/057/071 | Superseded “no owning story” cylinder statements contradicted accepted10.1 ENGINE-CYLINDER owner. |
| F5 | EL171 | Settled rest existed, but spring energy expression omitted preload/gravity and fixed yield omitted transient overshoot. Make preload/energy/damping and maximum-compression yield explicit. Initial isolated reading overlooked existing rest paragraph; the narrower energy/transient finding stands. |
| F6 | EL033 | Pressure/energy ignored piston weight and changing water column. Include hydraulic face/port datum, piston and liquid potential; fixed1m header differs from prescribed157Pa at moving face. |
| F7 | EL036/172 | Fixed-speed45degree jets make an arc, not a filled sector; range2h assumes equal elevations; a3m cap lacked a pressure/speed/loss mechanism. |
| F8 | EL173 | No-lens narrow beam misses both radius0.55 receivers centered±0.8m, not one full-power hit. |
| F9 | CAT017 | ClockPulseFeedbackTests invalid stream/generation/revision/source/tick/sequence boundary assertions unharvested. |
| F10 | EL028/029/169/170 plus shared F water law | Dry payload contributes inertia, not per-body fluid drag. Loaded damping times omitted m_float/m_total; raft battens need separate immersion accounting. |
| F11 | RAD017 / Water suppliers | Fluid domain/FluidIn/FluidOut cannot directly connect Water-domain supplier/consumer recipes. |
| F12 | RAD020 / TH030/010 | RTG independently used squared-temperature converter law and different ambient while TH030 requires shared converter; align generic law/declared parameters and recompute controls. |
| VR-1 | GAP09 | Supporting reviewer found missing moving-head dimensions and actual slider/latch declaration; full declaration required. |

The owner selected **baseline-only facts** for earlier-revision wavefront/LightConeVisual citations. CAT009/061/069/029 must retain only a6c914e-supported facts and explicit gaps; no earlier-revision citation exception is approved.

## Deleted-source sample work

Samples are read directly using `git show a6c914e:<path>`; original source is immutable and retained in git, not duplicated here. Large sources were read in bounded sequential sed ranges. This is source inspection, not execution of retired tests. CPU callbacks/solvers/proof-grade numerical mechanisms remain excluded; behavior and acceptance facts are retained or explicitly disposed.

| Batch | Risk-based sampled paths / rationale | Review result so far |
| --- | --- | --- |
| A | ImpactFrameTests.cs; engine/physics/PhysicsTiltSensor.cs; PhysicsTiltSensorTests.cs (tests under CuriousContraptions.tests) | Surface-relative launch, moving-assistance exclusion, axial-spin difference, atomic membership and reset facts captured in CAT015/023/062. |
| B | ClockPulseFeedbackTests.cs; engine/SimulationCounters.cs; parts/ElectricalLogicPart.cs; parts/ClockPart.cs | Saturation, power-vs-truth and counter identity preserved; F9 adds omitted invalid pulse identities. |
| C | parts/CannonPart.cs; parts/TrampolinePart.cs; parts/WoundSpringPart.cs | Finite stores, payload geometry, stroke/skin and latch declarations sampled; F4 schedule contradictions. |
| D | CompressionTransferSupplyTests.cs; parts/BellowsPart.cs; parts/SoundMeterPart.cs; engine/physics/BodyDragLoad.cs | Same-interval reaction, carrier-relative compression, power gating/hysteresis and finite drag sampled. |
| E | parts/MirrorPart.cs; parts/BeamSplitterPart.cs; parts/ColourFilterPart.cs | Front-only reflection, two-sided split/filter, transparent solid panes and closed RGB filter admission sampled. |
| F | engine/physics/StoredFlowSource.cs; StoredFlowSourceTests.cs; engine/physics/BodyDragLoad.cs | Shared finite allocation/no double debit and drag units sampled; cross-family F10. |
| G | engine/physics/AxialGasGeometry.cs; engine/physics/SealedGasState.cs; engine/physics/BodyDragLoad.cs | Geometry-to-volume and positive gas inventory, per-body damping; new hydraulic/ballistic controls reviewed. |
| H | engine/physics/SealedGasState.cs; engine/physics/ConvergingGasNozzle.cs; AdiabaticGasDischargeTests.cs | Derived state, choked/subsonic/no-flow and enthalpy conservation oracles sampled. |
| I | parts/WeightPart.cs; engine/physics/PhysicsTransmissionJoint.cs; engine/SceneRotaryShaft.cs | Tie/load role, finite inertia, bidirectional ratio and zero/invalid ratio rejection sampled. |
| J | tools/Campaign/SpringboardLesson.cs; tools/Campaign/Program.cs; engine/SceneTransmissionJoint.cs | Passive/precharged lesson and all15 composite spring lanes inspected; ratio and guide identity validation sampled. |
| K | parts/ColourFilterPart.cs; engine/LightNetwork.cs; parts/FlashlightPart.cs | Finite frame and cone/occlusion laws; flashlight source still needs complete final sample reconciliation; F8 geometry control. |
| L | engine/physics/PhysicsImpactEffects.cs; engine/physics/SegmentOcclusionSweep.cs; PhysicsImpactEffectTests.cs | Callback retirement/transactional publication and finite visibility sampled; remaining impact test slice reconciliation pending. |
| M | engine/physics/PhysicsJoint.cs; engine/physics/JointEquations.cs; JointRangeTests.cs | Distinct owned bodies/dynamic participant, no weld kind, zero-width locked range and moment-arm basis sampled. |
| N | engine/physics/SealedGasState.cs; engine/physics/ConvergingGasNozzle.cs; AdiabaticGasDischargeTests.cs | No thermal element legacy; shared gas state/venting laws and numeric chains examined. |
| O | reference/P0-022-before/docs/coverage/engine/task-017.json; task-002.json; task-003.json | Radiation hits are FutureProduct/coverage relation metadata, not radiation implementations. Cross-family F11/F12 remain. |

No batch-level terminal Pass is claimed yet. Supporting declaration/variant coverage and final numerical/fix/source reconciliation remain required.

## Raw independent numerical checks

### Cold pack failure reproduction

Command: `python3 - <<'PY'` with the following exact body:

```python
# Independent thermal ODE oracle from declared capacities/conductances; no production model.
dt=1/480; cp=437.5; ct=67.5; m=.05; cw=m*1046; ci=m*525; latent=m*20875
pack=255.
for charge in (1,2):
 tray=288.; hw=latent+cw*15; maxf=0.; frozen=None
 for tick in range(480*180):
  tw=273+(hw-latent)/cw if hw>latent else 273 if hw>=0 else 273+hw/ci
  flow=12.8*(tray-pack); internal=16*(tray-tw)
  pack+=(flow+1.2*(288-pack))/cp*dt
  tray+=(-flow-internal+.75*(288-tray))/ct*dt
  hw+=internal*dt
  f=max(0,min(1,1-hw/latent));maxf=max(maxf,f)
  if f>=1 and frozen is None:frozen=(tick/480,pack,tray,tw);break
 print('charge',charge,'first_frozen',frozen,'max_solid_fraction',maxf,'last_pack',pack)
```

Raw output, exit0 (reported time is tick index, within one1/480s substep):

```text
charge 1 first_frozen (25.88125, 264.66988015169755, 269.55960979535604, 273) max_solid_fraction 1 last_pack 264.66988015169755
charge 2 first_frozen None max_solid_fraction 0.12881676703698786 last_pack 278.10883251418653
```

### TEG failure reproduction

Command: `python3 - <<'PY'` body:

```python
for sink in [True,False]:
 b,h,c,s=450.,288.,288.,288.; peak=(0,0);dt=1/480
 for tick in range(480*120):
  qbh=12.8*(b-h);qh=.5*(h-c);p=max(0,min(16,.3*(1-c/h)*qh));qc=qh-min(p,6);qcs=32*(c-s) if sink else 0
  b+=(-qbh-3.2*(b-288))/420*dt;h+=(qbh-qh)/45*dt;c+=(qc-qcs-(c-288))/45*dt;s+=(qcs-5*(s-288))/225*dt
  if p>peak[0]:peak=(p,tick/480,b,h,c)
 print(sink,peak)
```

Raw output, exit0:

```text
True (5.718157199823671, 11.122916666666667, 422.6354534089823, 416.9618702544259, 290.8864034092666)
False (5.1840929320289275, 9.35, 424.8674445311316, 415.9279288201246, 296.03328182160453)
```

### Initial Anvil command

`anvil check --changed`, exit0:

```text
  ℹ No changed files to analyse
Last run written to .anvil/last-check.txt
```

This is not an explicit Markdown scan; the final check must pass `--extensions md,yaml`.

### Initial membership/citation/link output

```text
COUNT 348 Counter({'EL': 216, 'CAT': 72, 'TH': 37, 'RAD': 19, 'GAP': 4})
UNIQUE CITATIONS 2947 SOURCE BLOBS 576
LINK FAILURES []
INDEX SPEC LINKS 429 unique 348 missing set()
EXPECTED 1578 COVERED 1577 MISSING ['engine/physics/PreciseScalar.cs.uid'] DUPLICATE []
```

Initial heading regex accepted only “1.” and falsely listed “1)” headings; corrected six-top-level-section enumeration found zero anomalies. Initial named-row uppercase count283 excludes ten lowercase umbrella aliases, so it is not the full293 check. Index repeated CAT links are deliberate refines references, not duplicate spec identity rows.



## Frozen boundary and final independent disposition

Frozen candidate: [story-7-0-snapshot.json](story-7-0-snapshot.json), SHA-256 `d86c1cd61d96854981e09d7fde1c0a8391895de3956b42b295eb0beab1ff1a3f`. Its 358 candidate byte hashes and 21 current-input hashes independently match the workspace. Baseline source identity is `a6c914e367bb39316887b670e1fde1e4d0973f23`; workspace HEAD is `863585d071cbf3b6a84f07bde2471bab75188095`. Review-only evidence is excluded from candidate hashing. This record does not hash itself or require future receipts as approval inputs.

**Verdict: Pass within the justified documentation scope.** All A–O batches were freshly reviewed under `/root/reviewer`; no historical batch Pass was reused. Source sampling covers at least three deleted sources per batch as listed above. The remaining K flashlight, D Bellows constants/joint and L impact-test ranges were read and reconciled: finite source cone and physical button activation; finite compression work/slider travel; post-response impulses, ordinary contact constraints, angular momentum, replay and atomic admission. Retired executable callbacks, faulting-step rollback and proof-grade geometry remain explicitly excluded under the current architecture. The fresh review is risk-based source sampling, not an exhaustive semantic proof of every deleted line or every proposed numeric constant.

The independent [named-variant supporting review](review-7-0-named-variant-support.md), by actual agent `/root/variant_review`, supplies all-276 requirement/map/JSON capability and declaration/variant coverage. All 293 named anchors, including ten umbrellas and seven product-only rows, independently map exactly once in the index. The supporting reviewer independently closed VR-1 at GAP-09 SHA-256 `fb46f16aaab6924479144deb6fed79c5c93e3418e3f8f8ae6933b86cc568b363`: head dimensions/mass, bounded slider, latch and clearance/force controls are explicit.

### Resolved findings and affected re-review

| Finding | Independent re-review of final closure |
| --- | --- |
| F1 | Orphan UID row exists; regenerated deletion set covers 1,580/1,580 tracked paths exactly once, including both parked GPU sidecars. No tracked source was deleted in this slice. |
| F2 | TH-13/17/29 use first-freeze about25.883s, pack264.67K, explicit fresh second tray and about0.129 peak solid fraction; finite ambient/phase accounting agrees with independent oracle. |
| F3 | TH-30 and its source/sink closure use finite transient output, matched-demand estimates and actual demand-coupled law; no-sink motor may turn, power/temperature distinguish control. |
| F4 | All seven cylinder consumers now identify Story10.1; retained owner choices distinguish cylinder admission from alternative shapes. G/N references use the accepted story mapping. |
| F5 | EL-171 preload2.943N balances its plate, full spring and gravity stores agree, damping100Ns/m and maximum-compression yield bounded0.036m³ remove the fixed-yield contradiction. |
| F6 | EL-033 includes moving water column and piston weight; independently x0.131984m/E1.980993J at1m header; full stroke mouth509.136Pa/E21.5917J. |
| F7 | EL-036 explicit64-slot four-band sampler, passive1.5m useful-head regulator with dissipated excess and elevation-aware ballistic range; EL-172 basin datum agrees. Sector coverage is accumulated over the sampler cycle. |
| F8 | EL-173 no-lens control leaves both offset receivers dark; separate aligned receiver control proves the narrow beam. |
| F9 | CAT-017 C30 explicitly preserves all six invalid event identities and unchanged pending/accepted/colour state at baselineL162–195. |
| F10 | EL-028/029/169/170 use per-body water damping; dry cargo changes inertia only. Independent5% settling times13.804/19.173/24.900/21.569s support14/19/25/22s declarations. |
| F11 | RAD-17 uses shared Water domain and WaterInlet/WaterOutlet, matching supplier recipes. |
| F12 | RAD-20 uses TH-30 Carnot law with explicit proposed conductance/capacity overrides,288K ambient and matched-load estimates; independently0.504W60s/0.555W120s cooled versus0.00897/0.00293W uncooled. |
| VR-1 | Independent supporting reviewer resolved geometry/constraint declaration, mass, clearances and force discrimination as above. |

The owner's baseline-only source decision is satisfied. CAT-009/061/069 preserve the exact surviving caller tuples and independently sourced test radius/art values while making missing record-field mapping explicit. CAT-029 retains baseline source/cone-binding facts and the absent visual implementation gap. All 3,020 distinct explicit citations across574 source blobs resolve at a6c914e and remain in range; no pre-baseline legacy citations remain.

Every scoped local Markdown file/anchor resolves. All348 specs have six top-level sections and occur in the index. Requirements bytes are unchanged. A multiset comparison preserves all392 prior epics acceptance bullets;34 added bullets make pipe/funnel/cylinder obligations explicit. Dependency changes do not waive original acceptance: remaining scheduling loops and unsupported nudging integrations stay open and prevent their future dependent story completion. Open proposed-model/owner choices are allowed readiness-spec content, not undisclosed completed implementation.

Runtime, builds, Chrome and deployed-origin behavior are not applicable to this documentation-only slice and are not claimed. No code, schema, tests or runtime source is changed. The coordinator confirmed local commit is the only publication step required now; commit/tree identity still requires review after commit. A subsequent owner decision on CAT-015 recharge/partial affordable boost is a new narrow relevant-input delta and is not silently included in this frozen Pass.

## Final raw checks

The following are exact executed commands and tool outputs. Full Anvil JSON listed all358 files; its compact retained result is the parsed original result, not a second scan.

### Frozen structural/citation scan

```sh
python3 - <<'PY'
import json,pathlib,hashlib,re,subprocess,collections
root=pathlib.Path('.');snap=json.loads(pathlib.Path('_bmad-output/implementation-artifacts/story-7-0-snapshot.json').read_text())
print('snapshot_sha256',hashlib.sha256(pathlib.Path('_bmad-output/implementation-artifacts/story-7-0-snapshot.json').read_bytes()).hexdigest())
print('snapshot_keys',list(snap))
for key in ['candidate','relevant_inputs']:
 rows=snap.get(key,[]);bad=[p for p,h in rows if not pathlib.Path(p).exists() or hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()!=h]
 print(key,len(rows),'mismatches',bad)
files=sorted(pathlib.Path('docs/planning/elements').glob('*.md'))
specs=[p for p in files if re.match(r'(CAT|EL|TH|RAD|GAP)-\d+',p.name)]
print('specs',len(specs),collections.Counter(p.name.split('-')[0] for p in specs))
print('section_failures',[(str(p),len(re.findall(r'^## ',p.read_text(),re.M))) for p in specs if len(re.findall(r'^## ',p.read_text(),re.M))!=6])
cache={};bad=[];cit=set();revisions=set()
for p in files:
 for m in re.finditer(r'([\w./+-]+)@([a-f0-9]{7,40}):L(\d+)(?:-L(\d+))?',p.read_text()):
  path,rev,a,b=m.groups();a=int(a);b=int(b or a);cit.add((path,rev,a,b));revisions.add(rev)
  if (path,rev) not in cache:
   r=subprocess.run(['git','show',rev+':'+path],text=True,capture_output=True);cache[path,rev]=r.stdout.splitlines() if r.returncode==0 else None
  lines=cache[path,rev]
  if lines is None or a<1 or b<a or b>len(lines):bad.append((str(p),m.group()))
print('citations',len(cit),'blobs',len(cache),'revisions',sorted(revisions),'bad',bad)
readme=pathlib.Path('docs/planning/elements/README.md').read_text()
links=re.findall(r'\]\(([^)#]+)(?:#[^)]*)?\)',readme)
linked={(pathlib.Path('docs/planning/elements')/x).resolve() for x in links if x.endswith('.md')}
print('index_missing_specs',[str(p) for p in specs if p.resolve() not in linked])
print('non_document_candidate',[p for p,h in snap['candidate'] if not p.endswith(('.md','.yaml'))])
r=subprocess.run(['anvil','check','--extensions','md,yaml','--format','json']+[p for p,h in snap['candidate']],text=True,capture_output=True)
print('ANVIL_EXIT',r.returncode);print(r.stdout);print(r.stderr)
PY
```

Exit 0; raw output:

```text
Warning: truncated output (original token count: 5202)
Total output lines: 391

snapshot_sha256 d86c1cd61d96854981e09d7fde1c0a8391895de3956b42b295eb0beab1ff1a3f
snapshot_keys ['purpose', 'baseline_commit', 'workspace_head', 'reviewer', 'implementer', 'identity_format', 'candidate', 'current_inputs', 'legacy_inputs', 'excluded']
candidate 358 mismatches []
relevant_inputs 0 mismatches []
specs 348 Counter({'EL': 216, 'CAT': 72, 'TH': 37, 'RAD': 19, 'GAP': 4})
section_failures []
citations 3020 blobs 574 revisions ['a6c914e'] bad []
index_missing_specs []
non_document_candidate []
ANVIL_EXIT 0
Full file listing omitted here; original result c08544 and parsed result below retain clean358-file scan.
```

### Inputs, all identities, links and Anvil result

```sh
python3 - <<'PY'
import json,pathlib,hashlib,re,subprocess,collections,urllib.parse
s=json.loads(pathlib.Path('_bmad-output/implementation-artifacts/story-7-0-snapshot.json').read_text())
print('current_inputs',len(s['current_inputs']),'mismatches',[p for p,h in s['current_inputs'] if hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()!=h])
idx=pathlib.Path('docs/planning/elements/README.md').read_text();named=pathlib.Path('docs/planning/invest/named-elements.md').read_text()
anchors=re.findall(r'<a id="([^"]+)"',named);refs=re.findall(r'\]\(\.\./invest/named-elements\.md#([^)]+)\)',idx)
print('named_anchors',len(anchors),'index_refs',len(refs),'duplicates',[x for x,n in collections.Counter(refs).items() if n>1],'missing',sorted(set(anchors)-set(refs)),'extra',sorted(set(refs)-set(anchors)))
bad=[];targets={}
for p,h in s['candidate']:
 p=pathlib.Path(p)
 if p.suffix!='.md':continue
 for dest in re.findall(r'\]\(([^)]+)\)',p.read_text()):
  dest=dest.split(' "')[0].strip('<>')
  if re.match(r'[a-zA-Z][\w+.-]*:',dest):continue
  file,_,anchor=dest.partition('#');target=(p.parent/urllib.parse.unquote(file)).resolve() if file else p.resolve()
  if not target.exists():bad.append((str(p),dest,'missing_file'));continue
  if anchor and target.suffix=='.md':
   if target not in targets:
    text=target.read_text();ids=set(re.findall(r'<a\s+(?:id|name)="([^"]+)"',text));seen=collections.Counter()
    for head in re.findall(r'^#{1,6}\s+(.+?)\s*#*$',text,re.M):
     head=re.sub(r'\[([^]]+)\]\([^)]+\)',r'\1',head);head=re.sub(r'<[^>]+>','',head);slug=re.sub(r'[^\w\- ]','',head.lower()).replace(' ','-')
     n=seen[slug];seen[slug]+=1;ids.add(slug+('-'+str(n) if n else ''))
    targets[target]=ids
   if urllib.parse.unquote(anchor) not in targets[target]:bad.append((str(p),dest,'missing_anchor'))
print('link_failures',bad)
print('requirements_diff_bytes',len(subprocess.check_output(['git','diff','--','docs/planning/requirements.md'])))
a=json.loads(pathlib.Path('.anvil/last-check.json').read_text());print('anvil_result',{'files':len(a['files']),'checksRun':a['checksRun'],'warnings':a['warnings'],'summary':a['summary']})
PY
```

Exit 0; raw output:

```text
current_inputs 21 mismatches []
named_anchors 293 index_refs 293 duplicates [] missing [] extra []
link_failures []
requirements_diff_bytes 0
anvil_result {'files': 358, 'checksRun': ['secret-detection', 'antipattern-scan'], 'warnings': [], 'summary': {'total': 0, 'errors': 0, 'warnings': 0, 'info': 0, 'suppressed': 0}}

```

### Independent deletion scope

```sh
python3 - <<'PY'
import subprocess,re,pathlib,collections,xml.etree.ElementTree as ET
def blob(p):return subprocess.check_output(['git','show','a6c914e:'+p],text=True)
paths=set(subprocess.check_output(['git','ls-tree','-r','--name-only','a6c914e'],text=True).splitlines())
compiled=set()
for proj in ['CuriousContraptions.csproj','CuriousContraptions.tests/CuriousContraptions.tests.csproj']:
 for n in ET.fromstring(blob(proj)).iter('Compile'):
  if n.get('Include'):
   p=str(pathlib.PurePosixPath(proj).parent/n.get('Include'))
   compiled.add(p.removeprefix('./'))
dead=set()
for p in paths:
 if p.startswith(('reference/','engine/physics/','engine/bridge/','diagnostics/','CuriousContraptions.Geometry/')):dead.add(p)
 if re.match(r'tools/(p0-[^/]+|P0-007-[^/]+|PortableGeometryProof|LifecycleContractReview|Performance|Performance.Tests|Campaign|Playtest)/',p):dead.add(p)
 if p.startswith('CuriousContraptions.tests/') and (p.endswith('.cs') or p.endswith('.cs.orig')) and p not in compiled:dead.add(p)
 if re.match(r'(engine|parts)/[^/]+\.cs(?:\.uid)?$',p) and p.removesuffix('.uid') not in compiled:dead.add(p)
 if p in ['engine/gpu/WorkshopPipe.cs','engine/gpu/AnnularProfile.cs','engine/gpu/WorkshopPipe.cs.uid','engine/gpu/AnnularProfile.cs.uid']:dead.add(p)
for p in paths:
 if p.startswith('parts/scenes/') and p.endswith('.tscn'):
  if any(q in dead for q in re.findall(r'res://([^"]+)',blob(p))):dead.add(p)
for p in paths:
 if p.startswith('parts/catalog/') and p.endswith('.tres'):
  if any(q in dead for q in re.findall(r'res://([^"]+)',blob(p))):dead.add(p)
rows=[]
for l in pathlib.Path('docs/planning/elements/legacy-disposition.md').read_text().splitlines():
 m=re.match(r'\| `([^`]+)`(.*?) \|',l)
 if m:rows.append((m[1],'+ `.uid`' in m[2],l))
def covers(p,row):
 q,uid,_=row
 return p==q or p.startswith(q.rstrip('/')+'/') or uid and p==q+'.uid'
missing=[p for p in sorted(dead) if not any(covers(p,row) for row in rows)]
dupes=[p for p in sorted(dead) if sum(covers(p,row) for row in rows)>1]
print('deletion_expected',len(dead),'covered',len(dead)-len(missing),'missing',missing,'duplicates',dupes)
print('scopes',dict(collections.Counter(p.split('/')[0] for p in dead)))
print('orphan_row',[r[2] for r in rows if r[0]=='engine/physics/PreciseScalar.cs.uid'])
print('current_deleted_tracked',subprocess.check_output(['git','diff','--name-only','--diff-filter=D'],text=True).splitlines())
PY
```

Exit 0; raw output:

```text
deletion_expected 1580 covered 1580 missing [] duplicates []
scopes {'CuriousContraptions.tests': 417, 'diagnostics': 50, 'engine': 441, 'reference': 392, 'tools': 68, 'parts': 206, 'CuriousContraptions.Geometry': 6}
orphan_row ['| `engine/physics/PreciseScalar.cs.uid` | — | N-INFRA: orphan Godot identity sidecar; the corresponding source is absent at a6c914e, so it contains no element behaviour. |']
current_deleted_tracked []

```

### Acceptance preservation

```sh
python3 - <<'PY'
import pathlib,re,subprocess,collections
p='_bmad-output/planning-artifacts/epics.md'
old=subprocess.check_output(['git','show','HEAD:'+p],text=True);new=pathlib.Path(p).read_text()
def accepts(s):
 out=[]
 for piece in re.split(r'^### Story ',s,flags=re.M)[1:]:
  if '**Acceptance Criteria:**' not in piece:continue
  ac=piece.split('**Acceptance Criteria:**',1)[1].split('\n## ')[0]
  out += [l.strip() for l in ac.splitlines() if l.strip().startswith('- **')]
 return collections.Counter(out)
a,b=accepts(old),accepts(new)
print('old_acceptance_bullets',sum(a.values()),'new',sum(b.values()),'removed',list((a-b).elements()))
print('added',list((b-a).elements()))
print('tracked_non_document_diff',[p for p in subprocess.check_output(['git','diff','--name-only'],text=True).splitlines() if not p.endswith(('.md','.yaml'))])
PY
```

Exit 0; raw output:

```text
Warning: truncated output (original token count: 1370)
Total output lines: 3

old_acceptance_bullets 392 new 426 removed []
Added34 acceptance bullets (pipe/funnel/cylinder); tracked_non_document_diff []. Full original tool result a4a76f.

```

### Fixed numerical oracle output

Independent Python scalar equilibrium/ODE check used dt=1/480, the declared accumulator force balance, per-body damping fraction and RAD-20 hot/cold/sink energy equations. Exact command is retained in tool transcript with this output; source variables and formulae are detailed in the finding table.

```text
accumulator 0.13198435671292427 136.2437353703394 156.96 1.9809928052112944
accumulator 0.5 430.65625 509.13625 21.5917
platform_empty tau 4.608 time5% 13.804334316536789
platform_ball tau 4.608 time5% 13.804334316536789
boat_empty tau 6.3999999999999995 time5% 19.17268655074554
boat_ball tau 6.3999999999999995 time5% 19.17268655074554
cork_empty tau 8.311688311688313 time5% 24.89959292304616
cork_domino tau 8.311688311688311 time5% 24.899592923046157
raft_empty tau 7.199999999999999 time5% 21.56927236958873
raft_ball tau 7.199999999999999 time5% 21.56927236958873
RTG True [(60, 0.503979265445782, 327.2204696990676, 290.14913970925403, 289.7031699117308), (120, 0.5547035142190688, 330.1795950108298, 291.1121421876661, 290.6416526407634), (300, 0.5601589596607526, 330.7926199509064, 291.4971101375957, 291.02343255638914)]
RTG False [(60, 0.008966290140050053, 367.1065915030714, 361.8693438398653, 288.0), (120, 0.002934232454885562, 414.390730255097, 411.2076067811325, 288.0), (300, 0.00013084580727958895, 471.3937190988282, 470.6767943747115, 288.0)]

```

## Narrow final delta: independently approved before local commit

The same implementation owner corrected late finding **F13**: the original RAD-20 control used two0.1W recipe motors (maximum combined0.2W) as an overload against a cooled source available0.50–0.56W. That cannot establish overload. The replacement requires physically resisting motor loads, real torque/speed controls and an observed summed requested power greater than current availability before checking bounded delivery and conserved heat/work. The reviewer independently reread the actual change: **F13 resolved**. The brief intermediate Fail was correct while this known violation remained; the frozen Pass alone is not the final approval.

The owner subsequently required CAT-015 recharge now, affordable partial boost when energy is insufficient and ordinary bounce at zero. CAT-015 and EL-195 now preserve that finite-accounting behavior, while explicitly leaving source/port/rate unresolved. No Battery default is inferred; EL-195 Supplied remains a separate required variant. README G/N status rows are mechanical readiness labels, not runtime claims. Story completion tasks/status, owner questions, TODO and ledger now reflect this result. The deletion summary was corrected from1,578 to1,580 because both parked GPU UID sidecars are included in the final enumeration, already covered by ledger rows.

Actual reviewed delta: `.anvil/story-7-0-final-delta.patch`, SHA-256 `52c9894e1af51bb25ee4dc1cd7fa6c3db12c23ca0857ed7d7d53676a45ba7bf6`. Full initial candidate diff: `.anvil/story-7-0-candidate.patch`, implementer SHA-256 `0ae195b6913289194f40bf06032f302f957a852f70deb020ef4699adaa983105`; delivered spec content and tracked diffs were independently read. The original immutable snapshot remains unchanged; these nine exact hashes supersede its corresponding entries. All other349 candidate files and all21 current inputs still match. Review-only records remain excluded.

Exact hash verification command:

```sh
python3 - <<'PY'
import json,pathlib,hashlib
s=json.loads(pathlib.Path('_bmad-output/implementation-artifacts/story-7-0-snapshot.json').read_text())
print('candidate_changes')
for p,h in s['candidate']:
 now=hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
 if h!=now:print(p,now)
print('input_changes',[p for p,h in s['current_inputs'] if hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()!=h])
PY
```

Raw output, exit0, tool chunk a0f071:

```text
candidate_changes
TODO.md d04ba8f9dd2471ae0b4150e39120e97b06685942c51828cb7ad794fbb2a911e2
_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md ba399464795db5ad5a5d5710ec153fb449be5cc27dba05581dba64298e0db073
_bmad-output/implementation-artifacts/sprint-status.yaml f4171421c50d10f470a39e479744368989650309db2a198c4380f00d7262d759
_bmad-output/implementation-artifacts/story-7-0-owner-questions.md ee0095edcb155829daac98aff173001d06d36346726fbffe785e3771345be95c
docs/planning/elements/CAT-015-bumper.md fbee990590787d1f3b8ded35d1b4f765462379bbfbe429957fe4ce5adde4b955
docs/planning/elements/EL-195-pinball-bumper.md d93e2c822bd36cdec906a35e2788343e963c9bb1b7493111d9b1fd813aeef271
docs/planning/elements/RAD-20-radioisotope-thermoelectric-generator.md f537b22915985ce24571aac5acdf7826a505cf4613100bf97d9d6cdd5eb73b6a
docs/planning/elements/README.md 2acddfe1dd28773c66424653d3b7cf9e9b3212f5152e6d071b8f516e1cd3e8be
docs/planning/elements/legacy-disposition.md 4997df7f3bce5a499795337a0ca2879cb0e4f9119e39443d1dd59455d7e503a5
input_changes []

```

Independent `anvil check --extensions md,yaml --format json <the nine changed paths>` returned exit0/files9/warnings[]/checks[secret-detection,antipattern-scan] (tool chunk09ea39). Final mechanical count wording passed implementer's identical9-file scan, independently read from `.anvil/last-check.json` (tool190531). Existing links and citations are retained; newly added review links resolve to the same existing record. No requirement or source-input bytes changed.

**SnapshotApproval: Pass.** Every pre-commit documentation criterion is satisfied for the original358-file snapshot with these nine replacements. Local commit only is authorized by the owner/coordinator's existing instruction. The genuinely publication-dependent check is exact local commit/tree membership and bytes; no remote/deployed/runtime check applies to this documentation slice. The same reviewer retains ownership of that check.

### Exact independent fixed numerical command

This is the exact command for the fixed numerical output retained above (no repeat run needed):

```sh
python3 - <<'PY'
import math
rho,g,A,k,mp=16.,9.81,.16,128.,.5
for x in [(rho*g*A-mp*g)/(k+rho*g*A),.5]:
 pf=(k*x+mp*g)/A; pm=pf+rho*g*x; E=.5*k*x*x+mp*g*x+.5*rho*g*A*x*x
 print('accumulator',x,pf,pm,E)
for kind,moving,damped,displacement in [('platform_empty',2,2,4.608),('platform_ball',3,2,4.608),('boat_empty',1.2,1.2,3.84),('boat_ball',2.2,1.2,3.84),('cork_empty',.154,.154,.64),('cork_domino',.554,.154,.64),('raft_empty',1,.8,2.88),('raft_ball',2,.8,2.88)]:
 fraction=moving/displacement; tau=2*moving/(damped*fraction);print(kind,'tau',tau,'time5%',tau*math.log(20))
for sink in [True,False]:
 h,c,s=288.,288.,288.;dt=1/480;out=[]
 for tick in range(480*300):
  q=.4*(h-c);p=max(0,min(1,.3*(1-c/h)*q));contact=32*(c-s) if sink else 0
  h+=(20-.1*(h-288)-q)/10*dt;c+=(q-p-contact)/2*dt;s+=(contact-5*(s-288))/225*dt
  if tick+1 in [480*60,480*120,480*300]:out.append((round((tick+1)*dt),p,h,c,s))
 print('RTG',sink,out)
PY
```
