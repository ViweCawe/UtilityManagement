import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { traceNetwork, traceEdge } from '../UtilityManagerProjects/wwwroot/js/water-network-graph.mjs';

const network = JSON.parse(readFileSync(new URL('../UtilityManagerProjects/App_Data/water-network.json', import.meta.url)));

test('map has valid, unique points, unique edges and documented source IDs', () => {
    const ids = new Set(network.nodes.map(n => n.id));
    assert.equal(ids.size, network.nodes.length);
    assert.equal(network.nodes.length, 33);
    assert.equal(network.connections.length, 32);
    const pairs = new Set();
    for (const node of network.nodes) assert.match(node.figmaNodeId, /^\d+:\d+$/);
    for (const edge of network.connections) {
        assert.ok(ids.has(edge.from) && ids.has(edge.to));
        assert.notEqual(edge.from, edge.to);
        assert.ok(!pairs.has(`${edge.from}/${edge.to}`));
        pairs.add(`${edge.from}/${edge.to}`);
        assert.ok(edge.figmaConnectorId);
    }
});

test('municipal route reaches both stations without absorbing borehole or rainwater', () => {
    const trace = traceNetwork(network, 'main-tank');
    assert.deepEqual([...trace.upstream], []);
    for (const id of ['garage-roof', 'cable-car-transfer', 'docking-bay', 'reservoir', 'dosing', 'header', 'male-female-toilets', 'vida-cafe'])
        assert.ok(trace.downstream.has(id), id);
    for (const id of ['rainwater', 'borehole-in', 'fire-hydrant'])
        assert.ok(!trace.downstream.has(id), id);
});

test('reservoir recognises municipal transfer and additional rainwater input', () => {
    const { upstream } = traceNetwork(network, 'reservoir');
    for (const id of ['rainwater', 'docking-bay', 'cable-car-transfer', 'main-tank']) assert.ok(upstream.has(id));
});

test('Level 1 toilets have normal municipal basins and borehole flushing, with municipal backup noted',()=>{
 const regular=traceNetwork(network,'staff-toilets-lower');
 for(const id of ['main-tank','borehole-in','borehole-out']) assert.ok(regular.upstream.has(id));
 const main=network.connections.find(e=>e.from==='main-tank'&&e.to==='staff-toilets-lower');
 assert.equal(main.kind,'supply');assert.match(main.purpose,/Basins/);assert.match(main.condition,/flushing only when borehole/);
 assert.ok(!regular.upstream.has('garage-roof'));
});

test('unresolved points never acquire inferred physical feeds', () => {
    for (const id of ['fire-hydrant']) {
        const trace = traceNetwork(network, id, true);
        assert.equal(trace.upstream.size + trace.downstream.size, 0);
    }
});

test('highlighting rain route does not highlight municipal input into the same reservoir', () => {
    const trace = traceNetwork(network, 'rainwater');
    const incoming = network.connections.find(e => e.from === 'docking-bay' && e.to === 'reservoir');
    assert.equal(traceEdge(incoming, 'rainwater', trace), false);
    assert.ok(trace.downstream.has('male-female-toilets'));
});

test('traversal terminates even if later data introduces a cycle', () => {
    const cyclic = { connections: [{ from: 'a', to: 'b' }, { from: 'b', to: 'a' }] };
    const trace = traceNetwork(cyclic, 'a');
    assert.deepEqual([...trace.upstream], ['b']);
    assert.deepEqual([...trace.downstream], ['b']);
});

test('updated workshop records separate unmetered outlets and flushing-only borehole supply', () => {
    for (const id of ['workshop-basins', 'workshop-fountains', 'level4-toilets']) {
        assert.equal(network.nodes.find(n => n.id === id).canLinkMeter, false);
        assert.ok(network.connections.some(e => e.from === 'workshop' && e.to === id));
    }
    assert.equal(network.connections.find(e => e.from === 'borehole-out' && e.to === 'workshop').condition, 'Toilet flushing only');
    assert.equal(network.nodes.find(n => n.id === 'fire-hydrant').kind, 'source');
    assert.ok(!network.connections.some(e => e.from === 'main-tank' && e.to === 'fire-hydrant'));
});

test('borehole flushing does not reach municipal-only workshop basins or fountains',()=>{
 const bore=traceNetwork(network,'borehole-out');assert.ok(bore.downstream.has('workshop'));
 for(const id of ['workshop-basins','workshop-fountains']) assert.ok(!bore.downstream.has(id));
 const basins=traceNetwork(network,'workshop-basins');assert.ok(basins.upstream.has('main-tank'));assert.ok(!basins.upstream.has('borehole-out'));
});
