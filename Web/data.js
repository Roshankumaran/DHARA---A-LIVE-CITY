export const cities=[['Bengaluru',12.97194,77.59369],['Chennai',13.08784,80.27847],['Mumbai',19.07283,72.88261],['Delhi',28.62137,77.2148],['Hyderabad',17.38405,78.45636]];
export const weatherFields=[
 ['temperature_2m','Temperature','°C',-100,100],['relative_humidity_2m','Relative humidity','%',0,100],
 ['rain','Rain rate','mm',0,Infinity],['precipitation','Precipitation total','mm',0,Infinity],
 ['wind_speed_10m','Wind speed','km/h',0,Infinity],['wind_direction_10m','Wind direction FROM','°',0,360],
 ['cloud_cover','Cloud cover','%',0,100],['shortwave_radiation','Shortwave radiation','W/m²',0,Infinity],['weather_code','Weather code','wmo code',0,99]
];
export const airFields=[['us_aqi','US AQI','USAQI'],['european_aqi','European AQI','EAQI'],['pm2_5','PM2.5','μg/m³'],['pm10','PM10','μg/m³'],['nitrogen_dioxide','Nitrogen dioxide','μg/m³'],['ozone','Ozone','μg/m³'],['sulphur_dioxide','Sulphur dioxide','μg/m³'],['carbon_monoxide','Carbon monoxide','μg/m³']];
const codes={0:'Clear sky',1:'Mainly clear',2:'Partly cloudy',3:'Overcast',45:'Fog',48:'Depositing rime fog',51:'Light drizzle',53:'Moderate drizzle',55:'Dense drizzle',56:'Light freezing drizzle',57:'Dense freezing drizzle',61:'Slight rain',63:'Moderate rain',65:'Heavy rain',66:'Light freezing rain',67:'Heavy freezing rain',71:'Slight snowfall',73:'Moderate snowfall',75:'Heavy snowfall',77:'Snow grains',80:'Slight rain showers',81:'Moderate rain showers',82:'Violent rain showers',85:'Slight snow showers',86:'Heavy snow showers',95:'Thunderstorm',96:'Thunderstorm with slight hail',99:'Thunderstorm with heavy hail'};
export const condition=code=>codes[code]??'Unavailable';
export function validTime(time){
 if(typeof time!=='string'||!/^\d{4}-\d\d-\d\dT\d\d:\d\d$/.test(time))throw Error('Invalid API timestamp');
 const ms=Date.parse(time+'+05:30');
 if(!Number.isFinite(ms)||new Date(ms+19800000).toISOString().slice(0,16)!==time)throw Error('Invalid API date');
 return ms;
}
function normalized(unit){return typeof unit==='string'?unit.replaceAll('µ','μ'):unit;}
function row(data,units,fields,interval){
 if(!data||!units)throw Error('Missing API data or units');
 validTime(data.time);
 return fields.map(([key,name,unit,min=0,max=Infinity])=>{
  let value=data[key];
  if(value===null||value===undefined)value=null;
  else{
   if(normalized(units[key])!==normalized(unit))throw Error(`Unexpected unit for ${key}`);
   if(typeof value!=='number'||!Number.isFinite(value)||value<min||value>max)throw Error(`Invalid ${key}`);
   if(key==='weather_code'&&!Object.hasOwn(codes,value))throw Error('Unknown WMO weather code');
   if(key==='rain')value=value*3600/interval;
  }
  return {key,name,value,unit:key==='rain'?'mm/h':unit==='wmo code'?'WMO':unit,time:data.time,interval};
 });
}
export function parseResponse(body,kind,now=Date.now()){
 if(!body||body.error===true)throw Error(body?.reason||'API returned an error');
 if(body.utc_offset_seconds!==19800)throw Error('Expected IST offset (+05:30)');
 if(body.current_units?.time!=='iso8601')throw Error('Expected ISO timestamps');
 const stamp=validTime(body.current?.time);
 if(now-stamp>(kind==='weather'?2:3)*3600000||stamp-now>300000)throw Error('API valid time is outdated or in the future');
 const interval=body.current.interval;
 if(body.current_units.interval!=='seconds'||!Number.isFinite(interval)||interval<=0||interval>3600)throw Error('Invalid aggregation interval');
 const current=row(body.current,body.current_units,kind==='weather'?weatherFields:airFields,interval);
 const forecast=[];
 if(kind==='weather'&&body.hourly){
  const h=body.hourly,units=body.hourly_units;
  if(!Array.isArray(h.time)||!h.time.length||units?.time!=='iso8601')throw Error('Missing forecast times');
  for(const [key] of weatherFields)if(h[key]!==undefined&&(!Array.isArray(h[key])||h[key].length!==h.time.length))throw Error(`Misaligned forecast ${key}`);
  let previous=-Infinity;
  h.time.forEach((time,i)=>{
   const timeMs=validTime(time);if(timeMs<=previous)throw Error('Unordered forecast timestamps');previous=timeMs;
   const values={time};for(const [key] of weatherFields)values[key]=h[key]?.[i];
   const fields=row(values,units,weatherFields,3600);
   if(timeMs>=stamp)forecast.push({time,fields});
  });
 }
 return {time:body.current.time,current,forecast,retrieved:now,source:kind==='weather'?'Open-Meteo Forecast API':'Open-Meteo Air Quality API / CAMS global'};
}
export function url(city,kind){
 const [,latitude,longitude]=city;const params=new URLSearchParams({latitude,longitude,timezone:'Asia/Kolkata'});
 if(kind==='weather'){
  const fields=weatherFields.map(f=>f[0]).join(',');Object.entries({current:fields,hourly:fields,wind_speed_unit:'kmh',temperature_unit:'celsius',precipitation_unit:'mm',forecast_days:'2'}).forEach(([k,v])=>params.set(k,v));
 }else{params.set('current',airFields.map(f=>f[0]).join(','));params.set('domains','cams_global');}
 return `https://${kind==='weather'?'api.open-meteo.com/v1/forecast':'air-quality-api.open-meteo.com/v1/air-quality'}?${params}`;
}
export function retryDelay(header,failures,now=Date.now()){
 const backoff=Math.min(300000,15000*2**Math.min(Math.max(0,failures-1),5));
 let server=0;
 if(header&&/^\d+$/.test(header))server=Number(header)*1000;
 else if(header&&Number.isFinite(Date.parse(header)))server=Date.parse(header)-now;
 return Math.max(backoff,Math.min(86400000,server));
}
export function csv(city,groups,now=Date.now()){
 const quote=v=>'"'+String(v??'Unavailable').replaceAll('"','""')+'"';
 const rows=[['city','field','value','unit','source','valid_time_IST','retrieved_UTC','availability','classification','aggregation_seconds']];
 for(const group of groups)for(const field of group.fields){
  rows.push([city,field.name,field.value,field.unit,group.source,field.time,group.retrieved?new Date(group.retrieved).toISOString():null,field.value===null?'Unavailable':now-group.retrieved>600000?'Stale':group.error?'Retained after failure':'Available',group.classification,field.interval]);
 }
 return rows.map(r=>r.map(quote).join(',')).join('\r\n')+'\r\n';
}
