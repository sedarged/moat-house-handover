import { createInitialSessionState } from '../models/contracts.js';

export const appState = {
  currentRoute: 'home',
  selectedShift: null,
  selectedShiftLabel: null,
  activeSessionMode: null,
  activeSessionDate: null,
  activeSessionStatus: null,
  activeSessionId: null,
  activeSessionSummary: null,
  lastPersistenceStatus: null,
  runtimeStatus: null,
  runtimeStatusError: null,
  activeDiagnosticsStatus: null,
  activeDiagnosticsPayload: null,
  activeSettingsStatus: null,
  activeSettingsPayload: null,
  activeAdminWarnings: [],
  activeAdminLastUpdatedAt: null,
  session: createInitialSessionState(),
  activeDepartmentName: null,
  activeDepartment: null,
  activeAttachments: [],
  selectedAttachmentId: null,
  viewerState: null,
  activeBudget: null,
  budgetSummary: null,
  preview: null,
  generatedReports: [],
  sendPackage: null
};

function shiftLabelFromCode(shiftCode) {
  if (!shiftCode) return null;
  return shiftCode === 'NS' ? 'Night Shift' : `${shiftCode} Shift`;
}

function applyActiveDepartmentOnly(payload) {
  appState.activeDepartment = payload || null;
  appState.activeDepartmentName = payload?.deptName || appState.activeDepartmentName;
}

function upsertDepartmentIntoSession(payload) {
  if (!payload?.deptName) return null;
  const current = Array.isArray(appState.session?.departments) ? appState.session.departments : [];
  const index = current.findIndex((dept) => dept?.deptName === payload.deptName);
  const merged = index >= 0 ? { ...current[index], ...payload } : { ...payload };
  appState.session.departments = index >= 0
    ? current.map((dept, i) => (i === index ? merged : dept))
    : [...current, merged];
  appState.session.updatedAt = merged.updatedAt || appState.session.updatedAt;
  appState.session.updatedBy = merged.updatedBy || appState.session.updatedBy;
  return merged;
}

export function setSelectedShift(shiftCode) { appState.selectedShift = shiftCode || null; }
export function setRuntimeStatus(status) { appState.runtimeStatus = status || null; appState.runtimeStatusError = null; }
export function setRuntimeStatusError(message) { appState.runtimeStatusError = message || 'Status unavailable'; }

export function applySessionPayload(sessionPayload = {}) {
  appState.activeDepartmentName = null;
  appState.activeDepartment = null;
  appState.activeAttachments = [];
  appState.selectedAttachmentId = null;
  appState.viewerState = null;
  appState.activeBudget = null;
  appState.budgetSummary = null;
  appState.preview = null;
  appState.generatedReports = [];
  appState.sendPackage = null;

  appState.session = {
    ...createInitialSessionState(),
    sessionId: sessionPayload.sessionId,
    shiftCode: sessionPayload.shiftCode,
    shiftDate: sessionPayload.shiftDate,
    sessionStatus: sessionPayload.sessionStatus,
    departments: sessionPayload.departments || [],
    userName: sessionPayload.updatedBy || sessionPayload.createdBy || null,
    createdAt: sessionPayload.createdAt,
    createdBy: sessionPayload.createdBy,
    updatedAt: sessionPayload.updatedAt,
    updatedBy: sessionPayload.updatedBy
  };

  appState.selectedShift = sessionPayload.shiftCode || appState.selectedShift || null;
  appState.selectedShiftLabel = shiftLabelFromCode(sessionPayload.shiftCode) || appState.selectedShiftLabel || null;
  appState.activeSessionDate = sessionPayload.shiftDate || appState.activeSessionDate || null;
  appState.activeSessionStatus = sessionPayload.sessionStatus || appState.activeSessionStatus || null;
  appState.activeSessionId = sessionPayload.sessionId ?? null;
  appState.activeSessionSummary = {
    status: sessionPayload.sessionStatus || null,
    updatedAt: sessionPayload.updatedAt || sessionPayload.createdAt || null,
    updatedBy: sessionPayload.updatedBy || sessionPayload.createdBy || null
  };
  appState.lastPersistenceStatus = sessionPayload.sessionId
    ? `Loaded session ${sessionPayload.sessionId}.`
    : 'Session payload loaded without a persisted id.';
}

export function setActiveDepartmentName(deptName) { appState.activeDepartmentName = deptName || null; }
export function applyActiveDepartmentPayload(payload) {
  applyActiveDepartmentOnly(payload);
  const merged = upsertDepartmentIntoSession(payload);
  if (merged) {
    applyActiveDepartmentOnly(merged);
    appState.lastPersistenceStatus = `Department updated: ${merged.deptName}`;
  }
}
export function applyDepartmentSummaryPayload(departments) {
  if (!Array.isArray(departments)) return;
  appState.session.departments = departments;
  const latest = departments.filter((dept) => dept?.updatedAt).sort((a, b) => String(b.updatedAt).localeCompare(String(a.updatedAt)))[0];
  if (latest) { appState.session.updatedAt = latest.updatedAt; appState.session.updatedBy = latest.updatedBy || appState.session.updatedBy; }
}
export function upsertSessionDepartmentPayload(payload) {
  const merged = upsertDepartmentIntoSession(payload);
  if (!merged) return;
  applyActiveDepartmentOnly(merged);
  appState.lastPersistenceStatus = `Department saved: ${merged.deptName}`;
}
export function applyAttachmentListPayload(listPayload) {
  appState.activeAttachments = Array.isArray(listPayload?.attachments) ? listPayload.attachments : [];
  if (!appState.activeAttachments.some((item) => item.attachmentId === appState.selectedAttachmentId)) {
    appState.selectedAttachmentId = appState.activeAttachments[0]?.attachmentId ?? null;
  }
}
export function setSelectedAttachmentId(attachmentId) { appState.selectedAttachmentId = attachmentId || null; }
export function applyViewerPayload(viewerPayload) { appState.viewerState = viewerPayload || null; appState.selectedAttachmentId = viewerPayload?.current?.attachmentId || appState.selectedAttachmentId; }
export function applyBudgetPayload(payload) { appState.activeBudget = payload || null; appState.budgetSummary = payload?.summary || payload?.totals || null; }
export function applyBudgetSummaryPayload(payload) { appState.budgetSummary = payload || null; }
export function applyPreviewPayload(payload) { appState.preview = payload || null; }
export function appendGeneratedReport(result) { if (!result) return; const existing = Array.isArray(appState.generatedReports) ? appState.generatedReports : []; appState.generatedReports = [result, ...existing].slice(0, 10); }
export function applySendPackagePayload(payload) { appState.sendPackage = payload || null; }

export function setActiveSessionContext(context = {}) {
  appState.selectedShift = context.selectedShift || appState.selectedShift || null;
  appState.selectedShiftLabel = context.selectedShiftLabel || appState.selectedShiftLabel || null;
  appState.activeSessionMode = context.activeSessionMode || appState.activeSessionMode || null;
  appState.activeSessionDate = context.activeSessionDate || appState.activeSessionDate || null;
  appState.activeSessionStatus = context.activeSessionStatus || appState.activeSessionStatus || null;
  appState.activeSessionId = context.activeSessionId ?? appState.activeSessionId ?? null;
  appState.activeSessionSummary = context.activeSessionSummary || appState.activeSessionSummary || null;
}
