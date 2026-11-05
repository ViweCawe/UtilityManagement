import { test } from 'node:test';
import assert from 'node:assert/strict';
import { arrangeNetwork, removePoint, validateNetwork } from '../UtilityManagerProjects/wwwroot/js/network-editor-model.mjs';
const point = (id, extra = {}) => ({ id, label: id, station: 'Lower', kind: 'usage', canLinkMeter: true, x: 0, y: 0, ...extra });
const route = (from, to, extra = {}) => ({ from, to, kind: 'supply', condition: '', ...extra });

test('users can add metered points without Figma identifiers and arrange them by supply', () => {
    const map = { nodes: [point('source', { kind: 'source', meterId: 4 }), point('tank', { kind: 'storage', meterId: 5 }), point('outlet')], connections: [route('source', 'tank'), route('tank', 'outlet')] };
    arrangeNetwork(map);
    assert.ok(map.nodes[0].y < map.nodes[1].y && map.nodes[1].y < map.nodes[2].y);
});
test('loops, missing points, duplicate routes and self connections are rejected', () => {
    for (const connections of [[route('a', 'b'), route('b', 'a')], [route('a', 'missing')], [route('a', 'b'), route('a', 'b')], [route('a', 'a')]])
        assert.throws(() => validateNetwork({ nodes: [point('a'), point('b')], connections }));
});
test('a meter cannot be assigned twice or assigned to an unmetered outlet', () => {
    assert.throws(() => validateNetwork({ nodes: [point('a', { meterId: 5 }), point('b', { meterId: 5 })], connections: [] }));
    assert.throws(() => validateNetwork({ nodes: [point('a', { canLinkMeter: false, meterId: 5 })], connections: [] }));
});
test('conditional routes need a condition', () => {
    assert.throws(() => validateNetwork({ nodes: [point('a'), point('b')], connections: [route('a', 'b', { kind: 'backup' })] }));
});
test('removing a point removes its incoming and outgoing routes without touching other meter assignments', () => {
    const map = { nodes: [point('a'), point('b'), point('c', { meterId: 9 })], connections: [route('a', 'b'), route('b', 'c'), route('a', 'c')] };
    removePoint(map, 'b');
    assert.deepEqual(map.connections, [route('a', 'c')]);
    assert.equal(map.nodes[1].meterId, 9);
});
test('cross-station transfer and disconnected points receive finite nonoverlapping positions', () => {
    const map = { nodes: [point('a'), point('b', { station: 'Top' }), ...Array.from({ length: 12 }, (_, i) => point(`outlet-${i}`))], connections: [route('a', 'b', { kind: 'transfer' })] };
    arrangeNetwork(map);
    const positions = new Set(map.nodes.map(n => `${n.station}/${n.x}/${n.y}`));
    assert.equal(positions.size, map.nodes.length);
    assert.ok(map.nodes.every(n => Number.isFinite(n.x) && Number.isFinite(n.y)));
});
test('an empty network is rejected, while a replacement source with a new identity is allowed', () => {
    assert.throws(() => validateNetwork({ nodes: [], connections: [] }));
    assert.doesNotThrow(() => arrangeNetwork({ nodes: [point('new-source', { kind: 'source' })], connections: [] }));
});

test('external supply is explicit and rejects internal or unmetered boundaries', () => {
    assert.doesNotThrow(() => validateNetwork({nodes: [point('inlet', {kind:'storage', isExternalSupply:true})], connections: []}));
    assert.throws(() => validateNetwork({nodes: [point('outlet', {isExternalSupply:true})], connections: []}));
    assert.throws(() => validateNetwork({nodes: [point('source', {kind:'source', canLinkMeter:false, isExternalSupply:true})], connections: []}));
    assert.throws(() => validateNetwork({nodes: [point('parent'), point('tank', {kind:'storage', isExternalSupply:true})], connections: [route('parent','tank')]}));
});

test('Main reporting boundary stays unique and cannot measure mixed sources',()=>{
 const main=point('m',{kind:'source',isExternalSupply:true,supplyOrigin:'municipal',reportingRole:'main'});
 const bore=point('b',{kind:'source',isExternalSupply:true,supplyOrigin:'borehole'});
 assert.doesNotThrow(()=>validateNetwork({nodes:[main,bore],connections:[]}));
 assert.throws(()=>validateNetwork({nodes:[main,point('second',{kind:'source',reportingRole:'main',supplyOrigin:'municipal'})],connections:[]}));
 assert.throws(()=>validateNetwork({nodes:[main,bore,point('mixed',{reportingRole:'borehole-outlet'})],connections:[route('m','mixed'),route('b','mixed')]}));
 assert.throws(()=>validateNetwork({nodes:[main,point('basin')],connections:[route('m','basin',{sourceFilter:'borehole'})]}));
});

const storageMap=()=>({nodes:[point('source',{kind:'source',meterId:1,storage:{openingDate:'2025-01-01T00:00:00',openingVolumeLitres:0,inletMeterIds:[1],outletMeterIds:[2],allFlowsConfirmed:true}}),point('tank',{kind:'storage',meterId:2,storage:{openingDate:'2025-01-01T00:00:00',openingVolumeLitres:0,inletMeterIds:[2],outletMeterIds:[3],allFlowsConfirmed:true}}),point('use',{meterId:3,measuresFinalUse:true})],connections:[route('source','tank'),route('tank','use')]});
test('a shared transfer debits its supplier and credits its receiving tank',()=>assert.doesNotThrow(()=>validateNetwork(storageMap())));
test('a boundary counter cannot credit two tanks or double as final use',()=>{
 const duplicate=storageMap();duplicate.nodes[0].storage.inletMeterIds=[2];assert.throws(()=>validateNetwork(duplicate));
 const transferUse=storageMap();transferUse.nodes[1].measuresFinalUse=true;assert.throws(()=>validateNetwork(transferUse));
});
test('paired transfers must follow the map and stock requires a measured date',()=>{
 const disconnected=storageMap();disconnected.connections=disconnected.connections.filter(e=>e.from!=='source');assert.throws(()=>validateNetwork(disconnected));
 const undated=storageMap();undated.nodes[0].storage.openingDate=null;assert.throws(()=>validateNetwork(undated));
});
