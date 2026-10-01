#!/usr/bin/env python3
"""Reconcile all source definitions; code references and catalog records never imply functional pass."""
import csv,json,re,hashlib,os
from pathlib import Path
root=Path(__file__).resolve().parents[2]
cat=list(csv.DictReader((root/'docs/action-catalog/catalog.csv').open()))
coverage=json.loads((root/'docs/implementation/functional-coverage.json').read_text());old={a['actionKey']:a for a in coverage['actions']}
screens=[]
for line in (root/'docs/ux-ui/screen-inventory.md').read_text().splitlines():
 if re.match(r'\| FX\d\d-S\d',line):
  cells=[c.strip() for c in line.strip('|').split('|')];screens.append(dict(zip(['id','name','route','scope'],cells[:4])))
assert len(cat)==733 and len({r['ActionKey'] for r in cat})==733
assert len(screens)==202 and len({r['id'] for r in screens})==202
assert len({r['Feature'] for r in cat})==40
handlers={1:'Identity',2:'Access',3:'Modules',4:'Sharing',5:'Support',6:'Notifications',7:'Files',8:'Trash',9:'Settings',11:'Productivity',12:'Productivity',13:'Productivity',14:'Reminders',15:'Planner',16:'Goals',17:'Habits',20:'Documents',21:'Bookmarks',22:'Snippets',23:'Reading',24:'Organization',25:'Discovery',26:'Dashboard',27:'Finance',32:'DeveloperTools'}
refs={r['ActionKey']:[] for r in cat}
for p in (root/'src/Nexora.Infrastructure').rglob('*Service.cs'):
 s=p.read_text()
 for key in refs:
  if '"'+key+'"' in s:refs[key].append(str(p.relative_to(root)))
rows=[]
for r in cat:
 key=r['ActionKey'];num=int(r['Feature'][-2:]);c=old[key]
 rows.append({'planningId':c['planningId'],'actionKey':key,'feature':r['Feature'],'kind':r['Kind'],'context':r['Context'],'catalogStatus':r['Status'],'sourceScreens':r['Screens'],'candidateEndpointModule':handlers.get(num,''),'serviceLiteralReferences':refs[key],'functionalResult':c['result'],'testBindings':c.get('testBindings',[]),'runtimeGateResult':'Not run','positiveResult':'Not run' if c['result']=='Not run' else c['result'],'missingEvidence':'Full state/role/boundary/failure/race matrix unverified; candidate references are not evidence of a handler or execution.'})
for s in screens:
 s['route']=s['route'].strip('`');s['feature']='FX-'+s['id'][2:4];s['actionKeys']=[r['ActionKey'] for r in cat if s['id'] in r['Screens']];s['definitionResult']='Not run';s['routeProbe']='Not run';s['functionalStates']='Not run'
modules=[{'feature':f'FX-{n:02}','goal':f'NXG-FX{n:02}-G01','candidateEndpointModule':handlers.get(n,''),'actionCount':sum(r['feature']==f'FX-{n:02}' for r in rows),'screenCount':sum(s['feature']==f'FX-{n:02}' for s in screens),'functionalResult':'Incomplete'} for n in range(1,41)]
for x in [*rows,*screens]:x['fullScopePassed']=False
out=Path(os.environ.get('NEXORA_QA_EVIDENCE_DIR',str(root.parent/'evidence')))/'full-plan-ledger.json'
out.write_text(json.dumps({'sourceCommit':'fa4e46053f533693c3b13e101345e335f99324fb','authorityCommit':'8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3','counts':{'features':40,'screenDefinitions':202,'actionKeys':733},'evidenceRule':'No source/catalog/route probe counts as full functional pass. Retired/gated/missing implementation remain separate.','modules':modules,'screens':screens,'actions':rows},ensure_ascii=False,separators=(',',':'))+'\n')
print(json.dumps({'features':len(modules),'screens':len(screens),'actions':len(rows),'modulesWithCandidateEndpointFolders':len(handlers),'fullScopePassed':0}))
