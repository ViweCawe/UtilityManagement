export function validateNetwork(map) {
    if (!Array.isArray(map.nodes) || map.nodes.length < 1 || map.nodes.length > 500 || !Array.isArray(map.connections) || map.connections.length > 1500)
        throw new Error('Use 1–500 points and at most 1500 connections.');
    const ids = new Set(), meters = new Set(), pairs = new Set();
    for (const n of map.nodes) {
        if (!n || !/^[a-z0-9-]{1,80}$/.test(n.id) || ids.has(n.id) || !n.label?.trim() || n.label.length > 100 ||
            !['Lower', 'Top'].includes(n.station) || !['source', 'storage', 'distribution', 'usage', 'transfer', 'unresolved'].includes(n.kind) || (n.note?.length || 0) > 1000)
            throw new Error('Every point needs a unique identity, a name, a station and a role.');
        ids.add(n.id);
        if (n.meterId != null) {
            if (!n.canLinkMeter || !Number.isInteger(n.meterId) || n.meterId <= 0 || meters.has(n.meterId)) throw new Error('Assign each meter to only one metered point.');
            meters.add(n.meterId);
        }
    }
    for (const e of map.connections) {
        const pair = `${e.from}/${e.to}`;
        if (!ids.has(e.from) || !ids.has(e.to) || e.from === e.to || pairs.has(pair) ||
            !['supply', 'transfer', 'rain', 'backup'].includes(e.kind) || (e.condition?.length || 0) > 200 || (e.kind === 'backup' && !e.condition?.trim()))
            throw new Error('Choose different From and To points, avoid duplicate connections, and describe conditional routes.');
        pairs.add(pair);
    }
    for(const node of map.nodes.filter(n=>n.isExternalSupply===true))
        if(!node.canLinkMeter || !['source','storage'].includes(node.kind) || map.connections.some(e=>e.to===node.id && e.kind!=='backup'))
            throw new Error('An external supply must be a metered Source or Storage point without a normal upstream connection.');
    const visiting = new Set(), visited = new Set();
    function visit(id) {
        if (visited.has(id)) return;
        if (visiting.has(id)) throw new Error('These connections make a loop. Remove the connection that returns water to an earlier point.');
        visiting.add(id);
        for (const edge of map.connections.filter(e => e.from === id)) visit(edge.to);
        visiting.delete(id); visited.add(id);
    }
    for (const id of ids) visit(id);
}

export function arrangeNetwork(map) {
    validateNetwork(map);
    for (const station of ['Lower', 'Top']) {
        const nodes = map.nodes.filter(n => n.station === station);
        const ids = new Set(nodes.map(n => n.id));
        const edges = map.connections.filter(e => ids.has(e.from) && ids.has(e.to));
        const ranks = new Map();
        function rank(id) {
            if (ranks.has(id)) return ranks.get(id);
            const parents = edges.filter(e => e.to === id);
            const value = parents.length ? Math.max(...parents.map(e => rank(e.from))) + 1 : 0;
            ranks.set(id, value); return value;
        }
        const layers = new Map();
        for (const node of nodes) { const r = rank(node.id); if (!layers.has(r)) layers.set(r, []); layers.get(r).push(node); }
        let y = 0;
        for (const [, layer] of [...layers].sort((a, b) => a[0] - b[0])) {
            layer.forEach((node, i) => { node.x = (i % 5) * 240; node.y = y + Math.floor(i / 5) * 165; });
            y += Math.ceil(layer.length / 5) * 165;
        }
    }
    return map;
}

export function removePoint(map, id) {
    map.nodes = map.nodes.filter(n => n.id !== id);
    map.connections = map.connections.filter(e => e.from !== id && e.to !== id);
}
