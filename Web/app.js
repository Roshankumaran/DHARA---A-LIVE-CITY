import {cities,weatherFields,airFields,condition,parseResponse,url,retryDelay,csv} from './data.js';
const $=id=>document.getElementById(id);
let generation=0,controller,timer;
let states={weather:{},air:{}};
const selectedCity=()=>cities[Number($('city').value)];
for(const [index,city] of cities.entries())$('city').add(new Option(city[0],index));
function status(state){
 if(state.loading)return state.data?'Updating · retained values':'Loading…';
 if(state.error)return `Request failed · ${state.data?'retained values':'unavailable'} · ${state.error}`;
 if(state.cooldown>Date.now())return 'Waiting for API retry cooldown';
 if(!state.data)return 'Unavailable';
 if(Date.now()-state.data.retrieved>600000)return 'Stale data';
 return state.data.current.some(f=>f.value===null)?'Partial model data':'Model estimates connected';
}
function shownWeather(){return states.weather.data?.forecast.find(f=>f.time===$('forecast').value)?.fields??states.weather.data?.current;}
function table(id,fields,definitions){
 const body=$(id);body.replaceChildren();
 for(const [key,name,unit] of definitions){
  const field=fields?.find(f=>f.key===key);const tr=document.createElement('tr');
  const label=document.createElement('th');label.scope='row';label.textContent=name;
  const value=document.createElement('td');value.textContent=field?.value==null?'Unavailable':`${new Intl.NumberFormat('en-IN',{maximumFractionDigits:2}).format(field.value)} ${field.unit}`;
  tr.append(label,value);body.append(tr);
 }
}
function render(){
 const w=states.weather,a=states.air;const fields=shownWeather();
 $('city-name').textContent=selectedCity()[0];
 $('weather-status').textContent=status(w);$('air-status').textContent=status(a);
 $('weather-status').className=w.error?'status error':'status';$('air-status').className=a.error?'status error':'status';
 table('weather-values',fields,weatherFields);table('air-values',a.data?.current,airFields);
 const time=fields?.[0]?.time;
 $('weather-meta').textContent=`${$('forecast').value==='current'?'Current model estimate':'Hourly forecast'} · valid ${time??'unavailable'} IST · retrieved ${w.data?new Date(w.data.retrieved).toISOString():'unavailable'}`;
 $('air-meta').textContent=`Current CAMS model estimate · valid ${a.data?.time??'unavailable'} IST · retrieved ${a.data?new Date(a.data.retrieved).toISOString():'unavailable'}`;
 const code=fields?.find(f=>f.key==='weather_code')?.value;
 $('condition').textContent=condition(code);
 $('aggregation').textContent=`Rain rate is the preceding ${fields?.[0]?.interval??'unknown'}-second rain total divided by interval hours. Precipitation is a separate total; solar radiation is an interval mean. Wind direction is FROM.`;
 $('refresh').disabled=Boolean(w.loading||a.loading);
 $('export').disabled=!w.data&&!a.data;
 $('forecast').disabled=!w.data?.forecast.length;
 const waits=[w,a].filter(s=>s.cooldown>Date.now()).map(s=>Math.ceil((s.cooldown-Date.now())/1000));
 $('retry-note').textContent=waits.length?`Automatic retry scheduled; server cooldown up to ${Math.max(...waits)} seconds. Changing cities does not bypass a server cooldown.`:'Refreshes every five minutes. Failed requests retry with a bounded delay. Missing values remain unavailable.';
}
function forecastOptions(){
 const old=$('forecast').value;$('forecast').replaceChildren(new Option('Current weather','current'));
 for(const entry of states.weather.data?.forecast??[])$('forecast').add(new Option(`${entry.time.replace('T',' ')} IST`,entry.time));
 if([...$('forecast').options].some(o=>o.value===old))$('forecast').value=old;
}
async function request(kind,gen){
 const state=states[kind];
 if(state.cooldown>Date.now())return;
 state.loading=true;render();
 const timeout=new AbortController();const timeoutId=setTimeout(()=>timeout.abort(),20000);
 try{
  const response=await fetch(url(selectedCity(),kind),{signal:AbortSignal.any([controller.signal,timeout.signal]),cache:'no-store'});
  if(!response.ok){
   if(response.status===429||response.status===503)state.cooldown=Date.now()+retryDelay(response.headers.get('Retry-After'),(state.failures??0)+1);
   throw Error(`HTTP ${response.status}`);
  }
  const data=parseResponse(await response.json(),kind);
  if(gen!==generation)return;
  if(state.data&&data.time<state.data.time)throw Error('Older response rejected');
  state.data=data;state.error=null;state.failures=0;state.cooldown=0;state.due=Date.now()+300000;
  if(kind==='weather')forecastOptions();
 }catch(error){
  if(gen!==generation)return;
  state.error=timeout.signal.aborted?'Request timed out':error.message;state.failures=(state.failures??0)+1;
  state.due=Math.max(state.cooldown??0,Date.now()+retryDelay(null,state.failures));
 }finally{clearTimeout(timeoutId);if(gen===generation){state.loading=false;render();}}
}
async function refresh(force=false){
 clearTimeout(timer);const gen=generation;
 await Promise.all(['weather','air'].filter(k=>force||!states[k].due||states[k].due<=Date.now()).map(k=>request(k,gen)));
 if(gen!==generation)return;
 const due=Math.min(...Object.values(states).map(s=>Math.max(s.due??Date.now(),s.cooldown??0)));
 timer=setTimeout(()=>refresh(),Math.max(1000,due-Date.now()));render();
}
function changeCity(){
 const cooldowns={weather:states.weather.cooldown,air:states.air.cooldown};
 generation++;controller?.abort();clearTimeout(timer);controller=new AbortController();
 states={weather:{cooldown:cooldowns.weather},air:{cooldown:cooldowns.air}};
 $('forecast').replaceChildren(new Option('Current weather','current'));$('export-status').textContent='';render();refresh(true);
}
$('city').addEventListener('change',changeCity);
$('refresh').addEventListener('click',()=>refresh(true));
$('forecast').addEventListener('change',render);
$('export').addEventListener('click',()=>{
 try{
  const groups=[];
  for(const kind of ['weather','air']){const state=states[kind];if(state.data)groups.push({fields:kind==='weather'?shownWeather():state.data.current,source:state.data.source,retrieved:state.data.retrieved,error:state.error,classification:kind==='weather'&&$('forecast').value!=='current'?'Hourly forecast':'Current model estimate'});}
  if(!groups.length)throw Error('No API data is available to export');
  const blob=new Blob(['\uFEFF',csv(selectedCity()[0],groups)],{type:'text/csv;charset=utf-8'});
  const href=URL.createObjectURL(blob),a=document.createElement('a');a.href=href;a.download=`DHARA_Environment_${selectedCity()[0]}_${new Date().toISOString().replaceAll(':','-')}.csv`;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(href),60000);
  $('export-status').textContent='CSV generated and download requested. Check your browser downloads.';
 }catch(error){$('export-status').textContent=`Export failed: ${error.message}`;}
});
fetch('./release.json').then(r=>{if(!r.ok)throw Error('Release information unavailable');return r.json();}).then(release=>{
 const link=$('download');link.href=release.url;link.removeAttribute('aria-disabled');
 $('release').textContent=`${release.version} · ${(release.bytes/1048576).toFixed(1)} MiB · ${release.unityTests} Unity tests passed`;
 $('checksum').textContent=release.sha256;
}).catch(error=>{$('release').textContent=error.message;});
changeCity();setInterval(render,1000);
