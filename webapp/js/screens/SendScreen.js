import { sendService } from '../services/sendService.js';
import { iconBrandSvg, iconArrowLeft, iconBell, iconUser, iconCalendar, iconClockSm, iconSend, iconRefresh } from '../core/icons.js';

function formatDate(iso) {
  if (!iso) return 'Not available';
  const parts = String(iso).split('-');
  if (parts.length === 3) return `${parts[2]}/${parts[1]}/${parts[0]}`;
  return String(iso);
}

function safeText(v, fallback = 'Not available') {
  if (v == null) return fallback;
  const s = String(v).trim();
  return s ? s : fallback;
}

function mapStatus(value) {
  const valid = new Set(['Ready', 'Needs review', 'Missing', 'Not available', 'Future phase']);
  return valid.has(value) ? value : 'Needs review';
}

function statusTag(status) {
  const span = document.createElement('span');
  span.className = 'readiness-chip';
  span.textContent = mapStatus(status);
  return span;
}

function infoItem(label, value) {
  const row = document.createElement('div');
  row.className = 'send-info-item';
  const k = document.createElement('span');
  k.className = 'send-info-label';
  k.textContent = `${label}: `;
  const v = document.createElement('span');
  v.className = 'send-info-value';
  v.textContent = safeText(value);
  row.append(k, v);
  return row;
}

function reportRowsFromState(state) {
  const reports = Array.isArray(state.generatedReports) ? state.generatedReports : [];
  const rows = [];
  reports.forEach((report) => {
    const filePaths = Array.isArray(report?.filePaths) ? report.filePaths : [];
    if (!filePaths.length) return;
    filePaths.forEach((path) => {
      rows.push({
        reportType: safeText(report.reportType, 'Not available'),
        filePath: safeText(path),
        generatedAt: safeText(report.generatedAt),
        status: 'Ready'
      });
    });
  });
  return rows;
}

export function renderSendScreen(root, state) {
  const session = state?.session || {};
  const sessionId = session.sessionId;
  const shiftCode = safeText(session.shiftCode, state.selectedShift || 'Not available');
  const shiftDate = formatDate(session.shiftDate || state.activeSessionDate);
  const recipients = { to: 'Not configured', cc: 'Not configured', bcc: 'Not configured', profile: 'UI fallback / not configured' };
  const subjectDefault = `${shiftCode} Shift Handover — ${shiftDate}`;
  let subject = subjectDefault;
  let body = `Hello,\n\nPlease find attached the shift handover package for:\n- Date: ${shiftDate}\n- Shift: ${shiftCode}\n- Session: ${safeText(sessionId, 'Not opened')}\n\nIncluded:\n- Handover report: Needs review\n- Budget report: Needs review\n- Attachments/evidence: Needs review\n\nRegards,\nSupervisor\n\nThis is a preview. No email was sent.`;

  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen send-email-review-screen';
  screen.innerHTML = `<header class="screen-header"><button class="header-back" id="send-back" type="button">${iconArrowLeft}</button><div class="header-brand">${iconBrandSvg}<div class="header-brand-text"><span class="header-brand-sub">Moat House</span><span class="header-brand-name">Operations</span></div></div><div class="header-title">SEND / EMAIL REVIEW</div><div class="header-actions"><button class="header-icon-btn" type="button">${iconBell}</button><span class="header-divider"></span><button class="header-icon-btn" type="button">${iconUser}</button></div></header>`;

  const info = document.createElement('div');
  info.className = 'screen-infobar';
  info.append(
    infoItem('Date', shiftDate),
    infoItem('Shift', shiftCode),
    infoItem('Session', safeText(sessionId, 'Not opened')),
    infoItem('Report readiness', reportRowsFromState(state).length ? 'Ready' : 'Missing'),
    infoItem('Send readiness', 'Future phase'),
    infoItem('Last report action', safeText(state.generatedReports?.[0]?.generatedAt, 'Not available'))
  );

  const content = document.createElement('div');
  content.className = 'screen-content';
  const status = document.createElement('p');
  status.className = 'status-line warn';
  status.textContent = 'Email sending is not wired in this phase. No email was sent.';
  content.append(status);

  const cards = document.createElement('div'); cards.className = 'preview-readiness-grid';
  const cardDefs = [
    ['Handover report', reportRowsFromState(state).some((r) => r.reportType === 'handover') ? 'Ready' : 'Missing'],
    ['Budget report', reportRowsFromState(state).some((r) => r.reportType === 'budget') ? 'Ready' : 'Missing'],
    ['Attachment evidence', 'Needs review'], ['Recipients', 'Needs review'], ['Subject/body', 'Ready'], ['Send service', 'Future phase']
  ];
  cardDefs.forEach(([label, st]) => { const c = document.createElement('article'); c.className = 'preview-readiness-card'; const h = document.createElement('h4'); h.textContent = label; c.append(h, statusTag(st)); cards.append(c); });
  const cardWrap = document.createElement('section'); cardWrap.className = 'section-block'; const t = document.createElement('h3'); t.className='section-title'; t.textContent='Send readiness'; cardWrap.append(t,cards); content.append(cardWrap);

  const rec = document.createElement('section'); rec.className='section-block send-recipients-section'; rec.append(Object.entries(recipients).map(([k,v])=>infoItem(k.toUpperCase(),v))[0],Object.entries(recipients).map(([k,v])=>infoItem(k.toUpperCase(),v))[1],Object.entries(recipients).map(([k,v])=>infoItem(k.toUpperCase(),v))[2],infoItem('Profile/source',recipients.profile));
  content.append(rec);
  const subj = document.createElement('section'); subj.className='section-block send-subject-section'; const sh=document.createElement('h3'); sh.className='section-title'; sh.textContent='Email subject'; const si=document.createElement('textarea'); si.className='send-textarea'; si.value=subject; si.addEventListener('input',()=>{subject=si.value;}); subj.append(sh,si); content.append(subj);
  const bodySec=document.createElement('section'); bodySec.className='section-block send-body-section'; const bh=document.createElement('h3'); bh.className='section-title'; bh.textContent='Email body preview'; const bi=document.createElement('textarea'); bi.className='send-textarea'; bi.rows=10; bi.value=body; bi.addEventListener('input',()=>{body=bi.value;}); bodySec.append(bh,bi); content.append(bodySec);

  const outputs = document.createElement('section'); outputs.className='section-block send-output-section'; const oh=document.createElement('h3'); oh.className='section-title'; oh.textContent='Generated report outputs / attachments'; outputs.append(oh);
  const list = document.createElement('div');
  const reportRows = reportRowsFromState(state);
  if (!reportRows.length) { const p=document.createElement('p'); p.className='status-line'; p.textContent='No generated reports available yet. Generate reports in Preview / Reports before sending.'; list.append(p);} else { reportRows.forEach((r)=>{ const row=document.createElement('div'); row.className='send-info-item'; row.textContent=`${r.reportType} · ${r.filePath} · ${r.generatedAt} · ${r.status}`; list.append(row);}); }
  outputs.append(list); content.append(outputs);

  const action=document.createElement('section'); action.className='section-block'; const ah=document.createElement('h3'); ah.className='section-title'; ah.textContent='Final send action panel'; action.append(ah);
  const bar=document.createElement('div'); bar.className='preview-report-actions-grid';
  const mk=(name,disabled=false)=>{const b=document.createElement('button'); b.className='btn btn-secondary'; b.textContent=name; b.disabled=disabled; return b;};
  const refresh=mk('Refresh Send Review'); const validate=mk('Validate Email Package'); const draft=mk('Create Draft / Prepare Email', true); const send=mk('Send Email', true); const backPrev=mk('Back to Preview / Reports');
  refresh.addEventListener('click', ()=>renderSendScreen(root,state));
  validate.addEventListener('click', ()=>{ const blockers=[]; if (!reportRows.length) blockers.push('Missing generated reports.'); if (recipients.to==='Not configured') blockers.push('Missing recipients.'); blockers.push('Send service unavailable in this phase.'); status.className='status-line warn'; status.textContent=`Validation: ${blockers.join(' ')}`; });
  backPrev.addEventListener('click', ()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:'preview'}})));
  bar.append(refresh,validate,draft,send,backPrev); action.append(bar); content.append(action);

  const footer=document.createElement('footer'); footer.className='screen-footer';
  const nav = [['Back to Preview / Reports','preview'],['Back to Handover Session','sessionContinue'],['Home','home']];
  nav.forEach(([n,r])=>{const b=document.createElement('button'); b.className='btn btn-ghost'; b.textContent=n; b.addEventListener('click',()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:r}}))); footer.append(b);});

  screen.querySelector('#send-back').addEventListener('click', ()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:'preview'}})));
  screen.append(info, content, footer);
  root.append(screen);
}
