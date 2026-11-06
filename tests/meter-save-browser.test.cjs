const {test}=require('node:test');const assert=require('node:assert/strict');const {chromium}=require('playwright');
test('blank optional serial saves direction, survives reload, and only confirmed saves show green',async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_EXECUTABLE_PATH,args:['--no-sandbox']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:1000},extraHTTPHeaders:{'X-Validation-Role':'Admin'}});await page.route(/https:\/\//,r=>r.abort());
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  const url='http://127.0.0.1:5188/Meters/Edit/6';
  let response=await page.goto(url);assert.equal(response.status(),200);
  assert.equal(await page.getByLabel('Serial / physical meter number',{exact:true}).inputValue(),'');
  await page.getByLabel('Measurement direction',{exact:true}).selectOption('2');
  await page.getByRole('button',{name:'Save Changes',exact:true}).click();
  await page.getByRole('status').filter({hasText:'Saved successfully.'}).waitFor();
  assert.equal(await page.locator('.validation-summary-valid').isVisible(),false,'no empty red error panel beside a successful save');
  assert.equal(await page.getByLabel('Measurement direction',{exact:true}).inputValue(),'2');
  const color=await page.locator('.meter-save-success').evaluate(e=>({background:getComputedStyle(e).backgroundColor,text:getComputedStyle(e).color}));
  assert.equal(color.background,'rgb(232, 245, 238)');assert.equal(color.text,'rgb(20, 83, 45)');
  await page.reload();assert.equal(await page.getByLabel('Measurement direction',{exact:true}).inputValue(),'2','saved direction remains after reload');
  await page.getByLabel('Measurement direction',{exact:true}).selectOption('1');await page.getByRole('button',{name:'Save Changes',exact:true}).click();await page.locator('.meter-save-success').waitFor();
  await page.reload();assert.equal(await page.getByLabel('Measurement direction',{exact:true}).inputValue(),'1','correction from a confirmed direction also persists');
  // Valid-looking input with a deliberately ignored write must not show success.
  await page.getByLabel('Meter name',{exact:false}).fill('__simulate_ignored_update__');await page.getByRole('button',{name:'Save Changes',exact:true}).click();
  await page.getByRole('alert').filter({hasText:'Changes could not be confirmed'}).waitFor();assert.equal(await page.locator('.meter-save-success').count(),0);
  await page.goto(url);assert.equal(await page.getByLabel('Meter name',{exact:false}).inputValue(),'New usage meter');
  await page.getByLabel('Meter name',{exact:false}).fill('');await page.getByRole('button',{name:'Save Changes',exact:true}).click();
  await page.getByRole('alert').filter({hasText:'Enter a meter name'}).waitFor();assert.equal(await page.locator('.meter-save-success').count(),0);
  // Replacement chains keep a consistent corrected direction.
  await page.goto('http://127.0.0.1:5188/Meters/Edit/8');await page.getByLabel('Measurement direction',{exact:true}).selectOption('2');await page.getByRole('button',{name:'Save Changes',exact:true}).click();await page.locator('.meter-save-success').waitFor();
  await page.goto('http://127.0.0.1:5188/Meters/Edit/10');assert.equal(await page.getByLabel('Measurement direction',{exact:true}).inputValue(),'2','predecessor shares the corrected point direction');
  await page.goto(url);await page.getByLabel('Measurement direction',{exact:true}).selectOption('2');await page.getByRole('button',{name:'Save Changes',exact:true}).click();await page.locator('.meter-save-success').waitFor();
  await page.screenshot({path:'/tmp/meter-save-confirmation-desktop.png',fullPage:true});await page.setViewportSize({width:390,height:844});await page.screenshot({path:'/tmp/meter-save-confirmation-mobile.png',fullPage:true});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+2),false);assert.deepEqual(errors,[]);
 }finally{await browser.close();}
});
