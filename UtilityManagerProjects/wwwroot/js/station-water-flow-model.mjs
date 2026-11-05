// Preserve unknown intervals and genuine zero flow without connecting across gaps.
export function dailyFlowSegments(series){
 const segments=[];let current=[];
 series.forEach((value,index)=>{if(value===null||!Number.isFinite(value)){if(current.length)segments.push(current);current=[];}else current.push({index,value});});
 if(current.length)segments.push(current);return segments;
}

// Stack the station coordinate spaces to avoid overlapping their existing layouts.
export function stationNetworkLayout(network,station='All'){
 const offsets={},labels=[];let bottom=0;
 const stations=station==='All'?['Lower','Top']:[station];
 for(const name of stations){const points=network.nodes.filter(n=>n.station===name);if(!points.length)continue;
  offsets[name]=bottom;labels.push({text:name+' Station',y:bottom-10});bottom+=Math.max(...points.map(n=>n.y))+220;
 }
 const nodes=network.nodes.filter(n=>stations.includes(n.station));
 return{nodes,offsets,labels,height:Math.max(165,...nodes.map(n=>n.y+offsets[n.station]+165))};
}
