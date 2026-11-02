// Traverse each direction separately: another branch's source is not a supply
// of the selected point merely because both feed the same downstream tank.
export function traceNetwork(network, selectedId, includeBackup = false) {
    const edges = network.connections.filter(e => includeBackup || e.kind !== 'backup');
    function walk(reverse) {
        const found = new Set();
        const pending = [selectedId];
        while (pending.length) {
            const id = pending.pop();
            for (const edge of edges) {
                if ((reverse ? edge.to : edge.from) !== id) continue;
                const next = reverse ? edge.from : edge.to;
                if (next !== selectedId && !found.has(next)) {
                    found.add(next);
                    pending.push(next);
                }
            }
        }
        return found;
    }
    return { upstream: walk(true), downstream: walk(false) };
}

export function traceEdge(edge, selectedId, trace, includeBackup = false) {
    if (edge.kind === 'backup' && !includeBackup) return false;
    return ((edge.to === selectedId || trace.upstream.has(edge.to)) && trace.upstream.has(edge.from)) ||
        ((edge.from === selectedId || trace.downstream.has(edge.from)) && trace.downstream.has(edge.to));
}
