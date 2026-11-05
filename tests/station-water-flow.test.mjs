import{test}from'node:test';import assert from'node:assert/strict';
import{dailyFlowSegments,stationNetworkLayout}from'../UtilityManagerProjects/wwwroot/js/station-water-flow-model.mjs';
test('daily curves preserve measured zeros and leave unknown intervals disconnected',()=>{
 assert.deepEqual(dailyFlowSegments([null,0,10,null,20,30,null]),[[{index:1,value:0},{index:2,value:10}],[{index:4,value:20},{index:5,value:30}]]);
 assert.deepEqual(dailyFlowSegments([null,null]),[]);assert.deepEqual(dailyFlowSegments([0]),[[{index:0,value:0}]]);
});
test('both station coordinate spaces remain distinct, and either station can be viewed alone',()=>{
 const map={nodes:[{id:'main',station:'Lower',x:0,y:100},{id:'end',station:'Lower',x:200,y:800},{id:'dock',station:'Top',x:0,y:100},{id:'header',station:'Top',x:0,y:500}]};
 const all=stationNetworkLayout(map);assert.equal(all.nodes.length,4);assert.ok(all.offsets.Top>800+120);assert.equal(all.labels.length,2);assert.ok(all.height>all.offsets.Top+500+120);
 const top=stationNetworkLayout(map,'Top');assert.equal(top.nodes.length,2);assert.equal(top.offsets.Top,0);assert.equal(top.labels[0].text,'Top Station');
});
