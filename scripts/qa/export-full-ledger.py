#!/usr/bin/env python3
"""Export exact complete scope; metadata and route probes do not certify workflows."""
import csv,json,os,re,collections
from pathlib import Path
root=Path(__file__).resolve().parents[2]
out=Path(os.environ.get('NEXORA_QA_EVIDENCE_DIR',str(root.parent/'evidence')))
l=json.loads((out/'full-plan-ledger.json').read_text());coverage=json.loads((root/'docs/implementation/functional-coverage.json').read_text())
bindings={
'FN-034':['notifications.inbox.read','notifications.inbox.mark_read','notifications.inbox.mark_unread','notifications.inbox.mark_all_read','notifications.inbox.delete'],
'FN-035':['access.user.read','modules.catalog.read'],
'FN-036':['access.permission.read','access.permission.set','settings.preference.read','settings.preference.update'],
'FN-037':['access.entitlement.set','goals.goal.read'],
'FN-038':['modules.catalog.read','modules.policy.defaults'],
'FN-039':['planner.plan.read','planner.plan.reorder'],
'FN-040':['projects.project.read','projects.project.update','projects.project.skip']}
r=json.loads((out/'full-200-results.json').read_text());assert r['stats']['expected']==200 and all(r['stats'][k]==0 for k in ['unexpected','flaky','skipped'])
for a in l['actions']:
 if a['functionalResult']=='Excluded-retired':a['positiveResult']='Excluded-retired'
 ids=[k for k,v in bindings.items() if a['actionKey'] in v]
 if ids:
  a['testBindings']=sorted(set(a['testBindings']+ids));a['functionalResult']='Partial coverage';a['positiveResult']='Partial coverage';a['executedSubsetResult']='Passed on 5 viewports; only bound assertions'
 a['fullScopePassed']=False
for kind in ['modules','screens','actions']:
 rows=l[kind]
 if kind=='modules':
  for m in rows:
   items=[a for a in l['actions'] if a['feature']==m['feature']];m['runtimeState']=items[0]['runtimeModule']['State'];m['systemEnabled']=items[0]['runtimeModule']['SystemEnabled'];m['partialActions']=sum(a['functionalResult']=='Partial coverage' for a in items);m['notRunActions']=sum(a['functionalResult']=='Not run' for a in items);m['retiredActions']=sum(a['functionalResult']=='Excluded-retired' for a in items);m['missingSqlActionKeys']=sum(a['runtimePermission'] is None for a in items);m['fullScopePassed']=False
 # Stable complete schemas; lists/objects serialized in individual CSV cells.
 fields=list(dict.fromkeys(k for x in rows for k in x))
 with (root/f'docs/implementation/full-plan-{kind}.csv').open('w',newline='') as f:
  writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows({k:json.dumps(v,ensure_ascii=False,separators=(',',':')) if isinstance(v,(dict,list)) else v for k,v in row.items()} for row in rows)
assert [len(l[k]) for k in ['modules','screens','actions']]==[40,202,733]
(out/'full-plan-ledger.json').write_text(json.dumps(l,ensure_ascii=False,separators=(',',':'))+'\n')
summary={'counts':l['counts'],'actionResults':dict(collections.Counter(a['functionalResult'] for a in l['actions'])),'positiveResults':dict(collections.Counter(a['positiveResult'] for a in l['actions'])),'fullScopePassed':False,'browser':r['stats'],'evidenceRule':l['evidenceRule']}
(out/'full-plan-summary.json').write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps(summary))
