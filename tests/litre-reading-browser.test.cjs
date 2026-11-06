const {test}=require('node:test');const assert=require('node:assert/strict');const {chromium}=require('playwright');
test('reading pages show 806000 L with original 806 m³; capture submits raw counter once',async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_EXECUTABLE_PATH,args:['--no-sandbox']});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:1000},extraHTTPHeaders:{'X-Validation-Role':'Admin'}});await page.route(/https:\/\//,r=>r.abort());const errors=[];page.on('pageerror',e=>{if(e.message!=='Chart is not defined')errors.push(e.message);});
  let response=await page.goto('http://127.0.0.1:5188/MeterReadings/List?DateFilter=Last7&MeterId=6');assert.equal(response.status(),200);
  let row=page.locator('tr').filter({hasText:'RDG-0064'});assert.equal(await row.count(),1);let cells=await row.locator('td').allTextContents();assert.match(cells[7],/800,000 L/);assert.match(cells[8],/806,000 L/);assert.match(cells[8],/Meter: 806 m³/);assert.match(cells[9],/6,000 L/);
  await page.goto('http://127.0.0.1:5188/MeterReadings/History/6');row=page.locator('tr').filter({hasText:'RDG-0064'});assert.match(await row.innerText(),/806,000 L/);assert.match(await row.innerText(),/6,000 L/);
  await page.goto('http://127.0.0.1:5188/MeterReadings/Display/64');assert.match(await page.locator('body').innerText(),/806 m³ × 1,000 = 806,000 L/);assert.equal(await page.getByLabel('Counter as shown on the meter (m³)',{exact:true}).inputValue(),'806');await page.locator('#counter-conversion').filter({hasText:'806,000 L'}).waitFor();
  await page.screenshot({path:'/tmp/litre-reading-detail-desktop.png',fullPage:true});await page.setViewportSize({width:390,height:844});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+2),false);await page.screenshot({path:'/tmp/litre-reading-detail-mobile.png',fullPage:true});
  await page.goto('http://127.0.0.1:5188/MeterReadings/Create');await page.getByLabel('Meter',{exact:true}).selectOption('6');await page.getByLabel('Counter as shown on the meter (m³)',{exact:true}).fill('806.125');await page.locator('#counter-conversion').filter({hasText:'806,125 L'}).waitFor();assert.equal(await page.locator('#Input_CurrentReading').inputValue(),'806.125');
  await page.locator('#Input_CurrentReading').fill('806');await page.locator('#counter-conversion').filter({hasText:'806,000 L'}).waitFor();await page.screenshot({path:'/tmp/litre-reading-create-mobile.png',fullPage:true});
  let submitted;await page.route('**/MeterReadings/Create',async route=>{if(route.request().method()!=='POST')return route.continue();submitted=new URLSearchParams(route.request().postData()).get('Input.CurrentReading');await route.fulfill({contentType:'text/html',body:'<p>Captured raw submission for validation</p>'});});
  await page.getByRole('button',{name:'Save Meter Reading',exact:true}).click();await page.getByText('Captured raw submission for validation').waitFor();assert.equal(submitted,'806','litre preview must never replace the counter submitted to the server');
  await page.goto('http://127.0.0.1:5188/Dashboard/Dashboard?DateFilter=Last7&MeterId=6');const latest=page.getByRole('region',{name:'Daily average by meter'});assert.match(await latest.innerText(),/806,000 L/);assert.match(await latest.innerText(),/Meter: 806 m³/);
  assert.deepEqual(errors,[]);
 }finally{await browser.close();}
});
