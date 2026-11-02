import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { traceNetwork, traceEdge } from '../UtilityManagerProjects/wwwroot/js/water-network-graph.mjs';

const network = JSON.parse(readFileSync(new URL('../UtilityManagerProjects/App_Data/water-network.json', import.meta.url)));

test('map has valid, unique points, unique edges and documented source IDs', () => {
    const ids = new Set(network.nodes.map(n => n.id));
    assert.equal(ids.size, network.nodes.length);
    assert.equal(network.nodes.length, 29);
    assert.equal(network.connections.length, 27);
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
    for (const id of ['rainwater', 'borehole-in', 'fire-hydrant', 'staff-toilets-lower'])
        assert.ok(!trace.downstream.has(id), id);
});

test('reservoir recognises municipal transfer and additional rainwater input', () => {
    const { upstream } = traceNetwork(network, 'reservoir');
    for (const id of ['rainwater', 'docking-bay', 'cable-car-transfer', 'main-tank']) assert.ok(upstream.has(id));
});

test('staff-toilet fallback is included only when explicitly requested', () => {
    const regular = traceNetwork(network, 'staff-toilets-lower');
    assert.deepEqual(new Set(regular.upstream), new Set(['borehole-in', 'borehole-out']));
    const alternative = traceNetwork(network, 'staff-toilets-lower', true);
    for (const id of ['garage-roof', 'main-tank', 'borehole-in', 'borehole-out']) assert.ok(alternative.upstream.has(id));
    const backup = network.connections.find(e => e.kind === 'backup');
    assert.equal(backup.condition, 'Borehole supply is closed');
    assert.equal(traceEdge(backup, 'staff-toilets-lower', regular), false);
    assert.equal(traceEdge(backup, 'staff-toilets-lower', alternative, true), true);
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
