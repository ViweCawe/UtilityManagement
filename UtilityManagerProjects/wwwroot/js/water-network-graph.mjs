import { nodeOrigins } from './water-network-accounting.mjs';
// Keep source restrictions while traversing shared locations: a borehole flushing
// path must not acquire a downstream municipal-only basin circuit.
export function traceNetwork(network, selectedId, includeBackup = false) {
 const edges=network.connections.filter(e=>includeBackup||e.kind!=='backup');const activeEdges=new Set();
 function walk(reverse) {
  const found=new Set(),seen=new Set();
  const initial=reverse?new Set(['municipal','borehole','rain','other']):nodeOrigins(network,selectedId);
  const pending=[{id:selectedId,sources:initial}];
  while(pending.length) {
   const {id,sources}=pending.pop(),key=id+'/'+[...sources].sort().join(',');if(seen.has(key)) continue;seen.add(key);
   for(const edge of edges) {
    if((reverse?edge.to:edge.from)!==id) continue;
    const next=reverse?edge.from:edge.to;
    const allowed=new Set([...sources].filter(s=>!edge.sourceFilter||edge.sourceFilter===s));
    if(reverse) {const upstream=nodeOrigins(network,next);for(const s of allowed) if(!upstream.has(s)) allowed.delete(s);}
    if(!allowed.size) continue;
    activeEdges.add(edge);
    if(next!==selectedId) {found.add(next);pending.push({id:next,sources:allowed});}
   }
  }
  return found;
 }
 return {upstream:walk(true),downstream:walk(false),activeEdges};
}
export function traceEdge(edge,selectedId,trace,includeBackup=false) {
 if(edge.kind==='backup'&&!includeBackup) return false;
 if(trace.activeEdges) return trace.activeEdges.has(edge);
 return ((edge.to===selectedId||trace.upstream.has(edge.to))&&trace.upstream.has(edge.from))||((edge.from===selectedId||trace.downstream.has(edge.from))&&trace.downstream.has(edge.to));
}
