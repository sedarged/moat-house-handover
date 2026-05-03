import { attachmentsService } from '../services/attachmentsService.js';

const HANDOVER_ATTACHMENT_AREAS = [
  'General Handover','Injection','MetaPress','Berks','Wilts','Racking','Butchery','Further Processing','Tumblers','Smoke Tumbler','Minimums & Samples','Goods In & Despatch','Dry Goods','Additional'
];
const STATUS_MAP = new Map([['ready','Ready'],['missing','Missing'],['needs review','Needs review'],['unsupported','Unsupported'],['not uploaded','Not uploaded']]);

function el(tag, cls, text){ const n=document.createElement(tag); if(cls) n.className=cls; if(text!==undefined) n.textContent=String(text); return n; }
function nav(route){ window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route}})); }
function safe(v,f='—'){ return v===undefined||v===null||v===''?f:String(v); }
function safeStatus(v){ return STATUS_MAP.get(String(v||'').trim().toLowerCase()) || 'Needs review'; }
function fmtDate(v){ if(!v) return '—'; const d=new Date(v); return Number.isNaN(d.getTime())?safe(v):d.toLocaleString(); }
function fmtSize(v){ const n=Number(v); if(!Number.isFinite(n)||n<=0) return '—'; if(n<1024) return `${n} B`; if(n<1048576) return `${(n/1024).toFixed(1)} KB`; return `${(n/1048576).toFixed(1)} MB`; }

function fallbackRows(state){
  const u=state.session?.updatedBy||state.session?.createdBy||'Supervisor';
  return [
    { attachmentId:1, deptName:'Injection', displayName:'injector-reading.png', fileType:'Image', fileSizeBytes:245221, addedBy:u, addedAt:'2026-04-29T14:10:00', status:'Ready' },
    { attachmentId:2, deptName:'MetaPress', displayName:'metapress-note.pdf', fileType:'Document', fileSizeBytes:14520, addedBy:u, addedAt:'2026-04-29T14:12:00', status:'Needs review' },
    { attachmentId:3, deptName:'General Handover', displayName:'handover-comment.txt', fileType:'Text', fileSizeBytes:612, addedBy:u, addedAt:'2026-04-29T14:14:00', status:'Not uploaded', isFallback:true }
  ];
}

export function renderAttachmentsScreen(root,state){
  const section=el('section','attachments-screen');
  const shift=state.selectedShift||state.session?.shiftCode||'AM';
  const date=state.activeSessionDate||state.session?.shiftDate||new Date().toLocaleDateString();
  const mode=state.activeSessionMode||'continue';
  const sid=state.activeSessionId||state.session?.sessionId||null;
  const statusPanel=el('div','attachments-status-panel');

  section.innerHTML='<div class="attachments-head"><h2>ATTACHMENTS</h2><p>Session evidence for handover areas and shift handover context.</p></div>';
  const context=el('div','attachments-context-strip');
  ['Date',date,'Shift',shift,'Session',safe(sid,'Not opened'),'Mode',mode].forEach((v,i)=>{ if(i%2===0){const s=el('span');s.append(document.createTextNode(`${v}: `),el('strong',null,['Date',date,'Shift',shift,'Session',safe(sid,'Not opened'),'Mode',mode][i+1]));context.append(s);} });
  section.append(context);

  const summary=el('div','attachments-summary-grid'); section.append(summary);
  const filterBar=el('div','attachments-filter-row'); const sel=el('select','attachments-area-filter'); HANDOVER_ATTACHMENT_AREAS.forEach(a=>{const o=el('option',null,a);o.value=a;sel.append(o);}); const all=el('option',null,'All areas'); all.value='__all'; sel.prepend(all); filterBar.append(el('label',null,'Area filter'),sel); section.append(filterBar);

  const tableWrap=el('div','attachments-table-wrap'); const table=el('table','attachments-table'); table.innerHTML='<thead><tr><th>Area / Department</th><th>File name</th><th>File type</th><th>Size</th><th>Added by</th><th>Added at</th><th>Status</th><th>Actions</th></tr></thead>'; const tbody=el('tbody'); table.append(tbody); tableWrap.append(table); section.append(tableWrap);

  const add=el('section','attachments-add-panel'); add.append(el('h3',null,'Add attachment')); const addMsg=el('p','status-line warn','Attachment upload requires desktop host file picker/storage wiring. No file was saved.');
  const addArea=el('select'); HANDOVER_ATTACHMENT_AREAS.forEach(a=>{const o=el('option',null,a);o.value=a;addArea.append(o);});
  const note=el('textarea'); note.rows=2; note.placeholder='Notes / description';
  const pick=el('button','btn btn-secondary','Select file'); const attach=el('button','btn btn-primary','Attach'); attach.disabled=true;
  add.append(addArea,pick,note,attach,addMsg); section.append(add);

  const actions=el('div','attachments-bottom-actions');
  const btnRefresh=el('button','btn btn-secondary','Refresh'); const btnAdd=el('button','btn btn-primary','Add Attachment'); const btnPreview=el('button','btn btn-secondary','Preview / Reports'); const btnSession=el('button','btn btn-ghost','Back to Handover Session'); const btnBoard=el('button','btn btn-ghost','Back to Department Status Board');
  actions.append(btnRefresh,btnAdd,btnPreview,btnSession,btnBoard); section.append(statusPanel,actions);

  let rows=[];
  async function load(){
    let provider='Browser fallback'; let canDelete='future'; let canView='future';
    try{ if(sid){ const payload=await attachmentsService.listAttachments(sid,0,sel.value==='__all'?'General Handover':sel.value); rows=Array.isArray(payload?.attachments)?payload.attachments:[]; provider='Host service'; canDelete='available'; canView='available'; } else { rows=[]; }
    }catch{ rows=[]; }
    if(!rows.length){ rows=fallbackRows(state); }
    const current=sel.value; const filtered= current==='__all'?rows:rows.filter(r=>safe(r.deptName,'General Handover')===current);
    tbody.replaceChildren();
    filtered.forEach((r)=>{ const tr=el('tr'); const a=safe(r.deptName,'General Handover'); const name=safe(r.displayName||r.fileName,'Unknown'); const typ=safe(r.fileType,'Unknown'); const st=safeStatus(r.status); [a,name,typ,fmtSize(r.fileSizeBytes||r.sizeBytes),safe(r.addedBy,'—'),fmtDate(r.addedAt||r.createdAt),st].forEach(v=>tr.append(el('td',null,v))); const act=el('td'); const v=el('button','btn btn-ghost','Open / View'); v.disabled=canView!=='available'; const d=el('button','btn btn-ghost','Remove'); d.disabled=canDelete!=='available'; act.append(v,d); tr.append(act); tbody.append(tr); });
    const areas=new Set(rows.map(r=>safe(r.deptName,'General Handover')));
    const needs=rows.filter(r=>safeStatus(r.status)==='Needs review').length; const miss=rows.filter(r=>['Missing','Not uploaded'].includes(safeStatus(r.status))).length; const last=rows.map(r=>r.addedAt||r.createdAt).filter(Boolean).sort().reverse()[0]||null;
    summary.replaceChildren(...[['Total attachments',rows.length],['Areas with attachments',areas.size],['Needs review',needs],['Missing / not uploaded',miss],['Last added',fmtDate(last)],['Storage mode',provider]].map(([k,v])=>{const c=el('article','attachments-summary-card'); c.append(el('span',null,k),el('strong',null,v)); return c;}));
    statusPanel.replaceChildren(el('h3',null,'Attachment service status'),...[
      `Connected to host attachment service: ${provider==='Host service'?'Yes':'No (Browser fallback)'}`,
      'Storage path: available/unavailable',
      'Upload support: future',
      `Delete support: ${canDelete}`,
      `Preview support: ${canView}`
    ].map(t=>el('p',null,t)));
  }
  sel.addEventListener('change',load); btnRefresh.addEventListener('click',load); btnAdd.addEventListener('click',()=>add.scrollIntoView({behavior:'smooth',block:'start'})); btnPreview.addEventListener('click',()=>nav('reports')); btnSession.addEventListener('click',()=>nav(mode==='create'?'sessionCreate':mode==='open'?'sessionOpen':'sessionContinue')); btnBoard.addEventListener('click',()=>nav('departmentBoard')); pick.addEventListener('click',()=>{addMsg.textContent='Attachment upload requires desktop host file picker/storage wiring. No file was saved.';});

  root.replaceChildren(section); load();
}
