// Run against the Admin.Validation --serve host; all data is synthetic.
const{test}=require('node:test');const assert=require('node:assert/strict');const{chromium}=require('playwright');
test('station flow, borehole pairing, curves and cross-station source traces render on actual Razor pages',async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_EXECUTABLE_PATH,args:['--no-sandbox']});
 try{
  const context=await browser.newContext({extraHTTPHeaders:{'X-Validation-Role':'Admin'},viewport:{width:1440,height:1000}});
  await context.route(/https:\/\//,r=>r.abort());const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
  let response=await page.goto('http://127.0.0.1:5188/WaterBalance');assert.equal(response.status(),200);
  await page.getByRole('heading',{name:'Lower Station water flow',exact:true}).waitFor();await page.getByRole('heading',{name:'Top Station water flow',exact:true}).waitFor();
  await page.getByRole('heading',{name:'Borehole · incoming and outgoing',exact:true}).waitFor();
  assert.equal(await page.locator('.station-flow-chart').count(),2);
  assert.match(await page.locator('.borehole-matched').textContent(),/700.0 L in.*350.0 L out.*350.0 L measured difference/s);
  assert.match(await page.locator('[data-flow-point="docking-bay"]').textContent(),/200.0 L/,'Top Station shows measured transfer volume');
  assert.equal(await page.locator('.station-flow-chart').nth(1).locator('svg path').count(),1,'Top Station curve uses measured daily averages');
  await page.locator('.station-chart-status').first().filter({hasText:'days covered'}).waitFor();
  assert.ok(await page.locator('.station-flow-chart').first().locator('svg path').count()>0,'daily flow actually draws');
  await page.locator('.station-flow-meter').first().selectOption('workshop');
  assert.match(await page.locator('.station-chart-status').first().textContent(),/link its actual meter/,'missing linked meter is unknown');
  const house=page.locator('[data-flow-point="male-female-toilets"]');await house.locator('summary').click();assert.match(await house.locator('details').textContent(),/Main Meter.*Docking Bay.*Reservoir.*Header Tank.*Toilets/s);
  await page.locator('.station-comparison').first().locator(':scope > summary').click();
  assert.match(await page.locator('.station-comparison').first().textContent(),/combined counter|Link an active water meter/,'comparison gaps explain themselves');
  await page.screenshot({path:'/tmp/station-water-balance-desktop.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+2),false,'mobile station page fits');
  await page.screenshot({path:'/tmp/station-water-balance-mobile.png',fullPage:true});
  response=await page.goto('http://127.0.0.1:5188/WaterNetwork?node=male-female-toilets');assert.equal(response.status(),200);
  await page.locator('#network-controls').waitFor({state:'visible'});assert.equal(await page.locator('#network-station').inputValue(),'All');
  assert.equal(await page.locator('#network-diagram .wn-node').count(),34,'both stations shown together');
  assert.equal(await page.locator('.wn-station-label').count(),2);
  assert.match(await page.locator('#selected-point-title').textContent(),/Male and Female/,'source link selects the correct point');
  assert.match(await page.locator('#selected-point-routes').textContent(),/Main Meter.*Docking Bay.*Reservoir.*Header Tank/s,'an unlinked point still shows its mapped source route');
  await page.locator('#network-point').selectOption('main-tank');
  assert.equal(await page.evaluate(()=>{const box=document.querySelector('.wn-node.selected').getBoundingClientRect(),frame=document.querySelector('.wn-canvas').getBoundingClientRect();return box.left>=frame.left&&box.right<=frame.right;}),true,'selected source remains visible inside the mobile canvas');
  assert.match(await page.locator('#network-diagram .wn-node').filter({has:page.locator('.wn-reading-value',{hasText:'Flow:'})}).first().textContent(),/Counter:/,'flow and counter visible together');
  const paths=await page.locator('#network-diagram>path title').allTextContents();assert.ok(paths.some(p=>p.includes('Main Meter')&&p.includes('Cable car')),'cross-station link drawn');
  await page.locator('#network-station').selectOption('Top');assert.equal(await page.locator('.wn-station-label').textContent(),'Top Station');assert.ok(await page.locator('#network-diagram .wn-node').count()<34);
  await page.locator('#network-station').selectOption('All');assert.equal(await page.locator('#network-diagram .wn-node').count(),34);
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+2),false,'mobile map canvas stays inside page');
  await page.screenshot({path:'/tmp/station-water-network-mobile.png',fullPage:true});
  // Selecting historical dates must not show today's counter as if it existed then.
  await page.goto('http://127.0.0.1:5188/WaterNetwork?StartDate=2020-01-01&EndDate=2020-01-07&node=main-tank');
  await page.locator('#network-controls').waitFor({state:'visible'});
  assert.match(await page.locator('#selected-point-routes').textContent(),/No readings yet/);
  assert.match(await page.locator('#selected-point-routes').textContent(),/Selected-period flow is unknown/);
  assert.deepEqual(errors,[]);
 }finally{await browser.close();}
});
