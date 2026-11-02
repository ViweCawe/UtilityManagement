import { traceNetwork, traceEdge } from './water-network-graph.mjs';

const payload = document.getElementById('water-network-data');
if (payload) {
    const network = JSON.parse(payload.textContent);
    const readings = JSON.parse(document.getElementById('water-network-readings')?.textContent || '{}');
    const formatReading = value => new Intl.NumberFormat().format(value);
    const nodes = new Map(network.nodes.map(n => [n.id, n]));
    const station = document.getElementById('network-station');
    const point = document.getElementById('network-point');
    const backup = document.getElementById('network-backup');
    const diagram = document.getElementById('network-diagram');
    const details = document.getElementById('selected-point-routes');
    const namespace = 'http://www.w3.org/2000/svg';
    const roleLabels = { source: 'Source', storage: 'Storage tank', distribution: 'Distribution', usage: 'Water use', transfer: 'Transfer', unresolved: 'To confirm' };
    let selectedId = 'main-tank';

    function svg(type, attributes = {}, text) {
        const element = document.createElementNS(namespace, type);
        for (const [key, value] of Object.entries(attributes)) element.setAttribute(key, value);
        if (text !== undefined) element.textContent = text;
        return element;
    }

    function select(id) {
        selectedId = id;
        const node = nodes.get(id);
        if (station.value !== node.station) {
            station.value = node.station;
            fillPoints();
        }
        point.value = id;
        render();
    }

    function fillPoints() {
        point.replaceChildren();
        for (const node of network.nodes.filter(n => n.station === station.value)) {
            const option = document.createElement('option');
            option.value = node.id;
            option.textContent = node.label;
            point.append(option);
        }
    }

    function routeList(title, ids, condition) {
        const heading = document.createElement('h4');
        heading.textContent = title;
        details.append(heading);
        const list = document.createElement('ul');
        if (!ids.length) {
            const item = document.createElement('li');
            item.textContent = condition || 'None shown on the map.';
            list.append(item);
        }
        for (const id of ids) {
            const node = nodes.get(id);
            const item = document.createElement('li');
            const button = document.createElement('button');
            button.type = 'button';
            button.textContent = `Show ${node.label} (${node.station} Station)`;
            button.addEventListener('click', () => select(id));
            item.append(button);
            list.append(item);
        }
        details.append(list);
    }

    function render() {
        const selected = nodes.get(selectedId);
        const trace = traceNetwork(network, selectedId, backup.checked);
        const visible = network.nodes.filter(n => n.station === station.value);
        const active = new Set([selectedId, ...trace.upstream, ...trace.downstream]);
        const height = Math.max(...visible.map(n => n.y)) + 165;
        diagram.setAttribute('viewBox', `-20 -25 1310 ${height + 25}`);
        diagram.style.aspectRatio = `1310 / ${height + 25}`;
        diagram.replaceChildren();
        const defs = svg('defs');
        for (const [id, color] of [['flow-arrow', '#94a3b8'], ['active-arrow', '#2563eb']]) {
            const marker = svg('marker', { id, viewBox: '0 0 10 10', refX: 9, refY: 5,
                markerWidth: 7, markerHeight: 7, orient: 'auto-start-reverse' });
            marker.append(svg('path', { d: 'M 0 0 L 10 5 L 0 10 z', fill: color }));
            defs.append(marker);
        }
        diagram.append(defs);
        for (const edge of network.connections) {
            const from = nodes.get(edge.from), to = nodes.get(edge.to);
            if (from.station !== station.value || to.station !== station.value) continue;
            const highlighted = traceEdge(edge, selectedId, trace, backup.checked);
            let x1 = from.x + 90, y1 = from.y + 120, x2 = to.x + 90, y2 = to.y;
            let d;
            if (from.y === to.y) {
                const direction = to.x > from.x ? 1 : -1;
                x1 = from.x + (direction === 1 ? 180 : 0); y1 = from.y + 40;
                x2 = to.x + (direction === 1 ? 0 : 180); y2 = to.y + 40;
                d = `M ${x1} ${y1} L ${x2} ${y2}`;
            } else {
                const mid = y1 + (y2 - y1) / 2;
                d = `M ${x1} ${y1} C ${x1} ${mid}, ${x2} ${mid}, ${x2} ${y2}`;
            }
            const path = svg('path', { d, fill: 'none', stroke: highlighted ? '#2563eb' : '#94a3b8',
                'stroke-width': highlighted ? 3 : 2, 'marker-end': `url(#${highlighted ? 'active-arrow' : 'flow-arrow'})`,
                ...(edge.kind === 'backup' ? { 'stroke-dasharray': '8 6' } : {}) });
            path.append(svg('title', {}, `${from.label} → ${to.label}${edge.condition ? ': ' + edge.condition : ''}`));
            diagram.append(path);
        }
        for (const node of visible) {
            const group = svg('g', { transform: `translate(${node.x},${node.y})`, tabindex: 0,
                role: 'button', 'aria-label': `${node.label}, ${node.kind}${node.id === selectedId ? ', selected' : ''}`,
                'aria-pressed': node.id === selectedId ? 'true' : 'false',
                class: `wn-node ${node.id === selectedId ? 'selected' : ''} ${active.has(node.id) ? 'traced' : ''} ${node.kind}` });
            group.append(svg('rect', { width: 180, height: 120, rx: 12 }));
            const words = node.label.split(' '), lines = [''];
            for (const word of words) {
                if ((lines[lines.length - 1] + word).length > 23) lines.push('');
                lines[lines.length - 1] += (lines[lines.length - 1] ? ' ' : '') + word;
            }
            const text = svg('text', { x: 90, y: 34 - (lines.length - 1) * 7, 'text-anchor': 'middle' });
            lines.forEach((line, index) => text.append(svg('tspan', { x: 90, dy: index ? 16 : 0 }, line)));
            group.append(text, svg('text', { x: 90, y: 68, 'text-anchor': 'middle', class: 'wn-node-kind' }, roleLabels[node.kind].toUpperCase()));
            const measurement = readings[node.id];
            const valueLabel = measurement ? (measurement.currentReading == null ? 'No readings yet' : `${formatReading(measurement.currentReading)} ${measurement.unit}`) : (node.canLinkMeter === false ? 'No separate meter' : 'Assign a meter');
            group.append(svg('text', { x: 90, y: 90, 'text-anchor': 'middle', class: 'wn-reading-value' }, valueLabel));
            group.append(svg('text', { x: 90, y: 108, 'text-anchor': 'middle', class: 'wn-reading-date' }, measurement?.capturedAt || ''));
            group.addEventListener('click', () => select(node.id));
            group.addEventListener('keydown', event => {
                if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); select(node.id); }
            });
            diagram.append(group);
        }
        document.getElementById('selected-point-kind').textContent = `${selected.station} Station / ${roleLabels[selected.kind]}`;
        document.getElementById('selected-point-title').textContent = selected.label;
        document.getElementById('selected-point-note').textContent = selected.note || 'Route shown on the source map. Meter identity is not yet verified.';
        details.replaceChildren();
        const measurement = readings[selectedId];
        const readout = document.createElement('p');
        readout.className = 'wn-reading-panel';
        readout.textContent = measurement ? `${measurement.name} (#${measurement.meterId}) · Latest counter: ${measurement.currentReading == null ? 'No readings yet' : formatReading(measurement.currentReading) + ' ' + measurement.unit} · Captured: ${measurement.capturedAt || '—'}` : (selected.canLinkMeter === false ? 'No separate meter at this point.' : 'Assign the actual meter to show its saved readings.');
        details.append(readout);
        if (measurement?.historyUrl) {
            const history = document.createElement('a'); history.href = measurement.historyUrl;
            history.textContent = 'View this meter’s readings'; details.append(history);
        }
        const supplies = network.connections.filter(e => e.to === selectedId && e.kind !== 'backup');
        const destinations = network.connections.filter(e => e.from === selectedId && e.kind !== 'backup');
        routeList('Water comes from', supplies.map(e => e.from), selected.kind === 'source' ? 'Source point.' : undefined);
        routeList('Water goes to', destinations.map(e => e.to));
        for (const edge of network.connections.filter(e => e.kind === 'backup' && (e.to === selectedId || e.from === selectedId))) {
            routeList(`Backup only: ${edge.condition}`, [edge.to === selectedId ? edge.from : edge.to]);
        }
        const explanation = document.createElement('p');
        explanation.className = 'wn-trace-summary';
        explanation.textContent = `Highlighted route: ${trace.upstream.size} connected points before this point and ${trace.downstream.size} after it. Use “Show” to follow a connection on the map.`;
        details.append(explanation);
    }

    document.getElementById('network-reset').addEventListener('click', () => select('main-tank'));
    station.addEventListener('change', () => {
        fillPoints();
        select(station.value === 'Lower' ? 'main-tank' : 'header');
    });
    point.addEventListener('change', () => select(point.value));
    backup.addEventListener('change', render);
    fillPoints();
    select(selectedId);
    document.getElementById('network-controls').hidden = false;
    document.getElementById('network-explorer').hidden = false;
}
