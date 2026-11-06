const{test}=require('node:test');const assert=require('node:assert/strict');const{chromium}=require('playwright');
test('source outlet, unknown inlet, tank balances and consumption-only tables stay distinct',async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_EXECUTABLE_PATH,args:['--no-sandbox']});
 try{
  const context=await browser.newContext({viewport:{width:1440,height:1000},extraHTTPHeaders:{'X-Validation-Role':'Admin'}});await context.route(/https:\/\//,r=>r.abort());
  const page=await context.newPage();const errors=[];page.on('pageerror',e=>{if(e.message!=='Chart is not defined')errors.push(e.message);}); // Existing Dashboard/Reports charts use a CDN deliberately blocked in this test.
  for(const path of ['/WaterBalance','/MeterReadings/List?DateFilter=Last7','/Dashboard/Dashboard?DateFilter=Last7','/Dashboard/Reports?DateFilter=Last7']){
   const response=await page.goto('http://127.0.0.1:5188'+path);assert.equal(response.status(),200,path);
   const consumption=page.locator('[aria-label="Water consumption only"]');assert.equal(await consumption.count(),1,path+' consumption table exists');
   assert.deepEqual(await consumption.locator('[data-consumption-meter]').evaluateAll(rows=>rows.map(r=>r.dataset.consumptionMeter)),['9'],path+' contains only confirmed consumption meter');
   assert.match(await consumption.textContent(),/100.0 L/);assert.doesNotMatch(await consumption.textContent(),/Main Meter|Docking Bay|Borehole Out/,'source and transfer rows excluded');
   if(!path.startsWith('/Dashboard/Dashboard')){const transfers=page.locator('[aria-label="Measured outlet transfers"]');assert.equal(await transfers.count(),1);assert.match(await transfers.textContent(),/300.*200.*100/s);assert.doesNotMatch(await transfers.textContent(),/Toilets consumption/);}
   if(path==='/WaterBalance'){
    const source=page.locator('[data-tank-balance="main-tank"]');const cells=await source.locator(':scope>td').allTextContents();
    assert.match(cells[2],/Unknown/,'municipal inflow stays unknown');assert.match(cells[3],/700.0 L/,'Main outlet is shown as water leaving');assert.match(cells[4],/Unknown/,'remaining source stock stays unknown');
    assert.deepEqual(await page.locator('.tank-balance-table thead th').allTextContents(),['Tank','Opening stock','Water received / confirmed transfer','Water leaving','Closing stock']);
    await page.screenshot({path:'/tmp/outlet-tank-and-consumption-desktop.png',fullPage:true});
    await page.setViewportSize({width:390,height:844});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+2),false);await page.screenshot({path:'/tmp/outlet-tank-and-consumption-mobile.png',fullPage:true});await page.setViewportSize({width:1440,height:1000});
   }
   if(path.startsWith('/MeterReadings')){assert.equal(await page.getByRole('columnheader',{name:'Recorded flow / energy',exact:true}).count(),1);assert.match(await page.locator('body').textContent(),/Source outlet \/ distribution/);}
  }
  await page.goto('http://127.0.0.1:5188/WaterNetwork?node=main-tank');await page.locator('#network-controls').waitFor({state:'visible'});assert.match(await page.locator('#selected-point-routes').textContent(),/Municipal inlet/);
  assert.deepEqual(errors,[]);
 }finally{await browser.close();}
});
