(() => {
    const kind = document.getElementById('Input_AssetKind');
    const role = document.getElementById('Input_NetworkRole');
    const capacity = document.getElementById('Input_CapacityLitres');
    const cumulative = document.getElementById('Input_IsCumulative');
    if (!kind || !role) return;
    function update(changed) {
        const tank = kind.value === '2';
        for (const option of role.options) {
            option.disabled = !!option.value && (tank ? !['1', '3', '5'].includes(option.value) : option.value === '5');
        }
        if (changed && role.selectedOptions[0]?.disabled) role.value = '';
        capacity.disabled = !tank;
        if (changed && !tank) capacity.value = '';
        if (cumulative) { cumulative.disabled = false; }
        for (const input of document.querySelectorAll('input[name="Input.MeterType"]')) {
            if (tank) input.checked = input.value === 'Water';
            input.disabled = tank && input.value !== 'Water';
        }
    }
    kind.addEventListener('change', () => update(true));
    update(false);
})();
