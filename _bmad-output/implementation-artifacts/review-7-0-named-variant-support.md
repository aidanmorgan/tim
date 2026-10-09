# Story 7.0 named variant/declaration supporting review

Reviewer identity: `/root/variant_review`. Implementation owner: `/root/implementation`.
Terminal reviewer: `/root/reviewer`. Date: 2026-10-09.
Read-only supporting review; no deliverables edited. This record contains reviewer evidence only.

## Criterion and impact scope

Authority: [Story 7.0 spec](spec-7-0-element-implementation-readiness.md), AGENTS.md, delivery-workflow paired review and evidence rules. Scope is the 276 named EL/TH/RAD/GAP specs, their exact requirements and named-element anchors, capability map rows and coverage binding capabilities. Main reviewer binds the frozen candidate; this preliminary supporting record does not assert a frozen snapshot or terminal Pass.

| Criterion | Check and result | Limits |
| --- | --- | --- |
| Exactly one full spec per named identity | 216 EL, 37 TH, 19 RAD, 4 GAP; 276 total. All have six top-level sections. | File/section structure, not blanket semantic approval. |
| Source identity preservation | Each spec identity has exactly one requirements anchor, named entry and map row. | No requirement edits made. |
| Declaration completeness | All 276 declarations checked for ten required categories, followed by semantic examination of apparent misses. GAP-09 produces VR-1; EL-208 tether covers its implicit constraint. | Keyword scan is triage; numerical/body-geometry derivations not comprehensively repeated. |
| Capability source mapping | Every JSON-binding capability is named in its spec. | Six delivered EL specs omit literal proof-owner IDs; not a failure because authoritative binding remains linked and Story 7.0 does not require repeating that literal ID. |
| Variant preservation | Source-row variant/mode/preset extraction; examined electrical and optical identity separation, pool-ball conditional gravity, heat-pump reversal, programmable presets, radiation energy/aperture/decay presets, granular and fragmentation modes. No dropped explicit variant established. | Does not claim exhaustive revalidation of every numeric proposal or previous batch Pass. |
| Runtime / builds / legacy deletion | Not applicable to this supporting documentation check. | No runtime, build, legacy-harvest sampling or citation resolution claimed here; main review owns those checks. |

Batch coverage: F 22, G 22, H 30, I 22, J 28, K 28, L 32, M 29, N 37, O 26 = 276. Every batch received identity/section/declaration-category/capability membership checks. Semantic variant probes above concentrate on H/I/K/L/M/N/O; water F/G variant declarations and their separate neighboring identities were inspected, but all water numeric chains were not re-derived.

## Finding VR-1 — declaration incomplete

Status: open, sent to implementation and terminal reviewer.

[Fragmentation station](../../docs/planning/elements/GAP-09-fragmentation-station.md) section 2 names a moving head, Impact latch and Press linear drive but does not define head shape/dimensions or slider/latch axis, travel/end stops and latch relationship. Press head mass is not stated as shared with the Impact head. Section 3 schedules slider/latch capabilities, which does not supply the missing declaration.

Story 7.0 requires bodies/shapes with dimensions and constraints/joints for every full spec. Smallest fix: proposed shared head box dimensions and mass, explicit vertical slider/latch declarations for both variants with travel fitting the 0.2–1.0 m drop and arch; explain unsourced choices. Re-review those additions and geometry consistency.

## Original raw commands and results

Commands ran from `/Users/aidan/dev/personal/tim`; all recorded commands below exited 0. These are preserved original command bodies/results, not rerun claims.

### Identity and declaration inventory (tool chunk 6e6de4)

```sh
python3 - <<'PY'
import pathlib,re
r=pathlib.Path('docs/planning/requirements.md').read_text(); n=pathlib.Path('docs/planning/invest/named-elements.md').read_text(); em=pathlib.Path('docs/planning/general-engine-element-map.md').read_text(); counts={}; errors=[]
for p in pathlib.Path('docs/planning/elements').glob('*.md'):
 m=re.match(r'(EL|TH|RAD|GAP)-(\d+)',p.name)
 if not m:continue
 counts[m[1]]=counts.get(m[1],0)+1;a={'EL':'element','TH':'thermal','RAD':'radiation','GAP':'gap'}[m[1]]+'-'+m[2]; t=p.read_text()
 for label,src,needle in [('requirement',r,'id="'+a+'"'),('named entry',n,'id="'+a+'"'),('map',em,'requirements.md#'+a+')')]:
  if src.count(needle)!=1:errors.append((p.name,label,src.count(needle)))
 sections=re.split(r'^## ',t,flags=re.M)[1:]
 decl=sections[1].lower()
 for label,pat in [('body',r'bod(?:y|ies)|shape'),('material',r'material|friction'),('constraint',r'constraint|joint'),('port',r'port|socket'),('activation',r'sensor|activation'),('energy',r'energy|work'),('parameters',r'parameter'),('cosmetics',r'cosmetic|animation'),('art',r'\bart\b'),('catalogue',r'catalogue|inventory')]:
  if not re.search(pat,decl):errors.append((p.name,'declaration '+label))
print('identity counts',counts,'sum',sum(counts.values()))
print('source identity and declaration coverage errors',errors)
PY
```

```text
identity counts {'EL': 216, 'RAD': 19, 'TH': 37, 'GAP': 4} sum 276
source identity and declaration coverage errors [('EL-208-balloon.md', 'declaration constraint'), ('GAP-09-fragmentation-station.md', 'declaration constraint')]
```

Manual follow-up (tool chunk 4142be) read `sed -n '1,125p' docs/planning/elements/GAP-09-fragmentation-station.md; sed -n '1,100p' docs/planning/elements/EL-208-balloon.md`. Actual GAP-09 §2 declared a 3 kg Impact hammer and 200 N Press drive, with no head dimensions or explicit joint declaration. EL-208 §2 Behavior 2 declared the Tie socket at (0, −r, 0) and taut-rope restraint, so the heading-level absence is a false positive.

### Capability membership (tool chunk 88c95d)

```sh
python3 - <<'PY'
import pathlib,json,re
by={}
for p in pathlib.Path('docs/coverage/engine').glob('*.json'):
 d=json.loads(p.read_text())
 for c in d.get('Consumers',[]):
  source=c.get('Source',{}).get('Id','')
  if re.fullmatch(r'(element|thermal|radiation|gap)-\d+',source):by[source]=c
miss=0
for f in sorted(pathlib.Path('docs/planning/elements').glob('*.md')):
 m=re.match(r'(EL|TH|RAD|GAP)-(\d+)',f.name)
 if not m:continue
 a={'EL':'element','TH':'thermal','RAD':'radiation','GAP':'gap'}[m[1]]+'-'+m[2];t=f.read_text();c=by.get(a)
 if not c:print('NO_BINDING',a);continue
 missing=[x for x in c['Capabilities'] if x not in t]
 if missing: print('MISSING_CAPABILITY',f.name,','.join(missing));miss+=1
 if c.get('ProofOwner') and c['ProofOwner'] not in t:print('MISSING_OWNER',f.name,c['ProofOwner'])
print('specs with unnamed binding capabilities',miss)
PY
```

```text
MISSING_OWNER EL-187-basketball.md S294
MISSING_OWNER EL-188-bowling-ball.md S295
MISSING_OWNER EL-189-tennis-ball.md S297
MISSING_OWNER EL-190-ramp.md S293
MISSING_OWNER EL-191-wall.md S298
MISSING_OWNER EL-204-weight.md S306
specs with unnamed binding capabilities 0
```

### Original structural probe (tool chunk 3a443a)

```sh
python3 - <<'PY'
import re,pathlib
r=pathlib.Path('docs/planning/requirements.md').read_text()
files=list(pathlib.Path('docs/planning/elements').glob('EL-*.md'))+list(pathlib.Path('docs/planning/elements').glob('TH-*.md'))+list(pathlib.Path('docs/planning/elements').glob('RAD-*.md'))+list(pathlib.Path('docs/planning/elements').glob('GAP-*.md'))
print('FILES',len(files))
for f in sorted(files):
 t=f.read_text(); h=re.findall(r'^## (.+)',t,re.M)
 if len(h)!=6:print('HEADINGS',f.name,h)
 for word in ['variant','material','parameters','cosmetic','inventory','save','reset','control']:
  if word not in t.lower():print('ABSENT',f.name,word)
PY
```

```text
FILES 276
ABSENT EL-187-basketball.md variant
ABSENT EL-188-bowling-ball.md variant
ABSENT EL-189-tennis-ball.md variant
ABSENT EL-208-balloon.md variant
```

The original command also scanned source-record lines for variant wording; it printed no additional results. Literal “variant” absence is not a defect for the three distinct balls; EL-208 separately declares buoyant rise, tether and puncture.

## Supporting outcome

Known violation VR-1 remains open at this record's creation. No terminal verdict. Exact source identities and review of the frozen candidate belong to the main reviewer record; this supporting work cannot alone establish story completion.

## VR-1 narrow re-review — resolved

The same reviewer `/root/variant_review` independently read the corrected GAP-09 §2 after implementation notification. Reviewed file SHA-256: `fb46f16aaab6924479144deb6fed79c5c93e3418e3f8f8ae6933b86cc568b363`.

The shared 3 kg head is now a 0.60 × 0.10 × 0.60 m box; a vertical prismatic joint fixes horizontal position/orientation and bounds underside height to 0.10–1.60 m. Impact locks at 0.60 + drop_height until activation; Press declares bounded powered drive and an unsupplied holding lock. New values are explicitly proposed with geometry/gameplay reasons. Maximum head top 1.70 m clears the proposed 1.75 m crossbar underside by 0.05 m; minimum release underside 0.80 m clears the intact block top 0.60 m. Resting head force 29.43 N is below either crush threshold; 200 N drive exceeds both. No unintended change to named modes or their existing controls found.

**Scoped outcome: VR-1 resolved; supporting declaration/identity/variant review has no remaining known finding.** This does not establish a terminal Story 7.0 Pass or comprehensively revalidate numerical proposals, legacy harvest or prior batch evidence.

Raw re-review command, exit 0:

```sh
python3 - <<'PY'
import pathlib,hashlib
p=pathlib.Path('docs/planning/elements/GAP-09-fragmentation-station.md'); b=p.read_bytes()
print('reviewed_path',p)
print('sha256',hashlib.sha256(b).hexdigest())
print('head_top_at_max_drop',0.60+1.0+0.10)
print('crossbar_clearance',1.75-(0.60+1.0+0.10))
print('minimum_release_underside',0.60+0.2)
print('head_mass_kg',3)
print('resting_head_force_N',3*9.81)
print('minimum_crush_threshold_N',120)
print('drive_force_N',200)
print('maximum_crush_threshold_N',180)
for term in ['0.60 × 0.10 × 0.60','mass 3 kg in both variants','vertical prismatic joint','0.10 to 1.60','releasable lock','unsupplied static holding lock']:
 print('declared',term,term in b.decode())
PY
```

```text
reviewed_path docs/planning/elements/GAP-09-fragmentation-station.md
sha256 fb46f16aaab6924479144deb6fed79c5c93e3418e3f8f8ae6933b86cc568b363
head_top_at_max_drop 1.7000000000000002
crossbar_clearance 0.04999999999999982
minimum_release_underside 0.8
head_mass_kg 3
resting_head_force_N 29.43
minimum_crush_threshold_N 120
drive_force_N 200
maximum_crush_threshold_N 180
declared 0.60 × 0.10 × 0.60 True
declared mass 3 kg in both variants True
declared vertical prismatic joint True
declared 0.10 to 1.60 True
declared releasable lock True
declared unsupplied static holding lock True
```


