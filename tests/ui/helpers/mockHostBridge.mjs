export const richFixture = {
  sessionPayload: {
    sessionId: 1001,
    shiftCode: 'PM',
    shiftDate: '2026-04-29',
    sessionStatus: 'Open',
    createdAt: '2026-04-29T13:55:00',
    createdBy: 'Supervisor',
    updatedAt: '2026-04-29T14:25:00',
    updatedBy: 'Supervisor',
    departments: [
      { deptName: 'Injection', deptStatus: 'Complete', downtimeMin: 12, efficiencyPct: 94.2, yieldPct: 98.1, attachmentCount: 2, updatedAt: '2026-04-29T14:10:00', updatedBy: 'Supervisor', notes: 'Line running to plan.' },
      { deptName: 'MetaPress', deptStatus: 'Incomplete', downtimeMin: 18, efficiencyPct: 89.5, yieldPct: 96.4, attachmentCount: 1, updatedAt: '2026-04-29T14:12:00', updatedBy: 'Supervisor', notes: 'Waiting for maintenance confirmation.' },
      { deptName: 'Berks', deptStatus: 'Complete', downtimeMin: 8, efficiencyPct: 92.4, yieldPct: 97.0, attachmentCount: 0, updatedAt: '2026-04-29T14:14:00', updatedBy: 'Supervisor', notes: '' },
      { deptName: 'Wilts', deptStatus: 'Complete', downtimeMin: 5, efficiencyPct: 95.0, yieldPct: 98.3, attachmentCount: 1, updatedAt: '2026-04-29T14:16:00', updatedBy: 'Supervisor', notes: '' },
      { deptName: 'Racking', deptStatus: 'Complete', attachmentCount: 0, updatedAt: '2026-04-29T14:17:00', updatedBy: 'Supervisor', notes: 'Racks staged for NS.' },
      { deptName: 'Butchery', deptStatus: 'Not running', attachmentCount: 0, updatedAt: null, updatedBy: null, notes: '' },
      { deptName: 'Further Processing', deptStatus: 'Incomplete', attachmentCount: 1, updatedAt: '2026-04-29T14:18:00', updatedBy: 'Supervisor', notes: 'Short one operative.' },
      { deptName: 'Tumblers', deptStatus: 'Complete', attachmentCount: 0, updatedAt: '2026-04-29T14:19:00', updatedBy: 'Supervisor', notes: '' },
      { deptName: 'Smoke Tumbler', deptStatus: 'Not running', attachmentCount: 0, updatedAt: null, updatedBy: null, notes: '' },
      { deptName: 'Minimums & Samples', deptStatus: 'Complete', attachmentCount: 0, updatedAt: '2026-04-29T14:20:00', updatedBy: 'Supervisor', notes: '' },
      { deptName: 'Goods In & Despatch', deptStatus: 'Complete', attachmentCount: 0, updatedAt: '2026-04-29T14:21:00', updatedBy: 'Supervisor', notes: '' },
      { deptName: 'Dry Goods', deptStatus: 'Complete', attachmentCount: 0, updatedAt: '2026-04-29T14:22:00', updatedBy: 'Supervisor', notes: '' },
      { deptName: 'Additional', deptStatus: 'Incomplete', attachmentCount: 0, updatedAt: '2026-04-29T14:23:00', updatedBy: 'Supervisor', notes: 'Agency cover requested.' }
    ]
  },
  budgetPayload: {
    summary: {
      linesPlanned: 3,
      linesCount: 3,
      plannedTotal: 41,
      usedTotal: 38,
      totalStaffOnRegister: 44,
      holidayCount: 2,
      absentCount: 1,
      otherReasonCount: 0,
      agencyUsedCount: 1,
      varianceTotal: -3,
      status: 'under',
      comments: 'One operator moved to cover line support.\nNo agency required.',
      lastUpdatedAt: '2026-04-29T19:12:00',
      lastUpdatedBy: 'Supervisor'
    },
    totals: {
      plannedTotal: 41,
      usedTotal: 38,
      varianceTotal: -3,
      status: 'under'
    },
    rows: [
      { budgetRowId: 1, deptName: 'Injection', plannedQty: 6, usedQty: 6, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 2, deptName: 'MetaPress', plannedQty: 4, usedQty: 4, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 3, deptName: 'Berks', plannedQty: 3, usedQty: 3, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 4, deptName: 'Wilts', plannedQty: 3, usedQty: 2, variance: -1, status: 'under', reasonText: 'Holiday' },
      { budgetRowId: 5, deptName: 'Further Processing', plannedQty: 4, usedQty: 3, variance: -1, status: 'under', reasonText: 'Absent' },
      { budgetRowId: 6, deptName: 'Brine operative', plannedQty: 2, usedQty: 2, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 7, deptName: 'Rack cleaner / domestic', plannedQty: 2, usedQty: 1, variance: -1, status: 'under', reasonText: 'Holiday' },
      { budgetRowId: 8, deptName: 'Goods In', plannedQty: 2, usedQty: 2, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 9, deptName: 'Dry Goods', plannedQty: 2, usedQty: 1, variance: -1, status: 'under', reasonText: 'Agency cover' },
      { budgetRowId: 10, deptName: 'Supervisors', plannedQty: 3, usedQty: 3, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 11, deptName: 'Admin', plannedQty: 2, usedQty: 2, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 12, deptName: 'Cleaners', plannedQty: 2, usedQty: 2, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 13, deptName: 'Stock controller', plannedQty: 1, usedQty: 1, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 14, deptName: 'Training', plannedQty: 1, usedQty: 1, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 15, deptName: 'Trolley Porter T1/T2', plannedQty: 2, usedQty: 2, variance: 0, status: 'on target', reasonText: '' },
      { budgetRowId: 16, deptName: 'Butchery', plannedQty: 2, usedQty: 2, variance: 0, status: 'on target', reasonText: '' }
    ]
  }
};


export const pickerFixture = {
  ...richFixture,
  sessions: [
    { sessionId: 2001, shiftCode: 'NS', shiftLabel: 'Night Shift', shiftDate: '2026-05-01', sessionStatus: 'Open', createdAt: '2026-05-01T21:55:00', createdBy: 'Lead A', updatedAt: '2026-05-01T23:10:00', updatedBy: 'Lead A' },
    { sessionId: 2002, shiftCode: 'NS', shiftLabel: 'Night Shift', shiftDate: '2026-05-02', sessionStatus: 'Draft', createdAt: '2026-05-02T21:50:00', createdBy: 'Lead B', updatedAt: '2026-05-02T22:20:00', updatedBy: 'Lead B' }
  ],
  openByIdPayloads: {
    2001: { ...richFixture.sessionPayload, sessionId: 2001, shiftCode: 'NS', shiftDate: '2026-05-01', sessionStatus: 'Open', updatedBy: 'Lead A' },
    2002: { ...richFixture.sessionPayload, sessionId: 2002, shiftCode: 'NS', shiftDate: '2026-05-02', sessionStatus: 'Draft', updatedBy: 'Lead B' }
  }
};

export const budgetUnavailableFixture = {
  ...richFixture,
  failBudgetLoad: true
};

export const budgetSaveFailureFixture = {
  ...richFixture,
  failBudgetSave: true
};

function clone(value) {
  return JSON.parse(JSON.stringify(value));
}

function calculateBudget(rows, meta = {}) {
  const normalisedRows = (Array.isArray(rows) ? rows : []).map((row, index) => {
    const plannedQty = row.plannedQty == null || row.plannedQty === '' ? null : Number(row.plannedQty);
    const usedQty = row.usedQty == null || row.usedQty === '' ? null : Number(row.usedQty);
    const variance = (Number.isFinite(usedQty) ? usedQty : 0) - (Number.isFinite(plannedQty) ? plannedQty : 0);
    return {
      budgetRowId: row.budgetRowId ?? index + 1,
      deptName: row.deptName || row.labourArea || '',
      plannedQty,
      usedQty,
      variance,
      status: variance > 0 ? 'over' : variance < 0 ? 'under' : 'on target',
      reasonText: row.reasonText || row.reason || ''
    };
  });

  const plannedTotal = normalisedRows.reduce((sum, row) => sum + (Number(row.plannedQty) || 0), 0);
  const usedTotal = normalisedRows.reduce((sum, row) => sum + (Number(row.usedQty) || 0), 0);
  const varianceTotal = usedTotal - plannedTotal;
  const reasonText = normalisedRows.map((row) => String(row.reasonText || '').toLowerCase());

  const summary = {
    linesPlanned: meta?.linesPlanned ?? richFixture.budgetPayload.summary.linesPlanned,
    linesCount: meta?.linesPlanned ?? richFixture.budgetPayload.summary.linesCount,
    plannedTotal,
    usedTotal,
    totalStaffOnRegister: meta?.totalStaffOnRegister ?? richFixture.budgetPayload.summary.totalStaffOnRegister,
    holidayCount: reasonText.filter((item) => item.includes('holiday')).length,
    absentCount: reasonText.filter((item) => item.includes('absent')).length,
    otherReasonCount: reasonText.filter((item) => item && !item.includes('holiday') && !item.includes('absent') && !item.includes('agency')).length,
    agencyUsedCount: reasonText.filter((item) => item.includes('agency')).length,
    varianceTotal,
    status: varianceTotal > 0 ? 'over' : varianceTotal < 0 ? 'under' : 'on target',
    comments: meta?.comments ?? richFixture.budgetPayload.summary.comments,
    lastUpdatedAt: '2026-04-29T19:12:00',
    lastUpdatedBy: 'Mock Host'
  };

  return {
    summary,
    totals: {
      plannedTotal,
      usedTotal,
      varianceTotal,
      status: summary.status
    },
    rows: normalisedRows
  };
}

export async function installMockHostBridge(page, fixture = richFixture) {
  await page.addInitScript((seed) => {
    const listeners = new Set();
    const state = {
      sessionPayload: JSON.parse(JSON.stringify(seed.sessionPayload)),
      budgetPayload: JSON.parse(JSON.stringify(seed.budgetPayload)),
      sessions: JSON.parse(JSON.stringify(seed.sessions || [])),
      openByIdPayloads: JSON.parse(JSON.stringify(seed.openByIdPayloads || {})),
      failSessionList: !!seed.failSessionList,
      failBudgetLoad: !!seed.failBudgetLoad,
      failBudgetSave: !!seed.failBudgetSave
    };

    function calculateBudget(rows, meta = {}) {
      const normalisedRows = (Array.isArray(rows) ? rows : []).map((row, index) => {
        const plannedQty = row.plannedQty == null || row.plannedQty === '' ? null : Number(row.plannedQty);
        const usedQty = row.usedQty == null || row.usedQty === '' ? null : Number(row.usedQty);
        const variance = (Number.isFinite(usedQty) ? usedQty : 0) - (Number.isFinite(plannedQty) ? plannedQty : 0);
        return {
          budgetRowId: row.budgetRowId ?? index + 1,
          deptName: row.deptName || row.labourArea || '',
          plannedQty,
          usedQty,
          variance,
          status: variance > 0 ? 'over' : variance < 0 ? 'under' : 'on target',
          reasonText: row.reasonText || row.reason || ''
        };
      });

      const plannedTotal = normalisedRows.reduce((sum, row) => sum + (Number(row.plannedQty) || 0), 0);
      const usedTotal = normalisedRows.reduce((sum, row) => sum + (Number(row.usedQty) || 0), 0);
      const varianceTotal = usedTotal - plannedTotal;
      const reasonText = normalisedRows.map((row) => String(row.reasonText || '').toLowerCase());
      const summary = {
        linesPlanned: meta?.linesPlanned ?? state.budgetPayload.summary.linesPlanned,
        linesCount: meta?.linesPlanned ?? state.budgetPayload.summary.linesCount,
        plannedTotal,
        usedTotal,
        totalStaffOnRegister: meta?.totalStaffOnRegister ?? state.budgetPayload.summary.totalStaffOnRegister,
        holidayCount: reasonText.filter((item) => item.includes('holiday')).length,
        absentCount: reasonText.filter((item) => item.includes('absent')).length,
        otherReasonCount: reasonText.filter((item) => item && !item.includes('holiday') && !item.includes('absent') && !item.includes('agency')).length,
        agencyUsedCount: reasonText.filter((item) => item.includes('agency')).length,
        varianceTotal,
        status: varianceTotal > 0 ? 'over' : varianceTotal < 0 ? 'under' : 'on target',
        comments: meta?.comments ?? state.budgetPayload.summary.comments,
        lastUpdatedAt: '2026-04-29T19:12:00',
        lastUpdatedBy: 'Mock Host'
      };
      return { summary, totals: { plannedTotal, usedTotal, varianceTotal, status: summary.status }, rows: normalisedRows };
    }

    function previewPayload() {
      return {
        session: state.sessionPayload,
        departments: state.sessionPayload.departments,
        budgetSummary: state.budgetPayload.summary,
        budgetRows: state.budgetPayload.rows,
        attachmentSummary: [
          { deptName: 'Injection', attachmentCount: 2, attachments: [{ displayName: 'injector-reading.png' }, { displayName: 'brine-temp.png' }] },
          { deptName: 'MetaPress', attachmentCount: 1, attachments: [{ displayName: 'metapress-note.png' }] }
        ]
      };
    }

    function handle(type, payload) {
      switch (type) {
        case 'runtime.getStatus':
          return { ok: true, mode: 'browser-mock' };
        case 'session.open':
        case 'session.createBlank':
          return state.sessionPayload;
        case 'session.clearDay':
          return state.sessionPayload;
        case 'session.list':
          if (state.failSessionList) throw new Error('Mock list failure');
          return state.sessions;
        case 'session.openById':
          return state.openByIdPayloads[payload?.sessionId] || state.sessionPayload;
        case 'budget.load':
          if (state.failBudgetLoad) throw new Error('Mock budget load unavailable');
          return state.budgetPayload;
        case 'budget.recalculate':
          return calculateBudget(payload?.rows, payload?.meta);
        case 'budget.save':
          if (state.failBudgetSave) throw new Error('Mock budget save unavailable');
          state.budgetPayload = calculateBudget(payload?.rows, payload?.meta);
          return state.budgetPayload;
        case 'dashboard.budgetSummary':
          return state.budgetPayload.summary;
        case 'preview.load':
          return previewPayload();

        case 'attachment.list':
          return { attachments: [
            { attachmentId: 11, deptName: 'Injection', displayName: 'injector-reading.png', fileType: 'Image', fileSizeBytes: 245221, addedBy: 'Mock Host', addedAt: '2026-04-29T14:10:00', status: 'Ready' },
            { attachmentId: 12, deptName: 'MetaPress', displayName: 'metapress-note.pdf', fileType: 'Document', fileSizeBytes: 14520, addedBy: 'Mock Host', addedAt: '2026-04-29T14:12:00', status: 'Needs review' },
            { attachmentId: 13, deptName: 'Goods In & Despatch', displayName: 'dispatch-checklist.docx', fileType: 'Document', fileSizeBytes: 50000, addedBy: 'Mock Host', addedAt: '2026-04-29T14:20:00', status: 'Missing' }
          ] };
        case 'reports.generateHandover':
          return { reportType: 'handover', generatedAt: '2026-04-29T14:40:00', generatedBy: 'Mock Host', filePaths: ['C:/Mock/Handover.html'] };
        case 'reports.generateBudget':
          return { reportType: 'budget', generatedAt: '2026-04-29T14:41:00', generatedBy: 'Mock Host', filePaths: ['C:/Mock/Budget.html'] };
        case 'reports.generateAll':
          return { reportType: 'all', generatedAt: '2026-04-29T14:42:00', generatedBy: 'Mock Host', filePaths: ['C:/Mock/Handover.html', 'C:/Mock/Budget.html'] };
        case 'reports.openFolder':
          return { openedPath: 'C:/Mock' };
        default:
          return { ok: true, type, payload };
      }
    }

    window.chrome = window.chrome || {};
    window.chrome.webview = {
      addEventListener(type, callback) {
        if (type === 'message') listeners.add(callback);
      },
      removeEventListener(type, callback) {
        if (type === 'message') listeners.delete(callback);
      },
      postMessage(request) {
        setTimeout(() => {
          try {
            const response = { requestId: request.requestId, success: true, payload: handle(request.type, request.payload) };
            listeners.forEach((callback) => callback({ data: response }));
          } catch (error) {
            const response = { requestId: request.requestId, success: false, error: error instanceof Error ? error.message : String(error) };
            listeners.forEach((callback) => callback({ data: response }));
          }
        }, 0);
      }
    };
  }, clone(fixture));
}

export async function openMockRoute(page, routeName, fixture = richFixture) {
  await installMockHostBridge(page, fixture);
  await page.goto('/webapp/index.html');
  await page.waitForFunction(() => Boolean(window.__mhApp));
  if (routeName !== 'shift') {
    await page.evaluate(({ sessionPayload, routeName }) => {
      window.__mhApp.applySessionPayload(sessionPayload);
      window.__mhApp.navigate(routeName);
    }, { sessionPayload: fixture.sessionPayload, routeName });
  }
}
