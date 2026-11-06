import {test} from 'node:test';
import assert from 'node:assert/strict';
import {displayCounter} from '../UtilityManagerProjects/wwwroot/js/counter-conversion.mjs';
test('whole and fractional cubic metres display as litres exactly',()=>{
 assert.equal(displayCounter('806',1000),'806,000');
 assert.equal(displayCounter('806.125',1000),'806,125');
 assert.equal(displayCounter('0.000001',1000),'0.001');
 assert.equal(displayCounter('9999999999999.999999',1000),'9,999,999,999,999,999.999');
 assert.equal(displayCounter('000806.000000',1000),'806,000');
});
test('litre and electricity counters retain their quantity',()=>{
 assert.equal(displayCounter('806',1),'806');assert.equal(displayCounter('0',1000),'0');assert.equal(displayCounter('.5',1000),'500');
});
test('invalid or unconfirmed conversions never fabricate a counter',()=>{
 for(const value of ['', '-1','1e3','806,000','1.1234567','NaN'])assert.equal(displayCounter(value,1000),null);
 assert.equal(displayCounter('806',100),null);
});
