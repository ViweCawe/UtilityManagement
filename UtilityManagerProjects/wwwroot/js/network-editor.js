import { arrangeNetwork, removePoint, validateNetwork } from './network-editor-model.mjs';
import { flowSections, reportingRole, reportingRoles, origins, nodeOrigins, usageArea, rootOrigin } from './water-network-accounting.mjs';
const form=document.getElementById('network-edit-form'),hidden=document.getElementById('NetworkJson'),error=document.getElementById('network-editor-error');
function el(tag,text,cls){const n=document.createElement(tag);if(text)n.textContent=text;if(cls)n.className=cls;return n;}
function button(text,fn){const n=el('button',text);n.type='button';n.addEventListener('click',fn);return n;}
let fieldSequence=0;
function field(title,control){const wrapper=el('div','', 'ne-field'),label=el('label',title);control.id ||= `network-field-${++fieldSequence}`;label.htmlFor=control.id;wrapper.append(label,control);return wrapper;}
function input(value,change,max=100){const n=el('input');n.value=value||'';n.maxLength=max;n.addEventListener('input',()=>change(n.value));return n;}
function select(options,value,change){const n=el('select');for(const [v,text] of options){const o=el('option',text);o.value=v;n.append(o);}if(value&&!options.some(o=>String(o[0])===String(value))){const o=el('option',`Unavailable assignment #${value}`);o.value=value;n.append(o);}n.value=value??'';n.addEventListener('change',()=>change(n.value));return n;}
function showError(message){error.textContent=message;error.hidden=false;error.scrollIntoView({block:'center'});}
try {
 const map=JSON.parse(hidden.value),meters=JSON.parse(document.getElementById('network-editor-meters').textContent),roles={source:'Source',storage:'Metered tank',distribution:'Distribution',usage:'Water use',transfer:'Transfer',unresolved:'Needs confirmation'};
 const task=document.getElementById('network-editor-task');let selected=map.nodes.find(n=>n.id==='workshop')?.id||map.nodes[0].id;
 const pointOptions=()=>flowSections(map).flatMap(g=>g.nodes.map(({node})=>[node.id,`${node.label} (${node.station}) · ${nodeOrigins(map,node.id).size>1?'shared supply':origins[[...nodeOrigins(map,node.id)][0]]||'unconfirmed'}`]));
 function renameLinks(){renderFlow();renderReview();}
 function routeRow(route){const row=el('div','', 'ne-route-card');row.append(field('Supplied by',select([['','Choose supplier'],...pointOptions().filter(p=>p[0]!==route.to)],route.from,v=>{route.from=v;renameLinks();})),field('Route operation',select([['supply','Normal supply'],['transfer','Tank transfer'],['backup','Conditional / backup'],['rain','Rainwater']],route.kind,v=>{route.kind=v;renameLinks();})),field('Used for',input(route.purpose,v=>route.purpose=v,120)),field('Carries water from',select([['','All sources on this route'],...Object.entries(origins)],route.sourceFilter||'',v=>{route.sourceFilter=v;renameLinks();})),field('When / operating note',input(route.condition,v=>route.condition=v,200)),button('Remove this supply route',()=>{map.connections.splice(map.connections.indexOf(route),1);renderAll();}));return row;}
 function renderTask(){const root=document.getElementById('network-editor-workspace');root.replaceChildren();
  if(task.value==='review'){root.append(el('p','Open the flow sections below. Source totals and usage-area names are edited inside each point.'));return;}
  if(task.value==='move'){
   root.append(field('Existing point to rearrange',select(pointOptions(),selected,v=>{selected=v;renderTask();})));
   const node=map.nodes.find(n=>n.id===selected);root.append(el('p',`${node.label} keeps the same meter and readings. Only change the suppliers or route notes below.`));
   for(const route of map.connections.filter(e=>e.to===selected))root.append(routeRow(route));
   if(!map.connections.some(e=>e.to===selected))root.append(el('p','This point currently has no upstream supplier. Keep it that way only if it is an external source.'));
   root.append(button('Add another supplier to this point',()=>{map.connections.push({from:'',to:selected,kind:'supply',purpose:'',condition:'',sourceFilter:''});renderTask();renderReview();}));return;
  }
  root.append(el('p','Create the meter record first if it does not exist, then attach that meter to its physical supplier.'));
  const create=el('a','Create a meter or tank first');create.href=form.dataset.createMeterUrl;root.append(create);
  const draft={meterId:'',label:'',parent:'',area:'',kind:'usage',purpose:''};
  const available=meters.filter(m=>!map.nodes.some(n=>n.meterId===m.id));
  const name=input('',v=>draft.label=v);const kind=select(Object.entries(roles),'usage',v=>draft.kind=v);
  root.append(field('Actual meter',select([['','Choose an unused meter'],['unmetered','Unmetered outlet (no counter)'],...available.map(m=>[String(m.id),m.name])],'',v=>{draft.meterId=v;const m=meters.find(m=>String(m.id)===v);if(m){draft.label=m.name;name.value=m.name;draft.kind=({1:'source',2:'transfer',3:'distribution',4:'usage',5:'storage'})[m.role]||'usage';kind.value=draft.kind;}})),field('Point name',name),field('Point role',kind),field('Supplied by',select([['','External inlet / no upstream supplier'],...pointOptions()],'',v=>draft.parent=v)),field('Usage area (optional)',input('',v=>draft.area=v)),field('Used for (optional)',input('',v=>draft.purpose=v,120)));
  root.append(button('Connect this new point',()=>{
   if(!draft.meterId||!draft.label.trim())return showError('Choose an unused meter or unmetered outlet, and enter its name.');
   if(!draft.parent&&!['source','storage'].includes(draft.kind))return showError('Choose its upstream supplier. Only an external source or storage inlet may start without a supplier.');
   const node={id:`point-${Array.from(crypto.getRandomValues(new Uint8Array(16)),b=>b.toString(16).padStart(2,'0')).join('')}`,label:draft.label.trim(),station:map.nodes.find(n=>n.id===draft.parent)?.station||'Lower',kind:draft.kind,canLinkMeter:draft.meterId!=='unmetered',meterId:draft.meterId==='unmetered'?null:Number(draft.meterId),usageArea:draft.area.trim(),reportingRole:'',isExternalSupply:!draft.parent&&draft.meterId!=='unmetered',supplyOrigin:'other',note:'',x:0,y:0};map.nodes.push(node);
   if(draft.parent)map.connections.push({from:draft.parent,to:node.id,kind:'supply',purpose:draft.purpose,condition:'',sourceFilter:''});
   selected=node.id;task.value='review';renderAll();const card=document.querySelector(`[data-point-id="${node.id}"]`);card.open=true;card.scrollIntoView({block:'center'});
  }));
 }
 function storageControls(node) {
  const box=el('details','', 'ne-storage-settings');box.append(el('summary','Storage balance and final-use measurements'));
  if(node.canLinkMeter!==false){const final=el('input');final.type='checkbox';final.disabled=['main','municipal-inlet','borehole-inlet','borehole-outlet'].includes(reportingRole(node));final.checked=node.measuresFinalUse===true;final.addEventListener('change',()=>{node.measuresFinalUse=final.checked;renderReview();});box.append(field('This counter measures final consumption, not a tank transfer',final));}
  if(!['source','storage'].includes(node.kind))return box;
  const enable=el('input');enable.type='checkbox';enable.checked=!!node.storage;
  enable.addEventListener('change',()=>{node.storage=enable.checked?{openingDate:null,openingVolumeLitres:null,inletMeterIds:[],outletMeterIds:[],allFlowsConfirmed:false}:null;const replacement=storageControls(node);replacement.open=true;box.replaceWith(replacement);renderReview();});
  box.append(field('This point represents physical stored water; configure its balance',enable));
  box.append(el('p','A cumulative reading is not tank contents. Only enter a measured opening volume. One transfer counter may be the supplier’s outlet and the next tank’s inlet.'));
  if(!node.storage)return box;
  const stock=node.storage,volume=input(stock.openingVolumeLitres??'',v=>stock.openingVolumeLitres=v===''?null:Number(v));volume.type='number';volume.min='0';volume.step='0.01';
  const date=input(stock.openingDate?.slice(0,10)||'',v=>stock.openingDate=v?v+'T00:00:00':null);date.type='date';
  box.append(field('Measured opening stock (litres), not the counter value',volume),field('Opening stock measured at end of this date',date));
  function choices(key,title){const group=el('fieldset');group.append(el('legend',title));for(const meter of meters.filter(m=>map.nodes.some(n=>n.meterId===m.id))){const checked=el('input');checked.type='checkbox';checked.checked=stock[key].includes(meter.id);checked.disabled=key==='inletMeterIds'&&reportingRole(node)==='main'&&meter.id===node.meterId;checked.addEventListener('change',()=>{stock[key]=checked.checked?[...stock[key],meter.id]:stock[key].filter(id=>id!==meter.id);renderReview();});const label=el('label',meter.name,'ne-meter-choice');label.prepend(checked);group.append(label);}return group;}
  box.append(choices('inletMeterIds','Counters measuring water into this tank'),choices('outletMeterIds','Counters measuring all water leaving this tank'));
  const verified=el('input');verified.type='checkbox';verified.checked=stock.allFlowsConfirmed===true;verified.addEventListener('change',()=>{stock.allFlowsConfirmed=verified.checked;renderReview();});box.append(field('I verified every inlet, outlet, rain input and use/loss is accounted for by these counters',verified));
  box.append(el('p','If any path is unmetered or not confirmed, leave this unchecked. Unknown remaining stock will stay unknown.'));
  return box;
 }
 function renderFlow(){const root=document.getElementById('network-editor-points');const opened=new Set([...root.querySelectorAll('details[open]')].map(e=>e.dataset.pointId));root.replaceChildren();
  for(const section of flowSections(map)){
   const group=el('section','', 'ne-flow-section');group.append(el('h3',section.label));
   for(const {node,depth} of section.nodes){const card=el('details','', 'ne-flow-point');card.dataset.pointId=node.id;card.open=opened.has(node.id);card.style.setProperty('--flow-depth',Math.min(depth,3));
    const parents=map.connections.filter(e=>e.to===node.id),sourceNames=[...nodeOrigins(map,node.id)].map(s=>origins[s]||s);
    const summary=el('summary',`${node.label} · ${node.station} Station · ${sourceNames.join(' + ')}${node.canLinkMeter!==false&&!node.meterId?' · meter not linked':''}`);card.append(summary);
    const description=el('p',parents.length?`Supplied by: ${parents.map(e=>map.nodes.find(n=>n.id===e.from)?.label||'Choose supplier').join(' + ')}`:'External inlet / no mapped supplier');card.append(description);
    if(sourceNames.length>1&&node.canLinkMeter!==false)card.append(el('p','Combined meter: its source volumes cannot be separated from one counter. Keep source paths distinct; link separate inlet meters only when they physically exist.','ne-combined-note'));
    const fields=el('div','', 'ne-point');const name=input(node.label,v=>node.label=v);name.addEventListener('change',()=>{renderFlow();renderTask();});
    fields.append(field('Name',name),field('Station',select([['Lower','Lower Station'],['Top','Top Station']],node.station,v=>node.station=v)),field('Role',select(Object.entries(roles),node.kind,v=>{node.kind=v;renderReview();})),field('Usage area',input(usageArea(node),v=>{node.usageArea=v;renderReview();})));
    const metered=el('input');metered.type='checkbox';metered.checked=node.canLinkMeter!==false;
    metered.addEventListener('change',()=>{node.canLinkMeter=metered.checked;if(!metered.checked){node.meterId=null;node.reportingRole='';node.isExternalSupply=false;}renderFlow();renderReview();});
    fields.append(field('Record a meter at this point',metered));
    if(node.canLinkMeter!==false){fields.append(field('Actual meter',select([['','Not assigned'],...meters.filter(m=>m.id===node.meterId||!map.nodes.some(n=>n.meterId===m.id)).map(m=>[String(m.id),m.name])],node.meterId,v=>{node.meterId=v?Number(v):null;renderReview();})),field('Reporting boundary',select(Object.entries(reportingRoles),reportingRole(node),v=>{node.reportingRole=v;if(v==='main')node.isExternalSupply=false;if(['municipal-inlet','hydrant','borehole-inlet'].includes(v)){node.isExternalSupply=true;node.supplyOrigin=v==='borehole-inlet'?'borehole':'municipal';}renderReview();})));
    }
    if(node.canLinkMeter!==false&&(['source','storage'].includes(node.kind)||node.isExternalSupply)) {
     const external=el('input');external.type='checkbox';external.checked=node.isExternalSupply===true;external.disabled=reportingRole(node)==='main';
     external.addEventListener('change',()=>{node.isExternalSupply=external.checked;renderReview();});fields.append(field('External water inlet (not a downstream meter)',external));
    }
    if(!parents.length)fields.append(field('Source water type',select(Object.entries(origins),rootOrigin(node),v=>{node.supplyOrigin=v;renderFlow();renderReview();})));
    fields.append(field('Description',input(node.note,v=>node.note=v,1000)));card.append(fields);card.append(storageControls(node));
    card.append(button('Change supply connections',()=>{selected=node.id;task.value='move';renderTask();document.getElementById('network-editor-workspace').scrollIntoView({block:'center'});}),button('Remove point from map',()=>{if(confirm(`Remove ${node.label} and its routes from the map? Its meter and reading history are kept.`)){removePoint(map,node.id);selected=map.nodes[0]?.id;renderAll();}}));group.append(card);
   }root.append(group);
  }
 }
 function renderReview(){error.hidden=true;const root=document.getElementById('network-editor-review');root.replaceChildren();
  root.append(el('p',`${map.nodes.length} points · ${map.connections.length} routes. Changes affect the map after Save.`));
  const mains=map.nodes.filter(n=>reportingRole(n)==='main');root.append(el('p',mains.length===1?`Recorded source outlet: ${mains[0].label}${mains[0].meterId?'':' (link its meter)'}`:'Choose exactly one Main Meter reporting boundary.'));
  for(const n of map.nodes.filter(n=>nodeOrigins(map,n.id).size>1&&n.canLinkMeter!==false))root.append(el('p',`${n.label}: combined-source meter; source split not separately measured.`));
  try{validateNetwork(map);root.append(el('p','Routes checked. Review meter assignments, then save.'));}catch(e){root.append(el('p','Needs attention: '+e.message,'ne-combined-note'));}
 }
 function renderAll(){renderTask();renderFlow();renderReview();}
 task.addEventListener('change',renderTask);
 form.addEventListener('submit',event=>{try{map.nodes.forEach(n=>n.label=n.label.trim());arrangeNetwork(map);hidden.value=JSON.stringify(map);}catch(e){event.preventDefault();showError(e.message);}});
 renderAll();document.getElementById('network-editor-controls').hidden=false;
} catch(e){showError('The editor could not open this map. Reload the saved version. '+e.message);}
