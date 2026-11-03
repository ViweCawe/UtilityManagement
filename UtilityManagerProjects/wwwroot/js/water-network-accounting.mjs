export const reportingRoles = { '': 'Not a source total', main: 'Main Meter · municipal total', 'borehole-outlet': 'Borehole Out · water supplied', 'borehole-inlet': 'Borehole Incoming · stored inflow', hydrant: 'Fire Hydrant · separate branch' };
export const origins = { municipal:'Municipal', borehole:'Borehole', rain:'Rainwater', other:'Other / unconfirmed' };
export function reportingRole(node) { return node.reportingRole ?? ({'main-tank':'main','borehole-out':'borehole-outlet','borehole-in':'borehole-inlet','fire-hydrant':'hydrant'}[node.id]||''); }
export function usageArea(node) { return (node.usageArea ?? ({workshop:'Workshop','staff-toilets-lower':'Level 1 Visitor Toilets'}[node.id]||'')).trim(); }
export function rootOrigin(node) { return node?.supplyOrigin || ({main:'municipal',hydrant:'municipal','borehole-inlet':'borehole','borehole-outlet':'borehole'}[reportingRole(node||{})]) || (node?.id==='rainwater'?'rain':'other'); }
export function nodeOrigins(map,id,visiting=new Set(),cache=new Map()) {
 if(cache.has(id))return new Set(cache.get(id));
 if(visiting.has(id)) return new Set(['other']);
 const seen=new Set(visiting);seen.add(id);
 const parents=map.connections.filter(e=>e.to===id);
 if(!parents.length) return new Set([rootOrigin(map.nodes?.find(n=>n.id===id))]);
 const result=new Set();
 for(const e of parents) for(const source of e.kind==='rain'?['rain']:nodeOrigins(map,e.from,seen,cache)) if(!e.sourceFilter||e.sourceFilter===source) result.add(source);
 cache.set(id,new Set(result));return result;
}
export function flowSections(map) {
 const order=[],remaining=new Set(map.nodes.map(n=>n.id));
 while(remaining.size) {
  const ready=map.nodes.filter(n=>remaining.has(n.id)&&!map.connections.some(e=>e.to===n.id&&remaining.has(e.from)));
  if(!ready.length) { order.push(...map.nodes.filter(n=>remaining.has(n.id)));break; }
  for(const n of ready) {order.push(n);remaining.delete(n.id);}
 }
 const rank=new Map();for(const n of order) rank.set(n.id,Math.max(0,...map.connections.filter(e=>e.to===n.id).map(e=>(rank.get(e.from)||0)+1)));
 const groups=[['municipal','Municipal supply · Main Meter and distribution'],['borehole','Borehole · storage and outlet'],['mixed','Shared supplies · merged circuits and downstream points'],['hydrant','Fire Hydrant · separate municipal branch'],['other','Other supplies and unconnected points']].map(([id,label])=>({id,label,nodes:[]}));
 function hasHydrant(id,seen=new Set()) {if(seen.has(id))return false;seen.add(id);if(reportingRole(map.nodes.find(n=>n.id===id)||{})==='hydrant')return true;return map.connections.filter(e=>e.to===id).some(e=>hasHydrant(e.from,seen));}
 function afterMerge(id,seen=new Set()) {if(seen.has(id))return false;seen.add(id);if(nodeOrigins(map,id).size>1)return true;return map.connections.filter(e=>e.to===id).some(e=>afterMerge(e.from,seen));}
 for(const n of order) { const sources=nodeOrigins(map,n.id);const id=afterMerge(n.id)?'mixed':sources.has('municipal')?(hasHydrant(n.id)?'hydrant':'municipal'):sources.has('borehole')?'borehole':'other';groups.find(g=>g.id===id).nodes.push({node:n,depth:rank.get(n.id)}); }
 return groups.filter(g=>g.nodes.length);
}
