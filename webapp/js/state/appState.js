import { createInitialSessionState } from '../models/contracts.js';

export const appState = {
  currentRoute: 'shift',
  session: createInitialSessionState(),
  activeDepartmentName: null,
  activeDepartment: null,
  activeAttachments: [],
  selectedAttachmentId: null,
  viewerState: null,
  activeBudget: null,
  budgetSummary: null
};

export function applySessionPayload(sessionPayload) {
  appState.activeDepartmentName = null;
  appState.activeDepartment = null;
  appState.activeAttachments = [];
  appState.selectedAttachmentId = null;
  appState.viewerState = null;
  appState.activeBudget = null;
  appState.budgetSummary = null;

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
}

export function setActiveDepartmentName(deptName) {
  appState.activeDepartmentName = deptName || null;
}

export function applyActiveDepartmentPayload(payload) {
  appState.activeDepartment = payload || null;
  appState.activeDepartmentName = payload?.deptName || appState.activeDepartmentName;
}

export function applyDepartmentSummaryPayload(departments) {
  if (!Array.isArray(departments)) {
    return;
  }

  appState.session.departments = departments;

  const latest = departments
    .filter((dept) => dept?.updatedAt)
    .sort((a, b) => String(b.updatedAt).localeCompare(String(a.updatedAt)))[0];

  if (latest) {
    appState.session.updatedAt = latest.updatedAt;
    appState.session.updatedBy = latest.updatedBy || appState.session.updatedBy;
  }
}

export function applyAttachmentListPayload(listPayload) {
  appState.activeAttachments = Array.isArray(listPayload?.attachments) ? listPayload.attachments : [];

  if (!appState.activeAttachments.some((item) => item.attachmentId === appState.selectedAttachmentId)) {
    appState.selectedAttachmentId = appState.activeAttachments[0]?.attachmentId ?? null;
  }
}

export function setSelectedAttachmentId(attachmentId) {
  appState.selectedAttachmentId = attachmentId || null;
}

export function applyViewerPayload(viewerPayload) {
  appState.viewerState = viewerPayload || null;
  appState.selectedAttachmentId = viewerPayload?.current?.attachmentId || appState.selectedAttachmentId;
}

export function applyBudgetPayload(payload) {
  appState.activeBudget = payload || null;
  appState.budgetSummary = payload?.summary || payload?.totals || null;
}

export function applyBudgetSummaryPayload(payload) {
  appState.budgetSummary = payload || null;
}
