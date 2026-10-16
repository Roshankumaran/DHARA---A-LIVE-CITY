from pathlib import Path
import xml.etree.ElementTree as E,json,re,csv
root_dir=Path(__file__).resolve().parents[1]
runs=[];latest={}
for filename in ('exploration-editmode.xml','exploration-playmode.xml','exploration-visual-retest.xml'):
 root=E.parse(root_dir/'Verification'/filename).getroot();runs.append({'file':filename,**{k:root.get(k) for k in ('total','passed','failed','skipped','duration')},'failures':[t.get('fullname') for t in root.iter('test-case') if t.get('result')=='Failed']})
 for t in root.iter('test-case'):latest[t.get('fullname')]={'name':t.get('name'),'result':t.get('result'),'duration':t.get('duration'),'evidence':filename}
source={}
for name in ('EditMode/ExplorationModelTests.cs','PlayMode/ExplorationRuntimeTests.cs'):
 relative=Path('Assets/Dhara/Tests')/name;s=(root_dir/relative).read_text(encoding='utf-8');matches=list(re.finditer(r'\[(?:UnityTest|Test)\]\s+public\s+(?:void|IEnumerator)\s+([A-HS]\d{2}_\w+)\s*\(',s))
 for i,m in enumerate(matches):
  end=matches[i+1].start() if i+1<len(matches) else len(s)
  source[m.group(1)]={'source':str(relative).replace('\\','/'),'line':s[:m.start()].count('\n')+1,'conditionsAndExpectedResult':s[m.start():end].strip()}
records=[]
for full,result in sorted(latest.items()):
 n=result['name'];match=re.match(r'([A-HS]\d{2})_',n)
 if match:records.append({'id':match.group(1),'fullname':full,**result,**source[n]})
assert len(records)==168,(len(records),len(source))
summary={'newDistinctCases':len(records),'totalDistinctCases':len(latest),'newPassed':sum(r['result']=='Passed' for r in records),'latestPassed':sum(v['result']=='Passed' for v in latest.values()),'latestFailed':sum(v['result']=='Failed' for v in latest.values()),'blockedOrUnexecuted':sum(v['result'] not in ('Passed','Failed') for v in latest.values()),'finalRuns':runs,'counting':'87 EditMode + 146 full-suite PlayMode cases + one additional edge-coverage case = 234 distinct cases. The 130-case affected-runtime rerun overlaps 129 full-suite cases; repeated runs and assertions are not additional cases. 33 standalone core checks and 5 HTTPS checks are separate.','groups':{g:sum(r['id'].startswith(g) for r in records) for g in 'ABCDEFGHS'},'cases':records}
(root_dir/'Verification/exploration-test-manifest.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
with (root_dir/'Verification/exploration-test-cases.csv').open('w',newline='',encoding='utf-8') as f:
 w=csv.DictWriter(f,fieldnames=['id','name','result','duration','evidence','source','line','conditionsAndExpectedResult']);w.writeheader();w.writerows({k:r[k] for k in w.fieldnames} for r in records)
print({k:v for k,v in summary.items() if k!='cases'})
