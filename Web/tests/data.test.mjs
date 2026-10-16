import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {cities,parseResponse,validTime,retryDelay,csv,url} from '../data.js';
const read=p=>JSON.parse(readFileSync(new URL(p,import.meta.url),'utf8').replace(/^\uFEFF/,''));
const weather=read('fixtures/Weather/Bengaluru.json');
const air=read('fixtures/AirQuality/Bengaluru.json');
const now=validTime(weather.current.time)+60000;
for(const city of cities){
 test(`${city[0]} actual saved weather schema`,()=>{const raw=read(`fixtures/Weather/${city[0]}.json`);const data=parseResponse(raw,'weather',validTime(raw.current.time)+60000);assert.equal(data.current.length,9);assert.ok(data.forecast.length>24);assert.equal(data.current.find(f=>f.key==='temperature_2m').value,raw.current.temperature_2m);});
 test(`${city[0]} actual saved AQ schema`,()=>{const raw=read(`fixtures/AirQuality/${city[0]}.json`);const data=parseResponse(raw,'air',validTime(raw.current.time)+60000);assert.equal(data.current.length,8);assert.equal(data.current.find(f=>f.key==='carbon_monoxide').value,raw.current.carbon_monoxide);});
}
test('interval conversion fixture and null remain distinct',()=>{const raw=structuredClone(weather);raw.current.rain=2;raw.current.interval=900;raw.current.relative_humidity_2m=null;const d=parseResponse(raw,'weather',now);assert.equal(d.current.find(f=>f.key==='rain').value,8);assert.equal(d.current.find(f=>f.key==='relative_humidity_2m').value,null);});
for(const [name,change] of [
 ['wrong unit',r=>r.current_units.temperature_2m='°F'],['wrong timezone',r=>r.utc_offset_seconds=0],['invalid humidity',r=>r.current.relative_humidity_2m=101],['invalid WMO code',r=>r.current.weather_code=62],['misaligned array',r=>r.hourly.rain.pop()],['unordered times',r=>r.hourly.time[1]=r.hourly.time[0]],['string numeric value',r=>r.current.rain='2'],['invalid interval',r=>r.current.interval=0],['error body',r=>r.error=true]
])test(`reject ${name}`,()=>{const raw=structuredClone(weather);change(raw);assert.throws(()=>parseResponse(raw,'weather',now));});
test('reject stale and future data',()=>{assert.throws(()=>parseResponse(weather,'weather',now+3*3600000));assert.throws(()=>parseResponse(weather,'weather',now-600000));});
test('reject impossible calendar dates',()=>assert.throws(()=>validTime('2026-02-30T12:00')));
test('CAMS AQI scales are validated independently',()=>{const raw=structuredClone(air);raw.current_units.us_aqi='EAQI';assert.throws(()=>parseResponse(raw,'air',validTime(raw.current.time)));});
test('retry seconds, dates and caps',()=>{assert.equal(retryDelay('120',1,now),120000);assert.equal(retryDelay(new Date(now+120000).toUTCString(),1,now),120000);assert.equal(retryDelay(null,10,now),300000);assert.equal(retryDelay('999999',1,now),86400000);});
test('CSV uses source timestamps and escapes special characters',()=>{const d=parseResponse(weather,'weather',now);const text=csv('City, "A"',[{fields:d.current,source:d.source,retrieved:now,classification:'Current model estimate'}],now);assert.ok(text.includes('"City, ""A"""'));assert.ok(text.includes(weather.current.time));assert.equal(text.split('\r\n').length,11);});
test('CSV empty dataset still has header',()=>assert.equal(csv('Bengaluru',[]).split('\r\n').length,2));
test('URLs request the correct endpoints and all fields',()=>{assert.ok(url(cities[0],'weather').includes('api.open-meteo.com/v1/forecast'));assert.ok(url(cities[0],'air').includes('air-quality-api.open-meteo.com'));assert.ok(url(cities[0],'air').includes('carbon_monoxide'));});
