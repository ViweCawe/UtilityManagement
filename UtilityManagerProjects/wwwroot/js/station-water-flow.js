import { dailyFlowSegments } from './station-water-flow-model.mjs';
const ns='http://www.w3.org/2000/svg';
function svg(tag,attrs={},text){const e=document.createElementNS(ns,tag);for(const[k,v]of Object.entries(attrs))e.setAttribute(k,v);if(text!==undefined)e.textContent=text;return e;}
for(const chart of document.querySelectorAll('.station-flow-chart')){
 const data=JSON.parse(chart.querySelector('.station-flow-series').textContent),select=chart.querySelector('select'),canvas=chart.querySelector('svg'),status=chart.querySelector('.station-chart-status');
 function render(){const point=data.points.find(p=>p.id===select.value);canvas.replaceChildren();const values=point.series.filter(v=>v!==null),max=Math.max(1,...values),count=point.series.length;
  const x=i=>60+(count>1?i/(count-1):.5)*710,y=v=>170-v/max*140;
  canvas.append(svg('line',{x1:60,y1:170,x2:770,y2:170,stroke:'#94a3b8'}),svg('text',{x:12,y:35},new Intl.NumberFormat(undefined,{maximumFractionDigits:1}).format(max)),svg('text',{x:16,y:173},'0'),svg('text',{x:12,y:17},'L/day'),svg('text',{x:60,y:198},data.dates[0]),svg('text',{x:770,y:198,'text-anchor':'end'},data.dates.at(-1)));
  for(const segment of dailyFlowSegments(point.series)){const d=segment.map((p,i)=>`${i?'L':'M'} ${x(p.index)} ${y(p.value)}`).join(' ');canvas.append(svg('path',{d,stroke:'#1d4ed8','stroke-width':2.5,fill:'none'}));if(segment.length===1)canvas.append(svg('circle',{cx:x(segment[0].index),cy:y(segment[0].value),r:3,fill:'#1d4ed8'}));}
  if(!values.length)canvas.append(svg('text',{x:420,y:95,'text-anchor':'middle'},'No flow calculated'));
  status.textContent=!point.linked?`${point.label}: link its actual meter in the network editor to show saved readings.`:values.length?`${point.label}: ${values.length} of ${count} days covered. Daily averages between captures; missing intervals remain gaps.`:`${point.label}: capture two readings on different dates to calculate flow. No usage is assumed.`;
  canvas.setAttribute('aria-label',`${point.label} daily average flow in litres per day, ${values.length} of ${count} days covered`);
 }if(data.selected)select.value=data.selected;select.addEventListener('change',render);render();
}
