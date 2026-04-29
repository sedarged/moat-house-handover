import { hostRequest } from '../core/hostBridge.js';

function toNullableNumber(value) {
  if (value === '' || value == null) {
    return null;
  }

  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function normalizeRows(rows) {
  return (Array.isArray(rows) ? rows : []).map((row) => ({
    budgetRowId: row.budgetRowId ?? null,
    deptName: String(row.deptName || '').trim(),
    plannedQty: toNullableNumber(row.plannedQty),
    usedQty: toNullableNumber(row.usedQty),
    reasonText: String(row.reasonText || '')
  }));
}

export const budgetService = {
  async loadBudget(sessionId, userName = '') {
    return hostRequest('budget.load', { sessionId, userName });
  },

  async saveBudget(sessionId, rows, meta = null, userName = '') {
    return hostRequest('budget.save', {
      sessionId,
      userName,
      rows: normalizeRows(rows),
      meta
    });
  },

  async recalculate(sessionId, rows, meta = null) {
    return hostRequest('budget.recalculate', {
      sessionId,
      rows: normalizeRows(rows),
      meta
    });
  },

  async loadDashboardBudgetSummary(sessionId) {
    return hostRequest('dashboard.budgetSummary', { sessionId });
  }
};
