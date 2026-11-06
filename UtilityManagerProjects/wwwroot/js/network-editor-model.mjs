import { reportingRole, nodeOrigins, counterBoundary } from './water-network-accounting.mjs';
export function validateNetwork(map) {
    if (!Array.isArray(map.nodes) || map.nodes.length < 1 || map.nodes.length > 500 || !Array.isArray(map.connections) || map.connections.length > 1500)
        throw new Error('Use 1–500 points and at most 1500 connections.');
    const ids = new Set(), meters = new Set(), pairs = new Set();
    for (const n of map.nodes) {
        if (!n || !/^[a-z0-9-]{1,80}$/.test(n.id) || ids.has(n.id) || !n.label?.trim() || n.label.length > 100 ||
            !['outlet','inlet','unconfirmed'].includes(counterBoundary(n)) || !['Lower', 'Top'].includes(n.station) || !['source', 'storage', 'distribution', 'usage', 'transfer', 'unresolved'].includes(n.kind) || (n.note?.length || 0) > 1000 || (n.usageArea?.length||0)>100 || !['','main','municipal-inlet','borehole-inlet','borehole-outlet','hydrant'].includes(reportingRole(n)) || !['','municipal','borehole','rain','other'].includes(n.supplyOrigin||''))
            throw new Error('Every point needs a unique identity, a name, a station and a role.');
        ids.add(n.id);
        if (n.meterId != null) {
            if (n.canLinkMeter===false || !Number.isInteger(n.meterId) || n.meterId <= 0 || meters.has(n.meterId)) throw new Error('Assign each meter to only one metered point.');
            meters.add(n.meterId);
        }
    }
    for (const e of map.connections) {
        const pair = `${e.from}/${e.to}`;
        if (!ids.has(e.from) || !ids.has(e.to) || e.from === e.to || pairs.has(pair) ||
            !['supply', 'transfer', 'rain', 'backup'].includes(e.kind) || (e.condition?.length || 0) > 200 || (e.purpose?.length||0)>120 || !['','municipal','borehole','rain','other'].includes(e.sourceFilter||'') || (e.kind === 'backup' && !e.condition?.trim()))
            throw new Error('Choose different From and To points, avoid duplicate connections, and describe conditional routes.');
        pairs.add(pair);
    }
    for(const node of map.nodes.filter(n=>n.isExternalSupply===true))
        if(node.canLinkMeter===false || !['source','storage'].includes(node.kind) || map.connections.some(e=>e.to===node.id && e.kind!=='backup'))
            throw new Error('An external supply must be a metered Source or Storage point without a normal upstream connection.');
    const credits=new Map(),debits=new Set();
    for(const n of map.nodes.filter(n=>n.storage)) {
        const b=n.storage,ins=b.inletMeterIds,outs=b.outletMeterIds;
        if(!['source','storage'].includes(n.kind)||!Array.isArray(ins)||!Array.isArray(outs)||ins.length>500||outs.length>500||[...ins,...outs].some(id=>!Number.isInteger(id)||id<=0)||ins.some(id=>outs.includes(id)))throw new Error('Storage needs distinct inlet and outlet counters on a physical source/storage point.');
        if((b.openingDate!=null)!==(b.openingVolumeLitres!=null)||b.openingVolumeLitres!=null&&(!Number.isFinite(b.openingVolumeLitres)||b.openingVolumeLitres<0))throw new Error('Enter both a measured opening volume and its date. Do not use the cumulative counter as stock.');
        if(b.openingDate!=null){const date=new Date(b.openingDate);if(!Number.isFinite(date.getTime())||date.getFullYear()<1753||date.setHours(0,0,0,0)>new Date().setHours(0,0,0,0))throw new Error('Choose a valid opening stock date no later than today.');}
        if(counterBoundary(n)==='outlet'&&ins.includes(n.meterId))throw new Error('This tank counter measures an outlet. Its counter cannot also be its tank inlet.');
        for(const id of ins){if(credits.has(id))throw new Error('A transfer counter can credit only one receiving tank.');credits.set(id,n.id);}
        for(const id of outs){if(debits.has(id))throw new Error('A transfer counter can debit only one supplying tank.');debits.add(id);}
    }
    for(const n of map.nodes.filter(n=>n.measuresFinalUse)) {
        if(n.canLinkMeter===false||['main','municipal-inlet','borehole-inlet','borehole-outlet'].includes(reportingRole(n))||credits.has(n.meterId))throw new Error('A supply/transfer credited to storage cannot also count as final use.');
        const queue=map.connections.filter(e=>e.from===n.id).map(e=>e.to),seen=new Set();
        while(queue.length){const id=queue.shift();if(seen.has(id))continue;seen.add(id);const child=map.nodes.find(p=>p.id===id);if(child.canLinkMeter!==false||child.kind==='storage')throw new Error('A counter feeding another meter/tank is a transfer, not final consumption.');queue.push(...map.connections.filter(e=>e.from===id).map(e=>e.to));}
    }
    for(const n of map.nodes.filter(n=>n.storage))for(const id of n.storage.outletMeterIds){const to=credits.get(id);if(!to)continue;const queue=map.connections.filter(e=>e.from===n.id).map(e=>e.to),seen=new Set();while(queue.length){const p=queue.shift();if(seen.has(p))continue;seen.add(p);queue.push(...map.connections.filter(e=>e.from===p).map(e=>e.to));}if(!seen.has(to))throw new Error('A paired tank transfer must follow a route on the map.');}
    const visiting = new Set(), visited = new Set();
    function visit(id) {
        if (visited.has(id)) return;
        if (visiting.has(id)) throw new Error('These connections make a loop. Remove the connection that returns water to an earlier point.');
        visiting.add(id);
        for (const edge of map.connections.filter(e => e.from === id)) visit(edge.to);
        visiting.delete(id); visited.add(id);
    }
    for (const id of ids) visit(id);
    for(const e of map.connections) if(e.sourceFilter&&!nodeOrigins(map,e.from).has(e.sourceFilter)) throw new Error('This supplier does not carry the chosen water source. Check the source type and route restriction.');
    const roles=new Set();
    for(const n of map.nodes) {
        const role=reportingRole(n);if(!role) continue;
        if(n.canLinkMeter===false||roles.has(role)) throw new Error('Choose one distinct metered point for each reporting boundary.');roles.add(role);
        if(['municipal-inlet','hydrant','borehole-inlet'].includes(role)&&map.connections.some(e=>e.to===n.id&&e.kind!=='backup')) throw new Error('An inlet reporting meter must not have a normal upstream supplier.');
        const expected=['main','municipal-inlet','hydrant'].includes(role)?'municipal':'borehole',sources=nodeOrigins(map,n.id);
        if(sources.size!==1||!sources.has(expected)) throw new Error('This reporting meter measures the wrong or mixed sources. A combined counter cannot be a municipal or borehole total.');
    }

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
