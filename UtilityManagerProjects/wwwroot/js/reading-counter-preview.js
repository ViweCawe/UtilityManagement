import { displayCounter } from './counter-conversion.mjs';
const input=document.querySelector('[data-counter-entry]');
const payload=document.getElementById('counter-unit-options');
if(input&&payload){
    const units=JSON.parse(payload.textContent),selector=document.getElementById(input.dataset.meterSelect),output=document.getElementById(input.dataset.counterReadout),label=document.getElementById('counter-entry-label');
    function update(){
        const info=units[selector?.value||input.dataset.fixedMeter];
        if(label)label.textContent=info?`Counter as shown on the meter (${info.Unit})`:'Counter as shown on the meter';
        if(!info){output.textContent='Select a meter to see its counter unit.';return;}
        const converted=displayCounter(input.value,info.Factor);
        output.textContent=converted===null?`Enter the counter in ${info.Unit}, with up to six decimal places.`:
            `Display: ${converted} ${info.DisplayUnit}${info.Factor!==1?` (${input.value} ${info.Unit} × ${info.Factor})`:''}. Cumulative counter; period flow uses the difference between readings.`;
    }
    input.addEventListener('input',update);selector?.addEventListener('change',update);update();
}
