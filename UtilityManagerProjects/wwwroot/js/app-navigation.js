(() => {
 const button=document.querySelector('.sidebar-toggle');
 const sidebar=document.querySelector('#utility-sidebar');
 if(!button||!sidebar) return;
 function close() { button.setAttribute('aria-expanded','false');sidebar.classList.remove('is-open'); }
 button.addEventListener('click',()=>{const open=button.getAttribute('aria-expanded')!=='true';button.setAttribute('aria-expanded',String(open));sidebar.classList.toggle('is-open',open);});
 document.addEventListener('keydown',event=>{if(event.key==='Escape'&&button.getAttribute('aria-expanded')==='true'){close();button.focus();}});
 document.addEventListener('click',event=>{if(!sidebar.contains(event.target)&&!button.contains(event.target)) close();});
})();
