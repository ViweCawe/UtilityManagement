import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {nodeOrigins,flowSections} from '../UtilityManagerProjects/wwwroot/js/water-network-accounting.mjs';
import {validateNetwork} from '../UtilityManagerProjects/wwwroot/js/network-editor-model.mjs';
const map=JSON.parse(fs.readFileSync(new URL('../UtilityManagerProjects/App_Data/water-network.json',import.meta.url)));
test('combined counters remain mixed but clean-water-only outlets remain municipal',()=>{
 assert.deepEqual(nodeOrigins(map,'workshop'),new Set(['municipal','borehole']));
 assert.deepEqual(nodeOrigins(map,'staff-toilets-lower'),new Set(['municipal','borehole']));
 assert.deepEqual(nodeOrigins(map,'workshop-basins'),new Set(['municipal']));
 assert.deepEqual(nodeOrigins(map,'borehole-out'),new Set(['borehole']));
 validateNetwork(map);
});
test('flow sections keep sources upstream and keep hydrant separate from Main',()=>{
 const groups=flowSections(map),municipal=groups.find(g=>g.id==='municipal').nodes.map(x=>x.node.id);
 assert.ok(municipal.indexOf('main-tank')<municipal.indexOf('garage-roof'));
 assert.ok(!municipal.includes('fire-hydrant'));
 assert.ok(groups.find(g=>g.id==='hydrant').nodes.some(x=>x.node.id==='fire-hydrant'));
 assert.ok(groups.find(g=>g.id==='mixed').nodes.some(x=>x.node.id==='workshop'));
});
