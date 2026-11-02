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
