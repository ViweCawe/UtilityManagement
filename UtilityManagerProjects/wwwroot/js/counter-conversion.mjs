// Scale the decimal text with integer arithmetic so large counters and red-dial
// fractions keep their exact value. This preview never alters the input field.
export function displayCounter(raw, factor) {
    if (![1,1000].includes(factor)) return null;
    const value=String(raw).trim();
    if (!/^(?:\d+(?:\.\d{0,6})?|\.\d{1,6})$/.test(value)) return null;
    const [whole='',fraction='']=value.split('.');
    const scaled=(BigInt(whole||'0')*1000000n+BigInt(fraction.padEnd(6,'0')))*BigInt(factor);
    const integer=scaled/1000000n, decimals=(scaled%1000000n).toString().padStart(6,'0').replace(/0+$/,'');
    return new Intl.NumberFormat('en-GB').format(integer)+(decimals?'.'+decimals:'');
}
