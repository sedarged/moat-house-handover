import { createInitialSessionState } from '../models/contracts.js';

export const appState = {
  currentRoute: 'home',
  selectedShift: null,
  runtimeStatus: null,
  runtimeStatusError: null,
  session: createInitialSessionState(),
  activeDepartmentName: null,
  selectedAttachmentId: null
};

export function setSelectedShift(shiftCode) { appState.selectedShift = shiftCode || null; }
export function setRuntimeStatus(status) { appState.runtimeStatus = status || null; appState.runtimeStatusError = null; }
export function setRuntimeStatusError(message) { appState.runtimeStatusError = message || 'Status unavailable'; }
export function applySessionPayload(payload) { if (payload) appState.session = { ...appState.session, ...payload }; }
export function setActiveDepartmentName(name) { appState.activeDepartmentName = name || null; }
export function setSelectedAttachmentId(id) { appState.selectedAttachmentId = id || null; }
