// Run against the synthetic Admin.Validation host with --serve.
const {test}=require('node:test');
const assert=require('node:assert/strict');
const {chromium}=require('playwright');
test('connect a new meter, rearrange it, add a second supply and serialize reviewed flow',async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_EXECUTABLE_PATH||undefined,args:['--no-sandbox']});
 try {
  const context=await browser.newContext({viewport:{width:1440,height:1000},extraHTTPHeaders:{'X-Validation-Role':'Admin'}});
  await context.route(/https:\/\//,r=>r.abort());
  const page=await context.newPage();await page.goto('http://127.0.0.1:5188/WaterNetwork/Edit');
  await page.locator('#network-editor-controls').waitFor({state:'visible'});
  assert.equal(await page.locator('.ne-flow-point').count(),33);
  const workspace=page.locator('#network-editor-workspace');
  await workspace.getByLabel('Actual meter',{exact:true}).selectOption('6');
  await workspace.getByLabel('Supplied by',{exact:true}).selectOption('garage-roof');
  await workspace.getByLabel('Usage area (optional)').fill('Workshop');
  await workspace.getByRole('button',{name:'Connect this new point',exact:true}).click();
  assert.equal(await page.locator('.ne-flow-point').count(),34);
  const added=page.locator('.ne-flow-point').filter({has:page.locator('summary',{hasText:'New usage meter'})});
  const id=await added.getAttribute('data-point-id');assert.ok(id);
  assert.match(await added.locator('summary').textContent(),/Municipal/);
  await page.locator('#network-editor-task').selectOption('move');
  await workspace.getByLabel('Supplied by',{exact:true}).selectOption('borehole-out');
  assert.match(await added.locator('summary').textContent(),/Borehole/);
  await workspace.getByRole('button',{name:'Add another supplier to this point'}).click();
  await workspace.getByLabel('Supplied by',{exact:true}).nth(1).selectOption('main-tank');
  assert.match(await added.locator('summary').textContent(),/Municipal|Borehole/);
  assert.ok((await page.locator('#network-editor-review').textContent()).includes('source split not separately measured'));
  let payload;
  await context.route('**/WaterNetwork/Edit',async route=>{
   if(route.request().method()!=='POST')return route.continue();
   payload=JSON.parse(new URLSearchParams(route.request().postData()).get('NetworkJson'));
   await route.fulfill({contentType:'text/html',body:'<p>Captured reviewed network for validation</p>'});
  });
  await page.getByRole('button',{name:'Save reviewed network'}).click();
  await page.waitForSelector('text=Captured reviewed network for validation');
  assert.equal(payload.nodes.find(n=>n.id===id).meterId,6);
  assert.deepEqual(payload.connections.filter(e=>e.to===id).map(e=>e.from).sort(),['borehole-out','main-tank']);
  assert.equal(payload.nodes.find(n=>n.id==='main-tank').meterId,1);
  assert.equal(payload.nodes.length,34);
  await page.goto('http://127.0.0.1:5188/WaterNetwork/Edit');
  await page.setViewportSize({width:390,height:844});
  await page.screenshot({path:require('node:path').join(require('node:os').tmpdir(),'flow-editor-mobile.png'),fullPage:true});
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+2);assert.equal(overflow,false,'mobile editor fits viewport');
 } finally {await browser.close();}
});
