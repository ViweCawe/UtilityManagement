import { arrangeNetwork, removePoint } from './network-editor-model.mjs';

const form = document.getElementById('network-edit-form');
const hidden = document.getElementById('NetworkJson');
const error = document.getElementById('network-editor-error');
try {
    const map = JSON.parse(hidden.value);
    const meters = JSON.parse(document.getElementById('network-editor-meters').textContent);
    const roles = { source: 'Source', storage: 'Storage tank', distribution: 'Distribution', usage: 'Water use', transfer: 'Transfer', unresolved: 'Needs confirmation' };
    const routeKinds = { supply: 'Normal supply', transfer: 'Transfer', rain: 'Rainwater', backup: 'Conditional supply' };
    function el(tag, text, className) {
        const n = document.createElement(tag); if (text) n.textContent = text; if (className) n.className = className; return n;
    }
    function button(text, callback) { const b = el('button', text); b.type = 'button'; b.addEventListener('click', callback); return b; }
    function field(title, control) { const label = el('label', title); label.append(control); return label; }
    function input(value, change, max = 100) {
        const n = document.createElement('input'); n.value = value || ''; n.maxLength = max;
        n.addEventListener('input', () => change(n.value)); return n;
    }
    function select(options, value, change) {
        const n = document.createElement('select');
        for (const [v, text] of options) { const o = el('option', text); o.value = v; n.append(o); }
        if (value && ![...n.options].some(o => o.value === String(value))) { const o = el('option', `Unavailable meter #${value} — choose another or unlink`); o.value = value; n.append(o); }
        n.value = value == null ? '' : String(value); n.addEventListener('change', () => change(n.value)); return n;
    }
    function showError(message) { error.textContent = message; error.hidden = false; error.scrollIntoView({ block: 'center' }); }
    function renderRoutes() {
        const root = document.getElementById('network-editor-routes'); root.replaceChildren();
        const points = [['', 'Choose a point'], ...map.nodes.map(n => [n.id, `${n.label} (${n.station})`])];
        map.connections.forEach((route, i) => {
            const row = el('div', '', 'ne-row');
            row.append(field('From', select(points, route.from, value => route.from = value)), field('To', select(points, route.to, value => route.to = value)),
                field('Supply type', select(Object.entries(routeKinds), route.kind, value => route.kind = value)),
                field('Condition / note', input(route.condition, value => route.condition = value, 200)),
                button('Remove connection', () => { map.connections.splice(i, 1); renderRoutes(); }));
            root.append(row);
        });
        if (!map.connections.length) root.append(el('p', 'No connections yet. Add a connection to show what supplies what.'));
    }
    function renderPoints() {
        const root = document.getElementById('network-editor-points'); root.replaceChildren();
        for (const node of map.nodes) {
            const row = el('div', '', 'ne-point');
            const name = input(node.label, value => node.label = value); name.required = true;
            name.addEventListener('change', renderRoutes);
            row.append(field('Point name', name), field('Station', select([['Lower', 'Lower Station'], ['Top', 'Top Station']], node.station, value => { node.station = value; renderRoutes(); })),
                field('Role', select(Object.entries(roles), node.kind, value => node.kind = value)));
            const metered = document.createElement('input'); metered.type = 'checkbox'; metered.checked = node.canLinkMeter !== false;
            metered.addEventListener('change', () => { node.canLinkMeter = metered.checked; if (!metered.checked) node.meterId = null; renderPoints(); });
            row.append(field('Has a meter', metered));
            if (node.canLinkMeter !== false) row.append(field('Actual meter', select([['', 'Not assigned yet'], ...meters.map(m => [String(m.id), `${m.name} (#${m.id})`])], node.meterId, value => {
                node.meterId = value ? Number(value) : null;
                if (node.label === 'New point' && node.meterId) { const meter = meters.find(m => m.id === node.meterId); node.label = meter.name; node.kind = ({ 1: 'source', 2: 'transfer', 3: 'distribution', 4: 'usage', 5: 'storage' })[meter.role] || node.kind; renderPoints(); renderRoutes(); }
            })));
            row.append(field('Description', input(node.note, value => node.note = value, 1000)), button('Remove point', () => {
                if (window.confirm(`Remove ${node.label} and its diagram connections? The meter and readings will be kept.`)) { removePoint(map, node.id); renderPoints(); renderRoutes(); }
            })); root.append(row);
        }
    }
    document.getElementById('add-network-point').addEventListener('click', () => {
        map.nodes.push({ id: `point-${Array.from(crypto.getRandomValues(new Uint8Array(16)), b => b.toString(16).padStart(2, '0')).join('')}`, label: 'New point', station: 'Lower', kind: 'usage', canLinkMeter: true, meterId: null, note: '', x: 0, y: 0 }); renderPoints(); renderRoutes();
        document.getElementById('network-editor-points').lastElementChild.scrollIntoView({ block: 'center' });
    });
    document.getElementById('add-network-route').addEventListener('click', () => {
        map.connections.push({ from: '', to: '', kind: 'supply', condition: '' }); renderRoutes();
        document.getElementById('network-editor-routes').lastElementChild.scrollIntoView({ block: 'center' });
    });
    form.addEventListener('submit', event => {
        try { map.nodes.forEach(n => n.label = n.label.trim()); arrangeNetwork(map); hidden.value = JSON.stringify(map); }
        catch (e) { event.preventDefault(); showError(e.message); }
    });
    renderPoints(); renderRoutes(); document.getElementById('network-editor-controls').hidden = false;
} catch { error.textContent = 'The editor could not open this map. Reload the page to load the saved version.'; error.hidden = false; }
