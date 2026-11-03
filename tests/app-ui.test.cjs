const {chromium}=require('playwright');
const path=require('node:path'); const os=require('node:os');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_EXECUTABLE_PATH||undefined,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1440,height:1000},extraHTTPHeaders:{'X-Validation-Role':'Admin'}});
 // Exercise the app even if the external font/chart CDN is unavailable.
 await context.route(/https:\/\//,route=>route.abort());
 const page=await context.newPage();const failures=[];
 const contrast=()=>{
  function rgb(s){const m=s.match(/[\d.]+/g);return m?m.map(Number):null;}
  function lum(c){return c.slice(0,3).map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4}).reduce((s,v,i)=>s+v*[.2126,.7152,.0722][i],0);}
  return [...document.querySelectorAll('h1,h2,h3,p,label,small,strong,span,a,button,th,td')].filter(e=>[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())).flatMap(e=>{
   const box=e.getBoundingClientRect(),style=getComputedStyle(e);if(box.width===0||box.height===0||box.bottom<0||box.top>innerHeight||style.visibility==='hidden')return [];
   let bg=[242,245,249],n=e;while(n){const c=rgb(getComputedStyle(n).backgroundColor);if(c&&c.length===3||c&&c[3]>=.99){bg=c;break;}n=n.parentElement;}
   const fg=rgb(style.color);if(!fg)return[];const a=lum(fg),b=lum(bg);const ratio=(Math.max(a,b)+.05)/(Math.min(a,b)+.05);const large=parseFloat(style.fontSize)>=24||(parseFloat(style.fontSize)>=18.66&&parseInt(style.fontWeight)>=700);
   return ratio<(large?3:4.5)?[{text:e.textContent.trim().slice(0,70),tag:e.tagName,cls:e.className,fg:style.color,bg,ratio:ratio.toFixed(2)}]:[];
  });
 };
 for(const [name,url] of [['landing','/'],['dashboard','/Dashboard/Dashboard?DateFilter=Last7'],['users','/Admin/UserManagement'],['areas','/Area/AreaIndex'],['locations','/Stations'],['readings','/MeterReadings/List'],['environmental','/Environmental'],['reports','/Dashboard/Reports']]) {
  await page.goto('http://127.0.0.1:5188'+url,{waitUntil:'domcontentloaded'});
  if(name!=='landing') {
   if(await page.getByRole('link',{name:'Users',exact:true}).count()!==1)throw Error('duplicate/missing Users');
   if(await page.locator('a[aria-current="page"]').count()!==1)throw Error('wrong active navigation on '+name);
  }
  if(name==='dashboard') {
   if(await page.locator('#meter-averages-title').count()!==1)throw Error('missing Dashboard daily average');
   const field=page.locator('input[type=date]').first();const style=await field.evaluate(e=>({bg:getComputedStyle(e).backgroundColor,color:getComputedStyle(e).color,scheme:getComputedStyle(e).colorScheme}));console.log('Calendar style',style);if(style.bg!=='rgb(255, 255, 255)'||style.color!=='rgb(23, 43, 66)')throw Error('Calendar contrast incorrect');
  }
  const low=await page.evaluate(contrast);failures.push({page:name,low});
  await page.screenshot({path:path.join(os.tmpdir(),'cleanup-'+name+'.png'),fullPage:name==='landing'});
 }
 await context.setExtraHTTPHeaders({'X-Validation-Role':'Reader'});await page.goto('http://127.0.0.1:5188/Dashboard/Dashboard');
 if(await page.getByRole('link',{name:'Users',exact:true}).count())throw Error('Reader sees Admin link');
 await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.join(os.tmpdir(),'cleanup-mobile-debug.png')});await page.getByRole('button',{name:'Menu',exact:true}).click();
 if(await page.locator('.sidebar-toggle').getAttribute('aria-expanded')!=='true')throw Error('menu failed to open');
 await page.keyboard.press('Escape');if(await page.locator('.sidebar-toggle').getAttribute('aria-expanded')!=='false')throw Error('Escape failed');
 await page.screenshot({path:path.join(os.tmpdir(),'cleanup-mobile.png')});
 await context.setExtraHTTPHeaders({});await page.goto('http://127.0.0.1:5188/Identity/Account/Login');failures.push({page:'login',low:await page.evaluate(contrast)});await page.screenshot({path:path.join(os.tmpdir(),'cleanup-login.png'),fullPage:true});
 if(failures.some(p=>p.low.length))throw Error('Visible text contrast failures: '+JSON.stringify(failures.filter(p=>p.low.length)));console.log('PASS: desktop navigation, role visibility, Dashboard table, date field style and mobile menu interactions.');await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
