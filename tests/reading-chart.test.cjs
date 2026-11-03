const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const source=fs.readFileSync(path.join(__dirname,'../UtilityManagerProjects/Pages/Dashboard/Dashboard.cshtml'),'utf8');
function chart(factory,data) {
 const start=source.indexOf('function '+factory+'(');
 assert.ok(start>=0,'chart factory exists');
 const end=source.indexOf('\n        function ',start+1);
 const captured=[];
 const context={Chart:function(element,config){captured.push(config);},document:{getElementById:id=>({id})},numberFormatter:new Intl.NumberFormat('en')};
 vm.createContext(context);vm.runInContext(source.slice(start,end<0?undefined:end),context);
 context[factory]('canvas',data.map((_,i)=>String(i)),data,'Water','L','#1764ba','#e8f1fc');
 return captured[0];
}
test('mini chart shows a single valid value, including measured zero',()=>{
 for(const value of [100,0]) {
  const config=chart('createMiniLineChart',[null,value,null]);
  assert.ok(config.data.datasets[0].pointRadius>0);
  assert.equal(config.data.datasets[0].data[0],null);
 }
});
test('long trend range still shows one isolated valid point',()=>{
 const data=Array(90).fill(null);data[50]=100;
 const config=chart('createTrendChart',data);
 assert.ok(config.data.datasets[0].pointRadius>0);
 assert.equal(config.data.datasets[0].spanGaps,false);
});
test('empty graphs do not invent values or claim saved readings are absent',()=>{
 const config=chart('createTrendChart',[null,null]);
 assert.ok(config.data.datasets[0].data.every(v=>v===null));
 assert.match(config.options.plugins.noData.text,/No calculated/);
});
